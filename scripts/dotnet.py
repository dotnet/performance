#!/usr/bin/env python3

"""
Contains the functionality around DotNet Cli.
"""

import ssl
from argparse import Action, ArgumentParser, ArgumentTypeError, ArgumentError
from collections import namedtuple
from glob import iglob
from json import loads
from logging import getLogger
from os import chmod, environ, listdir, makedirs, path, pathsep, system
from re import search
from shutil import rmtree
from stat import S_IRWXU
from subprocess import CalledProcessError, check_output
from sys import argv, platform
from typing import Tuple
from urllib.error import URLError
from urllib.parse import urlparse
from urllib.request import urlopen
from time import sleep

from performance.common import get_machine_architecture
from performance.common import get_repo_root_path
from performance.common import get_tools_directory
from performance.common import push_dir
from performance.common import RunCommand
from performance.common import validate_supported_runtime
from performance.logger import setup_loggers
from channel_map import ChannelMap


def info(verbose: bool) -> None:
    """
    Executes `dotnet --info` in order to get the .NET Core information from the
    dotnet executable.
    """
    cmdline = ['dotnet', '--info']
    RunCommand(cmdline, verbose=verbose).run()

def exec(asm_path: str, success_exit_codes: list, verbose: bool, *args) -> int:
    """
    Executes `dotnet exec` which can be used to execute assemblies
    """
    asm_path=path.abspath(asm_path)
    working_dir=path.dirname(asm_path)
    if not path.exists(asm_path):
        raise ArgumentError('Cannot find assembly {} to exec'.format(asm_path))

    cmdline = ['dotnet', 'exec', path.basename(asm_path)]
    cmdline += list(args)
    return RunCommand(cmdline, success_exit_codes, verbose=verbose).run(working_dir)

def __log_script_header(message: str):
    message_length = len(message)
    getLogger().info('-' * message_length)
    getLogger().info(message)
    getLogger().info('-' * message_length)


CSharpProjFile = namedtuple('CSharpProjFile', [
    'file_name',
    'working_directory'
])

class FrameworkAction(Action):
    '''
    Used by the ArgumentParser to represent the information needed to parse the
    supported .NET frameworks argument from the command line.
    '''

    def __call__(self, parser, namespace, values, option_string=None):
        if values:
            setattr(namespace, self.dest, list(set(values)))

    @staticmethod
    def get_target_framework_moniker(framework: str) -> str:
        '''
        Translates framework name to target framework moniker (TFM)
        To run NativeAOT benchmarks we need to run the host BDN process as latest
        .NET the host process will build and run AOT benchmarks
        '''
        if framework == 'nativeaot6.0':
            return 'net6.0'
        if framework == 'nativeaot7.0':
            return 'net7.0'
        else:
            return framework

    @staticmethod
    def get_target_framework_monikers(frameworks: list) -> list:
        '''
        Translates framework names to target framework monikers (TFM)
        Required to run AOT benchmarks where the host process must be .NET
        , not NativeAOT.
        '''
        monikers = [
            FrameworkAction.get_target_framework_moniker(framework)
            for framework in frameworks
        ]

        # ['net6.0', 'nativeaot6.0'] should become ['net6.0']
        return list(set(monikers))

class VersionsAction(Action):
    '''
    Argument parser helper class used to validates the dotnet-versions input.
    '''

    def __call__(self, parser, namespace, values, option_string=None):
        if values:
            for version in values:
                if not search(r'^\d\.\d+\.\d+', version):
                    raise ArgumentTypeError(
                        'Version "{}" is in the wrong format'.format(version))
            setattr(namespace, self.dest, values)


class CompilationAction(Action):
    '''
    Tiered: (Default)

    NoTiering: Tiering is disabled, but R2R code is not disabled.
        This includes R2R code, useful for comparison against Tiered and
        FullyJittedNoTiering for changes to R2R code or tiering.

    Default: Don't set any environment variables. Use what the compiler views
        as the default.

    FullyJittedNoTiering: Tiering and R2R are disabled.
        This is JIT-only, useful for comparison against Tiered and NoTiering
        for changes to R2R code or tiering.

    MinOpt:
        Uses minopt-JIT for methods that do not have pregenerated code, useful
        for startup time comparisons in scenario benchmarks that include a
        startup time measurement (probably not for microbenchmarks), probably
        not useful for a PR.

    For PRs it is recommended to kick off a Tiered run, and being able to
    manually kick-off NoTiering and FullyJittedNoTiering modes when needed.
    '''
    # TODO: Would 'Default' make sense for .NET Framework / NativeAOT / Mono?
    # TODO: Should only be required for benchmark execution under certain tools

    TIERED = 'Tiered'
    NO_TIERING = 'NoTiering'
    DEFAULT = 'Default'
    FULLY_JITTED_NO_TIERING = 'FullyJittedNoTiering'
    MIN_OPT = 'MinOpt'

    def __call__(self, parser, namespace, values, option_string=None):
        if values:
            if values not in CompilationAction.modes():
                raise ArgumentTypeError('Unknown mode: {}'.format(values))
            setattr(namespace, self.dest, values)

    @staticmethod
    def __set_mode(mode: str) -> None:
        # Remove potentially set environments.
        COMPLUS_ENVIRONMENTS = [
            'COMPlus_JITMinOpts',
            'COMPlus_ReadyToRun',
            'COMPlus_TieredCompilation',
            'COMPlus_ZapDisable',
        ]
        for complus_environment in COMPLUS_ENVIRONMENTS:
            if complus_environment in environ:
                environ.pop(complus_environment)

        # Configure .NET Runtime
        if mode == CompilationAction.TIERED:
            environ['COMPlus_TieredCompilation'] = '1'
        elif mode == CompilationAction.NO_TIERING:
            environ['COMPlus_TieredCompilation'] = '0'
        elif mode == CompilationAction.FULLY_JITTED_NO_TIERING:
            environ['COMPlus_ReadyToRun'] = '0'
            environ['COMPlus_TieredCompilation'] = '0'
            environ['COMPlus_ZapDisable'] = '1'
        elif mode == CompilationAction.MIN_OPT:
            environ['COMPlus_JITMinOpts'] = '1'
            environ['COMPlus_TieredCompilation'] = '0'
        elif mode != CompilationAction.DEFAULT:
            raise ArgumentTypeError('Unknown mode: {}'.format(mode))

    @staticmethod
    def validate(usr_mode: str) -> str:
        '''Validate user input.'''
        requested_mode = None
        for mode in CompilationAction.modes():
            if usr_mode.casefold() == mode.casefold():
                requested_mode = mode
                break
        if not requested_mode:
            raise ArgumentTypeError('Unknown mode: {}'.format(usr_mode))
        CompilationAction.__set_mode(requested_mode)
        return requested_mode

    @staticmethod
    def modes() -> list:
        '''Available .NET Performance modes.'''
        return [
            CompilationAction.DEFAULT,
            CompilationAction.TIERED,
            CompilationAction.NO_TIERING,
            CompilationAction.FULLY_JITTED_NO_TIERING,
            CompilationAction.MIN_OPT
        ]

    @staticmethod
    def noenv() -> str:
        '''Default .NET performance mode.'''
        return CompilationAction.modes()[0]  # No environment set

    @staticmethod
    def help_text() -> str:
        '''Gets the help string describing the different compilation modes.'''
        return '''Different compilation modes that can be set to change the
        .NET compilation behavior. The default configurations have changed between
        releases of .NET. These flags enable ensuring consistency when running
        more than one runtime. The different modes are: {}: no
        environment variables are set; {}: tiering is enabled.
        {}: tiering is disabled, but includes R2R code, and it is useful for
        comparison against Tiered; {}: This is JIT-only, useful for comparison
        against Tiered and NoTier for changes to R2R code or tiering; {}: uses
        minopt-JIT for methods that do not have pregenerated code, and useful
        for startup time comparisons in scenario benchmarks that include a
        startup time measurement (probably not for microbenchmarks), probably
        not useful for a PR.'''.format(
            CompilationAction.DEFAULT,
            CompilationAction.TIERED,
            CompilationAction.NO_TIERING,
            CompilationAction.FULLY_JITTED_NO_TIERING,
            CompilationAction.MIN_OPT
        )


class CSharpProject:
    '''
    This is a class wrapper around the `dotnet` command line interface.
    Remark: It assumes dotnet is already in the PATH.
    '''

    def __init__(self, project: CSharpProjFile, bin_directory: str):
        if not project.file_name:
            raise TypeError('C# file name cannot be null.')
        if not project.working_directory:
            raise TypeError('C# working directory cannot be null.')
        if not bin_directory:
            raise TypeError('bin folder cannot be null.')

        self.__csproj_file = path.abspath(project.file_name)
        self.__working_directory = path.abspath(project.working_directory)
        self.__bin_directory = bin_directory

        if not path.isdir(self.__working_directory):
            raise ValueError(
                'Specified working directory: {}, does not exist.'.format(
                    self.__working_directory
                )
            )
        if not path.isfile(self.__csproj_file):
            raise ValueError(
                'Specified project file: {}, does not exist.'.format(
                    self.__csproj_file
                )
            )

    @property
    def working_directory(self) -> str:
        '''Gets the working directory for the dotnet process to be started.'''
        return self.__working_directory

    @property
    def csproj_file(self) -> str:
        '''Gets the project file to run the dotnet cli against.'''
        return self.__csproj_file

    @property
    def project_name(self) -> str:
        '''Gets the project name.'''
        return path.splitext(path.basename(self.__csproj_file))[0]

    @property
    def bin_path(self) -> str:
        '''Gets the directory in which the built binaries will be placed.'''
        return self.__bin_directory

    def restore(self, 
                packages_path: str, 
                verbose: bool,
                runtime_identifier: str = None) -> None:
        '''
        Calls dotnet to restore the dependencies and tools of the specified
        project.

        Keyword arguments:
            packages_path -- The directory to restore packages to.
        MSBuild arguments used to avoid "process cannot access the file":
            /p:UseSharedCompilation=false -- disable shared compilation
            /p:BuildInParallel=false -- disable parallel builds
            /m:1 -- don't spawn more than a single process
        '''
        if not packages_path:
            raise TypeError('Unspecified packages directory.')
        cmdline = [
            'dotnet', 'restore',
            self.csproj_file,
            '--packages', packages_path,
            '/p:UseSharedCompilation=false', '/p:BuildInParallel=false', '/m:1',
        ]

        if runtime_identifier:
            cmdline += ['--runtime', runtime_identifier]
            
        RunCommand(cmdline, verbose=verbose, retry=1).run(
            self.working_directory)

    def build(self,
              configuration: str,
              verbose: bool,
              packages_path: str,
              target_framework_monikers: list = None,
              output_to_bindir: bool = False,
              runtime_identifier: str = None,
              *args) -> None:
        '''Calls dotnet to build the specified project.'''
        if not target_framework_monikers:  # Build all supported frameworks.
            cmdline = [
                'dotnet', 'build',
                self.csproj_file,
                '--configuration', configuration,
                '--no-restore',
                "/p:NuGetPackageRoot={}".format(packages_path),
                '/p:UseSharedCompilation=false', '/p:BuildInParallel=false', '/m:1',
            ]

            if output_to_bindir:
                cmdline = cmdline + ['--output', self.__bin_directory]
            
            if runtime_identifier:
                cmdline = cmdline + ['--runtime', runtime_identifier]
            
            if args:
                cmdline = cmdline + list(args)
            
            RunCommand(cmdline, verbose=verbose).run(
                self.working_directory)

        else:  # Only build specified frameworks
            for target_framework_moniker in target_framework_monikers:
                cmdline = [
                    'dotnet', 'build',
                    self.csproj_file,
                    '--configuration', configuration,
                    '--framework', target_framework_moniker,
                    '--no-restore',
                    "/p:NuGetPackageRoot={}".format(packages_path),
                    '/p:UseSharedCompilation=false', '/p:BuildInParallel=false', '/m:1',
                ]

                if output_to_bindir:
                    cmdline = cmdline + ['--output', self.__bin_directory]

                if runtime_identifier:
                    cmdline = cmdline + ['--runtime', runtime_identifier]

                if args:
                    cmdline = cmdline + list(args)
                
                RunCommand(cmdline, verbose=verbose).run(
                    self.working_directory)
    @staticmethod
    def new(template: str,
            output_dir: str,
            bin_dir: str,
            verbose: bool,
            working_directory: str,
            force: bool = False,
            exename: str = None,
            language: str = None,
            no_https: bool = False,
            no_restore: bool = True
            ):
        '''
        Creates a new project with the specified template
        '''
        cmdline = [
            'dotnet', 'new',
            template,
            '--output', output_dir
        ]
        if no_restore:
            cmdline += ['--no-restore']

        if force:
            cmdline += ['--force']
        
        if exename:
            cmdline += ['--name', exename]

        if language:
            cmdline += ['--language', language]

        if no_https:
            cmdline += ['--no-https']

        RunCommand(cmdline, verbose=verbose).run(
            working_directory
        )
        # the file could be any project type. let's guess.
        project_type = 'csproj'
        if language == 'vb':
            project_type = 'vbproj'

        return CSharpProject(CSharpProjFile(path.join(output_dir, '%s.%s' % (exename or output_dir, project_type)),
                                            working_directory),
                             bin_dir)

    def publish(self,
                configuration: str,
                output_dir: str,
                verbose: bool,
                packages_path,
                target_framework_moniker: str = None,
                runtime_identifier: str = None,
                msbuildprops: list = None,
                *args
                ) -> None:
        '''
        Invokes publish on the specified project
        '''
        cmdline = [
            'dotnet', 'publish',
            self.csproj_file,
            '--configuration', configuration,
            '--output', output_dir,
            "/p:NuGetPackageRoot={}".format(packages_path),
            '/p:UseSharedCompilation=false', '/p:BuildInParallel=false', '/m:1'
        ]
        if runtime_identifier:
            cmdline += ['--runtime', runtime_identifier]

        if target_framework_moniker:
            cmdline += ['--framework', target_framework_moniker]

        if msbuildprops:
            cmdline = cmdline + msbuildprops

        if args:
            cmdline = cmdline + list(args)

        RunCommand(cmdline, verbose=verbose).run(
            self.working_directory
        )

    @staticmethod
    def __print_complus_environment() -> None:
        getLogger().info('-' * 50)
        getLogger().info('Dumping COMPlus/DOTNET environment:')
        COMPLUS_PREFIX = 'COMPlus'
        DOTNET_PREFIX = 'DOTNET'
        implementationDetails = [ 'DOTNET_CLI_TELEMETRY_OPTOUT', 'DOTNET_MULTILEVEL_LOOKUP', 'DOTNET_ROOT' ]
        for env in environ:
            if env[:len(COMPLUS_PREFIX)].lower() == COMPLUS_PREFIX.lower() or env[:len(DOTNET_PREFIX)].lower() == DOTNET_PREFIX.lower():
                if not (env.upper() in implementationDetails):
                    getLogger().info('  "%s=%s"', env, environ[env])
        getLogger().info('-' * 50)

    def run(self,
            configuration: str,
            target_framework_moniker: str,
            success_exit_codes: list,
            verbose: bool,
            *args) -> int:
        '''
        Calls dotnet to run a .NET project output.
        '''
        CSharpProject.__print_complus_environment()
        cmdline = [
            'dotnet', 'run',
            '--project', self.csproj_file,
            '--configuration', configuration,
            '--framework', target_framework_moniker,
            '--no-restore', '--no-build',
        ]

        if args:
            cmdline = cmdline + list(args)
        return RunCommand(cmdline, success_exit_codes, verbose=verbose).run(
            self.working_directory)


def get_framework_version(framework: str) -> str:
    groups = search(r".*(\d)\.(\d)$", framework)
    if not groups:
        raise ValueError("Unknown target framework: {}".format(framework))

    FrameworkVersion = namedtuple('FrameworkVersion', ['major', 'minor'])
    version = FrameworkVersion(int(groups.group(1)), int(groups.group(2)))

    return version


def get_base_path(dotnet_path: str = None) -> str:
    """Gets the dotnet Host version from the `dotnet --info` command."""
    if not dotnet_path:
        dotnet_path = 'dotnet'

    output = check_output([dotnet_path, '--info'])

    for line in output.splitlines():
        decoded_line = line.decode('utf-8')

        # The .NET Command Line Tools `--info` had a different output in 2.0
        # This line seems commons in all Cli, so we can use the base path to
        # get information about the .NET SDK/Runtime
        groups = search(r"^ +Base Path\: +(.+)$", decoded_line)
        if groups:
            break

    if not groups:
        raise RuntimeError(
            'Did not find "Base Path:" entry on the `dotnet --info` command'
        )

    return groups.group(1)

def get_sdk_path(dotnet_path: str = None) -> str:
    base_path = get_base_path(dotnet_path)
    sdk_path = path.abspath(path.join(base_path, '..'))
    return sdk_path

def get_dotnet_path() -> str:
    base_path = get_base_path(None)
    dotnet_path = path.abspath(path.join(base_path, '..', '..'))
    return dotnet_path


def get_dotnet_version(
        framework: str,
        dotnet_path: str = None,
        sdk_path: str = None) -> str:
    version = get_framework_version(framework)

    sdk_path = get_sdk_path(dotnet_path) if sdk_path is None else sdk_path

    sdks = [
        d for d in listdir(sdk_path) if path.isdir(path.join(sdk_path, d))
    ]
    sdks.sort(reverse=True)

    # Determine the SDK being used.
    # Attempt 1: Try to use exact match.
    sdk = next((f for f in sdks if f.startswith(
        "{}.{}".format(version.major, version.minor))), None)
    if not sdk:
        # Attempt 2: Increase the minor version by 1 and retry.
        sdk = next((f for f in sdks if f.startswith(
            "{}.{}".format(version.major, version.minor + 1))), None)
    if not sdk:
        sdk = next((f for f in sdks if f.startswith(
            "{}.{}".format('6', '0'))), None)
    if not sdk:
        raise RuntimeError(
            "Unable to determine the .NET SDK used for {}".format(framework)
        )

    return sdk


def get_dotnet_sdk(
        framework: str,
        dotnet_path: str = None,
        sdk: str = None) -> str:
    """Gets the dotnet Host commit sha from the `dotnet --info` command."""

    sdk_path = get_sdk_path(dotnet_path)
    sdk = get_dotnet_version(framework, dotnet_path,
                             sdk_path) if sdk is None else sdk

    with open(path.join(sdk_path, sdk, '.version')) as sdk_version_file:
        return sdk_version_file.readline().strip()
    raise RuntimeError("Unable to retrieve information about the .NET SDK.")


def get_repository(repository: str) -> Tuple[str, str]:
    url_path = urlparse(repository).path
    tokens = url_path.split("/")
    if len(tokens) != 3:
        raise ValueError('Unable to determine owner and repo from url.')
    owner = tokens[1]
    repo = tokens[2]

    return owner, repo


def get_commit_date(
        framework: str,
        commit_sha: str,
        repository: str = None
) -> str:
    '''
    Gets the .NET Core committer date using the GitHub Web API from the
    repository.
    '''
    if not framework:
        raise ValueError('Target framework was not defined.')
    if not commit_sha:
        raise ValueError('.NET Commit sha was not defined.')

    url = None
    urlformat = 'https://api.github.com/repos/%s/%s/commits/%s'
    if repository is None:
        # The origin of the repo where the commit belongs to has changed
        # between release. Here we attempt to naively guess the repo.
        core_sdk_frameworks = ChannelMap.get_supported_frameworks()
        core_sdk_frameworks.remove('netcoreapp2.1')
        repo = 'core-sdk' if framework  in core_sdk_frameworks else 'cli'
        url = urlformat % ('dotnet', repo, commit_sha)
    else:
        owner, repo = get_repository(repository)
        url = urlformat % (owner, repo, commit_sha)

    build_timestamp = None
    sleep_time = 10 # Start with 10 second sleep timer
    for retrycount in range(5):
        try:
            with urlopen(url) as response:
                getLogger().info("Commit: %s", url)
                item = loads(response.read().decode('utf-8'))
                build_timestamp = item['commit']['committer']['date']
                break
        except URLError as error:
            getLogger().warning(f"URL Error trying to get commit date from {url}; Reason: {error.reason}; Attempt {retrycount}")
            sleep(sleep_time)
            sleep_time = sleep_time * 2

    if not build_timestamp:
        raise RuntimeError(
            'Could not get timestamp for commit %s' % commit_sha)
    return build_timestamp

def get_project_name(csproj_file: str) -> str:
    '''
    Gets the project name from the csproj file path
    '''
    return path.splitext(path.basename(path.abspath(csproj_file)))[0]

def get_main_assembly_path(
        bin_directory: str,
        project_name: str) -> str:
    '''
    Gets the main assembly path, as {project_name}.dll, or .exe
    '''
    exe=path.join(bin_directory, project_name + '.exe')
    if path.exists(exe):
        return exe

    dll=path.join(bin_directory, project_name + '.dll')
    if path.exists(dll):
        return dll

    raise ValueError(
        'Unable to find main assembly - {} or {} in {}'.format(exe, dll, bin_directory))

def get_build_directory(
        bin_directory: str,
        project_name: str,
        configuration: str,
        target_framework_moniker: str) -> None:
    '''
    Gets the  output directory where the built artifacts are in with
    respect to the specified bin_directory.
    '''
    with push_dir(bin_directory):
        return path.join(
            bin_directory,
            __find_build_directory(
                configuration=configuration,
                project_name=project_name,
                target_framework_moniker=target_framework_moniker,
            )
        )


def __find_build_directory(
        configuration: str,
        project_name: str,
        target_framework_moniker: str) -> str:
    '''
    Attempts to get the output directory where the built artifacts are in
    with respect to the current working directory.
    '''
    pattern = '**/{ProjectName}/**/{Configuration}/{TargetFramework}'.format(
        ProjectName=project_name,
        Configuration=configuration,
        TargetFramework=target_framework_moniker
    )

    for path_name in iglob(pattern, recursive=True):
        if path.isdir(path_name):
            return path_name

    raise ValueError(
        'Unable to determine directory for the specified pattern.')


def __get_directory(architecture: str) -> str:
    '''Gets the default directory where dotnet is to be installed.'''
    return path.join(get_tools_directory(), 'dotnet', architecture)


def remove_dotnet(architecture: str) -> str:
    '''
    Removes the dotnet installed in the tools directory associated with the
    specified architecture.
    '''
    rmtree(__get_directory(architecture))

def shutdown_server(verbose:bool) -> None:
    '''
    Shuts down the dotnet server
    '''
    cmdline = [
        'dotnet', 'build-server', 'shutdown'
    ]
    try:
        RunCommand(cmdline, verbose=verbose).run(
            get_repo_root_path())
    except CalledProcessError:
        # Shutting down the build server can fail (see https://github.com/dotnet/sdk/issues/10573), so we'll do it by hand also
        # using os.system dirctly here instead of RunCommand as we don't want logging, and don't care if these fail.
        if platform == 'win32':
            system('TASKKILL /F /T /IM dotnet.exe 2> nul || TASKKILL /F /T /IM VSTest.Console.exe 2> nul || TASKKILL /F /T /IM msbuild.exe 2> nul')
        else:
            system('killall -9 dotnet 2> /dev/null || killall -9 VSTest.Console 2> /dev/null || killall -9 msbuild 2> /dev/null')


def install(
        architecture: str,
        channels: list,
        versions: str,
        verbose: bool,
        install_dir: str = None) -> None:
    '''
    Downloads dotnet cli into the tools folder.
    '''
    __log_script_header("Downloading DotNet Cli")

    if not install_dir:
        install_dir = __get_directory(architecture)
    if not path.exists(install_dir):
        makedirs(install_dir)

    getLogger().info("DotNet Install Path: '%s'", install_dir)

    # Download appropriate dotnet install script
    dotnetInstallScriptExtension = '.ps1' if platform == 'win32' else '.sh'
    dotnetInstallScriptName = 'dotnet-install' + dotnetInstallScriptExtension
    url = 'https://dot.net/v1/'  
    dotnetInstallScriptUrl = url + dotnetInstallScriptName

    dotnetInstallScriptPath = path.join(install_dir, dotnetInstallScriptName)

    getLogger().info('Downloading %s', dotnetInstallScriptUrl)
    count = 0
    while count < 3:
        try:
            with urlopen(dotnetInstallScriptUrl, context=ssl._create_unverified_context()) as response:
                if "html" in response.info()['Content-Type']:
                    count = count + 1
                    sleep(1) # sleep one second
                    continue
                with open(dotnetInstallScriptPath, 'wb') as outfile:
                    outfile.write(response.read())
                    break
        except Exception:
            count = count + 1
            sleep(1)
            continue

    if count == 3:
        getLogger().error("Fatal error: could not download dotnet-install script")
        raise Exception("Fatal error: could not download dotnet-install script")

    if platform != 'win32':
        chmod(dotnetInstallScriptPath, S_IRWXU)

    dotnetInstallInterpreter = [
        'powershell.exe',
        '-NoProfile',
        '-ExecutionPolicy', 'Bypass',
        dotnetInstallScriptPath
    ] if platform == 'win32' else [dotnetInstallScriptPath]

    # If Version is supplied, pull down the specified version

    common_cmdline_args = dotnetInstallInterpreter + [
        '-InstallDir', install_dir,
        '-Architecture', architecture
    ]

    # Install Runtime/SDKs
    if versions:
        for version in versions:
            cmdline_args = common_cmdline_args + ['-Version', version]
            RunCommand(cmdline_args, verbose=verbose, retry=1).run(
                get_repo_root_path()
            )

    # Only check channels if versions are not supplied.
    # When we supply a version, but still pull down with -Channel, we will use
    # whichever sdk is newer. So if we are trying to check an older version,
    # or if there is a new version between when we start a run and when we actually
    # run, we will be testing the "wrong" version, ie, not the version we specified.
    if (not versions) and channels:
        for channel in channels:
            cmdline_args = common_cmdline_args + ['-Channel', ChannelMap.get_branch(channel)]
            if ChannelMap.get_quality_from_channel(channel) is not None:
                cmdline_args += ['-Quality', ChannelMap.get_quality_from_channel(channel)]
            RunCommand(cmdline_args, verbose=verbose, retry=1).run(
                get_repo_root_path()
            )

    setup_dotnet(install_dir)

def setup_dotnet(dotnet_path: str):
    # Set DotNet Cli environment variables.
    environ['DOTNET_CLI_TELEMETRY_OPTOUT'] = '1'
    environ['DOTNET_MULTILEVEL_LOOKUP'] = '0'
    environ['UseSharedCompilation'] = 'false'
    environ['DOTNET_ROOT'] = dotnet_path

    # Add installed dotnet cli to PATH
    environ["PATH"] = dotnet_path + pathsep + environ["PATH"]

    # If we have copied dotnet from a different machine, then it may not be
    # marked as executable. Fix this.
    if platform != 'win32':
        chmod(path.join(dotnet_path, 'dotnet'), S_IRWXU)

def __add_arguments(parser: ArgumentParser) -> ArgumentParser:
    '''
    Adds new arguments to the specified ArgumentParser object.
    '''

    if not isinstance(parser, ArgumentParser):
        raise TypeError('Invalid parser.')

    SUPPORTED_ARCHITECTURES = [
        'x64',
        'x86',
        'arm',
        'arm64',
    ]
    parser.add_argument(
        '--architecture',
        dest='architecture',
        required=False,
        default=get_machine_architecture(),
        choices=SUPPORTED_ARCHITECTURES,
        help='Architecture of DotNet Cli binaries to be installed.'
    )

    parser.add_argument(
        '--dotnet-versions',
        dest="dotnet_versions",
        required=False,
        nargs='+',
        default=[],
        action=VersionsAction,
        help='Version of the dotnet cli to install in the A.B.C format'
    )

    return parser


def add_arguments(parser: ArgumentParser) -> ArgumentParser:
    '''
    Adds new arguments to the specified ArgumentParser object.
    '''

    parser = __add_arguments(parser)

    # .NET Compilation modes.
    parser.add_argument(
        '--dotnet-compilation-mode',
        dest='dotnet_compilation_mode',
        required=False,
        action=CompilationAction,
        choices=CompilationAction.modes(),
        default=CompilationAction.noenv(),
        type=CompilationAction.validate,
        help='{}'.format(CompilationAction.help_text())
    )

    return parser


def __process_arguments(args: list):
    parser = ArgumentParser(
        description='DotNet Cli wrapper.',
        allow_abbrev=False
    )
    subparsers = parser.add_subparsers(
        title='Subcommands',
        description='Supported DotNet Cli subcommands',
        dest='install',
    )
    subparsers.required = True

    install_parser = subparsers.add_parser(
        'install',
        allow_abbrev=False,
        help='Installs dotnet cli',
    )

    install_parser.add_argument(
        '--channels',
        dest='channels',
        required=False,
        nargs='+',
        default=['main'],
        choices= ChannelMap.get_supported_channels(),
        help='Download DotNet Cli from the Channel specified.'
    )

    install_parser = __add_arguments(install_parser)

    # private install arguments.
    install_parser.add_argument(
        '--install-dir',
        dest='install_dir',
        required=False,
        type=str,
        help='''Path to where to install dotnet. Note that binaries will be '''
             '''placed directly in a given directory.''',
    )
    install_parser.add_argument(
        '-v', '--verbose',
        required=False,
        default=False,
        action='store_true',
        help='Turns on verbosity (default "False")',
    )
    return parser.parse_args(args)


def __main(args: list) -> int:
    validate_supported_runtime()
    args = __process_arguments(args)
    setup_loggers(verbose=args.verbose)
    install(
        architecture=args.architecture,
        channels=args.channels,
        versions=args.dotnet_versions,
        verbose=args.verbose,
        install_dir=args.install_dir,
    )


if __name__ == "__main__":
    __main(argv[1:])
