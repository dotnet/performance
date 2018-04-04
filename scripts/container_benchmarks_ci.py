from os import path
import util
import subprocess
import time
import logging

logging.basicConfig(level=logging.INFO, format="[%(asctime)s][%(levelname)s] %(message)s", datefmt="%Y-%m-%d %H:%M")
logger = logging.getLogger(__name__)

if __name__ == "__main__":
    script_dir = path.dirname(path.realpath(__file__))
    benchmark_dir = path.normpath(path.join(script_dir, '..', 'src', 'docker'))
    benchmark_reports_dir = path.normpath(path.join(benchmark_dir, 'reports'))

    logger.info("Running dotnet containers benchmark")
    util.dotnet(['run'], cwd=benchmark_dir, stdout=None, stderr=None)

    logger.info("Generating submission metadata")
    util.generate_metadata(
        name='Container Size Benchmark',
        user_email='dotnet-bot@microsoft.com'
    )

    logger.info("Generating machine data")
    util.generate_machinedata()

    logger.info("Generating build data")
    timestamp = time.strftime('%Y-%m-%dT%H:%M:%SZ')
    util.generate_build(
        branch='master',
        number=timestamp,
        timestamp=timestamp,
        type='rolling',
        repository='https://github.com/dotnet/performance'
    )

    logger.info("Generating measurement json file")
    util.generate_measurement_csv(
        datafile=path.join(benchmark_reports_dir, 'benchview.csv'),
        metric='size',
        unit='bytes',
        ascending=False,
    )

    logger.info("Generating submission json file")
    config = util.docker_info(
        "ServerVersion",
        "OperatingSystem",
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

    logger.info("Uploading submission to BenchView")
    util.upload(
        container="dotnetcli"
    )
