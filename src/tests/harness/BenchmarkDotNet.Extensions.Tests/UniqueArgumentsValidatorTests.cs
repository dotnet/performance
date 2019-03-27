// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Validators;
using Xunit;

namespace Tests
{
    public class UniqueArgumentsValidatorTests
    {
        [Theory]
        [InlineData(typeof(WithDuplicateValueTypeArguments), true)]
        [InlineData(typeof(WithDuplicateReferenceTypeArguments), true)]
        [InlineData(typeof(WithoutDuplicatedValueTypeArguments), false)]
        [InlineData(typeof(WithoutDuplicatedReferenceTypeArguments), false)]
        public void DuplicatedArgumentsAreDetected(Type typeWithBenchmarks, bool shouldReportError)
        {
            var benchmarksForType = BenchmarkConverter.TypeToBenchmarks(typeWithBenchmarks);
            var validationParameters = new ValidationParameters(benchmarksForType.BenchmarksCases, benchmarksForType.Config);

            var validationErrors = new UniqueArgumentsValidator().Validate(validationParameters);

            if (shouldReportError)
                Assert.NotEmpty(validationErrors);
            else
                Assert.Empty(validationErrors);
        }

        public class WithDuplicateValueTypeArguments
        {
            public static IEnumerable<object> Values => new object[]
            {
                ushort.MinValue, // 0
                (ushort)0, // ushort.MinValue
                ushort.MaxValue
            };

            [Benchmark]
            [ArgumentsSource(nameof(Values))]
            public string ToString(ushort value) => value.ToString();
        }

        public class WithDuplicateReferenceTypeArguments
        {
            public static IEnumerable<object> Values => new object[]
            {
                "",
                string.Empty,
                "something"
            };

            [Benchmark]
            [ArgumentsSource(nameof(Values))]
            public int GetHashCode(string value) => value.GetHashCode();
        }

        public class WithoutDuplicatedValueTypeArguments
        {
            public static IEnumerable<object> Values => new object[]
            {
                ushort.MinValue,
                ushort.MaxValue
            };

            [Benchmark]
            [ArgumentsSource(nameof(Values))]
            public string ToString(ushort value) => value.ToString();
        }

        public class WithoutDuplicatedReferenceTypeArguments
        {
            public static IEnumerable<object> Values => new object[]
            {
                "",
                "something"
            };

            [Benchmark]
            [ArgumentsSource(nameof(Values))]
            public int GetHashCode(string value) => value.GetHashCode();
        }
    }
}
