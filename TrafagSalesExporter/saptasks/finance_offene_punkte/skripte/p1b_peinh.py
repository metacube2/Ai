# PEINH-Pruefung: Verhaeltnis Konzernkosten je Einheit (Export) zu STPRS/PEINH (P76), je PEINH und Waehrung
import csv, collections
from p76lib import MBEW, unit_cost, norm_mat
def f(v):
    try: return float(v) if v else 0.0
    except ValueError: return 0.0
res = collections.defaultdict(collections.Counter); mats = collections.defaultdict(set); med = collections.defaultdict(list)
for r in csv.DictReader(open('s23_fd.csv', encoding='utf-8')):
    if r['Year'] not in ('2025','2026') or 'Konzernkosten TR AG' not in r['Cost Source'] or f(r['Quantity']) == 0: continue
    m = norm_mat(r['Material']); b = MBEW.get(m); u = unit_cost(m)
    if not b or not u: continue
    pe = int(float(b['PEINH'])); q = (f(r['Cost Basis CHF']) / f(r['Quantity'])) / u
    if abs(q - 1) <= 0.01: s = 'a gleich (+-1%)'
    elif 0.8 <= q <= 1.25: s = 'b nahe (0.8-1.25, Kurs oder Preisstand)'
    elif pe > 1 and abs(q / pe - 1) <= 0.05: s = 'c = STPRS ohne PEINH'
    else: s = 'd anders'
    key = ('PEINH 1' if pe == 1 else 'PEINH >1', r['Currency'])
    res[key][s] += 1
    if pe > 1: mats[s].add(m)
    if s.startswith('b'): med[r['Currency']].append(q)
for k in sorted(res): print(k, dict(sorted(res[k].items())))
print('PEINH>1 Materialien je Klasse:', {k: len(v) for k, v in mats.items()})
import statistics
print('Median Verhaeltnis Klasse b je Waehrung:', {k: round(statistics.median(v), 4) for k, v in med.items()})
