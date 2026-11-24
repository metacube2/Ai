# Punkt 5: Kandidaten in MAKT D/E und MARA fuer die 5 Indien-Artikel Code 8 (nur lesend)
import re
from p76lib import MAKT, MARA, MARC, unit_cost
SUCH = {
    'DM000083/84 GDM insulation assy': [r'INSUL', r'ISOLIER', r'D[AÄ]MM', r'DYB0407', r'HYOSUNG', r'W[AÄ]RMESCHUTZ', r'THERMAL'],
    'DM000354 rain cover X6 thermal insulation': [r'RAIN', r'REGEN', r'2GSH', r'COVER', r'HAUBE', r'SCHUTZKAPPE.*X6', r'\bX6\b'],
    'FA000028/29 Welding machine': [r'WELD', r'SCHWEISS', r'SCHWEIß'],
}
for k, pats in SUCH.items():
    print('==', k)
    hits = {}
    for m, t in MAKT.items():
        txt = ' | '.join(f'{s}:{v}' for s, v in t.items())
        for p in pats:
            if re.search(p, txt, re.I):
                hits.setdefault(m, (p, txt))
    for m, (p, txt) in sorted(hits.items(), key=lambda x: (bool(MARA.get(x[0], {}).get('LVORM')), x[0]))[:40]:
        a = MARA.get(m, {})
        print(f'  {m:12} {a.get("MTART",""):5} LVORM={a.get("LVORM","")!s:1} MARC={"ja" if m in MARC else "nein"} STPRS/PEINH={unit_cost(m)} [{p}] {txt[:110]}  ZZTYP={a.get("ZZTYP","")[:40]}')
    print('  Treffer gesamt', len(hits))
