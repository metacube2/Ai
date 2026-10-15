"""Themenlandkarte Ingo als SVG. Aufruf aus dem Projektordner: python projektmanagement/themenlandkarte.py docs/THEMENLANDKARTE_<datum>.svg
Inhalte (Quellen, Themen, Personen, Abhaengigkeiten) oben in den Listen pflegen; Stand im Titel anpassen."""
import sys
from html import escape

W, H = 1960, 1110
out = []
A = out.append


def t(x, y, s, size=13, weight=400, fill="#1f2937", anchor="start", italic=False):
    st = ' font-style="italic"' if italic else ""
    A(f'<text x="{x}" y="{y}" font-size="{size}" font-weight="{weight}" fill="{fill}" text-anchor="{anchor}"{st}>{escape(s)}</text>')


def box(x, y, w, h, fill="#fff", stroke="#d1d5db", r=10, sw=1.2, extra=""):
    A(f'<rect x="{x}" y="{y}" width="{w}" height="{h}" rx="{r}" fill="{fill}" stroke="{stroke}" stroke-width="{sw}" {extra}/>')


def chip(x, y, label, color, text="#fff"):
    w = 7 * len(label) + 14
    A(f'<rect x="{x}" y="{y}" width="{w}" height="18" rx="9" fill="{color}"/>')
    t(x + w / 2, y + 13, label, 10.5, 600, text, "middle")
    return w + 5


# ------------------------------------------------------------------ Daten
SRC = {  # Kuerzel: (Name, Detail, Farbe)
    "SAP": ("SAP P76 Gateway", "ZPOWERBI_EINKAUF_SRV, Benutzer POWERBI", "#0b5cad"),
    "CDS": ("SAP CDS-Analytik", "Liefertreue, Query-Berechtigung", "#3b82c4"),
    "B1": ("SAP Business One (HANA)", "FR, IT, US", "#0e7490"),
    "SGE": ("Sage", "Indien", "#0891b2"),
    "ALP": ("Alphaplan", "Deutschland (Delta dünn)", "#0d9488"),
    "XLS": ("Excel-Importe", "UK, ES, DE manuell", "#059669"),
    "HRD": ("HR-Dateien / Rexx", "Upgreat-Export offen", "#65a30d"),
    "MES": ("MES", "nur FPY", "#a16207"),
    "SPO": ("SharePoint", "Listen, Ablagen", "#7c3aed"),
    "EXT": ("Öffentliche Quellen", "GDELT, Eurostat, EZB", "#9333ea"),
}
MOD = [  # Reihenfolge = Raster zeilenweise (3 x 3), abhaengige Bereiche liegen nebeneinander
    ("Finance Cockpit", "#b91c1c", ["Soll/Ist, Controlling, Hochrechnung", "Gruppenmarge, Standardkosten", "Journal CH/AT, Sales_All für Andreas", "Marktsegmente, Weltlage (Passwort)"],
     ["SAP", "B1", "SGE", "ALP", "XLS", "EXT"], "Entscheide Andreas offen (A1, B1–B3, F5/F8)"),
    ("SAP-Entwicklung & Betrieb", "#334155", ["ZC23 / ZM_OFFENE_KDAUF schnell", "ZLO03 Tageszeit, ZC12 Nullmengen", "ZZPRDAT Abnahme ab 26.10.", "Berechtigungen, Entsperrungen"],
     ["SAP"], "T76K912728 nach P76 offen"),
    ("Logistik", "#4d7c0f", ["Logistik live (Kommission., 3D-Lager)", "Kapazität, Produktivität", "Stücklisten, Verwendung & Risiko", "ZM_OFFENE_FAUF Bedarfsverursacher"],
     ["SAP"], "Werte gegen SAP abgleichen"),
    ("Verkauf & Railway", "#c2410c", ["Kunden, Rückgang, Konzentration", "Cross-Selling, Preise, Prognose", "Railway-Sicht für Heiko/Patrick", "Kundenfelder DE (Rohail)"],
     ["SAP", "B1", "ALP", "XLS"], "Top 15 / DB4 / Schweiz bestätigen"),
    ("Einkauf", "#b45309", ["Spend, Bestellungen, Kontrakte (ME3L)", "Lieferanten, Lieferperformance", "Produktgruppe über ZLO03", "Interaktiv, Datenservice (Delta)"],
     ["SAP"], "Delta 14.10. ~13:30 prüfen"),
    ("HR KPI", "#15803d", ["Fluktuation, Absenzen", "Unfalltage, Ferien", "HrKpiSet / HrAbsenzSet", "Login separat"],
     ["SAP", "HRD"], "Rexx-Export über Upgreat"),
    ("Netzwerk & IT", "#1d4ed8", ["Verfügbarkeit, Sicherheit", "Active Directory, GPO, DNS", "Arbeitsplätze, Migration", "VPN-Abbrüche, Support"],
     ["EXT"], "IIS-Dauerbetrieb (ISS-017) bei IT"),
    ("Operations", "#0f766e", ["KPI Liefertreue OTD / OTIF / OTP", "Shopfloor-Meeting (Fabio)", "ZD05 Fehlteile, Forecast", "Push 04:00 aus SAP geplant"],
     ["SAP", "CDS", "MES"], "Rolle POWERBI läuft 31.10. ab"),
    ("Projekte & Austausch", "#6d28d9", ["Trafag Projekte (HERMES)", "Trafag Reddit (Forum)", "Benutzerhandbuch", "Startseite, Nutzungsmessung"],
     ["SPO"], "geringe Nutzung: messen bis 11.11."),
]
PEOPLE = [
    ("Andreas", "Finance, Entscheide", [0, 3]),
    ("Rino", "Vier-Augen-Prüfung", [0]),
    ("Armin", "Einkauf", [4]),
    ("Fabio", "Operations, Einkauf-Ideen", [7, 4]),
    ("Neva / Ann-Katrin", "Logistik, ZLO03", [2, 1]),
    ("Sonja / Nadja", "HR", [5]),
    ("Rohail", "Deutschland (Alphaplan)", [3, 0]),
    ("Paola", "Italien (B1)", [0]),
    ("Heiko / Patrick", "Railway", [3]),
    ("Lucas / Marco / Adil", "ZZPRDAT, SAP", [1]),
    ("Upgreat (Dardan, Ueli)", "IT-Partner", [6, 5]),
    ("Hauptadmin SAP", "Rollen, Transporte", [1, 7]),
    ("Interims-Chef", "Management, HERMES", [8]),
]
CROSS = [
    ("Plattform & Betrieb", "Trafag Dashboard (Blazor), SQLite, Release-Worktree, Deploy-Runner, Wochensicherung, Auto-Modus-Regeln", "#111827"),
    ("Daten & Regeln", "Konzernkunden aus, CHF zum Budgetkurs, Disponent → Abteilung, Arbeitstage Mo–Fr, Konsistenz Finance/Verkauf", "#374151"),
    ("Steuerung & Doku", "Wochen_Todo, Issue-Log, HERMES-Vorhaben, router.md, RAG, LEARNINGS, Koordination", "#4b5563"),
    ("Nutzung & Einführung", "Ersetzen statt ergänzen, Besitzer je Bereich, ins Meeting holen, anonyme Messung", "#6b7280"),
]
DEPS = [  # (von, nach, Beschriftung, Art); nur Nachbarn im Raster, Pfeil laeuft durch die Luecke
    (3, 0, "gleiche Quellen", "dep"),
    (3, 0, "DE-Delta dünn", "block"),
    (1, 4, "ZLO03 → Produktgruppe", "dep"),
    (4, 7, "ZD05 Fehlteile", "dep"),
    (1, 2, "ZC22 / ZC23", "dep"),
]

# ------------------------------------------------------------------ Kopf
A(f'<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 {W} {H}" width="{W}" height="{H}" font-family="Segoe UI, Open Sans, Arial, sans-serif">')
A('<defs><marker id="arr" viewBox="0 0 10 10" refX="9" refY="5" markerWidth="7" markerHeight="7" orient="auto-start-reverse"><path d="M0,0 L10,5 L0,10 z" fill="#2563eb"/></marker>'
  '<marker id="arrR" viewBox="0 0 10 10" refX="9" refY="5" markerWidth="7" markerHeight="7" orient="auto-start-reverse"><path d="M0,0 L10,5 L0,10 z" fill="#dc2626"/></marker>'
  '<filter id="sh" x="-5%" y="-5%" width="110%" height="120%"><feDropShadow dx="0" dy="2" stdDeviation="3" flood-opacity="0.12"/></filter></defs>')
A(f'<rect width="{W}" height="{H}" fill="#f6f7f9"/>')
t(40, 50, "Themenlandkarte Ingo Kohler · SAP Specialist Trafag", 26, 700, "#111827")
t(40, 76, "Stand 15.10.2026 · Quellen links, Themen in der Mitte, Ansprechpartner rechts, gemeinsame Grundlagen unten. Rote Hinweise = offen oder blockiert.", 14, 400, "#4b5563")

# ------------------------------------------------------------------ Quellen
sx, sy, sw, sh = 40, 120, 300, 62
t(sx, sy - 12, "DATENQUELLEN", 12, 700, "#6b7280")
for i, (k, (name, det, col)) in enumerate(SRC.items()):
    y = sy + i * (sh + 10)
    box(sx, y, sw, sh, "#fff", "#e5e7eb", extra='filter="url(#sh)"')
    A(f'<rect x="{sx}" y="{y}" width="8" height="{sh}" rx="4" fill="{col}"/>')
    chip(sx + 20, y + 10, k, col)
    t(sx + 72, y + 24, name, 14, 600)
    t(sx + 20, y + 48, det, 12, 400, "#6b7280")

# ------------------------------------------------------------------ Themen (3 x 3)
mx, my, mw, mh, gx, gy = 380, 120, 330, 222, 90, 50
centers = []
t(mx, my - 12, "THEMEN (ein Kasten je Bereich, Chips = Datenquellen)", 12, 700, "#6b7280")
for i, (title, col, items, srcs, warn) in enumerate(MOD):
    c, r = i % 3, i // 3
    x, y = mx + c * (mw + gx), my + r * (mh + gy)
    centers.append((x + mw / 2, y + mh / 2, x, y))
    box(x, y, mw, mh, "#fff", col, r=12, sw=2, extra='filter="url(#sh)"')
    A(f'<path d="M{x},{y + 12} a12,12 0 0 1 12,-12 h{mw - 24} a12,12 0 0 1 12,12 v26 h-{mw} z" fill="{col}"/>')
    t(x + 14, y + 26, title, 16, 700, "#fff")
    for j, it in enumerate(items):
        A(f'<circle cx="{x + 20}" cy="{y + 58 + j * 22}" r="3" fill="{col}"/>')
        t(x + 30, y + 62 + j * 22, it, 13)
    cx = x + 14
    for k in srcs:
        cx += chip(cx, y + 150, k, SRC[k][2])
    A(f'<rect x="{x + 10}" y="{y + 180}" width="{mw - 20}" height="30" rx="6" fill="#fef2f2" stroke="#fecaca"/>')
    t(x + 22, y + 200, "⚠ " + warn, 12.5, 600, "#b91c1c")

# Abhaengigkeiten: senkrechte Nachbarn = Pfeil durch die waagrechte Luecke, waagrechte Nachbarn = durch die senkrechte
pair_seen = {}
for a, b, label, kind in DEPS:
    _, _, ax, ay = centers[a]
    _, _, bx, by = centers[b]
    col, mk, dash = ("#2563eb", "arr", "") if kind == "dep" else ("#dc2626", "arrR", ' stroke-dasharray="6 4"')
    w = 7.0 * len(label) + 18
    if ax == bx:  # senkrecht
        n = pair_seen.get((min(a, b), max(a, b)), 0)
        pair_seen[(min(a, b), max(a, b))] = n + 1
        x = ax + (40 if n == 0 else 190)
        y1, y2 = (ay + mh, by) if by > ay else (ay, by + mh)
        A(f'<line x1="{x}" y1="{y1 + 2}" x2="{x}" y2="{y2 - 2}" stroke="{col}" stroke-width="2.6" marker-end="url(#{mk})"{dash}/>')
        A(f'<rect x="{x + 10}" y="{(y1 + y2) / 2 - 11}" width="{w}" height="22" rx="11" fill="#fff" stroke="{col}" stroke-width="1.2"/>')
        t(x + 10 + w / 2, (y1 + y2) / 2 + 4, label, 11.5, 600, col, "middle")
    else:  # waagrecht
        y = ay + 92
        x1, x2 = (ax + mw, bx) if bx > ax else (ax, bx + mw)
        A(f'<line x1="{x1 + 2}" y1="{y}" x2="{x2 - 2}" y2="{y}" stroke="{col}" stroke-width="2.6" marker-end="url(#{mk})"{dash}/>')
        lines = label.split(" / ")
        for j, ln in enumerate(lines):
            t((x1 + x2) / 2, y - 10 - (len(lines) - 1 - j) * 15, ln, 11.5, 600, col, "middle")

# ------------------------------------------------------------------ Personen
px, py, pw, ph = 1590, 120, 332, 52
t(px, py - 12, "ANSPRECHPARTNER (Chips = Themen)", 12, 700, "#6b7280")
for i, (name, role, mods) in enumerate(PEOPLE):
    y = py + i * (ph + 8)
    box(px, y, pw, ph, "#fff", "#e5e7eb", extra='filter="url(#sh)"')
    t(px + 14, y + 22, name, 14, 600)
    t(px + 14, y + 41, role, 12, 400, "#6b7280")
    cx = px + pw - 12
    for m in reversed(mods):
        label = {"SAP-Entwicklung": "SAP", "Netzwerk": "IT"}.get(MOD[m][0].split(" ")[0], MOD[m][0].split(" ")[0])
        w = 7 * len(label) + 14
        cx -= w
        A(f'<rect x="{cx}" y="{y + 17}" width="{w}" height="18" rx="9" fill="{MOD[m][1]}"/>')
        t(cx + w / 2, y + 30, label, 10.5, 600, "#fff", "middle")
        cx -= 5

# ------------------------------------------------------------------ Querschnitt
qy = my + 3 * (mh + gy) + 16
t(40, qy - 12, "GEMEINSAME GRUNDLAGEN (gelten für alle Themen)", 12, 700, "#6b7280")
qw = (W - 80 - 3 * 20) / 4
for i, (title, txt, col) in enumerate(CROSS):
    x = 40 + i * (qw + 20)
    box(x, qy, qw, 104, "#fff", col, sw=1.6, extra='filter="url(#sh)"')
    A(f'<rect x="{x}" y="{qy}" width="{qw}" height="30" rx="10" fill="{col}"/>')
    A(f'<rect x="{x}" y="{qy + 18}" width="{qw}" height="12" fill="{col}"/>')
    t(x + 14, qy + 21, title, 14, 700, "#fff")
    words, line, lines = txt.split(" "), "", []
    for wd in words:
        if len(line) + len(wd) > 58:
            lines.append(line)
            line = wd
        else:
            line = (line + " " + wd).strip()
    lines.append(line)
    for j, ln in enumerate(lines[:3]):
        t(x + 14, qy + 52 + j * 18, ln, 12.5, 400, "#374151")

# ------------------------------------------------------------------ Legende
ly = H - 34
A(f'<line x1="40" y1="{ly - 4}" x2="80" y2="{ly - 4}" stroke="#2563eb" stroke-width="2.4" marker-end="url(#arr)"/>')
t(88, ly, "fachliche Abhängigkeit (Daten oder Logik fliessen hinüber)", 12.5, 400, "#374151")
A(f'<line x1="480" y1="{ly - 4}" x2="520" y2="{ly - 4}" stroke="#dc2626" stroke-width="2.4" stroke-dasharray="7 5" marker-end="url(#arrR)"/>')
t(528, ly, "offener Punkt, der ein anderes Thema bremst", 12.5, 400, "#374151")
t(W - 40, ly, "Quelle: router.md, Wochen_Todo, docs/*, Stand 15.10.2026", 12, 400, "#9ca3af", "end")
A("</svg>")
open(sys.argv[1], "w", encoding="utf-8").write("\n".join(out))
print("ok")
