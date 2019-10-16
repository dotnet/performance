'''
Commands and utilities for pre.py scripts
'''

import sys
from argparse import ArgumentParser
from dotnet import CSharpProject, CSharpProjFile
from shared import const
from performance.common import get_packages_directory

BUILD = 'build'
PUBLISH = 'publish'
RESTORE = 'restore'
DEBUG = 'Debug'
RELEASE = 'Release'

OPERATIONS = (BUILD,
              RESTORE,
              PUBLISH
             )

class PreCommands:
    '''
    Handles building and publishing
    '''

    def __init__(self):
        self.project: CSharpProject
        self.projectfile: CSharpProjFile
        parser = ArgumentParser()
        subparsers = parser.add_subparsers(title='Operations', 
                                           description='Common preperation steps for perf tests.',
                                           required=True,
                                           dest='operation')
        restore_parser = subparsers.add_parser(RESTORE, help='Restores the project')

        build_parser = subparsers.add_parser(BUILD, help='Builds the project')
        self.add_common_arguments(build_parser)

        publish_parser = subparsers.add_parser(PUBLISH, help='Publishes the project')
        self.add_common_arguments(publish_parser)

        args = parser.parse_args()
        self.configuration = args.configuration
        self.operation = args.operation

    def new(self, template: str, output_dir: str, bin_dir: str, exename: str, working_directory: str):
        'makes a new app with the given template'
        self.project = CSharpProject.new(template=template,
                                 output_dir=output_dir,
                                 bin_dir=bin_dir,
                                 exename=exename,
                                 working_directory=working_directory,
                                 force=True,
                                 verbose=True)
        return self

    def add_common_arguments(self, parser: ArgumentParser):
        "Options that are common across many 'dotnet' commands"
        parser.add_argument('-c', '--configuration',
                            dest='configuration',
                            choices=[DEBUG, RELEASE],
                            metavar='config')
        parser.add_argument('-f', '--framework',
                            dest='framework',
                            metavar='framework')
        parser.add_argument('-r', '--runtime',
                            dest='runtime',
                            metavar='runtime')
        parser.add_argument('--msbuild',
                            help='Flags passed through to msbuild',
                            dest='msbuild',
                            metavar='/p:Foo=Bar;/p:Baz=Blee;...')
        parser.set_defaults(configuration=RELEASE)

    def existing(self, projectfile: str):
        'create a project from existing project file'
        csproj = CSharpProjFile(projectfile, sys.path[0])
        self.project = CSharpProject(csproj, const.BINDIR)
        return self

    def execute(self):
        'Parses args and runs precommands'
        if self.operation == BUILD:
            self._restore()
            self._build(self.configuration)
        if self.operation == RESTORE:
            self._restore()
        if self.operation == PUBLISH:
            self._restore()
            self._publish(self.configuration)

    def _publish(self, configuration: str):
        self.project.publish(configuration=configuration,
                             output_dir=const.PUBDIR, 
                             verbose=True,
                             packages_path=get_packages_directory()
                             )

    def _restore(self):
        self.project.restore(packages_path=get_packages_directory(), verbose=True)

    def _build(self, configuration: str):
        self.project.build(configuration=configuration,
                               verbose=True,
                               packages_path=get_packages_directory(),
                               target_framework_monikers=None,
                               output_to_bindir=True)