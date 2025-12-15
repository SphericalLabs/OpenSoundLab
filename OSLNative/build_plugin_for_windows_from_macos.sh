#!/bin/bash

# Configuration
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
OUTPUT_DIR="${SCRIPT_DIR}/../Assets/OSLNative/x64/Release"
OUTPUT_FILE="OSLNative.dll"
SOURCE_FILES="Artefact.cpp Compressor.cpp CRingBuffer.cpp Delay.cpp Filter.cpp FreeVerb/freeverb/components/allpass.cpp FreeVerb/freeverb/components/comb.cpp FreeVerb/freeverb/components/revmodel.cpp Freeverb.cpp main.cpp MasterBusRecorder/AudioPluginUtil.cpp MasterBusRecorder/MasterBusRecorder.cpp resample.cpp RingBuffer.cpp util.c"
INCLUDES="-IMasterBusRecorder -IFreeVerb/dfx-library -IFreeVerb/freeverb/components"
DEFINES="-DWIN32 -D_WINDOWS -D_USRDLL -DOSLNative_EXPORTS -DNDEBUG"
FLAGS="-shared -static-libgcc -static-libstdc++ -Wl,--add-stdcall-alias -O3 -std=c++17"
COMPILER="x86_64-w64-mingw32-g++"

# Main
echo "Build Windows Plugin from macOS..."

# Enter script directory to resolve relative paths
cd "$SCRIPT_DIR"

# Check requirements
if ! command -v $COMPILER &> /dev/null; then
    echo "Error: $COMPILER could not be found. Please install mingw-w64 (e.g., brew install mingw-w64)."
    exit 1
fi

mkdir -p "$OUTPUT_DIR"

echo "Compiling..."
set -e
$COMPILER $SOURCE_FILES $INCLUDES $DEFINES $FLAGS -o "$OUTPUT_DIR/$OUTPUT_FILE"

if [ -f "$OUTPUT_DIR/$OUTPUT_FILE" ]; then
    echo "Success: Created $OUTPUT_DIR/$OUTPUT_FILE"
else
    echo "Error: Build failed."
    exit 1
fi
