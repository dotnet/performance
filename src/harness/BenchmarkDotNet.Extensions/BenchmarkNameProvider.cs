// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Running;

namespace BenchmarkDotNet.Extensions;

public static class BenchmarkNameProvider
{
    public static string GetBenchmarkName(BenchmarkCase benchmark)
    {
        var method = benchmark.Descriptor.WorkloadMethod;
        if (benchmark.Descriptor.Type.FullName == "System.Tests.Perf_Enum"
            && (method.Name is "InterpolateIntoSpan_NonFlags" or "InterpolateIntoStringBuilder_NonFlags")
            && benchmark.Parameters.Items.Count == 1)
        {
            var parameters = method.GetParameters();
            var argument = benchmark.Parameters.Items[0];
            if (parameters.Length == 1 && parameters[0].Name == "value"
                && parameters[0].ParameterType.FullName == "System.Tests.Perf_Enum+Colors"
                && argument.IsArgument && argument.Name == "value"
                && argument.Value is Enum value && value.GetType() == parameters[0].ParameterType
                && Convert.ToInt64(value) == 6)
            {
                // The old DayOfWeek attribute ran as Colors(6), but exported and partitioned as Saturday.
                return $"System.Tests.Perf_Enum.{method.Name}(value: Saturday)";
            }
        }

        return FullNameProvider.GetBenchmarkName(benchmark);
    }
}
