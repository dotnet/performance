#!/usr/bin/env python3

import urllib.request
import sys
import subprocess
import argparse
import os
import stat

##########################################################################
# Argument Parser
##########################################################################

description = 'Downloads dotnet'

parser = argparse.ArgumentParser(description=description)

parser.add_argument('-arch', dest='arch', default='x64', choices=['x86','x64'])
parser.add_argument('-runtimeId', dest='runtimeId', default=None)
parser.add_argument('-installDir', dest='installDir', default='.dotnet')
parser.add_argument('-branch', dest='branch', default='master', choices=['master', 'release/2.1.3xx', 'release/2.0.0', 'release/1.1.0'])

def main(args):
    arch = args.arch
    installDir = args.installDir
    runtimeId = args.runtimeId

    # Download appropriate dotnet install script
    dotnetInstallScriptExtension = '.ps1' if sys.platform == 'win32' else '.sh'
    dotnetInstallScriptName = 'dotnet-install' + dotnetInstallScriptExtension
    dotnetInstallScriptUrl = 'https://raw.githubusercontent.com/dotnet/cli/master/scripts/obtain/' + dotnetInstallScriptName

    urllib.request.urlretrieve(dotnetInstallScriptUrl, dotnetInstallScriptName)
    os.chmod(dotnetInstallScriptName, stat.S_IRWXU)

    # run dotnet-install script
    rid = [] if runtimeId is None else ['--runtime-id',runtimeId]
    dotnetInstallInterpreter = ['powershell', '-NoProfile', '-executionpolicy', 'bypass', '.\\%s' % (dotnetInstallScriptName)] if sys.platform == 'win32' else ['./%s' % (dotnetInstallScriptName)]

    runArgs = dotnetInstallInterpreter + ['-Runtime',
            'dotnet',
            '-Architecture',
            arch,
            '-InstallDir',
            installDir,
            '-Channel',
            args.branch] + rid


    p = subprocess.Popen(' '.join(runArgs), shell=True)
    p.communicate()

    runArgs =  dotnetInstallInterpreter + ['-Architecture',
            arch,
            '-InstallDir',
            installDir,
            '-Channel',
            'master']

    p = subprocess.Popen(' '.join(runArgs), shell=True)
    p.communicate()

if __name__ == "__main__":
    Args = parser.parse_args(sys.argv[1:])
    sys.exit(main(Args))
