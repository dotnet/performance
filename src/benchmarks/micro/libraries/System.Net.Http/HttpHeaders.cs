// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using MicroBenchmarks;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;

namespace System.Net.Http.Tests
{
    [BenchmarkCategory(Categories.Libraries)]
    public class HttpHeaders
    {
        private readonly HttpResponseHeaders _responseHeaders = new HttpResponseMessage().Headers;

        [Benchmark]
        [ArgumentsSource(nameof(IEnumerableArgument))]
        public bool TryAddWithoutValidationTests(TryAddWithoutValidationTestData data)
        {
            var result = _responseHeaders.TryAddWithoutValidation("headerName", data.Values);
            _responseHeaders.Clear();
            return result;
        }

        public IEnumerable<TryAddWithoutValidationTestData> IEnumerableArgument()
        {
            yield return new TryAddWithoutValidationTestData("Array", Enumerable.Range(0, 5).Select(i => "value" + i).ToArray()); // tests IList optimized case
            yield return new TryAddWithoutValidationTestData("List", Enumerable.Range(0, 5).Select(i => "value" + i).ToList()); // tests IList optimized case
            yield return new TryAddWithoutValidationTestData("Hashset", new HashSet<string>(Enumerable.Range(0, 5).Select(i => "value" + i))); // tests the slow path IEnumerable case
        }
    }

    public class TryAddWithoutValidationTestData
    {
        public IEnumerable<string> Values { get; }

        public string InstanceName { get; }

        public TryAddWithoutValidationTestData(string instanceName, IEnumerable<string> values)
        {
            InstanceName = instanceName;
            Values = values;
        }

        public override string ToString() => InstanceName;
    }
}