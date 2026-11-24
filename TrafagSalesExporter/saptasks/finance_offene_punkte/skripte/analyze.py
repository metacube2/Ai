# Auswertung Aufgabe 7 aus den CSV-Auszuegen von extract.py (nur lesend).
# Aufruf: python -I -X utf8 analyze.py <praefix> <Mapping.xlsx>
import sys, csv, collections, openpyxl
pre, mapx = sys.argv[1], sys.argv[2]
TSCS = ['TRCH', 'TRAT', 'TRDE', 'TRES', 'TRFR', 'TRIT', 'TRUK', 'TRUS', 'TRIN']

def f(v):
    try:
        return float(v) if v not in ('', None) else 0.0
    except ValueError:
        return 0.0

rows = []
with open(pre + '_fd.csv', encoding='utf-8') as fh:
    for r in csv.DictReader(fh):
        if r['Year'] in ('2025', '2026'):
            for k in ('Issue Topic', 'Supplier Origin', 'Mapping'):
                r.setdefault(k, '')
            rows.append(r)
print(f'== {pre}: Finance Details 2025/2026: {len(rows)} Zeilen')

# A Lieferant je Land
print('\nA Zeilen mit Lieferant (Nummer oder Name gefuellt) / Zeilen gesamt; Supplier Type')
for t in TSCS:
    R = [r for r in rows if r['TSC'] == t]
    lief = sum(1 for r in R if r['Supplier number'] or r['Supplier name'])
    typ = collections.Counter(r['Supplier Type'] or '-' for r in R)
    print(f'  {t}: {lief} von {len(R)}   Typ: {dict(typ.most_common())}')

# B Issue Topic je Land
print('\nB Issue Topic je Land (Zeilen)')
for t in TSCS:
    c = collections.Counter(r['Issue Topic'] or 'leer' for r in rows if r['TSC'] == t)
    print(f'  {t}: ' + ', '.join(f'{k}: {v}' for k, v in sorted(c.items(), key=lambda x: (x[0] == 'leer', len(x[0]), x[0]))))

# C Supplier Origin
print('\nC Supplier Origin gesamt:', dict(collections.Counter(r['Supplier Origin'] or 'leer' for r in rows).most_common()))
for t in TSCS:
    c = collections.Counter(r['Supplier Origin'] or 'leer' for r in rows if r['TSC'] == t)
    print(f'  {t}: {dict(c.most_common())}')

# D Entscheid 1 (Topic 5)
print('\nD Entscheid 1, Issue Topic 5')
T5 = [r for r in rows if r['Issue Topic'] == '5']
def e1(R, lab):
    ns = sum(f(r['Net Sales CHF']) for r in R)
    cb = sum(f(r['Cost Basis CHF']) for r in R)
    lok = sum(f(r['Standard Cost CHF (per unit)']) * f(r['Quantity']) for r in R)
    ohne = sum(1 for r in R if f(r['Standard Cost CHF (per unit)']) == 0)
    print(f'  {lab}: {len(R)} Zeilen, Umsatz {ns/1e6:.3f} Mio CHF, HK TR AG {cb/1e6:.3f}, lokal {lok/1e6:.3f}, Diff {(lok-cb)/1e6:.3f} Mio CHF, ohne lokalen Preis {ohne}')
e1(T5, 'gesamt')
for t in TSCS:
    R = [r for r in T5 if r['TSC'] == t]
    if R:
        e1(R, t)

# E H7-Nebenbefund
print('\nE Konzernkosten TR AG unter 5 % des lokalen Preises')
cs_trag = collections.Counter(r['Cost Source'] for r in rows if 'TR AG' in r['Cost Source'])
print('  Cost Sources mit TR AG:', dict(cs_trag))
def h7(variant):
    out = []
    for r in rows:
        if 'Konzernkosten TR AG' not in r['Cost Source']:
            continue
        q = f(r['Quantity'])
        if q == 0:
            continue
        grp = f(r['Cost Basis CHF']) / q
        if variant == 'std':
            loc = f(r['Standard Cost CHF (per unit)'])
        else:
            loc = f(r['Net Sales CHF']) / q
        if loc > 0 and grp < 0.05 * loc:
            out.append(r)
    return out
for v in ('std', 'preis'):
    R = h7(v)
    ns = sum(f(r['Net Sales CHF']) for r in R)
    h = sum(1 for r in R if r['Material'].upper().startswith('H7'))
    print(f'  Variante {v}: {len(R)} Zeilen, Umsatz {ns:,.0f} CHF, H7-Material {h}, je TSC {dict(collections.Counter(r["TSC"] for r in R))}')

# F Gruppenmarge Summary aus Details
print('\nF Gruppenmarge: Gruppen mit Marge (aus Details gezaehlt)')
OPEN = {'Lieferant unklar', 'Kostenwaehrung abweichend', 'Konzernkosten fehlen'}
g = collections.OrderedDict()
with open(pre + '_gm.csv', encoding='utf-8') as fh:
    for r in csv.DictReader(fh):
        k = (r['Year'], r['Country Key'], r['TSC'], r['Currency'])
        g.setdefault(k, collections.Counter())
        if r['Status'] in OPEN:
            g[k][r['Status']] += 1
mit = [k for k, c in g.items() if not c]
print(f'  {len(mit)} von {len(g)} Gruppen mit Marge')
for k, c in g.items():
    if c:
        print('   offen:', k, dict(c))

# G Mapping
print('\nG Mapping', mapx)
wb = openpyxl.load_workbook(mapx, read_only=True, data_only=True)
ws = wb['Mapping']
it = ws.iter_rows(values_only=True)
kopf = list(next(it))
M = [dict(zip(kopf, r)) for r in it if r and r[0]]
print('  Zeilen', len(M), ' Status:', dict(collections.Counter(m['Status'] for m in M)), ' Effective:', dict(collections.Counter(m['Effective'] for m in M)))
for t in sorted(set(m['TSC'] for m in M)):
    MM = [m for m in M if m['TSC'] == t]
    st = collections.Counter(m['Status'] for m in MM)
    vor = [m for m in MM if m['Status'] == 'Vorschlag']
    print(f'  {t}: {dict(st)}; Vorschlaege Umsatz {sum(f(m["Net Sales (local currency)"]) for m in vor)/1e6:.3f} Mio {MM[0]["Currency"]}')
print('  bestaetigt:', [(m['TSC'], m['Local Material'], m['Group Material Number'], m['Reviewed By'], m['Reviewed At']) for m in M if str(m['Status']).lower().startswith('best')][:30])

# H Unklar / Topic 9
print('\nH Supplier Type Unklar oder Issue Topic 9')
for t in TSCS:
    R = [r for r in rows if r['TSC'] == t and (r['Supplier Type'] == 'Unklar' or r['Issue Topic'] == '9')]
    if not R:
        continue
    t9 = sum(1 for r in R if r['Issue Topic'] == '9')
    leer = sum(1 for r in R if not r['Material'])
    lief = sum(1 for r in R if r['Supplier number'] or r['Supplier name'] or r['Supplier country'])
    byy = collections.defaultdict(lambda: [0, 0.0, 0.0])
    for r in R:
        b = byy[r['Year']]; b[0] += 1; b[1] += f(r['Net Sales Actual']); b[2] += f(r['Net Sales CHF'])
    print(f'  {t}: {len(R)} Zeilen (Topic 9: {t9}), ohne Material {leer}, mit Lieferantenfeld {lief}; je Jahr ' +
          '; '.join(f'{y}: {v[0]} Z., {v[1]:,.0f} lokal, {v[2]:,.0f} CHF' for y, v in sorted(byy.items())))
