# Lokale Vektorsuche ueber die Projektdokumentation

Stand: 2026-09-03

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

Der Dienst bindet ausschliesslich an `127.0.0.1` und ist damit aus dem Firmennetz nicht
erreichbar. Die Weboberflaeche liegt unter <http://127.0.0.1:6333/dashboard>.

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

Aufgenommen werden alle `*.md` des Repositories ausser Build-Ausgaben (`bin`, `obj`,
`node_modules`, `packages`, `TestResults`), Versionsverwaltung und Editorordnern sowie allen
Pfaden, die `.tmp_` oder `ExportPackage` enthalten. Das sind Exportpakete und temporaere
Sondierungen, keine Projektdokumentation.

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
