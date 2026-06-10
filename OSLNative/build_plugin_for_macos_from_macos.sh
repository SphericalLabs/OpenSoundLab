#!/bin/bash

set -euo pipefail
shopt -s nullglob

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
OUTPUT_DIR="${SCRIPT_DIR}/../Assets/Plugins/OSLNative/macos/Release"
OUTPUT_FILE="${OUTPUT_DIR}/libOSLNative.dylib"
MACOS_DEPLOYMENT_TARGET="${OSL_NATIVE_MACOS_DEPLOYMENT_TARGET:-11.5}"
MACOS_ARCHS="${OSL_NATIVE_MACOS_ARCHS:-arm64}"
MACOS_SDKROOT="${OSL_NATIVE_MACOS_SDKROOT:-}"
ENABLE_FAST_MATH="${OSL_NATIVE_FAST_MATH:-0}"
ENABLE_LTO="${OSL_NATIVE_LTO:-1}"
ENABLE_STRIP="${OSL_NATIVE_STRIP:-1}"
ENABLE_CODESIGN="${OSL_NATIVE_CODESIGN:-1}"

cd "$SCRIPT_DIR"

source "$SCRIPT_DIR/Build/native_sources.sh"
osl_native_print_source_summary

INCLUDES=()
for includeDir in "${OSL_NATIVE_INCLUDE_DIRS[@]}"; do
    INCLUDES+=("-I${includeDir}")
done

DEFINES=(
    -DTEST=1
    -DNDEBUG
    -DDEBUG=0
    -DCoreAudio_Debug=0
)

ARCH_FLAGS=()
for arch in $MACOS_ARCHS; do
    ARCH_FLAGS+=("-arch" "$arch")
done

COMMON_FLAGS=(
    "${ARCH_FLAGS[@]}"
    -O3
    -g0
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

if [ "$ENABLE_LTO" = "1" ]; then
    COMMON_FLAGS+=(-flto=thin)
fi

if [ "$ENABLE_FAST_MATH" = "1" ]; then
    COMMON_FLAGS+=(-ffast-math)
fi

if [ -z "$MACOS_SDKROOT" ] && command -v xcrun >/dev/null 2>&1; then
    MACOS_SDKROOT="$(xcrun --sdk macosx --show-sdk-path 2>/dev/null || true)"
fi

if [ -n "$MACOS_SDKROOT" ]; then
    COMMON_FLAGS+=(-isysroot "$MACOS_SDKROOT")
fi

C_FLAGS=(
    "${COMMON_FLAGS[@]}"
    -std=gnu99
)

CXX_FLAGS=(
    "${COMMON_FLAGS[@]}"
    -std=c++17
    -fvisibility-inlines-hidden
)

LINK_FLAGS=(
    -dynamiclib
    -Wl,-dead_strip
    -Wl,-dead_strip_dylibs
    -framework
    Accelerate
)

echo "Build macOS Plugin from macOS..."

CLANG="$(xcrun --find clang 2>/dev/null || command -v clang || true)"
CLANGXX="$(xcrun --find clang++ 2>/dev/null || command -v clang++ || true)"
STRIP_BIN="$(xcrun --find strip 2>/dev/null || command -v strip || true)"

if [ -z "$CLANG" ]; then
    echo "Error: clang could not be found."
    exit 1
fi

if [ -z "$CLANGXX" ]; then
    echo "Error: clang++ could not be found."
    exit 1
fi

mkdir -p "$OUTPUT_DIR"
OBJECT_DIR="$SCRIPT_DIR/Build/obj/macos"
mkdir -p "$OBJECT_DIR"

OBJECTS=()
for sourceFile in "${OSL_NATIVE_SOURCES[@]}"; do
    objectFile="$OBJECT_DIR/${sourceFile//\//_}.o"
    OBJECTS+=("$objectFile")

    case "$sourceFile" in
        *.c)
            "$CLANG" -c "$sourceFile" "${INCLUDES[@]}" "${DEFINES[@]}" "${C_FLAGS[@]}" -o "$objectFile"
            ;;
        *)
            "$CLANGXX" -c "$sourceFile" "${INCLUDES[@]}" "${DEFINES[@]}" "${CXX_FLAGS[@]}" -o "$objectFile"
            ;;
    esac
done

echo "Linking..."
"$CLANGXX" \
    "${OBJECTS[@]}" \
    "${COMMON_FLAGS[@]}" \
    "${LINK_FLAGS[@]}" \
    -o "$OUTPUT_FILE"

if [ -f "$OUTPUT_FILE" ]; then
    if [ "$ENABLE_STRIP" = "1" ]; then
        if [ -n "$STRIP_BIN" ]; then
            echo "Stripping local/debug symbols..."
            "$STRIP_BIN" -S -x "$OUTPUT_FILE"
        else
            echo "Warning: strip could not be found; leaving symbols unstripped."
        fi
    fi

    if [ "$ENABLE_CODESIGN" = "1" ]; then
        if command -v codesign >/dev/null 2>&1; then
            echo "Ad-hoc code signing..."
            codesign --force --timestamp=none --sign - "$OUTPUT_FILE"
        else
            echo "Warning: codesign could not be found; leaving dylib unsigned."
        fi
    fi

    echo "Success: Created $OUTPUT_FILE"
    "$SCRIPT_DIR/Build/restore_plugin_meta_templates.sh" "macos/Release/libOSLNative.dylib.meta"
else
    echo "Error: Build finished but output file was not created."
    exit 1
fi

echo "Success: macOS build completed."
