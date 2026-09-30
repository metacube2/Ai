# SAP: Konditionsart ZRL2 (Mengenrabatt Lieferant) fuer Trafag AT, EkOrg 1200

Stand: 2026-09-30. Quelle: Mailverlauf Fabio Palma, Lucas Castro, Marco Widmer vom
29. und 30.09.2026 und die Transporttabellen in T76 (`E070`, `E070C`, `E07T`), per RFC gelesen.

## Fachlich

Neue Preisstruktur fuer den Einkauf der Trafag AT (EkOrg 1200) beim Lieferanten Trafag AG
(70953), aufgebaut am 2026-09-29 von Fabio und Ingo. Statt Spezialpreisen gilt: Bruttopreis,
minus 44 % Wiederverkaufsrabatt (RL01), minus Mengenstaffel (ZRL2), kaskadiert.
Beispiel: 100 € brutto bei 10 Stueck ergibt nach 44 % 56 € und nach 30 % netto 39,20 €.

## Customizing, transportiert

| | |
| --- | --- |
| Transport | **`T76K912628`** „Customizing Staffelpreise lieferantmenge %", Workbench/Customizing `W`, Aufgabe `T76K912629`, Inhaber KOI, freigegeben 29.09., Ziel P76 |
| Quellmandant | **090**, nicht 100 (`E070C`). Nach Mandant 100 kam der Inhalt per **SCC1**; der Auftrag selbst bleibt dabei ein 090-Auftrag |
| Inhalt | Konditionsart ZRL2 „Mengenrabatt Lief. %" (Kopie von RL01, Bezugsgroesse C, Staffelart A, Staffelmengeneinheit ST); Kalkulationsschema RM0000 neue Stufe 11 / Zaehler 0 mit ZRL2, Von-Stufe leer, damit ZRL2 auf den Nettowert nach RL01 rechnet |
| Test T76/100 | Fabio: Bestellung 45148453 (Lieferant Trafag CH, EkOrg 1200) erfolgreich. Marco: 45148454 und 45148455 im Werk 1100, PB00 wird wie gewohnt gezogen, keine Abweichung |
| Freigabe | Lucas Castro, 30.09.: „von mir aus darfst du den TA ins P76 transportieren" |
| Import P76 | **30.09.2026 durch Ingo** |

ZRL2 greift nur, wenn sie einem Lieferanten (Teilsortiment) ausdruecklich zugeordnet ist; sonst
wird sie nicht gezogen (Fabio, 30.09.).

## Stammdaten, nicht transportiert

Pflegt Fabio im Live von Hand; Stand der Pflege nach dem Import nicht belegt.

- Lieferant 70953, EkOrg 1200: Lieferantenteilsortimente STD und SPEZ.
- RL01 auf Ebene Teilsortiment STD: 44 %, gueltig ab 29.09.2026; der alte RL01 auf
  Lieferantenebene hat das Loeschkennzeichen.
- ZRL2 auf Teilsortiment STD, Staffel: ab 2 ST 10 %, ab 5 ST 25 %, ab 10 ST 30 %, ab 25 ST 35 %,
  ab 50 ST 40 %.
- Infosaetze: PB00 als Bruttopreis, Teilsortiment STD bzw. SPEZ fuer Materialien mit Spezialpreis.

Schulung der AT-Benutzer: Fabio vor Ort am Donnerstag nach dem 29.09. (Change Management im Projekt).

## Falle, die hier Zeit gekostet hat

In Mandant 100 war `T76K912628` in SE09/SE10 nicht zu sehen. Zwei Gruende, beide gemessen:
der Auftrag ist **freigegeben** (Standardselektion zeigt nur aenderbare) und stammt aus
**Mandant 090**. Finden: SE01 mit der Nummer, SE10 mit „Freigegeben", oder STMS-Importqueue P76.

## Offen

- Stammdatenpflege im Live und Testbestellung durch Fabio.
- Die Nachbarauftraege `T76K912622` bis `T76K912627` (ebenfalls Mandant 090) sind hier nicht
  zugeordnet; ob sie zu ZRL2 gehoeren, ist nicht geprueft.
