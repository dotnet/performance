// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Filters;

namespace BenchmarkDotNet.Attributes.Filters
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public sealed class AotFilterAttribute : FilterConfigBaseAttribute
    {
        public AotFilterAttribute(string reason)
            : base(new SimpleFilter(benchmark =>
                benchmark.GetRuntime() is not (NativeAotRuntime or MonoAotRuntime or MonoWasmAotRuntime or MonoAotLLVMRuntime)))
        {
        }
    }
}
