// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Toolchains.DotNetCli;

namespace BenchmarkDotNet.Extensions;

public sealed record MonoAotLLVMSettings : DotNetCliSettings
{
    public required string CustomRuntimePack { get; init; }
    public required string AotCompilerPath { get; init; }
    public required MonoAotCompilerMode AotCompilerMode { get; init; }
}
