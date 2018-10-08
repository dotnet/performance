#!/usr/bin/env python3

import json
import urllib.request
import argparse
import os
import shutil
import subprocess
import sys
import glob
from sys import version_info
from build.common import is_supported_version
from build.common import get_repo_root_path

##########################################################################
# Argument Parser
##########################################################################

description = 'Tool to run .NET benchmarks'

parser = argparse.ArgumentParser(description=description)

parser.add_argument('-framework', dest='framework', default='netcoreapp3.0', required=False, choices=['netcoreapp3.0', 'netcoreapp2.2', 'netcoreapp2.1', 'netcoreapp2.0', 'net461'])
parser.add_argument('-arch', dest='arch', default='x64', required=False, choices=['x64', 'x86'])
parser.add_argument('-uploadToBenchview', dest='uploadToBenchview', action='store_true', default=False)
parser.add_argument('-category', dest='category', required=True, choices=['coreclr', 'corefx'], type=str.lower)
parser.add_argument('-branch', dest='branch', required=True)
parser.add_argument('-runType', dest='runType', default='rolling', choices=['rolling', 'private', 'local'])
parser.add_argument('-maxIterations', dest='maxIterations', type=int, default=20)

##########################################################################
# Helper Functions
##########################################################################

def log(message):
    """ Print logging information
    Args:
        message (str): message to be printed
    """

    print('[%s]: %s' % (sys.argv[0], message))

def run_command(runArgs, environment, errorMessage):
    """ Run command specified by runArgs
    Args:
        runargs (str[]): list of arguments for subprocess
        environment(str{}): dict of environment variable
        errorMessage(str): message to print if there is an error
    """
    log('')
    log(" ".join(runArgs))

    try:
        subprocess.run(runArgs, check=True, env=environment)
    except subprocess.CalledProcessError as e:
        log(errorMessage)
        raise

def get_dotnet_sha(dotnetPath):
    """ Discovers the dotnet sha
    Args:
        dotnetPath (str): dotnet.exe path
    """
    out = subprocess.check_output([dotnetPath, '--info'])

    foundHost = False
    for line in out.splitlines():
        decodedLine = line.decode('utf-8')

        # First look for the host information, since that is the sha we are looking for
        # Then grab the first Commit line we find, which will be the sha of the framework
        # we are testing
        if 'Host' in decodedLine:
            foundHost = True
        elif foundHost and 'Commit' in decodedLine:
            return decodedLine.strip().split()[1]

    return ''

def generate_results_for_benchview(python, better, hasWarmupRun, benchmarkOutputDir, benchviewPath):
    """ Generates results to be uploaded to benchview using measurement.py
    Args:
        python (str): python executable
        better (str): how to order results
        hasWarmupRun (bool): if there was a warmup run
        benchmarkOutputDir (str): path to where benchmark results were written
        benchviewPath (str): path to benchview tools
    """
    benchviewMeasurementParser = 'xunit'
    lvMeasurementArgs = [benchviewMeasurementParser,
            '--better',
            better]
    if hasWarmupRun:
        lvMeasurementArgs = lvMeasurementArgs + ['--drop-first-value']

    lvMeasurementArgs = lvMeasurementArgs + ['--append']

    files = glob.iglob(os.path.join(benchmarkOutputDir, "*.xml"))
    for filename in files:
        runArgs = [python, os.path.join(benchviewPath, 'measurement.py')] + lvMeasurementArgs + [filename]
        run_command(runArgs, os.environ, 'Call to %s failed' % runArgs[1])

def upload_to_benchview(python, benchviewPath, operatingSystem, collectionFlags, architecture, runType, category):
    """ Upload results to benchview
    Args:
        python (str): python executable
        benchviewPath (str): path to benchview tools
        operatingSystem (str): operating system of the run
        collectionFlags (str): collection flags
        architecture (str): architecture of the run (x86, x64)
    """
    measurementJson = os.path.join(os.getcwd(), 'measurement.json')
    buildJson = os.path.join(os.getcwd(), 'build.json')
    machinedataJson = os.path.join(os.getcwd(), 'machinedata.json')
    submissionMetadataJson = os.path.join(os.getcwd(), 'submission-metadata.json')

    for jsonFile in [measurementJson, buildJson, machinedataJson, submissionMetadataJson]:
        if not os.path.isfile(jsonFile):
            raise Exception('%s does not exist. There is no data to be uploaded.' % jsonFile)

    etwCollection = 'Off' if collectionFlags == 'stopwatch' else 'On'
    runArgs = [python,
            os.path.join(benchviewPath, 'submission.py'),
            measurementJson,
            '--build',
            buildJson,
            '--machine-data',
            machinedataJson,
            '--metadata',
            submissionMetadataJson,
            '--group',
            '.Net %s Performance' % category,
            '--type',
            runType,
            '--config-name',
            'Release',
            '--config',
            'OS',
            operatingSystem,
            '--config',
            'Profile',
            etwCollection,
            '--architecture',
            architecture,
            '--machinepool',
            'PerfSnake']

    run_command(runArgs, os.environ, 'Call to %s failed' % runArgs[1])

    runArgs = [python,
            os.path.join(benchviewPath, 'upload.py'),
            'submission.json',
            '--container',
            category]

    run_command(runArgs, os.environ, 'Call to %s failed' % runArgs[1])

##########################################################################
# Main
##########################################################################
def main(args):
    if not is_supported_version():
        log("Python 3.5 or newer is required")
        return 1

    if not sys.platform == 'win32':
        log("Script is not compatible with %s" % sys.platform)
        return 2

    python = 'py'
    runEnv = dict(os.environ)
    runEnv['DOTNET_MULTILEVEL_LOOKUP'] = '0'
    runEnv['UseSharedCompilation'] = 'false'

    workspace = get_repo_root_path()

    # Download dotnet
    runArgs = ['py', os.path.join(workspace, 'scripts', 'dotnet-install.py'), '-arch', args.arch, '-installDir', '.dotnet', '-branch', args.branch]
    run_command(runArgs, runEnv, 'Failed to install dotnet')

    # Get the dotnet sha
    dotnetPath = os.path.join(workspace, '.dotnet', 'dotnet.exe')
    dotnetVersion = get_dotnet_sha(dotnetPath)

    benchmarksDirectoryPath = os.path.join(workspace, 'src', 'benchmarks')
    os.chdir(benchmarksDirectoryPath)

    # Build the Benchmarks project
    performanceHarnessCsproj = os.path.join('Benchmarks.csproj')
    runArgs = [dotnetPath, 'restore', performanceHarnessCsproj]
    run_command(runArgs, runEnv, 'Failed to restore %s' % performanceHarnessCsproj)

    runArgs = [dotnetPath, 'publish', performanceHarnessCsproj, '-c', 'Release', '-f', args.framework]
    run_command(runArgs, runEnv, 'Failed to publish %s' % performanceHarnessCsproj)

    # Run the tests
    benchmarkOutputDir = os.path.join(benchmarksDirectoryPath, 'bin', 'Release', args.framework, 'publish')
    os.chdir(benchmarkOutputDir)

    runArgs = [dotnetPath, 'Benchmarks.dll', '--cli', dotnetPath, '--tfms', args.framework, '--allCategories', args.category, '--maxIterationCount', str(args.maxIterations)]
    run_command(runArgs, runEnv, 'Failed to run Benchmarks.dll')

    if args.uploadToBenchview:
        # Download nuget
        nugetPath = os.path.join(workspace, 'nuget.exe')
        runArgs = ['powershell', '-NoProfile', 'wget', 'https://dist.nuget.org/win-x86-commandline/latest/nuget.exe', '-OutFile', '"' + nugetPath + '"', ]
        run_command(runArgs, runEnv, 'Failed to download nuget.exe')

        # Download benchview scripts
        benchviewPath = os.path.join(workspace, 'Microsoft.BenchView.JSONFormat')
        if os.path.exists(benchviewPath):
            shutil.rmtree(benchviewPath)
        runArgs = [nugetPath, 'install', 'Microsoft.BenchView.JSONFormat', '-Source', 'http://benchviewtestfeed.azurewebsites.net/nuget', '-OutputDirectory', workspace, '-Prerelease', '-ExcludeVersion']
        run_command(runArgs, runEnv, 'Failed to download Microsoft.BenchView.JSONFormat')

        benchviewName = 'performance rolling %s' % dotnetVersion

        # Generate submission-metadata.json
        benchviewPath = os.path.join(benchviewPath, 'tools')
        runScript = 'submission-metadata.py'
        runArgs = [python, os.path.join(benchviewPath, runScript), '--name', '"%s"' % (benchviewName), '--user-email', 'dotnet-bot@microsoft.com']
        run_command(runArgs, runEnv, '%s failed to run' % runScript)

        # Generate build.json
        r = urllib.request.urlopen('https://api.github.com/repos/dotnet/core-setup/commits/%s' % dotnetVersion)
        repoItem = json.loads(r.read().decode('utf-8'))
        buildTimestamp = repoItem['commit']['committer']['date']

        if buildTimestamp == '' or buildTimestamp is None:
            log('Could not get timestamp for commit %s' % dotnetVersion)
            return 3

        runScript = 'build.py'
        runArgs = [
            python,
            os.path.join(benchviewPath, runScript), 'none',
            '--repository', 'https://github.com/dotnet/core-setup/',
            '--branch', args.branch,
            '--number', dotnetVersion,
            '--source-timestamp', buildTimestamp,
            '--type', 'rolling'
        ]
        run_command(runArgs, runEnv, '%s failed to run' % runScript)

        # Generate machinedata.json
        runScript = 'machinedata.py'
        runArgs = [python, os.path.join(benchviewPath, runScript)]
        run_command(runArgs, runEnv, '%s failed to run' % runScript)

        # Generate measurement.json and submit to benchview
        generate_results_for_benchview(python, 'desc', True, benchmarkOutputDir, benchviewPath)
        upload_to_benchview(python, benchviewPath, 'Windows_NT', 'stopwatch', args.arch, args.runType, args.category)

if __name__ == "__main__":
    Args = parser.parse_args(sys.argv[1:])
    sys.exit(main(Args))
