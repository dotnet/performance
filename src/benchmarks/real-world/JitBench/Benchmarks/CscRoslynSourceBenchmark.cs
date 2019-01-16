// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace RealWorld
{
    class CscRoslynSourceBenchmark : CscBenchmark
    {
        public CscRoslynSourceBenchmark() : base("Csc_Roslyn_Source")
        {
        }

        protected override async Task SetupSourceToCompile(string intermediateOutputDir, string runtimeDirPath, bool useExistingSetup, ITestOutputHelper output)
        {
            string cscSourceDownloadLink = "https://roslyninfra.blob.core.windows.net/perf-artifacts/CodeAnalysisRepro" +
                    (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ".zip" : ".tar.gz");
            string sourceDownloadDir = Path.Combine(intermediateOutputDir, "roslynSource");
            string sourceDir = Path.Combine(sourceDownloadDir, "CodeAnalysisRepro");
            CommandLineArguments = "@repro.rsp";
            WorkingDirPath = sourceDir;
            if(useExistingSetup)
            {
                return;
            }

            await FileTasks.DownloadAndUnzip(cscSourceDownloadLink, sourceDownloadDir, output);
        }
    }
}
