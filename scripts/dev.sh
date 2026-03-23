#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

cleanup() {
  if [[ -n "${FRONTEND_PID:-}" ]]; then
    kill "$FRONTEND_PID" 2>/dev/null || true
  fi
  if [[ -n "${BACKEND_PID:-}" ]]; then
    kill "$BACKEND_PID" 2>/dev/null || true
  fi
}

trap cleanup EXIT INT TERM

cd "$ROOT_DIR/frontend"
npm start &
FRONTEND_PID=$!

cd "$ROOT_DIR"
./.dotnet/dotnet run --project backend/FoodHelper.Api &
BACKEND_PID=$!

wait "$FRONTEND_PID" "$BACKEND_PID"
