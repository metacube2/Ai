(function () {
  const stateByCanvas = new WeakMap();

  function normalizeRows(rows) {
    return Array.isArray(rows) ? rows.filter(row => row && Number.isFinite(Number(row.value))) : [];
  }

  function buildAxes(rows) {
    const countries = [...new Set(rows.map(row => String(row.country || "-")))].sort();
    const years = [...new Set(rows.map(row => Number(row.year || 0)))].sort((a, b) => a - b);
    const maxValue = rows.reduce((max, row) => Math.max(max, Math.abs(Number(row.value || 0))), 0) || 1;
    return { countries, years, maxValue };
  }

  function createThreeScene(canvas, rows, options) {
    const THREE = window.THREE;
    const factor = normalizeFactor(options && options.scenarioFactor);
    const renderer = new THREE.WebGLRenderer({ canvas, antialias: true, alpha: false });
    renderer.setPixelRatio(Math.min(window.devicePixelRatio || 1, 2));
    renderer.setClearColor(0xf7f9fb, 1);

    const scene = new THREE.Scene();
    const camera = new THREE.PerspectiveCamera(45, 1, 0.1, 1000);
    camera.position.set(18, 15, 22);
    camera.lookAt(0, 0, 0);

    const root = new THREE.Group();
    scene.add(root);
    scene.add(new THREE.AmbientLight(0xffffff, 0.72));
    const light = new THREE.DirectionalLight(0xffffff, 0.75);
    light.position.set(8, 18, 10);
    scene.add(light);

    const axes = buildAxes(rows);
    const xStep = axes.countries.length > 1 ? 16 / (axes.countries.length - 1) : 0;
    const zStep = axes.years.length > 1 ? 12 / (axes.years.length - 1) : 0;
    const xStart = -8;
    const zStart = -6;

    const gridMaterial = new THREE.LineBasicMaterial({ color: 0xb7c1cc, transparent: true, opacity: 0.55 });
    const gridPoints = [];
    for (let i = 0; i <= axes.countries.length; i++) {
      const x = xStart + (i - 0.5) * (xStep || 2);
      gridPoints.push(new THREE.Vector3(x, 0, zStart - 1), new THREE.Vector3(x, 0, zStart + Math.max(1, axes.years.length - 1) * (zStep || 2) + 1));
    }
    for (let i = 0; i <= axes.years.length; i++) {
      const z = zStart + (i - 0.5) * (zStep || 2);
      gridPoints.push(new THREE.Vector3(xStart - 1, 0, z), new THREE.Vector3(xStart + Math.max(1, axes.countries.length - 1) * (xStep || 2) + 1, 0, z));
    }
    root.add(new THREE.LineSegments(new THREE.BufferGeometry().setFromPoints(gridPoints), gridMaterial));

    const barGeometry = new THREE.BoxGeometry(0.68, 1, 0.68);
    const bars = [];
    rows.forEach(row => {
      const countryIndex = Math.max(0, axes.countries.indexOf(String(row.country || "-")));
      const yearIndex = Math.max(0, axes.years.indexOf(Number(row.year || 0)));
      const rawValue = Math.abs(Number(row.value || 0));
      const height = Math.max(0.08, rawValue / axes.maxValue * 8);
      const material = new THREE.MeshStandardMaterial({
        color: colorForValue(rawValue / axes.maxValue),
        roughness: 0.58,
        metalness: 0.05
      });
      const bar = new THREE.Mesh(barGeometry, material);
      bar.userData.baseHeight = height;
      bar.position.set(xStart + countryIndex * (xStep || 2), 0, zStart + yearIndex * (zStep || 2));
      applyBarFactor(bar, factor);
      bars.push(bar);
      root.add(bar);
    });

    addCanvasLabel(scene, THREE, options.title || "", -8.8, 9.2, -7.8, 1.05);
    axes.countries.forEach((country, index) => addCanvasLabel(scene, THREE, country, xStart + index * (xStep || 2), -0.15, zStart - 1.3, 0.58));
    axes.years.forEach((year, index) => addCanvasLabel(scene, THREE, String(year), xStart - 1.6, -0.15, zStart + index * (zStep || 2), 0.58));

    const previous = stateByCanvas.get(canvas);
    const state = {
      renderer,
      scene,
      camera,
      root,
      angleX: previous ? previous.angleX : -0.62,
      angleY: previous ? previous.angleY : 0.78,
      distance: previous ? previous.distance : 30,
      targetX: previous ? previous.targetX : 0,
      targetY: previous ? previous.targetY : 2.8,
      targetZ: previous ? previous.targetZ : 0,
      factor,
      bars,
      dragging: false,
      dragMode: "rotate",
      lastX: 0,
      lastY: 0
    };
    attachInteraction(canvas, state);
    stateByCanvas.set(canvas, state);
    resizeAndRender(canvas);
  }

  function normalizeFactor(value) {
    const factor = Number(value);
    if (!Number.isFinite(factor)) return 1;
    return Math.max(0.5, Math.min(1.5, factor));
  }

  function applyBarFactor(bar, factor) {
    const height = Math.max(0.02, Number(bar.userData.baseHeight || 0.08) * factor);
    bar.scale.y = height;
    bar.position.y = height / 2;
  }

  function addCanvasLabel(scene, THREE, text, x, y, z, scale) {
    const labelCanvas = document.createElement("canvas");
    labelCanvas.width = 512;
    labelCanvas.height = 128;
    const ctx = labelCanvas.getContext("2d");
    ctx.clearRect(0, 0, labelCanvas.width, labelCanvas.height);
    ctx.fillStyle = "#243241";
    ctx.font = "600 44px Open Sans, Arial, sans-serif";
    ctx.textAlign = "center";
    ctx.textBaseline = "middle";
    ctx.fillText(String(text || "-"), 256, 64, 480);
    const texture = new THREE.CanvasTexture(labelCanvas);
    const material = new THREE.SpriteMaterial({ map: texture, transparent: true });
    const sprite = new THREE.Sprite(material);
    sprite.position.set(x, y, z);
    sprite.scale.set(3.5 * scale, 0.85 * scale, 1);
    scene.add(sprite);
  }

  function colorForValue(t) {
    const clamped = Math.max(0, Math.min(1, t));
    const r = Math.round(45 + clamped * 178);
    const g = Math.round(105 + clamped * 78);
    const b = Math.round(155 - clamped * 88);
    return (r << 16) + (g << 8) + b;
  }

  function attachInteraction(canvas, state) {
    canvas.onpointerdown = event => {
      event.preventDefault();
      state.dragging = true;
      state.dragMode = event.button === 2 || event.button === 1 || event.shiftKey ? "pan" : "rotate";
      state.lastX = event.clientX;
      state.lastY = event.clientY;
      canvas.setPointerCapture(event.pointerId);
    };
    canvas.onpointermove = event => {
      if (!state.dragging) return;
      const dx = event.clientX - state.lastX;
      const dy = event.clientY - state.lastY;
      state.lastX = event.clientX;
      state.lastY = event.clientY;
      if (state.dragMode === "pan") {
        panCamera(state, dx, dy);
      } else {
        state.angleY += dx * 0.008;
        state.angleX = Math.max(-1.25, Math.min(-0.15, state.angleX + dy * 0.006));
      }
      renderState(state, canvas);
    };
    canvas.onpointerup = event => {
      state.dragging = false;
      try { canvas.releasePointerCapture(event.pointerId); } catch { }
    };
    canvas.onpointercancel = () => {
      state.dragging = false;
    };
    canvas.oncontextmenu = event => {
      event.preventDefault();
    };
    canvas.onwheel = event => {
      event.preventDefault();
      const delta = normalizeWheelDelta(event);
      const zoomFactor = delta > 0 ? 1.12 : 0.88;
      state.distance = Math.max(14, Math.min(62, state.distance * zoomFactor));
      renderState(state, canvas);
    };
  }

  function normalizeWheelDelta(event) {
    if (Number.isFinite(event.deltaY) && event.deltaY !== 0) {
      return event.deltaY > 0 ? 1 : -1;
    }
    if (Number.isFinite(event.wheelDelta) && event.wheelDelta !== 0) {
      return event.wheelDelta < 0 ? 1 : -1;
    }
    return 1;
  }

  function panCamera(state, dx, dy) {
    const scale = state.distance * 0.0018;
    const rightX = Math.cos(state.angleY);
    const rightZ = -Math.sin(state.angleY);
    const forwardX = Math.sin(state.angleY);
    const forwardZ = Math.cos(state.angleY);
    state.targetX -= dx * scale * rightX;
    state.targetZ -= dx * scale * rightZ;
    state.targetX += dy * scale * forwardX;
    state.targetZ += dy * scale * forwardZ;
  }

  function renderState(state, canvas) {
    const width = canvas.clientWidth || 900;
    const height = canvas.clientHeight || 520;
    state.camera.aspect = width / height;
    state.camera.updateProjectionMatrix();
    const horizontal = Math.cos(state.angleX) * state.distance;
    state.camera.position.set(
      state.targetX + Math.sin(state.angleY) * horizontal,
      state.targetY + Math.sin(-state.angleX) * state.distance,
      state.targetZ + Math.cos(state.angleY) * horizontal);
    state.camera.lookAt(state.targetX, state.targetY, state.targetZ);
    state.renderer.setSize(width, height, false);
    state.renderer.render(state.scene, state.camera);
  }

  function renderFallback(canvas, rows, options) {
    const ctx = canvas.getContext("2d");
    const rect = canvas.getBoundingClientRect();
    const dpr = Math.min(window.devicePixelRatio || 1, 2);
    canvas.width = Math.max(1, Math.floor(rect.width * dpr));
    canvas.height = Math.max(1, Math.floor(rect.height * dpr));
    ctx.setTransform(dpr, 0, 0, dpr, 0, 0);
    const width = rect.width;
    const height = rect.height;
    ctx.fillStyle = "#f7f9fb";
    ctx.fillRect(0, 0, width, height);
    ctx.fillStyle = "#243241";
    ctx.font = "600 16px Open Sans, Arial, sans-serif";
    ctx.fillText(options.title || "3D data analysis", 18, 28);

    const axes = buildAxes(rows);
    const barWidth = Math.max(12, Math.min(42, (width - 80) / Math.max(1, rows.length) * 0.72));
    rows.forEach((row, index) => {
      const value = Math.abs(Number(row.value || 0));
      const barHeight = value / axes.maxValue * (height - 110);
      const x = 44 + index * ((width - 90) / Math.max(1, rows.length));
      const y = height - 42 - barHeight;
      ctx.fillStyle = `hsl(${205 - (value / axes.maxValue) * 115}, 58%, 45%)`;
      ctx.fillRect(x, y, barWidth, barHeight);
    });
    ctx.fillStyle = "#52606d";
    ctx.font = "12px Open Sans, Arial, sans-serif";
    ctx.fillText("Fallback canvas renderer: Three.js could not be loaded.", 18, height - 16);
  }

  function resizeAndRender(canvas) {
    const state = stateByCanvas.get(canvas);
    if (state) renderState(state, canvas);
  }

  window.trafagFinance3d = {
    render: function (canvas, rows, options) {
      if (!canvas) return;
      const normalizedRows = normalizeRows(rows);
      if (normalizedRows.length === 0) return;
      const existing = stateByCanvas.get(canvas);
      if (existing && existing.renderer && existing.renderer.dispose) {
        existing.renderer.dispose();
      }
      if (window.THREE && window.THREE.WebGLRenderer) {
        createThreeScene(canvas, normalizedRows, options || {});
      } else {
        renderFallback(canvas, normalizedRows, options || {});
      }
    },
    updateFactor: function (canvas, factor) {
      const state = stateByCanvas.get(canvas);
      if (!state || !state.bars) return;
      state.factor = normalizeFactor(factor);
      state.bars.forEach(bar => applyBarFactor(bar, state.factor));
      renderState(state, canvas);
    },
    resize: resizeAndRender,
    pixelProbe: function (canvas) {
      const ctx = canvas && canvas.getContext ? canvas.getContext("2d", { willReadFrequently: true }) : null;
      if (!ctx) return -1;
      const data = ctx.getImageData(0, 0, Math.min(32, canvas.width), Math.min(32, canvas.height)).data;
      let sum = 0;
      for (let i = 0; i < data.length; i += 4) sum += data[i] + data[i + 1] + data[i + 2];
      return sum;
    }
  };

  window.addEventListener("resize", () => {
    document.querySelectorAll(".finance-3d-canvas").forEach(canvas => resizeAndRender(canvas));
  });
})();
