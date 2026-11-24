# Laedt die P76-Abzuege (nur lesend) und stellt Hilfsfunktionen fuer Typencode-Vergleiche bereit.
import csv, re, os, statistics, collections
P76 = r'C:\Users\koi\source\repos\Ai\TrafagSalesExporter\saptasks\finance_offene_punkte\p76'

def _rd(name):
    with open(os.path.join(P76, name), encoding='utf-8', newline='') as fh:
        return list(csv.DictReader(fh, delimiter=';'))

def norm_mat(m):
    m = (m or '').strip().upper()
    return m.lstrip('0') if m.isdigit() else m

MARA = {norm_mat(r['MATNR']): r for r in _rd('MARA.csv')}
MBEW = {norm_mat(r['MATNR']): r for r in _rd('MBEW_1100.csv')}
MARC = {norm_mat(r['MATNR']): r for r in _rd('MARC_1100.csv')}
MAKT = collections.defaultdict(dict)
for r in _rd('MAKT.csv'):
    MAKT[norm_mat(r['MATNR'])][r['SPRAS']] = r['MAKTX']

def unit_cost(m):
    b = MBEW.get(norm_mat(m))
    if not b:
        return None
    try:
        p = float(b['PEINH'] or 1) or 1.0
        return float(b['STPRS'] or 0) / p
    except ValueError:
        return None

def active(m):
    m = norm_mat(m)
    a = MARA.get(m); c = MARC.get(m)
    return bool(a) and not a['LVORM'] and bool(c) and not c['LVORM']

def code_keys_from_tokens(tokens):
    """key1 = alle Token zusammen; key2 = ohne reine Null-Token (Fueller 0000/00)."""
    toks = [t.upper() for t in tokens if t]
    k1 = ''.join(toks)
    k2 = ''.join(t for t in toks if set(t) != {'0'})
    return {k for k in (k1, k2) if len(k) >= 8}

# Index Typencode -> Materialien (nur ohne Loeschvormerkung in MARA)
TYP = collections.defaultdict(set)
for m, r in MARA.items():
    if r['LVORM']:
        continue
    if r['ZZTYP'].strip():
        for k in code_keys_from_tokens(r['ZZTYP'].split()):
            TYP[k].add(m)
    f4 = re.sub(r'[\s.]', '', r['ZZTYP_F4'].upper())
    if len(f4) >= 8:
        TYP[f4].add(m)

CODE_RE = re.compile(r'(?<![0-9A-Z.])(\d{3,4}(?:\.[0-9A-Z]{1,5}){2,14})(?![0-9A-Z])', re.I)

def text_codes(text):
    """Typencode-Kandidaten aus einem Text: Familie (3-4 Ziffern) plus mindestens zwei Token, getrennt durch Punkt/Leerzeichen.
    Liefert je Kandidat die Schluessel des ganzen Codes und aller Verkuerzungen am Ende (laengste zuerst)."""
    out = []
    for mo in CODE_RE.finditer((text or '').upper()):
        toks = mo.group(1).split('.')
        variants = []
        for n in range(len(toks), 2, -1):
            variants.append((n, code_keys_from_tokens(toks[:n])))
        out.append((mo.group(1), toks, variants))
    return out

def match_codes(text):
    """Bestes Ergebnis: ('eindeutig'|'mehrdeutig'|'kein Treffer', materials, code, vollstaendig?)"""
    best = None
    for raw, toks, variants in text_codes(text):
        for n, keys in variants:
            hits = set()
            for k in keys:
                hits |= TYP.get(k, set())
            if hits:
                full = n == len(toks)
                cand = ('eindeutig' if len(hits) == 1 else 'mehrdeutig', sorted(hits), '.'.join(toks[:n]), full)
                if best is None or (cand[0] == 'eindeutig' and best[0] != 'eindeutig') or (cand[3] and not best[3] and cand[0] == best[0]):
                    best = cand
                break
    return best or ('kein Treffer', [], '', False)
