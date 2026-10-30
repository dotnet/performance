'''
Because of how pytest finds things, all import modules must start with scripts.
'''

from scripts.dotnet import CSharpProject, CSharpProjFile, FrameworkVersion, WasmPackage, WasmWorkloadCohort
import os
import subprocess
import sys
from unittest.mock import Mock

import pytest


@pytest.mark.parametrize("explicit_nuget_root", [False, True])
def test_custom_restore_root_is_used_for_package_imports(tmp_path, explicit_nuget_root):
    repo_root = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..'))
    project = os.path.join(
        repo_root, 'src', 'harness', 'BenchmarkDotNet.Extensions',
        'BenchmarkDotNet.Extensions.csproj')
    restore_root = tmp_path / "restored packages"
    expected_root = tmp_path / "explicit root" if explicit_nuget_root else restore_root
    command = [
        "dotnet", "msbuild", project, "-nologo", "-getProperty:NuGetPackageRoot",
        f"-p:RestorePackagesPath={restore_root}",
    ]
    if explicit_nuget_root:
        command.append(f"-p:NuGetPackageRoot={expected_root}")

    result = subprocess.run(command, cwd=repo_root, capture_output=True, text=True)

    assert result.returncode == 0, result.stdout + result.stderr
    assert os.path.normcase(os.path.normpath(result.stdout.strip())) == (
        os.path.normcase(os.path.normpath(str(expected_root))))


def test_dotnet_import_does_not_use_deprecated_namedtuple_syntax():
    result = subprocess.run(
        [
            sys.executable,
            "-W", "error:Creating NamedTuple classes using keyword arguments:DeprecationWarning",
            "-c", "import dotnet",
        ],
        cwd=os.path.dirname(os.path.dirname(os.path.abspath(__file__))),
        capture_output=True,
        text=True,
    )
    assert result.returncode == 0, result.stderr


@pytest.mark.parametrize(
    ("record_type", "fields"),
    [
        (CSharpProjFile, {"file_name": "test.csproj", "working_directory": "project"}),
        (WasmPackage, {"package_id": "Microsoft.NET.Runtime.WebAssembly.Sdk", "version": "11.0.0"}),
        (
            WasmWorkloadCohort,
            {
                "sdk_version": "11.0.100",
                "product_version": "11.0.0",
                "host_rid": "win-x64",
                "workload_id": "wasm-tools",
                "packages": (WasmPackage("Microsoft.NET.Runtime.WebAssembly.Sdk", "11.0.0"),),
            },
        ),
        (FrameworkVersion, {"major": 11, "minor": 0}),
    ],
)
def test_namedtuple_records_preserve_fields_and_constructors(record_type, fields):
    record = record_type(**fields)

    assert record == record_type(*fields.values())
    assert tuple(record) == tuple(fields.values())
    assert record._fields == tuple(fields)
    for name, value in fields.items():
        assert getattr(record, name) == value


def test_new():
    CSharpProject.new('console', 'test_new', 'test_bin', False, '.')
    assert os.path.isdir('test_new')
    assert os.path.isfile(os.path.join('test_new', 'test_new.csproj'))


@pytest.mark.parametrize("use_link", [False, True])
@pytest.mark.parametrize("frameworks", [None, ["net10.0"]])
def test_restore_and_build_use_canonical_project_paths(tmp_path, monkeypatch, use_link, frameworks):
    project_dir = tmp_path / "project"
    project_dir.mkdir()
    project_file = project_dir / "test.csproj"
    project_file.write_text('<Project Sdk="Microsoft.NET.Sdk" />')
    input_dir = project_dir
    link = tmp_path / "alias"
    if use_link:
        if os.name == "nt":
            subprocess.run(
                [os.environ["COMSPEC"], "/c", "mklink", "/J", str(link), str(project_dir)],
                check=True,
                capture_output=True,
            )
        else:
            link.symlink_to(project_dir, target_is_directory=True)
        input_dir = link

    try:
        run_command = Mock()
        monkeypatch.setattr("scripts.dotnet.RunCommand", run_command)
        project = CSharpProject(
            CSharpProjFile(str(input_dir / "test.csproj"), str(input_dir)),
            str(tmp_path / "bin"),
        )
        project.restore(str(tmp_path / "packages"), verbose=False)
        project.build(
            "Release",
            verbose=False,
            packages_path=str(tmp_path / "packages"),
            target_framework_monikers=frameworks,
        )

        assert project.csproj_file == os.path.realpath(project_file)
        assert project.working_directory == os.path.realpath(project_dir)
        assert run_command.call_count == 2
        for call in run_command.call_args_list:
            assert call.args[0][2] == os.path.realpath(project_file)
        for call in run_command.return_value.run.call_args_list:
            assert call.args == (os.path.realpath(project_dir),)
    finally:
        if use_link:
            if os.name == "nt":
                os.rmdir(link)
            else:
                link.unlink()
