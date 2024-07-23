# Benchmarking workflow for [dotnet/runtime](https://github.com/dotnet/runtime) repository {#top}

## Table of Contents

- [Benchmarking workflow for dotnet/runtime repository {#top}](#benchmarking-workflow-for-dotnetruntime-repository-top)
  - [Table of Contents](#table-of-contents)
  - [Introduction](#introduction)
    - [Code Organization](#code-organization)
    - [dotnet runtime Prerequisites for CLR](#dotnet-runtime-prerequisites-for-clr)
    - [dotnet runtime testing for wasm](#dotnet-runtime-testing-for-wasm)
      - [Install v8 engine](#install-v8-engine)
      - [Run the benchmarks with the interpreter](#run-the-benchmarks-with-the-interpreter)
      - [Run the benchmarks with AOT](#run-the-benchmarks-with-aot)
      - [Note about "file ... being used by another process" error](#note-about-file--being-used-by-another-process-error)
    - [dotnet runtime testing for MonoAOT](#dotnet-runtime-testing-for-monoaot)
      - [Prerequisites (Files either built locally (with build.(sh/cmd) or downloaded from payload above (if same system setup) (in this order))](#prerequisites-files-either-built-locally-with-buildshcmd-or-downloaded-from-payload-above-if-same-system-setup-in-this-order)
      - [Running on Linux](#running-on-linux)
      - [Running on Windows](#running-on-windows)
    - [dotnet runtime testing for MonoInterpreter](#dotnet-runtime-testing-for-monointerpreter)
      - [Prerequisites (Build files either built locally or downloaded from payload above)](#prerequisites-build-files-either-built-locally-or-downloaded-from-payload-above)
      - [Running on Linux](#running-on-linux-1)
      - [Running on Windows](#running-on-windows-1)
    - [dotnet runtime testing for Mono Default (JIT)](#dotnet-runtime-testing-for-mono-default-jit)
      - [Prerequisites (Build files either built locally or downloaded from payload above)](#prerequisites-build-files-either-built-locally-or-downloaded-from-payload-above-1)
      - [Running on Linux](#running-on-linux-2)
      - [Running on Windows](#running-on-windows-2)
  - [Preventing Regressions](#preventing-regressions)
    - [Running against the latest .NET Core SDK](#running-against-the-latest-net-core-sdk)
  - [Solving Regressions](#solving-regressions)
    - [Repro Case](#repro-case)
    - [Profiling](#profiling)
    - [Running against Older Versions](#running-against-older-versions)
    - [Confirmation](#confirmation)
  - [Benchmarking new API](#benchmarking-new-api)
    - [Reference](#reference)
    - [PR](#pr)

## Introduction

This repository is **independent of the [dotnet/runtime](https://github.com/dotnet/runtime) build system.** All you need to get the benchmarks running is to download the dotnet SDKs and cli or use the python script which can do it for you. Please see [Prerequisites](./prerequisites.md) for more.

If you are not familiar with BenchmarkDotNet or this repository you should read the [Microbenchmarks Guide](../src/benchmarks/micro/README.md) first. It's really short and concise, we really encourage you to read it.

To learn more about designing benchmarks, please read [Microbenchmark Design Guidelines](./microbenchmark-design-guidelines.md).

### Code Organization

All Libraries benchmarks which have been ported from the old CoreFX repository belong to the corresponding folders: `libraries\$namespace`. The Runtime (old CoreCLR) benchmarks can be found in `runtime` subfolder. The directory structure is the following (some folders have been omitted for brevity):

```log
PS C:\Projects\performance\src\benchmarks\micro> tree
├───libraries
│   ├───System
│   ├───System.Collections
│   ├───System.ComponentModel.TypeConverter
│   ├───System.Console
│   ├───System.Diagnostics
│   ├───System.Globalization
│   ├───System.IO.Compression
│   ├───System.IO.FileSystem
│   ├───System.IO.MemoryMappedFiles
│   ├───System.IO.Pipes
│   ├───System.Linq
│   ├───System.Memory
│   ├───System.Net.Http
│   ├───System.Net.Primitives
│   ├───System.Net.Sockets
│   ├───System.Numerics.Vectors
│   ├───System.Runtime
│   ├───System.Runtime.Extensions
│   ├───System.Runtime.Numerics
│   ├───System.Runtime.Serialization.Formatters
│   ├───System.Security.Cryptography
│   ├───System.Security.Cryptography.Primitives
│   ├───System.Text.Encoding
│   ├───System.Text.RegularExpressions
│   ├───System.Threading
│   ├───System.Threading.Channels
│   ├───System.Threading.Tasks
│   ├───System.Threading.Tasks.Extensions
│   ├───System.Threading.ThreadPool
│   ├───System.Threading.Timers
│   └───System.Xml.XmlDocument
├───runtime
│   ├───BenchmarksGame
│   │   └───Inputs
│   ├───Benchstones
│   │   ├───BenchF
│   │   └───BenchI
│   ├───BilinearInterpol
│   ├───Burgers
│   ├───Bytemark
│   ├───Devirtualization
│   ├───Exceptions
│   ├───FractalPerf
│   ├───Inlining
│   ├───Layout
│   ├───Linq
│   ├───Lowering
│   ├───Math
│   │   └───Functions
│   │       ├───Double
│   │       └───Single
│   ├───PacketTracer
│   ├───perflab
│   ├───Roslyn
│   ├───SciMark
│   ├───SIMD
│   ├───Span
│   ├───System.Reflection
│   └───V8
```

During the port from xunit-performance to BenchmarkDotNet, the namespaces, type and methods names were not changed. The exception to this rule are all `System.Collections` ([#92](https://github.com/dotnet/performance/pull/92)) and `Span<T>` ([#94](https://github.com/dotnet/performance/pull/94)) benchmarks which got rewritten to utilize the full capabilities of BenchmarkDotNet.

Please remember that you can filter the benchmarks using a glob pattern applied to namespace.typeName.methodName ([read more](./benchmarkdotnet.md#Filtering-the-Benchmarks)):

```cmd
dotnet run -c Release -f net9.0 --filter System.Memory*
```

(Run the above command on `src/benchmarks/micro/MicroBenchmarks.csproj`.)

Moreover, every Libaries benchmark belongs to a [Libraries category](../src/benchmarks/micro/README.md#Categories). Same goes for Runtime.

### dotnet runtime Prerequisites for CLR

In order to run the benchmarks against local [dotnet/runtime](https://github.com/dotnet/runtime) build you need to build the dotnet/runtime repository in **Release**:

```cmd
C:\Projects\runtime> build -c Release
```

**The most important build artifact for us is CoreRun**. CoreRun is a simple host that does NOT take any dependency on NuGet. BenchmarkDotNet generates some boilerplate code, builds it using dotnet cli and tells CoreRun.exe to run the benchmarks from the auto-generated library. CoreRun runs the benchmarks using the libraries that are placed in its folder. When a benchmarked code has a dependency to `System.ABC.dll` version 4.5 and CoreRun has `System.ABC.dll` version 4.5.1 in its folder, then CoreRun is going to load and use `System.ABC.dll` version 4.5.1. **This means that with a single clone of this dotnet/performance repository you can run benchmarks against private builds of [dotnet/runtime](https://github.com/dotnet/runtime) from many different locations.**

Every time you want to run the benchmarks against local build of [dotnet/runtime](https://github.com/dotnet/runtime) you need to provide the path to CoreRun:

```cmd
dotnet run -c Release -f net9.0 --filter $someFilter \
    --coreRun C:\Projects\runtime\artifacts\bin\testhost\net9.0-windows-Release-x64\shared\Microsoft.NETCore.App\9.0.0\CoreRun.exe
```

**Note:** BenchmarkDotNet expects a path to `CoreRun.exe` file (`corerun` on Unix), not to `Core_Root` folder.

Once you rebuild the part of [dotnet/runtime](https://github.com/dotnet/runtime) you are working on, the appropriate `.dll` gets updated and the next time you run the benchmarks, CoreRun is going to load the updated library.

```cmd
C:\Projects\runtime\src\libraries\System.Text.RegularExpressions\src> dotnet msbuild /p:Configuration=Release
```

**Note:** the exception to this rule are libraries that **are not part of the shared SDK**. The `build` script of the runtime repo does not copy them to the CoreRun folder so you need to do it on your own:

```cmd
cp artifacts\bin\runtime\net9.0-Windows_NT-Release-x64\Microsoft.Extensions.Caching.Memory.dll artifacts\bin\testhost\net9.0-windows-Release-x64\shared\Microsoft.NETCore.App\9.0.0\
```

Of course only if you want to benchmark these specific libraries. If you don't, the default versions defined in [MicroBenchmarks.csproj](../src/benchmarks/micro/MicroBenchmarks.csproj) project file are going to get used.

### dotnet runtime testing for wasm

In order to run the benchmarks against local [dotnet/runtime](https://github.com/dotnet/runtime) build:

1. build the dotnet/runtime repository in **Release**

```cmd
/path/to/dotnet/runtime$ ./build.sh mono+libs -os browser -c Release
```

2. Prepare a sdk with `wasm-tools` workload installed using the built artifacts

```cmd
/path/to/dotnet/runtime$ ./dotnet.sh build -p:TargetOS=browser -p:TargetArchitecture=wasm -c Release src/mono/wasm/Wasm.Build.Tests /t:InstallWorkloadUsingArtifacts
```

This would produce `/path/to/dotnet/runtime/artifacts/bin/dotnet-latest`, which should be used to run the benchmarks.

3. And you need `/path/to/dotnet/runtime/src/mono/browser/test-main.mjs`

#### Install v8 engine

Make sure you have the v8 engine installed and in the PATH. Follow the installation [instructions](https://github.com/dotnet/runtime/tree/main/src/mono/browser#installation-of-javascript-engines) if you don't have v8 installed.

#### Run the benchmarks with the interpreter

1. Configure NuGet Feeds to Include Local Packages. Open the `NuGet.config` and modify `packageSources` section
```
<add key="dotnet-runtime" value="/path/to/dotnet/runtime/artifacts/packages/Release/Shipping" />
```

2. Run the benchmark
```cmd
/path/to/dotnet/performance$ python3 ./scripts/benchmarks_ci.py -f net9.0 --dotnet-path </path/to/dotnet/runtime/>artifacts/bin/dotnet-latest --wasm --run-isolated --bdn-artifacts artifacts/BenchmarkDotNet.Artifacts
    --bdn-arguments="--anyCategories Libraries Runtime --category-exclusion-filter NoInterpreter NoWASM NoMono --logBuildOutput --wasmArgs=\"--module\" --wasmDataDir </path/to/dotnet/runtime>/src/mono/browser --filter <filter>"
```

#### Run the benchmarks with AOT

Essentially, add `--aotcompilermode wasm` to the `--bdn-arguments=".."`:

```cmd
/path/to/dotnet/performance$ python3 ./scripts/benchmarks_ci.py --csproj src/benchmarks/micro/MicroBenchmarks.csproj -f net9.0 --dotnet-path </path/to/dotnet/runtime/>artifacts/bin/dotnet-latest --wasm --run-isolated --bdn-artifacts artifacts/BenchmarkDotNet.Artifacts
    --bdn-arguments="--category-exclusion-filter NoInterpreter NoWASM NoMono --aotcompilermode wasm --logBuildOutput --buildTimeout 3600 --wasmArgs=\"--module\" --wasmDataDir </path/to/dotnet/runtime>/src/mono/browser --filter <filter>"
```

#### Note about "file ... being used by another process" error

If you are seeing warnings like:
`warning MSB3026: Could not copy "/Users/radical/dev/performance/artifacts/obj/MicroBenchmarks/Release/net7.0/MicroBenchmarks.pdb" to "/Users/radical/dev/performance/artifacts/bin/MicroBenchmarks/Release/net7.0/MicroBenchmarks.pdb". Beginning retry 1 in 1000ms. The process cannot access the file '/Users/radical/dev/performance/artifacts/bin/MicroBenchmarks/Release/net7.0/MicroBenchmarks.pdb' because it is being used by another process.`

.. then ensure that `--run-isolated` is being passed.

The problem, and `--run-isolated` is described in the commit message - https://github.com/dotnet/performance/commit/2afd09171688e1a36cc9dbd8ac5d23c910ab80cb

### dotnet runtime testing for MonoAOT

#### Prerequisites (Files either built locally (with build.(sh/cmd) or downloaded from payload above (if same system setup) (in this order))

- Libraries build extracted to `runtime/artifacts` or build instructions: [Libraries README](https://github.com/dotnet/runtime/blob/main/docs/workflow/building/libraries/README.md) args: `-subset libs+libs.tests -rc release -configuration Release -arch $RunArch -framework net9.0`
- CoreCLR product build extracted to `runtime/artifacts/bin/coreclr/$RunOS.$RunArch.Release`, build instructions: [CoreCLR README](https://github.com/dotnet/runtime/blob/main/docs/workflow/building/coreclr/README.md) args: `-subset clr+libs -rc release -configuration Release -arch $RunArch -framework net9.0`
- AOT MONO build extracted to `runtime/artifacts/bin/mono/$RunOS.$RunArch.Release`, build instructions: [MONO README](https://github.com/dotnet/runtime/blob/main/docs/workflow/building/mono/README.md) args: `-arch $RunArch -os $RunOS -s mono+libs+host+packs -c Release /p:CrossBuild=false /p:MonoLLVMUseCxx11Abi=false`
- Dotnet SDK installed for dotnet commands
- Running commands from the runtime folder

#### Running on Linux

```cmd
# Set $RunDir to the runtime directory
RunDir=`pwd`

# Set the OS, arch, and OSId
RunOS='linux'
RunOSId='linux'
RunArch='x64'

# Create aot directory 
mkdir -p $RunDir/artifacts/bin/aot/sgen
mkdir -p $RunDir/artifacts/bin/aot/pack
cp -r $RunDir/artifacts/obj/mono/$RunOS.$RunArch.Release/mono/* $RunDir/artifacts/bin/aot/sgen
cp -r $RunDir/artifacts/bin/microsoft.netcore.app.runtime.$RunOS-$RunArch/Release/* $RunDir/artifacts/bin/aot/pack

# Create Core Root
$RunDir/src/tests/build.sh release $RunArch generatelayoutonly /p:LibrariesConfiguration=Release

# Clone performance 
git clone --branch main --depth 1 --quiet https://github.com/dotnet/performance.git $RunDir/performance

# One line run:
python3 $RunDir/performance/scripts/benchmarks_ci.py --csproj $RunDir/performance/src/benchmarks/micro/MicroBenchmarks.csproj --incremental no --architecture $RunArch -f net9.0 --filter '$TestToRun*' --bdn-artifacts $RunDir/artifacts/BenchmarkDotNet.Artifacts --bdn-arguments=""--anyCategories Libraries Runtime  --category-exclusion-filter NoAOT NoWASM --runtimes monoaotllvm --aotcompilerpath $RunDir/artifacts/bin/aot/sgen/mini/mono-sgen --customruntimepack $RunDir/artifacts/bin/aot/pack --aotcompilermode llvm --logBuildOutput --generateBinLog""

# Individual Commands:
# Restore 
dotnet restore $RunDir/performance/src/benchmarks/micro/MicroBenchmarks.csproj --packages $RunDir/performance/artifacts/packages /p:UseSharedCompilation=false /p:BuildInParallel=false /m:1

# Build
dotnet build $RunDir/performance/src/benchmarks/micro/MicroBenchmarks.csproj --configuration Release --framework net9.0 --no-restore /p:NuGetPackageRoot=$RunDir/performance/artifacts/packages /p:UseSharedCompilation=false /p:BuildInParallel=false /m:1

# Run
dotnet run --project $RunDir/performance/src/benchmarks/micro/MicroBenchmarks.csproj --configuration Release --framework net9.0 --no-restore --no-build -- --filter $TestToRun* --anyCategories Libraries Runtime "" --category-exclusion-filter NoAOT NoWASM --runtimes monoaotllvm --aotcompilerpath $RunDir/artifacts/bin/aot/sgen/mini/mono-sgen --customruntimepack $RunDir/artifacts/bin/aot/pack --aotcompilermode llvm --logBuildOutput --generateBinLog "" --artifacts $RunDir/artifacts/BenchmarkDotNet.Artifacts --packages $RunDir/performance/artifacts/packages --buildTimeout 1200
```

#### Running on Windows

```cmd
# Set $RunDir to the runtime directory
$RunDir=""FullPathHere""

# Set the OS, arch, and OSId
RunOS='windows'
RunOSId='win'
RunArch='x64'

# Create aot directory
mkdir $RunDir\artifacts\bin\aot\sgen
mkdir $RunDir\artifacts\bin\aot\pack
xcopy $RunDir\artifacts\obj\mono\$RunOS.$RunArch.Release\mono $RunDir\artifacts\bin\aot\sgen\ /e /y
xcopy $RunDir\artifacts\bin\microsoft.netcore.app.runtime.$RunOSId-$RunArch\Release $RunDir\artifacts\bin\aot\pack\ /e /y

# Create Core Root
$RunDir\src\tests\build.cmd release $RunArch generatelayoutonly /p:LibrariesConfiguration=Release

# Clone performance 
git clone --branch main --depth 1 --quiet https://github.com/dotnet/performance.git $RunDir\performance

# One line run:
python3 $RunDir\performance\scripts\benchmarks_ci.py --csproj $RunDir\performance\src\benchmarks\micro\MicroBenchmarks.csproj --incremental no --architecture $RunArch -f net9.0 --filter '$TestToRun*' --bdn-artifacts $RunDir\artifacts\BenchmarkDotNet.Artifacts --bdn-arguments=""--anyCategories Libraries Runtime  --category-exclusion-filter NoAOT NoWASM --runtimes monoaotllvm --aotcompilerpath $RunDir\artifacts\bin\aot\sgen\mini\mono-sgen.exe --customruntimepack $RunDir\artifacts\bin\aot\pack --aotcompilermode llvm --logBuildOutput --generateBinLog""

# Individual Commands:
# Restore 
dotnet restore $RunDir\performance\src\benchmarks\micro\MicroBenchmarks.csproj --packages $RunDir\performance\artifacts\packages /p:UseSharedCompilation=false /p:BuildInParallel=false /m:1

# Build
dotnet build $RunDir\performance\src\benchmarks\micro\MicroBenchmarks.csproj --configuration Release --framework net9.0 --no-restore /p:NuGetPackageRoot=$RunDir\performance\artifacts\packages /p:UseSharedCompilation=false /p:BuildInParallel=false /m:1

# Run
dotnet run --project $RunDir\performance\src\benchmarks\micro\MicroBenchmarks.csproj --configuration Release --framework net9.0 --no-restore --no-build -- --filter $TestToRun* --anyCategories Libraries Runtime "" --category-exclusion-filter NoAOT NoWASM --runtimes monoaotllvm --aotcompilerpath $RunDir\artifacts\bin\aot\sgen\mini\mono-sgen.exe --customruntimepack $RunDir\artifacts\bin\aot\pack -aotcompilermode llvm --logBuildOutput --generateBinLog "" --artifacts $RunDir\artifacts\BenchmarkDotNet.Artifacts --packages $RunDir\performance\artifacts\packages --buildTimeout 1200
```

### dotnet runtime testing for MonoInterpreter

#### Prerequisites (Build files either built locally or downloaded from payload above)

- Libraries build extracted to `runtime/artifacts` or build instructions: [Libraries README](https://github.com/dotnet/runtime/blob/main/docs/workflow/building/libraries/README.md) args: `-subset libs+libs.tests -rc release -configuration Release -arch $RunArch -framework net9.0`
- CoreCLR product build extracted to `runtime/artifacts/bin/coreclr/$RunOS.$RunArch.Release`, build instructions: [CoreCLR README](https://github.com/dotnet/runtime/blob/main/docs/workflow/building/coreclr/README.md) args: `-subset clr+libs -rc release -configuration Release -arch $RunArch -framework net9.0`
- Mono Runtime build extracted to `runtime/artifacts/bin/mono/$RunOS.$RunArch.Release`, build instructions: [MONO README](https://github.com/dotnet/runtime/blob/main/docs/workflow/building/mono/README.md) args: `-arch $RunArch -os $RunOS -s mono+libs+host+packs -c Release`
- Dotnet SDK installed for dotnet commands
- Running commands from the runtime folder

#### Running on Linux

```cmd
# Set $RunDir to the runtime directory
RunDir=`pwd`

# Set the OS, arch, and OSId
RunOS='linux'
RunOSId='linux'
RunArch='x64'

# Create mono dotnet
mkdir -p $RunDir/artifacts/dotnet-mono
$RunDir/build.sh -subset libs.pretest -configuration release -ci -arch $RunArch -testscope innerloop /p:RuntimeArtifactsPath=$RunDir/artifacts/bin/mono/$RunOS.$RunArch.Release /p:RuntimeFlavor=mono
cp $RunDir/artifacts/bin/runtime/net9.0-$RunOS-Release-$RunArch/* $RunDir/artifacts/bin/testhost/net9.0-$RunOS-Release-$RunArch/shared/Microsoft.NETCore.App/9.0.0 -rf
cp $RunDir/artifacts/bin/testhost/net9.0-$RunOS-Release-$RunArch/* $RunDir/artifacts/dotnet-mono -r
cp $RunDir/artifacts/bin/coreclr/$RunOS.$RunArch.Release/corerun $RunDir/artifacts/dotnet-mono/shared/Microsoft.NETCore.App/9.0.0/corerun

# Create Core Root
$RunDir/src/tests/build.sh release $RunArch generatelayoutonly /p:LibrariesConfiguration=Release

# Clone performance 
git clone --branch main --depth 1 --quiet https://github.com/dotnet/performance.git $RunDir/performance

# Use Interpreter
export MONO_ENV_OPTIONS=""--interpreter""

# One line run:
python3 $RunDir/performance/scripts/benchmarks_ci.py --csproj $RunDir/performance/src/benchmarks/micro/MicroBenchmarks.csproj --incremental no --architecture $RunArch -f net9.0 --filter '$TestToRun*' --bdn-artifacts $RunDir/artifacts/BenchmarkDotNet.Artifacts --bdn-arguments=""--anyCategories Libraries Runtime  --category-exclusion-filter NoInterpreter NoMono --logBuildOutput --generateBinLog --corerun $RunDir/artifacts/dotnet-mono/shared/Microsoft.NETCore.App/9.0.0/corerun"" 

# Individual Commands:
# Restore 
dotnet restore $RunDir/performance/src/benchmarks/micro/MicroBenchmarks.csproj --packages $RunDir/performance/artifacts/packages /p:UseSharedCompilation=false /p:BuildInParallel=false /m:1

# Build
dotnet build $RunDir/performance/src/benchmarks/micro/MicroBenchmarks.csproj --configuration Release --framework net9.0 --no-restore /p:NuGetPackageRoot=$RunDir/performance/artifacts/packages /p:UseSharedCompilation=false /p:BuildInParallel=false /m:1

# Run
dotnet run --project $RunDir/performance/src/benchmarks/micro/MicroBenchmarks.csproj --configuration Release --framework net9.0 --no-restore --no-build -- --filter '$TestToRun*' --anyCategories Libraries Runtime "" --category-exclusion-filter NoInterpreter NoMono --logBuildOutput --generateBinLog --corerun $RunDir/artifacts/dotnet-mono/shared/Microsoft.NETCore.App/9.0.0/corerun --artifacts $RunDir/artifacts/BenchmarkDotNet.Artifacts --packages $RunDir/performance/artifacts/packages
```

#### Running on Windows

```cmd
# Set $RunDir to the runtime directory
$RunDir=""FullPathHere""

# Set the OS, arch, and OSId
RunOS='windows'
RunOSId='win'
RunArch='x64'

# Create mono dotnet
mkdir -p $RunDir/artifacts/dotnet-mono
$RunDir/build.sh -subset libs.pretest -configuration release -ci -arch $RunArch -testscope innerloop /p:RuntimeArtifactsPath=$RunDir\artifacts\bin\mono\$RunOS.$RunArch.Release /p:RuntimeFlavor=mono
xcopy $RunDir\artifacts\bin\runtime\net9.0-$RunOS-Release-$RunArch\ $RunDir\artifacts\bin\testhost\net9.0-$RunOS-Release-$RunArch\shared\Microsoft.NETCore.App\9.0.0\ /e /y
xcopy $RunDir\artifacts\bin\testhost\net9.0-$RunOS-Release-$RunArch\ $RunDir\artifacts\dotnet-mono\ /e /y
xcopy $RunDir\artifacts\bin\coreclr\$RunOS.$RunArch.Release\corerun $RunDir\artifacts\dotnet-mono\shared\Microsoft.NETCore.App\9.0.0\corerun /y

# Create Core Root
$RunDir\src\tests\build.cmd release $RunArch generatelayoutonly /p:LibrariesConfiguration=Release

# Clone performance 
git clone --branch main --depth 1 --quiet https://github.com/dotnet/performance.git $RunDir\performance

# Use Interpreter
export MONO_ENV_OPTIONS=""--interpreter""

# One line run:
python3 $RunDir\performance\scripts\benchmarks_ci.py --csproj $RunDir\performance\src\benchmarks\micro\MicroBenchmarks.csproj --incremental no --architecture $RunArch -f net9.0 --filter '$TestToRun*' --bdn-artifacts $RunDir\artifacts\BenchmarkDotNet.Artifacts --bdn-arguments=""--anyCategories Libraries Runtime  --category-exclusion-filter NoInterpreter NoMono --logBuildOutput --generateBinLog --corerun $RunDir\artifacts\dotnet-mono\shared\Microsoft.NETCore.App\9.0.0\corerun.exe"" 

# Individual Commands:
# Restore 
dotnet restore $RunDir\performance\src\benchmarks\micro\MicroBenchmarks.csproj --packages $RunDir\performance\artifacts\packages /p:UseSharedCompilation=false /p:BuildInParallel=false /m:1

# Build
dotnet build $RunDir\performance\src\benchmarks\micro\MicroBenchmarks.csproj --configuration Release --framework net9.0 --no-restore /p:NuGetPackageRoot=$RunDir\performance\artifacts\packages /p:UseSharedCompilation=false /p:BuildInParallel=false /m:1

# Run
dotnet run --project $RunDir\performance\src\benchmarks\micro\MicroBenchmarks.csproj --configuration Release --framework net9.0 --no-restore --no-build -- --filter '$TestToRun*' --anyCategories Libraries Runtime "" --category-exclusion-filter NoInterpreter NoMono --logBuildOutput --generateBinLog --corerun $RunDir\artifacts\dotnet-mono\shared\Microsoft.NETCore.App\9.0.0\corerun.exe --artifacts $RunDir\artifacts\BenchmarkDotNet.Artifacts --packages $RunDir\performance\artifacts\packages
```

### dotnet runtime testing for Mono Default (JIT)

#### Prerequisites (Build files either built locally or downloaded from payload above)

- Libraries build extracted to `runtime/artifacts` or build instructions: [Libraries README](https://github.com/dotnet/runtime/blob/main/docs/workflow/building/libraries/README.md) args: `-subset libs+libs.tests -rc release -configuration Release -arch $RunArch -framework net9.0`
- CoreCLR product build extracted to `runtime/artifacts/bin/coreclr/$RunOS.$RunArch.Release`, build instructions: [CoreCLR README](https://github.com/dotnet/runtime/blob/main/docs/workflow/building/coreclr/README.md) args: `-subset clr+libs -rc release -configuration Release -arch $RunArch -framework net9.0`
- Mono Runtime build extracted to `runtime/artifacts/bin/mono/$RunOS.$RunArch.Release`, build instructions: [MONO README](https://github.com/dotnet/runtime/blob/main/docs/workflow/building/mono/README.md) args: `-arch $RunArch -os $RunOS -s mono+libs+host+packs -c Release`
- Dotnet SDK installed for dotnet commands
- Running commands from the runtime folder

#### Running on Linux

```cmd
# Set $RunDir to the runtime directory
RunDir=`pwd`

# Set the OS, arch, and OSId
RunOS='linux'
RunOSId='linux'
RunArch='x64'

# Create mono dotnet
mkdir -p $RunDir/artifacts/dotnet-mono
$RunDir/build.sh -subset libs.pretest -configuration release -ci -arch $RunArch -testscope innerloop /p:RuntimeArtifactsPath=$RunDir/artifacts/bin/mono/$RunOS.$RunArch.Release /p:RuntimeFlavor=mono
cp $RunDir/artifacts/bin/runtime/net9.0-$RunOS-Release-$RunArch/* $RunDir/artifacts/bin/testhost/net9.0-$RunOS-Release-$RunArch/shared/Microsoft.NETCore.App/9.0.0 -rf
cp $RunDir/artifacts/bin/testhost/net9.0-$RunOS-Release-$RunArch/* $RunDir/artifacts/dotnet-mono -r
cp $RunDir/artifacts/bin/coreclr/$RunOS.$RunArch.Release/corerun $RunDir/artifacts/dotnet-mono/shared/Microsoft.NETCore.App/9.0.0/corerun

# Create Core Root
$RunDir/src/tests/build.sh release $RunArch generatelayoutonly /p:LibrariesConfiguration=Release

# Clone performance 
git clone --branch main --depth 1 --quiet https://github.com/dotnet/performance.git $RunDir/performance

# One line run:
python3 $RunDir/performance/scripts/benchmarks_ci.py --csproj $RunDir/performance/src/benchmarks/micro/MicroBenchmarks.csproj --incremental no --architecture $RunArch -f net9.0 --filter '$TestToRun*' --bdn-artifacts $RunDir/artifacts/BenchmarkDotNet.Artifacts --bdn-arguments=""--anyCategories Libraries Runtime  --category-exclusion-filter NoInterpreter NoMono --logBuildOutput --generateBinLog --corerun $RunDir/artifacts/dotnet-mono/shared/Microsoft.NETCore.App/9.0.0/corerun"" 

# Individual Commands:
# Restore 
dotnet restore $RunDir/performance/src/benchmarks/micro/MicroBenchmarks.csproj --packages $RunDir/performance/artifacts/packages /p:UseSharedCompilation=false /p:BuildInParallel=false /m:1

# Build
dotnet build $RunDir/performance/src/benchmarks/micro/MicroBenchmarks.csproj --configuration Release --framework net9.0 --no-restore /p:NuGetPackageRoot=$RunDir/performance/artifacts/packages /p:UseSharedCompilation=false /p:BuildInParallel=false /m:1

# Run
dotnet run --project $RunDir/performance/src/benchmarks/micro/MicroBenchmarks.csproj --configuration Release --framework net9.0 --no-restore --no-build -- --filter '$TestToRun*' --anyCategories Libraries Runtime "" --category-exclusion-filter NoInterpreter NoMono --logBuildOutput --generateBinLog --corerun $RunDir/artifacts/dotnet-mono/shared/Microsoft.NETCore.App/9.0.0/corerun --artifacts $RunDir/artifacts/BenchmarkDotNet.Artifacts --packages $RunDir/performance/artifacts/packages
```

#### Running on Windows

```cmd
# Set $RunDir to the runtime directory
$RunDir=""FullPathHere""

# Set the OS, arch, and OSId
RunOS='windows'
RunOSId='win'
RunArch='x64'

# Create mono dotnet
mkdir -p $RunDir/artifacts/dotnet-mono
$RunDir/build.sh -subset libs.pretest -configuration release -ci -arch $RunArch -testscope innerloop /p:RuntimeArtifactsPath=$RunDir\artifacts\bin\mono\$RunOS.$RunArch.Release /p:RuntimeFlavor=mono
xcopy $RunDir\artifacts\bin\runtime\net9.0-$RunOS-Release-$RunArch\ $RunDir\artifacts\bin\testhost\net9.0-$RunOS-Release-$RunArch\shared\Microsoft.NETCore.App\9.0.0\ /e /y
xcopy $RunDir\artifacts\bin\testhost\net9.0-$RunOS-Release-$RunArch\ $RunDir\artifacts\dotnet-mono\ /e /y
xcopy $RunDir\artifacts\bin\coreclr\$RunOS.$RunArch.Release\corerun $RunDir\artifacts\dotnet-mono\shared\Microsoft.NETCore.App\9.0.0\corerun /y

# Create Core Root
$RunDir\src\tests\build.cmd release $RunArch generatelayoutonly /p:LibrariesConfiguration=Release

# Clone performance 
git clone --branch main --depth 1 --quiet https://github.com/dotnet/performance.git $RunDir\performance

# One line run:
python3 $RunDir\performance\scripts\benchmarks_ci.py --csproj $RunDir\performance\src\benchmarks\micro\MicroBenchmarks.csproj --incremental no --architecture $RunArch -f net9.0 --filter '$TestToRun*' --bdn-artifacts $RunDir\artifacts\BenchmarkDotNet.Artifacts --bdn-arguments=""--anyCategories Libraries Runtime  --category-exclusion-filter NoInterpreter NoMono --logBuildOutput --generateBinLog --corerun $RunDir\artifacts\dotnet-mono\shared\Microsoft.NETCore.App\9.0.0\corerun.exe"" 

# Individual Commands:
# Restore 
dotnet restore $RunDir\performance\src\benchmarks\micro\MicroBenchmarks.csproj --packages $RunDir\performance\artifacts\packages /p:UseSharedCompilation=false /p:BuildInParallel=false /m:1

# Build
dotnet build $RunDir\performance\src\benchmarks\micro\MicroBenchmarks.csproj --configuration Release --framework net9.0 --no-restore /p:NuGetPackageRoot=$RunDir\performance\artifacts\packages /p:UseSharedCompilation=false /p:BuildInParallel=false /m:1

# Run
dotnet run --project $RunDir\performance\src\benchmarks\micro\MicroBenchmarks.csproj --configuration Release --framework net9.0 --no-restore --no-build -- --filter '$TestToRun*' --anyCategories Libraries Runtime "" --category-exclusion-filter NoInterpreter NoMono --logBuildOutput --generateBinLog --corerun $RunDir\artifacts\dotnet-mono\shared\Microsoft.NETCore.App\9.0.0\corerun.exe --artifacts $RunDir\artifacts\BenchmarkDotNet.Artifacts --packages $RunDir\performance\artifacts\packages
```

## Preventing Regressions

Preventing regressions is a fundamental part of our performance culture. The cheapest regression is one that does not get into the product.

**Before introducing any changes that may impact performance**, you should run the benchmarks that test the performance of the feature that you are going to work on and store the results in a **dedicated** folder.

```cmd
C:\Projects\performance\src\benchmarks\micro> dotnet run -c Release -f net9.0 \
    --artifacts "C:\results\before" \
    --coreRun "C:\Projects\runtime\artifacts\bin\testhost\net9.0-windows-Release-x64\shared\Microsoft.NETCore.App\9.0.0\CoreRun.exe" \
    --filter System.IO.Pipes*
```

Please try to **avoid running any resource-heavy processes** that could **spoil** the benchmark results while running the benchmarks.

You can also create a **copy** of the folder with CoreRun and all the libraries to be able to run the benchmarks against the **unmodified base** in the future.

After you introduce the changes and rebuild the part of [dotnet/runtime](https://github.com/dotnet/runtime) that you are working on **in Release** you should re-run the benchmarks. Remember to store the results in a different folder.

```cmd
C:\Projects\runtime\src\libraries\System.IO.Pipes\src> dotnet msbuild /p:Configuration=Release

C:\Projects\performance\src\benchmarks\micro> dotnet run -c Release -f net9.0 \
    --artifacts "C:\results\after" \
    --coreRun "C:\Projects\runtime\artifacts\bin\testhost\net9.0-windows-Release-x64\shared\Microsoft.NETCore.App\9.0.0\CoreRun.exe" \
    --filter System.IO.Pipes*
```

When you have the results you should use [ResultsComparer](../src/tools/ResultsComparer/README.md) to find out how your changes have affected the performance:

```cmd
C:\Projects\performance\src\tools\ResultsComparer> dotnet run --base "C:\results\before" --diff "C:\results\after" --threshold 2%
```

Sample output:

```log
No Slower results for the provided threshold = 2% and noise filter = 0.3ns.
```

| Faster                                                                           | base/diff | Base Median (ns) | Diff Median (ns) | Modality|
| -------------------------------------------------------------------------------- | ---------:| ----------------:| ----------------:| --------:|
| System.IO.Pipes.Tests.Perf_NamedPipeStream_ServerIn_ClientOut.ReadWrite(size: 10 |      1.16 |        297167.47 |        255575.49 |         |

### Running against the latest .NET Core SDK

To run the benchmarks against the latest .NET Core SDK you can use the [benchmarks_ci.py](../scripts/benchmarks_ci.py) script. It's going to download the latest .NET Core SDK(s) for the provided framework(s) and run the benchmarks for you. Please see [Prerequisites](./prerequisites.md#python) for more.

```cmd
C:\Projects\performance> py scripts\benchmarks_ci.py -f net9.0 \
    --bdn-arguments="--artifacts "C:\results\latest_sdk"" \
    --filter System.IO.Pipes*
```

## Solving Regressions

### Repro Case

Once a regression is spotted, the first thing that you need to do is to create a benchmark that shows the problem. Typically every performance bug report comes with a small repro case. This is a perfect candidate for the benchmark (it might require some cleanup).

The next step is to send a PR to this repository with the aforementioned benchmark. Our automation is going to run this benchmark and export the results to our reporting system. When your fix to dotnet/runtime gets merged, our reports are going to show the difference. It also helps us to keep track of the old performance bugs and make sure that they never come back.

### Profiling

The real performance investigation starts with profiling. We have a comprehensive guide about profiling [dotnet/runtime](https://github.com/dotnet/runtime), we really encourage you to read it: [Profiling dotnet/runtime workflow](./profiling-workflow-dotnet-runtime.md).

To profile the benchmarked code and produce an ETW Trace file ([read more](./benchmarkdotnet.md#Profiling)):

```cmd
dotnet run -c Release -f net9.0 --profiler ETW --filter $YourFilter
```

The benchmarking tool is going to print the path to the `.etl` trace file. You should open it with PerfView or Windows Performance Analyzer and start the analysis from there. If you are not familiar with PerfView, you should watch [PerfView Tutorial](https://channel9.msdn.com/Series/PerfView-Tutorial) by @vancem first. It's an investment that is going to pay off very quickly.

```log
// * Diagnostic Output - EtwProfiler *
Exported 1 trace file(s). Example:
C:\Projects\performance\artifacts\20190215-0303-51368\Benchstone\BenchF\Adams\Test.etl
```

If profiling using the `--profiler ETW` is not enough, you should use a different profiler. When attaching to a process please keep in mind that what you run in the console is Host process, while the actual benchmarking is performed in dedicated processes. If you want to disable this behavior, you should use [InProcessToolchain](./benchmarkdotnet.md#Running-In-Process).

### Running against Older Versions

BenchmarkDotNet has some extra features that might be useful when doing performance investigation:

- You can run the benchmarks against [multiple Runtimes](./benchmarkdotnet.md#Multiple-Runtimes). It can be very useful when the regression has been introduced between .NET Core releases, for example: between net8.0 and net9.0.
- You can run the benchmarks using provided [dotnet cli](./benchmarkdotnet.md#dotnet-cli). You can download few dotnet SDKs, unzip them and just run the benchmarks to spot the version that has introduced the regression to narrow down your investigation.
- You can run the benchmarks using few [CoreRuns](./benchmarkdotnet.md#CoreRun). You can build the latest [dotnet/runtime](https://github.com/dotnet/runtime) in Release, create a copy of the folder with CoreRun and use git to checkout an older commit. Then rebuild [dotnet/runtime](https://github.com/dotnet/runtime) and run the benchmarks against the old and new builds. This can narrow down your investigation to the commit that has introduced the bug.

### Confirmation

When you identify and fix the regression, you should use [ResultsComparer](../src/tools/ResultsComparer/README.md) to confirm that you have solved the problem. Please remember that if the regression was found in a very common type like `Span<T>` and you are not sure which benchmarks to run, you can run all of them using `--filter *`.

Please take a moment to consider how the regression managed to enter the product. Are we now properly protected?

## Benchmarking new API

When developing new [dotnet/runtime](https://github.com/dotnet/runtime) features, we should be thinking about the performance from day one. One part of doing this is writing benchmarks at the same time when we write our first unit tests. Keeping the benchmarks in a separate repository makes it a little bit harder to run the benchmarks against new API, but it's still very easy.

### Reference

When you develop a new feature, whether it's a new method/type/library all you need to do is to build it in Release and just reference the produced implementation `.dll` from the [MicroBenchmarks.csproj](../src/benchmarks/micro/MicroBenchmarks.csproj) project file.

The easiest way to do it is to open [MicroBenchmarks.sln](../src/benchmarks/micro/MicroBenchmarks.sln) with Visual Studio, right click on the [MicroBenchmarks](../src/benchmarks/micro/MicroBenchmarks.csproj) project file, select "Add", then "Reference..." and in the new Dialog Window click "Browse" in the left bottom corner. From the File Picker, choose the new library and click "Add". Make sure to pick the reference assembly (not implementation assembly) which falls under path `artifacts\bin\ref\netcoreapp\`. Next, from the Solution Explorer window expand Dependencies for MicroBenchmarks solution and after selecting the assembly which you just added, set CopyLocal value to `No` from within the Properties window. Please don't forget to Save the changes (Ctrl+Shift+S). From this moment you should be able to consume new public types and methods exposed by the referenced library.

Sample changes:

```cs
namespace System
{
    public static class Console
    {
        public static void WriteHelloWorld() => WriteLine("Hello World!");
        // the rest omitted for brevity
    }
}
```

Sample project file change:

```xml
<ItemGroup>
  <Reference Include="System.Console">
    <HintPath>..\..\..\..\runtime\artifacts\bin\ref\netcoreapp\System.Console.dll</HintPath>
    <Private>false</Private>
  </Reference>
</ItemGroup>
```

### PR

Because the benchmarks are not in the [dotnet/runtime](https://github.com/dotnet/runtime) repository you must do two PR's.

The first thing you need to do is send a PR with the new API to the [dotnet/runtime](https://github.com/dotnet/runtime) repository. Once your PR gets merged and a new NuGet package is published to the [dotnet/runtime](https://github.com/dotnet/runtime) NuGet feed, you should remove the Reference to a `.dll` and install/update the package consumed by [MicroBenchmarks](../src/benchmarks/micro/MicroBenchmarks.csproj). You can do this by running the following script locally:

```cmd
/home/adsitnik/projects/performance>python3 ./scripts/benchmarks_ci.py --filter $YourFilter -f net9.0
```cmd
This script will try to pull the latest .NET Core SDK from [dotnet/runtime](https://github.com/dotnet/runtime) nightly build, which should contain the new API that you just merged in your first PR, and use that to build MicroBenchmarks project and then run the benchmarks that satisfy the filter you provided.

After you have confirmed your benchmarks successfully run locally, then your PR should be ready for performance repo.
