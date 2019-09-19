// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace CompilerBenchmarks
{
    internal static class Helpers
    {
        public const string TestProjectEnvVarName = "ROSLYN_TEST_PROJECT_DIR";

        public static CSharpCommandLineArguments GetReproCommandLineArgs()
        {
            var projectDir = Environment.GetEnvironmentVariable(TestProjectEnvVarName);
            return CSharpCommandLineParser.Default.Parse(
                new[] { "@repro.rsp"},
                projectDir,
                sdkDirectory: null);
        }

        public static CSharpCompilation CreateReproCompilation()
        {
            var projectDir = Environment.GetEnvironmentVariable(TestProjectEnvVarName);
            var cmdLineArgs = GetReproCommandLineArgs();
            var sourceFiles = cmdLineArgs.SourceFiles;
            var trees = new SyntaxTree[sourceFiles.Length];
            Parallel.For(0, sourceFiles.Length, i =>
            {
                var path = sourceFiles[i].Path;
                trees[i] = SyntaxFactory.ParseSyntaxTree(
                    File.ReadAllText(path),
                    cmdLineArgs.ParseOptions,
                    path);
            });

            var references = cmdLineArgs.MetadataReferences
                .Select(r => MetadataReference.CreateFromFile(Path.Combine(projectDir, r.Reference)))
                .ToList();

            return CSharpCompilation.Create(
                "Microsoft.CodeAnalysis",
                trees,
                references,
                options: cmdLineArgs.CompilationOptions);
        }
    }
}
