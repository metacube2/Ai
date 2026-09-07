# ABAP-Analysereport: Standardpreis + Journal (CH/AT)

Stand: 2026-07-14
Report: `ZFIN_ANALYSE_STPRS_JOURNAL.abap`
Ziel: **travp762 (PRODUKTIV)** — ausdruecklich nicht travt762.

## Wozu

Das Finance Dashboard hat drei offene Punkte, die alle nur SAP beantworten kann.
Statt sie einzeln per OData zu erraten, beantwortet dieser eine Report sie an der Quelle.
Der Report ist **rein lesend** (nur SELECT/WRITE, keine Aenderung, kein COMMIT).

## Ausfuehrung

1. `SE38` -> Report anlegen (Typ 1, ausfuehrbares Programm), Quelltext einfuegen, aktivieren.
2. Selektion:
   - `s_bukrs`: die Buchungskreise Schweiz und Oesterreich (leer = alle).
   - `s_gjahr`: `2025` bis `2026`.
   - `p_abschn`: Anzahl Stichprobenzeilen (Default 4).
3. Ausfuehren, dann `System > Liste > Sichern > Lokale Datei` und die Datei an Ingo geben.

Laufzeit-Hinweis: Teil 4 liest die Fakturapositionen ab 01.01.2025. Bei sehr grossen
Mengen zuerst mit nur einem Buchungskreis testen.

## Welche Frage beantwortet welcher Teil

| Teil | Offene Frage | Was wir aus der Antwort machen |
| --- | --- | --- |
| 1 | Welche Buchungskreise sind CH bzw. AT, welche Hauswaehrung? | Trennung CH/AT im Dashboard (Spalte `CompanyCode`), korrekte Waehrung |
| 2 | Ueber welchen Bewertungskreis (`BWKEY`) haengt der Materialpreis am Buchungskreis? | Join-Schluessel fuer den Standardpreis |
| 3 | **Ist `MBEW-STPRS` ueberhaupt gefuellt?** Wie viele Materialien haben `VPRSV = S`? Welche Preiseinheit (`PEINH`)? | Ob der Standardpreis als Kostenbasis taugt. `PEINH` ist kritisch: der Preis gilt pro X Stueck — wird das uebersehen, liegt die Marge um Faktor 10/100 daneben |
| 4a | **Ist `VBRP-WAVWR` (Kostenwert direkt auf der Fakturaposition) gefuellt?** | Wenn ja, ist das die einfachste Loesung: Kosten kommen direkt mit der Umsatzzeile, ganz ohne Materialstamm-Join |
| 4b | Haben die **tatsaechlich fakturierten** Materialien einen Standardpreis? | Eine hohe Abdeckung im Materialstamm nuetzt nichts, wenn ausgerechnet die verkauften Artikel keinen Preis haben. Das ist die eigentliche Kennzahl |
| 4c | Stichprobe zum Nachrechnen von Hand | Fachliche Plausibilisierung mit Andreas (Menge x Preis / Preiseinheit vs. Netto) |
| 5 | **Gibt es 2026er Fakturen auf PRODUKTIV?** | Das Dashboard sieht fuer CH/AT null Zeilen fuer 2026. In der App ist als Service `travt762` (Test!) hinterlegt. Kommen hier 2026-Zeilen zurueck, ist die Ursache gefunden: falsches System, kein SAP-Datenproblem |
| 6 | Reicht `BSIS` fuer das Hauptbuch-Journal, oder brauchen wir `BSEG`? | `BSIS/BSID/BSIK` enthalten nur **offene** Posten; ausgeglichene liegen in `BSAS/BSAD/BSAK`. Die Zahlen zeigen, wie gross die Luecke waere. Davon haengt ab, ob die geplante OData-Spezifikation (`FinanzJournalSet`) ueberhaupt noch noetig ist |
| 7 | Welche Belegarten (`BLART`) gelten als "manuelle Buchung"? | Das Dashboard nimmt aktuell `SA` an — diese Annahme muss Finance bestaetigen |

## Was danach passiert

- **Ist `WAVWR` gut gefuellt (Teil 4a):** wir lesen den Kostenwert direkt aus der Faktura.
  Kein neues SAP-Objekt noetig, nur ein zusaetzliches Feld im bestehenden EntitySet
  `FinanzdataSchweizOeSet`.
- **Ist nur `STPRS` gut gefuellt (Teil 3/4b):** wir lesen zusaetzlich `mbewSet`
  (existiert bereits im Service `ZPOWERBI_EINKAUF_SRV`) und joinen auf Material +
  Bewertungskreis — inklusive `PEINH`.
- **Ist beides duenn:** dann ist die Gruppenmarge fuer CH/AT fachlich nicht berechenbar
  und das muss mit Andreas geklaert werden, statt eine Scheingenauigkeit zu bauen.

## Offene fachliche Fragen an Andreas (unabhaengig vom Report)

1. Welche Kostenart ist gemeint: lokaler Einstandswert der Rechnungszeile oder
   Konzern-Herstellkosten?
2. Bei internem Trafag-Lieferanten: welcher Preis gilt — der der liefernden oder der
   der verkaufenden Gesellschaft?
3. Gilt `BLART = SA` als "manuell"?
