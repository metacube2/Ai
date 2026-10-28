/**
 * Peakfinder fuer das Live-Video der Aurora-Livecam (2026-10-28).
 *
 * Legt anklickbare Gipfelmarken ueber #webcam-player. Daten: window.AURORA_PEAKFINDER
 * aus js/peakfinder-daten.js (erzeugt von aurora-livecam/peakfinder/kalibrieren.py),
 * Bildkoordinaten in Pixeln des Kamerabilds 3072 x 1728 (Referenzlage). Die Kamera
 * verschiebt sich gelegentlich um einige Dutzend Pixel; den aktuellen Versatz misst
 * der Dienst caurora-peakfinder auf cAurora und schreibt ihn nach peakfinder-versatz.txt.
 *
 * Die Ebene liegt IM #live-video-wrapper und zoomt/verschiebt sich daher mit
 * video-zoom.js. Marken sind <button>, video-zoom.js laesst Klicks darauf in Ruhe.
 * Keine externen Abhaengigkeiten.
 */
(() => {
    'use strict';
    const D = window.AURORA_PEAKFINDER;
    if (!D || !Array.isArray(D.gipfel)) return;

    const TEXTE = {
        de: { knopf: 'Gipfel', aus: 'Gipfelnamen ausblenden', an: 'Gipfelnamen einblenden', richtung: 'Richtung', rand: 'Lage am Bildrand ungenau', vollbild: 'Vollbild', zu: 'Schliessen' },
        en: { knopf: 'Peaks', aus: 'Hide peak names', an: 'Show peak names', richtung: 'Bearing', rand: 'Position near image edge approximate', vollbild: 'Full screen', zu: 'Close' },
        fr: { knopf: 'Sommets', aus: 'Masquer les sommets', an: 'Afficher les sommets', richtung: 'Direction', rand: 'Position approximative au bord', vollbild: 'Plein écran', zu: 'Fermer' },
        it: { knopf: 'Vette', aus: 'Nascondi le vette', an: 'Mostra le vette', richtung: 'Direzione', rand: 'Posizione al bordo approssimativa', vollbild: 'Schermo intero', zu: 'Chiudi' },
        es: { knopf: 'Cumbres', aus: 'Ocultar cumbres', an: 'Mostrar cumbres', richtung: 'Dirección', rand: 'Posición aproximada en el borde', vollbild: 'Pantalla completa', zu: 'Cerrar' },
        zh: { knopf: '山峰', aus: '隐藏山峰名称', an: '显示山峰名称', richtung: '方位', rand: '边缘位置不精确', vollbild: '全屏', zu: '关闭' },
    };
    const text = (k) => {
        const l = (document.documentElement.lang || 'de').slice(0, 2);
        return (TEXTE[l] || TEXTE.de)[k];
    };
    const zahl = (v, n) => {
        const l = document.documentElement.lang || 'de';
        try { return v.toLocaleString(l === 'de' ? 'de-CH' : l, { maximumFractionDigits: n, minimumFractionDigits: n }); } catch (e) { return v.toFixed(n); }
    };
    const merken = (wert) => { try { localStorage.setItem('auroraGipfel', wert ? '1' : '0'); } catch (e) { /* privat */ } };
    const gemerkt = () => { try { return localStorage.getItem('auroraGipfel'); } catch (e) { return null; } };

    const BW = (D.bild && D.bild.breite) || 3072;
    const BH = (D.bild && D.bild.hoehe) || 1728;
    let versatz = { dx: (D.versatz_standard || {}).dx || 0, dy: (D.versatz_standard || {}).dy || 0 };
    let an = gemerkt() !== '0';
    let zoom = 1;
    let offen = null;   // Index des Gipfels mit offener Karte
    let ebene, karte, knopf, video, wrapper, container;

    function start() {
        wrapper = document.getElementById('live-video-wrapper');
        video = document.getElementById('webcam-player');
        if (!wrapper || !video) return;
        container = wrapper.closest('.video-container') || wrapper.parentElement;

        ebene = document.createElement('div');
        ebene.className = 'pf-ebene';
        ebene.setAttribute('aria-label', text('knopf'));
        wrapper.appendChild(ebene);

        karte = document.createElement('div');
        karte.className = 'pf-karte';
        karte.hidden = true;
        ebene.appendChild(karte);

        D.gipfel.forEach((g, i) => {
            const b = document.createElement('button');
            b.type = 'button';
            b.className = 'pf-marke';
            b.dataset.i = i;
            b.setAttribute('aria-label', g.name + ', ' + g.hoehe + ' m');
            b.innerHTML = '<span class="pf-punkt"></span><span class="pf-name"></span>';
            b.querySelector('.pf-name').textContent = g.name;
            b.addEventListener('click', (e) => { e.stopPropagation(); zeigeKarte(i); });
            ebene.appendChild(b);
            g._el = b;
        });

        const leiste = document.createElement('div');
        leiste.className = 'pf-leiste';
        knopf = document.createElement('button');
        knopf.type = 'button';
        knopf.className = 'pf-knopf';
        knopf.addEventListener('click', (e) => { e.stopPropagation(); schalten(!an); });
        leiste.appendChild(knopf);

        const voll = container.requestFullscreen || container.webkitRequestFullscreen;
        if (voll) {
            const vb = document.createElement('button');
            vb.type = 'button';
            vb.className = 'pf-vollbild';
            vb.textContent = '⛶';
            vb.title = text('vollbild');
            vb.setAttribute('aria-label', text('vollbild'));
            vb.addEventListener('click', (e) => {
                e.stopPropagation();
                const ist = document.fullscreenElement || document.webkitFullscreenElement;
                if (ist) (document.exitFullscreen || document.webkitExitFullscreen).call(document);
                else voll.call(container);
            });
            leiste.appendChild(vb);
        }
        container.appendChild(leiste);

        document.addEventListener('click', (e) => { if (offen !== null && !karte.contains(e.target)) schliesseKarte(); });
        document.addEventListener('keydown', (e) => { if (e.key === 'Escape') schliesseKarte(); });

        if (window.ResizeObserver) new ResizeObserver(lage).observe(wrapper);
        window.addEventListener('resize', lage);
        video.addEventListener('loadedmetadata', lage);
        document.addEventListener('fullscreenchange', lage);
        document.addEventListener('webkitfullscreenchange', lage);
        new MutationObserver(zoomLesen).observe(wrapper, { attributes: true, attributeFilter: ['style'] });
        new MutationObserver(() => schalten(an)).observe(document.documentElement, { attributes: true, attributeFilter: ['lang'] });
        setInterval(sichtbarkeit, 1000);

        schalten(an);
        zoomLesen();
        versatzHolen();
        setInterval(versatzHolen, 5 * 60 * 1000);
    }

    // Inhaltsrechteck des Videos (object-fit: contain) in Wrapper-Koordinaten
    function lage() {
        if (!ebene) return;
        const ew = video.clientWidth, eh = video.clientHeight;
        if (!ew || !eh) return;
        const vw = video.videoWidth || 16, vh = video.videoHeight || 9;
        const s = Math.min(ew / vw, eh / vh);
        const cw = vw * s, ch = vh * s;
        ebene.style.left = (video.offsetLeft + (ew - cw) / 2) + 'px';
        ebene.style.top = (video.offsetTop + (eh - ch) / 2) + 'px';
        ebene.style.width = cw + 'px';
        ebene.style.height = ch + 'px';
        anordnen(cw, ch);
    }

    function zoomLesen() {
        const m = /scale\(([\d.]+)\)/.exec(wrapper.style.transform || '');
        const z = m ? parseFloat(m[1]) : 1;
        if (Math.abs(z - zoom) < 1e-3 && ebene.style.width) return;
        zoom = z > 0 ? z : 1;
        ebene.style.setProperty('--pf-s', String(1 / zoom));
        lage();
    }

    // Marken setzen; nach Rang auswaehlen, damit sich nichts ueberlappt.
    // Abstaende in Bildschirmpixeln, also zeigt Hineinzoomen mehr Namen.
    function anordnen(cw, ch) {
        const sx = cw * zoom, belegtPunkt = [], belegtName = [];
        const reihenfolge = D.gipfel.map((g, i) => i).sort((a, b) => D.gipfel[b].rang - D.gipfel[a].rang);
        for (const i of reihenfolge) {
            const g = D.gipfel[i];
            const x = (g.x + versatz.dx) / BW, y = (g.y + versatz.dy) / BH;
            const el = g._el;
            if (x < 0 || x > 1 || y < 0 || y > 1) { el.hidden = true; continue; }
            el.hidden = false;
            el.style.left = (x * 100) + '%';
            el.style.top = (y * 100) + '%';
            const px = x * sx;
            const breite = Math.min(170, 12 + g.name.length * 6.6);
            const punktFrei = belegtPunkt.every((p) => Math.abs(p - px) >= 11);
            const nameFrei = punktFrei && belegtName.every(([a, b]) => px + breite / 2 + 6 < a || px - breite / 2 - 6 > b);
            el.classList.toggle('pf-still', !punktFrei && i !== offen);
            el.classList.toggle('pf-mit-name', nameFrei && i !== offen);
            el.classList.toggle('pf-unsicher', !!g.unsicher);
            el.classList.toggle('pf-l', x < 0.06);
            el.classList.toggle('pf-r', x > 0.94);
            if (punktFrei) belegtPunkt.push(px);
            if (nameFrei) belegtName.push([px - breite / 2, px + breite / 2]);
        }
        if (offen !== null) karteSetzen();
    }

    function zeigeKarte(i) {
        if (offen === i) { schliesseKarte(); return; }
        offen = i;
        const g = D.gipfel[i];
        karte.innerHTML = '';
        const zu = document.createElement('button');
        zu.type = 'button';
        zu.className = 'pf-zu';
        zu.textContent = '×';
        zu.setAttribute('aria-label', text('zu'));
        zu.addEventListener('click', (e) => { e.stopPropagation(); schliesseKarte(); });
        const t = document.createElement('strong');
        t.textContent = g.name;
        const z = document.createElement('span');
        z.textContent = zahl(g.hoehe, 0).replace(/ | /g, ' ') + ' m · ' + zahl(g.dist_km, 1) + ' km · ' + text('richtung') + ' ' + Math.round(g.az) + '°';
        karte.append(zu, t, z);
        if (g.unsicher) {
            const u = document.createElement('em');
            u.textContent = text('rand');
            karte.append(u);
        }
        karte.hidden = false;
        D.gipfel.forEach((h) => h._el.classList.toggle('pf-aktiv', h === g));
        lage();
    }

    function karteSetzen() {
        const g = D.gipfel[offen];
        const x = (g.x + versatz.dx) / BW, y = (g.y + versatz.dy) / BH;
        karte.style.left = (x * 100) + '%';
        karte.style.top = (y * 100) + '%';
        // an den Raendern nach innen klappen (Wrapper schneidet ab)
        karte.classList.toggle('pf-links', x > 0.75);
        karte.classList.toggle('pf-rechts', x < 0.25);
        karte.classList.toggle('pf-unten', y < 0.3);
    }

    function schliesseKarte() {
        if (offen === null) return;
        offen = null;
        karte.hidden = true;
        D.gipfel.forEach((h) => h._el.classList.remove('pf-aktiv'));
        lage();
    }

    function schalten(wert) {
        an = !!wert;
        merken(an);
        ebene.classList.toggle('pf-aus', !an);
        knopf.textContent = '⛰ ' + text('knopf');
        knopf.setAttribute('aria-pressed', an ? 'true' : 'false');
        knopf.title = an ? text('aus') : text('an');
        if (!an) schliesseKarte();
        lage();
    }

    // Zeitraffer und Tagesvideo liegen ueber dem Live-Bild: Leiste dann ausblenden
    function sichtbarkeit() {
        const andere = ['timelapse-viewer', 'daily-video-player'].some((id) => {
            const e = document.getElementById(id);
            return e && getComputedStyle(e).display !== 'none';
        });
        container.classList.toggle('pf-verdeckt', andere);
    }

    function versatzHolen() {
        if (!window.fetch || !D.versatz_url) return;
        fetch(D.versatz_url + '?t=' + Date.now(), { cache: 'no-store' })
            .then((r) => (r.ok ? r.json() : null))
            .then((v) => {
                if (!v || !isFinite(v.dx) || !isFinite(v.dy) || Math.abs(v.dx) > 150 || Math.abs(v.dy) > 150) return;
                if (v.dx === versatz.dx && v.dy === versatz.dy) return;
                versatz = { dx: v.dx, dy: v.dy };
                lage();
            })
            .catch(() => { /* Standardversatz bleibt */ });
    }

    if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', start);
    else start();
})();
