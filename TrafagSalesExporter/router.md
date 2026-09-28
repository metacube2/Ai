# Router — globaler Einstieg

Stand: 2026-09-11

Dies ist der **einzige** globale Einstieg in die Dokumentation. Von hier fuehrt genau ein
Schritt in einen Themenast, von dort genau ein Schritt in die Detaildatei.

**Ladeweg fuer eine Aufgabe: `router.md` -> ein Unterrouter -> eine Detaildatei.**
Mehr soll nicht noetig sein. Wer den ganzen Bestand sucht, nimmt `baum.md` — das ist ein
Vollstaendigkeitsindex zur Pruefung, keine Lesereihenfolge.

Ist dagegen unklar, in welcher Datei eine Aussage ueberhaupt steht, hilft vorab das
MCP-Werkzeug `qdrant-find`: eine semantische Suche ueber alle Markdown-Dateien,
eingerichtet in `Tools/Qdrant/`. Sie nennt Datei und Abschnitt und ersetzt weder diesen
Ladeweg noch das Lesen der Datei, denn ihre Treffer sind Ausschnitte aus dem letzten
Indexlauf. Antwortet sie gar nicht, laeuft Qdrant nicht; `Tools/Qdrant/Start-Qdrant.ps1`
startet es.

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

   **Wer SAP bedienen soll, liest vorher `saptasks/SAP_ARBEITSWEISE_UND_WERKZEUGE.md`.**
   Dort steht, was ein Agent im System selbst tun kann — lesen, schreiben, aktivieren,
   Reports ausfuehren, OData pruefen — und welche Fallen dabei Stunden kosten. Ohne
   diese Datei faengt jede SAP-Aufgabe wieder bei der Frage an, ob Fernsteuerung
   ueberhaupt geht. Sie geht, und zwar ohne einen einzigen Klick.
6. **Vor jeder Aenderung, parallelen Arbeit, jedem Build und Deploy:**
   `docs/AGENT_COORDINATION.md` lesen, den eigenen Bereich eintragen und beim Abschluss
   mit Status und Nachweis aktualisieren. Eintraege mit `abgeschlossen`, `deployed`,
   `frei` oder `Historie` sind keine laufende Arbeit. Die Datei fuehrt seit dem
   2026-09-09 nur laufende Arbeit und einen Kurzindex der letzten sieben Tage; der
   Volltext des Abgeschlossenen steht in `docs/AGENT_COORDINATION_HISTORIE.md` und ist
   Nachschlagewerk, nicht Pflichtlektuere. Eine reine Auskunft ohne jede Aenderung
   braucht diesen Schritt nicht, siehe `CLAUDE.md` Abschnitt 3a.
7. **Arbeitsregeln, Tests und fachliche Grenzen:** `persona.md`.
8. **Ingo testet immer auf dem deployten Produktivstand, nie lokal.** Eine leere oder
   abweichende lokale `trafag_exporter.db` oder ein lokal gestarteter Dev-Server sind kein
   Beleg fuer den produktiven Zustand. Bei UI-/Datenfragen den produktiven Stand pruefen
   (Browser, Admin-Logs, `docs/rag/DEPLOYMENT.md`), nicht die lokale Kopie im Repo.

## Themenaeste

| Ast | Wofuer | Unterrouter |
| --- | --- | --- |
| **Finance** | Finance Cockpit, Soll/Ist, Formeln, Marge, Standardkosten, Supplier, Journal, Marktsegmente | `docs/router/finance.md` |
| **Standortdaten** | Exporte und Importe je Land (ES, DE, UK, IT, IN, CH/AT), Feldluecken, Ansprechpartner | `docs/router/standortdaten.md` |
| **Einkauf** | Spend, Bestellungen, Kontrakte, Supply Chain, Logistik, Produktgruppen, ABC/XYZ | `docs/router/einkauf.md` |
| **HR** | HR-KPI-Cockpit, Fluktuation, Absenzen | `docs/router/hr.md` |
| **Plattform** | Architektur, Deployment, Admin, Requirements, Werkzeuge, Serveranalyse, Outlook-Grenzen, Arbeitsplatzleistung und WLAN, Tempo der Webapp | `docs/router/plattform.md` |
| **SAP** | **SAP selbst bedienen (Werkzeuge, Grenzen, Fallen)**, ABAP, OData/Gateway, ZLO03, ZZPRDAT, PPWR, Produktsparten, SAP-Kalkulation | `docs/router/sap.md` |
| **Projekt** | Agentenkoordination, Projektstatus, Roadmap, Arbeitsregeln, Aenderungsstand | `docs/router/projekt.md` |
| **Wo stehen wir gerade?** Uebergabestand mit offenen Punkten und Reihenfolge | `docs/UEBERGABE_2026-09-08.md` |

## Wenn der Ast nicht klar ist

| Frage | Ast |
| --- | --- |
| „Stimmt diese Zahl im Dashboard?" | Finance |
| „Warum fehlt Feld X bei Land Y?" | Standortdaten |
| „Woran arbeite ich gerade, was ist noch offen?" | Projekt |
| „Wie bringe ich das auf den Server?" | Plattform |
| „Warum ist mein Arbeitsplatz oder die Anzeige zaeh?" | Plattform |
| „Warum ist die Webapp langsam oder zeigt lange nichts?" | Plattform, `docs/PLATTFORM_TEMPO_2026-09-28.md` |
| „Was liefert SAP und wie?" | SAP, bei Verkaufszahlen Standortdaten |
| „Kann ich in SAP selbst etwas anlegen, aendern oder messen?" | **Ja.** `saptasks/SAP_ARBEITSWEISE_UND_WERKZEUGE.md`, ohne Umweg ueber einen Unterrouter |

Hilft auch das nicht weiter, ist `qdrant-find` aus dem Kopf dieser Datei der naechste
Schritt.

Vollstaendiger Dateibestand mit Einordnung: `baum.md`.
