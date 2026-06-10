#!/usr/bin/env bash

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
source "$SCRIPT_DIR/common.sh"

USAGE="Usage: ./build_unity_for_android_from_macos.sh [arm64-v8a]"
ANDROID_ABI="${1:-arm64-v8a}"
ANDROID_PLATFORM="${OPEN_RESONANCE_ANDROID_PLATFORM:-android-22}"

if [ "$ANDROID_ABI" != "arm64-v8a" ]; then
    echo "Invalid Android ABI: $ANDROID_ABI"
    echo "$USAGE"
    exit 1
fi

ANDROID_NDK_PATH="$(or_find_android_ndk || true)"
if [ -z "$ANDROID_NDK_PATH" ]; then
    or_fail "Android NDK could not be found. Set OPEN_RESONANCE_ANDROID_NDK or install Android support for the Unity editor version in ProjectSettings/ProjectVersion.txt."
fi

ANDROID_TOOLCHAIN_FILE="$ANDROID_NDK_PATH/build/cmake/android.toolchain.cmake"
if [ ! -f "$ANDROID_TOOLCHAIN_FILE" ]; then
    or_fail "Android CMake toolchain file is missing: $ANDROID_TOOLCHAIN_FILE"
fi

or_clone_core_deps
or_clone_unity_deps --skip_embree -a="$(or_host_arch)"

or_cmake_configure_and_install "android-$ANDROID_ABI" \
    -DCMAKE_TOOLCHAIN_FILE="$ANDROID_TOOLCHAIN_FILE" \
    -DANDROID_PLATFORM="$ANDROID_PLATFORM" \
    -DANDROID_ABI="$ANDROID_ABI" \
    -DRA_UNITY_ENABLE_REVERB_BAKING:BOOL=OFF \
    -DRA_UNITY_ENABLE_SOUNDFIELD_RECORDING:BOOL=ON

or_maybe_install_unity_assets "android-$ANDROID_ABI"
