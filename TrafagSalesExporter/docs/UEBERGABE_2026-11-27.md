# Uebergabe 2026-11-27: Shopfloor (Fabio), SAP, Z-Objekte, Agenten-Monitor

Stand: 2026-11-27 vormittags. Geschrieben von der Sitzung `trafagsalesexporter-aa` (Auftrag Ingo: „schreibe am Schluss md, ich starte
Chat neu, bereite alles vor, Instruktion fuer weitere Arbeiten“). **Eine neue Sitzung liest zuerst diese Datei**, dann wie immer
`CLAUDE.md`, `router.md`, `docs/AGENT_COORDINATION.md` (vor jeder Aenderung) und `docs/rag/LEARNINGS.md` (vor SAP/Deploy).
Status je Aufgabe gilt weiter aus `projektmanagement/Wochen_Todo.tsv`; diese Datei ordnet nur die Reihenfolge und nennt die Fallen.

## 0. Zuerst wissen

- **Git Bash startet seit 27.11. ~09:00 nicht** (`fatal error - add_item ("\??\C:\Program Files\Git", "/", ...) failed, errno 1`).
  Kein haengender Prozess, Git unveraendert (2.51.0, Aug. 2025); Ursache nicht gefunden. **Abhilfe: Windows ab-/anmelden oder
  neu starten.** Bis dahin PowerShell nehmen (`git.exe` geht dort). **Deploy braucht Bash** (Befehl nach `CLAUDE.md` 5), also erst
  nach dem Neustart deployen.
- **Produktivstand Dashboard: `a2f7c97`** (Zweig `shopfloor-paket-2611`, Worktree `C:\TMP\Ai_sf2611`, gepusht). **Jeder naechste
  Deploy muss `a2f7c97` enthalten**; der Zweig ist nicht in `feature/supplier-overrides-trit` gemergt. Release-Worktree
  `C:\TMP\TrafagSalesExporter_release_eafd1c5` steht auf `a2f7c97`.
- Repo `metacube2/Ai` ist **oeffentlich**: vor jedem Push die enthaltenen Firmendaten Ingo nennen (router Regel 4).
- Neuer Worktree: sofort leere `trafag_exporter.db` im Projektordner anlegen (sonst MSB3030, LEARNINGS 26.11.).
- P76 nur in **eigener** GUI-Sitzung (`.tmp_sap_probe/SapGuiP76Hilfe.vbs neu`, schliessen mit `ok <n> /i`), nie Ingos Sitzungen,
  keine Passwortskripte (`SapCredential.ps1`/`Test-SapPassword` lehnt der Auto-Modus ab), T76 nur `RunSapProbe.ps1` mit DPAPI.

## 1. Shopfloor Welle 2 / Punkt 9 einschalten (wartet auf Ingo: Gateway-Caches P76)

Doku: `docs/SHOPFLOOR_2026-10-07.md` 28.10, Plan/Schnittstelle `saptasks/shopfloor_sap/PLAN_WELLE2_2026-11-25.md` 11.8/11.9.
SAP-Auftrag `T76K912954` ist **in P76 importiert** (Ingo 26.11.), aber das Board bekam bis 26.11. abends **404** (Caches).

1. Ingo: in P76 `/IWBEP/CACHE_CLEANUP`, dann `/IWFND/CACHE_CLEANUP` (`ZPOWERBI_EINKAUF_MDL`); bei 404 die **ganze Runde wiederholen**.
2. Pruefen (nur lesen): `GET https://trch-webapp-bidashboard.trafagch.local/BiDashboard/shopfloor/api/sap/nenner?tag=2026-11-16`
   (Shell: `curl --ntlm -u :`, aus PowerShell geht `curl.exe --ntlm` nicht; sonst im Arbeits-Chrome). Erwartet `"ok":true`, Nenner
   KW 47 = **1'525** (Fabios Wert).
3. Im Worktree `C:\TMP\Ai_sf2611` in `appsettings.json` `Shopfloor:SapKdTerminEnabled` und `SapNennerEnabled` auf `true`, Tests
   (Stand 1'925/1'925), Commit, Release-Worktree auf den Commit, Tests, Deploy mit dem Werkzeug (Bash, nach Neustart), Commit-Zeile
   pruefen, `-wal`/`-shm` per Einzel-`ls` auf den Share.
4. Abnahme: `api/sap/kdtermin?module=tvav&vbeln=419908&posnr=10` gegen Fabios P76-Protokoll (SHOPFLOOR 28.10 „Erwartung fuer die
   P76-Abnahme“: FA 1281397, PA 2539374/2539375, Termin 06.01.2027; /20: PA 2539372) bzw. MD04 im selben Moment; Nenner-Tabelle auf
   der ZD05-Seite (KW 47 1'525, „SAP“-Kennzeichen). Danach SHOPFLOOR, rag DEPLOYMENT, Wochen_Todo 172.

## 2. Shopfloor Punkt 12: 9 ZD05-Positionen fehlen im Board (Mail Fabio 27.11. 08:22, Marco Widmer cc)

Analyse laeuft bzw. lief (Agent sap-t76, P76/T76 nur lesen, Freigabe Ingo „sap p und t offen“). **Ergebnis siehe Nachtrag unten**
(Abschnitt 8), falls schon eingetragen. Belegt aus dem Code:
- Das Board holt die ZD05 nur fuer **Disponent 001 und 003** (`appsettings.json` `Shopfloor:Zd05Dispo`): E11229 (002) und H90313 (004)
  fehlen deshalb.
- „erledigt“ im Board = im vorigen Import da, im neuen nicht (`ShopfloorZd05.ImportRows`): E11155 kam am 27.11. 06:17 nicht mit.
- Offen: B66360/B66364/C24254 (Eigenfertigung, BESKZ E: Filter im Set `ShopZd05Set`?), D64231/D65038/C37815 (Zeitversatz zu 06:17?).
Dann: Plan (Disponenten erweitern? Eigenfertigung aufnehmen? Abrufzeit?), Entscheid Ingo/Fabio, umsetzen, Antwort an Fabio.

## 3. Shopfloor Punkt 13: Radar je Stichtag festhalten (Board, ohne SAP)

Wunsch Fabio/Marco: Radar-Werte z. B. je Monatsende speichern, Verlauf aus den gespeicherten Werten zeigen, heute live. Vorschlag:
Tabelle `radar_stichtag` in `shopfloor.db` (Stichtag, Sparte, Werte), Schreiben am Monatsletzten (im SAP-Lauf oder Hintergrund) und
per Knopf, Anzeige Verlauf unter dem Radar (`ShopfloorRadar.cs`, `app.js` `zdRadar`). Erst Plan an Ingo, dann bauen, Tests, Deploy.

## 4. Offen bei Fabio (Mails stehen im Chat vom 26.11.; ob gesendet, Ingo fragen)

- Antwort Board-Teil + SAP-Teil (eine Mail „schreibe mail neu“, 26.11.) mit 3 Fragen zu Punkt 9 (BESKZ F bei TVAV/Angebote? nur offene
  FA? Angebote-Handfelder nur leer fuellen?) und Frage Mail-Ereignisse je Liste.
- Rueckfrage **TMVFU Werk 1200** (Z2 bewusst 2025 fuer AT, Werk 1100 im offenen `T76K912186` auf Y2, 12 vs. 2'496, Materialarten fuer
  OMIG 0002): `saptasks/tmvfu_werk1200/NOTES.md`, Wochen_Todo 179. **Nichts aendern bis Fabio antwortet.**
- Radar-Abweichung gegen Marco (Stichtag 29.10., nach Import V1.04): GFS TR5 1'829 statt 1'066, TX Einkauf 2'097 statt 2'654.
- Mailversand Board: Absender laeuft ueber Alex (ICT, Fabio hat angefragt); danach `Shopfloor:Mail` (Modus smtp, Host, Absender,
  SendenAb) und `Shopfloor:BaseUrl` setzen; ICT-Mailtext steht im Chat 26.11.

## 5. Z-Objekte aufraeumen (SCMON/SUSG seit 26.11. aktiv)

`saptasks/zobj_aufraeumen/NOTES.md` 8/9. Termine in Wochen_Todo:
- **175, faellig heute 27.11.:** P76 nur lesen, Transaktion SUSG „Aggregationsprotokoll“ der ersten Verdichtung (27.11. 02:30) ohne
  Fehler? SCMON Records/Tagesscheiben ansehen (eigene Sitzung, nichts druecken).
- 176 (03.12.): SCMON-Wachstum gegen Limit 20 Mio.; 177 (15.12., Ingo): ST03N-Aufbewahrung P76 verlaengern; 178 (15.01.2027):
  `Z_KOI_ZOBJ_NUTZUNG` erneut in P76, mit Liste `Downloads\Z_Objekte_Nutzung_P76_2026-11-26.xlsx` vergleichen. Loeschen fruehestens 2027.

## 6. Agenten-Monitor Mail-Waechter (eigenes Repo `C:\Users\koi\source\repos\AgentMonitor`, kein Remote)

Stand `62a9604`, 110 Tests, eingespielt (README im Repo). Alle 30 min Outlook nur lesen fuer Absenderliste (Fabio), stille Analyse,
Antworten als `.eml` + Register + Chrome-Lauf mit Kategorie „Agent-Entwurf“; der Monitor sendet nie. Offene Entscheide Ingo: IT-Freigabe
Outlook-Programmzugriff (COM liefert keinen Text), PreToolUse-Hook gegen Lesebefehle ausserhalb des Repos bei SAP-Schaltern,
Rueckblick 3 Tage, Mac (lagoon) nachziehen, Subtree in `werkzeuge/agentmonitor-videodoku`. Neues Outlook legt eine geoeffnete
`.eml` sofort als Entwurf an.

## 7. Sonst offen

- Z999 / IC-Preisfindung (Wochen_Todo 153): Angebot an Ingo, in P76 nur lesend zu pruefen, ob Z999-Saetze existieren und ob
  `T76K912884` importiert ist (`saptasks/ic_preisfindung/NOTES.md` 8.9–15). Noch nicht beantwortet.
- Eigene P76-Sitzung von 26.11. (SUSG) evtl. noch offen: mit `SapGuiP76Hilfe.vbs info` pruefen, eigene mit `ok <n> /i` schliessen.

## 8. Nachtrag Ergebnis Punkt 12 (ZD05)

*(wird eingetragen, sobald der Agent zurueck ist; fehlt dieser Abschnitt, ist die Analyse in der neuen Sitzung neu zu starten)*
