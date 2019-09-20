// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Emit;
using Roslyn.Utilities;
using static Microsoft.CodeAnalysis.Compilation;

namespace CompilerBenchmarks
{
    [BenchmarkCategory("Roslyn")]
    public class StageBenchmarks
    {
        private CSharpCompilation _comp;
        private CompilationWithAnalyzers _compWithAnalyzers;
        private CommonPEModuleBuilder _moduleBeingBuilt;
        private EmitOptions _options;
        private MemoryStream _peStream;

        [GlobalSetup(Targets = new[] {
             nameof(GetDiagnostics),
             nameof(GetDiagnosticsWithAnalyzers) })]
        public void LoadCompilation()
        {
            _comp = Helpers.CreateReproCompilation();
        }

        [IterationSetup(Target = nameof(GetDiagnostics))]
        public void LoadFreshCompilation()
        {
            var options = _comp.Options.WithConcurrentBuild(false);
            // Since we want to measure binding and symbol construction
            // cost it's important that we don't re-use the same compilation
            // as results will be cached
            _comp = CSharpCompilation.Create(
                _comp.AssemblyName,
                _comp.SyntaxTrees,
                _comp.References,
                options);
        }

        [IterationSetup(Target = nameof(GetDiagnosticsWithAnalyzers))]
        public void LoadFreshCompilationWithAnalyzers()
        {
            LoadFreshCompilation();
            _compWithAnalyzers = Helpers.CreateReproCompilationWithAnalyzers(
                _comp,
                Helpers.GetReproCommandLineArgs());
        }

        [Benchmark]
        public object GetDiagnostics()
        {
            return _comp.GetDiagnostics();
        }

        [Benchmark]
        public async Task<object> GetDiagnosticsWithAnalyzers()
        {
            return await _compWithAnalyzers.GetAllDiagnosticsAsync();
        }

        [GlobalSetup(Target = nameof(CompileMethodsAndEmit))]
        public void LoadCompilationAndGetDiagnostics()
        {
            LoadCompilation();
            _peStream = new MemoryStream();
            // Call GetDiagnostics to force declaration symbol binding to finish
            _ = _comp.GetDiagnostics();
        }

        [Benchmark]
        public object CompileMethodsAndEmit()
        {
            _peStream.Position = 0;
            return _comp.WithOptions(_comp.Options.WithConcurrentBuild(false)).Emit(_peStream);
        }

        [GlobalSetup(Target = nameof(SerializeMetadata))]
        public void CompileMethods()
        {
            LoadCompilationAndGetDiagnostics();

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
                xmlDocStream: null,
                win32Resources: null,
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
