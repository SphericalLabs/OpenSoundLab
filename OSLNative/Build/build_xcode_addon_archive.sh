#!/usr/bin/env bash

set -euo pipefail
shopt -s nullglob

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
NATIVE_DIR="$(cd "$SCRIPT_DIR/.." && pwd)"
CONFIGURATION_NAME="${CONFIGURATION:-Release}"
OUTPUT_DIR="${OSL_NATIVE_XCODE_ADDON_BUILD_DIR:-$NATIVE_DIR/Xcode/build/XcodeAddons/$CONFIGURATION_NAME}"
OUTPUT_FILE="${OSL_NATIVE_XCODE_ADDON_ARCHIVE:-$OUTPUT_DIR/libOSLNativeAddons.a}"
OBJECT_DIR="$OUTPUT_DIR/Objects"
MACOS_DEPLOYMENT_TARGET="${MACOSX_DEPLOYMENT_TARGET:-${OSL_NATIVE_MACOS_DEPLOYMENT_TARGET:-11.5}}"
MACOS_SDKROOT="${SDKROOT:-${OSL_NATIVE_MACOS_SDKROOT:-}}"
MACOS_ARCHS="${ARCHS:-${OSL_NATIVE_MACOS_ARCHS:-arm64}}"

cd "$NATIVE_DIR"

source "$SCRIPT_DIR/native_sources.sh"

ADDON_SOURCES=()
for sourcePath in "${OSL_NATIVE_SOURCES[@]}"; do
    if osl_native_is_addon_source "$sourcePath"; then
        ADDON_SOURCES+=("$sourcePath")
    fi
done

INCLUDES=()
for includeDir in "${OSL_NATIVE_INCLUDE_DIRS[@]}"; do
    INCLUDES+=("-I${includeDir}")
done

DEFINES=(
    -DTEST=1
    -DCoreAudio_Debug=0
)

if [ "$CONFIGURATION_NAME" = "Debug" ]; then
    DEFINES+=(-DDEBUG=1)
    OPTIMIZATION_FLAGS=(-O0 -g)
else
    DEFINES+=(-DNDEBUG -DDEBUG=0)
    OPTIMIZATION_FLAGS=(-O3 -g0)
fi

ARCH_FLAGS=()
for arch in $MACOS_ARCHS; do
    ARCH_FLAGS+=("-arch" "$arch")
done

FLAGS=(
    "${ARCH_FLAGS[@]}"
    "${OPTIMIZATION_FLAGS[@]}"
    -std=c++17
    -fPIC
    -fstrict-aliasing
    -fno-math-errno
    -ffp-contract=fast
    -fvisibility=hidden
    -fvisibility-inlines-hidden
    "-mmacosx-version-min=${MACOS_DEPLOYMENT_TARGET}"
    -Wno-braced-scalar-init
    -Wno-implicit-const-int-float-conversion
)

if [ -z "$MACOS_SDKROOT" ] || [ "$MACOS_SDKROOT" = "macosx" ]; then
    MACOS_SDKROOT="$(xcrun --sdk macosx --show-sdk-path 2>/dev/null || true)"
fi

if [ -n "$MACOS_SDKROOT" ]; then
    FLAGS+=(-isysroot "$MACOS_SDKROOT")
fi

CLANGXX="$(xcrun --find clang++ 2>/dev/null || command -v clang++ || true)"
LIBTOOL="$(xcrun --find libtool 2>/dev/null || command -v libtool || true)"

if [ -z "$CLANGXX" ]; then
    echo "Error: clang++ could not be found."
    exit 1
fi

if [ -z "$LIBTOOL" ]; then
    echo "Error: libtool could not be found."
    exit 1
fi

mkdir -p "$OBJECT_DIR"
rm -f "$OBJECT_DIR"/*.o "$OUTPUT_FILE"

OBJECTS=()

compile_source() {
    local sourceFile="$1"
    local objectName
    local objectFile

    objectName="$(printf "%s" "$sourceFile" | tr '/ .' '___')"
    objectFile="$OBJECT_DIR/${objectName}.o"

    "$CLANGXX" \
        -c "$sourceFile" \
        "${INCLUDES[@]}" \
        "${DEFINES[@]}" \
        "${FLAGS[@]}" \
        -o "$objectFile"

    OBJECTS+=("$objectFile")
}

if [ "${#ADDON_SOURCES[@]}" -eq 0 ]; then
    EMPTY_SOURCE="$OBJECT_DIR/empty_addon_archive.cpp"
    printf "int oslNativeXcodeAddonsEmptyArchive = 0;\n" > "$EMPTY_SOURCE"
    compile_source "$EMPTY_SOURCE"
else
    for sourcePath in "${ADDON_SOURCES[@]}"; do
        compile_source "$sourcePath"
    done
fi

"$LIBTOOL" -static -o "$OUTPUT_FILE" "${OBJECTS[@]}"

osl_native_print_source_summary
echo "OSLNative Xcode addon archive: $OUTPUT_FILE"
