# RAG HR KPI

Stand: 2026-06-16

## Kurzstand

- HR KPI Cockpit wurde um produktive Cockpit-Funktionen erweitert.
- Enthalten sind Anleitung, Datenordner, Dateifrische, Datenstatus, Ampeln, Periodenvergleich, Datenqualitaet, Austritte, Absenzen, Managementsicht und Drucken/PDF.
- Managementsicht anonymisiert Personennamen in Detailtabellen.
- HR KPI Zugang unterstuetzt zusaetzliche Admin-User ueber `HrKpiAccess.AdminUsers`.
- Alter HR-User `hr` wurde nicht geaendert.
- Aktueller Zusatzuser: `hradmin`; Passwort wurde separat kommuniziert, im Repository liegt nur der Hash in `appsettings.json`.

## Datenquellen

- Rexx-/SAP-Dateien aus konfiguriertem Datenordner.
- Datenordner im Cockpit je Lauf anpassbar und dauerhaft ueber `HrKpi:DataFolder`.
- Login-Logik akzeptiert den primaeren HR-User oder einen Eintrag aus `AdminUsers` und setzt den HR-Unlock-Cookie fuer den passenden User-Hash.

## Rohquellen Nur Bei Bedarf

- Nachdoku: `docs/HR_KPI_NACHDOKU_2026-05-13.md`
- Fachpruefung: `docs/HR_KPI_PRUEFUNG_SWISS_BEST_PRACTICES.md`
- Anwenderdoku: `docs/HR_KPI_ANLEITUNG_HR_2026-05-20.docx`
