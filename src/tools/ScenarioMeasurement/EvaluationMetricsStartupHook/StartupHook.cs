// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

/// <summary>Collects MSBuild evaluation metrics in each process and writes them to a per-process TSV file.</summary>
public static class StartupHook
{
    private const string EventSourceName = "Microsoft-Build";
    private const string EvaluationDurationInstrumentName = "msbuild.project.evaluation.duration";
    private const int MaxProjectFileLength = 4096;
    private const string LifecycleHeader =
        "version\tsession\tprocessId\thookStartTimestamp\tlistenerStartTimestamp\tcompletionTimestamp\tcapabilityTimestamp";
    private const int LifecycleVersion = 2;
    private const string PassDurationInstrumentName = "msbuild.project.evaluation.pass.duration";
    private const int ProjectEvaluationCompletedEventId = 113;
    private const int ProjectEvaluationPassCompletedEventId = 114;
    private const string OutputDirectoryEnvironmentVariable = "PERFSTAR_EVALUATION_METRICS_DIR";
    private const string SessionEnvironmentVariable = "PERFSTAR_EVALUATION_METRICS_SESSION";
    private const string ServerNodeModeArgument = "/nodemode:8";
    private const EventKeywords EvaluationMeasurementsKeyword = (EventKeywords)0x4;

    private static readonly object s_lock = new();
    private static readonly List<MeasurementRecord> s_measurements = [];

    private static bool s_activeMarkerCreated;
    private static string? s_activePath;
    private static long? s_capabilityTimestamp;
    private static long s_hookStartTimestamp;
    private static EventListener? s_listener;
    private static string? s_lifecyclePath;
    private static long? s_listenerStartTimestamp;
    private static string? s_errorPath;
    private static string? s_outputPath;
    private static string? s_session;
    private static int s_errorWritten;
    private static bool s_processExitRegistered;
    private static bool s_lifecycleCompleted;

    public static void Initialize() => Initialize(EventSourceName);

    internal static void InitializeForTests(string eventSourceName) => Initialize(eventSourceName);

    private static void Initialize(string eventSourceName)
    {
        try
        {
            var outputDirectory = Environment.GetEnvironmentVariable(OutputDirectoryEnvironmentVariable);
            var session = Environment.GetEnvironmentVariable(SessionEnvironmentVariable);
            if (string.IsNullOrWhiteSpace(outputDirectory) || string.IsNullOrWhiteSpace(session))
            {
                return;
            }

            s_hookStartTimestamp = Stopwatch.GetTimestamp();
            Directory.CreateDirectory(outputDirectory);
            var sanitizedSession = SanitizeFileName(session);
            s_outputPath = Path.Combine(
                outputDirectory,
                $"{sanitizedSession}-{Environment.ProcessId.ToString(CultureInfo.InvariantCulture)}.metrics.tsv");
            s_errorPath = Path.ChangeExtension(s_outputPath, ".error.txt");
            s_activePath = s_outputPath + ".active";
            s_lifecyclePath = Path.Combine(
                outputDirectory,
                $"{sanitizedSession}-{Environment.ProcessId.ToString(CultureInfo.InvariantCulture)}.lifecycle.tsv");
            s_session = sanitizedSession;
            if (IsMSBuildServerProcess(Environment.GetCommandLineArgs()))
            {
                File.WriteAllText(
                    Path.Combine(
                        outputDirectory,
                        $"{sanitizedSession}-{Environment.ProcessId.ToString(CultureInfo.InvariantCulture)}.server"),
                    Environment.ProcessId.ToString(CultureInfo.InvariantCulture));
            }
            var isSessionLeader = TryClaimSessionLeader(outputDirectory, sanitizedSession);

            var listener = new EvaluationEventListener(eventSourceName);
            s_listener = listener;
            if (isSessionLeader)
            {
                ActivateLifecycleTracking();
            }
            listener.Start();

            AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
            s_processExitRegistered = true;
        }
        catch (Exception ex)
        {
            WriteError(ex);
            s_listener?.Dispose();
            s_listener = null;
            s_outputPath = null;
            DeleteActiveMarker();
        }
    }

    private static void RecordEvent(EventWrittenEventArgs eventData)
    {
        switch (eventData.EventId)
        {
            case ProjectEvaluationCompletedEventId:
                ValidateEventContract(
                    eventData,
                    nameof(ProjectEvaluationCompletedEventId),
                    "ProjectEvaluationCompleted",
                    expectedVersion: 0,
                    ["durationSeconds", "stage", "origin", "succeeded", "projectFile", "evaluationId"]);
                (var projectFile, var evaluationId) = GetEvaluationIdentity(eventData);
                AddMeasurement(
                    EvaluationDurationInstrumentName,
                    GetDuration(eventData, allowNaN: true),
                    GetStage(eventData, 1),
                    string.Empty,
                    GetOrigin(eventData, 2),
                    GetPayload<bool>(eventData, 3),
                    projectFile,
                    evaluationId,
                    establishesCapability: true);
                break;

            case ProjectEvaluationPassCompletedEventId:
                ValidateEventContract(
                    eventData,
                    nameof(ProjectEvaluationPassCompletedEventId),
                    "ProjectEvaluationPassCompleted",
                    expectedVersion: 0,
                    ["durationSeconds", "stage", "pass", "origin", "projectFile", "evaluationId"]);
                (projectFile, evaluationId) = GetEvaluationIdentity(eventData);
                AddMeasurement(
                    PassDurationInstrumentName,
                    GetDuration(eventData, allowNaN: false),
                    GetStage(eventData, 1),
                    GetPass(eventData, 2),
                    GetOrigin(eventData, 3),
                    succeeded: null,
                    projectFile,
                    evaluationId,
                    establishesCapability: false);
                break;
        }
    }

    private static void AddMeasurement(
        string instrument,
        double durationSeconds,
        string stage,
        string pass,
        string origin,
        bool? succeeded,
        string projectFile,
        int evaluationId,
        bool establishesCapability)
    {
        var record = new MeasurementRecord(
            Stopwatch.GetTimestamp(),
            instrument,
            stage,
            pass,
            origin,
            succeeded,
            durationSeconds,
            projectFile,
            evaluationId);
        lock (s_lock)
        {
            s_measurements.Add(record);
            if (establishesCapability &&
                (!s_capabilityTimestamp.HasValue || record.Timestamp < s_capabilityTimestamp.Value))
            {
                s_capabilityTimestamp = record.Timestamp;
            }
        }
    }

    private static void ValidateEventContract(
        EventWrittenEventArgs eventData,
        string eventIdName,
        string expectedEventName,
        byte expectedVersion,
        string[] expectedPayloadNames)
    {
        if (eventData.Level != EventLevel.Informational ||
            eventData.Opcode != EventOpcode.Info ||
            (eventData.Keywords & EvaluationMeasurementsKeyword) == 0 ||
            !string.Equals(eventData.EventName, expectedEventName, StringComparison.Ordinal) ||
            eventData.Version != expectedVersion ||
            eventData.Payload is null ||
            eventData.PayloadNames is null ||
            eventData.Payload.Count != expectedPayloadNames.Length ||
            !eventData.PayloadNames.SequenceEqual(expectedPayloadNames, StringComparer.Ordinal))
        {
            throw new InvalidDataException(
                $"MSBuild {eventIdName} event did not match the expected evaluation measurement contract.");
        }
    }

    private static (string ProjectFile, int EvaluationId) GetEvaluationIdentity(EventWrittenEventArgs eventData)
    {
        var projectFile = GetPayload<string>(eventData, 4);
        if (projectFile.Length > MaxProjectFileLength)
        {
            throw new InvalidDataException(
                $"MSBuild evaluation event {eventData.EventId} project file exceeded {MaxProjectFileLength} characters.");
        }

        return (projectFile, GetPayload<int>(eventData, 5));
    }

    private static double GetDuration(EventWrittenEventArgs eventData, bool allowNaN)
    {
        var durationSeconds = GetPayload<double>(eventData, 0);
        if (double.IsInfinity(durationSeconds) ||
            durationSeconds < 0 ||
            (!allowNaN && double.IsNaN(durationSeconds)))
        {
            throw new InvalidDataException(
                $"MSBuild evaluation event {eventData.EventId} contained an invalid duration.");
        }

        return durationSeconds;
    }

    private static string GetStage(EventWrittenEventArgs eventData, int index)
    {
        var stage = GetPayload<string>(eventData, index);
        return stage is "properties" or "item_definitions" or "items" or "using_tasks" or "full" or "unknown"
            ? stage
            : throw new InvalidDataException(
                $"MSBuild evaluation event {eventData.EventId} contained unknown stage '{stage}'.");
    }

    private static string GetPass(EventWrittenEventArgs eventData, int index)
    {
        var pass = GetPayload<string>(eventData, index);
        return pass is "initial_properties" or "properties" or "item_definitions" or "items" or "using_tasks" or "targets"
            ? pass
            : throw new InvalidDataException(
                $"MSBuild evaluation event {eventData.EventId} contained unknown pass '{pass}'.");
    }

    private static string GetOrigin(EventWrittenEventArgs eventData, int index)
    {
        var origin = GetPayload<string>(eventData, index);
        return origin is "build_submission" or "outside_build_submission"
            ? origin
            : throw new InvalidDataException(
                $"MSBuild evaluation event {eventData.EventId} contained unknown origin '{origin}'.");
    }

    private static T GetPayload<T>(EventWrittenEventArgs eventData, int index)
    {
        var value = eventData.Payload![index];
        return value is T typedValue
            ? typedValue
            : throw new InvalidDataException(
                $"MSBuild evaluation event {eventData.EventId} payload '{eventData.PayloadNames![index]}' " +
                $"was not a {typeof(T).Name}.");
    }

    private static void WriteMeasurements()
    {
        try
        {
            if (s_outputPath is null)
            {
                return;
            }

            s_listener?.Dispose();
            s_listener = null;

            MeasurementRecord[] measurements;
            long? capabilityTimestamp;
            long? listenerStartTimestamp;
            lock (s_lock)
            {
                if (!s_activeMarkerCreated)
                {
                    return;
                }

                measurements = s_measurements
                    .OrderBy(measurement => measurement.Timestamp)
                    .ToArray();
                capabilityTimestamp = s_capabilityTimestamp;
                listenerStartTimestamp = s_listenerStartTimestamp;
            }

            var temporaryPath = s_outputPath + ".tmp";
            using (var writer = new StreamWriter(temporaryPath))
            {
                writer.WriteLine(
                    "timestamp\tinstrument\tstage\tpass\torigin\tsucceeded\tdurationSeconds\tprojectFileBase64\tevaluationId");
                foreach (var measurement in measurements)
                {
                    writer.Write(measurement.Timestamp.ToString(CultureInfo.InvariantCulture));
                    writer.Write('\t');
                    writer.Write(measurement.Instrument);
                    writer.Write('\t');
                    writer.Write(measurement.Stage);
                    writer.Write('\t');
                    writer.Write(measurement.Pass);
                    writer.Write('\t');
                    writer.Write(measurement.Origin);
                    writer.Write('\t');
                    if (measurement.Succeeded.HasValue)
                    {
                        writer.Write(measurement.Succeeded.Value ? "true" : "false");
                    }
                    writer.Write('\t');
                    writer.Write(measurement.DurationSeconds.ToString("R", CultureInfo.InvariantCulture));
                    writer.Write('\t');
                    writer.Write(Convert.ToBase64String(Encoding.UTF8.GetBytes(measurement.ProjectFile)));
                    writer.Write('\t');
                    writer.WriteLine(measurement.EvaluationId.ToString(CultureInfo.InvariantCulture));
                }
            }
            File.Move(temporaryPath, s_outputPath, overwrite: true);
            WriteLifecycle(
                s_hookStartTimestamp,
                listenerStartTimestamp,
                Stopwatch.GetTimestamp(),
                capabilityTimestamp);
        }
        catch (Exception ex)
        {
            WriteError(ex);
        }
        finally
        {
            s_listener?.Dispose();
            DeleteActiveMarker();
        }
    }

    internal static void FlushForTests() => WriteMeasurements();

    internal static bool IsMSBuildServerProcess(IEnumerable<string> commandLineArguments) =>
        commandLineArguments.Any(argument =>
            string.Equals(argument, ServerNodeModeArgument, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(argument, "-nodemode:8", StringComparison.OrdinalIgnoreCase));

    internal static void ResetForTests()
    {
        if (s_processExitRegistered)
        {
            AppDomain.CurrentDomain.ProcessExit -= OnProcessExit;
            s_processExitRegistered = false;
        }

        s_listener?.Dispose();
        s_listener = null;
        lock (s_lock)
        {
            s_measurements.Clear();
        }

        s_errorPath = null;
        s_hookStartTimestamp = 0;
        s_lifecyclePath = null;
        s_outputPath = null;
        s_session = null;
        s_capabilityTimestamp = null;
        s_listenerStartTimestamp = null;
        s_lifecycleCompleted = false;
        DeleteActiveMarker();
        s_errorWritten = 0;
    }

    private static void OnProcessExit(object? sender, EventArgs args) => WriteMeasurements();

    private static void ActivateLifecycleTracking()
    {
        lock (s_lock)
        {
            if (s_activeMarkerCreated)
            {
                return;
            }

            if (s_activePath is null)
            {
                throw new InvalidOperationException("Evaluation metrics active-marker path was not initialized.");
            }

            WriteLifecycle(
                s_hookStartTimestamp,
                s_listenerStartTimestamp,
                completionTimestamp: null,
                capabilityTimestamp: null);
            File.WriteAllText(
                s_activePath,
                Environment.ProcessId.ToString(CultureInfo.InvariantCulture));
            s_activeMarkerCreated = true;
        }
    }

    private static void WriteLifecycle(
        long hookStartTimestamp,
        long? listenerStartTimestamp,
        long? completionTimestamp,
        long? capabilityTimestamp)
    {
        lock (s_lock)
        {
            if (s_lifecycleCompleted)
            {
                return;
            }
            WriteLifecycleCore(hookStartTimestamp, listenerStartTimestamp,
                completionTimestamp, capabilityTimestamp);
            s_lifecycleCompleted = completionTimestamp.HasValue;
        }
    }

    private static void WriteLifecycleCore(
        long hookStartTimestamp,
        long? listenerStartTimestamp,
        long? completionTimestamp,
        long? capabilityTimestamp)
    {
        if (s_lifecyclePath is null || s_session is null)
        {
            throw new InvalidOperationException("Evaluation metrics lifecycle path was not initialized.");
        }

        var temporaryPath = s_lifecyclePath + ".tmp";
        using (var writer = new StreamWriter(temporaryPath))
        {
            writer.WriteLine(LifecycleHeader);
            writer.Write(LifecycleVersion.ToString(CultureInfo.InvariantCulture));
            writer.Write('\t');
            writer.Write(s_session);
            writer.Write('\t');
            writer.Write(Environment.ProcessId.ToString(CultureInfo.InvariantCulture));
            writer.Write('\t');
            writer.Write(hookStartTimestamp.ToString(CultureInfo.InvariantCulture));
            writer.Write('\t');
            if (listenerStartTimestamp.HasValue)
            {
                writer.Write(listenerStartTimestamp.Value.ToString(CultureInfo.InvariantCulture));
            }
            writer.Write('\t');
            if (completionTimestamp.HasValue)
            {
                writer.Write(completionTimestamp.Value.ToString(CultureInfo.InvariantCulture));
            }
            writer.Write('\t');
            if (capabilityTimestamp.HasValue)
            {
                writer.Write(capabilityTimestamp.Value.ToString(CultureInfo.InvariantCulture));
            }
            writer.WriteLine();
        }
        for (var attempt = 0; ; attempt++)
        {
            try
            {
                File.Move(temporaryPath, s_lifecyclePath, overwrite: true);
                break;
            }
            catch (Exception ex) when (
                OperatingSystem.IsWindows() && attempt < 5 &&
                ((ex is IOException && (ex.HResult & 0xFFFF) is 32 or 33) ||
                 (ex is UnauthorizedAccessException && File.Exists(s_lifecyclePath) &&
                  (File.GetAttributes(s_lifecyclePath) & FileAttributes.ReadOnly) == 0)))
            {
                Console.Error.WriteLine($"Evaluation lifecycle replacement is temporarily locked; retrying ({attempt + 1}/5).");
                File.AppendAllText(s_lifecyclePath + ".retries.txt",
                    $"{attempt + 1}{Environment.NewLine}");
                Thread.Sleep(25 * (attempt + 1));
            }
        }
    }

    private static void MarkListenerStarted()
    {
        lock (s_lock)
        {
            if (s_listenerStartTimestamp.HasValue)
            {
                return;
            }

            if (!s_activeMarkerCreated)
            {
                throw new InvalidOperationException("Evaluation metrics lifecycle tracking was not activated.");
            }

            s_listenerStartTimestamp = Stopwatch.GetTimestamp();
            WriteLifecycle(
                s_hookStartTimestamp,
                s_listenerStartTimestamp,
                completionTimestamp: null,
                capabilityTimestamp: null);
        }
    }

    private static void DeleteActiveMarker()
    {
        string? activePath;
        lock (s_lock)
        {
            if (!s_activeMarkerCreated)
            {
                s_activePath = null;
                return;
            }

            activePath = s_activePath;
            s_activeMarkerCreated = false;
            s_activePath = null;
        }

        try
        {
            if (activePath is not null)
            {
                File.Delete(activePath);
            }
        }
        catch
        {
        }
    }

    private static bool TryClaimSessionLeader(string outputDirectory, string session)
    {
        var claimPath = Path.Combine(outputDirectory, $"{session}-leader.claim");
        try
        {
            using (var claim = new FileStream(
                claimPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.Read))
            using (var writer = new StreamWriter(claim))
            {
                writer.Write(Environment.ProcessId.ToString(CultureInfo.InvariantCulture));
                return true;
            }
        }
        catch (IOException) when (File.Exists(claimPath))
        {
            return false;
        }
    }

    private static void WriteError(Exception exception)
    {
        if (s_errorPath is null || Interlocked.Exchange(ref s_errorWritten, 1) != 0)
        {
            return;
        }

        try
        {
            File.WriteAllText(s_errorPath, exception.ToString());
        }
        catch
        {
        }
    }

    private static string SanitizeFileName(string value)
    {
        var result = value.ToCharArray();
        for (var i = 0; i < result.Length; i++)
        {
            if (!char.IsAsciiLetterOrDigit(result[i]) && result[i] is not ('-' or '_'))
            {
                result[i] = '_';
            }
        }

        return new string(result);
    }

    private sealed class EvaluationEventListener : EventListener
    {
        private readonly string? _eventSourceName;

        public EvaluationEventListener(string eventSourceName)
        {
            _eventSourceName = eventSourceName;
        }

        public void Start()
        {
            foreach (var eventSource in EventSource.GetSources())
            {
                TryEnable(eventSource);
            }
        }

        protected override void OnEventSourceCreated(EventSource eventSource)
            => TryEnable(eventSource);

        private void TryEnable(EventSource eventSource)
        {
            if (_eventSourceName is null ||
                !string.Equals(eventSource.Name, _eventSourceName, StringComparison.Ordinal))
            {
                return;
            }

            try
            {
                ActivateLifecycleTracking();
                MarkListenerStarted();
                EnableEvents(
                    eventSource,
                    EventLevel.Informational,
                    EvaluationMeasurementsKeyword);
            }
            catch (Exception ex)
            {
                WriteError(ex);
            }
        }

        protected override void OnEventWritten(EventWrittenEventArgs eventData)
        {
            try
            {
                RecordEvent(eventData);
            }
            catch (Exception ex)
            {
                WriteError(ex);
            }
        }
    }

    private readonly record struct MeasurementRecord(
        long Timestamp,
        string Instrument,
        string Stage,
        string Pass,
        string Origin,
        bool? Succeeded,
        double DurationSeconds,
        string ProjectFile,
        int EvaluationId);
}
