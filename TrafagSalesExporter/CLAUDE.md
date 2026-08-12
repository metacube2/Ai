# Verbindliche Arbeitsregeln fuer Claude

Diese Datei gilt fuer Arbeiten im gesamten Repository.

## Agentenkoordination ist Pflicht

Vor jeder Analyse, Dateianderung, Codeanderung, Testausfuehrung, App-Startaktion
oder jedem Deployment zuerst `docs/AGENT_COORDINATION.md` vollstaendig lesen.

Danach:

1. Pruefen, ob ein anderer Agent den Bereich oder gemeinsame Dateien reserviert
   hat.
2. Vor eigener Arbeit den eigenen Auftrag, die betroffenen Dateien und den Status
   in `docs/AGENT_COORDINATION.md` eintragen.
3. Keine reservierten oder fremd geaenderten Dateien ohne Abstimmung bearbeiten.
4. Beim Abschluss Ergebnis, geaenderte Dateien, Tests und Deploystatus in
   `docs/AGENT_COORDINATION.md` nachtragen und die Reservierung wieder freigeben.
5. Eintraege mit `abgeschlossen`, `deployed`, `frei` oder `Historie` sind keine
   aktuell laufenden Agentenarbeiten.

Diese Schritte duerfen auch dann nicht uebersprungen werden, wenn der Auftrag
bereits in einem Chat beschrieben wurde.

## Kontextnavigation

Nach der Koordinationspruefung `docs/RAG_ROUTER.md` lesen und nur die dort fuer
das Thema genannten Kurz- und Detaildateien laden. Direkt gepruefte Live-Fakten
haben Vorrang vor historischen Notizen.

Bereichsspezifische `CLAUDE.md`-Dateien, zum Beispiel `zlo03/CLAUDE.md`, gelten
zusaetzlich zu dieser Root-Datei.
