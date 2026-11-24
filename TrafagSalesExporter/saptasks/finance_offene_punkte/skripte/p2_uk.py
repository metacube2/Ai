# Punkt 2: UK-Artikel Code 2 gegen MARA-Typencode (P76), Plausibilitaet ueber lokale Kosten / (STPRS/PEINH). Nur lesend.
import csv, collections, statistics
from p76lib import match_codes, unit_cost, active, MAKT, MARA, norm_mat
def f(v):
    try: return float(v) if v else 0.0
    except ValueError: return 0.0
loc = collections.defaultdict(list)
for r in csv.DictReader(open('s23_fd.csv', encoding='utf-8')):
    if r['TSC'] == 'TRUK' and r['Year'] in ('2025', '2026') and f(r['Standard Cost CHF (per unit)']) > 0:
        loc[r['Material']].append(f(r['Standard Cost CHF (per unit)']))
U = list(csv.DictReader(open('uk_code2.csv', encoding='utf-8-sig'), delimiter=';'))
out = []; cnt = collections.Counter(); z = collections.Counter(); gbp = collections.Counter(); bsp = collections.defaultdict(list)
fak = collections.defaultdict(list)
for r in U:
    k, mats, code, full = match_codes(r['Text'] + ' ' + r['Material'])
    if k != 'kein Treffer' and not full:
        k = 'nur Teilcode, ' + k
    lc = statistics.median(loc[r['Material']]) if loc[r['Material']] else None
    tgt = mats[0] if len(mats) == 1 else ''
    uc = unit_cost(tgt) if tgt else None
    factor = round(lc / uc, 3) if (lc and uc) else None
    if factor: fak[k].append(factor)
    cnt[k] += 1; z[k] += int(r['Zeilen']); gbp[k] += f(r['Umsatz GBP'])
    if len(bsp[k]) < 10: bsp[k].append((r['Material'], code, mats[:3], factor))
    out.append([k, r['Material'], r['Text'][:200], r['Zeilen'], r['Umsatz GBP'], code, 'ja' if full else ('nein' if code else ''),
                ','.join(mats[:10]), len(mats), tgt, MAKT[tgt].get('D', '') if tgt else '', 'ja' if (tgt and active(tgt)) else ('nein' if tgt else ''),
                round(lc, 4) if lc else '', round(uc, 4) if uc else '', factor if factor else ''])
order = {'eindeutig': 0, 'mehrdeutig': 1, 'nur Teilcode, eindeutig': 2, 'nur Teilcode, mehrdeutig': 3, 'kein Treffer': 4}
out.sort(key=lambda x: (order[x[0]], -f(x[4])))
with open('uk_typencode_vorschlag.csv', 'w', newline='', encoding='utf-8-sig') as fh:
    w = csv.writer(fh, delimiter=';')
    w.writerow(['Klasse', 'UK Material', 'UK Text', 'Zeilen', 'Umsatz GBP', 'Typencode im Text', 'ganzer Code', 'Treffer MARA', 'Anzahl Treffer',
                'Vorschlag Gruppennummer', 'MAKT D Ziel', 'Ziel aktiv (MARA+MARC 1100)', 'lokale Kosten CHF/Einheit (Median)', 'STPRS/PEINH CHF', 'Faktor lokal/STPRS'])
    w.writerows(out)
for k in order:
    v = fak[k]
    print(f'{k}: {cnt[k]} Artikel, {z[k]} Zeilen, {gbp[k]:,.0f} GBP; Faktor n={len(v)} Median {statistics.median(v) if v else "-"}; '
          f'0.3-3: {sum(1 for x in v if 0.3 <= x <= 3)}; Code ganz: {sum(1 for o in out if o[0]==k and o[6]=="ja")}')
    for b in bsp[k]: print('   ', b)
print('Total', sum(cnt.values()), sum(z.values()), round(sum(gbp.values())))
