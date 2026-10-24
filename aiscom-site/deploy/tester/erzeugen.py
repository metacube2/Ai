#!/usr/bin/env python3
"""Erzeugt index.html der Tester-Seite (www.aiscom.ch/tester/) aus den gebauten Tester-DMGs: Version, Build, Datum,
Groesse, SHA-256 und Notarisierung je App werden aus DMG und App-Paket gelesen, nichts von Hand eingetragen.

Aufruf (im Ordner deploy/tester/):
  python3 erzeugen.py
Die DMG-Pfade und App-Pakete stehen in APPS; die DMGs selbst liegen nicht im Repo (gross), sie werden mit
instructions/scripts/deploy-lxc.sh 104 <dmg> /var/www/html/tester/<name>.dmg eingespielt (siehe README).
Die Seite zeigt nur Downloads und die Installationsschritte, keine Testpunkte oder Hinweise (Benutzer 2026-10-24).
Alle Apps sind Vollversionen: Kamera-Import, Antennen-Studio und MacYaesu als Tester-Fassung ohne Lizenzschluessel
(verteilen.sh --tester), Aurora Studio und Stretcher haben keine Demo-Grenze."""
import hashlib
import os
import plistlib
import subprocess
import sys
from datetime import datetime
from pathlib import Path

GIT = Path.home() / "Projects/Git"
HIER = Path(__file__).resolve().parent

APPS = [
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
        "dmg": GIT / "kamera-import/dist/KameraImport-Tester.dmg",
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
]


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
    info = plistlib.loads((a["app"] / "Contents/Info.plist").read_bytes())
    dmg = a["dmg"]
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


def seite(apps):
    alle_notarisiert = all(d["notarisiert"] for d in apps)
    schritt3 = (
        "<li><b>Starten</b> wie jede andere App – die Apps sind von Apple beglaubigt.</li>"
        if alle_notarisiert else
        "<li><b>Erster Start:</b> Rechtsklick auf die App › <b>Öffnen</b> › „Öffnen“ bestätigen. Blockiert macOS trotzdem: "
        "Systemeinstellungen › Datenschutz &amp; Sicherheit › unten <b>„Trotzdem öffnen“</b>. Nur beim ersten Start nötig.</li>"
    )
    stand = datetime.now().strftime("%d.%m.%Y %H:%M")
    karten = "".join(karte(d) for d in apps)
    return f"""<!DOCTYPE html>
<html lang="de">
<head>
<meta charset="UTF-8">
<meta name="viewport" content="width=device-width, initial-scale=1.0">
<title>Testversionen | AISCOM</title>
<meta name="robots" content="noindex,nofollow">
<link rel="icon" href="/favicon.ico">
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
.karten {{ display:grid; grid-template-columns:repeat(auto-fit,minmax(420px,1fr)); gap:24px; }}
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
footer {{ margin-top:36px; color:var(--muted); font-size:13px; }}
@media (max-width:520px) {{ .karten {{ grid-template-columns:1fr; }} .kopf {{ flex-direction:column; }} }}
</style>
</head>
<body>
<main>
  <header><div class="logo"><i></i>AISCOM · TESTVERSIONEN</div></header>
  <h1>Apps zum Testen</h1>
  <p class="unter">Aktuelle Fassungen von Aurora Studio, Kamera-Import, Antennen-Studio, MacYaesu, Stretcher und Obhut für den Mac, alle als Vollversion.</p>

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

  <footer>Stand {stand}. SHA-256 zum Vergleich: im Terminal <code>shasum -a 256 ~/Downloads/&lt;Datei&gt;.dmg</code>.</footer>
</main>
<script>
document.querySelectorAll('.sha').forEach(e => e.addEventListener('click', () => {{ navigator.clipboard?.writeText(e.textContent); e.title = 'kopiert'; }}));
</script>
</body>
</html>
"""


if __name__ == "__main__":
    fertig = []
    for a in APPS:
        if not a["dmg"].exists() or not a["app"].exists():
            print(f"FEHLT: {a['dmg']} oder {a['app']}", file=sys.stderr)
            sys.exit(1)
        fertig.append(daten(a))
    (HIER / "index.html").write_text(seite(fertig), encoding="utf-8")
    for d in fertig:
        print(f"{d['name']}: {d['version']} ({d['build']}) {d['groesse']} "
              f"{'Developer ID' if d['developer_id'] else 'ad hoc'}{', notarisiert' if d['notarisiert'] else ''} sha256 {d['sha'][:16]}… → {d['datei']}")
    print("index.html geschrieben")
