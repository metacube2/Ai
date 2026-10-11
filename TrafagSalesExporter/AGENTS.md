# Verbindliche Arbeitsregeln fuer Codex und andere Agenten

Diese Datei gilt fuer das gesamte Repository.

## 1. Einstieg in die Dokumentation

**`router.md` in der Repository-Wurzel ist der einzige globale Einstieg.**

Ladeweg fuer eine Aufgabe: `router.md` -> ein Unterrouter unter `docs/router/` -> die
Detaildatei. Mehr soll nicht noetig sein. Nicht wahllos Dateien aus `docs/` lesen.

`baum.md` listet den vollstaendigen Bestand und dient der Vollstaendigkeitspruefung, nicht
dem Einlesen in eine Aufgabe.

Direkt gepruefte Live-Fakten haben Vorrang vor historischen Notizen. Die uebrigen
Vorrangregeln stehen in `router.md`.

## 2. Agentenkoordination ist Pflicht

Vor jeder Analyse, Dateiaenderung, Codeaenderung, Testausfuehrung, App-Startaktion oder
jedem Deployment zuerst `docs/AGENT_COORDINATION.md` vollstaendig lesen.

Danach:

1. Pruefen, ob ein anderer Agent den Bereich oder gemeinsame Dateien reserviert hat.
2. Vor eigener Arbeit den eigenen Auftrag, die betroffenen Dateien und den Status dort
   eintragen.
3. Keine reservierten oder fremd geaenderten Dateien ohne Abstimmung bearbeiten.
4. Beim Abschluss Ergebnis, geaenderte Dateien, Tests und Deploystatus nachtragen und die
   Reservierung freigeben.
5. Eintraege mit `abgeschlossen`, `deployed`, `frei` oder `Historie` sind keine aktuell
   laufende Arbeit.

Diese Schritte duerfen auch dann nicht uebersprungen werden, wenn der Auftrag bereits in
einem Chat beschrieben wurde.

## 3. Arbeitsweise

Arbeitsregeln, Testerwartungen und fachliche Grenzen stehen in `persona.md`.

Bereichsspezifische `AGENTS.md`-Dateien gelten zusaetzlich zu dieser Root-Datei.
