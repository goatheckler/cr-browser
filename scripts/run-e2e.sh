#!/usr/bin/env bash
set -euo pipefail

# Simple harness to run backend + frontend + playwright tests.
# Requirements: dotnet, node (v20), and playwright browsers installed (npx playwright install --with-deps chromium)

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
BACKEND_PROJ="$ROOT_DIR/backend/src/CrBrowser.Api/CrBrowser.Api.csproj"
FRONTEND_DIR="$ROOT_DIR/frontend"

PORT_FRONTEND=5173
PORT_BACKEND=5214

# Kill any existing processes on our ports before starting
echo "[pre-cleanup] Killing any existing processes on ports $PORT_BACKEND and $PORT_FRONTEND" >&2
lsof -ti:$PORT_BACKEND | xargs -r kill -9 2>/dev/null || true
lsof -ti:$PORT_FRONTEND | xargs -r kill -9 2>/dev/null || true
sleep 1

cleanup() {
  echo "[cleanup] Stopping background processes" >&2
  # Force kill all background jobs spawned by this script
  jobs -p | xargs -r kill -9 2>/dev/null || true
  # Also try specific PIDs if captured
  [[ -n "${BACKEND_PID:-}" ]] && kill -9 $BACKEND_PID 2>/dev/null || true
  [[ -n "${FRONTEND_PID:-}" ]] && kill -9 $FRONTEND_PID 2>/dev/null || true
}
trap cleanup EXIT

echo "[backend] Starting API" >&2
dotnet run --project "$BACKEND_PROJ" &
BACKEND_PID=$!

# Wait for health endpoint
for i in {1..30}; do
  if curl -sf http://localhost:${PORT_BACKEND}/api/health >/dev/null; then
    echo "[backend] Healthy" >&2
    break
  fi
  sleep 1
  if [[ $i -eq 30 ]]; then
    echo "Backend failed to start" >&2
    exit 1
  fi
done

echo "[frontend] Installing deps (if needed)" >&2
(cd "$FRONTEND_DIR" && npm install)

# Ensure playwright browser is installed (without system deps to avoid sudo)
(cd "$FRONTEND_DIR" && npx playwright install chromium)

echo "[frontend] Starting dev server" >&2
(cd "$FRONTEND_DIR" && npm run dev) &
FRONTEND_PID=$!

for i in {1..30}; do
  if curl -sf http://localhost:${PORT_FRONTEND} >/dev/null; then
    echo "[frontend] Ready" >&2
    break
  fi
  sleep 1
  if [[ $i -eq 30 ]]; then
    echo "Frontend failed to start" >&2
    exit 1
  fi
done

echo "[e2e] Running Playwright tests" >&2
(cd "$FRONTEND_DIR" && npx playwright test --reporter=line)
