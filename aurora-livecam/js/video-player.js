/**
 * Daily Video Player - Spielt Tagesvideos im Hauptfenster ab
 */
const DailyVideoPlayer = {
    currentVideo: null,
    videoElement: null,

    init: function() {
        this.createPlayerElement();
        this.setupEventListeners();
    },

    createPlayerElement: function() {
        // Player-Container erstellen falls nicht vorhanden
        if (document.getElementById('daily-video-player')) return;

        const container = document.createElement('div');
        container.id = 'daily-video-player';
        container.style.display = 'none';
        container.innerHTML = `
            <video id="daily-video" controls playsinline>
                <source src="" type="video/mp4">
            </video>
            <div class="video-player-controls">
                <button id="dvp-back-live" class="tl-btn tl-back-btn">
                    <i class="fas fa-video"></i> Zurück zu Live
                </button>
                <a id="dvp-download" class="button" style="display:none;">
                    <i class="fas fa-download"></i> Download
                </a>
            </div>
        `;

        // Nach dem Webcam-Player einfügen
        const videoContainer = document.querySelector('.video-container');
        if (videoContainer) {
            videoContainer.appendChild(container);
        }

        this.videoElement = document.getElementById('daily-video');
    },

    setupEventListeners: function() {
        document.getElementById('dvp-back-live')?.addEventListener('click', () => this.backToLive());

        // Video-Ende Event
        this.videoElement?.addEventListener('ended', () => {
            // Optional: Automatisch zurück zu Live
        });
    },

    playVideo: function(videoPath, allowDownload = true) {
        this.currentVideo = videoPath;

        // Andere Player verstecken
        document.getElementById('webcam-player').style.display = 'none';
        document.getElementById('timelapse-viewer').style.display = 'none';
        document.getElementById('timelapse-controls')?.style.display = 'none';

        // Diesen Player anzeigen
        const player = document.getElementById('daily-video-player');
        player.style.display = 'block';

        // Video laden
        this.videoElement.src = videoPath;
        this.videoElement.load();
        this.videoElement.play();

        // Download-Button
        const downloadBtn = document.getElementById('dvp-download');
        if (allowDownload && downloadBtn) {
            downloadBtn.style.display = 'inline-block';
            downloadBtn.href = videoPath;
            downloadBtn.download = videoPath.split('/').pop();
        } else if (downloadBtn) {
            downloadBtn.style.display = 'none';
        }
    },

    backToLive: function() {
        // Video stoppen
        if (this.videoElement) {
            this.videoElement.pause();
            this.videoElement.src = '';
        }

        // Player verstecken
        document.getElementById('daily-video-player').style.display = 'none';

        // Live-Stream anzeigen
        document.getElementById('webcam-player').style.display = 'block';
    },

    // Wird vom Kalender aufgerufen
    handleCalendarClick: function(videoPath, playInPlayer, allowDownload) {
        if (playInPlayer) {
            this.playVideo(videoPath, allowDownload);
        } else {
            // Nur Download
            window.location.href = videoPath;
        }
    }
};

// Initialisierung
document.addEventListener('DOMContentLoaded', function() {
    DailyVideoPlayer.init();
});
