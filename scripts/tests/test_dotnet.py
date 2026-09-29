'''
Because of how pytest finds things, all import modules must start with scripts.
'''

from scripts.dotnet import CSharpProject
import os
def test_new():
    CSharpProject.new('console', 'test_new', 'test_bin', False, '.')
    assert os.path.isdir('test_new')
    assert os.path.isfile(os.path.join('test_new', 'test_new.csproj'))