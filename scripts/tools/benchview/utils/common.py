from sys import version_info, stderr
from collections import namedtuple
from os import path
import subprocess


def get_script_directory(fileName: str) -> str:
    return path.dirname(path.realpath(fileName))


def is_supported_version() -> bool:
    return version_info.major > 2 and version_info.minor > 4


def is_null_or_whitespace(name: str) -> bool:
    return not name or name.isspace()


def is_number(value: str) -> bool:
    try:
        float(value)
    except ValueError:
        try:
            float(int(value, 16))
        except ValueError:
            return False
    return True


def to_number(value: str) -> float:
    try:
        return float(value)
    except ValueError:
        try:
            return float(int(value, 16))
        except ValueError:
            return None
    return None


def run_command(cmdline : list, valid_exit_codes : list = [0], get_output : bool = False, silent : bool = True):
    should_pipe = (not silent) or get_output

    quoted_cmdline = subprocess.list2cmdline(cmdline)
    quoted_cmdline += ' > {}'.format(os.devnull) if not should_pipe else ''
    if not silent:
        print('$> {}'.format(quoted_cmdline))

    proc = subprocess.Popen(
        cmdline,
        stdout=subprocess.PIPE if should_pipe else subprocess.DEVNULL,
        stderr=subprocess.STDOUT,
        universal_newlines=True
    )

    lines = []
    if proc.stdout != None:
        for line in iter(proc.stdout.readline, ''):
            line = line.rstrip()
            if get_output:
                lines.append(line)
            if not silent:
                print(line)
        proc.stdout.close()

    proc.wait()
    if (valid_exit_codes is not None) and (not proc.returncode in valid_exit_codes):
        print("Exited with exit code {}".format(proc.returncode), file=stderr)
        raise subprocess.CalledProcessError(proc.returncode, quoted_cmdline)

    ProcessResult = namedtuple("ProcessResult", ["lines", "exit_code"])
    return ProcessResult(lines=lines, exit_code = proc.returncode)
