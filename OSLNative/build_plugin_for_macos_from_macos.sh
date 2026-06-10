#!/bin/bash

# Configuration
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_PATH="${SCRIPT_DIR}/Xcode/OSLNative.xcodeproj"
CONFIGURATION="Release"
MODULE_CACHE_PATH=""

echo "Build macOS Plugin from macOS..."

resolve_developer_dir() {
    if [ -n "$DEVELOPER_DIR" ] && [ -x "$DEVELOPER_DIR/usr/bin/xcodebuild" ]; then
        echo "$DEVELOPER_DIR"
        return 0
    fi

    local selected_dir
    selected_dir="$(xcode-select -p 2>/dev/null)"
    if [ -n "$selected_dir" ] && [[ "$selected_dir" != *"/CommandLineTools"* ]] && [ -x "$selected_dir/usr/bin/xcodebuild" ]; then
        echo "$selected_dir"
        return 0
    fi

    local xcode_app
    for xcode_app in /Applications/Xcode*.app; do
        if [ -d "$xcode_app/Contents/Developer" ]; then
            echo "$xcode_app/Contents/Developer"
            return 0
        fi
    done

    xcode_app=$(mdfind "kMDItemCFBundleIdentifier == 'com.apple.dt.Xcode'" | grep ".app$" | head -n 1)
    if [ -d "$xcode_app/Contents/Developer" ]; then
        echo "$xcode_app/Contents/Developer"
        return 0
    fi

    return 1
}

# Check requirements
if ! command -v xcodebuild &> /dev/null; then
    echo "Error: xcodebuild could not be found."
    exit 1
fi

if ! DEVELOPER_DIR="$(resolve_developer_dir)"; then
    echo "Error: Could not locate a usable Xcode developer directory."
    echo "Please run 'sudo xcode-select -s /path/to/Xcode.app/Contents/Developer' or set DEVELOPER_DIR."
    exit 1
fi
export DEVELOPER_DIR
echo "Using developer directory: $DEVELOPER_DIR"

MODULE_CACHE_PATH="$(mktemp -d "${TMPDIR:-/tmp}/osl_module_cache.XXXXXX")"
cleanup() {
    if [ -n "$MODULE_CACHE_PATH" ] && [ -d "$MODULE_CACHE_PATH" ]; then
        rm -rf "$MODULE_CACHE_PATH"
    fi
}
trap cleanup EXIT

echo "Building Xcode project..."
xcodebuild -project "$PROJECT_PATH" -alltargets -configuration "$CONFIGURATION" \
    CLANG_MODULE_CACHE_PATH="$MODULE_CACHE_PATH"

# We will check if the build succeeded.
if [ $? -ne 0 ]; then
    echo "Error: macOS build failed."
    exit 1
fi

# Move artifact to Assets
OUTPUT_FILE="${SCRIPT_DIR}/Xcode/build/Release/libOSLNative.dylib"
DEST_FILE="${SCRIPT_DIR}/../Assets/OSLNative/macos/Release/libOSLNative.dylib"

if [ -f "$OUTPUT_FILE" ]; then
    mkdir -p "$(dirname "$DEST_FILE")"
    mv "$OUTPUT_FILE" "$DEST_FILE"
    echo "Success: Moved $OUTPUT_FILE -> $DEST_FILE"
else
    echo "Error: Build finished but output file not found at $OUTPUT_FILE"
    exit 1
fi

# Note: Xcode usually handles the output placement based on project settings.
# Assuming formatting/copying to Assets is handled by the project build phases or default Xcode behavior.
# If not, we might need to add a copy step here similar to other scripts,
# but usually for Unity plugins it's set in Xcode's "Build Locations" or a post-build script.

echo "Success: macOS build completed."
