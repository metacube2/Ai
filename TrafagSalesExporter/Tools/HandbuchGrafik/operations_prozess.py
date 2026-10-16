"""Prozessgrafik Operations fuer das Benutzerhandbuch (Wunsch Ingo 2026-10-16), im Trafag-CI.

Erzeugt wwwroot/handbuch/bilder/operations_prozess.svg (Reiter Handbuch). Die PNG-Fassung fuer Word erzeugt
render_png.ps1 daraus mit Edge headless. Aufruf aus dem Repo-Ordner:
    python -X utf8 Tools/HandbuchGrafik/operations_prozess.py
CI: Orange #C8501E, dunkel #B5481B, Schrift Open Sans, Logo wwwroot/trafag.jpg (eingebettet, weil ein SVG im
<img>-Tag keine externen Dateien laedt).
"""
import base64
import os

OR, DK, GR, TX, BG, LI = "#C8501E", "#B5481B", "#8A8A8A", "#3C3C3C", "#FBF7F5", "#F3E6E0"
W, H = 1400, 880
root = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
logo = base64.b64encode(open(os.path.join(root, "wwwroot", "trafag.jpg"), "rb").read()).decode()
o = []


def esc(s):
    return s.replace("&", "&amp;").replace("<", "&lt;").replace(">", "&gt;")


def box(x, y, w, h, title, lines=(), fill="#FFFFFF", stroke=OR, tcol=TX, sw=1.6, r=8, tsize=15):
    o.append(f'<rect x="{x}" y="{y}" width="{w}" height="{h}" rx="{r}" fill="{fill}" stroke="{stroke}" stroke-width="{sw}"/>')
    o.append(f'<text x="{x + 12}" y="{y + 22}" font-size="{tsize}" font-weight="700" fill="{tcol}">{esc(title)}</text>')
    for i, l in enumerate(lines):
        o.append(f'<text x="{x + 12}" y="{y + 41 + i * 17}" font-size="12.5" fill="{tcol if tcol != TX else "#555"}">{esc(l)}</text>')


def arrow(x1, y1, x2, y2, col=OR, label=None, lx=None, ly=None, dash=False):
    d = ' stroke-dasharray="6 4"' if dash else ""
    o.append(f'<line x1="{x1}" y1="{y1}" x2="{x2}" y2="{y2}" stroke="{col}" stroke-width="2"{d} marker-end="url(#pf{"g" if col == GR else ""})"/>')
    if label:
        o.append(f'<text x="{lx}" y="{ly}" font-size="11.5" fill="{col}" font-weight="600">{esc(label)}</text>')


def head(x, y, t):
    o.append(f'<text x="{x}" y="{y}" font-size="13" font-weight="700" fill="{DK}" letter-spacing="1.5">{esc(t.upper())}</text>')
    o.append(f'<line x1="{x}" y1="{y + 7}" x2="{x + 60}" y2="{y + 7}" stroke="{OR}" stroke-width="3"/>')


o.append(f'<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 {W} {H}" width="{W}" height="{H}" font-family="Open Sans, Segoe UI, Arial, sans-serif">')
o.append(f'<defs><marker id="pf" viewBox="0 0 10 10" refX="9" refY="5" markerWidth="7" markerHeight="7" orient="auto"><path d="M0,0 L10,5 L0,10 z" fill="{OR}"/></marker>'
         f'<marker id="pfg" viewBox="0 0 10 10" refX="9" refY="5" markerWidth="7" markerHeight="7" orient="auto"><path d="M0,0 L10,5 L0,10 z" fill="{GR}"/></marker></defs>')
o.append(f'<rect width="{W}" height="{H}" fill="#FFFFFF"/>')
# Kopf
o.append(f'<rect x="0" y="0" width="{W}" height="74" fill="{OR}"/>')
o.append(f'<text x="30" y="46" font-size="26" font-weight="700" fill="#FFFFFF">Operations: Prozess und Datenfluss</text>')
o.append(f'<text x="560" y="45" font-size="14" fill="#FFFFFF">Shopfloor-Board, SAP und tägliches Shop-Floor-Meeting</text>')
o.append(f'<rect x="{W - 150}" y="12" width="124" height="50" rx="6" fill="#FFFFFF"/>')
o.append(f'<image x="{W - 142}" y="14" width="108" height="46" href="data:image/jpeg;base64,{logo}"/>')

# Spalte 1: Quellen
head(30, 112, "1  Quellen")
src = [
    ("SAP: Fehlteile ZD05", ["Unterdeckung mit Umterminierung", "Disponent 001, 003 · werktags"], "#FFFFFF", OR),
    ("SAP: Plan- und Fertigungsaufträge", ["offene Menge je Eckendtermin", "nächste 28 Tage → Forecast"], "#FFFFFF", OR),
    ("SAP: Rückmeldungen (Logik ZC27)", ["Gutmenge nach Erfassungsdatum", "Logistik und Tagesmenge TR5"], "#FFFFFF", OR),
    ("SAP: Fertigungsaufträge Istende", ["WE-Menge und Gesamtmenge", "Tagesmenge und FPY SEH"], "#FFFFFF", OR),
    ("SAP: Liefertreue (monatlich)", ["Aufträge, Lieferungen, Warenausgang", "ab dem 2. des Monats"], "#FFFFFF", OR),
    ("MES", ["FPY TR5, TX, DW, CZ"], "#FFFFFF", GR),
    ("Von Hand im Board", ["Teamleiter, PPD, Einkauf, Verkauf,", "Logistik: Ziele, Personal, Ampel"], LI, DK),
]
for i, (t, ls, f, s) in enumerate(src):
    y = 130 + i * 84
    box(30, y, 360, 72, t, ls, fill=f, stroke=s)
    arrow(390, y + 36, 448, 430, col=GR if s == GR else OR)

# Spalte 2: Cockpit
head(460, 112, "2  Cockpit Operations")
o.append(f'<rect x="450" y="124" width="500" height="594" rx="10" fill="{BG}" stroke="{OR}" stroke-width="1.6"/>')
o.append(f'<rect x="465" y="138" width="470" height="44" rx="6" fill="{OR}"/>')
o.append(f'<text x="480" y="158" font-size="14" font-weight="700" fill="#FFFFFF">SAP-Abgleich werktags 04:00 und 12:45 Uhr</text>')
o.append(f'<text x="480" y="175" font-size="12" fill="#FFFFFF">Eintrag vom Tag D = Stand bis D-1 · Montag: Fr bis So</text>')
c1, c2, cw = 465, 712, 223
box(c1, 198, cw, 84, "Abteilungen", ["SEH · TR5 · TX · DW", "Vorrat, Tagesmenge, Ziel,", "Personal, FPY"])
box(c2, 198, cw, 84, "PPD", ["Menge in Verzug je Abteilung", "Terminverschiebungen", "Prio-Liste Fehlmaterial"])
box(c1, 298, cw, 70, "Ampel und Lieferzeiten", ["Status je Bereich, Erfüllung %", "Lieferzeit gegen Ziel, Grund"])
box(c2, 298, cw, 70, "Logistik", ["verpackt (Ist) je Arbeitsplatz", "gegen Tagesmenge (Soll)"])
box(c1, 384, cw, 70, "Einkauf ZD05", ["Zu besprechen, Code, Termin", "Bewertungskennzahl ≤ 1,20"])
box(c2, 384, cw, 70, "Verkauf", ["Auftragseingang, Umsatz,", "Go-for-it, Book to Bill"])
box(c1, 470, cw, 70, "Forecast und PPA-Grafik", ["Menge gegen Kapazität", "Auslastung 85 bis 105 %"])
box(c2, 470, cw, 70, "Listen", ["Pendenzen, Rückstände, TVAV,", "Angebote, CZ ↔ CH, Einkauf"])
box(c1, 556, 470, 58, "KPI Liefertreue: OTD · OTIF · OTP", ["monatlich, Abweichungsliste nach Kunde und Material"])
box(c1, 630, 470, 72, "Startseite: Ablauf des Meetings", ["Pflichtpunkte erfasst ja oder nein, Ampelpunkte je Bereich,", "offene und überfällige Pendenzen, Rückstände, Kennzahl ZD05"],
    fill=LI, stroke=DK)
# Verbindungen innerhalb des Cockpits
arrow(c2, 240, c1 + cw + 2, 240)
arrow(c1 + cw - 6, 282, c2 + 14, 298)
o.append(f'<text x="{c1 + cw + 4}" y="294" font-size="10.5" fill="{OR}" font-weight="600">Soll</text>')
arrow(c1 + 40, 282, c1 + 40, 298)
o.append(f'<path d="M {c1} 262 L {c1 - 8} 262 L {c1 - 8} 505 L {c1 - 2} 505" fill="none" stroke="{OR}" stroke-width="2" stroke-dasharray="6 4" marker-end="url(#pf)"/>')
o.append(f'<text x="{c1 - 6}" y="380" font-size="10.5" fill="{OR}" font-weight="600" transform="rotate(-90 {c1 - 12} 380)">Leistung</text>')

# Spalte 3: Meeting und Wirkung
head(1010, 112, "3  Meeting und Wirkung")
mx, mw = 1000, 370
box(mx, 130, mw, 92, "Tägliches Shop-Floor-Meeting", ["10 bis 20 Minuten, Ablauf von oben nach unten", "Freitag erweitert: CZ, MTF, GFS, Q, IT"], fill=DK, stroke=DK, tcol="#FFFFFF", tsize=16)
box(mx, 246, mw, 78, "Abweichungen besprechen", ["rote und orange Ampeln, ZD05 „Zu besprechen“,", "überfällige Pendenzen und Rückstände"])
box(mx, 348, mw, 78, "Massnahmen festhalten", ["Pendenz mit Termin, neuer Liefertermin,", "Terminanfragen beantworten"])
box(mx, 450, mw, 78, "Umsetzen", ["Personal verschieben, Schicht öffnen,", "Bestellung vorziehen, Termin in SAP"])
box(mx, 552, mw, 78, "Wirkung prüfen", ["Tageswerte am nächsten Morgen,", "Liefertreue im Monatsbericht"])
for y in (222, 324, 426, 528):
    arrow(mx + mw / 2, y, mx + mw / 2, y + 22)
arrow(950, 176, 998, 176, label="Board", lx=952, ly=168)
# Rueckkopplung nach SAP
o.append(f'<path d="M {mx + mw / 2} 630 L {mx + mw / 2} 790 L 210 790 L 210 718" fill="none" stroke="{DK}" stroke-width="2.2" stroke-dasharray="8 5" marker-end="url(#pf)"/>')
o.append(f'<text x="470" y="782" font-size="13" font-weight="600" fill="{DK}">Änderungen in SAP (Termine, Bestellungen, Rückmeldungen) fliessen in den nächsten Abgleich</text>')
# Fuss
o.append(f'<line x1="30" y1="826" x2="{W - 30}" y2="826" stroke="{OR}" stroke-width="2"/>')
o.append(f'<text x="30" y="852" font-size="12.5" fill="{TX}">Orange: aus SAP · Grau: MES · Hell hinterlegt: von Hand erfasst. Werte von Hand überschreibt die SAP-Lieferung nicht.</text>')
o.append(f'<text x="{W - 30}" y="852" font-size="12.5" fill="{GR}" text-anchor="end">Trafag AG · Stand 16.10.2026</text>')
o.append('</svg>')

out = os.path.join(root, "wwwroot", "handbuch", "bilder")
os.makedirs(out, exist_ok=True)
open(os.path.join(out, "operations_prozess.svg"), "w", encoding="utf-8").write("\n".join(o))
print("ok", os.path.join(out, "operations_prozess.svg"))
