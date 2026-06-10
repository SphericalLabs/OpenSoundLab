#!/usr/bin/env bash

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
source "$SCRIPT_DIR/common.sh"

USAGE="Usage: ./build_unity_for_macos_from_macos.sh [x86_64|arm64|all]"
TARGET="${1:-all}"
MACOS_DEPLOYMENT_TARGET="${OPEN_RESONANCE_MACOS_DEPLOYMENT_TARGET:-11.5}"
BUILT_TARGETS=()

build_macos_x86_64() {
    or_clone_unity_deps -a=x86_64
    or_cmake_configure_and_install "macos-x86_64" \
        -DCMAKE_OSX_ARCHITECTURES=x86_64 \
        -DCMAKE_OSX_DEPLOYMENT_TARGET="$MACOS_DEPLOYMENT_TARGET" \
        -DRA_UNITY_ENABLE_REVERB_BAKING:BOOL=ON \
        -DRA_UNITY_ENABLE_SOUNDFIELD_RECORDING:BOOL=ON
    BUILT_TARGETS+=("macos-x86_64")
}

build_macos_arm64() {
    or_clone_unity_deps --skip_embree -a=arm64
    or_cmake_configure_and_install "macos-arm64" \
        -DCMAKE_OSX_ARCHITECTURES=arm64 \
        -DCMAKE_OSX_DEPLOYMENT_TARGET="$MACOS_DEPLOYMENT_TARGET" \
        -DRA_UNITY_ENABLE_REVERB_BAKING:BOOL=OFF \
        -DRA_UNITY_ENABLE_SOUNDFIELD_RECORDING:BOOL=ON
    BUILT_TARGETS+=("macos-arm64")
}

or_clone_core_deps

case "$TARGET" in
    x86_64)
        build_macos_x86_64
        ;;
    arm64)
        build_macos_arm64
        ;;
    all)
        build_macos_x86_64
        build_macos_arm64
        ;;
    *)
        echo "Invalid target: $TARGET"
        echo "$USAGE"
        exit 1
        ;;
esac

or_maybe_generate_unity_package "${BUILT_TARGETS[@]}"
