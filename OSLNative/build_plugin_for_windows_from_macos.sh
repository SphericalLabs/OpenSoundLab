#!/bin/bash

# Configuration
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
OUTPUT_DIR="${SCRIPT_DIR}/../Assets/Plugins/OSLNative/x64/Release"
OUTPUT_FILE="OSLNative.dll"
DEFINES=(-DWIN32 -D_WINDOWS -D_USRDLL -DOSLNative_EXPORTS -DNDEBUG -DTEST=1)
C_FLAGS=(-O3 -std=gnu99)
CXX_FLAGS=(-O3 -std=c++17)
LINK_FLAGS=(-shared -static-libgcc -static-libstdc++ -Wl,--add-stdcall-alias -lws2_32 -liphlpapi -lm)
C_COMPILER="x86_64-w64-mingw32-gcc"
CXX_COMPILER="x86_64-w64-mingw32-g++"

# Main
echo "Build Windows Plugin from macOS..."
export PATH="/opt/homebrew/bin:/usr/local/bin:$PATH"

# Enter script directory to resolve relative paths
cd "$SCRIPT_DIR"

source "$SCRIPT_DIR/Build/native_sources.sh"
osl_native_print_source_summary

INCLUDES=()
for includeDir in "${OSL_NATIVE_INCLUDE_DIRS[@]}"; do
    INCLUDES+=("-I${includeDir}")
done

# Check requirements
if ! command -v "$C_COMPILER" &> /dev/null; then
    echo "Error: $C_COMPILER could not be found. Please install mingw-w64 (e.g., brew install mingw-w64)."
    exit 1
fi

if ! command -v "$CXX_COMPILER" &> /dev/null; then
    echo "Error: $CXX_COMPILER could not be found. Please install mingw-w64 (e.g., brew install mingw-w64)."
    exit 1
fi

mkdir -p "$OUTPUT_DIR"
OBJECT_DIR="$SCRIPT_DIR/Build/obj/windows"
mkdir -p "$OBJECT_DIR"

echo "Compiling..."
set -e
OBJECTS=()
for sourceFile in "${OSL_NATIVE_SOURCES[@]}"; do
    objectFile="$OBJECT_DIR/${sourceFile//\//_}.o"
    OBJECTS+=("$objectFile")

    case "$sourceFile" in
        *.c)
            "$C_COMPILER" -c "$sourceFile" "${INCLUDES[@]}" "${DEFINES[@]}" "${C_FLAGS[@]}" -o "$objectFile"
            ;;
        *)
            "$CXX_COMPILER" -c "$sourceFile" "${INCLUDES[@]}" "${DEFINES[@]}" "${CXX_FLAGS[@]}" -o "$objectFile"
            ;;
    esac
done

echo "Linking..."
"$CXX_COMPILER" "${OBJECTS[@]}" "${LINK_FLAGS[@]}" -o "$OUTPUT_DIR/$OUTPUT_FILE"

if [ -f "$OUTPUT_DIR/$OUTPUT_FILE" ]; then
    echo "Success: Created $OUTPUT_DIR/$OUTPUT_FILE"
    "$SCRIPT_DIR/Build/restore_plugin_meta_templates.sh" "x64/Release/OSLNative.dll.meta"
else
    echo "Error: Build failed."
    exit 1
fi
