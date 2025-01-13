// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.IO;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Emit;
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

        [Benchmark]
        public ImmutableArray<Diagnostic> GetDiagnostics() => _comp.GetDiagnostics();

        [IterationSetup(Target = nameof(GetDiagnosticsWithAnalyzers))]
        public void LoadFreshCompilationWithAnalyzers()
        {
            LoadFreshCompilation();
            _compWithAnalyzers = Helpers.CreateReproCompilationWithAnalyzers(
                _comp,
                Helpers.GetReproCommandLineArgs());
        }

        [Benchmark]
        public Task<ImmutableArray<Diagnostic>> GetDiagnosticsWithAnalyzers()
            => _compWithAnalyzers.GetAllDiagnosticsAsync();

        [GlobalSetup(Target = nameof(CompileMethodsAndEmit))]
        public void LoadCompilationAndGetDiagnostics()
        {
            LoadCompilation();
            _peStream = new MemoryStream();
            // Call GetDiagnostics to force declaration symbol binding to finish
            _ = _comp.GetDiagnostics();
        }

        [Benchmark]
        public EmitResult CompileMethodsAndEmit()
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
                diagnostics: diagnostics,
                filterOpt: null,
                cancellationToken: default);

            if (!success)
            {
                throw new InvalidOperationException("Did not successfully compile methods");
            }

            _comp.GenerateResources(_moduleBeingBuilt, win32Resources: null, useRawWin32Resources: false, diagnostics, cancellationToken: default);
            _comp.GenerateDocumentationComments(xmlDocStream: null, _options.OutputNameOverride, diagnostics, cancellationToken: default);

            _comp.ReportUnusedImports(diagnostics, default);
            _moduleBeingBuilt.CompilationFinished();

            diagnostics.Free();
        }

        [Benchmark]
        public Stream SerializeMetadata()
        {
            _peStream.Position = 0;
            var diagnostics = DiagnosticBag.GetInstance();

            _comp.SerializeToPeStream(
                _moduleBeingBuilt,
                new SimpleEmitStreamProvider(_peStream),
                metadataPEStreamProvider: null,
                pdbStreamProvider: null,
                rebuildData: null,
                testSymWriterFactory: null,
                diagnostics,
                _options,
                privateKeyOpt: null,
                cancellationToken: default);

            diagnostics.Free();

            return _peStream;
        }
    }
}
