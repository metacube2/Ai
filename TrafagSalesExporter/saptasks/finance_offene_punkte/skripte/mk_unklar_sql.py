# Baut je TSC (TRFR, TRUS, TRIT) eine reine Lese-Abfrage fuer HanaQ: Belegart (DocType), Artikel, Konto der Unklar-Zeilen.
import csv, collections
by = collections.defaultdict(lambda: {'INV': set(), 'CRN': set()})
for r in csv.DictReader(open('unklar_zeilen.csv', encoding='utf-8-sig'), delimiter=';'):
    if r['TSC'] in ('TRFR', 'TRUS', 'TRIT'):
        by[r['TSC']][r['Document Type']].add(int(float(r['Document Entry'])))
for tsc, d in by.items():
    parts = []
    for typ, (h, l) in (('INV', ('OINV', 'INV1')), ('CRN', ('ORIN', 'RIN1'))):
        if not d[typ]:
            continue
        ids = ','.join(str(x) for x in sorted(d[typ]))
        parts.append(f"""-- {tsc} {typ} je Belegart, Artikel leer, Konto
SELECT '{typ}' AS T, h."DocType", CASE WHEN COALESCE(p."ItemCode",'') = '' THEN 'ohne Artikel' ELSE 'mit Artikel' END AS ART, p."AcctCode", COUNT(*) AS N, SUM(p."LineTotal") AS LT FROM "{{SCHEMA}}"."{h}" h INNER JOIN "{{SCHEMA}}"."{l}" p ON p."DocEntry" = h."DocEntry" WHERE h."DocEntry" IN ({ids}) GROUP BY h."DocType", CASE WHEN COALESCE(p."ItemCode",'') = '' THEN 'ohne Artikel' ELSE 'mit Artikel' END, p."AcctCode" ORDER BY 5 DESC""")
        # Belegart aller Ausgangsrechnungen/Gutschriften ab 2025 zur Einordnung
        parts.append(f"""-- {tsc} {typ} alle Belege ab 2025 je DocType
SELECT '{typ}' AS T, h."DocType", COUNT(DISTINCT h."DocEntry") AS BELEGE, COUNT(*) AS ZEILEN FROM "{{SCHEMA}}"."{h}" h INNER JOIN "{{SCHEMA}}"."{l}" p ON p."DocEntry" = h."DocEntry" WHERE h."DocDate" >= '2025-01-01' GROUP BY h."DocType\"""")
    open(f'q_unklar_{tsc}.sql', 'w', encoding='utf-8').write('\n;;\n'.join(parts) + '\n')
    print(tsc, {k: len(v) for k, v in d.items()})
