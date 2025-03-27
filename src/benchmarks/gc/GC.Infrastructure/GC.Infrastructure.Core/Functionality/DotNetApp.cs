using System.Xml.Linq;

namespace GC.Infrastructure.Core.Functionality
{
    public class DotNetApp
    {
        public string AppRoot { get; } = String.Empty;
        public string AppName { get; } = String.Empty;

        public static string GetProjectFilePath(string appRoot, string appName)
        {
            string projectFile = Path.Combine(appRoot, $"{appName}.csproj");
            if (!File.Exists(projectFile))
            {
                throw new Exception($"{nameof(DotNetApp)}: Project file {projectFile} doesn't exist in {appRoot}");
            }
            return projectFile;
        }

        public static string GetTargetFramework(string projectFilePath)
        {
            string xmlData = File.ReadAllText(projectFilePath);
            XDocument doc = XDocument.Parse(xmlData);
            return doc.Root!.Element("PropertyGroup")!.Element("TargetFramework")!.Value;
        }

        public static string GetSymbolFolder(string appRoot, string targetFramework, string buildConfig, string targetRID = "")
        {
            string appSymbolFolder = string.IsNullOrEmpty(targetRID)
                switch
            {
                true => Path.Combine(appRoot, "bin", buildConfig, targetFramework),
                false => Path.Combine(appRoot, "bin", buildConfig, targetFramework, targetRID)
            };
            if (!Directory.Exists(appSymbolFolder))
            {
                throw new Exception($"{nameof(DotNetApp)}: Symbol folder {appSymbolFolder} doesn't exist in {appRoot}");
            }
            return appSymbolFolder;
        }

        public static string GetAppExecutable(string symbolFolder, string appName, string targetRID)
        {
            string excutableFileExtension = DotNetInfrastructure.GetExcutableFileExtensionByRID(targetRID);
            string excutable = Path.Combine(symbolFolder, $"{appName}{excutableFileExtension}");
            if (!File.Exists(excutable))
            {
                throw new Exception($"{nameof(DotNetApp)}: Executable {excutable} doesn't exist in {symbolFolder}");
            }
            return excutable;
        }

        public static string GetAppIL(string symbolFolder, string appName)
        {
            string ilPath = Path.Combine(symbolFolder, $"{appName}.dll");
            if (!File.Exists(ilPath))
            {
                throw new Exception($"{nameof(DotNetApp)}: IL {ilPath} doesn't exist in {symbolFolder}");
            }
            return ilPath;
        }

        public static CommandInvokeResult BuildApp(string dotNetExecutable,
                                                   string projectFile,
                                                   string buildConfig,
                                                   string targetRID = "",
                                                   string workingDirectory = "",
                                                   bool redirectStdOutErr = true,
                                                   bool silent = false)
        {
            string arguments = string.IsNullOrEmpty(targetRID)
                switch
            {
                true => $"build -c {buildConfig}",
                false => $"build -r {targetRID} -c {buildConfig}"
            };

            CommandInvoker invoker = new(dotNetExecutable,
                                         arguments,
                                         new(),
                                         workingDirectory,
                                         redirectStdOutErr,
                                         silent);
            return invoker.WaitForResult();
        }

        public string ProjectFilePath()
        {
            return DotNetApp.GetProjectFilePath(AppRoot, AppName);
        }

        public string TargetFramework()
        {
            string projectFilePath = this.ProjectFilePath();
            return DotNetApp.GetTargetFramework(projectFilePath);
        }

        public string SymbolFolder(string buildConfig, string targetRID = "")
        {
            string targetFramework = this.TargetFramework();
            return DotNetApp.GetSymbolFolder(AppRoot, targetFramework, buildConfig, targetRID);
        }
    }
}
