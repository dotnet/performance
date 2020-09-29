'''
Because of how pytest finds things, all import modules must start with scripts.
'''

from scripts.dotnet import CSharpProject, CSharpProjFile
import os
import sys
def test_new():
    CSharpProject.new('console', 'test_new', False, '.')
    assert os.path.isdir('test_new')
    assert os.path.isfile(os.path.combine('test_new', 'test_new.csproj'))