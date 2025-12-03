# Mail Fine-Tuning Web-App für macOS (Apple Silicon)

Eine vollständige lokale Web-Anwendung für das Fine-Tuning von LLMs auf Mail-Daten, optimiert für Apple Silicon (M4 Pro mit 24GB RAM).

## Features

- 📥 **Mail Import**: Drag & Drop Upload von .mbox, .eml, .txt Dateien mit automatischer Bereinigung
- 🏷️ **Labeling Interface**: Komfortable UI zum manuellen Labeln von Mails
- 📊 **Export & Statistiken**: JSONL Export für Training mit detaillierten Statistiken
- 🤖 **Modell-Management**: Verwaltung von MLX-Modellen
- 🎯 **Training**: LoRA Fine-Tuning mit Live-Updates und Visualisierung
- 🧪 **Evaluation**: Chat-Interface mit Vergleichsmodus (Base vs. Fine-tuned)

## Technologie-Stack

- **Backend**: Python (FastAPI)
- **Frontend**: HTML/CSS/JavaScript (Vanilla, keine Dependencies)
- **ML Framework**: MLX (Apple Silicon optimiert)
- **Database**: SQLite
- **Empfohlene Modelle**: Mistral 7B, Llama 3 8B (via MLX)

## Projektstruktur

```
mail-finetuning/
├── backend/
│   ├── main.py              # FastAPI App
│   ├── mail_parser.py       # Mail Import & Bereinigung
│   ├── data_manager.py      # SQLite Operationen
│   ├── training.py          # MLX Training Wrapper
│   └── inference.py         # Modell-Inferenz
├── frontend/
│   ├── index.html
│   ├── style.css
│   └── app.js
├── data/
│   ├── mails.db             # SQLite Datenbank
│   ├── train.jsonl
│   └── val.jsonl
├── models/                  # Heruntergeladene Modelle
├── output/                  # Trainierte Adapter
└── requirements.txt
```

## Installation

### Voraussetzungen

- macOS mit Apple Silicon (M1/M2/M3/M4)
- Python 3.10 oder höher
- mindestens 16GB RAM (24GB empfohlen)

### 1. Repository Setup

```bash
cd training
```

### 2. Virtual Environment erstellen

```bash
python3 -m venv venv
source venv/bin/activate
```

### 3. Dependencies installieren

```bash
pip install -r requirements.txt
```

### 4. Modell herunterladen

Wähle ein MLX-optimiertes Modell von Hugging Face:

```bash
# Mistral 7B (4-bit quantisiert, ~4GB)
huggingface-cli download mlx-community/Mistral-7B-Instruct-v0.3-4bit \
    --local-dir models/Mistral-7B-Instruct-v0.3-4bit

# ODER Llama 3 8B (4-bit quantisiert, ~5GB)
huggingface-cli download mlx-community/Meta-Llama-3-8B-Instruct-4bit \
    --local-dir models/Meta-Llama-3-8B-Instruct-4bit
```

**Hinweis**: Die 4-bit Versionen sind für 24GB RAM optimal. Für mehr RAM können auch größere Versionen genutzt werden.

## Nutzung

### 1. Server starten

```bash
cd backend
python main.py
```

Die App ist dann verfügbar unter: **http://localhost:8000**

### 2. Workflow

#### Schritt 1: Mails importieren

1. Gehe zu "Mail Import"
2. Ziehe .eml, .mbox oder .txt Dateien per Drag & Drop in den Upload-Bereich
3. Die Mails werden automatisch geparst und bereinigt

#### Schritt 2: Mails labeln

1. Wechsle zu "Labeling"
2. Für jede Mail:
   - Wähle den **Aufgabentyp** (Zusammenfassen, Antwort schreiben, etc.)
   - Gib den **erwarteten Output** ein
   - Klicke "Speichern" oder nutze Shortcut `S`
3. Nutze Shortcuts: `N` (Nächste), `S` (Speichern), `K` (Skip)

**Tipp**: Mindestens 50 gelabelte Beispiele für gutes Fine-Tuning!

#### Schritt 3: Daten exportieren

1. Gehe zu "Export & Stats"
2. Prüfe die Statistiken (mind. 50 gelabelte Mails empfohlen)
3. Klicke "JSONL generieren"
4. Optional: Download der JSONL-Dateien zur Archivierung

#### Schritt 4: Training starten

1. Wechsle zu "Training"
2. Konfiguriere Parameter:
   - **Modell**: Wähle heruntergeladenes Modell
   - **Learning Rate**: Standard 1e-5 (bei Overfitting niedriger)
   - **Epochs**: 3-5 für erste Versuche
   - **Batch Size**: 4 (bei 24GB RAM sicher)
   - **LoRA Rank**: 8-16 (höher = mehr Kapazität, mehr RAM)
3. Klicke "Training starten"
4. Beobachte Live-Updates:
   - Training/Validation Loss
   - Fortschritt und ETA
   - Speichernutzung

**Warnung bei Overfitting**: Wenn Validation Loss steigt während Training Loss sinkt, Training abbrechen!

#### Schritt 5: Modell testen

1. Gehe zu "Evaluation"
2. Wähle Task-Type und gib Mail-Text ein
3. Klicke "Vergleich starten"
4. Sieh dir die Ausgaben von Base- und Fine-tuned-Modell an

### 3. Export des fertigen Modells

Nach erfolgreichem Training liegen die LoRA-Adapter in `output/run_[timestamp]/adapters.npz`.

Um das Modell zu nutzen:

```python
from mlx_lm import load

model = load(
    "models/Mistral-7B-Instruct-v0.3-4bit",
    adapter_path="output/run_1234567890/adapters.npz"
)
```

## API Endpoints

### Mails

- `POST /api/mails/upload` - Mails hochladen
- `GET /api/mails` - Alle Mails abrufen
- `GET /api/mails/{id}` - Einzelne Mail
- `PUT /api/mails/{id}` - Mail aktualisieren (Labeling)
- `DELETE /api/mails/{id}` - Mail löschen

### Export

- `GET /api/export/stats` - Statistiken
- `POST /api/export/jsonl` - Training-Daten generieren
- `GET /api/export/download/{train|val}` - JSONL herunterladen

### Modelle

- `GET /api/models` - Verfügbare Modelle
- `POST /api/models/download` - Modell herunterladen (Placeholder)

### Training

- `POST /api/training/start` - Training starten
- `POST /api/training/stop` - Training stoppen
- `GET /api/training/status` - Status abrufen
- `GET /api/training/stream` - SSE Stream für Live-Updates

### Inference

- `POST /api/inference/load` - Modell laden
- `GET /api/inference/loaded` - Geladene Modelle
- `POST /api/inference/generate` - Text generieren
- `POST /api/inference/compare` - Modell-Vergleich
- `GET /api/inference/test-prompts` - Test-Prompts

## Tipps & Best Practices

### Datenqualität

- **Mindestens 50 Beispiele** pro Task-Type
- **Einheitlicher Output-Stil**: Achte auf konsistente Formatierung
- **Diverse Beispiele**: Verschiedene Mail-Längen und Stile
- **Klare Labels**: Vermeide mehrdeutige oder widersprüchliche Labels

### Training

- **Learning Rate**:
  - 1e-5 für die meisten Fälle
  - 5e-6 bei Overfitting
  - 1e-4 bei sehr kleinem Datensatz (Vorsicht!)

- **Epochs**:
  - 3 Epochs für Start
  - Mehr Epochs wenn Loss noch sinkt
  - Weniger wenn Overfitting auftritt

- **LoRA Rank**:
  - 8 für einfache Tasks
  - 16-32 für komplexe Tasks
  - Höher = mehr Kapazität aber mehr RAM

### Overfitting erkennen

Zeichen von Overfitting:
- ✅ Training Loss sinkt kontinuierlich
- ❌ Validation Loss steigt oder stagniert
- ❌ Modell "memoriert" exakte Trainingsbeispiele

Lösungen:
- Mehr Daten sammeln
- Kleinere Learning Rate
- Weniger Epochs
- Niedrigere LoRA Rank

## Troubleshooting

### "Out of Memory" Fehler

- Reduziere Batch Size (4 → 2 → 1)
- Nutze kleineres Modell (4-bit quantisiert)
- Schließe andere Programme

### Training sehr langsam

- Prüfe ob Metal Performance Shaders aktiv sind
- Nutze 4-bit quantisierte Modelle
- Reduziere max_seq_length (Standard: 2048)

### Modell gibt schlechte Ergebnisse

- Mehr/bessere Trainingsdaten
- Längeres Training (mehr Epochs)
- Höhere LoRA Rank
- Prüfe Prompt-Format

## Wichtige Hinweise

### MLX Training Loop

**WICHTIG**: Die aktuelle Implementierung in `training.py` enthält eine **simulierte Training Loop**. Für produktiven Einsatz muss diese durch echtes MLX Training ersetzt werden:

```python
# Beispiel für echtes MLX Training mit mlx-lm
from mlx_lm.tuner import train

train(
    model_path=str(model_path),
    data_path=str(train_file),
    val_data_path=str(val_file),
    adapter_file=str(output_path / 'adapters.npz'),
    iters=total_steps,
    learning_rate=config.learning_rate,
    batch_size=config.batch_size,
    # ... weitere Parameter
)
```

Siehe [mlx-lm Dokumentation](https://github.com/ml-explore/mlx-examples/tree/main/llms) für Details.

### Inference

Die Inference-Implementation in `inference.py` nutzt `mlx_lm.generate()`. Stelle sicher, dass das richtige Prompt-Format für dein Modell genutzt wird (z.B. ChatML, Llama-Format, etc.).

## Entwicklung

### Debug-Modus

```bash
uvicorn main:app --reload --log-level debug
```

### Tests (TODO)

```bash
pytest tests/
```

## Lizenz

MIT License

## Support

Bei Problemen:
1. Prüfe die Browser Console (F12) für Frontend-Fehler
2. Prüfe die Server-Logs für Backend-Fehler
3. Stelle sicher, dass alle Dependencies installiert sind
4. Prüfe, dass MLX korrekt auf Apple Silicon läuft

## Roadmap

- [ ] Echte MLX Training Loop implementieren
- [ ] Automatisches Checkpoint-Management
- [ ] Model Merging (Base + Adapter zusammenführen)
- [ ] Export für Deployment
- [ ] Batch-Inference
- [ ] Tests
- [ ] Docker Support

---

**Viel Erfolg beim Fine-Tuning! 🚀**
