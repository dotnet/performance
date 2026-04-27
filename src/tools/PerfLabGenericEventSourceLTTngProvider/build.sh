#!/bin/bash

while getopts ":o:" opt; do
    case $opt in
        o)
            OUTPUT=$OPTARG
            ;;
        :)
            echo "Option -$OPTARG requires an argument." >&2
            exit 1
            ;;
        \?)
            echo "Invalid option: -$OPTARG." >&2
            exit 1
            ;;
    esac
done

pushd $(dirname $(realpath $0)) > /dev/null

# install liblttng-ust-dev

gcc -O2 -c -I. -fpic PerfLabGenericEventSourceLTTngProvider.c && \
gcc -O2 -o $OUTPUT/PerfLabGenericEventSourceLTTngProvider.so -shared PerfLabGenericEventSourceLTTngProvider.o -llttng-ust

BUILD_EXIT_CODE=$?

popd > /dev/null

exit $BUILD_EXIT_CODE
