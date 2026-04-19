'''
Pre-commands for MAUI Desktop BenchmarkDotNet benchmarks.
Kept minimal — all heavy lifting (clone, build, patch, run) is in test.py
to keep the correlation payload small.
'''
import argparse
from performance.logger import setup_loggers, getLogger

setup_loggers(True)
log = getLogger(__name__)

if __name__ == '__main__':
    parser = argparse.ArgumentParser(description='MAUI Desktop BDN Benchmarks - Pre-commands')
    parser.add_argument('-f', '--framework', default='net11.0',
                        help='Target .NET framework (determines MAUI branch)')
    args = parser.parse_args()
    log.info(f'MAUI Desktop BDN Benchmarks pre-commands (framework={args.framework})')
    log.info('Setup deferred to test.py to minimize correlation payload.')

