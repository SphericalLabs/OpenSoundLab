#!/usr/bin/env bash

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
source "$SCRIPT_DIR/common.sh"

USAGE="Usage: ./build_all_from_macos.sh [macos|android|ios|macos-android|all]"

TARGET="${1:-}"
if [ -z "$TARGET" ]; then
    echo "$USAGE"
    exit 1
fi

run_with_deferred_install() {
    OPEN_RESONANCE_SKIP_INSTALL=1 "$@"
}

case "$TARGET" in
    macos)
        run_with_deferred_install "$SCRIPT_DIR/build_unity_for_macos_from_macos.sh"
        ;;
    android)
        run_with_deferred_install "$SCRIPT_DIR/build_unity_for_android_from_macos.sh"
        ;;
    ios)
        or_require_ios_sdk
        run_with_deferred_install "$SCRIPT_DIR/build_unity_for_ios_from_macos.sh"
        ;;
    macos-android)
        run_with_deferred_install "$SCRIPT_DIR/build_unity_for_macos_from_macos.sh"
        run_with_deferred_install "$SCRIPT_DIR/build_unity_for_android_from_macos.sh"
        ;;
    all)
        or_require_ios_sdk
        run_with_deferred_install "$SCRIPT_DIR/build_unity_for_macos_from_macos.sh"
        run_with_deferred_install "$SCRIPT_DIR/build_unity_for_android_from_macos.sh"
        run_with_deferred_install "$SCRIPT_DIR/build_unity_for_ios_from_macos.sh"
        ;;
    *)
        echo "Invalid target: $TARGET"
        echo "$USAGE"
        exit 1
        ;;
esac

if [ "${OPEN_RESONANCE_SKIP_INSTALL:-0}" != "1" ]; then
    case "$TARGET" in
        macos)
            "$SCRIPT_DIR/install_unity_package.sh" macos-x86_64 macos-arm64
            ;;
        android)
            "$SCRIPT_DIR/install_unity_package.sh" android-arm64-v8a
            ;;
        ios)
            "$SCRIPT_DIR/install_unity_package.sh" ios-os64
            ;;
        macos-android)
            "$SCRIPT_DIR/install_unity_package.sh" macos-x86_64 macos-arm64 android-arm64-v8a
            ;;
        all)
            "$SCRIPT_DIR/install_unity_package.sh" macos-x86_64 macos-arm64 android-arm64-v8a ios-os64
            ;;
    esac
fi

echo "Done."
