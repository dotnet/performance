// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Filters;

namespace BenchmarkDotNet.Attributes.Filters;

public class AotFilterAttribute : FilterConfigBaseAttribute
{
    public AotFilterAttribute(string? reason = null)
        : base(new SimpleFilter(benchmark => benchmark.GetRuntime() is not (NativeAotRuntime or MonoAotLLVMRuntime)))
    {
    }
}
