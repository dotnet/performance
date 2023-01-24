import os
import subprocess
from shared.runner import TestTraits, Runner
from performance.logger import setup_loggers, getLogger
import sys
import winreg
import requests

EXENAME = 'MauiBlazorDesktopTesting'

def main():
    setup_loggers(True)

    install_webview()
    traits = TestTraits(exename=EXENAME,
                        guiapp='true',
                        startupmetric='WinUIBlazor',
                        timeout=30,
                        measurementdelay='6',
                        runwithoutexit='false',
                        processwillexit="false",
                        )
    runner = Runner(traits)
    runner.run()

def install_webview():
    WebViewURL = 'https://go.microsoft.com/fwlink/p/?LinkId=2124703' # Obtained from https://developer.microsoft.com/en-us/microsoft-edge/webview2/#download-section
    WebViewInstalled = False

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

if __name__ == "__main__":
    result = subprocess.run(['powershell', '-Command', r'Get-ChildItem .\pub\Microsoft.Maui.dll | Select-Object -ExpandProperty VersionInfo | Select-Object ProductVersion | Select-Object -ExpandProperty ProductVersion'], stdout=subprocess.PIPE, stderr=subprocess.STDOUT, shell=True)
    os.environ["MAUI_VERSION"] = result.stdout.decode('utf-8').strip()
    print(f'Env: MAUI_VERSION: {os.environ["MAUI_VERSION"]}')
    if("sha" not in os.environ["MAUI_VERSION"] and "azdo" not in os.environ["MAUI_VERSION"]):
        raise ValueError(f"MAUI_VERSION does not contain sha and azdo indicating failure to retrieve or set the value. MAUI_VERSION: {os.environ['MAUI_VERSION']}")
    main()
