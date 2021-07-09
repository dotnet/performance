'''
post cleanup script
'''

from shared.postcommands import clean_directories
import subprocess

clean_directories()
subprocess.run(["dotnet", "workload", "uninstall", "microsoft-net-sdk-blazorwebassembly-aot"])
