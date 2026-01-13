/**
 * Video Zoom Controller - Zoom für alle Video-Modi
 */
let currentZoom = 1;

function applyZoom(zoomValue) {
    const config = window.zoomConfig || { minZoom: 1, maxZoom: 4 };
    currentZoom = Math.max(config.minZoom, Math.min(config.maxZoom, zoomValue));

    const valueEl = document.getElementById('zoom-value');
    const slider = document.getElementById('zoom-range');

    if (valueEl) valueEl.textContent = currentZoom + 'x';
    if (slider) slider.value = currentZoom;

    // Alle Video-Elemente zoomen
    const targets = [
        document.getElementById('webcam-player'),
        document.getElementById('timelapse-image'),
        document.getElementById('daily-video')
    ].filter(Boolean);

    targets.forEach((el) => {
        el.style.transform = `scale(${currentZoom})`;
        el.style.transformOrigin = 'center center';
        el.style.transition = 'transform 0.2s ease';
    });
}

function adjustZoom(delta) {
    applyZoom(currentZoom + delta);
}

function resetZoom() {
    applyZoom(1);
}

// Initialisierung
document.addEventListener('DOMContentLoaded', function() {
    const config = window.zoomConfig || {};
    if (!config.enabled) return;

    const slider = document.getElementById('zoom-range');
    if (slider) {
        slider.addEventListener('input', (event) => {
            applyZoom(Number(event.target.value));
        });
    }

    // Initial zoom anwenden (ohne transform bei 1x)
    currentZoom = config.defaultZoom || 1;
    if (currentZoom !== 1) {
        applyZoom(currentZoom);
    }
});
