#!/usr/bin/env python3
"""Baut das E-Book (EPUB 3) aus kapitel/*.txt und bilder/*.svg.

Auszeichnung in den Kapiteln:
  # Titel            Kapiteltitel (erste Zeile)
  @ Ort · Jahr       Ortszeile unter dem Titel
  ## Zwischentitel
  [bild]             hier das Kapitelbild (bilder/<kapitelname>.svg) einbetten
  > Zeile           Faktenkasten „Stand heute“ (aufeinanderfolgende Zeilen = ein Kasten)
  ---                Szenenwechsel
  *kursiv*
Aufruf: python3 werkzeuge/bauen.py  → ausgabe/Die-Schweiz-denkt-mit.epub
"""
import datetime
import html
import re
import subprocess
import uuid
import zipfile
from pathlib import Path

WURZEL = Path(__file__).resolve().parent.parent
TITEL = "Die Schweiz denkt mit"
UNTERTITEL = "Geschichten aus der Zukunft der künstlichen Intelligenz, 2027–2045"
AUTOR = "AISCOM"
SPRACHE = "de-CH"
KENNUNG = "urn:uuid:" + str(uuid.uuid5(uuid.NAMESPACE_URL, "aiscom.ch/ki-buch/die-schweiz-denkt-mit"))

CSS = """
@namespace epub "http://www.idpf.org/2007/ops";
body { font-family: Georgia, "Iowan Old Style", serif; line-height: 1.55; margin: 0 4%; hyphens: auto; -webkit-hyphens: auto; }
h1 { font-family: "Avenir Next", "Helvetica Neue", sans-serif; font-size: 1.7em; line-height: 1.2; margin: 1.4em 0 .2em; }
h2 { font-family: "Avenir Next", "Helvetica Neue", sans-serif; font-size: 1.15em; margin: 1.6em 0 .4em; }
p { margin: 0; text-indent: 1.2em; text-align: justify; }
p.anzeige { text-indent: 0; text-align: left; font-family: Menlo, "Courier New", monospace; font-size: .8em; margin: .5em 0 .5em 1em; padding-left: .6em; border-left: 2px solid #b3261e; }
p.erst, h1 + p, h2 + p, .ort + p, hr + p, figure + p, aside + p { text-indent: 0; }
.ort { font-family: "Avenir Next", "Helvetica Neue", sans-serif; font-size: .85em; letter-spacing: .08em; text-transform: uppercase; color: #3a6f8f; margin: 0 0 1.2em; text-indent: 0; }
hr { border: 0; text-align: center; margin: 1.2em 0; }
hr::after { content: "✦"; color: #3a6f8f; }
figure { margin: 1.2em 0; text-align: center; page-break-inside: avoid; }
figure svg { width: 100%; height: auto; max-height: 60vh; }
figcaption { font-family: "Avenir Next", "Helvetica Neue", sans-serif; font-size: .8em; color: #555; margin-top: .3em; }
aside { margin: 1.4em 0; padding: .8em 1em; border-left: 4px solid #3a6f8f; background: #eef4f8; font-family: "Avenir Next", "Helvetica Neue", sans-serif; font-size: .88em; page-break-inside: avoid; }
aside p { text-indent: 0; text-align: left; margin: .3em 0; }
aside .kopf { font-weight: 700; letter-spacing: .06em; text-transform: uppercase; font-size: .85em; color: #3a6f8f; }
.titelei { text-align: center; margin-top: 18%; }
.titelei h1 { font-size: 2.2em; }
.titelei p { text-indent: 0; text-align: center; }
.klein { font-size: .85em; color: #555; }
nav ol { list-style: none; padding-left: 0; } nav li { margin: .4em 0; }
"""


def inline(t: str) -> str:
    t = html.escape(t, quote=False)
    return re.sub(r"\*([^*]+)\*", r"<em>\1</em>", t)


def svg_einbetten(name: str) -> tuple[str, str]:
    p = WURZEL / "bilder" / f"{name}.svg"
    if not p.exists():
        return "", ""
    s = p.read_text(encoding="utf-8")
    s = re.sub(r"<\?xml[^>]*\?>", "", s).strip()
    m = re.search(r"<title>(.*?)</title>", s, re.S)
    return s, (m.group(1).strip() if m else "")


def kapitel_xhtml(name: str, text: str) -> tuple[str, str, bool]:
    zeilen = text.strip().split("\n")
    titel = zeilen[0].lstrip("# ").strip()
    teile, absatz, kasten, hat_svg = [], [], [], False

    def absatz_schliessen():
        if absatz:
            teile.append(f"<p>{inline(' '.join(absatz))}</p>")
            absatz.clear()

    def kasten_schliessen():
        if kasten:
            inhalt = "".join(f"<p>{inline(z)}</p>" for z in kasten[1:]) if kasten[0].startswith("!") else "".join(f"<p>{inline(z)}</p>" for z in kasten)
            kopf = kasten[0][1:].strip() if kasten[0].startswith("!") else "Stand heute"
            teile.append(f'<aside epub:type="sidebar"><p class="kopf">{inline(kopf)}</p>{inhalt}</aside>')
            kasten.clear()

    teile.append(f"<h1>{inline(titel)}</h1>")
    for z in zeilen[1:]:
        s = z.rstrip()
        if s.startswith("@"):
            absatz_schliessen(); kasten_schliessen()
            teile.append(f'<p class="ort">{inline(s[1:].strip())}</p>')
        elif s.startswith("## "):
            absatz_schliessen(); kasten_schliessen()
            teile.append(f"<h2>{inline(s[3:])}</h2>")
        elif s.strip() == "[bild]":
            absatz_schliessen(); kasten_schliessen()
            svg, unter = svg_einbetten(name)
            if svg:
                hat_svg = True
                teile.append(f"<figure>{svg}" + (f"<figcaption>{inline(unter)}</figcaption>" if unter else "") + "</figure>")
        elif s.startswith(">"):
            absatz_schliessen()
            kasten.append(s[1:].strip())
        elif s.strip().startswith("*") and s.strip().endswith("*") and len(s.strip()) > 2:
            # Bildschirmanzeige der Maschine: jede Zeile ein eigener Absatz
            absatz_schliessen(); kasten_schliessen()
            teile.append(f'<p class="anzeige">{inline(s.strip())}</p>')
        elif s.strip() == "---":
            absatz_schliessen(); kasten_schliessen()
            teile.append("<hr/>")
        elif not s.strip():
            absatz_schliessen(); kasten_schliessen()
        else:
            kasten_schliessen()
            absatz.append(s.strip())
    absatz_schliessen(); kasten_schliessen()
    return titel, seite(titel, "\n".join(teile)), hat_svg


def seite(titel: str, inhalt: str) -> str:
    return f"""<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml" xmlns:epub="http://www.idpf.org/2007/ops" xml:lang="{SPRACHE}" lang="{SPRACHE}">
<head><meta charset="UTF-8"/><title>{html.escape(titel)}</title><link rel="stylesheet" type="text/css" href="buch.css"/></head>
<body>
{inhalt}
</body>
</html>
"""


def umschlag_png(svg: Path, png: Path):
    """Umschlag als PNG (viele Lesegeraete zeigen nur Rasterbilder als Cover) ueber Chrome ohne Fenster"""
    chrome = "/Applications/Google Chrome.app/Contents/MacOS/Google Chrome"
    hulle = png.with_suffix(".html")
    hulle.write_text(f'<html><body style="margin:0">{svg.read_text(encoding="utf-8")}</body></html>', encoding="utf-8")
    subprocess.run([chrome, "--headless=new", "--disable-gpu", "--hide-scrollbars", "--window-size=1600,2400",
                    f"--screenshot={png}", f"file://{hulle}"], capture_output=True, check=True)
    hulle.unlink()


def bauen():
    kapitel = sorted((WURZEL / "kapitel").glob("*.txt"))
    aus = WURZEL / "ausgabe"
    aus.mkdir(exist_ok=True)
    cover_svg = WURZEL / "bilder" / "umschlag.svg"
    cover_png = aus / "umschlag.png"
    umschlag_png(cover_svg, cover_png)

    dateien, manifest, spine, nav = {}, [], [], []
    dateien["buch.css"] = CSS
    manifest.append('<item id="css" href="buch.css" media-type="text/css"/>')
    manifest.append('<item id="umschlag-bild" href="umschlag.png" media-type="image/png" properties="cover-image"/>')

    umschlag = seite(TITEL, f'<figure style="margin:0">{svg_einbetten("umschlag")[0]}</figure>')
    dateien["umschlag.xhtml"] = umschlag
    manifest.append('<item id="umschlag" href="umschlag.xhtml" media-type="application/xhtml+xml" properties="svg"/>')
    spine.append('<itemref idref="umschlag" linear="yes"/>')

    jahr = datetime.date.today().year
    titelei = seite(TITEL, f"""<div class="titelei"><h1>{TITEL}</h1><p>{UNTERTITEL}</p>
<p class="klein" style="margin-top:3em">Ein Zukunftsbuch. Die Geschichten sind erfunden; Menschen, Firmen und Ereignisse der
Zukunft sind Fiktion. Die Kästen „Stand heute“ beschreiben, was es zum Zeitpunkt des Schreibens (Herbst {jahr}) schon gibt.</p>
<p class="klein" style="margin-top:2em">© {jahr} {AUTOR} · aiscom.ch</p></div>""")
    dateien["titelei.xhtml"] = titelei
    manifest.append('<item id="titelei" href="titelei.xhtml" media-type="application/xhtml+xml"/>')
    spine.append('<itemref idref="titelei"/>')

    manifest.append('<item id="nav" href="inhalt.xhtml" media-type="application/xhtml+xml" properties="nav"/>')
    spine.append('<itemref idref="nav"/>')

    woerter = 0
    for i, k in enumerate(kapitel, 1):
        text = k.read_text(encoding="utf-8")
        woerter += len(re.findall(r"\w+", text))
        titel, x, hat_svg = kapitel_xhtml(k.stem, text)
        datei = f"k{i:02d}.xhtml"
        dateien[datei] = x
        eig = ' properties="svg"' if hat_svg else ""
        manifest.append(f'<item id="k{i:02d}" href="{datei}" media-type="application/xhtml+xml"{eig}/>')
        spine.append(f'<itemref idref="k{i:02d}"/>')
        nav.append(f'<li><a href="{datei}">{html.escape(titel)}</a></li>')

    dateien["inhalt.xhtml"] = seite("Inhalt", f'<nav epub:type="toc" id="toc"><h1>Inhalt</h1><ol>{"".join(nav)}</ol></nav>'
                                    f'<nav epub:type="landmarks" hidden="hidden"><ol><li><a epub:type="cover" href="umschlag.xhtml">Umschlag</a></li>'
                                    f'<li><a epub:type="toc" href="inhalt.xhtml">Inhalt</a></li><li><a epub:type="bodymatter" href="k01.xhtml">Beginn</a></li></ol></nav>')
    jetzt = datetime.datetime.now(datetime.timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ")
    opf = f"""<?xml version="1.0" encoding="UTF-8"?>
<package xmlns="http://www.idpf.org/2007/opf" version="3.0" unique-identifier="kennung" xml:lang="{SPRACHE}">
<metadata xmlns:dc="http://purl.org/dc/elements/1.1/">
<dc:identifier id="kennung">{KENNUNG}</dc:identifier>
<dc:title>{TITEL}</dc:title>
<dc:creator>{AUTOR}</dc:creator>
<dc:language>{SPRACHE}</dc:language>
<dc:description>{UNTERTITEL}</dc:description>
<dc:subject>Künstliche Intelligenz</dc:subject><dc:subject>Schweiz</dc:subject><dc:subject>Zukunft</dc:subject>
<meta property="dcterms:modified">{jetzt}</meta>
<meta name="cover" content="umschlag-bild"/>
</metadata>
<manifest>
{chr(10).join(manifest)}
</manifest>
<spine>
{chr(10).join(spine)}
</spine>
</package>
"""
    ziel = aus / "Die-Schweiz-denkt-mit.epub"
    with zipfile.ZipFile(ziel, "w") as z:
        z.writestr(zipfile.ZipInfo("mimetype"), "application/epub+zip", compress_type=zipfile.ZIP_STORED)
        z.writestr("META-INF/container.xml", """<?xml version="1.0" encoding="UTF-8"?>
<container version="1.0" xmlns="urn:oasis:names:tc:opendocument:xmlns:container"><rootfiles>
<rootfile full-path="OEBPS/buch.opf" media-type="application/oebps-package+xml"/></rootfiles></container>""", compress_type=zipfile.ZIP_DEFLATED)
        z.writestr("OEBPS/buch.opf", opf, compress_type=zipfile.ZIP_DEFLATED)
        for name, inhalt in dateien.items():
            z.writestr(f"OEBPS/{name}", inhalt, compress_type=zipfile.ZIP_DEFLATED)
        z.write(cover_png, "OEBPS/umschlag.png", compress_type=zipfile.ZIP_DEFLATED)
    print(f"{ziel}: {len(kapitel)} Kapitel, {woerter} Woerter (~{woerter // 280} Seiten bei 280 Woertern/Seite)")


if __name__ == "__main__":
    bauen()
