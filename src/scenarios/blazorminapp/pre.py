'''
pre-command
'''
import sys, os, subprocess, shutil
from performance.logger import setup_loggers
from shared import const
from shared.precommands import PreCommands
from test import EXENAME

# For blazor3.2 the linker argument '--dump-dependencies' should be added statically to enable linker dump
# For blazor5.0 the linker argument can be added to the command line as an msbuild property
setup_loggers(True)
precommands = PreCommands()
precommands.new(template='blazorwasm',
                output_dir=const.APPDIR,
                bin_dir=const.BINDIR,
                exename=EXENAME,
                working_directory=sys.path[0])

# This project uses native relinking

# We want to use native relinking which requires the workload
precommands.install_workload('wasm-tools')
f = open(os.path.join(os.getcwd(), "app", "emptyblazorwasmtemplate.csproj"), 'r')
outFileText = ""
for line in f.readlines():
    if "<PropertyGroup>" in line:
        outFileText += line
        outFileText += "    <BlazorEnableTimeZoneSupport>false</BlazorEnableTimeZoneSupport>" + os.linesep
        outFileText += "    <InvariantGlobalization>true</InvariantGlobalization>" + os.linesep
        # this will trigger native relinking, and fail the build if the workload is not available
        outFileText += "    <WasmBuildNative>true</WasmBuildNative>" + os.linesep
        # skip unncessary relinking done after build
        outFileText += "    <WasmBuildOnlyAfterPublish>true</WasmBuildOnlyAfterPublish>" + os.linesep
    else:
        outFileText += line
f.close()
os.remove(os.path.join(os.getcwd(), "app", "emptyblazorwasmtemplate.csproj"))
f = open(os.path.join(os.getcwd(), "app", "emptyblazorwasmtemplate.csproj"), 'w')
f.write(outFileText)
f.close()
precommands.execute()
