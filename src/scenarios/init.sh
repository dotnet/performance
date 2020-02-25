# Add scripts and current directory to PYTHONPATH
absolutePath="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
scriptPath="$absolutePath/../../scripts"
export PYTHONPATH=$PYTHONPATH:$scriptPath:$absolutePath

dotnetDirectory="$absolutePath/../../tools/dotnet/x64"
channel=""
# Parse arguments
if [ "$1" == "-dotnetdir" ]
then
    dotnetDirectory="$2"
elif [ "$1" == "-installdotnetfromchannel" ]
then
    channel="$2"
fi

# Download dotnet from the specified channel
if [ "$channel" != "" ]
then
    dotnetScript="$scriptPath/dotnet.py"
    echo "Downloading dotnet from channel $channel"
    python3 $dotnetScript install --channels $channel -v
fi

export DOTNET_ROOT=$dotnetDirectory
export PATH="$dotnetDirectory:$PATH"
export DOTNET_CLI_TELEMETRY_OPTOUT='1'
export DOTNET_MULTILEVEL_LOOKUP='0'
export UseSharedCompilation='false'
