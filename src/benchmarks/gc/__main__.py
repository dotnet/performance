# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.
# See the LICENSE file in the project root for more information.

from src.all_commands import ALL_COMMANDS
from src.commonlib.command import run_command

if __name__ == "__main__":
    run_command(ALL_COMMANDS)
