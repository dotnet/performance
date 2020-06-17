// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Helper functionality to locate inputs and find outputs for
// regex-redux benchmark in CoreCLR test harness

using System;
using System.IO;
using System.Text;

namespace BenchmarksGame
{
    class RegexReduxHelpers
    {
        public string InputFile;
        public int ExpectedLength;

        public RegexReduxHelpers(bool bigInput)
        {
            if (bigInput)
            {
                InputFile = InputFileHelper.FindInputFile("regexdna-input25000.txt");
                ExpectedLength = 136381;
            }
            else
            {
                InputFile = InputFileHelper.FindInputFile("regexdna-input25.txt");
                ExpectedLength = 152;
            }
        }
    }
}
