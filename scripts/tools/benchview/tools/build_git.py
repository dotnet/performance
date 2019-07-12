from benchview.utils.common import run_command
from datetime import datetime
from shutil import which

def git_get_repository():
    url = run_command(['git', 'ls-remote', '--get-url'], get_output=True).lines[0].strip()

    # strip .git off the end of the url
    git_suffix = '.git'
    if url.endswith(git_suffix):
        new_length = len(url) - len(git_suffix)
        url = url[:new_length]

    return url

def git_get_branch():
    result = run_command(['git', 'symbolic-ref', '--short', 'HEAD'], get_output=True, valid_exit_codes=None)
    if result.exit_code != 0:
        raise Exception("Unable to determine git branch. You may be in a detached HEAD state. Override using --branch")
    return result.lines[0].strip()

def git_get_sha1():
    return run_command(['git', 'rev-parse', 'HEAD'], get_output=True).lines[0].strip()

def git_get_timestamp():
    unix_timestamp = int(run_command(['git', 'log', '-1', '--pretty=%ct'], get_output=True).lines[0].strip())

    # Convert to match RFC 3339, Section 5.6.
    return datetime.utcfromtimestamp(unix_timestamp).strftime("%Y-%m-%dT%H:%M:%SZ")

def git_impl(args):
    if which('git') is None:
        raise EnvironmentError('git was not found on the PATH') 

    for prop in ['branch', 'repository', 'number', 'source_timestamp']:
        # Invoke calls to gather default arguments once we know the git subparser is selected
        if callable(args[prop]):
            args[prop] = args[prop]()