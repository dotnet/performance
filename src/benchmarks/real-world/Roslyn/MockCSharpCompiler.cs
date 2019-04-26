// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace CompilerBenchmarks
{
    internal class MockCSharpCompiler : CSharpCompiler
    {
        public MockCSharpCompiler(string responseFile, string workingDirectory, string[] args)
            : base(CSharpCommandLineParser.Default, responseFile, args, CreateBuildPaths(workingDirectory), Environment.GetEnvironmentVariable("LIB"), null)
        {
        }

        private static BuildPaths CreateBuildPaths(string workingDirectory, string sdkDirectory = null)
            =>  new BuildPaths(
                clientDir: AppContext.BaseDirectory,
                workingDir: workingDirectory,
                sdkDir: sdkDirectory,
                tempDir: Path.GetTempPath());
    }
}