# Gegenprobe: liest die Import-Datei wie MaterialMappingExcel.Parse und prueft wie MaterialMappingStore.ImportAsync
# (Stand Worktree C:\TMP\TrafagSalesExporter_rx2311, 6503c1d). Nur lesend.
import sys, re, sqlite3, collections, itertools, openpyxl
path = sys.argv[1]
wb = openpyxl.load_workbook(path, data_only=True)
ws = wb['Mapping'] if 'Mapping' in wb.sheetnames else wb.worksheets[0]
cols = {}
for c in ws[1]:
    if c.value is not None and str(c.value).strip():
        cols.setdefault(str(c.value).strip().lower(), c.column)
for req in ['TSC', 'Local Material', 'Group Material Number', 'Status']:
    assert req.lower() in cols, 'Spalte fehlt: ' + req
def rd(row, h):
    c = cols.get(h.lower())
    v = ws.cell(row, c).value if c else None
    return '' if v is None else str(v).strip()
def keynorm(v):
    x = ''.join(ch for ch in (v or '').strip().upper() if not ch.isspace())
    y = x.lstrip('0')
    return '' if not x else (y or '0')
def status_norm(v):
    v = (v or '').strip().lower()
    return {'': 'Vorschlag', 'vorschlag': 'Vorschlag', 'proposed': 'Vorschlag', 'proposal': 'Vorschlag', 'offen': 'offen', 'open': 'offen',
            'bestaetigt': 'bestaetigt', 'bestätigt': 'bestaetigt', 'confirmed': 'bestaetigt', 'abgelehnt': 'abgelehnt', 'rejected': 'abgelehnt'}.get(v)
def entity(v):
    c = ''.join(ch for ch in (v or '') if ch.isalpha()).upper()
    return {'TRAG': 'TR_AG', 'TRIT': 'TR_IT', 'TRIN': 'TR_IN'}.get(c)
con = sqlite3.connect(r'file:C:\Users\koi\source\repos\Ai\TrafagSalesExporter\trafag_exporter.db?mode=ro', uri=True)
known = {r[0].strip().upper() for r in con.execute('SELECT TSC FROM Sites') if r[0]}
errors = []; valid = []; skipped = 0; st = collections.Counter(); per = collections.Counter(); ns = collections.Counter()
for n in range(2, ws.max_row + 1):
    tsc, lm, gm, s = rd(n, 'TSC'), rd(n, 'Local Material'), rd(n, 'Group Material Number'), rd(n, 'Status')
    if not any([tsc, lm, gm, rd(n, 'Delivering Material Number'), rd(n, 'Local Text'), s]):
        skipped += 1; continue
    t = tsc.upper(); k = keynorm(lm)
    if not t or not k: errors.append(f'{n}: TSC/Nummer fehlt'); continue
    if t not in known: errors.append(f'{n}: TSC {t} unbekannt (lokales DB-Abbild)'); continue
    sn = status_norm(s)
    if sn is None: errors.append(f'{n}: Status {s} unbekannt'); continue
    et = rd(n, 'Delivering Entity')
    if et and entity(et) is None: errors.append(f'{n}: Entity {et} unbekannt'); continue
    has = bool(gm.strip()) or (entity(et) is not None and keynorm(rd(n, 'Delivering Material Number')))
    if sn not in ('abgelehnt', 'offen') and not has: errors.append(f'{n}: Gruppennummer fehlt'); continue
    conf = rd(n, 'Confidence')
    if conf and (not re.fullmatch(r'[+-]?\d+', conf) or not 0 <= int(conf) <= 100): errors.append(f'{n}: Sicherheit {conf}'); continue
    valid.append((t, k, n)); st[sn] += 1; per[t] += 1
    try: ns[t] += float(ws.cell(n, cols['net sales (local currency)']).value or 0)
    except (TypeError, ValueError): pass
dup = [(key, [x[2] for x in g]) for key, g in itertools.groupby(sorted(valid), key=lambda x: (x[0], x[1]))]
dups = [(key, rows) for key, rows in dup if len(rows) > 1]
print('Bekannte TSC (lokal):', sorted(known))
print('gueltig', len(valid), 'leer uebersprungen', skipped, 'Status', dict(st), 'je TSC', dict(per), 'Umsatz', {k: round(v) for k, v in ns.items()})
print('Fehler', len(errors), errors[:10]); print('Doppelte Schluessel', dups[:10])
