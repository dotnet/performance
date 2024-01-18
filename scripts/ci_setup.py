#!/usr/bin/env python3

from argparse import ArgumentParser, ArgumentTypeError
from logging import getLogger

import os
import sys
import datetime

from subprocess import check_output
from typing import Any, Optional, List

from performance.common import get_machine_architecture, get_repo_root_path, set_environment_variable
from performance.common import get_tools_directory
from performance.common import push_dir
from performance.common import validate_supported_runtime
from performance.logger import setup_loggers
from channel_map import ChannelMap

import dotnet

def init_tools(
        architecture: str,
        dotnet_versions: List[str],
        channel: str,
        verbose: bool,
        install_dir: Optional[str]=None) -> None:
    '''
    Install tools used by this repository into the tools folder.
    This function writes a semaphore file when tools have been successfully
    installed in order to avoid reinstalling them on every rerun.
    '''
    getLogger().info('Installing tools.')

    dotnet.install(
        architecture=architecture,
        channels=[channel],
        versions=dotnet_versions,
        verbose=verbose,
        install_dir=install_dir
    )

def add_arguments(parser: ArgumentParser) -> ArgumentParser:
    '''Adds new arguments to the specified ArgumentParser object.'''

    if not isinstance(parser, ArgumentParser):
        raise TypeError('Invalid parser.')

    # Download DotNet Cli
    dotnet.add_arguments(parser)

    parser.add_argument(
        '--channel',
        dest='channel',
        required=True,
        choices=ChannelMap.get_supported_channels(),
        type=str,
        help='Channel to download product from'
    )
    parser.add_argument(
        '--no-dynamic-pgo',
        dest='pgo_status',
        required=False,
        action='store_const',
        const='nodynamicpgo'
    )
    parser.add_argument(
        '--physical-promotion',
        dest='physical_promotion_status',
        required=False,
        action='store_const',
        const='physicalpromotion'
    )
    parser.add_argument(
        '--no-r2r',
        dest='r2r_status',
        required=False,
        action='store_const',
        const='nor2r'
    )
    parser.add_argument(
        '--experiment-name',
        dest='experiment_name',
        required=False,
        type=str
    )
    parser.add_argument(
        '--branch',
        dest='branch',
        required=False,
        type=str,
        help='Product branch.'
    )
    parser.add_argument(
        '--commit-sha',
        dest='commit_sha',
        required=False,
        type=str,
        help='Product commit sha.'
    )
    parser.add_argument(
        '--commit-time',
        dest='commit_time',
        required=False,
        type=str,
        help='Product commit time. Format: %Y-%m-%d %H:%M:%S %z'
    )
    parser.add_argument(
        '--local-build',
        dest="local_build",
        required=False,
        action='store_true',
        default=False,
        help='Whether the test is being run against a local build'
    )
    parser.add_argument(
        '--repository',
        dest='repository',
        required=False,
        type=str,
        help='Product repository.'
    )
    parser.add_argument(
        '--queue',
        dest='queue',
        default='testQueue',
        required=False,
        type=str,
        help='Test queue'
    )
    parser.add_argument(
        '--build-number',
        dest='build_number',
        default='1234.1',
        required=False,
        type=str,
        help='Build number'
    )
    
    parser.add_argument(
        '--locale',
        dest='locale',
        default='en-US',
        required=False,
        type=str,
        help='Locale'
    )
    parser.add_argument(
        '--perf-hash',
        dest='perf_hash',
        default='testSha',
        required=False,
        type=str,
        help='Sha of the performance repo'
    )

    parser.add_argument(
        '--get-perf-hash',
        dest="get_perf_hash",
        required=False,
        action='store_true',
        default=False,
        help='Discover the hash of the performance repository'
    )

    def __valid_file_path(file_path: str) -> str:
        '''Verifies that specified file path exists.'''
        file_path = os.path.abspath(file_path)
        if not os.path.isfile(file_path):
            raise ArgumentTypeError('{} does not exist.'.format(file_path))
        return file_path

    parser.add_argument(
        '--cli',
        dest='cli',
        required=False,
        type=__valid_file_path,
        help='Full path to dotnet.exe',
    )

    parser.add_argument(
        '--output-file',
        dest='output_file',
        required=False,
        default=os.path.join(get_tools_directory(),'machine-setup'),
        type=str,
        help='Filename to write the setup script to'
    )

    parser.add_argument(
        '--install-dir',
        dest='install_dir',
        required=False,
        type=str,
        help='Directory to install dotnet to'
    )

    parser.add_argument(
        '--not-in-lab',
        dest='not_in_lab',
        required=False,
        action='store_true',
        default=False,
        help='Indicates that this is not running in perflab'
    )

    def __is_valid_dotnet_path(dp: str) -> str:
        if not os.path.isdir(dp):
            raise ArgumentTypeError('Directory {} does not exist'.format(dp))
        if not os.path.isfile(os.path.join(dp, 'dotnet')):
            raise ArgumentTypeError('Could not find dotnet in {}'.format(dp))
        return dp

    parser.add_argument(
        '--dotnet-path',
        dest='dotnet_path',
        required=False,
        type=__is_valid_dotnet_path,
        help='Path to a custom dotnet'
    )

    # Generic arguments.
    parser.add_argument(
        '-q', '--quiet',
        required=False,
        default=False,
        action='store_true',
        help='Turns off verbosity.',
    )

    parser.add_argument(
        '--build-configs',
        dest="build_configs",
        required=False,
        nargs='+',
        default=[],
        help='Configurations used in the build in key=value format'
    )

    parser.add_argument(
        '--maui-version',
        dest='maui_version',
        default='',
        required=False,
        type=str,
        help='Version of Maui used to build app packages'
    )

    parser.add_argument(
        '--affinity',
        required=False,
        help='Affinity value set as PERFLAB_DATA_AFFINITY. In scenarios, this value is directly used to set affinity. In benchmark jobs, affinity is set in benchmark_jobs.yml via BDN command line arg'
    )

    parser.add_argument(
        '--run-env-vars',
        nargs='*',
        help='Environment variables to set on the machine in the form of key=value key2=value2. Will also be saved to additional data'
    )

    parser.add_argument(
        '--target-windows',
        dest='target_windows',
        required=False,
        action='store_true',
        default=False,
        help='Will it run on a Windows Helix Queue?'
    )

    return parser

def __process_arguments(args: List[str]):
    parser = ArgumentParser(
        description='Tool to generate a machine setup script',
        allow_abbrev=False,
        # epilog=os.linesep.join(__doc__.splitlines())
        epilog=__doc__,
    )
    add_arguments(parser)
    return parser.parse_args(args)


class CiSetupArgs:
    def __init__(
            self,
            channel: str,
            quiet: bool = False,
            commit_sha: Optional[str] = None,
            repository: Optional[str] = None,
            architecture: str = get_machine_architecture(),
            dotnet_path: Optional[str] = None,
            dotnet_versions: List[str] = [],
            install_dir: Optional[str] = None,
            build_configs: List[str] = [],
            pgo_status: Optional[str] = None,
            get_perf_hash: bool = False,
            perf_hash: str = 'testSha',
            cli: Optional[str] = None,
            commit_time: Optional[str] = None,
            local_build: bool = False,
            branch: Optional[str] = None,
            output_file: str = os.path.join(get_tools_directory(), 'machine-setup'),
            not_in_lab: bool = False,
            queue: str = 'testQueue',
            build_number: str = '1234.1',
            locale: str = 'en-US',
            maui_version: str = '',
            affinity: Optional[str] = None,
            run_env_vars: Optional[List[str]] = None,
            target_windows: bool = True,
            physical_promotion_status: Optional[str] = None,
            r2r_status: Optional[str] = None,
            experiment_name: Optional[str] = None):
        self.channel = channel
        self.quiet = quiet
        self.commit_sha = commit_sha
        self.repository = repository
        self.architecture = architecture
        self.dotnet_path = dotnet_path
        self.dotnet_versions = dotnet_versions
        self.install_dir = install_dir
        self.build_configs = build_configs
        self.pgo_status = pgo_status
        self.get_perf_hash = get_perf_hash
        self.perf_hash = perf_hash
        self.cli = cli
        self.commit_time = commit_time
        self.local_build = local_build
        self.branch = branch
        self.output_file = output_file
        self.not_in_lab = not_in_lab
        self.queue = queue
        self.build_number = build_number
        self.locale = locale
        self.maui_version = maui_version
        self.affinity = affinity
        self.run_env_vars = run_env_vars
        self.target_windows = target_windows
        self.physical_promotion_status = physical_promotion_status
        self.r2r_status = r2r_status
        self.experiment_name = experiment_name

def main(args: Any):
    verbose = not args.quiet
    setup_loggers(verbose=verbose)

    # if repository is not set, then we are doing a core-sdk in performance repo run
    # if repository is set, user needs to supply the commit_sha
    if not ((args.commit_sha is None) == (args.repository is None)):
        raise ValueError('Either both commit_sha and repository should be set or neither')
    
    # for CI pipelines, use the agent OS
    if not args.local_build:
        args.target_windows = sys.platform == 'win32'

    # Acquire necessary tools (dotnet)
    # For arm64 runs, download the x64 version so we can get the information we need, but set all variables
    # as if we were running normally. This is a workaround due to the fact that arm64 binaries cannot run
    # in the cross containers, so we are running the ci setup script in a normal ubuntu container
    architecture = 'x64' if args.architecture == 'arm64' else args.architecture

    if not args.dotnet_path:
        if args.architecture == 'arm64':
            init_tools(
                architecture='arm64',
                dotnet_versions=args.dotnet_versions,
                channel=args.channel,
                verbose=verbose
            )
            
        init_tools(
            architecture=architecture,
            dotnet_versions=args.dotnet_versions,
            channel=args.channel,
            verbose=verbose,
            install_dir=args.install_dir
        )
    else:
        dotnet.setup_dotnet(args.dotnet_path)

    # dotnet --info
    dotnet.info(verbose=verbose)

    # When running on internal repos, the repository comes to us incorrectly
    # (ie https://github.com/dotnet-coreclr). Replace dashes with slashes in that case.
    repo_url = None if args.repository is None else args.repository.replace('-','/')

    variable_format = 'set "%s=%s"\n' if args.target_windows else 'export %s="%s"\n'
    path_variable = 'set PATH=%s;%%PATH%%\n' if args.target_windows else 'export PATH=%s:$PATH\n'
    which = 'where dotnet\n' if args.target_windows else 'which dotnet\n'
    dotnet_path = '%HELIX_CORRELATION_PAYLOAD%\\dotnet' if args.target_windows else '$HELIX_CORRELATION_PAYLOAD/dotnet'
    owner, repo = ('dotnet', 'core-sdk') if repo_url is None else (dotnet.get_repository(repo_url))
    config_string = ';'.join(args.build_configs) if args.target_windows else "%s" % ';'.join(args.build_configs)
    pgo_config = ''
    physical_promotion_config = ''
    r2r_config = ''
    experiment_config = ''
    showenv = 'set' if args.target_windows else 'printenv'

    if args.pgo_status == 'nodynamicpgo':
        pgo_config = variable_format % ('DOTNET_TieredPGO', '0')

    if args.physical_promotion_status == 'physicalpromotion':
        physical_promotion_config = variable_format % ('DOTNET_JitEnablePhysicalPromotion', '1')

    if args.r2r_status == 'nor2r':
        r2r_config = variable_format % ('DOTNET_ReadyToRun', '0')

    if args.experiment_name == "crossblocklocalassertionprop":
        experiment_config = variable_format % ('DOTNET_JitEnableCrossBlockLocalAssertionProp', '1')
    elif args.experiment_name == "gdv3":
        experiment_config = variable_format % ('DOTNET_JitGuardedDevirtualizationMaxTypeChecks', '3')

    output = ''

    with push_dir(get_repo_root_path()):
        output = check_output(['git', 'rev-parse', 'HEAD'])

    decoded_lines: List[str] = []

    for line in output.splitlines():
        decoded_lines = decoded_lines + [line.decode('utf-8')]

    decoded_output = ''.join(decoded_lines)

    perfHash = decoded_output if args.get_perf_hash else args.perf_hash

    framework = ChannelMap.get_target_framework_moniker(args.channel)

    # if the extension is already present, don't add it
    output_file = args.output_file
    if not output_file.endswith("cmd") and not output_file.endswith(".sh"):
        extension = ".cmd" if args.target_windows else ".sh"
        output_file += extension

    if not framework.startswith('net4'):
        target_framework_moniker = dotnet.FrameworkAction.get_target_framework_moniker(framework)
        dotnet_version = dotnet.get_dotnet_version(target_framework_moniker, args.cli) if args.dotnet_versions == [] else args.dotnet_versions[0]
        commit_sha = dotnet.get_dotnet_sdk(target_framework_moniker, args.cli) if args.commit_sha is None else args.commit_sha

        if args.local_build:
            source_timestamp = datetime.datetime.utcnow().strftime('%Y-%m-%dT%H:%M:%SZ')
        elif(args.commit_time is not None):
            try:
                parsed_timestamp = datetime.datetime.strptime(args.commit_time, '%Y-%m-%d %H:%M:%S %z').astimezone(datetime.timezone.utc)
                source_timestamp = parsed_timestamp.strftime('%Y-%m-%dT%H:%M:%SZ')
            except ValueError:
                getLogger().warning('Invalid commit_time format. Please use YYYY-MM-DD HH:MM:SS +/-HHMM. Attempting to get commit time from api.github.com.')
                source_timestamp = dotnet.get_commit_date(target_framework_moniker, commit_sha, repo_url)
        else:
            source_timestamp = dotnet.get_commit_date(target_framework_moniker, commit_sha, repo_url)

        branch = ChannelMap.get_branch(args.channel) if not args.branch else args.branch

        getLogger().info("Writing script to %s" % output_file)
        dir_path = os.path.dirname(output_file)
        if not os.path.isdir(dir_path):
            os.mkdir(dir_path)

        perflab_upload_token = os.environ.get('PerfCommandUploadToken' if args.target_windows else 'PerfCommandUploadTokenLinux')
        run_name = os.environ.get("PERFLAB_RUNNAME")

        with open(output_file, 'w') as out_file:
            out_file.write(which)
            out_file.write(pgo_config)
            out_file.write(physical_promotion_config)
            out_file.write(r2r_config)
            out_file.write(experiment_config)
            out_file.write(variable_format % ('PERFLAB_INLAB', '0' if args.not_in_lab else '1'))
            out_file.write(variable_format % ('PERFLAB_REPO', '/'.join([owner, repo])))
            out_file.write(variable_format % ('PERFLAB_BRANCH', branch))
            out_file.write(variable_format % ('PERFLAB_PERFHASH', perfHash))
            out_file.write(variable_format % ('PERFLAB_HASH', commit_sha))
            out_file.write(variable_format % ('PERFLAB_QUEUE', args.queue))
            out_file.write(variable_format % ('PERFLAB_BUILDNUM', args.build_number))
            out_file.write(variable_format % ('PERFLAB_BUILDARCH', args.architecture))
            out_file.write(variable_format % ('PERFLAB_LOCALE', args.locale))
            out_file.write(variable_format % ('PERFLAB_BUILDTIMESTAMP', source_timestamp))
            out_file.write(variable_format % ('PERFLAB_CONFIGS', config_string))
            out_file.write(variable_format % ('DOTNET_VERSION', dotnet_version))
            out_file.write(variable_format % ('PERFLAB_TARGET_FRAMEWORKS', framework))
            out_file.write(variable_format % ('DOTNET_CLI_TELEMETRY_OPTOUT', '1'))
            out_file.write(variable_format % ('DOTNET_MULTILEVEL_LOOKUP', '0'))
            out_file.write(variable_format % ('UseSharedCompilation', 'false'))
            out_file.write(variable_format % ('DOTNET_ROOT', dotnet_path))
            out_file.write(variable_format % ('MAUI_VERSION', args.maui_version))
            if perflab_upload_token is not None:
                out_file.write(variable_format % ('PERFLAB_UPLOAD_TOKEN', perflab_upload_token))
            if run_name is not None:
                out_file.write(variable_format % ('PERFLAB_RUNNAME', run_name))
            out_file.write(path_variable % dotnet_path)
            if args.affinity:
                out_file.write(variable_format % ('PERFLAB_DATA_AFFINITY', args.affinity))
            if args.run_env_vars:
                for env_var in args.run_env_vars:
                    key, value = env_var.split('=', 1) 
                    out_file.write(variable_format % (key, value))
                    out_file.write(variable_format % ("PERFLAB_DATA_" + key, value))
            out_file.write(showenv)
    else:
        with open(output_file, 'w') as out_file:
            out_file.write(variable_format % ('PERFLAB_INLAB', '0'))
            out_file.write(variable_format % ('PERFLAB_TARGET_FRAMEWORKS', framework))
            out_file.write(path_variable % dotnet_path)
    
    # The '_Framework' is needed for specifying frameworks in proj files and for building tools later in the pipeline
    set_environment_variable('PERFLAB_Framework', framework)

def __main(argv: List[str]):
    validate_supported_runtime()
    args = __process_arguments(argv)
    main(CiSetupArgs(**vars(args)))


if __name__ == "__main__":
    __main(sys.argv[1:])
