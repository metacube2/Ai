# Einleitung und Wegweiser

Das Trafag Cockpit führt Daten aus SAP, den Exporten der Standorte, den HR-Dateien, dem Netzwerk und öffentlichen Quellen an einer Stelle zusammen. Dieses Handbuch erklärt jede Seite aus betriebswirtschaftlicher Sicht: wozu sie da ist, was die Grafik zeigt, wie die Zahl entsteht und in welcher Arbeitssituation man sie einsetzt. Technische Einzelheiten stehen in der Entwicklerdokumentation, nicht hier.

Das Handbuch beschreibt den Stand der Software vom 5. Oktober 2026. Es ist im Cockpit unter dem Reiter **Benutzerhandbuch** lesbar und als Word-Datei herunterladbar.

## So ist jede Seite beschrieben

Jede Seite folgt demselben Aufbau, damit man schnell findet, was man sucht.

- **Wozu:** die betriebswirtschaftliche Frage, die die Seite beantwortet.
- **Was man sieht:** Kennzahlen, Grafiken und Bedienelemente.
- **Wie gerechnet wird:** Datenquelle, Filter und Formel in Worten.
- **Einsatz im Arbeitsalltag:** typische Situationen je Rolle, mit den Fragen, die die Seite beantwortet.
- **Grenzen und Fallstricke:** was die Zahl nicht aussagt und wo man vorsichtig sein muss.

Am Ende jedes Kapitels steht ein Kennzahlen-Glossar.

## Wegweiser nach Rolle

| Rolle | Wichtigste Seiten | Typische Anlässe |
|---|---|---|
| **Controller** | Finance Cockpit (Soll/Ist Vergleich, Controlling mit Umsatzbrücke und Hochrechnung), Management Schnellübersicht, Gruppenmarge, Datenqualität, Abweichungen | Monatsabschluss, Hochrechnung, Abweichungsanalyse, Vorbereitung Verwaltungsrat |
| **Geschäftsleitung** | Management Schnellübersicht und Entscheidungen, Controlling, Weltlage Radar, Verkauf Konzentration und Prognose | Monatsgespräch, Strategie, Risikobeurteilung |
| **Einkäufer** | Einkauf Dashboard, Spend, Offene Bestellungen, Kontrakte, Lieferanten, Lieferperformance, Preisentwicklung, Einkauf Interaktiv | Lieferantenverhandlung, Jahresgespräch, Terminverfolgung, Bündelung |
| **Disponent** | Bestellbedarf und Deckung, Materialabhängigkeit, Materialdisposition und Fehlteile, Dispositionsprüfung, Verwendung und Risiko | Fehlteile klären, Abkündigungen, Bestellvorschläge prüfen |
| **Verkauf und Vertriebsleitung** | Verkauf Kunden, Rückgang, Neue und verlorene Kunden, Cross-Selling, Preisstreuung, Weltkarte, Verkauf Interaktiv | Kundenbesuch, Account-Planung, Preisgespräch, Budget |
| **Logistik** | Logistik live (Übersicht, 3D-Lager, Produktivität, Kapazität), Stücklistenanalyse | Tagessteuerung, Schichtplanung, Kapazitätsabgleich mit der Produktion |
| **HR** | HR Dashboard und HR KPI Schulung | Fluktuations- und Absenzenbericht, Quartalsauswertung |
| **IT und Geschäftsleitung** | Netzwerk Übersicht, Verfügbarkeit, Migration, Bericht | Budget für Geräteersatz, Support-Ende, Störungen einordnen |

## Allgemeine Regeln für alle Zahlen

Beträge sind in CHF, sofern nichts anderes steht. Fremdwährungen werden mit dem Kurs umgerechnet, den die jeweilige Seite nennt. Konzerninterne Umsätze und Bestellungen zwischen Trafag-Gesellschaften sind in Verkauf und Weltlage ausgeschlossen, damit die Gruppe nicht mit sich selbst rechnet.

Zeiträume enden in der Regel mit dem letzten vollständigen Monat. Ein angebrochener Monat würde Vergleiche mit dem Vorjahr verzerren.

Die Daten werden automatisch nachgeladen, je nach Quelle laufend, stündlich oder nachts. Jede Seite nennt den Stand ihrer Daten. Wer eine Zahl weitergibt, nennt am besten diesen Stand mit.

Fällt eine Zahl auf, ist zuerst zu prüfen, ob die Daten vollständig sind (Datenstatus, Daten-Heartbeat, Datenqualität), bevor man fachliche Schlüsse zieht.

Fragen und Fehlermeldungen zum Cockpit gehen an Ingo Kohler, SAP Specialist.
