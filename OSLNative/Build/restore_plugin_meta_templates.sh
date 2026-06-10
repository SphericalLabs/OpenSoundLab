#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
NATIVE_DIR="$(cd "$SCRIPT_DIR/.." && pwd)"
PROJECT_DIR="$(cd "$NATIVE_DIR/.." && pwd)"
TEMPLATE_DIR="$NATIVE_DIR/PluginMetaTemplates"
PLUGIN_DIR="$PROJECT_DIR/Assets/Plugins/OSLNative"

DEFAULT_META_PATHS=(
    "arm64/Release/libOSLNative.so.meta"
    "macos/Release/libOSLNative.dylib.meta"
    "x64/Release/OSLNative.dll.meta"
)

restore_meta() {
    local relative_path="$1"
    local source_path="$TEMPLATE_DIR/$relative_path"
    local destination_path="$PLUGIN_DIR/$relative_path"
    local plugin_path="${destination_path%.meta}"

    if [ ! -f "$source_path" ]; then
        echo "Error: Missing OSLNative plugin meta template at $source_path"
        return 1
    fi

    if [ ! -f "$plugin_path" ]; then
        echo "Skipping OSLNative plugin meta restore because plugin was not built: $plugin_path"
        return 0
    fi

    mkdir -p "$(dirname "$destination_path")"
    cp "$source_path" "$destination_path"
    echo "Restored OSLNative plugin meta: $destination_path"
}

if [ "$#" -eq 0 ]; then
    set -- "${DEFAULT_META_PATHS[@]}"
fi

for relative_path in "$@"; do
    restore_meta "$relative_path"
done
