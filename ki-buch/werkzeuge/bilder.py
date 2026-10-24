#!/usr/bin/env python3
"""Zeichnet die Kapitelbilder (SVG) fuer das E-Book. Jedes Bild: viewBox 800x500, <title> = Bildunterschrift.
Aufruf: python3 werkzeuge/bilder.py  → bilder/*.svg"""
import math
import random
from pathlib import Path

ZIEL = Path(__file__).resolve().parent.parent / "bilder"
W, H = 800, 500


def berge(r, y0, hoehe, zacken, glatt=0.5, breite=W):
    """Bergkamm als Pfad (geschlossen nach unten)"""
    punkte = []
    n = zacken * 6
    phase = [r.uniform(0, 6.28) for _ in range(4)]
    for i in range(n + 1):
        x = breite * i / n
        y = y0 - hoehe * (0.55 + 0.25 * math.sin(i / n * zacken * 1.3 + phase[0])
                          + 0.15 * math.sin(i / n * zacken * 3.1 + phase[1]) * glatt
                          + 0.08 * math.sin(i / n * zacken * 7.7 + phase[2]))
        y += r.uniform(-hoehe * 0.04, hoehe * 0.04)
        punkte.append((x, y))
    d = f"M0,{H} " + " ".join(f"L{x:.1f},{y:.1f}" for x, y in punkte) + f" L{breite},{H} Z"
    return d


def svg(name, titel, inhalt, defs=""):
    s = f'''<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 {W} {H}" width="{W}" height="{H}" role="img" aria-label="{titel}">
<title>{titel}</title>
<defs>{defs}</defs>
{inhalt}
</svg>
'''
    (ZIEL / f"{name}.svg").write_text(s, encoding="utf-8")


def himmel(p, oben, unten):
    return (f'<linearGradient id="{p}h" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="{oben}"/>'
            f'<stop offset="1" stop-color="{unten}"/></linearGradient>', f'<rect width="{W}" height="{H}" fill="url(#{p}h)"/>')


def sterne(r, n, ymax, farbe="#fff"):
    return "".join(f'<circle cx="{r.uniform(0, W):.0f}" cy="{r.uniform(0, ymax):.0f}" r="{r.choice([0.8, 1, 1.4]):.1f}" fill="{farbe}" opacity="{r.uniform(.3, .9):.2f}"/>' for _ in range(n))


def schnee(r, n, farbe="#fff"):
    return "".join(f'<circle cx="{r.uniform(0, W):.0f}" cy="{r.uniform(0, H):.0f}" r="{r.uniform(1, 3):.1f}" fill="{farbe}" opacity="{r.uniform(.4, .9):.2f}"/>' for _ in range(n))


def haus(x, y, s=1.0, wand="#e9dcc3", dach="#5b3a29", fenster="#ffd27a"):
    w, h = 46 * s, 30 * s
    return (f'<g><rect x="{x:.0f}" y="{y - h:.0f}" width="{w:.0f}" height="{h:.0f}" fill="{wand}"/>'
            f'<path d="M{x - 6 * s:.0f},{y - h:.0f} L{x + w / 2:.0f},{y - h - 22 * s:.0f} L{x + w + 6 * s:.0f},{y - h:.0f} Z" fill="{dach}"/>'
            f'<rect x="{x + 8 * s:.0f}" y="{y - h + 8 * s:.0f}" width="{9 * s:.0f}" height="{9 * s:.0f}" fill="{fenster}"/>'
            f'<rect x="{x + 28 * s:.0f}" y="{y - h + 8 * s:.0f}" width="{9 * s:.0f}" height="{9 * s:.0f}" fill="{fenster}"/></g>')


def tanne(x, y, s, farbe="#1f3b2d"):
    return f'<path d="M{x},{y - 40 * s} L{x - 12 * s},{y} L{x + 12 * s},{y} Z" fill="{farbe}"/>'


# ---------------------------------------------------------------------------------------------------------------

def prolog():
    r = random.Random(1)
    d, hg = himmel("p0", "#1d2b4f", "#e9a76b")
    inhalt = hg + sterne(r, 40, 160)
    inhalt += f'<path d="{berge(r, 330, 220, 5)}" fill="#3b4a6b"/>'
    inhalt += f'<path d="{berge(r, 380, 140, 7)}" fill="#2a3552"/>'
    # Altstadt Chur: Daecher und Kirchturm
    for i in range(14):
        x = 40 + i * 52
        inhalt += haus(x, 470, 0.9 + (i % 3) * 0.15, wand="#c9b79c", dach="#6e3b2f")
    inhalt += '<rect x="372" y="300" width="26" height="170" fill="#b9a68a"/><path d="M366,300 L385,240 L404,300 Z" fill="#4c2c24"/>'
    # Zug
    inhalt += '<rect x="0" y="472" width="800" height="28" fill="#1b1f2e"/><g fill="#c8102e"><rect x="430" y="452" width="150" height="24" rx="6"/><rect x="586" y="452" width="150" height="24" rx="6"/></g>'
    inhalt += "".join(f'<rect x="{440 + i * 20}" y="457" width="12" height="8" fill="#ffe7a8"/>' for i in range(14))
    # Notizbuch
    inhalt += '<g transform="translate(70,330) rotate(-8)"><rect width="120" height="90" rx="4" fill="#f4ecd8" stroke="#4a3b2a" stroke-width="2"/><line x1="60" y1="0" x2="60" y2="90" stroke="#4a3b2a"/>'
    inhalt += "".join(f'<line x1="70" y1="{14 + i * 12}" x2="110" y2="{14 + i * 12}" stroke="#9a8a70"/>' for i in range(6)) + '</g>'
    svg("00-prolog", "Chur im Abendlicht: Hier beginnt die Reise.", inhalt, d)


def lawine():
    r = random.Random(2)
    d, hg = himmel("p1", "#0b1630", "#2c3e6b")
    inhalt = hg + sterne(r, 70, 200)
    inhalt += '<circle cx="640" cy="80" r="26" fill="#f2f0e6" opacity=".9"/>'
    inhalt += f'<path d="{berge(r, 300, 230, 3)}" fill="#e8eef7"/>'
    # Rinne mit Hoehenlinien
    inhalt += '<path d="M330,120 C350,200 360,260 420,330 L470,330 C430,260 410,190 380,110 Z" fill="#c9d6ea"/>'
    for i in range(7):
        inhalt += f'<path d="M{300 + i * 4},{150 + i * 28} C{360},{170 + i * 28} {420},{160 + i * 30} {500 - i * 3},{180 + i * 28}" fill="none" stroke="#7d93b8" stroke-width="1" opacity=".7"/>'
    # Wahrscheinlichkeitsflaeche
    inhalt += '<path d="M340,130 C365,210 375,265 430,330 L465,330 C425,262 405,195 375,125 Z" fill="#e0453a" opacity=".35"/>'
    inhalt += f'<path d="{berge(r, 430, 90, 6)}" fill="#203152"/>'
    # Strasse mit Glasfaser-Sensoren
    inhalt += '<path d="M0,410 C200,380 400,420 800,390" fill="none" stroke="#5b6b8a" stroke-width="10"/>'
    inhalt += '<path d="M0,410 C200,380 400,420 800,390" fill="none" stroke="#ffd166" stroke-width="2" stroke-dasharray="2 14"/>'
    for i, x in enumerate([380, 440, 500]):
        inhalt += haus(x, 412 - i * 2, 0.8, wand="#d8cdb8", dach="#40302a")
    inhalt += "".join(tanne(x, 450 + r.uniform(0, 30), r.uniform(.7, 1.2), "#14233a") for x in range(10, 800, 34))
    inhalt += '<rect x="448" y="280" width="62" height="28" rx="6" fill="#b3261e"/><text x="479" y="301" text-anchor="middle" font-family="Helvetica, Arial" font-size="20" fill="#fff" font-weight="700">0,61</text>'
    svg("01-lawine", "Die Rinne über Tschuggen: rot die berechnete Auslösewahrscheinlichkeit, gelb die Glasfaser als Sensor.", inhalt, d)


def gletscher():
    r = random.Random(3)
    d, hg = himmel("p2", "#8ec5e8", "#eaf5fb")
    inhalt = hg
    inhalt += f'<path d="{berge(r, 280, 220, 4)}" fill="#7b8794"/>'
    inhalt += '<path d="M220,160 C300,200 330,260 360,340 L520,340 C470,260 440,200 520,150 C420,170 330,150 220,160 Z" fill="#f4fbff" stroke="#b8d7ea"/>'
    for i in range(6):
        inhalt += f'<path d="M{260 + i * 30},{190 + i * 20} l40,10" stroke="#9cc7e0" stroke-width="2"/>'
    inhalt += '<path d="M520,120 L560,110 L590,150 L555,175 Z" fill="#6b5a4e" stroke="#e0453a" stroke-width="3" stroke-dasharray="6 4"/>'
    inhalt += f'<path d="{berge(r, 420, 90, 5)}" fill="#4f6b52"/>'
    # Radarstation
    inhalt += '<g transform="translate(120,360)"><rect x="-4" y="0" width="8" height="40" fill="#333"/><rect x="-22" y="-14" width="44" height="16" fill="#e8e8e8" stroke="#333"/>'
    inhalt += "".join(f'<path d="M30,{-6} a{18 + i * 14},{18 + i * 14} 0 0 1 0,{-1}" fill="none"/>' for i in range(3))
    inhalt += "".join(f'<path d="M26,-6 Q{50 + i * 18},{-30 - i * 10} {70 + i * 22},-6" fill="none" stroke="#e0453a" stroke-width="2" opacity="{.9 - i * .25}"/>' for i in range(3)) + '</g>'
    # Kurve inverse Geschwindigkeit
    inhalt += '<g transform="translate(560,300)"><rect width="210" height="140" rx="8" fill="#ffffff" opacity=".92" stroke="#355"/>'
    inhalt += '<line x1="20" y1="120" x2="195" y2="120" stroke="#355"/><line x1="20" y1="15" x2="20" y2="120" stroke="#355"/>'
    pts = " ".join(f"{20 + i * 8},{25 + 95 * (i / 20) ** 0.9 + r.uniform(-4, 4):.0f}" for i in range(21))
    inhalt += f'<polyline points="{pts}" fill="none" stroke="#1d6fa3" stroke-width="2.5"/>'
    inhalt += '<line x1="20" y1="22" x2="190" y2="120" stroke="#e0453a" stroke-dasharray="5 4"/>'
    inhalt += '<text x="28" y="34" font-family="Helvetica, Arial" font-size="12" fill="#355">1 / Geschwindigkeit</text><text x="150" y="134" font-family="Helvetica, Arial" font-size="12" fill="#355">Zeit</text></g>'
    svg("02-gletscher", "Der Wächter misst den Felssporn: sinkt die Kurve gegen null, droht der Abbruch.", inhalt, d)


def strom():
    r = random.Random(4)
    d, hg = himmel("p3", "#9aa7b4", "#dfe3e6")
    inhalt = hg
    inhalt += f'<path d="{berge(r, 250, 170, 4)}" fill="#6f7d8c"/>'
    # zwei Stauseen und Druckleitung
    inhalt += '<ellipse cx="200" cy="130" rx="80" ry="16" fill="#3d7ea6"/><ellipse cx="520" cy="300" rx="110" ry="20" fill="#3d7ea6"/>'
    inhalt += '<path d="M230,140 C300,220 380,250 470,295" fill="none" stroke="#2b4c63" stroke-width="6"/>'
    inhalt += '<path d="M230,140 C300,220 380,250 470,295" fill="none" stroke="#8fd3ff" stroke-width="2" stroke-dasharray="8 8"/>'
    inhalt += f'<path d="{berge(r, 400, 80, 6)}" fill="#4d5a4a"/>'
    # Masten mit Leitungen
    xs = [60, 220, 380, 540, 700]
    for x in xs:
        inhalt += f'<path d="M{x - 14},470 L{x},360 L{x + 14},470 M{x - 22},380 L{x + 22},380 M{x - 16},400 L{x + 16},400" stroke="#2d2d2d" stroke-width="2.5" fill="none"/>'
    for y in (380, 400):
        inhalt += "".join(f'<path d="M{a + 22},{y} Q{(a + b) / 2},{y + 22} {b - 22},{y}" stroke="#2d2d2d" fill="none"/>' for a, b in zip(xs, xs[1:]))
    # Haeuser mit Kaestchen
    for i, x in enumerate(range(90, 760, 120)):
        inhalt += haus(x, 490, 0.8, wand="#f1ebe0", dach="#8b3a3a")
        inhalt += f'<circle cx="{x + 18}" cy="456" r="4" fill="#3cb371"/>'
    # Lastkurve
    inhalt += '<g transform="translate(530,40)"><rect width="250" height="120" rx="8" fill="#fff" opacity=".9"/>'
    norm = " ".join(f"{15 + i * 10},{100 - 60 * math.exp(-((i - 12) / 4) ** 2) - 10:.0f}" for i in range(20))
    flex = " ".join(f"{15 + i * 10},{100 - 35 * math.exp(-((i - 12) / 5) ** 2) - 10:.0f}" for i in range(20))
    inhalt += f'<polyline points="{norm}" fill="none" stroke="#c0392b" stroke-width="2" stroke-dasharray="5 4"/><polyline points="{flex}" fill="none" stroke="#27ae60" stroke-width="3"/>'
    inhalt += '<text x="14" y="18" font-family="Helvetica, Arial" font-size="12" fill="#333">Abendspitze ohne / mit Flexibilität</text></g>'
    svg("03-strom", "Pumpspeicher oben, Kästchen in jedem Keller unten: Der Dirigent glättet die Abendspitze.", inhalt, d)


def aerztin():
    r = random.Random(5)
    d, hg = himmel("p4", "#cfe3f0", "#f6efe2")
    inhalt = hg
    inhalt += f'<path d="{berge(r, 230, 150, 4)}" fill="#a7b4bf"/>'
    # Engadinerhaus mit Sgraffito
    inhalt += '<rect x="230" y="200" width="340" height="270" fill="#efe6d6" stroke="#8a7a66" stroke-width="2"/>'
    inhalt += '<path d="M210,200 L400,120 L590,200 Z" fill="#6c6f73"/>'
    for i in range(8):
        inhalt += f'<path d="M{240 + i * 41},212 l20,12 l20,-12" fill="none" stroke="#7a6a58" stroke-width="2"/>'
    for (x, y) in [(270, 260), (360, 260), (450, 260), (270, 350), (450, 350)]:
        inhalt += f'<rect x="{x}" y="{y}" width="44" height="52" fill="#3a4a5a" stroke="#d8cbb4" stroke-width="8"/>'
    inhalt += '<path d="M360,470 L360,370 Q395,340 430,370 L430,470 Z" fill="#6b4a2f"/>'
    # Herzlinie und gelber Punkt
    pts = []
    for i in range(80):
        x = 20 + i * 9.5
        y = 120 + (-35 if i % 16 == 8 else (18 if i % 16 == 9 else 0))
        pts.append(f"{x:.0f},{y}")
    inhalt += f'<polyline points="{" ".join(pts)}" fill="none" stroke="#c0392b" stroke-width="2" opacity=".6"/>'
    inhalt += '<circle cx="700" cy="70" r="16" fill="#f4c430"/><text x="660" y="105" font-family="Helvetica, Arial" font-size="13" fill="#5a4a2a">Hinweis</text>'
    svg("04-aerztin", "Die Praxis hinter dem Dorfplatz von Scuol – und ein kleiner gelber Punkt.", inhalt, d)


def basel():
    r = random.Random(6)
    d, hg = himmel("p5", "#10283b", "#2f5a75")
    inhalt = hg
    # Rhein und Muenster
    inhalt += '<rect x="0" y="380" width="800" height="120" fill="#1e4d6b"/>'
    inhalt += "".join(f'<path d="M{r.uniform(0, 800):.0f},{r.uniform(390, 490):.0f} l40,0" stroke="#5f9ec2" opacity=".5"/>' for _ in range(30))
    inhalt += '<path d="M80,380 L80,250 L100,200 L120,250 L120,380 Z M150,380 L150,250 L170,200 L190,250 L190,380 Z M110,380 L110,300 L160,300 L160,380 Z" fill="#8c4a3c"/>'
    inhalt += '<path d="M210,380 L210,330 L330,330 L330,380 Z" fill="#7a3e32"/>'
    # Molekuel
    atome = [(480, 170), (540, 140), (600, 170), (600, 235), (540, 265), (480, 235), (660, 140), (720, 170), (740, 235), (700, 290), (650, 320), (420, 140)]
    bindungen = [(0, 1), (1, 2), (2, 3), (3, 4), (4, 5), (5, 0), (2, 6), (6, 7), (7, 8), (8, 9), (9, 10), (0, 11)]
    for a, b in bindungen:
        (x1, y1), (x2, y2) = atome[a], atome[b]
        inhalt += f'<line x1="{x1}" y1="{y1}" x2="{x2}" y2="{y2}" stroke="#d7ecf7" stroke-width="4"/>'
    farben = ["#9bd1ff", "#9bd1ff", "#5dade2", "#ff6b6b", "#9bd1ff", "#5dade2", "#9bd1ff", "#9bd1ff", "#f7dc6f", "#9bd1ff", "#ff6b6b", "#9bd1ff"]
    for (x, y), f in zip(atome, farben):
        inhalt += f'<circle cx="{x}" cy="{y}" r="13" fill="{f}" stroke="#0c1c2a" stroke-width="2"/>'
    inhalt += '<text x="420" y="350" font-family="Helvetica, Arial" font-size="26" fill="#fff" font-weight="700">Molekül 7</text>'
    # Bakterium
    inhalt += '<g transform="translate(260,150) rotate(-20)"><rect x="-60" y="-24" width="120" height="48" rx="24" fill="#7dcea0" opacity=".8" stroke="#2e8b57" stroke-width="3"/><rect x="-50" y="-16" width="100" height="32" rx="16" fill="none" stroke="#2e8b57" stroke-dasharray="4 4"/></g>'
    svg("05-basel", "Am Rhein entworfen: ein Molekül, das die doppelte Festung des Bakteriums überwindet.", inhalt, d)


def sprachen():
    d, hg = himmel("p6", "#e8eef5", "#ffffff")
    inhalt = hg
    # Bundeshaus-Kuppel
    inhalt += '<rect x="150" y="300" width="500" height="170" fill="#c9bfa8"/><rect x="330" y="230" width="140" height="70" fill="#d6ccb4"/>'
    inhalt += '<path d="M330,230 Q400,120 470,230 Z" fill="#4f7d6e"/><rect x="394" y="110" width="12" height="30" fill="#4f7d6e"/>'
    inhalt += "".join(f'<rect x="{170 + i * 40}" y="330" width="18" height="40" fill="#8a816e"/>' for i in range(12))
    inhalt += '<rect x="0" y="470" width="800" height="30" fill="#9a9180"/>'
    blasen = [(40, 60, "Grüezi", "#c8102e"), (230, 30, "Bonjour", "#1f5aa6"), (470, 30, "Buongiorno", "#2e8b57"), (640, 70, "Bun di", "#8e44ad")]
    for x, y, t, f in blasen:
        w = 26 + len(t) * 13
        inhalt += f'<g><rect x="{x}" y="{y}" width="{w}" height="50" rx="20" fill="{f}"/><path d="M{x + 30},{y + 50} l10,18 l8,-18 Z" fill="{f}"/>'
        inhalt += f'<text x="{x + w / 2}" y="{y + 33}" text-anchor="middle" font-family="Helvetica, Arial" font-size="22" fill="#fff" font-weight="700">{t}</text></g>'
    inhalt += '<path d="M160,200 C260,250 540,250 640,200" fill="none" stroke="#999" stroke-dasharray="3 6"/>'
    svg("06-sprachen", "Vier Landessprachen, ein Saal: zum ersten Mal auch Rumantsch in Echtzeit.", inhalt, d)


def abstimmung():
    r = random.Random(8)
    d, hg = himmel("p7", "#f3f3f3", "#dcdcdc")
    inhalt = hg
    # Urne
    inhalt += '<rect x="90" y="250" width="200" height="190" rx="10" fill="#c8102e"/><rect x="140" y="250" width="100" height="10" fill="#7a0a1c"/>'
    inhalt += '<path d="M190,300 l0,90 M145,345 l90,0" stroke="#fff" stroke-width="22"/>'
    inhalt += '<g transform="translate(150,170) rotate(-10)"><rect width="90" height="110" fill="#fff" stroke="#555"/><text x="12" y="30" font-family="Helvetica, Arial" font-size="16" fill="#333">Ja</text><rect x="54" y="16" width="18" height="18" fill="none" stroke="#333"/><path d="M57,25 l5,6 l10,-12" stroke="#c8102e" stroke-width="3" fill="none"/><text x="12" y="70" font-family="Helvetica, Arial" font-size="16" fill="#333">Nein</text><rect x="54" y="56" width="18" height="18" fill="none" stroke="#333"/></g>'
    # Videorahmen mit Lupe
    inhalt += '<rect x="360" y="90" width="380" height="240" rx="12" fill="#1c1c1c"/><rect x="376" y="106" width="348" height="196" fill="#3a3f47"/>'
    inhalt += '<circle cx="550" cy="180" r="40" fill="#c9a98a"/><rect x="505" y="220" width="90" height="82" rx="30" fill="#2f4f6f"/>'
    pts = " ".join(f"{380 + i * 6},{318 + 10 * math.sin(i * .7) * r.uniform(.2, 1):.0f}" for i in range(58))
    inhalt += f'<polyline points="{pts}" fill="none" stroke="#6bc1ff" stroke-width="2"/>'
    inhalt += '<g transform="translate(640,240)"><circle r="56" fill="#ffffff" opacity=".25" stroke="#f4c430" stroke-width="8"/><line x1="40" y1="40" x2="95" y2="95" stroke="#f4c430" stroke-width="14" stroke-linecap="round"/></g>'
    inhalt += '<text x="380" y="380" font-family="Helvetica, Arial" font-size="20" fill="#333">Fragen Sie, bis Sie keine Fragen mehr haben.</text>'
    svg("07-abstimmung", "Die Wache prüft das Video – und zeigt jedem, wie man selbst prüft.", inhalt, d)


def zahnrad(cx, cy, rad, zaehne, farbe, winkel=0):
    pts = []
    for i in range(zaehne * 2):
        a = math.pi * i / zaehne + winkel
        rr = rad if i % 2 == 0 else rad * 0.84
        a2 = a + math.pi / zaehne * 0.5
        pts += [(cx + rr * math.cos(a), cy + rr * math.sin(a)), (cx + rr * math.cos(a2), cy + rr * math.sin(a2))]
    d = "M" + " L".join(f"{x:.1f},{y:.1f}" for x, y in pts) + " Z"
    return f'<path d="{d}" fill="{farbe}" stroke="#5a4416" stroke-width="1.5"/><circle cx="{cx}" cy="{cy}" r="{rad * .25:.0f}" fill="#2b1f0a"/>'


def uhrwerk():
    r = random.Random(9)
    d, hg = himmel("p8", "#dfe6ea", "#f7f4ec")
    inhalt = hg
    inhalt += f'<path d="{berge(r, 300, 70, 3, glatt=.2)}" fill="#7d9a7a"/>'
    inhalt += f'<path d="{berge(r, 340, 50, 4, glatt=.2)}" fill="#5c7a5c"/>'
    inhalt += '<ellipse cx="400" cy="345" rx="160" ry="10" fill="#7fb0c9"/>'
    inhalt += zahnrad(300, 380, 90, 18, "#d9b44a", 0.1) + zahnrad(440, 330, 60, 12, "#e3c46a", 0.3) + zahnrad(540, 410, 70, 14, "#cfa43c", 0.05) + zahnrad(640, 320, 40, 10, "#e8cf7d")
    # Unruh-Spirale
    sp = " ".join(f"{180 + (4 + t * 1.2) * math.cos(t):.1f},{230 + (4 + t * 1.2) * math.sin(t):.1f}" for t in [i * 0.2 for i in range(160)])
    inhalt += f'<polyline points="{sp}" fill="none" stroke="#3d6e9e" stroke-width="1.5"/><circle cx="180" cy="230" r="46" fill="none" stroke="#7a5a1a" stroke-width="5"/>'
    # Tonfeder und Hammer
    inhalt += '<path d="M600,150 A120,120 0 0 1 740,260" fill="none" stroke="#555" stroke-width="3"/><rect x="580" y="140" width="40" height="10" fill="#333" transform="rotate(-25 600 145)"/>'
    inhalt += "".join(f'<path d="M{690 + i * 14},{170 + i * 10} q8,-8 16,0" fill="none" stroke="#c0392b" opacity="{.8 - i * .2}"/>' for i in range(4))
    svg("08-uhrwerk", "Räder, Unruh, Tonfeder: das Gedächtnis der Hände im Vallée de Joux.", inhalt, d)


def alp():
    r = random.Random(10)
    d, hg = himmel("p9", "#0d1a2c", "#2b4766")
    inhalt = hg + sterne(r, 80, 220)
    inhalt += '<path d="M600,60 a26,26 0 1 0 0.1,0 Z" fill="#f3eed8"/><circle cx="612" cy="56" r="24" fill="#0d1a2c"/>'
    inhalt += f'<path d="{berge(r, 260, 160, 4)}" fill="#3b4f66"/>'
    inhalt += f'<path d="{berge(r, 370, 70, 6, glatt=.3)}" fill="#24384a"/>'
    inhalt += '<ellipse cx="250" cy="400" rx="90" ry="12" fill="#3a5f7d" opacity=".8"/><ellipse cx="560" cy="430" rx="60" ry="8" fill="#3a5f7d" opacity=".8"/>'
    # Schafe
    for _ in range(34):
        x, y = r.uniform(320, 520), r.uniform(390, 470)
        inhalt += f'<ellipse cx="{x:.0f}" cy="{y:.0f}" rx="9" ry="6" fill="#e8e4d8"/><circle cx="{x + 9:.0f}" cy="{y - 2:.0f}" r="3" fill="#333"/>'
    # Wolf (Waermebild)
    inhalt += '<g transform="translate(650,410)"><path d="M0,0 C10,-12 40,-14 55,-8 L70,-20 L72,-6 C78,0 75,8 66,8 L60,20 L54,20 L52,10 L18,10 L14,22 L8,22 L8,8 C2,6 -8,4 -14,-4 Z" fill="#ff8c42"/></g>'
    inhalt += '<circle cx="685" cy="405" r="48" fill="none" stroke="#ff8c42" stroke-dasharray="6 5"/>'
    # Drohne mit Lichtkegel
    inhalt += '<path d="M640,250 L600,390 L760,390 Z" fill="#fff6c2" opacity=".18"/><g transform="translate(680,245)"><rect x="-18" y="-4" width="36" height="8" rx="3" fill="#ddd"/><line x1="-30" y1="-8" x2="-6" y2="-8" stroke="#ddd" stroke-width="2"/><line x1="6" y1="-8" x2="30" y2="-8" stroke="#ddd" stroke-width="2"/><circle cx="0" cy="6" r="3" fill="#ffef8a"/></g>'
    # Hund
    inhalt += '<path d="M290,450 c8,-14 34,-14 44,-6 l8,-8 l3,10 c4,6 0,12 -8,12 l-2,12 h-5 l-1,-8 h-26 l-2,8 h-5 l0,-10 c-6,-2 -8,-6 -6,-10 Z" fill="#f4f1e8"/>'
    svg("09-alp", "Nacht auf der Alp Flix: Wärmebild, Drohnenlicht – und M41 im Bachtobel.", inhalt, d)


def gotthard():
    d, hg = himmel("p10", "#2a2420", "#4a3c33")
    inhalt = hg
    # Tunnelquerschnitt
    inhalt += '<path d="M120,470 L120,230 A280,200 0 0 1 680,230 L680,470 Z" fill="#6d6259"/>'
    inhalt += '<path d="M160,470 L160,240 A240,170 0 0 1 640,240 L640,470 Z" fill="#9b8f84"/>'
    inhalt += "".join(f'<line x1="{170 + i * 30}" y1="{100 + abs(8 - i) * 12}" x2="{170 + i * 30}" y2="{110 + abs(8 - i) * 12}" stroke="#ffd166" stroke-width="3"/>' for i in range(16))
    # Gleise in Perspektive
    inhalt += '<path d="M300,470 L390,300 M500,470 L410,300" stroke="#555" stroke-width="6"/>'
    for i in range(8):
        y = 300 + i * 22
        k = (y - 300) / 170
        inhalt += f'<line x1="{390 - 90 * k - 20}" y1="{y}" x2="{410 + 90 * k + 20}" y2="{y}" stroke="#6b5440" stroke-width="{3 + 4 * k:.1f}"/>'
    # Weiche mit Riss
    inhalt += '<path d="M430,420 L520,360" stroke="#555" stroke-width="5"/><circle cx="470" cy="400" r="18" fill="none" stroke="#e0453a" stroke-width="3"/><path d="M464,394 l5,6 l-3,4 l6,4" stroke="#e0453a" stroke-width="2" fill="none"/>'
    # Schallwellen / Zuhoerer
    inhalt += "".join(f'<path d="M560,{300 - i * 8} q{30 + i * 12},{40 + i * 10} 0,{80 + i * 16}" fill="none" stroke="#7fd1ff" stroke-width="2" opacity="{.9 - i * .2}"/>' for i in range(4))
    inhalt += '<text x="40" y="60" font-family="Helvetica, Arial" font-size="20" fill="#f5e6d3">km 22 · MFS Faido · 28 °C</text>'
    svg("10-gotthard", "Der Zuhörer hört die Weiche 34, bevor sie bricht.", inhalt, d)


def genf():
    r = random.Random(12)
    d, hg = himmel("p11", "#1a1430", "#3a2d5c")
    inhalt = hg
    # Kuppel mit Tropfsteinen (Saal XX)
    farben = ["#e74c3c", "#f39c12", "#27ae60", "#2980b9", "#8e44ad", "#16a085", "#d35400"]
    for i in range(60):
        x = 10 + i * 13.3
        lang = r.uniform(40, 170)
        f = farben[i % len(farben)]
        inhalt += f'<path d="M{x - 8:.0f},0 Q{x:.0f},{lang:.0f} {x + 8:.0f},0 Z" fill="{f}" opacity=".85"/>'
    # Jet d'eau und See
    inhalt += '<rect x="0" y="400" width="800" height="100" fill="#1d3557"/><path d="M600,400 C596,300 598,230 600,180 C602,230 604,300 600,400 Z" fill="#e8f4ff" opacity=".9"/>'
    # fuenf Schluessel, drei leuchtend
    for i in range(5):
        x = 120 + i * 90
        leuchtet = i in (0, 2, 4)
        f = "#f4c430" if leuchtet else "#6c6c7a"
        inhalt += f'<g transform="translate({x},300)"><circle r="18" fill="none" stroke="{f}" stroke-width="7"/><rect x="16" y="-4" width="44" height="8" fill="{f}"/><rect x="46" y="4" width="6" height="12" fill="{f}"/><rect x="34" y="4" width="6" height="9" fill="{f}"/></g>'
    inhalt += '<text x="120" y="370" font-family="Helvetica, Arial" font-size="18" fill="#f5f0ff">drei von fünf Schlüsseln</text>'
    svg("11-genf", "Unter der Farbkuppel von Saal XX: ein Notschalter, den keiner allein drücken kann.", inhalt, d)


def schule():
    d, hg = himmel("p12", "#fdf6e3", "#f3e5c0")
    inhalt = hg
    # offenes Buch
    inhalt += '<path d="M120,380 Q260,330 400,380 L400,170 Q260,120 120,170 Z" fill="#ffffff" stroke="#8a6d3b" stroke-width="3"/>'
    inhalt += '<path d="M680,380 Q540,330 400,380 L400,170 Q540,120 680,170 Z" fill="#ffffff" stroke="#8a6d3b" stroke-width="3"/>'
    for i in range(8):
        inhalt += f'<line x1="150" y1="{195 + i * 20}" x2="370" y2="{195 + i * 20}" stroke="#b8a888"/>'
    inhalt += '<text x="155" y="200" font-family="Georgia, serif" font-size="17" fill="#333">Der Torhüter von Asmara</text>'
    # Tor und Ball auf rechter Seite
    inhalt += '<rect x="460" y="210" width="170" height="90" fill="none" stroke="#2e7d32" stroke-width="5"/>'
    inhalt += "".join(f'<line x1="{470 + i * 16}" y1="212" x2="{470 + i * 16}" y2="298" stroke="#a5d6a7"/>' for i in range(10))
    inhalt += '<circle cx="545" cy="330" r="20" fill="#fff" stroke="#222" stroke-width="2"/><path d="M545,318 l9,7 l-4,10 h-10 l-4,-10 Z" fill="#222"/>'
    # Tablet mit Wellen (Zuhoeren)
    inhalt += '<rect x="580" y="390" width="160" height="100" rx="12" fill="#263238"/><rect x="592" y="400" width="136" height="80" rx="4" fill="#4fc3f7"/>'
    inhalt += "".join(f'<rect x="{606 + i * 12}" y="{440 - abs(5 - i) * 4 - (i % 3) * 5}" width="6" height="{abs(5 - i) * 8 + 8}" fill="#fff"/>' for i in range(10))
    inhalt += '<text x="60" y="460" font-family="Helvetica, Arial" font-size="18" fill="#6d4c41">Für Herrn Lehmann, der mich hat vorlesen lassen.</text>'
    svg("12-schule", "Ein Buch, Kapitel für Kapitel geschrieben für einen Jungen, der Torhüter werden wollte.", inhalt, d)


def cern():
    r = random.Random(14)
    d, hg = himmel("p13", "#05070f", "#111a33")
    inhalt = hg + sterne(r, 60, 500, "#9ab")
    inhalt += '<ellipse cx="400" cy="380" rx="360" ry="90" fill="none" stroke="#2f6db5" stroke-width="6" opacity=".7"/>'
    inhalt += '<ellipse cx="400" cy="380" rx="360" ry="90" fill="none" stroke="#9fd0ff" stroke-width="1.5" stroke-dasharray="3 10"/>'
    # Kollisionspunkt und Spuren
    cx, cy = 400, 220
    inhalt += f'<circle cx="{cx}" cy="{cy}" r="120" fill="none" stroke="#334" stroke-width="1"/><circle cx="{cx}" cy="{cy}" r="70" fill="none" stroke="#334"/>'
    for _ in range(40):
        a = r.uniform(0, 6.28)
        l = r.uniform(60, 180)
        krumm = r.uniform(-60, 60)
        x2, y2 = cx + l * math.cos(a), cy + l * math.sin(a)
        inhalt += f'<path d="M{cx},{cy} Q{cx + l / 2 * math.cos(a) + krumm * math.sin(a):.0f},{cy + l / 2 * math.sin(a) - krumm * math.cos(a):.0f} {x2:.0f},{y2:.0f}" fill="none" stroke="#6fa8dc" stroke-width="1" opacity=".6"/>'
    # die zwei verzoegerten Bündel (gelb)
    for a in (0.4, 0.4 + math.pi):
        sx, sy = cx + 30 * math.cos(a), cy + 30 * math.sin(a)
        inhalt += f'<line x1="{cx}" y1="{cy}" x2="{sx:.0f}" y2="{sy:.0f}" stroke="#f4c430" stroke-width="2" stroke-dasharray="3 3"/><circle cx="{sx:.0f}" cy="{sy:.0f}" r="4" fill="#f4c430"/>'
        for da in (-0.25, 0, 0.22):
            inhalt += f'<line x1="{sx:.0f}" y1="{sy:.0f}" x2="{sx + 160 * math.cos(a + da):.0f}" y2="{sy + 160 * math.sin(a + da):.0f}" stroke="#f4c430" stroke-width="2.5"/>'
    inhalt += f'<circle cx="{cx}" cy="{cy}" r="6" fill="#fff"/>'
    inhalt += '<text x="560" y="60" font-family="Helvetica, Arial" font-size="22" fill="#f4c430" font-weight="700">X(19)?</text>'
    svg("13-cern", "Kollision 4 117 902 553 286: Zwei Bündel, die ein paar Millimeter zu spät beginnen.", inhalt, d)


def postauto():
    r = random.Random(15)
    d, hg = himmel("p14", "#c9d3dd", "#eef2f5")
    inhalt = hg
    inhalt += f'<path d="{berge(r, 260, 180, 4)}" fill="#ffffff" stroke="#c3cdd6"/>'
    inhalt += f'<path d="{berge(r, 360, 90, 5)}" fill="#e4eaf0"/>'
    # Strasse in Kurve
    inhalt += '<path d="M-20,480 C200,470 300,400 420,380 C560,360 640,330 820,320" fill="none" stroke="#9aa4ad" stroke-width="60"/>'
    inhalt += '<path d="M-20,480 C200,470 300,400 420,380 C560,360 640,330 820,320" fill="none" stroke="#f4f6f8" stroke-width="2" stroke-dasharray="14 18"/>'
    # Lastwagen quer
    inhalt += '<g transform="translate(560,300) rotate(-12)"><rect width="150" height="44" fill="#7f8c8d"/><rect x="150" y="6" width="40" height="38" fill="#c0392b"/></g>'
    # Postauto
    inhalt += '<g transform="translate(200,380) rotate(-8)"><rect width="190" height="62" rx="10" fill="#ffcc00" stroke="#9c7c00" stroke-width="2"/>'
    inhalt += "".join(f'<rect x="{14 + i * 34}" y="10" width="26" height="22" rx="3" fill="#2c3e50"/>' for i in range(5))
    inhalt += '<rect x="0" y="40" width="190" height="6" fill="#c8102e"/><circle cx="40" cy="64" r="12" fill="#222"/><circle cx="150" cy="64" r="12" fill="#222"/>'
    inhalt += '<circle cx="188" cy="48" r="5" fill="#ffa500"/></g>'
    # Radarboegen
    inhalt += "".join(f'<path d="M400,{340 - i * 6} q{50 + i * 25},{-20 - i * 6} {100 + i * 50},{-10 - i * 4}" fill="none" stroke="#2e86de" stroke-width="2" opacity="{.8 - i * .2}"/>' for i in range(4))
    inhalt += schnee(r, 160)
    svg("14-postauto", "Oberhalb von Hinterrhein: Das Postauto hält dreissig Meter vor dem Lastwagen.", inhalt, d)


def schreinerei():
    r = random.Random(16)
    d, hg = himmel("p15", "#d4e6f1", "#fbf5e9")
    inhalt = hg
    # Alpstein mit Saentis
    inhalt += '<path d="M0,260 L120,200 L200,230 L300,150 L360,175 L420,110 L470,160 L560,140 L640,190 L800,170 L800,300 L0,300 Z" fill="#9aa5ae"/>'
    inhalt += '<rect x="416" y="80" width="6" height="32" fill="#555"/>'
    inhalt += '<path d="M0,320 C200,280 600,300 800,280 L800,500 L0,500 Z" fill="#7fae5e"/>'
    # Bretter (Holzmaserung)
    for i in range(5):
        y = 340 + i * 30
        inhalt += f'<rect x="80" y="{y}" width="420" height="24" rx="3" fill="#{["c69c6d", "b5835a", "d2a679", "a9744f", "c08b5c"][i]}" stroke="#7a5230"/>'
        inhalt += "".join(f'<path d="M{100 + j * 60},{y + 12} q15,{r.choice([-6, 6])} 30,0" fill="none" stroke="#7a5230" opacity=".5"/>' for j in range(6))
    # Offerte mit Haken
    inhalt += '<g transform="translate(560,320) rotate(5)"><rect width="160" height="150" fill="#fff" stroke="#555"/><text x="14" y="28" font-family="Helvetica, Arial" font-size="16" fill="#333" font-weight="700">Offerte</text>'
    inhalt += "".join(f'<line x1="14" y1="{46 + i * 14}" x2="{140 - (i % 3) * 20}" y2="{46 + i * 14}" stroke="#aaa"/>' for i in range(6))
    inhalt += '<path d="M100,120 l14,14 l30,-34" stroke="#2e8b57" stroke-width="6" fill="none"/></g>'
    # Hobel
    inhalt += '<g transform="translate(300,300)"><path d="M0,30 L20,0 L140,0 L150,30 Z" fill="#8b5a2b"/><rect x="60" y="-24" width="24" height="26" rx="6" fill="#6b3f1d"/></g>'
    svg("15-schreinerei", "Vor dem Alpstein: Holz für die Werkstatt, Papier für den Assistenten.", inhalt, d)


def pflege():
    r = random.Random(17)
    d, hg = himmel("p16", "#f6d9b8", "#fff4e6")
    inhalt = hg
    inhalt += '<path d="M0,300 C150,250 300,280 450,240 C600,200 700,250 800,230 L800,500 L0,500 Z" fill="#8fbf6a"/>'
    inhalt += '<path d="M0,360 C200,320 400,350 800,320 L800,500 L0,500 Z" fill="#76a955"/>'
    # Emmentaler Bauernhaus mit grossem Dach
    inhalt += '<path d="M220,330 L400,200 L580,330 Z" fill="#6b4226"/><path d="M200,340 L400,190 L600,340" fill="none" stroke="#4a2c18" stroke-width="8"/>'
    inhalt += '<rect x="260" y="330" width="280" height="130" fill="#d9b88f"/>'
    inhalt += "".join(f'<rect x="{280 + i * 48}" y="350" width="30" height="34" fill="#fff8d6" stroke="#7a5a3a" stroke-width="3"/>' for i in range(5))
    inhalt += "".join(f'<rect x="{282 + i * 48}" y="388" width="26" height="8" fill="#c0392b"/>' for i in range(5))
    # Kirschbaeume
    for x in (100, 680, 740):
        inhalt += f'<rect x="{x - 4}" y="380" width="8" height="60" fill="#5d4037"/>'
        inhalt += "".join(f'<circle cx="{x + r.uniform(-30, 30):.0f}" cy="{370 + r.uniform(-30, 10):.0f}" r="{r.uniform(10, 18):.0f}" fill="#fbe4ee" opacity=".9"/>' for _ in range(9))
    # Lautsprecher mit Wellen im Fenster
    inhalt += '<rect x="380" y="410" width="40" height="30" rx="8" fill="#455a64"/>'
    inhalt += "".join(f'<path d="M424,{420 - i * 4} q{8 + i * 6},{5 + i * 4} 0,{10 + i * 8}" fill="none" stroke="#455a64" stroke-width="2"/>' for i in range(3))
    svg("16-pflege", "Das Bauernhaus oberhalb von Langnau: vier geschenkte Jahre daheim.", inhalt, d)


def epilog():
    r = random.Random(18)
    d, hg = himmel("p17", "#22304f", "#56657f")
    inhalt = hg
    inhalt += f'<path d="{berge(r, 300, 180, 4)}" fill="#7f8ea8"/>'
    for i in range(14):
        x = 30 + i * 54
        inhalt += haus(x, 470, 0.95 + (i % 2) * 0.2, wand="#d8cdbb", dach="#ffffff", fenster="#ffcf6b")
    inhalt += '<rect x="0" y="470" width="800" height="30" fill="#e9eef5"/>'
    # Postkarte
    inhalt += '<g transform="translate(470,90) rotate(6)"><rect width="250" height="160" fill="#fffaf0" stroke="#8a7a66" stroke-width="2"/><line x1="140" y1="12" x2="140" y2="148" stroke="#ccc"/>'
    inhalt += '<rect x="196" y="12" width="40" height="46" fill="#c8102e"/><path d="M216,22 v26 M203,35 h26" stroke="#fff" stroke-width="7"/>'
    inhalt += "".join(f'<path d="M14,{40 + i * 18} q30,-6 60,0 t60,0" fill="none" stroke="#5a6b8a" stroke-width="1.5"/>' for i in range(6))
    inhalt += '<text x="14" y="28" font-family="Georgia, serif" font-size="14" font-style="italic" fill="#3a4a6a">Liebe Frau Caflisch …</text></g>'
    inhalt += schnee(r, 140)
    svg("17-epilog", "Schnee über Chur, die Art, vor der man Respekt hat, aber keine Angst.", inhalt, d)


def umschlag():
    r = random.Random(42)
    W2, H2 = 1600, 2400
    defs = ('<linearGradient id="uh" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="#0b1630"/><stop offset=".55" stop-color="#23406e"/>'
            '<stop offset="1" stop-color="#f0b27a"/></linearGradient>')
    inhalt = f'<rect width="{W2}" height="{H2}" fill="url(#uh)"/>'
    inhalt += "".join(f'<circle cx="{r.uniform(0, W2):.0f}" cy="{r.uniform(0, 1200):.0f}" r="{r.choice([1.5, 2, 3])}" fill="#fff" opacity="{r.uniform(.3, .9):.2f}"/>' for _ in range(220))
    # Netz-Sternbild in Form des Matterhorns
    knoten = [(800, 980), (700, 1180), (620, 1340), (520, 1500), (900, 1150), (990, 1300), (1080, 1480), (760, 1080), (860, 1260), (700, 1400), (960, 1420)]
    kanten = [(0, 7), (7, 1), (1, 2), (2, 3), (0, 4), (4, 5), (5, 6), (7, 8), (8, 5), (1, 8), (2, 9), (9, 8), (8, 10), (10, 6), (9, 3)]
    for a, b in kanten:
        (x1, y1), (x2, y2) = knoten[a], knoten[b]
        inhalt += f'<line x1="{x1}" y1="{y1}" x2="{x2}" y2="{y2}" stroke="#9fd8ff" stroke-width="3" opacity=".8"/>'
    inhalt += "".join(f'<circle cx="{x}" cy="{y}" r="10" fill="#fff"/><circle cx="{x}" cy="{y}" r="22" fill="#9fd8ff" opacity=".25"/>' for x, y in knoten)
    # Bergkette
    punkte = []
    for i in range(121):
        x = W2 * i / 120
        y = 1650 - 260 * math.exp(-((x - 800) / 260) ** 2) - 120 * (0.6 + 0.4 * math.sin(i * .37)) - r.uniform(0, 30)
        punkte.append(f"L{x:.0f},{y:.0f}")
    inhalt += f'<path d="M0,{H2} {" ".join(punkte)} L{W2},{H2} Z" fill="#162544"/>'
    punkte = []
    for i in range(121):
        x = W2 * i / 120
        y = 1850 - 140 * (0.5 + 0.5 * math.sin(i * .21 + 1)) - r.uniform(0, 25)
        punkte.append(f"L{x:.0f},{y:.0f}")
    inhalt += f'<path d="M0,{H2} {" ".join(punkte)} L{W2},{H2} Z" fill="#0d1830"/>'
    inhalt += '<rect x="740" y="2060" width="120" height="120" fill="#c8102e"/><path d="M800,2082 v76 M762,2120 h76" stroke="#fff" stroke-width="24"/>'
    inhalt += ('<text x="800" y="360" text-anchor="middle" font-family="Avenir Next, Helvetica Neue, Arial" font-size="150" font-weight="700" fill="#ffffff">Die Schweiz</text>'
               '<text x="800" y="530" text-anchor="middle" font-family="Avenir Next, Helvetica Neue, Arial" font-size="150" font-weight="700" fill="#9fd8ff">denkt mit</text>'
               '<text x="800" y="660" text-anchor="middle" font-family="Georgia, serif" font-size="54" font-style="italic" fill="#e8eef7">Geschichten aus der Zukunft</text>'
               '<text x="800" y="730" text-anchor="middle" font-family="Georgia, serif" font-size="54" font-style="italic" fill="#e8eef7">der künstlichen Intelligenz</text>'
               '<text x="800" y="810" text-anchor="middle" font-family="Avenir Next, Helvetica Neue, Arial" font-size="44" letter-spacing="8" fill="#f0b27a">2027 – 2045</text>'
               '<text x="800" y="2300" text-anchor="middle" font-family="Avenir Next, Helvetica Neue, Arial" font-size="48" letter-spacing="14" fill="#e8eef7">AISCOM</text>')
    s = f'''<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 {W2} {H2}" width="{W2}" height="{H2}" role="img" aria-label="Umschlag: Die Schweiz denkt mit">
<title>Die Schweiz denkt mit</title>
<defs>{defs}</defs>
{inhalt}
</svg>
'''
    (ZIEL / "umschlag.svg").write_text(s, encoding="utf-8")


if __name__ == "__main__":
    ZIEL.mkdir(exist_ok=True)
    for f in (prolog, lawine, gletscher, strom, aerztin, basel, sprachen, abstimmung, uhrwerk, alp, gotthard, genf, schule, cern, postauto, schreinerei, pflege, epilog, umschlag):
        f()
    print(len(list(ZIEL.glob("*.svg"))), "Bilder")
