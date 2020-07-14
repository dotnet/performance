# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.
# See the LICENSE file in the project root for more information.

from dataclasses import dataclass
from datetime import datetime
from pathlib import Path
from typing import Sequence
from webbrowser import open as open_in_browser
from urllib.parse import urlencode

from .analysis.analyze_cpu_samples import ANALYZE_CPU_SAMPLES_COMMANDS
from .analysis.analyze_joins import ANALYZE_JOINS_COMMANDS
from .analysis.analyze_single import ANALYZE_SINGLE_COMMANDS
from .analysis.chart_configs import CHART_CONFIGS_COMMANDS
from .analysis.chart_individual_gcs import CHART_INDIVIDUAL_GCS_COMMANDS
from .analysis.condemned_reasons import CONDEMNED_REASONS_COMMANDS
from .analysis.core_analysis import PROCESS_DOC, process_predicate_from_parts
from .analysis.gui_server import get_app, is_port_used
from .analysis.mem_utils import MEM_UTILS_COMMANDS
from .analysis.report import REPORT_COMMANDS
from .analysis.trace_commands import TRACE_COMMANDS

from .commonlib.get_built import (
    BUILD_COMMANDS,
    get_corerun_path_from_core_root,
    get_sigcheck_output,
)
from .commonlib.config import sigcheck_exists
from .commonlib.collection_util import combine_mappings
from .commonlib.command import Command, CommandKind, CommandsMapping, HELP_COMMANDS
from .commonlib.host_info import HOST_INFO_COMMANDS
from .commonlib.setup import SETUP_COMMANDS
from .commonlib.type_utils import argument, with_slots

from .exec.remote import EXEC_ON_MACHINES_COMMANDS
from .exec.generate_tests import GENERATE_COMMANDS
from .exec.run_tests import RUN_TESTS_COMMANDS

from .lint import LINT_COMMANDS
from .suite import SUITE_COMMANDS


@with_slots
@dataclass(frozen=True)
class _SigcheckArgs:
    path: Path = argument(
        name_optional=True, doc="Path to a Core_Root directory built from coreclr."
    )


def _sigcheck(args: _SigcheckArgs) -> None:
    assert sigcheck_exists()
    print(get_sigcheck_output(get_corerun_path_from_core_root(args.path)))


@with_slots
@dataclass(frozen=True)
class _GuiArgs:
    trace_file: Path = argument(doc="Path to trace file.", name_optional=True)
    process: Sequence[str] = argument(doc=PROCESS_DOC)


def _gui(args: _GuiArgs) -> None:
    if 1 + 1 == 2:
        raise Exception("Won't work, gui code was removed")

    port = 5000
    app = get_app()
    pid = app.load_etl_and_get_process_id[0](
        args.trace_file, process_predicate_from_parts(args.process)
    )

    assert not is_port_used(port)
    params = {"etlPath": args.trace_file, "pid": pid}
    open_in_browser(f"http://localhost:{port}/gui/index.html?{urlencode(params)}")

    app.app.run(port=port)


@with_slots
@dataclass(frozen=True)
class _GreetArgs:
    name: str = argument(name_optional=True, doc="Your name.")
    time: bool = argument(default=False, doc="Enable to tell you the time.")


def _greet(args: _GreetArgs) -> None:
    print(f"Hello, {args.name}!")
    if args.time:
        print(f"It is {datetime.now()}")


ALL_COMMANDS: CommandsMapping = combine_mappings(
    {
        "greet": Command(
            hidden=True,
            kind=CommandKind.infra,
            fn=_greet,
            doc="""
            Say hello.
            This is used as a sample command.
            """,
        ),
        "gui": Command(
            hidden=True,
            kind=CommandKind.analysis,
            fn=_gui,
            doc="""
        EXPERIMENTAL
        Shows a single trace file in a web GUI.
        """,
        ),
        "sigcheck": Command(
            hidden=True,
            kind=CommandKind.infra,
            fn=_sigcheck,
            doc="""
        Runs sigcheck on a provided coreclr repo to test if it is up-to-date.
        """,
        ),
    },
    ANALYZE_CPU_SAMPLES_COMMANDS,
    ANALYZE_JOINS_COMMANDS,
    ANALYZE_SINGLE_COMMANDS,
    BUILD_COMMANDS,
    CHART_CONFIGS_COMMANDS,
    CHART_INDIVIDUAL_GCS_COMMANDS,
    CONDEMNED_REASONS_COMMANDS,
    EXEC_ON_MACHINES_COMMANDS,
    GENERATE_COMMANDS,
    HELP_COMMANDS,
    HOST_INFO_COMMANDS,
    LINT_COMMANDS,
    MEM_UTILS_COMMANDS,
    REPORT_COMMANDS,
    RUN_TESTS_COMMANDS,
    SETUP_COMMANDS,
    SUITE_COMMANDS,
    TRACE_COMMANDS,
)
