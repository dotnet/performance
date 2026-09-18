// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Toolchains;

namespace BenchmarkDotNet.Extensions;

public class MonoAotLLVMToolChain(MonoAotLLVMRuntime runtime, MonoAotLLVMSettings settings)
    : Toolchain("MonoAotLLVM", runtime, new MonoAotLLVMBuilder(settings), Toolchains.Executor.Instance)
{
}
