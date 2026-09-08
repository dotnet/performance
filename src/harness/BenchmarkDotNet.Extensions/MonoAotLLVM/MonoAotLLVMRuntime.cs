// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains;

namespace BenchmarkDotNet.Extensions;

public sealed class MonoAotLLVMRuntime(Version version) : Runtime
{
    public override string Name => "MonoAOTLLVM";

    public override Version Version { get; } = version;

    public override IToolchain GetDefaultToolchain(BenchmarkCase benchmarkCase)
        => throw new NotSupportedException("MonoAOTLLVM benchmarks must set the toolchain explicitly.");
}
