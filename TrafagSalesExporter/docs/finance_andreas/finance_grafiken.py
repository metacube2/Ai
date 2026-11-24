# Grafiken fuer die PDF an Andreas (reportlab.graphics). Farben: Kategorien blau/orange (validiert mit dem dataviz-Pruefskript,
# 24.11.2026), Status gruen/gelb/orange nur zusammen mit einem Wort, Text immer in Textfarbe.
from reportlab.graphics.shapes import Circle, Drawing, Line, Polygon, Rect, String
from reportlab.lib import colors
from reportlab.pdfbase.pdfmetrics import stringWidth

W = 482  # 170 mm Satzbreite

INK = colors.HexColor('#0b0b0b')
INK2 = colors.HexColor('#52514e')
MUTED = colors.HexColor('#8a8984')
GRID = colors.HexColor('#e2e1dc')
SURF = colors.HexColor('#f5f5f3')
ACCENT = colors.HexColor('#C8102E')

KONZERN = colors.HexColor('#2a78d6')      # Konzernkosten (Kategorie 1)
KONZERN_T = colors.HexColor('#dbe9fb')
LOKAL = colors.HexColor('#eb6834')        # lokale Kosten (Kategorie 2)
LOKAL_T = colors.HexColor('#fbe1d6')
NULL = colors.HexColor('#6b6a65')         # Kosten 0
NULL_T = colors.HexColor('#e9e8e4')
VORHER = colors.HexColor('#86b6ef')       # blau Stufe 250 (vorher)
NACHHER = colors.HexColor('#2a78d6')      # blau Stufe 450 (nachher)

GOOD = colors.HexColor('#0ca30c')
WARN = colors.HexColor('#fab219')
SERIOUS = colors.HexColor('#ec835a')


def _wrap(text, font, size, width):
    words, lines, cur = text.split(), [], ''
    for w in words:
        t = (cur + ' ' + w).strip()
        if stringWidth(t, font, size) <= width or not cur:
            cur = t
        else:
            lines.append(cur)
            cur = w
    if cur:
        lines.append(cur)
    return lines


def _text(d, x, y, text, size=8, font='Cal', color=INK, anchor='start'):
    d.add(String(x, y, text, fontName=font, fontSize=size, fillColor=color, textAnchor=anchor))


def _block(d, cx, top, text, width, size=7.6, font='Cal', color=INK, leading=None):
    """Zentrierter, umbrochener Text ab Oberkante top; gibt die Unterkante zurueck."""
    leading = leading or size * 1.22
    y = top - size
    for line in _wrap(text, font, size, width):
        _text(d, cx, y, line, size, font, color, 'middle')
        y -= leading
    return y + leading - size * 0.3


def _box(d, x, y, w, h, fill, stroke=None, r=5, sw=0.8):
    d.add(Rect(x, y, w, h, rx=r, ry=r, fillColor=fill, strokeColor=stroke, strokeWidth=sw if stroke else 0))


def _arrow(d, x1, y1, x2, y2, color=INK2, w=1.1):
    d.add(Line(x1, y1, x2, y2, strokeColor=color, strokeWidth=w))
    import math
    a = math.atan2(y2 - y1, x2 - x1)
    s = 5
    p1 = (x2 - s * math.cos(a - 0.45), y2 - s * math.sin(a - 0.45))
    p2 = (x2 - s * math.cos(a + 0.45), y2 - s * math.sin(a + 0.45))
    d.add(Polygon([x2, y2, p1[0], p1[1], p2[0], p2[1]], fillColor=color, strokeColor=color, strokeWidth=0.5))


# 1 Kennzahlen
def kpi_tiles():
    tiles = [
        ('86 %', 'FR-Zeilen mit Lieferant', 'vorher 4 %'),
        ('63 %', 'US-Zeilen mit Lieferant', 'vorher 0,3 %'),
        ('11 von 18', 'Gruppen mit Gruppenmarge', '16 ohne Zeilen ohne Artikel'),
        ('0,85 Mio.', 'CHF Marge an Entscheid 1', 'Werkstamm oder lokal'),
    ]
    h, gap = 62, 8
    tw = (W - gap * 3) / 4
    d = Drawing(W, h)
    for i, (big, label, note) in enumerate(tiles):
        x = i * (tw + gap)
        _box(d, x, 0, tw, h, SURF, r=6)
        d.add(Rect(x, h - 3, tw, 3, fillColor=ACCENT, strokeColor=None))
        _text(d, x + 10, h - 27, big, 19, 'CalB', INK)
        _text(d, x + 10, h - 42, label, 8, 'Cal', INK2)
        _text(d, x + 10, h - 54, note, 7.5, 'Cal', MUTED)
    return d


# 2 Zeitstrahl
def timeline():
    items = [
        ('13.11.', 'Zuordnung je Zeile', 'Supplier Type, Delivering Entity, Cost Source'),
        ('16.11.', 'Blatt Lieferantenzuordnung', 'je Gesellschaft und Monat'),
        ('18.11.', 'Lieferant aus Wareneingang', 'FR, US, IT, IN; ES gebaut'),
        ('19.11.', 'Issue Topic und Mapping', 'use 0, Indien-Ware, TRCH/TRAT aus'),
        ('20.11.', 'Stufe 2', 'Mapping auf TR IT, Supplier Origin'),
    ]
    h = 92
    d = Drawing(W, h)
    y = 58
    x0, x1 = 30, W - 30
    d.add(Line(x0, y, x1, y, strokeColor=GRID, strokeWidth=3))
    step = (x1 - x0) / (len(items) - 1)
    for i, (date, title, note) in enumerate(items):
        x = x0 + i * step
        d.add(Circle(x, y, 7, fillColor=ACCENT, strokeColor=colors.white, strokeWidth=2))
        _text(d, x, y + 14, date, 9.5, 'CalB', INK, 'middle')
        bottom = _block(d, x, y - 12, title, step - 8, 8, 'CalB', INK)
        _block(d, x, bottom - 2, note, step - 8, 7.2, 'Cal', INK2)
    return d


# 3 Prozessgrafik Kostenkette
def cost_flow():
    h = 262
    d = Drawing(W, h)
    cols = 6
    cw = W / cols
    bw, bh = cw - 22, 50
    top_y = h - 18 - bh
    res_y = top_y - 36 - 62
    dec = [
        ('Verkaufszeile', None),
        ('CH oder AT?', 'Regel'),
        ('Indien: Sales Type gefüllt?', 'FFM, CM, LRD'),
        ('Lieferant vorhanden?', 'Stamm, Liste, Wareneingang'),
        ('Material im Werkstamm 1100?', 'Trafag AG'),
    ]
    _text(d, 0, h - 9, 'Prüfreihenfolge je Zeile (links nach rechts), Ergebnis darunter', 8, 'CalB', INK2)
    for i, (t, sub) in enumerate(dec):
        x = i * cw + 6
        if i == 0:
            _box(d, x, top_y, bw, bh, INK, r=bh / 2)
            _block(d, x + bw / 2, top_y + bh / 2 + 5, t, bw - 8, 8.5, 'CalB', colors.white)
        else:
            _box(d, x, top_y, bw, bh, colors.white, INK, r=6, sw=1.2)
            b = _block(d, x + bw / 2, top_y + bh - 7, t, bw - 10, 8, 'CalB', INK)
            _block(d, x + bw / 2, b - 2, sub, bw - 10, 6.6, 'Cal', MUTED)
        if i < len(dec) - 1:
            _arrow(d, x + bw, top_y + bh / 2, x + cw, top_y + bh / 2)
            if i > 0:
                _text(d, x + bw + 11, top_y + bh / 2 + 4, 'nein', 6.4, 'Cal', INK2, 'middle')
    # letzter nein -> lokal
    x5 = 5 * cw + 6
    _arrow(d, 4 * cw + 6 + bw, top_y + bh / 2, x5, top_y + bh / 2)
    _text(d, 4 * cw + 6 + bw + 11, top_y + bh / 2 + 4, 'nein', 6.4, 'Cal', INK2, 'middle')
    _box(d, x5, top_y, bw, bh, LOKAL_T, LOKAL, r=6)
    _block(d, x5 + bw / 2, top_y + bh - 8, 'lokale Standardkosten', bw - 10, 8, 'CalB', INK)
    _block(d, x5 + bw / 2, top_y + 17, 'Issue Topic 1', bw - 10, 6.8, 'Cal', INK2)

    results = [
        (1, 'Kosten Trafag AG', 'intern', KONZERN_T, KONZERN),
        (2, 'FFM, CM: Trafag India. LRD: Trafag AG', 'intern', KONZERN_T, KONZERN),
        (3, 'TRAFAG im Namen: Kosten der liefernden Gesellschaft. Sonst extern: lokale Kosten', '', SURF, INK2),
        (4, 'Kosten Trafag AG', 'Issue Topic 5', KONZERN_T, KONZERN),
    ]
    rh = 62
    for col, t, tag, fill, stroke in results:
        x = col * cw + 6
        _arrow(d, x + bw / 2, top_y, x + bw / 2, res_y + rh)
        _text(d, x + bw / 2 + 5, top_y - 16, 'ja', 6.8, 'Cal', INK2)
        _box(d, x, res_y, bw, rh, fill, stroke, r=6)
        b = _block(d, x + bw / 2, res_y + rh - 7, t, bw - 10, 7.4, 'CalB' if len(t) < 20 else 'Cal', INK)
        if tag:
            _block(d, x + bw / 2, b - 3, tag, bw - 10, 6.8, 'Cal', INK2)

    # Absicherung
    yb = 8
    hb = 40
    _text(d, 0, yb + hb + 8, 'Absicherung bei jeder Zeile, wenn Kosten fehlen', 8, 'CalB', INK2)
    chev = [
        ('Konzernkosten fehlen oder Artikel nicht gefunden (auch nicht über Mapping)', SURF, INK2),
        ('lokale Standardkosten der verkaufenden Gesellschaft', LOKAL_T, LOKAL),
        ('fehlen auch diese: Kosten 0 (use 0)', NULL_T, NULL),
        ('Issue Topic nennt den Grund je Zeile', colors.white, ACCENT),
    ]
    cwid = (W - 3 * 16) / 4
    for i, (t, fill, stroke) in enumerate(chev):
        x = i * (cwid + 16)
        _box(d, x, yb, cwid, hb, fill, stroke, r=6)
        _block(d, x + cwid / 2, yb + hb - 7, t, cwid - 12, 7.4, 'Cal', INK)
        if i < 3:
            _arrow(d, x + cwid + 2, yb + hb / 2, x + cwid + 14, yb + hb / 2)
    return d


# 4 Issue-Topic-Matrix
def topic_matrix():
    rows = [
        ('leer', (0, 0, 0), 'Konzernkosten', KONZERN_T, KONZERN),
        ('1', (1, 1, 0), 'lokale Kosten', LOKAL_T, LOKAL),
        ('2', (0, 1, 0), 'lokale Kosten', LOKAL_T, LOKAL),
        ('3', (0, 0, 1), 'lokale Kosten', LOKAL_T, LOKAL),
        ('4', (1, 0, 1), 'lokale Kosten', LOKAL_T, LOKAL),
        ('5', (1, 0, 0), 'Kosten Trafag AG (Werkstamm)', KONZERN_T, KONZERN),
        ('6', (1, 1, 1), 'Kosten 0 (use 0)', NULL_T, NULL),
        ('7', (0, 1, 1), 'Kosten 0 (use 0)', NULL_T, NULL),
    ]
    rh, head = 19, 30
    h = head + rh * len(rows) + 4
    d = Drawing(W, h)
    cx = [22, 120, 215, 310]
    heads = ['Code', 'kein Lieferant', 'nicht gefunden', 'keine Kosten']
    for x, t in zip(cx, heads):
        _text(d, x, h - 14, t, 8, 'CalB', INK2, 'middle')
    _text(d, 360, h - 14, 'gerechnet wird mit', 8, 'CalB', INK2)
    d.add(Line(0, h - head + 6, W, h - head + 6, strokeColor=GRID, strokeWidth=1))
    for i, (code, bits, res, fill, stroke) in enumerate(rows):
        y = h - head - (i + 1) * rh + 6
        if i % 2 == 0:
            d.add(Rect(0, y - 4, W, rh, fillColor=SURF, strokeColor=None))
        _text(d, cx[0], y + 2, code, 9, 'CalB', INK, 'middle')
        for x, b in zip(cx[1:], bits):
            if b:
                d.add(Circle(x, y + 5, 5, fillColor=INK, strokeColor=None))
            else:
                d.add(Circle(x, y + 5, 4.5, fillColor=colors.white, strokeColor=MUTED, strokeWidth=1))
        _box(d, 352, y - 1, 128, 13, fill, stroke, r=6, sw=0.7)
        _text(d, 360, y + 2.5, res, 7.6, 'Cal', INK)
    return d


# 5 Ampel je Land
def country_tiles():
    tiles = [
        ('CH/AT', GOOD, 'erledigt'), ('FR', GOOD, 'erledigt'), ('US', GOOD, 'erledigt'), ('IT', GOOD, 'erledigt'),
        ('IN', WARN, 'Vorschläge da'), ('UK', WARN, 'Vorschläge da'), ('ES', SERIOUS, 'wartet auf Santi'), ('DE', WARN, 'Vorschläge da'),
    ]
    h, gap = 54, 5
    tw = (W - gap * 7) / 8
    d = Drawing(W, h)
    for i, (c, col, word) in enumerate(tiles):
        x = i * (tw + gap)
        _box(d, x, 0, tw, h, SURF, r=6)
        _text(d, x + tw / 2, h - 18, c, 11, 'CalB', INK, 'middle')
        d.add(Circle(x + tw / 2, h - 31, 5.5, fillColor=col, strokeColor=None))
        _text(d, x + tw / 2, 7, word, 7, 'Cal', INK2, 'middle')
    return d


# 6 Balken vorher/nachher
def supplier_bars():
    # vorher: Export 17.11. (Anteil an den Zeilen vom 18.11.), nachher: Export 23.11.
    data = [
        ('FR', 100 * 120 / 2846, 2440, 2850, ''),
        ('US', 100 * 6 / 1790, 1146, 1811, ''),
        ('IT', 100 * 19696 / 20220, 20228, 20234, 'vorher mit Übergangsliste Paola'),
        ('IN', 100 * 960 / 8453, 1381, 8497, 'Sales Type entscheidet, Lieferant nur ergänzend'),
        ('UK', 100.0, 3139, 3139, 'Felder schon immer gefüllt'),
        ('ES', 0.0, 0, 8230, 'wartet auf Santis Sage-Export'),
        ('DE', 0.0, 0, 7853, 'Alphaplan liefert keinen Lieferanten'),
    ]
    bar, gap, group = 8, 2, 30
    top = 30
    h = top + group * len(data) + 22
    d = Drawing(W, h)
    x0, xw = 34, 250
    # Legende
    d.add(Rect(x0, h - 12, 9, 9, fillColor=VORHER, strokeColor=None))
    _text(d, x0 + 13, h - 11, 'vor dem 18.11. (Export 17.11.)', 7.8, 'Cal', INK2)
    d.add(Rect(x0 + 160, h - 12, 9, 9, fillColor=NACHHER, strokeColor=None))
    _text(d, x0 + 173, h - 11, 'mit letztem Wareneingang (Export 23.11.)', 7.8, 'Cal', INK2)
    base_y = h - top
    for p in (0, 25, 50, 75, 100):
        x = x0 + xw * p / 100
        d.add(Line(x, 16, x, base_y + 4, strokeColor=GRID, strokeWidth=0.6))
        _text(d, x, 6, f'{p} %', 6.8, 'Cal', MUTED, 'middle')
    for i, (c, before_pct, after, total, note) in enumerate(data):
        gy = base_y - i * group - 4
        _text(d, 0, gy - bar - 1, c, 9, 'CalB', INK)
        for j, (pct, col) in enumerate(((before_pct, VORHER), (100 * after / total if total else 0, NACHHER))):
            y = gy - (j + 1) * bar - j * gap
            wpx = max(xw * pct / 100, 0.0)
            if wpx > 0:
                d.add(Rect(x0, y, wpx, bar, rx=2, ry=2, fillColor=col, strokeColor=None))
            lab = f'{pct:.1f} %'.replace('.', ',') if 0 < pct < 1 else f'{pct:.0f} %'
            _text(d, x0 + wpx + 4, y + 1.2, lab, 7, 'Cal', INK2)
        _text(d, x0 + xw + 34, gy - bar - 1, f"{after:,} von {total:,} Zeilen".replace(',', "'"), 7.4, 'Cal', INK)
        if note:
            _text(d, x0 + xw + 34, gy - bar - 10, note, 6.8, 'Cal', MUTED)
    d.add(Line(x0, 16, x0, base_y + 4, strokeColor=INK2, strokeWidth=0.8))
    return d


# 7 Entscheid 1
def decision_one():
    h = 96
    d = Drawing(W, h)
    x0, xw, maxv = 120, 300, 3.5
    rows = [('Werkstamm (heute produktiv)', 2.252, KONZERN), ('immer lokale Kosten', 3.097, LOKAL)]
    _text(d, 0, h - 10, 'Herstellkosten der 5\'933 Zeilen ohne Lieferant, 2025/2026 (Umsatz 5,53 Mio. CHF, Export 23.11.)', 8, 'CalB', INK2)
    for i, (lab, v, col) in enumerate(rows):
        y = h - 38 - i * 24
        _text(d, 0, y + 4, lab, 8.2, 'Cal', INK)
        d.add(Rect(x0, y, xw * v / maxv, 14, rx=3, ry=3, fillColor=col, strokeColor=None))
        _text(d, x0 + xw * v / maxv + 5, y + 4, f'{v:.2f} Mio. CHF'.replace('.', ','), 8.2, 'CalB', INK)
    xa, xb = x0 + xw * 2.252 / maxv, x0 + xw * 3.097 / maxv
    yk = 12
    d.add(Line(xa, yk + 10, xa, yk + 4, strokeColor=INK2, strokeWidth=0.8))
    d.add(Line(xb, yk + 10, xb, yk + 4, strokeColor=INK2, strokeWidth=0.8))
    d.add(Line(xa, yk + 4, xb, yk + 4, strokeColor=INK2, strokeWidth=0.8))
    _text(d, (xa + xb) / 2, yk - 6, '0,85 Mio. CHF weniger Gruppenmarge mit „immer lokal“', 7.8, 'Cal', INK, 'middle')
    return d
