import xml.etree.ElementTree as ET
tree = ET.parse('eng/Versions.props')
root = tree.getroot().find("PropertyGroup/MicrosoftDotnetSdkInternalPackageVersion")
print("##vso[task.setvariable variable=DotnetVersion;isSecret=false;isOutput=false]--dotnet-versions {}".format(root.text))