# Erzeugt die PDF "Finance Dashboard: Lieferantenzuordnung, Mapping und Issue Topic" fuer Andreas (Stand 24.11.2026).
# Aufruf: uv run --no-project --with reportlab python finance_stand_andreas_pdf.py <ausgabe.pdf>
# Inhalt aus docs/rag/FINANCE.md, docs/FINANCE_SUPPLIER.md 11/12, docs/FINANCE_PLAN_MAPPING_ISSUE_TOPIC_2026-11-18.md,
# docs/FINANCE_PLAN_MAPPING_STUFE2_2026-11-20.md, docs/FINANCE_REGELSET_ABGLEICH_2026-11-20.md. Keine neue Messung.
import sys
from reportlab.lib import colors
from reportlab.lib.enums import TA_LEFT
from reportlab.lib.pagesizes import A4
from reportlab.lib.styles import ParagraphStyle
from reportlab.lib.units import mm
from reportlab.pdfbase import pdfmetrics
from reportlab.pdfbase.ttfonts import TTFont
from reportlab.graphics.shapes import Drawing
from reportlab.platypus import (KeepTogether, PageBreak, Paragraph, SimpleDocTemplate, Spacer, Table, TableStyle)
import os
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import finance_grafiken as G  # noqa: E402

pdfmetrics.registerFont(TTFont('Cal', r'C:\Windows\Fonts\calibri.ttf'))
pdfmetrics.registerFont(TTFont('CalB', r'C:\Windows\Fonts\calibrib.ttf'))
from reportlab.pdfbase.pdfmetrics import registerFontFamily
registerFontFamily('Cal', normal='Cal', bold='CalB', italic='Cal', boldItalic='CalB')

ACCENT = colors.HexColor('#C8102E')
DARK = colors.HexColor('#222222')
GREY = colors.HexColor('#666666')
LIGHT = colors.HexColor('#F3F3F3')
LINE = colors.HexColor('#D0D0D0')
GREEN = colors.HexColor('#0ca30c')
YELLOW = colors.HexColor('#fab219')
ORANGE = colors.HexColor('#ec835a')
RED = colors.HexColor('#C62828')

base = ParagraphStyle('base', fontName='Cal', fontSize=10, leading=13.5, textColor=DARK, alignment=TA_LEFT)
small = ParagraphStyle('small', parent=base, fontSize=8.5, leading=11)
cell = ParagraphStyle('cell', parent=base, fontSize=8.8, leading=11.2)
cellb = ParagraphStyle('cellb', parent=cell, fontName='CalB')
head = ParagraphStyle('head', parent=cell, fontName='CalB', textColor=colors.white)
h1 = ParagraphStyle('h1', parent=base, fontName='CalB', fontSize=20, leading=24, textColor=DARK, spaceAfter=4)
sub = ParagraphStyle('sub', parent=base, fontSize=11, leading=14, textColor=GREY, spaceAfter=10)
h2 = ParagraphStyle('h2', parent=base, fontName='CalB', fontSize=13.5, leading=17, textColor=ACCENT, spaceBefore=12, spaceAfter=5)
h3 = ParagraphStyle('h3', parent=base, fontName='CalB', fontSize=10.5, leading=14, spaceBefore=6, spaceAfter=2)
bullet = ParagraphStyle('bullet', parent=base, leftIndent=12, bulletIndent=2, spaceAfter=2)
box = ParagraphStyle('box', parent=base, fontSize=10, leading=13.5)


def P(t, s=base):
    return Paragraph(t, s)


def bullets(items):
    return [Paragraph(i, bullet, bulletText='\u2022') for i in items]


def table(rows, widths, header=True, zebra=True, first_bold=False):
    data = []
    for r_i, r in enumerate(rows):
        row = []
        for c_i, c in enumerate(r):
            if isinstance(c, str):
                st = head if (header and r_i == 0) else (cellb if (first_bold and c_i == 0) else cell)
                row.append(Paragraph(c, st))
            else:
                row.append(c)
        data.append(row)
    t = Table(data, colWidths=widths, repeatRows=1 if header else 0)
    style = [
        ('VALIGN', (0, 0), (-1, -1), 'TOP'),
        ('LEFTPADDING', (0, 0), (-1, -1), 4), ('RIGHTPADDING', (0, 0), (-1, -1), 4),
        ('TOPPADDING', (0, 0), (-1, -1), 3), ('BOTTOMPADDING', (0, 0), (-1, -1), 3),
        ('LINEBELOW', (0, 0), (-1, -1), 0.4, LINE),
    ]
    if header:
        style.append(('BACKGROUND', (0, 0), (-1, 0), DARK))
    if zebra:
        for i in range(1 if header else 0, len(data)):
            if i % 2 == 0:
                style.append(('BACKGROUND', (0, i), (-1, i), LIGHT))
    t.setStyle(TableStyle(style))
    return t


def note_box(flowables, color=ACCENT):
    t = Table([[flowables]], colWidths=[170 * mm])
    t.setStyle(TableStyle([
        ('BOX', (0, 0), (-1, -1), 0.6, color), ('LINEBEFORE', (0, 0), (0, -1), 3, color),
        ('BACKGROUND', (0, 0), (-1, -1), colors.HexColor('#FBF3F4')),
        ('LEFTPADDING', (0, 0), (-1, -1), 8), ('RIGHTPADDING', (0, 0), (-1, -1), 8),
        ('TOPPADDING', (0, 0), (-1, -1), 6), ('BOTTOMPADDING', (0, 0), (-1, -1), 6),
    ]))
    return t


def dot(color, text):
    return Paragraph(f'<font color="{color.hexval().replace("0x", "#")}">\u25CF</font> {text}', cell)


def on_page(c, doc):
    c.saveState()
    c.setFillColor(ACCENT)
    c.rect(0, A4[1] - 6 * mm, A4[0], 6 * mm, fill=1, stroke=0)
    c.setFont('Cal', 8)
    c.setFillColor(GREY)
    c.drawString(20 * mm, 10 * mm, 'Trafag AG, Finance Dashboard: Lieferantenzuordnung, Mapping und Issue Topic, Stand 24.11.2026')
    c.drawRightString(A4[0] - 20 * mm, 10 * mm, f'Seite {doc.page}')
    c.restoreState()


def build(out):
    doc = SimpleDocTemplate(out, pagesize=A4, leftMargin=20 * mm, rightMargin=20 * mm, topMargin=18 * mm, bottomMargin=18 * mm,
                            title='Finance Dashboard: Lieferantenzuordnung, Mapping und Issue Topic', author='Ingo Kohler',
                            subject='Stand 24.11.2026 fuer Andreas')
    s = []
    s.append(P('Lieferantenzuordnung, Mapping und Issue Topic', h1))
    s.append(P('Finance Dashboard und Sales_All: was sich seit dem 13.11.2026 geändert hat, Stand je Land und offene Entscheide. '
               'Für Andreas, Stand 24.11.2026.', sub))
    s.append(G.kpi_tiles())
    s.append(Spacer(1, 10))

    s.append(note_box([
        P('<b>Kurzfassung</b>', box), Spacer(1, 3),
        *bullets([
            'Sales_All zeigt seit dem 13.11., <b>wie jede Zeile wirklich zugeordnet wird</b> (Supplier Type, Delivering Entity, '
            'Cost Source) und seit dem 20.11. auch, <b>woher der Lieferant stammt</b> (Supplier Origin).',
            'Seit dem 18.11. kommt der Lieferant in <b>Frankreich, USA, Italien und Indien</b> auch aus dem <b>letzten Wareneingang</b>, '
            'wenn der Artikelstamm keinen Lieferanten hat. Frankreich ging damit von 120 auf 2\'440 von 2\'850 Zeilen mit Lieferant, '
            'die USA von 6 auf 1\'146 von 1\'811 (Export 23.11.).',
            'Die Spalte <b>Issue Topic</b> sagt seit dem 19.11. je Zeile, warum die Kostenkette nicht bis zu den Konzernkosten kam '
            '(Matrix 1 bis 7). Fehlen auch die lokalen Kosten, wird mit <b>Kosten 0</b> gerechnet (use 0).',
            'Eine <b>Mapping-Tabelle</b> ordnet lokale Artikelnummern der Gruppennummer zu. Sie wirkt nur mit <b>bestätigten</b> Zeilen; '
            'am 23.11. war noch keine bestätigt. Seit dem 20.11. geht das auch gegen die Artikelnummern von <b>Trafag Italia</b>.',
            '<b>Am 24.11. in SAP (P76) und B1 nachgeprüft</b> (Abschnitt 7): 568 deutsche Vorschläge sind nach SAP sicher, 99 '
            'UK-Artikel tragen einen eindeutigen Trafag-Typencode, Italien braucht keine Stammpflege mehr, die „unklaren“ Zeilen sind '
            'Nebenerlöse ohne Artikel, und die tiefen Konzernkosten bei H7-Materialien sind Prüfzeugnisse, kein Rechenfehler.',
            '<b>Von dir offen:</b> die schriftliche Bestätigung „ohne Lieferant gilt der Schweizer Werkstamm“ (rund 0,85 Mio. CHF '
            'Gruppenmarge), der Umgang mit Zeilen ohne Artikel und mit Prüfzeugnissen, Indien LRD, und wer die Mappings bestätigt.',
        ]),
    ]))
    s.append(Spacer(1, 4))
    s.append(P('Zahlen aus dem regulären Export Sales_All vom 23.11.2026 (Blatt Finance Details, Rechnungen 2025 und 2026), per '
               'Skript gerechnet und an den Exporten vom 18. und 20.11. gegengeprüft; Vorher-Werte aus dem Export vom 17.11.', small))

    # 1 Chronologie
    s.append(P('1. Was neu ist, in der Reihenfolge der Einführung', h2))
    s.append(G.timeline())
    s.append(Spacer(1, 6))
    s.append(table([
        ['Datum', 'Änderung', 'Wo sichtbar'],
        ['13.11.', 'Wirksame Zuordnung je Zeile: <b>Supplier Type</b> (Intern, Extern, Lokal, Unklar), <b>Delivering Entity</b> '
                   '(z. B. TR AG, TR IT, TR IN) und <b>Cost Source</b> (woher die Herstellkosten kommen).',
         'Sales_All, Blatt Sales Spalten 55 bis 57, Finance Details 43 und 44'],
        ['16.11.', 'Neues Blatt <b>Lieferantenzuordnung</b>: wirksame Zuordnung je Gesellschaft mit Zeilen, Anteil und Net Sales, gesamt '
                   'und je Monat. Grund: in den rohen Lieferantenfeldern ist zum Beispiel CH leer, wirksam ist CH aber zu 100 % Trafag AG.',
         'Sales_All, Blatt Lieferantenzuordnung'],
        ['18.11.', '<b>Lieferant aus dem letzten Wareneingang</b> für die B1-Gesellschaften FR, US, IT und IN: ist im Artikelstamm kein '
                   'Lieferant gepflegt, gilt der Lieferant des letzten nicht stornierten Wareneingangs desselben Artikels am oder vor '
                   'dem Rechnungsdatum (wie mit dir am 12.11. besprochen). Für Spanien ist dasselbe aus Sage gebaut.',
         'Lieferantenfelder in Sales_All'],
        ['19.11.', 'Spalten <b>Issue Topic</b> und <b>Mapping</b>; Mapping-Tabelle mit Seite im Finance Cockpit und eigener Datei '
                   '<b>Mapping_JJJJ-MM-TT.xlsx</b> neben Sales_All; <b>use 0</b>; Ware aus Indien über Italien, UK und USA findet die '
                   'Kosten von Trafag India; TRCH und TRAT sind vom Mapping ausgenommen.',
         'Sales_All, letzte Spalten von Sales und Finance Details; Finance Cockpit > Mapping'],
        ['20.11.', '<b>Stufe 2:</b> Mapping auch auf die Artikelnummer von Trafag Italia; Issue Topic unterscheidet bei TR IT und TR IN '
                   '„Artikel unbekannt“ (2) von „Artikel bekannt, Kosten fehlen“ (3); neue Spalte <b>Supplier Origin</b>.',
         'Sales_All, Sales Spalte 60, Finance Details 47'],
    ], [17 * mm, 108 * mm, 45 * mm]))

    # 2 Kostenkette
    s.append(P('2. So wird je Zeile gerechnet', h2))
    s.append(P('Gerechnet wird nur bis zur <b>ersten liefernden Konzerngesellschaft</b> (ein Schritt, Entscheid vom 09.09.). '
               'Blau = Konzernkosten, orange = lokale Kosten, grau = Kosten 0.'))
    s.append(Spacer(1, 4))
    s.append(G.cost_flow())

    s.append(P('Herkunft des Lieferanten (Spalte Supplier Origin)', h3))
    s.append(table([
        ['Wert', 'Bedeutung'],
        ['Rule CH/AT', 'CH und AT, per Regel Trafag AG'],
        ['Rule Sales Type', 'Indien, aus dem Feld Sales Type'],
        ['Source field', 'Lieferant aus dem Artikelstamm der Gesellschaft. Ältere Zeilen zeigen diesen Wert, bis ihr Land neu exportiert wird'],
        ['Last goods receipt', 'Lieferant aus dem letzten Wareneingang (seit 18.11.). <b>Hinweis:</b> wurde bis 24.11. wegen eines Programmfehlers '
                               'als „Source field“ gezeigt; behoben, sichtbar ab dem nächsten Export je Land'],
        ['IT override list', 'Übergangsliste Italien (Paola), geht dem Wareneingang vor; gleicher Hinweis wie oben'],
        ['CH plant master', 'kein Lieferant, Material im Schweizer Werkstamm gefunden'],
        ['none', 'kein Lieferant und nicht im Werkstamm'],
    ], [35 * mm, 135 * mm]))

    # 3 Issue Topic
    s.append(PageBreak())
    s.append(P('3. Issue Topic: warum die Kostenkette nicht bis zu den Konzernkosten kam', h2))
    s.append(P('Die Matrix ist die vom 18./19.11. Drei Fragen je Zeile: <b>kein Lieferant</b> / <b>Artikelnummer bei der liefernden '
               'Gesellschaft nicht gefunden</b> (auch nicht über das Mapping) / <b>keine Kosten</b>. 1 = trifft zu. '
               'Leer heisst: kein Issue, es gelten die Konzernkosten (bei externem Lieferanten die lokalen Kosten).'))
    s.append(Spacer(1, 4))
    s.append(G.topic_matrix())
    s.append(Spacer(1, 4))
    s.append(P('Zusatzcodes für Fälle, die die Matrix nicht abdeckt (in der Feldhilfe als „nicht Teil der Matrix“ gekennzeichnet):', base))
    s.extend(bullets([
        '<b>7</b> auch bei externem Lieferanten, wenn die lokalen Kosten fehlen.',
        '<b>8</b> Indien LRD ohne Konzernkosten: Kosten bleiben offen, weil der lokale Wert dort der IC-Preis ist (Entscheid 05.08.).',
        '<b>9</b> Lieferant unklar: Zeile ohne Artikel und ohne Lieferant, also nicht prüfbar. Am 24.11. in B1 geprüft: das sind '
        'Dienstleistungs- und Nebenerlöszeilen (Fracht, Zollzuschlag, Gutschriften, Personalverleih), siehe Abschnitt 7.',
        '<b>10</b> interner Lieferant ohne Kostenquelle.',
    ]))
    s.append(P('<b>Häufigste Codes je Land</b> (Export 23.11.): TRDE 1: 5\'555 und 6: 2\'158 Zeilen, TRES 5: 5\'248, TRUK 2: 1\'485, '
               'TRCH 3: 1\'243, TRAT 2: 217, TRIT 3: 191 und 7: 60, TRIN 8: 153. In der Gruppenmarge-Summary haben 11 von 18 Gruppen '
               'eine Marge (am 19.11.: 2). Leer bleiben FR, IN und US (2025 und 2026) sowie IT 2025, wegen Zeilen mit Code 9 '
               '(ohne Artikel) oder Code 8 (Indien LRD). Ohne die 131 Zeilen ohne Artikel wären es 16 von 18. Die Kennzahl „Saubere '
               'Kostenbasis %“ zählt use-0-Zeilen bewusst weiter als nicht sauber.', base))

    # 4 Mapping
    s.append(P('4. Mapping: lokale Artikelnummer auf Gruppennummer', h2))
    s.append(P('Viele Tochtergesellschaften verkaufen Trafag-Ware unter eigener Artikelnummer. Findet das Programm die Nummer bei der '
               'liefernden Gesellschaft nicht (Issue Topic 1, 2, 6, 7), kann eine Mapping-Zeile die passende Gruppennummer liefern. '
               'Dann rechnet die Zeile mit den Konzernkosten statt mit den lokalen Kosten.'))
    s.extend(bullets([
        '<b>Wirkt nur bestätigt.</b> Ein Vorschlag ändert keine Zahl, bis er bestätigt ist (seit 19.11. auch für Regeltreffer). '
        'Einzige Ausnahme ist die Italien-Regel „I“ + Nummer, sie wirkt sofort. <b>Am 23.11. war noch kein Vorschlag bestätigt</b>; '
        'die Zahlen in Sales_All sind also noch ohne Mapping gerechnet.',
        '<b>Vorschläge</b> kommen aus dem Artikeltext (Ähnlichkeit) und aus Regeln, zum Beispiel für Deutschland Typencode oder '
        'Sachnummer in der Artikelnummer (Nummer nach dem letzten Schrägstrich, im Werkstamm 1100).',
        '<b>Seit 20.11. auch gegen Trafag Italia</b> (dein Fall Italien liefert an Spanien): Liefert TR IT die Zeile, sucht das '
        'Programm mit einer bestätigten italienischen Artikelnummer zuerst die Kosten von TR IT. Gleiches gilt für TR IN.',
        '<b>TRCH und TRAT</b> sind ausgenommen, sie verkaufen per Regel als Trafag AG.',
        '<b>Wo:</b> Finance Cockpit > Mapping (Liste, Bestätigen, Import/Export) und die Datei Mapping_JJJJ-MM-TT.xlsx neben Sales_All '
        'mit Vorschlag, Sicherheit und Gewicht (Zeilen, Umsatz).',
        '<b>Grösster Block ist Deutschland:</b> 732 Vorschläge mit 5,75 Mio. EUR Umsatz. <b>Vorprüfung gegen SAP P76 (24.11.):</b> '
        '568 sicher (Nummer oder Typencode stimmt, Material aktiv, Kostenfaktor 0,3 bis 8; 4,83 Mio. EUR), 73 prüfen (0,55 Mio. EUR), 91 '
        'wohl falsch (Ziel zum Löschen vorgemerkt oder Verpackung; 0,37 Mio. EUR). Das ist eine Prüfhilfe, keine Bestätigung.',
        '<b>UK:</b> 99 Artikel (549 Zeilen, 427\'670 GBP) tragen im Text einen Trafag-Typencode, der in SAP eindeutig auf genau ein '
        'Material zeigt; weitere 14 sind mehrdeutig, 152 ohne Treffer (UK-eigene Nummern).',
    ]))

    # 5 Laender
    s.append(PageBreak())
    s.append(P('5. Stand je Land', h2))
    s.append(P('Ampel: grün = erledigt, gelb = Vorschläge da, warten auf Bestätigung, orange = wartet auf jemanden. '
               'Zeilen mit Lieferant aus dem Export 23.11.', small))
    s.append(Spacer(1, 3))
    s.append(G.country_tiles())
    s.append(Spacer(1, 8))
    s.append(P('Anteil der Zeilen mit Lieferant, vor und nach dem Lieferanten aus dem Wareneingang', h3))
    s.append(G.supplier_bars())
    s.append(Spacer(1, 8))
    s.append(table([
        ['Land', 'Woher der Lieferant kommt', 'Stand', 'Offen bei'],
        ['CH, AT', 'Regel: immer Trafag AG', dot(GREEN, '100 % Intern / TR AG'), '–'],
        ['FR', 'Artikelstamm, sonst letzter Wareneingang',
         dot(GREEN, '2\'440 von 2\'850 Zeilen mit Lieferant (vorher 120). 12 Zeilen ohne Artikel (Gewährleistungsgutschriften)'), '–'],
        ['US', 'Artikelstamm, sonst letzter Wareneingang',
         dot(GREEN, '1\'146 von 1\'811 (vorher 6). 499 Zeilen über den Werkstamm (Entscheid 1); 77 Zeilen ohne Artikel, '
                    'v. a. Zollzuschlag „Tariff Add-On“ und Fracht'),
         'Entscheid 1, Zeilen ohne Artikel'],
        ['IT', 'Artikelstamm, Übergangsliste Paola, letzter Wareneingang',
         dot(GREEN, '20\'228 von 20\'234. Die 162 Artikel ohne Standardlieferant haben jetzt alle 510 Zeilen mit Lieferant aus dem '
                    'Wareneingang; Ware aus Indien findet die Kosten TR IN'),
         '– (Rest: 6 Hausnummern, 0 EUR)'],
        ['IN', 'Sales Type (FFM, CM, LRD), zusätzlich Wareneingang',
         dot(YELLOW, 'Sales Type entscheidet; 1\'381 von 8\'497 Zeilen zusätzlich mit Lieferant. 5 LRD-Artikel ohne Sachnummer '
                     '(Code 8): Kandidaten aus SAP vorgeschlagen, zwei sind Schweissmaschinen (Anlagegut)'),
         'Indien: Kandidaten bestätigen'],
        ['UK', 'Lieferantenfelder vollständig (3\'139 von 3\'139)',
         dot(YELLOW, '1\'485 Zeilen Code 2 (UK-eigene Nummern). Typencode in SAP: 99 Artikel eindeutig zugeordnet (549 Zeilen, '
                     '427\'670 GBP), als Mapping-Vorschläge vorbereitet'),
         'Cornell (TR09 mit zwei Namen, eine Zeile)'],
        ['ES', 'Letzter Sage-Wareneingang, seit 18.11. gebaut',
         dot(ORANGE, '0 von 8\'230 mit Lieferant, bis der neue Sage-Export läuft; 5\'248 Zeilen Code 5 (Werkstamm)'),
         'Santi: Exportlauf mit Lieferantenspalten'],
        ['DE', 'Alphaplan-Export ohne Lieferantenspalte',
         dot(YELLOW, '0 von 7\'853 mit Lieferant; deutsche Nummern meist nicht im Werkstamm (Code 1 und 6). 568 Mapping-Vorschläge '
                     'nach SAP-Vorprüfung sicher (4,83 Mio. EUR), zum Import vorbereitet'),
         'Rohail (Lieferant im Export), Mappings bestätigen'],
    ], [15 * mm, 42 * mm, 75 * mm, 38 * mm]))

    # 6 Entscheide
    s.append(P('6. Was ich von dir brauche', h2))
    s.append(G.decision_one())
    s.append(Spacer(1, 6))
    s.append(table([
        ['Nr', 'Frage', 'Grösse', 'Vorschlag'],
        ['1', '<b>Ohne Lieferant: Schweizer Werkstamm oder immer lokal?</b> Am 18.11. mündlich „das können wir so machen“; '
              'bitte schriftlich bestätigen.',
         '5\'933 Zeilen, 5,53 Mio. CHF Umsatz (2025/2026), davon ES 5\'248 und US 499 Zeilen. Herstellkosten Werkstamm 2,25, '
         '„immer lokal“ 3,10 Mio. CHF: rund 0,85 Mio. CHF Gruppenmarge Unterschied',
         'Werkstamm (so produktiv). Die Wareneingänge bestätigen ihn überwiegend'],
        ['2', '<b>Indien LRD ohne Konzernkosten</b> (Code 8): Kosten offen lassen, bis Indien die Sachnummern bestätigt?',
         '153 Zeilen, 5 Artikel (DM000083 allein 122 Zeilen, 9,62 Mio. INR). Kandidaten aus SAP: C16421, C14917, F90162',
         'ja; Schweissmaschinen FA000028/29 als Nebenerlös'],
        ['3', '<b>Wer bestätigt die Mappings?</b>', 'DE 732 Vorschläge, davon 568 nach SAP sicher (4,83 Mio. EUR); UK 96 eindeutig und plausibel',
         'die sicheren DE und UK in einem Zug bestätigen, den Rest an Rohail und Cornell'],
        ['4', '<b>Externer Lieferant, lokale Kosten fehlen:</b> mit Kosten 0 rechnen (Code 7)?', 'wenige Zeilen', 'ja (heutiger Stand)'],
        ['5', '<b>Zeilen ohne Artikel</b> (Code 9): Fracht, US-Zollzuschlag, Gewährleistungsgutschriften, IT-Personalverleih. Als '
              'eigene Gruppe „Nebenerlös“ mit Kosten 0 führen oder aus der Gruppenmarge nehmen? Gehören sie in den Ist-Umsatz '
              '(z. B. die FR-Zeile auf dem Mehrwertsteuerkonto 445710)?',
         '131 Zeilen: FR 12, US 77, IT 6, IN 36. Ohne sie hätten 16 statt 11 von 18 Gruppen eine Marge',
         'Nebenerlös mit Kosten 0'],
        ['6', '<b>Prüfzeugnisse EN10204</b> (H70210, H70310, H70312 bis H70317): Standardpreis Trafag AG 0,01 CHF, lokal z. B. '
              'H70313 zu rund 60 CHF verkauft. Wie Dienstleistung behandeln oder Standardpreis in CH pflegen?',
         '245 Zeilen, 53\'018 CHF; dazu 19 Zeilen mit Platzhalterpreisen (E99999, H99999 u. a.), 21\'127 CHF',
         'wie Dienstleistung (Nebenerlös)'],
        ['7', '<b>CH/AT:</b> gilt Intern / Trafag AG auch, wenn zum Material ein externer Einkaufsbeleg existiert?',
         'Stichprobe liegt vor (z. B. F88103)', 'offen'],
    ], [9 * mm, 62 * mm, 55 * mm, 44 * mm]))

    s.append(P('7. Am 24.11. in SAP (P76) und B1 nachgeprüft', h2))
    s.append(P('Nur gelesen: Materialstamm aus P76 (MARA mit Typencode, MBEW, MARC, MAKT), Belege aus SAP B1 (FR, US, IT), '
               'Export Sales_All und Mapping vom 23.11. Nichts wurde geändert oder bestätigt.', small))
    s.append(Spacer(1, 3))
    s.append(table([
        ['Thema', 'Ergebnis', 'Folge'],
        ['Tiefe Konzernkosten (276 Zeilen)', 'Kein Rechenfehler: die Preiseinheit wird richtig angewendet (geprüft an allen 74\'169 '
         'Zeilen mit Konzernkosten). 245 Zeilen sind EN10204-Prüfzeugnisse mit Standardpreis 0,01 CHF, 19 Zeilen Platzhalterpreise',
         'Frage 6'],
        ['„Unklar“ (Code 9, 131 Zeilen)', 'Alle ohne Artikel und ohne Lieferant; in B1 Dienstleistungsbelege: US-Zollzuschlag '
         '(26\'068 USD), Fracht, FR-Gewährleistungsgutschriften, IT-Personalverleih 2025 (86\'550 EUR)', 'Frage 5'],
        ['Italien, 162 Artikel ohne Standardlieferant', 'Stamm in B1 weiter leer, aber alle 510 Zeilen haben jetzt einen Lieferanten '
         'aus dem Wareneingang (443 Trafag AG, 67 italienische Lieferanten)', 'keine Stammpflege für die Marge nötig'],
        ['UK, Code 2', 'Typencode im Text gegen SAP: 99 Artikel eindeutig, 14 mehrdeutig, 10 nur Teilcode, 152 ohne Treffer',
         'Vorschläge zur Bestätigung'],
        ['DE-Mappings', '568 sicher, 73 prüfen, 91 wohl falsch (meist Ziel zum Löschen vorgemerkt)', 'Liste Mapping_DE_Vorpruefung_P76'],
        ['Brücken aus Belegen', 'Kundenmaterialnummern in SAP (KNMT, IC-Aufträge) und Katalognummern in B1 sind kaum gepflegt: nur 4 '
         'Artikel lassen sich so auf eine Trafag-AG-Nummer zurückführen, 79 spanische auf TR-IT-Handelsware. Kein Widerspruch zu den '
         'sicheren Vorschlägen', 'verlässliche Brücke bleibt der Typencode; Feld „Trafag-Nummer“ in den Tochter-ERP wäre die Lösung'],
        ['Indien LRD', 'Kandidaten C16421 und C14917 (Freilufthaube 87X6), F90162 (Regenschutz mit Isolation); '
         'FA000028/29 sind Schweissmaschinen', 'Frage 2'],
        ['Supplier Origin', 'Lieferant aus Wareneingang wirkt, die Herkunftsangabe fehlt aber im Export (Programmfehler)',
         'behoben 24.11., sichtbar ab dem nächsten Export je Land'],
    ], [40 * mm, 92 * mm, 38 * mm]))

    s.append(P('8. Wo du was findest', h2))
    s.append(table([
        ['Was', 'Wo'],
        ['Zuordnung je Zeile', 'Sales_All, Blatt Sales Spalten 55 bis 57 (Supplier Type, Delivering Entity, Cost Source) und 60 '
                               '(Supplier Origin); Finance Details 43, 44 und 47; Issue Topic und Mapping in den letzten Spalten'],
        ['Zuordnung je Gesellschaft', 'Sales_All, Blatt Lieferantenzuordnung'],
        ['Erklärung der Felder', 'Sales_All, Blatt Finance Filter Hilfe'],
        ['Mapping', 'Finance Cockpit > Mapping; Datei Mapping_JJJJ-MM-TT.xlsx neben Sales_All; Vorprüfung DE '
                    'Mapping_DE_Vorpruefung_P76_2026-11-24.xlsx'],
        ['Gruppenmarge', 'Finance Cockpit und Blätter Gruppenmarge Summary / Details in Sales_All'],
    ], [45 * mm, 125 * mm]))
    s.append(Spacer(1, 8))
    s.append(P('Fragen an Ingo Kohler. Die technischen Einzelheiten stehen in der Projektdokumentation des Finance Dashboards.', small))

    # Keine erzwungenen Seitenumbrueche; jede Ueberschrift bleibt mit dem folgenden Absatz und der folgenden Tabelle zusammen.
    s = [x for x in s if not isinstance(x, PageBreak)]
    grouped, i = [], 0
    while i < len(s):
        x = s[i]
        if isinstance(x, Paragraph) and x.style.name in ('h2', 'h3'):
            j = i + 1
            block = [x]
            while j < len(s) and len(block) < 5:
                block.append(s[j])
                j += 1
                if isinstance(s[j - 1], (Table, Drawing)):
                    break
            grouped.append(KeepTogether(block))
            i = j
        else:
            grouped.append(x)
            i += 1
    doc.build(grouped, onFirstPage=on_page, onLaterPages=on_page)


if __name__ == '__main__':
    build(sys.argv[1])
