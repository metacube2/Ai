# SAP PS (Projektsystem): Einfuehrung und Demo in T76

Stand: 2026-10-20. Auftrag des Chefs an Ingo, Anlass Sitzung mit Philip Steiger (PMO): Wie funktioniert die
Leistungs- und Zeiterfassung auf Projekten in SAP, wie bucht man auf Teilprojekte, wie kommen externe Kosten aus
Rechnungen aufs Projekt, und ist das einfach oder schwierig? Zielbild laut Notiz: Zeiterfassung in SAP nicht mehr auf
Innenauftraege, sondern ueber das PS-Modul; Planung und Risiken in TPG ProjectPowerPack (Folien
`20260223 - Abgleich TPG - Trafag.pptx`, Stufe 2/3 „Anbindung SAP Zeitrueckmeldung/Finanzdaten“).

Durchgespielt ist alles in **T76 Mandant 100** (Testsystem, Claudes eigene Sitzung). **P76 ist am 2026-10-20 nur lesend
geprueft** (Abschnitt 3a): gleicher Stand wie T76 vor der Demo. Screenshots: `docs/img/ps_demo_2026-10-20/`.

## 1. Kurzfazit fuer den Chef

| Frage | Antwort |
| --- | --- |
| Gibt es PS in unserem SAP? | Ja, als Standardmodul vorhanden, aber **fuer Trafag nicht eingerichtet** (Abschnitt 3) |
| Ist es fuer Anwender einfach? | **Ja.** Ein Projekt mit zwei Teilprojekten war in CJ01 in wenigen Minuten angelegt; Zeit ist eine Zeile je Projekt und Tag |
| Ist die Einfuehrung einfach? | **Nein, mittel.** Kostenrechnungskreis, Projektprofil, CATS-Profil, Leistungsarten mit Tarifen, Feldstatus der Sachkonten, Abrechnung und Berechtigungen muessen eingerichtet werden |
| Groesster Brocken | **Zeiterfassung**: heute in rexx. SAP-Zeit auf Projekte heisst doppelt erfassen oder eine Schnittstelle rexx -> SAP |
| TPG ProjectPowerPack | nicht im SAP installiert (kein Namensraum, keine Transaktion in T76); Anbindung an SAP ist Stufe 2/3 der Folien und mit Philip/TPG zu klaeren |

## 2. Wie PS funktioniert (Begriffe)

- **Projektdefinition**: Klammer um das Projekt (Nummer, Bezeichnung, Kostenrechnungskreis, Buchungskreis, Werk, Waehrung, Termine).
- **PSP-Element** (Projektstrukturplan-Element, englisch WBS): die Teilprojekte in einer Hierarchie. Kennzeichen je Element:
  *Planungselement* (darf geplant werden), *Kontierungselement* (darf bebucht werden), *Fakturierungselement*.
- **Netzplan/Vorgaenge**: optional fuer Termin- und Kapazitaetsplanung; fuer Zeit- und Kostensammlung nicht noetig.
- **Status**: erst **freigegeben** (FREI) darf gebucht werden. So laesst sich auch abgrenzen, wann und worauf gebucht wird.
- **Kosten** sammeln sich auf dem PSP-Element: Stunden x Tarif der Leistungsart, Rechnungen, Material. Am Ende rechnet man
  sie ab (CJ88), zum Beispiel auf eine Kostenstelle oder Anlage.

„Abgegrenzt buchen“ kann zweierlei heissen, beides geht:
1. **Wer darf worauf buchen:** nur PSP-Elemente mit Kontierungskennzeichen und Status FREI sind bebuchbar; einzelne
   Teilprojekte lassen sich sperren (Status „technisch abgeschlossen“ oder Buchungssperre) und ueber Berechtigungen trennen.
2. **Periodengerecht:** Buchungsdatum bestimmt die Periode; Budget (CJ30) und Plan (CJ40) laufen je Jahr/Periode.
Welche Bedeutung Philip meint, ist mit ihm zu klaeren.

## 3. Befunde aus T76 (gemessen)

| Nr | Befund | Beleg |
| --- | --- | --- |
| 1 | Keine Projekte, keine PSP-Elemente, keine CATS-Saetze vorhanden (PROJ, PRPS, CATSDB leer) | SapProbe 20.10. |
| 2 | Nur SAP-Standardprofile `0000001` „Standard-Projektprofil“ und `0000002` „Investitionsprojekte“; das Standardprofil zeigt auf **KoKrs/BuKrs/GsBer/Werk 0001** (SAP-Muster), nicht auf Trafag | Bild 03 |
| 3 | Im Kostenrechnungskreis **1000** war die Komponente **„Projekte“ nicht aktiv** (TKA00-PRJKT leer, nur Auftraege aktiv). Folge: KB21N und CAT2 boten gar kein Feld PSP-Element an | Bild 08, 10 |
| 4 | CATS-Profile nur `SAP` („Beratung SAP“, schlaegt Adils Personalnummer vor) und `HR-ONLY`; im Profil `SAP` sind die Empfaengerspalten gesperrt, Projektzeit ist darin nicht erfassbar | Bild 06, 07 |
| 5 | Personalstamm ist in SAP vorhanden (PA0001), Ingo = Personalnummer 3216, Kostenstelle 230 | SapProbe |
| 6 | Kostenstelle 230 hat keinen Stammsatz fuer Leistungsart 100 im Jahr 2026; Kostenstellen 100, 200, 240 ff. haben einen (CSSL) | KB21N-Meldung |
| 7 | TPG ProjectPowerPack ist nicht im SAP installiert (kein Namensraum `/TPG*`, keine Transaktion) | SapProbe |

## 3a. Stand P76 (nur lesend, 2026-10-20, `.tmp_sap_probe/P76Pruefung_PS.ps1`)

| Punkt | P76 |
| --- | --- |
| Komponente „Projekte“ im KoKrs 1000 (TKA00-PRJKT) | **aus** (nur Auftraege aktiv) |
| Projektprofile | nur `0000001`, `0000002` (SAP-Standard) |
| Projekte (PROJ) / PSP-Elemente (PRPS) | **keine** |
| CATS-Profile / Zeitsaetze CATSDB seit 01.01.2026 | `SAP`, `HR-ONLY` / **keine** |
| Innenauftraege (AUTYP 01) seit 2025 | 44 neue (Z015 13, Z003 11, Z012 8, Z002 5, Z011 4, Z014 3); Auftrag 100362 aus Folie 10 ist Z011 „Neu 87x9 DW“ |
| Kostenstellen mit Leistungsart 2026 (CSSL) | 394 mit LstArt 100, 394 mit 200 |

Fazit: Projekte laufen in P76 heute als Innenauftraege; PS ist dort genauso wenig eingerichtet wie in T76.

## 4. Durchgespielt (Demo in T76/100)

| Schritt | Transaktion | Ergebnis | Bild |
| --- | --- | --- | --- |
| Project Builder ansehen | CJ20N | Einstieg, Objekte (Projektdefinition, PSP, Netzplan, Meilenstein ...) | 01, 02 |
| Projekt anlegen | CJ01 | `DEMO-PS` „Demo PS Einfuehrung (Test)“, Profil 0000001, Org-Daten **von Hand** auf KoKrs 1000, BuKrs 1100, Werk 1100, CHF, 01.10. bis 30.12.2026 | 03 |
| Teilprojekte | CJ01 | `DEMO-PS` Gesamtprojekt (Stufe 1), `DEMO-PS.1` Teilprojekt 1 Entwicklung, `DEMO-PS.2` Teilprojekt 2 Test und Zulassung (Stufe 2, Plan + Kont) | 04 |
| Freigeben | CJ02 | Bearbeiten > Status > Freigeben, gesichert | 05 |
| Anzeigen | CJ20N | Projekt im Project Builder | 09 |
| Zeit im Arbeitszeitblatt | CAT2 | Profil `SAP`, Personalnummer 3216, KW 43: Projektspalten gesperrt, **nicht buchbar** | 06, 07 |
| Leistung aufs Projekt (ohne PS-Komponente) | KB21N | kein Feld PSP-Element | 08 |
| **Customizing (Wunsch Ingo, nur Test)** | OKKP | KoKrs 1000, ab Geschaeftsjahr 2002: Haken **„Projekte“** gesetzt. Eigener **lokaler** Auftrag `T76K912750` „DEMO PS T76 Projekte aktiv - NICHT FREIGEBEN“, **ohne Zielsystem** (SAP: „Lokaler Auftrag wird nicht transportiert“) | 10, 11 |
| Projektzeit buchen | KB21N | Sender Kostenstelle **240**, Leistungsart 100 „Personenstunden“, Empfaenger `DEMO-PS.1`, 6 h, Tarif 66 CHF/h = **396 CHF**, Kostenart 90000, CO-Beleg **311258907** | 12 |
| Istkosten am Projekt | CJI3 | DB-Profil `000000000001`, Projekt DEMO-PS: 396 CHF auf `DEMO-PS.1` | 13, 14 |

KB21N ist dieselbe Buchung, die CATS im Hintergrund erzeugt (CAT2 erfassen, CAT7 in die Kostenrechnung uebernehmen):
Stunden x Tarif der Senderkostenstelle entlasten die Kostenstelle und belasten das Teilprojekt.

**Externe Kosten (durchgespielt):**

| Schritt | Transaktion | Ergebnis | Bild |
| --- | --- | --- | --- |
| Bestellung mit Kontierung P | ME21N | Lieferant 66228, EKORG 1100, EKGRP 110, Position KontTyp P, 1 LE, 1'500 CHF, Warengruppe 01, Werk 1100, Sachkonto 45011, PSP `DEMO-PS.2`, ohne WE. SAP meldete „Normalbestellung 45148479 angelegt“, **die Verbuchung brach aber ab** (SM13: nur `RV_MESSAGE_UPDATE` = Nachrichtenausgabe, alle Bestell- und Kontierungsbausteine „Init“). Bestellung existiert nicht; T76-Problem der Nachrichtenfindung, nicht PS | 15 |
| Rechnung ohne Bestellung | FB60 | Lieferant 66228, 1'500 CHF, V0, Zeile Sachkonto 45011 mit PSP `DEMO-PS.2` (Spalte PROJK), FI-Beleg **19102004** | (16, nicht in Folien: zeigt Bankverbindung) |
| Projekt-Istkosten | CJI3 | `DEMO-PS.1` 396 CHF (90000) + `DEMO-PS.2` 1'500 CHF (45011) = **1'896 CHF** | 17 |

Das Feld PSP-Element ist in der FB60-Zeile fuer Konto 45011 erlaubt (Feldstatus passt).

## 5. Transaktionen

| Zweck | Transaktion |
| --- | --- |
| Projekt anlegen/aendern/anzeigen | **CJ20N** Project Builder; klassisch CJ01/CJ02/CJ03 |
| Kostenplan, Budget | CJ40, CJ30 |
| Zeit erfassen | **CAT2** Arbeitszeitblatt, CATS_APPR Genehmigung, **CAT7** Uebernahme in die Kostenrechnung |
| Leistung direkt verrechnen | KB21N |
| Externe Kosten | ME21N (Kontierung P), MIGO, MIRO; FB60 ohne Bestellung |
| Auswertung | **CJI3** Ist-Einzelposten, S_ALR_87013558 Budget/Ist/Obligo, CN41N Strukturuebersicht |
| Abschluss | CJ88 Abrechnung |
| Customizing | OKKP (Komponente Projekte), OPSA (Projektprofil), CAC1 (CATS-Profil), KP26 (Tarife) |

## 6. Was fuer eine Einfuehrung noetig waere

1. Komponente „Projekte“ im Kostenrechnungskreis 1000 aktivieren (in T76 nur lokal getestet).
2. Eigenes Trafag-Projektprofil (OPSA) mit KoKrs 1000, BuKrs 1100, Werk, Waehrung CHF, Abrechnungsprofil, Statusschema;
   Editiermaske fuer Projektnummern.
3. CATS-Erfassungsprofil mit Feld PSP-Element (CAC1), Genehmigung, Uebernahme CAT7 als Job.
4. Leistungsarten und Tarife je Kostenstelle der Projektmitarbeitenden (KP26), heute fehlt zum Beispiel 230/100.
5. Feldstatus der Aufwandskonten fuer externe Kosten mit PSP-Element.
6. Berechtigungsrollen (Projektleiter, Erfassende, Controlling).
7. Entscheid Zeiterfassung: rexx bleibt fuehrend mit Schnittstelle nach SAP, oder Erfassung direkt in SAP (CAT2/Fiori).
8. Anbindung TPG ProjectPowerPack an SAP (Stufe 2/3): mit Philip Steiger und TPG klaeren.

## 6a. Folien

In `20260223 - Abgleich TPG - Trafag (mit SAP PS).pptx` (Repo-Wurzel, nicht versioniert; Original unveraendert daneben) stehen
nach Folie 10 „Stufe 2/3 - Anbindung SAP Zeitrueckmeldung/Finanzdaten“ sechs neue Folien 11 bis 16: Alternative zu
Innenauftraegen (PS-Struktur und Kostenfluesse), Demo Projekt anlegen, Demo Projektzeit buchen (KB21N -> CJI3), Demo externe
Kosten (ME21N Kontierung P, FB60 -> CJI3), Fazit mit P76-Befund, Transaktionen. Gebaut per PowerPoint-COM im Stil der Vorlage (Arial Narrow, Trafag-Orange), Skript im Scratchpad.

## 7. Aufraeumen in T76

- FI-Beleg 19102004 (Kreditorenrechnung 1'500 CHF auf `DEMO-PS.2`) bleibt als Demo stehen; zurueck mit FB08 stornieren.
- SM13 enthaelt den abgebrochenen Verbuchungssatz der Bestellung (20.10. 11:06:30, ME21N); nicht nachbuchen, bei Bedarf loeschen.
- Projekt `DEMO-PS` (Status FREI) und CO-Beleg 311258907 bleiben als Demo stehen. Zurueck: Beleg mit KB24N stornieren,
  Projekt auf „technisch abgeschlossen“ und mit Loeschvormerkung versehen.
- Customizing: Haken „Projekte“ in OKKP fuer KoKrs 1000 ist in T76 gesetzt. **Auftrag `T76K912750` ist lokal und darf
  nicht freigegeben werden**; zum Zuruecknehmen in OKKP den Haken entfernen (geht nur, solange keine Projektkosten mehr
  stehen, also erst nach Storno).
