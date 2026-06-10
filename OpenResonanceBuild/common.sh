#!/usr/bin/env bash

set -euo pipefail

OR_BUILD_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
OR_PROJECT_DIR="$(cd "$OR_BUILD_DIR/.." && pwd)"
OR_SUBMODULE_DIR="$OR_PROJECT_DIR/OpenResonance"
OR_BUILD_ROOT="${OPEN_RESONANCE_BUILD_ROOT:-$OR_BUILD_DIR/Build}"
OR_STAGING_ROOT="${OPEN_RESONANCE_STAGING_ROOT:-$OR_BUILD_DIR/Staging}"
OR_CMAKE_GENERATOR="${OPEN_RESONANCE_CMAKE_GENERATOR:-Ninja}"
OR_PROFILE="${OPEN_RESONANCE_PROFILE:-Release}"
OR_CMAKE_POLICY_VERSION_MINIMUM="${OPEN_RESONANCE_CMAKE_POLICY_VERSION_MINIMUM:-3.5}"

or_log() {
    printf "\n==> %s\n" "$*"
}

or_fail() {
    echo "Error: $*" >&2
    exit 1
}

or_need_command() {
    command -v "$1" >/dev/null 2>&1 || or_fail "$1 could not be found."
}

or_require_submodule() {
    if [ ! -f "$OR_SUBMODULE_DIR/CMakeLists.txt" ]; then
        or_fail "OpenResonance submodule is missing. Run: git submodule update --init --recursive OpenResonance"
    fi
}

or_need_cmake() {
    or_need_command cmake
    if [ "$OR_CMAKE_GENERATOR" = "Ninja" ]; then
        or_need_command ninja
    fi
}

or_host_arch() {
    uname -m
}

or_unity_editor_version() {
    awk '/m_EditorVersion:/ { print $2; exit }' "$OR_PROJECT_DIR/ProjectSettings/ProjectVersion.txt"
}

or_find_android_ndk() {
    if [ -n "${OPEN_RESONANCE_ANDROID_NDK:-}" ]; then
        echo "$OPEN_RESONANCE_ANDROID_NDK"
        return
    fi
    if [ -n "${ANDROID_NDK:-}" ]; then
        echo "$ANDROID_NDK"
        return
    fi
    if [ -n "${ANDROID_NDK_HOME:-}" ]; then
        echo "$ANDROID_NDK_HOME"
        return
    fi

    local unity_version
    unity_version="$(or_unity_editor_version)"
    local candidates=(
        "/Applications/Unity/Hub/Editor/$unity_version/PlaybackEngines/AndroidPlayer/NDK"
        "$HOME/Applications/Unity/Hub/Editor/$unity_version/PlaybackEngines/AndroidPlayer/NDK"
        "$HOME/Library/Unity/Hub/Editor/$unity_version/PlaybackEngines/AndroidPlayer/NDK"
    )

    local candidate
    for candidate in "${candidates[@]}"; do
        if [ -f "$candidate/build/cmake/android.toolchain.cmake" ]; then
            echo "$candidate"
            return
        fi
    done

    return 1
}

or_require_ios_sdk() {
    if ! xcodebuild -version >/dev/null 2>&1; then
        local active_developer_dir
        active_developer_dir="$(xcode-select -p 2>/dev/null || true)"
        or_fail "iOS builds require full Xcode with the iphoneos SDK. Active developer directory is '${active_developer_dir:-unknown}', which does not provide xcodebuild. Install Xcode and run: sudo xcode-select -s /Applications/Xcode.app/Contents/Developer"
    fi

    local iphoneos_sdk
    iphoneos_sdk="$(xcrun --sdk iphoneos --show-sdk-path 2>/dev/null || true)"
    if [ -z "$iphoneos_sdk" ] || [ ! -d "$iphoneos_sdk" ]; then
        local active_developer_dir
        active_developer_dir="$(xcode-select -p 2>/dev/null || true)"
        or_fail "iOS builds require the iphoneos SDK, but xcrun cannot find it. Active developer directory is '${active_developer_dir:-unknown}'. Install/select full Xcode with: sudo xcode-select -s /Applications/Xcode.app/Contents/Developer"
    fi
}

or_clone_core_deps() {
    or_require_submodule
    or_need_command git
    or_log "Preparing OpenResonance core dependencies"
    "$OR_SUBMODULE_DIR/third_party/clone_core_deps.sh"
}

or_clone_unity_deps() {
    or_require_submodule
    or_need_command git
    or_need_command cmake
    or_log "Preparing OpenResonance Unity dependencies"

    local build_embree=true
    local arg
    for arg in "$@"; do
        if [ "$arg" = "--skip_embree" ]; then
            build_embree=false
        fi
    done

    if [ "$build_embree" = "true" ]; then
        or_prepare_embree_checkout
        or_patch_embree_for_current_tools
    fi

    local real_cmake
    real_cmake="$(command -v cmake)"
    local wrapper_dir="$OR_BUILD_ROOT/cmake-policy-wrapper"
    mkdir -p "$wrapper_dir"

    cat > "$wrapper_dir/cmake" <<EOF
#!/usr/bin/env bash
set -euo pipefail

REAL_CMAKE="$real_cmake"
POLICY_VERSION="$OR_CMAKE_POLICY_VERSION_MINIMUM"

for arg in "\$@"; do
    case "\$arg" in
        --build|--install|-E)
            exec "\$REAL_CMAKE" "\$@"
            ;;
    esac
done

exec "\$REAL_CMAKE" \
    -DCMAKE_POLICY_VERSION_MINIMUM="\$POLICY_VERSION" \
    -DCMAKE_POLICY_DEFAULT_CMP0074=NEW \
    -DBUILD_TESTING:BOOL=OFF \
    "\$@"
EOF
    chmod +x "$wrapper_dir/cmake"

    PATH="$wrapper_dir:$PATH" "$OR_SUBMODULE_DIR/third_party/clone_build_install_unity_deps.sh" "$@"
}

or_prepare_embree_checkout() {
    local third_party_dir="$OR_SUBMODULE_DIR/third_party"
    local embree_dir="$third_party_dir/embree"

    if [ -d "$embree_dir" ]; then
        return
    fi

    or_log "Cloning Embree v2.16.5 before applying OpenSoundLab compatibility patch"
    git clone -b "v2.16.5" "https://github.com/embree/embree.git" "$embree_dir"
    (
        cd "$embree_dir"
        git checkout "v2.16.5"
        patch -p1 < "$third_party_dir/patches/libembree.patch"
    )
}

or_patch_embree_for_current_tools() {
    local cmake_file="$OR_SUBMODULE_DIR/third_party/embree/CMakeLists.txt"

    if [ ! -f "$cmake_file" ]; then
        or_fail "Embree CMakeLists.txt is missing: $cmake_file"
    fi

    if grep -Fq 'cmake_policy(SET CMP0042 OLD)' "$cmake_file"; then
        perl -0pi -e 's/cmake_policy\(SET CMP0042 OLD\)/cmake_policy(SET CMP0042 NEW)/g' "$cmake_file"
    fi

    if grep -Fq '${CMAKE_CXX_COMPILER_ID} STREQUAL "Clang")' "$cmake_file" &&
       ! grep -Fq '${CMAKE_CXX_COMPILER_ID} STREQUAL "AppleClang")' "$cmake_file"; then
        perl -0pi -e 's/\$\{CMAKE_CXX_COMPILER_ID\} STREQUAL "Clang"\)/\${CMAKE_CXX_COMPILER_ID} STREQUAL "Clang" OR \${CMAKE_CXX_COMPILER_ID} STREQUAL "AppleClang")/g' "$cmake_file"
    fi
}

or_cmake_configure_and_install() {
    local target_name="$1"
    shift

    or_require_submodule
    or_need_cmake

    local build_dir="$OR_BUILD_ROOT/$target_name"
    local install_dir="$OR_STAGING_ROOT/$target_name"

    if [ "${OPEN_RESONANCE_CLEAN_BUILD:-1}" = "1" ]; then
        rm -rf "$build_dir" "$install_dir"
    fi

    mkdir -p "$build_dir" "$install_dir"

    or_log "Configuring OpenResonance Unity plugin: $target_name"
    cmake \
        -S "$OR_SUBMODULE_DIR" \
        -B "$build_dir" \
        -G "$OR_CMAKE_GENERATOR" \
        -DCMAKE_BUILD_TYPE="$OR_PROFILE" \
        -DBUILD_UNITY_PLUGIN:BOOL=ON \
        -DINSTALL_DIR="$install_dir" \
        "$@"

    or_log "Building OpenResonance Unity plugin: $target_name"
    cmake --build "$build_dir" --config "$OR_PROFILE" --target install
}

or_maybe_install_unity_assets() {
    if [ "${OPEN_RESONANCE_SKIP_INSTALL:-0}" = "1" ]; then
        return
    fi

    "$OR_BUILD_DIR/install_unity_assets.sh" "$@"
}
