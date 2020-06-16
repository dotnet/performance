import xml.etree.ElementTree as ET
import os
import argparse

parser = argparse.ArgumentParser(description='Get the branch name.')
parser.add_argument('--branch-name', type=str, dest='branch',
                   help='Name of the branch we are running')

args = parser.parse_args()

if not args.branch == "master":
    print("##vso[task.setvariable variable=DotnetVersion;isSecret=false;isOutput=false]")
else:
    if not os.path.exists('eng/Versions.props'):
        raise  FileNotFoundError("Versions.props does not exist")
    tree = ET.parse('eng/Versions.props')
    root = tree.getroot().find("PropertyGroup/MicrosoftDotnetSdkInternalPackageVersion")
    if root == None:
        raise ValueError("Structure of Versions.props has changed")
    print("##vso[task.setvariable variable=DotnetVersion;isSecret=false;isOutput=false]--dotnet-versions {}".format(root.text))