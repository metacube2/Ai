#!/bin/zsh
# Kerntests ohne Xcode-Testziel: uebersetzt CAT-Kern (CATCommand, RadioState, CATProtocol, SerialPortManager) mit
# einem Logger-Ersatz und Tests/Kern/*.swift per swiftc und fuehrt sie aus. Kein Zugriff auf Ports, Einstellungen
# oder Netz. Aufruf: Tools/tests.sh  (Exit-Code 0 = alle gruen)
set -eu
cd "${0:A:h}/.."
export DEVELOPER_DIR=${DEVELOPER_DIR:-/Volumes/ExtOsX/Programme/Xcode.app/Contents/Developer}
AUS=${TMPDIR:-/tmp}/macyaesu-kerntests
mkdir -p $AUS
xcrun swiftc -swift-version 5 -D KERNTEST -target arm64-apple-macos15.0 -o $AUS/kerntests \
  MacYaesu/Models/CATCommand.swift MacYaesu/Models/RadioState.swift \
  MacYaesu/Services/CATProtocol.swift MacYaesu/Services/SerialPortManager.swift \
  Tests/Kern/*.swift
$AUS/kerntests "$@"
