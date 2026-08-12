# Einkauf-Lokalisierung und einfache Projektsuite

Stand: 2026-08-01

Diese Datei beschreibt den aktuell gueltigen Umsetzungsstand der neuen
Oberflaechensprachen im Einkauf sowie der untersten Hauptnavigation
`Poor Man's Project Management Suite`. Bei spaeteren Abweichungen haben Code,
automatisierte Tests und ein neuerer direkt gepruefter Produktivstand Vorrang.

## Unterstuetzte Oberflaechensprachen

Die Anwendung bietet folgende Sprachcodes:

| Code | Sprache |
| --- | --- |
| `de` | Deutsch |
| `en` | Englisch |
| `es` | Spanisch |
| `it` | Italienisch |
| `hi` | Hindi |
| `sq` | Albanisch / Shqip |
| `tr` | Tuerkisch |
| `tlh` | Klingonisch / tlhIngan Hol |

Die Sprachwahl ist pro Blazor-Sitzung registriert (`IUiTextService` als
`Scoped`). Ein Benutzer veraendert damit nicht mehr die Sprache anderer aktiver
Benutzer. Das Einkaufsdashboard und die Einkaufs-Datenquellenseite reagieren
direkt auf `UiText.Changed` und rendern ihre bereits geladenen Live-Daten in der
neu gewaehlten Sprache erneut.

## Vollstaendigkeit im Einkauf

Der allgemeine Uebersetzungskatalog deckt direkte `T(de, en)`-Aufrufe ab. Der
zusaetzliche `PurchasingUiTextCatalog` inventarisiert seit 2026-08-06 85 dynamisch gewaehlte
Textpaare, die ein reiner Quelltext-Regulaerausdruck nicht sicher erkennen kann.
Dazu gehoeren:

- KPI-Details und Live-/Warte-/Simulationszustaende;
- Pipeline-, Lieferanten-, Kontrakt- und offene-Bestellungen-Texte;
- produktive Ideen-Unterseiten und deren Analysevarianten;
- Spend-Aufriss mit Beschaffungsregion, Waehrung, Warengruppe und Material;
- lange Hinweise zur CHF-Bewertung, Region und T023T-Warengruppenlogik;
- dynamische Zeitraumtexte, bei denen der variable Zeitraum erst nach der
  Uebersetzung eingesetzt wird.

Die maschinell erzeugten Werte fuer Spanisch, Italienisch, Hindi, Albanisch und
Tuerkisch liegen in `PurchasingUiTextGeneratedTranslations.cs`. Klingonische
Fachbegriffe ohne direkte Entsprechung werden in
`PurchasingKlingonOverrides.cs` mit vorhandenen Klingonisch-Woertern
umschrieben. Technische Kennungen wie `SAP`, `EKKO`, `EKPO`, `EKET`, `CHF` und
`Power BI` bleiben unveraendert. Das ist eine technische Vollstaendigkeits- und
Plausibilitaetspruefung, keine Zertifizierung durch muttersprachliche
Uebersetzer.

## Automatische Absicherung

Der Release-Test umfasst 351 Tests. Die Lokalisierungstests pruefen insbesondere:

- jeden der 85 dynamischen Einkaufsschluessel in `es`, `it`, `hi`, `sq`, `tr`
  und `tlh`;
- nicht leere Uebersetzungen und unveraenderte Formatplatzhalter;
- die sitzungsbezogene Registrierung des Sprachdienstes;
- An- und Abmeldung der Einkaufsseiten am Sprachwechsel-Ereignis;
- ausschliesslich lateinische Schrift im Klingonisch-Katalog;
- keine versehentlich verbliebenen englischen Einkaufs-Fachwoerter in den
  geprueften Klingonisch-Umschreibungen.

## Poor Man's Project Management Suite

Die Suite ist als unterster Hauptnavigationseintrag `Projekte` eingebunden und
unter `/projekte` erreichbar. Sie ist bewusst einfach gehalten und speichert in
der bestehenden SQLite-Datenbank.

Funktionen:

- Projekte neu erfassen und bearbeiten;
- Titel, Beschreibung, Verantwortlicher, Start- und Faelligkeitsdatum;
- Fortschritt von 0 bis 100 Prozent;
- Status `Idee`, `Geplant`, `In Arbeit`, `Blockiert`, `Abgeschlossen`;
- Prioritaet `Niedrig`, `Normal`, `Hoch`, `Kritisch`;
- Kennzahlen fuer aktive, laufende, blockierte und abgeschlossene Projekte;
- Projekte archivieren und archivierte Eintraege optional anzeigen;
- lokalisierte Oberflaeche in allen oben aufgefuehrten Sprachen.

Technische Bausteine:

| Bereich | Datei / Komponente |
| --- | --- |
| UI und Route | `Components/Pages/Projects.razor` |
| Datenmodell | `Models/ProjectItem.cs` |
| Persistenz | `Services/ProjectManagementService.cs` |
| Servicevertrag | `Services/IProjectManagementService.cs` |
| Navigation | `Services/DatabaseSeedService.cs` |
| Datenbankabbildung | `Data/AppDbContext.cs` |

Speichern begrenzt den Fortschritt auf 0 bis 100, normalisiert unbekannte
Status-/Prioritaetswerte auf sichere Standardwerte und schreibt
`CreatedAtUtc`/`UpdatedAtUtc`. Archivieren ist reversibel auf Datenbankebene;
die aktuelle UI bietet jedoch bewusst nur die Archivaktion und die Anzeige
archivierter Eintraege, noch keine Wiederherstellen-Schaltflaeche.

## Release und Betrieb

Die Lokalisierungsaenderung fuehrt keine weitere Datenbankmigration aus. Beim
IIS-Publish darf `trafag_exporter.db` nicht ueberschrieben werden. Der
Produktivcheck umfasst mindestens HTTP 200 fuer `/`, `/einkauf`,
`/einkauf/verbindungen` und `/projekte` sowie die veroeffentlichte DLL-Pruefsumme.
