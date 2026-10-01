# RAG HR KPI

Stand: 2026-09-30

## Kurzstand

- **2026-09-30: Arbeitstag 8,0 h statt 8,4 h** (Entscheid Ingo, deckt sich mit Sonjas Tagessoll
  8 h bei 100 %). `e3545c4`, eine Konstante `HoursPerWorkday` fuer Krankheitstage, 61-Tage-Grenze
  und FTE aus Sollzeit; rund 5 % mehr Krankheitstage. **Produktiv seit 2026-09-30 09:36** (`docs/rag/DEPLOYMENT.md`).
  Ueberarbeitete Mail an Sonja liegt als `.eml` in Downloads. Details `docs/HR_KPI.md` 8.4.
- **2026-09-30: automatischer SAP-Import gebaut, NICHT produktiv.** EntitySet `HrKpiSet` in
  `ZPOWERBI_EINKAUF_SRV` rechnet live aus PA0001/0002/0007/2001 (nur Cockpit-Felder, keine Namen,
  kein Lohn), in T76 aktiv und im Gateway Client mit 200 geprueft, Transport `T76K912530` gemeinsam
  mit dem Journal. Cockpit holt taeglich ab 05:00 ab und schreibt `hrdata/HR_KPI_Export.xlsx`
  (`4051506`, 795/795). Seit 01.10. in P76 (200) und produktiv, erster Abruf 02.10. ab 05:00. Wertestichprobe 01.10. bestanden. Abruf holt bewusst den laufenden Monat (Entscheid 01.10.); Unfalltage damit nur Monat bis heute. `docs/HR_KPI.md` 8.6.
- **2026-09-30 HR-Sitzung:** Kader 8,1 h, normale Mitarbeitende 8,0 h (Kader = Rexx `Leitung j/n` = ja, `c51445c`, produktiv seit 01.10. 09:12); Rexx
  erfasst Absenzen je Fall mit Von/Bis und Status (neuer Bericht noetig); Absenz-Ampel bis 4,99 %
  gelb, ab 5 % rot bestaetigt; Uebersichtskachel in `81140fa` auf „ab 5 %“ angeglichen (produktiv seit 01.10.). SAP-Datei `HR_KPI_Export` ist NICHT automatisiert (Report
  `Z_HR_KPI_CONS` von Hand), Ziel taeglich. Details `docs/HR_KPI.md` 8.5 und 8.6.
- **2026-09-30: kein Rexx-Hub.** Automatischer Export der vier Rexx-Berichte soll ueber Upgreat
  (Ueli Kunzmann) laufen, Mail am 30.09. versendet, Antwort offen. dormakaba nach Rexx laeuft schon
  seit 02.09. alle 15 Minuten. Details `docs/HR_KPI.md` 8.3.

- **2026-09-29, Mailhistorie gesichtet:** offene Wuensche von Sonja vom 2026-02-03 (Ferien am Stueck,
  Fit-&-Wohl nach 6. Absenz oder 12 Krankheitstagen, Uebertrag Restferien) und die spaeteren KPIs
  (Stellenplan, Umfragen, Kununu, Time to hire, Lohnkosten) sind nicht umgesetzt. Rexx-Hub fuer den
  automatischen Export ist seit 2026-07-29 von Nadja beauftragt (4'800 EUR p. a.), Stand offen. Mail
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
