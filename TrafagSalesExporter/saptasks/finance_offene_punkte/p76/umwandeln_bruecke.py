# Bruecke lokale Artikelnummer <-> Trafag-Nummer: wandelt die SE16-%PC-Rohexporte aus P76 (KNA1, KNMT, VBAK, VBAP) in CSV.
# Aufruf: python -I umwandeln_bruecke.py <Ordner mit Rohexporten>
# Rohexporte (nur Scratchpad): kna1_roh.txt (KUNNR LAND1 NAME1 KTOKD VBUND, alle Kunden), knmt_roh.txt (ganze Tabelle),
# vbak_roh.txt (AUDAT ab 01.07.2022, alle Kunden), vbap_roh.txt (VBELN aus VBAK_IC). Ausgabe UTF-8, Semikolon, CRLF.
import collections
import csv
import os
import sys

HIER = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HIER)
from umwandeln import lese, matnr_extern  # noqa: E402

# Namen, die laut Kurztext keine Tochtergesellschaft sind oder Trafag AG selbst (Einschaetzung Agent, nicht aus SAP)
HINWEIS = {
    '301920': 'Testkunde/Muster', '303800': 'Testkunde/Muster', '304051': 'Testkunde/Muster',
    '300522': 'Fremdkunde mit Trafag im Namen', '309722': 'Fremdkunde mit Trafag im Namen',
    '304825': 'Fremdkunde mit Trafag im Namen',
}


def ext(n):
    return matnr_extern(n)


def iso(d):
    d = d.strip()
    return d[6:10] + '-' + d[3:5] + '-' + d[0:2] if len(d) == 10 else d


def schreibe(name, felder, zeilen):
    with open(os.path.join(HIER, name), 'w', encoding='utf-8', newline='') as f:
        w = csv.writer(f, delimiter=';', lineterminator='\r\n')
        w.writerow(felder)
        for z in zeilen:
            w.writerow([z[k] for k in felder])


def main(roh):
    _, kna1, e1 = lese(os.path.join(roh, 'kna1_roh.txt'), 'KUNNR')
    ic = {}
    for r in kna1:
        if r['VBUND'].strip() or 'TRAFAG' in r['NAME1'].upper():
            k = ext(r['KUNNR'])
            h = HINWEIS.get(k, 'Trafag AG / CH intern' if r['LAND1'].strip() == 'CH' else '')
            ic[k] = {'KUNNR': k, 'NAME1': r['NAME1'].strip(), 'LAND1': r['LAND1'].strip(), 'KTOKD': r['KTOKD'].strip(),
                     'VBUND': r['VBUND'].strip(), 'HINWEIS_AGENT': h}
    schreibe('KNA1_IC.csv', ['KUNNR', 'NAME1', 'LAND1', 'KTOKD', 'VBUND', 'HINWEIS_AGENT'], sorted(ic.values(), key=lambda x: x['KUNNR']))

    _, knmt, e2 = lese(os.path.join(roh, 'knmt_roh.txt'), 'KUNNR')
    km = [{'VKORG': r['VKORG'].strip(), 'VTWEG': r['VTWEG'].strip(), 'KUNNR': ext(r['KUNNR']), 'MATNR': ext(r['MATNR']),
           'KDMAT': r['KDMAT'].strip(), 'POSTX': r['POSTX'].strip()} for r in knmt]
    for z in km:
        z['IC_KUNDE'] = 'X' if z['KUNNR'] in ic else ''
    schreibe('KNMT.csv', ['VKORG', 'VTWEG', 'KUNNR', 'MATNR', 'KDMAT', 'POSTX', 'IC_KUNDE'], km)

    _, vbak, e3 = lese(os.path.join(roh, 'vbak_roh.txt'), 'VBELN')
    kopf = {}
    for r in vbak:
        k = ext(r['KUNNR'])
        e = iso(r['ERDAT'])
        if k in ic and e >= '2023-01-01':
            v = ext(r['VBELN'])
            kopf[v] = {'VBELN': v, 'KUNNR': k, 'ERDAT': e, 'AUDAT': iso(r['AUDAT']), 'AUART': r['AUART'].strip(),
                       'VBTYP': r['VBTYP'].strip(), 'BSTNK': r['BSTNK'].strip()}
    schreibe('VBAK_IC.csv', ['VBELN', 'KUNNR', 'ERDAT', 'AUDAT', 'AUART', 'VBTYP', 'BSTNK'], sorted(kopf.values(), key=lambda x: x['VBELN']))

    _, vbap, e4 = lese(os.path.join(roh, 'vbap_roh.txt'), 'VBELN')
    pos = []
    for r in vbap:
        v = ext(r['VBELN'])
        h = kopf[v]
        pos.append({'VBELN': v, 'POSNR': str(int(r['POSNR'])), 'MATNR': ext(r['MATNR']), 'KDMAT': r['KDMAT'].strip(),
                    'ARKTX': r['ARKTX'].strip(), 'KUNNR': h['KUNNR'], 'LAND1': ic[h['KUNNR']]['LAND1'], 'ERDAT': h['ERDAT'],
                    'AUART': h['AUART'], 'VBTYP': h['VBTYP']})
    schreibe('VBAP_IC.csv', ['VBELN', 'POSNR', 'MATNR', 'KDMAT', 'ARKTX', 'KUNNR', 'LAND1', 'ERDAT', 'AUART', 'VBTYP'], pos)

    print('ZEILEN KNA1 gelesen', len(kna1), 'IC', len(ic), '| KNMT', len(km), 'davon IC', sum(1 for z in km if z['IC_KUNDE']),
          '| VBAK gelesen', len(vbak), 'IC ab 2023', len(kopf), '| VBAP', len(pos), '| Ersatzzeichen', e1, e2, e3, e4)
    print('VBAP Schluessel eindeutig', len({(p['VBELN'], p['POSNR']) for p in pos}), 'Auftraege mit Positionen',
          len({p['VBELN'] for p in pos}))
    print('VBAP KDMAT gefuellt', sum(1 for p in pos if p['KDMAT']), 'davon VBTYP C', sum(1 for p in pos if p['KDMAT'] and p['VBTYP'] == 'C'))

    kn = collections.Counter(z['KUNNR'] for z in km if z['IC_KUNDE'])
    pa = collections.Counter(p['KUNNR'] for p in pos)
    pk = collections.Counter(p['KUNNR'] for p in pos if p['KDMAT'])
    dk = collections.defaultdict(set)
    for p in pos:
        if p['KDMAT']:
            dk[p['KUNNR']].add(p['KDMAT'])
    print('KUNNR;LAND1;NAME1;HINWEIS;KNMT;POS;POS_KDMAT;KDMAT_VERSCH')
    for k in sorted(ic, key=lambda x: (ic[x]['LAND1'], x)):
        if kn[k] or pa[k]:
            i = ic[k]
            print(';'.join([k, i['LAND1'], i['NAME1'], i['HINWEIS_AGENT'], str(kn[k]), str(pa[k]), str(pk[k]), str(len(dk[k]))]))
    # Beispiele je Land: erst Auftragsposition, sonst KNMT
    print('BEISPIELE')
    je = collections.defaultdict(list)
    for p in pos:
        if p['KDMAT'] and p['KDMAT'] != p['MATNR'] and len(je[p['LAND1']]) < 3 and (p['KDMAT'], p['MATNR']) not in [(a, b) for a, b, _ in je[p['LAND1']]]:
            je[p['LAND1']].append((p['KDMAT'], p['MATNR'], 'VBAP ' + p['VBELN']))
    for z in km:
        if z['IC_KUNDE']:
            l = ic[z['KUNNR']]['LAND1']
            if len(je[l]) < 3 and z['KDMAT']:
                je[l].append((z['KDMAT'], z['MATNR'], 'KNMT ' + z['KUNNR']))
    for l in sorted(je):
        print(l, ' | '.join('%s -> %s (%s)' % b for b in je[l]))


if __name__ == '__main__':
    main(sys.argv[1])
