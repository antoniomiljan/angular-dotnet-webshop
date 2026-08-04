#!/usr/bin/env bash
# Starts the whole local dev stack: Postgres, API, Stripe webhook forwarder, Angular.
# Ctrl+C stops everything it started.
set -euo pipefail
cd "$(dirname "${BASH_SOURCE[0]}")"

LOG_DIR="$(mktemp -d)"
PIDS=()

cleanup() {
  echo ""
  echo "Shutting down..."
  for pid in "${PIDS[@]}"; do
    kill "$pid" 2>/dev/null || true
  done
  wait 2>/dev/null || true
  rm -rf "$LOG_DIR"
}
trap cleanup EXIT INT TERM

echo "==> Starting Postgres (docker compose)"
docker compose up -d

echo "==> Waiting for Postgres..."
until docker compose exec -T db pg_isready -U postgres >/dev/null 2>&1; do
  sleep 1
done

# Free up 5255 if a stale `dotnet run` from a previous session is still holding it -
# otherwise you keep talking to yesterday's compiled code without realizing it.
STALE_PID="$(lsof -ti:5255 2>/dev/null || true)"
if [ -n "$STALE_PID" ]; then
  echo "==> Killing stale process on :5255 (pid $STALE_PID)"
  kill "$STALE_PID" 2>/dev/null || true
  sleep 1
fi

echo "==> Fetching a fresh Stripe webhook secret"
WEBHOOK_SECRET="$(stripe listen --print-secret)"
(cd Api && dotnet user-secrets set "Stripe:WebhookSecret" "$WEBHOOK_SECRET" >/dev/null)
echo "    Webhook secret synced to user-secrets."

echo "==> Starting API"
(cd Api && dotnet run) >"$LOG_DIR/api.log" 2>&1 &
PIDS+=($!)

echo "==> Waiting for API on :5255..."
until curl -s -o /dev/null http://localhost:5255/api/products; do
  sleep 1
done

echo "==> Starting Stripe webhook forwarder"
stripe listen --forward-to http://localhost:5255/api/webhooks/stripe >"$LOG_DIR/stripe.log" 2>&1 &
PIDS+=($!)

echo "==> Starting Angular dev server"
(cd ClientApp && ng serve) >"$LOG_DIR/ng.log" 2>&1 &
PIDS+=($!)

echo ""
echo "Everything is up. Tailing logs below (Ctrl+C stops all of it):"
echo "  API:    http://localhost:5255"
echo "  Angular: http://localhost:4200"
echo ""

tail -f "$LOG_DIR/api.log" "$LOG_DIR/stripe.log" "$LOG_DIR/ng.log"
