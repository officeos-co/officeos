#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
LOG_DIR="${EAOS_LOG_DIR:-$ROOT_DIR/.runlogs}"
BACKEND_PORT="${EAOS_BACKEND_PORT:-5000}"

usage() {
  cat <<EOF
Usage: ./scripts/dev

Starts the backend in the background.
Logs and PID files are written directly under .runlogs/.
EOF
}

while [[ $# -gt 0 ]]; do
  case "$1" in
    -h|--help)
      usage
      exit 0
      ;;
    *)
      echo "Unknown option: $1" >&2
      usage >&2
      exit 1
      ;;
  esac
  shift
done

mkdir -p "$LOG_DIR"

stop_pid_file() {
  local name="$1"
  local pid_file="$2"

  if [[ ! -f "$pid_file" ]]; then
    return
  fi

  local pid
  pid="$(cat "$pid_file" 2>/dev/null || true)"

  if [[ "$pid" =~ ^[0-9]+$ ]] && kill -0 "$pid" 2>/dev/null; then
    echo "Stopping existing $name process ($pid)..."
    kill "$pid" 2>/dev/null || true

    for _ in {1..20}; do
      if ! kill -0 "$pid" 2>/dev/null; then
        return
      fi
      sleep 0.1
    done

    echo "Force stopping existing $name process ($pid)..."
    kill -9 "$pid" 2>/dev/null || true
  fi
}

free_port() {
  local port="$1"
  local pids

  pids="$(lsof -tiTCP:"$port" -sTCP:LISTEN 2>/dev/null || true)"
  if [[ -z "$pids" ]]; then
    return
  fi

  echo "Freeing port $port..."
  while IFS= read -r pid; do
    [[ -z "$pid" ]] && continue
    kill "$pid" 2>/dev/null || true
  done <<< "$pids"

  for _ in {1..20}; do
    if [[ -z "$(lsof -tiTCP:"$port" -sTCP:LISTEN 2>/dev/null || true)" ]]; then
      return
    fi
    sleep 0.1
  done

  while IFS= read -r pid; do
    [[ -z "$pid" ]] && continue
    kill -9 "$pid" 2>/dev/null || true
  done <<< "$(lsof -tiTCP:"$port" -sTCP:LISTEN 2>/dev/null || true)"
}

stop_pid_file "backend" "$LOG_DIR/backend.pid"
free_port "$BACKEND_PORT"

rm -f "$LOG_DIR"/backend.log \
  "$LOG_DIR"/backend.pid

(
  cd "$ROOT_DIR/apps/backend"
  ASPNETCORE_URLS="http://localhost:$BACKEND_PORT" dotnet run --project src/OffceOs.csproj
) > "$LOG_DIR/backend.log" 2>&1 &
echo "$!" > "$LOG_DIR/backend.pid"

cat <<EOF
Dev processes started.

Logs:
  $LOG_DIR/backend.log

Stop host processes:
  kill \$(cat $LOG_DIR/backend.pid)
EOF
