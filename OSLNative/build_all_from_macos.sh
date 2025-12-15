#!/bin/bash

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
USAGE="Usage: ./build_all_from_macos.sh [android|windows|macos|all]"

TARGET=$1

if [ -z "$TARGET" ]; then
    echo "$USAGE"
    exit 1
fi

build_android() {
    echo "--------------------------------------------------"
    echo "Target: Android"
    echo "--------------------------------------------------"
    "$SCRIPT_DIR/build_plugin_for_android_from_macos.sh"
}

build_windows() {
    echo "--------------------------------------------------"
    echo "Target: Windows"
    echo "--------------------------------------------------"
    "$SCRIPT_DIR/build_plugin_for_windows_from_macos.sh"
}

build_macos() {
    echo "--------------------------------------------------"
    echo "Target: macOS"
    echo "--------------------------------------------------"
    "$SCRIPT_DIR/build_plugin_for_macos_from_macos.sh"
}

case "$TARGET" in
    android)
        build_android
        ;;
    windows)
        build_windows
        ;;
    macos)
        build_macos
        ;;
    all)
        build_android
        build_windows
        build_macos
        ;;
    *)
        echo "Invalid target: $TARGET"
        echo "$USAGE"
        exit 1
        ;;
esac

echo "--------------------------------------------------"
echo "Done."
