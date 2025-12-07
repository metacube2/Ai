# Paperless Finance Report

Ein Python-basiertes CLI-Tool, das über die Paperless-ngx REST-API Dokumente abruft, Beträge und Custom Fields extrahiert und daraus Finanzberichte generiert.

## Features

- **Basis-Auswertung**: Summe aller Beträge nach Tags, Kategorien, Korrespondenten
- **Zeiträume**: Filter nach Jahr, Monat oder beliebigem Datumsbereich
- **Gruppierung**: Nach Tag, Kategorie, Korrespondent, Zahlungsart, Monat, Quartal
- **Vergleichsberichte**: Jahresvergleiche mit Veränderungsanalyse
- **Mehrere Ausgabeformate**: CLI, HTML (mit Chart.js Diagrammen), PDF, JSON, CSV
- **Caching**: Optionaler Festplatten-Cache für bessere Performance
- **Flexibel**: Konfigurierbare Custom Field Namen

## Installation

### Voraussetzungen

- Python 3.8+
- Paperless-ngx Installation mit REST-API Zugriff
- API-Token (erstellen unter: Paperless → Einstellungen → Authentifizierungs-Tokens)

### Installation

```bash
# Repository klonen
git clone https://github.com/yourusername/paperless-report.git
cd paperless-report

# Virtuelle Umgebung erstellen
python3 -m venv venv
source venv/bin/activate  # Linux/macOS
# oder: venv\Scripts\activate  # Windows

# Dependencies installieren
pip install -r requirements.txt

# Optional: Vollinstallation mit PDF-Support
pip install -e ".[full]"
```

### Konfiguration

```bash
# Beispiel-Konfiguration erstellen
cp config.yaml.example config.yaml

# Konfiguration anpassen
nano config.yaml
```

Mindestens erforderlich:
```yaml
paperless:
  url: "http://localhost:8000"  # Deine Paperless URL
  token: "YOUR_API_TOKEN"       # API Token
```

Alternativ kann der Token auch als Umgebungsvariable gesetzt werden:
```bash
export PAPERLESS_TOKEN="your_api_token"
```

## Verwendung

### Verbindung testen

```bash
python main.py test
```

### Jahresbericht

```bash
# CLI-Ausgabe
python main.py report --year 2024

# Mit Details
python main.py report --year 2024 --detail

# HTML-Bericht
python main.py report --year 2024 --format html

# PDF-Bericht
python main.py report --year 2024 --format pdf
```

### Mit Filtern

```bash
# Nach Tag filtern
python main.py report --year 2024 --tag rechnung

# Nach Korrespondent filtern
python main.py report --year 2024 --correspondent "Swisscom"

# Nach Monat filtern
python main.py report --year 2024 --month 6
```

### Gruppierung

```bash
# Nach Tag gruppieren (Standard)
python main.py report --year 2024 --group-by tag

# Nach Korrespondent gruppieren
python main.py report --year 2024 --group-by correspondent

# Nach Kategorie und Monat gruppieren
python main.py report --year 2024 --group-by category --group-by month
```

### Jahresvergleich

```bash
# CLI-Vergleich
python main.py compare 2023 2024

# HTML-Vergleichsbericht
python main.py compare 2023 2024 --format html
```

### Weitere Befehle

```bash
# Dokumente auflisten
python main.py list-docs --tag rechnung --limit 50

# Cache löschen
python main.py clear-cache

# Hilfe anzeigen
python main.py --help
python main.py report --help
```

## Custom Fields in Paperless

Für die volle Funktionalität sollten folgende Custom Fields in Paperless angelegt werden:

| Feldname        | Typ      | Beschreibung                          |
|-----------------|----------|---------------------------------------|
| `betrag`        | Währung  | Rechnungsbetrag                       |
| `rechnungsdatum`| Datum    | Datum der Rechnung                    |
| `kategorie`     | Auswahl  | Wohnen, Gesundheit, Mobilität, etc.   |
| `zahlungsart`   | Auswahl  | Bar, Einzahlung, LSV, eBill           |

Die Feldnamen können in der `config.yaml` angepasst werden.

## Ausgabeformate

### CLI

Einfache tabellarische Ausgabe im Terminal.

### HTML

Interaktiver Bericht mit:
- Zusammenfassungskarten
- Chart.js Diagramme (Doughnut, Bar, Line)
- Sortierbare Tabellen
- Links zu Paperless-Dokumenten
- Export-Button für CSV

### PDF

Druckfertiger PDF-Bericht (benötigt WeasyPrint).

### JSON

Maschinenlesbares Format für weitere Verarbeitung.

### CSV

Excel-kompatibles Format mit BOM für korrekte Umlaute.

## Projektstruktur

```
paperless-report/
├── config.yaml.example      # Beispiel-Konfiguration
├── config.py                # Konfigurationsmanagement
├── paperless_client.py      # API-Client
├── extractor.py             # Datenextraktion und -aggregation
├── report_generator.py      # Berichtsgenerierung
├── main.py                  # CLI-Einstiegspunkt
├── templates/
│   └── report.html          # HTML-Template
├── output/                  # Generierte Berichte
├── requirements.txt
├── setup.py
└── README.md
```

## Lizenz

MIT License
