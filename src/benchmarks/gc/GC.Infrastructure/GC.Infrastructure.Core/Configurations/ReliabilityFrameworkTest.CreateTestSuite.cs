using System.Xml.Linq;

namespace GC.Infrastructure.Core.Configurations
{
    public static class ReliabilityFrameworkTestSuiteCreator
    {
        public static void CreateTestingAssets(string ReliabilityFrameworkDll, 
                                               string targetRID,
                                               string CoreRoot,
                                               string outputFolder,
                                               string GCPerfSimDll,
                                               string TestFolder)
        {            
            try
            {
                Console.WriteLine($"====== Copy ReliabilityFramework.dll and Tests to `outputFolder` ======");
                Utilities.CopyFile(ReliabilityFrameworkDll, outputFolder);                

                Console.WriteLine($"====== Copy gcperfsim.dll to Tests ======");
                Utilities.CopyFolderRecursively(TestFolder, outputFolder);
            }
            catch (Exception ex)
            {
                throw new Exception($"{nameof(ReliabilityFrameworkTestSuiteCreator)}: Fail to generate test assets: {ex}");
            }
        }

        public static void GenerateTestScript(string rid,
                                              string baseSuiteFolder,
                                              string coreRoot,
                                              string configPath,     
                                              string gcMode,
                                              string outputRoot,
                                              string scriptPath)
        {
            string scriptFoler = Path.GetDirectoryName(scriptPath);
            string osName = rid.Split("-")
                    .FirstOrDefault("");

            string configName = Path.GetFileName(configPath).Split("-")
                .FirstOrDefault("");

            (string DOTNET_gcServer, string DOTNET_GCDynamicAdaptationMode) =
            gcMode switch
            {
                "Datas" => ("1", "1"),
                "Server" => ("1", "0"),
                "Workstation" => ("0", "0"),
                _ => throw new Exception($"{nameof(ReliabilityFrameworkTestSuiteCreator)}: Unknow GC mode: {gcMode}")
            };

            string scriptSettingSegment;
            string scriptBaseContent;

            if (osName == "win")
            {
                scriptSettingSegment =
$"""
########## setting start ##########
$Env:DOTNET_gcServer={DOTNET_gcServer}
$Env:DOTNET_GCDynamicAdaptationMode={DOTNET_GCDynamicAdaptationMode}

$OutputRoot="{outputRoot}"
$CORE_ROOT="{coreRoot}"

# set config path
$config_path=Join-Path -Path $PSScriptRoot -ChildPath "{Path.GetRelativePath(scriptFoler, configPath)}"

# set test folder
$test_folder=Join-Path -Path $OutputRoot -ChildPath "Tests"

# set ReliabilityFramework.dll path
$reliability_framework_dll=Join-Path -Path $OutputRoot -ChildPath "ReliabilityFramework.dll" 

# set output folder
$output_folder=$PSScriptRoot
########## setting end ##########
""";
                string baseScriptPath = Path.Combine(baseSuiteFolder, "TestingScript.ps1.txt");
                scriptBaseContent = File.ReadAllText(baseScriptPath);
            }
            else
            {
                scriptSettingSegment =
$"""
#!/bin/bash

script_root=$(dirname $(realpath $0))
########## setting start ##########
# set gc mode
export DOTNET_gcServer={DOTNET_gcServer}
export DOTNET_GCDynamicAdaptationMode={DOTNET_GCDynamicAdaptationMode}

# set core_root
Output_Root={outputRoot}
CORE_ROOT={coreRoot}

# set config path
config_path=$script_root/{Path.GetRelativePath(scriptFoler, configPath)}

# set test folder
test_folder=$Output_Root/Tests

# set ReliabilityFramework.dll path
reliability_framework_dll=$Output_Root/ReliabilityFramework.dll

# set output folder
output_folder=$script_root

########## setting end ##########
""";
                string baseScriptPath = Path.Combine(baseSuiteFolder, "TestingScript.sh.txt");
                scriptBaseContent = File.ReadAllText(baseScriptPath);
            }

            string content = $"{scriptSettingSegment}\n\n{scriptBaseContent}";
            File.WriteAllText(scriptPath, content);
        }

        public static void GenerateTestConfig(string rid, string baseSuiteFolder, string configName, bool enableStress, string gcMode, string configPath)
        {
            try
            {
                string osName = rid.Split("-")
                    .FirstOrDefault("");

                string maximumWaitTime =
                    (osName, enableStress) switch
                    {
                        ("win", false) => Windows[configName][gcMode],
                        ("win", true) => WindowsStress[configName][gcMode],
                        ("linux", false) => Linux[configName][gcMode],
                        ("linux", true) => LinuxStress[configName][gcMode],
                        _ => throw new Exception($"{nameof(ReliabilityFrameworkTestSuiteCreator)}: Unknown OS {osName}")
                    };

                string baseConfigPath = Path.Combine(baseSuiteFolder, $"{configName}.config");
                string baseConfigContent = File.ReadAllText(baseConfigPath);
                XElement config = XElement.Parse(baseConfigContent);
                config.SetAttributeValue("maximumWaitTime", maximumWaitTime);
                config.SetAttributeValue("maximumExecutionTime", "24:00:00");
                config.SetAttributeValue("maximumTestRuns", "-1");

                config.Save(configPath, SaveOptions.OmitDuplicateNamespaces);
            }
            catch (Exception e)
            {
                throw new Exception($"{nameof(ReliabilityFrameworkTestSuiteCreator)}: Fail to generate test config: {e.Message}");
            }
        }

        private static Dictionary<string, Dictionary<string, string>> Windows { get; } = new()
        {
            { "loh", new() { { "Server", "00:10:00"}, { "Workstation", "01:45:00"}, { "Datas", "00:10:00"} } },
            { "poh", new() { { "Server", "00:05:00"}, { "Workstation", "00:50:00" }, { "Datas", "00:05:00" } } },
            { "non_induced", new() { { "Server", "00:30:00"}, { "Workstation", "03:30:00"}, { "Datas", "00:20:00"} } },
            { "finalization", new() { { "Server", "00:05:00"}, { "Workstation", "00:50:00"}, { "Datas", "00:15:00" } } }
        };
        private static Dictionary<string, Dictionary<string, string>> WindowsStress { get; } = new()
        {
            { "loh", new() { { "Server", "00:02:00"}, { "Workstation", "00:10:00"}, { "Datas", "00:05:00"} } },
            { "poh", new() { { "Server", "00:02:00"}, { "Workstation", "00:10:00" }, { "Datas", "00:05:00" } } },
            { "non_induced", new() { { "Server", "00:25:00"}, { "Workstation", "04:00:00"}, { "Datas", "00:35:00"} } },
            { "finalization", new() { { "Server", "00:05:00"}, { "Workstation", "00:40:00"}, { "Datas", "01:40:00" } } }
        };
        private static Dictionary<string, Dictionary<string, string>> Linux { get; } = new()
        {
            { "loh", new() { { "Server", "00:25:00"}, { "Workstation", "00:20:00"}, { "Datas", "00:20:00"} } },
            { "poh", new() { { "Server", "00:10:00"}, { "Workstation", "00:10:00" }, { "Datas", "00:10:00" } } },
            { "non_induced", new() { { "Server", "00:25:00"}, { "Workstation", "00:55:00"}, { "Datas", "00:15:00"} } },
            { "finalization", new() { { "Server", "00:20:00"}, { "Workstation", "01:30:00"}, { "Datas", "00:25:00" } } }
        };
        private static Dictionary<string, Dictionary<string, string>> LinuxStress { get; } = new()
        {
            { "loh", new() { { "Server", "00:10:00"}, { "Workstation", "00:25:00"}, { "Datas", "00:08:00"} } },
            { "poh", new() { { "Server", "00:05:00"}, { "Workstation", "00:15:00" }, { "Datas", "00:05:00" } } },
            { "non_induced", new() { { "Server", "00:25:00"}, { "Workstation", "01:10:00"}, { "Datas", "00:30:00"} } },
            { "finalization", new() { { "Server", "00:20:00"}, { "Workstation", "00:50:00"}, { "Datas", "04:00:00" } } }
        };
    }
}
