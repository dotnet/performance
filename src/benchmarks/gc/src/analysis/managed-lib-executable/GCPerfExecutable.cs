// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// NOTE: This is just for debugging without needing pythonnet.
// Not intended to be fully-fledged analysis.

using System;
using System.Collections.Generic;
using System.Linq;

using GCPerf;

[Obsolete]
public static class Program 
{
    public static void Main(string[] args)
    {
        string cmd = args[0];
        switch (cmd)
        {
            case "analyze-single":
                AnalyzeSingle(args.Skip(1).ToArray());
                break;
            default:
                throw new Exception($"Bad command {cmd}");
        }
    }

    public static void AnalyzeSingle(IReadOnlyList<string> args)
    {
        string path = args[0];
        var processes = Analysis.GetTracedProcesses(path, collectEventNames: true, collectPerHeapHistoryTimes: true);
        // do something with them
    }
}