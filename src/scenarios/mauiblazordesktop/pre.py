'''
pre-command: Example call 'python .\pre.py publish -f net7.0-windows10.0.19041.0 -c Release'
'''
import shutil
import subprocess
import sys
import os
from performance.logger import setup_loggers, getLogger
from shared.codefixes import insert_after
from shared.precommands import PreCommands
from shared import const
from test import EXENAME
import requests
import winreg

setup_loggers(True)
NugetURL = 'https://raw.githubusercontent.com/dotnet/maui/main/NuGet.config'
WebViewURL = 'https://go.microsoft.com/fwlink/p/?LinkId=2124703' # Obtained from https://developer.microsoft.com/en-us/microsoft-edge/webview2/#download-section
WebViewInstalled = False
NugetFile = requests.get(NugetURL)
open('./Nuget.config', 'wb').write(NugetFile.content)

lmkey = r"SOFTWARE\WOW6432Node\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}"
cukey = r"Software\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}"
with winreg.ConnectRegistry(None, winreg.HKEY_LOCAL_MACHINE) as hklm_hive:
    try:
        with winreg.OpenKey(hklm_hive, lmkey) as openkey:
            pvvalue = winreg.QueryValueEx(openkey, 'pv')[0]
            if pvvalue and pvvalue != '' and pvvalue != '0.0.0.0':
                WebViewInstalled = True
                getLogger().info(f"WebView Found; pvvalue(version) {pvvalue}")
    except:
        getLogger().warning("WebView not verified in Local_Machine Registry")
if not WebViewInstalled:
    try:
        with winreg.ConnectRegistry(None, winreg.HKEY_CURRENT_USER) as hkcu_hive:
            with winreg.OpenKey(hkcu_hive, cukey) as openkey:
                pvvalue = winreg.QueryValueEx(openkey, 'pv')[0]
                if pvvalue and pvvalue != '' and pvvalue != '0.0.0.0':
                    WebViewInstalled = True
                    getLogger().info(f"WebView Found; pvvalue(version) {pvvalue}")
    except:
        getLogger().warning("WebView not verified in Current_Machine Registry")
if not WebViewInstalled:
    getLogger().info("Installing WebView2")
    WebViewInstallFile = requests.get(WebViewURL)
    open('./MicrosoftEdgeWebview2Setup.exe', 'wb').write(WebViewInstallFile.content)
    subprocess.run(['powershell', '-Command', r'Start-Process "./MicrosoftEdgeWebview2Setup.exe" -Wait'], stdout=subprocess.PIPE, stderr=subprocess.STDOUT, shell=True)
    with winreg.ConnectRegistry(None, winreg.HKEY_LOCAL_MACHINE) as hklm_hive:
        try:
            with winreg.OpenKey(hklm_hive, lmkey) as openkey:
                pvvalue = winreg.QueryValueEx(openkey, 'pv')[0]
                if pvvalue and pvvalue != '' and pvvalue != '0.0.0.0':
                    WebViewInstalled = True
                    getLogger().info(f"WebView Found; pvvalue(version) {pvvalue}")
        except:
            getLogger().warning("WebView not verified in Local_Machine Registry")
    if not WebViewInstalled:
        try:
            with winreg.ConnectRegistry(None, winreg.HKEY_CURRENT_USER) as hkcu_hive:
                with winreg.OpenKey(hkcu_hive, cukey) as openkey:
                    pvvalue = winreg.QueryValueEx(openkey, 'pv')[0]
                    if pvvalue and pvvalue != '' and pvvalue != '0.0.0.0':
                        WebViewInstalled = True
                        getLogger().info(f"WebView Found; pvvalue(version) {pvvalue}")
        except:
            getLogger().warning("WebView not verified in Current_Machine Registry.")
            getLogger().error("Blazor cannot run without WebView installed, exiting execution.")
            sys.exit(-1)
else:
    getLogger().info("WebViewAlreadyInstalled")
    

precommands = PreCommands()
precommands.install_workload('maui', ['--from-rollback-file', 'https://aka.ms/dotnet/maui/net7.0.json', '--configfile', './Nuget.config'])
precommands.new(template='maui-blazor',
                output_dir=const.APPDIR,
                bin_dir=const.BINDIR,
                exename=EXENAME,
                working_directory=sys.path[0],
                no_restore=False)

subprocess.run(["dotnet", "add", "./app", "package", "Microsoft.WindowsAppSDK"]) # Add the package reference for the Microsoft.WindowsAppSDK for self-contained running
shutil.copy2(os.path.join(const.SRCDIR, 'Replacement.Index.razor.cs'), os.path.join(const.APPDIR, 'Pages', 'Index.razor.cs'))
precommands.add_startup_logging(os.path.join('Pages', 'Index.razor.cs'), "if (firstRender) {")
precommands.execute(['/p:Platform=x64','/p:WindowsAppSDKSelfContained=True','/p:WindowsPackageType=None','/p:WinUISDKReferences=False','/p:PublishReadyToRun=true'])
