# Punkt 1: H7-Zeilen gegen MBEW 1100 (PEINH-Anwendung) und Einordnung Zeugnis/Platzhalter; dazu PEINH-Pruefung ueber alle
# Zeilen mit Konzernkosten TR AG im Export 23.11. (nur lesend)
import csv, collections, re
from p76lib import MBEW, MAKT, MARA, unit_cost, norm_mat
def f(v):
    try: return float(v) if v else 0.0
    except ValueError: return 0.0
ZEUG = re.compile(r'EN\s?10204|ZEUGNIS|WERKSBESCH|ZERTIFIKAT|CERTIFI|PRUEFPROT|PRÜFPROT|INSPECTION|ABNAHME', re.I)
H = list(csv.DictReader(open('h7_zeilen.csv', encoding='utf-8-sig'), delimiter=';'))
klass = collections.Counter(); ku = collections.Counter(); km = collections.defaultdict(set)
peinh_h = collections.Counter()
for r in H:
    m = norm_mat(r['Material']); u = unit_cost(m); b = MBEW.get(m)
    peinh_h[b['PEINH'] if b else 'fehlt'] += 1
    txt = ' '.join([r['Name'], MAKT[m].get('D', ''), MAKT[m].get('E', '')])
    if ZEUG.search(txt): k = 'Zeugnis/Zertifikat'
    elif u is not None and u <= 0.05: k = 'Platzhalterpreis (STPRS/PEINH <= 0.05 CHF)'
    else: k = 'anderes'
    klass[k] += 1; ku[k] += f(r['Net Sales CHF']); km[k].add(m)
print('H7 276: PEINH-Verteilung', dict(peinh_h))
for k in klass: print(f'  {k}: {klass[k]} Zeilen, {ku[k]:,.0f} CHF, {len(km[k])} Materialien {sorted(km[k])[:12]}')
# PEINH-Pruefung ueber alle Konzernkosten-TR-AG-Zeilen
rows = [r for r in csv.DictReader(open('s23_fd.csv', encoding='utf-8')) if r['Year'] in ('2025', '2026') and 'Konzernkosten TR AG' in r['Cost Source'] and f(r['Quantity']) != 0]
res = collections.defaultdict(lambda: collections.Counter()); mats = collections.defaultdict(lambda: collections.defaultdict(set)); bsp = {}
for r in rows:
    m = norm_mat(r['Material']); b = MBEW.get(m)
    if not b: res['ohne MBEW']['-'] += 1; continue
    pe = b['PEINH']; u = unit_cost(m); g = f(r['Cost Basis CHF']) / f(r['Quantity'])
    if u == 0: s = 'STPRS 0'
    elif abs(g / u - 1) <= 0.01: s = 'gleich STPRS/PEINH'
    elif abs(g / float(b['STPRS']) - 1) <= 0.01: s = 'gleich STPRS (PEINH NICHT angewendet)'
    else: s = 'anders (Preis seit Cache geaendert?)'
    res[pe][s] += 1; mats[pe][s].add(m)
    if pe != '1' and s != 'gleich STPRS/PEINH': bsp.setdefault((pe, s), (m, b['STPRS'], pe, round(g, 6)))
print('\nAlle Zeilen mit Konzernkosten TR AG:', len(rows))
for pe, c in sorted(res.items()):
    print(f'  PEINH {pe}: ' + '; '.join(f'{s}: {n} Z. / {len(mats[pe][s])} Mat.' for s, n in c.items()))
print('Beispiele Abweichung PEINH<>1:', bsp)
