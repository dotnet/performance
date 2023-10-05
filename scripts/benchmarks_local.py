# This is a script for testing the performance of the different dotnet/runtime build types locally. It is assumed that prereqs for the build type are already installed.
# Example usage from the performance/scripts folder: 
# python .\benchmarks_local.py --local-test-repo "<absolute path to runtime folder>/runtime" --run-type MonoJIT --filter "*Span.IndexerBench.CoveredIndex2*"
# or if you want remotes:
# python .\benchmarks_local.py --commits dd079f53b95519c8398d8b0c6e796aaf7686b99a --repo-storage-path "<absolute path to where you want to store runtime clones>" --run-types MonoInterpreter MonoJIT --filter "*Span.IndexerBench.CoveredIndex2*"

# The general flow as it stands is:
# * For each commit single or pair value specified:
#   * Get the repo to the proper commit
#   * Build the dependencies for the run types specified and copy them to the artifact storage path
# * Run the benchmarks:
#   * For each run type specified with the artifacts all passed as coreruns to take advantage of BDN's comparisons
# * Adding a new run type:
#   * Add the run type to the RunType enum
#   * Add the build instructions to the generate_all_runtime_artifacts function
#   * Add the BDN run arguments to the generate_combined_benchmark_ci_args function
#
# Prereqs:
# Normal prereqs for building the target runtime: https://github.com/dotnet/runtime/blob/main/docs/workflow/README.md#Build_Requirements
# Python 3
# gitpython (pip install --global gitpython)
# Ubuntu Version 22.04 if using Ubuntu
# May need llvm+clang 16 for MonoAOTLLVM (https://apt.llvm.org/)
# Wasm need jsvu installed and setup (No need to setup EMSDK, the tool does that automatically when building)


import glob
import os
import platform
import shutil
import sys
import xml.etree.ElementTree as xmlTree
from argparse import ArgumentParser, ArgumentTypeError, Namespace
from datetime import datetime
from enum import Enum, EnumMeta
from logging import getLogger
from subprocess import CalledProcessError

import benchmarks_ci
import dotnet
from channel_map import ChannelMap
from git import GitCommandError
from git.repo import Repo
from performance.common import RunCommand, get_machine_architecture
from performance.logger import setup_loggers

# Assumptions: We are only testing this Performance repo, should allow single run or multiple runs
# For dotnet_version based runs, use the benchmarks_monthly .py script instead
# Verify the input commands
# What are supported default cases: MonoJIT, MonoInterpreter, Corerun, WasmInterpreter etc. (WIP: MONOAOTLLVM)

start_time = datetime.now()
local_shared_string = "local"

class RunType(Enum):
    CoreRun = 1
    MonoAOTLLVM = 2
    MonoInterpreter = 3
    MonoJIT = 4
    WasmInterpreter = 5
    WasmAOT = 6

def is_windows(parsed_args: Namespace):
    return parsed_args.os == "windows"

def get_os_short_name(os_name: str):
    if os_name == "windows":
        return "win"
    elif os_name == "linux":
        return "linux"
    elif os_name == "osx":
        return "osx"
    elif os_name == "browser":
        return "browser"
    else:
        raise ValueError(f"Unknown OS {os_name}")

def is_running_as_admin(parsed_args: Namespace) -> bool:
    if is_windows(parsed_args):
        import ctypes
        return ctypes.windll.shell32.IsUserAnAdmin()
    else:
        return os.getuid() == 0 # type: ignore We know that os.getuid() is a method on Unix-like systems, ignore the pylance unknown type error for getuid.

def kill_dotnet_processes(parsed_args: Namespace):
    if is_windows(parsed_args):
        os.system('TASKKILL /F /T /IM dotnet.exe 2> nul || TASKKILL /F /T /IM VSTest.Console.exe 2> nul || TASKKILL /F /T /IM msbuild.exe 2> nul || TASKKILL /F /T /IM ".NET Host" 2> nul')
    else:
        os.system('killall -9 dotnet 2> /dev/null || killall -9 VSTest.Console 2> /dev/null || killall -9 msbuild 2> /dev/null || killall -9 ".NET Host" 2> /dev/null') # Always kill dotnet so it isn't left with handles on its files

# Use EnumMeta until set to using python 3.11 or greater, where the name is switched to EnumType (although EnumMeta should still work as an alias)
def enum_name_to_enum(enum_type: EnumMeta, enum_name: str):
    try:
        return enum_type[enum_name]
    except KeyError:
        raise ArgumentTypeError(f"Invalid run type name {enum_name}.")

def enum_name_list_to_enum_list(enum_type: EnumMeta, enum_name_list: list[str]):
    return [enum_name_to_enum(enum_type, enum_name) for enum_name in enum_name_list]

def check_for_runtype_specified(parsed_args: Namespace, run_types_to_check: list[RunType]) -> bool:
    for run_type in run_types_to_check:
        if run_type.name in parsed_args.run_type_names:
            return True
    return False

# Uses python copy, to copy the contents of a directory to another directory while overwriting any existing files
def copy_directory_contents(src_dir: str, dest_dir: str):
    for src_dirpath, _, src_filenames in os.walk(src_dir):
        dest_dirpath = os.path.join(dest_dir, os.path.relpath(src_dirpath, src_dir))
        if not os.path.exists(dest_dirpath):
            os.makedirs(dest_dirpath)
        for src_filename in src_filenames:
            if os.path.exists(os.path.join(dest_dirpath, src_filename)) and os.path.samefile(os.path.join(src_dirpath, src_filename), os.path.join(dest_dirpath, src_filename)):
                continue
            shutil.copy2(os.path.join(src_dirpath, src_filename), dest_dirpath)
        
# Builds libs and corerun by default
def build_runtime_dependency(parsed_args: Namespace, repo_path: str, subset: str = "clr+libs", configuration: str = "Release", os_override = "", arch_override = "", additional_args: list[str] = []):    
    if is_windows(parsed_args):
        build_libs_and_corerun_command = [
                "powershell",
                "-File",
                "build.ps1"
        ]
    else:
        build_libs_and_corerun_command = [
                "bash",
                "build.sh"
        ]
    build_libs_and_corerun_command += [
                "-subset", subset, 
                "-configuration", configuration, 
                "-os", os_override if os_override else parsed_args.os,
                "-arch", arch_override if arch_override else parsed_args.architecture,  
                "-bl"
            ] + additional_args
    RunCommand(build_libs_and_corerun_command, verbose=True).run(os.path.join(repo_path, "eng"))

def generate_layout(parsed_args: Namespace, repo_path: str, additional_args: list[str] = []):
    # Run the command
    if is_windows(parsed_args):
        generate_layout_command = ["build.cmd"]
    else:
        generate_layout_command = ["./build.sh"]
    generate_layout_command += [
                "release",
                parsed_args.architecture,
                "generatelayoutonly",
                "/p:LibrariesConfiguration=Release"
            ] + additional_args
    RunCommand(generate_layout_command, verbose=True).run(os.path.join(repo_path, "src", "tests"))

def get_run_artifact_path(parsed_args: Namespace, run_type: RunType, commit: str) -> str:
    return os.path.join(parsed_args.artifact_storage_path, f"{run_type.name}-{commit}-{parsed_args.os}-{parsed_args.architecture}")

# Try to generate all of a single runs dependencies at once to save time
def generate_all_runtype_dependencies(parsed_args: Namespace, repo_path: str, commit: str, force_regenerate: bool = False):
    getLogger().info(f"Generating dependencies for {' '.join(map(str, parsed_args.run_type_names))} run types in {repo_path} and storing in {parsed_args.artifact_storage_path}.")
    
    if check_for_runtype_specified(parsed_args, [RunType.CoreRun]):
        artifact_core_root = os.path.join(get_run_artifact_path(parsed_args, RunType.CoreRun, commit), "Core_Root")
        
        if force_regenerate or not os.path.exists(artifact_core_root):
            build_runtime_dependency(parsed_args, repo_path)
            generate_layout(parsed_args, repo_path)
            # Store the corerun in the artifact storage path
            generated_core_root = os.path.join(repo_path, "artifacts", "tests", "coreclr", f"{parsed_args.os}.{parsed_args.architecture}.Release", "Tests", "Core_Root")
            shutil.rmtree(artifact_core_root, ignore_errors=True)
            copy_directory_contents(generated_core_root, artifact_core_root)
        else:
            getLogger().info(f"CoreRun already exists in {artifact_core_root}. Skipping generation.")

    if check_for_runtype_specified(parsed_args, [RunType.MonoInterpreter, RunType.MonoJIT]):
        artifact_mono_interpreter = os.path.join(get_run_artifact_path(parsed_args, RunType.MonoInterpreter, commit), "dotnet_mono")
        artifact_mono_jit = os.path.join(get_run_artifact_path(parsed_args, RunType.MonoJIT, commit), "dotnet_mono")

        if force_regenerate or not os.path.exists(artifact_mono_interpreter) or not os.path.exists(artifact_mono_jit):
            build_runtime_dependency(parsed_args, repo_path, "clr+mono+libs")
            build_runtime_dependency(parsed_args, repo_path, "libs.pretest", additional_args=['-testscope', 'innerloop', '/p:RuntimeFlavor=mono', f"/p:RuntimeArtifactsPath={os.path.join(repo_path, 'artifacts', 'bin', 'mono', f'{parsed_args.os}.{parsed_args.architecture}.Release')}"])

            # Get the dotnet version from the currently checked out runtimes Versions.props file (we assume that it exists)
            versions_props_path = os.path.join(repo_path, "eng", "Versions.props")
            tree = xmlTree.parse(versions_props_path)
            root = tree.getroot()
            product_version_element = root.find(".//ProductVersion")
            major_version_element = root.find(".//MajorVersion")
            if product_version_element is not None and major_version_element is not None:
                product_version = product_version_element.text
                major_version = major_version_element.text
            else:
                raise RuntimeError("ProductVersion or MajorVersion element not found in Versions.props file.")
                
            # Create the mono-dotnet
            src_dir_runtime = os.path.join(repo_path, "artifacts", "bin", "runtime", f"net{major_version}.0-{parsed_args.os}-Release-{parsed_args.architecture}")
            dest_dir_testhost_product = os.path.join(repo_path, "artifacts", "bin", "testhost", f"net{major_version}.0-{parsed_args.os}-Release-{parsed_args.architecture}", "shared", "Microsoft.NETCore.App", f"{product_version}") # Wrap product_version to force string type, otherwise we get warning: Argument of type "str | Any | None" cannot be assigned to parameter "paths" of type "BytesPath" in function "join"
            copy_directory_contents(src_dir_runtime, dest_dir_testhost_product)
            src_dir_testhost = os.path.join(repo_path, "artifacts", "bin", "testhost", f"net{major_version}.0-{parsed_args.os}-Release-{parsed_args.architecture}")
            dest_dir_dotnet_mono = os.path.join(repo_path, "artifacts", "dotnet_mono")
            copy_directory_contents(src_dir_testhost, dest_dir_dotnet_mono)
            src_file_corerun = os.path.join(repo_path, "artifacts", "bin", "coreclr", f"{parsed_args.os}.{parsed_args.architecture}.Release", f"corerun{'.exe' if is_windows(parsed_args) else ''}")
            dest_dir_dotnet_mono_shared = os.path.join(repo_path, "artifacts", "dotnet_mono", "shared", "Microsoft.NETCore.App", f"{product_version}") # Wrap product_version to force string type, otherwise we get warning: Argument of type "str | Any | None" cannot be assigned to parameter "paths" of type "BytesPath" in function "join"
            dest_file_corerun = os.path.join(dest_dir_dotnet_mono_shared, f"corerun{'.exe' if is_windows(parsed_args) else ''}")
            shutil.copy2(src_file_corerun, dest_file_corerun)

            # Store the dotnet_mono in the artifact storage path
            src_dir_dotnet_mono = os.path.join(repo_path, "artifacts", "dotnet_mono")
            shutil.rmtree(artifact_mono_interpreter, ignore_errors=True)
            copy_directory_contents(src_dir_dotnet_mono, artifact_mono_interpreter)
            shutil.rmtree(artifact_mono_jit, ignore_errors=True)
            copy_directory_contents(src_dir_dotnet_mono, artifact_mono_jit)
        else:
            getLogger().info(f"dotnet_mono already exists in {artifact_mono_interpreter} and {artifact_mono_jit}. Skipping generation.")

    if check_for_runtype_specified(parsed_args, [RunType.MonoAOTLLVM]):
        artifact_mono_aot_llvm = os.path.join(get_run_artifact_path(parsed_args, RunType.MonoAOTLLVM, commit), "monoaot")
        if force_regenerate or not os.path.exists(artifact_mono_aot_llvm):
            build_args = ['/p:MonoEnableLLVM=True', '/p:MonoAOTEnableLLVM=true', '/p:BuildMonoAOTCrossCompiler=true', f'/p:AotHostArchitecture={parsed_args.architecture}', f'/p:AotHostOS={parsed_args.os}']
            if parsed_args.mono_libclang_path:
                build_args.append(f'/p:MonoLibClang={parsed_args.mono_libclang_path}')
            build_runtime_dependency(parsed_args, repo_path, "mono+libs+host+packs", additional_args=build_args)
            
            # Move to the bin/aot location
            src_dir_aot = os.path.join(repo_path, "artifacts", "bin", "mono", f"{parsed_args.os}.{parsed_args.architecture}.Release", "cross", f"{get_os_short_name(parsed_args.os)}-{parsed_args.architecture}")
            dest_dir_aot = os.path.join(repo_path, "artifacts", "bin", "aot")
            copy_directory_contents(src_dir_aot, dest_dir_aot)
            src_dir_aot_pack = os.path.join(repo_path, "artifacts", "bin", f"microsoft.netcore.app.runtime.{get_os_short_name(parsed_args.os)}-{parsed_args.architecture}", "Release")
            dest_dir_aot_pack = os.path.join(repo_path, "artifacts", "bin", "aot", "pack")
            copy_directory_contents(src_dir_aot_pack, dest_dir_aot_pack)
            
            src_dir_aot_final = os.path.join(repo_path, "artifacts", "bin", "aot")
            shutil.rmtree(artifact_mono_aot_llvm, ignore_errors=True)
            copy_directory_contents(src_dir_aot_final, artifact_mono_aot_llvm)
        else:
            getLogger().info(f"dotnet_mono already exists in {artifact_mono_aot_llvm}. Skipping generation.")

    if check_for_runtype_specified(parsed_args, [RunType.WasmInterpreter, RunType.WasmAOT]):
        # Must have jsvu installed also
        artifact_wasm_wasm = os.path.join(get_run_artifact_path(parsed_args, RunType.WasmInterpreter, commit), "wasm_bundle")
        artifact_wasm_aot = os.path.join(get_run_artifact_path(parsed_args, RunType.WasmAOT, commit), "wasm_bundle")
        if force_regenerate or not os.path.exists(artifact_wasm_wasm) or not os.path.exists(artifact_wasm_aot):
            provision_wasm_command = [
                "make",
                "-C",
                os.path.join("src", "mono", "wasm"),
                "provision-wasm"
            ]
            RunCommand(provision_wasm_command, verbose=True).run(os.path.join(repo_path))
            os.environ["EMSDK_PATH"] = os.path.join(repo_path, 'src', 'mono', 'wasm', 'emsdk')

            build_runtime_dependency(parsed_args, repo_path, "mono+libs", os_override="browser", arch_override="wasm", additional_args=[f'/p:AotHostArchitecture={parsed_args.architecture}', f'/p:AotHostOS={parsed_args.os}'])
            src_dir_dotnet_latest = os.path.join(repo_path, "artifacts", "BrowserWasm", "staging", "dotnet-latest")
            dest_dir_wasm_dotnet = os.path.join(repo_path, "artifacts", "bin", "wasm", "dotnet")
            copy_directory_contents(src_dir_dotnet_latest, dest_dir_wasm_dotnet)
            src_dir_built_nugets = os.path.join(repo_path, "artifacts", "BrowserWasm", "staging", "built-nugets")
            dest_dir_bin_wasm = os.path.join(repo_path, "artifacts", "bin", "wasm")
            copy_directory_contents(src_dir_built_nugets, dest_dir_bin_wasm)
            src_file_test_main = os.path.join(repo_path, "src", "mono", "wasm", "test-main.js")
            dest_dir_wasm_data = os.path.join(repo_path, "artifacts", "bin", "wasm", "wasm-data")
            dest_file_test_main = os.path.join(dest_dir_wasm_data, "test-main.js")
            if not os.path.exists(dest_dir_wasm_data):
                os.makedirs(dest_dir_wasm_data)
            shutil.copy2(src_file_test_main, dest_file_test_main)

            # Store the dotnet_mono in the artifact storage path
            src_dir_dotnet_wasm = os.path.join(repo_path, "artifacts", "bin", "wasm")
            shutil.rmtree(artifact_wasm_wasm, ignore_errors=True)
            copy_directory_contents(src_dir_dotnet_wasm, artifact_wasm_wasm)
            shutil.rmtree(artifact_wasm_aot, ignore_errors=True)
            copy_directory_contents(src_dir_dotnet_wasm, artifact_wasm_aot)

            # Add wasm-tools to dotnet instance
            RunCommand([os.path.join(parsed_args.dotnet_dir_path, f'dotnet{".exe" if is_windows(parsed_args) else ""}'), "workload", "install", "wasm-tools"], verbose=True).run()
        else:
            getLogger().info(f"wasm_bundle already exists in {artifact_wasm_wasm} and {artifact_wasm_aot}. Skipping generation.")

    getLogger().info(f"Finished generating dependencies for {' '.join(map(str, parsed_args.run_type_names))} run types in {repo_path} and stored in {parsed_args.artifact_storage_path}.")

def generate_combined_benchmark_ci_args(parsed_args: Namespace, specific_run_type: RunType, all_commits: list[str]) -> list[str]:
    getLogger().info(f"Generating benchmark_ci.py arguments for {specific_run_type.name} run type using artifacts in {parsed_args.artifact_storage_path}.")
    bdn_args_unescaped: list[str] = []
    benchmark_ci_args = [
        '--architecture', parsed_args.architecture,
        '--frameworks', parsed_args.framework,
        '--filter', parsed_args.filter,
        '--dotnet-path', parsed_args.dotnet_dir_path,
        '--csproj', parsed_args.csproj,
        '--incremental', "no",
        '--bdn-artifacts', os.path.join(parsed_args.artifact_storage_path, f"BenchmarkDotNet.Artifacts.{specific_run_type.name}.{start_time.strftime('%y%m%d_%H%M%S')}") # We don't include the commit hash in the artifact path because we are combining multiple runs into on
    ]

    if specific_run_type == RunType.CoreRun:
        bdn_args_unescaped += [
            '--anyCategories', 'Libraries', 'Runtime',
            '--logBuildOutput',
            '--generateBinLog'
        ]
        bdn_args_unescaped += ['--corerun']
        for commit in all_commits:
            bdn_args_unescaped += [os.path.join(get_run_artifact_path(parsed_args, RunType.CoreRun, commit), "Core_Root", f'corerun{".exe" if is_windows(parsed_args) else ""}')]
    
    elif specific_run_type == RunType.MonoInterpreter:
        bdn_args_unescaped += [
            '--anyCategories', 'Libraries', 'Runtime', 
            '--category-exclusion-filter', 'NoInterpreter', 'NoMono',
            '--logBuildOutput',
            '--generateBinLog'
        ]
        bdn_args_unescaped += ['--corerun']
        for commit in all_commits:
            # We can force only one capture because the artifact_paths include the commit hash which is what we get the corerun from.
            corerun_capture = glob.glob(os.path.join(get_run_artifact_path(parsed_args, RunType.MonoInterpreter, commit), "dotnet_mono", "shared", "Microsoft.NETCore.App", "*", f'corerun{".exe" if is_windows(parsed_args) else ""}'))
            if len(corerun_capture) == 0:
                raise Exception(f"Could not find corerun in {get_run_artifact_path(parsed_args, RunType.MonoInterpreter, commit)}")
            elif len(corerun_capture) > 1:
                raise Exception(f"Found multiple corerun in {get_run_artifact_path(parsed_args, RunType.MonoInterpreter, commit)}")
            else:
                corerun_path = corerun_capture[0]
            bdn_args_unescaped += [corerun_path]
        bdn_args_unescaped += ['--envVars', 'MONO_ENV_OPTIONS:--interpreter']

    elif specific_run_type == RunType.MonoJIT:
        bdn_args_unescaped += [
            '--anyCategories', 'Libraries', 'Runtime', 
            '--category-exclusion-filter', 'NoInterpreter', 'NoMono',
            '--logBuildOutput',
            '--generateBinLog'
        ]
        bdn_args_unescaped += ['--corerun']
        for commit in all_commits:
            # We can force only one capture because the artifact_paths include the commit hash which is what we get the corerun from.
            corerun_capture = glob.glob(os.path.join(get_run_artifact_path(parsed_args, RunType.MonoJIT, commit), "dotnet_mono", "shared", "Microsoft.NETCore.App", "*", f'corerun{".exe" if is_windows(parsed_args) else ""}'))
            if len(corerun_capture) == 0:
                raise Exception(f"Could not find corerun in {get_run_artifact_path(parsed_args, RunType.MonoJIT, commit)}")
            elif len(corerun_capture) > 1:
                raise Exception(f"Found multiple corerun in {get_run_artifact_path(parsed_args, RunType.MonoJIT, commit)}")
            else:
                corerun_path = corerun_capture[0]
            bdn_args_unescaped += [corerun_path]

    # for commit in all_commits: There is not a way to run multiple Wasm's at once via CI, instead will split single run vs multi-run scenarios
    elif specific_run_type == RunType.MonoAOTLLVM:
        raise TypeError("MonoAOTLLVM does not support combined benchmark ci arg generation, use single benchmark generation and loop the benchmark_ci.py calls.")

    elif specific_run_type == RunType.WasmInterpreter:
        raise TypeError("WasmInterpreter does not support combined benchmark ci arg generation, use single benchmark generation and loop the benchmark_ci.py calls.")

    elif specific_run_type == RunType.WasmAOT:
        raise TypeError("WasmAOT does not support combined benchmark ci arg generation, use single benchmark generation and loop the benchmark_ci.py calls.")

    if parsed_args.bdn_arguments:
        bdn_args_unescaped += [parsed_args.bdn_arguments]
    benchmark_ci_args += [f'--bdn-arguments={" ".join(bdn_args_unescaped)}']
    getLogger().info(f"Finished generating benchmark_ci.py arguments for {specific_run_type.name} run type using artifacts in {parsed_args.artifact_storage_path}.")
    return benchmark_ci_args

def generate_single_benchmark_ci_args(parsed_args: Namespace, specific_run_type: RunType, commit: str) -> list[str]:
    getLogger().info(f"Generating benchmark_ci.py arguments for {specific_run_type.name} run type using artifacts in {parsed_args.artifact_storage_path}.")
    bdn_args_unescaped: list[str] = []
    benchmark_ci_args = [
        '--architecture', parsed_args.architecture,
        '--frameworks', parsed_args.framework,
        '--filter', parsed_args.filter,
        '--dotnet-path', parsed_args.dotnet_dir_path,
        '--csproj', parsed_args.csproj,
        '--incremental', "no",
        '--bdn-artifacts', os.path.join(parsed_args.artifact_storage_path, f"BenchmarkDotNet.Artifacts.{specific_run_type.name}.{commit}.{start_time.strftime('%y%m%d_%H%M%S')}") # We add the commit hash to the artifact path because we are only running one commit at a time and they would clobber if running more than one commit perf type
    ]

    if specific_run_type == RunType.CoreRun:
        bdn_args_unescaped += [
            '--anyCategories', 'Libraries', 'Runtime',
            '--logBuildOutput',
            '--generateBinLog',
            '--corerun', os.path.join(get_run_artifact_path(parsed_args, RunType.CoreRun, commit), "Core_Root", f'corerun{".exe" if is_windows(parsed_args) else ""}')
        ]

    elif specific_run_type == RunType.MonoAOTLLVM:
        bdn_args_unescaped += [
            '--anyCategories', 'Libraries', 'Runtime',
            '--category-exclusion-filter', 'NoAOT', 'NoWASM',
            '--runtimes', "monoaotllvm",
            '--aotcompilerpath', os.path.join(get_run_artifact_path(parsed_args, RunType.MonoAOTLLVM, commit), "monoaot", f"mono-aot-cross{'.exe' if is_windows(parsed_args) else ''}"),
            '--customruntimepack', os.path.join(get_run_artifact_path(parsed_args, RunType.MonoAOTLLVM, commit), "monoaot", "pack"),
            '--aotcompilermode', 'llvm',
            '--logBuildOutput',
            '--generateBinLog'
        ]
    
    elif specific_run_type == RunType.MonoInterpreter:
        bdn_args_unescaped += [
            '--anyCategories', 'Libraries', 'Runtime', 
            '--category-exclusion-filter', 'NoInterpreter', 'NoMono',
            '--logBuildOutput',
            '--generateBinLog'
        ]
        
        # We can force only one capture because the artifact_paths include the commit hash which is what we get the corerun from. There should only ever be 1 core run so this is just a check.
        corerun_capture = glob.glob(os.path.join(get_run_artifact_path(parsed_args, RunType.MonoInterpreter, commit), "dotnet_mono", "shared", "Microsoft.NETCore.App", "*", f'corerun{".exe" if is_windows(parsed_args) else ""}'))
        if len(corerun_capture) == 0:
            raise Exception(f"Could not find corerun in {get_run_artifact_path(parsed_args, RunType.MonoInterpreter, commit)}")
        elif len(corerun_capture) > 1:
            raise Exception(f"Found multiple corerun in {get_run_artifact_path(parsed_args, RunType.MonoInterpreter, commit)}")
        else:
            corerun_path = corerun_capture[0]
        bdn_args_unescaped += [
            '--corerun', corerun_path,
            '--envVars', 'MONO_ENV_OPTIONS:--interpreter'
        ]

    elif specific_run_type == RunType.MonoJIT:
        bdn_args_unescaped += [
            '--anyCategories', 'Libraries', 'Runtime', 
            '--category-exclusion-filter', 'NoInterpreter', 'NoMono',
            '--logBuildOutput',
            '--generateBinLog'
        ]
        
        # We can force only one capture because the artifact_paths include the commit hash which is what we get the corerun from.
        corerun_capture = glob.glob(os.path.join(get_run_artifact_path(parsed_args, RunType.MonoJIT, commit), "dotnet_mono", "shared", "Microsoft.NETCore.App", "*", f'corerun{".exe" if is_windows(parsed_args) else ""}'))
        if len(corerun_capture) == 0:
            raise Exception(f"Could not find corerun in {get_run_artifact_path(parsed_args, RunType.MonoJIT, commit)}")
        elif len(corerun_capture) > 1:
            raise Exception(f"Found multiple corerun in {get_run_artifact_path(parsed_args, RunType.MonoJIT, commit)}")
        else:
            corerun_path = corerun_capture[0]
        bdn_args_unescaped += ['--corerun', corerun_path]

    # for commit in all_commits: There is not a way to run multiple Wasm's at once via CI, instead will split single run vs multi-run scenarios
    elif specific_run_type == RunType.WasmInterpreter:
        benchmark_ci_args += ['--wasm']
        # Ensure there is a space at the beginning of `--wasmArgs` argument, so BDN
        # can correctly read them as sub-arguments for `--wasmArgs`
        bdn_args_unescaped += [
            '--anyCategories', 'Libraries', 'Runtime',
            '--category-exclusion-filter', 'NoInterpreter', 'NoWASM', 'NoMono',
            '--wasmDataDir', os.path.join(get_run_artifact_path(parsed_args, RunType.WasmInterpreter, commit), "wasm_bundle", "wasm-data"),
            '--wasmEngine', parsed_args.wasm_engine_path,
            '--wasmArgs', '\" --experimental-wasm-eh --expose_wasm --module\"',
            '--logBuildOutput',
            '--generateBinLog'
        ]

    elif specific_run_type == RunType.WasmAOT:
        benchmark_ci_args += ['--wasm']
        # Ensure there is a space at the beginning of `--wasmArgs` argument, so BDN
        # can correctly read them as sub-arguments for `--wasmArgs`
        bdn_args_unescaped += [
            '--anyCategories', 'Libraries', 'Runtime',
            '--category-exclusion-filter', 'NoInterpreter', 'NoWASM', 'NoMono',
            '--wasmDataDir', os.path.join(get_run_artifact_path(parsed_args, RunType.WasmAOT, commit), "wasm_bundle", "wasm-data"),
            '--wasmEngine', parsed_args.wasm_engine_path,
            '--wasmArgs', '\" --experimental-wasm-eh --expose_wasm --module\"',
            '--aotcompilermode', 'wasm',
            '--logBuildOutput',
            '--generateBinLog'
        ]

    if parsed_args.bdn_arguments:
        bdn_args_unescaped += [parsed_args.bdn_arguments]
    benchmark_ci_args += [f'--bdn-arguments={" ".join(bdn_args_unescaped)}']
    getLogger().info(f"Finished generating benchmark_ci.py arguments for {specific_run_type.name} run type commit {commit} using artifacts in {parsed_args.artifact_storage_path}.")
    return benchmark_ci_args

def generate_artifacts_for_commit(parsed_args: Namespace, repo_url: str, repo_dir: str, commit: str, is_local: bool = False) -> None:
    kill_dotnet_processes(parsed_args)
    if is_local:
        repo_path = repo_dir
        if not os.path.exists(repo_path):
            raise RuntimeError(f"The specified local path {repo_path} does not exist.")
        getLogger().info(f"Running for {repo_path} at {commit}.")
    else:
        repo_path = os.path.join(parsed_args.repo_storage_path, repo_dir)
        getLogger().info(f"Running for {repo_path} at {commit}.")

        if not os.path.exists(repo_path):
            repo = Repo.clone_from(repo_url, repo_path) # type: ignore 'Type of "clone_from" is partially unknown', we know it is a method and returns a Repo
            repo.git.checkout(commit)
            repo.git.show('HEAD')
        else:
            repo = Repo(repo_path)
            repo.remotes.origin.fetch()
            repo.git.checkout(commit)
            repo.git.show('HEAD')

    # Determine what we need to generate for the local benchmarks
    generate_all_runtype_dependencies(parsed_args, repo_path, commit, (is_local and not parsed_args.skip_local_rebuild) or parsed_args.rebuild_artifacts)

# Run tests on the local machine
def run_benchmarks(parsed_args: Namespace, commits: list[str]) -> None:
    # Generate the correct benchmarks_ci.py arguments for the run type
    for run_type_meta in enum_name_list_to_enum_list(RunType, parsed_args.run_type_names):
        # Run the benchmarks_ci.py test and save results
        run_type = RunType(run_type_meta)
        try:
            if run_type in [RunType.CoreRun, RunType.MonoInterpreter, RunType.MonoJIT]:
                benchmark_ci_args = generate_combined_benchmark_ci_args(parsed_args, run_type, commits)
                getLogger().info(f"Running benchmarks_ci.py for {run_type} at {commits} with arguments \"{' '.join(benchmark_ci_args)}\".")
                kill_dotnet_processes(parsed_args)
                benchmarks_ci.main(benchmark_ci_args) # Build the runtime includes a download of dotnet at this location
            elif run_type in [RunType.MonoAOTLLVM, RunType.WasmInterpreter, RunType.WasmAOT]:
                for commit in commits:
                    benchmark_ci_args = generate_single_benchmark_ci_args(parsed_args, run_type, commit)
                    getLogger().info(f"Running single benchmarks_ci.py for {run_type} at {commit} with arguments \"{' '.join(benchmark_ci_args)}\".")
                    kill_dotnet_processes(parsed_args)
                    benchmarks_ci.main(benchmark_ci_args)
            else:
                raise TypeError(f"Run type {run_type} is not supported. Please check the run type and try again.")
        except CalledProcessError:
            getLogger().error('benchmarks_ci exited with non zero exit code, please check the log and report benchmark failure')
            raise

        getLogger().info(f"Finished running benchmark for {run_type} at {commits}.")

def install_dotnet(parsed_args: Namespace) -> None:
    if not os.path.exists(parsed_args.dotnet_dir_path) or parsed_args.reinstall_dotnet:
        dotnet.install(parsed_args.architecture, ["main"], parsed_args.dotnet_versions, parsed_args.verbose, parsed_args.dotnet_dir_path)
    dotnet.setup_dotnet(parsed_args.dotnet_dir_path)

# Check if the specified references exist in the given repository URL.
# If a reference does not exist, raise an exception.
# 
# Arguments:
# - repo_url (str): The URL of the repository to check.
# - references (list): A list of references (branches or commit hashes) to check.
# - repo_storage_path (str): The directory where the cloned repository is stored.
# - repo_dir (str): The name of the directory where the cloned repository is stored.
#
# Returns: None
def check_references_exist_and_add_branch_commits(repo_url: str, references: list[str], repo_storage_path: str, repo_dir: str):
    getLogger().debug(f"Inside check_references_exist_and_add_branch_commits: Checking if references {references} exist in {repo_url}.")
    
    # Initialize a new Git repository in the specified directory
    repo_combined_path = os.path.join(repo_storage_path, repo_dir)
    if not os.path.exists(repo_combined_path):
        getLogger().debug(f"Cloning {repo_url} to {repo_combined_path}.")
        repo = Repo.clone_from(repo_url, repo_combined_path) # type: ignore 'Type of "clone_from" is partially unknown', we know it is a method and returns a Repo
    else:
        repo = Repo(repo_combined_path)
        repo.remotes.origin.fetch()

    # Check if each reference exists in the repository
    for reference in references:
        try:
            repo.git.branch('-r', '--contains', reference) # Use git branch -r --contains <commit> to check if a commit is in a branch
        except GitCommandError:
            raise Exception(f"Reference {reference} does not exist in {repo_url}.")

def add_arguments(parser: ArgumentParser):
    dotnet.add_arguments(parser)

    # Arguments for the local runner script
    parser.add_argument('--list-cached-builds', action='store_true', help='Lists the cached builds located in the artifact-storage-path.')
    parser.add_argument('--commits', nargs='+', type=str, help='The commits to test.')
    parser.add_argument('--repo-url', type=str, default='https://github.com/dotnet/runtime.git', help='The runtime repo to test from, used to get data for a fork.')
    parser.add_argument('--local-test-repo', type=str, help='Path to a local repo with the runtime source code to test from.') 
    parser.add_argument('--separate-repos', action='store_true', help='Whether to test each runtime version from their own separate repo directory.') # TODO: Do we want to have this as an actual option? It made sense before a shared build cache was added
    parser.add_argument('--repo-storage-path', type=str, default=os.getcwd(), help='The path to store the cloned repositories in.')
    parser.add_argument('--artifact-storage-path', type=str, default=os.path.join(os.getcwd(), "runtime-testing-artifacts"), help=f'The path to store the artifacts in (builds, results, etc). Default is {os.path.join(os.getcwd(), "runtime-testing-artifacts")}')
    parser.add_argument('--rebuild-artifacts', action='store_true', help='Whether to rebuild the artifacts for the specified commits before benchmarking.')
    parser.add_argument('--reinstall-dotnet', action='store_true', help='Whether to reinstall dotnet for use in building the benchmarks before running the benchmarks.')
    parser.add_argument('--build-only', action='store_true', help='Whether to only build the artifacts for the specified commits and not run the benchmarks.')
    parser.add_argument('--skip-local-rebuild', action='store_true', help='Whether to skip rebuilding the local repo and use the already built version (if already built). Useful if you need to run against local changes again.')
    parser.add_argument('--allow-non-admin-execution', action='store_true', help='Whether to allow non-admin execution of the script. Admin execution is highly recommended as it minimizes the chance of encountering errors, but may not be possible in all cases.')
    def __is_valid_run_type(value: str):
        try:
            RunType[value]
        except KeyError:
            raise ArgumentTypeError(f"Invalid run type: {value}.")
        return value
    parser.add_argument('--run-types', dest='run_type_names', nargs='+', type=__is_valid_run_type, choices=[run_type.name for run_type in RunType], help='The types of runs to perform.')
    parser.add_argument('--quiet', dest='verbose', action='store_false', help='Whether to not print verbose output.')

    # Arguments specifically for dependency generation and BDN
    parser.add_argument('--bdn-arguments', type=str, default="", help='Command line arguments to be passed to BenchmarkDotNet, wrapped in quotes')
    parser.add_argument('--architecture', choices=['x64', 'x86', 'arm64', 'arm'], default=get_machine_architecture(), help='Specifies the SDK processor architecture')
    parser.add_argument('--os', choices=['windows', 'linux', 'osx'], default=get_default_os(), help='Specifies the operating system of the system. Darwin is OSX.')
    parser.add_argument('--filter', type=str, default='*', help='Specifies the benchmark filter to pass to BenchmarkDotNet')
    parser.add_argument('-f', '--framework', choices=ChannelMap.get_supported_frameworks(), default='net8.0', help='The target framework used to build the microbenchmarks.') # Can and should this accept multiple frameworks?
    parser.add_argument('--csproj', type=str, default=os.path.join("..", "src", "benchmarks", "micro", "MicroBenchmarks.csproj"), help='The path to the csproj file to run benchmarks against.')   
    parser.add_argument('--mono-libclang-path', type=str, help='The full path to the clang compiler to use for the benchmarks. e.g. "/usr/local/lib/libclang.so.16", used for "MonoLibClang" build property.')
    parser.add_argument('--wasm-engine-path', type=str, help='The full path to the wasm engine to use for the benchmarks. e.g. /usr/local/bin/v8') # TODO: Setup required arguments

def get_default_os():
    system = platform.system().lower()
    if system == 'darwin':
        return 'osx'
    elif system in ['windows', 'linux', 'osx']:
        return system
    else:
        raise Exception("Unsupported operating system: {system}.")

def __main(args: list[str]):
    # Define the ArgumentParser
    parser = ArgumentParser(description='Run local benchmarks for the Performance repo.', conflict_handler='resolve')
    add_arguments(parser)
    parsed_args = parser.parse_args(args)
    parsed_args.dotnet_dir_path = os.path.join(parsed_args.artifact_storage_path, "dotnet")
    
    setup_loggers(verbose=parsed_args.verbose)

    # Ensure we are running as admin
    if not is_running_as_admin(parsed_args):
        if parsed_args.allow_non_admin_execution:
            getLogger().warning("This script is not running as an administrator. This may cause errors.")
        else:
            raise Exception("This script must be run as an administrator or --allow-non-admin-execution must be passed.")

    # If list cached builds is specified, list the cached builds and exit
    if parsed_args.list_cached_builds:
        for folder in os.listdir(parsed_args.artifact_storage_path): # type: ignore warning about folder type being unknown, we know it is a string
            if any([run_type.name in folder for run_type in RunType]): 
                getLogger().info(folder) # type: ignore We know folder is a string
        return

    # Check to make sure we have something specified to test
    if parsed_args.commits or parsed_args.local_test_repo:
        if parsed_args.commits:
            getLogger().info(f"Commits to test are: {parsed_args.commits}")
        if parsed_args.local_test_repo:
            getLogger().info(f"Local repo to test is: {parsed_args.local_test_repo}")
    else:
        raise Exception("A commit id and/or local repo must be specified.")

    getLogger().debug(f"Input arguments: {parsed_args}")

    repo_dirs: list[str] = []
    repo_url = parsed_args.repo_url
    if parsed_args.commits:
        getLogger().info(f"Checking if references {parsed_args.commits} exist in {repo_url}.")
        check_references_exist_and_add_branch_commits(repo_url, parsed_args.commits, parsed_args.repo_storage_path, repo_dirs[0] if parsed_args.separate_repos else "runtime")
        for commit in parsed_args.commits:
            repo_dirs.append(f"runtime-{commit.replace('/', '-')}")

    try:
        getLogger().info("Killing any running dotnet, vstest, or msbuild processes... (ignore system cannot find path specified)")
        kill_dotnet_processes(parsed_args)

        # Install Dotnet so we can add tools
        install_dotnet(parsed_args)

        # Generate the artifacts for each of the remote versions
        if parsed_args.commits:
            getLogger().info(f"References {parsed_args.commits} exist in {repo_url}.")
            for repo_dir, commit in zip(repo_dirs, parsed_args.commits):
                if parsed_args.separate_repos:
                    generate_artifacts_for_commit(parsed_args, repo_url, repo_dir, commit)
                else:
                    generate_artifacts_for_commit(parsed_args, repo_url, "runtime", commit)

        # Generate the artifacts for the local version
        if parsed_args.local_test_repo:
            generate_artifacts_for_commit(parsed_args, local_shared_string, parsed_args.local_test_repo, local_shared_string, True)

        if not parsed_args.build_only:
            # Run the benchmarks
            commitsToRun: list[str] = []
            if parsed_args.commits:
                commitsToRun = parsed_args.commits
            if parsed_args.local_test_repo:
                commitsToRun.append(local_shared_string)
            run_benchmarks(parsed_args, commitsToRun)
        else:
            getLogger().info("Skipping benchmark run because --build-only was specified.")
        
    finally:
        kill_dotnet_processes(parsed_args)
    # TODO: Compare the results of the benchmarks with results comparer (Currently will need to be done manually)

if __name__ == "__main__":
    __main(sys.argv[1:])
