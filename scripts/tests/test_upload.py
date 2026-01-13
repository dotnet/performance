'''
Tests for upload.py
Because of how pytest finds things, all import modules must start with scripts.
'''

from scripts.upload import get_unique_name

def test_get_unique_name_normal():
    '''Test that get_unique_name works with normal-sized filenames'''
    result = get_unique_name("/path/to/file.json", "unique123")
    assert result == "unique123-file.json"

def test_get_unique_name_with_path():
    '''Test that get_unique_name uses only basename'''
    result = get_unique_name("/very/long/path/to/myfile.txt", "abc-def-123")
    assert result == "abc-def-123-myfile.txt"

def test_get_unique_name_long_filename():
    '''Test that get_unique_name handles long filenames while preserving unique_id and extension'''
    long_filename = "a" * 1000 + ".json"
    unique_id = "unique123"
    result = get_unique_name(long_filename, unique_id)
    
    # Check that the result is under 1024 characters
    assert len(result) <= 1024
    
    # Check that it starts with unique_id
    assert result.startswith(unique_id + "-")
    
    # Check that it ends with the extension
    assert result.endswith(".json")

def test_get_unique_name_very_long_filename_and_id():
    '''Test that get_unique_name handles very long unique_id and filename'''
    long_filename = "b" * 1000 + ".txt"
    long_unique_id = "x" * 500
    result = get_unique_name(long_filename, long_unique_id)
    
    # Check that the result is under 1024 characters
    assert len(result) <= 1024
    
    # Check that it starts with unique_id (unique_id should always be preserved)
    assert result.startswith(long_unique_id + "-")

def test_get_unique_name_no_extension():
    '''Test that get_unique_name handles files without extension'''
    long_filename = "c" * 1000
    unique_id = "unique456"
    result = get_unique_name(long_filename, unique_id)
    
    # Check that the result is under 1024 characters
    assert len(result) <= 1024
    
    # Check that it starts with unique_id
    assert result.startswith(unique_id + "-")

def test_get_unique_name_uniqueness():
    '''Test that different unique_ids produce different names even with same filename'''
    filename = "test.json"
    result1 = get_unique_name(filename, "id1")
    result2 = get_unique_name(filename, "id2")
    
    assert result1 != result2
    assert result1 == "id1-test.json"
    assert result2 == "id2-test.json"

def test_get_unique_name_long_extension():
    '''Test handling of unusually long file extensions'''
    filename = "file.verylongextension1234567890"
    unique_id = "a" * 1000
    result = get_unique_name(filename, unique_id)
    
    # Check that the result is under 1024 characters
    assert len(result) <= 1024
    
    # Check that it starts with unique_id
    assert result.startswith(unique_id + "-")

def test_get_unique_name_edge_case_exactly_1024():
    '''Test edge case where name is exactly 1024 characters'''
    # Create a filename that when combined with unique_id equals exactly 1024
    unique_id = "a" * 100
    # Need to account for the hyphen
    basename = "b" * (1024 - len(unique_id) - 1)
    result = get_unique_name(basename, unique_id)
    
    assert len(result) == 1024
    assert result == unique_id + "-" + basename

def test_get_unique_name_extremely_long_unique_id():
    '''Test edge case where unique_id itself is very long (>1023 chars)'''
    extremely_long_unique_id = "x" * 2000
    filename = "test.json"
    result = get_unique_name(filename, extremely_long_unique_id)
    
    # Should truncate to exactly 1024 characters
    assert len(result) <= 1024
    # Should be truncated unique_id
    assert result == extremely_long_unique_id[:1024]

def test_get_unique_name_long_unique_id_no_space_for_basename():
    '''Test when unique_id is so long there's minimal space for basename'''
    long_unique_id = "y" * 1020
    filename = "verylongfilename.json"
    result = get_unique_name(filename, long_unique_id)
    
    # Should stay under 1024
    assert len(result) <= 1024
    # Should start with unique_id
    assert result.startswith(long_unique_id)
