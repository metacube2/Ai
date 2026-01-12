(() => {
    const config = window.zoomConfig || {};
    if (!config.enabled) return;

    const slider = document.getElementById('zoom-range');
    const valueEl = document.getElementById('zoom-value');
    if (!slider || !valueEl) return;

    const minZoom = Number(config.minZoom || 1);
    const maxZoom = Number(config.maxZoom || 100);
    const defaultZoom = Number(config.defaultZoom || 1);

    slider.min = minZoom;
    slider.max = maxZoom;
    slider.value = defaultZoom;

    const targets = [
        document.getElementById('webcam-player'),
        document.getElementById('timelapse-image'),
        document.getElementById('daily-video')
    ].filter(Boolean);

    const applyZoom = (zoomValue) => {
        const zoom = Math.max(minZoom, Math.min(maxZoom, zoomValue));
        valueEl.textContent = `${zoom}x`;
        targets.forEach((el) => {
            el.style.transform = `scale(${zoom})`;
            el.style.transformOrigin = 'center center';
        });
    };

    applyZoom(defaultZoom);

    slider.addEventListener('input', (event) => {
        applyZoom(Number(event.target.value));
    });
})();
