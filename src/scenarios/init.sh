if [[ -z $1 ]]; then
    echo "Please specify a channel to download dotnet from; example: ./init.sh <channel>"
    exit 1
fi

ABSOLUTE_PATH="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
SCRIPT_PATH="$ABSOLUTE_PATH/../../scripts"
export PYTHONPATH=$PYTHONPATH:$SCRIPT_PATH:$ABSOLUTE_PATH
DOTNET_SCRIPT="$SCRIPT_PATH/dotnet.py"
DOTNET_DIRECTORY="$ABSOLUTE_PATH/dotnet"

if [[ -d $DOTNET_DIRECTORY ]]; then
    echo "Removing $DOTNET_DIRECTORY"
    rm -r $DOTNET_DIRECTORY
fi

echo "Downloading dotnet from channel $1"
python $DOTNET_SCRIPT install --channels $1 --install-dir $DOTNET_DIRECTORY

export DOTNET_CLI_TELEMETRY_OPTOUT='1'
export DOTNET_MULTILEVEL_LOOKUP='0'
export UseSharedCompilation='false'
export DOTNET_ROOT=$DOTNET_DIRECTORY
export PATH="$DOTNET_DIRECTORY:$PATH"

dotnet --info
