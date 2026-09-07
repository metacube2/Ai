#!/bin/zsh
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
TOOL_SOURCE="$SCRIPT_DIR/Tools/LicenseTool/Sources/main.swift"
TOOL_BINARY="/tmp/license-tool"
MODULE_CACHE="/tmp/swift-module-cache"

if [[ $# -lt 1 ]]; then
  echo "Usage: $0 <email@example.com>"
  exit 1
fi

EMAIL="$1"

/usr/bin/swiftc -module-cache-path "$MODULE_CACHE" "$TOOL_SOURCE" -o "$TOOL_BINARY"
"$TOOL_BINARY" generate "$EMAIL"
