# Aufgabe 6: 162 TR-IT-Artikel ohne Standardlieferant (Liste Paola 13.11.) gegen den Export 23.11. (nur lesend)
import csv, collections, openpyxl
wb = openpyxl.load_workbook('paola_att/TRIT_items_without_preferred_vendor_2026-11-13.xlsx', read_only=True, data_only=True)
it = wb.worksheets[0].iter_rows(values_only=True)
next(it)
liste = {}
for r in it:
    if r and r[0] is not None:
        liste[str(r[0]).strip().upper()] = (r[1], r[2] or 0, r[3] or 0)
print('Liste: Artikel', len(liste), 'Zeilen', sum(v[1] for v in liste.values()), 'Umsatz EUR', round(sum(v[2] for v in liste.values())))

def f(v):
    try:
        return float(v) if v else 0.0
    except ValueError:
        return 0.0

rows = [r for r in csv.DictReader(open('s23_fd.csv', encoding='utf-8')) if r['TSC'] == 'TRIT' and r['Year'] in ('2025', '2026')]
L = [r for r in rows if r['Material'].strip().upper() in liste]
print('Export 23.11.: Zeilen dieser Artikel', len(L), 'Umsatz EUR', round(sum(f(r['Net Sales Actual']) for r in L)))
art = collections.defaultdict(lambda: {'mit': 0, 'ohne': 0, 'u_mit': 0.0, 'u_ohne': 0.0, 'lief': collections.Counter(), 'typ': collections.Counter()})
for r in L:
    a = art[r['Material'].strip().upper()]
    if r['Supplier number'] or r['Supplier name']:
        a['mit'] += 1; a['u_mit'] += f(r['Net Sales Actual']); a['lief'][(r['Supplier number'], r['Supplier name'])] += 1
    else:
        a['ohne'] += 1; a['u_ohne'] += f(r['Net Sales Actual'])
    a['typ'][(r['Supplier Type'], r['Delivering Entity'], r['Cost Source'], r['Issue Topic'], r['Supplier Origin'])] += 1
voll = [m for m, a in art.items() if a['ohne'] == 0]
teil = [m for m, a in art.items() if a['ohne'] and a['mit']]
leer = [m for m, a in art.items() if a['mit'] == 0]
nicht = [m for m in liste if m not in art]
print(f'Artikel im Export: {len(art)}; ganz mit Lieferant {len(voll)}, teilweise {len(teil)}, ohne {len(leer)}; nicht im Export 2025/26: {len(nicht)}')
print('Zeilen mit Lieferant', sum(a['mit'] for a in art.values()), 'EUR', round(sum(a['u_mit'] for a in art.values())),
      '| ohne', sum(a['ohne'] for a in art.values()), 'EUR', round(sum(a['u_ohne'] for a in art.values())))
lc = collections.Counter(); lcu = collections.Counter()
for a in art.values():
    for k, v in a['lief'].items():
        lc[k] += v
print('Lieferanten (Nummer, Name) Zeilen:', lc.most_common(15))
tc = collections.Counter()
for a in art.values():
    tc.update(a['typ'])
print('Typ/Entity/CostSource/Topic/Origin:', tc.most_common(15))
# Restliste
rest = sorted(teil + leer, key=lambda m: -art[m]['u_ohne'])
with open('italien_rest.csv', 'w', newline='', encoding='utf-8-sig') as fh:
    w = csv.writer(fh, delimiter=';')
    w.writerow(['Item code (B1)', 'Description', 'Zeilen ohne Lieferant', 'Umsatz EUR ohne Lieferant', 'Zeilen mit Lieferant', 'Erste/letzte Rechnung laut Liste'])
    for m in rest:
        a = art[m]
        w.writerow([m, liste[m][0], a['ohne'], round(a['u_ohne'], 2), a['mit'], ''])
print('Restliste italien_rest.csv', len(rest))
open('italien162_codes.txt', 'w').write('\n'.join(sorted(liste)))
open('italien_rest_codes.txt', 'w').write('\n'.join(rest))
print('nicht im Export (Beispiele):', nicht[:10])
