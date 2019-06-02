// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using static Microsoft.CodeAnalysis.Compilation;

namespace CompilerBenchmarks
{
    [BenchmarkCategory("Roslyn")]
    public class StageBenchmarks
    {
        private Compilation _comp;
        private CommonPEModuleBuilder _moduleBeingBuilt;
        private EmitOptions _options;
        private MemoryStream _peStream;
        private ParseOptions _parseOptions;
        private SourceText[] _files;

        [GlobalSetup(Target = nameof(Parsing))]
        public void ParsingSetup()
        {
            var projectDir = Environment.GetEnvironmentVariable(Helpers.TestProjectEnvVarName);
            var responseFile = Path.Combine(projectDir, "repro.rsp");
            var cmdLineParser = new CSharpCommandLineParser();
            var args = cmdLineParser.Parse(new[] { "@" + responseFile }, projectDir, sdkDirectory: null);
            _parseOptions = args.ParseOptions;
            _files = new SourceText[args.SourceFiles.Length];
            Parallel.For(0, _files.Length, index =>
            {
                using (var fstream = new FileStream(args.SourceFiles[index].Path, FileMode.Open))
                {
                    _files[index] = SourceText.From(fstream);
                }
            });
        }

        [Benchmark]
        public SyntaxTree[] Parsing()
        {
            var trees = new SyntaxTree[_files.Length];
            Parallel.For(0, _files.Length, index =>
            {
                trees[index] = SyntaxFactory.ParseSyntaxTree(_files[index], _parseOptions);
            });
            return trees;
        }

        [GlobalSetup(Target = nameof(CompileMethodsAndEmit))]
        public void LoadCompilation()
        {
            _peStream = new MemoryStream();
            _comp = Helpers.CreateReproCompilation();

            // Call GetDiagnostics to force declaration symbol binding to finish
            _ = _comp.GetDiagnostics();
        }

        [Benchmark]
        public object CompileMethodsAndEmit()
        {
            _peStream.Position = 0;
            return _comp.Emit(_peStream);
        }

        [GlobalSetup(Target = nameof(SerializeMetadata))]
        public void CompileMethods()
        {
            LoadCompilation();

            _options = EmitOptions.Default.WithIncludePrivateMembers(true);

            bool embedPdb = _options.DebugInformationFormat == DebugInformationFormat.Embedded;

            var diagnostics = DiagnosticBag.GetInstance();

            _moduleBeingBuilt = _comp.CheckOptionsAndCreateModuleBuilder(
                diagnostics,
                manifestResources: null,
                _options,
                debugEntryPoint: null,
                sourceLinkStream: null,
                embeddedTexts: null,
                testData: null,
                cancellationToken: default);

            bool success = false;

            success = _comp.CompileMethods(
                _moduleBeingBuilt,
                emittingPdb: embedPdb,
                emitMetadataOnly: _options.EmitMetadataOnly,
                emitTestCoverageData: _options.EmitTestCoverageData,
                diagnostics: diagnostics,
                filterOpt: null,
                cancellationToken: default);

            _comp.GenerateResourcesAndDocumentationComments(
                _moduleBeingBuilt,
                xmlDocumentationStream: null,
                win32ResourcesStream: null,
                _options.OutputNameOverride,
                diagnostics,
                cancellationToken: default);

            _comp.ReportUnusedImports(null, diagnostics, default);
            _moduleBeingBuilt.CompilationFinished();

            diagnostics.Free();
        }

        [Benchmark]
        public object SerializeMetadata()
        {
            _peStream.Position = 0;
            var diagnostics = DiagnosticBag.GetInstance();

            _comp.SerializeToPeStream(
                _moduleBeingBuilt,
                new SimpleEmitStreamProvider(_peStream),
                metadataPEStreamProvider: null,
                pdbStreamProvider: null,
                testSymWriterFactory: null,
                diagnostics,
                metadataOnly: _options.EmitMetadataOnly,
                includePrivateMembers: _options.IncludePrivateMembers,
                emitTestCoverageData: _options.EmitTestCoverageData,
                pePdbFilePath: _options.PdbFilePath,
                privateKeyOpt: null,
                cancellationToken: default);

            diagnostics.Free();

            return _peStream;
        }
    }
}
