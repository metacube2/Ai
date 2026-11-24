# Zusatzzahlen fuer NOTES: Code 8 je Artikel, UK Code 2 Summe, Topic-9-Wirkung, H7-Muster (nur lesend)
import sys, csv, collections
pre = sys.argv[1]
def f(v):
    try:
        return float(v) if v else 0.0
    except ValueError:
        return 0.0
rows = [r for r in csv.DictReader(open(pre + '_fd.csv', encoding='utf-8')) if r['Year'] in ('2025', '2026')]
c8 = collections.defaultdict(lambda: [0, 0.0])
for r in rows:
    if r['TSC'] == 'TRIN' and r.get('Issue Topic') == '8':
        c8[r['Material']][0] += 1; c8[r['Material']][1] += f(r['Net Sales Actual'])
print('Code 8:', len(c8), 'Artikel', sum(v[0] for v in c8.values()), 'Zeilen', {k: (v[0], round(v[1])) for k, v in c8.items()})
uk = [r for r in rows if r['TSC'] == 'TRUK' and r.get('Issue Topic') == '2']
print('UK Code 2:', len(uk), 'Zeilen', len(set(r['Material'] for r in uk)), 'Artikel', round(sum(f(r['Net Sales Actual']) for r in uk)), 'GBP', round(sum(f(r['Net Sales CHF']) for r in uk)), 'CHF')
h7 = collections.Counter()
for r in rows:
    if 'Konzernkosten TR AG' in r['Cost Source'] and f(r['Quantity']) != 0:
        q = f(r['Quantity']); g = f(r['Cost Basis CHF']) / q; l = f(r['Standard Cost CHF (per unit)'])
        if l > 0 and g < 0.05 * l:
            h7[r['Material'][:4]] += 1
print('H7-Muster Praefix:', h7.most_common(10))
