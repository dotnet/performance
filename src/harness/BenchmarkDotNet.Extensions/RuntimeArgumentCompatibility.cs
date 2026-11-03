// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Versioning;
using BenchmarkDotNet.ConsoleArguments;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Loggers;

namespace BenchmarkDotNet.Extensions
{
    public static class RuntimeArgumentCompatibility
    {
        public static MonoAotLLVMToolChain? ParseMonoAotLLVM(List<string> arguments, string? targetFrameworkName)
        {
            if (ValidateNativeResponseFiles(arguments))
                return null;

            var args = ExpandRuntimeSelectors(arguments);
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

            ValidateAndRemoveInProcessOptions(args);
            runtimeOption = args.FindIndex(IsRuntimeOption);
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
            var toolchain = new MonoAotLLVMToolChain(new MonoAotLLVMRuntime(framework.Version), new()
            {
                TargetFrameworkMoniker = $"net{framework.Version.Major}.{framework.Version.Minor}",
                CliPath = cli is null ? null : new FileInfo(cli),
                PackagesPath = packages is null ? null : new DirectoryInfo(packages),
                UseFreshPackages = ParseFreshPackages(args),
                AotCompilerPath = Path.GetFullPath(compiler),
                CustomRuntimePack = Path.GetFullPath(pack),
                AotCompilerMode = mode
            });
            arguments.Clear();
            arguments.AddRange(args);
            return toolchain;
        }

        public static List<string> Normalize(IEnumerable<string> arguments)
        {
            var original = new List<string>(arguments);
            if (ValidateNativeResponseFiles(original))
                return original;

            var args = ExpandRuntimeSelectors(original);
            string? flavor = RemoveOption(args, "--wasmRuntimeFlavor");
            string? aotMode = RemoveOption(args, "--aotcompilermode");
            bool changed = flavor is not null || aotMode is not null;

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

                    changed |= args[i] != moniker;
                    args[i] = moniker;
                }
            }

            if (!foundWasm && (flavor is not null || aotMode is not null))
            {
                throw new ArgumentException("--wasmRuntimeFlavor and --aotcompilermode wasm require an explicit WebAssembly runtime.");
            }

            return changed ? args : original;
        }

        private static bool IsRuntimeOption(string argument)
            => argument.Equals("--runtimes", StringComparison.OrdinalIgnoreCase)
                || argument.Equals("-r", StringComparison.OrdinalIgnoreCase);

        private static bool ParseFreshPackages(List<string> args)
        {
            var flags = new List<string>();
            foreach (string argument in args)
            {
                if (argument == "--")
                    break;
                if (argument.Equals("--freshPackages", StringComparison.OrdinalIgnoreCase)
                    || argument.StartsWith("--freshPackages=", StringComparison.OrdinalIgnoreCase))
                    flags.Add(argument);
            }
            if (flags.Count == 0)
                return false;

            var logger = new AccumulationLogger();
            var (success, _, options) = ConfigParser.Parse(flags.ToArray(), logger);
            if (!success || options is null)
                throw new ArgumentException(logger.GetLog());
            return options.UseFreshPackages;
        }

        private static List<string> ExpandRuntimeSelectors(IEnumerable<string> arguments)
        {
            var result = new List<string>();
            foreach (string argument in arguments)
            {
                if (argument.StartsWith("--runtimes=", StringComparison.OrdinalIgnoreCase))
                {
                    result.Add(argument.Substring(0, "--runtimes".Length));
                    result.Add(argument.Substring("--runtimes=".Length));
                }
                else
                    result.Add(argument);
            }
            return result;
        }

        private static bool ValidateNativeResponseFiles(List<string> args)
        {
            if (!args.Exists(argument => argument.StartsWith("@", StringComparison.Ordinal)))
                return false;

            // BDN's response expansion is private. Keep its tokenizer and native response files unchanged,
            // but reject files combined with legacy options rather than miss a hidden runtime override.
            var logger = new AccumulationLogger();
            if (!ConfigParser.Parse(args.ToArray(), logger).isSuccess)
            {
                throw new ArgumentException("Response files require native BenchmarkDotNet options and runtime names. "
                    + "Pass legacy WASM or monoaotllvm compatibility options inline without response files."
                    + Environment.NewLine + logger.GetLog());
            }
            return true;
        }

        private static void ValidateAndRemoveInProcessOptions(List<string> args)
        {
            var disabled = new List<(int Index, int Count)>();
            for (int i = 0; i < args.Count; i++)
            {
                int equals = args[i].IndexOf('=');
                string option = equals < 0 ? args[i] : args[i].Substring(0, equals);
                if (!option.Equals("--inProcess", StringComparison.OrdinalIgnoreCase)
                    && !option.Equals("-i", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string? value = equals >= 0 ? args[i].Substring(equals + 1)
                    : i + 1 < args.Count ? args[i + 1] : null;
                if (!bool.TryParse(value, out bool enabled) || enabled)
                {
                    throw new ArgumentException("monoaotllvm cannot run in process; omit --inProcess or explicitly set it to false.");
                }
                disabled.Add((i, equals >= 0 ? 1 : 2));
                if (equals < 0)
                    i++;
            }

            // BDN's non-nullable bool option is presence-based, even with an explicit false.
            for (int i = disabled.Count - 1; i >= 0; i--)
                args.RemoveRange(disabled[i].Index, disabled[i].Count);
        }

        private static string? RemoveOption(List<string> args, string option)
        {
            bool Matches(string argument) => argument.Equals(option, StringComparison.OrdinalIgnoreCase)
                || argument.StartsWith(option + "=", StringComparison.OrdinalIgnoreCase);
            int index = args.FindIndex(argument => Matches(argument));
            if (index < 0)
            {
                return null;
            }
            bool attached = args[index].StartsWith(option + "=", StringComparison.OrdinalIgnoreCase);
            if (!attached && (index + 1 >= args.Count || args[index + 1].StartsWith("-", StringComparison.Ordinal)))
            {
                throw new ArgumentException($"{option} requires a value.");
            }

            string value = attached ? args[index].Substring(option.Length + 1) : args[index + 1];
            if (value.Length == 0)
                throw new ArgumentException($"{option} requires a value.");
            args.RemoveRange(index, attached ? 1 : 2);
            if (args.Exists(argument => Matches(argument)))
            {
                throw new ArgumentException($"{option} must be specified only once.");
            }
            return value;
        }
    }
}
