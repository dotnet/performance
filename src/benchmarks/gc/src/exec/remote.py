# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.
# See the LICENSE file in the project root for more information.

"""
Exmple usage:
py remote.py run-tests
    --username redmond/yourname
    --password swordfish
    --machine file:$work/machines.yaml
        [or `--machine machine1 machine2` -- do not prefix machine names with `\\`]
    --bench-name gen0size
    [--parallel --just-build]
"""

from concurrent.futures import ThreadPoolExecutor
from dataclasses import dataclass
from getpass import getuser, getpass
from textwrap import indent
from typing import Callable, Sequence

from ..commonlib.bench_file import Machine, MACHINE_DOC, parse_machines_arg
from ..commonlib.collection_util import split_once
from ..commonlib.command import (
    check_command_parses,
    Command,
    CommandKind,
    CommandsMapping,
    parse_command_args,
)
from ..commonlib.config import GC_PATH
from ..commonlib.type_utils import argument, T, U, with_slots
from ..commonlib.util import ExecArgs, exec_and_get_output, exec_cmd

# Assuming these are always in these locations
_PY = "C:/WINDOWS/py.exe"
_GIT = "C:/Program Files/Git/cmd/git.exe"


@with_slots
@dataclass(frozen=True)
class _CommonArgs:
    machine: Sequence[str] = argument(doc=MACHINE_DOC)
    parallel: bool = argument(
        default=False,
        doc="""
    If true, run the command on all machines at the same time.
    If false, wait for one machine to finish before starting on the next.
    Defaults to false as it is easier to debug that way if the command fails.
    """,
    )


@with_slots
@dataclass(frozen=True)
class _UserAndPass:
    username: str
    password: str


def _get_user_and_pass() -> _UserAndPass:
    # TODO: don't hardcode 'redmond'. How to determine that?
    return _UserAndPass(username=f"redmond\\{getuser()}", password=getpass())


def _prefix(up: _UserAndPass) -> Sequence[str]:
    return (
        "psexec",
        "-u",
        up.username,
        "-p",
        up.password,
        "-w",
        str(GC_PATH),  # Assumes this is in the same directory on all machines
    )


def _do_run(args: _CommonArgs, up: _UserAndPass, cmd: Sequence[str]) -> None:
    pfx = _prefix(up)
    machines = parse_machines_arg(args.machine)

    if args.parallel:
        outputs = parallel_map_threaded(
            lambda machine: exec_and_get_output(_exec_args_for_machine(pfx, machine, cmd)), machines
        )
        print("DONE")
        for output, machine in zip(outputs, machines):
            print(f"Output for {machine.name}:")
            print(indent(output, "\t"))
    else:
        # psexec can take all machines in one command,
        # but will continue running on the next machine even if you press ctrl-c to cancel.
        # So use a loop to give python the opportunity to exit.
        for machine in machines:
            exec_cmd(_exec_args_for_machine(pfx, machine, cmd))


def _exec_args_for_machine(prefix: Sequence[str], machine: Machine, cmd: Sequence[str]) -> ExecArgs:
    print(f"psexec on {machine.name}: {' '.join(cmd)}")
    # We want this quiet because it passes the password to psexec
    return ExecArgs((*prefix, f"\\\\{machine.name}", *cmd), quiet_print=True)


@with_slots
@dataclass(frozen=True)
class RemoteRestartArgs:
    # Apparently I don't need the password to force them to restart?
    machine: Sequence[str] = argument(doc="Names of machines to restart")


# restart-computer is a cmdlet and not a real executable, so have to invoke powershell
_POWERSHELL = "C:/WINDOWS/System32/WindowsPowerShell/v1.0/powershell.exe"


def remote_restart(args: RemoteRestartArgs) -> None:
    machines = parse_machines_arg(args.machine)
    print(
        "This will forcibly restart all remote machines.\n"
        + f"Those are: {','.join([m.name for m in machines])}\n"
        + "Type 'pretty please' 3 times if you really want to do this"
    )
    for _ in range(3):
        assert input() == "pretty please"
    for m in machines:
        exec_cmd(ExecArgs((_POWERSHELL, "restart-computer", "-computername", m.name, "-force")))


def _update_this_repository(args: _CommonArgs, up: _UserAndPass) -> None:
    _do_run(args, up, ("git", "pull"))
    _do_run(args, up, (_PY, "-m", "pip", "install", "-r", "requirements.txt"))


@with_slots
@dataclass(frozen=True)
class RemoteDoArgs(_CommonArgs):
    no_update: bool = argument(default=False, doc="Skips calling 'git pull' to save time.")


def remote_do(argv: Sequence[str]) -> None:
    # Loading this lazily as it imports this
    from ..all_commands import ALL_COMMANDS

    # Separated by a `--`
    local_argv, remote_cmd_and_argv = split_once(argv, lambda a: a == "--")
    local_args = parse_command_args("remote-do", RemoteDoArgs, local_argv)
    # We could just take remote_argv as is, but parse it to catch errors early

    check_command_parses(ALL_COMMANDS, remote_cmd_and_argv)
    up = _get_user_and_pass()
    if not local_args.no_update:
        _update_this_repository(local_args, up)
    _do_run(local_args, up, (_PY, ".", *remote_cmd_and_argv))


def parallel_map_threaded(f: Callable[[T], U], s: Sequence[T]) -> Sequence[U]:
    """Run 'f' in separate threads."""
    with ThreadPoolExecutor() as executor:
        futures = [executor.submit(f, x) for x in s]
        return [f.result() for f in futures]


EXEC_ON_MACHINES_COMMANDS: CommandsMapping = {
    "remote-restart": Command(
        hidden=True,
        kind=CommandKind.infra,
        fn=remote_restart,
        doc="Forcibly restart remote machines.",
    ),
    "remote-do": Command(
        kind=CommandKind.run,
        fn=remote_do,
        doc="""
    Runs a command on remote machines.
    This only works on Windows machines. (Both you and the remote machine should be Windows.)
    """,
        detail="""
    The syntax looks like:
    
        py . remote-do --machine name:gcintel480 name:gcamd240 -- run //mycomputername/bench/sample.yaml

    The `--` separates the arguments to remote-do from the command the remote machine should run.

    Every remote machine should:
    * Have the performance repository installed at the same path.
    * Have already run `py . setup`.
    * Have the required coreclrs checked out and built, at the same path on all machines.

    remote-do will do a `git pull` to ensure remote machines have the latest infra;
    and the infra always automatically rebuilds any non-python dependencies when they are changed.

    When you specify paths in a command they may be UNC paths, such as //mycomputername/bench/sample.yaml.
    Sharing should be enabled for that directory.
    
    When remote computers run tests, they write to *their* corresponding directory --
    they don't write to the calling machine's directory.
    """,
        priority=2,
    ),
}
