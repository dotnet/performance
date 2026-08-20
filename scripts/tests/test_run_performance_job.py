from pathlib import Path
import sys

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))

from scripts.run_performance_job import get_pre_commands


def test_ubuntu_pre_commands_install_venv_package():
    commands = get_pre_commands("linux", "ubuntu", True, "", "", "Release", "")

    assert any("sudo apt-get -y install python3-pip python3-venv" in command for command in commands)


def test_azure_linux_pre_commands_do_not_use_apt_packages():
    commands = get_pre_commands("linux", "azurelinux", True, "", "", "Release", "")

    assert any("sudo tdnf -y install python3-pip" in command for command in commands)
    assert not any("python3-venv" in command for command in commands)


def test_windows_pre_commands_do_not_install_linux_venv_package():
    commands = get_pre_commands("windows", None, True, "", "", "Release", "")

    assert not any("python3-venv" in command for command in commands)
