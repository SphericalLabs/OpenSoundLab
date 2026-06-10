#!/usr/bin/env bash

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
source "$SCRIPT_DIR/common.sh"

TEMPLATE_DIR="$OR_BUILD_DIR/PluginMetaTemplates"
PLUGIN_DIR="$OR_PROJECT_DIR/Assets/ResonanceAudio/Plugins"

restore_meta() {
    local relative_path="$1"
    local source_path="$TEMPLATE_DIR/$relative_path"
    local destination_path="$PLUGIN_DIR/$relative_path"
    local plugin_path="${destination_path%.meta}"

    if [ ! -f "$source_path" ]; then
        or_fail "Missing OpenResonance plugin meta template: $source_path"
    fi

    if [ ! -e "$plugin_path" ]; then
        echo "Skipping OpenResonance plugin meta restore because plugin path is missing: $plugin_path"
        return
    fi

    mkdir -p "$(dirname "$destination_path")"
    cp "$source_path" "$destination_path"
    echo "Restored OpenResonance plugin meta: $destination_path"
}

if [ "$#" -eq 0 ]; then
    while IFS= read -r relative_path; do
        restore_meta "$relative_path"
    done < <(cd "$TEMPLATE_DIR" && find . -name "*.meta" -type f | sed 's#^\./##' | sort)
else
    for relative_path in "$@"; do
        restore_meta "$relative_path"
    done
fi
