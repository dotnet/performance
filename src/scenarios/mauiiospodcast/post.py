'''
post cleanup script
'''

from shared.postcommands import clean_directories
from performance.common import remove_directory

remove_directory("dotnet-podcasts")
clean_directories()
