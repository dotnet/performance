if [[ -z $1 ]]; then
    CHANNEL='master'
else
    CHANNEL=$1
fi

ABSOLUTE_PATH="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
SCRIPT_PATH="$ABSOLUTE_PATH/../../scripts"
export PYTHONPATH=$PYTHONPATH:$SCRIPT_PATH:$ABSOLUTE_PATH
DOTNET_SCRIPT="$SCRIPT_PATH/dotnet.py"
DOTNET_DIRECTORY="$ABSOLUTE_PATH/../../tools/dotnet/x64"
echo "Downloading dotnet from channel $CHANNEL"
python3 $DOTNET_SCRIPT install --channels $CHANNEL -v

export DOTNET_CLI_TELEMETRY_OPTOUT='1'
export DOTNET_MULTILEVEL_LOOKUP='0'
export UseSharedCompilation='false'
export DOTNET_ROOT=$DOTNET_DIRECTORY
export PATH="$DOTNET_DIRECTORY:$PATH"
