'''
post cleanup script
'''

from shared.postcommands import PostCommands, clean_directories

postcommands = PostCommands()
clean_directories()
postcommands.uninstall_workload('maui')