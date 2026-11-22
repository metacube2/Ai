# ccusage installieren und nutzen

Stand: 2026-06-22

## Zweck

`ccusage` ist ein kleines Kommandozeilen-Tool, das die **lokale** Token-Nutzung und die
geschaetzten Kosten von Coding-Agent-CLIs auswertet. Es liest dafuer nur die lokal
gespeicherten Session-Logdateien auf dem eigenen Rechner aus.

Unterstuetzt werden u. a.:

- **Claude Code** (Anthropic)
- **Codex** (OpenAI)
- weitere Quellen wie OpenCode, Amp, Gemini CLI, GitHub Copilot CLI usw.

Projektseite: <https://ccusage.com> · Repo: <https://github.com/ccusage/ccusage>

## Voraussetzung

Node.js muss installiert sein (mit dabei ist `npm`).

Pruefen in PowerShell:

```powershell
node --version
npm --version
```

Wenn nichts kommt: Node.js LTS von <https://nodejs.org> installieren und PowerShell
neu starten. Getestet wurde mit Node `v22.15.0` und npm `11.13.0`.

## Variante A: Global installieren (empfohlen)

Einmalig installieren, danach direkt als `ccusage` aufrufbar:

```powershell
npm install -g ccusage@latest
```

Pruefen:

```powershell
ccusage --version
```

Spaeter aktualisieren:

```powershell
npm update -g ccusage
```

## Variante B: Ohne Installation ausfuehren

Wer nichts global installieren will, kann das Tool direkt per `npx` starten:

```powershell
npx ccusage@latest
```

Beim ersten Lauf laedt `npx` das Paket kurz herunter; danach laeuft es sofort.

## Nutzung

### Alle erkannten Quellen zusammen

```powershell
ccusage            # taeglich (Standard)
ccusage daily      # taeglich
ccusage weekly     # woechentlich
ccusage monthly    # monatlich
ccusage session    # pro Session
ccusage blocks     # Claude 5-Stunden-Abrechnungsfenster
```

### Quellenspezifisch

```powershell
ccusage claude daily     # nur Claude Code
ccusage codex daily      # nur Codex (OpenAI)
ccusage gemini daily     # nur Gemini CLI
```

### Nuetzliche Optionen

```powershell
ccusage daily --since 2026-06-01 --until 2026-06-22   # Zeitraum filtern
ccusage monthly --breakdown                           # Kosten je Modell
ccusage claude daily --instances                      # nach Projekt gruppiert
ccusage daily --json                                  # JSON-Export
ccusage daily --no-cost                               # Kostenspalten ausblenden
ccusage --compact                                     # kompakte Tabelle (Screenshots)
```

## Wichtig: Wie die Kostenangabe zu lesen ist

Die angezeigten Kosten sind eine **rechnerische Schaetzung** auf Basis oeffentlicher
Token-Listenpreise (API / Pay-per-Token).

- Wer ein **Abo/Pauschale** nutzt (z. B. Claude-Code-Abo, ChatGPT/Codex Plus/Pro/Team),
  zahlt real **nicht** diese Summe. Das Tool rechnet die Tokens einfach gegen die
  Listenpreise und kennt das Abo nicht.
- Aussagekraeftig als echte Kosten ist die Zahl nur bei reiner **API-Abrechnung pro Token**.

Fuer alle anderen Faelle ist `ccusage` vor allem nuetzlich, um **Token-Volumen,
Modellverteilung und Nutzungsverlauf** zu sehen, nicht als exakte Rechnung.

## Persoenliches Kostenlimit fuer die KI

In `persona.md` (Abschnitt "Kostenkontrolle (ccusage)") kann jeder Nutzer ein
eigenes Limit eintragen. Ist es aktiviert, prueft die KI beim Sitzungsstart und
an Zwischenpunkten den aktuellen Stand per ccusage und warnt, bevor das Limit
erreicht wird.

Beispiel fuer ein Monats-Token-Limit (sinnvoll fuer Abo-Nutzer):

```text
KOSTENLIMIT_AKTIV: ja
KOSTENLIMIT_ZEITRAUM: monat
KOSTENLIMIT_QUELLE: alle
KOSTENLIMIT_WERT_TOKEN: 50000000
WARNSCHWELLE_PROZENT: 80
```

Hinweis: Diese Persona-Pruefung wirkt nur, solange die KI aktiv ist. Eine echte
zeitgesteuerte automatische Pruefung braucht zusaetzlich einen Hook in
`settings.json` (bei Bedarf von Ingo einrichten lassen).

## Datenschutz / Hinweis

- `ccusage` liest nur **lokale** Logdateien auf dem eigenen Rechner.
- Es sendet keine Daten an Trafag-Systeme.
- Die ausgewerteten Zahlen sind persoenliche Nutzungsdaten des jeweiligen Rechners.
