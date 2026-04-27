using Diagnostics.Tracing.StackSources;
using Microsoft.Diagnostics.Symbols;
using Microsoft.Diagnostics.Tracing.Stacks;
using System.Text.RegularExpressions;
using Address = System.UInt64;
using EtlxNS = Microsoft.Diagnostics.Tracing.Etlx;

namespace GC.Analysis.API.CPUAnalysis
{
    public static class SourceCodeAnalysis
    {
        internal static TraceEventStackSource GetTraceEventStackSource(StackSource source)
        {
            StackSourceStacks rawSource = source;
            TraceEventStackSource asTraceEventStackSource = null;
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

        public static SourceLocation GetSourceLocation(CallTreeNodeBase asCallTreeNodeBase,
                                                       string cellText,
                                                       SymbolReader symbolReader,
                                                       FilterStackSource stackSource,
                                                       out SortedDictionary<int, float> metricOnLine)
        {
            metricOnLine = null;
            var m = Regex.Match(cellText, "(.*!.*)");
            if (m.Success)
            {
                cellText = m.Groups[1].Value;
            }

            // Find the most numerous call stack
            // TODO this can be reasonably expensive.   If it is a problem do something about it (e.g. sampling)
            var frameIndexCounts = new Dictionary<StackSourceFrameIndex, float>();

            asCallTreeNodeBase.GetSamples(false, delegate (StackSourceSampleIndex sampleIdx)
            {
                // Find the callStackIdx which corresponds to the name in the cell, and log it to callStackIndexCounts
                var matchingFrameIndex = StackSourceFrameIndex.Invalid;
                var sample = stackSource.GetSampleByIndex(sampleIdx);
                var callStackIdx = sample.StackIndex;
                while (callStackIdx != StackSourceCallStackIndex.Invalid)
                {
                    var frameIndex = stackSource.GetFrameIndex(callStackIdx);
                    var frameName = stackSource.GetFrameName(frameIndex, false);
                    if (frameName.Contains(cellText))
                    {
                        matchingFrameIndex = frameIndex;        // We keep overwriting it, so we get the entry closest to the root.  
                    }

                    callStackIdx = stackSource.GetCallerIndex(callStackIdx);
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
                return null;
            }

            // Find the most primitive TraceEventStackSource
            TraceEventStackSource asTraceEventStackSource = GetTraceEventStackSource(stackSource);
            if (asTraceEventStackSource == null)
            {
                // TODO: Log error.
                //StatusBar.LogError("Source does not support symbolic lookup.");
                return null;
            }

            var reader = symbolReader;

            var frameToLine = new Dictionary<StackSourceFrameIndex, int>();

            // OK actually get the source location of the maximal value (our return value). 
            var sourceLocation = asTraceEventStackSource.GetSourceLine(maxFrameIdx, reader);
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

            //StatusBar.LogWriter.WriteLine();
            //StatusBar.LogWriter.WriteLine("Metric as a function of code address");
            //StatusBar.LogWriter.WriteLine("      Address    :   Line     Metric");
            /*
            foreach (var keyValue in nativeAddressFreq)
            {
                // TODO: USe this.
                Console.WriteLine("    {0,12:x} : {1,6} {2,10:f1}", keyValue.Key, keyValue.Value.Item1, keyValue.Value.Item2);
            }

            if (sourceLocation == null)
            {
                //StatusBar.LogError("Source could not find a source location for the given Frame.");
                return null;
            }
            */

            //StatusBar.LogWriter.WriteLine();
            //StatusBar.LogWriter.WriteLine("Metric per line in the file {0}", Path.GetFileName(sourceLocation.SourceFile.BuildTimeFilePath));
            /*
    #pragma warning disable CS8602 // Dereference of a possibly null reference.
            foreach (var keyVal in metricOnLine)
            {
                // TODO: Fix this.
                //StatusBar.LogWriter.WriteLine("    Line {0,5}:  Metric {1,5:n1}", keyVal.Key, keyVal.Value);
                Console.WriteLine("    Line {0,5}:  Metric {1,5:n1}", keyVal.Key, keyVal.Value);
            }
            */
#pragma warning restore CS8602 // Dereference of a possibly null reference.

            return sourceLocation;
        }
    }
}
