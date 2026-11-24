# DE: lokale Kosten je Einheit gegen Verkaufspreis je Einheit (Plausibilitaet der lokalen Kostenbasis)
import csv, collections, statistics
def f(v):
    try: return float(v) if v else 0.0
    except ValueError: return 0.0
q = []; z = 0; nul = 0
for r in csv.DictReader(open('s23_fd.csv', encoding='utf-8')):
    if r['TSC'] == 'TRDE' and r['Year'] in ('2025','2026'):
        z += 1
        if f(r['Quantity']) > 0 and f(r['Net Sales CHF']) > 0:
            c = f(r['Standard Cost CHF (per unit)'])
            if c > 0: q.append(c / (f(r['Net Sales CHF']) / f(r['Quantity'])))
            else: nul += 1
q.sort(); n = len(q)
print('TRDE Zeilen', z, 'mit Kosten', n, 'ohne', nul, 'Kosten/Preis Median', round(statistics.median(q),3), 'P10', round(q[n//10],3), 'P90', round(q[9*n//10],3), '>1:', sum(1 for x in q if x>1), '>10:', sum(1 for x in q if x>10))
