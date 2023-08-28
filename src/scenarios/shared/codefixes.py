# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.
# See the LICENSE file in the project root for more information.

'''
routines for fixing up code in templates
'''

from re import sub
from typing import List

def readfile(file: str) -> List[str]:
    ret: List[str] = []
    with open(file, "r") as opened:
        for line in opened:
            ret.append(line)
    return ret

def writefile(file: str, lines: List[str]):
    with open(file, "w") as opened:
        opened.writelines(lines)

# insert string after the first occurance of the search string
def insert_after(file: str, search: str, insert: str):
    lines = readfile(file)
    for i in range(len(lines)):
        if search in lines[i]:
            lines.insert(i+1, ("%s\n" % insert))
            break
    writefile(file, lines)

def replace_line(file: str, search: str, replace: str):
    lines: List[str] = []
    for line in readfile(file):
        lines.append(sub(search, replace, line))
    writefile(file, lines)
