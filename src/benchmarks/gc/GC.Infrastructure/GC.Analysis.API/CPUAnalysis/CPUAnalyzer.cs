using Microsoft.Diagnostics.Symbols;
using Microsoft.Diagnostics.Tracing.Analysis;
using Microsoft.Diagnostics.Tracing.Parsers.Kernel;
using Microsoft.Diagnostics.Tracing.Stacks;
using System.Text.RegularExpressions;

using Microsoft.Diagnostics.Tracing.Etlx;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using Diagnostics.Tracing.StackSources;
using Newtonsoft.Json;
using System.Reflection.Metadata.Ecma335;

namespace GC.Analysis.API
{
    public sealed class CPUProcessData
    {
        public int ProcessID { get; set; }
        public string ProcessName { get; set; }
        // Method Name -> Thread Data -> GC # -> CPU Data
        public Dictionary<string, Dictionary<int, Dictionary<int, List<CPUThreadData>>>> PerThreadData { get; set; } = new();

        // Method Name -> GC # -> Thread Data -> CPU Data
        public Dictionary<string, Dictionary<int, Dictionary<int, List<CPUThreadData>>>> PerGCData { get; set; } = new();

        [JsonIgnore]
        public CPUAnalyzer Parent { get; internal set; }
        [JsonIgnore]
        internal StackView StackView { get; set; }
        // Method Name -> GC # -> CPU Data 
        public Dictionary<string, Dictionary<int, CPUThreadData>> OverallData { get; set; } = new();
        public Dictionary<int, Dictionary<string, CPUThreadData>> GCOverallData { get; set; } = new();
        public Dictionary<string, CPUThreadData> MethodToData { get; set; } = new();
    }

    public sealed class CPUAnalyzer
    {
        private const string DEFAULT_SYMBOL_PATH = @";SRV*C:\Symbols*https://msdl.microsoft.com/download/symbols;SRV*C:\Symbols*https://nuget.smbsrc.net;SRV*C:\Symbols*https://referencesource.microsoft.com/symbols";
        private readonly TextWriter _symbolWriter;

        public CPUAnalyzer(Analyzer analyzer, string yamlPath = "./DefaultMethods.yaml", string anaylsisLogFile = "", string symbolPath = "")
        {
            Analyzer = analyzer;

            // TODO: Parse out YAML Methods with the associations.
            IDeserializer deserializer = new DeserializerBuilder()
                                           .WithNamingConvention(UnderscoredNamingConvention.Instance)
                                           .Build();

            string configContents = File.ReadAllText(yamlPath);
            Configuration = deserializer.Deserialize<GCMethodsData>(configContents);

            _symbolWriter = TextWriter.Null;
            if (!string.IsNullOrEmpty(anaylsisLogFile))
            {
                _symbolWriter = new StreamWriter(anaylsisLogFile);
            }

            if (!string.IsNullOrEmpty(symbolPath))
            {
                // Set a reasonable default.
                symbolPath = DEFAULT_SYMBOL_PATH + ";" + symbolPath;
            }
            else
            {
                symbolPath = DEFAULT_SYMBOL_PATH;
            }

            SymbolReader = new SymbolReader(_symbolWriter, symbolPath);
            SymbolReader.Options = SymbolReaderOptions.CacheOnly | SymbolReaderOptions.NoNGenSymbolCreation;
            SymbolReader.SecurityCheck = path => true;

            // Resolve symbols.
            foreach (var module in analyzer.TraceLog.ModuleFiles)
            {
                if (module.Name.ToLower().Contains("clr") /* || module.Name.ToLower().Contains("ntoskrnl") */)
                {
                    // Only resolve symbols for modules whose name includes "clr".
                    analyzer.TraceLog.CodeAddresses.LookupSymbolsForModule(SymbolReader, module);
                }
            }

            SymbolReader.Log.Flush();

            List<string> allMethods = Configuration.gc_methods.ToList();
            // TODO: Segregate the non-GC methods here.
            CPUAnalyzers = GetAllCPUAnalyzers(analyzer, new HashSet<string>(allMethods));
        }

        internal static List<CallTreeNodeBase> FindNodesByName(string nodeNamePat, List<CallTreeNodeBase> byID)
        {
            List<CallTreeNodeBase> callTreeNodes = new();

            var regEx = new Regex(nodeNamePat, RegexOptions.IgnoreCase);
            foreach (var node in byID)
            {
                if (regEx.IsMatch(node.Name))
                {
                    callTreeNodes.Add(node);
                }
            }

            return callTreeNodes;
        }

        internal void DepthFirstSearch(CallTreeNode node, string method, int gcNumber, CPUProcessData processData)
        {
            Regex matchOnGCThread = new Regex(@"(Thread \([^\)]+\))", RegexOptions.IgnoreCase);

            // For all the callees of the nodes.
            foreach (var callee in node.AllCallees)
            {
                // If there is match on the thread.
                Match match = matchOnGCThread.Match(callee.DisplayName);
                if (match.Success)
                {
                    // Extract thread.
                    var threadID = Convert.ToInt32(match.Groups[0].Value.Split(" ")[1].Replace("(", string.Empty).Replace(")", string.Empty));

                    CPUThreadData cpuThreadData = new()
                    {
                        Method = method,
                        InclusiveCount = callee.InclusiveCount,
                        ExclusiveCount = callee.ExclusiveCount,
                        Thread = threadID.ToString(),
                        Callers = new(),
                    };

                    var cpuThreads = GetCPUThreadNode(method: method, threadID: threadID, gcNumber: gcNumber, processData: processData);
                    var cpuThreadsByGC = GetCPUThreadNodeByGCNumber(method, threadID, gcNumber, processData);

                    CallTreeNode cursor = callee;
                    while (cursor != null)
                    {
                        cpuThreadData.Callers.Add(cursor.Caller?.DisplayName ?? string.Empty);
                        cursor = cursor.Caller;
                    }

                    cpuThreads.Add(cpuThreadData);
                    cpuThreadsByGC.Add(cpuThreadData);
                }

                // Kick off the DFS.
                DepthFirstSearch(callee, method, gcNumber, processData);
            }
        }

        internal static List<CPUThreadData> GetCPUThreadNode(string method, int threadID, int gcNumber, CPUProcessData processData)
        {
            if (!processData.PerThreadData.TryGetValue(method, out var threadToGCCPUData))
            {
                processData.PerThreadData[method] = threadToGCCPUData = new();
            }

            if (!threadToGCCPUData.TryGetValue(threadID, out var threadCPUData))
            {
                threadToGCCPUData[threadID] = threadCPUData = new();
            }

            if (!threadCPUData.TryGetValue(gcNumber, out var cpuData))
            {
                threadCPUData[gcNumber] = cpuData = new();
            }

            return cpuData;
        }

        internal static List<CPUThreadData> GetCPUThreadNodeByGCNumber(string method, int threadID, int gcNumber, CPUProcessData processData)
        {
            if (!processData.PerGCData.TryGetValue(method, out var gcToThreadData))
            {
                processData.PerGCData[method] = gcToThreadData = new();
            }

            if (!gcToThreadData.TryGetValue(gcNumber, out var gcCPUData))
            {
                gcToThreadData[gcNumber] = gcCPUData = new();
            }

            if (!gcCPUData.TryGetValue(threadID, out var cpuData))
            {
                gcCPUData[threadID] = cpuData = new();
            }

            return cpuData;
        }

        internal Dictionary<string, List<CPUProcessData>> GetAllCPUAnalyzers(Analyzer analyzer, HashSet<string> methodsToConsider)
        {
            Microsoft.Diagnostics.Tracing.Etlx.TraceLog traceLog = analyzer.TraceLog;
            Dictionary<int, string> processIDMap = new();
            foreach (var process in traceLog.Processes)
            {
                if (analyzer.AllGCProcessData.ContainsKey(process.Name))
                {
                    processIDMap[process.ProcessID] = process.Name;
                }
            }

            var eventSource = traceLog.Events.GetSource();

            Dictionary<string, List<CPUProcessData>> cpuProcessData = new();

            bool isInGC = false;
            MutableTraceEventStackSource stackSource = new MutableTraceEventStackSource(traceLog);
            StackSourceSample sample = new StackSourceSample(stackSource);
            var keys = new HashSet<string>(analyzer.AllGCProcessData.Keys);

            eventSource.NeedLoadedDotNetRuntimes();
            eventSource.AddCallbackOnProcessStart((Microsoft.Diagnostics.Tracing.Analysis.TraceProcess proc) =>
            {
                // Don't create the analysis for cases that haven't been requested.
                if (!processIDMap.TryGetValue(proc.ProcessID, out var procName))
                {
                    return;
                }

                if (!cpuProcessData.TryGetValue(procName, out var cpuProcesses))
                {
                    cpuProcessData[procName] = cpuProcesses = new List<CPUProcessData>();
                }

                CPUProcessData processData = new()
                {
                    Parent = this,
                    ProcessID = proc.ProcessID,
                    ProcessName = procName,
                };
                var predicate = new Predicate<Microsoft.Diagnostics.Tracing.TraceEvent>((s) => s.ProcessID == proc.ProcessID);
                var traceEvents = analyzer.TraceLog.Events.Filter((x) => ((predicate == null) || predicate(x)) && x is SampledProfileTraceData && x.ProcessID != 0);
                var traceStackSource = new TraceEventStackSource(traceEvents);
                traceStackSource.ShowUnknownAddresses = true;

                // Clone the samples so that the caller doesn't have to go back to the ETL file from here on.
                var allStackSource = CopyStackSource.Clone(traceStackSource);
                processData.StackView = new StackView(analyzer.TraceLog, allStackSource, SymbolReader, traceEvents);

                foreach (var method in Configuration.gc_methods)
                {
                    CPUThreadData d = processData.MethodToData[method] = new CPUThreadData
                    {
                        Method = method,
                        Thread = "-1"
                    };

                    var n = processData.StackView.GetAllCPUDataByName(method);
                    d.InclusiveCount = n.InclusiveCount;
                    d.ExclusiveCount = n.ExclusiveCount;
                }

                var process = analyzer.GetProcessGCData(procName).SingleOrDefault(g => g.ProcessID == proc.ProcessID);
                if (process == null)
                {
                    _symbolWriter.WriteLine($"CPUAnalysis: No Data found for: {procName}");
                    return;
                }

                foreach (var gc in process.GCs)
                {
                    if (gc.Type == Microsoft.Diagnostics.Tracing.Parsers.Clr.GCType.BackgroundGC)
                    {
                        continue;
                    }

                    Dictionary<string, CPUThreadData> methodDict = new();
                    var filterParams = new FilterParams();
                    filterParams.StartTimeRelativeMSec = gc.PauseStartRelativeMSec.ToString("0.######");
                    filterParams.EndTimeRelativeMSec = (gc.PauseStartRelativeMSec + gc.PauseDurationMSec).ToString("0.######");
                    FilterStackSource timeFilteredStackSource =
                        new FilterStackSource(filterParams, allStackSource, ScalingPolicyKind.ScaleToData);
                    StackView stackView = new(processData.Parent.Analyzer.TraceLog, timeFilteredStackSource, processData.Parent.SymbolReader, processData.StackView.TraceEvents);

                    foreach (var method in Configuration.gc_methods)
                    {
                        if (!methodDict.TryGetValue(method, out var m))
                        {
                            methodDict[method] = m = new CPUThreadData
                            {
                                Method = method,
                                Thread = "-1",
                            };
                        }

                        var n = stackView.GetAllCPUDataByName(method);
                        if (!n.Method.Contains(method))
                        {
                            m.InclusiveCount = 0;
                            m.ExclusiveCount = 0;
                        }

                        else
                        {
                            m.InclusiveCount = n.InclusiveCount;
                            m.ExclusiveCount = n.ExclusiveCount;
                        }
                    }

                    processData.GCOverallData[gc.Number] = methodDict;
                }

                // For each gc.
                foreach (var gc in processData.GCOverallData)
                {
                    // For each method in that gc.
                    foreach (var method in gc.Value)
                    {
                        if (!processData.OverallData.TryGetValue(method.Key, out var d))
                        {
                            processData.OverallData[method.Key] = d = new Dictionary<int, CPUThreadData>();
                        }

                        if (!d.TryGetValue(gc.Key, out var m))
                        {
                            d[gc.Key] = m = new CPUThreadData
                            {
                                Method = method.Key,
                                Thread = "-1"
                            };
                        }

                        m.InclusiveCount += method.Value.InclusiveCount;
                        m.ExclusiveCount += method.Value.ExclusiveCount;
                    }
                }

                cpuProcesses.Add(processData);

                proc.AddCallbackOnDotNetRuntimeLoad((TraceLoadedDotNetRuntime runtime) =>
                {
                    HashSet<CallTreeNode> visited = new();
                    runtime.GCStart += (p, gc) =>
                    {
                        isInGC = true;
                    };

                    runtime.GCEnd += (p, gc) =>
                    {
                        isInGC = false;

                        // Process.
                        CallTree callTree = new CallTree(ScalingPolicyKind.ScaleToData)
                        {
                            StackSource = stackSource
                        };

                        List<CallTreeNodeBase> byID = callTree.ByIDSortedExclusiveMetric();

                        foreach (var method in methodsToConsider)
                        {
                            // Find nodes by name -> For each node, recurse till we find the thread nodes and then accrue them appropriately. 
                            var nodes = FindNodesByName(method, byID);

                            foreach (var node in nodes)
                            {
                                var callerTree = AggregateCallTreeNode.CallerTree(node);

                                foreach (var callee in callerTree.AllCallees)
                                {
                                    DepthFirstSearch(callee, method, gc.Number, processData);
                                }
                            }
                        }

                        stackSource = new MutableTraceEventStackSource(analyzer.TraceLog);
                        sample = new StackSourceSample(stackSource);
                    };
                });
            });

            eventSource.Kernel.PerfInfoSample += (SampledProfileTraceData data) =>
            {
                if (isInGC && keys.Contains(data.ProcessName))
                {
                    sample.StackIndex = stackSource.GetCallStack(data.CallStackIndex(), data);
                    sample.TimeRelativeMSec = data.TimeStampRelativeMSec;
                    stackSource.AddSample(sample);
                }
            };

            eventSource.Process();
            return cpuProcessData;
        }

        public Analyzer Analyzer { get; }
        public SymbolReader SymbolReader { get; }
        public GCMethodsData Configuration { get; }

        // Process Name -> CPU Process Data.
        public Dictionary<string, List<CPUProcessData>> CPUAnalyzers { get; } = new();
    }
}
