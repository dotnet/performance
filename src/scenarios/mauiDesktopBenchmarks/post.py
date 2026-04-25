'''
Post-commands for MAUI Desktop BenchmarkDotNet benchmarks.
Cleans up the cloned maui repo and temporary artifacts.
'''
import os
from performance.common import remove_directory
from performance.logger import setup_loggers, getLogger

setup_loggers(True)
log = getLogger(__name__)

MAUI_REPO_DIR = 'maui_repo'


def cleanup():
    """Remove the cloned maui repository and any leftover artifacts."""
    if os.path.exists(MAUI_REPO_DIR):
        log.info(f'Removing cloned MAUI repo: {MAUI_REPO_DIR}')
        remove_directory(MAUI_REPO_DIR)

    # Clean up combined report if still in working directory
    combined = 'combined-perf-lab-report.json'
    if os.path.exists(combined):
        os.remove(combined)

    log.info('Post-commands cleanup complete.')


if __name__ == '__main__':
    cleanup()
