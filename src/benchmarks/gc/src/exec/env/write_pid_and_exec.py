# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.
# See the LICENSE file in the project root for more information.

from os import execv, getpid
from os.path import realpath
from pathlib import Path
from sys import argv

here = Path(realpath(__file__))
(here.parent / "__pid.txt").write_text(str(getpid()), encoding="utf-8")

assert argv[0].endswith("write_pid_and_exec.py")

# Not a typo -- we pass argv[1] as both the program path and the first argument
# NOTE: This will fail if argv[1] is not an absolute path
execv(argv[1], argv[1:])
