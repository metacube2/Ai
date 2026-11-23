# Uebergabe 2026-11-23: Code-Review 2, Einkauf-Tempo, ZLO03 FIX 29

Stand: 2026-11-23 10:50. Zweck: Ingo schliesst den Chat; diese Datei sagt der naechsten Sitzung, was fertig ist, was offen ist
und womit sie beginnt. Gueltige Statusquelle bleibt `projektmanagement/Wochen_Todo.tsv` (Zeilen 159, 168, 169) und fuer Finance
der Issue-Log; diese Datei ist der Einstieg, nicht die Statusquelle.

## 1. Zuerst tun (24.11. morgens)

1. **Mitternachtslauf Einkauf pruefen.** Serverprotokoll `\\trch-webapp-bidashboard.trafagch.local\BiDashboard$\logs\stdout_*.log`
   (neueste Datei): Zeilen „Einkauf-Lauf Delta gestartet“ (00:00) und „Einkauf-Lauf Delta beendet: … Dauer: …“. Die Meldung nennt
   seit `e76df02` die Dauer je Abschnitt. Erster echter Lauf des neuen Deltas; der Nachhol-Lauf am 23.11. lief ueber 58 Minuten und
   wurde auf Wunsch Ingo abgebrochen, ohne dass seine Abschnittszeiten bekannt wurden. Wenn der Lauf wieder lange braucht: den
   langsamen Abschnitt aus der Meldung nennen, nicht raten. Fehlt der Lauf (Worker nachts tot), holt ihn der Timer nur bis 06:00
   nach; danach wartet er auf die naechste Nacht (Absicht). Ergebnis in `docs/PLATTFORM_TEMPO_2026-09-28.md` 11.5 nachtragen.
2. **ZLO03 FIX 29 im Dashboard belegen.** Materialanteil je Produktgruppe nach dem Lauf: 40731 muss von rund 106.26 auf rund
   93.04 CHF je Stueck sinken, `H10040` darf nicht mehr vorkommen. Erst damit ist der Inhalt in P76 belegt (Quelltext in P76 nicht
   lesbar). Ergebnis in `zlo03/PLAN_ZLO03_FREMDBESCHAFFUNG_2026-11-23.md` Abschnitt 8.
3. **Sonntag 29.11. 00:00** laeuft erstmals der automatische Full Load (statt Delta). Am 30.11. Dauer und Erfolg pruefen.

## 2. Was am 20. und 23.11. fertig wurde

| Thema | Stand | Nachweis |
| --- | --- | --- |
| Code-Review 2 mit Clean-Code- und Testpruefung | Dokument fertig, am Code nichts geaendert | `docs/CODE_REVIEW_2026-11-20.md` |
| Einkauf-Delta: echtes Delta, Slot 00:00, Nachholen nur bis 06:00, sonntags Full Load, Cache wirklich im Hintergrund, Hinweisbalken, Deploy-Pruefung aus dem Serverprotokoll | deployed 23.11. 07:37 (`d4e9a18`) und 08:36 (`155276e`) | `docs/PLATTFORM_TEMPO_2026-09-28.md` 11, `docs/rag/DEPLOYMENT.md` |
| ZLO03 FIX 29: Schalter `P_NOFRM` „Fremdteile nicht auflösen“, Dashboard setzt ihn | in P76 importiert 23.11. 10:30 (`T76K912940`, RC 0000) | `zlo03/PLAN_ZLO03_FREMDBESCHAFFUNG_2026-11-23.md` 7 und 8 |

## 3. Offen, mit wem

| Punkt | Wer | Wo |
| --- | --- | --- |
| **Zweig `einkauf-tempo-2011` nicht im Hauptzweig.** Produktiv ist `155276e`; `feature/supplier-overrides-trit` kennt ihn nicht, weil im Hauptordner `Services/TimerBackgroundService.cs` (und `Program.cs`) die unkommittierte Finance_All-Aenderung liegt. **Ein Deploy vom Hauptzweig nimmt Delta-Umbau, Cache-Korrektur und Hinweisbalken zurueck.** Erst Finance_All klaeren (committen, verwerfen oder beiseitelegen), dann mergen | Entscheid Ingo | Wochen_Todo 168, `docs/rag/DEPLOYMENT.md` |
| Befund `B65814` (Geraet 31318): Einkaufspreis 4.62 CHF unter der Summe der Stuecklistenteile 11.64 CHF; mit FIX 29 faellt die Stueckliste weg. Beistellung ohne Kennzeichen Lohnbearbeitung oder veraltete Stueckliste? | Ingo mit Einkauf | Plan FIX 29, Abschnitt 7 |
| Armin vorwarnen: Materialanteil sinkt ab 24.11. (40731 rund 12 %) | Ingo | dito |
| PCBA-Kopie `Z_ZLO03_PCBA`: Schalter dort sperren, solange `P_PCBA = X` (E5) | beim Bau der Kopie | `zlo03/PLAN_ZLO03_PCBA_2026-11-18.md` |
| Einkauf-Vorberechnung seit 09.10. dreimal langsamer (35 s auf 100 bis 150 s); Messung braucht eine Kopie der Wochensicherung (Produktivdaten) | Freigabe Ingo | `docs/PLATTFORM_TEMPO_2026-09-28.md` 11.4 |
| Code-Review: Doings D1 bis D9 (ohne Entscheid moeglich) und Entscheide B1 bis B7 (u. a. `Security:Enabled`, `/netzwerk` offen?) | Ingo | `docs/CODE_REVIEW_2026-11-20.md` 6, Wochen_Todo 159 |
| `.claude/agents/hr.md` nennt den Rexx Hub noch als ausgeschaltet (seit 19.11. ein) | naechste Sitzung | Review 20.11. Abschnitt 8 |

## 4. Was ausserhalb von Git liegt

- `.tmp_tools/DeployHeadless/Program.cs` und `.csproj` (lokal, nicht versioniert): Einkauf-Pruefung vor dem Deploy, kompiliert
  `Services/PurchasingRunLog.cs` aus dem Release-Worktree mit; Uebersteuern mit `--trotz-einkauf-lauf`.
- Worktrees: `C:\TMP\TrafagSalesExporter_tempo2011` (Zweig `einkauf-tempo-2011`, bis zum Merge behalten),
  `C:\TMP\TrafagSalesExporter_review_9e27a70` (auf `ad4f437`, kann weg). Release-Worktree steht auf `155276e`.
- Deploy-Aufruf, wenn die Shell in `projektmanagement/` steht: `dotnet run --project ../.tmp_tools/DeployHeadless -c Release > ../.tmp_deploy_<datum>_<thema>.log 2>&1`.
- Rohdaten der SAP-Lesungen lagen im Scratchpad der Sitzung und sind mit ihr weg; die Befunde stehen in den Plan-Dateien.

## 5. Fehler dieser Tage (Volltext in `docs/rag/LEARNINGS.md`)

Review vom 12.11. erst spaet gefunden und „nirgends erfasst“ behauptet; Zaehlung „8 neue hohe“ ohne die alten Befundtexte; Deploy
tagsueber mit sofortigem Nachhol-Lauf; Testergebnis gemeldet, bevor es gelesen war; Timer-Fangnetz, das einen Export ausgeloest
haette; 207-MB-Dump mit `git add -A`; im Chat „Datenbank 90 Minuten belegt“, obwohl der Delta erst am Ende schreibt; in P76 ein
ad hoc angepasstes Skript mit Kurzdump (nichts geaendert).
