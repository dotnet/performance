print_usage() {
    echo "Invalid argument. Usage:"
    echo "./init.sh"
    echo "./init.sh -dotnetdir <custom dotnet directory>"
    echo "./init.sh -channel <channel to download new dotnet>"
    exit 1
}

setup_python3_version() {
    sudo apt -y install python3.7-dev python3.7-venv
    sudo update-alternatives --install /usr/bin/python3 python3 /usr/bin/python3.7 1
    sudo update-alternatives --set python3 /usr/bin/python3.7
}

# $1 is dotnet directory
setup_env() {
    export DOTNET_ROOT="$1"
    export PATH="$1:$PATH"
    export DOTNET_CLI_TELEMETRY_OPTOUT='1'
    export DOTNET_MULTILEVEL_LOOKUP='0'
    export UseSharedCompilation='false'
}

# $1 is channel
download_dotnet() {
    dotnetScript="$scriptPath/dotnet.py"
    echo "Downloading dotnet from channel $1"
    python3 $dotnetScript install --channels $1 -v
}

setup_python3_version
# Add scripts and current directory to PYTHONPATH
absolutePath="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
scriptPath="$absolutePath/../../scripts"
export PYTHONPATH=$PYTHONPATH:$scriptPath:$absolutePath

# Parse arguments
if [ "$#" -gt 2 ]
then 
    print_usage
elif [ "$1" == "-dotnetdir" ] && [ "$2" != "" ]
then
    dotnetDirectory="$2"
    setup_env $dotnetDirectory
elif [ "$1" == "-channel" ] && [ "$2" != "" ]
then
    channel="$2"
    download_dotnet $channel
    dotnetDirectory="$absolutePath/../../tools/dotnet/x64"
    setup_env $dotnetDirectory
elif [ "$#" -gt 0 ]
then 
    print_usage
fi
