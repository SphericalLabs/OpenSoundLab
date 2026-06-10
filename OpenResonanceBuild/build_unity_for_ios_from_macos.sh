#!/usr/bin/env bash

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
source "$SCRIPT_DIR/common.sh"

IOS_TOOLCHAIN_FILE="$OR_SUBMODULE_DIR/third_party/ios-cmake/ios.toolchain.cmake"

or_require_ios_sdk
or_clone_core_deps
or_clone_unity_deps --skip_embree -a="$(or_host_arch)"

if [ ! -f "$IOS_TOOLCHAIN_FILE" ]; then
    or_fail "iOS CMake toolchain file is missing: $IOS_TOOLCHAIN_FILE"
fi

or_cmake_configure_and_install "ios-os64" \
    -DCMAKE_TOOLCHAIN_FILE="$IOS_TOOLCHAIN_FILE" \
    -DPLATFORM=OS64 \
    -DCMAKE_OSX_ARCHITECTURES=arm64 \
    -DRA_UNITY_ENABLE_REVERB_BAKING:BOOL=OFF \
    -DRA_UNITY_ENABLE_SOUNDFIELD_RECORDING:BOOL=ON

or_maybe_install_unity_assets "ios-os64"
