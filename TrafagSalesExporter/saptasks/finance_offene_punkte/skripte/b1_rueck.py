# TRIT-OSCN (Trafag-Gesellschaften als Kunden): passt die Katalognummer zu einer lokalen Nummer mit Mapping-Bedarf der Kundengesellschaft?
import csv, collections, openpyxl
from p76lib import MARA, norm_mat
P2T = {'C_CH01_0304792': 'TRES', 'C_CH01_0017655': 'TRFR', 'C_CH01_0019719': 'TRAT', 'C_CH01_0000820': 'TRDE', 'C_CH01_0019684': 'TRUK',
       'C_CH01_0304546': 'TRUS', 'C_CH01_0302179': 'TRCH', 'C_CH01_0024687': 'CZ', 'C_CH01_0304698': 'JP'}
def k(x):
    x = ''.join(str(x or '').upper().split()); return x.lstrip('0') or x
wm = openpyxl.load_workbook('Mapping_2026-11-23.xlsx', read_only=True, data_only=True)
it = wm['Mapping'].iter_rows(values_only=True); mk = list(next(it))
need = collections.defaultdict(dict)
for v in it:
    m = dict(zip(mk, v))
    if m['TSC']: need[m['TSC']][k(m['Local Material'])] = m
R = [r for r in csv.DictReader(open('b1_paare.csv', encoding='utf-8-sig'), delimiter=';') if r['TSC'] == 'TRIT' and r['Quelle'] == 'OSCN']
c = collections.Counter(); ex = collections.defaultdict(list)
for r in R:
    t = P2T.get(r['Partner'], '?')
    hit = k(r['Katalognummer']) in need.get(t, {})
    mara = norm_mat(r['Katalognummer']) in MARA
    c[(t, 'Treffer Mapping-Bedarf' if hit else 'kein Treffer', 'Katalognr in MARA' if mara else '')] += 1
    if len(ex[t]) < 4: ex[t].append((r['Artikel lokal'], r['Katalognummer'][:40]))
for key in sorted(c): print(key, c[key])
for t, e in ex.items(): print(t, e)
