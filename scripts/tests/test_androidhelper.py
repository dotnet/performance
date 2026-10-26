import importlib
import logging
import os
from pathlib import Path
import stat
import subprocess
import sys
from unittest.mock import Mock, call

import pytest


@pytest.fixture
def androidhelper(monkeypatch):
    repo_root = Path(__file__).resolve().parents[2]
    monkeypatch.syspath_prepend(str(repo_root / "scripts"))
    monkeypatch.syspath_prepend(str(repo_root / "src" / "scenarios"))
    return importlib.import_module("shared.androidhelper")


def test_failed_warmup_logs_output_without_pipes_and_preserves_error(
    androidhelper, monkeypatch, caplog
):
    command = [
        sys.executable, "-c",
        "import sys; print('probe stdout'); "
        "print('probe diagnostic', file=sys.stderr); sys.exit(23)",
    ]
    monkeypatch.setattr(androidhelper, "xharness_adb", lambda: command)
    original_run = subprocess.run
    outputs = []

    def run_without_pipes(*args, **kwargs):
        output = kwargs["stdout"]
        assert stat.S_ISREG(os.fstat(output.fileno()).st_mode)
        assert kwargs["stderr"] == subprocess.STDOUT
        outputs.append(output)
        return original_run(*args, **kwargs)

    monkeypatch.setattr(androidhelper.subprocess, "run", run_without_pipes)
    helper = androidhelper.AndroidHelper()
    with caplog.at_level(logging.INFO), pytest.raises(subprocess.CalledProcessError) as failure:
        try:
            helper.setup_device("test.package", "test.apk", False)
        finally:
            helper.close_device()

    assert failure.value.returncode == 23
    assert failure.value.cmd == command + ["shell", "echo", "Hello World"]
    assert "probe stdout" in caplog.messages
    assert "probe diagnostic" in caplog.messages
    assert len(outputs) == 1
    assert outputs[0].closed


def test_successful_warmup_logs_output_then_continues(androidhelper, monkeypatch, caplog):
    command = [sys.executable, "-c", "print('probe succeeded')"]
    monkeypatch.setattr(androidhelper, "xharness_adb", lambda: command)
    next_command = Mock(side_effect=RuntimeError("stop after warmup"))
    monkeypatch.setattr(androidhelper, "RunCommand", next_command)

    with caplog.at_level(logging.INFO), pytest.raises(RuntimeError, match="stop after warmup"):
        androidhelper.AndroidHelper().setup_device("test.package", "test.apk", False)

    assert "probe succeeded" in caplog.messages
    next_command.assert_called_once_with(command + ["shell", "wm", "size"], verbose=True)


def test_missing_warmup_executable_is_not_masked(androidhelper, monkeypatch, tmp_path):
    monkeypatch.setattr(androidhelper, "xharness_adb", lambda: [str(tmp_path / "missing-xharness")])
    helper = androidhelper.AndroidHelper()

    with pytest.raises(FileNotFoundError):
        try:
            helper.setup_device("test.package", "test.apk", False)
        finally:
            helper.close_device()


def test_skipped_warmup_does_not_run_probe(androidhelper, monkeypatch):
    probe = Mock()
    monkeypatch.setattr(androidhelper.subprocess, "run", probe)
    monkeypatch.setattr(androidhelper, "xharness_adb", lambda: ["adb"])
    next_command = Mock(side_effect=RuntimeError("stop before device settings"))
    monkeypatch.setattr(androidhelper, "RunCommand", next_command)

    with pytest.raises(RuntimeError, match="stop before device settings"):
        androidhelper.AndroidHelper().setup_device(
            "test.package", "test.apk", False,
            skip_xharness_warmup=True, skip_package_verifier=True,
        )

    probe.assert_not_called()
    next_command.assert_called_once_with(
        ["adb", "shell", "settings", "get", "global", "window_animation_scale"],
        verbose=True,
    )


@pytest.mark.parametrize("initialized", [False, True])
@pytest.mark.parametrize("skip_uninstall", [False, True])
def test_cleanup_only_stops_initialized_app(
    androidhelper, monkeypatch, initialized, skip_uninstall
):
    run = Mock()
    monkeypatch.setattr(androidhelper, "RunCommand", run)
    monkeypatch.setattr(androidhelper, "xharness_adb", lambda: ["xharness", "android", "adb", "--"])
    monkeypatch.setattr(androidhelper, "xharnesscommand", lambda: ["xharness"])
    helper = androidhelper.AndroidHelper()
    helper.packagename = "test.package"
    if initialized:
        helper.stopappcommand = ["adb", "shell", "am", "force-stop", helper.packagename]

    helper.close_device(skip_uninstall=skip_uninstall)

    expected = []
    if initialized and not skip_uninstall:
        expected = [
            call(helper.stopappcommand, verbose=True),
            call(["xharness", "android", "uninstall", "--package-name", "test.package"], verbose=True),
        ]
    assert run.call_args_list == expected


def test_partial_setup_still_restores_saved_settings(androidhelper, monkeypatch):
    run = Mock()
    monkeypatch.setattr(androidhelper, "RunCommand", run)
    monkeypatch.setattr(androidhelper, "xharness_adb", lambda: ["adb"])
    helper = androidhelper.AndroidHelper()
    helper.packagename = "test.package"
    helper.startwindowanimationscale = "0.5"

    helper.close_device()

    run.assert_called_once_with(
        ["adb", "shell", "settings", "put", "global", "window_animation_scale", "0.5"],
        verbose=True,
    )
