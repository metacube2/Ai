#!/bin/zsh
# Prueft www.aiscom.ch am Inhalt der Antwort, nicht nur am Statuscode.
check() { # pfad erwarteter_text
  local body=$(curl -sL -m 15 "https://www.aiscom.ch$1")
  if print -r -- "$body" | grep -q -- "$2"; then print "OK    $1"; else print "FEHLER $1 (erwartet: $2)"; fi
}
check /             'IT-Lösungen'
check /de           'Projekte im Verkauf'
check /en           'Products for sale'
check /it           'Prodotti in vendita'
check /de/services  '3D'
check /de/contact   'Nachricht senden'
check /macyaesu/    'MacYaesu'
check /kamera-uploader/ 'Kamera-Uploader'
check /synth/       'html'
check /aiscanner/   'html'
check /sitemap.xml  'https://www.aiscom.ch/de'
for p in /aiscom/README.md /aiscom/database/aiscom.db /rtsp-recorder.error.log /admin/login /aiscom/frontend/package.json /api/auth/check /api/cms; do
  code=$(curl -s -o /dev/null -w '%{http_code}' -m 15 "https://www.aiscom.ch$p")
  body=$(curl -s -m 15 "https://www.aiscom.ch$p" | head -c 300)
  if [[ $code == 403 || $code == 404 ]] && ! print -r -- "$body" | grep -qi 'password\|SQLite\|"name"'; then print "GESPERRT $p ($code)"; else print "OFFEN!  $p ($code)"; fi
done
