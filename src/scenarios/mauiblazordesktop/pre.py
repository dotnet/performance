'''
pre-command: Example call 'python .\pre.py publish -f net6.0-windows10.0.19041.0 -c Release'
'''
import shutil
import subprocess
import sys
import os
from performance.logger import setup_loggers
from shared.codefixes import insert_after
from shared.precommands import PreCommands
from shared import const
from test import EXENAME
import requests
import winreg

setup_loggers(True)
NugetURL = 'https://raw.githubusercontent.com/dotnet/maui/main/NuGet.config'
WebViewURL = 'https://go.microsoft.com/fwlink/p/?LinkId=2124703'
WebViewInstalled = False
NugetFile = requests.get(NugetURL)
open('./Nuget.config', 'wb').write(NugetFile.content)
# TODO Check if we already have Webview installed, otherwise, install it
with winreg.ConnectRegistry(None, winreg.HKEY_LOCAL_MACHINE) as hklm_hive:
    lmkey = r"SOFTWARE\WOW6432Node\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}"
    print(f"Opening {lmkey}")
    try:
        with winreg.OpenKey(hklm_hive, lmkey) as openkey:
            pvvalue = winreg.QueryValueEx(openkey, 'pv')[0]
            if pvvalue and pvvalue != '' and pvvalue != '0.0.0.0':
                WebViewInstalled = True
                print(f"pvvalue found {pvvalue}")
    except:
        print("WebView not verified in Local_Machine Registry")
if not WebViewInstalled:
    try:
        with winreg.ConnectRegistry(None, winreg.HKEY_CURRENT_USER) as hkcu_hive:
            cukey = r"Software\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}"
            print(f"Opening {cukey}")
            with winreg.OpenKey(hkcu_hive, cukey) as openkey:
                pvvalue = winreg.QueryValueEx(openkey, 'pv')[0]
                if pvvalue and pvvalue != '' and pvvalue != '0.0.0.0':
                    WebViewInstalled = True
    except:
        print("WebView not verified in Current_Machine Registry")

if not WebViewInstalled:
    WebViewInstallFile = requests.get(WebViewURL)
    open('./MicrosoftEdgeWebview2Setup.exe', 'wb').write(WebViewInstallFile.content)
    subprocess.run(['powershell', '-Command', r'Start-Process "./MicrosoftEdgeWebview2Setup.exe" -Wait'], stdout=subprocess.PIPE, stderr=subprocess.STDOUT, shell=True)
    print("Installed WebView2")
else:
    print("WebViewAlreadyInstalled")

precommands = PreCommands()
precommands.install_workload('maui', ['--from-rollback-file', 'https://aka.ms/dotnet/maui/net6.0.json', '--configfile', './Nuget.config'])
precommands.new(template='maui-blazor',
                output_dir=const.APPDIR,
                bin_dir=const.BINDIR,
                exename=EXENAME,
                working_directory=sys.path[0],
                no_restore=False)

subprocess.run(["dotnet", "add", "./app", "package", "Microsoft.WindowsAppSDK"]) # Add the package reference for the Microsoft.WindowsAppSDK for self-contained running
shutil.copy2(os.path.join(const.SRCDIR, 'Replacement.Index.razor.cs'), os.path.join(const.APPDIR, 'Pages', 'Index.razor.cs'))
precommands.add_startup_logging(os.path.join('Pages', 'Index.razor.cs'), "if (firstRender) {")
#insert_after(os.path.join(const.APPDIR, 'MauiBlazorDesktopTesting.csproj'), "<TargetFrameworks>net6.0-android;net6.0-ios;net6.0-maccatalyst</TargetFrameworks>", "        <TargetFrameworks>$(TargetFrameworks);net6.0-windows10.0.19041.0</TargetFrameworks>")

# print("File: ")
# with open(os.path.join(const.APPDIR, 'Pages', 'Index.razor.cs'), 'r') as f:
#     print(f.read())

precommands.execute(['/p:Platform=x64','/p:WindowsAppSDKSelfContained=True','/p:WindowsPackageType=None','/p:WinUISDKReferences=False','/p:PublishReadyToRun=true'])
#shutil.copyfile('DesktopTestBinlog.binlog', os.path.join(os.environ.get('HELIX_WORKITEM_UPLOAD_ROOT'), 'Binlog.binlog'))
