// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace MicroBenchmarks
{
    public static class Categories
    {
        /// <summary>
        /// benchmarks belonging to this category are not going to be executed as part of our daily CI runs
        /// we are going to run them periodically to make sure we don't regress any of the most popular 3rd party libraries.
        /// </summary>
        public const string ThirdParty = "ThirdParty";
        
        /// <summary>
        /// benchmarks belonging to this category are executed for CI jobs
        /// </summary>
        public const string Libraries = "Libraries";
        
        /// <summary>
        /// benchmarks belonging to this category are executed for CI jobs
        /// </summary>
        public const string Runtime = "Runtime";
        public const string BenchmarksGame = "BenchmarksGame";
        public const string Benchstones = "Benchstones";
        public const string BenchF = "BenchF";
        public const string BenchI = "BenchI";
        public const string MDBenchF = "MDBenchF";
        public const string MDBenchI = "MDBenchI";
        public const string Inlining = "Inlining";
        public const string V8 = "V8";
        public const string Perflab = "Perflab";
        public const string Virtual = "Virtual";
        public const string ByteMark = "ByteMark";
        public const string Burgers = "Burgers";
        public const string SciMark = "SciMark";
        public const string CscBench = "CscBench";
        public const string NoWASM = "NoWASM";

        public const string JIT = "JIT";
        public const string JSON = "JSON";
        public const string LINQ = "LINQ";
        public const string Reflection = "Reflection";
        public const string SIMD = "SIMD";
        public const string Span = "Span";
        public const string Collections = "Collections";
        public const string GenericCollections = "GenericCollections";
        public const string NonGenericCollections = "NonGenericCollections";
        public const string NoInterpreter = "NoInterpreter";
        public const string NoMono = "NoMono";
        public const string NoAOT = "NoAOT";
        public const string Regex = "Regex";
        public const string Sve = "Sve";
    }
}
