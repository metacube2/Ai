# Wie viele Gruppen haetten eine Marge, wenn Zeilen ohne Material mit Status "Lieferant unklar" nicht sperren wuerden
import csv, collections, sys
OPEN = {'Lieferant unklar', 'Kostenwaehrung abweichend', 'Konzernkosten fehlen'}
g = collections.OrderedDict(); unklar_mit_mat = 0
for r in csv.DictReader(open(sys.argv[1] + '_gm.csv', encoding='utf-8')):
    k = (r['Year'], r['Country Key'], r['TSC'], r['Currency'])
    g.setdefault(k, collections.Counter())
    if r['Status'] == 'Lieferant unklar' and r['Material'] == '':
        continue
    if r['Status'] == 'Lieferant unklar':
        unklar_mit_mat += 1
    if r['Status'] in OPEN:
        g[k][r['Status']] += 1
print('Gruppen mit Marge ohne die Zeilen ohne Material:', sum(1 for c in g.values() if not c), 'von', len(g), '| unklar mit Material:', unklar_mit_mat)
for k, c in g.items():
    if c:
        print('  offen', k, dict(c))
