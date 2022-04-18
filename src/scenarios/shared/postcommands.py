from shared.const import APPDIR, TMPDIR, TRACEDIR, PUBDIR, BINDIR, CROSSGENDIR
from performance.common import remove_directory
from argparse import ArgumentParser
import subprocess

class PostCommands:
    '''
    Handles any post run cleanup
    '''

    def __init__(self):
        parser = ArgumentParser()

        parser.add_argument('--readonly-dotnet',
                            dest='readonly_dotnet',
                            action='store_true',
                            help='Indicates that the dotnet being used should not be modified (for example, when it is ahared with other builds)')

        args = parser.parse_args()
        self.readonly_dotnet = args.readonly_dotnet

    def uninstall_workload(self, workloadid: str):
        if not self.readonly_dotnet:
            subprocess.run(["dotnet", "workload", "uninstall", workloadid])

def clean_directories():
    to_remove = (APPDIR, TMPDIR, TRACEDIR, PUBDIR, BINDIR, CROSSGENDIR, "emsdk")
    print(f"Removing {','.join(to_remove)} if exist ...")
    for dir in to_remove:
        remove_directory(dir)
