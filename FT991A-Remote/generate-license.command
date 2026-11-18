#!/bin/zsh
# MacYaesu-Lizenzschluessel ausstellen (Ed25519, seit 2026-11-06). Der private Schluessel liegt im Schluesselbund;
# das eigentliche Werkzeug ist ~/Projects/Git/aiscom-lizenz/lizenz.swift.
set -euo pipefail

if [[ $# -lt 1 ]]; then
  echo "Aufruf: $0 <email@example.com>"
  exit 1
fi

exec swift "$HOME/Projects/Git/aiscom-lizenz/lizenz.swift" ausstellen macyaesu "$1"
