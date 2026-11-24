# Baut die Liste "Zukaufteile mit Stueckliste/Arbeitsplan" (Werk 1100) aus SE16-Exporten P76 (24.11.2026).
# Aufruf: python -I liste_bauen.py <Exportordner> <Ziel.xlsx>
import sys, os, collections, datetime
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from se16lesen import lese, zahl
from openpyxl import Workbook
from openpyxl.styles import Font, Alignment, PatternFill
from openpyxl.utils import get_column_letter

P = sys.argv[1].rstrip('\\') + '\\'
ZIEL = sys.argv[2]
HEUTE = '20261124'


def d8(s):  # TT.MM.JJJJ -> JJJJMMTT
    s = (s or '').strip()
    return s[6:10] + s[3:5] + s[0:2] if len(s) == 10 else ''


def dat(s):
    v = d8(s)
    if not v or v.startswith('0000'):
        return None
    return datetime.date(int(v[:4]), int(v[4:6]), int(v[6:]))


marc = {m['MATNR']: m for m in lese(P + 'marc_f.txt')}
fakt = {k for k, m in marc.items() if not m['LVORM']}
mast_all = lese(P + 'mast.txt')
mast = collections.defaultdict(list)
for m in mast_all:
    if m['MATNR'] in fakt and m['STLAN'] in ('1', '3'):
        mast[m['MATNR']].append(m)
andere_verw = [m for m in mast_all if m['MATNR'] in fakt and m['STLAN'] not in ('1', '3')]

stko = {}
for s in lese(P + 'stko.txt'):
    k = (s['STLNR'], s['STLAL'])
    if k not in stko or int(s['STKOZ']) > int(stko[k]['STKOZ']):
        stko[k] = s
# STAS: je Knoten der letzte Stand bis heute; gueltig, wenn nicht geloescht
stas = {}
for a in lese(P + 'stas.txt'):
    if d8(a['DATUV']) > HEUTE:
        continue
    k = (a['STLNR'], a['STLAL'], a['STLKN'])
    if k not in stas or int(a['STASZ']) > int(stas[k]['STASZ']):
        stas[k] = a
stpo = {}
for p in lese(P + 'stpo.txt'):
    if d8(p['DATUV']) > HEUTE:
        continue
    k = (p['STLNR'], p['STLKN'])
    if k not in stpo or int(p['STPOZ']) > int(stpo[k]['STPOZ']):
        stpo[k] = p


def positionen(stlnr, stlal):
    erg = []
    for (n, al, kn), a in stas.items():
        if n == stlnr and al == stlal and not a['LKENZ']:
            p = stpo.get((n, kn))
            if p and not p['LKENZ']:
                erg.append(p)
    return sorted(erg, key=lambda p: p['POSNR'])


# Arbeitsplan: MAPL je Schluessel letzter Zaehler, PLKO je Plangruppe/-zaehler letzter Zaehler
mapl = {}
for m in lese(P + 'mapl.txt'):
    if m['MATNR'] not in fakt:
        continue
    k = (m['MATNR'], m['PLNTY'], m['PLNNR'], m['PLNAL'], m['ZKRIZ'])
    if k not in mapl or int(m['ZAEHL']) > int(mapl[k]['ZAEHL']):
        mapl[k] = m
plko = {}
for p in lese(P + 'plko.txt'):
    k = (p['PLNTY'], p['PLNNR'], p['PLNAL'])
    if k not in plko or int(p['ZAEHL']) > int(plko[k]['ZAEHL']):
        plko[k] = p
plaene = collections.defaultdict(list)
andere_plaene = collections.defaultdict(set)
for k, m in mapl.items():
    if m['LOEKZ']:
        continue
    pk = plko.get((m['PLNTY'], m['PLNNR'], m['PLNAL']))
    if m['PLNTY'] != 'N':
        andere_plaene[m['MATNR']].add('%s %s/%s' % (m['PLNTY'], m['PLNNR'], m['PLNAL']))
        continue
    if pk is None or pk['LOEKZ']:
        continue
    plaene[m['MATNR']].append((m, pk))

# Nur Plantyp N zaehlt als Arbeitsplan (PLKO VERWE 1); andere Plantypen (in P76 nur Q, 2 Zuordnungen) nur als Hinweis
kand = sorted(set(mast) | set(plaene))
makt = {m['MATNR']: m['MAKTX'] for m in lese(P + 'makt.txt')}
mara = {m['MATNR']: m for m in lese(P + 'mara.txt')}
mbew = {m['MATNR']: m for m in lese(P + 'mbew.txt') if m['BWKEY'] == '1100' and not m['BWTAR']}
eina = collections.defaultdict(list)
for e in lese(P + 'eina.txt'):
    if e['MATNR'] in fakt:
        eina[e['MATNR']].append(e)
eine = collections.defaultdict(list)
for e in lese(P + 'eine.txt'):
    eine[e['INFNR']].append(e)
t460 = {t['SOBSL']: t['LTEXT'] for t in lese(P + 't460t.txt')} if os.path.exists(P + 't460t.txt') else {}
t141 = {t['MMSTA']: t['MTSTB'] for t in lese(P + 't141t.txt')} if os.path.exists(P + 't141t.txt') else {}

E_LOHN = 'Lohnbearbeitung, Stückliste gehört dazu'
E_SELBST = 'Selbst-Stückliste, harmlos'
E_PRUEF = 'Stückliste/Arbeitsplan bei Zukaufteil prüfen'
zeilen = []
for mn in kand:
    mc = marc[mn]
    boms = mast.get(mn, [])
    verw, stlnrs, ps, bmeng, s_an, s_ae = [], [], [], [], [], []
    for b in boms:
        verw.append(b['STLAN']); stlnrs.append(b['STLNR'])
        ko = stko.get((b['STLNR'], b['STLAL']))
        if ko and (ko['LKENZ'] or ko['LOEKZ']):
            continue
        if ko:
            bmeng.append('%s %s' % (ko['BMENG'], ko['BMEIN']))
            s_an.append(dat(ko['ANDAT'])); s_ae.append(dat(ko['AEDAT']))
        ps.extend((b['MATNR'], p) for p in positionen(b['STLNR'], b['STLAL']))
    hat_stl = bool(boms)
    lpos = [p for _, p in ps if p['POSTP'] != 'T']
    tpos = [p for _, p in ps if p['POSTP'] == 'T']
    fremd = [p for p in lpos if p['IDNRK'] != mn]
    selbst = bool(lpos) and not fremd
    preis = [p for _, p in ps if zahl(p['PREIS']) != 0]
    hat_plan = bool(plaene.get(mn))
    plantyp = sorted({m['PLNTY'] for m, _ in plaene.get(mn, [])})
    plangr = sorted({'%s/%s' % (m['PLNNR'], m['PLNAL']) for m, _ in plaene.get(mn, [])})
    p_an = [dat(pk['ANDAT']) for _, pk in plaene.get(mn, []) if pk]
    p_ae = [dat(pk['AEDAT']) for _, pk in plaene.get(mn, []) if pk]
    p_stat = sorted({pk['STATU'] for _, pk in plaene.get(mn, []) if pk})
    # Infosatz: EINA nicht geloescht, EINE fuer Werk 1100 oder werksuebergreifend und nicht geloescht
    infs, esokz = [], set()
    for e in eina.get(mn, []):
        if e['LOEKZ']:
            continue
        for x in eine.get(e['INFNR'], []):
            if x['WERKS'] in ('1100', '') and not x['LOEKZ']:
                infs.append(e['INFNR']); esokz.add(x['ESOKZ'])
    infs = sorted(set(infs))
    bw = mbew.get(mn, {})
    stprs = zahl(bw.get('STPRS', '')); peinh = zahl(bw.get('PEINH', '')) or 1
    sobsl = mc['SOBSL']
    if sobsl == '30':
        einst = E_LOHN
        grund = 'Lohnbearbeitung' + (' (mit Arbeitsplan)' if hat_plan else '')
        rang = 9
    elif selbst and not hat_plan:
        einst, grund, rang = E_SELBST, 'nur Selbst-Stückliste', 8
    else:
        einst = E_PRUEF
        if fremd and hat_plan:
            grund, rang = 'Stückliste mit Fremdkomponenten und Arbeitsplan', 1
        elif fremd:
            grund, rang = 'Stückliste mit Fremdkomponenten, kein Arbeitsplan', 2
        elif hat_plan and selbst:
            grund, rang = 'Arbeitsplan, Stückliste nur sich selbst', 3
        elif hat_plan and hat_stl:
            grund, rang = 'Arbeitsplan, Stückliste ohne Lagerposition', 4
        elif hat_plan:
            grund, rang = 'nur Arbeitsplan, keine Stückliste', 5
        else:
            grund, rang = 'Stückliste ohne Lagerposition (nur Text/leer)', 6
    komp = '; '.join('%s %s %s%s' % (p['IDNRK'] or ('Text: ' + p['POTX1'])[:40], p['MENGE'], p['MEINS'], ' (T)' if p['POSTP'] == 'T' else '') for _, p in ps[:8])
    if len(ps) > 8:
        komp += '; ...'
    zeilen.append(dict(
        rang=rang, einst=einst, grund=grund, mn=mn, txt=makt.get(mn, ''), mtart=mara.get(mn, {}).get('MTART', ''),
        beskz=mc['BESKZ'], sobsl=sobsl, sobsl_t=t460.get(sobsl, '') if sobsl else '', mmsta=mc['MMSTA'],
        mmsta_t=t141.get(mc['MMSTA'], '') if mc['MMSTA'] else '', stl='ja' if hat_stl else 'nein',
        verw=', '.join(sorted(set(verw))), stlnr=', '.join(stlnrs), bmeng=', '.join(bmeng), npos=len(ps), nl=len(lpos),
        nt=len(tpos), nfremd=len(fremd), selbst='ja' if selbst else 'nein', npreis=len(preis), komp=komp,
        s_an=min([x for x in s_an if x], default=None), s_ae=max([x for x in s_ae if x], default=None),
        pos_ae=max([x for _, p in ps for x in (dat(p['ANDAT']), dat(p['AEDAT'])) if x], default=None),
        plan='ja' if hat_plan else 'nein', plantyp=', '.join(plantyp), plangr=', '.join(plangr), p_stat=', '.join(p_stat),
        p_an=min([x for x in p_an if x], default=None), p_ae=max([x for x in p_ae if x], default=None),
        stprs=stprs, peinh=peinh, vprsv=bw.get('VPRSV', ''), info='ja' if infs else 'nein', infnr=', '.join(infs),
        esokz=', '.join(sorted(esokz)), mara_lv=mara.get(mn, {}).get('LVORM', ''),
        anderer_plan=', '.join(sorted(andere_plaene.get(mn, set())))))

zeilen.sort(key=lambda z: (z['rang'], -z['nfremd'], z['mn']))

# ---- Zaehlungen
zael = collections.Counter((z['einst'], z['plan']) for z in zeilen)
gruende = collections.Counter(z['grund'] for z in zeilen if z['einst'] == E_PRUEF)
mst13 = {m['MATNR'] for m in mast_all if m['STLAN'] in ('1', '3')}
t76 = dict(alle=len(set(marc) & mst13), aktiv=len(fakt & mst13),
           ohne=sum(1 for m in fakt & mst13 if not marc[m]['SOBSL']), lohn=sum(1 for m in fakt & mst13 if marc[m]['SOBSL'] == '30'))

wb = Workbook()
ws = wb.active
ws.title = 'Übersicht'
fett = Font(bold=True)
r = 1
def zeile(*werte, bold=False):
    global r
    for i, w in enumerate(werte, 1):
        c = ws.cell(row=r, column=i, value=w)
        if bold:
            c.font = fett
    r += 1
zeile('Zukaufteile (Beschaffungsart F) mit Stückliste oder Arbeitsplan, Werk 1100', bold=True)
zeile('Quelle: P76 (Produktion, Mandant 100), nur gelesen mit SE16 am 24.11.2026 zwischen 10:05 und 10:13 Uhr.')
zeile('Auswahl: MARC Werk 1100, BESKZ = F, ohne Löschvormerkung (LVORM leer), und mindestens eines von:')
zeile('  a) Stückliste: MAST Werk 1100, Verwendung 1 oder 3, Stücklistenkopf STKO nicht gelöscht')
zeile('  b) Arbeitsplan: MAPL Werk 1100 (letzter Stand je Zuordnung nicht gelöscht), Plankopf PLKO (letzter Zähler) nicht gelöscht')
r += 1
zeile('Regel', bold=True)
zeile('Bei BESKZ F ohne Sonderbeschaffung nimmt die Kalkulation den Einkaufspreis (Infosatz); Stückliste und Arbeitsplan werden dafür nicht gebraucht.')
zeile('Bei Sonderbeschaffung 30 (Lohnbearbeitung) gehört die Stückliste dazu: sie beschreibt das Beistellmaterial.')
zeile('Eine Selbst-Stückliste (einzige Lagerposition ist das Material selbst, „rekursiv erlaubt“) stört die Kalkulation nicht.')
zeile('Auslegung: Hat ein Material neben der Selbst-Stückliste auch einen Arbeitsplan, steht es unter „prüfen“ (wegen des Arbeitsplans).')
zeile('In P76 haben alle 255 Selbst-Stücklisten einen Arbeitsplan; deshalb ist „Selbst-Stückliste, harmlos“ leer.')
r += 1
zeile('Zählung je Einstufung', 'mit Arbeitsplan', 'ohne Arbeitsplan', 'gesamt', bold=True)
for e in (E_PRUEF, E_SELBST, E_LOHN):
    a, b = zael[(e, 'ja')], zael[(e, 'nein')]
    zeile(e, a, b, a + b)
zeile('Summe', sum(v for (e, p), v in zael.items() if p == 'ja'), sum(v for (e, p), v in zael.items() if p == 'nein'), len(zeilen), bold=True)
r += 1
zeile('„prüfen“ aufgeteilt nach Grund (so sortiert im Blatt Liste)', 'Anzahl', bold=True)
for g, n in sorted(gruende.items(), key=lambda x: min(z['rang'] for z in zeilen if z['grund'] == x[0])):
    zeile(g, n)
r += 1
zeile('Weitere Zahlen', 'Anzahl', bold=True)
zeile('Positionen mit eigenem Preis (STPO-PREIS gefüllt) in allen gelesenen Stücklisten', sum(z['npreis'] for z in zeilen))
zeile('Materialien mit Infosatz (Werk 1100 oder werksübergreifend, nicht gelöscht)', sum(1 for z in zeilen if z['info'] == 'ja'))
zeile('Vergleich T76 (Plan ZLO03 FIX 29): F mit Stückliste Verw. 1/3, alle / ohne Löschvormerkung', '1866 / 1212')
zeile('Dasselbe in P76 heute', '%d / %d' % (t76['alle'], t76['aktiv']))
zeile('  davon ohne Sonderbeschaffung / Lohnbearbeitung 30 (T76: 1169 / 43)', '%d / %d' % (t76['ohne'], t76['lohn']))
zeile('Stückliste angelegt 2024 bis 2026', sum(1 for z in zeilen if z['s_an'] and z['s_an'].year >= 2024))
zeile('Arbeitsplan angelegt 2024 bis 2026', sum(1 for z in zeilen if z['p_an'] and z['p_an'].year >= 2024))
r += 1
zeile('Anlagejahr je Grund (frühestes Anlagedatum von Stückliste/Arbeitsplan)', 'vor 2024', 'ab 2024', bold=True)
kt = collections.Counter()
for z in zeilen:
    j = [d.year for d in (z['s_an'], z['p_an']) if d]
    kt[(z['grund'], bool(j) and min(j) >= 2024)] += 1
for g in sorted({z['grund'] for z in zeilen}, key=lambda g: min(z['rang'] for z in zeilen if z['grund'] == g)):
    zeile(g, kt[(g, False)], kt[(g, True)])
r += 1
zeile('Sonderbeschaffung Kalkulation (MARC-SOBSK) gefüllt', sum(1 for m in kand if marc[m].get('SOBSK')))
zeile('F-Teile ohne Löschvormerkung mit Stückliste anderer Verwendung (nicht in der Liste)', ', '.join('Verw. %s: %d' % kv for kv in sorted(collections.Counter(m['STLAN'] for m in andere_verw).items())))
zeile('  (Verwendung 9 = PCBA-Stücklisten; Nummern im Blatt „Verwendung 9 und 2“, Positionen dort nicht gelesen)')
geloescht = sorted({m['MATNR'] for m in mapl.values() if m['LOEKZ'] and d8(m['AEDAT']) >= '20261101' and m['MATNR'] not in kand})
zeile('Hinweis: F-Teile, deren Arbeitsplanzuordnung seit 01.11.2026 gelöscht wurde und die nicht in der Liste stehen', ', '.join(geloescht) or 'keine')
r += 1
zeile('Spalten im Blatt Liste', bold=True)
for k, v in [
    ('Einstufung / Prüfgrund', 'siehe oben; sortiert: Stückliste mit Fremdkomponenten und Arbeitsplan zuerst'),
    ('Material, Kurztext, Materialart', 'MARA, MAKT (Deutsch)'),
    ('BESKZ, SOBSL, Werksstatus', 'MARC Werk 1100 (Sonderbeschaffung mit Text aus T460T, Status mit Text aus T141T)'),
    ('Stückliste, Verwendung, Stücklistennr., Basismenge', 'MAST, STKO (Alternative laut MAST)'),
    ('Positionen gesamt / Lager / Text / Fremdkomponenten', 'gültige, nicht gelöschte Positionen (STAS/STPO, Stand heute); Fremdkomponente = Lagerposition, die nicht das Material selbst ist'),
    ('Selbst-Stückliste', 'ja, wenn alle Lagerpositionen das Material selbst sind'),
    ('Positionen mit Preis', 'STPO-PREIS ungleich 0 (z. B. Nicht-Lager-Positionen)'),
    ('Komponenten', 'bis zu 8 Positionen: Komponente, Menge, Einheit; (T) = Textposition'),
    ('Stückliste angelegt/geändert', 'STKO ANDAT / AEDAT (AEDAT ist in P76 fast nie gefüllt)'),
    ('Positionen zuletzt angelegt/geändert', 'jüngstes ANDAT/AEDAT der gültigen STPO-Positionen'),
    ('Arbeitsplan, Plantyp, Plangruppe/-zähler, Status', 'MAPL/PLKO; Status = PLKO-STATU'),
    ('Arbeitsplan angelegt/geändert', 'PLKO ANDAT / AEDAT (frühestes / jüngstes über die Planzähler)'),
    ('Standardpreis, Preiseinheit, Preissteuerung', 'MBEW Bewertungskreis 1100'),
    ('Infosatz, Infosatznr., Infotyp', 'EINA/EINE: nicht gelöscht, Werk 1100 oder ohne Werk; Infotyp = EINE-ESOKZ'),
    ('Anderer Plan', 'MAPL-Zuordnung mit anderem Plantyp als N (in P76 nur Q); zählt nicht als Arbeitsplan'),
]:
    zeile(k, v)
ws.column_dimensions['A'].width = 95
for c in 'BCD':
    ws.column_dimensions[c].width = 18

wl = wb.create_sheet('Liste')
kopf = [('Einstufung', 'einst', 40), ('Prüfgrund', 'grund', 44), ('Material', 'mn', 12), ('Kurztext', 'txt', 40),
        ('Materialart', 'mtart', 10), ('BESKZ', 'beskz', 7), ('SOBSL', 'sobsl', 7), ('Sonderbeschaffung', 'sobsl_t', 16),
        ('Werksstatus', 'mmsta', 9), ('Werksstatus Text', 'mmsta_t', 20), ('Stückliste', 'stl', 9), ('Verwendung', 'verw', 10),
        ('Stücklistennr.', 'stlnr', 12), ('Basismenge', 'bmeng', 12), ('Positionen', 'npos', 9), ('davon Lager', 'nl', 9),
        ('davon Text', 'nt', 9), ('Fremdkomponenten', 'nfremd', 10), ('Selbst-Stückliste', 'selbst', 10),
        ('Positionen mit Preis', 'npreis', 10), ('Komponenten', 'komp', 60), ('Stückliste angelegt', 's_an', 12),
        ('Stückliste geändert', 's_ae', 12), ('Positionen zuletzt angelegt/geändert', 'pos_ae', 13), ('Arbeitsplan', 'plan', 10), ('Plantyp', 'plantyp', 8),
        ('Plangruppe/-zähler', 'plangr', 22), ('Planstatus', 'p_stat', 9), ('Arbeitsplan angelegt', 'p_an', 12),
        ('Arbeitsplan geändert', 'p_ae', 12), ('Standardpreis CHF', 'stprs', 12), ('Preiseinheit', 'peinh', 9),
        ('Preissteuerung', 'vprsv', 9), ('Infosatz', 'info', 8), ('Infosatznr.', 'infnr', 24), ('Infotyp', 'esokz', 8),
        ('Löschvormerkung Mandant (MARA)', 'mara_lv', 10), ('Anderer Plan (Typ Plangruppe/-zähler)', 'anderer_plan', 18)]
for i, (t, _, w) in enumerate(kopf, 1):
    c = wl.cell(row=1, column=i, value=t)
    c.font = fett
    c.fill = PatternFill('solid', fgColor='DDEBF7')
    c.alignment = Alignment(wrap_text=True, vertical='top')
    wl.column_dimensions[get_column_letter(i)].width = w
for j, z in enumerate(zeilen, 2):
    for i, (_, k, _) in enumerate(kopf, 1):
        c = wl.cell(row=j, column=i, value=z[k])
        if isinstance(z[k], datetime.date):
            c.number_format = 'DD.MM.YYYY'
        elif k == 'stprs':
            c.number_format = '#,##0.00'
wl.freeze_panes = 'A2'
wl.auto_filter.ref = 'A1:%s%d' % (get_column_letter(len(kopf)), len(zeilen) + 1)
wv = wb.create_sheet('Verwendung 9 und 2')
for i, t in enumerate(['Material', 'Kurztext (nur wenn in Liste)', 'Verwendung', 'Stücklistennr.', 'in Liste (über Verw. 1/3 oder Arbeitsplan)'], 1):
    wv.cell(row=1, column=i, value=t).font = fett
in_liste = {z['mn'] for z in zeilen}
for j, m in enumerate(sorted(andere_verw, key=lambda x: (x['MATNR'], x['STLAN'])), 2):
    for i, v in enumerate([m['MATNR'], makt.get(m['MATNR'], ''), m['STLAN'], m['STLNR'], 'ja' if m['MATNR'] in in_liste else 'nein'], 1):
        wv.cell(row=j, column=i, value=v)
for c, w in zip('ABCDE', (12, 40, 11, 14, 30)):
    wv.column_dimensions[c].width = w
wv.freeze_panes = 'A2'
wb.save(ZIEL)

print('ZEILEN', len(zeilen))
for (e, p), n in sorted(zael.items()):
    print('  ', e, '| Arbeitsplan', p, n)
for g, n in gruende.most_common():
    print('  prüfen:', g, n)
print('T76-Vergleich P76', t76)
print('PEINH>1', len([z for z in zeilen if z['peinh'] > 1]), [(z['mn'], z['stprs'], z['peinh']) for z in zeilen if z['peinh'] >= 1000])
print('Hinweis geloescht', geloescht)
print('Preis-Positionen', sum(z['npreis'] for z in zeilen), 'Infosatz ja', sum(1 for z in zeilen if z['info'] == 'ja'))
print('Plantypen', collections.Counter(z['plantyp'] for z in zeilen), 'anderer Plan', [(z['mn'], z['anderer_plan'], z['einst'][:10]) for z in zeilen if z['anderer_plan']])
print('Stl angelegt Jahr', sorted(collections.Counter(z['s_an'].year for z in zeilen if z['s_an']).items()))
print('Stl geaendert Jahr', sorted(collections.Counter(z['s_ae'].year for z in zeilen if z['s_ae']).items()))
print('Plan angelegt Jahr', sorted(collections.Counter(z['p_an'].year for z in zeilen if z['p_an']).items()))
print('Plan geaendert Jahr', sorted(collections.Counter(z['p_ae'].year for z in zeilen if z['p_ae']).items()))
print('MMSTA', collections.Counter(z['mmsta'] for z in zeilen).most_common(), 'MTART', collections.Counter(z['mtart'] for z in zeilen).most_common())
print('MARA LVORM', collections.Counter(z['mara_lv'] for z in zeilen), 'ohne Infosatz', sum(1 for z in zeilen if z['info'] == 'nein'), 'STPRS 0', sum(1 for z in zeilen if not z['stprs']))
print('Fremdkomp. Materialarten Top', collections.Counter(z['komp'].split(' ')[0][:1] for z in zeilen if z['nfremd']).most_common(6))
for z in zeilen:
    if z['mn'] in ('C17389',):
        print('BEISPIEL', z['mn'], z['einst'], '|', z['grund'], '|', z['komp'], '| Plan', z['plangr'], z['p_an'], '| Stl', z['stlnr'], z['s_an'], '| STPRS', z['stprs'], z['peinh'], '| Info', z['info'], z['infnr'])
