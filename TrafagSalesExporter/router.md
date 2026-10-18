# Router — globaler Einstieg

Stand: 2026-08-17

Dies ist der **einzige** globale Einstieg in die Dokumentation. Von hier fuehrt genau ein
Schritt in einen Themenast, von dort genau ein Schritt in die Detaildatei.

**Ladeweg fuer eine Aufgabe: `router.md` -> ein Unterrouter -> eine Detaildatei.**
Mehr soll nicht noetig sein. Wer den ganzen Bestand sucht, nimmt `baum.md` — das ist ein
Vollstaendigkeitsindex zur Pruefung, keine Lesereihenfolge.

## Vorrangregeln

Diese Regeln gelten vor jeder Detaildatei. Sie sind aus echten Fehlern entstanden.

1. **Direkt gepruefte Live-Fakten schlagen jede Notiz.** Eine datierte Arbeitsnotiz ist
   ein Beleg fuer den Tag ihrer Messung, nicht fuer heute.
2. **Statusfragen („ist X noch offen?") nie aus einer Arbeitsnotiz beantworten.** Gueltig
   ist `docs/Issue_Log_Konsolidiert_2026-08-12.tsv`. Am 2026-08-12 waren zwei Punkte in
   Markdown-Dateien als offen gefuehrt, die produktiv laengst erledigt waren, und ein
   hoher Punkt fehlte ganz.
3. **Bevor ein Standort um Daten oder Pflege gebeten wird: pruefen, ob die Information
   schon vorliegt oder unsere eigene Export-SQL sie nur nicht liest.** Die Queries in
   `AlphaplanExportPackage/` und `SageSpainExportPackage/` sind unsere. Das ist zweimal in
   einer Woche schiefgegangen (DE 2026-08-03, IN 2026-08-05). Ein Standort, der
   ueberfluessige Pflege geliefert bekommt, nimmt die naechste Bitte nicht mehr ernst.
4. **Fuellgrade nie mit `Spalte > 0` messen.** `StandardCost` und `PostingDate` sind
   TEXT-Spalten; in SQLite ist Text groesser als jede Zahl, das ergibt falsche 100 %.
   `CAST(... AS REAL)` verwenden und die Grundgesamtheit fachlich filtern.
5. **SAP- und HANA-Fakten nie aus Erinnerung ableiten.** Live-Werkzeuge verwenden und das
   Ergebnis nachdokumentieren. Keine Tabellen- oder Feldnamen erfinden — genau dieser
   Fehler hat bei UK-2025 und beim IT-Superlativ zugeschlagen.
6. **Vor jeder Aenderung, parallelen Arbeit, jedem Build und Deploy:**
   `docs/AGENT_COORDINATION.md` lesen, den eigenen Bereich eintragen und beim Abschluss
   mit Status und Nachweis aktualisieren. Eintraege mit `abgeschlossen`, `deployed`,
   `frei` oder `Historie` sind keine laufende Arbeit.
7. **Arbeitsregeln, Tests und fachliche Grenzen:** `persona.md`.

## Themenaeste

| Ast | Wofuer | Unterrouter |
| --- | --- | --- |
| **Finance** | Finance Cockpit, Soll/Ist, Formeln, Marge, Standardkosten, Supplier, Journal, Marktsegmente | `docs/router/finance.md` |
| **Standortdaten** | Exporte und Importe je Land (ES, DE, UK, IT, IN, CH/AT), Feldluecken, Ansprechpartner | `docs/router/standortdaten.md` |
| **Einkauf** | Spend, Bestellungen, Kontrakte, Supply Chain, Logistik, Produktgruppen, ABC/XYZ | `docs/router/einkauf.md` |
| **HR** | HR-KPI-Cockpit, Fluktuation, Absenzen | `docs/router/hr.md` |
| **Plattform** | Architektur, Deployment, Admin, Requirements, Werkzeuge, Serveranalyse | `docs/router/plattform.md` |
| **SAP** | ABAP, ZLO03, ZZPRDAT, PPWR, Produktsparten, SAP-Kalkulation | `docs/router/sap.md` |
| **Projekt** | Agentenkoordination, Projektstatus, Roadmap, Arbeitsregeln, Aenderungsstand | `docs/router/projekt.md` |

## Wenn der Ast nicht klar ist

| Frage | Ast |
| --- | --- |
| „Stimmt diese Zahl im Dashboard?" | Finance |
| „Warum fehlt Feld X bei Land Y?" | Standortdaten |
| „Woran arbeite ich gerade, was ist noch offen?" | Projekt |
| „Wie bringe ich das auf den Server?" | Plattform |
| „Was liefert SAP und wie?" | SAP, bei Verkaufszahlen Standortdaten |

Vollstaendiger Dateibestand mit Einordnung: `baum.md`.
