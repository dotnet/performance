#!/usr/bin/env python3

from argparse import ArgumentParser, ArgumentTypeError
from logging import getLogger

import os
import sys

from subprocess import check_output

from performance.common import get_repo_root_path
from performance.common import get_tools_directory
from performance.common import push_dir
from performance.common import validate_supported_runtime
from performance.logger import setup_loggers
from channel_map import ChannelMap

import dotnet
import micro_benchmarks

global_extension = ".cmd" if sys.platform == 'win32' else '.sh'

def init_tools(
        architecture: str,
        dotnet_versions: str,
        channel: str,
        verbose: bool,
        install_dir: str=None) -> None:
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
    micro_benchmarks.add_arguments(parser)

    parser.add_argument(
        '--channel',
        dest='channel',
        required=True,
        choices=ChannelMap.get_supported_channels(),
        type=str,
        help='Channel to download product from'
    )
    parser.add_argument(
        '--no-pgo',
        dest='pgo_status',
        required=False,
        action='store_const',
        const='nopgo'
    )
    parser.add_argument(
        '--dynamic-pgo',
        dest='pgo_status',
        required=False,
        action='store_const',
        const='dynamicpgo'
    )
    parser.add_argument(
        '--full-pgo',
        dest='pgo_status',
        required=False,
        action='store_const',
        const='fullpgo'
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

    parser.add_argument(
        '--output-file',
        dest='output_file',
        required=False,
        default=os.path.join(get_tools_directory(),'machine-setup' + global_extension),
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
    return parser

def __process_arguments(args: list):
    parser = ArgumentParser(
        description='Tool to generate a machine setup script',
        allow_abbrev=False,
        # epilog=os.linesep.join(__doc__.splitlines())
        epilog=__doc__,
    )
    add_arguments(parser)
    return parser.parse_args(args)

def __write_pipeline_variable(name: str, value: str):
    # Create a variable in the build pipeline
    getLogger().info("Writing pipeline variable %s with value %s" % (name, value))
    print('##vso[task.setvariable variable=%s]%s' % (name, value))

def __main(args: list) -> int:
    validate_supported_runtime()
    args = __process_arguments(args)
    verbose = not args.quiet
    setup_loggers(verbose=verbose)

    # if repository is not set, then we are doing a core-sdk in performance repo run
    # if repository is set, user needs to supply the commit_sha
    if not ((args.commit_sha is None) == (args.repository is None)):
        raise ValueError('Either both commit_sha and repository should be set or neither')

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

    variable_format = 'set %s=%s\n' if sys.platform == 'win32' else 'export %s=%s\n'
    path_variable = 'set PATH=%s;%%PATH%%\n' if sys.platform == 'win32' else 'export PATH=%s:$PATH\n'
    which = 'where dotnet\n' if sys.platform == 'win32' else 'which dotnet\n'
    dotnet_path = '%HELIX_CORRELATION_PAYLOAD%\dotnet' if sys.platform == 'win32' else '$HELIX_CORRELATION_PAYLOAD/dotnet'
    owner, repo = ('dotnet', 'core-sdk') if args.repository is None else (dotnet.get_repository(repo_url))
    config_string = ';'.join(args.build_configs) if sys.platform == 'win32' else '"%s"' % ';'.join(args.build_configs)
    pgo_config = ''
    showenv = 'set' if sys.platform == 'win32' else 'printenv'

    if args.pgo_status == 'nopgo':
        pgo_config = variable_format % ('COMPlus_TC_QuickJitForLoops', '1')
        pgo_config += variable_format % ('COMPlus_TC_OnStackReplacement','1')
    elif args.pgo_status == 'dynamicpgo':
        pgo_config = variable_format % ('COMPlus_TieredPGO', '1')
    elif args.pgo_status == 'fullpgo':
        pgo_config = variable_format % ('COMPlus_TieredPGO', '1')
        pgo_config += variable_format % ('COMPlus_ReadyToRun','0')
        pgo_config += variable_format % ('COMPlus_TC_QuickJitForLoops','1')

    output = ''

    with push_dir(get_repo_root_path()):
        output = check_output(['git', 'rev-parse', 'HEAD'])

    decoded_lines = []

    for line in output.splitlines():
        decoded_lines = decoded_lines + [line.decode('utf-8')]

    decoded_output = ''.join(decoded_lines)

    perfHash = decoded_output if args.get_perf_hash else args.perf_hash

    framework = ChannelMap.get_target_framework_moniker(args.channel)
    if not framework.startswith('net4'):
        target_framework_moniker = dotnet.FrameworkAction.get_target_framework_moniker(framework)
        dotnet_version = dotnet.get_dotnet_version(target_framework_moniker, args.cli) if args.dotnet_versions == [] else args.dotnet_versions[0]
        commit_sha = dotnet.get_dotnet_sdk(target_framework_moniker, args.cli) if args.commit_sha is None else args.commit_sha
        source_timestamp = dotnet.get_commit_date(target_framework_moniker, commit_sha, repo_url)

        branch = ChannelMap.get_branch(args.channel) if not args.branch else args.branch

        getLogger().info("Writing script to %s" % args.output_file)
        dir_path = os.path.dirname(args.output_file)
        if not os.path.isdir(dir_path):
            os.mkdir(dir_path)

        with open(args.output_file, 'w') as out_file:
            out_file.write(which)
            out_file.write(pgo_config)
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
            out_file.write(path_variable % dotnet_path)
            out_file.write(showenv)
    else:
        with open(args.output_file, 'w') as out_file:
            out_file.write(variable_format % ('PERFLAB_INLAB', '0'))
            out_file.write(variable_format % ('PERFLAB_TARGET_FRAMEWORKS', framework))
            out_file.write(path_variable % dotnet_path)
    
    # The '_Framework' is needed for specifying frameworks in proj files and for building tools later in the pipeline
    __write_pipeline_variable('PERFLAB_Framework', framework)




if __name__ == "__main__":
    __main(sys.argv[1:])
