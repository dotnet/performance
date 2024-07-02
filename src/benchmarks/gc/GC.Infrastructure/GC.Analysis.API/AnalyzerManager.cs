using System.Collections.Concurrent;

namespace GC.Analysis.API
{
    public static class AnalyzerManager
    {
        public static Analyzer GetAnalyzer(string tracePath) => new Analyzer(tracePath);

        public static Dictionary<string, Analyzer> GetAnalyzer(IEnumerable<string> tracePaths)
        {
            Dictionary<string, Analyzer> analyzers = new();
            foreach (var tracePath in tracePaths)
            {
                Analyzer analyzer = new Analyzer(tracePath);
                analyzers[tracePath] = analyzer;
            }

            return analyzers;
        }

        public static Dictionary<string, Analyzer> GetAllAnalyzers(string basePath, bool recursive = false)
        {
            Dictionary<string, Analyzer> analyzers = new();

            IEnumerable<string> allFiles = null;

            if (!recursive)
            {
                allFiles = Directory.GetFiles(basePath);
            }

            else
            {
                allFiles = Directory.EnumerateFiles(basePath, "*", SearchOption.AllDirectories);
            }

            foreach (var file in allFiles)
            {
                if (file.EndsWith("etl.zip"))
                {
                    string withoutExtension = file.Replace("etl.zip", "etlx");
                    if (File.Exists(withoutExtension))
                    {
                        Analyzer analyzer = new Analyzer(withoutExtension);
                        analyzers[withoutExtension] = analyzer;
                    }

                    else
                    {
                        Analyzer analyzer = new Analyzer(file);
                        analyzers[file] = analyzer;
                    }
                }

                else if (file.EndsWith(".nettrace"))
                {
                    Analyzer analyzer = new Analyzer(file);
                    analyzers[file] = analyzer;
                }
            }

            return analyzers;
        }

        public static IReadOnlyDictionary<string, Analyzer> GetAllAnalyzersParallel(string basePath, bool recursive = false)
        {
            ConcurrentDictionary<string, Analyzer> analyzers = new();

            IEnumerable<string> allFiles = null;

            if (!recursive)
            {
                allFiles = Directory.GetFiles(basePath);
            }

            else
            {
                allFiles = Directory.EnumerateFiles(basePath, "*", SearchOption.AllDirectories);
            }

            Parallel.ForEach(allFiles, (file) =>
            {
                if (file.EndsWith("etl.zip"))
                {
                    string withoutExtension = file.Replace("etl.zip", "etlx");
                    if (File.Exists(withoutExtension))
                    {
                        Analyzer analyzer = new Analyzer(withoutExtension);
                        analyzers[withoutExtension] = analyzer;
                    }

                    else
                    {
                        Analyzer analyzer = new Analyzer(file);
                        analyzers[file] = analyzer;
                    }
                }

                else if (file.EndsWith(".nettrace"))
                {
                    Analyzer analyzer = new Analyzer(file);
                    analyzers[file] = analyzer;
                }
            });

            return analyzers;
        }
    }
}
