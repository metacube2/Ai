"""Referenz fuer QmMeldungSet aus T76 per RFC (nur lesen) nachbauen und erwartete JSON-Laengen je Feld rechnen.
Aufruf: python -I -X utf8 referenz_rfc.py <repo> <arbeitsordner>
Schreibt <arbeitsordner>/ref_qm.json (enthaelt Kunden- und Personennamen: nur im Scratchpad ablegen, nicht committen)
und gibt je Property die erwartete Mehrlaenge von $select=Qmnum,<Property> gegenueber $select=Qmnum aus.
Danach referenz_sysstat_menge.py mit denselben Argumenten (liest ref_qm.json). NOTES.md Abschnitt 4.
Passwort: T76-Ablage von SapCredential.ps1 (RunSapProbe.ps1); fuer P76 nicht verwendbar.
"""
import json, subprocess, sys, os, collections

repo, d = sys.argv[1], sys.argv[2]
probe = os.path.join(repo, '.tmp_sap_probe', 'RunSapProbe.ps1')


def read(table, fields, where, rows=999999):
    cmd = ['powershell', '-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', probe, 'table-read', table,
           '--fields', ','.join(fields), '--rowcount', str(rows), '--format', 'json', '--quiet']
    if where:
        cmd[8:8] = ['--where', where]
    out = subprocess.run(cmd, capture_output=True, cwd=repo).stdout.decode('utf-8', errors='replace')
    i, j = out.find('['), out.rfind(']')
    if i < 0:
        if 'TABLE_WITHOUT_DATA' in out:
            return []
        raise SystemExit('Fehler bei %s: %s' % (table, out[-400:]))
    return json.loads(out[i:j + 1])


def chunks(vals, n=40):
    vals = sorted(set(vals))
    for k in range(0, len(vals), n):
        yield vals[k:k + n]


def inlist(field, vals):
    return '%s IN (%s)' % (field, ','.join("'%s'" % v for v in vals))


qmel = read('QMEL', ['QMNUM', 'QMART', 'OBJNR', 'ERDAT', 'QMTXT', 'KUNUM', 'MATNR', 'RKMNG', 'MGEIN', 'RKDAT',
                     'PRODDAT', 'KZLOESCH'], "QMART = 'Q1'")
closed = set()
for r in read('JEST', ['OBJNR', 'STAT'], "OBJNR LIKE 'QM%' AND INACT = ' ' AND STAT IN ('I0072','I0076')"):
    closed.add(r['OBJNR'])
kopf = sorted([r for r in qmel if r['OBJNR'] not in closed and not r['KZLOESCH'].strip()], key=lambda r: r['QMNUM'])
print('offen', len(kopf))

kna1 = {}
for c in chunks([r['KUNUM'] for r in kopf if r['KUNUM'].strip()]):
    for r in read('KNA1', ['KUNNR', 'NAME1'], inlist('KUNNR', c)):
        kna1[r['KUNNR']] = r['NAME1']
makt = {}
for c in chunks([r['MATNR'] for r in kopf if r['MATNR'].strip()], 25):
    for r in read('MAKT', ['MATNR', 'SPRAS', 'MAKTX'], inlist('MATNR', c) + " AND SPRAS IN ('D','E')"):
        makt[(r['MATNR'], r['SPRAS'])] = r['MAKTX']
qmsm = collections.defaultdict(list)
for c in chunks([r['QMNUM'] for r in kopf], 30):
    for r in read('QMSM', ['QMNUM', 'ERDAT', 'KZLOESCH'], inlist('QMNUM', c)):
        if not r['KZLOESCH'].strip():
            qmsm[r['QMNUM']].append(r['ERDAT'])
ihpa = {}
for c in chunks([r['OBJNR'] for r in kopf], 25):
    for r in sorted(read('IHPA', ['OBJNR', 'COUNTER', 'PARNR', 'KZLOESCH'], inlist('OBJNR', c) + " AND PARVW = 'VW'"),
                    key=lambda r: r['COUNTER']):
        if not r['KZLOESCH'].strip() and r['OBJNR'] not in ihpa:
            ihpa[r['OBJNR']] = r['PARNR'].strip()
names = {}
pernrs = sorted(set(v.zfill(8)[-8:] for v in ihpa.values()))
for c in chunks(pernrs):
    rows = read('PA0001', ['PERNR', 'ENDDA', 'ENAME'], inlist('PERNR', c))
    for r in sorted(rows, key=lambda r: r['ENDDA']):
        names[r['PERNR']] = r['ENAME']
ust = collections.Counter(r['OBJNR'] for r in read('JEST', ['OBJNR', 'STAT'],
                                                    "OBJNR LIKE 'QM%' AND INACT = ' ' AND STAT LIKE 'E%'")
                          if r['OBJNR'] in {k['OBJNR'] for k in kopf})
print('offene mit Anwenderstatus', sum(ust.values()))

def nz(v):
    v = v.strip()
    return v.lstrip('0') or ('0' if v else '')

def dat(v):
    v = v.strip()
    return '' if v in ('', '00000000') else v

out = []
for r in kopf:
    m = r['MATNR'].strip()
    sm = sorted(x for x in qmsm.get(r['QMNUM'], []) if x != '00000000')
    pn = ihpa.get(r['OBJNR'])
    out.append({
        'Qmnum': nz(r['QMNUM']), 'Qmart': r['QMART'].strip(), 'Status': '', 'Kunnr': nz(r['KUNUM']),
        'Kunde': kna1.get(r['KUNUM'], '').strip() if r['KUNUM'].strip() else '',
        'Matnr': nz(m), 'Maktx': (makt.get((m, 'D')) or makt.get((m, 'E')) or '').strip() if m else '',
        'MengeRoh': r['RKMNG'].strip(), 'Mengeneinheit': r['MGEIN'].strip(), 'Fehlerbild': r['QMTXT'].rstrip(),
        'Angelegt': dat(r['ERDAT']), 'Ruecklieferung': dat(r['RKDAT']), 'Produktion': dat(r['PRODDAT']),
        'Massnahme': sm[0] if sm else '', 'Bearbeiter': names.get(pn.zfill(8)[-8:], '').strip() if pn else '',
        'Produktlinie': '',
    })
json.dump(out, open(os.path.join(d, 'ref_qm.json'), 'w', encoding='utf-8'), ensure_ascii=False, indent=0)

def blen(s):
    return len(json.dumps(s, ensure_ascii=False).encode('utf-8'))

for f in ['Qmart', 'Status', 'Kunnr', 'Kunde', 'Matnr', 'Maktx', 'Mengeneinheit', 'Fehlerbild', 'Angelegt',
          'Ruecklieferung', 'Produktion', 'Massnahme', 'Bearbeiter', 'Produktlinie']:
    total = sum(len((',"%s":' % f).encode()) + blen(o[f]) for o in out)
    leer = sum(1 for o in out if not o[f])
    print('%-15s erwartet +%7d Byte, leer %3d von %d' % (f, total, leer, len(out)))
print('erste 5:')
for o in out[:5]:
    print({k: v for k, v in o.items() if k != 'Bearbeiter'})
