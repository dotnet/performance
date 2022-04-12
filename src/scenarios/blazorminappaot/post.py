'''
post cleanup script
'''

from shared.postcommands import PostCommands
import subprocess

postcommands = PostCommands()
postcommands.clean_directories()
postcommands.uninstall_workload('wasm-tools')
