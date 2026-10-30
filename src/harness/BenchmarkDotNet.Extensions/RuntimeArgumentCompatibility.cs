// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Versioning;
using BenchmarkDotNet.Environments;

namespace BenchmarkDotNet.Extensions
{
    public static class RuntimeArgumentCompatibility
    {
        public static MonoAotLLVMToolChain? ParseMonoAotLLVM(List<string> args, string? targetFrameworkName)
        {
            int runtimeOption = -1;
            for (int i = 0; i < args.Count; i++)
            {
                if (!IsRuntimeOption(args[i]))
                {
                    continue;
                }

                int first = i + 1;
                while (i + 1 < args.Count && !args[i + 1].StartsWith("-", StringComparison.Ordinal))
                {
                    if (args[++i].Equals("monoaotllvm", StringComparison.OrdinalIgnoreCase))
                    {
                        if (runtimeOption >= 0)
                        {
                            throw new ArgumentException("Specify monoaotllvm only once.");
                        }
                        runtimeOption = first - 1;
                    }
                }
            }
            if (runtimeOption < 0)
            {
                return null;
            }

            if (args.FindAll(IsRuntimeOption).Count != 1
                || runtimeOption + 2 < args.Count && !args[runtimeOption + 2].StartsWith("-", StringComparison.Ordinal)
                || !args[runtimeOption + 1].Equals("monoaotllvm", StringComparison.OrdinalIgnoreCase)
                || args.Exists(argument => argument.Equals("--corerun", StringComparison.OrdinalIgnoreCase)
                    || argument.StartsWith("--corerun=", StringComparison.OrdinalIgnoreCase)))
            {
                throw new ArgumentException("Run monoaotllvm separately from other runtimes so no requested job is replaced.");
            }

            args.RemoveRange(runtimeOption, 2);
            string compiler = RemoveOption(args, "--aotcompilerpath")
                ?? throw new ArgumentException("--aotcompilerpath is required for monoaotllvm.");
            string pack = RemoveOption(args, "--customruntimepack")
                ?? throw new ArgumentException("--customruntimepack is required for monoaotllvm.");
            string? modeValue = RemoveOption(args, "--aotcompilermode");
            if (!Enum.TryParse(modeValue ?? "mini", ignoreCase: true, out MonoAotCompilerMode mode)
                || !Enum.IsDefined(mode))
            {
                throw new ArgumentException("--aotcompilermode must be mini or llvm for monoaotllvm.");
            }
            if (string.IsNullOrEmpty(targetFrameworkName))
            {
                throw new ArgumentException("The benchmark assembly must declare its target framework for monoaotllvm.");
            }
            var framework = new FrameworkName(targetFrameworkName);
            if (framework.Identifier != ".NETCoreApp")
            {
                throw new ArgumentException("monoaotllvm requires a .NETCoreApp benchmark target framework.");
            }

            string? cli = RemoveOption(new List<string>(args), "--cli");
            string? packages = RemoveOption(new List<string>(args), "--packages");
            return new MonoAotLLVMToolChain(new MonoAotLLVMRuntime(framework.Version), new()
            {
                TargetFrameworkMoniker = $"net{framework.Version.Major}.{framework.Version.Minor}",
                CliPath = cli is null ? null : new FileInfo(cli),
                PackagesPath = packages is null ? null : new DirectoryInfo(packages),
                AotCompilerPath = Path.GetFullPath(compiler),
                CustomRuntimePack = Path.GetFullPath(pack),
                AotCompilerMode = mode
            });
        }

        public static List<string> Normalize(IEnumerable<string> arguments)
        {
            var args = new List<string>(arguments);
            string? flavor = RemoveOption(args, "--wasmRuntimeFlavor");
            string? aotMode = RemoveOption(args, "--aotcompilermode");

            if (flavor is not null && flavor is not ("Mono" or "CoreCLR"))
            {
                throw new ArgumentException("--wasmRuntimeFlavor must be Mono or CoreCLR.");
            }

            if (aotMode is not null && aotMode != "wasm")
            {
                throw new ArgumentException(
                    $"--aotcompilermode {aotMode} requires the native Mono AOT+LLVM toolchain with --runtimes monoaotllvm.");
            }

            bool foundWasm = false;
            for (int i = 0; i < args.Count; i++)
            {
                if (!IsRuntimeOption(args[i]))
                {
                    continue;
                }

                while (i + 1 < args.Count && !args[i + 1].StartsWith("-", StringComparison.Ordinal))
                {
                    string moniker = args[++i];
                    if (moniker.Equals("monoaotllvm", StringComparison.OrdinalIgnoreCase))
                    {
                        throw new ArgumentException(
                            "Native Mono AOT+LLVM options must be parsed before normalizing WebAssembly options.");
                    }

                    bool legacyWasm = moniker.Equals("wasm", StringComparison.OrdinalIgnoreCase)
                        || moniker.StartsWith("wasmnet", StringComparison.OrdinalIgnoreCase);
                    if (legacyWasm)
                    {
                        string version = moniker.Equals("wasm", StringComparison.OrdinalIgnoreCase) ? "6.0" : moniker.Substring(7);
                        moniker = (flavor == "CoreCLR" ? "corewasm" : "monowasm") + version;
                    }

                    if (!Runtime.TryParse(moniker, out var runtime) || runtime is not WasmRuntime)
                    {
                        // Leave unrelated runtime spellings and their validation to BenchmarkDotNet.
                        continue;
                    }

                    foundWasm = true;
                    bool coreClr = runtime is CoreWasmRuntime;
                    if (flavor is not null && (flavor == "CoreCLR") != coreClr)
                    {
                        throw new ArgumentException($"--wasmRuntimeFlavor {flavor} conflicts with runtime {moniker}.");
                    }
                    if (aotMode == "wasm")
                    {
                        if (coreClr)
                        {
                            throw new ArgumentException("--aotcompilermode wasm requires Mono WebAssembly, not CoreCLR.");
                        }
                        moniker = $"monowasmaot{runtime.Version}";
                    }

                    args[i] = moniker;
                }
            }

            if (!foundWasm && (flavor is not null || aotMode is not null))
            {
                throw new ArgumentException("--wasmRuntimeFlavor and --aotcompilermode wasm require an explicit WebAssembly runtime.");
            }

            return args;
        }

        private static bool IsRuntimeOption(string argument)
            => argument.Equals("--runtimes", StringComparison.OrdinalIgnoreCase) || argument == "-r";

        private static string? RemoveOption(List<string> args, string option)
        {
            int index = args.FindIndex(argument => argument.Equals(option, StringComparison.OrdinalIgnoreCase));
            if (index < 0)
            {
                return null;
            }
            if (index + 1 >= args.Count || args[index + 1].StartsWith("-", StringComparison.Ordinal))
            {
                throw new ArgumentException($"{option} requires a value.");
            }

            string value = args[index + 1];
            args.RemoveRange(index, 2);
            if (args.Exists(argument => argument.Equals(option, StringComparison.OrdinalIgnoreCase)))
            {
                throw new ArgumentException($"{option} must be specified only once.");
            }
            return value;
        }
    }
}
