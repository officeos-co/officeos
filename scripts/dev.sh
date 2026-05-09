#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
LOG_DIR="${EAOS_LOG_DIR:-$ROOT_DIR/.runlogs}"
BUILD_POD_EXECUTOR=false

usage() {
  cat <<EOF
Usage: ./scripts/dev [--build-pod-executor]

Starts Docker infrastructure, backend, and dashboard in the background.
Logs and PID files are written directly under .runlogs/.
EOF
}

while [[ $# -gt 0 ]]; do
  case "$1" in
    --build-pod-executor)
      BUILD_POD_EXECUTOR=true
      ;;
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
rm -f "$LOG_DIR"/backend.log \
  "$LOG_DIR"/dashboard.log \
  "$LOG_DIR"/pod-executor-build.log \
  "$LOG_DIR"/backend.pid \
  "$LOG_DIR"/dashboard.pid

echo "Starting infrastructure..."
docker compose -f "$ROOT_DIR/docker-compose.infra.yml" up -d

if [[ "$BUILD_POD_EXECUTOR" == true ]]; then
  echo "Building pod executor image..."
  docker build -t harkro123/eaos-pod-executor:latest "$ROOT_DIR/packages/pod-executor" \
    2>&1 | tee "$LOG_DIR/pod-executor-build.log"
fi

(
  cd "$ROOT_DIR/apps/backend"
  dotnet run --project src/EnterpriseAgentOs.Api
) > "$LOG_DIR/backend.log" 2>&1 &
echo "$!" > "$LOG_DIR/backend.pid"

(
  cd "$ROOT_DIR/apps/dashboard"
  bun dev
) > "$LOG_DIR/dashboard.log" 2>&1 &
echo "$!" > "$LOG_DIR/dashboard.pid"

cat <<EOF
Dev processes started.

Logs:
  $LOG_DIR/backend.log
  $LOG_DIR/dashboard.log

Stop host processes:
  kill \$(cat $LOG_DIR/backend.pid) \$(cat $LOG_DIR/dashboard.pid)

Infrastructure logs:
  docker compose -f docker-compose.infra.yml logs -f
EOF
