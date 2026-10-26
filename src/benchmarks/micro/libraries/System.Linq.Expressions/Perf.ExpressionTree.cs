// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace System.Linq.Expressions.Tests
{
    [BenchmarkCategory(Categories.Libraries, Categories.LINQ)]
    public class Perf_ExpressionTree
    {
        // The shape EnumerableQuery and ORM test doubles compile for every query: Queryable.* calls over
        // an IQueryable captured in a closure. Compile() emits a DynamicMethod that is jitted in FullOpts,
        // so this measures JIT time of dynamic methods (see dotnet/runtime#127203).
        private Expression<Func<Row>> _queryableWhere;

        [GlobalSetup]
        public void Setup() => _queryableWhere = BuildQueryableWhere(new List<Row>().AsQueryable(), 7);

        [Benchmark]
        [BenchmarkCategory(Categories.NoAOT)]
        public Func<Row> Compile_QueryableWhere() => _queryableWhere.Compile();

        [Benchmark]
        public Func<Row> CompileInterpreted_QueryableWhere() => _queryableWhere.Compile(preferInterpretation: true);

        private static Expression<Func<Row>> BuildQueryableWhere(IQueryable<Row> source, int owner)
            => () => source.Where(r => r.OwnerId == owner).SingleOrDefault(r => r.Id == owner);

        public class Row
        {
            public int Id { get; set; }
            public int OwnerId { get; set; }
            public string Name { get; set; }
        }
    }
}
