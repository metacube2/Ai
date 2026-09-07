#!/usr/bin/env bash
set -euo pipefail

REMOTE_HOST="${REMOTE_HOST:-root@192.168.178.113}"
REMOTE_DIR="${REMOTE_DIR:-/data/compose/trade_web}"
ARCHIVE="/tmp/trade_web_deploy.tar.gz"

cd "$(dirname "$0")"

tar \
  --exclude='__pycache__' \
  --exclude='*.pyc' \
  --exclude='.pytest_cache' \
  --exclude='.DS_Store' \
  -czf "$ARCHIVE" .

ssh "$REMOTE_HOST" "mkdir -p '$REMOTE_DIR'"
scp "$ARCHIVE" "$REMOTE_HOST:/tmp/trade_web_deploy.tar.gz"
ssh "$REMOTE_HOST" "cd '$REMOTE_DIR' && tar -xzf /tmp/trade_web_deploy.tar.gz && docker compose up -d --build"

echo "Trading Web deployed to http://192.168.178.113:5050"
