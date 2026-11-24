import csv, collections
codes = set(c for c in open('italien162_codes.txt').read().split('\n') if c)
agg = collections.defaultdict(lambda: [0, 0.0, '', ''])
for r in csv.DictReader(open('s23_fd.csv', encoding='utf-8')):
    if r['TSC'] == 'TRIT' and r['Year'] in ('2025', '2026') and r['Material'].strip().upper() in codes and r['Supplier Type'] == 'Extern':
        a = agg[r['Material']]
        a[0] += 1; a[1] += float(r['Net Sales Actual'] or 0); a[2] = r['Name'][:40]; a[3] = r['Supplier number']
num = [m for m in agg if m.isdigit()]
print('Artikel extern:', len(agg), 'davon numerisch (Trafag-Sachnummer-Form):', len(num))
for m, a in sorted(agg.items(), key=lambda x: -x[1][1]):
    print(m, a[0], round(a[1]), a[3], a[2])
