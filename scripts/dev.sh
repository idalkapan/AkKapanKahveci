#!/bin/sh
set -eu
cd "$(dirname "$0")/.."
if ! command -v dotnet >/dev/null || ! command -v npm >/dev/null; then
  echo 'Node.js ve .NET 8 SDK gerekli.' >&2
  exit 1
fi
if [ ! -d frontend/node_modules ]; then
  (cd frontend && npm ci)
fi
dotnet build backend
(cd backend && exec dotnet run --no-build) &
backend_pid=$!
cleanup() { kill "$backend_pid" "${frontend_pid:-}" 2>/dev/null || true; }
trap cleanup EXIT INT TERM
(cd frontend && exec npm run dev) &
frontend_pid=$!
echo 'Ak Kapan: http://localhost:3000 — Ctrl+C ile kapatın.'
wait "$backend_pid" "$frontend_pid"
