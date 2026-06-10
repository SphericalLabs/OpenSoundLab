#!/usr/bin/env bash

shopt -s nullglob

OSL_NATIVE_CORE_DIR="${OSL_NATIVE_CORE_DIR:-Core}"
OSL_NATIVE_ADDONS_DIR="${OSL_NATIVE_ADDONS_DIR:-Addons}"
OSL_NATIVE_PACKAGE_ADDONS_DIR="${OSL_NATIVE_PACKAGE_ADDONS_DIR:-../Packages/*/OSLNativeAddons~ ../Packages/*/OSLNativeAddons}"
OSL_NATIVE_ADDON_MANIFESTS=()
OSL_NATIVE_ADDON_IDS=()
OSL_NATIVE_ADDON_DIRS=()

OSL_NATIVE_INCLUDE_DIRS=(
    "$OSL_NATIVE_CORE_DIR/Abi"
    "$OSL_NATIVE_CORE_DIR/Shared/Buffers"
    "$OSL_NATIVE_CORE_DIR/Shared/Math"
    "$OSL_NATIVE_CORE_DIR/Devices/Drum"
    "$OSL_NATIVE_CORE_DIR/Devices/Envelope"
    "$OSL_NATIVE_CORE_DIR/Devices/Filter"
    "$OSL_NATIVE_CORE_DIR/Devices/FilterTwo"
    "$OSL_NATIVE_CORE_DIR/Devices/Keyboard"
    "$OSL_NATIVE_CORE_DIR/Devices/LinkPhaseGenerator"
    "$OSL_NATIVE_CORE_DIR/Devices/Maraca"
    "$OSL_NATIVE_CORE_DIR/Devices/Noise"
    "$OSL_NATIVE_CORE_DIR/Devices/Oscillator"
    "$OSL_NATIVE_CORE_DIR/Devices/ClipPlayer"
    "$OSL_NATIVE_CORE_DIR/Devices/Utilities"
    "$OSL_NATIVE_CORE_DIR/Devices/Xylophone"
    "$OSL_NATIVE_CORE_DIR/External/FreeVerb/dfx-library"
    "$OSL_NATIVE_CORE_DIR/External/FreeVerb/freeverb/components"
    "$OSL_NATIVE_CORE_DIR/External/UnityAudioPlugin"
    "$OSL_NATIVE_CORE_DIR/External/pcg-cpp/include"
    "$OSL_NATIVE_CORE_DIR/External/link/include"
    "$OSL_NATIVE_CORE_DIR/External/link/modules/asio-standalone/asio/include"
    "$OSL_NATIVE_CORE_DIR/External/AprilTag"
    "$OSL_NATIVE_CORE_DIR/External/AprilTag/common"
)

OSL_NATIVE_SOURCES=(
    "$OSL_NATIVE_CORE_DIR/Shared/Math/AudioMath.cpp"
    "$OSL_NATIVE_CORE_DIR/Shared/Buffers/CompressedRingBuffer.cpp"
    "$OSL_NATIVE_CORE_DIR/Shared/Buffers/CRingBuffer.cpp"
    "$OSL_NATIVE_CORE_DIR/Shared/Buffers/RingBuffer.cpp"
    "$OSL_NATIVE_CORE_DIR/Shared/Math/Biquad.cpp"
    "$OSL_NATIVE_CORE_DIR/Shared/Math/Resample.cpp"
    "$OSL_NATIVE_CORE_DIR/Shared/Math/Util.c"
    "$OSL_NATIVE_CORE_DIR/Devices/ClipPlayer/ClipPlayerApi.cpp"
    "$OSL_NATIVE_CORE_DIR/Devices/Drum/DrumApi.cpp"
    "$OSL_NATIVE_CORE_DIR/Devices/Envelope/EnvelopeApi.cpp"
    "$OSL_NATIVE_CORE_DIR/Devices/Envelope/EnvelopeMath.cpp"
    "$OSL_NATIVE_CORE_DIR/Devices/Filter/Filter.cpp"
    "$OSL_NATIVE_CORE_DIR/Devices/Filter/CombFilterApi.cpp"
    "$OSL_NATIVE_CORE_DIR/Devices/FilterTwo/FilterTwo.cpp"
    "$OSL_NATIVE_CORE_DIR/Devices/Keyboard/KeyboardApi.cpp"
    "$OSL_NATIVE_CORE_DIR/Devices/LinkPhaseGenerator/LinkPhaseGeneratorApi.cpp"
    "$OSL_NATIVE_CORE_DIR/Devices/Maraca/MaracaApi.cpp"
    "$OSL_NATIVE_CORE_DIR/Devices/Noise/NoiseApi.cpp"
    "$OSL_NATIVE_CORE_DIR/Devices/Oscillator/OscillatorApi.cpp"
    "$OSL_NATIVE_CORE_DIR/Devices/Oscillator/Oscillator.cpp"
    "$OSL_NATIVE_CORE_DIR/Devices/Utilities/BufferApi.cpp"
    "$OSL_NATIVE_CORE_DIR/Devices/Utilities/DiagnosticApi.cpp"
    "$OSL_NATIVE_CORE_DIR/Devices/Utilities/FaderApi.cpp"
    "$OSL_NATIVE_CORE_DIR/Devices/Utilities/GateApi.cpp"
    "$OSL_NATIVE_CORE_DIR/Devices/Utilities/MicApi.cpp"
    "$OSL_NATIVE_CORE_DIR/Devices/Utilities/AprilTagApi.cpp"
    "$OSL_NATIVE_CORE_DIR/Devices/Utilities/WaveTextureApi.cpp"
    "$OSL_NATIVE_CORE_DIR/Devices/Xylophone/XylophoneApi.cpp"
    "$OSL_NATIVE_CORE_DIR/External/AprilTag/apriltag.c"
    "$OSL_NATIVE_CORE_DIR/External/AprilTag/apriltag_pose.c"
    "$OSL_NATIVE_CORE_DIR/External/AprilTag/apriltag_quad_thresh.c"
    "$OSL_NATIVE_CORE_DIR/External/AprilTag/tagStandard41h12.c"
    "$OSL_NATIVE_CORE_DIR/External/AprilTag/common/g2d.c"
    "$OSL_NATIVE_CORE_DIR/External/AprilTag/common/homography.c"
    "$OSL_NATIVE_CORE_DIR/External/AprilTag/common/image_u8.c"
    "$OSL_NATIVE_CORE_DIR/External/AprilTag/common/image_u8_parallel.c"
    "$OSL_NATIVE_CORE_DIR/External/AprilTag/common/image_u8x3.c"
    "$OSL_NATIVE_CORE_DIR/External/AprilTag/common/matd.c"
    "$OSL_NATIVE_CORE_DIR/External/AprilTag/common/pnm.c"
    "$OSL_NATIVE_CORE_DIR/External/AprilTag/common/pthreads_cross.c"
    "$OSL_NATIVE_CORE_DIR/External/AprilTag/common/string_util.c"
    "$OSL_NATIVE_CORE_DIR/External/AprilTag/common/svd22.c"
    "$OSL_NATIVE_CORE_DIR/External/AprilTag/common/time_util.c"
    "$OSL_NATIVE_CORE_DIR/External/AprilTag/common/unionfind.c"
    "$OSL_NATIVE_CORE_DIR/External/AprilTag/common/workerpool.c"
    "$OSL_NATIVE_CORE_DIR/External/AprilTag/common/zarray.c"
    "$OSL_NATIVE_CORE_DIR/External/AprilTag/common/zhash.c"
    "$OSL_NATIVE_CORE_DIR/External/AprilTag/common/zmaxheap.c"
    "$OSL_NATIVE_CORE_DIR/External/FreeVerb/freeverb/components"/*.cpp
    "$OSL_NATIVE_CORE_DIR/External/FreeVerb/dfx-library"/*.cpp
    "$OSL_NATIVE_CORE_DIR/External/UnityAudioPlugin"/*.cpp
)

osl_native_has_addon_id() {
    local addonId="$1"
    local existingAddonId

    if [ "${#OSL_NATIVE_ADDON_IDS[@]}" -eq 0 ]; then
        return 1
    fi

    for existingAddonId in "${OSL_NATIVE_ADDON_IDS[@]}"; do
        [ "$existingAddonId" = "$addonId" ] && return 0
    done

    return 1
}

osl_native_load_addon_root() {
    local addonRoot="$1"
    local addonSources

    [ -d "$addonRoot" ] || return 0

    for addonSources in "$addonRoot"/*/addon_sources.sh; do
        [ -f "$addonSources" ] || continue
        OSL_NATIVE_ADDON_DIR="$(dirname "$addonSources")"
        OSL_NATIVE_ADDON_ID="$(basename "$OSL_NATIVE_ADDON_DIR")"
        osl_native_has_addon_id "$OSL_NATIVE_ADDON_ID" && continue
        OSL_NATIVE_ADDON_MANIFESTS+=("$addonSources")
        OSL_NATIVE_ADDON_IDS+=("$OSL_NATIVE_ADDON_ID")
        OSL_NATIVE_ADDON_DIRS+=("$OSL_NATIVE_ADDON_DIR")
        source "$addonSources"
    done
}

osl_native_load_addon_root "$OSL_NATIVE_ADDONS_DIR"

for packageAddonRoot in $OSL_NATIVE_PACKAGE_ADDONS_DIR; do
    osl_native_load_addon_root "$packageAddonRoot"
done

osl_native_is_addon_source() {
    local sourcePath="$1"
    local addonDir

    if [ "${#OSL_NATIVE_ADDON_DIRS[@]}" -eq 0 ]; then
        return 1
    fi

    for addonDir in "${OSL_NATIVE_ADDON_DIRS[@]}"; do
        case "$sourcePath" in
            "$addonDir"/*)
                return 0
                ;;
        esac
    done

    return 1
}

osl_native_addon_source_count() {
    local sourcePath
    local sourceCount=0

    for sourcePath in "${OSL_NATIVE_SOURCES[@]}"; do
        if osl_native_is_addon_source "$sourcePath"; then
            sourceCount=$((sourceCount + 1))
        fi
    done

    printf "%s" "$sourceCount"
}

osl_native_print_source_summary() {
    local addonNames="none"

    if [ "${#OSL_NATIVE_ADDON_IDS[@]}" -gt 0 ]; then
        addonNames="${OSL_NATIVE_ADDON_IDS[*]}"
    fi

    echo "OSLNative native addons: $addonNames"
    echo "OSLNative source files: ${#OSL_NATIVE_SOURCES[@]} total, $(osl_native_addon_source_count) from addons"
}
