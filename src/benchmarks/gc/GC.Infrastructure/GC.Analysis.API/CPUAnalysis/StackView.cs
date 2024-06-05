using Diagnostics.Tracing.StackSources;
using Microsoft.Data.Analysis;
using Microsoft.Diagnostics.Symbols;
using Microsoft.Diagnostics.Tracing.Parsers.Kernel;
using Microsoft.Diagnostics.Tracing.Stacks;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace GC.Analysis.API
{
    using ProcessID = Int32;
    using ThreadID = Int32;
    using EtlxNS = Microsoft.Diagnostics.Tracing.Etlx;
    using Address = UInt64;

    internal class StackView
    {
        private static readonly char[] SymbolSeparator = new char[] { '!' };
        public Lazy<List<CallTreeNodeBase>> GCThreadNodes { get; }

        private EtlxNS.TraceLog _traceLog;
        private StackSource _rawStackSource;
        private SymbolReader _symbolReader;
        private CallTree _callTree;
        private List<CallTreeNodeBase> _byName;
        private HashSet<string> _resolvedSymbolModules = new HashSet<string>();

        public StackView(EtlxNS.TraceLog traceLog, StackSource stackSource, SymbolReader symbolReader, EtlxNS.TraceEvents traceEvents)
        {
            _traceLog = traceLog;
            _rawStackSource = stackSource;
            _symbolReader = symbolReader;
            TraceEvents = traceEvents;
            LookupWarmNGENSymbols();
        }

        public SymbolReader SymbolReader => _symbolReader;

        public CallTree CallTree
        {
            get
            {
                if (_callTree == null)
                {
                    FilterStackSource filterStackSource = new FilterStackSource(new FilterParams(), _rawStackSource, ScalingPolicyKind.ScaleToData);
                    _callTree = new CallTree(ScalingPolicyKind.ScaleToData)
                    {
                        StackSource = filterStackSource
                    };
                }
                return _callTree;
            }
        }

        private IEnumerable<CallTreeNodeBase> ByName
        {
            get
            {
                if (_byName == null)
                {
                    _byName = CallTree.ByIDSortedExclusiveMetric();
                }

                return _byName;
            }
        }

        public EtlxNS.TraceLog TraceLog => _traceLog;
        public EtlxNS.TraceEvents TraceEvents { get; }

        public CallTreeNodeBase FindNodeByName(string nodeNamePat)
        {
            var regEx = new Regex(nodeNamePat, RegexOptions.IgnoreCase);
            foreach (var node in ByName)
            {
                if (regEx.IsMatch(node.Name))
                {
                    return node;
                }
            }

            return CallTree.Root;
        }

        public IReadOnlyList<CallTreeNodeBase> GetAllMatchedCallTreeNodesByName(string nodeNamePat)
        {
            List<CallTreeNodeBase> callTreeNodeBases = new List<CallTreeNodeBase>();
            var regEx = new Regex(nodeNamePat, RegexOptions.IgnoreCase);

            foreach (var node in ByName)
            {
                if (regEx.IsMatch(node.Name))
                {
                    callTreeNodeBases.Add(node);
                }
            }

            return callTreeNodeBases;
        }

        public CPUThreadData GetAllCPUDataByName(string nodeNamePat, string caller)
        {
            CPUThreadData data = new();
            data.Method = nodeNamePat;
            data.InclusiveCount = 0;
            data.ExclusiveCount = 0;

            List<string> callers = new();
            var regEx = new Regex(nodeNamePat, RegexOptions.IgnoreCase);
            foreach (var node in ByName)
            {
                if (regEx.IsMatch(node.Name))
                {
                    data.InclusiveCount += node.InclusiveCount;
                    data.ExclusiveCount += node.ExclusiveCount;

                    CallTreeNode cursor = AggregateCallTreeNode.CallerTree(node);
                    while (cursor != null)
                    {
                        callers.Add(cursor.Caller?.DisplayName ?? string.Empty);
                        cursor = cursor.Caller;
                    }
                }
            }

            data.Callers = callers;
            return data;
        }

        public CPUThreadData GetAllCPUDataByName(string nodeNamePat)
        {
            CPUThreadData data = new();
            data.Method = nodeNamePat;
            data.InclusiveCount = 0;
            data.ExclusiveCount = 0;

            var regEx = new Regex(nodeNamePat, RegexOptions.IgnoreCase);
            foreach (var node in ByName)
            {
                if (regEx.IsMatch(node.Name))
                {
                    data.InclusiveCount += node.InclusiveCount;
                    data.ExclusiveCount += node.ExclusiveCount;
                }
            }

            return data;
        }

        public IEnumerable<CPUThreadData> GetAllMatchedCPUDataByName(string nodeNamePat)
        {
            List<CPUThreadData> allData = new();

            var regEx = new Regex(nodeNamePat, RegexOptions.IgnoreCase);
            foreach (var node in ByName)
            {
                if (regEx.IsMatch(node.Name))
                {
                    CPUThreadData data = new()
                    {
                        Method = node.Name,
                        InclusiveCount = node.InclusiveCount,
                        ExclusiveCount = node.ExclusiveCount
                    };

                    allData.Add(data);
                }
            }

            return allData;
        }

        public CallTreeNode GetCallers(string focusNodeName)
        {
            var focusNode = FindNodeByName(focusNodeName);
            return AggregateCallTreeNode.CallerTree(focusNode);
        }

        public CallTreeNode GetCallees(string focusNodeName)
        {
            var focusNode = FindNodeByName(focusNodeName);
            return AggregateCallTreeNode.CalleeTree(focusNode);
        }

        public CallTreeNodeBase GetCallTreeNode(string symbolName)
        {
            string[] symbolParts = symbolName.Split(SymbolSeparator);
            if (symbolParts.Length != 2)
            {
                return null;
            }

            // Try to get the call tree node.
            CallTreeNodeBase node = FindNodeByName(Regex.Escape(symbolName));

            // Check to see if the node matches.
            if (node.Name.StartsWith(symbolName, StringComparison.OrdinalIgnoreCase))
            {
                return node;
            }

            // Check to see if we should attempt to load symbols.
            if (!_resolvedSymbolModules.Contains(symbolParts[0]))
            {
                // Look for an unresolved symbols node for the module.
                string unresolvedSymbolsNodeName = symbolParts[0] + "!?";
                node = FindNodeByName(unresolvedSymbolsNodeName);
                if (node.Name.Equals(unresolvedSymbolsNodeName, StringComparison.OrdinalIgnoreCase))
                {
                    // Symbols haven't been resolved yet.  Try to resolve them now.
                    EtlxNS.TraceModuleFile moduleFile = _traceLog.ModuleFiles.Where(m => m.Name.Equals(symbolParts[0], StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
                    if (moduleFile != null)
                    {
                        // Special handling for NGEN images.
                        if (symbolParts[0].EndsWith(".ni", StringComparison.OrdinalIgnoreCase))
                        {
                            SymbolReaderOptions options = _symbolReader.Options;
                            try
                            {
                                _symbolReader.Options = SymbolReaderOptions.CacheOnly;
                                _traceLog.CallStacks.CodeAddresses.LookupSymbolsForModule(_symbolReader, moduleFile);
                            }
                            finally
                            {
                                _symbolReader.Options = options;
                            }
                        }
                        else
                        {
                            _traceLog.CallStacks.CodeAddresses.LookupSymbolsForModule(_symbolReader, moduleFile);
                        }
                        InvalidateCachedStructures();
                    }
                }

                // Mark the module as resolved so that we don't try again.
                _resolvedSymbolModules.Add(symbolParts[0]);

                // Try to get the call tree node one more time.
                node = FindNodeByName(Regex.Escape(symbolName));

                // Check to see if the node matches.
                if (node.Name.StartsWith(symbolName, StringComparison.OrdinalIgnoreCase))
                {
                    return node;
                }
            }

            return null;
        }

        private void LookupWarmNGENSymbols()
        {
            TraceEventStackSource asTraceEventStackSource = GetTraceEventStackSource(_rawStackSource);
            if (asTraceEventStackSource == null)
            {
                return;
            }

            SymbolReaderOptions savedOptions = _symbolReader.Options;
            try
            {
                // NGEN PDBs (even those not yet produced) are considered to be in the cache.
                _symbolReader.Options = SymbolReaderOptions.CacheOnly;

                // Resolve all NGEN images.
                asTraceEventStackSource.LookupWarmSymbols(1, _symbolReader, _rawStackSource, s => s.Name.EndsWith(".ni", StringComparison.OrdinalIgnoreCase));

                // Invalidate cached data structures to finish resolving symbols.
                InvalidateCachedStructures();
            }
            finally
            {
                _symbolReader.Options = savedOptions;
            }
        }

        /// <summary>
        /// Unwind the wrapped sources to get to a TraceEventStackSource if possible. 
        /// </summary>
        private static TraceEventStackSource GetTraceEventStackSource(StackSource source)
        {
            StackSourceStacks rawSource = source;
            TraceEventStackSource? asTraceEventStackSource = null;
            for (; ; )
            {
                asTraceEventStackSource = rawSource as TraceEventStackSource;
                if (asTraceEventStackSource != null)
                {
                    return asTraceEventStackSource;
                }

                var asCopyStackSource = rawSource as CopyStackSource;
                if (asCopyStackSource != null)
                {
                    rawSource = asCopyStackSource.SourceStacks;
                    continue;
                }
                var asStackSource = rawSource as StackSource;
                if (asStackSource != null && asStackSource != asStackSource.BaseStackSource)
                {
                    rawSource = asStackSource.BaseStackSource;
                    continue;
                }
                return null;
            }
        }

        private void InvalidateCachedStructures()
        {
            _byName = null;
            _callTree = null;
        }

        public DataFrame GetCPUDataForGCMethods(IEnumerable<string> methods)
        {
            StringDataFrameColumn methodName = new StringDataFrameColumn("Method Name");
            DoubleDataFrameColumn inclusiveCount = new DoubleDataFrameColumn("Inclusive Count");
            DoubleDataFrameColumn exclusiveCount = new DoubleDataFrameColumn("Exclusive Count");

            foreach (var method in methods)
            {
                // Assumes, symbols are all well resolved.
                var node = GetAllCPUDataByName(method);
                methodName.Append(node.Method);
                inclusiveCount.Append(node.InclusiveCount);
                exclusiveCount.Append(node.ExclusiveCount);
            }

            return new DataFrame(methodName, inclusiveCount, exclusiveCount).OrderByDescending("Exclusive Count");
        }

        public bool TryGetSourceLocation(
            string searchString,
            [NotNullWhen(true)] out SourceLocation? sourceLocation,
            [NotNullWhen(true)] out SortedDictionary<int, float>? metricOnLine)
        {
            sourceLocation = null;
            metricOnLine = null;

            // Find the most numerous call stack
            // TODO this can be reasonably expensive.   If it is a problem do something about it (e.g. sampling)
            var frameIndexCounts = new Dictionary<StackSourceFrameIndex, float>();
            FindNodeByName(searchString).GetSamples(false, delegate (StackSourceSampleIndex sampleIdx)
            {
                // Find the callStackIdx which corresponds to the name in the cell, and log it to callStackIndexCounts
                var matchingFrameIndex = StackSourceFrameIndex.Invalid;
                var sample = CallTree.StackSource.GetSampleByIndex(sampleIdx);
                var callStackIdx = sample.StackIndex;
                while (callStackIdx != StackSourceCallStackIndex.Invalid)
                {
                    var frameIndex = CallTree.StackSource.GetFrameIndex(callStackIdx);
                    var frameName = CallTree.StackSource.GetFrameName(frameIndex, false);
                    if (frameName.Contains(searchString))
                    {
                        matchingFrameIndex = frameIndex;        // We keep overwriting it, so we get the entry closest to the root.  
                    }

                    callStackIdx = CallTree.StackSource.GetCallerIndex(callStackIdx);
                }
                if (matchingFrameIndex != StackSourceFrameIndex.Invalid)
                {
                    float count = 0;
                    frameIndexCounts.TryGetValue(matchingFrameIndex, out count);
                    frameIndexCounts[matchingFrameIndex] = count + sample.Metric;
                }
                return true;
            });

            // Get the frame with the most counts, we go to THAT line and only open THAT file.
            // If other samples are in that file we also display them but it is this maximum
            // that drives which file we open and where we put the editor's focus.  
            StackSourceFrameIndex maxFrameIdx = StackSourceFrameIndex.Invalid;
            float maxFrameIdxCount = -1;
            foreach (var keyValue in frameIndexCounts)
            {
                if (keyValue.Value >= maxFrameIdxCount)
                {
                    maxFrameIdxCount = keyValue.Value;
                    maxFrameIdx = keyValue.Key;
                }
            }

            if (maxFrameIdx == StackSourceFrameIndex.Invalid)
            {
                // TODO: Log an error.
                return false;
            }

            // Find the most primitive TraceEventStackSource
            TraceEventStackSource asTraceEventStackSource = GetTraceEventStackSource(CallTree.StackSource);
            var cpuEvents = asTraceEventStackSource.TraceLog.Events.Where(e => e is SampledProfileTraceData && e.ProcessID != 0);

            if (asTraceEventStackSource == null)
            {
                // TODO: Log error.
                //StatusBar.LogError("Source does not support symbolic lookup.");
                return false;
            }

            var reader = _symbolReader;

            var frameToLine = new Dictionary<StackSourceFrameIndex, int>();

            // OK actually get the source location of the maximal value (our return value). 
            sourceLocation = asTraceEventStackSource.GetSourceLine(maxFrameIdx, reader);
            if (sourceLocation != null)
            {
                var filePathForMax = sourceLocation.SourceFile.BuildTimeFilePath;
                metricOnLine = new SortedDictionary<int, float>();
                // Accumulate the counts on a line basis
                foreach (StackSourceFrameIndex frameIdx in frameIndexCounts.Keys)
                {
                    var loc = asTraceEventStackSource.GetSourceLine(frameIdx, reader);
                    if (loc != null && loc.SourceFile.BuildTimeFilePath == filePathForMax)
                    {
                        frameToLine[frameIdx] = loc.LineNumber;
                        float metric;
                        metricOnLine.TryGetValue(loc.LineNumber, out metric);
                        metric += frameIndexCounts[frameIdx];
                        metricOnLine[loc.LineNumber] = metric;
                    }
                }
            }

            // show the frequency on a per address form.  

            bool commonMethodIdxSet = false;
            EtlxNS.MethodIndex commonMethodIdx = EtlxNS.MethodIndex.Invalid;

            var nativeAddressFreq = new SortedDictionary<Address, Tuple<int, float>>();
            foreach (var keyValue in frameIndexCounts)
            {
                var codeAddr = asTraceEventStackSource.GetFrameCodeAddress(keyValue.Key);
                if (codeAddr != EtlxNS.CodeAddressIndex.Invalid)
                {
                    var methodIdx = asTraceEventStackSource.TraceLog.CodeAddresses.MethodIndex(codeAddr);
                    if (methodIdx != EtlxNS.MethodIndex.Invalid)
                    {
                        if (!commonMethodIdxSet)
                        {
                            commonMethodIdx = methodIdx;            // First time, set it as the common method.  
                        }
                        else if (methodIdx != commonMethodIdx)
                        {
                            methodIdx = EtlxNS.MethodIndex.Invalid;        // More than one method, give up.  
                        }

                        commonMethodIdxSet = true;
                    }

                    var nativeAddr = asTraceEventStackSource.TraceLog.CodeAddresses.Address(codeAddr);
                    var lineNum = 0;
                    frameToLine.TryGetValue(keyValue.Key, out lineNum);
                    nativeAddressFreq[nativeAddr] = new Tuple<int, float>(lineNum, keyValue.Value);
                }
            }

            return sourceLocation != null;
        }

        public string Annotate(SortedDictionary<int, float> metricOnLine, SourceLocation sourceLocation)
        {
            string FindFile(string baseFilePath, string extension = "", string cacheDir = "")
            {
                // We expect the original file to exist
                //Debug.Assert(File.Exists(baseFilePath));

                var baseFileName = Path.GetFileNameWithoutExtension(baseFilePath);
                var baseFileInfo = new FileInfo(baseFilePath);

                // The hash is a combination of full path, size and last write timestamp
                var hashData = Tuple.Create(Path.GetFullPath(baseFilePath), baseFileInfo.Length, baseFileInfo.LastWriteTimeUtc);
                int hash = hashData.GetHashCode();

                string ret = Path.Combine(cacheDir, baseFileName + "_" + hash.ToString("x") + extension);
                return ret;
            }

            string ToCompactString(float value)
            {
                var suffix = " |";
                for (int i = 0; ; i++)
                {
                    if (value < 999.95)
                    {
                        return value.ToString("f1").PadLeft(5) + suffix;
                    }

                    value = value / 1000;
                    if (i == 0)
                    {
                        suffix = "K|";
                    }
                    else if (i == 1)
                    {
                        suffix = "M|";
                    }
                    else if (i == 2)
                    {
                        suffix = "G|";
                    }
                    else
                    {
                        return "******|";
                    }
                }
            }

            void AnnotateLines(string inFileName, string outFileName, SortedDictionary<int, float> lineData)
            {
                using (var inFile = File.OpenText(inFileName))
                using (var outFile = File.CreateText(outFileName))
                {
                    int lineNum = 0;
                    for (; ; )
                    {
                        var line = inFile.ReadLine();
                        if (line == null)
                        {
                            break;
                        }

                        lineNum++;

                        float value;
                        if (lineData.TryGetValue(lineNum, out value))
                        {
                            outFile.Write(ToCompactString(value));
                        }
                        else if (lineNum == 1)
                        {
                            outFile.Write("Metric|");
                        }
                        else
                        {
                            outFile.Write("       ");
                        }

                        outFile.WriteLine(line);
                    }
                }
            }

            if (metricOnLine == null)
            {
                return string.Empty;
            }

            var sourceFile = sourceLocation.SourceFile;
            string logicalSourcePath = sourceLocation.SourceFile.GetSourceFile();
            if (logicalSourcePath != null)
            {
                bool checksumMatches = sourceLocation.SourceFile.ChecksumMatches;
            }

            var sourcePathToOpen = logicalSourcePath;
            if (sourcePathToOpen != null)
            {
                //StatusBar.Log("Resolved source file to " + sourcePathToOpen);
                if (metricOnLine != null)
                {
                    sourcePathToOpen = FindFile(sourcePathToOpen, Path.GetExtension(sourcePathToOpen));
                    //StatusBar.Log("Annotating source with metric to the file " + sourcePathToOpen);
                    AnnotateLines(logicalSourcePath, sourcePathToOpen, metricOnLine);
                }
            }

            var firstLine = metricOnLine.First().Key - 5;
            var lastLine = metricOnLine.Last().Key;

            return string.Join("\n",
                File.ReadLines(sourcePathToOpen).Skip(firstLine - 1).Take(lastLine - firstLine + 1));
        }

    }
}
