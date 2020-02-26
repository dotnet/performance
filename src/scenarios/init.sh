print_usage() {
    echo "Invalid argument. Usage:"
    echo "./init.sh"
    echo "./init.sh -dotnetdir <custom dotnet directory>"
    echo "./init.sh -channel <channel to download new dotnet>"
    exit 1
}

# Add scripts and current directory to PYTHONPATH
absolutePath="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
scriptPath="$absolutePath/../../scripts"
export PYTHONPATH=$PYTHONPATH:$scriptPath:$absolutePath

dotnetDirectory="$absolutePath/../../tools/dotnet/x64"
channel=""

# Parse arguments
if [ "$#" -gt 2 ]
then 
    print_usage
elif [ "$1" == "-dotnetdir" ] && [ "$2" != "" ]
then
    dotnetDirectory="$2"
elif [ "$1" == "-channel" ] && [ "$2" != "" ]
then
    channel="$2"
elif [ "$#" -gt 0 ]
then 
    print_usage
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
