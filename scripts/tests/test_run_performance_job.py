import pytest

from scripts.run_performance_job import APT_LOCK_TIMEOUT_OPTION, get_bdn_arguments, get_pre_commands


@pytest.mark.parametrize("os_group", ["windows", "linux"])
@pytest.mark.parametrize(("framework", "runtime"), [
    ("net11.0", "r2r11_0"), ("net12.0", "r2r12_0"),
])
def test_r2r_interpreter_matches_target_and_preserves_toolchain(os_group, framework, runtime):
    arguments = get_bdn_arguments(
        run_categories="libraries",
        internal=True,
        os_group=os_group,
        runtime_type="coreclr_r2r_interpreter",
        codegen_type="Interpreter",
        framework=framework,
    )

    assert arguments[arguments.index("--runtimes") + 1] == runtime
    assert arguments[arguments.index("--msbuild-arguments") + 1] == (
        "/p:PublishReadyToRunCrossgen2ExtraArgs=--target-allows-runtime-code-generation:false")
    prefix = (
        "%HELIX_CORRELATION_PAYLOAD%\\r2r_interpreter\\" if os_group == "windows"
        else "$HELIX_CORRELATION_PAYLOAD/r2r_interpreter/")
    assert arguments[arguments.index("--customruntimepack") + 1] == prefix + "runtimepack"
    assert arguments[arguments.index("--aotcompilerpath") + 1] == prefix + "crossgen2"


def test_r2r_interpreter_requires_an_explicit_framework():
    with pytest.raises(ValueError, match="target framework is required"):
        get_bdn_arguments(
            run_categories="libraries",
            internal=True,
            os_group="windows",
            runtime_type="coreclr_r2r_interpreter",
            codegen_type="Interpreter",
        )


def get_generated_apt_commands(*, internal: bool, runtime_type: str) -> list[str]:
    pre_commands = get_pre_commands(
        os_group="linux",
        os_distro="ubuntu",
        internal=internal,
        runtime_type=runtime_type,
        codegen_type="jit",
        build_config="Release",
        v8_version="12.0.0",
        wasm_local_package_version="11.0.0-ci" if runtime_type == "wasm_coreclr" else None,
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


def test_linux_prerequisite_failure_remains_failed_after_health_check():
    pre_commands = get_pre_commands(
        os_group="linux",
        os_distro="ubuntu",
        internal=True,
        runtime_type="coreclr",
        codegen_type="jit",
        build_config="Release",
        v8_version="12.0.0",
    )

    health_check = next(
        command for command in pre_commands if "machine_health.py" in command
    )
    assert health_check.endswith("check || true; exit 1; fi")
