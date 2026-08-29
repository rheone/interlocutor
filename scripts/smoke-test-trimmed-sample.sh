#!/bin/bash
# Runs a published sample binary and exercises every dispatch path a trimmed publish must get right - typed
# Send/Publish, both open-generic dispatch paths, notification fan-out, the short-circuiting pipeline stage,
# and delete - asserting HTTP status codes and that nothing in the log is an unhandled exception.
#
# A build that produces zero trim warnings is not proof the app works: this exact sample built warning-free
# and crashed on every request the first two times its trim story was checked in this repo (see
# maintainer_docs/adr/0016-trim-compatible-not-aot-compatible.md and documentation/trim-and-aot-support.md). This script
# exists so that mistake can't happen silently again - CI must run it, not just publish.
#
# Usage: scripts/smoke-test-trimmed-sample.sh <path-to-published-binary> [port]
set -euo pipefail

BINARY="${1:?Usage: smoke-test-trimmed-sample.sh <path-to-published-binary> [port]}"
PORT="${2:-5299}"
BASE="http://127.0.0.1:${PORT}"
LOG="$(mktemp)"
BODY="$(mktemp)"

"$BINARY" --urls "$BASE" >"$LOG" 2>&1 &
PID=$!
trap 'kill "$PID" 2>/dev/null || true; rm -f "$BODY"' EXIT

fail() {
    echo "FAIL: $1" >&2
    echo "--- sample log ($LOG) ---" >&2
    cat "$LOG" >&2
    exit 1
}

# Wait for the app to start listening rather than sleeping a fixed amount - a trimmed self-contained publish
# is slower to start than a framework-dependent one.
ready=false
for _ in $(seq 1 30); do
    if curl -s -o /dev/null "$BASE/todos/created-count"; then
        ready=true
        break
    fi
    if ! kill -0 "$PID" 2>/dev/null; then
        fail "the process exited before it ever started listening on $BASE"
    fi
    sleep 1
done
[ "$ready" = true ] || fail "never started listening on $BASE within 30s"

check() {
    local description="$1" expected="$2"
    shift 2
    local status
    status=$(curl -s -o "$BODY" -w '%{http_code}' "$@")
    if [ "$status" != "$expected" ]; then
        fail "$description: expected HTTP $expected, got $status ($(cat "$BODY"))"
    fi
    echo "ok  $description -> HTTP $status"
}

check "create a to-do" 200 -X POST "$BASE/todos" -H "Content-Type: application/json" -d '{"title":"buy milk"}'
check "read it back via the dedicated handler" 200 "$BASE/todos/1"
check "read it back via the open generic handler" 200 "$BASE/todos/1/generic"
check "a tag that doesn't exist" 404 "$BASE/tags/1"
check "notification fan-out counted the create" 200 "$BASE/todos/created-count"
check "duplicate title is short-circuited by the stage" 409 -X POST "$BASE/todos" -H "Content-Type: application/json" -d '{"title":"buy milk"}'
check "delete it" 200 -X DELETE "$BASE/todos/1"
check "it's gone" 404 "$BASE/todos/1"

if grep -qi "unhandled exception" "$LOG"; then
    fail "every check above passed on status code alone, but the log contains an unhandled exception"
fi

echo "All checks passed - the trimmed publish dispatches correctly end to end."
