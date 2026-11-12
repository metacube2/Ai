# RAG HR KPI

Stand: 2026-11-12 (Doku-Pruefung: der vorbereitete Rexx-Hub-Abruf ist seit dem Deploy vom 2026-11-12 11:33 auf dem Server, `e956183`, Schalter `HrKpi:RexxHub:Enabled` weiter aus; Kurzstand-Punkt Rexx Hub ergaenzt, ueberholte Rexx-Aussagen markiert); davor 2026-11-10 (Rexx-Hub-Abruf gebaut, Schalter `HrKpi:RexxHub:Enabled` aus, ~~nicht deployed~~ *(ueberholt am 2026-11-12: deployed 11:33, siehe vorn)*, `docs/HR_KPI.md` 8.18; Entscheid Ingo: Rexx Hub wird genommen, Lieferfelder, 8.17); davor 2026-11-04 12:41 (Unfallkachel aus SAP- und Rexx-Faellen, `f86b0ed`); davor 12:21 (Rexx-Abwesenheiten je Fall #777 produktiv, `4bdbc18`); davor 11:46 (Rexx-Gesamtabfrage #775, `6dd1a56`); davor 09:30 (Formelpruefung, drei Korrekturen, Reiter Interaktiv, produktiv; Rexx-Dateien vom 04.11.); davor 2026-11-03 (Rexx-Schnittstelle nach SAP), 2026-10-29 (Abschnittsnummer Kader 8.8), 2026-10-22 (Kader Mitarbeiterkreis 10), 2026-10-20

## Kurzstand

- **2026-11-10/12: Rexx Hub.** Entscheid Ingo am 10.11.: Der Rexx Hub wird genommen (`docs/HR_KPI.md` 8.17). Der Tagesabruf
  der Rexx-Abfragen #775 und #777 ueber den Hub ist gebaut (`1e99745`, `d99fbe6`) und seit dem Deploy vom 2026-11-12 11:33
  (`e956183`, `docs/rag/DEPLOYMENT.md`) auf dem Server, aber **ausgeschaltet** (Schalter `HrKpi:RexxHub:Enabled` aus); ueber den
  Hub wird also noch nichts abgerufen. Laut Mailverlauf AG36071 ist der Hub noch nicht bestellt
  (`saptasks/rexx_schnittstelle/NOTES.md` 13); bis dahin gilt weiter der Handexport von #775 und #777. Vor dem Umschalten zu
  klaeren: `docs/HR_KPI.md` 8.18, Umschaltanleitung in `saptasks/rexx_schnittstelle/NOTES.md` 14. *(Eingefuegt bei der
  Doku-Pruefung am 2026-11-12; vorher stand der Stand nur in der Kopfzeile.)*

- **2026-11-04 13:00: Stand nach Ampel und fuenf Fragen an HR** (`docs/HR_KPI.md` 8.16), Mailentwurf in Ingos Downloads,
  nicht gesendet. Antworten von HR offen.

- **2026-11-04 12:41: Kachel „Unfalltage laufender Monat“** nimmt den hoeheren Wert aus SAP-Tagesdatei und Unfallfaellen
  (SAP, Rexx #777, aus #775 ergaenzt); vorher 0,0, jetzt 4,2 Tage im November (`docs/HR_KPI.md` 8.15). Produktiv `f86b0ed`.

- **2026-11-04 12:21: Rexx-Abwesenheiten je Fall #777** (`docs/HR_KPI.md` 8.14). Private Abfrage #777 in der Rexx-Antragsliste
  (Von/Bis, Art, Status, Tagessoll je Antrag, ab 07.04.2026), Export `Hr-cockpit-absenzen-je-fall.xlsx` in `hrdata`. Bestaetigte
  Faelle, die in SAP fehlen, kommen mit Datum dazu (1'203: Ferien 730, Kompensation 355, Krankheit 87, Unfall 24, Militaer 7).
  Damit Krankheitskalender, Fit & Wohl (31) und „Ferien am Stueck“ (101 von 260) aktuell. Rest ohne Datum: rund 100
  Krankheitstage bei 13 Personen, von HR direkt in Rexx gebucht und nie in SAP (eine Person 49,5 Tage); erst die
  Rexx-Schnittstelle loest das. **Produktiv seit 12:21 (`4bdbc18`), in Chrome geprueft.**

- **2026-11-04 11:46: Rexx-Gesamtabfrage #775 statt vier Rexx-Dateien** (`docs/HR_KPI.md` 8.13). Private Rexx-Abfrage #775
  „HR-Cockpit Gesamt (Dashboard)“ (50 Spalten, voll), Export `Hrcockpitgesamtdashboard.xlsx` in `hrdata` (seit 11:46, Stand
  04.11.); liegt sie dort, liest das Cockpit nur sie, Schluessel Personalnummer. Fuer 01.01. bis Exporttag je Person der
  hoehere Wert aus SAP und Rexx (Krankheit, Unfall, Ferien, Kompensation, Militaer), weil SAP nur ueber Sonjas Import
  nachkommt. Echte Daten 2026: Unfall 56 → 131,9 Tage, Quote 2,5 → 3,0 % (gelb), Ferien bezogen 3'287,5 → 4'666 (alte
  Formel zog geplante Tage doppelt ab). Pflege: #775 mit Zeitraum 01.01. bis heute exportieren, Hinweis nach 7 Tagen.
  Vorher um 11:23 behoben: der Saldi-Export vom Morgen hatte Stichtag 31.12.2024 (Ferien Rest 510 statt 1'579,5).
  **Produktiv seit 11:46 (`6dd1a56`), in Chrome geprueft** (Uebersicht, Hinweise, Interaktiv mit Krankheitskalender).

- **2026-11-04: HR-Vorgaben aus den Mails umgesetzt** (`docs/HR_KPI.md` 8.12): Kurz/Lang je Krankheit ab dem 61. Tag,
  Absenzquote = Krankheit und Unfall (0290), Kachel „Quote ohne Langzeit“, Gelb auf der Uebersichtskachel, Vorjahresvergleich
  gleicher Zeitraum, Von/Bis-Kachel mit Spanne. Echte Daten 2026: Quote 2,5 %, ohne Langzeit 2,2 %. Offen fuer HR:
  Kontrollwerte 2025 passen nicht zum Export (2025 nur 6 „Kündigung AN“), Befristete im Headcount, 0210/0220/0280, Ferienformel *(geklaert 8.13)*.
  **Produktiv seit 10:21 (`69a63b6`).**

- **2026-11-04 09:30: Rexx-Dateien in `hrdata` aktuell** (Ingos Exporte vom 04.11., alte vom 08.07. in
  `hrdata\archiv_rexx_2026-07-08`; Austritte 30.11.2023 bis 09.10.2026). Ab naechster Woche soll die Rexx-Schnittstelle
  liefern. *(Ueberholt, Stand 2026-11-10: Das ist nicht eingetreten; am 10.11. fiel erst der Entscheid fuer den Rexx Hub, die
  Lieferung ist noch nicht eingerichtet, `docs/HR_KPI.md` 8.17.)* *Danach (8.11, Auftrag Ingo „das Aktuellere nehmen“):* „Vertrag endend“/„Langzeitkrank“ zaehlen als angestellt,
  Sollzeit-Spalte „Ø tägliche …“ erkannt, juengste Rexx-Krankmeldungen ergaenzen SAP (am 04.11. 34 Faelle), Ferien/
  Kompensation/Militaer zeigen den SAP-Stand (27.07.) mit Warnung. SAP-Ferien kommen nur mit Sonjas Handimport.
  **Produktiv seit 09:57 (`cfe2631`).** *Sichtpruefung um 11:50 erledigt (siehe #775 oben).*
- **2026-11-04: Formeln geprueft, drei Rechenfehler behoben, neuer Reiter „Interaktiv“** (*produktiv seit 09:20, `b65f059`;
  ueberholt: „nicht deployed“*). Krankenquote-Nenner je Person erst ab Eintritt, Quote je Person mit Pensum, Prognose rechnet ein
  laufendes Quartal vorher auf die volle Laenge hoch (vorher am 04.11. rund Faktor 2,6 zu tief). Offen fuer Entscheid:
  61-Tage-Regel je Fall statt Summe im Filterzeitraum (SAP 0260/0270), Vorjahresvergleich im laufenden Jahr, Beschriftung
  der Uebersichtskachel bei Von/Bis. Reiter Interaktiv mit acht animierten Ansichten (Kuendigungen im Zeitraffer,
  Austrittsfluss, Abteilungs-Galaxie, Bindungskurve, Krankheitskalender, Fit & Wohl, Ferien am Stueck, Was waere wenn);
  setzt die HR-Wuensche Fit & Wohl und Ferien am Stueck aus Sonjas Mail vom 03.02. als Ansicht um. `docs/HR_KPI.md` 8.10 und 9.

- **2026-11-03: Rexx-Schnittstelle nach SAP gefunden (T76 lesend).** Eigenentwicklung der EXPRIS AG
  (Paket `Z_HR_PA_REXX`): Sonja Richter (`RIS`) importiert von Hand Stammdaten (`Z_HR_PA_REXX`),
  Absenzen (`Z_HR_REXX_PA_ABS`, Batch-Input PA30) und Kommen/Gehen (`Z_HR_REXX_PA_ZE`); Dateien vom
  PC oder aus `/usr/sap/rexx`, kein Job in T76. **Krankheit/Unfall nimmt der Absenz-Import nicht mit**;
  **Ferien, Kompensation und Militaer kommen nur mit Sonjas Import nach SAP**, `HrAbsenzSet` hinkt
  dafuer nach (Aussage „P76 immer aktuell“ ueberholt). Ferienkonten in PA2006, Austrittsart nur
  teilweise in SAP. Idee Ingo: eigenes Programm nur in Z-Tabellen, taeglicher Job; nicht gebaut,
  haengt an der Dateilieferung. Mails (03.11. gelesen): Upgreat schloss Ticket #12699584 am 19.10. mit „Unable to solve“, Rexx betreut
  Trafag selbst (HR/Nadja); Hub-Angebot AG36071 4'800 EUR/Jahr gegen 4 Einzelschnittstellen 5'600 EUR/Jahr, *„Entscheid offen“ ueberholt am 2026-11-10:* **Ingo: wir nehmen den Rexx Hub**, Ingo migriert die bestehenden Schnittstellen, der Stempeluhrtransfer bleibt; Felder, die Rexx liefern muss, in `saptasks/rexx_schnittstelle/NOTES.md` 12; *Nachtrag 03.11. nachmittags: Sitzung Rexx Hub naechste Woche mit Alex und Artjom Yahno, Nadja und Sonja laut Alex nicht noetig*; Sonja: HR
  konnte noch nichts testen. `docs/HR_KPI.md` 8.9, `saptasks/rexx_schnittstelle/NOTES.md`.
- **2026-10-20 live geprueft: SAP-Abruf laeuft taeglich.** In `hrdata` auf dem Server sind `HR_KPI_Export.xlsx` und
  `HR_Absenzen_SAP.xlsx` heute um 05:07 geschrieben. Die Rexx-Dateien sind weiter vom 08.07. (Upgreat offen), wer danach
  eingetreten ist, fehlt in der Personenbasis. *(Ueberholt: Upgreat hat das Ticket am 19.10. geschlossen; die Rexx-Dateien
  sind am 2026-11-04 ersetzt worden, seither liest das Cockpit #775 und #777, siehe oben.)*

- **2026-09-30: Arbeitstag 8,0 h statt 8,4 h** (Entscheid Ingo, deckt sich mit Sonjas Tagessoll
  8 h bei 100 %). `e3545c4`, eine Konstante `HoursPerWorkday` fuer Krankheitstage, 61-Tage-Grenze
  und FTE aus Sollzeit; rund 5 % mehr Krankheitstage. **Produktiv seit 2026-09-30 09:36** (`docs/rag/DEPLOYMENT.md`).
  Ueberarbeitete Mail an Sonja liegt als `.eml` in Downloads. Details `docs/HR_KPI.md` 8.4.
- **2026-09-30: automatischer SAP-Import gebaut** (*ueberholt „NICHT produktiv“: seit 01.10. produktiv, siehe unten*). EntitySet `HrKpiSet` in
  `ZPOWERBI_EINKAUF_SRV` rechnet live aus PA0001/0002/0007/2001 (nur Cockpit-Felder, keine Namen,
  kein Lohn), in T76 aktiv und im Gateway Client mit 200 geprueft, Transport `T76K912530` gemeinsam
  mit dem Journal. Cockpit holt taeglich ab 05:00 ab und schreibt `hrdata/HR_KPI_Export.xlsx`
  (`4051506`, 795/795). Seit 01.10. in P76 (200) und produktiv, erster Abruf 02.10. ab 05:00. Wertestichprobe 01.10. bestanden. Abruf holt bewusst den laufenden Monat (Entscheid 01.10.); Unfalltage-Kacheln deshalb „laufender Monat“ (`931f151`). Erster Abruf 01.10. 09:46: 1'168 Personen. **Seit 01.10. 10:50 produktiv: Krankheit und Ferien je Fall aus SAP `PA2001`** ueber `HrAbsenzSet` (P76 importiert, `T76K912644`) und `hrdata/HR_Absenzen_SAP.xlsx` (erster Abruf 10:52, 4'948 Faelle); ohne Datei weiter Rexx. Personenbasis noch Rexx vom 08.07. Krankheitsarten 0220/0230/0240/0260/0270, von HR zu bestaetigen; mit SAP-Datei ohne Zeitraumfilter laufendes Jahr, vor dem Vorjahr Rueckfall auf Rexx (`c12b34a`, `HR_KPI.md` 8.7). `docs/HR_KPI.md` 8.6.
- **2026-10-22: Kader = SAP-Mitarbeiterkreis 10 „ML mit Kadervertrag“, 8,2 h** (Ingo; vorher Rexx „Leitung j/n“ und 8,1 h). `ede56b6`, produktiv seit 2026-10-22 15:48. `docs/HR_KPI.md` 8.8.
- **2026-09-30 HR-Sitzung:** *(Kaderregel ueberholt am 2026-10-22)* Kader 8,1 h, normale Mitarbeitende 8,0 h (Kader = Rexx `Leitung j/n` = ja, `c51445c`, produktiv seit 01.10. 09:12); Rexx
  erfasst Absenzen je Fall mit Von/Bis und Status (neuer Bericht noetig); Absenz-Ampel bis 4,99 %
  gelb, ab 5 % rot bestaetigt; Uebersichtskachel in `81140fa` auf „ab 5 %“ angeglichen (produktiv seit 01.10.). SAP-Datei `HR_KPI_Export` war damals NICHT automatisiert (Report
  `Z_HR_KPI_CONS` von Hand); *ueberholt: seit 01.10. taeglich ueber `HrKpiSet`*. Details `docs/HR_KPI.md` 8.5 und 8.6.
- *Ueberholt am 2026-11-10 (Rexx Hub wird genommen):* **2026-09-30: kein Rexx-Hub.** Automatischer Export der vier Rexx-Berichte soll ueber Upgreat
  (Ueli Kunzmann) laufen, Mail am 30.09. versendet, Antwort offen *(ueberholt am 2026-11-03: daraus Upgreat-Ticket #12699584, am 19.10. geschlossen; am 29.10. Angebot AG36071 Rexx Hub; Inhalt ungelesen)*. dormakaba nach Rexx laeuft schon
  seit 02.09. alle 15 Minuten. Details `docs/HR_KPI.md` 8.3.

- **2026-09-29, Mailhistorie gesichtet:** offene Wuensche von Sonja vom 2026-02-03 (Ferien am Stueck,
  Fit-&-Wohl nach 6. Absenz oder 12 Krankheitstagen, Uebertrag Restferien) und die spaeteren KPIs
  (Stellenplan, Umfragen, Kununu, Time to hire, Lohnkosten) sind nicht umgesetzt *(teilweise ueberholt am 2026-11-04:
  Ferien am Stueck und Fit-&-Wohl-Ausloeser zeigt der Reiter Interaktiv; Ruecklauf und Uebertrag offen)*. Rexx-Hub fuer den
  automatischen Export ist seit 2026-07-29 von Nadja beauftragt (4'800 EUR p. a.), Stand offen *(ueberholt am 2026-09-30:
  kein Rexx-Hub, Export ueber Upgreat, siehe oben; das wiederum ueberholt am 2026-11-10: der Rexx Hub wird genommen, ob Nadjas
  Auftrag noch besteht, ist zu klaeren, `docs/HR_KPI.md` 8.17; Nachtrag 2026-11-10 mittags: laut Mailverlauf AG36071 ist der
  Hub nicht bestellt, Nadjas Go wartet auf die IT-Pruefung durch Alex, `saptasks/rexx_schnittstelle/NOTES.md` 13)*. Mail
  an Sonja liegt als Entwurf, Punkt 2 fehlt. Details: `docs/HR_KPI.md` Abschnitt 8.3.

- **2026-09-29: Review-Reparaturen von Codex, produktiv seit 10:07** (`eafd1c5`, Uebersetzungen
  `c1fdfa2`, 747/747 Tests). Behoben im Arbeitsbaum: Von/Bis hat auch fuer Berechnungsjahr
  und Vergleichskacheln Vorrang vor dem Jahresfeld (H1); Monat/Quartal/YTD mit vollstaendigen
  Kalenderperioden, Headcount auch mit vor dem Von-Datum Ausgeschiedenen (H2); die Personenquote
  zeigt nicht mehr scheinbar belastbare Werte (H3); ein bewusst geleertes Jahr bleibt leer (H4);
  Tabellen nicht mehr auf 100/250 Zeilen gekappt, der PDF-Druck nennt seinen Seitenumfang (H5).
  Die Managementsicht verbirgt Namen, der Personenbezug bleibt; der Hilfetext sagt das jetzt
  richtig. `138/138` gezielte Tests laut Codex. Bericht: `docs/HR_EINKAUF_REVIEW_2026-09-29.md`.
  Offen bleiben die bekannten Fachfragen (8,4 h je Krankheitstag — seit 30.09. entschieden: 8,0 h —, periodengenaue Rexx-Absenzen,
  alte Quelldateien), ohne neue Aufgaben.

- **2026-09-28: fachliche Antworten von HR (Sonja Richter) umgesetzt, produktiv seit 11:25 (`5ae7f31`).**
  Langzeitkrank ab dem 61. Krankheitstag (Summe je Person, beide Rexx-Felder zaehlen als
  Krankheit), Reminderprofile ohne SAP-FTE und ohne Rexx-Sollzeit ausgeschlossen,
  Restferien-Ampel Q1 bis 5 Tage gruen und ab Q2 jeder Resttag rot, zusaetzlich
  „Fluktuation Prognose gleitend" (12 Monate) und „Fluktuation Vorjahr". GLZ-Schwellen,
  Fluktuationsdefinition und Ausschluesse von HR bestaetigt. Periodengenaue Krankenquote
  weiterhin nur mit einem auf den Zeitraum eingegrenzten Rexx-Export. Die Dateien in `hrdata`
  sind vom 08.07. (Rexx) und 26.05. (SAP). Details: `docs/HR_KPI.md` Abschnitt 8. Ein Mail-Entwurf an
  Sonja mit dem Umsetzungsstand und den zwei Call-Themen liegt vor (28.09., Versand durch Ingo).

- Produktiv deployed und verifiziert am 2026-08-06 14:24 MESZ, Commit
  `9435a5d`, Gesamtsuite `438/438` gruen: Die Krankenquote zieht bei den Arbeitstagen die neun
  gesetzlichen Feiertage des Kantons Zuerich ab. Ein automatischer
  Filtervertrag prueft 128 Kombinationen der sieben personenbezogenen Filter
  ueber alle sichtbaren HR-Ergebnisbloecke sowie eine Vollkombination mit
  Zeitraum und Fluktuation. Die Uebersicht zeigt bei nicht datierbaren
  Rexx-Absenzen ebenfalls keine scheinbar genaue Quote mehr. Details:
  `docs/HR_KPI.md`.
- HR KPI Cockpit wurde um produktive Cockpit-Funktionen erweitert.
- Enthalten sind Anleitung, Datenordner, Dateifrische, Datenstatus, Ampeln, Periodenvergleich, Datenqualitaet, Austritte, Absenzen, Managementsicht und Drucken/PDF.
- Managementsicht blendet Personennamen in Detailtabellen aus; sie ist keine anonyme Aggregatsicht, der Personenbezug bleibt (praezisiert 2026-09-29).
- HR KPI Zugang unterstuetzt zusaetzliche Admin-User ueber `HrKpiAccess.AdminUsers`.
- Alter HR-User `hr` wurde nicht geaendert.
- Aktueller Zusatzuser: `hradmin`; Passwort wurde separat kommuniziert, im Repository liegt nur der Hash in `appsettings.json`.
- Deployed 2026-07-01: Fluktuations-Kacheln sind fachlich klarer beschriftet, thematisch farbig hinterlegt und haben Hover-Texte mit Formel und genauer Bedeutung.
- Wichtigste YTD-Kachel: `Fluktuation YTD` = fluktuationsrelevante Austritte vom 01.01. des gewaehlten Jahres bis Stichtag / durchschnittlicher Headcount im gleichen Zeitraum. Bei vergangenen Jahren ist der Stichtag 31.12.; beim laufenden Jahr heutiger Tag bzw. gewaehlter Bis-Stichtag.
- Farblogik Fluktuations-Kacheln: Headcount/Basis blau, Austritte gelb, fluktuationsrelevante Austritte gruen, nicht relevante/ausgeschlossene Austritte grau, Fluktuationsraten rot, Prognose violett.
- Validierung/Deploy: Commit `874a61c Add HR turnover metric tooltips`, Tests `125/125` gruen, produktive DLL `01.07.2026 08:20:54`, Port 443 erreichbar.
- Review-Korrekturen vom 2026-07-07 sind seit den nachfolgenden produktiven
  Builds enthalten (Commit `1afac2f`): korrekter Vorjahresvergleich,
  YTD-konsistente Krankenquote/Fluktuation, aggregierte Top-Absenzen und neue
  Datenqualitaets-Hinweise. Details:
  `docs/HR_KPI.md`.

## Datenquellen

- Rexx-/SAP-Dateien aus konfiguriertem Datenordner.
- Datenordner im Cockpit je Lauf anpassbar und dauerhaft ueber `HrKpi:DataFolder`.
- Login-Logik akzeptiert den primaeren HR-User oder einen Eintrag aus `AdminUsers` und setzt den HR-Unlock-Cookie fuer den passenden User-Hash.

## Rohquellen Nur Bei Bedarf

- Nachdoku: `docs/HR_KPI.md`
- Fachpruefung: `docs/HR_KPI_PRUEFUNG_SWISS_BEST_PRACTICES.md`
- Feiertage/Filtervertrag: `docs/HR_KPI.md`
- Anwenderdoku: `docs/HR_KPI_ANLEITUNG_HR_2026-05-20.docx`
