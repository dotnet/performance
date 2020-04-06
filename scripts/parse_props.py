import xml.etree.ElementTree as ET
import os
import argparse

parser = argparse.ArgumentParser(description='Get the branch name.')
parser.add_argument('--branch-name', type=str, dest='branch',
                   help='an integer for the accumulator')

args = parser.parse_args()

if not args.branch == "master":
    exit(0)
if not os.path.exists('eng/Versions.props'):
    raise  ValueError("Versions.props does not exist")
tree = ET.parse('eng/Versions.props')
root = tree.getroot().find("PropertyGroup/MicrosoftDotnetSdkInternalPackageVersion")
if root == None:
    raise ValueError("Structure of Versions.props has changed")
print("##vso[task.setvariable variable=DotnetVersion;isSecret=false;isOutput=false]--dotnet-versions {}".format(root.text))