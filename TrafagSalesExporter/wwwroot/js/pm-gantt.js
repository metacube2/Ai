// Trafag Projekte: Gantt-Ziehen (2026-10-29).
// Blazor rendert Raster, Skala, Balken und Pfeile (PmSheetView.razor). Dieses Skript macht nur drei Dinge:
//  1. Balken ziehen (verschieben, Start- und Endgriff) und Abhaengigkeit per Ziehen vom Punkt am Balkenende.
//     Die Vorschau ist ein eigenes Element in .pm-sh-ghosts; der Blazor-Balken bleibt unberuehrt. Erst beim
//     Loslassen geht EIN Aufruf an .NET (BarDropped bzw. LinkDropped) mit ganzen Tagesindizes ab Skalenanfang.
//  2. Senkrechten Bildlauf von Raster und Gantt gleich halten; Trennlinie zwischen beiden ziehen.
//  3. Pfeiltasten im Raster sollen nicht die Seite rollen.
// Datumsrechnung nur mit Date.UTC, damit die Sommerzeitumstellung keinen Tag verschiebt.
window.pmGantt = (function () {
    const states = new WeakMap();
    const DAY = 86400000;
    const THRESHOLD = 3;

    function parseIso(iso) {
        const p = (iso || '').split('-').map(Number);
        return Date.UTC(p[0], p[1] - 1, p[2]);
    }

    function fmt(ms) {
        const d = new Date(ms);
        const two = n => (n < 10 ? '0' : '') + n;
        return two(d.getUTCDate()) + '.' + two(d.getUTCMonth() + 1) + '.' + d.getUTCFullYear();
    }

    // Freie Tage als Tagesindizes (vom Server: Wochenende und Zuercher Feiertage).
    function offDays(gantt) {
        const set = new Set();
        (gantt.dataset.off || '').split(',').forEach(s => { if (s !== '') set.add(Number(s)); });
        return set;
    }

    // Gleiche Regeln wie PmSchedule: auf Arbeitstage einrasten, Dauer in Arbeitstagen.
    // Ausserhalb der Skala ist nur das Wochenende bekannt.
    function isWork(i, off, fromMs) {
        if (off.has(i)) return false;
        const dow = new Date(fromMs + i * DAY).getUTCDay();
        return dow !== 0 && dow !== 6;
    }
    function snapForward(i, off, fromMs) { let n = 0; while (!isWork(i, off, fromMs) && n++ < 30) i++; return i; }
    function snapBackward(i, off, fromMs) { let n = 0; while (!isWork(i, off, fromMs) && n++ < 30) i--; return i; }
    function endFor(start, duration, off, fromMs) {
        let i = snapForward(start, off, fromMs);
        let left = Math.max(duration, 1) - 1;
        let guard = 0;
        while (left > 0 && guard++ < 5000) { i++; if (isWork(i, off, fromMs)) left--; }
        return i;
    }

    function init(root, dotnet) {
        if (!root || states.has(root)) return;
        const st = { root, dotnet, drag: null, syncing: false };

        st.onDown = e => onPointerDown(st, e);
        st.onMove = e => onPointerMove(st, e);
        st.onUp = e => onPointerUp(st, e, false);
        st.onKey = e => onKeyDown(st, e);
        st.onDragKey = e => { if (e.key === 'Escape') cancelDrag(st); };
        st.onScroll = e => onScroll(st, e);
        st.onDbl = e => onDoubleClick(st, e);

        root.addEventListener('pointerdown', st.onDown);
        root.addEventListener('dblclick', st.onDbl);
        root.addEventListener('keydown', st.onKey);
        // scroll blubbert nicht: in der Capture-Phase am Wurzelelement abfangen.
        root.addEventListener('scroll', st.onScroll, true);
        states.set(root, st);
        restoreSplit(root);
    }

    function dispose(root) {
        const st = root && states.get(root);
        if (!st) return;
        endDrag(st);
        root.removeEventListener('pointerdown', st.onDown);
        root.removeEventListener('dblclick', st.onDbl);
        root.removeEventListener('keydown', st.onKey);
        root.removeEventListener('scroll', st.onScroll, true);
        if (st.tip && st.tip.parentNode) st.tip.parentNode.removeChild(st.tip);
        states.delete(root);
    }

    function scrollToX(root, x) {
        const pane = root && root.querySelector('.pm-sh-ganttpane');
        if (pane) pane.scrollLeft = Math.max(0, x - pane.clientWidth / 3);
        // Die Trennlinie gibt es erst, wenn das Raster geladen ist.
        restoreSplit(root);
    }

    // ---------- Bildlauf und Tastatur ----------

    function onScroll(st, e) {
        const t = e.target;
        if (!(t instanceof Element) || st.syncing) return;
        let other = null;
        if (t.classList.contains('pm-sh-gridpane')) other = st.root.querySelector('.pm-sh-ganttpane');
        else if (t.classList.contains('pm-sh-ganttpane')) other = st.root.querySelector('.pm-sh-gridpane');
        if (other && other.scrollTop !== t.scrollTop) {
            st.syncing = true;
            other.scrollTop = t.scrollTop;
            requestAnimationFrame(() => { st.syncing = false; });
        }
    }

    function onKeyDown(st, e) {
        const t = e.target;
        if (t instanceof Element && t.classList.contains('pm-sh-gridpane') &&
            (e.key === 'ArrowUp' || e.key === 'ArrowDown' || e.key === 'ArrowLeft' || e.key === 'ArrowRight')) {
            e.preventDefault();
        }
    }

    function onDoubleClick(st, e) {
        const bar = e.target instanceof Element ? e.target.closest('.pm-sh-bar[data-id]') : null;
        if (bar) st.dotnet.invokeMethodAsync('BarOpen', Number(bar.dataset.id)).catch(() => { });
    }

    // ---------- Trennlinie ----------

    function restoreSplit(root) {
        try {
            const w = localStorage.getItem('pm-sh-split');
            const grid = root.querySelector('.pm-sh-gridpane');
            if (w && grid && !grid.style.flexBasis) grid.style.flexBasis = w + 'px';
        } catch (_) { /* ohne Speicher gilt die Vorgabe aus pm-sheet.css */ }
    }

    function startSplit(st, e, splitter) {
        const grid = st.root.querySelector('.pm-sh-gridpane');
        if (!grid) return;
        const x0 = e.clientX;
        const w0 = grid.getBoundingClientRect().width;
        splitter.classList.add('pm-sh-active');
        st.drag = {
            kind: 'split',
            move: ev => { grid.style.flexBasis = Math.max(160, w0 + ev.clientX - x0) + 'px'; },
            done: () => {
                splitter.classList.remove('pm-sh-active');
                try { localStorage.setItem('pm-sh-split', String(Math.round(grid.getBoundingClientRect().width))); } catch (_) { }
            }
        };
        listen(st);
        e.preventDefault();
    }

    // ---------- Balken ziehen ----------

    function listen(st) {
        window.addEventListener('pointermove', st.onMove);
        window.addEventListener('pointerup', st.onUp);
        window.addEventListener('pointercancel', st.onUp);
        window.addEventListener('keydown', st.onDragKey);
    }

    function unlisten(st) {
        window.removeEventListener('pointermove', st.onMove);
        window.removeEventListener('pointerup', st.onUp);
        window.removeEventListener('pointercancel', st.onUp);
        window.removeEventListener('keydown', st.onDragKey);
    }

    function onPointerDown(st, e) {
        if (e.button !== 0 || !(e.target instanceof Element) || st.drag) return;
        const splitter = e.target.closest('.pm-sh-splitter');
        if (splitter) { startSplit(st, e, splitter); return; }

        const bar = e.target.closest('.pm-sh-bar[data-id]');
        if (!bar) return;
        const gantt = bar.closest('.pm-sh-gantt');
        if (!gantt) return;
        const canDrag = bar.dataset.drag === '1';
        let mode = 'move';
        if (e.target.closest('.pm-sh-link')) mode = 'link';
        else if (e.target.closest('.pm-sh-grip-l')) mode = 'start';
        else if (e.target.closest('.pm-sh-grip-r')) mode = 'end';

        const fromIso = gantt.dataset.from;
        st.drag = {
            kind: 'bar',
            mode: canDrag ? mode : 'click',
            bar, gantt,
            id: Number(bar.dataset.id),
            s: Number(bar.dataset.s),
            e: Number(bar.dataset.e),
            dur: Number(bar.dataset.dur),
            ms: bar.dataset.ms === '1',
            // Bei jedem Ziehen neu lesen: der Zoom aendert die Tagesbreite.
            dw: Number(gantt.dataset.dw) || 12,
            fromIso,
            fromMs: parseIso(fromIso),
            off: offDays(gantt),
            x0: e.clientX,
            y0: e.clientY,
            moved: false,
            ns: Number(bar.dataset.s),
            ne: Number(bar.dataset.e)
        };
        listen(st);
        if (canDrag) e.preventDefault();
    }

    function onPointerMove(st, e) {
        const d = st.drag;
        if (!d) return;
        if (d.kind === 'split') { d.move(e); return; }
        const dx = e.clientX - d.x0;
        if (!d.moved && Math.abs(dx) < THRESHOLD && Math.abs(e.clientY - d.y0) < THRESHOLD) return;
        if (d.mode === 'click') return;
        if (!d.moved) { d.moved = true; beginPreview(d); }
        if (d.mode === 'link') { drawLink(d, e); return; }

        const days = Math.round(dx / d.dw);
        if (d.mode === 'move') {
            d.ns = snapForward(d.s + days, d.off, d.fromMs);
            d.ne = d.ms ? d.ns : endFor(d.ns, d.dur, d.off, d.fromMs);
        } else if (d.mode === 'start') {
            d.ns = Math.min(snapForward(d.s + days, d.off, d.fromMs), d.e);
            d.ne = d.e;
        } else {
            d.ns = d.s;
            d.ne = Math.max(snapBackward(d.e + days, d.off, d.fromMs), d.s);
        }
        placeGhost(d);
        showTip(st, d, e);
    }

    function onPointerUp(st, e, cancelled) {
        const d = st.drag;
        if (!d) return;
        unlisten(st);
        st.drag = null;
        if (d.kind === 'split') { d.done(); return; }
        hideTip(st);

        if (!d.moved) {
            clearPreview(d);
            if (!cancelled) st.dotnet.invokeMethodAsync('BarClicked', d.id).catch(() => { });
            return;
        }
        if (cancelled) { clearPreview(d); return; }

        if (d.mode === 'link') {
            clearPreview(d);
            const hit = document.elementFromPoint(e.clientX, e.clientY);
            const target = hit && hit.closest ? hit.closest('.pm-sh-bar[data-id]') : null;
            const to = target ? Number(target.dataset.id) : 0;
            if (to && to !== d.id) st.dotnet.invokeMethodAsync('LinkDropped', d.id, to).catch(() => { });
            return;
        }
        if (d.ns === d.s && d.ne === d.e) { clearPreview(d); return; }
        // Vorschau stehen lassen, bis der Server den neuen Stand gerendert hat; dann entfernen.
        st.dotnet.invokeMethodAsync('BarDropped', d.id, d.mode, d.fromIso, d.ns, d.ne)
            .catch(() => { })
            .finally(() => setTimeout(() => clearPreview(d), 60));
    }

    function cancelDrag(st) {
        if (st.drag) onPointerUp(st, { clientX: 0, clientY: 0 }, true);
    }

    function endDrag(st) {
        if (!st.drag) return;
        unlisten(st);
        if (st.drag.kind === 'bar') clearPreview(st.drag);
        st.drag = null;
        hideTip(st);
    }

    // ---------- Vorschau ----------

    function layer(d) { return d.gantt.querySelector('.pm-sh-ghosts'); }

    function beginPreview(d) {
        d.bar.setAttribute('data-ghosted', '1');
        const host = layer(d);
        if (!host) return;
        const row = d.bar.parentElement;
        const top = row ? row.offsetTop : 0;
        if (d.mode === 'link') {
            const svg = document.createElementNS('http://www.w3.org/2000/svg', 'svg');
            svg.setAttribute('class', 'pm-sh-ghost-svg');
            svg.setAttribute('width', String(d.gantt.scrollWidth));
            svg.setAttribute('height', String(host.parentElement ? host.parentElement.offsetHeight : 0));
            const line = document.createElementNS('http://www.w3.org/2000/svg', 'path');
            line.setAttribute('class', 'pm-sh-ghost-link');
            svg.appendChild(line);
            host.appendChild(svg);
            d.ghost = svg;
            d.line = line;
            d.lx = d.bar.offsetLeft + d.bar.offsetWidth;
            d.ly = top + d.bar.offsetTop + d.bar.offsetHeight / 2;
            return;
        }
        const ghost = document.createElement('div');
        ghost.className = 'pm-sh-ghost' + (d.ms ? ' pm-sh-ghost-ms' : '');
        ghost.style.top = (top + d.bar.offsetTop) + 'px';
        ghost.style.height = d.bar.offsetHeight + 'px';
        host.appendChild(ghost);
        d.ghost = ghost;
        placeGhost(d);
    }

    function placeGhost(d) {
        if (!d.ghost || d.mode === 'link') return;
        if (d.ms) {
            d.ghost.style.left = (d.ns * d.dw + d.dw / 2 - 8) + 'px';
            d.ghost.style.width = '16px';
        } else {
            d.ghost.style.left = (d.ns * d.dw) + 'px';
            d.ghost.style.width = ((d.ne - d.ns + 1) * d.dw) + 'px';
        }
    }

    function drawLink(d, e) {
        const host = layer(d);
        if (!d.line || !host) return;
        const box = host.getBoundingClientRect();
        d.line.setAttribute('d', 'M' + d.lx + ' ' + d.ly + ' L' + (e.clientX - box.left) + ' ' + (e.clientY - box.top));
    }

    function clearPreview(d) {
        if (d.bar) d.bar.removeAttribute('data-ghosted');
        if (d.ghost && d.ghost.parentNode) d.ghost.parentNode.removeChild(d.ghost);
        d.ghost = null;
    }

    function showTip(st, d, e) {
        if (!st.tip) {
            st.tip = document.createElement('div');
            st.tip.className = 'pm-sh-tip';
            document.body.appendChild(st.tip);
        }
        const start = d.fromMs + d.ns * DAY;
        const end = d.fromMs + d.ne * DAY;
        st.tip.textContent = d.ms || d.ns === d.ne ? fmt(start) : fmt(start) + ' – ' + fmt(end);
        st.tip.style.left = (e.clientX + 14) + 'px';
        st.tip.style.top = (e.clientY + 16) + 'px';
        st.tip.style.display = 'block';
    }

    function hideTip(st) {
        if (st.tip) st.tip.style.display = 'none';
    }

    return { init, dispose, scrollToX };
})();
