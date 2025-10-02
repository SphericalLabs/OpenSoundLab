#!/bin/zsh
set -euo pipefail

# Defaults
PORT=5555
BITRATE="16M"
CROP="2064:2208:0:0"
ANGLE="19"
MODE="wait-unplug"   # default; override with --immediate
STEP_DELAY=0.5

usage() {
  cat <<'EOF'
Usage: init_wifi_scrcpy.zsh [--immediate | --wait-unplug] [--port N] [--bitrate Bps] [--crop WxH:x:y] [--angle deg] [--] [extra scrcpy args]

Examples:
  ./init_wifi_scrcpy.zsh
  ./init_wifi_scrcpy.zsh --immediate --port 5555 --bitrate 12M --crop 2064:2208:0:0 -- --turn-screen-off

Notes:
  --immediate   Start scrcpy over TCP/IP right away (after enabling TCP).
  --wait-unplug Enable TCP/IP, ask you to unplug USB, then connect + launch scrcpy. (default)
EOF
}

# Parse args
typeset -a EXTRA_ARGS
EXTRA_ARGS=()
while [[ $# -gt 0 ]]; do
  case "$1" in
    --immediate) MODE="immediate"; shift ;;
    --wait-unplug) MODE="wait-unplug"; shift ;;
    --port) PORT="$2"; shift 2 ;;
    --bitrate) BITRATE="$2"; shift 2 ;;
    --crop) CROP="$2"; shift 2 ;;
    --angle) ANGLE="$2"; shift 2 ;;
    --help|-h) usage; exit 0 ;;
    --) shift; EXTRA_ARGS=("$@"); break ;;
    *) EXTRA_ARGS+=("$1"); shift ;;
  esac
done

log(){ printf '[%s] %s\n' "$(date +%H:%M:%S)" "$*"; }

pause_between_steps(){ sleep "$STEP_DELAY"; }

get_usb_serial() {
  adb devices | awk 'NR>1 && $2=="device" && $1 !~ /:/{print $1; exit}'
}

get_single_tcp_serial() {
  typeset -a serials
  local IFS=$'\n'
  serials=($(adb devices | awk 'NR>1 && $2=="device" && $1 ~ /:/{print $1}' || true))
  if (( ${#serials} == 1 )); then
    printf '%s\n' "${serials[1]}"
    return 0
  fi
  return 1
}

list_usb_serials() {
  adb devices | awk 'NR>1 && $2=="device" && $1 !~ /:/{print $1}'
}

list_tcp_serials() {
  adb devices | awk 'NR>1 && $2=="device" && $1 ~ /:/{print $1}'
}

get_device_ip() {
  local serial="${1:-}"
  local ip
  typeset -a adb_cmd
  adb_cmd=(adb)
  if [[ -n "${serial}" ]]; then adb_cmd+=(-s "$serial"); fi

  # Prefer explicit Wi-Fi interfaces when available
  for iface in wlan0 wlan1 wifi0; do
    ip=$({ "${adb_cmd[@]}" shell "ip -o -4 addr show $iface" 2>/dev/null \
      | tr -d '\r' \
      | awk '{print $4}' \
      | cut -d/ -f1 \
      | grep -E '^(10\.|172\.(1[6-9]|2[0-9]|3[0-1])\.|192\.168\.)' \
      | head -n1 || true; })
    [[ -n "${ip:-}" ]] && echo "$ip" && return 0
  done

  # Fallback: first non-loopback IPv4 address on an active interface
  ip=$({ "${adb_cmd[@]}" shell "ip -o -4 addr show up" 2>/dev/null \
    | tr -d '\r' \
    | awk '!/ lo /{print $4}' \
    | cut -d/ -f1 \
    | grep -E '^(10\.|172\.(1[6-9]|2[0-9]|3[0-1])\.|192\.168\.)' \
    | head -n1 || true; })
  [[ -n "${ip:-}" ]] && echo "$ip" && return 0

  # Final fallback: dhcp property exposed by some Android builds
  for prop in dhcp.wlan0.ipaddress dhcp.wlan1.ipaddress dhcp.wifi.ipaddress; do
    ip=$({ "${adb_cmd[@]}" shell getprop "$prop" 2>/dev/null | tr -d '\r' || true; })
    if [[ "${ip:-}" =~ ^(10\.|172\.(1[6-9]|2[0-9]|3[0-1])\.|192\.168\.) ]]; then
      echo "$ip"
      return 0
    fi
  done

  return 1
}

usb_serial_attached() {
  local serial="$1"
  adb devices | awk -v target="$serial" 'NR>1 && $1==target {print $0; exit}'
}

wait_for_usb_detach() {
  local serial="$1"
  local tries=120
  local missing_streak=0
  while (( tries-- > 0 )); do
    if [[ -z "$(usb_serial_attached "$serial" || true)" ]]; then
      (( missing_streak++ ))
      if (( missing_streak >= 4 )); then
        return 0
      fi
    else
      missing_streak=0
    fi
    sleep 0.5
  done
  return 1
}

ensure_only_tcp_active() {
  local target="$1"
  adb disconnect >/dev/null 2>&1 || true
  pause_between_steps
  adb connect "$target" >/dev/null
  pause_between_steps
}

maybe_reset_adb_state() {
  typeset -a usb_serials tcp_serials
  local IFS=$'\n'
  usb_serials=($(list_usb_serials || true))
  tcp_serials=($(list_tcp_serials || true))
  if (( ${#usb_serials} == 1 && ${#tcp_serials} == 1 )); then
    log "Detected one USB and one TCP/IP device; resetting adb daemon"
    adb kill-server >/dev/null 2>&1 || true
    pause_between_steps
  fi
}

launch_scrcpy() {
  local serial="$1"
  typeset -a args
  args=(-s "$serial" -b "$BITRATE" "--crop=$CROP")
  [[ -n "$ANGLE" ]] && args+=("--angle=$ANGLE")
  if [[ ${#EXTRA_ARGS[@]} -gt 0 ]]; then args+=("${EXTRA_ARGS[@]}"); fi
  log "Starting scrcpy on $serial"
  exec scrcpy "${args[@]}"
}

main() {
  maybe_reset_adb_state

  # Prefer already-connected TCP device
  local tcp usb ip target
  if tcp="$(get_single_tcp_serial || true)"; [[ -n "${tcp:-}" ]]; then
    log "Reusing existing TCP/IP device: $tcp"
    pause_between_steps
    ensure_only_tcp_active "$tcp"
    launch_scrcpy "$tcp"
  fi

  # Else fall back to USB
  usb="$(get_usb_serial || true)"
  if [[ -z "${usb:-}" ]]; then
    log "No USB device found and no reusable TCP/IP connection. Plug in via USB or ensure Wireless debugging is paired."
    exit 1
  fi
  log "Found USB device: $usb"
  pause_between_steps

  ip=$(get_device_ip "$usb" || true)
  if [[ -z "${ip:-}" ]]; then
    log "Could not determine device IP. Ensure Wi-Fi is on the same LAN."
    exit 1
  fi
  target="${ip}:${PORT}"
  log "Device LAN IP: $ip (target $target)"
  pause_between_steps

  log "Enabling TCP/IP on port $PORT"
  adb -s "$usb" tcpip "$PORT" >/dev/null
  pause_between_steps

  if [[ "$MODE" == "immediate" ]]; then
    log "Connecting over TCP/IP immediately"
    pause_between_steps
    ensure_only_tcp_active "$target"
    launch_scrcpy "$target"
  else
    log "Please unplug the USB cable when ready…"
    log "Press Enter once the cable is unplugged (Ctrl+C to abort)."
    read -r
    pause_between_steps
    if ! wait_for_usb_detach "$usb"; then
      log "Timed out waiting for USB to be unplugged."
      exit 1
    fi
    log "USB detached. Connecting to $target"
    pause_between_steps
    local connected=0
    for i in {1..20}; do
      if adb connect "$target" >/dev/null 2>&1; then
        log "Connected to $target"
        pause_between_steps
        connected=1
        break
      fi
      sleep 0.5
    done
    if (( ! connected )); then
      log "Failed to connect to $target over TCP/IP."
      exit 1
    fi
    ensure_only_tcp_active "$target"
    pause_between_steps
    launch_scrcpy "$target"
  fi
}

# Basic sanity checks for macOS users
command -v adb >/dev/null || { echo "adb not found. Install Android Platform Tools: brew install android-platform-tools"; exit 1; }
command -v scrcpy >/dev/null || { echo "scrcpy not found. Install via Homebrew: brew install scrcpy"; exit 1; }

main
