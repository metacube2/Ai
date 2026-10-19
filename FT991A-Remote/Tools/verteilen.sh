#!/bin/zsh
# MacYaesu: Release bauen, mit "Developer ID Application" signieren, als DMG notarisieren und das Ticket anheften.
# Voraussetzungen (einmalig): Zertifikat "Developer ID Application" im Schluesselbund und Notar-Zugang
#   xcrun notarytool store-credentials aiscom-notar --apple-id <apple-id> --team-id <TEAMID>
# Aufruf: Tools/verteilen.sh   → dist/MacYaesu.dmg (danach Website-Deploy wie in instructions/homelab/aiscom-website.md)
set -eu
cd "${0:A:h}/.."

PROJEKT=MacYesu.xcodeproj
SCHEMA=MacYesu
APPNAME=MacYaesu
ENTITLEMENTS=MacYaesu/FT991A_Remote.entitlements
PROFIL=${PROFIL:-aiscom-notar}
export DEVELOPER_DIR=${DEVELOPER_DIR:-/Volumes/ExtOsX/Programme/Xcode.app/Contents/Developer}

ZEILE=$(security find-identity -v -p codesigning | grep "Developer ID Application" | head -1 || true)
[[ -n $ZEILE ]] || { print -u2 "Kein Zertifikat 'Developer ID Application' im Schluesselbund."; exit 1 }
IDENTITAET=$(print -- $ZEILE | sed -E 's/.*"(.*)".*/\1/')
TEAM=$(print -- $IDENTITAET | sed -E 's/.*\(([A-Z0-9]{10})\)$/\1/')
print "Signiere mit: $IDENTITAET"

ZIEL=build/verteilen
rm -rf $ZIEL && mkdir -p $ZIEL dist
xcodebuild -project $PROJEKT -scheme $SCHEMA -configuration Release -derivedDataPath $ZIEL/dd \
  CODE_SIGN_STYLE=Manual CODE_SIGN_IDENTITY="$IDENTITAET" DEVELOPMENT_TEAM=$TEAM PROVISIONING_PROFILE_SPECIFIER= \
  OTHER_CODE_SIGN_FLAGS="--timestamp" build | grep -E "error:|BUILD (SUCCEEDED|FAILED)"
APP="$ZIEL/dd/Build/Products/Release/$APPNAME.app"
[[ -d "$APP" ]] || { print -u2 "Build fehlgeschlagen"; exit 1 }

# Eingebettete Programme von innen nach aussen, dann die App (Hardened Runtime + Entitlements)
find "$APP/Contents" -type f -perm -u+x ! -path "*/MacOS/*" -print0 | while IFS= read -r -d '' f; do
  if file "$f" | grep -q "Mach-O"; then codesign --force --options runtime --timestamp --sign "$IDENTITAET" "$f"; fi
done
codesign --force --options runtime --timestamp --entitlements $ENTITLEMENTS --sign "$IDENTITAET" "$APP"
codesign --verify --strict --deep --verbose=2 "$APP"

# DMG wie bisher: App + Verweis auf /Applications
BUEHNE=$ZIEL/dmg
mkdir -p $BUEHNE && ditto "$APP" "$BUEHNE/$APPNAME.app" && ln -s /Applications "$BUEHNE/Applications"
DMG=$ZIEL/$APPNAME.dmg
hdiutil create -volname $APPNAME -srcfolder $BUEHNE -format UDZO -ov $DMG >/dev/null
codesign --force --timestamp --sign "$IDENTITAET" $DMG

xcrun notarytool submit $DMG --keychain-profile $PROFIL --wait
xcrun stapler staple $DMG
spctl --assess --type open --context context:primary-signature --verbose=2 $DMG
cp $DMG dist/$APPNAME.dmg
print "Fertig: dist/$APPNAME.dmg"
