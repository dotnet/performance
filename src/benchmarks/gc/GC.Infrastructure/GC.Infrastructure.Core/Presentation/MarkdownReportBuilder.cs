using GC.Infrastructure.Core.Analysis;
using System.Text;

namespace GC.Infrastructure.Core.Presentation
{
    public static class MarkdownReportBuilder
    {
        public static StringBuilder AddHeading(this StringBuilder sb, string heading, int hs)
        {
            sb.AppendLine($"{Enumerable.Repeat("#", hs)} {heading}");
            return sb;
        }

        public static StringBuilder AddCode(this StringBuilder sb, string code)
        {
            sb.AppendLine($"```{code}```");
            return sb;
        }

        public static string CopySectionFromMarkDownPath(string path, string sectionToCopy)
        {
            string allText = File.ReadAllText(path);
            return CopySectionFromMarkDown(allText, sectionToCopy);
        }

        public static string CopySectionFromMarkDown(string markdown, string sectionToCopy)
        {
            string[] lines = markdown.Split("\n", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

            // Iterate till we find the section.
            int runnerIdx = -1;
            for (int startIdx = 0; startIdx < lines.Length; startIdx++)
            {
                string line = lines[startIdx];
                if (line.StartsWith($"# {sectionToCopy}"))
                {
                    runnerIdx = startIdx + 1;
                    break;
                }
            }

            // Section not found.
            if (runnerIdx == -1)
            {
                return string.Empty;
            }

            // Once the section is obtained, iterate till we get to the next section.
            StringBuilder sb = new();
            while (runnerIdx < lines.Length)
            {
                string line = lines[runnerIdx];

                if (line.StartsWith("# "))
                {
                    break;
                }

                sb.AppendLine(line);
                runnerIdx++;
            }

            return sb.ToString();
        }

        public static void AddIncompleteTestsSection(this StreamWriter sw, Dictionary<string, ProcessExecutionDetails> executionDetails)
        {
            sw.WriteLine("# Incomplete Tests");

            foreach (var p in executionDetails)
            {
                if (p.Value.HasFailed)
                {
                    sw.WriteLine($"### {p.Key}");
                    sw.WriteLine($"Standard Error: {p.Value.StandardError}\n");
                    sw.WriteLine();
                    sw.WriteLine($"Standard Out: {p.Value.StandardOut}\n");
                    sw.WriteLine();
                    sw.WriteLine($"Repro: \n ```{p.Value.CommandlineArgs}```\n");
                }
            }
        }

        public static void AddIncompleteTestsSectionWithYamlFileName(this StreamWriter sw, string yamlFileName, Dictionary<string, ProcessExecutionDetails> executionDetails)
        {
            sw.WriteLine($"# Incomplete Tests: {yamlFileName}.yaml");

            foreach (var p in executionDetails)
            {
                if (p.Value.HasFailed)
                {
                    sw.WriteLine($"### {p.Key}");
                    sw.WriteLine($"Standard Error: {p.Value.StandardError}\n");
                    sw.WriteLine();
                    sw.WriteLine($"Standard Out: {p.Value.StandardOut}\n");
                    sw.WriteLine();
                    sw.WriteLine($"Repro: \n ```{p.Value.CommandlineArgs}```\n");
                }
            }
        }

        public static void AddReproSection(this StreamWriter sw, Dictionary<string, ProcessExecutionDetails> executionDetails)
        {
            sw.WriteLine("## Repro Steps");

            foreach (var p in executionDetails)
            {
                sw.WriteLine($"### {p.Key}");
                sw.WriteLine($"```{p.Value.CommandlineArgs}```\n");
            }

            sw.WriteLine();
        }

        public static void GenerateChecklist(this StreamWriter sw, string[] gcperfsimConfigurations, string[] microbenchmarkConfigurations, HashSet<string> aspnetRuns)
        {
            var cleanedGCPerfSimConfigurations = new HashSet<string>(gcperfsimConfigurations.Select(c => Path.GetFileNameWithoutExtension(c)));
            var cleanedMicrobenchmarkConfigurations = new HashSet<string>(microbenchmarkConfigurations.Select(c => Path.GetFileNameWithoutExtension(c)));

            // GCPerfSim.
            string containsNormalServer_GCPerfSim = cleanedGCPerfSimConfigurations.Contains("Normal_Server") ? "x" : " ";
            string containsNormalWorkstation_GCPerfSim = cleanedGCPerfSimConfigurations.Contains("Normal_Workstation") ? "x" : " ";
            string containsLargePagesServer_GCPerfSim = cleanedGCPerfSimConfigurations.Contains("LargePages_Workstation") ? "x" : " ";
            string containsLargePagesWorkstation_GCPerfSim = cleanedGCPerfSimConfigurations.Contains("LargePages_Server") ? "x" : " ";
            string containsHighMemory_GCPerfSim = cleanedGCPerfSimConfigurations.Contains("HighMemory") ? "x" : " ";
            string containsLowMemoryContainer_GCPerfSim = cleanedGCPerfSimConfigurations.Contains("LowMemoryContainer") ? "x" : " ";

            // Microbenchmarks.
            string containsServer_Microbenchmarks = cleanedMicrobenchmarkConfigurations.Contains("Microbenchmrks_Server") ? "x" : " ";
            string containsWorkstation_Microbenchmarks = cleanedGCPerfSimConfigurations.Contains("Microbenchmarks_Workstation") ? "x" : " ";

            // ASPNet Configurations.
            string contains_JsonMinWindows = aspnetRuns.Contains("JsonMin_Windows") ? "x" : " ";
            string contains_FortunesEtf_Windows = aspnetRuns.Contains("FortunesETF_Windows") ? "x" : " ";
            string contains_Stage1Grpc_Windows = aspnetRuns.Contains("Stage1Grpc_Windows") ? "x" : " ";

            sw.WriteLine("# Checklist \n");

            sw.WriteLine($"- [x] GC Perf Sim \n\t- [{containsNormalServer_GCPerfSim}] Normal Server \n\t- [{containsNormalWorkstation_GCPerfSim}] Normal Workstation \n\t- [{containsLargePagesServer_GCPerfSim}] Large Pages Server \n\t- [{containsLargePagesWorkstation_GCPerfSim}] Large Pages Workstation \n\t- [{containsLowMemoryContainer_GCPerfSim}] Low Memory Container \n\t- [{containsHighMemory_GCPerfSim}] High Memory Load");
            sw.WriteLine($"- [x] Microbenchmarks \n\t- [{containsWorkstation_Microbenchmarks}] Workstation \n\t- [{containsServer_Microbenchmarks}] Server");
            sw.WriteLine($"- [x] ASPNet Benchmarks \n\t- [{contains_JsonMinWindows}] JsonMin_Windows \n\t- [{contains_FortunesEtf_Windows}]  Fortunes ETF_Windows \n\t- [{contains_Stage1Grpc_Windows}] Stage1Grpc_Windows ");
        }
    }
}
