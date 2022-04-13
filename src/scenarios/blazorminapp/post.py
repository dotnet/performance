'''
post cleanup script
'''

from shared.postcommands import PostCommands, clean_directories
import subprocess

postcommands = PostCommands()
clean_directories()
postcommands.uninstall_workload('wasm-tools')
