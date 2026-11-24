# B1-Brueckenpaare aus den HanaQ-Ausgaben in eine CSV; Abgleich mit dem Mapping-Bedarf (nur lesend)
import csv, re, openpyxl, collections
from p76lib import MARA, norm_mat
rows = []
for tsc in ('TRFR', 'TRUS', 'TRIT'):
    txt = open(f'r_br2_{tsc}.out', encoding='utf-8', errors='replace').read()
    for block in re.split(r'\n=== ', txt):
        quelle = 'OSCN' if block.startswith('PAARE OSCN') else ('OITM.SuppCatNum' if block.startswith('PAARE OITM') else None)
        if not quelle:
            continue
        lines = block.strip().split('\n')
        if len(lines) < 3 or lines[2].startswith('0 |'):
            continue
        paare = lines[2].split(' | ', 1)[1]
        for p in paare.split('~'):
            a = p.split('|')
            if len(a) == 3:
                rows.append([tsc, quelle, a[0], a[1], a[2]])
with open('b1_paare.csv', 'w', newline='', encoding='utf-8-sig') as fh:
    w = csv.writer(fh, delimiter=';'); w.writerow(['TSC', 'Quelle', 'Artikel lokal', 'Partner', 'Katalognummer']); w.writerows(rows)
print('Paare', collections.Counter((r[0], r[1], r[3]) for r in rows))
wm = openpyxl.load_workbook('Mapping_2026-11-23.xlsx', read_only=True, data_only=True)
it = wm['Mapping'].iter_rows(values_only=True); mk = list(next(it))
need = {(m[0], str(m[1]).strip().upper()) for m in it if m[0]}
hit = [r for r in rows if (r[0], r[2].strip().upper()) in need]
print('davon Artikel mit Mapping-Bedarf:', len(hit), collections.Counter((r[0], r[3]) for r in hit))
print('Katalognummer in MARA aktiv:', sum(1 for r in rows if norm_mat(r[4]) in MARA and not MARA[norm_mat(r[4])]['LVORM']), 'von', len(rows))
print('Beispiele', hit[:8])
