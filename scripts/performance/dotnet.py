'''
Contains the functionality around DotNet Cli.
'''

import os

from .common import get_repo_root_path
from .common import RunCommand


def dotnet_info(verbose: bool) -> None:
    """
    Executes `dotnet --info` in order to get the .NET Core information from the
    dotnet executable.
    """
    cmdline = ['dotnet', '--info']
    RunCommand(cmdline, verbose=verbose).run()


class DotNetProject:
    '''
    This is a class wrapper around the `dotnet` command line interface.
    Remark: It assumes dotnet is already in the PATH.
    '''

    def __init__(
            self,
            working_directory: str,
            csproj_file: str):
        if not working_directory:
            raise TypeError('Unspecified working directory.')
        if not os.path.isdir(working_directory):
            raise ValueError(
                'Specified working directory: {}, does not exist.'.format(
                    working_directory))

        if os.path.isabs(csproj_file) and not os.path.exists(csproj_file):
            raise ValueError(
                'Specified project file: {}, does not exist.'.format(
                    csproj_file))
        elif not os.path.exists(os.path.join(working_directory, csproj_file)):
            raise ValueError(
                'Specified project file: {}, does not exist.'.format(
                    csproj_file))

        self.__working_directory = working_directory
        self.__csproj_file = csproj_file

    @property
    def working_directory(self) -> str:
        '''Gets the working directory for the dotnet process to be started.'''
        return self.__working_directory

    @property
    def csproj_file(self) -> str:
        '''Gets the project file to run the dotnet cli against.'''
        return self.__csproj_file

    @property
    def bin_path(self) -> str:
        '''Gets the directory in which the built binaries will be placed.'''
        return os.path.join(get_repo_root_path(), 'bin')

    def restore(self, packages_path: str, verbose: bool) -> None:
        '''
        Calls dotnet to restore the dependencies and tools of the specified
        project.

        Keyword arguments:
        packages_path -- The directory to restore packages to.
        '''
        if not packages_path:
            raise TypeError('Unspecified packages directory.')
        cmdline = [
            'dotnet', 'restore',
            self.csproj_file,
            '--packages', packages_path
        ]
        RunCommand(cmdline, verbose=verbose).run(
            self.working_directory)

    def build(self,
              configuration: str,
              frameworks: list,
              verbose: bool,
              *args) -> None:
        '''Calls dotnet to build the specified project.'''
        if not frameworks:  # Frameworks were not specified, the build all.
            cmdline = [
                'dotnet', 'build',
                self.csproj_file,
                '--configuration', configuration,
            ]
            if args:
                cmdline = cmdline + list(args)
            RunCommand(cmdline, verbose=verbose).run(
                self.working_directory)
        else:  # Only build specified frameworks
            for framework in frameworks:
                cmdline = [
                    'dotnet', 'build',
                    self.csproj_file,
                    '--configuration', configuration,
                    '--framework', framework
                ]
                if args:
                    cmdline = cmdline + list(args)
                RunCommand(cmdline, verbose=verbose).run(
                    self.working_directory)

    def run(self,
            configuration: str,
            framework: str,
            verbose: bool,
            *args) -> None:
        '''
        Calls dotnet to run a .NET project output.
        '''

        cmdline = [
            'dotnet', 'run',
            '--project', self.csproj_file,
            '--configuration', configuration,
            '--framework', framework
        ]

        if args:
            cmdline = cmdline + list(args)
        RunCommand(cmdline, verbose=verbose).run(
            self.working_directory)
