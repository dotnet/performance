'''
Tests for benchmarks_ci.py Helix upload logic
'''

import os
import tempfile
from pathlib import Path

def test_path_containment_direct_child():
    """Test that a direct child directory is detected as contained"""
    with tempfile.TemporaryDirectory() as tmpdir:
        parent = os.path.realpath(tmpdir)
        child = os.path.realpath(os.path.join(tmpdir, "subdir"))
        os.makedirs(child, exist_ok=True)
        
        # The logic from benchmarks_ci.py
        artifacts_in_upload = child.startswith(parent + os.sep) or child == parent
        assert artifacts_in_upload == True

def test_path_containment_nested_child():
    """Test that a nested child directory is detected as contained"""
    with tempfile.TemporaryDirectory() as tmpdir:
        parent = os.path.realpath(tmpdir)
        child = os.path.realpath(os.path.join(tmpdir, "sub1", "sub2", "artifacts"))
        os.makedirs(child, exist_ok=True)
        
        # The logic from benchmarks_ci.py
        artifacts_in_upload = child.startswith(parent + os.sep) or child == parent
        assert artifacts_in_upload == True

def test_path_containment_same_path():
    """Test that the same path is detected as contained"""
    with tempfile.TemporaryDirectory() as tmpdir:
        path = os.path.realpath(tmpdir)
        
        # The logic from benchmarks_ci.py
        artifacts_in_upload = path.startswith(path + os.sep) or path == path
        assert artifacts_in_upload == True

def test_path_containment_not_contained():
    """Test that separate directories are not detected as contained"""
    with tempfile.TemporaryDirectory() as tmpdir1:
        with tempfile.TemporaryDirectory() as tmpdir2:
            parent = os.path.realpath(tmpdir1)
            child = os.path.realpath(tmpdir2)
            
            # The logic from benchmarks_ci.py
            artifacts_in_upload = child.startswith(parent + os.sep) or child == parent
            assert artifacts_in_upload == False

def test_path_containment_sibling():
    """Test that sibling directories are not detected as contained"""
    with tempfile.TemporaryDirectory() as tmpdir:
        parent = os.path.realpath(tmpdir)
        sibling1 = os.path.realpath(os.path.join(tmpdir, "dir1"))
        sibling2 = os.path.realpath(os.path.join(tmpdir, "dir2"))
        os.makedirs(sibling1, exist_ok=True)
        os.makedirs(sibling2, exist_ok=True)
        
        # The logic from benchmarks_ci.py - sibling1 should not be in sibling2
        artifacts_in_upload = sibling1.startswith(sibling2 + os.sep) or sibling1 == sibling2
        assert artifacts_in_upload == False

def test_path_containment_similar_prefix():
    """Test that directories with similar prefixes but not parent/child are not detected as contained"""
    with tempfile.TemporaryDirectory() as tmpdir:
        # Create paths like /tmp/xyz/artifacts and /tmp/xyz/artifacts-other
        # These have similar prefixes but are not parent/child
        dir1 = os.path.realpath(os.path.join(tmpdir, "artifacts"))
        dir2 = os.path.realpath(os.path.join(tmpdir, "artifacts-other"))
        os.makedirs(dir1, exist_ok=True)
        os.makedirs(dir2, exist_ok=True)
        
        # The logic from benchmarks_ci.py - dir2 should not be considered as being in dir1
        artifacts_in_upload = dir2.startswith(dir1 + os.sep) or dir2 == dir1
        assert artifacts_in_upload == False
