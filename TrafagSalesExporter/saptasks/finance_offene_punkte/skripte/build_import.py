# Baut die Import-Datei fuer /finance-cockpit/mapping (Format MaterialMappingExcel, Produktivstand rx2311). Nur Datei, kein Import.
# Inhalt: DE-Vorschlaege Klasse "1 sicher" (Faktor 0,3-8) und UK-Typencode "eindeutig" mit Faktor 0,5-6 und aktivem Ziel.
import csv, openpyxl
from openpyxl.styles import Font
from p76lib import MAKT, norm_mat, active
OUT = r'C:\Users\koi\Downloads\Mapping_Import_DE_UK_sicher_2026-11-24.xlsx'
HEAD = ["TSC", "Local Material", "Local Text", "Group Material Number", "Text Trafag AG", "Delivering Entity", "Delivering Material Number",
        "Method", "Method Detail", "Confidence", "Status", "Reviewed By", "Reviewed At", "Effective", "Rows", "Net Sales (local currency)", "Currency"]
# Original-Zeilen des Mapping-Exports 23.11. (Local Material, Method, Confidence, Rows, Net Sales genau wie dort)
wm = openpyxl.load_workbook('Mapping_2026-11-23.xlsx', read_only=True, data_only=True)
it = wm['Mapping'].iter_rows(values_only=True); mk = list(next(it))
MAP = {}
for v in it:
    m = dict(zip(mk, v))
    if m['TSC']:
        MAP[(m['TSC'], str(m['Local Material']))] = m
# DE sicher aus der Vorpruefung (Faktor 8)
wb = openpyxl.load_workbook(r'C:\Users\koi\Downloads\Mapping_DE_Vorpruefung_P76_2026-11-24.xlsx', read_only=True, data_only=True)
it = wb['Liste'].iter_rows(values_only=True); h = next(it)
out = []
for v in it:
    r = dict(zip(h, v))
    if r['Klasse'] != '1 sicher':
        continue
    m = MAP[('TRDE', str(r['Lokale Nummer']))]
    assert str(m['Group Material Number']) == str(r['Vorgeschlagene Gruppennummer']), r['Lokale Nummer']
    out.append(['TRDE', str(m['Local Material']), m['Local Text'] or '', str(m['Group Material Number']), MAKT[norm_mat(str(m['Group Material Number']))].get('D', '') or (m['Text Trafag AG'] or ''),
                '', '', m['Method'] or 'Regel', (m['Method Detail'] or '') + ' | Vorpruefung P76 24.11. sicher (Faktor ' + str(r['Faktor lokal/STPRS']) + ')',
                str(int(m['Confidence'] or 0)), 'bestaetigt', '', '', '', m['Rows'], m['Net Sales (local currency)'], m['Currency']])
nde = len(out)
# UK eindeutig, Faktor 0,5-6, Ziel aktiv
for r in csv.DictReader(open('uk_typencode_vorschlag.csv', encoding='utf-8-sig'), delimiter=';'):
    if r['Klasse'] != 'eindeutig' or not r['Faktor lokal/STPRS']:
        continue
    fk = float(r['Faktor lokal/STPRS']); tg = r['Vorschlag Gruppennummer']
    if not (0.5 <= fk <= 6) or not active(tg):
        continue
    m = MAP.get(('TRUK', r['UK Material']), {})
    out.append(['TRUK', r['UK Material'], (m.get('Local Text') or r['UK Text'])[:500], tg, MAKT[norm_mat(tg)].get('D', ''), '', '',
                'Hand', 'UK Typencode ' + r['Typencode im Text'] + ' = MARA-ZZTYP P76 (Vorpruefung 24.11., Faktor ' + str(fk) + ')',
                '100', 'bestaetigt', '', '', '', int(r['Zeilen']), float(r['Umsatz GBP']), 'GBP'])
nuk = len(out) - nde
wbo = openpyxl.Workbook(); ws = wbo.active; ws.title = 'Mapping'
ws.append(HEAD)
for c in ws[1]: c.font = Font(bold=True)
for row in out:
    ws.append(row)
for row in ws.iter_rows(min_row=2):
    for c in row[:13]:
        c.number_format = '@'
        if c.value is not None and not isinstance(c.value, str):
            c.value = str(c.value)
ws.auto_filter.ref = ws.dimensions; ws.freeze_panes = 'A2'
hp = wbo.create_sheet('Hinweis')
for t in ['Import-Datei fuer Finance Cockpit > Mapping (Excel importieren). Erstellt 24.11.2026 als Pruefhilfe, NICHT importiert.',
          f'TRDE: {nde} Vorschlaege der Klasse sicher aus Mapping_DE_Vorpruefung_P76_2026-11-24.xlsx (Faktor 0,3 bis 8, Entscheid Ingo 24.11.).',
          f'TRUK: {nuk} eindeutige Typencode-Treffer (ganzer Code, genau ein Material in MARA ohne Loeschvormerkung, Ziel in MARC 1100 aktiv, Faktor 0,5 bis 6).',
          'Status bestaetigt: die Zeilen wirken nach dem Import (Pruefer = angemeldeter Benutzer). Wer nur Vorschlaege will, setzt Spalte Status auf Vorschlag.',
          'Der Import liest nur das Blatt Mapping (Spalten ueber die Kopfzeile). Rows/Net Sales/Effective/Reviewed At liest der Import nicht.']:
    hp.append([t])
wbo.save(OUT)
print('gespeichert', OUT, 'TRDE', nde, 'TRUK', nuk)
