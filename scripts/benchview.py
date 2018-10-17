#!/usr/bin/env python3

'''
Support script around BenchView script.
'''

from errno import EEXIST
from logging import getLogger
from os import path
from urllib.parse import urlparse
from urllib.request import urlopen
from xml.etree import ElementTree
from zipfile import ZipFile

from performance.common import get_tools_directory
from performance.common import get_python_executable
from performance.common import make_directory
from performance.common import remove_directory
from performance.common import RunCommand
from performance.common import validate_supported_runtime
from performance.logger import setup_loggers


class BenchView:
    '''
    Wrapper class around the BenchView scripts used to serialize performance
    data.
    '''

    def __init__(self, working_directory: str, verbose: bool):
        self.__python = get_python_executable()
        self.__tools = path.join(BenchView.get_scripts_directory(), 'tools')
        self.__verbose = verbose
        self.__working_directory = working_directory

    @staticmethod
    def get_scripts_directory() -> str:
        '''BenchView scripts install directory.'''
        return path.join(
            get_tools_directory(), 'Microsoft.BenchView.JSONFormat')

    @property
    def python(self):
        '''
        Gets the absolute path of the executable binary for the Python
        interpreter.
        '''
        return self.__python

    @property
    def tools_directory(self) -> str:
        '''BenchView tools directory.'''
        return self.__tools

    @property
    def verbose(self) -> bool:
        '''Enables/Disables verbosity.'''
        return self.__verbose

    @property
    def working_directory(self) -> str:
        '''Working directory for invoking BenchView scripts.'''
        return self.__working_directory

    def build(
            self,
            build_type: str,
            # subparser: str,  ['none', 'git']
            branch: str = None,
            commit: str = None,
            repository: str = None,
            source_timestamp: str = None) -> None:
        '''Wrapper around BenchView's build.py'''

        cmdline = [
            self.python, path.join(self.tools_directory, 'build.py'),
            'git',  # TODO: Maybe none?
            '--type', build_type
        ]
        if branch:
            cmdline += ['--branch', branch]
        if commit:
            cmdline += ['--number', commit]
        if repository:
            cmdline += ['--repository', repository]
        if source_timestamp:
            cmdline += ['--source-timestamp', source_timestamp]
        RunCommand(cmdline, verbose=self.verbose).run(self.working_directory)

    def machinedata(self) -> None:
        '''Wrapper around BenchView's machinedata.py'''

        cmdline = [
            self.python, path.join(self.tools_directory, 'machinedata.py')
        ]
        RunCommand(cmdline, verbose=self.verbose).run(self.working_directory)

    def measurement(self, bdn_json_path: str) -> None:
        '''Wrapper around BenchView's measurement.py'''

        cmdline = [
            self.python, path.join(self.tools_directory, 'measurement.py'),
            'bdn',
            bdn_json_path,
            '--append',
        ]
        RunCommand(cmdline, verbose=self.verbose).run(self.working_directory)

    def submission_metadata(self, name: str) -> None:
        '''Wrapper around BenchView's submission-metadata.py'''

        cmdline = [
            self.python,
            path.join(self.tools_directory, 'submission-metadata.py'),
            '--name', name,
            '--user-email', 'dotnet-bot@microsoft.com'
        ]
        RunCommand(cmdline, verbose=self.verbose).run(self.working_directory)

    def submission(
            self,
            architecture: str,
            config_name: str,
            configs: dict,
            machinepool: str,
            jobgroup: str,
            jobtype: str
    ) -> None:
        '''Wrapper around BenchView's submission.py'''

        cmdline = [
            self.python, path.join(self.tools_directory, 'submission.py'),
            path.join(self.working_directory, 'measurement.json'),

            '--build', path.join(
                self.working_directory, 'build.json'),
            '--machine-data', path.join(
                self.working_directory, 'machinedata.json'),
            '--metadata', path.join(
                self.working_directory, 'submission-metadata.json'),

            '--group', jobgroup,
            '--type', jobtype,
            '--config-name', config_name,
            '--architecture', architecture,
            '--machinepool', machinepool
        ]
        for key, value in configs.items():
            cmdline += ['--config', key, value]
        RunCommand(cmdline, verbose=self.verbose).run(self.working_directory)

    def upload(self, container: str) -> None:
        '''Wrapper around BenchView's upload.py'''

        cmdline = [
            self.python,
            path.join(self.tools_directory, 'upload.py'),
            '--container', container,
        ]
        RunCommand(cmdline, verbose=self.verbose).run(self.working_directory)


def install():
    '''
    Downloads scripts that serialize/upload performance data to BenchView.
    '''
    __log_script_header()

    url_str = __get_latest_benchview_script_version()
    benchview_path = BenchView.get_scripts_directory()

    if path.isdir(benchview_path):
        remove_directory(benchview_path)
    if not path.exists(benchview_path):
        make_directory(benchview_path)

    getLogger().info('%s -> %s', url_str, benchview_path)

    zipfile_path = __download_zip_file(url_str, benchview_path)
    __unzip_file(zipfile_path, benchview_path)


def __download_zip_file(url_str: str, output_path: str):
    if not url_str:
        raise ValueError('URL was not defined.')
    url = urlparse(url_str)
    if not url.scheme or not url.netloc or not url.path:
        raise ValueError('Invalid URL: {}'.format(url_str))
    if not output_path:
        raise ValueError('Invalid output directory: {}'.format(output_path))

    zip_file_name = path.basename(url.path)
    zip_file_name = path.join(output_path, zip_file_name)

    if not path.splitext(zip_file_name)[1] == '.zip':
        zip_file_name = '{}.zip'.format(zip_file_name)

    if path.isfile(zip_file_name):
        raise FileExistsError(EEXIST, 'File exists', zip_file_name)

    with urlopen(url_str) as response, open(zip_file_name, 'wb') as zipfile:
        zipfile.write(response.read())
    return zip_file_name


def __unzip_file(file_path: str, output_path: str):
    '''Extract all members from the archive to the specified directory.'''
    # TODO: Error checking?
    with ZipFile(file_path, 'r') as zipfile:
        zipfile.extractall(output_path)


def __log_script_header():
    start_msg = "Downloading BenchView scripts"
    getLogger().info('-' * len(start_msg))
    getLogger().info(start_msg)
    getLogger().info('-' * len(start_msg))


def __get_latest_benchview_script_version() -> str:
    scheme_authority = 'http://benchviewtestfeed.azurewebsites.net'
    fullpath = "/nuget/FindPackagesById()?id='Microsoft.BenchView.JSONFormat'"
    url_str = '{}{}'.format(scheme_authority, fullpath)
    with urlopen(url_str) as response:
        tree = ElementTree.parse(response)
        root = tree.getroot()
        namespace = root.tag[0:root.tag.index('}') + 1]
        xpath = '{0}entry/{0}content[@type="application/zip"]'.format(
            namespace)
        packages = [element.get('src') for element in tree.findall(xpath)]
        if not packages:
            raise RuntimeError('No BenchView packages found.')
        packages.sort()
        return packages[-1]


def __main():
    validate_supported_runtime()
    setup_loggers(verbose=True)
    install()


if __name__ == "__main__":
    __main()
