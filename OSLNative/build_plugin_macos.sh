#!/usr/bin/env bash

# Re-exec under bash if invoked via zsh/sh
if [ -z "${BASH_VERSION:-}" ]; then
  echo "Re-running under bash…"
  exec /usr/bin/env bash "$0" "$@"
fi

set -euo pipefail

# --- Config ---------------------------------------------------------------

REQUIRED_NDK_VERSION="26.1.10909125"   # Android NDK r26b
ANDROID_HOME="${ANDROID_HOME:-${HOME}/Library/Android/sdk}"
ZSHRC="${HOME}/.zshrc"

# Script + build locations
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
BUILD_DIR="${SCRIPT_DIR}"
ANDROID_MK="${BUILD_DIR}/Android.mk"
APP_MK="${BUILD_DIR}/Application.mk"

# Output/destination paths (relative to BUILD_DIR)
OUTPUT_SO_REL="libs/arm64-v8a/libOSLNative.so"
DEST_SO_REL="../Assets/OSLNative/arm64/Release/libOSLNative.so"

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

  log "Updating Homebrew..."
  "${brewbin}" update

  log "Ensuring Temurin 17 JDK and Android command-line tools..."
  "${brewbin}" install --cask temurin@17 || true
  "${brewbin}" install android-commandlinetools || true

  # Set JAVA_HOME for this script run if available
  if /usr/libexec/java_home -v 17 >/dev/null 2>&1; then
    export JAVA_HOME="$("/usr/libexec/java_home" -v 17)"
  fi
}

ensure_cmdline_tools_in_sdk() {
  log "Installing cmdline-tools;latest into \$ANDROID_HOME via --sdk_root…"
  local S
  if [ -x "$ANDROID_HOME/cmdline-tools/latest/bin/sdkmanager" ]; then
    S="$ANDROID_HOME/cmdline-tools/latest/bin/sdkmanager"
  elif command -v sdkmanager >/dev/null 2>&1; then
    S="$(command -v sdkmanager)"
  else
    echo "sdkmanager not found on PATH or under \$ANDROID_HOME"; exit 1
  fi

  # Ensure repos config exists (prevents first-run oddities)
  mkdir -p "$HOME/.android"
  : > "$HOME/.android/repositories.cfg"

  # Show what we’re using and its version
  "$S" --version || { echo "sdkmanager not runnable"; exit 1; }

  # Install cmdline-tools;latest with visible output and a retry
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

  log "Installing platform-tools, a platform, CMake, and NDK ${REQUIRED_NDK_VERSION}…"
  "$S" --sdk_root="$ANDROID_HOME" --install \
    "platform-tools" \
    "platforms;android-34" \
    "cmake;3.22.1" \
    "ndk;${REQUIRED_NDK_VERSION}"

  yes | "$S" --sdk_root="$ANDROID_HOME" --licenses || true

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
