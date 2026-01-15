/**
 * Video Zoom & Pan Controller
 * - Zoom für alle Video-Modi (Live, Timelapse, Tagesvideo)
 * - Pan-Funktion: Mit Maus den gezoomten Bereich verschieben
 */
(() => {
    const config = window.zoomConfig || {};
    if (!config.enabled) return;

    let currentZoom = 1;
    let panX = 0;
    let panY = 0;
    let isDragging = false;
    let startX = 0;
    let startY = 0;

    const minZoom = Number(config.minZoom || 1);
    const maxZoom = Number(config.maxZoom || 4);
    const defaultZoom = Number(config.defaultZoom || 1);

    const slider = document.getElementById('zoom-range');
    const valueEl = document.getElementById('zoom-value');

    // Finde das aktuell aktive Video-Element
    function getActiveTarget() {
        const webcam = document.getElementById('webcam-player');
        const timelapse = document.getElementById('timelapse-image');
        const daily = document.getElementById('daily-video');
        const timelapseViewer = document.getElementById('timelapse-viewer');
        const dailyPlayer = document.getElementById('daily-video-player');

        // Prüfe welches Element sichtbar ist
        if (dailyPlayer && dailyPlayer.style.display !== 'none' && daily) {
            return daily;
        }
        if (timelapseViewer && timelapseViewer.style.display !== 'none' && timelapse) {
            return timelapse;
        }
        if (webcam) {
            return webcam;
        }
        return null;
    }

    // Wende Zoom und Pan auf das aktive Element an
    function applyTransform() {
        const target = getActiveTarget();
        if (!target) return;

        // Bei Zoom 1x: Kein Pan erlaubt
        if (currentZoom <= 1) {
            panX = 0;
            panY = 0;
        }

        // Begrenzen der Pan-Werte basierend auf Zoom
        const maxPan = (currentZoom - 1) * 50; // Prozent
        panX = Math.max(-maxPan, Math.min(maxPan, panX));
        panY = Math.max(-maxPan, Math.min(maxPan, panY));

        target.style.transform = `scale(${currentZoom}) translate(${panX}%, ${panY}%)`;
        target.style.transformOrigin = 'center center';
        target.style.transition = isDragging ? 'none' : 'transform 0.2s ease';

        // Update UI
        if (valueEl) valueEl.textContent = `${currentZoom.toFixed(1)}x`;
        if (slider) slider.value = currentZoom;
    }

    // Zoom setzen
    function setZoom(value) {
        currentZoom = Math.max(minZoom, Math.min(maxZoom, value));
        applyTransform();
    }

    // Zoom anpassen
    function adjustZoom(delta) {
        setZoom(currentZoom + delta);
    }

    // Zoom zurücksetzen
    function resetZoom() {
        currentZoom = 1;
        panX = 0;
        panY = 0;
        applyTransform();
    }

    // Mouse Events für Pan
    function setupPanEvents() {
        const container = document.querySelector('.video-container');
        if (!container) return;

        container.addEventListener('mousedown', (e) => {
            if (currentZoom <= 1) return;
            isDragging = true;
            startX = e.clientX;
            startY = e.clientY;
            container.style.cursor = 'grabbing';
            e.preventDefault();
        });

        document.addEventListener('mousemove', (e) => {
            if (!isDragging) return;

            const dx = (e.clientX - startX) / 5; // Sensitivität anpassen
            const dy = (e.clientY - startY) / 5;

            panX += dx / currentZoom;
            panY += dy / currentZoom;

            startX = e.clientX;
            startY = e.clientY;

            applyTransform();
        });

        document.addEventListener('mouseup', () => {
            if (isDragging) {
                isDragging = false;
                const container = document.querySelector('.video-container');
                if (container) container.style.cursor = currentZoom > 1 ? 'grab' : 'default';
            }
        });

        // Touch Events für Mobile
        container.addEventListener('touchstart', (e) => {
            if (currentZoom <= 1 || e.touches.length !== 1) return;
            isDragging = true;
            startX = e.touches[0].clientX;
            startY = e.touches[0].clientY;
        }, { passive: true });

        container.addEventListener('touchmove', (e) => {
            if (!isDragging || e.touches.length !== 1) return;

            const dx = (e.touches[0].clientX - startX) / 5;
            const dy = (e.touches[0].clientY - startY) / 5;

            panX += dx / currentZoom;
            panY += dy / currentZoom;

            startX = e.touches[0].clientX;
            startY = e.touches[0].clientY;

            applyTransform();
        }, { passive: true });

        container.addEventListener('touchend', () => {
            isDragging = false;
        });

        // Cursor anpassen bei Zoom
        container.style.cursor = 'default';
    }

    // Slider Events
    function setupSlider() {
        if (!slider) return;

        slider.min = minZoom;
        slider.max = maxZoom;
        slider.step = 0.5;
        slider.value = defaultZoom;

        slider.addEventListener('input', (e) => {
            setZoom(Number(e.target.value));
        });
    }

    // Globale Funktionen für Buttons
    window.adjustZoom = adjustZoom;
    window.resetZoom = resetZoom;
    window.setZoom = setZoom;

    // Initialisierung
    document.addEventListener('DOMContentLoaded', () => {
        setupSlider();
        setupPanEvents();
        currentZoom = defaultZoom;

        // Warte kurz, damit Video-Elemente geladen sind
        setTimeout(() => {
            applyTransform();
        }, 500);

        // Update Cursor bei Zoom-Änderung
        const container = document.querySelector('.video-container');
        if (container) {
            const observer = new MutationObserver(() => {
                container.style.cursor = currentZoom > 1 ? 'grab' : 'default';
            });
        }
    });

    // Bei Moduswechsel Pan zurücksetzen
    window.addEventListener('click', (e) => {
        if (e.target.id === 'timelapse-button' ||
            e.target.closest('#timelapse-button') ||
            e.target.id === 'dvp-back-live' ||
            e.target.closest('.play-link')) {
            panX = 0;
            panY = 0;
            setTimeout(applyTransform, 100);
        }
    });
})();
