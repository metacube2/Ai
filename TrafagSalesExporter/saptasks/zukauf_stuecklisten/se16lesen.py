# Liest SE16-%PC-Exporte (Tab-Text, Codepage 1160/cp1252) ueber die Kopfzeile mit den Feldnamen.
# Nutzung: from se16lesen import lese; zeilen = lese(pfad)  -> Liste von dicts {FELD: wert (getrimmt)}
import sys


def lese(pfad):
    with open(pfad, 'rb') as f:
        roh = f.read().decode('cp1252', errors='replace')
    zeilen = roh.split('\r\n') if '\r\n' in roh else roh.split('\n')
    kopf = None
    daten = []
    for z in zeilen:
        teile = z.split('\t')
        if kopf is None:
            if len(teile) > 2 and teile[1].strip() == 'MANDT':
                kopf = [(i, t.strip()) for i, t in enumerate(teile) if t.strip()]
            continue
        if len(teile) < 3 or teile[1].strip() == '' or teile[1].strip() == 'MANDT':
            continue
        d = {}
        for i, name in kopf:
            d[name] = teile[i].strip() if i < len(teile) else ''
        daten.append(d)
    if kopf is None:
        raise SystemExit('keine Kopfzeile in ' + pfad)
    return daten


def zahl(s):
    # SE16-Export P76 (Benutzer KOI): Dezimalpunkt, Tausender-Komma (z. B. PEINH "1,000" = 1000, MARC "1,080.000").
    # Ein einzelnes Komma ist deshalb ein Tausendertrenner, kein Dezimalzeichen (Fall MBEW-PEINH 24.11.2026).
    s = (s or '').strip().replace("'", '')
    if not s:
        return 0.0
    neg = s.endswith('-')
    s = s.rstrip('-').replace(',', '')
    v = float(s)
    return -v if neg else v


if __name__ == '__main__':
    d = lese(sys.argv[1])
    print('ZEILEN', len(d))
    if d:
        print('FELDER', ' '.join(list(d[0].keys())[:60]))
