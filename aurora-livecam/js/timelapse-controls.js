/**
 * Timelapse Controller mit Slider, Geschwindigkeit und Rückwärts
 */
const TimelapseController = {
    imageFiles: [],
    currentIndex: 0,
    isPlaying: false,
    isReverse: false,
    speed: 1,
    availableSpeeds: [1, 10, 100],
    intervalId: null,
    baseInterval: 200, // ms bei 1x

    init: function(imageFilesArray) {
        this.imageFiles = imageFilesArray;
        this.setupControls();
        this.updateSlider();
    },

    setupControls: function() {
        const container = document.getElementById('timelapse-controls');
        if (!container) return;

        container.innerHTML = `
            <div class="timelapse-control-bar">
                <button id="tl-play-pause" class="tl-btn" title="Play/Pause">
                    <i class="fas fa-play"></i>
                </button>
                <button id="tl-reverse" class="tl-btn" title="Rückwärts">
                    <i class="fas fa-backward"></i>
                </button>
                <div class="tl-slider-container">
                    <input type="range" id="tl-slider" min="0" max="100" value="0">
                    <span id="tl-time-display">00:00:00</span>
                </div>
                <div class="tl-speed-container">
                    <button id="tl-speed" class="tl-btn tl-speed-btn">1x</button>
                </div>
                <button id="tl-back-live" class="tl-btn tl-back-btn" title="Zurück zu Live">
                    <i class="fas fa-video"></i> Live
                </button>
            </div>
        `;

        // Event Listeners
        document.getElementById('tl-play-pause').onclick = () => this.togglePlay();
        document.getElementById('tl-reverse').onclick = () => this.toggleReverse();
        document.getElementById('tl-speed').onclick = () => this.cycleSpeed();
        document.getElementById('tl-back-live').onclick = () => this.backToLive();

        const slider = document.getElementById('tl-slider');
        slider.max = this.imageFiles.length - 1;
        slider.oninput = (e) => this.seekTo(parseInt(e.target.value));
    },

    togglePlay: function() {
        this.isPlaying = !this.isPlaying;
        const btn = document.getElementById('tl-play-pause');
        btn.innerHTML = this.isPlaying ? '<i class="fas fa-pause"></i>' : '<i class="fas fa-play"></i>';

        if (this.isPlaying) {
            this.startPlayback();
        } else {
            this.stopPlayback();
        }
    },

    toggleReverse: function() {
        this.isReverse = !this.isReverse;
        const btn = document.getElementById('tl-reverse');
        btn.classList.toggle('active', this.isReverse);
        btn.innerHTML = this.isReverse ?
            '<i class="fas fa-forward"></i>' :
            '<i class="fas fa-backward"></i>';
    },

    cycleSpeed: function() {
        const idx = this.availableSpeeds.indexOf(this.speed);
        this.speed = this.availableSpeeds[(idx + 1) % this.availableSpeeds.length];
        document.getElementById('tl-speed').textContent = this.speed + 'x';

        if (this.isPlaying) {
            this.stopPlayback();
            this.startPlayback();
        }
    },

    startPlayback: function() {
        const interval = this.baseInterval / this.speed;
        this.intervalId = setInterval(() => this.nextFrame(), interval);
    },

    stopPlayback: function() {
        if (this.intervalId) {
            clearInterval(this.intervalId);
            this.intervalId = null;
        }
    },

    nextFrame: function() {
        if (this.isReverse) {
            this.currentIndex--;
            if (this.currentIndex < 0) this.currentIndex = this.imageFiles.length - 1;
        } else {
            this.currentIndex++;
            if (this.currentIndex >= this.imageFiles.length) this.currentIndex = 0;
        }
        this.showFrame(this.currentIndex);
    },

    seekTo: function(index) {
        this.currentIndex = index;
        this.showFrame(index);
    },

    showFrame: function(index) {
        const img = document.getElementById('timelapse-image');
        if (img && this.imageFiles[index]) {
            img.src = this.imageFiles[index];
        }
        this.updateSlider();
        this.updateTimeDisplay();
    },

    updateSlider: function() {
        const slider = document.getElementById('tl-slider');
        if (slider) slider.value = this.currentIndex;
    },

    updateTimeDisplay: function() {
        const display = document.getElementById('tl-time-display');
        if (!display || !this.imageFiles[this.currentIndex]) return;

        const filename = this.imageFiles[this.currentIndex];
        const match = filename.match(/(\d{4})(\d{2})(\d{2})_(\d{2})(\d{2})(\d{2})/);
        if (match) {
            const [_, y, m, d, h, min, s] = match;
            display.textContent = `${d}.${m}.${y} ${h}:${min}:${s}`;
        }
    },

    backToLive: function() {
        this.stopPlayback();
        this.isPlaying = false;

        // Live-Video wieder anzeigen
        document.getElementById('timelapse-viewer').style.display = 'none';
        document.getElementById('webcam-player').style.display = 'block';
        document.getElementById('timelapse-button').textContent = 'Wochenzeitraffer';

        // Controls verstecken
        const controls = document.getElementById('timelapse-controls');
        if (controls) controls.style.display = 'none';
    },

    show: function() {
        document.getElementById('timelapse-viewer').style.display = 'block';
        document.getElementById('webcam-player').style.display = 'none';
        document.getElementById('daily-video-player').style.display = 'none';

        const controls = document.getElementById('timelapse-controls');
        if (controls) controls.style.display = 'block';

        this.currentIndex = 0;
        this.showFrame(0);
    }
};
