/**
 * Video Zoom & Pan Controller
 * Zoomt auf Wrapper-Layer statt direkt auf Video-Elemente
 */
(() => {
    const config = window.zoomConfig || {};
    if (!config.enabled) return;

    let currentZoom = 1;
    let panX = 0;
    let panY = 0;
    let isDragging = false;
    let lastX = 0;
    let lastY = 0;

    const minZoom = Number(config.minZoom || 1);
    const maxZoom = Number(config.maxZoom || 4);

    const slider = document.getElementById('zoom-range');
    const valueEl = document.getElementById('zoom-value');

    // Wrapper-IDs für jeden Modus
    const wrapperIds = ['live-video-wrapper', 'timelapse-wrapper', 'daily-video-wrapper'];

    // Finde den aktuell sichtbaren Wrapper
    function getActiveWrapper() {
        // Prüfe daily-video-player
        const dailyPlayer = document.getElementById('daily-video-player');
        if (dailyPlayer && dailyPlayer.style.display !== 'none') {
            return document.getElementById('daily-video-wrapper');
        }

        // Prüfe timelapse-viewer
        const timelapseViewer = document.getElementById('timelapse-viewer');
        if (timelapseViewer && timelapseViewer.style.display !== 'none') {
            return document.getElementById('timelapse-wrapper');
        }

        // Fallback: Live-Video
        return document.getElementById('live-video-wrapper');
    }

    // Wende Transform auf ALLE Wrapper an (damit beim Wechsel der Zoom erhalten bleibt)
    function applyTransform() {
        // Bei Zoom 1x: Kein Pan
        if (currentZoom <= 1) {
            panX = 0;
            panY = 0;
        }

        // Pan begrenzen basierend auf Zoom
        const maxPan = (currentZoom - 1) * 50;
        panX = Math.max(-maxPan, Math.min(maxPan, panX));
        panY = Math.max(-maxPan, Math.min(maxPan, panY));

        // Transform auf alle Wrapper anwenden
        wrapperIds.forEach(id => {
            const wrapper = document.getElementById(id);
            if (wrapper) {
                wrapper.style.transform = `scale(${currentZoom}) translate(${panX}%, ${panY}%)`;
                wrapper.style.transition = isDragging ? 'none' : 'transform 0.15s ease-out';
            }
        });

        // UI Update
        if (valueEl) valueEl.textContent = `${currentZoom.toFixed(1)}x`;
        if (slider) slider.value = currentZoom;

        // Cursor Update
        updateCursor();
    }

    function updateCursor() {
        const container = document.querySelector('.video-container');
        if (container) {
            if (currentZoom > 1) {
                container.classList.add('zoomed');
            } else {
                container.classList.remove('zoomed');
            }
        }
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

        // Mousedown - Start dragging
        container.addEventListener('mousedown', (e) => {
            if (currentZoom <= 1) return;
            // Ignoriere Klicks auf Controls
            if (e.target.closest('.zoom-controls, button, a')) return;

            isDragging = true;
            lastX = e.clientX;
            lastY = e.clientY;
            e.preventDefault();
        });

        // Mousemove - Dragging
        document.addEventListener('mousemove', (e) => {
            if (!isDragging) return;

            const deltaX = e.clientX - lastX;
            const deltaY = e.clientY - lastY;

            // Sensitivität basierend auf Zoom
            const sensitivity = 0.15 / currentZoom;
            panX += deltaX * sensitivity;
            panY += deltaY * sensitivity;

            lastX = e.clientX;
            lastY = e.clientY;

            applyTransform();
        });

        // Mouseup - Stop dragging
        document.addEventListener('mouseup', () => {
            isDragging = false;
        });

        // Mouse leave
        document.addEventListener('mouseleave', () => {
            isDragging = false;
        });

        // Touch Events für Mobile
        container.addEventListener('touchstart', (e) => {
            if (currentZoom <= 1 || e.touches.length !== 1) return;
            if (e.target.closest('.zoom-controls, button, a')) return;

            isDragging = true;
            lastX = e.touches[0].clientX;
            lastY = e.touches[0].clientY;
        }, { passive: true });

        container.addEventListener('touchmove', (e) => {
            if (!isDragging || e.touches.length !== 1) return;

            const deltaX = e.touches[0].clientX - lastX;
            const deltaY = e.touches[0].clientY - lastY;

            const sensitivity = 0.15 / currentZoom;
            panX += deltaX * sensitivity;
            panY += deltaY * sensitivity;

            lastX = e.touches[0].clientX;
            lastY = e.touches[0].clientY;

            applyTransform();
        }, { passive: true });

        container.addEventListener('touchend', () => {
            isDragging = false;
        });

        // Doppelklick zum Zurücksetzen
        container.addEventListener('dblclick', (e) => {
            if (e.target.closest('.zoom-controls, button, a')) return;
            resetZoom();
        });
    }

    // Slider Setup
    function setupSlider() {
        if (!slider) return;

        slider.min = minZoom;
        slider.max = maxZoom;
        slider.step = 0.5;
        slider.value = 1;

        slider.addEventListener('input', (e) => {
            setZoom(Number(e.target.value));
        });
    }

    // Globale Funktionen
    window.adjustZoom = adjustZoom;
    window.resetZoom = resetZoom;
    window.setZoom = setZoom;

    // Initialisierung
    document.addEventListener('DOMContentLoaded', () => {
        setupSlider();
        setupPanEvents();

        // Initial State
        currentZoom = 1;
        applyTransform();

        console.log('Video Zoom & Pan initialized');
    });
})();
