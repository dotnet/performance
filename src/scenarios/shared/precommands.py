'''
Commands and utilities for pre.py scripts
'''

import sys
import os
import shutil
import subprocess
from logging import getLogger
from argparse import ArgumentParser
from dotnet import CSharpProject, CSharpProjFile
from shared import const
from shared.crossgen import CrossgenArguments
from shared.codefixes import replace_line, insert_after
from performance.common import extension, get_packages_directory, get_repo_root_path, RunCommand, helixpayload

DEFAULT = 'default'
BUILD = 'build'
PUBLISH = 'publish'
CROSSGEN = 'crossgen'
CROSSGEN2 = 'crossgen2'
EXTRACT = 'extract'
DEBUG = 'Debug'
RELEASE = 'Release'

OPERATIONS = (DEFAULT,
              BUILD,
              PUBLISH,
              CROSSGEN,
              CROSSGEN2,
              EXTRACT
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
        
        extract_parser = subparsers.add_parser(EXTRACT, help='Used for local runs that extract the binaries to be run from a zip file. Requires a path to the zip' )
        self.add_common_arguments(extract_parser)
        extract_parser.add_argument('-p', '--pathtozip',
                    dest='pathtozip',
                    metavar='pathtozip',
                    help='Path to the zip file to extract',
                    required=True)

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
        print(self.msbuild)
        self.msbuildstatic = args.msbuildstatic
        self.binlog = args.binlog
        self.has_workload = args.has_workload
        self.readonly_dotnet = args.readonly_dotnet
        self.windows = args.windows
        
        if self.operation == CROSSGEN:
            self.crossgen_arguments.parse_crossgen_args(args)
        if self.operation == CROSSGEN2:
            self.crossgen_arguments.parse_crossgen2_args(args)
        if self.operation == EXTRACT:
            self.pathtozip = args.pathtozip


    def new(self,
            template: str,
            output_dir: str,
            bin_dir: str,
            exename: str,
            working_directory: str,
            language: str = None,
            no_https: bool = False,
            no_restore: bool = True):
        'makes a new app with the given template'
        self.project = CSharpProject.new(template=template,
                                 output_dir=output_dir,
                                 bin_dir=bin_dir,
                                 exename=exename,
                                 working_directory=working_directory,
                                 force=True,
                                 verbose=True,
                                 language=language,
                                 no_https=no_https,
                                 no_restore=no_restore)
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
        parser.add_argument('--has-workload',
                            dest='has_workload',
                            default=False,
                            action='store_true',
                            help='Indicates that the dotnet being used has workload already installed')
        parser.add_argument('--readonly-dotnet',
                            dest='readonly_dotnet',
                            default=False,
                            action='store_true',
                            help='Indicates that the dotnet being used should not be modified (for example, when it is ahared with other builds)')
        parser.add_argument('--windowsui',
                            dest='windows',
                            action='store_true',
                            help='must be set for UI tests so the proper rid is used')
        parser.set_defaults(configuration=RELEASE)

    def existing(self, projectdir: str, projectfile: str):
        'create a project from existing project file'
        self._backup(projectdir)
        csproj = CSharpProjFile(os.path.join(const.APPDIR, projectfile), sys.path[0])
        self.project = CSharpProject(csproj, const.BINDIR)
        self._updateframework(csproj.file_name)

    def execute(self, build_args: list = []):
        'Parses args and runs precommands'
        if self.operation == DEFAULT:
            pass
        if self.operation == BUILD:
            self._restore()
            self._build(configuration=self.configuration, framework=self.framework, build_args=build_args)
        if self.operation == PUBLISH:
            self._restore()
            self._publish(configuration=self.configuration,
                          runtime_identifier=self.runtime_identifier,
                          framework=self.framework, build_args=build_args)
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

    def install_workload(self, workloadid: str, install_args: list = ["--skip-manifest-update"]):
        'Installs the workload, if needed'
        if not self.has_workload:
            if self.readonly_dotnet:
                raise Exception('workload needed to build, but has_workload=false, and readonly_dotnet=true')
            subprocess.run(["dotnet", "workload", "install", workloadid] + install_args)

    def uninstall_workload(self, workloadid: str):
        'Uninstalls the workload, if possible'
        if self.has_workload and not self.readonly_dotnet:
            subprocess.run(["dotnet", "workload", "uninstall", workloadid])

    def _addstaticmsbuildproperty(self, projectfile: str):
        'Insert static msbuild property in the specified project file'
        if self.msbuildstatic:
          for propertyarg in self.msbuildstatic.split(';'):
            propertyname, propertyvalue = propertyarg.split('=')
            propertystring = f'\n  <PropertyGroup>\n    <{propertyname}>{propertyvalue}</{propertyname}>\n  </PropertyGroup>'
            insert_after(projectfile, r'</PropertyGroup>', propertystring )

    def _parsemsbuildproperties(self) -> list:
        if self.msbuild:
            proplist = list()
            for propertyarg in self.msbuild.split(';'):
                proplist.append(propertyarg)
            return proplist
        return None

    def _updateframework(self, projectfile: str):
        'Update the <TargetFramework> property so we can re-use the template'
        if self.framework:
            if self.windows:
                replace_line(projectfile, r'<TargetFramework>.*?</TargetFramework>', f'<TargetFramework>{self.framework}-windows</TargetFramework>')
            else:
                replace_line(projectfile, r'<TargetFramework>.*?</TargetFramework>', f'<TargetFramework>{self.framework}</TargetFramework>')

    def _publish(self, configuration: str, framework: str = None, runtime_identifier: str = None, build_args: list = []):
        self.project.publish(configuration,
                             const.PUBDIR, 
                             True,
                             os.path.join(get_packages_directory(), ''), # blazor publish targets require the trailing slash for joining the paths
                             framework if not self.windows else f'{framework}-windows',
                             runtime_identifier,
                             self._parsemsbuildproperties(),
                             '-bl:%s' % self.binlog if self.binlog else "",
                             *build_args
                             )

    def _restore(self):
        self.project.restore(packages_path=get_packages_directory(), verbose=True)

    def _build(self, configuration: str, framework: str = None, build_args: list = []):
        self.project.build(configuration=configuration,
                               verbose=True,
                               packages_path=get_packages_directory(),
                               target_framework_monikers=[framework],
                               output_to_bindir=True,
                               *build_args)

    def _backup(self, projectdir:str):
        'Copy from projectdir to appdir so we do not modify the source code'
        if os.path.isdir(const.APPDIR):
            shutil.rmtree(const.APPDIR)
        shutil.copytree(projectdir, const.APPDIR)

