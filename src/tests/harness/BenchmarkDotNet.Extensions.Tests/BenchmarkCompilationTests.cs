// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Reflection;
using BenchmarkDotNet.Attributes;
using Xunit;

namespace Tests
{
    public class BenchmarkCompilationTests
    {
        [Theory]
        [InlineData(nameof(System.Tests.Perf_Enum.InterpolateIntoSpan_NonFlags))]
        [InlineData(nameof(System.Tests.Perf_Enum.InterpolateIntoStringBuilder_NonFlags))]
        public void EnumInterpolationArgumentsPreserveHistoricalBindings(string methodName)
        {
            var method = typeof(System.Tests.Perf_Enum).GetMethod(methodName)!;
            Assert.Equal(typeof(System.Tests.Perf_Enum.Colors), Assert.Single(method.GetParameters()).ParameterType);

            var arguments = method.GetCustomAttributes<ArgumentsAttribute>()
                .Select(argument => Assert.Single(argument.Values))
                .ToArray();

            Assert.IsType<System.Tests.Perf_Enum.Colors>(arguments[0]);
            Assert.Equal(new[] { 6, 42 }, arguments.Select(System.Convert.ToInt32));
        }

        [Fact]
        public void BenchmarkAssemblyHasBeenWoven()
        {
            var methods = typeof(System.Tests.Perf_Int32).Assembly.GetTypes()
                .SelectMany(type => type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
                .Where(method => method.IsDefined(typeof(BenchmarkAttribute)))
                .ToArray();

            Assert.NotEmpty(methods);
            Assert.All(methods, method => Assert.True(method.MethodImplementationFlags.HasFlag(MethodImplAttributes.NoInlining),
                $"{method.DeclaringType!.FullName}.{method.Name} has not been woven."));
        }
    }
}
