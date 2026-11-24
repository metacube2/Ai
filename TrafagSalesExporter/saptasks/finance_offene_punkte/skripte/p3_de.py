# Punkt 3: DE-Mapping-Vorschlaege (Export Mapping 23.11.) gegen P76 MARA/MARC/MBEW 1100 pruefen. Nur lesend; Ergebnis ist eine
# Pruefhilfe (Excel in Downloads), keine Bestaetigung.
import csv, re, collections, statistics, openpyxl
from openpyxl.styles import Font, PatternFill
from p76lib import MARA, MARC, MBEW, MAKT, unit_cost, norm_mat, match_codes
OUT = r'C:\Users\koi\Downloads\Mapping_DE_Vorpruefung_P76_2026-11-24.xlsx'
def f(v):
    try: return float(v) if v not in ('', None) else 0.0
    except ValueError: return 0.0
loc = collections.defaultdict(list); price = collections.defaultdict(list)
for r in csv.DictReader(open('s23_fd.csv', encoding='utf-8')):
    if r['TSC'] == 'TRDE' and r['Year'] in ('2025', '2026'):
        m = r['Material'].strip().upper()
        if f(r['Standard Cost CHF (per unit)']) > 0: loc[m].append(f(r['Standard Cost CHF (per unit)']))
        if f(r['Quantity']) > 0 and f(r['Net Sales CHF']) > 0: price[m].append(f(r['Net Sales CHF']) / f(r['Quantity']))
SKIP = re.compile(r'(ERSETZT|ALT|KUNDEN[- ]?ART|CUSTOMER)\W*(DURCH|NR\.?|NO\.?|ARTIKEL\S*)?\W*$', re.I)
def numbers(s):
    return set(re.findall(r'(?<![0-9A-Z.])(\d{5,6})(?![0-9.])', (s or '').upper()))
rows = list(csv.DictReader(open('de_mapping.csv', encoding='utf-8-sig'), delimiter=';'))
V = [r for r in rows if r['Status'] == 'Vorschlag']
out = []
for r in V:
    lm = r['Lokale Nummer']; txt = r['Text']; tg = norm_mat(r['Vorgeschlagene Gruppennummer'])
    a = MARA.get(tg); c = MARC.get(tg); b = MBEW.get(tg)
    mara_ok = bool(a) and not a['LVORM']; marc_ok = bool(c) and not c['LVORM']
    after_slash = lm.split('/')[-1].strip() if '/' in lm else ''
    nr_lokal = norm_mat(after_slash) == tg if after_slash else False
    nr_any_lokal = tg in {norm_mat(x) for x in numbers(lm)}
    nr_text = tg in {norm_mat(x) for x in numbers(txt)}
    k, mats, code, full = match_codes(lm + ' ' + txt)
    typ_exact = full and k == 'eindeutig' and mats == [tg]
    typ_incl = full and tg in mats
    typ_other = full and k == 'eindeutig' and mats and mats != [tg]
    lc = statistics.median(loc[lm.upper()]) if loc[lm.upper()] else None
    uc = unit_cost(tg)
    fak = round(lc / uc, 3) if (lc and uc) else None
    aktiv = mara_ok and marc_ok
    stimmt = nr_lokal or nr_any_lokal or typ_exact
    gruende = []
    if not a: gruende.append('Ziel nicht in MARA')
    elif a['LVORM']: gruende.append('Ziel in MARA zum Loeschen vorgemerkt')
    if a and not c: gruende.append('Ziel nicht in MARC 1100')
    elif c and c['LVORM']: gruende.append('Ziel in MARC 1100 zum Loeschen vorgemerkt')
    if typ_other: gruende.append('Typencode zeigt auf ' + ','.join(mats))
    if fak is not None and (fak < 0.1 or fak > 10): gruende.append(f'Faktor {fak} ausserhalb 0,1-10')
    nr_ok = nr_lokal or nr_any_lokal
    if gruende and (not aktiv or (typ_other and not nr_ok) or (fak is not None and (fak < 0.1 or fak > 10))):
        kl = '3 wohl falsch'
    elif stimmt and not typ_other and aktiv and fak is not None and 0.3 <= fak <= 3:
        kl = '1 sicher'
    else:
        kl = '2 pruefen'
        if not stimmt: gruende.append('Nummer/Typencode nicht exakt (nur Text oder Teilcode)' if (nr_text or typ_incl or code) else 'keine Nummer und kein Typencode zum Ziel')
        if fak is None: gruende.append('Faktor nicht berechenbar (keine lokalen Kosten oder kein STPRS)')
        elif not (0.3 <= fak <= 3): gruende.append(f'Faktor {fak} ausserhalb 0,3-3')
    out.append({'Klasse': kl, 'Lokale Nummer': lm, 'Text': txt[:250], 'Vorgeschlagene Gruppennummer': tg, 'MAKT D Ziel': MAKT[tg].get('D', ''),
                'Verfahren': r['Verfahren Detail'], 'Sicherheit': f(r['Sicherheit']), 'Zeilen': int(f(r['Zeilen (Mapping)'])), 'Umsatz EUR': f(r['Umsatz (Mapping, lokal)']),
                'Nummer nach Schraegstrich = Ziel': 'ja' if nr_lokal else '', 'Ziel als Nummer in DE-Nummer': 'ja' if nr_any_lokal else '',
                'Ziel als Nummer im Text': 'ja' if nr_text else '', 'Typencode im Text': code, 'Typencode exakt = Ziel': 'ja' if typ_exact else ('mehrdeutig, Ziel dabei' if (typ_incl and not typ_exact) else ''),
                'MARA aktiv': 'ja' if mara_ok else 'nein', 'MARC 1100 aktiv': 'ja' if marc_ok else 'nein', 'BESKZ': c['BESKZ'] if c else '', 'MMSTA': c['MMSTA'] if c else '',
                'Lokale Kosten CHF/Einheit': round(lc, 4) if lc else None, 'STPRS/PEINH CHF': round(uc, 4) if uc is not None else None, 'PEINH': b['PEINH'] if b else '',
                'Faktor lokal/STPRS': fak, 'Verkaufspreis CHF/Einheit (Median)': round(statistics.median(price[lm.upper()]), 2) if price[lm.upper()] else None,
                'Begruendung': '; '.join(gruende)})
out.sort(key=lambda x: (x['Klasse'], -x['Umsatz EUR']))
agg = collections.OrderedDict()
for kl in ('1 sicher', '2 pruefen', '3 wohl falsch'):
    R = [o for o in out if o['Klasse'] == kl]
    agg[kl] = (len(R), sum(o['Zeilen'] for o in R), sum(o['Umsatz EUR'] for o in R))
print('Vorschlaege', len(out)); [print(' ', k, v[0], 'Vorschlaege', v[1], 'Zeilen', round(v[2]), 'EUR') for k, v in agg.items()]
gr = collections.Counter()
for o in out:
    for g in o['Begruendung'].split('; '):
        if g: gr[(o['Klasse'], re.sub(r'[\d.,]+', 'n', g))] += 1
for k, v in sorted(gr.items()): print('   ', k, v)
fk = [o['Faktor lokal/STPRS'] for o in out if o['Faktor lokal/STPRS']]
print('Faktor Median', statistics.median(fk), 'n', len(fk))
wb = openpyxl.Workbook(); ws = wb.active; ws.title = 'Übersicht'
ws.append(['Mapping DE: Vorpruefung der 732 Vorschlaege gegen P76 (Stand 24.11.2026)'])
ws.append(['Nur Pruefhilfe, keine Bestaetigung. Quelle: Mapping_2026-11-23.xlsx, Sales_All_2026-11-23.xlsx (lokale Kosten), P76 MARA/MARC/MBEW Werk 1100, gelesen 24.11. 11:50-12:04.'])
ws.append([])
ws.append(['Klasse', 'Vorschlaege', 'Zeilen', 'Umsatz EUR'])
for k, v in agg.items(): ws.append([k, v[0], v[1], round(v[2], 2)])
ws.append(['Summe', sum(v[0] for v in agg.values()), sum(v[1] for v in agg.values()), round(sum(v[2] for v in agg.values()), 2)])
ws.append([])
for t in ['Regeln:',
          '1 sicher: Nummer (nach dem Schraegstrich oder als 5-6-stellige Zahl in der DE-Nummer) oder ganzer Typencode zeigt eindeutig auf das Ziel, Ziel in MARA und MARC 1100 ohne Loeschvormerkung, Faktor lokale Kosten / (STPRS/PEINH) zwischen 0,3 und 3.',
          '3 wohl falsch: Ziel fehlt in MARA/MARC 1100 oder ist zum Loeschen vorgemerkt, oder der ganze Typencode zeigt eindeutig auf ein anderes Material und keine Nummer stuetzt das Ziel, oder Faktor unter 0,1 bzw. ueber 10.',
          '2 pruefen: alles andere (z. B. nur Textaehnlichkeit, Nummer nur im Text, Faktor 0,1-0,3 oder 3-10, keine lokalen Kosten).',
          'Hinweis Faktor: DE kauft bei TR AG zum Konzernpreis; der Median liegt bei rund 3,9. Mit Grenze 3 statt z. B. 8 landen viele sonst stimmige Vorschlaege in pruefen (Spalte Faktor ansehen).',
          'Faktor: lokale Kosten CHF je Einheit (Median der TRDE-Zeilen 2025/2026 im Export 23.11.) geteilt durch STPRS/PEINH (P76, CHF). Mengeneinheit DE nicht geprueft.',
          'Typencode: Punkt-getrennter Code im Text (Familie 3-4 Ziffern), verglichen mit MARA-ZZTYP/ZZTYP_F4 ohne Leerzeichen, Null-Fueller 0000 wahlweise weggelassen.']:
    ws.append([t])
ws['A1'].font = Font(bold=True, size=13)
for c in ws[4]: c.font = Font(bold=True)
ws.column_dimensions['A'].width = 18
wl = wb.create_sheet('Liste')
hdr = list(out[0].keys()); wl.append(hdr)
for o in out: wl.append([o[h] for h in hdr])
for c in wl[1]: c.font = Font(bold=True, color='FFFFFF'); c.fill = PatternFill('solid', fgColor='222222')
wl.auto_filter.ref = wl.dimensions; wl.freeze_panes = 'A2'
for col, w in zip('ABCDE', (14, 26, 60, 14, 32)): wl.column_dimensions[col].width = w
wb.save(OUT); print('gespeichert', OUT)
