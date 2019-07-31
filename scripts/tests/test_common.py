'''
Because of how pytest finds things, all import modules must start with scripts.
'''

from scripts.performance.common import get_repo_root_path

def test_rootpath():
    assert get_repo_root_path().endswith('performance')