# Archiv: verschobene Historie, kein Sollstand

Stand: 2026-10-22 (angelegt). Entscheid Ingo 2026-10-22: neue Archive kommen hierher, das am
2026-09-01 in `835b317` geloeschte `docs/raw_md_archive/` wird nicht wiederhergestellt.

Alles in diesem Ordner ist **unveraendert** aus einer Arbeitsdatei herausgeschnitten worden, damit
die Arbeitsdatei kurz bleibt. Nichts hier beantwortet eine Statusfrage (`router.md` Regel 1 und 2);
jeder Eintrag belegt den Stand seines Datums.

| Datei | Herkunft | Zeitraum |
| --- | --- | --- |
| `LASTCHANGE_ARCHIV_2026-08-01_bis_2026-09-30.md` | `lastchange.md`, verschoben 2026-10-22 | Abschnitte 2026-07-30 bis 2026-09-30, inkl. alte Listen „Offene Punkte (nicht erledigt)“ und „Aktueller Kurzstand“ |
| `DEPLOYMENT_KURZSTAND_ARCHIV_bis_2026-10-12.md` | `docs/rag/DEPLOYMENT.md` Abschnitt Kurzstand, verschoben 2026-10-22 | Deploys, Datenkorrekturen, Nachweise 2026-08-03 bis 2026-10-09 |

Die Kurzindex-Zeilen der Agentenkoordination wandern nicht hierher, sondern in
`docs/AGENT_COORDINATION_HISTORIE.md` (letzter Abschnitt).

## Nur noch in Git: `docs/raw_md_archive/`

Diese fuenf Dateien und das ZIP wurden am 2026-09-01 im Commit `835b317` geloescht (vermutlich
versehentlich, Befund 2026-09-29). Abruf: `git show 835b317^:TrafagSalesExporter/docs/raw_md_archive/<Datei>`.

| Datei | Inhalt |
| --- | --- |
| `LASTCHANGE_ARCHIV_bis_2026-07-12.md` | Aenderungsstand 2026-06-04 bis 2026-07-12 und Nachtraege Mai/Juni 2026 |
| `LASTCHANGE_ARCHIV_2026-07-13_bis_2026-07-30.md` | Aenderungsstand 2026-07-13 bis 2026-07-30 |
| `RAG_ROUTER_ARCHIV_2026-07-31.md` | Routerstand vor der Aufteilung in `router.md` und `docs/router/` |
| `RAG_KURZDATEIEN_ARCHIV_ueberholte_eintraege.md` | aus den RAG-Kurzdateien entfernte Eintraege |
| `HISTORY_CANONICAL.md.raw` | kanonische Detailhistorie, alter LLM-Systemkontext und Finance-Handoff |
| `original_history_raws.zip` | Original-Volltexte, nur zur Wiederherstellung |
