// Copyright (c) Microsoft. All rights reserved.

#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace Microsoft.Build.PerformanceTests.Measurements;

/// <summary>Configures the startup hook and aggregates its per-process evaluation metric files.</summary>
internal sealed class EvaluationMetricsSession : IDisposable
{
    private static readonly TimeSpan s_defaultCompletionTimeout = TimeSpan.FromSeconds(2);
    private static readonly string[] s_passCodes = ["pass0", "pass1", "pass2", "pass3", "pass3dot1", "pass4", "pass5"];

    private const string EvaluationDurationInstrumentName = "msbuild.project.evaluation.duration";
    private const string IdentityMetricsHeader =
        "timestamp\tinstrument\tstage\tpass\torigin\tsucceeded\tdurationSeconds\tprojectFileBase64\tevaluationId";
    private const string LegacyMetricsHeader =
        "timestamp\tinstrument\tstage\tpass\torigin\tsucceeded\tdurationSeconds";
    private const string LifecycleHeader =
        "version\tsession\tprocessId\thookStartTimestamp\tlistenerStartTimestamp\tcompletionTimestamp\tcapabilityTimestamp";
    private const int LifecycleVersion = 2;
    private const string PassDurationInstrumentName = "msbuild.project.evaluation.pass.duration";
    private const string HookAssemblyName = "PerfStar.EvaluationMetrics.StartupHook.dll";
    private const string OutputDirectoryEnvironmentVariable = "PERFSTAR_EVALUATION_METRICS_DIR";
    private const string SessionEnvironmentVariable = "PERFSTAR_EVALUATION_METRICS_SESSION";
    private const string StartupHooksEnvironmentVariable = "DOTNET_STARTUP_HOOKS";
    private const string DisableNodeReuseEnvironmentVariable = "MSBUILDDISABLENODEREUSE";
    private const string UseServerEnvironmentVariable = "MSBUILDUSESERVER";
    private const string ServerProcessMarkerSuffix = ".server";

    private readonly string _outputDirectory;
    private readonly string _session;
    private readonly string _startupHookPath;
    private readonly bool _forceProcessExit;
    private readonly bool _preserveFiles;
    private bool _disposed;

    public EvaluationMetricsSession(string workingDirectory, bool forceProcessExit = true,
        string? sessionId = null, bool preserveFiles = false)
    {
        var assemblyDirectory = Path.GetDirectoryName(typeof(EvaluationMetricsSession).Assembly.Location)
            ?? throw new InvalidOperationException("PerformanceTests assembly directory was not available.");
        _startupHookPath = Path.Combine(assemblyDirectory, HookAssemblyName);
        if (!File.Exists(_startupHookPath))
        {
            throw new FileNotFoundException("Evaluation metrics startup hook was not found.", _startupHookPath);
        }

        _outputDirectory = Path.Combine(workingDirectory, ".perfstar-evaluation-metrics");
        Directory.CreateDirectory(_outputDirectory);
        if (sessionId is not null && !Guid.TryParseExact(sessionId, "N", out _))
        {
            throw new ArgumentException("Expected a complete metrics session ID.", nameof(sessionId));
        }
        _session = sessionId ?? Guid.NewGuid().ToString("N");
        _forceProcessExit = forceProcessExit;
        _preserveFiles = preserveFiles;
        EnvironmentVariables = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [OutputDirectoryEnvironmentVariable] = _outputDirectory,
            [SessionEnvironmentVariable] = _session,
            [StartupHooksEnvironmentVariable] = AppendStartupHook(
                Environment.GetEnvironmentVariable(StartupHooksEnvironmentVariable),
                _startupHookPath),
        };
    }

    public Dictionary<string, string> EnvironmentVariables { get; }

    internal Action? FilesSettledForTests { get; set; }

    public Dictionary<string, string> AddToEnvironment(Dictionary<string, string>? environmentVariables)
    {
        var result = environmentVariables is null
            ? new Dictionary<string, string>(StringComparer.Ordinal)
            : new Dictionary<string, string>(environmentVariables, StringComparer.Ordinal);

        result[OutputDirectoryEnvironmentVariable] = _outputDirectory;
        result[SessionEnvironmentVariable] = _session;
        if (_forceProcessExit)
        {
            result[DisableNodeReuseEnvironmentVariable] = "1";
            result[UseServerEnvironmentVariable] = "0";
        }
        result.TryGetValue(StartupHooksEnvironmentVariable, out var existingStartupHooks);
        existingStartupHooks ??= Environment.GetEnvironmentVariable(StartupHooksEnvironmentVariable);
        result[StartupHooksEnvironmentVariable] = AppendStartupHook(existingStartupHooks, _startupHookPath);

        return result;
    }

    public EvaluationMetricsSnapshot? Read(bool waitForFirstFile = false)
        => ReadRecordsAndAggregate(waitForFirstFile, s_defaultCompletionTimeout);

    public EvaluationMetricsSnapshot? Read(TimeSpan completionTimeout)
        => ReadRecordsAndAggregate(
            waitForFirstFile: true,
            completionTimeout,
            requireServerProcess: false);

    public EvaluationMetricsSnapshot? ReadFromServer(TimeSpan completionTimeout)
        => ReadRecordsAndAggregate(
            waitForFirstFile: true,
            completionTimeout,
            requireServerProcess: true);

    private EvaluationMetricsSnapshot? ReadRecordsAndAggregate(
        bool waitForFirstFile,
        TimeSpan completionTimeout,
        bool requireServerProcess = false)
    {
        var collection = ReadCollection(waitForFirstFile, completionTimeout);
        if (requireServerProcess)
        {
            EnsureServerProcessProducedMetrics(collection.Records);
        }

        return Aggregate(collection.Records);
    }

    public EvaluationMetricsWindowReadResult ReadByTimeWindows(
        IReadOnlyList<EvaluationMetricsTimeWindow> windows,
        bool waitForFirstFile = false)
    {
        var collection = ReadCollection(
            waitForFirstFile,
            s_defaultCompletionTimeout);
        List<EvaluationMetricsSnapshot?> results = new(windows.Count);
        foreach (var window in windows)
        {
            var snapshot = Aggregate(
                collection.Records.Where(record =>
                    record.Timestamp >= window.StartTimestamp &&
                    record.Timestamp <= window.EndTimestamp));
            results.Add(snapshot ?? (collection.CanReportZero(window) ? CreateZeroSnapshot() : null));
        }
        return new(collection.IsAvailable, results);
    }

    private EvaluationMetricsCollectionReadResult ReadCollection(
        bool waitForFirstFile,
        TimeSpan completionTimeout)
    {
        if (waitForFirstFile && completionTimeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(completionTimeout),
                completionTimeout,
                "The evaluation metrics completion timeout must be positive.");
        }

        var completionTimeoutSeconds = completionTimeout.TotalSeconds.ToString(
            "R",
            CultureInfo.InvariantCulture);

        ThrowIfCollectorFailed();

        var files = GetMetricFiles();
        var activeCollectorsExist = ActiveCollectorsExist();
        if (!waitForFirstFile && activeCollectorsExist)
        {
            return EvaluationMetricsCollectionReadResult.Empty;
        }

        if (waitForFirstFile && (files.Length == 0 || activeCollectorsExist))
        {
            var firstFileWait = Stopwatch.StartNew();
            while ((files.Length == 0 || activeCollectorsExist) &&
                   firstFileWait.Elapsed < completionTimeout)
            {
                Thread.Sleep(100);
                ThrowIfCollectorFailed();
                files = GetMetricFiles();
                activeCollectorsExist = ActiveCollectorsExist();
            }
        }

        if (activeCollectorsExist)
        {
            throw new EvaluationMetricsCollectionTimeoutException(
                $"Evaluation metrics collectors did not finish within {completionTimeoutSeconds} seconds.");
        }

        if (files.Length == 0)
        {
            if (GetLifecycleFiles().Length != 0 || TemporaryCollectionFilesExist())
            {
                throw new EvaluationMetricsIncompleteCollectionException(
                    "Evaluation metrics collector started but did not produce a completed metrics file.");
            }

            if (waitForFirstFile)
            {
                throw new EvaluationMetricsCollectionTimeoutException(
                    $"Evaluation metrics collector did not produce a completion file within {completionTimeoutSeconds} seconds.");
            }

            return EvaluationMetricsCollectionReadResult.Empty;
        }

        var settledFiles = WaitForFilesToSettle(
            new CollectionFileState(files, GetLifecycleFiles()));
        ThrowIfCollectorFailed();
        FilesSettledForTests?.Invoke();
        ThrowIfCollectorFailed();
        if (ActiveCollectorsExist())
        {
            throw new EvaluationMetricsIncompleteCollectionException(
                "Evaluation metrics collector cohort changed after files appeared to settle.");
        }
        if (TemporaryCollectionFilesExist())
        {
            throw new EvaluationMetricsIncompleteCollectionException(
                "Evaluation metrics collector left an incomplete temporary file.");
        }

        CollectionFileState currentFiles = new(GetMetricFiles(), GetLifecycleFiles());
        if (!settledFiles.HasSameFiles(currentFiles))
        {
            throw new EvaluationMetricsIncompleteCollectionException(
                "Evaluation metrics collector cohort changed after files appeared to settle.");
        }

        List<EvaluationMetricRecord> records = [];
        HashSet<int> metricProcessIds = [];

        foreach (var file in settledFiles.MetricFiles)
        {
            var processId = GetMetricProcessId(file);
            metricProcessIds.Add(processId);
            var lines = File.ReadAllLines(file);
            bool hasIdentity;
            if (lines.Length == 0)
            {
                throw new InvalidDataException($"Invalid evaluation metrics file '{file}'.");
            }
            if (string.Equals(lines[0], IdentityMetricsHeader, StringComparison.Ordinal))
            {
                hasIdentity = true;
            }
            else if (string.Equals(lines[0], LegacyMetricsHeader, StringComparison.Ordinal))
            {
                hasIdentity = false;
            }
            else
            {
                throw new InvalidDataException($"Invalid evaluation metrics file '{file}'.");
            }

            for (var i = 1; i < lines.Length; i++)
            {
                var fields = lines[i].Split('\t');
                if (fields.Length != (hasIdentity ? 9 : 7) ||
                    !long.TryParse(fields[0], NumberStyles.None, CultureInfo.InvariantCulture, out var timestamp) ||
                    !double.TryParse(fields[6], NumberStyles.Float, CultureInfo.InvariantCulture, out var durationSeconds))
                {
                    throw new InvalidDataException($"Invalid evaluation metrics row in '{file}': {lines[i]}");
                }

                bool? succeeded = null;
                if (!string.IsNullOrEmpty(fields[5]))
                {
                    if (!bool.TryParse(fields[5], out var parsedSucceeded))
                    {
                        throw new InvalidDataException($"Invalid evaluation metrics row in '{file}': {lines[i]}");
                    }
                    succeeded = parsedSucceeded;
                }

                string? projectFile = null;
                int? evaluationId = null;
                if (hasIdentity)
                {
                    try
                    {
                        projectFile = new UTF8Encoding(
                            encoderShouldEmitUTF8Identifier: false,
                            throwOnInvalidBytes: true).GetString(Convert.FromBase64String(fields[7]));
                    }
                    catch (Exception ex) when (ex is FormatException or DecoderFallbackException)
                    {
                        throw new InvalidDataException(
                            $"Invalid evaluation metrics row in '{file}': {lines[i]}",
                            ex);
                    }

                    if (!int.TryParse(
                            fields[8],
                            NumberStyles.Integer,
                            CultureInfo.InvariantCulture,
                            out var parsedEvaluationId))
                    {
                        throw new InvalidDataException($"Invalid evaluation metrics row in '{file}': {lines[i]}");
                    }
                    evaluationId = parsedEvaluationId;
                }

                records.Add(new(
                    processId,
                    timestamp,
                    fields[1],
                    fields[2],
                    fields[3],
                    fields[4],
                    succeeded,
                    durationSeconds,
                    projectFile,
                    evaluationId));
            }
        }

        var lifecycleRecords =
            ReadLifecycleRecords(settledFiles.LifecycleFiles, metricProcessIds, records);
        if (lifecycleRecords.Count != 0 &&
            records.Count != 0 &&
            !lifecycleRecords.Any(lifecycle => lifecycle.CapabilityTimestamp.HasValue))
        {
            throw new EvaluationMetricsIncompleteCollectionException(
                "Evaluation metrics were recorded without a validated completed-evaluation capability event.");
        }
        ThrowIfCollectorFailed();
        if (ActiveCollectorsExist() ||
            TemporaryCollectionFilesExist() ||
            !settledFiles.HasSameFiles(new(GetMetricFiles(), GetLifecycleFiles())))
        {
            throw new EvaluationMetricsIncompleteCollectionException(
                "Evaluation metrics collector cohort changed while files were being read.");
        }

        foreach (var file in settledFiles.MetricFiles)
        {
            if (!_preserveFiles)
            {
                File.Delete(file);
            }
        }
        foreach (var file in settledFiles.LifecycleFiles)
        {
            if (!_preserveFiles)
            {
                File.Delete(file);
            }
        }

        return new(records, lifecycleRecords);
    }

    private List<EvaluationMetricsLifecycleRecord> ReadLifecycleRecords(
        string[] lifecycleFiles,
        HashSet<int> metricProcessIds,
        List<EvaluationMetricRecord> records)
    {
        if (lifecycleFiles.Length == 0)
        {
            return [];
        }

        List<EvaluationMetricsLifecycleRecord> lifecycleRecords = new(lifecycleFiles.Length);
        HashSet<int> lifecycleProcessIds = [];
        foreach (var file in lifecycleFiles)
        {
            var fileProcessId = GetLifecycleProcessId(file);
            var lines = File.ReadAllLines(file);
            if (lines.Length != 2 ||
                !string.Equals(lines[0], LifecycleHeader, StringComparison.Ordinal))
            {
                throw new InvalidDataException($"Invalid evaluation metrics lifecycle file '{file}'.");
            }

            var fields = lines[1].Split('\t');
            if (fields.Length != 7 ||
                !int.TryParse(fields[0], NumberStyles.None, CultureInfo.InvariantCulture, out var version) ||
                version != LifecycleVersion ||
                !string.Equals(fields[1], _session, StringComparison.Ordinal) ||
                !int.TryParse(fields[2], NumberStyles.None, CultureInfo.InvariantCulture, out var processId) ||
                processId <= 0 ||
                processId != fileProcessId ||
                !long.TryParse(fields[3], NumberStyles.None, CultureInfo.InvariantCulture, out var hookStartTimestamp) ||
                hookStartTimestamp <= 0 ||
                !long.TryParse(fields[5], NumberStyles.None, CultureInfo.InvariantCulture, out var completionTimestamp) ||
                completionTimestamp < hookStartTimestamp)
            {
                throw new InvalidDataException($"Invalid evaluation metrics lifecycle row in '{file}': {lines[1]}");
            }

            long? listenerStartTimestamp = null;
            if (!string.IsNullOrEmpty(fields[4]))
            {
                if (!long.TryParse(
                        fields[4],
                        NumberStyles.None,
                        CultureInfo.InvariantCulture,
                        out var parsedListenerStartTimestamp) ||
                    parsedListenerStartTimestamp < hookStartTimestamp ||
                    parsedListenerStartTimestamp > completionTimestamp)
                {
                    throw new InvalidDataException(
                        $"Invalid evaluation metrics lifecycle row in '{file}': {lines[1]}");
                }
                listenerStartTimestamp = parsedListenerStartTimestamp;
            }

            long? capabilityTimestamp = null;
            if (!string.IsNullOrEmpty(fields[6]))
            {
                if (!long.TryParse(
                        fields[6],
                        NumberStyles.None,
                        CultureInfo.InvariantCulture,
                        out var parsedCapabilityTimestamp) ||
                    !listenerStartTimestamp.HasValue ||
                    parsedCapabilityTimestamp < listenerStartTimestamp.Value ||
                    parsedCapabilityTimestamp > completionTimestamp ||
                    !records.Any(record => IsCapabilityRecord(
                        record,
                        processId,
                        parsedCapabilityTimestamp)))
                {
                    throw new InvalidDataException(
                        $"Invalid evaluation metrics lifecycle row in '{file}': {lines[1]}");
                }
                capabilityTimestamp = parsedCapabilityTimestamp;
            }

            if (!lifecycleProcessIds.Add(processId))
            {
                throw new InvalidDataException(
                    $"Duplicate evaluation metrics lifecycle process ID '{processId}'.");
            }

            lifecycleRecords.Add(new(
                processId,
                hookStartTimestamp,
                listenerStartTimestamp,
                completionTimestamp,
                capabilityTimestamp));
        }

        if (!metricProcessIds.SetEquals(lifecycleProcessIds))
        {
            throw new EvaluationMetricsIncompleteCollectionException(
                "Evaluation metrics files and lifecycle participant cohort did not match.");
        }

        return lifecycleRecords;
    }

    private static bool IsCapabilityRecord(
        EvaluationMetricRecord record,
        int processId,
        long capabilityTimestamp) =>
        record.ProcessId == processId &&
        record.Timestamp == capabilityTimestamp &&
        record.Instrument == EvaluationDurationInstrumentName &&
        record.Stage is "properties" or "item_definitions" or "items" or "using_tasks" or "full" or "unknown" &&
        string.IsNullOrEmpty(record.Pass) &&
        record.Origin is "build_submission" or "outside_build_submission" &&
        record.Succeeded.HasValue &&
        !double.IsInfinity(record.DurationSeconds) &&
        (double.IsNaN(record.DurationSeconds) || record.DurationSeconds >= 0);

    private void EnsureServerProcessProducedMetrics(List<EvaluationMetricRecord> records)
    {
        var serverMarkers = Directory.GetFiles(
            _outputDirectory,
            $"{_session}-*{ServerProcessMarkerSuffix}");
        if (serverMarkers.Length == 0)
        {
            throw new EvaluationMetricsServerNotUsedException(
                "The measured invocation did not start an explicitly identified MSBuild Server process.");
        }

        HashSet<int> serverProcessIds = [];
        foreach (var marker in serverMarkers)
        {
            if (!int.TryParse(
                    File.ReadAllText(marker),
                    NumberStyles.None,
                    CultureInfo.InvariantCulture,
                    out var serverProcessId))
            {
                throw new InvalidDataException($"Invalid MSBuild Server process marker '{marker}'.");
            }

            serverProcessIds.Add(serverProcessId);
        }

        foreach (var record in records)
        {
            if (serverProcessIds.Contains(record.ProcessId))
            {
                return;
            }
        }

        throw new EvaluationMetricsServerNotUsedException(
            "The explicitly identified MSBuild Server process produced no evaluation metrics.");
    }

    private int GetMetricProcessId(string path)
    {
        var fileName = Path.GetFileName(path);
        var prefix = _session + "-";
        const string suffix = ".metrics.tsv";
        if (!fileName.StartsWith(prefix, StringComparison.Ordinal) ||
            !fileName.EndsWith(suffix, StringComparison.Ordinal) ||
            !int.TryParse(
                fileName.AsSpan(prefix.Length, fileName.Length - prefix.Length - suffix.Length),
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out var processId) ||
            processId <= 0)
        {
            throw new InvalidDataException($"Invalid evaluation metrics file name '{fileName}'.");
        }

        return processId;
    }

    private int GetLifecycleProcessId(string path)
    {
        var fileName = Path.GetFileName(path);
        var prefix = _session + "-";
        const string suffix = ".lifecycle.tsv";
        if (!fileName.StartsWith(prefix, StringComparison.Ordinal) ||
            !fileName.EndsWith(suffix, StringComparison.Ordinal) ||
            !int.TryParse(
                fileName.AsSpan(prefix.Length, fileName.Length - prefix.Length - suffix.Length),
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out var processId) ||
            processId <= 0)
        {
            throw new InvalidDataException($"Invalid evaluation metrics lifecycle file name '{fileName}'.");
        }

        return processId;
    }

    private static EvaluationMetricsSnapshot? Aggregate(
        IEnumerable<EvaluationMetricRecord> records)
    {
        EvaluationMetricsSnapshotBuilder snapshot = new();
        var hasRecords = false;
        foreach (var record in records)
        {
            hasRecords = true;
            var isOutsideBuildSubmission = IsOutsideBuildSubmissionOrigin(record.Origin);
            switch (record.Instrument)
            {
                case EvaluationDurationInstrumentName:
                    if (!record.Succeeded.HasValue)
                    {
                        throw new InvalidDataException("Evaluation duration measurement is missing the succeeded tag.");
                    }
                    if (double.IsInfinity(record.DurationSeconds) || record.DurationSeconds < 0)
                    {
                        throw new InvalidDataException("Evaluation duration measurement has an invalid duration.");
                    }
                    snapshot.AddEvaluation(record, isOutsideBuildSubmission);
                    break;

                case PassDurationInstrumentName:
                    if (!double.IsFinite(record.DurationSeconds) || record.DurationSeconds < 0)
                    {
                        throw new InvalidDataException("Evaluation pass duration measurement has an invalid duration.");
                    }
                    var pass = GetPassCode(record.Pass)
                        ?? throw new InvalidDataException($"Unknown evaluation pass: {record.Pass}");
                    snapshot.AddPass(pass, record.DurationSeconds, isOutsideBuildSubmission);
                    break;

                default:
                    throw new InvalidDataException($"Unknown evaluation metrics instrument: {record.Instrument}");
            }
        }
        return hasRecords ? snapshot.ToSnapshot() : null;
    }

    private static EvaluationMetricsSnapshot CreateZeroSnapshot()
    {
        var passDurations = s_passCodes.ToDictionary(
            pass => pass,
            _ => 0d,
            StringComparer.Ordinal);
        return new(
            TotalCount: 0,
            TotalDurationMilliseconds: 0,
            OutsideBuildSubmissionCount: 0,
            OutsideBuildSubmissionDurationMilliseconds: 0,
            PassDurationsMilliseconds: passDurations,
            OutsideBuildSubmissionPassDurationsMilliseconds:
                new Dictionary<string, double>(passDurations, StringComparer.Ordinal),
            MinimumEvaluation: null,
            MaximumEvaluation: null);
    }

    public void Dispose()
    {
        if (_disposed || _preserveFiles)
        {
            return;
        }

        try
        {
            foreach (var file in Directory.GetFiles(_outputDirectory, $"{_session}-*.*"))
            {
                File.Delete(file);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Console.Error.WriteLine($"Unable to clean evaluation metrics session {_session}: {ex.Message}");
        }

        _disposed = true;
    }

    private CollectionFileState WaitForFilesToSettle(CollectionFileState initialFiles)
    {
        var stopwatch = Stopwatch.StartNew();
        var previousFiles = initialFiles;
        var stableChecks = 0;

        while (stopwatch.Elapsed < TimeSpan.FromSeconds(5))
        {
            Thread.Sleep(100);
            ThrowIfCollectorFailed();

            CollectionFileState currentFiles = new(GetMetricFiles(), GetLifecycleFiles());
            if (!TemporaryCollectionFilesExist() &&
                !ActiveCollectorsExist() &&
                currentFiles.HasSameFiles(previousFiles))
            {
                stableChecks++;
                if (stopwatch.Elapsed >= TimeSpan.FromMilliseconds(500) && stableChecks >= 3)
                {
                    return currentFiles;
                }
            }
            else
            {
                stableChecks = 0;
            }

            previousFiles = currentFiles;
        }

        throw new EvaluationMetricsIncompleteCollectionException(
            "Evaluation metrics collector files did not settle to a completed cohort.");
    }

    private string[] GetMetricFiles()
    {
        var files = Directory.GetFiles(_outputDirectory, $"{_session}-*.metrics.tsv");
        Array.Sort(files, StringComparer.Ordinal);
        return files;
    }

    private string[] GetLifecycleFiles()
    {
        var files = Directory.GetFiles(_outputDirectory, $"{_session}-*.lifecycle.tsv");
        Array.Sort(files, StringComparer.Ordinal);
        return files;
    }

    private bool TemporaryCollectionFilesExist() =>
        Directory.GetFiles(_outputDirectory, $"{_session}-*.metrics.tsv.tmp").Length != 0 ||
        Directory.GetFiles(_outputDirectory, $"{_session}-*.lifecycle.tsv.tmp").Length != 0;

    private bool ActiveCollectorsExist()
    {
        var activeCollectorsExist = false;
        foreach (var markerPath in Directory.GetFiles(
                     _outputDirectory,
                     $"{_session}-*.metrics.tsv.active"))
        {
            string markerContents;
            try
            {
                markerContents = File.ReadAllText(markerPath);
            }
            catch (FileNotFoundException)
            {
                continue;
            }
            catch (DirectoryNotFoundException)
            {
                continue;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                activeCollectorsExist = true;
                continue;
            }

            if (!int.TryParse(
                    markerContents,
                    NumberStyles.None,
                    CultureInfo.InvariantCulture,
                    out var processId))
            {
                activeCollectorsExist = true;
                continue;
            }

            try
            {
                using (var process = Process.GetProcessById(processId))
                {
                    if (!process.HasExited)
                    {
                        activeCollectorsExist = true;
                    }
                }
            }
            catch (ArgumentException)
            {
                DeleteStaleMarker(markerPath, ref activeCollectorsExist);
            }
            catch (InvalidOperationException)
            {
                DeleteStaleMarker(markerPath, ref activeCollectorsExist);
            }
        }

        return activeCollectorsExist;
    }

    private static void DeleteStaleMarker(string markerPath, ref bool activeCollectorsExist)
    {
        try
        {
            File.Delete(markerPath);
        }
        catch (FileNotFoundException)
        {
        }
        catch (DirectoryNotFoundException)
        {
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            activeCollectorsExist = true;
        }
    }

    private void ThrowIfCollectorFailed()
    {
        var errors = Directory.GetFiles(_outputDirectory, $"{_session}-*.error.txt");
        if (errors.Length == 0)
        {
            return;
        }

        throw new InvalidOperationException(
            $"Evaluation metrics startup hook failed: {File.ReadAllText(errors[0])}");
    }

    private static string AppendStartupHook(string? existing, string startupHookPath)
    {
        if (string.IsNullOrWhiteSpace(existing))
        {
            return startupHookPath;
        }

        var comparison = OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;
        foreach (var path in existing.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            if (string.Equals(path, startupHookPath, comparison))
            {
                return existing;
            }
        }

        return existing + Path.PathSeparator + startupHookPath;
    }

    private static bool IsOutsideBuildSubmissionOrigin(string origin) =>
        origin is "outside_build_submission" or "standalone";

    private static string? GetPassCode(string pass) => pass switch
    {
        "initial_properties" => "pass0",
        "properties" => "pass1",
        "item_definitions" => "pass2",
        "items" => "pass3",
        "lazy_items" => "pass3dot1",
        "using_tasks" => "pass4",
        "targets" => "pass5",
        _ => null,
    };

    private sealed record EvaluationMetricRecord(
        int ProcessId,
        long Timestamp,
        string Instrument,
        string Stage,
        string Pass,
        string Origin,
        bool? Succeeded,
        double DurationSeconds,
        string? ProjectFile,
        int? EvaluationId);

    private sealed record EvaluationMetricsLifecycleRecord(
        int ProcessId,
        long HookStartTimestamp,
        long? ListenerStartTimestamp,
        long CompletionTimestamp,
        long? CapabilityTimestamp);

    private sealed class EvaluationMetricsCollectionReadResult
    {
        public static EvaluationMetricsCollectionReadResult Empty { get; } = new([], []);

        private readonly IReadOnlyList<EvaluationMetricsLifecycleRecord> _lifecycleRecords;

        public EvaluationMetricsCollectionReadResult(
            List<EvaluationMetricRecord> records,
            IReadOnlyList<EvaluationMetricsLifecycleRecord> lifecycleRecords)
        {
            Records = records;
            _lifecycleRecords = lifecycleRecords;
        }

        public bool IsAvailable =>
            _lifecycleRecords.Count == 0
                ? Records.Count != 0
                : _lifecycleRecords.Any(lifecycle => lifecycle.CapabilityTimestamp.HasValue);

        public List<EvaluationMetricRecord> Records { get; }

        public bool CanReportZero(EvaluationMetricsTimeWindow window)
        {
            var windowParticipants = _lifecycleRecords
                .Where(lifecycle =>
                    lifecycle.HookStartTimestamp <= window.EndTimestamp &&
                    lifecycle.CompletionTimestamp >= window.StartTimestamp)
                .ToArray();
            return windowParticipants.Length != 0 &&
                windowParticipants.All(lifecycle =>
                    (lifecycle.ListenerStartTimestamp.HasValue &&
                     lifecycle.ListenerStartTimestamp.Value <= window.StartTimestamp) ||
                    (lifecycle.HookStartTimestamp >= window.StartTimestamp &&
                     lifecycle.CompletionTimestamp <= window.EndTimestamp)) &&
                windowParticipants.Any(lifecycle =>
                    lifecycle.CapabilityTimestamp.HasValue &&
                    lifecycle.CapabilityTimestamp.Value <= window.StartTimestamp &&
                    lifecycle.ListenerStartTimestamp.HasValue &&
                    lifecycle.ListenerStartTimestamp.Value <= window.StartTimestamp &&
                    lifecycle.CompletionTimestamp >= window.EndTimestamp);
        }
    }

    private sealed class CollectionFileState
    {
        private readonly FileState[] _fileStates;

        public CollectionFileState(string[] metricFiles, string[] lifecycleFiles)
        {
            MetricFiles = metricFiles;
            LifecycleFiles = lifecycleFiles;
            _fileStates = metricFiles
                .Concat(lifecycleFiles)
                .Select(path =>
                {
                    FileInfo info = new(path);
                    return new FileState(path, info.Length, info.LastWriteTimeUtc.Ticks);
                })
                .ToArray();
        }

        public string[] LifecycleFiles { get; }

        public string[] MetricFiles { get; }

        public bool HasSameFiles(CollectionFileState other) =>
            _fileStates.SequenceEqual(other._fileStates);

        private sealed record FileState(string Path, long Length, long LastWriteTimeUtcTicks);
    }

    private sealed class EvaluationMetricsSnapshotBuilder
    {
        private readonly Dictionary<string, double> _outsideBuildSubmissionPassDurationsSeconds = new(StringComparer.Ordinal);
        private readonly Dictionary<string, double> _passDurationsSeconds = new(StringComparer.Ordinal);
        private EvaluationMetricRecord? _maximumEvaluation;
        private EvaluationMetricRecord? _minimumEvaluation;
        private bool _hasMissingEvaluationIdentity;
        private bool _hasUnknownEvaluationDuration;

        public long TotalCount { get; private set; }

        public double TotalDurationSeconds { get; private set; }

        public long OutsideBuildSubmissionCount { get; private set; }

        public double OutsideBuildSubmissionDurationSeconds { get; private set; }

        public void AddEvaluation(
            EvaluationMetricRecord record,
            bool isOutsideBuildSubmission)
        {
            TotalCount++;
            if (double.IsNaN(record.DurationSeconds))
            {
                _hasUnknownEvaluationDuration = true;
            }
            else
            {
                TotalDurationSeconds += record.DurationSeconds;
                if (!record.EvaluationId.HasValue || record.ProjectFile is null)
                {
                    _hasMissingEvaluationIdentity = true;
                }
                else
                {
                    if (_minimumEvaluation is null || CompareEvaluations(record, _minimumEvaluation) < 0)
                    {
                        _minimumEvaluation = record;
                    }
                    if (_maximumEvaluation is null || CompareEvaluationsByMaximum(record, _maximumEvaluation) < 0)
                    {
                        _maximumEvaluation = record;
                    }
                }
            }
            if (isOutsideBuildSubmission)
            {
                OutsideBuildSubmissionCount++;
                if (!double.IsNaN(record.DurationSeconds))
                {
                    OutsideBuildSubmissionDurationSeconds += record.DurationSeconds;
                }
            }
        }

        public void AddPass(string pass, double durationSeconds, bool isOutsideBuildSubmission)
        {
            AddDuration(_passDurationsSeconds, pass, durationSeconds);
            if (isOutsideBuildSubmission)
            {
                AddDuration(_outsideBuildSubmissionPassDurationsSeconds, pass, durationSeconds);
            }
        }

        public EvaluationMetricsSnapshot ToSnapshot()
        {
            if (_hasUnknownEvaluationDuration)
            {
                throw new EvaluationMetricsIncompleteCollectionException(
                    $"Evaluation metrics included {TotalCount} completed evaluations, but at least one duration " +
                    "was unavailable because collection began after that evaluation started.");
            }

            return new(
                TotalCount,
                TotalDurationSeconds * 1000,
                OutsideBuildSubmissionCount,
                OutsideBuildSubmissionDurationSeconds * 1000,
                ToMilliseconds(_passDurationsSeconds),
                ToMilliseconds(_outsideBuildSubmissionPassDurationsSeconds),
                _hasMissingEvaluationIdentity ? null : ToExtremum(_minimumEvaluation),
                _hasMissingEvaluationIdentity ? null : ToExtremum(_maximumEvaluation));
        }

        private static int CompareEvaluations(
            EvaluationMetricRecord left,
            EvaluationMetricRecord right)
        {
            var durationComparison = left.DurationSeconds.CompareTo(right.DurationSeconds);
            return durationComparison != 0 ? durationComparison : CompareIdentity(left, right);
        }

        private static int CompareEvaluationsByMaximum(
            EvaluationMetricRecord left,
            EvaluationMetricRecord right)
        {
            var durationComparison = right.DurationSeconds.CompareTo(left.DurationSeconds);
            return durationComparison != 0 ? durationComparison : CompareIdentity(left, right);
        }

        private static int CompareIdentity(
            EvaluationMetricRecord left,
            EvaluationMetricRecord right)
        {
            var projectComparison = string.Compare(
                left.ProjectFile,
                right.ProjectFile,
                StringComparison.Ordinal);
            if (projectComparison != 0)
            {
                return projectComparison;
            }

            var processComparison = left.ProcessId.CompareTo(right.ProcessId);
            return processComparison != 0
                ? processComparison
                : left.EvaluationId!.Value.CompareTo(right.EvaluationId!.Value);
        }

        private static EvaluationExtremum? ToExtremum(EvaluationMetricRecord? record) =>
            record is null
                ? null
                : new(
                    record.DurationSeconds * 1000,
                    record.ProjectFile!,
                    record.ProcessId,
                    record.EvaluationId!.Value);

        private static void AddDuration(Dictionary<string, double> durations, string pass, double durationSeconds)
        {
            durations.TryGetValue(pass, out var current);
            durations[pass] = current + durationSeconds;
        }

        private static Dictionary<string, double> ToMilliseconds(Dictionary<string, double> durationsSeconds)
        {
            Dictionary<string, double> result = new(durationsSeconds.Count, StringComparer.Ordinal);
            foreach ((var pass, var durationSeconds) in durationsSeconds)
            {
                result.Add(pass, durationSeconds * 1000);
            }
            return result;
        }
    }
}

internal sealed record EvaluationMetricsTimeWindow(
    long StartTimestamp,
    long EndTimestamp);

internal sealed class EvaluationMetricsCollectionTimeoutException : IOException
{
    public EvaluationMetricsCollectionTimeoutException(string message)
        : base(message)
    {
    }
}

internal sealed class EvaluationMetricsServerNotUsedException : InvalidOperationException
{
    public EvaluationMetricsServerNotUsedException(string message)
        : base(message)
    {
    }
}

internal sealed class EvaluationMetricsIncompleteCollectionException : IOException
{
    public EvaluationMetricsIncompleteCollectionException(string message)
        : base(message)
    {
    }
}

internal sealed record EvaluationMetricsWindowReadResult(
    bool IsAvailable,
    IReadOnlyList<EvaluationMetricsSnapshot?> Windows);

internal sealed record EvaluationMetricsSnapshot(
    long TotalCount,
    double TotalDurationMilliseconds,
    long OutsideBuildSubmissionCount,
    double OutsideBuildSubmissionDurationMilliseconds,
    IReadOnlyDictionary<string, double> PassDurationsMilliseconds,
    IReadOnlyDictionary<string, double> OutsideBuildSubmissionPassDurationsMilliseconds,
    EvaluationExtremum? MinimumEvaluation,
    EvaluationExtremum? MaximumEvaluation);

internal sealed record EvaluationExtremum(
    double DurationMilliseconds,
    string ProjectFile,
    int ProcessId,
    int EvaluationId);
