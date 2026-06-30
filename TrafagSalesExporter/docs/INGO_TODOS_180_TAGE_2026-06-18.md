# 180-Tage-Todos Ingo - Analytics, BI und .NET

Stand: 18.06.2026  
Zeitraum: 18.06.2026 bis ca. 15.12.2026

## Zweck

Dieses Dokument ergaenzt die Management- und IT-Notfall-Dokumentation um die konkreten Aufgaben von Ingo fuer die naechsten 180 Tage. Es fokussiert auf Analytics, BI-Dashboards, Datenintegration, Report-/Z-Funktionen sowie .NET- und ASP-Webseiten.

## Abgrenzung

### In Scope fuer Ingo

- Sales Management Cockpit / Data-Lake: Extraktion und Konsolidierung der ERP-Daten aus den Tochtergesellschaften.
- Finance- und Sales-Reporting: Harmonisierung der Kennzahlen, Auditierbarkeit und Nachweisfuehrung.
- HR Dashboard: Analyse und Visualisierung der REXX- und SAP-Daten.
- Einkaufs Dashboard: Kennzahlen fuer Einkauf, Lieferanten, Bestellvolumen, offene Bestellungen und Disposition.
- BI-Plattform und Analytic Website: Betrieb, Weiterentwicklung, Deployment und Datenqualitaet.
- Pflege von Report-/Z-Funktionen, soweit sie fuer Analytics, Reporting oder .NET/ASP-Webseiten benoetigt werden.

### Nicht in Scope fuer Ingo

- S/4HANA Compatibility Check, RPC-/RFC-Abschaltungen und die ca. 30 betroffenen SAP-Applikationen liegen gemaess aktueller Zuordnung bei Lucas.
- SAP Business One Server- und Applikationsprojekt liegt bei Lucas, Upgreat, NTT bzw. ANG.
- Netzwerk-, Server-, Security- und Infrastruktur-Erneuerungen liegen bei Alex, Ramon, Upgreat und den jeweiligen Infrastrukturpartnern.

## Executive Summary

Die hoechste Prioritaet fuer die naechsten 180 Tage ist das Sales Management Cockpit / Data-Lake. Der Schwerpunkt liegt auf dem Onboarding von Spanien, der Vereinheitlichung der Laenderlogik und der auditierbaren Konsolidierung in einer gemeinsamen Gold-Schicht.

Die zweite Prioritaet sind HR Dashboard und Einkaufs Dashboard. Beim HR Dashboard steht die Abnahme von Phase 1 im Vordergrund. Beim Einkaufs Dashboard geht es um ein belastbares MVP mit Excel-aehnlicher Kaskadierungstabelle, Lieferantennamen, Jahres-Spend, offenen Bestellungen, Filterung von geloeschten Positionen und Materialstatus.

Quer ueber alle Themen bleibt die Reduktion des Know-how-Risikos wichtig: technische Dokumentation, nachvollziehbare Nachweise, klare Deployments, Datenqualitaetspruefungen und einfache Wiederanlaufprozesse.

## Prioritaet 1: Sales Management Cockpit / Data-Lake

### Q3 2026 - bis Ende September

1. Spain-Onboarding abschliessen
   - Sage 200 / SQL Server Datenquelle mit Santi Gomez stabil anbinden.
   - Bronze-Layer-Extraktion stabilisieren.
   - Silver-Transformation fuer Spanien aufsetzen.
   - Spanisches Datenmodell an die bestehenden Tochtergesellschaften angleichen.
   - Referenzwerte mit Finance abstimmen, insbesondere Umsatz- und Margenwerte fuer 2025.

2. Laenderlogik und Finance-Regeln finalisieren
   - Country-specific Formeln fuer IT, FR, IN, US, AT, ES, UK und DE fachlich klaeren.
   - Regeln in der Finance-/Sales-Logik vereinheitlichen.
   - Nachweisfuehrung ueber Audit-CSV, Finance Dashboard Nachweis und zentrale Excel-Pruefungen absichern.
   - Unterschiede zwischen Quell-ERP, Export, Transformation und Dashboard dokumentieren.

3. Produkt- und Spartenzuordnung stabilisieren
   - ProductDivisionRefSet / ZSCHWEIZ Mapping weiter pruefen.
   - Guardrails fuer fehlende oder widerspruechliche Produktzuordnungen aktiv halten.
   - Lokale Materialnummern, insbesondere DE/Alphaplan, in die Zuordnungslogik aufnehmen.
   - Restklasse "Uebrige" bzw. 0008 bewusst behandeln und dokumentieren.

4. ZLO03 / ZPOWERBI_VC_TXT Non-Determinismus klaeren
   - SELECT SINGLE ohne ORDER BY als Risiko fuer nicht reproduzierbare Zahlen bewerten.
   - Falls fuer das Cockpit relevant: deterministische Logik abstimmen und dokumentieren.
   - Technische Aenderungen an SAP-Objekten bleiben bei Lucas bzw. SAP-Verantwortlichen; Ingo liefert fachlichen Reporting-Impact und Testnachweise.

### Q4 2026 - Oktober bis Mitte Dezember

1. Cross-Subsidiary-Dashboard finalisieren
   - Alle relevanten Laender in einer konsolidierten Gold-Quelle zusammenfuehren.
   - Dashboard fuer gruppenweite Sales- und Finance-Sicht finalisieren.
   - Power BI bzw. Analytic Website so ausrichten, dass Monats- und Quartalsreports reproduzierbar sind.

2. Data-Quality- und Reconciliation-Prozess etablieren
   - Soll-/Ist-Abgleich pro Tochtergesellschaft definieren.
   - Monatliche Pruefpunkte fuer Umsatz, Marge, Produktgruppe, Land und Waehrung aufbauen.
   - Audit-CSV und Finance Dashboard Nachweis als wiederholbaren Review-Prozess verwenden.
   - Abweichungen mit klarer Ursache, Owner und Korrekturstatus dokumentieren.

3. Refresh- und Performance-Tuning
   - Refresh-Zeiten der Pipelines und Dashboards messen.
   - Engpaesse vor dem Jahresend-Reporting beseitigen.
   - Stabilitaet der produktiven Deployments pruefen, inklusive App-Offline-Prozess und Datenbank-Sidecars.

## Prioritaet 2: HR Dashboard

### Q3 2026 - bis Ende September

1. Phase 1 mit Sonja final abnehmen
   - REXX- und SAP-Datenstatus pruefen.
   - HR-KPI-Berechnungen gegen fachliche Erwartung validieren.
   - Management-Ansicht mit Anonymisierung final pruefen.
   - Rollen und Zugriff fuer HR Admins bestaetigen.

2. Betriebsfaehigkeit absichern
   - Datenordner, Aktualitaetsanzeige und Fehlermeldungen dokumentieren.
   - Import- und Refresh-Prozess kurz beschreiben.
   - Bekannte Datenluecken und manuelle Schritte festhalten.

### Q4 2026 - Oktober bis Mitte Dezember

1. HR Phase 2 scopen
   - Feedback aus der Phase-1-Abnahme aufnehmen.
   - Erweiterungen priorisieren.
   - Aufwand und Abhaengigkeiten fuer 2027 vorbereiten.

## Prioritaet 3: Einkaufs Dashboard

### Q3 2026 - bis Ende September

1. KPI-Scope mit Einkauf festlegen
   - Spend pro Jahr und Lieferant.
   - Bereits beschafft bzw. gebucht.
   - Offene Bestellungen und Zulauf.
   - Aktive Lieferanten auf Basis echter Einkaufsbewegungen.
   - Filter fuer Loeschkennzeichen und Materialstatus MARA-MSTAE.

2. Excel-aehnliche Kaskadierungstabelle umsetzen und fachlich pruefen
   - Lieferant in den Zeilen.
   - Jahre 2020 bis aktuelles Jahr in den Spalten.
   - Summen und Sortierung Top-down.
   - Lieferantennamen statt Platzhalter wie "Lieferant A".
   - Referenz gegen vorhandene Power-BI-/Excel-Sicht pruefen.

3. Datenquellen und Feldmapping klaeren
   - EKKO, EKPO und EKET als Basis fuer Bestellungen, Positionen und Einteilungen.
   - Lieferantennamen ueber belastbare Quelle ergaenzen, z.B. SAP-Data-Service, LFA1 oder vorhandenes Mapping.
   - MARA-MSTAE bzw. Materialstatus in die Einkaufslogik aufnehmen, sofern Quelle verfuegbar ist.

### Q4 2026 - Oktober bis Mitte Dezember

1. Einkaufs Dashboard MVP ausliefern
   - Spend-Analyse pro Lieferant fuer das aktuelle Jahr.
   - Balkenansicht fuer gebuchten/beschafften Wert und offene Bestellungen.
   - Offene Verpflichtungen sinnvoll darstellen, vorzugsweise als Zukunfts-/Faelligkeitssicht.
   - Lieferanten- und Artikel-Sicht fuer offene Dispositionen bereitstellen.

2. Datenqualitaet und Plausibilitaet absichern
   - Geloeschte Positionen standardmaessig herausfiltern.
   - Materialstatus als Filter sichtbar machen.
   - Aktive Lieferanten nachvollziehbar definieren.
   - Auffaellige Werte mit Einkauf pruefen, insbesondere wenn Summen oder Lieferantenanzahl unplausibel wirken.

## Querschnittsthemen

1. Know-how-Risiko reduzieren
   - Architektur, Datenquellen, Mapping-Regeln und Deployment-Schritte dokumentieren.
   - RAG-/Handoff-Dokumente aktuell halten.
   - Kritische Report- und Z-Funktionen mit fachlichem Zweck und technischer Abhaengigkeit beschreiben.
   - Wiederanlauf nach Server- oder Deployment-Problemen kurz dokumentieren.

2. Betrieb der Analytic Website sichern
   - Deployments versionieren und committen.
   - Tests vor produktiven Deployments ausfuehren.
   - App-Offline-Prozess beim Publish verwenden.
   - Produktive Datenbanken und Konfigurationen nicht durch Publish-Artefakte ueberschreiben.

3. Stakeholder-Kommunikation
   - Monatsweise Status an Andreas bzw. betroffene Fachbereiche geben.
   - Offene fachliche Entscheidungen sichtbar halten.
   - Quartalsende als natuerliche Meilenstein- und Abnahmezeitpunkte verwenden.

## Abhaengigkeiten

- Finance / Andreas: Budget- und Margenlogik, Nachweise, Gruppenreporting, Review der Laenderformeln.
- Einkauf: KPI-Definitionen, Plausibilisierung von Lieferanten, Spend und offenen Bestellungen.
- HR / Sonja: HR-Fachabnahme und Phase-2-Priorisierung.
- Lucas / SAP-Team: SAP-Objekte, Z-Funktionen, Produktmapping, S/4-Themen und fachliche SAP-Abhaengigkeiten.
- Upgreat / Infrastruktur: Server, Zugriff, Netzwerk, SharePoint, Betrieb und Security-Rahmen.
- Tochtergesellschaften: Bereitstellung und Validierung der lokalen ERP-Exporte.

## Risiken und Gegenmassnahmen

| Risiko | Auswirkung | Gegenmassnahme |
| --- | --- | --- |
| Know-how liegt stark bei Einzelpersonen | Ausfall oder Wechsel erschwert Betrieb und Weiterentwicklung | Dokumentation, Handoff, Source Control, klare Runbooks |
| Uneinheitliche Laenderlogik | Gruppenzahlen sind nicht vergleichbar | Regeln fachlich abnehmen, Audit-CSV und Nachweis nutzen |
| Fehlende Lieferantennamen oder Materialstatus | Einkaufsdashboard wirkt unprofessionell oder falsch | SAP-Quelle fuer Lieferantenstamm und MARA-MSTAE klaeren |
| Nicht reproduzierbare SAP-Abfragen | Dashboard-Zahlen schwanken ohne fachlichen Grund | Deterministische Query-Logik pruefen und SAP-Owner einbinden |
| Deployments ueberschreiben produktive Daten | Produktionsausfall oder Datenverlust | Publish-Prozess mit Tests, App-Offline und Datenbankschutz |
| Zu viele parallele Themen | Verzögerung bei Kernzielen | Sales/Data-Lake priorisieren, HR/Einkauf als klare MVPs schneiden |

## Konkrete naechste 10 Schritte

1. Spain-Onboarding-Status mit Santi Gomez und Finance festziehen.
2. Offene Laenderformeln fuer IT, FR, IN, US, AT, ES, UK und DE in einer Entscheidungsliste abschliessen.
3. Finance-Audit-Paket fuer Monats-/Quartalsreview definieren.
4. HR-Phase-1-Abnahme mit Sonja terminieren und Abnahmeliste vorbereiten.
5. Einkaufs-KPI-Scope mit Einkauf final bestaetigen.
6. Quelle fuer Lieferantennamen und MARA-MSTAE im Einkaufsdashboard klaeren.
7. Einkaufs-Matrix 2020 bis aktuelles Jahr gegen Referenzbild und Einkauf plausibilisieren.
8. Refresh- und Performance-Baseline fuer die produktive Analytic Website messen.
9. Runbook fuer Deployment, Datenrefresh und Wiederanlauf aktualisieren.
10. Ende Q3 2026 einen Review mit Andreas zu Sales/Data-Lake, HR und Einkauf durchfuehren.

## Kurzfassung fuer Management-Dokument

Ingo verantwortet in den naechsten 180 Tagen schwerpunktmaessig das Sales Management Cockpit / Data-Lake sowie HR- und Einkaufsdashboard. Prioritaet 1 ist die stabile, auditierbare Konsolidierung der Sales- und Finance-Daten aus den Tochtergesellschaften, inklusive Spanien-Onboarding, Harmonisierung der Laenderlogik und Reconciliation-Prozess. Prioritaet 2 ist die Abnahme des HR Dashboards Phase 1 und die Lieferung eines Einkaufsdashboard-MVPs mit Lieferanten-, Spend-, offenen Bestellungen- und Materialstatus-Sicht. Parallel werden Dokumentation, Nachweise, Deployments und Wiederanlaufprozesse verbessert, um das Know-how-Risiko in Analytics, BI und .NET/ASP-Betrieb zu reduzieren.
