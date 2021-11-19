from logging import getLogger
import os
'''
post cleanup script
'''

from shared.postcommands import clean_directories

getLogger().info("CExit Code: " + str(os.getenv('_commandExitCode')  ))
clean_directories()
