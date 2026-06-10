#!/usr/bin/env bash

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
source "$SCRIPT_DIR/common.sh"

SOURCE_ASSETS="$OR_SUBMODULE_DIR/platforms/unity/UnityIntegration/Assets/ResonanceAudio"
DEST_ASSETS="$OR_PROJECT_DIR/Assets/ResonanceAudio"
EXPECTED_DEST_ASSETS="$OR_PROJECT_DIR/Assets/ResonanceAudio"

or_require_submodule
or_need_command rsync

if [ ! -d "$SOURCE_ASSETS" ]; then
    or_fail "OpenResonance Unity assets are missing: $SOURCE_ASSETS"
fi
if [ "$DEST_ASSETS" != "$EXPECTED_DEST_ASSETS" ]; then
    or_fail "Refusing to install OpenResonance assets to unexpected path: $DEST_ASSETS"
fi
case "$DEST_ASSETS" in
    "$OR_PROJECT_DIR"/Assets/ResonanceAudio) ;;
    *)
        or_fail "Refusing to run rsync --delete outside the OpenSoundLab ResonanceAudio asset folder: $DEST_ASSETS"
        ;;
esac
if [ "$DEST_ASSETS" = "/" ] || [ "$DEST_ASSETS" = "$OR_PROJECT_DIR" ] || [ "$DEST_ASSETS" = "$OR_PROJECT_DIR/Assets" ]; then
    or_fail "Refusing unsafe OpenResonance install destination: $DEST_ASSETS"
fi

or_log "Installing OpenResonance Unity assets without demos"
mkdir -p "$DEST_ASSETS"

if [ -f "$SOURCE_ASSETS.meta" ]; then
    cp "$SOURCE_ASSETS.meta" "$DEST_ASSETS.meta"
fi

rsync -a --delete \
    --exclude "Demos/" \
    --exclude "Demos.meta" \
    --exclude "Resources/ResonanceAudioMixer.mixer" \
    --exclude "Resources/ResonanceAudioMixer.mixer.meta" \
    "$SOURCE_ASSETS/" \
    "$DEST_ASSETS/"

if [ -d "$DEST_ASSETS/Demos" ]; then
    rm -rf "$DEST_ASSETS/Demos"
fi
if [ -f "$DEST_ASSETS/Demos.meta" ]; then
    rm -f "$DEST_ASSETS/Demos.meta"
fi

if [ -d "$OR_STAGING_ROOT" ]; then
    or_log "Installing staged OpenResonance native plugins"
    if [ "$#" -gt 0 ]; then
        for target_name in "$@"; do
            plugins_dir="$OR_STAGING_ROOT/$target_name/unity/Assets/ResonanceAudio/Plugins"
            if [ -d "$plugins_dir" ]; then
                rsync -a "$plugins_dir/" "$DEST_ASSETS/Plugins/"
            else
                echo "Skipping OpenResonance staged plugins because target is missing: $target_name"
            fi
        done
    else
        while IFS= read -r -d '' plugins_dir; do
            rsync -a "$plugins_dir/" "$DEST_ASSETS/Plugins/"
        done < <(find "$OR_STAGING_ROOT" -path "*/unity/Assets/ResonanceAudio/Plugins" -type d -print0)
    fi
fi

"$OR_BUILD_DIR/restore_plugin_meta_templates.sh"

echo "Installed OpenResonance Unity assets into $DEST_ASSETS"
