# Extraktionsdatum je TSC und Supplier Origin je TSC/Extraktionsdatum aus Blatt Sales (nur lesend)
import sys, collections, openpyxl
wb = openpyxl.load_workbook(sys.argv[1], read_only=True, data_only=True)
it = wb['Sales'].iter_rows(values_only=True)
k = list(next(it)); ix = {h: i for i, h in enumerate(k)}
c = collections.Counter()
for r in it:
    if not r or not r[ix['TSC']]:
        continue
    d = str(r[ix['extraction date']] or '')[:10]
    so = r[ix['Supplier Origin']] if 'Supplier Origin' in ix else '-'
    c[(str(r[ix['TSC']]), d, str(so))] += 1
for key in sorted(c):
    print(key, c[key])
