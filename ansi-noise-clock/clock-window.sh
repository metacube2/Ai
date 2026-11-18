#!/usr/bin/env bash
# Startet die ANSI Noise Clock in einem eigenen neuen Terminal-Fenster.
#
#   ./clock-window.sh                       Standard
#   ./clock-window.sh --palette amber --zoom 3.2
#   CLOCK_ROWS=45 CLOCK_COLS=150 ./clock-window.sh
#
# Alle Argumente werden unveraendert an noise_clock.py weitergereicht.
set -euo pipefail

DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PY="${PYTHON:-$(command -v python3 || true)}"
ROWS="${CLOCK_ROWS:-36}"
COLS="${CLOCK_COLS:-120}"
APP="${CLOCK_TERM_APP:-Terminal}"

if [ -z "$PY" ]; then
  echo "python3 nicht gefunden. PYTHON=/pfad/zu/python3 setzen." >&2
  exit 1
fi

# Kommandozeile fuer die Shell im neuen Fenster zusammenbauen
CMD="clear; exec $(printf %q "$PY") $(printf %q "$DIR/noise_clock.py")"
for arg in "$@"; do
  CMD="$CMD $(printf %q "$arg")"
done

# und danach als AppleScript-Stringliteral escapen
ESC=${CMD//\\/\\\\}
ESC=${ESC//\"/\\\"}

if [ "$APP" = "iTerm" ] || [ "$APP" = "iTerm2" ]; then
  osascript <<EOF
tell application "iTerm"
  activate
  set newWindow to (create window with default profile)
  tell current session of newWindow
    write text "$ESC"
  end tell
end tell
EOF
else
  osascript <<EOF
tell application "Terminal"
  activate
  do script "$ESC"
  set theWindow to window 1
  try
    set number of rows of theWindow to $ROWS
    set number of columns of theWindow to $COLS
  end try
  set custom title of theWindow to "ANSI Noise Clock"
end tell
EOF
fi
