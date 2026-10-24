#!/bin/zsh
# MacYaesu: Release bauen, mit "Developer ID Application" signieren, als DMG notarisieren und das Ticket anheften.
# Voraussetzungen (einmalig): Zertifikat "Developer ID Application" im Schluesselbund und Notar-Zugang
#   xcrun notarytool store-credentials aiscom-notar --apple-id <apple-id> --team-id <TEAMID>
# Aufruf: Tools/verteilen.sh               → dist/MacYaesu.dmg (danach Website-Deploy wie in instructions/homelab/aiscom-website.md)
#         Tools/verteilen.sh --tester      Tester-Fassung (Schalter MACYAESU_TESTER: aktiviert ohne Schluessel)
#                                          → dist/MacYaesu-Tester.dmg fuer www.aiscom.ch/tester/, nie verkaufen
#         Tools/verteilen.sh --ohne-notar  nur bauen, signieren, DMG (kein Kontakt zu Apples Notardienst; nicht weitergeben)
set -eu
cd "${0:A:h}/.."
TESTER=0; OHNE_NOTAR=0
for arg in "$@"; do
  case $arg in
    --tester) TESTER=1 ;;
    --ohne-notar) OHNE_NOTAR=1 ;;
    *) print -u2 "Unbekannter Schalter: $arg"; exit 1 ;;
  esac
done

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

# Tester-Fassung: eigener Build-Ordner und eigene DMG, damit die Verkaufs-DMG nie ueberschrieben wird
if (( TESTER )); then ZIEL=build/verteilen-tester; AUSGABE=dist/$APPNAME-Tester.dmg; BEDINGUNGEN='$(inherited) MACYAESU_TESTER'
else ZIEL=build/verteilen; AUSGABE=dist/$APPNAME.dmg; BEDINGUNGEN='$(inherited)'; fi
rm -rf $ZIEL && mkdir -p $ZIEL dist
xcodebuild -project $PROJEKT -scheme $SCHEMA -configuration Release -derivedDataPath $ZIEL/dd \
  CODE_SIGN_STYLE=Manual CODE_SIGN_IDENTITY="$IDENTITAET" DEVELOPMENT_TEAM=$TEAM PROVISIONING_PROFILE_SPECIFIER= \
  SWIFT_ACTIVE_COMPILATION_CONDITIONS="$BEDINGUNGEN" \
  OTHER_CODE_SIGN_FLAGS="--timestamp" build | grep -E "error:|BUILD (SUCCEEDED|FAILED)"
APP="$ZIEL/dd/Build/Products/Release/$APPNAME.app"
[[ -d "$APP" ]] || { print -u2 "Build fehlgeschlagen"; exit 1 }

# Eingebettete Programme von innen nach aussen, dann die App (Hardened Runtime + Entitlements)
find "$APP/Contents" -type f -perm -u+x ! -path "*/MacOS/*" -print0 | while IFS= read -r -d '' f; do
  if file "$f" | grep -q "Mach-O"; then codesign --force --options runtime --timestamp --sign "$IDENTITAET" "$f"; fi
done
codesign --force --options runtime --timestamp --entitlements $ENTITLEMENTS --sign "$IDENTITAET" "$APP"
codesign --verify --strict --deep --verbose=2 "$APP"
# Gegenprobe Fassung (Rauchtest): der Tester-Text steckt nur im Tester-Build; grep -c statt -q (SIGPIPE)
TESTER_TEXTE=$(strings "$APP/Contents/MacOS/$APPNAME" | grep -c "Vollversion (Testfassung)" || true)
if (( TESTER )); then (( TESTER_TEXTE > 0 )) || { print -u2 "Tester-Schalter nicht wirksam"; exit 1 }
else (( TESTER_TEXTE == 0 )) || { print -u2 "Tester-Text im Verkaufs-Build"; exit 1 }; fi

# DMG wie bisher: App + Verweis auf /Applications
BUEHNE=$ZIEL/dmg
mkdir -p $BUEHNE && ditto "$APP" "$BUEHNE/$APPNAME.app" && ln -s /Applications "$BUEHNE/Applications"
DMG=$ZIEL/$APPNAME.dmg
hdiutil create -volname $APPNAME -srcfolder $BUEHNE -format UDZO -ov $DMG >/dev/null
codesign --force --timestamp --sign "$IDENTITAET" $DMG

if (( OHNE_NOTAR )); then cp $DMG $AUSGABE; print "Fertig ohne Notarisierung: $AUSGABE – nicht weitergeben"; exit 0; fi
xcrun notarytool submit $DMG --keychain-profile $PROFIL --wait
xcrun stapler staple $DMG
spctl --assess --type open --context context:primary-signature --verbose=2 $DMG
cp $DMG $AUSGABE
print "Fertig: $AUSGABE"
