#!/usr/bin/env python3

'''
Support script around BenchView script.
'''

from collections import namedtuple
from errno import EEXIST
from glob import iglob
from logging import getLogger
from os import path
from urllib.parse import urlparse
from urllib.request import urlopen
from xml.etree import ElementTree
from zipfile import ZipFile

from performance.common import get_tools_directory
from performance.common import get_python_executable
from performance.common import make_directory
from performance.common import push_dir
from performance.common import remove_directory
from performance.common import RunCommand
from performance.common import validate_supported_runtime
from performance.logger import setup_loggers


class BenchView:
    '''
    Wrapper class around the BenchView scripts used to serialize performance
    data.
    '''

    def __init__(self, verbose: bool):
        self.__python = get_python_executable()
        self.__tools = path.join(BenchView.get_scripts_directory(), 'tools')
        self.__verbose = verbose

        # TODO: Fix BenchView scripts to use `loggin` instead of `print`.
        #   BenchView scripts perform rudimentary logging using `print` and
        #   this causes `Bad descriptor` error when redirecting output to null.
        #   At the moment, BenchView scripts only output on error or when it
        #   has written the generated output file, so setting `verbose=True`
        #   does not pollute output. In addition, these scripts logging is
        #   more robust, and this flag on will not pollute overall output.
        self.__verbose = True

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

    def build(
            self,
            working_directory: str,
            build_type: str,
            subparser: str = None,  # ['none', 'git']
            branch: str = None,
            commit: str = None,
            repository: str = None,
            source_timestamp: str = None) -> None:
        '''Wrapper around BenchView's build.py'''

        if not subparser:
            subparser = 'none'

        cmdline = [
            self.python, path.join(self.tools_directory, 'build.py'),
            subparser,
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
        RunCommand(cmdline, verbose=self.verbose).run(working_directory)

    def machinedata(self, working_directory: str, machine_manufacturer: str) -> None:
        '''Wrapper around BenchView's machinedata.py'''

        cmdline = [
            self.python, path.join(self.tools_directory, 'machinedata.py')
        ]
        if machine_manufacturer:
            cmdline += ['--machine_manufacturer', machine_manufacturer]
        RunCommand(cmdline, verbose=self.verbose).run(working_directory)

    def measurement(self, working_directory: str) -> None:
        '''Wrapper around BenchView's measurement.py'''

        common_cmdline = [
            self.python, path.join(self.tools_directory, 'measurement.py'),
            'bdn',
            '--append',
        ]

        full_json_files = []
        with push_dir(working_directory):
            pattern = "BenchmarkDotNet.Artifacts/**/*-full.json"
            getLogger().info(
                'Searching BenchmarkDotNet output files with: %s', pattern
            )

            for full_json_file in iglob(pattern, recursive=True):
                full_json_files.append(full_json_file)

        for full_json_file in full_json_files:
            cmdline = common_cmdline + [full_json_file]
            RunCommand(cmdline, verbose=self.verbose).run(working_directory)

    def submission_metadata(self, working_directory: str, name: str) -> None:
        '''Wrapper around BenchView's submission-metadata.py'''

        cmdline = [
            self.python,
            path.join(self.tools_directory, 'submission-metadata.py'),
            '--name', name,
            '--user-email', 'dotnet-bot@microsoft.com'
        ]
        RunCommand(cmdline, verbose=self.verbose).run(working_directory)

    def submission(
            self,
            working_directory: str,
            measurement_jsons: list,
            architecture: str,
            config_name: str,
            configs: dict,
            machinepool: str,
            jobgroup: str,
            jobtype: str
    ) -> None:
        '''Wrapper around BenchView's submission.py'''
        if not measurement_jsons:
            raise ValueError("No `measurement.json` were specified.")

        cmdline = [
            self.python, path.join(self.tools_directory, 'submission.py'),

            *measurement_jsons,

            '--build', path.join(
                working_directory, 'build.json'),
            '--machine-data', path.join(
                working_directory, 'machinedata.json'),
            '--metadata', path.join(
                working_directory, 'submission-metadata.json'),

            '--group', jobgroup,
            '--type', jobtype,
            '--config-name', config_name,
            '--architecture', architecture,
            '--machinepool', machinepool
        ]
        for key, value in configs.items():
            cmdline += ['--config', key, value]
        RunCommand(cmdline, verbose=self.verbose).run(working_directory)

    def upload(self, working_directory: str, container: str) -> None:
        '''Wrapper around BenchView's upload.py'''

        cmdline = [
            self.python,
            path.join(self.tools_directory, 'upload.py'),
            path.join(working_directory, 'submission.json'),
            '--container', container,
        ]
        RunCommand(cmdline, verbose=self.verbose).run(working_directory)


BuildInfo = namedtuple('BuildInfo', [
    'subparser',
    'branch',
    'commit_sha',
    'repository',
    'source_timestamp',
])


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
