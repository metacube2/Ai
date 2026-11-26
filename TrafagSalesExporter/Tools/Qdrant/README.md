# Lokale Vektorsuche ueber die Projektdokumentation

Stand: 2026-11-26 (Suche im Browser: neuer Abschnitt 8 mit `suche_server.py` und `Start-Suche.ps1`; die Angabe zur Weboberflaeche `/dashboard` in Abschnitt 2 ist ueberholt); davor 2026-11-12 (Doku-Pruefung: Kopf nachgefuehrt; Abschnitt 4 beschreibt seit dem 2026-11-10 den Reindex als eigenen Prozess mit Pruefung); Grundstand 2026-09-03

Dieser Ordner enthaelt die Werkzeuge, mit denen die Markdown-Dokumentation des Repositories
in eine lokale Vektordatenbank geschrieben und von Claude-Sitzungen semantisch durchsucht
wird. Alles laeuft auf dem Arbeitsplatzrechner im Benutzerkontext. Es sind **keine
Administratorrechte** noetig, und es verlassen **keine Inhalte den Rechner**.

## 1. Wozu das gut ist und wozu nicht

Der Einstieg in die Dokumentation bleibt `router.md`, so wie es `CLAUDE.md` vorschreibt. Die
Vektorsuche ersetzt den Router nicht, sondern hilft in dem Fall, den der Router nicht
abdeckt: wenn noch unklar ist, in welcher Datei eine Aussage ueberhaupt steht. Typisch sind
Fragen nach einer Fachregel, einem Entscheid, einem Issue-Stand oder einem Deploydatum.

Die Suche liefert Ausschnitte samt Datei und Abschnitt. Der Ausschnitt ist ein Hinweis, kein
Beleg. Die gefundene Datei danach vollstaendig lesen, denn der Index bildet den Stand des
letzten Indexlaufs ab und kann der Datei hinterherhinken.

## 2. Aufbau

| Baustein | Ort | Zweck |
|---|---|---|
| Qdrant 1.19.0 | `C:\Users\koi\tools\qdrant\qdrant.exe` | Vektordatenbank, gebunden an `127.0.0.1:6333` |
| Vektorspeicher | `C:\Users\koi\tools\qdrant\storage` | bewusst ausserhalb des Repositories |
| Einbettungsmodell | `C:\Users\koi\tools\qdrant\models` | `sentence-transformers/paraphrase-multilingual-MiniLM-L12-v2`, laeuft lokal per ONNX |
| `uv` 0.12.9 | `C:\Users\koi\.local\bin\uv.exe` | fuehrt den MCP-Server und das Indexierskript aus |
| MCP-Server | `uvx mcp-server-qdrant` | stellt Claude das Werkzeug `qdrant-find` bereit |
| Konfiguration | `.mcp.json` in der Repository-Wurzel | verbindet Claude Code mit dem MCP-Server |
| Suchseite (seit 2026-11-26) | `Tools/Qdrant/suche_server.py`, gestartet mit `Start-Suche.ps1` | Suche im Browser unter <http://127.0.0.1:6340/>, siehe Abschnitt 8 |

Der Dienst bindet ausschliesslich an `127.0.0.1` und ist damit aus dem Firmennetz nicht
erreichbar. ~~Die Weboberflaeche liegt unter <http://127.0.0.1:6333/dashboard>.~~
*Ueberholt am 2026-11-26:* `/dashboard` antwortet bei dieser Installation mit 404 (gemessen), neben
`qdrant.exe` liegt kein `static`-Ordner mit der Web-UI; `Start-Qdrant.ps1` nennt sie in seiner Meldung noch.
Auch mit dieser Oberflaeche liesse sich keine Frage in Worten suchen, weil die Frage zuerst mit
demselben Modell eingebettet werden muss. Dafuer gibt es jetzt die Suchseite aus Abschnitt 8.

Zum Einbetten laeuft ein kleines Sprachmodell lokal auf der CPU, rund 220 MB. Das ist keine
Wahlmoeglichkeit, sondern eine Notwendigkeit: Claude kann keine Einbettungen erzeugen, die
Anthropic-API hat dafuer keinen Endpunkt. Und selbst wenn sie einen haette, muesste der
MCP-Server den Suchtext bei jeder Anfrage im eigenen Prozess in einen Vektor umwandeln,
bevor er Qdrant fragt. Er kann dafuer nicht Claude aufrufen, denn Claude ist die Instanz,
die ihn gerade fragt. Ohne lokales Modell gibt es also keine Vektorsuche.

Die Modellwahl ist am 2026-09-03 zweimal korrigiert worden und das ist dokumentiert, weil
beide Fehler wiederkehren koennen:

* `sentence-transformers/all-MiniLM-L6-v2`, die Vorgabe des MCP-Servers, ist rein englisch
  trainiert. Auf die deutsche Frage „Wie wird die Anwendung produktiv deployed?" lieferte
  sie Abschnitte ueber Produktionsdaten statt ueber das Deployment.
* `jinaai/jina-embeddings-v2-base-de` traf fachlich gut, arbeitet aber mit 8192 Token
  Sequenzlaenge. Der Indexlauf belegte 24 GB Arbeitsspeicher und hatte nach 48 Minuten
  CPU-Zeit noch keinen einzigen Punkt geschrieben. Ein Index, dessen Neuaufbau eine Stunde
  dauert, wird nicht nachgefuehrt und veraltet damit sicher.

Gewaehlt ist deshalb `paraphrase-multilingual-MiniLM-L12-v2`: mehrsprachig einschliesslich
Deutsch, 384 Dimensionen, 220 MB, 512 Token Sequenzlaenge. Eingebettet wird in Stapeln von
32 Abschnitten, damit der Speicherbedarf unabhaengig von der Doku-Groesse begrenzt bleibt.

## 3. Taeglicher Gebrauch

Qdrant laeuft nicht automatisch nach dem Anmelden. Nach einem Neustart des Rechners einmal
starten:

```powershell
.\Tools\Qdrant\Start-Qdrant.ps1
```

Laeuft Qdrant nicht, meldet die Suche in der Claude-Sitzung einen Verbindungsfehler. Das ist
der haeufigste Stolperstein.

Im Browser suchen: `.\Tools\Qdrant\Start-Suche.ps1`, dann <http://127.0.0.1:6340/> (Abschnitt 8).

### Optionaler Autostart bei der Anmeldung

Wer den manuellen Start sparen will, richtet eine Aufgabe im Benutzerkontext ein. Auch das
braucht keine Administratorrechte. Der Befehl ist bewusst nicht automatisch ausgefuehrt
worden, weil ein Autostart eine dauerhafte Veraenderung am Arbeitsplatz ist und die
Entscheidung dafuer beim Benutzer liegt:

```powershell
$name = "Qdrant Doku-Suche (TrafagSalesExporter)"
$aktion = New-ScheduledTaskAction -Execute "C:\Users\koi\tools\qdrant\qdrant.exe" -WorkingDirectory "C:\Users\koi\tools\qdrant"
$ausloeser = New-ScheduledTaskTrigger -AtLogOn -User $env:USERNAME
$einstellungen = New-ScheduledTaskSettingsSet -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries -StartWhenAvailable -ExecutionTimeLimit ([TimeSpan]::Zero)
Register-ScheduledTask -TaskName $name -Action $aktion -Trigger $ausloeser -Settings $einstellungen
```

Rueckgaengig machen:

```powershell
Unregister-ScheduledTask -TaskName "Qdrant Doku-Suche (TrafagSalesExporter)" -Confirm:$false
```

## 4. Nach jeder Doku-Aenderung neu indexieren

**Der Index wird nicht automatisch nachgefuehrt.** Ein veralteter Index widerspricht still
den Dateien, die er abbilden soll, und startet die naechste Sitzung mit einer falschen
Auskunft. Das ist genau der Fehler, den Regel 3 in `CLAUDE.md` verhindern soll. Deshalb
gehoert nach jeder inhaltlichen Aenderung an der Dokumentation ein Lauf dazu:

```powershell
.\Tools\Qdrant\Reindex-Docs.ps1
```

Der Lauf ersetzt die Sammlung vollstaendig, damit geloeschte oder umbenannte Dateien nicht
als Leichen im Index bleiben. Er dauert einige Minuten, weil alle Abschnitte neu eingebettet
werden.

**Pruefen statt raten, ob der Index aktuell ist (seit 2026-11-10, Wochen_Todo 118):**

```powershell
.\Tools\Qdrant\Reindex-Docs.ps1 -NurPruefen     # baut nichts; Exitcode 0 = aktuell, 3 = veraltet
.\Tools\Qdrant\Reindex-Docs.ps1 -WennVeraltet   # prueft und baut nur bei Bedarf neu
```

Veraltet heisst: Die Punktzahl der Sammlung weicht von der Abschnittszahl der Dateien ab, oder
seit dem letzten vollstaendigen Lauf ist eine Markdown-Datei geaendert worden (die Meldung nennt
die Dateien), oder es gibt noch keinen Stand eines vollstaendigen Laufs. Der Stand steht in
`<QdrantVerzeichnis>\reindex_stand_<Sammlung>.json` (Beginn des Laufs, Datei-, Abschnitts- und
Punktzahl) und wird nur nach einem vollstaendigen Lauf geschrieben. Ein Wechsel des Git-Zweigs
aendert Dateizeiten und kann deshalb „veraltet“ melden, obwohl der Inhalt gleich ist; dann
einfach neu aufbauen.

**Der Lauf darf umgeleitet werden.** Der Indexer laeuft als eigener Prozess, seine Ausgaben
gehen nach `<QdrantVerzeichnis>\reindex.out.log` und `reindex.err.log`, es zaehlt nur der
Exitcode. Bis zum 2026-11-10 brach das Skript unter PowerShell 5.1 ab, sobald der Aufrufer
`2>&1` oder `*>` anhaengte, weil der fastembed-Hinweis auf stderr dann als Fehler galt
(`docs/rag/LEARNINGS.md`, 2026-10-21 und 2026-10-29). Nach dem Lauf vergleicht das Skript die
Punktzahl in Qdrant mit der Abschnittszahl und bricht bei einer Abweichung mit Fehler ab; ein
halb gefuellter Index wie am 2026-10-27 faellt damit sofort auf.

Aufgenommen werden alle `*.md` des Repositories ausser Build-Ausgaben (`bin`, `obj`,
`node_modules`, `packages`, `TestResults`), lokalen Python-Umgebungen (`.venv`, `venv`,
`site-packages`), Versionsverwaltung und Editorordnern sowie allen Pfaden, die `.tmp_` oder
`ExportPackage` enthalten. Das sind Fremdpakete, Exportpakete und temporaere Sondierungen,
keine Projektdokumentation.

## 5. Warum das Schema exakt passen muss

Das Indexierskript schreibt, der MCP-Server liest. Beide muessen in vier Punkten
uebereinstimmen, sonst liefert `qdrant-find` stillschweigend nichts zurueck, obwohl jeder
einzelne Schritt erfolgreich aussieht:

1. Payload-Schluessel `document` fuer den Text, `metadata` fuer die Zusatzangaben.
2. Benannter Vektor `fast-<modellname in Kleinbuchstaben>`, gebildet wie in
   `FastEmbedProvider.get_vector_name()` des Servers.
3. Distanzmass Cosine und die Dimension des jeweiligen Modells.
4. Dokumente mit `passage_embed`, Suchanfragen mit `query_embed`.

Wird das Modell in `.mcp.json` geaendert, aendert sich auch der Vektorname. Dann muss
`Reindex-Docs.ps1` mit demselben Modell laufen, sonst passt der Index nicht mehr zum Server.
Der Vorgabewert steht an beiden Stellen und muss gemeinsam gepflegt werden.

Ein fuenfter Punkt kommt hinzu, der nicht in der Konfiguration steht: **beide Seiten muessen
dieselbe fastembed-Version verwenden.** `uv` loest die Version bei jedem Lauf neu auf, und
fastembed hat die Berechnung fuer dieses Modell schon einmal geaendert; der Indexlauf warnt
seit 0.6 ausdruecklich, dass statt CLS-Einbettung nun Mittelwertbildung verwendet wird.
Solange Indexierskript und MCP-Server dieselbe Version ziehen, ist das harmlos. Am
2026-09-03 war das beidseitig `0.8.0`, geprueft mit
`uv run --python 3.12 --with fastembed --no-project` gegen beide Umgebungen. Zeigt die Suche
irgendwann nur noch unpassende Treffer, obwohl Qdrant laeuft und Punkte enthaelt, ist das
der erste Verdacht: Versionen vergleichen und neu indexieren.

## 6. Neuinstallation auf einem anderen Rechner

`.mcp.json` und die Skripte enthalten absolute Pfade unter `C:\Users\koi`. Auf einem anderen
Rechner sind diese Pfade anzupassen. Die Einrichtung selbst braucht keine Adminrechte:

1. Qdrant-ZIP `qdrant-x86_64-pc-windows-msvc.zip` aus den GitHub-Releases von `qdrant/qdrant`
   nach `%USERPROFILE%\tools\qdrant` entpacken, daneben `config/config.yaml` mit Bindung an
   `127.0.0.1` anlegen.
2. `uv-x86_64-pc-windows-msvc.zip` aus den Releases von `astral-sh/uv` nach
   `%USERPROFILE%\.local\bin` entpacken.
3. `Start-Qdrant.ps1`, dann `Reindex-Docs.ps1` ausfuehren.

Python muss nicht getrennt installiert werden, `uv` laedt eine eigene Laufzeit. Wichtig ist
die Festlegung auf **Python 3.12**: unter 3.14 gibt es fuer `pydantic-core` noch keine
fertigen Pakete, und der Quellcode-Bau scheitert ohne Visual-Studio-Buildtools, die
ihrerseits Adminrechte braeuchten.

## 7. Bewusste Festlegungen

* **Nur Lesen.** Der MCP-Server laeuft mit `QDRANT_READ_ONLY=true` und bietet deshalb nur
  `qdrant-find` an, nicht `qdrant-store`. Der Index ist damit immer eine Ableitung der
  Markdown-Dateien und nie eine zweite, abweichende Wissensquelle.
* **Kein Dateiwaechter.** Die Neuindexierung ist ein bewusster Schritt beim Abschluss einer
  Aufgabe, kein Hintergrundprozess.
* **Speicher ausserhalb des Repositories.** Der Vektorspeicher waere sonst eine grosse,
  staendig veraenderte Binaerablage im Arbeitsbaum.
* **Suchseite nur lesend und nur lokal.** `suche_server.py` schreibt nichts in Qdrant, bindet an
  `127.0.0.1` und beantwortet nur Aufrufe mit Host `127.0.0.1` oder `localhost`.

## 8. Suche im Browser

Seit 2026-11-26. Die Seite macht dasselbe wie `qdrant-find` in einer Claude-Sitzung: Frage
eintippen, Treffer mit Score, Datei, Abschnitt und Textausschnitt sehen.

```powershell
.\Tools\Qdrant\Start-Suche.ps1
```

Das Skript startet bei Bedarf Qdrant (ueber `Start-Qdrant.ps1`), dann den Suchdienst
`suche_server.py` im Hintergrund, wartet, bis das Modell geladen ist (beim ersten Start rund
15 Sekunden), und gibt die Adresse aus, normalerweise <http://127.0.0.1:6340/>. Ist Port 6340
belegt, nimmt der Dienst den naechsten freien bis 6349. Laeuft er schon, gibt das Skript nur die
Adresse aus. Keine Administratorrechte, kein Autostart; nach einem Neustart des Rechners das
Skript einmal ausfuehren.

| Datei unter `C:\Users\koi\tools\qdrant\` | Inhalt |
|---|---|
| `suche_dienst.json` | Port, PID und Adresse des laufenden Dienstes |
| `suche.out.log`, `suche.err.log` | Ausgaben des Dienstes, im Fehlerprotokoll auch jeder Aufruf |

Beenden: `Stop-Process -Id (Get-Content C:\Users\koi\tools\qdrant\suche_dienst.json | ConvertFrom-Json).pid`

**Was die Seite zeigt.** Oben den Stand des Index: Punktzahl der Sammlung, Zeitpunkt, Datei- und
Abschnittszahl des letzten vollstaendigen Laufs (aus `reindex_stand_trafag_docs.json`) und, wie
`Reindex-Docs.ps1 -NurPruefen`, ob seither Markdown-Dateien geaendert wurden. Dann Suchfeld, Filter
nach Ordner (zum Beispiel nur `docs/rag` oder nur `zlo03`) und Trefferzahl (Vorgabe 10). Ein Klick
auf den Dateinamen kopiert den vollstaendigen Pfad; oeffnen kann der Browser lokale Dateien von
einer `http`-Seite aus nicht. Laeuft Qdrant nicht, steht das samt Startbefehl auf der Seite.

**Schalter „alle Woerter muessen vorkommen“.** Die reine Vektorsuche uebersieht seltene
Fachwoerter. Gemessen am 2026-11-26: Auf „Handbestellwesen Mail an den Einkauf“ liefern
`qdrant-find` und die Seite dieselben Treffer in derselben Reihenfolge, aber
`docs/HANDBESTELLWESEN_KONZEPT_2026-10-22.md` steht nicht unter den ersten 50; vorne stehen
Abschnitte ueber Einkauf und Mails allgemein. Mit dem Schalter muss jedes Wort ab drei Zeichen
(ohne Fuellwoerter wie „den“, „und“) im Abschnitt vorkommen, die Reihenfolge bleibt die
semantische; dann steht das Konzept auf Platz 2 und die Lastchange-Zeile „Handbestellwesen, Mail
an den Einkauf versendet“ auf Platz 3. Auf „ZLO03 offene Mengenkontraktmenge B64344“ trifft schon
die reine Vektorsuche: `zlo03/BEFUND_MKMNG_ABRUFE_2026-11-25.md` auf Platz 1. Vorgabe ist aus,
damit die Seite ohne Haken genau wie `qdrant-find` sucht.

**Schnittstelle.** `GET /api/suche?q=<Frage>&n=10&ordner=docs/rag&woerter=1` liefert JSON mit
`treffer` (Rang, Score, Datei, Pfad, Abschnitt, Text); `ordner=/` heisst nur Dateien der
Repository-Wurzel. `GET /api/stand` liefert den Stand des Index und dient dem Startskript als
Kennung.

**Technik.** Der Dienst laeuft wie der Indexer mit `uv run --python 3.12` und denselben
Abhaengigkeiten (fastembed, qdrant-client); das Startskript versucht es zuerst mit `--offline`.
Die Frage wird mit `query_embed` eingebettet und mit dem benannten Vektor aus Abschnitt 5
gesucht. Der Modellcache ist `C:\Users\koi\tools\qdrant\models`, auch bei einem Direktstart ohne
Startskript. Der Abgleich mit `qdrant-find` (gleiche Treffer, gleiche Reihenfolge) ist der
Beleg, dass Einbettung und Schema zum Index passen; weicht er einmal ab, zuerst die
fastembed-Versionen nach Abschnitt 5 vergleichen. Am 2026-11-26 zog der Dienst fastembed 0.9.1
und qdrant-client 1.20.0 (anzeigen mit `/api/stand`).
