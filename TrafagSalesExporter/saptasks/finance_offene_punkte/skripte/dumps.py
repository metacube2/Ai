# Vorbereitende Auszuege aus dem neuesten Export (nur lesend). Ergebnis-CSVs im Scratchpad (Namen von Kunden/Lieferanten moeglich).
# Aufruf: python -I -X utf8 dumps.py <praefix> <Mapping.xlsx>
import sys, csv, collections, openpyxl
pre, mapx = sys.argv[1], sys.argv[2]

def f(v):
    try:
        return float(v) if v not in ('', None) else 0.0
    except ValueError:
        return 0.0

rows = []
with open(pre + '_fd.csv', encoding='utf-8') as fh:
    rd = csv.DictReader(fh)
    kopf = rd.fieldnames
    for r in rd:
        if r['Year'] in ('2025', '2026'):
            rows.append(r)

def write(name, header, data):
    with open(name, 'w', newline='', encoding='utf-8-sig') as fh:
        w = csv.writer(fh, delimiter=';')
        w.writerow(header)
        for d in data:
            w.writerow(d)
    print('geschrieben', name, len(data))

# 1 Unklar / Topic 9 (alle Spalten)
U = [r for r in rows if r['Supplier Type'] == 'Unklar' or r['Issue Topic'] == '9']
write('unklar_zeilen.csv', kopf, [[r[k] for k in kopf] for r in U])

# 2 h7_zeilen.csv: Konzernkosten TR AG unter 5 % der lokalen Standardkosten (je Einheit, CHF)
H = []
for r in rows:
    if 'Konzernkosten TR AG' not in r['Cost Source']:
        continue
    q = f(r['Quantity'])
    if q == 0:
        continue
    grp = f(r['Cost Basis CHF']) / q
    loc = f(r['Standard Cost CHF (per unit)'])
    if loc > 0 and grp < 0.05 * loc:
        H.append([r['TSC'], r['Year'], r['Invoice Number'], r['Position on invoice'], r['Material'], r['Name'], r['Quantity'], '',
                  r['Standard Cost'], r['Standard Cost Currency'], round(loc, 6), round(grp, 6), round(f(r['Cost Basis CHF']), 4),
                  r['Net Sales CHF'], r['Cost Source']])
write('h7_zeilen.csv', ['TSC', 'Year', 'Invoice Number', 'Position', 'Material', 'Name', 'Quantity', 'Mengeneinheit (nicht im Export)',
                        'Lokale Standardkosten', 'Waehrung lokal', 'Lokale Kosten CHF je Einheit', 'Konzernkosten TR AG CHF je Einheit',
                        'Cost Basis CHF', 'Net Sales CHF', 'Cost Source'], H)

# 3 uk_code2.csv
agg = collections.OrderedDict()
for r in rows:
    if r['TSC'] == 'TRUK' and r['Issue Topic'] == '2':
        a = agg.setdefault(r['Material'], {'text': r['Name'], 'z': 0, 'gbp': 0.0, 'chf': 0.0, 'lief': collections.Counter()})
        a['z'] += 1; a['gbp'] += f(r['Net Sales Actual']); a['chf'] += f(r['Net Sales CHF']); a['lief'][r['Supplier name']] += 1
data = sorted(([m, a['text'], a['z'], round(a['gbp'], 2), round(a['chf'], 2), a['lief'].most_common(1)[0][0]] for m, a in agg.items()), key=lambda x: -x[3])
write('uk_code2.csv', ['Material', 'Text', 'Zeilen', 'Umsatz GBP', 'Umsatz CHF', 'Haeufigster Lieferant'], data)

# 4 in_lrd_code8.csv
agg = collections.OrderedDict()
for r in rows:
    if r['TSC'] == 'TRIN' and r['Issue Topic'] == '8':
        a = agg.setdefault(r['Material'], {'text': r['Name'], 'z': 0, 'inr': 0.0, 'chf': 0.0})
        a['z'] += 1; a['inr'] += f(r['Net Sales Actual']); a['chf'] += f(r['Net Sales CHF'])
data = sorted(([m, a['text'], a['z'], round(a['inr'], 2), round(a['chf'], 2)] for m, a in agg.items()), key=lambda x: -x[3])
write('in_lrd_code8.csv', ['Material', 'Text', 'Zeilen', 'Umsatz INR', 'Umsatz CHF'], data)

# 5 de_mapping.csv: Vorschlaege TRDE aus dem Mapping-Export, lokale Kosten aus Finance Details TRDE 2025/2026
loc = collections.defaultdict(lambda: [0, 0.0, 0.0])
for r in rows:
    if r['TSC'] == 'TRDE':
        a = loc[r['Material'].strip().upper()]
        a[0] += 1; a[1] += f(r['Net Sales Actual']); a[2] += f(r['Standard Cost']) * f(r['Quantity'])
wb = openpyxl.load_workbook(mapx, read_only=True, data_only=True)
it = wb['Mapping'].iter_rows(values_only=True)
mk = list(next(it))
data = []
for v in it:
    m = dict(zip(mk, v))
    if m['TSC'] != 'TRDE':
        continue
    l = loc.get(str(m['Local Material']).strip().upper(), [0, 0.0, 0.0])
    data.append([m['Local Material'], m['Local Text'], m['Group Material Number'], m['Text Trafag AG'], m['Method'], m['Method Detail'],
                 m['Confidence'], m['Status'], m['Rows'], m['Net Sales (local currency)'], m['Currency'], l[0], round(l[1], 2), round(l[2], 2)])
data.sort(key=lambda x: -f(x[9]))
write('de_mapping.csv', ['Lokale Nummer', 'Text', 'Vorgeschlagene Gruppennummer', 'Text Trafag AG', 'Verfahren', 'Verfahren Detail',
                         'Sicherheit', 'Status', 'Zeilen (Mapping)', 'Umsatz (Mapping, lokal)', 'Waehrung',
                         'Zeilen FD 2025/26', 'Umsatz FD EUR', 'Lokale Kosten EUR (Standard Cost x Menge)'], data)
