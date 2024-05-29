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
    [BenchmarkCategory(Categories.Libraries, Categories.LINQ)]
    public class TryAddWithoutValidationTests
    {
        [Benchmark]
        [ArgumentsSource(nameof(IEnumerableArgument))]
        public bool AddHeaders(AddWithoutValidationTestData data) => new HttpResponseMessage().Headers.TryAddWithoutValidation("headerName", data.Values);

        public IEnumerable<AddWithoutValidationTestData> IEnumerableArgument()
        {
            yield return new AddWithoutValidationTestData("List", Enumerable.Range(0, 5).Select(i => "value" + i).ToList());
            yield return new AddWithoutValidationTestData("Array", Enumerable.Range(0, 5).Select(i => "value" + i).ToArray());
            yield return new AddWithoutValidationTestData("Hashset", new HashSet<string>(Enumerable.Range(0, 5).Select(i => "value" + i)));
        }
    }

    public class AddWithoutValidationTestData
    {
        public IEnumerable<string> Values { get; }

        public string InstanceName { get; }

        public AddWithoutValidationTestData(string instanceName, IEnumerable<string> values)
        {
            InstanceName = instanceName;
            Values = values;
        }

        public override string ToString() => InstanceName;
    }
}