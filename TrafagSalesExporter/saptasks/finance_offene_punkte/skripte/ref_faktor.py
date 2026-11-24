# Referenz: Verhaeltnis lokale Kosten / Konzernkosten TR AG bei Zeilen, die die Konzernkosten schon finden (je TSC)
import csv, collections, statistics
def f(v):
    try: return float(v) if v else 0.0
    except ValueError: return 0.0
q = collections.defaultdict(list)
for r in csv.DictReader(open('s23_fd.csv', encoding='utf-8')):
    if r['Year'] in ('2025','2026') and 'Konzernkosten TR AG' in r['Cost Source'] and f(r['Quantity']) != 0 and f(r['Standard Cost CHF (per unit)']) > 0 and f(r['Cost Basis CHF']) > 0:
        q[r['TSC']].append(f(r['Standard Cost CHF (per unit)']) / (f(r['Cost Basis CHF']) / f(r['Quantity'])))
for t, v in sorted(q.items()):
    v.sort(); n = len(v)
    print(t, n, 'Median', round(statistics.median(v), 3), 'P10', round(v[n//10], 3), 'P90', round(v[9*n//10], 3), 'Anteil 0.3-3', round(sum(1 for x in v if 0.3 <= x <= 3)/n, 3), 'Anteil 0.5-6', round(sum(1 for x in v if 0.5 <= x <= 6)/n, 3))
