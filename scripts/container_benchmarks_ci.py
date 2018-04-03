from os import path, environ
import util
import subprocess
import time

if __name__ == "__main__":
    script_dir = path.dirname(path.realpath(__file__))
    benchmark_dir = path.normpath(path.join(script_dir, '..', 'src', 'docker'))
    benchmark_reports_dir = path.normpath(path.join(benchmark_dir, 'reports'))

    #util.dotnet(['run'], cwd=benchmark_dir, stdout=None, stderr=None)

    util.generate_metadata(
        name='Container Size Benchmark',
        user_email='dotnet-bot@microsoft.com'
    )

    util.generate_machinedata()

    timestamp = time.strftime('%Y-%m-%dT%H:%M:%SZ')
    util.generate_build(
        branch='master',
        number=timestamp,
        timestamp=timestamp,
        type='rolling',
        repository='https://github.com/dotnet/performance'
    )

    util.generate_measurement_csv(
        datafile=path.join(benchmark_reports_dir, 'benchview.csv'),
        metric='size',
        unit='bytes',
        ascending=False,
    )

    config = util.docker_info(
        "Server Version",
        "Storage Driver",
        "Operating System",
        "OSType",
        "Architecture"
    )
    util.generate_submission(
        group="Containers",
        type="rolling",
        config_name="docker",
        config=config,
        arch=config["Architecture"],
        machinepool='perfsnake',
    )

    util.upload(
        container="dotnetcli"
    )
