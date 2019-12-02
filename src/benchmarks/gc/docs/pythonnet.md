# Installing Pythonnet

You can try `py -m pip install pythonnet`. If that works, you're done, but it may fail with:

    error: option --single-version-externally-managed not recognized

The fix is to install from source.
Instructions are at: https://github.com/pythonnet/pythonnet/wiki/Installation.

## Troubleshooting on all operating systems

As the instructions say, `py setup.py bdist_wheel --xplat` is the build command on both systems.

At the end, the instructions tell you to run `pip install --no-index --find-links=.\dist\ pythonnet`.
This may "succeed" saying `Requirement already satisfied: pythonnet in /path/to/pythonnet`. Even though it isn't really installed.
This is because the current directory is called `pythonnet`.
To fix this, go to the *parent* directory and use `sudo python3.7 -m pip install --no-index --find-links=./pythonnet/dist/` which circumvents this bug.

To verify that installation worked, run `import clr` in the python interpreter.


## Troubleshooting on Windows

### Remove shebang

First: Running `py setup.py ...` may do nothing due to the shebang at the start of `setup.py`,
launching `python`, which opens the Windows Store or does nothing if given arguments.
This may be fixed by removing the shebang `#!/usr/bin/env python` at the start of `setup.py`.


### You may need VS2015

There may be an error like:

    Cannot find the specified version of msbuild: '14' 

or:

    Could not load file or assembly 'Microsoft.Build.Utilities, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'

(The latter error message often begins by mentioning `RGiesecke.DllExport.targets`.)

If so, you may need to install Visual Studio 2015 (exactly, not a higher version).
(See https://my.visualstudio.com/Downloads?q=visual%20studio%202015)

This is not used as the main build tool, but it installs components that are apparently required.


### Modify setup.py to support a newer VS

You may see an error:

    MSBuild >=15.0 could not be found.
   
Actually, pythonnet's `setup.py` is hardcoded to work *only* with VS 15 (which is VS 2017) and not with higher versions such as 16 (VS 2019).
(Also, "Visual Studio Build Tools 2017" alone does not seem to be sufficient, you need the full IDE.)
But you can change this by modifying `setup.py`.
There is a method `_find_msbuild_tool_15` responsible for finding the msbuild executable path.
Instead of its complicated logic, you could just hardcode a path that works on your machine by changing the method body to:

```py
from pathlib import Path
# CHange the below path to something that works on your system
res = "C:\\Program Files (x86)\\Microsoft Visual Studio\\2019\\Community\\MSBuild\\Current\\Bin\\MSBuild.exe"
assert Path(res).exists()
return res
```

There don't appear to be any problems from using 2019 instead of 2017.


## Troubleshooting on non-Windows systems

### Downgrade mono
Pythonnet [does not work](https://github.com/pythonnet/pythonnet/issues/939) with the latest version of mono, so you'll need to downgrade that to version 5.

On Ubuntu the instructions are:

* Change `/etc/apt/sources.list.d/mono-official-stable.list` to:
```
deb https://download.mono-project.com/repo/ubuntu stable-bionic/snapshots/5.20.1 main
```
* `sudo apt remove mono-complete`
* `sudo apt update`
* `sudo apt autoremove`
* `sudo apt install mono-complete`
* `mono --version`, should be 5.20.1

Then to install from source:


### May be missing Python dev tools

If you see an error:
```
fatal error: Python.h: No such file or directory
```

You likely have python installed but not development tools.
See https://stackoverflow.com/questions/21530577/fatal-error-python-h-no-such-file-or-directory .
