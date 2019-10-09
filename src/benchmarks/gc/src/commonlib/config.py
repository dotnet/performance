# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.
# See the LICENSE file in the project root for more information.

from os.path import isabs, realpath
from pathlib import Path

from .util import assert_file_exists

# Use this for things that should be distributed as part of the .exe
SRC_PATH = Path(realpath(__file__)).parent.parent
GC_PATH = SRC_PATH.parent

assert (
    GC_PATH.name == "gc"
    and GC_PATH.parent.name == "benchmarks"
    and GC_PATH.parent.parent.name == "src"
)
PERFORMANCE_PATH = GC_PATH.parent.parent.parent

DOCS_PATH = GC_PATH / "docs"
EXEC_PATH = SRC_PATH / "exec"
EXEC_ENV_PATH = EXEC_PATH / "env"

CWD = Path.cwd()

DEPENDENCIES_PATH = SRC_PATH / "dependencies"
PERFVIEW_PATH = DEPENDENCIES_PATH / "PerfView.exe"
SIGCHECK64_PATH = DEPENDENCIES_PATH / "sigcheck64.exe"

# This dir is in .gitignore, so we use it for files we don't want to be checked in
BENCH_DIR_PATH = GC_PATH / "bench"
HOST_INFO_PATH = BENCH_DIR_PATH / "host_info.yaml"

# Downloading sigcheck is optional.
def sigcheck_exists() -> bool:
    return SIGCHECK64_PATH.exists()


def _get_path(rel: object) -> Path:
    assert isinstance(rel, str)
    return assert_file_exists(Path(rel) if isabs(rel) else GC_PATH / rel)
