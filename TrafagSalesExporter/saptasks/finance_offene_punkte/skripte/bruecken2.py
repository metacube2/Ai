# Bruecken lokale Artikelnummer -> Trafag-AG-Nummer aus Belegen (P76 KNMT/VBAP der IC-Kunden, B1-Katalognummern im TRIT-Mandanten)
# gegen den Mapping-Bedarf (Mapping_2026-11-23.xlsx, alle Laender). Nur lesend; schreibt Pruefdatei und Import-Dateien nach Downloads.
import csv, os, re, collections, openpyxl
from openpyxl.styles import Font, PatternFill
from p76lib import MARA, MARC, MAKT, norm_mat, unit_cost
P76 = r'C:\Users\koi\source\repos\Ai\TrafagSalesExporter\saptasks\finance_offene_punkte\p76'
OUT_LIST = r'C:\Users\koi\Downloads\Mapping_Bruecken_Beleg_2026-11-24.xlsx'
OUT_IMP = r'C:\Users\koi\Downloads\Mapping_Import_belegt_2026-11-24.xlsx'
OUT_IMP_TRIT = r'C:\Users\koi\Downloads\Mapping_Import_belegt_TR_IT_2026-11-24.xlsx'
MIDDAY = r'C:\Users\koi\Downloads\Mapping_Import_DE_UK_sicher_2026-11-24.xlsx'

def key(x):
    x = ''.join(str(x or '').strip().upper().split()); y = x.lstrip('0')
    return y if y else ('0' if x else '')
def aktiv(m):
    a = MARA.get(norm_mat(m)); return bool(a) and not a['LVORM']
DUMMY = re.compile(r'9{3,}\d?$')
def rd(name):
    with open(os.path.join(P76, name), encoding='utf-8-sig', newline='') as fh:
        return list(csv.DictReader(fh, delimiter=';'))

L2T = {'FR': 'TRFR', 'IT': 'TRIT', 'GB': 'TRUK', 'DE': 'TRDE', 'IN': 'TRIN', 'US': 'TRUS', 'ES': 'TRES', 'AT': 'TRAT'}
KUN = {norm_mat(r['KUNNR']): L2T[r['LAND1']] for r in rd('KNA1_IC.csv') if not (r.get('HINWEIS_AGENT') or '').strip() and r['LAND1'] in L2T}
fill = collections.Counter()
raw = []   # (TSC, key, kdmat, matnr, quelle)
for fname, quelle in (('KNMT.csv', 'Beleg KNMT'), ('VBAP_IC.csv', 'Beleg Auftrag')):
    for r in rd(fname):
        t = KUN.get(norm_mat(r.get('KUNNR', '')))
        if not t:
            continue
        kd = (r.get('KDMAT') or '').strip(); mt = norm_mat(r.get('MATNR') or '')
        fill[(quelle, t, 'Positionen IC')] += 1
        if kd:
            fill[(quelle, t, 'mit KDMAT')] += 1
        if kd and mt and key(kd) != key(mt):
            raw.append((t, key(kd), kd, mt, quelle))
fan = collections.defaultdict(set)
for t, k, kd, mt, q in raw:
    fan[mt].add((t, k))
bridges = []   # (TSC, key, lokal, AG-Ziel or '', TRIT-Artikel or '', Quelle)
for t, k, kd, mt, q in raw:
    if DUMMY.search(mt) or len(fan[mt]) >= 5:
        fill[(q, t, 'verworfen Sammel/Dummy')] += 1; continue
    bridges.append((t, k, kd, mt, '', q))
P2T = {'C_CH01_0304792': 'TRES', 'C_CH01_0017655': 'TRFR', 'C_CH01_0019719': 'TRAT', 'C_CH01_0000820': 'TRDE', 'C_CH01_0019684': 'TRUK', 'C_CH01_0304546': 'TRUS'}
for r in csv.DictReader(open('b1_paare.csv', encoding='utf-8-sig'), delimiter=';'):
    if r['TSC'] == 'TRIT' and r['Quelle'] == 'OSCN' and r['Partner'] in P2T:
        item = r['Artikel lokal'].strip().upper()
        ag = item[1:] if re.fullmatch(r'I\d{4,6}', item) else item
        ag = ag if (re.fullmatch(r'\d{4,6}', ag) and aktiv(ag)) else ''
        bridges.append((P2T[r['Partner']], key(r['Katalognummer']), r['Katalognummer'], ag, item, 'Beleg B1-Katalog'))
        fill[('Beleg B1-Katalog', P2T[r['Partner']], 'Paare')] += 1

wm = openpyxl.load_workbook('Mapping_2026-11-23.xlsx', read_only=True, data_only=True)
it = wm['Mapping'].iter_rows(values_only=True); mk = list(next(it))
NEED = {}
for v in it:
    m = dict(zip(mk, v))
    if m['TSC']:
        NEED[(m['TSC'], key(m['Local Material']))] = m
DEKL = {}
wb = openpyxl.load_workbook(r'C:\Users\koi\Downloads\Mapping_DE_Vorpruefung_P76_2026-11-24.xlsx', read_only=True, data_only=True)
it = wb['Liste'].iter_rows(values_only=True); h = next(it)
for v in it:
    r = dict(zip(h, v)); DEKL[key(r['Lokale Nummer'])] = r['Klasse']
UKKL = {key(r['UK Material']): r['Klasse'] for r in csv.DictReader(open('uk_typencode_vorschlag.csv', encoding='utf-8-sig'), delimiter=';')}
MID = {}
wb = openpyxl.load_workbook(MIDDAY, read_only=True, data_only=True)
it = wb['Mapping'].iter_rows(values_only=True); h = next(it)
for v in it:
    r = dict(zip(h, v)); MID[(r['TSC'], key(r['Local Material']))] = norm_mat(str(r['Group Material Number']))

by = collections.defaultdict(list)
for b in bridges:
    if (b[0], b[1]) in NEED:
        by[(b[0], b[1])].append(b)
liste = []; imp = []; imp_trit = []; widerspruch = []
for (t, k), bs in by.items():
    m = NEED[(t, k)]
    ag = sorted({b[3] for b in bs if b[3] and aktiv(b[3])})
    trit = sorted({b[4] for b in bs if b[4]})
    quellen = sorted({b[5] for b in bs})
    if len(ag) == 1: kl = 'belegt'
    elif len(ag) > 1: kl = 'Bruecke mehrdeutig'
    elif len(trit) == 1: kl = 'belegt TR IT (Artikel von TR IT)'
    elif len(trit) > 1: kl = 'Bruecke mehrdeutig'
    else: kl = 'Bruecke ohne aktives Ziel'
    ziel = ag[0] if kl == 'belegt' else ''
    vor = norm_mat(str(m['Group Material Number'] or ''))
    alt = DEKL.get(k, m['Status']) if t == 'TRDE' else (UKKL.get(k, m['Status']) if t == 'TRUK' else m['Status'])
    mid = MID.get((t, k), '')
    vergleich = ''
    if ziel:
        vergleich = ('gleich Vorschlag' if vor == ziel else 'WIDERSPRUCH Vorschlag ' + vor) if vor else 'neu (kein Vorschlag)'
        if mid and mid != ziel:
            widerspruch.append((t, m['Local Material'], mid, ziel, ','.join(quellen)))
    elif kl.startswith('belegt TR IT') and vor:
        vergleich = 'Vorschlag Gruppennummer ' + vor + ' statt TR-IT-Artikel'
    liste.append({'Klasse': kl, 'TSC': t, 'Lokale Nummer': m['Local Material'], 'Lokaler Text': (m['Local Text'] or '')[:200],
                  'Ziel Trafag AG': ziel or ','.join(ag), 'MAKT D Ziel': MAKT[ziel].get('D', '') if ziel else '',
                  'STPRS/PEINH Ziel CHF': unit_cost(ziel) if ziel else None, 'TR-IT-Artikel (B1-Katalog)': ','.join(trit),
                  'Quelle': ','.join(quellen), 'Bisheriger Status/Klasse': alt, 'Bisheriger Vorschlag': vor, 'Vergleich': vergleich,
                  'In Mittagsdatei': mid, 'Mittagsdatei stimmt': ('' if not mid else ('ja' if mid == ziel else 'NEIN')),
                  'Zeilen': m['Rows'] or 0, 'Umsatz lokal': m['Net Sales (local currency)'] or 0, 'Waehrung': m['Currency']})
    q0 = ' + '.join(quellen)
    base = [t, str(m['Local Material']), (m['Local Text'] or '')[:500]]
    tail = ['100', 'bestaetigt', '', '', '', m['Rows'], m['Net Sales (local currency)'], m['Currency']]
    if ziel and (not mid or mid != ziel):
        imp.append(base + [ziel, MAKT[ziel].get('D', ''), '', '', q0, q0 + ' (Bruecke P76/B1 24.11.' + ('; korrigiert Mittagsdatei ' + mid if mid else '') + ')'] + tail)
    elif kl.startswith('belegt TR IT'):
        imp_trit.append(base + ['', '', 'TR IT', trit[0], q0, q0 + ': Katalognummer der Schwester im TRIT-Artikel ' + trit[0] + ' (B1 OSCN, 24.11.)'] + tail)
order = {'belegt': 0, 'belegt TR IT (Artikel von TR IT)': 1, 'Bruecke mehrdeutig': 2, 'Bruecke ohne aktives Ziel': 3}
liste.sort(key=lambda r: (order[r['Klasse']], r['TSC'], -float(r['Umsatz lokal'] or 0)))

print('Fuellgrad:'); [print('  ', k, v) for k, v in sorted(fill.items())]
print('Bedarfstreffer:', collections.Counter((r['TSC'], r['Klasse']) for r in liste))
agg = collections.defaultdict(lambda: [0, 0, 0.0, '', 0, 0, 0])
for r in liste:
    for q in r['Quelle'].split(','):
        a = agg[(r['TSC'], r['Klasse'], q)]
        a[0] += 1; a[1] += int(r['Zeilen'] or 0); a[2] += float(r['Umsatz lokal'] or 0); a[3] = r['Waehrung']
        if r['Bisheriger Status/Klasse'] == '1 sicher':
            a[4] += 1; a[5] += r['Vergleich'] == 'gleich Vorschlag'; a[6] += r['Vergleich'].startswith('WIDER')
for k3, a in sorted(agg.items()): print('  ', k3, a)
print('Widersprueche Mittagsdatei:', widerspruch)

wbo = openpyxl.Workbook(); ws = wbo.active; ws.title = 'Übersicht'
ws.append(['Bruecken lokale Artikelnummer -> Trafag-AG-Nummer aus Belegen (Stand 24.11.2026), Pruefhilfe, nichts importiert'])
ws.append(['Quellen: P76 KNMT und VBAP der IC-Kunden (KNA1_IC ohne CH-intern/Test/Fremd), B1 TRIT OSCN (Katalognummern der Trafag-Gesellschaften als Kunden). Bedarf: Mapping_2026-11-23.xlsx.'])
ws.append(['belegt = genau ein aktives Trafag-AG-Ziel in MARA (Sammel-/Dummymaterialien wie STS999 verworfen). belegt TR IT = Bruecke auf einen TR-IT-Artikel ohne AG-Nummer (Import mit Delivering Entity TR IT).'])
ws.append([])
ws.append(['TSC', 'Klasse', 'Quelle', 'Artikel', 'Zeilen', 'Umsatz lokal', 'Waehrung', 'davon bisher sicher', 'sicher = Bruecke', 'sicher widerspricht'])
for c in ws[5]: c.font = Font(bold=True)
for (t, kl, q), a in sorted(agg.items()):
    ws.append([t, kl, q, a[0], a[1], round(a[2], 2), a[3], a[4], a[5], a[6]])
ws.append([]); ws.append(['Fuellgrad P76/B1 (IC-Kunden)', 'TSC', 'Merkmal', 'Anzahl'])
for (q, t, mm), n in sorted(fill.items()): ws.append([q, t, mm, n])
wl = wbo.create_sheet('Liste'); hdr = list(liste[0].keys()); wl.append(hdr)
for r in liste: wl.append([r[x] for x in hdr])
for c in wl[1]: c.font = Font(bold=True, color='FFFFFF'); c.fill = PatternFill('solid', fgColor='222222')
wl.auto_filter.ref = wl.dimensions; wl.freeze_panes = 'A2'
ww = wbo.create_sheet('Widersprueche Mittagsdatei'); ww.append(['TSC', 'Lokale Nummer', 'Mittagsdatei Ziel', 'Bruecke Ziel', 'Quelle'])
for w_ in widerspruch: ww.append(list(w_))
wbo.save(OUT_LIST); print('gespeichert', OUT_LIST)

HEAD = ["TSC", "Local Material", "Local Text", "Group Material Number", "Text Trafag AG", "Delivering Entity", "Delivering Material Number",
        "Method", "Method Detail", "Confidence", "Status", "Reviewed By", "Reviewed At", "Effective", "Rows", "Net Sales (local currency)", "Currency"]
def save(path, rows, hinweis):
    wb2 = openpyxl.Workbook(); s = wb2.active; s.title = 'Mapping'; s.append(HEAD)
    for c in s[1]: c.font = Font(bold=True)
    for r in rows: s.append(r)
    for row in s.iter_rows(min_row=2):
        for c in row[:13]:
            c.number_format = '@'
            if c.value is not None and not isinstance(c.value, str): c.value = str(c.value)
    s.auto_filter.ref = s.dimensions; s.freeze_panes = 'A2'
    hs = wb2.create_sheet('Hinweis')
    for t in hinweis: hs.append([t])
    wb2.save(path)
for p in (OUT_IMP, OUT_IMP_TRIT, r'C:\Users\koi\Downloads\Mapping_Import_belegt_TRES_2026-11-24.xlsx'):
    if os.path.exists(p) and p != OUT_IMP and p != OUT_IMP_TRIT: os.remove(p)   # Zwischenstand des ersten Laufs entfernen
hin = ['Import-Datei fuer Finance Cockpit > Mapping (Excel importieren). Erstellt 24.11.2026, NICHT importiert.',
       'Die Seite speichert als Methode nur Regel, Text oder Hand; "Beleg ..." wird als Hand gespeichert, die Quelle steht in Method Detail.']
save(OUT_IMP, imp, hin + ['Inhalt: Klasse belegt (Trafag-AG-Nummer aus KNMT/VBAP/B1-Katalog), die nicht schon gleich in Mapping_Import_DE_UK_sicher_2026-11-24.xlsx steht.'])
save(r'C:\Users\koi\Downloads\Mapping_Import_belegt_TR_IT_TRES_2026-11-24.xlsx', [r for r in imp_trit if r[0] == 'TRES'], hin + ['Nur TRES. Die Seite prueft den TSC gegen die Tabelle Sites; im lokalen Abbild 12.11. heisst Spanien dort TRSE. Dann lehnt der Import die ganze Datei ab; vorher pruefen.'])
imp_trit = [r for r in imp_trit if r[0] != 'TRES']
save(OUT_IMP_TRIT, imp_trit, hin + ['Inhalt: Klasse belegt TR IT (Spaniens bzw. UKs Nummer ist im TRIT-Mandanten als Katalognummer zum TR-IT-Artikel hinterlegt). Wirkt nur, wenn TR IT die Zeile liefert (Paket B).',
                                    'TRES-Zeilen: die Seite prueft den TSC gegen die Tabelle Sites; im lokalen Abbild 12.11. heisst Spanien dort TRSE. Dann lehnt der Import die ganze Datei ab.'])
print('Import belegt', len(imp), collections.Counter(r[0] for r in imp), '| Import TR IT', len(imp_trit), collections.Counter(r[0] for r in imp_trit))
