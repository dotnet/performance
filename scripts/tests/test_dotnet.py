'''
Because of how pytest finds things, all import modules must start with scripts.
'''

from scripts.dotnet import CSharpProject, CSharpProjFile
import os
import subprocess
from unittest.mock import Mock

import pytest


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
