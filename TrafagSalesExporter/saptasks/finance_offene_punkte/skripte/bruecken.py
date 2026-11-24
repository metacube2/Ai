# Bruecken lokale Artikelnummer -> Trafag-AG-Nummer aus Belegen (P76 KNMT/VBAP der IC-Kunden, B1-Katalognummern) gegen den
# Mapping-Bedarf (Mapping_2026-11-23.xlsx, alle Laender). Nur lesend; schreibt Pruefdatei und Import-Datei nach Downloads.
import csv, os, re, sys, collections, openpyxl
from openpyxl.styles import Font, PatternFill
from p76lib import MARA, MARC, MAKT, norm_mat
P76 = r'C:\Users\koi\source\repos\Ai\TrafagSalesExporter\saptasks\finance_offene_punkte\p76'
OUT_LIST = r'C:\Users\koi\Downloads\Mapping_Bruecken_Beleg_2026-11-24.xlsx'
OUT_IMP = r'C:\Users\koi\Downloads\Mapping_Import_belegt_2026-11-24.xlsx'
OUT_IMP_ES = r'C:\Users\koi\Downloads\Mapping_Import_belegt_TRES_2026-11-24.xlsx'
MIDDAY = r'C:\Users\koi\Downloads\Mapping_Import_DE_UK_sicher_2026-11-24.xlsx'

def key(x):
    x = ''.join(str(x or '').strip().upper().split())
    y = x.lstrip('0')
    return y if y else ('0' if x else '')

def aktiv(m):
    a = MARA.get(norm_mat(m))
    return bool(a) and not a['LVORM']

def rd(name):
    p = os.path.join(P76, name)
    if not os.path.exists(p):
        return None
    with open(p, encoding='utf-8-sig', newline='') as fh:
        return list(csv.DictReader(fh, delimiter=';'))

# --- Kunden -> TSC (KNA1_IC)
NAME2TSC = [(r'TRAFAG GMBH|GESELLSCHAFT F', 'TRDE'), (r'\(UK\)|UK LIMITED', 'TRUK'), (r'S\.?A\.?R\.?L|FRANCE', 'TRFR'),
            (r'ITALIA', 'TRIT'), (r'INC', 'TRUS'), (r'INDIA', 'TRIN'), (r'ESPA', 'TRES'), (r'GES\.? ?M\.?B\.?H|ÖSTERREICH|AUSTRIA', 'TRAT')]
def tsc_of(name, land=''):
    n = (name or '').upper()
    for pat, t in NAME2TSC:
        if re.search(pat, n):
            return t
    return {'DE': 'TRDE', 'GB': 'TRUK', 'FR': 'TRFR', 'IT': 'TRIT', 'US': 'TRUS', 'IN': 'TRIN', 'ES': 'TRES', 'AT': 'TRAT'}.get((land or '').upper(), '')
kna = rd('KNA1_IC.csv') or []
KUN = {}
for r in kna:
    kn = norm_mat(r.get('KUNNR', ''))
    KUN[kn] = r.get('TSC') or tsc_of(r.get('NAME1', ''), r.get('LAND1', ''))
print('KNA1_IC:', len(kna), 'Kunden', dict(collections.Counter(KUN.values())))

bridges = []   # (TSC, lokaler Schluessel, lokale Nummer, Ziel, Quelle, Zusatz)
for fname, quelle in (('KNMT.csv', 'Beleg KNMT'), ('VBAP_IC.csv', 'Beleg Auftrag')):
    rows = rd(fname)
    if rows is None:
        print(fname, 'fehlt'); continue
    n0 = 0
    for r in rows:
        kd = (r.get('KDMAT') or '').strip(); mt = (r.get('MATNR') or '').strip()
        t = r.get('TSC') or KUN.get(norm_mat(r.get('KUNNR') or r.get('KUNAG') or ''), '')
        if not kd or not mt or not t:
            continue
        n0 += 1
        bridges.append((t, key(kd), kd, norm_mat(mt), quelle, r.get('VBELN', '') or r.get('KUNNR', '')))
    print(fname, len(rows), 'Zeilen, nutzbar', n0)

# --- B1: Katalognummern der Trafag-Gesellschaften im TRIT-Mandanten (OSCN), Ziel = TRIT-Artikel
P2T = {'C_CH01_0304792': 'TRES', 'C_CH01_0017655': 'TRFR', 'C_CH01_0019719': 'TRAT', 'C_CH01_0000820': 'TRDE', 'C_CH01_0019684': 'TRUK', 'C_CH01_0304546': 'TRUS'}
b1_trit = []
for r in csv.DictReader(open('b1_paare.csv', encoding='utf-8-sig'), delimiter=';'):
    if r['TSC'] == 'TRIT' and r['Quelle'] == 'OSCN' and r['Partner'] in P2T:
        item = r['Artikel lokal'].strip().upper()
        tgt = item[1:] if re.fullmatch(r'I\d{4,6}', item) else item
        if aktiv(tgt):
            bridges.append((P2T[r['Partner']], key(r['Katalognummer']), r['Katalognummer'], norm_mat(tgt), 'Beleg B1-Katalog', 'TRIT ' + item))
        else:
            b1_trit.append((P2T[r['Partner']], key(r['Katalognummer']), r['Katalognummer'], item))

# --- Mapping-Bedarf und bisherige Klassen
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

# --- Bruecken je Bedarfszeile
byk = collections.defaultdict(lambda: collections.defaultdict(set))
for t, k, kd, tg, q, z in bridges:
    if (t, k) in NEED:
        byk[(t, k)][tg].add(q)
liste = []; imp = []; widerspruch = []
for (t, k), tg in byk.items():
    m = NEED[(t, k)]
    targets = sorted(tg)
    akt = [x for x in targets if aktiv(x)]
    quellen = sorted({q for x in akt for q in tg[x]})
    if len(akt) == 1:
        kl = 'belegt'
    elif len(akt) > 1:
        kl = 'Bruecke mehrdeutig'
    else:
        kl = 'Bruecke ohne aktives Ziel'
    vor = norm_mat(str(m['Group Material Number'] or ''))
    if t == 'TRDE': alt = DEKL.get(k, m['Status'])
    elif t == 'TRUK': alt = UKKL.get(k, m['Status'])
    else: alt = m['Status']
    mid = MID.get((t, k), '')
    ziel = akt[0] if kl == 'belegt' else ''
    vergleich = ''
    if ziel:
        if vor: vergleich = 'gleich Vorschlag' if vor == ziel else 'WIDERSPRUCH Vorschlag ' + vor
        else: vergleich = 'neu (kein Vorschlag)'
        if mid and mid != ziel:
            widerspruch.append((t, m['Local Material'], mid, ziel, ','.join(quellen)))
    rec = {'Klasse': kl, 'TSC': t, 'Lokale Nummer': m['Local Material'], 'Lokaler Text': (m['Local Text'] or '')[:200], 'Ziel Bruecke': ziel or ','.join(targets),
           'MAKT D Ziel': MAKT[ziel].get('D', '') if ziel else '', 'Quelle': ','.join(quellen) or ','.join(sorted({q for s in tg.values() for q in s})),
           'MARC 1100 aktiv': ('ja' if (ziel and ziel in MARC and not MARC[ziel]['LVORM']) else ('nein' if ziel else '')),
           'Bisheriger Status/Klasse': alt, 'Bisheriger Vorschlag': vor, 'Vergleich': vergleich,
           'In Mittagsdatei': (mid if mid else ''), 'Mittagsdatei stimmt': ('' if not mid else ('ja' if mid == ziel else 'NEIN')),
           'Zeilen': m['Rows'] or 0, 'Umsatz lokal': m['Net Sales (local currency)'] or 0, 'Waehrung': m['Currency']}
    liste.append(rec)
    if ziel and (not mid or mid != ziel):
        q0 = quellen[0] if len(quellen) == 1 else ' + '.join(quellen)
        imp.append([t, str(m['Local Material']), (m['Local Text'] or '')[:500], ziel, MAKT[ziel].get('D', ''), '', '', q0,
                    q0 + ' (Bruecke P76/B1 24.11.' + ('; korrigiert Mittagsdatei ' + mid if mid else '') + ')', '100', 'bestaetigt', '', '', '',
                    m['Rows'], m['Net Sales (local currency)'], m['Currency']])
liste.sort(key=lambda r: (r['Klasse'] != 'belegt', r['TSC'], -float(r['Umsatz lokal'] or 0)))
print('Bruecken mit Bedarfstreffer:', len(liste), collections.Counter((r['TSC'], r['Klasse']) for r in liste))
print('Widersprueche zur Mittagsdatei:', len(widerspruch), widerspruch[:20])
print('TR-IT-Bruecken (Ziel kein aktives MARA-Material, nicht belegt):', len(b1_trit), 'davon Bedarf', sum(1 for b in b1_trit if (b[0], b[1]) in NEED))

# --- Uebersicht je Land und Quelle
agg = collections.OrderedDict()
for r in liste:
    if r['Klasse'] != 'belegt': continue
    for q in r['Quelle'].split(','):
        a = agg.setdefault((r['TSC'], q), [0, 0, 0.0, 0, 0, 0, r['Waehrung']])
        a[0] += 1; a[1] += int(r['Zeilen'] or 0); a[2] += float(r['Umsatz lokal'] or 0)
        if r['Bisheriger Status/Klasse'] == '1 sicher':
            a[3] += 1
            if r['Vergleich'] == 'gleich Vorschlag': a[4] += 1
            else: a[5] += 1
wbo = openpyxl.Workbook(); ws = wbo.active; ws.title = 'Übersicht'
ws.append(['Bruecken lokale Artikelnummer -> Trafag-AG-Nummer aus Belegen (Stand 24.11.2026), Pruefhilfe, nichts importiert'])
ws.append(['Quellen: P76 KNMT und VBAP der IC-Kunden (KNA1_IC), B1 TRIT OSCN (Katalognummern der Trafag-Gesellschaften), Bedarf aus Mapping_2026-11-23.xlsx; belegt = genau ein aktives Ziel in MARA.'])
ws.append([])
ws.append(['TSC', 'Quelle', 'Artikel belegt', 'Zeilen', 'Umsatz lokal', 'Waehrung', 'davon bisher sicher', 'sicher = Bruecke', 'sicher widerspricht'])
for (t, q), a in sorted(agg.items()):
    ws.append([t, q, a[0], a[1], round(a[2], 2), a[6], a[3], a[4], a[5]])
ws.append([])
kc = collections.Counter((r['TSC'], r['Klasse'], r['Bisheriger Status/Klasse']) for r in liste)
ws.append(['TSC', 'Klasse', 'bisher', 'Artikel'])
for k3, n in sorted(kc.items(), key=lambda x: (x[0][0], x[0][1], str(x[0][2]))): ws.append([k3[0], k3[1], str(k3[2]), n])
for c in ws[4]: c.font = Font(bold=True)
wl = wbo.create_sheet('Liste')
hdr = list(liste[0].keys()) if liste else ['leer']; wl.append(hdr)
for r in liste: wl.append([r[x] for x in hdr])
for c in wl[1]: c.font = Font(bold=True, color='FFFFFF'); c.fill = PatternFill('solid', fgColor='222222')
wl.auto_filter.ref = wl.dimensions; wl.freeze_panes = 'A2'
ww = wbo.create_sheet('Widersprueche Mittagsdatei'); ww.append(['TSC', 'Lokale Nummer', 'Mittagsdatei Ziel', 'Bruecke Ziel', 'Quelle'])
for w_ in widerspruch: ww.append(list(w_))
wt = wbo.create_sheet('Bruecken TR IT'); wt.append(['TSC Kunde', 'Katalognummer (lokal)', 'TRIT-Artikel', 'Mapping-Bedarf'])
for b in b1_trit: wt.append([b[0], b[2], b[3], 'ja' if (b[0], b[1]) in NEED else ''])
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
hin = ['Import-Datei fuer Finance Cockpit > Mapping (Excel importieren). Erstellt 24.11.2026, NICHT importiert.',
       'Inhalt: Zeilen der Klasse belegt (Bruecke aus P76 KNMT/VBAP oder B1-Katalog, genau ein aktives Ziel in MARA), die nicht schon in Mapping_Import_DE_UK_sicher_2026-11-24.xlsx stehen, plus Korrekturen, wo die Bruecke der Mittagsdatei widerspricht.',
       'Hinweis: Die Seite speichert als Methode nur Regel, Text oder Hand; "Beleg ..." wird als Hand gespeichert, die Quelle steht in Method Detail.',
       'Status bestaetigt: wirkt nach dem Import.']
main = [r for r in imp if r[0] != 'TRES']; es = [r for r in imp if r[0] == 'TRES']
save(OUT_IMP, main, hin)
if es:
    save(OUT_IMP_ES, es, hin + ['Nur TRES: die Seite prueft den TSC gegen die Tabelle Sites; dort heisst Spanien (lokales Abbild 12.11.) TRSE. Diese Datei wird vermutlich mit "TSC TRES ist keine bekannte Gesellschaft" abgelehnt.'])
print('Import', OUT_IMP, len(main), collections.Counter(r[0] for r in main), '| TRES', len(es))
