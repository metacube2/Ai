# Quick Start Guide

Schnellstart-Anleitung für die Mail Fine-Tuning App.

## 1. Installation (5 Minuten)

```bash
# 1. Virtual Environment erstellen
python3 -m venv venv
source venv/bin/activate

# 2. Dependencies installieren
pip install -r requirements.txt

# 3. Modell herunterladen (ca. 4GB, dauert je nach Internetverbindung)
huggingface-cli download mlx-community/Mistral-7B-Instruct-v0.3-4bit \
    --local-dir models/Mistral-7B-Instruct-v0.3-4bit
```

## 2. Server starten

```bash
./start.sh
```

Oder manuell:

```bash
source venv/bin/activate
cd backend
python main.py
```

App öffnen: **http://localhost:8000**

## 3. Erste Schritte (10 Minuten)

### Schritt 1: Test-Mails erstellen

Erstelle eine Datei `test.txt` mit einer Beispiel-Mail:

```
Subject: Projekt Update
From: max@example.com
To: team@example.com

Hallo Team,

das neue Feature ist fertig und bereit für Testing.
Ich habe die API-Integration abgeschlossen und alle Tests laufen durch.

Bitte reviewt den Code bis Freitag.

Grüße
Max
```

### Schritt 2: Mails importieren

1. Öffne http://localhost:8000
2. Ziehe `test.txt` in den Upload-Bereich
3. Mail erscheint in der Liste

### Schritt 3: Erste Mail labeln

1. Klicke auf "Labeling" in der Sidebar
2. Wähle **Aufgabentyp**: "Zusammenfassen"
3. Gib **erwarteten Output** ein:
   ```
   Max hat das neue Feature fertiggestellt und alle Tests sind erfolgreich.
   Das Team soll den Code bis Freitag reviewen.
   ```
4. Klicke "Speichern" (oder drücke `S`)

### Schritt 4: Mehr Mails labeln

- Erstelle mindestens **20-50 Beispiel-Mails**
- Nutze verschiedene Typen:
  - Zusammenfassen
  - Antwort schreiben
  - Action Items extrahieren
- Nutze Shortcuts: `N` (Nächste), `S` (Speichern)

### Schritt 5: Statistiken prüfen

1. Gehe zu "Export & Stats"
2. Prüfe:
   - Mind. 50 gelabelte Mails? ✅
   - Gute Verteilung der Task-Types? ✅

### Schritt 6: Training starten

1. Gehe zu "Training"
2. Wähle dein Modell aus
3. Nutze Standard-Einstellungen:
   - Learning Rate: 1e-5
   - Epochs: 3
   - Batch Size: 4
   - LoRA Rank: 8
4. Klicke "Training starten"
5. Beobachte Live-Updates

⏱️ **Training dauert**: Ca. 5-10 Minuten bei 50 Beispielen

### Schritt 7: Modell testen

1. Gehe zu "Evaluation"
2. Klicke "Test-Beispiel laden"
3. Klicke "Vergleich starten"
4. Vergleiche Base- vs. Fine-tuned-Ausgabe

## Tipps

### Gute Trainingsdaten

✅ **DO**:
- Mindestens 50 Beispiele
- Konsistenter Output-Stil
- Diverse Mail-Typen
- Klare, eindeutige Labels

❌ **DON'T**:
- Zu wenige Beispiele (<20)
- Widersprüchliche Labels
- Nur sehr ähnliche Mails
- Zu lange Outputs (>500 Wörter)

### Training-Parameter

Für **erste Versuche**:
- Learning Rate: **1e-5**
- Epochs: **3**
- Batch Size: **4**
- LoRA Rank: **8**

Bei **Overfitting** (Val Loss steigt):
- Learning Rate: **5e-6** (niedriger)
- Epochs: **2** (weniger)

Bei **Underfitting** (beide Losses hoch):
- Epochs: **5** (mehr)
- LoRA Rank: **16** (höher)
- Mehr Daten sammeln!

### Keyboard Shortcuts

Im Labeling-Interface:
- `N` - Nächste Mail
- `S` - Speichern
- `K` - Skip (Überspringen)

## Troubleshooting

### Server startet nicht

```bash
# Prüfe Python-Version (mind. 3.10)
python3 --version

# Prüfe ob Port 8000 frei ist
lsof -i :8000

# Nutze anderen Port
uvicorn main:app --port 8001
```

### Modell nicht gefunden

```bash
# Prüfe ob Modell existiert
ls -la models/

# Download nochmal versuchen
huggingface-cli download mlx-community/Mistral-7B-Instruct-v0.3-4bit \
    --local-dir models/Mistral-7B-Instruct-v0.3-4bit
```

### Out of Memory

Reduziere Batch Size:
1. Gehe zu "Training"
2. Setze Batch Size auf **2** oder **1**

### Training sehr langsam

- Nutze 4-bit quantisierte Modelle
- Reduziere Batch Size
- Schließe andere Programme

## Nächste Schritte

Nach erfolgreichem ersten Training:

1. **Mehr Daten sammeln**: 100+ Beispiele für bessere Ergebnisse
2. **Parameter tunen**: Experimentiere mit Learning Rate und Epochs
3. **Verschiedene Tasks**: Probiere alle Task-Types aus
4. **Evaluation**: Teste ausgiebig mit neuen Mails

## Ressourcen

- Vollständige Doku: [README.md](README.md)
- MLX Doku: https://ml-explore.github.io/mlx/
- MLX-LM: https://github.com/ml-explore/mlx-examples

---

**Viel Erfolg! 🚀**

Bei Fragen schaue ins vollständige README oder die API-Dokumentation.
