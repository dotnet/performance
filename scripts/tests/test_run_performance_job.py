import pytest

from scripts.run_performance_job import APT_LOCK_TIMEOUT_OPTION, get_pre_commands


def get_generated_apt_commands(*, internal: bool, runtime_type: str) -> list[str]:
    pre_commands = get_pre_commands(
        os_group="linux",
        os_distro="ubuntu",
        internal=internal,
        runtime_type=runtime_type,
        codegen_type="jit",
        build_config="Release",
        v8_version="12.0.0",
    )

    return [
        command.strip()
        for pre_command in pre_commands
        for command in pre_command.split(" && ")
        if command.strip().startswith(("sudo apt ", "sudo apt-get "))
    ]


@pytest.mark.parametrize(
    ("internal", "runtime_type", "expected_command_count"),
    [
        (True, "coreclr", 4),
        (False, "wasm", 6),
        (True, "wasm", 10),
        (False, "wasm_coreclr", 6),
    ],
)
def test_all_generated_apt_commands_use_lock_timeout(
    internal: bool, runtime_type: str, expected_command_count: int
):
    apt_commands = get_generated_apt_commands(
        internal=internal, runtime_type=runtime_type
    )

    assert len(apt_commands) == expected_command_count
    assert all(
        f" {APT_LOCK_TIMEOUT_OPTION} " in command for command in apt_commands
    )


def test_generated_prerequisites_do_not_poll_dpkg_lock():
    pre_commands = get_pre_commands(
        os_group="linux",
        os_distro="ubuntu",
        internal=True,
        runtime_type="wasm",
        codegen_type="jit",
        build_config="Release",
        v8_version="12.0.0",
    )

    prerequisites = "\n".join(pre_commands)
    assert "fuser" not in prerequisites
    assert "Waiting for dpkg" not in prerequisites
