# Blatt Sales: Extraktionszeit je TSC (volle Zeit) und die 7 IT-Artikel, die in Finance Details fehlen (nur lesend)
import sys, collections, openpyxl
SEVEN = {'66176', 'M_IT01_001778', 'M_IT01_000842', 'M_IT01_000760', 'M_IT01_000531', 'M_IT01_000320', 'M_IT01_000311'}
wb = openpyxl.load_workbook(sys.argv[1], read_only=True, data_only=True)
it = wb['Sales'].iter_rows(values_only=True)
k = list(next(it)); ix = {h: i for i, h in enumerate(k)}
ts = collections.defaultdict(collections.Counter)
for r in it:
    if not r or not r[ix['TSC']]:
        continue
    t = str(r[ix['TSC']])
    ts[t][str(r[ix['extraction date']])] += 1
    m = str(r[ix['Material']] or '').strip().upper()
    if t == 'TRIT' and m in SEVEN:
        print('IT7', m, r[ix['Document Type']], r[ix['invoice date']], 'Qty', r[ix['Quantity']], 'Wert', r[ix['Sales Price/Value']],
              'Include', r[ix['Finance | Include']], 'Year', r[ix['Finance | Year']], 'Lief', r[ix['Supplier number']],
              'Typ', r[ix['Supplier Type']], 'Origin', r[ix['Supplier Origin']])
for t, c in sorted(ts.items()):
    print(t, c.most_common(4))
# Mapping TRIN
wm = openpyxl.load_workbook(sys.argv[2], read_only=True, data_only=True)
it = wm['Mapping'].iter_rows(values_only=True)
mk = list(next(it))
for v in it:
    m = dict(zip(mk, v))
    if m['TSC'] == 'TRIN':
        print('MAP-TRIN', m['Local Material'], m['Status'], m['Rows'], m['Net Sales (local currency)'], m['Method'], m['Group Material Number'])
