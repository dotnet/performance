# Add scripts and current directory to PYTHONPATH
absolutePath="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
scriptPath="$absolutePath/../../scripts"
export PYTHONPATH=$PYTHONPATH:$scriptPath:$absolutePath

dotnetDirectory=""
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
    installDirectory="$absolutePath/dotnet"
    # Remove existing dotnet directory to make sure we only have one version of dotnet 
    if [ -d "$installDirectory" ]
    then
        echo "Removing $installDirectory"
        rm -r $installDirectory
    fi
    dotnetScript="$scriptPath/dotnet.py"
    echo "Downloading dotnet from channel $channel"
    echo installDir: $installDirectory
    python3 $dotnetScript install --channels $channel --install-dir $installDirectory
fi

if [ "$dotnetDirectory" != "" ]
then
    export DOTNET_ROOT=$dotnetDirectory
    export PATH="$dotnetDirectory:$PATH"
elif [ "$installDirectory" != "" ]
then
    export DOTNET_ROOT=$installDirectory
    export PATH="$installDirectory:$PATH"
fi

export DOTNET_CLI_TELEMETRY_OPTOUT='1'
export DOTNET_MULTILEVEL_LOOKUP='0'
export UseSharedCompilation='false'

dotnet --info
