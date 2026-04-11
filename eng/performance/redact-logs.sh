#!/usr/bin/env bash

###############################################################################
# Bash equivalent of eng/common/post-build/redact-logs.ps1
# Installs (locally) the binlogtool and invokes redaction over an input path.
#
# Usage:
#   redact-logs.sh \
#     --input <path> \
#     --version <BinlogToolVersion> \
#     [--dotnet <dotnetPath>] \
#     [--feed <nugetFeedUrl>] \
#     [--tokens-file <file-with-tokens>] \
#     [token1 token2 ...]
#
# Notes:
# - Lines in the tokens file beginning with '# ' are ignored (comment lines).
# - Tokens matching the regex '^\$\(.*\)$' are ignored (likely unexpanded AzDO vars).
# - Non-zero exit from binlogtool is converted to a warning; script continues.
###############################################################################

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT_DIR="$(cd "$SCRIPT_DIR/../../.." && pwd)"
TOOLS_DIR="$ROOT_DIR/.tools"

PACKAGE_NAME="binlogtool"
DEFAULT_FEED='https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet-public/nuget/v3/index.json'
VERBOSITY='minimal'

print_usage() {
  grep '^#' "$0" | sed -e 's/^# //'
}

INPUT_PATH=""
BINLOG_TOOL_VERSION=""
DOTNET_PATH=""
PACKAGE_FEED="$DEFAULT_FEED"
TOKENS_FILE=""

ARGS=()
while [[ $# -gt 0 ]]; do
  case "$1" in
    --input)
      INPUT_PATH="$2"; shift 2 ;;
    --version)
      BINLOG_TOOL_VERSION="$2"; shift 2 ;;
    --dotnet)
      DOTNET_PATH="$2"; shift 2 ;;
    --feed)
      PACKAGE_FEED="$2"; shift 2 ;;
    --tokens-file)
      TOKENS_FILE="$2"; shift 2 ;;
    -h|--help)
      print_usage; exit 0 ;;
    --*)
      echo "Unknown option: $1" >&2; print_usage; exit 1 ;;
    *)
      # Positional / remaining tokens to redact
      ARGS+=("$1"); shift ;;
  esac
done

if [[ -z "$INPUT_PATH" || -z "$BINLOG_TOOL_VERSION" ]]; then
  echo "Error: --input and --version are required." >&2
  print_usage
  exit 1
fi

if [[ -n "$DOTNET_PATH" ]]; then
  DOTNET="$DOTNET_PATH"
else
  if command -v dotnet >/dev/null 2>&1; then
    DOTNET="$(command -v dotnet)"
  else
    echo "Error: dotnet not found and --dotnet not provided." >&2
    exit 1
  fi
fi

mkdir -p "$TOOLS_DIR"
pushd "$TOOLS_DIR" >/dev/null

# Remove global tool if installed (mirrors PowerShell behavior)
if "$DOTNET" tool list -g 2>/dev/null | grep -qi "^$PACKAGE_NAME[[:space:]]"; then
  "$DOTNET" tool uninstall "$PACKAGE_NAME" -g || true
fi

# Initialize (or reuse) local tool manifest
if [[ ! -f "./.config/dotnet-tools.json" ]]; then
  echo "Initializing local tool manifest..."
  "$DOTNET" new tool-manifest >/dev/null
fi

echo "Installing Binlog redactor CLI ($PACKAGE_NAME $BINLOG_TOOL_VERSION)..."
echo "'$DOTNET' tool install $PACKAGE_NAME --local --add-source '$PACKAGE_FEED' -v $VERBOSITY --version $BINLOG_TOOL_VERSION"
"$DOTNET" tool install "$PACKAGE_NAME" --local --add-source "$PACKAGE_FEED" -v "$VERBOSITY" --version "$BINLOG_TOOL_VERSION" 2>&1 || true

TOKENS_TO_REDACT=()

# Load tokens from file if present
if [[ -n "$TOKENS_FILE" && -f "$TOKENS_FILE" ]]; then
  echo "Adding additional sensitive data for redaction from file: $TOKENS_FILE"
  while IFS= read -r line || [[ -n "$line" ]]; do
    # Strip CR and trim
    line=${line%$'\r'}
    line="$(echo "$line" | sed -e 's/^[[:space:]]*//' -e 's/[[:space:]]*$//')"
    [[ $line == "# "* ]] && continue
    [[ -z "$line" ]] && continue
    TOKENS_TO_REDACT+=("$line")
  done < "$TOKENS_FILE"
fi

# Add tokens passed on command line
for t in "${ARGS[@]}"; do
  TOKENS_TO_REDACT+=("$t")
done

# Prepare -p: args while filtering AzDO variable patterns
PARAMS=()
shopt -s extglob
for t in "${TOKENS_TO_REDACT[@]}"; do
  [[ -z "$t" ]] && continue
  if [[ $t =~ ^\$\(.*\)$ ]]; then
    echo "Ignoring token $t as it is probably unexpanded AzDO variable"
    continue
  fi
  PARAMS+=("-p:$t")
done
shopt -u extglob

set +e
"$DOTNET" binlogtool redact --input:"$INPUT_PATH" --recurse --in-place "${PARAMS[@]}"
status=$?
set -e
if [[ $status -ne 0 ]]; then
  echo "[warning] Problems using Redactor tool (exit code: $status). But ignoring them now." >&2
fi

popd >/dev/null
echo "done."

exit 0
