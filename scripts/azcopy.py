#!/usr/bin/env python3
    
import os
from os import sys, path, makedirs, environ
from glob import glob
from performance.logger import setup_loggers
from urllib.request import urlopen
from tarfile import TarFile
from zipfile import ZipFile
from logging import getLogger

from performance.common import get_tools_directory, get_artifacts_directory
from performance.common import RunCommand

class AzCopy:
    def __init__(self, sas: str, path: str, verbose: bool):
        self.containerUrl = 'https://pvscmdupload.blob.core.windows.net/results/'
        self.path = path
        self.sas = sas
        self.verbose = verbose

        if(sys.platform == 'win32'):
            self.archivename = 'azcopy.zip'
            self.exename = 'azcopy.exe'
            self.downloadUrl = 'https://aka.ms/downloadazcopy-v10-windows'
        else:
            self.archivename = 'azcopy.tar.gz'
            self.exename = 'azcopy'
            self.downloadUrl = 'https://aka.ms/downloadazcopy-v10-linux'

    def get_azcopy_directory(self) -> str:
        return path.join(get_tools_directory(),'azcopy')

    def archive_path(self) -> str:
        return path.join(self.get_azcopy_directory(), self.archivename)

    def exe_path(self) -> str:
        return path.join(self.get_azcopy_directory(), self.exename)

    def get_upload_url(self) -> str:
        return f"{self.containerUrl}{self.path}{self.sas}"

    def download_azcopy(self) -> None:
        if(path.exists(self.exe_path()) == True):
            return

        getLogger().info('downloading azcopy')
        if not path.isdir(self.get_azcopy_directory()):
            makedirs(self.get_azcopy_directory())

        with urlopen(self.downloadUrl) as response, open(self.archive_path(), 'wb') as zipfile:
                zipfile.write(response.read())
        
        if(sys.platform == 'win32'):
            with ZipFile(self.archive_path()) as zipfile:
                item, = (zipfile for zipfile in zipfile.infolist() if zipfile.filename.endswith('.exe'))
                item.filename = path.basename(item.filename)
                zipfile.extract(item, self.get_azcopy_directory())
        else:
            tar = TarFile.open(self.archive_path())
            item, = (tar for tar in tar.getmembers() if tar.name.endswith('azcopy'))
            item.name = path.basename(item.name)
            tar.extract(item, self.get_azcopy_directory())
            tar.close()

    def upload_files(self, searchPath: str):
        self.download_azcopy()
        cmdline = [
            self.exe_path(), 'copy', searchPath, self.get_upload_url(), '--recursive=true'
        ]
        RunCommand(cmdline, verbose=self.verbose).run()

    @staticmethod
    def upload_results(containerPath: str, verbose: bool) -> None:
        setup_loggers(verbose)
        if(os.environ.get('PERFLAB_UPLOAD_TOKEN') != None):
            files = glob(path.join(get_artifacts_directory(), '**','*perf-lab-report.json'), recursive=True)
            if files:
                dirname = path.dirname(files[0])
                if len(files) == 1:
                    # need to work around a bug in azcopy which loses file name if there is only one file.
                    # https://github.com/Azure/azure-storage-azcopy/issues/410
                    containerPath = path.join(containerPath, path.basename(files[0]))
                AzCopy(os.environ['PERFLAB_UPLOAD_TOKEN'],containerPath,verbose).upload_files(path.join(dirname,'*perf-lab-report.json'))

if __name__ == "__main__":
    AzCopy.upload_results('', True)