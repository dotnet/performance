# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.
# See the LICENSE file in the project root for more information.

from dataclasses import dataclass
from glob import glob
from os import environ
from pathlib import Path
from sys import executable as py
from typing import Callable, Iterable, Mapping, Optional, Sequence

from gitignore_parser import parse_gitignore
from pur import update_requirements

from .analysis.generate_metrics_md import update_metrics_md

from .commonlib.command import (
    Command,
    CommandKind,
    CommandsMapping,
    validate_all_commands_are_documented,
)
from .commonlib.config import GC_PATH, SRC_PATH
from .commonlib.document_type import update_benchfile_md
from .commonlib.host_info import read_this_machines_host_info
from .commonlib.type_utils import argument, with_slots
from .commonlib.util import (
    ExecArgs,
    exec_and_get_output,
    exec_cmd,
    ExecError,
    get_extension,
    walk_files_recursive,
)


@with_slots
@dataclass(frozen=True)
class _LintArgs:
    update_dead_code: bool = argument(default=False, doc="Update dead_code.py")


def _lint(args: _LintArgs) -> None:
    def get_files(ext: str) -> Sequence[Path]:
        return [Path(file) for file in glob(f"{SRC_PATH}/**/*.{ext}", recursive=True)]

    py_files = [
        GC_PATH / "__main__.py",
        GC_PATH / "jupyter_notebook.py",
        *(file for file in get_files("py") if file.name != "dead_code.py"),
    ]
    py_files_minus_jupyter = [file for file in py_files if file.name != "jupyter_notebook.ipynb"]
    files = [*py_files, *get_files("pyi")]

    mypy_path = SRC_PATH / "mypy_stubs"
    dead_code = SRC_PATH / "dead_code.py"

    try:
        mypy_args: Sequence[str] = (
            py,
            "-m",
            "mypy",
            # Store the mypy cache in 'src' to keep root directory pretty
            "--cache-dir",
            str(SRC_PATH / ".mypy_cache"),
            "--strict",
            "--strict-equality",
            *(str(f) for f in files),
        )

        exec_cmd(ExecArgs(mypy_args, env={**environ, "MYPYPATH": str(mypy_path)}, quiet_print=True))

        host_info = read_this_machines_host_info()
        if host_info.n_logical_processors > 63:
            print(
                "WARN: Not running 'black' as that crashes for over 63 processors\n"
                "Fixed in https://github.com/python/black/issues/564 but not yet published"
            )
        else:
            exec_cmd(
                ExecArgs(
                    (py, "-m", "black", "--line-length", "100", *(str(f) for f in files)),
                    quiet_print=True,
                )
            )
        # pylint reports silly errors in interface files, like unused-argument
        # `-j 0` means it can run in parallel
        exec_cmd(
            ExecArgs(
                (py, "-m", "pylint", "-j", "0", *(str(f) for f in py_files)),
                quiet_print=True,
                # pylintrc is in SRC_PATH, so must set cwd there
                cwd=SRC_PATH,
            )
        )

        if args.update_dead_code:
            output = exec_and_get_output(
                ExecArgs(
                    (
                        py,
                        "-m",
                        "vulture",
                        *(str(f) for f in py_files_minus_jupyter),
                        "--make-whitelist",
                    )
                ),
                expect_exit_code=1,
            )
            dead_code.write_text(output)
        else:
            try:
                exec_cmd(
                    ExecArgs(
                        (
                            py,
                            "-m",
                            "vulture",
                            *(str(f) for f in py_files_minus_jupyter),
                            str(dead_code),
                        ),
                        quiet_print=True,
                    )
                )
            except ExecError:
                print(
                    "Hint: Remove the code, or to accept this, run 'py . lint --update-dead-code'."
                )
                raise

        validate_all_commands_are_documented()
        update_metrics_md()
        update_benchfile_md()
        _update_dependencies()
        _check_license()
    except ExecError:
        print("failed")
    else:
        print("passed")


_C_H_LICENSE = """
/* Licensed to the .NET Foundation under one or more agreements.
   The .NET Foundation licenses this file to you under the MIT license.
   See the LICENSE file in the project root for more information. */
""".strip()

_CS_LICENSE = """
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
""".strip()

_PY_LICENSE = _CS_LICENSE.replace("//", "#")

# Taken from https://docs.opensource.microsoft.com/releasing/copyright-headers.html
LICENSE_FOR_EXTENSION: Mapping[str, str] = {
    ".c": _C_H_LICENSE,
    ".h": _C_H_LICENSE,
    ".cs": _CS_LICENSE,
    ".py": _PY_LICENSE,
}


def _check_license() -> None:
    """
    Ensure that every file that needs a license has one.
    """
    _update_all_files_by_license(
        "adding license text",
        # For files consisting of *only* the license, the second \n will be omitted.
        lambda full_license_text, text: full_license_text + text
        if not text.startswith(full_license_text) and text != full_license_text[:-1]
        else None,
    )


def _all_non_ignored_files() -> Iterable[Path]:
    """All source files, whether Python, C, or C#."""
    matches = parse_gitignore(GC_PATH / ".gitignore")
    return walk_files_recursive(
        GC_PATH,
        # '.git' should be implicit in gitignore, but parse_gitignore doesn't handle that
        filter_dir=lambda dir_path: dir_path.name != ".git" and not matches(dir_path),
    )


def _update_all_files_by_license(descr: str, cb: Callable[[str, str], Optional[str]]) -> None:
    """cb: Takes license text and file text and returns new file text"""
    for path in _all_non_ignored_files():
        license_text = LICENSE_FOR_EXTENSION.get(get_extension(path), None)
        if license_text is not None:
            full_license_text = license_text + "\n\n"
            text = path.read_text(encoding="utf-8")
            new_text = cb(full_license_text, text)
            if new_text is not None:
                print(f"{path}: {descr}")
                path.write_text(new_text, encoding="utf-8")


_REQUIREMENTS = "src/requirements.txt"


def _update_dependencies() -> None:
    update_mapping = update_requirements(input_file=_REQUIREMENTS)
    for updates in update_mapping.values():
        for update in updates:
            print(update["message"])
            print(f"Please run:\n\npy -m pip install -r {_REQUIREMENTS}")


def _remove_license() -> None:
    _update_all_files_by_license(
        "removing_license",
        lambda full_license_text, text: text[len(full_license_text) :]
        if text.startswith(full_license_text)
        else None,
    )


LINT_COMMANDS: CommandsMapping = {
    "lint": Command(
        kind=CommandKind.infra,
        fn=_lint,
        doc="""
        Run this before committing.
        * Runs python linting tools: mypy, pylint, black, and vulture.
        * Checks that all files have a license.
        * Updates `requirements.txt`.
        * Automatically generates some files in `docs`.
        * Updates `requirements.txt` to use latest versions of dependencies.
        """,
    ),
    # Useful if the license needs to be changed.
    # Remove, then change the license text, then lint to add it.
    "remove-license": Command(
        hidden=True,
        kind=CommandKind.infra,
        fn=_remove_license,
        doc="""
        Removes license text from all files.
        Useful for changing license text --
        remove, change the license text, then lint to add it back.
        """,
    ),
}
