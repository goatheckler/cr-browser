#!/usr/bin/env bash
set -euo pipefail

# Simple harness to run backend + frontend + playwright tests.
# Requirements: dotnet, node (v20), and playwright browsers installed (npx playwright install --with-deps chromium)

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
BACKEND_PROJ="$ROOT_DIR/backend/src/GhcrBrowser.Api/GhcrBrowser.Api.csproj"
FRONTEND_DIR="$ROOT_DIR/frontend"

PORT_FRONTEND=5173
PORT_BACKEND=5214

cleanup() {
  echo "[cleanup] Stopping background processes" >&2
  [[ -n "${BACKEND_PID:-}" && -d /proc/${BACKEND_PID:-} ]] && kill $BACKEND_PID || true
  [[ -n "${FRONTEND_PID:-}" && -d /proc/${FRONTEND_PID:-} ]] && kill $FRONTEND_PID || true
}
trap cleanup EXIT

echo "[backend] Starting API" >&2
dotnet run --project "$BACKEND_PROJ" >/dev/null 2>&1 &
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
(cd "$FRONTEND_DIR" && npm install >/dev/null 2>&1)

# Ensure playwright browser is installed
(cd "$FRONTEND_DIR" && npx playwright install --with-deps chromium >/dev/null 2>&1 || true)

echo "[frontend] Starting dev server" >&2
(cd "$FRONTEND_DIR" && npm run dev >/dev/null 2>&1 &)
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
