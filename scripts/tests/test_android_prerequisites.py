import importlib
import importlib.util
import json
import os
from pathlib import Path
import shutil
import subprocess
import sys
from unittest.mock import Mock
import xml.etree.ElementTree as ET

import pytest


repo_root = Path(__file__).resolve().parents[2]


@pytest.fixture
def precommands(monkeypatch):
    monkeypatch.syspath_prepend(str(repo_root / "scripts"))
    monkeypatch.syspath_prepend(str(repo_root / "src" / "scenarios"))
    return importlib.import_module("shared.precommands")


@pytest.mark.parametrize(
    ("platform", "framework", "environment", "msbuild", "expected"),
    [
        ("win32", "net11.0-android", {}, None, "hosted-jdk21"),
        ("win32", "net11.0-android", {"JAVA_HOME": "hosted-jdk8"}, None, "hosted-jdk21"),
        ("win32", "net12.0-android", {}, None, "hosted-jdk21"),
        ("win32", "net10.0-android", {}, None, None),
        ("win32", "net11.0-ios", {}, None, None),
        ("win32", "net11.0", {}, None, None),
        ("linux", "net11.0-android", {}, None, None),
        ("win32", "net11.0-android", {"JavaSdkDirectory": "custom-jdk"}, None, "custom-jdk"),
        ("win32", "net11.0-android", {"JAVA_HOME": "custom-jdk"}, None, None),
        ("win32", "net11.0-android", {"JDK_HOME": "custom-jdk"}, None, None),
        ("win32", "net11.0-android", {}, "/p:JavaSdkDirectory=custom-jdk", None),
        ("win32", "net11.0-android", {"JAVA_HOME_21_X64": ""}, None, None),
    ],
)
def test_hosted_android_jdk_selection(
    precommands, monkeypatch, platform, framework, environment, msbuild, expected
):
    monkeypatch.setenv("JavaSdkDirectory", "")
    for name in ("JavaSdkDirectory", "JAVA_HOME", "JDK_HOME"):
        monkeypatch.delenv(name, raising=False)
    monkeypatch.setenv("JAVA_HOME_8_X64", "hosted-jdk8")
    monkeypatch.setenv("JAVA_HOME_21_X64", "hosted-jdk21")
    for name, value in environment.items():
        monkeypatch.setenv(name, value)
    pre = precommands.PreCommands.__new__(precommands.PreCommands)
    pre.framework = framework
    pre.msbuild = msbuild

    with monkeypatch.context() as platform_patch:
        platform_patch.setattr(precommands.sys, "platform", platform)
        pre._set_android_java_sdk()

    assert os.environ.get("JavaSdkDirectory") == expected


@pytest.fixture
def setup_module():
    path = repo_root / "src/scenarios/mauiandroidinnerloop/setup_helix.py"
    spec = importlib.util.spec_from_file_location("android_innerloop_setup", path)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


@pytest.fixture
def setup_context(tmp_path):
    return {
        "workitem_root": str(tmp_path),
        "dotnet_exe": str(tmp_path / "dotnet"),
        "csproj": str(tmp_path / "app" / "App.csproj"),
        "nuget_config": str(tmp_path / "app" / "NuGet.config"),
        "framework": "net11.0-android",
    }


def test_android_bootstrap_uses_builtin_google_provider(setup_module, setup_context, monkeypatch):
    run = Mock(return_value=Mock(returncode=0))
    monkeypatch.setattr(setup_module, "run_cmd", run)

    setup_module.install_android_dependencies(setup_context)

    restore, install = [call.args[0] for call in run.call_args_list]
    assert restore[:3] == [setup_context["dotnet_exe"], "restore", setup_context["csproj"]]
    assert install == [
        setup_context["dotnet_exe"], "msbuild", setup_context["csproj"],
        "-t:InstallAndroidDependencies",
        f"/p:AndroidSdkDirectory={os.path.join(setup_context['workitem_root'], 'android-sdk')}",
        f"/p:JavaSdkDirectory={os.path.join(setup_context['workitem_root'], 'jdk')}",
        "/p:AndroidManifestType=GoogleV2",
        "/p:AcceptAndroidSdkLicenses=True",
        "/p:TargetFramework=net11.0-android",
    ]
    assert setup_context["android_home"] == os.path.join(setup_context["workitem_root"], "android-sdk")


@pytest.mark.parametrize(("results", "expected"), [([23], 23), ([0, 42], 42)])
def test_android_bootstrap_preserves_failures(
    setup_module, setup_context, monkeypatch, results, expected
):
    run = Mock(side_effect=[Mock(returncode=code) for code in results])
    monkeypatch.setattr(setup_module, "run_cmd", run)
    dump = Mock()
    monkeypatch.setattr(setup_module, "_dump_log", dump)

    with pytest.raises(SystemExit) as failure:
        setup_module.install_android_dependencies(setup_context)

    assert failure.value.code == expected
    assert run.call_count == len(results)
    assert "android_home" not in setup_context
    dump.assert_called_once()


@pytest.mark.parametrize("bootstrap_fails", [False, True])
def test_device_readiness_follows_successful_bootstrap(
    setup_module, tmp_path, monkeypatch, bootstrap_fails
):
    monkeypatch.setattr(sys, "argv", ["setup_helix.py", "net11.0-android", "/p:UseMonoRuntime=false"])
    monkeypatch.delenv("HELIX_WORKITEM_UPLOAD_ROOT", raising=False)
    monkeypatch.setenv("HELIX_WORKITEM_ROOT", str(tmp_path))
    monkeypatch.setattr(setup_module, "setup_dotnet", Mock(return_value="isolated-dotnet"))
    calls = []
    monkeypatch.setattr(setup_module, "install_workload", lambda _: calls.append("workload"))

    def bootstrap(_):
        calls.append("bootstrap")
        if bootstrap_fails:
            raise SystemExit(42)

    monkeypatch.setattr(setup_module, "setup_android_sdk", bootstrap)
    monkeypatch.setattr(setup_module, "run_cmd", lambda *_args, **_kwargs: calls.append("info"))
    monkeypatch.setattr(setup_module, "setup_adb_device", lambda _: calls.append("readiness"))
    monkeypatch.setattr(setup_module, "restore_packages", lambda _: calls.append("restore"))
    if bootstrap_fails:
        with pytest.raises(SystemExit):
            setup_module.main()
        assert calls == ["workload", "bootstrap"]
    else:
        assert setup_module.main() == 0
        assert calls == ["workload", "bootstrap", "info", "readiness", "restore"]


@pytest.fixture(params=["true", "false"], ids=["windows", "unix"])
def innerloop_item(request, tmp_path):
    dotnet = shutil.which("dotnet")
    if dotnet is None:
        pytest.skip("MSBuild evaluation requires a .NET SDK")
    project = ET.parse(repo_root / "eng/performance/maui_scenarios_android_innerloop.proj")
    project.getroot().attrib.pop("Sdk")
    for element in project.findall("Import"):
        element.set("Project", str(repo_root / "eng/performance" / element.get("Project")))
    project_path = tmp_path / "innerloop.proj"
    project.write(project_path, encoding="utf-8")
    result = subprocess.run(
        [
            dotnet, "msbuild", str(project_path), "-nologo", "-getItem:HelixWorkItem",
            f"-p:TargetsWindows={request.param}", "-p:Python=python",
            "-p:PERFLAB_Framework=net11.0", "-p:RuntimeFlavor=coreclr",
            "-p:CodegenType=Default", "-p:InnerLoopIterations=1",
        ],
        cwd=tmp_path, check=True, capture_output=True, text=True,
    )
    item, = json.loads(result.stdout)["Items"]["HelixWorkItem"]
    return item


@pytest.mark.parametrize(("setup_exit", "test_exit"), [(0, 0), (23, 0), (0, 42)])
def test_effective_helix_command_gates_measurement(innerloop_item, tmp_path, setup_exit, test_exit):
    item = innerloop_item
    assert "setup_helix.py" not in item["PreCommands"]
    assert "&&" in item["Command"]
    assert item["PostCommands"] == "python post.py"
    (tmp_path / "setup_helix.py").write_text(
        "import json, pathlib, sys\n"
        "pathlib.Path('setup-args.json').write_text(json.dumps(sys.argv[1:]))\n"
        f"sys.exit({setup_exit})\n", encoding="utf-8",
    )
    (tmp_path / "test.py").write_text(
        "import json, pathlib, sys\n"
        "pathlib.Path('test-args.json').write_text(json.dumps(sys.argv[1:]))\n"
        f"sys.exit({test_exit})\n", encoding="utf-8",
    )
    environment = os.environ.copy()
    environment["PATH"] = str(Path(sys.executable).parent) + os.pathsep + environment["PATH"]
    result = subprocess.run(item["Command"], shell=True, cwd=tmp_path, env=environment)

    assert result.returncode == (setup_exit or test_exit)
    setup_args = json.loads((tmp_path / "setup-args.json").read_text())
    assert len(setup_args) == 2
    assert setup_args[0] == "net11.0-android"
    test_args_file = tmp_path / "test-args.json"
    assert test_args_file.exists() == (setup_exit == 0)
    if not setup_exit:
        args = json.loads(test_args_file.read_text())
        assert args[args.index("--msbuild-args") + 1] == setup_args[1]
        assert args[args.index("--edit-src") + 1] == "src/MainPage.xaml.cs;src/MainPage.xaml"
        assert args[args.index("--inner-loop-iterations") + 1] == "1"
        assert args[args.index("--scenario-name") + 1] == item["Identity"]
