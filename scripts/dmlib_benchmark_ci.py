#!/usr/bin/env python3

from os import path, environ
import util
import subprocess
import time
import platform
import logging

logging.basicConfig(level=logging.INFO, format="[%(asctime)s][%(levelname)s] %(message)s", datefmt="%Y-%m-%d %H:%M")
logger = logging.getLogger(__name__)

if __name__ == "__main__":
    script_dir = path.dirname(path.realpath(__file__))
    benchmark_dir = path.normpath(path.join(script_dir, '..', 'src', 'dmlib'))
    packages_dir = path.normpath(path.join(script_dir, '..', 'packages'))
    reports_dir = path.normpath(path.join(benchmark_dir, 'reports'))

    logger.info("Installing dotnet cli")
    dotnet_exe = util.aquire_dotnet('master')
    dotnet_commit = util.dotnet_commit(dotnet_exe)
    logger.info("Installed dotnet with commit %s", dotnet_commit)
    
    # Git information
    git_url = 'https://github.com/nategraf/azure-storage-net-data-movement'
    git_branch = 'download-perf'
    git_dst = path.join(packages_dir, 'azure-storage-net-data-movement')
    
    if not path.isdir(git_dst):
        logger.info("Cloning dmlib source code from %s", git_url)
        util.cmd(['git', 'clone', git_url, git_dst])
    
    logger.info("Checking out %s", git_branch)
    util.cmd(['git', 'checkout', git_branch], cwd=git_dst)
    
    git_commit = util.cmd(['git', 'log', '-1', '--pretty=format:%H'], cwd=git_dst).stdout.decode('utf-8').strip()
    logger.info("Obtained commit %s", git_commit)

    logger.info("Running dmlib benchmark")
    env = dict(environ)
    env.update({
        'NUM_FILES': '10',
        'ITERATIONS': '5',
        'FILE_SIZE': '104857600',
        'BENCHMARK_ACCOUNT': environ['BENCHMARK_ACCOUNT'],
        'BENCHMARK_SAS_TOKEN': environ['BENCHMARK_SAS_TOKEN']
    })
    util.cmd(
        [dotnet_exe, 'run'],
        cwd=benchmark_dir,
        stdout=None,
        stderr=None,
        env=env
    )

    logger.info("Generating submission metadata")
    util.generate_metadata(
        name='Azure Data Movement Library Benchmark',
        user_email='dotnet-bot@microsoft.com'
    )

    logger.info("Generating machine data")
    util.generate_machinedata()

    logger.info("Generating build data")
    timestamp = time.strftime('%Y-%m-%dT%H:%M:%SZ')
    util.generate_build(
        branch='master',
        number=dotnet_commit,
        timestamp=timestamp,
        type='rolling',
        repository='https://github.com/dotnet/performance'
    )

    logger.info("Generating measurement json file")
    util.generate_measurement_csv(
        datafile=path.join(reports_dir, 'benchview.csv'),
        metric='Average Throughput',
        unit='Mbps',
        ascending=False,
    )

    logger.info("Generating submission json file")
    config = {
        'Transport': 'HTTP',
        'Region': 'US West',
        'Number of Files': '50',
        'File Size': '100 MB'
    }
    util.generate_submission(
        group="Network",
        type="rolling",
        config_name="standard",
        config=config,
        arch=platform.machine(),
        machinepool='perfsnake'
    )

    logger.info("Uploading submission to BenchView")
    util.upload(
        container="dotnetcli"
    )
