# Shopfloor-Dashboard: Abgleich mit SAP und Excel, Feedback an Ingo

Stand: 13.11.2026, 09:30. Geprüft von Claude im Auftrag von Fabio.

Ziel: Das Dashboard soll das Excel der Teamleiter ablösen. Dafür müssen die SAP-Felder dieselben Werte zeigen wie das Excel heute.

## Womit geprüft wurde

- Dashboard: `api/daily` aller Abteilungen, 02.–13.11.2026 (Lieferung heute 06:16 und 07:57).
- Excel: «PPA-Shop-Floor-File-2026_Version 1.04.xlsx» (Daniel Tobler), Stand 13.11. morgens.
- SAP P76, nur lesend, am 13.11. zwischen 09:00 und 09:25:
  - ZC22 (ZM_OFFENE_FAUF) je Disponentengruppe, mit Planaufträgen, ohne erledigte.
  - ZC27 (ZPP_RM_MENGEN) je Arbeitsplatz, Werk 1100, Rückmeldedatum 02.–12.11.
  - AFRU (05.11., Werk 1100), AFKO (Disponent S*, Istendtermin 02.–12.11.), CE11000 (Periode 11/2026).

Die SAP-Abzüge sind rund drei Stunden jünger als die Dashboard-Lieferung. Abweichungen im Bereich von wenigen hundert Stück bei den Vorräten sind deshalb normal.

## Übersicht

| Nr. | Bereich | Befund | Wer |
|---|---|---|---|
| 1 | Auftragsvorrat 1/2/3 Monate (alle) | Dashboard rechnet bis Monatsende, das Excel rollend ab heute | Ingo |
| 2 | Auftragsvorrat TR5 | Dashboard enthält nicht-numerische Materialien (rund 3'100 Stk. zu viel) | Ingo |
| 3 | Auftragsvorrat DW | Dashboard enthält DS1 (rund 64'000 Stk.), das Excel nicht. Entscheid Fabio 13.11.: D*, Material 1–99999 | Ingo |
| 4 | Auftragsvorrat TX | Dashboard rund 900 Stk. höher, Ursache offen | Ingo |
| 5 | Verzug PPD | alle vier Werte falsch (TR5 559 statt 304, DW 9'575 statt rund 440) | Ingo |
| 6 | Logistik MLE01/02/04, DW DL790/DLQ72 | «letzter Vorgang» falsch umgesetzt, Dashboard zählt zu viel | Ingo |
| 7 | Planmenge TX nächste 5 AT | Dashboard ohne überfällige und heutige Menge | Ingo |
| 8 | Verkauf AE / Umsatz IST | Job fehlt; Definition gegen SAP und Excel bestätigt | Ingo |
| 9 | Monatswechsel Verkauf | «Vortag» muss letzter Arbeitstag sein | Ingo |
| 10 | Produzierte Tagesmenge SEH | Dashboard entspricht der Anleitung des Teamleiters (COOIS) und SAP exakt; keine Änderung | – |
| 11 | Erwarteter Umsatz | Fiori-Wert rund 600'000 CHF höher als ZC23 im Excel. Entscheid Fabio 13.11.: Fiori bleibt | – |

In Ordnung und ohne Änderung: Logistik MLE08+09 und MLM01 (9 von 9 Tagen exakt), Auftragsvorrat total SEH, Tagesziel SEH ab 10.11., offene Menge KW SEH, Tagesmenge TR5 (8 von 9 Tagen), Tagesmenge DW DL790 (7 von 9 Tagen).

## 1. Auftragsvorrat 1 / 2 / 3 Monate: rollend statt Monatsende

Das Dashboard rechnet wie spezifiziert bis Monatsende. Die Teamleiter rechnen aber rollend ab dem Abfragetag. Die Spezifikation vom 07.11. war in diesem Punkt falsch.

Kontrolle SEH am 13.11. (ZC22, Disponent S00–SZZ, alle Materialien):

| Fenster | Excel | Dashboard | SAP bis Monatsende | SAP rollend |
|---|---|---|---|---|
| 1 Monat | 38'633 | 27'002 | 27'062 (bis 30.11.) | 38'557 (bis 13.12.) |
| 2 Monate | 51'937 | 44'055 | 44'089 (bis 31.12.) | 51'955 (bis 13.01.) |
| 3 Monate | 80'669 | 68'368 | 68'162 (bis 31.01.) | 80'507 (bis 13.02.) |
| total | 156'679 | 156'723 | – | 156'517 |

Neue Regel, kumuliert inklusive überfälliger Aufträge:

- SEH, TR5, TX: Eckendtermin bis Abfragetag + 1 / 2 / 3 Kalendermonate.
- DW: Eckendtermin bis Abfragetag + 4 / 8 / 12 Wochen (28 / 56 / 84 Tage).

Kontrolle der übrigen Abteilungen am 13.11. (SAP mit der neuen Regel gegen Excel):

| Abteilung | 1 Monat SAP / Excel | 2 Monate SAP / Excel | 3 Monate SAP / Excel |
|---|---|---|---|
| TR5 (Monate) | 15'477 / 15'059 | 23'519 / 23'221 | 34'843 / 38'633 |
| TX (Monate) | 22'166 / 22'178 | 24'637 / 24'473 | 33'918 / 33'880 |
| DW (Wochen) | 16'730 / 16'521 | 24'158 / 23'949 | 33'819 / 33'610 |

Der Excel-Wert TR5 3 Monate (38'633) ist am 13.11. identisch mit SEH 1 Monat und vermutlich ein Übertragungsfehler im Excel.

## 2. Auftragsvorrat TR5: nur numerische Materialnummern

| | total |
|---|---|
| Excel | 61'846 |
| ZC22 EK*, Material 1–99999 | 61'857 |
| ZC22 EK*, alle Materialien | 64'945 |
| Dashboard | 65'234 |

Die Differenz sind Disponent EKB (1'005 Stk.) und nicht-numerische Materialien bei EK4 (2'083 Stk.). Bitte die Materialabgrenzung 1–99999 der Variante «/PPA EKX ALLE» übernehmen. Sie gilt für alle Felder (total, Monatsfenster, KW, Verzug).

## 3. Auftragsvorrat DW: Abgrenzung

| | total | aktuelle KW |
|---|---|---|
| Excel | 48'883 | 612 |
| ZC22 D*, Material 1–99999 | 49'092 | 726 |
| ZC22 D* ohne DW1, alle Materialien | 113'492 | 9'256 |
| Dashboard | 114'918 | 10'516 |

Das Dashboard folgt dem bisherigen Entscheid «D* ohne DW1/DL6, inklusive DS1/DS2/DSN». DS1 allein sind 64'391 Stk. mit nicht-numerischen Materialien, zum Teil seit September überfällig. Das Excel des Teamleiters zählt nur numerische Materialien: also ohne DS1 und ohne DW1, aber mit DS2 und DEF.

**Entscheid Fabio 13.11.2026:** Disponent D*, Material 1–99999. Das gilt für Vorrat total, die Fenster, offene Menge KW und Verzug DW. Der frühere Entscheid «ohne DW1/DL6, inklusive DS1/DS2/DSN» ist damit ersetzt.

## 4. Auftragsvorrat TX: rund 900 Stück zu viel

| | total | aktuelle KW |
|---|---|---|
| Excel | 48'885 | 901 |
| ZC22 EL* | 48'898 | 846 |
| Dashboard | 49'788 | 1'735 |

Die Differenz liegt seit dem 09.11. täglich bei 700 bis 1'200 Stk. und steckt in der aktuellen Woche. Die Ursache habe ich nicht gefunden. Bitte die Auftragsliste des Jobs gegen die Datei `ZC22_Positionen_bis_20.11_Abzug_2026-11-13_0905.csv` halten (alle ZC22-Positionen mit Eckendtermin bis 20.11.).

## 5. Verzug PPD

ZC22 mit Eckendtermin 01.01.2026 bis 12.11.2026, mit Planaufträgen, mit Vorgangsdaten, ohne erledigte:

| Abteilung | Dashboard | Excel | ZC22 (09:15) |
|---|---|---|---|
| SEH | 89 | 45 | 33 |
| TR5 | 559 | 304 | 304 |
| TX | 567 | 97 | 144 |
| DW | 9'575 | 279 | 441 (Material 1–99999); 17'174 mit DS1 und DW1 |

TR5 stimmt zwischen ZC22 und Excel exakt. Die Dashboard-Werte passen zu keiner der Abgrenzungen. Bitte prüfen:

- Eckendtermin von 01.01.2026 bis Vortag (nicht bis heute, keine älteren Aufträge).
- Menge = Spalte «Menge» aus ZC22.
- Gleiche Material- und Disponentenabgrenzung wie beim Vorrat (Punkte 2 und 3).

## 6. Logistik und DW: «letzter Vorgang» des Auftrags

ZC27 je Arbeitsplatz stimmt an allen Tagen exakt mit dem Excel überein. Das Dashboard weicht an diesen Tagen ab:

| Feld | Tag | Dashboard | ZC27 = Excel |
|---|---|---|---|
| MLE01 | 03.11. | 760 | 712 |
| MLE02 | 05.11. | 1'059 | 644 |
| MLE02 | 06.11. | 881 | 481 |
| MLE02 | 09.11. | 712 | 290 |
| MLE02 | 11.11. | 728 | 628 |
| MLE04 | 03.11. | 744 | 353 |
| MLE04 | 04.11. | 1'048 | 771 |
| MLE04 | 09.11. | 1'166 | 1'154 |
| DW DL790 | 02.11. | 716 | 668 |
| DW DL790 | 10.11. | 913 | 813 |
| DW DLQ72 | 02.11. | 501 | 486 |
| DW DLQ72 | 03.11. | 294 | 285 |
| DW DLQ72 | 05.11. | 659 | 547 |
| DW DLQ72 | 06.11. | 718 | 614 |
| DW DLQ72 | 09.11. | 1'014 | 854 |
| DW DLQ72 | 10.11. | 1'018 | 795 |
| DW DLQ72 | 11.11. | 950 | 825 |
| DW DLQ72 | 12.11. | 590 | 482 |

Ursache, belegt an MLE02 am 05.11. (AFRU, ARBID 10000689, ohne Stornos):

- Alle Rückmeldungen am Arbeitsplatz: 1'279 Stk.
- Je Auftrag nur der höchste Vorgang **an diesem Arbeitsplatz**: 1'059 Stk. Das ist der Dashboard-Wert.
- Nur Rückmeldungen, deren Vorgang der letzte Vorgang **des ganzen Auftrags** ist: 644 Stk. Das ist ZC27. Die Differenz sind die Aufträge 1277626 und 1277627 (je 200 Stk. auf Vorgang 0200) und 1276357 (15 Stk. auf Vorgang 0050).

Bitte die Prüfung wie in ZPP_RM_MENGEN bauen: höchste AFVC-VORNR über alle Vorgänge des Auftrags, nicht nur über die Vorgänge des gewählten Arbeitsplatzes.

Hinweis: ZC27 liefert mit einem Arbeitsplatz-Intervall andere Zahlen als mit einem einzelnen Arbeitsplatz (MLE01 bis MLM01 ergab für MLE02 null). Die Teamleiter fahren jeden Arbeitsplatz einzeln.

## 7. Planmenge TX nächste 5 Arbeitstage

| | Wert 13.11. |
|---|---|
| Excel | 7'048 |
| ZC22 EL*, Eckendtermin bis 20.11. kumuliert | 7'083 |
| ZC22 EL*, nur 16.–20.11. | 6'237 |
| Dashboard | 6'262 |

Das Excel zählt alles bis zum fünften Arbeitstag, inklusive überfälliger und heutiger Aufträge. Bitte so umstellen.

## 8. Verkauf: AE und Umsatz IST (Job J2/J3 fehlt)

Die sechs Felder `ae_aktuell`, `ae_tx`, `ae_dw`, `umsatz_ist`, `u_tx`, `u_dw` kommen noch nicht. Damit bleiben auch Tagesumsatz, Book to Bill, Abweichung und die Grafik leer.

Die Definition ist für November gegen SAP und Excel bestätigt:

- Tabelle CE11000, BUKRS 1100, PERIO 011.2026, VRGAR A (AE) und F (Umsatz). Es gibt nur PALEDGER B0.
- Nettoerlös = VV100 + VV110 − VV200 − VV210 − VV220 − VV250 − VV230 − VV240.
- Transmitter = WWPSP 0005, DW = WWPSP 0003.
- Stichtag: Stand bis und mit Vortag nach **HZDAT** (Angelegt am), nicht BUDAT.

Der Umsatz stimmt damit an den Stichtagen 03., 04., 05., 06., 09. und 10.11. auf den Franken mit dem Excel überein, am 04.–10.11. auch die Aufteilung Transmitter / DW. Tageswerte zum Abgleichen: `..\2026-11-12_verkauf\Kontrollwerte_Verkauf_CE11000_2026-11.csv`.

## 9. Monatswechsel Verkauf

Das Excel zeigt am ersten Arbeitstag des Monats noch den Endstand des Vormonats (Montag, 02.11.: Umsatz Oktober 8'585'235). Das Dashboard zeigte am 02.11. beim erwarteten Umsatz bereits November. «Monat des Vortags» muss «Monat des letzten Arbeitstags» heissen, für J2, J3 und J4.

Dazu gehört das Config-Skript `..\2026-11-12_verkauf\config_verkauf_tagesumsatz.js`: Es korrigiert den Tagesumsatz am Monatswechsel (heute minus 6.7 Mio. am zweiten Tag). Formel geprüft, Skript noch nicht mit Node gelaufen.

## 10. Produzierte Tagesmenge SEH: keine Änderung

| Tag | Dashboard | SAP Gutmenge (AFKO-IGMNG) | SAP Auftragsmenge (GAMNG) | Excel |
|---|---|---|---|---|
| 02.11. | 2'525 | 2'525 | 2'545 | 2'815 |
| 03.11. | 1'861 | 1'861 | 1'915 | 1'595 |
| 04.11. | 2'146 | 2'146 | 2'151 | 2'851 |
| 05.11. | 3'525 | 3'525 | 3'838 | 3'958 |
| 06.11. | 4'046 | 4'046 | 4'048 | 4'228 |
| 09.11. | 3'363 | 3'363 | 3'370 | 3'430 |
| 10.11. | 2'258 | 2'258 | 2'261 | 2'261 |
| 11.11. | 2'547 | 2'547 | 2'551 | 2'611 |
| 12.11. | 3'572 | 3'572 | 3'574 | 3'574 |

Die Anleitung des Teamleiters (Mail M.-T. Oana vom 15.10.2026, «Shopfloorboard.docx») nennt für SEH: COOIS, Auftragsköpfe, Layout /SEH 001, nur Fertigungsaufträge, Disponent S*, Istendtermin = Tag, Kennzahl «Gelieferte Menge». Genau so gerechnet ergibt SAP an allen neun Tagen den Dashboard-Wert (gelieferte Menge AFPO-WEMNG = rückgemeldete Gutmenge). Das Dashboard bleibt, wie es ist.

Die Excel-Einträge weichen an sieben Tagen davon ab und lassen sich mit keiner SAP-Menge durchgehend nachrechnen (Auftragsmenge, Gutmenge, gelieferte Menge, nach Istendtermin oder Lieferdatum). Das ist eine Frage an den Teamleiter, nicht an Ingo.

## 11. Erwarteter Umsatz Monat: Fiori bleibt

Dashboard (Fiori «Umsatz prüfen») am 13.11.: 6'517'496 CHF. Excel (ZC23) am 12.11.: 5'914'315 CHF. Entscheid Fabio 13.11.2026: Fiori bleibt die Quelle; die Abweichung zum Excel (ZC23) ist in Ordnung. Den Fiori-Wert habe ich nicht gegen SAP nachgerechnet.

## Nicht geprüft

- Tagesmenge TR5 am 03.11. (Dashboard 761, Excel 820): ZC27 für EDF72 liess sich nicht ausführen.
- FPY SEH: im Excel gibt es dafür keine Tageswerte.
- Felder ohne Lieferung: FPY TR5/TX/DW (MES), Tagesmenge TX, Sensorbestände, MTF, CZ, offene Kaufteile.
- Manuelle Felder (Mitarbeiter je Schicht, frei, krank, Lieferzeiten, Tagesziel TR5): im Dashboard leer, im Excel täglich gepflegt. Das ist eine Frage der Umstellung bei den Teamleitern, nicht der Schnittstelle.
