# RAG HR KPI

Stand: 2026-07-01

## Kurzstand

- HR KPI Cockpit wurde um produktive Cockpit-Funktionen erweitert.
- Enthalten sind Anleitung, Datenordner, Dateifrische, Datenstatus, Ampeln, Periodenvergleich, Datenqualitaet, Austritte, Absenzen, Managementsicht und Drucken/PDF.
- Managementsicht anonymisiert Personennamen in Detailtabellen.
- HR KPI Zugang unterstuetzt zusaetzliche Admin-User ueber `HrKpiAccess.AdminUsers`.
- Alter HR-User `hr` wurde nicht geaendert.
- Aktueller Zusatzuser: `hradmin`; Passwort wurde separat kommuniziert, im Repository liegt nur der Hash in `appsettings.json`.
- Deployed 2026-07-01: Fluktuations-Kacheln sind fachlich klarer beschriftet, thematisch farbig hinterlegt und haben Hover-Texte mit Formel und genauer Bedeutung.
- Wichtigste YTD-Kachel: `Fluktuation YTD` = fluktuationsrelevante Austritte vom 01.01. des gewaehlten Jahres bis Stichtag / durchschnittlicher Headcount im gleichen Zeitraum. Bei vergangenen Jahren ist der Stichtag 31.12.; beim laufenden Jahr heutiger Tag bzw. gewaehlter Bis-Stichtag.
- Farblogik Fluktuations-Kacheln: Headcount/Basis blau, Austritte gelb, fluktuationsrelevante Austritte gruen, nicht relevante/ausgeschlossene Austritte grau, Fluktuationsraten rot, Prognose violett.
- Validierung/Deploy: Commit `874a61c Add HR turnover metric tooltips`, Tests `125/125` gruen, produktive DLL `01.07.2026 08:20:54`, Port 443 erreichbar.

## Datenquellen

- Rexx-/SAP-Dateien aus konfiguriertem Datenordner.
- Datenordner im Cockpit je Lauf anpassbar und dauerhaft ueber `HrKpi:DataFolder`.
- Login-Logik akzeptiert den primaeren HR-User oder einen Eintrag aus `AdminUsers` und setzt den HR-Unlock-Cookie fuer den passenden User-Hash.

## Rohquellen Nur Bei Bedarf

- Nachdoku: `docs/HR_KPI_NACHDOKU_2026-05-13.md`
- Fachpruefung: `docs/HR_KPI_PRUEFUNG_SWISS_BEST_PRACTICES.md`
- Anwenderdoku: `docs/HR_KPI_ANLEITUNG_HR_2026-05-20.docx`
