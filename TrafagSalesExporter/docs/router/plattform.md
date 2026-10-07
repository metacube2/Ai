# Unterrouter Plattform

Zurueck: `router.md`. Stand: 2026-09-28.

Architektur, Deployment, Admin, Requirements, Werkzeuge, Serveranalyse, Outlook-Grenzen,
Arbeitsplatzleistung und WLAN-Aussetzer, Tempo der Webapp.

## Dateien

| Bedarf | Datei |
| --- | --- |
| **Aktuell verifizierter Produktivstand** | `docs/rag/DEPLOYMENT.md` |
| **Verfahren, Deploy-Konsole, die vier Fallen** | `docs/DEPLOYMENT.md` |
| **Tempo der Webapp: sofort sichtbare Oberflaeche, Messung aller Seiten, `/einkauf` 100 s, Management-Cockpit 41 s (Wechselkurse je Zeile per SQL)** | `docs/PLATTFORM_TEMPO_2026-09-28.md` |
| **NuGet-Sicherheitsbefund und Updatepfad** | `docs/NUGET_SICHERHEIT_2026-09-01.md` |
| Architektur, Kurzstand | `docs/rag/ARCHITECTURE.md` |
| Admin-Bereich, Kurzstand | `docs/rag/ADMIN.md` |
| Admin-Bereich und Startseite | `docs/ADMIN_BEREICH_STARTSEITE_2026-05-21.md` |
| Zusammenfuehrung der Admin-Menues | `docs/ADMIN_MENUE_ZUSAMMENFUEHRUNG_2026-08-11.md` |
| Gesamtfunktionalitaet, reverse-engineered | `docs/REQUIREMENTS.md` |
| Diagramme und technische Einordnung | `docs/PROGRAMM_DIAGRAMME.md` |
| Projektstand, Kurzstand | `docs/rag/PROJECT.md` |
| Pausenspiel (Nebenfeature, Reiter ausgeblendet) | `docs/PAUSENSPIEL.md` |
| **Weltlage** (seit 2026-10-06 unter Finance Cockpit, hinter Finance-Passwort): Radar je Abteilung, Länder, Rohstoffe und Währungen, Ereignisse; externe Quellen ohne Anmeldung (GDELT, FRED, Eurostat, IMF, EZB), Firewall-Zustand je Quelle, Rechenregeln | `docs/WELTLAGE_2026-10-05.md` |
| **Reiter Benutzerhandbuch**: betriebswirtschaftliches Handbuch aller Module (Wozu, Lesart, Rechnung, Einsatz je Rolle), Word im Trafag-CI, Quelle `wwwroot/handbuch/kapitel`, Generator, Neuerzeugung | `docs/handbuch/README.md` |
| **Reiter Netzwerk**: Netzkarte, Verfuegbarkeit, Netz oder Daten, Sicherheit, Active Directory, AD-Infrastruktur, Gruppenrichtlinien, DNS, Verlauf, Migration, Bericht, Arbeitsplaetze; Schutzregeln fuer Pruefungen und AD | `docs/NETZWERK_2026-10-02.md` |
| **Reiter Operations (Shopfloor)**: PPA-Shopfloor im Cockpit (Route `/operations`), C#-Port des Python-Backends, Zugriff nur fuer freigegebene Windows-Konten (`Shopfloor:AllowedUsers`/`OpenForAll`), Datenbank `shopfloor\shopfloor.db`, Push-Schnittstelle mit Token | `docs/SHOPFLOOR_2026-10-07.md` |
| ccusage installieren und nutzen | `docs/CCUSAGE_INSTALL_ANLEITUNG.md` |

## Live-Werkzeuge

### SAP ERP: SapProbe

Ort `.tmp_sap_probe/`, Start ueber `.tmp_sap_probe/RunSapProbeInteractive.ps1 <befehl>`.
Default `travt762.sap.trafag.com`, SID `T76`, Client 100 — Produktion `travp762` nur
bewusst per `--ashost`. Passwort interaktiv oder ueber `SAP_NCO_PASSWORD`, **nie** in Doku
oder Git.

| Befehl | Zweck |
| --- | --- |
| `system-info` | Verbindung und System pruefen |
| `table-read` | Tabelleninhalte lesen |
| `table-fields`, `field-exists` | DDIC-Felder und Datenelemente pruefen |
| `function-info`, `function-search`, `rfc-call` | RFC-Bausteine untersuchen und aufrufen |
| `abap-read`, `abap-check` | ABAP lesen und im System syntaxpruefen |
| `abap-write`, `abap-activate` | schreiben und aktivieren, nur mit `--confirm-write` |

Grenzen: DDIC-Strukturen bleiben manuell in SE11, globale Klassen in SE24/ADT,
Gateway-Modell und EntitySets in SEGW. SapProbe verifiziert SAP-Fakten, ersetzt diese
Oberflaechen aber nicht.

### Outlook: was ueber COM geht und was nicht

Am 2026-09-04 an Ingos Arbeitsplatz gemessen. `New-Object -ComObject Outlook.Application`
verbindet sich, und ein Entwurf laesst sich anlegen:

| Vorgang | Ergebnis |
| --- | --- |
| `CreateItem(0)`, `Save()` in `GetDefaultFolder(16)` | funktioniert, der Entwurf erscheint in „Entwuerfe" |
| `.Subject` setzen | funktioniert, Umlaute inbegriffen |
| `.Attachments.Add(<Pfad>)` | funktioniert |
| `.Body` oder `.HTMLBody` **setzen** | **wird stillschweigend verworfen**, die Rueckmessung liefert 0 Zeichen — ohne Fehler und ohne Dialog |
| `.PropertyAccessor` | ist `null`, nicht nutzbar |
| `.GetInspector.WordEditor` | kennt `Content.Text` nicht |
| `Session.OpenSharedItem(<eml>)` | blockiert ohne Rueckmeldung, laeuft in die Zeitgrenze |

Vermutlich eine DLP- oder Sicherheitsrichtlinie, die den Nachrichtentext schuetzt. **Der
brauchbare Weg** ist deshalb: Entwurf mit Betreff und Anhang per COM anlegen, den Text als
HTML-Datei danebenlegen und beim Versand einmal hineinkopieren. So entstanden
`docs/ZZPRDAT_Mail_Abnahme_2026-09-04.html` und der zugehoerige Entwurf. Wer das nicht
weiss, baut den Text dreimal ueber COM und wundert sich, dass der Entwurf leer bleibt.

### SAP B1/HANA: HanaQ

Lokaler Helfer `.tmp_tools/HanaQ/`, braucht den SAP-HANA-.NET-Client. Aufruf
`HanaQ.exe <TSC> <sqlFile> [dbPath]`. Verbindung, Schema und Credentials werden aus
`Sites`, `SourceSystemDefinitions` und `HanaServers` der lokalen SQLite-Kopie aufgeloest —
**keine Passwoerter in SQL-Dateien**. Guardrail: nur `SELECT`/`WITH`, Platzhalter `{schema}`.

Zwei Messfallen:

- Prozentangaben immer auf die fachliche Grundgesamtheit filtern, etwa aktive Lagerartikel
  statt aller `OITM`-Zeilen.
- `LIKE 'U_%'` matcht wegen des Platzhalter-Unterstrichs auch `UserSign`. Fuer UDF-Spalten
  `LIKE 'U\_%' ESCAPE '\'` schreiben. Schemavergleiche in `SYS.TABLE_COLUMNS`
  case-insensitiv, weil Schemanamen je Standort unterschiedlich geschrieben sind
  (`TRAFAG_LIVE` gegen `it01_p`).

### Standorte, die nur der Server erreicht: Serveranalyse

Zweck: lesende Abfragen gegen Systeme, die der Entwicklungsrechner nicht erreicht, etwa
Indiens HANA `20.197.20.60:30015`.

**Ausgefuehrt wird von der laufenden Anwendung**, nicht von einem Werkzeug auf dem Server:
`Services/ServerAnalysisBackgroundService.cs` prueft alle 20 Sekunden, ob im
Anwendungsordner `_analysis/run.trigger` liegt, arbeitet dann `_analysis/sql/*.sql` ab und
schreibt nach `_analysis/results`.

Grund fuer diesen Umweg: Auf `tragvapp401` sind `Invoke-Command`, `schtasks` und `C$`
gesperrt und es gibt keinen RDP-Zugang; der Share ist aber beschreibbar. Mit dem
Aliasnamen `trch-webapp-bidashboard` scheitert schon Kerberos.

Fernbedienung: `docs/analyse/Run-ServerAnalysis.ps1 -Action Run | Fetch | Clean`.
Abfragen in `docs/analyse/sql/`, Belege in `docs/analyse/ergebnisse/`.

Regeln fuer SQL-Dateien: Dateiname beginnt mit dem TSC (`TRIN__01_...`), Statementtrenner
ist eine Zeile ab `;;`, nur `SELECT`/`WITH` (`Services/ReadOnlySqlGuard.cs`), Platzhalter
`{schema}`, maximal 500 Zeilen je Statement. **Zwei Bindestriche als Zeichenkettenliteral
sind nicht moeglich** — sie gelten als Kommentar und der Guardrail lehnt ab.

### Grosse Auswertungen: die Produktivdatenbank vorher lokal kopieren

Die produktive SQLite liegt auf der Freigabe `BiDashboard$` des Servers
`trch-webapp-bidashboard.trafagch.local`, Datei `trafag_exporter.db`, und ist rund
360 MB gross. Jeder Blockzugriff laeuft ueber SMB, und SQLite liest bei einem
`GROUP BY` ueber eine grosse Tabelle sehr viele einzelne Bloecke. Das schlaegt brutal
durch.

Am 2026-09-08 an `Finance_All` gemessen, `FinancialJournalEntries` mit 469'629 Zeilen:

| Lauf | direkt ueber die Freigabe | mit lokaler Kopie |
|---|---:|---:|
| gekuerzt, 30 Tage im Detailblatt | **10 min 21 s** | **49 s** |
| vollstaendig | ueber 10 min | **4 min 10 s** |

Die gekuerzte Fassung schrieb nur ein Zwanzigstel der Detailzeilen und war trotzdem
genauso langsam. Damit ist belegt: **nicht das Schreiben der Excel bremst, sondern das
Lesen ueber das Netz.** Die Summenblaetter gehen ueber die ganze Tabelle, und genau die
kosten die Zeit.

Die Kopie selbst dauert unter einer Minute und amortisiert sich ab dem ersten
Tabellenscan. `Tools/FinanceAll/finance_all_xlsx.py` hat dafuer den Schalter `--lokal`;
wer ein eigenes Auswertungsskript baut, macht es genauso.

Zwei Punkte dazu:

* **Nur lesend, und die Kopie danach wegwerfen.** Das Skript legt sie unter `%TEMP%` an
  und raeumt sie am Ende weg. Eine liegengebliebene Kopie ist ein veralteter
  Produktivstand, auf den irgendwann jemand hereinfaellt.
* **Fuer kleine Abfragen lohnt es nicht.** `SqlQ` gegen die Freigabe ist fuer ein paar
  tausend Zeilen voellig in Ordnung. Die Kopie lohnt ab dem Punkt, wo ein voller
  Tabellenscan oder mehrere Aggregationen im Spiel sind.

## Arbeitsplatz Ingo: wenn die Anzeige zaeh wird

Gemessen am 2026-09-11 auf `NB61258` (Lenovo 20Y1, Ryzen 7 PRO 4750U, 37,7 GB RAM,
Samsung 970 PRO NVMe). Ingo meldete, die Anzeige sei traege und Programme reagierten
verzoegert, "wie wenn eine alte HDD drin waere". Die Vermutung lag damit auf Datentraeger
oder Arbeitsspeicher. **Beides war es nicht**, und genau deshalb steht die Messung hier:
Wer beim naechsten Mal wieder bei CPU und SSD anfaengt, verliert dieselbe Stunde.

Was die Messung ergab:

| Geprueft | Ergebnis |
|---|---|
| CPU unter Dauerlast, zwoelf Laeufe | 0,55 s je Lauf, 2 % Abweichung, also kein Throttling |
| Arbeitsspeicher | 24,8 GB frei, Auslagerungsdatei mit 0 MB unbenutzt |
| NVMe sequenziell | 833 MB/s schreiben, 1667 MB/s lesen |
| NVMe viele kleine Dateien | 0,6 ms je Datei im Temp-Ordner |
| Gesamtauslastung | CPU im Mittel 15 %, kein auffaelliger Prozess |
| **Externer Monitor** | **2310 x 990 bei 60 Hz** |

Der externe Philips 345E2 lief auf **2310 x 990**. Diese Aufloesung steht **nicht** in der
EDID-Liste des Monitors; dort ist **3440 x 1440** die hoechste, und der Treiber bietet sie
mit **75 Hz** an. 2310 x 990 kam also aus dem AMD-Treiber als benutzerdefinierter,
skalierter Modus. Damit skalierte die Grafikeinheit jedes einzelne Bild auf die native
Panelgroesse hoch, und das bei 60 statt 75 Hz.

Das erklaert den Befund vollstaendig: Die Vega-Grafik sitzt in der CPU und teilt sich den
Arbeitsspeicher, also schlaegt jede zusaetzliche Skalierung auf den Bildaufbau **aller**
Fenster durch. Im Taskmanager sieht das harmlos aus, weil es Latenz im Compositor ist und
keine Rechenlast. Wer nur auf die CPU-Anzeige schaut, sucht an der falschen Stelle.

Der Datentraeger-Leerlauf ("Festplatte ausschalten nach") sieht mit 30 Sekunden zwar
aggressiv aus, ist aber **keine Erklaerung**: Der Wert steht im ausbalancierten Plan
genauso, gilt fuer NVMe ohnehin nicht, und die gemessene Zugriffszeit widerlegt ihn.

Ein zweiter Befund betraf den Energieplan. Aktiv war ein selbst angelegter
"Benutzerdefinierter Energiesparplan 1" mit **minimalem Prozessorzustand 100 %** statt der
ueblichen 5 %. Das haelt den 15-Watt-Chip dauerhaft auf vollem Takt, kostet Waerme und
Luefter und nimmt thermische Reserve weg. Ursache der Traegheit war es an diesem Tag nicht,
eine Fehleinstellung aber schon.

**Behoben am 2026-09-11:** Ingo hat die Aufloesung auf 3440 x 1440 gesetzt und auf den von
der IT bereitgestellten Plan **"TRAFAG AG"** umgeschaltet, womit der minimale
Prozessorzustand wieder bei 5 % liegt. Beides nachgemessen und bestaetigt, die Anzeige
laeuft seitdem fluessig. **Offen geblieben ist die Bildwiederholrate**, sie steht weiterhin
auf 60 Hz, obwohl der Monitor 75 Hz anbietet.

### Die native Aufloesung macht die Schrift klein, und die DPI-Falle dahinter

Nach der Umstellung war die Schrift auf dem 34-Zoll-Panel kaum noch lesbar. Das ist die
erwartbare Folge, denn 3440 x 1440 auf 34 Zoll sind rund 110 Bildpunkte je Zoll, und
Windows empfiehlt dort **100 %**. Erhoeht man die Skalierung, bleibt das Bild scharf und
alles wird trotzdem groesser; eine niedrigere Aufloesung waere der falsche Weg zurueck.

Die Falle: Skalierung ist **pro Bildschirm** eingestellt, und in der Windows-Anzeigeseite
wirkt der Schieber immer nur auf den gerade ausgewaehlten Bildschirm. Ingo hatte die
Skalierung bereits erhoeht, sie landete aber auf dem internen Notebookdisplay, das damit
auf 175 % stand, waehrend der Philips unveraendert auf 100 % blieb. Gemessen sah das so
aus:

| Bildschirm | empfohlen | war eingestellt | maximal |
|---|---|---|---|
| `\.\DISPLAY1`, internes Panel | 150 % | 175 % | 175 % |
| `\.\DISPLAY2`, Philips 345E2 | 100 % | **100 %** | 225 % |

Gesetzt wurde der Philips danach auf **150 %**. Der logische Arbeitsbereich betraegt damit
2293 x 960 und ist immer noch breiter als ein gewoehnlicher Full-HD-Bildschirm.

Ausgelesen und gesetzt wird das ohne Klick ueber `QueryDisplayConfig` und
`DisplayConfigGetDeviceInfo` beziehungsweise `DisplayConfigSetDeviceInfo` aus `user32.dll`,
mit den undokumentierten Informationstypen **-3 zum Lesen und -4 zum Schreiben**. Der Wert
ist **relativ zur Empfehlung**, nicht absolut: Die Stufenliste lautet
100/125/150/175/200/225/250/300/350/400/450/500, und `min` gibt an, wie viele Stufen die
Empfehlung ueber dem Minimum liegt. Aus `-min` ergibt sich der Index der Empfehlung, aus
`-min + cur` der aktuelle Wert. Die Aenderung wirkt sofort, ohne Abmeldung und ohne
Neustart.

Zwei Stolpersteine, die dabei Zeit gekostet haben:

* Der Aufruf braucht die **Source**-Kennung aus `path.sourceInfo`, nicht die Target-Kennung.
* In PowerShell liefert der Zugriff auf ein verschachteltes Strukturfeld eine **Kopie**.
  `$s.header.type = 1` veraendert deshalb nichts, der Aufruf scheitert mit `rc=31`. Der
  Kopf muss als Ganzes zugewiesen werden, am einfachsten schon im C#-Teil.

### Was bei der Messung sonst noch auffiel

* Schreiben im Repository-Ordner kostet 1,3 ms je Datei gegenueber 0,6 ms im Temp-Ordner,
  also etwa das Doppelte. Das ist der Sophos-Echtzeitscanner und erklaert, warum sich
  Builds und Git zaeh anfuehlen, obwohl der Datentraeger schnell ist. Aenderbar ist das
  nur ueber eine Ausnahmeregel der IT, nicht vom Arbeitsplatz aus.
* Alle Werte ausser Monitor und Energieplan sind vier Minuten nach einem Neustart
  entstanden, also noch im Startbetrieb. Fuer eine Aussage ueber den Dauerbetrieb muesste
  nach einer Stunde normaler Arbeit nachgemessen werden.

## Arbeitsplatz Ingo: wiederkehrende WLAN-Aussetzer

Gemessen am 2026-09-11, gleicher Rechner wie im Abschnitt davor. Ingo meldete, das Internet
stocke immer wieder und sei am Vortag ebenfalls kurz weggebrochen.

**Die Momentaufnahme war einwandfrei und damit wertlos.** 80 Pings ohne einen einzigen
Verlust, 4 bis 5 ms zum Gateway wie ins Internet, DNS-Aufloesung unter 40 ms, HTTPS-Aufbau
zu `api.anthropic.com` zwischen 4 und 37 ms, sieben saubere Hops zu `1.1.1.1`. Bei einem
Fehler, der nur gelegentlich auftritt, beweist eine Messung von zwei Minuten nichts. Wer
hier weitermisst, verbrennt Zeit.

**Der richtige Griff ist das Ereignisprotokoll**, weil es die Vergangenheit schon
aufgezeichnet hat:

```powershell
Get-WinEvent -FilterHashtable @{ LogName='Microsoft-Windows-WLAN-AutoConfig/Operational'; StartTime=(Get-Date).AddDays(-3) }
```

Darin standen fuer drei Tage: sechs Trennungen (`8003`), vier gescheiterte
Verbindungsversuche (`8002`), acht erfolgreiche Verbindungen (`8001`) und zwoelf
Verbindungsversuche (`8000`). Die belegten Einzelfaelle:

| Zeitpunkt | Ereignis |
|---|---|
| 2026-09-10 17:59:57 | `11006` Fehler der Drahtlossicherheit, mit Trennung `8003` um 17:59:40 |
| 2026-09-11 06:45:07 | vier gescheiterte Versuche auf `Turbo`, `Turbo56G` und `trafag-802.1x` |
| 2026-09-11 10:19:24 und 10:21:53 | zwei Neuverbindungen `8001` innerhalb von zweieinhalb Minuten, zeitgleich zu Ingos Meldung "internet hat kurz gedroppt" |

Dazu kommt ein durchgehendes Muster: rund alle 26 bis 29 Minuten ein Paar
Sicherheits-Neuverhandlungen (`11004` angehalten, `11010` gestartet, `11005` erfolgreich)
im Abstand von exakt drei Minuten, in drei Tagen sechzigmal. Das ist die periodische
Schluesselerneuerung des Zugangspunkts.

Die Konfiguration, die das beguenstigt:

* **`Turbo` und `Turbo56G` sind zwei getrennte Profile**, also 2,4 GHz und 5 GHz als eigene
  Netze. Verbunden war `Turbo`. Die Adaptereinstellung **"Bevorzugtes Band"
  (`RoamingPreferredBandType`) steht auf "Keine Einstellung"**, Windows darf also frei
  wechseln, und jeder Wechsel ist ein kurzer Abriss.
* Das per Gruppenrichtlinie verteilte Firmenprofil **`trafag-802.1x`** liegt
  schreibgeschuetzt mit auf dem Geraet und wird zu Hause regelmaessig erfolglos mitprobiert.
* Treiber Intel Wi-Fi 6 AX200, Version `23.110.0.5` vom 2025-01-02, also nicht mehr aktuell.

Ausgeschlossen wurde der Energiesparmodus des Adapters: Er steht im Netzbetrieb auf
Hoechstleistung, und `Get-NetAdapterPowerManagement` zeigt keinen aktiven Ruhezustand.

**Nicht umgesetzt, weil die Rechte fehlen.** `TRAFAGCH\koi` ist kein lokaler Administrator.
Sowohl `Set-NetAdapterAdvancedProperty` fuer das bevorzugte Band als auch
`netsh wlan set profileparameter` fuer ein Alle-Benutzer-Profil brauchen erhoehte Rechte,
und `trafag-802.1x` ist als Gruppenrichtlinienprofil ohnehin schreibgeschuetzt. Der
Vorschlag geht damit an die IT: bevorzugtes Band auf 5 GHz festlegen, `trafag-802.1x`
ausserhalb des Standorts nicht automatisch verbinden lassen, und den Intel-Treiber
aktualisieren.

Eine Falle beim Nachmessen: **`netsh wlan show interfaces` verweigert die Auskunft**, wenn
die Standortdienste aus sind, mit dem irrefuehrenden Zusatz, es brauche erhoehte Rechte.
`Get-NetAdapter`, `Get-NetAdapterAdvancedProperty` und das Ereignisprotokoll liefern
dieselben Angaben ohne diese Huerde.

## Fallen in diesem Ast

Die vier Deploy-Fallen stehen ausfuehrlich in `docs/DEPLOYMENT.md` Abschnitt 4. Kurz:

- Ein erfolgreicher Publish kann die Haupt-DLL wegen **PreserveNewest** still
  ueberspringen. Weicht der SHA256 ab, ist das kein Nicht-Determinismus, sondern ein nicht
  erfolgter Kopiervorgang.
- Ein SHA-Vergleich allein beweist die Wirkung nicht — Tokens brauchen eine
  Vorher-Messung.
- `/p:PublishProfile=FolderProfile` setzt `DeleteExistingFiles=true` und wuerde die
  Produktivdatenbank im Zielordner treffen.
- Die XLSX im Publish-Ordner sind Build-Ausgabe und werden bei jedem Deploy ueberschrieben.

## Querverweise in Nachbaraeste

- ABAP und SAP-Objekte: `docs/router/sap.md`
- Agentenkoordination vor einem Deploy: `docs/router/projekt.md`
