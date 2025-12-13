<?php
/**
 * Zwölfton-Synthesizer (Dodekaphonie)
 * Nach der Lehre von Arnold Schönberg
 *
 * Regeln der Zwölftontechnik:
 * 1. Alle 12 chromatischen Töne müssen verwendet werden
 * 2. Kein Ton darf wiederholt werden, bevor alle anderen gespielt wurden
 * 3. Die Reihe kann transformiert werden: Original, Krebs, Umkehrung, Krebsumkehrung
 * 4. Transposition auf alle 12 Stufen ist erlaubt
 */

class TwelveToneGenerator {
    private array $noteNames = ['C', 'C#', 'D', 'D#', 'E', 'F', 'F#', 'G', 'G#', 'A', 'A#', 'B'];
    private array $originalRow;

    public function __construct() {
        $this->originalRow = $this->generateRandomRow();
    }

    /**
     * Generiert eine zufällige Zwölftonreihe
     */
    public function generateRandomRow(): array {
        $row = range(0, 11);
        shuffle($row);
        return $row;
    }

    /**
     * Gibt die Grundreihe zurück
     */
    public function getOriginalRow(): array {
        return $this->originalRow;
    }

    /**
     * Krebs (Retrograde) - Reihe rückwärts
     */
    public function getRetrograde(): array {
        return array_reverse($this->originalRow);
    }

    /**
     * Umkehrung (Inversion) - Intervalle gespiegelt
     */
    public function getInversion(): array {
        $inversion = [];
        $firstNote = $this->originalRow[0];

        foreach ($this->originalRow as $note) {
            $interval = $note - $firstNote;
            $invertedNote = ($firstNote - $interval + 12) % 12;
            $inversion[] = $invertedNote;
        }

        return $inversion;
    }

    /**
     * Krebsumkehrung (Retrograde Inversion)
     */
    public function getRetrogradeInversion(): array {
        return array_reverse($this->getInversion());
    }

    /**
     * Transponiert eine Reihe um n Halbtöne
     */
    public function transpose(array $row, int $semitones): array {
        return array_map(function($note) use ($semitones) {
            return ($note + $semitones) % 12;
        }, $row);
    }

    /**
     * Konvertiert Notennummern zu Notennamen
     */
    public function toNoteNames(array $row): array {
        return array_map(function($note) {
            return $this->noteNames[$note];
        }, $row);
    }

    /**
     * Generiert die komplette Zwölftonmatrix (12x12)
     */
    public function generateMatrix(): array {
        $matrix = [];
        $inversion = $this->getInversion();

        for ($i = 0; $i < 12; $i++) {
            $transposition = $inversion[$i];
            $matrix[$i] = $this->transpose($this->originalRow, $transposition);
        }

        return $matrix;
    }

    /**
     * Gibt alle Daten als JSON zurück
     */
    public function toJSON(): string {
        return json_encode([
            'original' => $this->originalRow,
            'retrograde' => $this->getRetrograde(),
            'inversion' => $this->getInversion(),
            'retrogradeInversion' => $this->getRetrogradeInversion(),
            'noteNames' => $this->toNoteNames($this->originalRow),
            'matrix' => $this->generateMatrix()
        ]);
    }
}

// API-Endpoint für neue Reihe
if (isset($_GET['action']) && $_GET['action'] === 'generate') {
    header('Content-Type: application/json');
    $generator = new TwelveToneGenerator();
    echo $generator->toJSON();
    exit;
}

$generator = new TwelveToneGenerator();
$initialData = $generator->toJSON();
?>
<!DOCTYPE html>
<html lang="de">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Zwölfton-Synthesizer | Dodekaphonie nach Schönberg</title>
    <style>
        * {
            margin: 0;
            padding: 0;
            box-sizing: border-box;
        }

        body {
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            background: linear-gradient(135deg, #1a1a2e 0%, #16213e 50%, #0f3460 100%);
            min-height: 100vh;
            color: #e0e0e0;
            overflow-x: hidden;
        }

        .container {
            max-width: 1400px;
            margin: 0 auto;
            padding: 20px;
        }

        header {
            text-align: center;
            padding: 30px 0;
            border-bottom: 1px solid rgba(255,255,255,0.1);
            margin-bottom: 30px;
        }

        h1 {
            font-size: 2.5em;
            background: linear-gradient(90deg, #00d4ff, #7b2cbf, #ff6b6b);
            -webkit-background-clip: text;
            -webkit-text-fill-color: transparent;
            background-clip: text;
            margin-bottom: 10px;
        }

        .subtitle {
            color: #888;
            font-size: 1.1em;
        }

        .controls {
            display: flex;
            justify-content: center;
            gap: 20px;
            margin-bottom: 30px;
            flex-wrap: wrap;
        }

        button {
            padding: 15px 30px;
            font-size: 1.1em;
            border: none;
            border-radius: 50px;
            cursor: pointer;
            transition: all 0.3s ease;
            font-weight: 600;
        }

        .btn-primary {
            background: linear-gradient(135deg, #00d4ff, #0099cc);
            color: #fff;
        }

        .btn-primary:hover {
            transform: translateY(-2px);
            box-shadow: 0 10px 30px rgba(0, 212, 255, 0.3);
        }

        .btn-secondary {
            background: linear-gradient(135deg, #7b2cbf, #5a189a);
            color: #fff;
        }

        .btn-secondary:hover {
            transform: translateY(-2px);
            box-shadow: 0 10px 30px rgba(123, 44, 191, 0.3);
        }

        .btn-danger {
            background: linear-gradient(135deg, #ff6b6b, #ee5a5a);
            color: #fff;
        }

        .btn-danger:hover {
            transform: translateY(-2px);
            box-shadow: 0 10px 30px rgba(255, 107, 107, 0.3);
        }

        .panel {
            background: rgba(255,255,255,0.05);
            border-radius: 20px;
            padding: 25px;
            margin-bottom: 25px;
            backdrop-filter: blur(10px);
            border: 1px solid rgba(255,255,255,0.1);
        }

        .panel h2 {
            margin-bottom: 20px;
            color: #00d4ff;
            font-size: 1.3em;
        }

        .row-display {
            display: flex;
            gap: 8px;
            flex-wrap: wrap;
            justify-content: center;
        }

        .note-box {
            width: 60px;
            height: 60px;
            display: flex;
            align-items: center;
            justify-content: center;
            background: rgba(0, 212, 255, 0.1);
            border: 2px solid rgba(0, 212, 255, 0.3);
            border-radius: 10px;
            font-weight: bold;
            font-size: 1.2em;
            transition: all 0.3s ease;
        }

        .note-box.active {
            background: linear-gradient(135deg, #00d4ff, #7b2cbf);
            border-color: #fff;
            transform: scale(1.2);
            box-shadow: 0 0 30px rgba(0, 212, 255, 0.5);
        }

        .note-box.played {
            background: rgba(123, 44, 191, 0.3);
            border-color: rgba(123, 44, 191, 0.5);
        }

        .sliders {
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
            gap: 20px;
        }

        .slider-group {
            display: flex;
            flex-direction: column;
            gap: 8px;
        }

        .slider-group label {
            display: flex;
            justify-content: space-between;
            color: #aaa;
        }

        input[type="range"] {
            width: 100%;
            height: 8px;
            border-radius: 4px;
            background: rgba(255,255,255,0.1);
            outline: none;
            -webkit-appearance: none;
        }

        input[type="range"]::-webkit-slider-thumb {
            -webkit-appearance: none;
            width: 20px;
            height: 20px;
            border-radius: 50%;
            background: linear-gradient(135deg, #00d4ff, #7b2cbf);
            cursor: pointer;
        }

        .visualizer-container {
            height: 200px;
            background: rgba(0,0,0,0.3);
            border-radius: 15px;
            overflow: hidden;
            position: relative;
        }

        #visualizer {
            width: 100%;
            height: 100%;
        }

        .transformation-select {
            display: flex;
            gap: 10px;
            flex-wrap: wrap;
            justify-content: center;
            margin-bottom: 20px;
        }

        .transform-btn {
            padding: 10px 20px;
            background: rgba(255,255,255,0.1);
            border: 1px solid rgba(255,255,255,0.2);
            border-radius: 25px;
            color: #e0e0e0;
            cursor: pointer;
            transition: all 0.3s ease;
        }

        .transform-btn.active {
            background: linear-gradient(135deg, #00d4ff, #7b2cbf);
            border-color: transparent;
        }

        .transform-btn:hover {
            background: rgba(0, 212, 255, 0.3);
        }

        .status {
            text-align: center;
            padding: 15px;
            background: rgba(0,0,0,0.2);
            border-radius: 10px;
            margin-top: 20px;
        }

        .status-indicator {
            display: inline-block;
            width: 12px;
            height: 12px;
            border-radius: 50%;
            margin-right: 10px;
            background: #666;
        }

        .status-indicator.playing {
            background: #00ff88;
            animation: pulse 1s infinite;
        }

        @keyframes pulse {
            0%, 100% { opacity: 1; }
            50% { opacity: 0.5; }
        }

        .matrix-container {
            overflow-x: auto;
        }

        .matrix {
            display: grid;
            grid-template-columns: repeat(12, 1fr);
            gap: 3px;
            min-width: 500px;
        }

        .matrix-cell {
            aspect-ratio: 1;
            display: flex;
            align-items: center;
            justify-content: center;
            background: rgba(0, 212, 255, 0.1);
            border-radius: 5px;
            font-size: 0.9em;
            font-weight: 500;
        }

        .info-text {
            background: rgba(0,0,0,0.2);
            padding: 15px;
            border-radius: 10px;
            margin-top: 15px;
            font-size: 0.9em;
            color: #aaa;
            line-height: 1.6;
        }

        footer {
            text-align: center;
            padding: 30px;
            color: #666;
            border-top: 1px solid rgba(255,255,255,0.1);
            margin-top: 30px;
        }

        @media (max-width: 768px) {
            h1 { font-size: 1.8em; }
            .note-box { width: 45px; height: 45px; font-size: 1em; }
            .controls { flex-direction: column; align-items: center; }
            button { width: 100%; max-width: 300px; }
        }
    </style>
</head>
<body>
    <div class="container">
        <header>
            <h1>Zwölfton-Synthesizer</h1>
            <p class="subtitle">Dodekaphonie nach Arnold Schönberg</p>
        </header>

        <div class="controls">
            <button id="startBtn" class="btn-primary">▶ Starten</button>
            <button id="stopBtn" class="btn-danger">◼ Stoppen</button>
            <button id="newRowBtn" class="btn-secondary">🎲 Neue Reihe</button>
        </div>

        <div class="panel">
            <h2>Aktuelle Zwölftonreihe</h2>
            <div class="transformation-select">
                <button class="transform-btn active" data-transform="original">Original (O)</button>
                <button class="transform-btn" data-transform="retrograde">Krebs (R)</button>
                <button class="transform-btn" data-transform="inversion">Umkehrung (I)</button>
                <button class="transform-btn" data-transform="retrogradeInversion">Krebsumkehrung (RI)</button>
            </div>
            <div id="rowDisplay" class="row-display"></div>
        </div>

        <div class="panel">
            <h2>Klangparameter</h2>
            <div class="sliders">
                <div class="slider-group">
                    <label>Tempo (BPM): <span id="tempoValue">120</span></label>
                    <input type="range" id="tempo" min="40" max="300" value="120">
                </div>
                <div class="slider-group">
                    <label>Oktave: <span id="octaveValue">4</span></label>
                    <input type="range" id="octave" min="2" max="6" value="4">
                </div>
                <div class="slider-group">
                    <label>Reverb: <span id="reverbValue">50</span>%</label>
                    <input type="range" id="reverb" min="0" max="100" value="50">
                </div>
                <div class="slider-group">
                    <label>Attack: <span id="attackValue">0.05</span>s</label>
                    <input type="range" id="attack" min="1" max="500" value="50">
                </div>
                <div class="slider-group">
                    <label>Release: <span id="releaseValue">0.3</span>s</label>
                    <input type="range" id="release" min="50" max="2000" value="300">
                </div>
                <div class="slider-group">
                    <label>Wellenform:</label>
                    <select id="waveform" style="padding: 8px; border-radius: 5px; background: rgba(255,255,255,0.1); color: #e0e0e0; border: 1px solid rgba(255,255,255,0.2);">
                        <option value="sine">Sinus</option>
                        <option value="triangle">Dreieck</option>
                        <option value="square">Rechteck</option>
                        <option value="sawtooth" selected>Sägezahn</option>
                    </select>
                </div>
            </div>
        </div>

        <div class="panel">
            <h2>Audio-Visualisierung</h2>
            <div class="visualizer-container">
                <canvas id="visualizer"></canvas>
            </div>
            <div class="status">
                <span id="statusIndicator" class="status-indicator"></span>
                <span id="statusText">Bereit zum Starten</span>
            </div>
        </div>

        <div class="panel">
            <h2>Zwölftonmatrix</h2>
            <div class="matrix-container">
                <div id="matrix" class="matrix"></div>
            </div>
            <div class="info-text">
                <strong>Die Zwölftontechnik (Dodekaphonie):</strong><br>
                Entwickelt von Arnold Schönberg um 1921. Alle 12 Halbtöne der chromatischen Tonleiter
                werden gleichberechtigt verwendet. Die Grundreihe kann in vier Formen erscheinen:
                <strong>Original (O)</strong> - die Grundreihe,
                <strong>Krebs (R)</strong> - rückwärts gespielt,
                <strong>Umkehrung (I)</strong> - Intervalle gespiegelt,
                <strong>Krebsumkehrung (RI)</strong> - Kombination aus Krebs und Umkehrung.
                Jede Form kann auf alle 12 Stufen transponiert werden (48 mögliche Reihen).
            </div>
        </div>

        <footer>
            <p>Zwölfton-Synthesizer &copy; 2024 | Basierend auf der Dodekaphonie von Arnold Schönberg</p>
        </footer>
    </div>

    <script>
    // Initiale Daten vom PHP-Backend
    let rowData = <?= $initialData ?>;

    // Audio-Kontext und Knoten
    let audioContext = null;
    let masterGain = null;
    let reverbGain = null;
    let dryGain = null;
    let convolver = null;
    let analyser = null;
    let isPlaying = false;
    let currentNoteIndex = 0;
    let playInterval = null;
    let currentTransform = 'original';

    // Frequenzen für alle Noten (A4 = 440Hz)
    const noteFrequencies = {
        0: 261.63,  // C
        1: 277.18,  // C#
        2: 293.66,  // D
        3: 311.13,  // D#
        4: 329.63,  // E
        5: 349.23,  // F
        6: 369.99,  // F#
        7: 392.00,  // G
        8: 415.30,  // G#
        9: 440.00,  // A
        10: 466.16, // A#
        11: 493.88  // B
    };

    const noteNames = ['C', 'C#', 'D', 'D#', 'E', 'F', 'F#', 'G', 'G#', 'A', 'A#', 'B'];

    /**
     * Initialisiert den Audio-Kontext
     */
    async function initAudio() {
        if (audioContext) return;

        audioContext = new (window.AudioContext || window.webkitAudioContext)();

        // Master Gain
        masterGain = audioContext.createGain();
        masterGain.gain.value = 0.5;
        masterGain.connect(audioContext.destination);

        // Analyser für Visualisierung
        analyser = audioContext.createAnalyser();
        analyser.fftSize = 2048;
        analyser.connect(masterGain);

        // Dry/Wet Gain für Reverb
        dryGain = audioContext.createGain();
        reverbGain = audioContext.createGain();

        dryGain.connect(analyser);
        reverbGain.connect(analyser);

        // Convolver für Reverb
        convolver = audioContext.createConvolver();
        convolver.connect(reverbGain);

        // Generiere Impulsantwort für Reverb
        await createReverbImpulse();

        updateReverbMix();
        startVisualization();
    }

    /**
     * Erstellt eine synthetische Impulsantwort für den Reverb
     */
    async function createReverbImpulse() {
        const sampleRate = audioContext.sampleRate;
        const length = sampleRate * 3; // 3 Sekunden Reverb
        const impulse = audioContext.createBuffer(2, length, sampleRate);

        for (let channel = 0; channel < 2; channel++) {
            const channelData = impulse.getChannelData(channel);
            for (let i = 0; i < length; i++) {
                // Exponentieller Decay mit etwas Rauschen
                const decay = Math.pow(1 - i / length, 2);
                channelData[i] = (Math.random() * 2 - 1) * decay;
            }
        }

        convolver.buffer = impulse;
    }

    /**
     * Aktualisiert das Dry/Wet-Verhältnis des Reverbs
     */
    function updateReverbMix() {
        const reverbAmount = document.getElementById('reverb').value / 100;
        dryGain.gain.value = 1 - reverbAmount * 0.5;
        reverbGain.gain.value = reverbAmount;
    }

    /**
     * Spielt eine Note
     */
    function playNote(noteNumber) {
        if (!audioContext) return;

        const octave = parseInt(document.getElementById('octave').value);
        const frequency = noteFrequencies[noteNumber] * Math.pow(2, octave - 4);
        const waveform = document.getElementById('waveform').value;
        const attack = document.getElementById('attack').value / 1000;
        const release = document.getElementById('release').value / 1000;

        // Oszillator
        const osc = audioContext.createOscillator();
        osc.type = waveform;
        osc.frequency.value = frequency;

        // Gain für ADSR-Hüllkurve
        const gainNode = audioContext.createGain();
        gainNode.gain.value = 0;

        // Verbindungen
        osc.connect(gainNode);
        gainNode.connect(dryGain);
        gainNode.connect(convolver);

        const now = audioContext.currentTime;

        // Attack
        gainNode.gain.linearRampToValueAtTime(0.7, now + attack);

        // Release
        gainNode.gain.linearRampToValueAtTime(0, now + attack + release);

        osc.start(now);
        osc.stop(now + attack + release + 0.1);
    }

    /**
     * Startet die automatische Wiedergabe
     */
    async function startPlaying() {
        await initAudio();

        if (isPlaying) return;
        isPlaying = true;
        currentNoteIndex = 0;

        updateStatus(true);
        playNextNote();
    }

    /**
     * Stoppt die Wiedergabe
     */
    function stopPlaying() {
        isPlaying = false;
        if (playInterval) {
            clearTimeout(playInterval);
            playInterval = null;
        }
        updateStatus(false);
        resetNoteDisplay();
    }

    /**
     * Spielt die nächste Note in der Reihe
     */
    function playNextNote() {
        if (!isPlaying) return;

        const row = getCurrentRow();
        const noteNumber = row[currentNoteIndex];

        // Visuelle Hervorhebung
        updateNoteDisplay(currentNoteIndex);

        // Note spielen
        playNote(noteNumber);

        // Nächste Note vorbereiten
        currentNoteIndex++;

        if (currentNoteIndex >= 12) {
            currentNoteIndex = 0;

            // Zufällig Transformation wechseln (optional)
            if (Math.random() > 0.7) {
                const transforms = ['original', 'retrograde', 'inversion', 'retrogradeInversion'];
                currentTransform = transforms[Math.floor(Math.random() * transforms.length)];
                updateTransformButtons();
                displayRow();
            }
        }

        // Tempo berechnen
        const bpm = parseInt(document.getElementById('tempo').value);
        const interval = 60000 / bpm;

        playInterval = setTimeout(playNextNote, interval);
    }

    /**
     * Gibt die aktuelle Reihenform zurück
     */
    function getCurrentRow() {
        switch (currentTransform) {
            case 'retrograde': return rowData.retrograde;
            case 'inversion': return rowData.inversion;
            case 'retrogradeInversion': return rowData.retrogradeInversion;
            default: return rowData.original;
        }
    }

    /**
     * Aktualisiert die visuelle Darstellung der aktuellen Note
     */
    function updateNoteDisplay(activeIndex) {
        const boxes = document.querySelectorAll('.note-box');
        boxes.forEach((box, index) => {
            box.classList.remove('active');
            if (index < activeIndex) {
                box.classList.add('played');
            } else {
                box.classList.remove('played');
            }
            if (index === activeIndex) {
                box.classList.add('active');
            }
        });
    }

    /**
     * Setzt die Notenanzeige zurück
     */
    function resetNoteDisplay() {
        const boxes = document.querySelectorAll('.note-box');
        boxes.forEach(box => {
            box.classList.remove('active', 'played');
        });
    }

    /**
     * Aktualisiert den Status-Anzeiger
     */
    function updateStatus(playing) {
        const indicator = document.getElementById('statusIndicator');
        const text = document.getElementById('statusText');

        if (playing) {
            indicator.classList.add('playing');
            text.textContent = 'Spielt...';
        } else {
            indicator.classList.remove('playing');
            text.textContent = 'Gestoppt';
        }
    }

    /**
     * Zeigt die Zwölftonreihe an
     */
    function displayRow() {
        const container = document.getElementById('rowDisplay');
        const row = getCurrentRow();

        container.innerHTML = row.map((note, index) => `
            <div class="note-box" data-note="${note}" data-index="${index}">
                ${noteNames[note]}
            </div>
        `).join('');
    }

    /**
     * Zeigt die Zwölftonmatrix an
     */
    function displayMatrix() {
        const container = document.getElementById('matrix');
        const matrix = rowData.matrix;

        container.innerHTML = matrix.flat().map(note => `
            <div class="matrix-cell">${noteNames[note]}</div>
        `).join('');
    }

    /**
     * Aktualisiert die Transformations-Buttons
     */
    function updateTransformButtons() {
        document.querySelectorAll('.transform-btn').forEach(btn => {
            btn.classList.toggle('active', btn.dataset.transform === currentTransform);
        });
    }

    /**
     * Generiert eine neue Reihe vom Server
     */
    async function generateNewRow() {
        try {
            const response = await fetch('?action=generate');
            rowData = await response.json();
            displayRow();
            displayMatrix();
            currentNoteIndex = 0;
            resetNoteDisplay();
        } catch (error) {
            console.error('Fehler beim Generieren:', error);
        }
    }

    /**
     * Audio-Visualisierung
     */
    function startVisualization() {
        const canvas = document.getElementById('visualizer');
        const ctx = canvas.getContext('2d');

        function resize() {
            canvas.width = canvas.offsetWidth * window.devicePixelRatio;
            canvas.height = canvas.offsetHeight * window.devicePixelRatio;
            ctx.scale(window.devicePixelRatio, window.devicePixelRatio);
        }
        resize();
        window.addEventListener('resize', resize);

        const bufferLength = analyser.frequencyBinCount;
        const dataArray = new Uint8Array(bufferLength);

        function draw() {
            requestAnimationFrame(draw);

            analyser.getByteTimeDomainData(dataArray);

            const width = canvas.offsetWidth;
            const height = canvas.offsetHeight;

            // Hintergrund mit Fade-Effekt
            ctx.fillStyle = 'rgba(0, 0, 0, 0.1)';
            ctx.fillRect(0, 0, width, height);

            // Wellenform zeichnen
            ctx.lineWidth = 2;
            ctx.strokeStyle = 'rgba(0, 212, 255, 0.8)';
            ctx.beginPath();

            const sliceWidth = width / bufferLength;
            let x = 0;

            for (let i = 0; i < bufferLength; i++) {
                const v = dataArray[i] / 128.0;
                const y = v * height / 2;

                if (i === 0) {
                    ctx.moveTo(x, y);
                } else {
                    ctx.lineTo(x, y);
                }

                x += sliceWidth;
            }

            ctx.lineTo(width, height / 2);
            ctx.stroke();

            // Frequenzspektrum
            analyser.getByteFrequencyData(dataArray);
            const barWidth = (width / 64) * 1.5;
            let barX = 0;

            for (let i = 0; i < 64; i++) {
                const barHeight = (dataArray[i] / 255) * height * 0.7;

                const gradient = ctx.createLinearGradient(0, height - barHeight, 0, height);
                gradient.addColorStop(0, 'rgba(123, 44, 191, 0.8)');
                gradient.addColorStop(1, 'rgba(0, 212, 255, 0.8)');

                ctx.fillStyle = gradient;
                ctx.fillRect(barX, height - barHeight, barWidth - 2, barHeight);

                barX += barWidth;
            }
        }

        draw();
    }

    // Event Listeners
    document.getElementById('startBtn').addEventListener('click', startPlaying);
    document.getElementById('stopBtn').addEventListener('click', stopPlaying);
    document.getElementById('newRowBtn').addEventListener('click', generateNewRow);

    document.querySelectorAll('.transform-btn').forEach(btn => {
        btn.addEventListener('click', () => {
            currentTransform = btn.dataset.transform;
            updateTransformButtons();
            displayRow();
            currentNoteIndex = 0;
            resetNoteDisplay();
        });
    });

    // Slider Updates
    document.getElementById('tempo').addEventListener('input', e => {
        document.getElementById('tempoValue').textContent = e.target.value;
    });

    document.getElementById('octave').addEventListener('input', e => {
        document.getElementById('octaveValue').textContent = e.target.value;
    });

    document.getElementById('reverb').addEventListener('input', e => {
        document.getElementById('reverbValue').textContent = e.target.value;
        if (audioContext) updateReverbMix();
    });

    document.getElementById('attack').addEventListener('input', e => {
        document.getElementById('attackValue').textContent = (e.target.value / 1000).toFixed(2);
    });

    document.getElementById('release').addEventListener('input', e => {
        document.getElementById('releaseValue').textContent = (e.target.value / 1000).toFixed(2);
    });

    // Initialisierung
    displayRow();
    displayMatrix();
    </script>
</body>
</html>
