#!/usr/bin/env bash
# Workaround script for missing install-python3.sh
# Python3 is already available on the system

echo "Python3 is already available: $(python3 --version)"
echo "Skipping download since Python3 is already installed"

# Create the expected installation directory structure
INSTALL_PATH=""
while [[ $# > 0 ]]; do
  opt="$(echo "${1/#--/-}" | tr "[:upper:]" "[:lower:]")"
  case "$opt" in
    -installpath)
      INSTALL_PATH=$2
      shift
      ;;
    -baseuri|-version)
      # Ignore these parameters
      shift
      ;;
    *)
      ;;
  esac
  shift
done

if [[ -n "$INSTALL_PATH" ]]; then
  mkdir -p "$INSTALL_PATH"
  # Create a symlink to the system python3
  ln -sf $(which python3) "$INSTALL_PATH/python3" 2>/dev/null || true
fi

exit 0