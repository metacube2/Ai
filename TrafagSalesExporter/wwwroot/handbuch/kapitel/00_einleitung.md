# Einleitung und Wegweiser

Das Trafag Cockpit führt Daten aus SAP, den Exporten der Standorte, den HR-Dateien, dem Shop-Floor-Meeting, dem Netzwerk und öffentlichen Quellen an einer Stelle zusammen. Dieses Handbuch erklärt jede Seite aus betriebswirtschaftlicher Sicht: wozu sie da ist, was die Grafik zeigt, wie die Zahl entsteht und in welcher Arbeitssituation man sie einsetzt. Technische Einzelheiten stehen in der Entwicklerdokumentation, nicht hier.

Das Handbuch beschreibt den Stand der Software vom 14. Oktober 2026; die Grundfassung entstand am 5. Oktober, die Kapitel Trafag Projekte, Trafag Reddit und Operations kamen danach dazu. Am 29. Oktober 2026 sind die Kapitel Einkauf, HR, Trafag Projekte und Operations sowie einzelne Stellen in Finance auf den Stand vom 29. Oktober 2026 berichtigt worden. Es ist im Cockpit unter dem Reiter **Benutzerhandbuch** lesbar und als Word-Datei herunterladbar.

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
| **Einkäufer** | Einkauf Dashboard, Spend-Aufriss (mit dem Inhalt des früheren Reiters Spend), Offene Bestellungen, Kontrakte, Lieferanten, Lieferperformance, Preisentwicklung | Lieferantenverhandlung, Jahresgespräch, Terminverfolgung, Bündelung |
| **Disponent** | Materialdisposition und Fehlteile, Dispositionsprüfung, Verwendung und Risiko (Bestellbedarf und Deckung sowie Materialabhängigkeit sind seit dem 22.10.2026 ausgeblendet) | Fehlteile klären, Abkündigungen, Bestellvorschläge prüfen |
| **Verkauf und Vertriebsleitung** | Verkauf Kunden, Rückgang, Neue und verlorene Kunden, Cross-Selling, Preisstreuung, Weltkarte, Verkauf Interaktiv | Kundenbesuch, Account-Planung, Preisgespräch, Budget |
| **Logistik** | Logistik live (Übersicht, 3D-Lager, Produktivität, Kapazität), Stücklistenanalyse | Tagessteuerung, Schichtplanung, Kapazitätsabgleich mit der Produktion |
| **HR** | HR Dashboard und HR KPI Schulung | Fluktuations- und Absenzenbericht, Quartalsauswertung |
| **IT und Geschäftsleitung** | Netzwerk Übersicht, Verfügbarkeit, Migration, Bericht | Budget für Geräteersatz, Support-Ende, Störungen einordnen |
| **Produktion, PPD und Leitung Operations** | Operations KPI (OTD, OTIF, OTP), Shopfloor mit Pendenzen, Abteilungswerten, Rückständen, Fehlteilen ZD05, Forecast und PPA-Grafik | Tägliches Shop-Floor-Meeting, Wochenplanung, Monatsbericht Liefertreue |

## Die Kopfleiste

Oben rechts stehen für alle Seiten dieselben Bedienelemente. Ganz links zeigt eine kleine Leuchtanzeige im Stil eines Kassettendecks die **CPU-Last**: der obere Balken **S** die Last des ganzen Servers, der untere Balken **C** den Anteil des Cockpits selbst, je von 0 bis 100 %; ein schwach leuchtendes Segment hält kurz die letzte Spitze fest. Sie hilft einzuordnen, ob eine langsame Seite am Server oder an der eigenen Verbindung liegt. Daneben folgen der Dimmer für die Helligkeit, die Statusampel mit den letzten Meldungen, die Umschaltung des Designs, Hell/Dunkel und die Sprache.

## Allgemeine Regeln für alle Zahlen

Beträge sind in CHF, sofern nichts anderes steht. Fremdwährungen werden mit dem Kurs umgerechnet, den die jeweilige Seite nennt. Konzerninterne Umsätze und Bestellungen zwischen Trafag-Gesellschaften sind in Verkauf und Weltlage ausgeschlossen, damit die Gruppe nicht mit sich selbst rechnet.

Zeiträume enden in der Regel mit dem letzten vollständigen Monat. Ein angebrochener Monat würde Vergleiche mit dem Vorjahr verzerren.

Die Daten werden automatisch nachgeladen, je nach Quelle laufend, stündlich oder nachts. Jede Seite nennt den Stand ihrer Daten. Wer eine Zahl weitergibt, nennt am besten diesen Stand mit.

Fällt eine Zahl auf, ist zuerst zu prüfen, ob die Daten vollständig sind (Datenstatus, Daten-Heartbeat, Datenqualität), bevor man fachliche Schlüsse zieht.

Fragen und Fehlermeldungen zum Cockpit gehen an Ingo Kohler, SAP Specialist.
