#!/usr/bin/env python3
"""Erzeugt index.html der Tester-Seite (www.aiscom.ch/tester/) aus den gebauten Tester-DMGs: Version, Build, Datum,
Groesse, SHA-256 und Notarisierung je App werden aus DMG und App-Paket gelesen, nichts von Hand eingetragen.

Aufruf (im Ordner deploy/tester/):
  python3 erzeugen.py
Die DMG-Pfade und App-Pakete stehen in APPS; die DMGs selbst liegen nicht im Repo (gross), sie werden mit
instructions/scripts/deploy-lxc.sh 104 <dmg> /var/www/html/tester/<name>.dmg eingespielt (siehe README).
Die Seite zeigt nur Downloads und die Installationsschritte, keine Testpunkte oder Hinweise (Benutzer 2026-10-24).
Drei Reiter je Plattform (Benutzer 2026-10-24): macOS (DMGs, notarisiert), iPhone/iPad (im Simulator gepruefte
Fassungen; Verteilung nur ueber TestFlight, Testergebnis aus dem xcresult), Android (signierte APKs, Version aus aapt).
Alle Apps sind Vollversionen: Kamera-Import, Antennen-Studio und MacYaesu als Tester-Fassung ohne Lizenzschluessel
(verteilen.sh --tester), Aurora Studio und Stretcher haben keine Demo-Grenze."""
import hashlib
import os
import plistlib
import subprocess
import sys
from datetime import datetime
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent.parent / "feed"))
import rss  # noqa: E402

GIT = Path.home() / "Projects/Git"
HIER = Path(__file__).resolve().parent

MAC = APPS = [
    {
        "name": "Aurora Studio",
        "kennung": "aurora-studio",
        "dmg": GIT / "aurora-studio-mac/build/verteilen/Aurora Studio.dmg",
        "app": GIT / "aurora-studio-mac/build/verteilen/dd/Build/Products/Release/Aurora Studio.app",
        "datei": "AuroraStudio.dmg",
        "kurz": "Live-Bildmischer, Tonpult, Sendeziele, Aufnahme, Visuals und Shorts für den Livestream auf dem Mac.",
    },
    {
        "name": "Kamera-Import",
        "kennung": "kamera-import",
        # Build 8 (gut/schlecht-Sortierung) nur lokal beim Benutzer (2026-10-24); verteilt wird Stand Build 7 plus Sparkle
        # als Build 9 (Arbeitsordner kamera-import-b7-sparkle, Zweig tester-sparkle)
        "dmg": GIT / "kamera-import/dist/tester-live/KameraImport-Tester-Build9.dmg",
        "app": GIT / "kamera-import/build/verteilen-tester/dd/Build/Products/Release/KameraImport.app",
        "datei": "KameraImport.dmg",
        "kurz": "Lädt von der per USB angeschlossenen Kamera nur die Fotos und Videos, die im Zielordner noch fehlen – auf den Mac oder per SFTP.",
    },
    {
        "name": "Antennen-Studio",
        "kennung": "antennen-studio",
        "dmg": GIT / "antennen-studio-mac/build/verteilen-tester/Antennen-Studio-Tester.dmg",
        "app": GIT / "antennen-studio-mac/build/verteilen-tester/dd/Build/Products/Release/Antennen-Studio.app",
        "datei": "AntennenStudio.dmg",
        "kurz": "NEC-Antennensimulation mit 3D-Ansicht, Strahlungsdiagramm, Frequenzgang und Optimierer (Kerne nec2c und momwire).",
        # eingebetteter momwire-Kern (Python) ist nur arm64, darum nicht aus dem Hauptprogramm ableiten
        "system": "macOS 14 oder neuer, Apple Silicon",
    },
    {
        "name": "MacYaesu",
        "kennung": "macyaesu",
        "dmg": GIT / "Ai/FT991A-Remote/dist/MacYaesu-Tester.dmg",
        "app": GIT / "Ai/FT991A-Remote/build/verteilen-tester/dd/Build/Products/Release/MacYaesu.app",
        "datei": "MacYaesu.dmg",
        "kurz": "Steuerung des Yaesu FT-991A per USB vom Mac: Frequenz, Modus, PTT, Speicher, Logbuch und Audio.",
    },
    {
        "name": "Stretcher",
        "kennung": "stretcher",
        "dmg": GIT / "stretcher-mac/build/verteilen/Stretcher.dmg",
        "app": GIT / "stretcher-mac/build/verteilen/dd/Build/Products/Release/Stretcher.app",
        "datei": "Stretcher.dmg",
        "kurz": "Extreme Zeitdehnung von Audio (Faktor 1 bis 1000) mit Sinusmodell, Paulstretch und Phase-Vocoder, Vergleich mit Kennzahlen.",
    },
    {
        "name": "Obhut",
        "kennung": "obhut",
        "dmg": GIT / "obhut-app/build/verteilen/Obhut.dmg",
        "app": GIT / "obhut-app/build/verteilen/dd/Build/Products/Release/Obhut.app",
        "datei": "Obhut.dmg",
        "kurz": "Familien koordinieren die Betreuung alternder Eltern: Termine, Medikamente, Vollmachten, Arztberichte, Notfallblatt. Deutsch, Englisch, Französisch.",
    },
    {
        "name": "Fotogedächtnis",
        "kennung": "fotogedaechtnis",
        "dmg": GIT / "fotogedaechtnis/build/verteilen/Fotogedaechtnis.dmg",
        "datei": "Fotogedaechtnis.dmg",
        "kurz": "Fotoarchiv mit KI-Suche ohne Cloud: „Katze auf dem Sofa 2019“. Alles bleibt auf dem Gerät. Deutsch, Englisch, Französisch.",
    },
    {
        "name": "Mietprüfer",
        "kennung": "mietpruefer",
        "dmg": GIT / "mietpruefer/build/verteilen/Mietpruefer.dmg",
        "app": GIT / "mietpruefer/build/verteilen/dd/Build/Products/Release/Mietpruefer.app",
        "datei": "Mietpruefer.dmg",
        "kurz": "Mietzinssenkung, Mieterhöhung und Nebenkosten nach Schweizer Mietrecht nachrechnen, Brief als PDF. Deutsch, Französisch, Italienisch, Englisch.",
    },
]


# iPhone/iPad: App-Paket des Simulator-Baus (Version) und letzter voller Klicktestlauf (build/mobiltest.xcresult)
IOS = [
    {"name": "Antennen-Studio", "kennung": "antennen-studio",
     "app": GIT / "antennen-studio-mac/build/ios-simulator/Antennen-Studio.app",
     "test": GIT / "antennen-studio-mac/build/mobiltest.xcresult",
     "kurz": "NEC-Antennensimulation auf dem iPhone: 3D, Diagramme, Regler, Frequenzgang, Optimierer (nec2c eingebaut)."},
    {"name": "Stretcher", "kennung": "stretcher",
     "app": GIT / "stretcher-mac/build/ios-simulator/Stretcher.app",
     "test": GIT / "stretcher-mac/build/mobiltest.xcresult",
     "kurz": "Extreme Zeitdehnung von Audio auf dem iPhone, drei Verfahren im Vergleich, eingebaute Beispielklänge."},
    {"name": "cAurora", "kennung": "caurora",
     "app": GIT / "caurora-ios/build/ios-simulator/cAurora.app",
     "test": GIT / "caurora-ios/build/mobiltest.xcresult",
     "kurz": "Livebild der Aurora-Kamera Oberdürnten, Zeitraffer, Ereignisse und Wetter auf dem iPhone. Sechs Sprachen."},
    {"name": "Obhut", "kennung": "obhut",
     "app": GIT / "obhut-app/build/ios-simulator/Obhut.app",
     "test": GIT / "obhut-app/build/mobiltest.xcresult",
     "kurz": "Betreuung alternder Eltern in der Familie koordinieren, auf dem iPhone."},
    {"name": "Fotogedächtnis", "kennung": "fotogedaechtnis",
     "app": GIT / "fotogedaechtnis/build/ios-simulator/Fotogedaechtnis.app",
     "test": GIT / "fotogedaechtnis/build/mobiltest.xcresult",
     "kurz": "Fotoarchiv mit lokaler KI-Suche auf dem iPhone, mit Apple Intelligence, wo verfügbar."},
    {"name": "Mietprüfer", "kennung": "mietpruefer",
     "app": GIT / "mietpruefer/build/ios-simulator/Mietpruefer.app",
     "test": GIT / "mietpruefer/build/mobiltest.xcresult",
     "kurz": "Schweizer Mietrecht auf dem iPhone: Senkung, Erhöhung, Nebenkosten prüfen und Brief erstellen."},
]

# Android: signierte Release-APKs; Version, Paket und Mindest-Android aus aapt, Signatur aus apksigner
ANDROID = [
    {"name": "Obhut", "kennung": "obhut",
     "apk": GIT / "obhut-android/app/build/outputs/apk/release/app-release.apk",
     "datei": "Obhut.apk",
     "kurz": "Betreuung alternder Eltern in der Familie koordinieren, auf Android. Gleiches Datenformat wie Mac und iPhone."},
    {"name": "cAurora", "kennung": "caurora",
     "apk": GIT / "cAuroraAndroid/app/build/outputs/apk/release/app-release.apk",
     "datei": "cAurora.apk",
     "kurz": "Livebild der Aurora-Kamera Oberdürnten, Zeitraffer, Ereignisse, Widget. Sechs Sprachen."},
]

BUILD_TOOLS = Path.home() / "Library/Android/sdk/build-tools/36.1.0"


def sha256(p: Path) -> str:
    h = hashlib.sha256()
    with p.open("rb") as f:
        for teil in iter(lambda: f.read(1 << 20), b""):
            h.update(teil)
    return h.hexdigest()


def groesse(p: Path) -> str:
    b = p.stat().st_size
    return f"{b / 1e6:.0f} MB" if b >= 1e6 else f"{b / 1e3:.0f} kB"


def developer_id(app: Path) -> bool:
    r = subprocess.run(["codesign", "-dv", str(app)], capture_output=True, text=True)
    return "Authority=Developer ID" in r.stderr or ("TeamIdentifier=" in r.stderr and "not set" not in r.stderr)


def notarisiert(dmg: Path) -> bool:
    """Ticket angeheftet? (stapler validate, Rueckgabe 0)"""
    umgebung = {**os.environ, "DEVELOPER_DIR": os.environ.get("DEVELOPER_DIR", "/Volumes/ExtOsX/Programme/Xcode.app/Contents/Developer")}
    return subprocess.run(["xcrun", "stapler", "validate", str(dmg)], capture_output=True, env=umgebung).returncode == 0


def system(a, info) -> str:
    """Mindestsystem und Architekturen aus dem App-Paket, nichts von Hand (Vorgabe in APPS nur bei eingebetteten Kernen)"""
    if a.get("system"):
        return a["system"]
    binaer = a["app"] / "Contents/MacOS" / a["app"].stem
    archs = set(subprocess.run(["lipo", "-archs", str(binaer)], capture_output=True, text=True).stdout.split())
    cpu = "Intel und Apple Silicon" if archs >= {"x86_64", "arm64"} else ("Apple Silicon" if archs == {"arm64"} else "Intel")
    mindest = info.get("LSMinimumSystemVersion", "?").split(".")[0]
    return f"macOS {mindest} oder neuer, {cpu}"


def daten(a):
    """Version, Signatur und System aus der App IM DMG (nicht aus dem Bauordner), damit Seite und Download zusammenpassen"""
    import tempfile
    dmg = a["dmg"]
    punkt = tempfile.mkdtemp(prefix="tester-dmg-")
    subprocess.run(["hdiutil", "attach", "-nobrowse", "-readonly", "-noautoopen", "-mountpoint", punkt, str(dmg)],
                   check=True, capture_output=True)
    try:
        app = next(Path(punkt).glob("*.app"))
        a = {**a, "app": app}
        info = plistlib.loads((app / "Contents/Info.plist").read_bytes())
        d = _daten(a, info, dmg)
    finally:
        subprocess.run(["hdiutil", "detach", punkt, "-quiet"], capture_output=True)
    return d


def _daten(a, info, dmg):
    return {
        **a,
        "system": system(a, info),
        "version": info.get("CFBundleShortVersionString", "?"),
        "build": info.get("CFBundleVersion", "?"),
        "datum": datetime.fromtimestamp(dmg.stat().st_mtime).strftime("%d.%m.%Y"),
        "groesse": groesse(dmg),
        "sha": sha256(dmg),
        "developer_id": developer_id(a["app"]),
        "notarisiert": notarisiert(dmg),
    }


def xcresult(p: Path):
    """(bestanden, gesamt, Datum) des letzten Klicktestlaufs oder None"""
    if not p.exists():
        return None
    import json
    umgebung = {**os.environ, "DEVELOPER_DIR": os.environ.get("DEVELOPER_DIR", "/Volumes/ExtOsX/Programme/Xcode.app/Contents/Developer")}
    r = subprocess.run(["xcrun", "xcresulttool", "get", "test-results", "summary", "--path", str(p)], capture_output=True, text=True, env=umgebung)
    if r.returncode != 0:
        return None
    j = json.loads(r.stdout)
    gesamt = j.get("passedTests", 0) + j.get("failedTests", 0)   # uebersprungene (z. B. Bildschirmfoto-Lauf) zaehlen nicht
    geraet = (j.get("devicesAndConfigurations") or [{}])[0].get("device", {})
    return j.get("passedTests", 0), gesamt, datetime.fromtimestamp(j.get("finishTime", 0)).strftime("%d.%m.%Y"), \
        f"{geraet.get('deviceName', '?')}, iOS {geraet.get('osVersion', '?')}", \
        datetime.fromtimestamp(j.get("finishTime", 0)).astimezone().isoformat()


def daten_ios(a):
    info = plistlib.loads((a["app"] / "Info.plist").read_bytes())
    return {**a, "version": info.get("CFBundleShortVersionString", "?"), "build": info.get("CFBundleVersion", "?"),
            "mindest": info.get("MinimumOSVersion", "?"), "test": xcresult(a["test"])}


def daten_android(a):
    apk = a["apk"]
    b = subprocess.run([str(BUILD_TOOLS / "aapt"), "dump", "badging", str(apk)], capture_output=True, text=True).stdout
    import re
    feld = lambda muster: (re.search(muster, b) or [None, "?"])[1]
    java = {**os.environ, "JAVA_HOME": "/opt/homebrew/opt/openjdk/libexec/openjdk.jdk/Contents/Home"}
    java["PATH"] = java["JAVA_HOME"] + "/bin:" + java.get("PATH", "")
    sig = subprocess.run([str(BUILD_TOOLS / "apksigner"), "verify", "--print-certs", str(apk)], capture_output=True, text=True, env=java)
    zert = (re.search(r"certificate SHA-256 digest: ([0-9a-f]+)", sig.stdout) or [None, ""])[1]
    sdk = feld(r"sdkVersion:'(\d+)'")
    android = {"21": "5", "23": "6", "24": "7", "26": "8", "28": "9", "29": "10", "30": "11", "31": "12", "33": "13", "34": "14", "35": "15"}.get(sdk, f"API {sdk}")
    return {**a, "version": feld(r"versionName='([^']+)'"), "build": feld(r"versionCode='(\d+)'"),
            "android": android, "signiert": sig.returncode == 0, "zert": zert[:16],
            "datum": datetime.fromtimestamp(apk.stat().st_mtime).strftime("%d.%m.%Y"), "groesse": groesse(apk), "sha": sha256(apk)}


def karte_ios(d):
    t = d["test"]
    test = f"{t[0]} von {t[1]} Klicktests bestanden ({t[3]}, {t[2]})" if t else "noch kein voller Testlauf"
    return f"""
<article class="karte" id="ios-{d['kennung']}">
  <div class="kopf">
    <img src="bilder/{d['kennung']}.png" alt="" width="96" height="96">
    <div>
      <h2>{d['name']}</h2>
      <p class="kurz">{d['kurz']}</p>
    </div>
  </div>
  <div class="hinweis">Installation über <b>TestFlight</b>: Einladung folgt per E-Mail.</div>
  <dl class="meta">
    <dt>Version</dt><dd>{d['version']} (Build {d['build']})</dd>
    <dt>System</dt><dd>iOS {d['mindest']} oder neuer, iPhone und iPad</dd>
    <dt>Geprüft</dt><dd>{test}</dd>
  </dl>
</article>"""


def karte_android(d):
    signatur = f"signiert, Zertifikat {d['zert']}…" if d["signiert"] else "nicht signiert"
    return f"""
<article class="karte" id="android-{d['kennung']}">
  <div class="kopf">
    <img src="bilder/{d['kennung']}.png" alt="" width="96" height="96">
    <div>
      <h2>{d['name']}</h2>
      <p class="kurz">{d['kurz']}</p>
    </div>
  </div>
  <a class="laden" href="{d['datei']}" download>
    <span class="pfeil">⬇</span>
    <span><b>{d['datei']}</b><small>Version {d['version']} (Build {d['build']}) · {d['groesse']} · {d['datum']}</small></span>
  </a>
  <dl class="meta">
    <dt>System</dt><dd>Android {d['android']} oder neuer</dd>
    <dt>Signatur</dt><dd>{signatur}</dd>
    <dt>SHA-256</dt><dd><code class="sha" title="Zum Kopieren klicken">{d['sha']}</code></dd>
  </dl>
</article>"""


def karte(d):
    signatur = ("Developer ID" if d["developer_id"] else "ad hoc") + (", von Apple beglaubigt" if d["notarisiert"] else "")
    return f"""
<article class="karte" id="{d['kennung']}">
  <div class="kopf">
    <img src="bilder/{d['kennung']}.png" alt="" width="96" height="96">
    <div>
      <h2>{d['name']}</h2>
      <p class="kurz">{d['kurz']}</p>
    </div>
  </div>
  <a class="laden" href="{d['datei']}" download>
    <span class="pfeil">⬇</span>
    <span><b>{d['datei']}</b><small>Version {d['version']} (Build {d['build']}) · {d['groesse']} · {d['datum']}</small></span>
  </a>
  <dl class="meta">
    <dt>System</dt><dd>{d['system']}</dd>
    <dt>Signatur</dt><dd>{signatur}</dd>
    <dt>SHA-256</dt><dd><code class="sha" title="Zum Kopieren klicken">{d['sha']}</code></dd>
  </dl>
</article>"""


def feed_eintraege(apps, ios, android):
    """Jede Fassung auf der Seite ist ein Feed-Eintrag; Datum = Datei (DMG/APK) bzw. Ende des Klicktestlaufs"""
    neu = []
    for d in apps:
        datum = datetime.fromtimestamp(d["dmg"].stat().st_mtime).astimezone()
        neu.append({"guid": f"{d['kennung']}-mac-{d['version']}-{d['build']}", "titel": f"{d['name']} {d['version']} (Build {d['build']}) für macOS",
                    "link": f"https://www.aiscom.ch/tester/#mac", "text": d["kurz"], "datum": datum.isoformat()})
    for d in ios:
        if not d["test"]:
            continue
        datum = datetime.fromisoformat(d["test"][4])
        neu.append({"guid": f"{d['kennung']}-ios-{d['version']}-{d['build']}", "titel": f"{d['name']} {d['version']} (Build {d['build']}) für iPhone/iPad",
                    "link": "https://www.aiscom.ch/tester/#ios", "text": f"{d['kurz']} {d['test'][0]} von {d['test'][1]} Klicktests bestanden. Installation über TestFlight.",
                    "datum": datum.isoformat()})
    for d in android:
        datum = datetime.fromtimestamp(d["apk"].stat().st_mtime).astimezone()
        neu.append({"guid": f"{d['kennung']}-android-{d['version']}-{d['build']}", "titel": f"{d['name']} {d['version']} (Build {d['build']}) für Android",
                    "link": "https://www.aiscom.ch/tester/#android", "text": d["kurz"], "datum": datum.isoformat()})
    return neu


def neuigkeiten(verlauf):
    zeilen = "".join(f'<li><time>{datetime.fromisoformat(e["datum"]).strftime("%d.%m.%Y")}</time> {e["titel"]}</li>' for e in verlauf[:6])
    return f'''<section class="neu"><h3>Neueste Fassungen</h3><ul>{zeilen}</ul>
    <a class="rss" href="feed.xml" type="application/rss+xml">RSS-Feed abonnieren</a></section>'''


def seite(apps, ios, android, verlauf):
    alle_notarisiert = all(d["notarisiert"] for d in apps)
    schritt3 = (
        "<li><b>Starten</b> wie jede andere App – die Apps sind von Apple beglaubigt.</li>"
        if alle_notarisiert else
        "<li><b>Erster Start:</b> Rechtsklick auf die App › <b>Öffnen</b> › „Öffnen“ bestätigen. Blockiert macOS trotzdem: "
        "Systemeinstellungen › Datenschutz &amp; Sicherheit › unten <b>„Trotzdem öffnen“</b>. Nur beim ersten Start nötig.</li>"
    )
    stand = datetime.now().strftime("%d.%m.%Y %H:%M")
    karten = "".join(karte(d) for d in apps)
    karten_ios = "".join(karte_ios(d) for d in ios)
    karten_android = "".join(karte_android(d) for d in android)
    return f"""<!DOCTYPE html>
<html lang="de">
<head>
<meta charset="UTF-8">
<meta name="viewport" content="width=device-width, initial-scale=1.0">
<title>Testversionen | AISCOM</title>
<meta name="robots" content="noindex,nofollow">
<link rel="icon" href="/favicon.ico">
<link rel="alternate" type="application/rss+xml" title="AISCOM Testversionen" href="feed.xml">
<style>
:root {{ --bg:#0a1628; --surface:#13213b; --surface-2:#1a2b4d; --text:#f5f7fb; --muted:#a8b3c7; --accent:#87ceeb; --line:rgba(135,206,235,.18); }}
* {{ box-sizing:border-box; }}
body {{ margin:0; min-height:100vh; font-family:"Avenir Next","Helvetica Neue",Arial,sans-serif; color:var(--text); line-height:1.6;
  background:radial-gradient(circle at top right, rgba(135,206,235,.12), transparent 32%), linear-gradient(180deg,#091120,var(--bg)); }}
a {{ color:var(--accent); }}
main {{ max-width:1080px; margin:0 auto; padding:40px 24px 60px; }}
header {{ display:flex; align-items:center; gap:14px; margin-bottom:8px; }}
header .logo {{ font:700 13px ui-monospace,Menlo,monospace; letter-spacing:4px; color:var(--accent); display:flex; align-items:center; gap:9px; }}
header .logo i {{ width:11px; height:11px; border-radius:50%; background:var(--accent); display:inline-block; box-shadow:0 0 12px var(--accent); }}
h1 {{ font-size:clamp(28px,4vw,40px); line-height:1.15; margin:6px 0 10px; }}
.unter {{ color:var(--muted); font-size:17px; max-width:760px; margin:0 0 28px; }}
.karten {{ display:grid; grid-template-columns:repeat(auto-fit,minmax(min(420px,100%),1fr)); gap:24px; }}
.karte {{ background:linear-gradient(180deg,var(--surface),#0f1b33); border:1px solid var(--line); border-radius:18px; padding:26px; box-shadow:0 20px 50px rgba(0,0,0,.35); }}
.kopf {{ display:flex; gap:18px; align-items:flex-start; }}
.kopf img {{ border-radius:22px; flex:none; filter:drop-shadow(0 8px 18px rgba(0,0,0,.45)); }}
h2 {{ margin:0 0 6px; font-size:26px; }}
.kurz {{ margin:0; color:var(--text); }}
.laden {{ display:flex; align-items:center; gap:16px; margin:22px 0 18px; padding:16px 20px; border-radius:14px; text-decoration:none; color:#06121f;
  background:linear-gradient(135deg,#9ad9f3,#5fb6dc); box-shadow:0 10px 26px rgba(95,182,220,.35); transition:transform .15s, box-shadow .15s; }}
.laden:hover {{ transform:translateY(-2px); box-shadow:0 14px 32px rgba(95,182,220,.45); }}
.laden .pfeil {{ font-size:28px; line-height:1; }}
.laden b {{ display:block; font-size:18px; }}
.laden small {{ display:block; font-size:13px; opacity:.85; }}
.meta {{ display:grid; grid-template-columns:90px 1fr; gap:4px 12px; margin:0; font-size:14px; }}
.meta dt {{ color:var(--muted); }} .meta dd {{ margin:0; overflow-wrap:anywhere; }}
.sha {{ font:12px ui-monospace,Menlo,monospace; background:rgba(0,0,0,.3); padding:3px 6px; border-radius:6px; cursor:pointer; }}
h3 {{ font-size:15px; letter-spacing:1px; text-transform:uppercase; color:var(--accent); margin:0 0 6px; }}
.schritte {{ background:var(--surface-2); border:1px solid var(--line); border-radius:18px; padding:24px 28px; margin:0; }}
.schritte ol {{ margin:0; padding-left:22px; }} .schritte li {{ margin-bottom:10px; }}
.reiter {{ display:flex; gap:8px; margin:0 0 26px; flex-wrap:wrap; }}
.reiter a {{ padding:10px 20px; border-radius:999px; border:1px solid var(--line); color:var(--text); text-decoration:none; font-weight:600; background:var(--surface); }}
.reiter a[aria-selected="true"] {{ background:linear-gradient(135deg,#9ad9f3,#5fb6dc); color:#06121f; border-color:transparent; }}
.reiter small {{ opacity:.7; font-weight:500; margin-left:6px; }}
.platt[hidden] {{ display:none; }}
.neu {{ background:var(--surface); border:1px solid var(--line); border-radius:18px; padding:18px 24px; margin:0 0 26px; }}
.neu ul {{ margin:6px 0 10px; padding-left:0; list-style:none; }} .neu li {{ margin:3px 0; }}
.neu time {{ color:var(--muted); font:13px ui-monospace,Menlo,monospace; margin-right:10px; }}
.rss {{ display:inline-block; padding:6px 14px; border-radius:999px; background:#f26522; color:#fff; text-decoration:none; font-weight:600; font-size:14px; }}
.hinweis {{ margin:22px 0 18px; padding:14px 18px; border-radius:14px; background:var(--surface-2); border:1px solid var(--line); }}
footer {{ margin-top:36px; color:var(--muted); font-size:13px; }}
@media (max-width:520px) {{ .karten {{ grid-template-columns:1fr; }} .kopf {{ flex-direction:column; }} }}
</style>
</head>
<body>
<main>
  <header><div class="logo"><i></i>AISCOM · TESTVERSIONEN</div></header>
  <h1>Apps zum Testen</h1>
  <p class="unter">Aktuelle Fassungen für Mac, iPhone/iPad und Android, alle als Vollversion.</p>

  {neuigkeiten(verlauf)}

  <nav class="reiter" role="tablist">
    <a href="#mac" role="tab" data-p="mac">macOS<small>{len(apps)}</small></a>
    <a href="#ios" role="tab" data-p="ios">iPhone / iPad<small>{len(ios)}</small></a>
    <a href="#android" role="tab" data-p="android">Android<small>{len(android)}</small></a>
  </nav>

  <div class="platt" id="p-mac" role="tabpanel">
  <section class="schritte">
    <h3>Installation</h3>
    <ol>
      <li><b>DMG öffnen</b> (Doppelklick auf die geladene Datei). Es erscheint ein Fenster mit der App und einer Verknüpfung <b>„Programme“</b>.</li>
      <li><b>App auf „Programme“ ziehen.</b> Danach das DMG-Fenster schliessen und das Laufwerk auswerfen; die App startest du aus dem Programme-Ordner oder über Spotlight.</li>
      {schritt3}
      <li><b>Neue Fassung:</b> einfach wieder hineinziehen und „Ersetzen“ wählen. Einstellungen und Daten bleiben erhalten.</li>
    </ol>
  </section>
  <section class="karten" style="margin-top:32px">{karten}
  </section>
  </div>

  <div class="platt" id="p-ios" role="tabpanel">
  <section class="schritte">
    <h3>Installation</h3>
    <ol>
      <li>Apple erlaubt iPhone-Apps ausserhalb des App Store nur über <b>TestFlight</b>. Du bekommst eine <b>Einladung per E-Mail</b>.</li>
      <li>Die App <b>TestFlight</b> aus dem App Store laden, in der E-Mail auf „Testen starten“ tippen, dann „Installieren“.</li>
      <li><b>Neue Fassung:</b> TestFlight meldet sie und aktualisiert auf Tipp.</li>
    </ol>
  </section>
  <section class="karten" style="margin-top:32px">{karten_ios}
  </section>
  </div>

  <div class="platt" id="p-android" role="tabpanel">
  <section class="schritte">
    <h3>Installation</h3>
    <ol>
      <li><b>APK auf dem Android-Gerät laden</b> (Link direkt auf dem Handy öffnen).</li>
      <li>Die geladene Datei antippen. Beim ersten Mal fragt Android, ob der Browser Apps installieren darf: <b>„Zulassen“</b>, dann <b>„Installieren“</b>.</li>
      <li><b>Neue Fassung:</b> neue APK gleich installieren, sie ersetzt die alte; Daten bleiben erhalten.</li>
    </ol>
  </section>
  <section class="karten" style="margin-top:32px">{karten_android}
  </section>
  </div>

  <footer>Stand {stand}. SHA-256 zum Vergleich: im Terminal <code>shasum -a 256 ~/Downloads/&lt;Datei&gt;.dmg</code>.</footer>
</main>
<script>
function zeigen() {{
  const p = (location.hash || '#mac').slice(1).split('-')[0];
  const gueltig = ['mac', 'ios', 'android'].includes(p) ? p : 'mac';
  document.querySelectorAll('.platt').forEach(e => e.hidden = e.id !== 'p-' + gueltig);
  document.querySelectorAll('.reiter a').forEach(a => a.setAttribute('aria-selected', a.dataset.p === gueltig));
}}
window.addEventListener('hashchange', zeigen); zeigen();
document.querySelectorAll('.sha').forEach(e => e.addEventListener('click', () => {{ navigator.clipboard?.writeText(e.textContent); e.title = 'kopiert'; }}));
</script>
</body>
</html>
"""


if __name__ == "__main__":
    fertig = []
    for a in APPS:
        if not a["dmg"].exists():
            print(f"FEHLT: {a['dmg']}", file=sys.stderr)
            sys.exit(1)
        fertig.append(daten(a))
    ios = []
    for a in IOS:
        if not a["app"].exists():
            print(f"FEHLT (iOS): {a['app']}", file=sys.stderr)
            sys.exit(1)
        ios.append(daten_ios(a))
    android = []
    for a in ANDROID:
        if not a["apk"].exists():
            print(f"FEHLT (Android): {a['apk']}", file=sys.stderr)
            sys.exit(1)
        android.append(daten_android(a))
    verlauf = rss.verlauf_ergaenzen(HIER / "verlauf.json", feed_eintraege(fertig, ios, android))
    (HIER / "feed.xml").write_text(rss.feed("AISCOM – Testversionen", "https://www.aiscom.ch/tester/", "https://www.aiscom.ch/tester/feed.xml",
                                            "Neue Testfassungen für Mac, iPhone/iPad und Android.", verlauf), encoding="utf-8")
    (HIER / "index.html").write_text(seite(fertig, ios, android, verlauf), encoding="utf-8")
    for d in fertig:
        print(f"Mac {d['name']}: {d['version']} ({d['build']}) {d['groesse']} "
              f"{'Developer ID' if d['developer_id'] else 'ad hoc'}{', notarisiert' if d['notarisiert'] else ''} sha256 {d['sha'][:16]}… → {d['datei']}")
    for d in ios:
        print(f"iOS {d['name']}: {d['version']} ({d['build']}) Test {d['test']}")
    for d in android:
        print(f"Android {d['name']}: {d['version']} ({d['build']}) Android {d['android']} {'signiert ' + d['zert'] if d['signiert'] else 'UNSIGNIERT'} sha256 {d['sha'][:16]}… → {d['datei']}")
    print("index.html geschrieben")
