'''
Commands and utilities for pre.py scripts
'''

import sys
import os
import shutil
from logging import getLogger
from argparse import ArgumentParser
from dotnet import CSharpProject, CSharpProjFile
from shared import const
from shared.crossgen import CrossgenArguments
from shared.util import extension
from shared.codefixes import replace_line, insert_after
from performance.common import get_packages_directory, get_repo_root_path, RunCommand, helixpayload

DEFAULT = 'default'
BUILD = 'build'
PUBLISH = 'publish'
CROSSGEN = 'crossgen'
CROSSGEN2 = 'crossgen2'
DEBUG = 'Debug'
RELEASE = 'Release'

OPERATIONS = (DEFAULT,
              BUILD,
              PUBLISH,
              CROSSGEN,
              CROSSGEN2
             )

class PreCommands:
    '''
    Handles building and publishing
    '''

    def __init__(self):
        self.project: CSharpProject
        self.projectfile: CSharpProjFile
        self.crossgen_arguments = CrossgenArguments()
        parser = ArgumentParser()

        subparsers = parser.add_subparsers(title='Operations', 
                                           description='Common preperation steps for perf tests. Should run under src\scenarios\<test asset folder>',
                                           dest='operation')

        default_parser = subparsers.add_parser(DEFAULT, help='Default operation (placeholder command and no specific operation will be executed)' )
        self.add_common_arguments(default_parser)

        build_parser = subparsers.add_parser(BUILD, help='Builds the project')
        self.add_common_arguments(build_parser)

        publish_parser = subparsers.add_parser(PUBLISH, help='Publishes the project')
        self.add_common_arguments(publish_parser)

        crossgen_parser = subparsers.add_parser(CROSSGEN, help='Runs crossgen on a particular file')
        self.add_common_arguments(crossgen_parser)
        self.crossgen_arguments.add_crossgen_arguments(crossgen_parser)

        crossgen2_parser = subparsers.add_parser(CROSSGEN2, help='Runs crossgen2 on a particular file')
        self.add_common_arguments(crossgen2_parser)
        self.crossgen_arguments.add_crossgen2_arguments(crossgen2_parser)

        args = parser.parse_args()

        if not args.operation:
            getLogger().error("Please specify an operation: %s" % list(OPERATIONS))
            sys.exit(1)

        self.configuration = args.configuration 
        self.operation = args.operation
        self.framework = args.framework
        self.runtime_identifier = args.runtime
        self.msbuild = args.msbuild
        self.msbuildstatic = args.msbuildstatic
        self.binlog = args.binlog

        if self.operation == CROSSGEN:
            self.crossgen_arguments.parse_crossgen_args(args)
        if self.operation == CROSSGEN2:
            self.crossgen_arguments.parse_crossgen2_args(args)


    def new(self,
            template: str,
            output_dir: str,
            bin_dir: str,
            exename: str,
            working_directory: str,
            language: str = None):
        'makes a new app with the given template'
        self.project = CSharpProject.new(template=template,
                                 output_dir=output_dir,
                                 bin_dir=bin_dir,
                                 exename=exename,
                                 working_directory=working_directory,
                                 force=True,
                                 verbose=True,
                                 language=language)
        self._updateframework(self.project.csproj_file)
        self._addstaticmsbuildproperty(self.project.csproj_file)

    def add_common_arguments(self, parser: ArgumentParser):
        "Options that are common across many 'dotnet' commands"
        parser.add_argument('-c', '--configuration',
                            dest='configuration',
                            choices=[DEBUG, RELEASE],
                            metavar='config',
                            help='configuration for build or publish - ex: Release or Debug')
        parser.add_argument('-f', '--framework',
                            dest='framework',
                            metavar='framework',
                            help='framework for build or publish - ex: netcoreapp3.0')
        parser.add_argument('-r', '--runtime',
                            dest='runtime',
                            metavar='runtime',
                            help='runtime for build or publish - ex: win-x64')
        parser.add_argument('--msbuild',
                            dest='msbuild',
                            metavar='msbuild',
                            help='a list of msbuild flags passed to build or publish command separated by semicolons - ex: /p:Foo=Bar;/p:Baz=Blee;...')
        parser.add_argument('--msbuild-static',
                            dest='msbuildstatic',
                            metavar='msbuildstatic',
                            help='a list of msbuild properties inserted into .csproj file of the project - ex: Foo=Bar;Bas=Blee;'
                           )
        parser.add_argument('--binlog',
                            dest='binlog',
                            metavar='<file-name>.binlog',
                            help='flag to turn on binlog for build or publish; ex: <file-name>.binlog')
        parser.set_defaults(configuration=RELEASE)

    def existing(self, projectdir: str, projectfile: str):
        'create a project from existing project file'
        self._backup(projectdir)
        csproj = CSharpProjFile(os.path.join(const.APPDIR, projectfile), sys.path[0])
        self.project = CSharpProject(csproj, const.BINDIR)
        self._updateframework(csproj.file_name)

    def execute(self):
        'Parses args and runs precommands'
        if self.operation == DEFAULT:
            pass
        if self.operation == BUILD:
            self._restore()
            self._build(configuration=self.configuration, framework=self.framework)
        if self.operation == PUBLISH:
            self._restore()
            self._publish(configuration=self.configuration,
                          runtime_identifier=self.runtime_identifier)
        if self.operation == CROSSGEN:
            startup_args = [
                os.path.join(self.crossgen_arguments.coreroot, 'crossgen%s' % extension()),
            ]
            startup_args += self.crossgen_arguments.get_crossgen_command_line()
            RunCommand(startup_args, verbose=True).run(self.crossgen_arguments.coreroot)
        if self.operation == CROSSGEN2:
            startup_args = [
                os.path.join(self.crossgen_arguments.coreroot, 'corerun%s' % extension()),
                os.path.join(self.crossgen_arguments.coreroot, 'crossgen2', 'crossgen2.dll'),
            ]
            startup_args += self.crossgen_arguments.get_crossgen2_command_line()
            RunCommand(startup_args, verbose=True).run(self.crossgen_arguments.coreroot)

    def add_startup_logging(self, file: str, line: str):
        self.add_event_source(file, line, "PerfLabGenericEventSource.Log.Startup();")

    def add_event_source(self, file: str, line: str, trace_statement: str):
        '''
        Adds a copy of the event source to the project and inserts the correct call
        file: relative path to the root of the project (where the project file lives)
        line: Exact line to insert trace statement after
        trace_statement: Statement to insert
        '''

        projpath = os.path.dirname(self.project.csproj_file)
        staticpath = os.path.join(get_repo_root_path(), "src", "scenarios", "staticdeps")
        if helixpayload():
            staticpath = os.path.join(helixpayload(), "staticdeps")
        shutil.copyfile(os.path.join(staticpath, "PerfLab.cs"), os.path.join(projpath, "PerfLab.cs"))
        filepath = os.path.join(projpath, file)
        insert_after(filepath, line, trace_statement)

    def _addstaticmsbuildproperty(self, projectfile: str):
        'Insert static msbuild property in the specified project file'
        if self.msbuildstatic:
          for propertyarg in self.msbuildstatic.split(';'):
            propertyname, propertyvalue = propertyarg.split('=')
            propertystring = f'\n  <PropertyGroup>\n    <{propertyname}>{propertyvalue}</{propertyname}>\n  </PropertyGroup>'
            insert_after(projectfile, r'</PropertyGroup>', propertystring )

    def _updateframework(self, projectfile: str):
        'Update the <TargetFramework> property so we can re-use the template'
        if self.framework:
            replace_line(projectfile, r'<TargetFramework>.*?</TargetFramework>', f'<TargetFramework>{self.framework}</TargetFramework>')

    def _publish(self, configuration: str, framework: str = None, runtime_identifier: str = None):
        self.project.publish(configuration,
                             const.PUBDIR, 
                             True,
                             os.path.join(get_packages_directory(), ''), # blazor publish targets require the trailing slash for joining the paths
                             framework,
                             runtime_identifier,
                             self.msbuild or "",
                             '-bl:%s' % self.binlog if self.binlog else ""
                             )

    def _restore(self):
        self.project.restore(packages_path=get_packages_directory(), verbose=True)

    def _build(self, configuration: str, framework: str = None):
        self.project.build(configuration=configuration,
                               verbose=True,
                               packages_path=get_packages_directory(),
                               target_framework_monikers=[framework],
                               output_to_bindir=True)

    def _backup(self, projectdir:str):
        'Copy from projectdir to appdir so we do not modify the source code'
        if os.path.isdir(const.APPDIR):
            shutil.rmtree(const.APPDIR)
        shutil.copytree(projectdir, const.APPDIR)

