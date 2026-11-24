codes = [c for c in open('italien162_codes.txt').read().split('\n') if c]
missing = ['66176', 'M_IT01_001778', 'M_IT01_000842', 'M_IT01_000760', 'M_IT01_000531', 'M_IT01_000320', 'M_IT01_000311']
lst = ','.join("'" + c.replace("'", "''") + "'" for c in codes)
ml = ','.join("'" + c + "'" for c in missing)
q = []
q.append(f"""-- TRIT 162 Artikel: Standardlieferant im Artikelstamm heute (OITM.CardCode)
SELECT COALESCE(NULLIF(i."CardCode", ''), '(leer)') AS CARDCODE, COUNT(*) AS ARTIKEL FROM "{{SCHEMA}}"."OITM" i WHERE UPPER(i."ItemCode") IN ({lst}) GROUP BY COALESCE(NULLIF(i."CardCode", ''), '(leer)') ORDER BY 2 DESC""")
q.append(f"""-- TRIT 162 Artikel: davon im Artikelstamm gefunden
SELECT COUNT(*) AS GEFUNDEN FROM "{{SCHEMA}}"."OITM" i WHERE UPPER(i."ItemCode") IN ({lst})""")
q.append(f"""-- TRIT 162 Artikel: Rechnungszeilen ab 2025 mit Wareneingang am oder vor Rechnungsdatum (Artikel ohne CardCode)
SELECT CASE WHEN EXISTS (SELECT 1 FROM "{{SCHEMA}}"."PDN1" g INNER JOIN "{{SCHEMA}}"."OPDN" o ON o."DocEntry" = g."DocEntry" WHERE g."ItemCode" = p."ItemCode" AND g."Quantity" > 0 AND o."CANCELED" = 'N' AND o."DocDate" <= h."DocDate") THEN 'mit WE' ELSE 'ohne WE' END AS WE, CASE WHEN COALESCE(i."CardCode", '') = '' THEN 'Stamm leer' ELSE 'Stamm gepflegt' END AS STAMM, COUNT(*) AS ZEILEN, SUM(p."LineTotal") AS EUR FROM "{{SCHEMA}}"."OINV" h INNER JOIN "{{SCHEMA}}"."INV1" p ON p."DocEntry" = h."DocEntry" LEFT JOIN "{{SCHEMA}}"."OITM" i ON i."ItemCode" = p."ItemCode" WHERE h."CANCELED" = 'N' AND h."DocDate" >= '2025-01-01' AND UPPER(p."ItemCode") IN ({lst}) GROUP BY CASE WHEN EXISTS (SELECT 1 FROM "{{SCHEMA}}"."PDN1" g INNER JOIN "{{SCHEMA}}"."OPDN" o ON o."DocEntry" = g."DocEntry" WHERE g."ItemCode" = p."ItemCode" AND g."Quantity" > 0 AND o."CANCELED" = 'N' AND o."DocDate" <= h."DocDate") THEN 'mit WE' ELSE 'ohne WE' END, CASE WHEN COALESCE(i."CardCode", '') = '' THEN 'Stamm leer' ELSE 'Stamm gepflegt' END""")
q.append(f"""-- TRIT 7 fehlende Artikel: Rechnungszeilen ab 2025 je Artikel und Konto
SELECT p."ItemCode", p."AcctCode", COUNT(*) AS ZEILEN, SUM(p."LineTotal") AS EUR, MIN(h."DocDate") AS VON, MAX(h."DocDate") AS BIS FROM "{{SCHEMA}}"."OINV" h INNER JOIN "{{SCHEMA}}"."INV1" p ON p."DocEntry" = h."DocEntry" WHERE h."CANCELED" = 'N' AND h."DocDate" >= '2025-01-01' AND p."ItemCode" IN ({ml}) GROUP BY p."ItemCode", p."AcctCode" ORDER BY 1""")
open('q_it162.sql', 'w', encoding='utf-8').write('\n;;\n'.join(q) + '\n')
print(len(codes))
