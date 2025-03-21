#!/bin/sh

print_usage() {
    echo "***Please run this script as SOURCE. ex: . ./init.sh***"
    echo "This script sets up PYTHONPATH and determines which dotnet to use."
    echo "Choose ONE of the following commands:"
    echo ". ./init.sh                                             # sets up PYTHONPATH only; uses default dotnet in PATH"
    echo ". ./init.sh -dotnetdir <custom dotnet directory>        # sets up PYTHONPATH; uses the specified dotnet"
    echo ". ./init.sh -channel <channel to download new dotnet>   # sets up PYTHONPATH; downloads dotnet from the specified channel or branch to <repo root>/tools/ and uses it; for a list of channels, check <repo root>/scripts/channel_map.py"
}

# $1 is dotnet directory
setup_env() {
    export DOTNET_ROOT="$1"
    export PATH="$1:$PATH"
    export DOTNET_CLI_TELEMETRY_OPTOUT='1'
    export DOTNET_MULTILEVEL_LOOKUP='0'
    export UseSharedCompilation='false'
    echo "Current dotnet directory: $DOTNET_ROOT"
    echo "If more than one version exist in this directory, usually the latest runtime and sdk will be used."
}

# $1 is channel
download_dotnet() {
    dotnetScript="$scriptPath/dotnet.py"
    echo "Downloading dotnet from channel $1"
    python3 $dotnetScript install --channels $1 -v
}

# Function to determine CPU architecture
get_cpu_architecture() {
    local arch
    arch=$(uname -m)
    if [[ $arch == "x86_64" ]]; then
        echo "x64"
    elif [[ $arch == i*86 ]]; then
        echo "x86"
    elif [[ $arch == "arm64" || $arch == "aarch64" ]]; then
        echo "arm64"
    else
        echo "Unknown architecture: $arch"
        exit 1
    fi
}

# Add scripts and current directory to PYTHONPATH
absolutePath="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
scriptPath="$absolutePath/../../scripts"
export PYTHONPATH=$PYTHONPATH:$scriptPath:$absolutePath
echo "PYTHONPATH=$PYTHONPATH"

# Parse arguments
if [ "$1" = "-help" ] || [ "$1" = "-h" ]
then
    print_usage
elif [ "$#" -gt 2 ]
then 
    print_usage
elif [ "$1" = "-dotnetdir" ] && [ "$2" != "" ]
then
    dotnetDirectory="$2"
    setup_env $dotnetDirectory
elif [ "$1" = "-channel" ] && [ "$2" != "" ]
then
    channel="$2"
    download_dotnet $channel
    dotnetDirectory="$absolutePath/../../tools/dotnet/$(get_cpu_architecture)"
    setup_env $dotnetDirectory
elif [ "$#" -gt 0 ]
then 
    print_usage
fi
