#!/usr/bin/env python3

import os
import sys

from performance.common import get_repo_root_path
from performance.common import push_dir
from performance.common import RunCommand

def _get_gcinfra_path() -> str:
    return os.path.join(get_repo_root_path(), "src", "benchmarks", "gc")

def __main(args: list[str]) -> int:
    infra_base_path = _get_gcinfra_path()
    with push_dir(infra_base_path):
        gcperfsim_path = os.path.join(infra_base_path, "src", "exec", "GCPerfSim")
        gcperf_path = os.path.join(infra_base_path, "src", "analysis", "managed-lib")
        ctools_path = os.path.join(infra_base_path, "src", "exec", "env")

        with push_dir(gcperfsim_path):
            cmdline = ['dotnet', 'build', '-c', 'release']
            RunCommand(cmdline, verbose=True).run()

        with push_dir(gcperf_path):
            cmdline = ['dotnet', 'publish']
            RunCommand(cmdline, verbose=True).run()

        with push_dir(ctools_path):
            cmdline = ['build.cmd']
            RunCommand(cmdline, verbose=True).run()
    return 0

if __name__ == "__main__":
    __main(sys.argv[1:])
