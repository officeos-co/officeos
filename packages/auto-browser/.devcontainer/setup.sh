#!/usr/bin/env bash
set -euo pipefail

echo "Installing Python dev dependencies..."
pip install --quiet -e ./controller[dev]

echo "Pulling Docker images..."
docker compose pull --quiet 2>/dev/null || true

echo "Setup complete. auto-browser will start automatically."
