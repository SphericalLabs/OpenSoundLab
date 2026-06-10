#!/usr/bin/env bash

# Re-exec under bash if invoked via zsh/sh
if [ -z "${BASH_VERSION:-}" ]; then
  echo "Re-running under bash…"
  exec /usr/bin/env bash "$0" "$@"
fi

set -euo pipefail

# --- Config ---------------------------------------------------------------

REQUIRED_NDK_VERSION="26.1.10909125"   # Android NDK r26b
REQUIRED_CMAKE_VERSION="3.22.1"
REQUIRED_PLATFORM="android-34"
ANDROID_HOME="${ANDROID_HOME:-${HOME}/Library/Android/sdk}"
ZSHRC="${HOME}/.zshrc"

# Script + build locations
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
BUILD_DIR="${SCRIPT_DIR}"
ANDROID_MK="${BUILD_DIR}/Android.mk"
APP_MK="${BUILD_DIR}/Application.mk"

# Output/destination paths (relative to BUILD_DIR)
OUTPUT_SO_REL="libs/arm64-v8a/libOSLNative.so"
DEST_SO_REL="../Assets/Plugins/OSLNative/arm64/Release/libOSLNative.so"

# --- Helpers --------------------------------------------------------------

log() { printf "\n==> %s\n" "$*"; }

need_brew() {
  if [ -x "/opt/homebrew/bin/brew" ]; then echo "/opt/homebrew/bin/brew"; return
  fi
  if [ -x "/usr/local/bin/brew" ]; then echo "/usr/local/bin/brew"; return
  fi
  echo ""
}

append_once() {
  local line="$1" file="$2"
  grep -Fqx "$line" "$file" 2>/dev/null || printf "%s\n" "$line" >>"$file"
}

brew_has_cask() {
  local brewbin="$1" cask="$2"
  "${brewbin}" list --cask "$cask" >/dev/null 2>&1
}

jdk17_installed() {
  /usr/libexec/java_home -v 17 >/dev/null 2>&1
}

sdkmanager_path() {
  if [ -x "$ANDROID_HOME/cmdline-tools/latest/bin/sdkmanager" ]; then
    echo "$ANDROID_HOME/cmdline-tools/latest/bin/sdkmanager"
    return 0
  fi
  if [ -x "/opt/homebrew/share/android-commandlinetools/cmdline-tools/latest/bin/sdkmanager" ]; then
    echo "/opt/homebrew/share/android-commandlinetools/cmdline-tools/latest/bin/sdkmanager"
    return 0
  fi
  if [ -x "/usr/local/share/android-commandlinetools/cmdline-tools/latest/bin/sdkmanager" ]; then
    echo "/usr/local/share/android-commandlinetools/cmdline-tools/latest/bin/sdkmanager"
    return 0
  fi
  if command -v sdkmanager >/dev/null 2>&1; then
    command -v sdkmanager
    return 0
  fi
  return 1
}

have_android_cmdline_tools() {
  [ -x "$ANDROID_HOME/cmdline-tools/latest/bin/sdkmanager" ]
}

have_android_platform_tools() {
  [ -x "$ANDROID_HOME/platform-tools/adb" ]
}

have_android_platform() {
  [ -d "$ANDROID_HOME/platforms/${REQUIRED_PLATFORM}" ]
}

have_android_cmake() {
  [ -x "$ANDROID_HOME/cmake/${REQUIRED_CMAKE_VERSION}/bin/cmake" ]
}

have_required_ndk() {
  [ -x "$ANDROID_HOME/ndk/${REQUIRED_NDK_VERSION}/ndk-build" ]
}

ensure_env() {
  mkdir -p "${ANDROID_HOME}"

  # Ensure ANDROID_HOME in shell profile for future sessions
  append_once "export ANDROID_HOME=\"${ANDROID_HOME}\"" "${ZSHRC}"
  append_once "export PATH=\"\$PATH:\$ANDROID_HOME/platform-tools:\$ANDROID_HOME/cmdline-tools/latest/bin\"" "${ZSHRC}"

  # Prefer JDK 17 (Temurin) on PATH for Gradle/NDK toolchains that expect it
  if /usr/libexec/java_home -v 17 >/dev/null 2>&1; then
    local jhome; jhome="$("/usr/libexec/java_home" -v 17)"
    append_once "export JAVA_HOME=\"${jhome}\"" "${ZSHRC}"
  fi
}

install_prereqs() {
  local brewbin; brewbin="$(need_brew)"
  if [ -z "${brewbin}" ]; then
    echo "Homebrew not found. Install it from https://brew.sh and re-run."
    exit 1
  fi

  local needs_brew_work=0
  if ! jdk17_installed && ! brew_has_cask "${brewbin}" "temurin@17"; then
    needs_brew_work=1
  fi
  if ! sdkmanager_path >/dev/null 2>&1 && ! brew_has_cask "${brewbin}" "android-commandlinetools"; then
    needs_brew_work=1
  fi

  if [ "${needs_brew_work}" -eq 1 ]; then
    log "Bootstrapping Homebrew prerequisites..."
    "${brewbin}" update

    if ! jdk17_installed; then
      if brew_has_cask "${brewbin}" "temurin@17"; then
        log "Temurin 17 already installed via Homebrew."
      else
        log "Installing Temurin 17 JDK..."
        "${brewbin}" install --cask temurin@17
      fi
    else
      log "JDK 17 already available."
    fi

    if sdkmanager_path >/dev/null 2>&1; then
      log "Android command-line tools already available."
    elif brew_has_cask "${brewbin}" "android-commandlinetools"; then
      log "android-commandlinetools already installed."
    else
      log "Installing android-commandlinetools..."
      "${brewbin}" install --cask android-commandlinetools
    fi
  else
    log "Homebrew prerequisites already present; skipping brew update/install."
  fi

  # Set JAVA_HOME for this script run if available
  if jdk17_installed; then
    export JAVA_HOME="$("/usr/libexec/java_home" -v 17)"
  fi
}

ensure_cmdline_tools_in_sdk() {
  local S
  if ! S="$(sdkmanager_path)"; then
    echo "sdkmanager not found on PATH or under \$ANDROID_HOME"; exit 1
  fi

  # Ensure repos config exists (prevents first-run oddities)
  mkdir -p "$HOME/.android"
  if [ ! -f "$HOME/.android/repositories.cfg" ]; then
    : > "$HOME/.android/repositories.cfg"
  fi

  # Show what we’re using and its version
  "$S" --version || { echo "sdkmanager not runnable"; exit 1; }

  if have_android_cmdline_tools; then
    log "cmdline-tools;latest already present in \$ANDROID_HOME."
    return
  fi

  log "Installing cmdline-tools;latest into \$ANDROID_HOME via --sdk_root…"
  if ! "$S" --sdk_root="$ANDROID_HOME" --install "cmdline-tools;latest"; then
    echo "cmdline-tools install failed — retrying once…"
    sleep 2
    "$S" --sdk_root="$ANDROID_HOME" --install "cmdline-tools;latest"
  fi

  log "Accepting licences…"
  yes | "$S" --sdk_root="$ANDROID_HOME" --licenses || true
}


install_android_packages() {
  local S="$ANDROID_HOME/cmdline-tools/latest/bin/sdkmanager"
  [ -x "$S" ] || { echo "Expected $S after cmdline-tools install"; exit 1; }

  local packages=()

  if ! have_android_platform_tools; then
    packages+=("platform-tools")
  fi
  if ! have_android_platform; then
    packages+=("platforms;${REQUIRED_PLATFORM}")
  fi
  if ! have_android_cmake; then
    packages+=("cmake;${REQUIRED_CMAKE_VERSION}")
  fi
  if ! have_required_ndk; then
    packages+=("ndk;${REQUIRED_NDK_VERSION}")
  fi

  if [ "${#packages[@]}" -gt 0 ]; then
    log "Installing missing Android SDK packages..."
    "$S" --sdk_root="$ANDROID_HOME" --install "${packages[@]}"
    yes | "$S" --sdk_root="$ANDROID_HOME" --licenses || true
  else
    log "Android SDK packages already match required versions; skipping sdkmanager installs."
  fi

  log "NDK folders under $ANDROID_HOME/ndk:"
  ls -1 "$ANDROID_HOME/ndk" || true
}


pick_ndk_build() {
  local ndk_dir="${ANDROID_HOME}/ndk/${REQUIRED_NDK_VERSION}"
  local candidate="${ndk_dir}/ndk-build"
  if [ -x "${candidate}" ]; then echo "${candidate}"; return 0; fi
  if command -v ndk-build >/dev/null 2>&1; then command -v ndk-build; return 0; fi
  echo ""
}

# --- Build (Option B: -C plus NDK_PROJECT_PATH) ---------------------------

build_with_ndk() {
  local ndk_build; ndk_build="$(pick_ndk_build)"
  if [ -z "${ndk_build}" ]; then
    echo "ndk-build not found (expected at ${ANDROID_HOME}/ndk/${REQUIRED_NDK_VERSION}/ndk-build)."
    exit 1
  fi

  log "Verifying project files…"
  if [ ! -f "${ANDROID_MK}" ]; then
    echo "Missing ${ANDROID_MK}"
    exit 1
  fi
  if [ ! -f "${APP_MK}" ]; then
    echo "Missing ${APP_MK}"
    exit 1
  fi

  log "Compiling native code with: ${ndk_build}"
  # Option B: use -C and also set NDK_PROJECT_PATH for r26 reliability
  NDK_PROJECT_PATH="${BUILD_DIR}" \
  "${ndk_build}" -C "${BUILD_DIR}" \
    APP_BUILD_SCRIPT="${ANDROID_MK}" \
    NDK_APPLICATION_MK="${APP_MK}" \
    "$@"

  # Move .so to destination
  local OUTPUT_SO="${BUILD_DIR}/${OUTPUT_SO_REL}"
  local DEST_SO="${BUILD_DIR}/${DEST_SO_REL}"
  if [ -f "${OUTPUT_SO}" ]; then
    mkdir -p "$(dirname "${DEST_SO}")"
    mv "${OUTPUT_SO}" "${DEST_SO}"
    log "Moved $(basename "${OUTPUT_SO}") -> ${DEST_SO}"
    "${SCRIPT_DIR}/Build/restore_plugin_meta_templates.sh" "arm64/Release/libOSLNative.so.meta"
  else
    echo "Build finished but ${OUTPUT_SO} was not found. Check ndk-build output above."
    exit 1
  fi

  # Optional: clean intermediates for a tidy workspace
  rm -rf "${BUILD_DIR}/obj" || true
}

# --- Main -----------------------------------------------------------------

main() {
  log "Android macOS bootstrap + build (r26b) starting…"
  ensure_env
  install_prereqs
  ensure_cmdline_tools_in_sdk
  install_android_packages
  build_with_ndk "$@"
  log "Done. Open a new terminal so the PATH/JAVA_HOME set in ${ZSHRC} apply everywhere."
}

main "$@"
