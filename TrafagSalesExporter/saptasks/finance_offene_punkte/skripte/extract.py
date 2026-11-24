# Liest Sales_All (nur lesend) und schreibt Finance Details (alle Jahre) und Gruppenmarge Details (Gruppenschluessel + Status)
# als CSV ins Scratchpad. Aufruf: python -I -X utf8 extract.py <Sales_All.xlsx> <praefix>
import sys, csv, openpyxl
src, pre = sys.argv[1], sys.argv[2]
wb = openpyxl.load_workbook(src, read_only=True, data_only=True)

def s(v):
    if v is None:
        return ''
    if isinstance(v, float) and v.is_integer() and abs(v) < 1e15:
        return repr(v)
    return str(v).strip()

ws = wb['Finance Details']
it = ws.iter_rows(values_only=True)
for row in it:
    if row and 'TSC' in row:
        kopf = [s(h) for h in row]
        break
n = 0
with open(pre + '_fd.csv', 'w', newline='', encoding='utf-8') as f:
    w = csv.writer(f)
    w.writerow(kopf)
    for r in it:
        if not r or r[kopf.index('TSC')] is None:
            continue
        w.writerow([s(v) for v in r[:len(kopf)]])
        n += 1
print('Finance Details Zeilen', n)

ws = wb['Gruppenmarge Details']
it = ws.iter_rows(values_only=True)
kopf = [s(h) for h in next(it)]
want = ['Year', 'Status', 'Country Key', 'TSC', 'Currency', 'Invoice Number', 'Position', 'Material', 'Supplier Type', 'Cost Source', 'Quantity', 'Unit Cost', 'Sales Value', 'Known Cost Basis']
ix = [kopf.index(c) for c in want]
m = 0
with open(pre + '_gm.csv', 'w', newline='', encoding='utf-8') as f:
    w = csv.writer(f)
    w.writerow(want)
    for r in it:
        if not r or r[0] is None:
            continue
        w.writerow([s(r[i]) for i in ix])
        m += 1
print('Gruppenmarge Details Zeilen', m)
