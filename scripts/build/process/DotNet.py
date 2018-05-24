'''
Contains the definition of DotNet process.
'''

import os

from ..common import get_repo_root_path
from ..runner.RunCommand import RunCommand


class DotNet(object):
    '''
    This is a class wrapper around the `dotnet` command line interface.
    '''

    def __init__(
            self,
            log_file: str,
            working_directory: str,
            csproj_file: str,
            verbose: bool):
        if not log_file:
            raise TypeError('Unspecified log file.')
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

        self.__log_file = log_file
        self.__working_directory = working_directory
        self.__csproj_file = csproj_file
        self.__verbose = verbose

    @property
    def log_file(self) -> str:
        '''Gets the log file name to write to.'''
        return self.__log_file

    @property
    def working_directory(self) -> str:
        '''Gets the working directory for the dotnet process to be started.'''
        return self.__working_directory

    @property
    def csproj_file(self) -> str:
        '''Gets the project file to run the dotnet cli against.'''
        return self.__csproj_file

    @property
    def verbose(self) -> bool:
        '''Gets a flag to whether verbosity if turned on or off.'''
        return self.__verbose

    @property
    def packages_path(self) -> str:
        '''Gets the folder to restore packages to.'''
        return os.path.join(get_repo_root_path(), 'packages')

    @property
    def bin_path(self) -> str:
        '''Gets the directory in which the built binaries will be placed.'''
        return os.path.join(get_repo_root_path(), 'bin')

    def restore(self) -> None:
        '''
        Calls dotnet to restore the dependencies and tools of the specified
        project.
        '''
        cmdline = ['dotnet', 'restore',
                   '--packages', self.packages_path,
                   self.csproj_file]
        RunCommand(self.log_file, cmdline, verbose=self.verbose).run(
            'dotnet-restore', self.working_directory)

    def publish(self,
                configuration: str,
                framework: str,
                product: str) -> None:
        '''
        Calls dotnet to pack the specified application and its dependencies
        into the repo bin folder for deployment to a hosting system.
        '''
        if not product:
            raise TypeError('Unspecified product name.')
        base_output_path = '{}{}'.format(
            os.path.join(self.bin_path, product), os.path.sep)

        cmdline = ['dotnet', 'publish',
                   '--no-restore',
                   '--configuration', configuration,
                   '--framework', framework,
                   self.csproj_file,
                   '/p:BaseOutputPath={}'.format(base_output_path)]
        RunCommand(self.log_file, cmdline, verbose=self.verbose).run(
            'dotnet-publish', self.working_directory)
