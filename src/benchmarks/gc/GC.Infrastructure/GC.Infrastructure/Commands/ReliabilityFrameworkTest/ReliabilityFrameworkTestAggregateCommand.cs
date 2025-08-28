using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.RegularExpressions;
using GC.Infrastructure.Core.Configurations;
using GC.Infrastructure.Core.Configurations.ReliabilityFrameworkTest;
using Spectre.Console;
using Spectre.Console.Cli;

namespace GC.Infrastructure.Commands.ReliabilityFrameworkTest
{
    public class ReliabilityFrameworkTestAggregateCommand : 
        Command<ReliabilityFrameworkTestAggregateCommand.ReliabilityFrameworkTestAggregateSettings>
    {
        public class ReliabilityFrameworkTestDumpAnalyzeResult
        {
            public string? AttributedError { get; set; }
            public string? DumpName { get; set; }
            public string? CallStackForAllThreadsLogName { get; set; }
            public string? CallStackLogName { get; set; }
            public string? SourceFilePath { get; set; }
            public string? LineNumber { get; set; }
        }
        public sealed class ReliabilityFrameworkTestAggregateSettings : CommandSettings
        {
            [Description("Path to Configuration.")]
            [CommandOption("-c|--configuration")]
            public required string ConfigurationPath { get; init; }
        }

        public override int Execute([NotNull] CommandContext context, 
                                    [NotNull] ReliabilityFrameworkTestAggregateSettings settings)
        {
            AnsiConsole.Write(new Rule("Aggregate Analysis Results For Reliability Framework Test"));
            AnsiConsole.WriteLine();

            ConfigurationChecker.VerifyFile(settings.ConfigurationPath,
                                            nameof(ReliabilityFrameworkTestAggregateSettings));
            ReliabilityFrameworkTestAnalyzeConfiguration configuration =
                ReliabilityFrameworkTestAnalyzeConfigurationParser.Parse(settings.ConfigurationPath);

            AggregateResult(configuration);
            return 0;
        }

        public static void AggregateResult(ReliabilityFrameworkTestAnalyzeConfiguration configuration)
        {
            List<ReliabilityFrameworkTestDumpAnalyzeResult> dumpAnalyzeResultList = new List<ReliabilityFrameworkTestDumpAnalyzeResult>();

            foreach (string callStackLogPath in Directory.GetFiles(configuration.AnalyzeOutputFolder, "*_callstack.txt"))
            {
                AnsiConsole.WriteLine($"====== Extracting information from {callStackLogPath} ======");

                string dumpPath = callStackLogPath.Replace("_callstack.txt", ".dmp");
                string callStackForAllThreadsLogPath = callStackLogPath.Replace(
                    "_callstack.txt", "_callstack_allthreads.txt");

                try
                {
                    string callStack = File.ReadAllText(callStackLogPath);

                    // Search for frame contains keywords.
                    string? frameInfo = FindFrameByKeyWord(configuration.StackFrameKeyWords, callStack);

                    // If no line contains given keywords, mark it as unknown error
                    if (String.IsNullOrEmpty(frameInfo))
                    {
                        ReliabilityFrameworkTestDumpAnalyzeResult unknownErrorResult = new()
                        {
                            AttributedError = "Unknown error",
                            DumpName = Path.GetFileName(dumpPath),
                            CallStackLogName = Path.GetFileName(callStackLogPath),
                            CallStackForAllThreadsLogName = Path.GetFileName(callStackForAllThreadsLogPath),
                            SourceFilePath = String.Empty,
                            LineNumber = String.Empty
                        };

                        dumpAnalyzeResultList.Add(unknownErrorResult);
                        continue;
                    }

                    // Extract source file path and line number
                    (string, int)? SrcFileLineNumTuple =
                        ExtractSrcFilePathAndLineNumberFromFrameInfo(frameInfo);
                    if (!SrcFileLineNumTuple.HasValue)
                    {
                        continue;
                    }
                    (string srcFilePath, int lineNumber) = SrcFileLineNumTuple.Value;

                    int lineIndex = lineNumber - 1;
                    string realSrcFilePath = string.Empty;

                    // Convert source file path if it's in wsl.
                    if (srcFilePath.StartsWith("/"))
                    {
                        if (String.IsNullOrEmpty(configuration.WSLInstanceLocation))
                        {
                            AnsiConsole.WriteLine($"[yellow]Provide wsl instance location to access source file.[/]");
                            continue;
                        }

                        string srcFilePathWithBackSlash = srcFilePath.Replace("/", "\\");
                        realSrcFilePath = configuration.WSLInstanceLocation + srcFilePathWithBackSlash;
                    }
                    else
                    {
                        realSrcFilePath = srcFilePath;
                    }

                    // Get source code line that throw error.
                    var srcLineList = File.ReadAllLines(realSrcFilePath);

                    string srcLine = srcLineList[lineIndex].Trim();
                    while (String.IsNullOrEmpty(srcLine) || String.IsNullOrWhiteSpace(srcLine))
                    {
                        lineIndex = lineIndex - 1;
                        srcLine = srcLineList[lineIndex].Trim();
                    }
                    string error = srcLine;

                    ReliabilityFrameworkTestDumpAnalyzeResult dumpAnalyzeResult = new()
                    {
                        AttributedError = error,
                        DumpName = Path.GetFileName(dumpPath),
                        CallStackLogName = Path.GetFileName(callStackLogPath),
                        CallStackForAllThreadsLogName = Path.GetFileName(callStackForAllThreadsLogPath),
                        SourceFilePath = realSrcFilePath,
                        LineNumber = (lineIndex + 1).ToString()
                    };

                    dumpAnalyzeResultList.Add(dumpAnalyzeResult);
                }
                catch (Exception ex)
                {
                    AnsiConsole.WriteLine($"[red]Fail to analyze {callStackLogPath}: {ex.Message}.[/]");
                }
            }

            GenerateResultTable(dumpAnalyzeResultList, configuration.AnalyzeOutputFolder);
        }

        private static void GenerateResultTable(List<ReliabilityFrameworkTestDumpAnalyzeResult> dumpAnalyzeResultList,
                                               string analyzeOutputFolder)
        {
            var resultListGroup = dumpAnalyzeResultList.GroupBy(dumpAnalyzeResult => dumpAnalyzeResult.AttributedError);

            StringBuilder sb = new StringBuilder();
            // Write title of table
            sb.AppendLine("| Attributed Error | Count/Total(percentage%) | Dump Name | Log Name(Call Stacks of All Threads)  | Source File Path | Line Number |");
            sb.AppendLine("| :---------- | :---------: | :---------- | :---------- | :---------- | :---------: |");

            foreach (IGrouping<string?, ReliabilityFrameworkTestDumpAnalyzeResult>? group in resultListGroup)
            {
                var resultListWithoutFirstItem = group.ToList();
                var firstResult = resultListWithoutFirstItem.FirstOrDefault();
                if (firstResult == null)
                {
                    continue;
                }
                resultListWithoutFirstItem.Remove(firstResult);

                string? attributedError = firstResult.AttributedError;
                string proportion = $"{group.Count()}/{dumpAnalyzeResultList.Count}";
                double proportionInPercentage = Convert.ToDouble(group.Count()) / Convert.ToDouble(dumpAnalyzeResultList.Count);
                string? dumpName = firstResult.DumpName;
                string? callStackForAllThreadsLogName = firstResult.CallStackForAllThreadsLogName;
                string? sourceFilePath = firstResult.SourceFilePath;
                string? lineNumber = firstResult.LineNumber;
                sb.AppendLine($"| {attributedError} | {proportion}({proportionInPercentage * 100}%) | {dumpName} | {callStackForAllThreadsLogName} | {sourceFilePath} | {lineNumber} |");

                foreach (ReliabilityFrameworkTestDumpAnalyzeResult? dumpAnalyzeResult in resultListWithoutFirstItem)
                {
                    dumpName = dumpAnalyzeResult.DumpName;
                    callStackForAllThreadsLogName = dumpAnalyzeResult.CallStackForAllThreadsLogName;
                    sourceFilePath = dumpAnalyzeResult.SourceFilePath;
                    lineNumber = dumpAnalyzeResult.LineNumber;
                    sb.AppendLine($"|  |  | {dumpName} | {callStackForAllThreadsLogName} | {sourceFilePath} | {lineNumber} |");
                }
            }

            try
            {
                string outputPath = Path.Combine(analyzeOutputFolder, "Results.md");
                File.WriteAllText(outputPath, sb.ToString());
            }
            catch (Exception ex)
            {
                throw new Exception($"Fail to write result to markdown: {ex.Message}");
            }

        }

        private static (string, int)? ExtractSrcFilePathAndLineNumberFromFrameInfo(string frameInfo)
        {
            string pattern = @"\[(.*?)\]";
            Match match = Regex.Match(frameInfo, pattern, RegexOptions.Singleline);

            if (!match.Success)
            {
                AnsiConsole.MarkupLine($"[red]Fail to extract source file path and line number from frame info: {frameInfo}[/]");
                return null;
            }

            string fileNameWithLineNumber = match.Groups[1].Value.Trim();
            string[] splitOutput = fileNameWithLineNumber.Split("@");

            string? fileName = splitOutput.FirstOrDefault(String.Empty);
            if (String.IsNullOrEmpty(fileName))
            {
                AnsiConsole.MarkupLine($"[red]Fail to extract source file path.[/]");
                return null;
            }

            string? lineNumberstr = splitOutput.LastOrDefault(String.Empty).Trim();
            if (String.IsNullOrEmpty(lineNumberstr))
            {
                AnsiConsole.MarkupLine($"[red]Fail to extract line number.[/]");
                return null;
            }

            bool success = int.TryParse(lineNumberstr, out int lineNumber);
            if (!success)
            {
                AnsiConsole.MarkupLine($"[red]Fail to parse line number.[/]");
                return null;
            }

            return (fileName, lineNumber);
        }

        private static string? FindFrameByKeyWord(List<string> keyWordList, string callStack)
        {
            string[] lines = callStack.Split("\n");
            foreach (string line in lines)
            {
                foreach (string keyWord in keyWordList)
                {
                    if (line.Contains(keyWord))
                    {
                        return line;
                    }
                }
            }
            AnsiConsole.MarkupLine($"[yellow]Fail to find keyword.[/]");
            return null;
        }
    }
}
