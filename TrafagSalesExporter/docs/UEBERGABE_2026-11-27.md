# Uebergabe 2026-11-27: Shopfloor (Fabio), SAP, Z-Objekte, Agenten-Monitor

Stand: 2026-11-27 vormittags, Abschnitt 8 (ZD05-Ergebnis) nachgetragen um ~10:00, Abschnitt 6 (Vorlesen) ~10:30. Geschrieben von der Sitzung `trafagsalesexporter-aa` (Auftrag Ingo: „schreibe am Schluss md, ich starte
Chat neu, bereite alles vor, Instruktion fuer weitere Arbeiten“). **Eine neue Sitzung liest zuerst diese Datei**, dann wie immer
`CLAUDE.md`, `router.md`, `docs/AGENT_COORDINATION.md` (vor jeder Aenderung) und `docs/rag/LEARNINGS.md` (vor SAP/Deploy).
Status je Aufgabe gilt weiter aus `projektmanagement/Wochen_Todo.tsv`; diese Datei ordnet nur die Reihenfolge und nennt die Fallen.

## 0. Zuerst wissen

- *Nachtrag 27.11. ~10:00: Git Bash laeuft in der neuen Sitzung wieder; der folgende Punkt ist damit ueberholt.*
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

## 1. Shopfloor Welle 2 / Punkt 9 einschalten (Caches P76 offenbar geleert, naechster Schritt 2)

Doku: `docs/SHOPFLOOR_2026-10-07.md` 28.10, Plan/Schnittstelle `saptasks/shopfloor_sap/PLAN_WELLE2_2026-11-25.md` 11.8/11.9.
SAP-Auftrag `T76K912954` ist **in P76 importiert** (Ingo 26.11.), aber das Board bekam bis 26.11. abends **404** (Caches).

*Nachtrag 27.11. 09:30 (Abschnitt 8): `ShopMatBezugSet` antwortet in P76 mit 200 statt 404; Schritt 1 ist damit offenbar erledigt,
der Wert 1'525 aber noch nicht gelesen. Weiter mit Schritt 2.*

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

Analyse erledigt 27.11. 09:30 (Agent sap-t76, P76/T76 nur lesen, Freigabe Ingo „sap p und t offen“). **Ergebnis und naechste
Schritte in Abschnitt 8**; die offenen Punkte unten sind dort beantwortet (Ursache: Set verlangt einen Bestell-Zugang, ZD05 nicht). Belegt aus dem Code:
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

*Nachtrag 27.11. ~10:30:* **Vorlesen** gebaut (Auftrag Ingo): `60f1860`, 130 Tests, bei Ingo eingespielt (Stimme horst, Sicherung
`%LOCALAPPDATA%\AgentMonitor\app_vorher_2026-11-27`), Block in `~/.claude/CLAUDE.md` angelegt, Durchlauf mit echtem Stop-Ereignis gehoert
(123 s bis zur Ansage, der Dienst laedt die Stimme nach Pause neu). Paket `Downloads\AgentMonitor_Paket_2026-11-27` (Ordner und ZIP)
**ohne Schluessel**: Ingo holt bei lagoon einen Paket-Schluessel mit horst und commanderdataTNG3 (diese Stimme ist fuer den heutigen
Schluessel nicht freigegeben) und legt ihn als `tts_schluessel.txt` in den Paketordner oder nach
`%APPDATA%\AgentMonitor\paket_tts_schluessel.txt` und baut neu. Subtree lokal in `..\Ai-werkzeuge` nachgezogen (`45d96b7`), **nicht
gepusht** (enthaelt Mail-Waechter mit Fabios Adresse, Entscheid Ingo). Naechster Wunsch Ingo: Auto-Update (NetSparkle).

## 7. Sonst offen

- Z999 / IC-Preisfindung (Wochen_Todo 153): Angebot an Ingo, in P76 nur lesend zu pruefen, ob Z999-Saetze existieren und ob
  `T76K912884` importiert ist (`saptasks/ic_preisfindung/NOTES.md` 8.9–15). Noch nicht beantwortet.
- Eigene P76-Sitzung von 26.11. (SUSG) evtl. noch offen: mit `SapGuiP76Hilfe.vbs info` pruefen, eigene mit `ok <n> /i` schliessen.

## 8. Nachtrag Ergebnis Punkt 12 (ZD05)

Eingetragen 27.11. nach der Rueckkehr des Agenten (sap-t76, 09:00 bis 09:30, P76/T76 nur gelesen, eigene P76-Sitzung mit `/i`
geschlossen). Ganzer Bericht: `saptasks/shopfloor_sap/ZD05_ANALYSE_2026-11-27.md`; Report-Quelltext (T76) neu im Repo:
`saptasks/shopfloor_sap/ZMM_CHECK_UMTERMINIERUNG_T76_2026-11-27.abap`.

**Ursache (am Quelltext belegt, von der Hauptsitzung nachgeprueft):** Das Set filtert strenger als der Report.
- Report ZD05 (`ZMM_CHECK_UMTERMINIERUNG`, Zeile 214): `check not wa_matnr is initial.` prueft die ganze Struktur; weil `zzudek`
  schon gefuellt ist, greift die Pruefung nie. ZD05 zeigt also **jedes Material mit Unterdeckung**.
- `ShopZd05Set` (`DPC_EXT_GET_ENTITYSET_2026-11-26_welle2_neu.abap` Zeile 555): `CHECK lv_sh_umt = abap_true` laesst nur Materialien
  durch, bei denen nach der Unterdeckung noch eine **offene Bestelleinteilung (EKET)** kommt. Fertigungsauftrag, Planauftrag und Banf
  zaehlen nicht als Zugang. Set-Zweig in T76 seit `T76K912718` (09.10.) unveraendert.
- Zaehlung P76 09:16: Disponent 001 Report 10 / Set 8, 003 16 / 12, 002 1 / 0, 004 1 / 0. Die Differenz geht genau auf.

**Je Material (MD04 P76, 09:23 bis 09:25):**
- D64231, D65038 (001): nur Banf als Zugang. B66364 (003): nur Fertigungsauftrag. QE11155 (003): nur Planauftrag. Alle belegt.
- B66360, C24254 (003): nur Fertigungsauftraege, hergeleitet aus der Zaehlung.
- E11229 (002) und H90313 (004): Disponent nicht in `Zd05Dispo`, **und** das Set liefert fuer 002/004 ohnehin nichts.
- E11155 und C37815 stehen 09:16 auch im Report nicht mehr (Unterdeckung weg bzw. nie unter Reichweite). Marcos Liste stammt
  vermutlich von einer anderen Uhrzeit (Set 06:17 22 Zeilen, 09:16 20; H90313 bei Fabio UD 17.12., jetzt 21.12.), nicht belegt.
- **E11155 und QE11155 sind zwei Materialien.** Fabios „07.12.“ passt zu QE11155.

**Ausgeschlossen:** Board (`ImportRows` verwirft nur leere/doppelte), `UP TO 4000 ROWS` (wirkt heute nicht, bleibt Risiko, T76 hat
fuer 003 3'586 Materialien). Nicht geprueft: Filter im Standardlayout von ZD05 (`LTDXD`).

**Nebenbefund zu Abschnitt 1:** `ShopMatBezugSet` (1100, 16. bis 21.11.) antwortet in P76 um 09:2x mit **200** statt 404. Die
Gateway-Caches sind also offenbar geleert; ob `Anzahl` = 1'525 ist, konnte der Agent nicht lesen (fuer P76 gibt es kein freigegebenes
Werkzeug, das den Antwortrumpf liest). Schritt 2 in Abschnitt 1 (Nenner-Abruf) ist damit der naechste.

**Nebenwirkung in P76:** SET/GET-Parameter in Ingos Benutzer: das naechste ZD05 schlaegt wahrscheinlich Disponent 004 vor, das naechste
MD04 das Material QE11155.

**Naechste Schritte (Entscheid Ingo mit Marco/Fabio, nichts davon ist begonnen):**
1. Soll das Set den Report abbilden (jedes Material mit Unterdeckung, auch Eigenfertigung/Planauftrag/Banf) oder ist die tote
   Pruefung im Report der eigentliche Fehler (ZD05 heisst „Bestellungen auf Umterminierung pruefen“)? Danach entweder
   `CHECK lv_sh_umt` im Set entfernen (T76, eigener Auftrag, Plan zuerst) oder den Report korrigieren.
2. Erst danach: Disponenten 002 und 004 in `Shopfloor:Zd05Dispo` aufnehmen?
3. Bei Fabio erfragen: Schreibweise E11155/QE11155 in Marcos Liste und Uhrzeit seines ZD05-Laufs.
4. Antwort an Fabio auf Punkt 12 erst nach Entscheid 1 (Inhalt steht oben).

## 9. Nachtrag 27.11. mittags: Mail Fabio 11:36 (Punkte 14–20), Antwort Punkt 12, Werk 1200

- Ingos Mail zu Punkt 12 (Analyse ZD05, zwei Entscheidfragen) ist **um 09:57 gesendet** (Abschnitt 8 erledigt).
- Fabio 11:36 mit Paket `Downloads\Shopfloor_Paket_Ingo_2026-11-27.zip` (entpackt daneben): Punkte 14–20 und **Antwort Fabio/Marco zu
  Punkt 12**: alle Unterdeckungen zeigen (`CHECK lv_sh_umt` weg), Planauftrag/Banf kein Zugang, **Fertigungsauftrag zaehlt als Zugang**,
  Disponenten 001–006, QE11155 ist Marcos Zeile, C37815 = Uhrzeit (Marco 06:50, Import 06:17). Im Testsystem bauen.
- **ZD05 rechnet nicht so:** der Report kennt nur Bestellungen als Zugang (Fertigungsauftraege fehlen), das Board weicht danach bei
  Eigenfertigung bewusst ab. An Fabio melden.
- Bauplan Punkte 12 und 14–20 mit Belegen, drei Wellen und sieben offenen Fragen an Fabio: `docs/shopfloor_paket_2026-11-27/PLAN.md`.
  SAP-Aenderung fuer Punkt 12 ist dort beschrieben, **noch nicht gebaut**.
- Werk 1200 (Wochen_Todo 179): Fabio sagt „umsetzen“ (in P76 schon 2'607 von 2'620 Materialien auf 02), FERT/HAWA OMIG in T76
  testen, Vorlagematerial reicht. Erster Testlauf 12:00 **abgebrochen ohne Aenderung**, weil das Netz beide SAP-Verbindungen trennte
  (`saptasks/tmvfu_werk1200/NOTES.md` Abschnitt 6). 090 pflegt Ingo selbst.
