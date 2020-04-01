import xml.etree.ElementTree as ET
import os

if not os.path.exists('eng/Versions.props'):
    raise  ValueError("Versions.props does not exist")
tree = ET.parse('eng/Versions.props')
root = tree.getroot().find("PropertyGroup/MicrosoftDotnetSdkInternalPackageVersion")
if root == None:
    raise ValueError("Structure of Versions.props has changed")
print("##vso[task.setvariable variable=DotnetVersion;isSecret=false;isOutput=false]--dotnet-versions {}".format(root.text))