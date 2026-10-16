import os
from unittest.mock import Mock

from scripts import machine_health


def test_fallback_venv_does_not_inherit_python_path(monkeypatch, tmp_path):
    venv_dir = tmp_path / "machine-health-venv"
    python_exe = venv_dir / "bin" / "python"
    python_exe.parent.mkdir(parents=True)
    python_exe.touch()

    monkeypatch.setenv("PYTHONHOME", "/helix/python")
    monkeypatch.setenv("PYTHONPATH", "/etc/helix/scripts")
    monkeypatch.setattr(machine_health.tempfile, "mkdtemp", lambda **_: str(venv_dir))
    monkeypatch.setattr("venv.create", Mock())
    monkeypatch.setattr(machine_health.shutil, "rmtree", Mock())

    completed = Mock(returncode=0)
    run = Mock(return_value=completed)
    monkeypatch.setattr(machine_health.subprocess, "run", run)

    assert machine_health._write_semaphore_with_new_venv("PERFVIPER001", "reason")

    assert run.call_count == 3
    for call in run.call_args_list:
        environment = call.kwargs["env"]
        assert "PYTHONHOME" not in environment
        assert "PYTHONPATH" not in environment
        assert environment["PATH"] == os.environ["PATH"]
