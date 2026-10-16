// Stuecklisten-Netz als „Gehirn“ (Wunsch Ingo 2026-10-16, Einkauf > Spend-Aufriss, Prototyp).
// three.js direkt (js/vendor/three.min.js, im Cockpit schon geladen), laeuft in Edge und Chrome ohne Internet.
// Erste Fassung mit 3d-force-graph fror bei ~15'000 Straengen ein (ein 3D-Objekt je Strang). Jetzt: alle Straenge in
// EINEM Linienpuffer, alle Punkte in EINEM Punktpuffer, feste Anordnung statt laufender Simulation:
// Endprodukte auf einer gehirnfoermigen Schale (zwei Haelften), jede Baugruppe/Komponente im Schwerpunkt ihrer
// Verwendungen. Teile mit vielen Verwendungen wandern so in die Mitte: die Kreuzpunkte.
// Farbe umschaltbar: LZ-Code, Lieferantenrisiko, Stufe.
(function () {
  const states = new WeakMap();
  const RISK = { "2": "#ff4d4f", "1": "#ffa94d", "3": "#868e96", "0": "#38d9a9", "-1": "#dee2e6" };
  const RISK_TXT = { "2": "überfällige Bestellung", "1": "nur ein Lieferant", "3": "kein Einkauf (24 Monate)", "0": "unkritisch", "-1": "nicht bewertet (nur Eltern)" };
  const ROLE = ["#C8501E", "#ffd43b", "#4dabf7"];
  const ROLE_TXT = ["Endprodukt", "Baugruppe", "Komponente"];

  function esc(s) { return String(s ?? "").replace(/[&<>"]/g, c => ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;" }[c])); }

  function lzColor(code, codes) {
    const i = codes.indexOf(code);
    if (i < 0) return "#6c757d";
    const h = codes.length <= 1 ? 0 : Math.round(i * 260 / (codes.length - 1));
    return `hsl(${h}, 85%, 58%)`;
  }
  function colorOf(st, n) {
    if (st.mode === "risk") return RISK[String(n.risk)] || "#dee2e6";
    if (st.mode === "role") return ROLE[n.role] || "#dee2e6";
    return lzColor(n.lz, st.codes);
  }
  function hash(id) { let h = 2166136261; for (let i = 0; i < id.length; i++) { h ^= id.charCodeAt(i); h = Math.imul(h, 16777619); } return (h >>> 0) / 4294967296; }

  // Feste Anordnung wie ein Gehirn: Ellipsoid mit zwei Haelften und Spalt, Windungen ueber eine Wellenfunktion.
  // Endprodukte aussen (Rinde), Baugruppen auf der mittleren, Komponenten auf der inneren Schale, jeweils in der
  // Richtung des Schwerpunkts ihrer Verwendungen. So bleiben Familien als Lappen zusammen, Kreuzpunkte liegen innen.
  function shell(dx, dy, dz, radius) {
    const len = Math.hypot(dx, dy, dz) || 1;
    let x = dx / len, y = dy / len, z = dz / len;
    const fold = 1 + 0.07 * Math.sin(7 * Math.atan2(z, x) + 3 * y) * Math.cos(5 * y);   // Windungen
    const side = x < 0 ? -1 : 1;
    return [side * (Math.abs(x) * radius * 1.18 * fold + radius * 0.10), y * radius * 0.86 * fold, z * radius * 1.12 * fold];
  }
  function layout(nodes, parentsOf) {
    const R = 260;
    const tops = nodes.filter(n => n.role === 0).sort((a, b) => a.id.localeCompare(b.id, "de", { numeric: true }));
    const halves = [tops.filter((_, i) => i < tops.length / 2), tops.filter((_, i) => i >= tops.length / 2)];
    halves.forEach((list, hIdx) => {
      const side = hIdx === 0 ? -1 : 1, m = list.length || 1, golden = Math.PI * (3 - Math.sqrt(5));
      list.forEach((n, i) => {
        const y = 1 - (i + 0.5) / m * 2, r = Math.sqrt(1 - y * y), th = i * golden;
        const p = shell(side * (Math.abs(Math.cos(th) * r) + 0.02), y, Math.sin(th) * r, R);
        n.x = p[0]; n.y = p[1]; n.z = p[2];
      });
    });
    const rest = nodes.filter(n => n.role !== 0);
    rest.forEach(n => { n.x = (hash(n.id) - .5); n.y = (hash(n.id + "y") - .5); n.z = (hash(n.id + "z") - .5); });
    for (let pass = 0; pass < 4; pass++) {
      for (const n of rest) {
        const ps = parentsOf[n.i]; if (!ps || !ps.length) continue;
        let x = 0, y = 0, z = 0;
        for (const p of ps) { x += p.x; y += p.y; z += p.z; }
        const radius = n.role === 1 ? R * 0.68 : R * (0.30 + 0.22 / Math.sqrt(ps.length));
        const j = 0.18 / Math.sqrt(ps.length);
        const q = shell(x / ps.length + (hash(n.id) - .5) * j * R, y / ps.length + (hash(n.id + "y") - .5) * j * R, z / ps.length + (hash(n.id + "z") - .5) * j * R, radius);
        n.x = q[0]; n.y = q[1]; n.z = q[2];
      }
    }
  }

  function makePointMaterial(THREE) {
    return new THREE.ShaderMaterial({
      transparent: true, depthWrite: false, blending: THREE.AdditiveBlending,
      uniforms: { uScale: { value: 1 }, uAlpha: { value: 1 } },
      vertexShader: `attribute float size; attribute vec3 color; varying vec3 vColor; uniform float uScale;
        void main(){ vColor = color; vec4 mv = modelViewMatrix * vec4(position,1.0);
          gl_PointSize = size * uScale * (300.0 / -mv.z); gl_Position = projectionMatrix * mv; }`,
      fragmentShader: `varying vec3 vColor; uniform float uAlpha;
        void main(){ vec2 c = gl_PointCoord - 0.5; float d = length(c); if (d > 0.5) discard;
          float a = smoothstep(0.5, 0.05, d); gl_FragColor = vec4(vColor * (0.6 + 0.6 * (1.0 - d * 2.0)), a * uAlpha); }`
    });
  }

  function render(el, data, opts) {
    const THREE = window.THREE;
    if (!THREE) throw new Error("three.js nicht geladen");
    dispose(el);
    const codes = data.lzCodes || [];
    const nodes = data.nodes.map((n, i) => ({ ...n, i }));
    const parentsOf = nodes.map(() => []), childrenOf = nodes.map(() => []);
    for (const l of data.links) { parentsOf[l[1]].push(nodes[l[0]]); childrenOf[l[0]].push(nodes[l[1]]); }
    layout(nodes, parentsOf);

    el.innerHTML = ""; el.style.position = "relative";
    const canvas = document.createElement("canvas"); canvas.style.cssText = "position:absolute;inset:0;width:100%;height:100%;display:block;cursor:grab";
    el.appendChild(canvas);
    const legendEl = document.createElement("div"); legendEl.className = "bb-legend"; el.appendChild(legendEl);
    const infoEl = document.createElement("div"); infoEl.className = "bb-info"; el.appendChild(infoEl);
    const tipEl = document.createElement("div"); tipEl.className = "bb-tip"; el.appendChild(tipEl);

    const renderer = new THREE.WebGLRenderer({ canvas, antialias: true, alpha: false });
    renderer.setPixelRatio(Math.min(window.devicePixelRatio || 1, 2));
    renderer.setClearColor(0x07090d, 1);
    const scene = new THREE.Scene();
    const camera = new THREE.PerspectiveCamera(50, 1, 1, 5000);
    const root = new THREE.Group(); scene.add(root);

    // Straenge: ein Linienpuffer, Farbe vom Kind, additiv (leuchtet, wo viele zusammenlaufen)
    const L = data.links.length;
    const lpos = new Float32Array(L * 6), lcol = new Float32Array(L * 6);
    const lineGeo = new THREE.BufferGeometry();
    lineGeo.setAttribute("position", new THREE.BufferAttribute(lpos, 3));
    lineGeo.setAttribute("color", new THREE.BufferAttribute(lcol, 3));
    const lineMat = new THREE.LineBasicMaterial({ vertexColors: true, transparent: true, opacity: 0.075, blending: THREE.AdditiveBlending, depthWrite: false });
    const lines = new THREE.LineSegments(lineGeo, lineMat); root.add(lines);
    data.links.forEach((l, k) => { const a = nodes[l[0]], b = nodes[l[1]]; lpos.set([a.x, a.y, a.z, b.x, b.y, b.z], k * 6); });

    // Punkte: ein Punktpuffer, Groesse nach Verwendungen (Kreuzpunkte gross)
    const N = nodes.length;
    const ppos = new Float32Array(N * 3), pcol = new Float32Array(N * 3), psize = new Float32Array(N);
    nodes.forEach((n, k) => ppos.set([n.x, n.y, n.z], k * 3));
    const pointGeo = new THREE.BufferGeometry();
    pointGeo.setAttribute("position", new THREE.BufferAttribute(ppos, 3));
    pointGeo.setAttribute("color", new THREE.BufferAttribute(pcol, 3));
    pointGeo.setAttribute("size", new THREE.BufferAttribute(psize, 1));
    const pointMat = makePointMaterial(THREE);
    const points = new THREE.Points(pointGeo, pointMat); root.add(points);

    // Hervorhebung (Klick): eigene Objekte, Rest abgedunkelt
    const hlLineGeo = new THREE.BufferGeometry(), hlPointGeo = new THREE.BufferGeometry();
    const hlLines = new THREE.LineSegments(hlLineGeo, new THREE.LineBasicMaterial({ vertexColors: true, transparent: true, opacity: 0.95, blending: THREE.AdditiveBlending, depthWrite: false }));
    const hlPoints = new THREE.Points(hlPointGeo, makePointMaterial(THREE));
    hlLines.visible = hlPoints.visible = false; root.add(hlLines); root.add(hlPoints);

    const st = { mode: (opts && opts.mode) || "lz", minCross: (opts && opts.minCross) || 20, codes, nodes, links: data.links, parentsOf, childrenOf,
      renderer, scene, camera, root, lineGeo, pointGeo, lineMat, pointMat, hlLines, hlPoints, legend: legendEl, info: infoEl, tip: tipEl,
      rotY: 0.4, rotX: 0.15, dist: 720, auto: true, sel: null, raf: 0, el };
    const tmp = new THREE.Color();

    function paint() {
      const cols = nodes.map(n => { tmp.set(colorOf(st, n)); return [tmp.r, tmp.g, tmp.b]; });
      nodes.forEach((n, k) => { pcol.set(cols[k], k * 3); psize[k] = n.parents >= st.minCross ? 6 + Math.sqrt(n.parents) * 1.8 : (n.role === 0 ? 2.2 : 1.6 + Math.sqrt(n.parents || 1) * 0.35); });
      data.links.forEach((l, k) => { const c = cols[l[1]]; lcol.set([c[0] * .35, c[1] * .35, c[2] * .35, c[0] * .9, c[1] * .9, c[2] * .9], k * 6); });
      pointGeo.attributes.color.needsUpdate = pointGeo.attributes.size.needsUpdate = lineGeo.attributes.color.needsUpdate = true;
      if (st.sel) select(st.sel);
      legend();
    }
    function legend() {
      let items;
      if (st.mode === "risk") items = Object.keys(RISK_TXT).map(k => [RISK[k], RISK_TXT[k]]);
      else if (st.mode === "role") items = ROLE.map((c, i) => [c, ROLE_TXT[i]]);
      else items = codes.map(c => [lzColor(c, codes), "LZ " + c]).concat([["#6c757d", "ohne LZ-Code"]]);
      legendEl.innerHTML = items.map(([c, t]) => `<span><i style="background:${c}"></i>${esc(t)}</span>`).join("");
    }
    function select(n) {
      st.sel = n;
      if (!n) { hlLines.visible = hlPoints.visible = false; lineMat.opacity = 0.075; pointMat.uniforms.uAlpha.value = 1; infoEl.style.display = "none"; return; }
      // ganze Bahnen nach oben und unten
      const set = new Set([n]), segs = [];
      const walk = (start, up) => { const stack = [start]; while (stack.length) { const x = stack.pop(); for (const y of (up ? parentsOf[x.i] : childrenOf[x.i])) { segs.push(up ? [y, x] : [x, y]); if (!set.has(y)) { set.add(y); stack.push(y); } } } };
      walk(n, true); walk(n, false);
      const hp = new Float32Array(segs.length * 6), hc = new Float32Array(segs.length * 6);
      segs.forEach(([a, b], k) => { hp.set([a.x, a.y, a.z, b.x, b.y, b.z], k * 6); tmp.set(colorOf(st, b)); hc.set([tmp.r, tmp.g, tmp.b, tmp.r, tmp.g, tmp.b], k * 6); });
      hlLineGeo.setAttribute("position", new THREE.BufferAttribute(hp, 3)); hlLineGeo.setAttribute("color", new THREE.BufferAttribute(hc, 3));
      const arr = [...set], qp = new Float32Array(arr.length * 3), qc = new Float32Array(arr.length * 3), qs = new Float32Array(arr.length);
      arr.forEach((x, k) => { qp.set([x.x, x.y, x.z], k * 3); tmp.set(colorOf(st, x)); qc.set([tmp.r, tmp.g, tmp.b], k * 3); qs[k] = x === n ? 16 : 5 + Math.sqrt(x.parents || 1); });
      hlPointGeo.setAttribute("position", new THREE.BufferAttribute(qp, 3)); hlPointGeo.setAttribute("color", new THREE.BufferAttribute(qc, 3)); hlPointGeo.setAttribute("size", new THREE.BufferAttribute(qs, 1));
      hlLines.visible = hlPoints.visible = true; lineMat.opacity = 0.03; pointMat.uniforms.uAlpha.value = 0.18;
      infoEl.style.display = "block";
      infoEl.innerHTML = `<b>${esc(n.id)}</b><div class="bb-t">${esc(n.text || "")}</div>
        <div>${ROLE_TXT[n.role]} · LZ-Code ${esc(n.lz)}</div>
        <div>${n.parents} direkte Verwendungen · ${n.topProducts} Endprodukte</div>
        <div>Lieferant: ${esc(RISK_TXT[String(n.risk)])}</div>
        <div>${set.size - 1} verbundene Materialien</div>
        <div class="bb-h">Klick ins Leere hebt die Hervorhebung auf</div>`;
    }

    // Bedienung: ziehen = drehen, Rad = zoomen, Klick = auswaehlen, darueberfahren = Name
    const ray = new THREE.Raycaster(); ray.params.Points = { threshold: 5 };
    const mouse = new THREE.Vector2();
    let drag = null, moved = false;
    function pick(ev) {
      const r = canvas.getBoundingClientRect();
      mouse.set(((ev.clientX - r.left) / r.width) * 2 - 1, -((ev.clientY - r.top) / r.height) * 2 + 1);
      ray.setFromCamera(mouse, camera);
      const hits = ray.intersectObject(points);
      if (!hits.length) return null;
      hits.sort((a, b) => (nodes[b.index].parents - nodes[a.index].parents) || (a.distanceToRay - b.distanceToRay));
      return nodes[hits[0].index];
    }
    canvas.addEventListener("pointerdown", ev => { drag = { x: ev.clientX, y: ev.clientY, ry: st.rotY, rx: st.rotX }; moved = false; st.auto = false; canvas.style.cursor = "grabbing"; });
    window.addEventListener("pointerup", st.onUp = ev => {
      if (!drag) return; canvas.style.cursor = "grab";
      if (!moved && ev.target === canvas) select(pick(ev));
      drag = null;
    });
    let tipTimer = 0;
    canvas.addEventListener("pointermove", ev => {
      if (drag) {
        const dx = ev.clientX - drag.x, dy = ev.clientY - drag.y;
        if (Math.abs(dx) + Math.abs(dy) > 3) moved = true;
        st.rotY = drag.ry + dx * 0.006; st.rotX = Math.max(-1.3, Math.min(1.3, drag.rx + dy * 0.006));
        return;
      }
      clearTimeout(tipTimer);
      tipTimer = setTimeout(() => {
        const n = pick(ev);
        if (!n) { tipEl.style.display = "none"; return; }
        const r = el.getBoundingClientRect();
        tipEl.style.display = "block"; tipEl.style.left = (ev.clientX - r.left + 14) + "px"; tipEl.style.top = (ev.clientY - r.top + 10) + "px";
        tipEl.innerHTML = `<b>${esc(n.id)}</b> ${esc(n.text || "")}<br>${ROLE_TXT[n.role]} · LZ ${esc(n.lz)} · ${n.parents} Verwendungen`;
      }, 60);
    });
    canvas.addEventListener("pointerleave", () => { tipEl.style.display = "none"; });
    canvas.addEventListener("wheel", ev => { ev.preventDefault(); st.dist = Math.max(160, Math.min(1600, st.dist * (ev.deltaY > 0 ? 1.1 : 0.9))); }, { passive: false });

    function resize() {
      const w = el.clientWidth || 800, h = el.clientHeight || 600;
      renderer.setSize(w, h, false); camera.aspect = w / h; camera.updateProjectionMatrix();
      pointMat.uniforms.uScale.value = hlPoints.material.uniforms.uScale.value = h / 600;
    }
    const ro = new ResizeObserver(resize); ro.observe(el); resize();

    function frame() {
      if (st.auto) st.rotY += 0.0018;
      root.rotation.set(st.rotX, st.rotY, 0);
      camera.position.set(0, 0, st.dist); camera.lookAt(0, 0, 0);
      renderer.render(scene, camera);
      st.raf = requestAnimationFrame(frame);
    }
    st.ro = ro; st.paint = paint; st.select = select;
    states.set(el, st);
    paint();
    frame();
    return { nodes: N, links: L };
  }

  function setMode(el, mode) { const st = states.get(el); if (!st) return; st.mode = mode; st.paint(); }
  function setMinCross(el, n) { const st = states.get(el); if (!st) return; st.minCross = Math.max(1, Number(n) || 1); st.paint(); }

  function dispose(el) {
    const st = states.get(el); if (!st) return;
    cancelAnimationFrame(st.raf);
    try { st.ro.disconnect(); window.removeEventListener("pointerup", st.onUp); st.renderer.dispose(); st.lineGeo.dispose(); st.pointGeo.dispose(); } catch (e) { /* bereits weg */ }
    states.delete(el);
    el.innerHTML = "";
  }

  window.trafagBomBrain = { render, setMode, setMinCross, dispose };
})();
