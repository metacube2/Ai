"""Erwartete Mehrlaenge fuer Menge und Sysstat (alle aktiven I-Status, Kurztexte TJ02T DE) zu QmMeldungSet.
Aufruf: python -I -X utf8 referenz_sysstat_menge.py <repo> <arbeitsordner>; braucht <arbeitsordner>/ref_qm.json aus
referenz_rfc.py. NOTES.md Abschnitt 4.
"""
import json, sys, os, subprocess, collections
from decimal import Decimal
repo, d = sys.argv[1], sys.argv[2]
out = json.load(open(os.path.join(d, 'ref_qm.json'), encoding='utf-8'))
menge = sum(len(',"Menge":'.encode()) + len(json.dumps('%.6f' % Decimal(o['MengeRoh'] or '0'))) for o in out)
print('Menge erwartet +', menge)
# Sysstat: alle aktiven Systemstatus (I*) je offenem Objekt, Kurztexte mit Leerzeichen
probe = os.path.join(repo, '.tmp_sap_probe', 'RunSapProbe.ps1')
def read(table, fields, where):
    cmd = ['powershell', '-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', probe, 'table-read', table,
           '--fields', ','.join(fields), '--where', where, '--rowcount', '999999', '--format', 'json', '--quiet']
    o = subprocess.run(cmd, capture_output=True, cwd=repo).stdout.decode('utf-8', errors='replace')
    return json.loads(o[o.find('['):o.rfind(']') + 1])
objs = {'QM' + o['Qmnum'].zfill(12) for o in out}
st = collections.defaultdict(list)
for r in read('JEST', ['OBJNR', 'STAT'], "OBJNR LIKE 'QM%' AND INACT = ' ' AND STAT LIKE 'I%'"):
    if r['OBJNR'] in objs:
        st[r['OBJNR']].append(r['STAT'])
txt = {r['ISTAT']: r['TXT04'] for r in read('TJ02T', ['ISTAT', 'TXT04'], "SPRAS = 'D'")}
tot = 0; comb = collections.Counter()
for ob in objs:
    line = ' '.join(sorted(txt.get(s, s) for s in st[ob]))
    comb[' '.join(sorted(st[ob]))] += 1
    tot += len(',"Sysstat":'.encode()) + len(json.dumps(line, ensure_ascii=False).encode())
print('Sysstat erwartet (alle aktiven I-Status) +', tot)
for k, v in comb.most_common(12):
    print(v, k, '=', ' '.join(txt.get(s, s) for s in k.split()))
