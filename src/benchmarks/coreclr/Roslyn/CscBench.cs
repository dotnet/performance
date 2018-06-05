// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;

public class CscBench
{
    public const int CompileIterations = 1500;
    public const int DataflowIterations = 10000;

    public static string MscorlibPath;

    private static string GetLoadedAssemblyLocation(string fileName)
    {
        return Assembly.GetEntryAssembly().GetReferencedAssemblies()
            .Select(assemblyName => Assembly.Load(assemblyName))
            .SingleOrDefault(@assembly => {
                return @assembly.Location.EndsWith(fileName, StringComparison.OrdinalIgnoreCase);
            })?.Location;
    }

    [GlobalSetup]
    public void SetMscorlib()
    {
        var runtimeDirectory = new FileInfo(GetLoadedAssemblyLocation("System.Runtime.dll"))
            .DirectoryName;

        // Some CoreCLR packages have System.Private.CoreLib.ni.dll only.
        var nicorlib = Path.Combine(runtimeDirectory, "System.Private.CoreLib.ni.dll");
        if (File.Exists(nicorlib))
        {
            MscorlibPath = nicorlib;
            return;
        }

        var corlib = Path.Combine(runtimeDirectory, "System.Private.CoreLib.dll");
        if (!File.Exists(corlib))
            throw new FileNotFoundException("System.Private.CoreLib is not present with the System.Runtime");

        MscorlibPath = corlib;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool CompileBench()
    {
        var expression = "6 * 7";
        var text = @"public class Calculator { public static object Evaluate() { return $; } }".Replace("$", expression);
        var tree = SyntaxFactory.ParseSyntaxTree(text);
        var compilation = CSharpCompilation.Create(
            "calc.dll",
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary),
            syntaxTrees: new[] { tree },
            references: new[] { MetadataReference.CreateFromFile(MscorlibPath) });

        bool result = true;
        for (int i = 0; i < CompileIterations; i++)
        {
            using (var stream = new MemoryStream())
            {
                var emitResult = compilation.Emit(stream);
                result &= emitResult.Success;
            }
        }

        return result;
    }

    [Benchmark]
    public bool CompileTest() => CompileBench();

    public static TextSpan GetSpanBetweenMarkers(SyntaxTree tree)
    {
        SyntaxTrivia startComment = tree
            .GetRoot()
            .DescendantTrivia()
            .First(syntaxTrivia => syntaxTrivia.ToString().Contains("start"));
        SyntaxTrivia endComment = tree
            .GetRoot()
            .DescendantTrivia()
            .First(syntaxTrivia => syntaxTrivia.ToString().Contains("end"));
        TextSpan textSpan = TextSpan.FromBounds(
            startComment.FullSpan.End,
            endComment.FullSpan.Start);
        return textSpan;
    }

    public static void GetStatementsBetweenMarkers(SyntaxTree tree, out StatementSyntax firstStatement, out StatementSyntax lastStatement)
    {
        TextSpan span = GetSpanBetweenMarkers(tree);
        var statementsInside = tree
            .GetRoot()
            .DescendantNodes(span)
            .OfType<StatementSyntax>()
            .Where(s => span.Contains(s.Span));
        firstStatement = statementsInside.First();
        var first = firstStatement;
        lastStatement = statementsInside
            .Where(s => s.Parent == first.Parent)
            .Last();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool DataflowBench()
    {
        var text = @"
class C {
    public void F(int x)
    {
        int a;
/*start*/
        int b;
        int x, y = 1;
        { var z = ""a""; }
/*end*/
        int c;
    }
}";
        var tree = SyntaxFactory.ParseSyntaxTree(text);
        var compilation = CSharpCompilation.Create(
            "calc.dll",
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary),
            syntaxTrees: new[] { tree },
            references: new[] { MetadataReference.CreateFromFile(MscorlibPath) });
        var semanticModel = compilation.GetSemanticModel(tree);

        bool result = true;
        for (int i = 0; i < DataflowIterations; i++)
        {
            StatementSyntax firstStatement, lastStatement;
            GetStatementsBetweenMarkers(tree, out firstStatement, out lastStatement);
            DataFlowAnalysis regionDataFlowAnalysis = semanticModel.AnalyzeDataFlow(firstStatement, lastStatement);
            string declaredVars = string.Join(",", regionDataFlowAnalysis
                .VariablesDeclared
                .Select(symbol => symbol.Name));

            result &= "b,x,y,z".Equals(declaredVars);
        }

        return result;
    }

    [Benchmark]
    public bool DatflowTest() => DataflowBench();
}
