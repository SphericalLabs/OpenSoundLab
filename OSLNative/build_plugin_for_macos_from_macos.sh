#!/bin/bash

# Configuration
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_PATH="${SCRIPT_DIR}/Xcode/OSLNative.xcodeproj"
CONFIGURATION="Release"

echo "Build macOS Plugin from macOS..."

# Check requirements
if ! command -v xcodebuild &> /dev/null; then
    echo "Error: xcodebuild could not be found."
    exit 1
fi

if [[ "$(xcode-select -p)" == *"/CommandLineTools"* ]]; then
    echo "Warning: active developer directory is CommandLineTools."
    echo "Attempting to locate Xcode.app..."

    # Try to find Xcode by Bundle ID, taking the first match
    XCODE_APP=$(mdfind "kMDItemCFBundleIdentifier == 'com.apple.dt.Xcode'" | grep ".app$" | head -n 1)

    if [ -d "$XCODE_APP" ]; then
        echo "Found Xcode at: $XCODE_APP"
        export DEVELOPER_DIR="$XCODE_APP/Contents/Developer"
    else
        echo "Error: Could not locate Xcode.app. Please run 'sudo xcode-select -s /path/to/Xcode.app'"
        exit 1
    fi
fi

echo "Building Xcode project..."
xcodebuild -project "$PROJECT_PATH" -alltargets -configuration "$CONFIGURATION"

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
    cp "$OUTPUT_FILE" "$DEST_FILE"
    echo "Success: Created $DEST_FILE"
else
    echo "Error: Build finished but output file not found at $OUTPUT_FILE"
    exit 1
fi

# Note: Xcode usually handles the output placement based on project settings.
# Assuming formatting/copying to Assets is handled by the project build phases or default Xcode behavior.
# If not, we might need to add a copy step here similar to other scripts,
# but usually for Unity plugins it's set in Xcode's "Build Locations" or a post-build script.

echo "Success: macOS build completed."
