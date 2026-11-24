# Finance: Regelset Lieferantenzuordnung gegen Matrix und Code (Stand 2026-11-20)

Quelle: `Regelset_Lieferantenzuordnung.docx` und `Regelset_Lieferantenzuordnung_mit_Beispielen.docx` (Ingo, Wurzel des
Repos, nicht versioniert; Zahlen darin aus Sales_All vom 16.11.2026). Verglichen mit der Issue-Topic-Matrix vom 18.11.
(`docs/FINANCE_PLAN_MAPPING_ISSUE_TOPIC_2026-11-18.md`) und `Services/GroupMarginCalculator.cs` (`ResolveIssueTopic`).

## 1. Regelset als Text

Erste Unterscheidung wie in Sales_All je Land: Import Italien mit Lieferant TRIT ist von dort, ausser die folgenden Regeln greifen.

2. **Indien**, Feld Sales Type: FFM oder CM = eigene Fertigung (Trafag India); LRD = Ware von Trafag AG Schweiz; leer = weiter mit 3.
3. **Lieferantenfeld** (Name oder Nummer) enthaelt TRAFAG: intern. TRAFAG+ITALIA = Trafag Italia; TRAFAG+INDIA = Trafag India;
   sonst Trafag AG. Anderer Name = extern, es gelten die Kosten der Verkaufszeile.
4. **Lieferantenfeld leer:** Fuehrt TR CH Werk 1100 das Material? ja = Trafag AG; nein = lokale Standardkosten der
   verkaufenden Gesellschaft; kein Material in der Zeile = unklar.
5. **Intern:** Herstellkosten = Standardpreis der liefernden Gesellschaft; findet das Programm keinen Preis: lokaler
   Standardpreis der Zeile. Gerechnet wird nur bis zur ersten liefernden Konzerngesellschaft (Schnitt, Entscheid 09.09.).

## 2. Abweichungen

| Nr | Stelle | Regelset | Code und Matrix | Wirkung |
|---|---|---|---|---|
| 1 | Indien LRD | Ware von Trafag AG, Kosten nach Regel 5 (Standardpreis liefernde Gesellschaft) | Kosten bleiben offen, Code 8 „Konzernkosten fehlen“ (Entscheid 05.08., der lokale Wert ist dort der IC-Preis); die Matrix kennt den Fall nicht (E4 im Plan) | 143 Zeilen |
| 2 | Kein Lieferant | Regel 4: Werkstamm TR CH Werk 1100 entscheidet (TR AG oder lokal) | Code und Matrix Zeile 5 folgen Regel 4; Beschluss 27.08. (ISS-003.5) lautet „immer lokal“, Plan nennt das R7 | 8'703 Zeilen, rund 2,5 Mio. CHF Marge (Entscheid 1 fuer Andreas) |
| 3 | Lokale Kosten fehlen | nicht geregelt („sonst lokaler Standardpreis“) | Matrix Zeilen 6/7: „use 0“; Code vergibt 7 auch bei externem Lieferant ohne lokale Kosten (Annahme, nicht Matrix) | Rechnung mit Kosten 0, Status „Standardpreis fehlt“ |
| 4 | Lieferant unklar | nur „kein Material in der Zeile“ | Code 9 bei widersprechenden Feldern (UK: TR09 mit Name Trafag AG); der Name gewinnt, das Regelset sagt nicht, ob Name oder Nummer | wenige Zeilen  *Berichtigt 2026-11-24:* Code 9 heisst im Code „kein Lieferant und kein Artikel“, also genau Regelset „kein Material in der Zeile“; die 131 Zeilen sind Nebenerloese ohne Artikel (`docs/FINANCE_SUPPLIER.md` 14); die Lesart „widersprechende Felder“ war falsch. |
| 5 | Regel 1 | Einleitungssatz zu Italien unvollstaendig, Nummerierung beginnt bei 2; die Antworten fuer Andreas nennen „Regel 1“ fuer CH und AT (TRCH, TRAT zu 100 % TR AG) | Code: CH/AT-Regel vorhanden | nur Text, aber Mail Andreas zitiert Regel 1 |

## 3. Offene Fragen (Frageliste)

1. Indien LRD: Kosten offen lassen (Stand heute, IC-Preis) oder Standardpreis TR AG (Wortlaut Regelset)?
2. Kein Lieferant: Regel 4 mit Werkstamm (Code, Matrix) oder „immer lokal“ (Beschluss 27.08.)? Wer bestaetigt?
3. Externer Lieferant, lokale Kosten fehlen: „use 0“ (Code 7) richtig?
4. Widerspruechliche Lieferantenfelder: Name oder Nummer gewinnt, oder „unklar“ (Code 9)?
5. Regel 1: vollstaendiger Satz zu Italien (Importware mit Lieferant TRIT: liefernde Gesellschaft TR IT, ausser 2 bis 5 greifen davor?).

Fragen 1 und 2 vor der Mail an Andreas klaeren. Bisher keine Code-Aenderung.
