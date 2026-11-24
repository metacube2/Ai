# Wandelt die SE16-%PC-Rohexporte aus P76 (Tab-Text, Codepage 1160 = cp1252) in die CSV-Dateien dieses Ordners um.
# Aufruf: python -I umwandeln.py <Ordner mit Rohexporten>
# Rohexporte (bleiben im Scratchpad, nicht im Repo): mara_roh.txt, mbew_1100_roh.txt, marc_1100_roh.txt,
# makt_de_roh.txt, makt_en_roh.txt. Ausgabe: MARA.csv, MBEW_1100.csv, MARC_1100.csv, MAKT.csv (UTF-8 ohne BOM, Semikolon)
# und eine Pruefausgabe auf stdout.
import csv
import os
import sys

HIER = os.path.dirname(os.path.abspath(__file__))


def lese(pfad, pflichtfeld='MATNR'):
    """Liest einen SE16-%PC-Export ueber die Kopfzeile; liefert (kopf, zeilen, ersatzzeichen)."""
    with open(pfad, 'rb') as f:
        roh = f.read().decode('cp1252', errors='replace')
    ersatz = roh.count('�')
    zeilen = roh.split('\r\n') if '\r\n' in roh else roh.split('\n')
    kopf = None
    kopfzeile = None
    daten = []
    for z in zeilen:
        teile = z.split('\t')
        if kopf is None:
            namen = [t.strip() for t in teile]
            if pflichtfeld in namen and z.startswith('\t'):
                kopf = [(i, t.strip()) for i, t in enumerate(teile) if t.strip()]
                kopfzeile = z
            continue
        if z == kopfzeile or not z.startswith('\t') or len(teile) < 2:
            continue
        d = {name: (teile[i] if i < len(teile) else '') for i, name in kopf}
        if not d.get(pflichtfeld, '').strip():
            continue
        daten.append(d)
    if kopf is None:
        raise SystemExit('keine Kopfzeile in ' + pfad)
    return [n for _, n in kopf], daten, ersatz


def zahl(s):
    # SE16-Export P76 (Benutzer KOI): Dezimalpunkt, Tausender-Komma ("1,000" = 1000), Minus hinten.
    s = (s or '').strip().replace("'", '')
    if not s:
        return 0.0
    neg = s.endswith('-')
    s = s.rstrip('-').replace(',', '')
    v = float(s)
    return -v if neg else v


def zahltext(v):
    t = ('%.6f' % v).rstrip('0').rstrip('.')
    return '0' if t in ('', '-0') else t


def matnr_extern(m):
    # SE16 zeigt numerische Materialnummern intern mit fuehrenden Nullen (000000000000055441); extern wie MM03.
    m = m.strip()
    if m.isdigit():
        return m.lstrip('0') or '0'
    return m


def schreibe(name, felder, zeilen):
    pfad = os.path.join(HIER, name)
    with open(pfad, 'w', encoding='utf-8', newline='') as f:
        w = csv.writer(f, delimiter=';', quoting=csv.QUOTE_MINIMAL, lineterminator='\r\n')
        w.writerow(felder)
        for z in zeilen:
            w.writerow([z[f] for f in felder])
    return pfad


def main(roh):
    pruef = []

    # MARA
    kopf, d, ers = lese(os.path.join(roh, 'mara_roh.txt'))
    felder = ['MATNR', 'MTART', 'MATKL', 'MEINS', 'LVORM', 'ZZTYP', 'ZZTYP_F4']
    assert set(felder) <= set(kopf), kopf
    mara = []
    for r in d:
        mara.append({f: (matnr_extern(r[f]) if f == 'MATNR' else r[f].strip()) for f in felder})
    mara_m = {r['MATNR'] for r in mara}
    pruef.append(('MARA', len(d), len(mara_m), ers, kopf))
    schreibe('MARA.csv', felder, mara)

    # MBEW
    kopf, d, ers = lese(os.path.join(roh, 'mbew_1100_roh.txt'))
    felder = ['MATNR', 'BWKEY', 'BWTAR', 'VPRSV', 'STPRS', 'VERPR', 'PEINH', 'LBKUM']
    mbew, mit_bwtar = [], 0
    for r in d:
        if r['BWTAR'].strip():
            mit_bwtar += 1
            continue
        z = {f: r[f].strip() for f in felder}
        z['MATNR'] = matnr_extern(z['MATNR'])
        for f in ('STPRS', 'VERPR', 'PEINH', 'LBKUM'):
            z[f] = zahltext(zahl(z[f]))
        mbew.append(z)
    pruef.append(('MBEW 1100', len(d), len({r['MATNR'] for r in mbew}), ers, 'BWTAR gefuellt verworfen: %d' % mit_bwtar))
    schreibe('MBEW_1100.csv', felder, mbew)

    # MARC
    kopf, d, ers = lese(os.path.join(roh, 'marc_1100_roh.txt'))
    felder = ['MATNR', 'WERKS', 'BESKZ', 'SOBSL', 'MMSTA', 'LVORM']
    marc = [{f: (matnr_extern(r[f]) if f == 'MATNR' else r[f].strip()) for f in felder} for r in d]
    pruef.append(('MARC 1100', len(d), len({r['MATNR'] for r in marc}), ers, ''))
    schreibe('MARC_1100.csv', felder, marc)

    # MAKT (zwei Laeufe SPRAS DE und EN; im Export steht der interne Schluessel D bzw. E)
    makt = []
    for datei in ('makt_de_roh.txt', 'makt_en_roh.txt'):
        kopf, d, ers = lese(os.path.join(roh, datei))
        teil = [{'MATNR': matnr_extern(r['MATNR']), 'SPRAS': r['SPRAS'].strip(), 'MAKTX': r['MAKTX'].strip()} for r in d]
        pruef.append(('MAKT ' + datei, len(d), len({(r['MATNR'], r['SPRAS']) for r in teil}), ers,
                      'SPRAS: ' + ','.join(sorted({r['SPRAS'] for r in teil}))))
        makt += teil
    schreibe('MAKT.csv', ['MATNR', 'SPRAS', 'MAKTX'], makt)

    for p in pruef:
        print('PRUEF', p[0], 'zeilen', p[1], 'eindeutig', p[2], 'ersatzzeichen', p[3], p[4] if isinstance(p[4], str) else '')

    print('MBEW nicht in MARA', len({r['MATNR'] for r in mbew} - mara_m))
    print('MARC nicht in MARA', len({r['MATNR'] for r in marc} - mara_m))
    print('MAKT nicht in MARA', len({r['MATNR'] for r in makt} - mara_m))
    print('MARA ZZTYP gefuellt', sum(1 for r in mara if r['ZZTYP']), 'ZZTYP_F4 gefuellt', sum(1 for r in mara if r['ZZTYP_F4']),
          'beide', sum(1 for r in mara if r['ZZTYP'] and r['ZZTYP_F4']), 'von', len(mara))
    print('MARA ohne LVORM', sum(1 for r in mara if not r['LVORM']), 'davon ZZTYP gefuellt',
          sum(1 for r in mara if not r['LVORM'] and r['ZZTYP']))


if __name__ == '__main__':
    main(sys.argv[1])
