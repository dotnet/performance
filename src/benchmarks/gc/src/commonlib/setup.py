# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.
# See the LICENSE file in the project root for more information.

from pathlib import Path
from subprocess import call
from sys import stdout
from typing import cast
from urllib import request
from zipfile import ZipFile

from .command import Command, CommandKind, CommandsMapping
from .config import PERFVIEW_PATH, SIGCHECK64_PATH
from .get_built import get_platform_name, is_arm
from .host_info import write_host_info
from .util import assert_file_exists, ensure_dir, os_is_windows, unlink_if_exists


def setup() -> None:
    if os_is_windows():
        _download_perfview_and_sigcheck()

    # Need sigcheck to do this. Also, since the host info tools are not
    # available on ARM, skip when on said architecture.
    if not is_arm():
        write_host_info()
    else:
        print(
            f"Warning: Detected you are working on {get_platform_name().upper()}. "
            "Don't forget to write `bench/host_info.yaml` or the tests will "
            "not work."
        )


_PERFVIEW_URL = "https://github.com/microsoft/perfview/releases/download/P2.0.52/PerfView.exe"
_SIGCHECK_URL = "https://download.sysinternals.com/files/Sigcheck.zip"
_SIGCHECK_ZIP_PATH = SIGCHECK64_PATH.parent / "Sigcheck.zip"
_SIGCHECK_PARENT_PATH = SIGCHECK64_PATH.parent
_SIGCHECK_EULA_PATH = _SIGCHECK_PARENT_PATH / "sigcheck_eula.txt"


def _download_perfview_and_sigcheck() -> None:
    for path in (PERFVIEW_PATH, SIGCHECK64_PATH, _SIGCHECK_ZIP_PATH, _SIGCHECK_EULA_PATH):
        unlink_if_exists(path)
        ensure_dir(path.parent)

    _download(_PERFVIEW_URL, PERFVIEW_PATH)

    print("You may optionally download SigCheck now.")
    print(
        "This will be used as an extra safety check to ensure that "
        "coreclr executables match commit hashes specified in benchfiles."
    )
    stdout.write("Download SigCheck? This will require accepting the license. (Y/N) ")
    stdout.flush()
    do_sigcheck = _read_y_n()

    if do_sigcheck:
        _download(_SIGCHECK_URL, _SIGCHECK_ZIP_PATH)
        with ZipFile(_SIGCHECK_ZIP_PATH, "r") as zip_file:
            # Mypy doesn't realize zip can handle paths, so cast to str
            dir_to_extract_to = cast(str, _SIGCHECK_PARENT_PATH)
            zip_file.extract(SIGCHECK64_PATH.name, dir_to_extract_to)
            zip_file.extract("Eula.txt", dir_to_extract_to)
            (_SIGCHECK_PARENT_PATH / "Eula.txt").rename(_SIGCHECK_EULA_PATH)
            assert_file_exists(SIGCHECK64_PATH)

        _SIGCHECK_ZIP_PATH.unlink()

        print("It will now ask you to accept the license:")
        call((str(SIGCHECK64_PATH),))

        key = "Software\\Sysinternals\\sigcheck\\EulaAccepted"
        print(f"To un-accept, remove the registry key {key} and delete {SIGCHECK64_PATH}")


def _download(url: str, path: Path) -> None:
    print(f"Downloading {url} to {path}...")
    request.urlretrieve(url, path)


def _read_y_n() -> bool:
    txt = input().lower()
    if txt == "y":
        return True
    elif txt == "n":
        return False
    else:
        print("Enter Y or N")
        return _read_y_n()


SETUP_COMMANDS: CommandsMapping = {
    "setup": Command(kind=CommandKind.infra, fn=setup, doc="Perform initial setup.")
}
