#!/usr/bin/env python3
"""Oeffentlicher RSS-Feed der AISCOM-Apps → feed.xml (live unter https://www.aiscom.ch/feed.xml).

Quelle sind die tatsaechlich oeffentlich verteilten Dateien, nichts von Hand:
- Mac-Apps mit Download auf der Produktseite: DMG per HTTPS laden, einhaengen, Version/Build aus Info.plist,
  Datum aus „Last-Modified“.
- Android cAurora: oeffentliche Beta-APK laden, Version aus aapt.
- Produktseiten ohne Download (noch in Vorbereitung): ein Eintrag „Produktseite“ mit Datum aus „Last-Modified“.
Der Verlauf steht in verlauf.json (im Repo); jede neue Version wird ein neuer Eintrag.
Aufruf: python3 erzeugen.py, dann deploy-lxc.sh 104 feed.xml /var/www/html/feed.xml (siehe README)."""
import plistlib
import re
import subprocess
import sys
import tempfile
import urllib.request
from datetime import datetime, timezone
from email.utils import parsedate_to_datetime
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import rss  # noqa: E402

HIER = Path(__file__).resolve().parent
BUILD_TOOLS = Path.home() / "Library/Android/sdk/build-tools/36.1.0"

MAC = [
    {"kennung": "macyaesu", "name": "MacYaesu", "seite": "https://www.aiscom.ch/macyaesu/", "datei": "MacYaesu.dmg",
     "text": "Fernsteuerung für den Yaesu FT-991A auf dem Mac: CAT-Steuerung, Logbuch, Audio."},
    {"kennung": "kamera-import", "name": "Kamera-Import", "seite": "https://www.aiscom.ch/kamera-uploader/", "datei": "KameraImport.dmg",
     "text": "Lädt von der Kamera nur die fehlenden Fotos und Videos, auf den Mac oder per SFTP."},
]
SEITEN = [
    {"kennung": "antennen-studio", "name": "Antennen-Studio", "seite": "https://www.aiscom.ch/antennen-studio/",
     "text": "NEC-Antennensimulation für den Mac mit 3D-Ansicht, Diagrammen, Frequenzgang und Optimierer."},
    {"kennung": "aurora-studio", "name": "Aurora Studio", "seite": "https://www.aiscom.ch/aurora-studio/",
     "text": "Live-Bildmischer und Tonpult für Livestreams auf dem Mac."},
]
ANDROID = [
    {"kennung": "caurora", "name": "cAurora", "seite": "https://www.aurora-weather-livecam.com/app/",
     "apk": "https://www.aurora-weather-livecam.com/app/caurora-beta.apk",
     "text": "Android-App zur Aurora-Livecam in Dürnten: Livebild, Zeitraffer, Ereignisse, Wetter. Kostenlos."},
]


def laden(url: str, ziel: Path) -> datetime:
    with urllib.request.urlopen(urllib.request.Request(url, headers={"User-Agent": "aiscom-feed"}), timeout=120) as r:
        ziel.write_bytes(r.read())
        lm = r.headers.get("Last-Modified")
    return parsedate_to_datetime(lm) if lm else datetime.now(timezone.utc)


def zeitstempel(url: str) -> datetime:
    req = urllib.request.Request(url, method="HEAD", headers={"User-Agent": "aiscom-feed"})
    with urllib.request.urlopen(req, timeout=30) as r:
        lm = r.headers.get("Last-Modified")
    return parsedate_to_datetime(lm) if lm else datetime.now(timezone.utc)


def mac_version(dmg: Path):
    punkt = tempfile.mkdtemp(prefix="feed-dmg-")
    subprocess.run(["hdiutil", "attach", "-nobrowse", "-readonly", "-noautoopen", "-mountpoint", punkt, str(dmg)], check=True, capture_output=True)
    try:
        app = next(Path(punkt).glob("*.app"))
        info = plistlib.loads((app / "Contents/Info.plist").read_bytes())
        return info.get("CFBundleShortVersionString", "?"), info.get("CFBundleVersion", "?")
    finally:
        subprocess.run(["hdiutil", "detach", punkt, "-quiet"], capture_output=True)


def eintraege() -> list:
    neu = []
    with tempfile.TemporaryDirectory() as t:
        for a in MAC:
            dmg = Path(t) / a["datei"]
            datum = laden(a["seite"] + a["datei"], dmg)
            v, b = mac_version(dmg)
            neu.append({"guid": f"{a['kennung']}-mac-{v}-{b}", "titel": f"{a['name']} {v} (Build {b}) für macOS",
                        "link": a["seite"], "text": a["text"], "datum": datum.isoformat()})
        for a in ANDROID:
            apk = Path(t) / "app.apk"
            datum = laden(a["apk"], apk)
            badge = subprocess.run([str(BUILD_TOOLS / "aapt"), "dump", "badging", str(apk)], capture_output=True, text=True).stdout
            v = re.search(r"versionName='([^']+)'", badge).group(1)
            b = re.search(r"versionCode='(\d+)'", badge).group(1)
            neu.append({"guid": f"{a['kennung']}-android-{v}-{b}", "titel": f"{a['name']} {v} für Android",
                        "link": a["seite"], "text": a["text"], "datum": datum.isoformat()})
    for a in SEITEN:
        datum = zeitstempel(a["seite"])
        neu.append({"guid": f"{a['kennung']}-seite", "titel": f"{a['name']}: Produktseite",
                    "link": a["seite"], "text": a["text"], "datum": datum.isoformat()})
    return neu


if __name__ == "__main__":
    alle = rss.verlauf_ergaenzen(HIER / "verlauf.json", eintraege())
    xml = rss.feed("AISCOM – neue App-Versionen", "https://www.aiscom.ch/de", "https://www.aiscom.ch/feed.xml",
                   "Neue Versionen der Apps von AISCOM (Mac und Android).", alle)
    (HIER / "feed.xml").write_text(xml, encoding="utf-8")
    for e in alle:
        print(e["datum"][:10], e["titel"])
    print("feed.xml geschrieben")
