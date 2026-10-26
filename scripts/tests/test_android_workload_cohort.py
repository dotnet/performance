import importlib
import importlib.util
import json
from pathlib import Path
import runpy
import sys
from types import SimpleNamespace
from unittest.mock import Mock

import pytest


repo_root = Path(__file__).resolve().parents[2]


@pytest.mark.parametrize("cached", [True, False], ids=["cached-discovery", "feed-fallback"])
def test_innerloop_pins_android_and_maui_through_helix(tmp_path, monkeypatch, cached):
    monkeypatch.syspath_prepend(str(repo_root / "scripts"))
    monkeypatch.syspath_prepend(str(repo_root / "src/scenarios"))
    precommands = importlib.import_module("shared.precommands")
    maui = importlib.import_module("shared.mauisharedpython")
    monkeypatch.setattr(importlib.import_module("performance.logger"), "setup_loggers", Mock())
    monkeypatch.delenv("MAUI_WORKLOAD_MODE", raising=False)

    payload_repo = tmp_path / "performance"
    scenario = payload_repo / "src/scenarios/mauiandroidinnerloop"
    scenario.mkdir(parents=True)
    config = payload_repo / "NuGet.config"
    config.write_text(
        '<configuration><packageSources>'
        '<add key="dotnet12" value="https://example.test/dotnet12/index.json" />'
        '<add key="dotnet11" value="https://example.test/dotnet11/index.json" />'
        '</packageSources></configuration>',
        encoding="utf-8",
    )
    monkeypatch.chdir(scenario)
    monkeypatch.syspath_prepend(str(scenario))
    monkeypatch.setitem(sys.modules, "test", SimpleNamespace(EXENAME="MauiAndroidInnerLoop"))
    monkeypatch.setattr(maui.MauiNuGetConfigContext, "__enter__", lambda self: self)
    monkeypatch.setattr(maui.MauiNuGetConfigContext, "__exit__", lambda *args: False)

    android = {
        "id": "Microsoft.NET.Sdk.Android.Manifest-12.0.100-alpha.1",
        "latestVersion": "37.99.0-preview.1.254",
        "sdk_version": "12.0.100-alpha.1",
        "dotnet_version": "12.0",
    }
    maui_package = {
        "id": "Microsoft.NET.Sdk.Maui.Manifest-11.0.100",
        "latestVersion": "11.0.0-rc.2.26526.6",
        "sdk_version": "11.0.100",
        "dotnet_version": "11.0",
    }
    expected = {
        "microsoft.net.sdk.android": "37.99.0-preview.1.254/12.0.100-alpha.1",
        "microsoft.net.sdk.maui": "11.0.0-rc.2.26526.6/11.0.100",
    }
    pre = Mock(has_workload=False, framework="net11.0")
    pre._cached_manifest_packages = {
        "microsoft.net.sdk.android": android,
        "microsoft.net.sdk.maui": maui_package,
    } if cached else {}
    monkeypatch.setattr(precommands, "PreCommands", lambda: pre)

    def query(workload, feed):
        if workload == "microsoft.net.sdk.android":
            assert "/dotnet12/" in feed
            return [android]
        assert workload == "microsoft.net.sdk.maui"
        if "/dotnet12/" in feed:
            raise ValueError("No MAUI manifest in this feed")
        assert "/dotnet11/" in feed
        return [maui_package]

    pre.get_packages_for_sdk_from_feed.side_effect = query
    installed = []

    def install(workload, args):
        assert workload == "maui-android"
        assert args == ["--from-rollback-file", "rollback_maui.json"]
        installed.append(json.loads(Path("rollback_maui.json").read_text()))

    pre.install_workload.side_effect = install

    def new(**kwargs):
        assert installed == [expected]
        assert kwargs["template"] == "maui"
        assert kwargs["no_restore"] is True
        app = Path(kwargs["output_dir"])
        (app / "Pages").mkdir(parents=True)
        (app / "MauiAndroidInnerLoop.csproj").write_text(
            '<Project><PropertyGroup><UseMaui>true</UseMaui></PropertyGroup>'
            '<ItemGroup><PackageReference Include="Microsoft.Maui.Controls" '
            'Version="$(MauiVersion)" /></ItemGroup></Project>',
            encoding="utf-8",
        )
        (app / "Pages/MainPage.xaml.cs").write_text("InitializeComponent();", encoding="utf-8")
        (app / "Pages/MainPage.xaml").write_text('<Label Text="Task Categories" />', encoding="utf-8")

    pre.new.side_effect = new
    runpy.run_path(str(repo_root / "src/scenarios/mauiandroidinnerloop/pre.py"), run_name="__main__")

    rollback = scenario / "rollback_maui.json"
    assert json.loads(rollback.read_text()) == expected
    pre.install_workload.assert_called_once()
    assert pre.get_packages_for_sdk_from_feed.call_count == (0 if cached else 3)
    project = (scenario / "app/MauiAndroidInnerLoop.csproj").read_text()
    assert 'Version="$(MauiVersion)"' in project
    assert "<MauiVersion>" not in project

    spec = importlib.util.spec_from_file_location(
        "innerloop_cohort_setup", repo_root / "src/scenarios/mauiandroidinnerloop/setup_helix.py"
    )
    setup = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(setup)
    run = Mock(return_value=SimpleNamespace(returncode=0))
    monkeypatch.setattr(setup, "run_cmd", run)
    setup.install_workload({
        "dotnet_exe": "isolated-dotnet",
        "csproj": str(scenario / "app/MauiAndroidInnerLoop.csproj"),
        "nuget_config": str(scenario / "app/NuGet.config"),
        "framework": "net11.0-android",
        "workitem_root": str(scenario),
    })

    assert run.call_args.args[0] == [
        "isolated-dotnet", "workload", "install", "maui-android",
        "--from-rollback-file", str(rollback),
        "--configfile", str(scenario / "app/NuGet.config"),
    ]
    assert json.loads(rollback.read_text()) == expected
