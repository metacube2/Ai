# Materialien, auf die viele verschiedene KDMAT zeigen (Sammel-/Dummyverdacht)
import csv, collections, os
from p76lib import norm_mat, MAKT, P76
L2T = {'FR': 'TRFR', 'IT': 'TRIT', 'GB': 'TRUK', 'DE': 'TRDE', 'IN': 'TRIN', 'US': 'TRUS', 'ES': 'TRES', 'AT': 'TRAT'}
K = {norm_mat(r['KUNNR']): L2T[r['LAND1']] for r in csv.DictReader(open(os.path.join(P76, 'KNA1_IC.csv'), encoding='utf-8-sig'), delimiter=';')
     if not r['HINWEIS_AGENT'] and r['LAND1'] in L2T}
fan = collections.defaultdict(set); ex = collections.defaultdict(list)
for f in ('KNMT.csv', 'VBAP_IC.csv'):
    for r in csv.DictReader(open(os.path.join(P76, f), encoding='utf-8-sig'), delimiter=';'):
        t = K.get(norm_mat(r['KUNNR'])); kd = (r['KDMAT'] or '').strip(); mt = norm_mat(r['MATNR'])
        if t and kd:
            fan[mt].add((t, kd))
            if len(ex[mt]) < 3: ex[mt].append((t, kd[:25]))
for mt, s in sorted(fan.items(), key=lambda x: -len(x[1]))[:12]:
    print(mt, len(s), MAKT[mt].get('D', '')[:30], ex[mt])
print('Materialien mit 2-4 KDMAT:', sum(1 for s in fan.values() if 2 <= len(s) <= 4), ' mit >=5:', sum(1 for s in fan.values() if len(s) >= 5))
