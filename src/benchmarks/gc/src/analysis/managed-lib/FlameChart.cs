// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

﻿using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Microsoft.Diagnostics.Tracing.Stacks;

namespace GCPerf
{
    /// <summary>
    /// Functionality for building flame chart representations of stack data
    /// </summary>
    public class FlameChart
    {
        /// <summary>
        /// Build a flame chart for the given StackSource
        /// </summary>
        /// <param name="stackSource">The set of stacks to represent</param>
        /// <param name="fileToWrite">The name of the output file</param>
        /// <param name="rootFunction">Optional. If specified, "roots" the flame chart on that function so all of its ancestors are not present.</param>
        /// <param name="title">Optional. The title of the flame chart</param>
        /// <returns>A boolean indicating whether the operation succeeded.</returns>
        public static bool BuildFlameChart(StackSource stackSource, string fileToWrite, string rootFunction, string title)
        {
            string intermediateFilePath = fileToWrite + "_tmp";
            if (!BuildInternalTempRepresentation(stackSource, intermediateFilePath, rootFunction))
                return false;

            string titleLabel = title ?? rootFunction ?? "";
            return InternalBuildFlameChart(intermediateFilePath, titleLabel, fileToWrite);
        }

        /// <summary>
        /// Build differential flame charts for two sets of stacks. One chart will contain the diff from A to B and the other from B to A.
        /// </summary>
        /// <param name="stackSourceA">A set of stacks</param>
        /// <param name="identifierA">An identifier for the set of stacks</param>
        /// <param name="stackSourceB">Another set of stacks</param>
        /// <param name="identifierB">An identifier for the second set of stacks</param>
        /// <param name="rootFunction">Optional. If specified, "roots" the flame chart on that function so all of its ancestors are not present.</param>
        /// <param name="outputDir">Directory in which to place the outputs.</param>
        /// <returns>A boolean indicating whether the operation succeeded.</returns>
        public static bool BuildDiffFlameCharts(StackSource stackSourceA, string identifierA, StackSource stackSourceB, string identifierB, string rootFunction, string outputDir)
        {
            // "Sanitize" the identifiers to remove any illegal characters
            string identifierASanitized = RemoveIllegalFilenameChars(identifierA);
            string identifierBSanitized = RemoveIllegalFilenameChars(identifierB);

            string tempFileA = Path.Combine(outputDir, string.Format("{0}_tmp", identifierASanitized));
            bool ret = BuildInternalTempRepresentation(stackSourceA, tempFileA, rootFunction);
            if (!ret)
                return false;

            string tempFileB = Path.Combine(outputDir, string.Format("{0}_tmp", identifierBSanitized));
            ret = BuildInternalTempRepresentation(stackSourceB, tempFileB, rootFunction);
            if (!ret)
                return false;

            string titlePrefix = string.IsNullOrWhiteSpace(rootFunction) ? string.Empty : string.Format("{0}-", RemoveIllegalFilenameChars(rootFunction));
            string chartTitle = string.Format("{0}{1} vs {2}", titlePrefix, identifierB, identifierA);

            string outputFilenameBase = string.Format("{0}{1}_vs_{2}", titlePrefix, identifierBSanitized, identifierASanitized);
            string outputDiffFile = Path.Combine(outputDir, outputFilenameBase + ".difftmp");
            string outputSvg = Path.Combine(outputDir, outputFilenameBase + ".svg");

            ProcessStartInfo psi = new ProcessStartInfo();
            psi.FileName = @"perl.exe";
            psi.CreateNoWindow = false;
            psi.UseShellExecute = false;
            psi.RedirectStandardOutput = true;

            // We pass the '-n' option to normalize the counts before diffing
            psi.Arguments = string.Format("difffolded.pl -n {0} {1}", tempFileA, tempFileB);

            var perlProc = Process.Start(psi);
            using (TextWriter writer = File.CreateText(outputDiffFile))
            {
                try
                {
                    writer.Write(perlProc.StandardOutput.ReadToEnd());
                }
                catch (IOException)
                {
                    return false;
                }
            }

            ret = InternalBuildFlameChart(outputDiffFile, chartTitle, outputSvg);
            if (!ret)
                return false;

            // Diff the other way as well
            chartTitle = string.Format("{0}{1} vs {2}", titlePrefix, identifierA, identifierB);
            outputFilenameBase = string.Format("{0}{1}_vs_{2}", titlePrefix, identifierASanitized, identifierBSanitized);
            outputDiffFile = Path.Combine(outputDir, outputFilenameBase + ".difftmp");
            outputSvg = Path.Combine(outputDir, outputFilenameBase + ".svg");

            psi.Arguments = string.Format("difffolded.pl {0} {1}", tempFileB, tempFileA);
            perlProc = Process.Start(psi);
            using (TextWriter writer = File.CreateText(outputDiffFile))
            {
                try
                {
                    writer.Write(perlProc.StandardOutput.ReadToEnd());
                }
                catch (IOException)
                {
                    return false;
                }
            }

            ret = InternalBuildFlameChart(outputDiffFile, chartTitle, outputSvg);

            return ret;
        }

        private static bool BuildInternalTempRepresentation(StackSource stackSource, string fileToWrite, string rootFunction)
        {
            StringBuilder flameChartStringBuilder = new StringBuilder();
            bool enableRootingOnFunction = !string.IsNullOrWhiteSpace(rootFunction);

            // Write out the flame chart format, one line per stack
            // eg: corerun;foo;bar;baz 1
            stackSource.ForEach(sample =>
            {
                Stack<StackSourceCallStackIndex> callStackIndices = new Stack<StackSourceCallStackIndex>();

                callStackIndices.Push(sample.StackIndex);

                StackSourceCallStackIndex callerIdx = stackSource.GetCallerIndex(sample.StackIndex);
                while (callerIdx != StackSourceCallStackIndex.Invalid)
                {
                    callStackIndices.Push(callerIdx);
                    callerIdx = stackSource.GetCallerIndex(callerIdx);
                }

                bool firstOne = true;
                bool foundRootFunction = false;
                while (callStackIndices.Count > 0)
                {
                    var currFrame = callStackIndices.Pop();
                    var frameIdx = stackSource.GetFrameIndex(currFrame);

                    var frameName = stackSource.GetFrameName(frameIdx, false);

                    // If we're rooting on a function, skip the frames above it
                    if (enableRootingOnFunction && !foundRootFunction)
                    {
                        if (frameName.Contains(rootFunction))
                        {
                            foundRootFunction = true;
                        }
                        else
                        {
                            continue;
                        }
                    }

                    if (!firstOne)
                    {
                        flameChartStringBuilder.Append(";");
                    }

                    flameChartStringBuilder.Append(frameName);
                    firstOne = false;
                }

                flameChartStringBuilder.Append(" 1");
                flameChartStringBuilder.AppendLine();
            });

            using (TextWriter writer = File.CreateText(fileToWrite))
            {
                try
                {
                    writer.Write(flameChartStringBuilder.ToString());
                }
                catch (IOException)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool InternalBuildFlameChart(string intermediateFilePath, string chartTitle, string outputFilePath)
        {
            ProcessStartInfo psi = new ProcessStartInfo();

            psi.FileName = @"perl.exe";
            psi.CreateNoWindow = false;
            psi.Arguments = string.Format("flamegraph.pl {0} --width 1800 --title=\"{1}\"", intermediateFilePath, chartTitle);
            psi.UseShellExecute = false;
            psi.RedirectStandardOutput = true;

            var perlProc = Process.Start(psi);
            using (TextWriter writer = File.CreateText(outputFilePath))
            {
                try
                {
                    writer.Write(perlProc.StandardOutput.ReadToEnd());
                }
                catch (IOException)
                {
                    return false;
                }
            }

            perlProc.WaitForExit();
            return true;
        }

        private static string RemoveIllegalFilenameChars(string filename)
        {
            string cleanFilename = filename;

            int idx = cleanFilename.IndexOfAny(Path.GetInvalidFileNameChars());
            while (idx != -1)
            {
                cleanFilename = cleanFilename.Remove(idx, 1);
                idx = cleanFilename.IndexOfAny(Path.GetInvalidFileNameChars());
            }

            return cleanFilename;
        }
    }
}
