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
    const chartType = normalizeChartType(options && options.chartType);
    const labelScale = normalizeLabelScale(options && options.labelScale);
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
    addAxisGuides(scene, THREE, layoutFromAxes(axes, xStep, zStep, xStart, zStart), options || {}, chartType, labelScale);

    const scalables = [];
    const layout = { axes, xStep, zStep, xStart, zStart };
    if (chartType === "line") {
      createLineChart(THREE, root, rows, layout, scalables);
    } else if (chartType === "surface") {
      createSurfaceChart(THREE, root, rows, layout, scalables);
    } else if (chartType === "pie") {
      createPieChart(THREE, root, rows, layout, scalables, labelScale);
    } else {
      createBarChart(THREE, root, rows, layout, scalables);
    }
    applyFactorToScalables(scalables, factor);

    addCanvasLabel(scene, THREE, options.title || "", -8.8, 9.2, -7.8, 1.05 * labelScale);
    axes.countries.forEach((country, index) => addCanvasLabel(scene, THREE, country, xStart + index * (xStep || 2), -0.15, zStart - 1.3, 0.58 * labelScale));
    axes.years.forEach((year, index) => addCanvasLabel(scene, THREE, String(year), xStart - 1.6, -0.15, zStart + index * (zStep || 2), 0.58 * labelScale));

    const previous = stateByCanvas.get(canvas);
    const state = {
      renderer,
      scene,
      camera,
      root,
      angleX: previous ? previous.angleX : -0.62,
      angleY: previous ? previous.angleY : 0.78,
      distance: previous ? previous.distance : 23,
      targetX: previous ? previous.targetX : 0,
      targetY: previous ? previous.targetY : 2.8,
      targetZ: previous ? previous.targetZ : 0,
      factor,
      scalables,
      dragging: false,
      dragMode: "rotate",
      lastX: 0,
      lastY: 0
    };
    attachInteraction(canvas, state);
    stateByCanvas.set(canvas, state);
    resizeAndRender(canvas);
  }

  function layoutFromAxes(axes, xStep, zStep, xStart, zStart) {
    return {
      axes,
      xStep,
      zStep,
      xStart,
      zStart,
      xEnd: xStart + Math.max(1, axes.countries.length - 1) * (xStep || 2),
      zEnd: zStart + Math.max(1, axes.years.length - 1) * (zStep || 2)
    };
  }

  function normalizeFactor(value) {
    const factor = Number(value);
    if (!Number.isFinite(factor)) return 1;
    return Math.max(0.5, Math.min(1.5, factor));
  }

  function normalizeChartType(value) {
    const text = String(value || "bar").toLowerCase();
    return ["bar", "line", "surface", "pie"].includes(text) ? text : "bar";
  }

  function normalizeLabelScale(value) {
    const scale = Number(value);
    if (!Number.isFinite(scale)) return 1.4;
    return Math.max(0.8, Math.min(2.5, scale));
  }

  function addAxisGuides(scene, THREE, layout, options, chartType, labelScale) {
    if (chartType === "pie") {
      addCanvasLabel(scene, THREE, options.pieAxis || "Pie: country shares", -8.4, 8.2, -7.6, 0.85 * labelScale);
      addCanvasLabel(scene, THREE, options.yAxis || "Y: value / indicator", 6.2, 1.4, 6.8, 0.62 * labelScale);
      return;
    }

    const xEnd = layout.xEnd + 1.2;
    const zEnd = layout.zEnd + 1.2;
    const axisYOffset = 0.04;
    addAxisLine(scene, THREE, new THREE.Vector3(layout.xStart - 1.2, axisYOffset, layout.zStart - 1.0), new THREE.Vector3(xEnd, axisYOffset, layout.zStart - 1.0), 0x2869a6);
    addAxisLine(scene, THREE, new THREE.Vector3(layout.xStart - 1.1, axisYOffset, layout.zStart - 1.0), new THREE.Vector3(layout.xStart - 1.1, axisYOffset, zEnd), 0x7a8f2a);
    addAxisLine(scene, THREE, new THREE.Vector3(layout.xStart - 1.1, 0, layout.zStart - 1.0), new THREE.Vector3(layout.xStart - 1.1, 8.8, layout.zStart - 1.0), 0xb84f3a);

    addCanvasLabel(scene, THREE, options.xAxis || "X: country", xEnd, 0.45, layout.zStart - 1.2, 0.66 * labelScale);
    addCanvasLabel(scene, THREE, options.zAxis || "Z: year / time", layout.xStart - 1.4, 0.45, zEnd, 0.66 * labelScale);
    addCanvasLabel(scene, THREE, options.yAxis || "Y: value / indicator", layout.xStart - 1.6, 9.3, layout.zStart - 1.0, 0.66 * labelScale);
  }

  function addAxisLine(scene, THREE, from, to, color) {
    const material = new THREE.LineBasicMaterial({ color, linewidth: 3 });
    const geometry = new THREE.BufferGeometry().setFromPoints([from, to]);
    scene.add(new THREE.Line(geometry, material));

    const direction = new THREE.Vector3().subVectors(to, from).normalize();
    const cone = new THREE.Mesh(
      new THREE.ConeGeometry(0.22, 0.55, 18),
      new THREE.MeshBasicMaterial({ color }));
    cone.position.copy(to);
    cone.quaternion.setFromUnitVectors(new THREE.Vector3(0, 1, 0), direction);
    scene.add(cone);
  }

  function rowPosition(row, layout) {
    const countryIndex = Math.max(0, layout.axes.countries.indexOf(String(row.country || "-")));
    const yearIndex = Math.max(0, layout.axes.years.indexOf(Number(row.year || 0)));
    const rawValue = Math.abs(Number(row.value || 0));
    return {
      x: layout.xStart + countryIndex * (layout.xStep || 2),
      z: layout.zStart + yearIndex * (layout.zStep || 2),
      baseHeight: Math.max(0.08, rawValue / layout.axes.maxValue * 8),
      ratio: rawValue / layout.axes.maxValue,
      country: String(row.country || "-"),
      year: Number(row.year || 0),
      value: rawValue
    };
  }

  function createBarChart(THREE, root, rows, layout, scalables) {
    const barGeometry = new THREE.BoxGeometry(0.68, 1, 0.68);
    rows.forEach(row => {
      const p = rowPosition(row, layout);
      const material = new THREE.MeshStandardMaterial({
        color: colorForValue(p.ratio),
        roughness: 0.58,
        metalness: 0.05
      });
      const bar = new THREE.Mesh(barGeometry, material);
      bar.userData.baseHeight = p.baseHeight;
      bar.position.set(p.x, 0, p.z);
      scalables.push({ type: "bar", object: bar });
      root.add(bar);
    });
  }

  function createLineChart(THREE, root, rows, layout, scalables) {
    const pointGeometry = new THREE.SphereGeometry(0.22, 16, 12);
    const lineMaterial = new THREE.LineBasicMaterial({ color: 0x2f6f9f, linewidth: 2 });
    const sortedGroups = groupRowsByCountry(rows, layout);
    sortedGroups.forEach((points, groupIndex) => {
      const material = new THREE.MeshStandardMaterial({ color: colorForSeries(groupIndex), roughness: 0.5 });
      const linePositions = [];
      points.forEach(point => {
        const marker = new THREE.Mesh(pointGeometry, material);
        marker.userData.baseHeight = point.baseHeight;
        marker.userData.baseX = point.x;
        marker.userData.baseZ = point.z;
        scalables.push({ type: "point", object: marker });
        root.add(marker);
        linePositions.push(point.x, point.baseHeight, point.z);
      });
      if (linePositions.length >= 6) {
        const geometry = new THREE.BufferGeometry();
        geometry.setAttribute("position", new THREE.Float32BufferAttribute(linePositions, 3));
        const line = new THREE.Line(geometry, lineMaterial.clone());
        line.userData.basePositions = linePositions.slice();
        scalables.push({ type: "line", object: line });
        root.add(line);
      }
    });
  }

  function createSurfaceChart(THREE, root, rows, layout, scalables) {
    const rowByKey = new Map(rows.map(row => [`${String(row.country || "-")}|${Number(row.year || 0)}`, rowPosition(row, layout)]));
    const vertices = [];
    const indices = [];
    layout.axes.countries.forEach(country => {
      layout.axes.years.forEach(year => {
        const p = rowByKey.get(`${country}|${year}`) || {
          x: layout.xStart + Math.max(0, layout.axes.countries.indexOf(country)) * (layout.xStep || 2),
          z: layout.zStart + Math.max(0, layout.axes.years.indexOf(year)) * (layout.zStep || 2),
          baseHeight: 0.04
        };
        vertices.push(p.x, p.baseHeight, p.z);
      });
    });
    const yearCount = layout.axes.years.length;
    for (let c = 0; c < layout.axes.countries.length - 1; c++) {
      for (let y = 0; y < layout.axes.years.length - 1; y++) {
        const a = c * yearCount + y;
        const b = (c + 1) * yearCount + y;
        const d = c * yearCount + y + 1;
        const e = (c + 1) * yearCount + y + 1;
        indices.push(a, b, d, b, e, d);
      }
    }
    const geometry = new THREE.BufferGeometry();
    geometry.setAttribute("position", new THREE.Float32BufferAttribute(vertices, 3));
    geometry.setIndex(indices);
    geometry.computeVertexNormals();
    const material = new THREE.MeshStandardMaterial({
      color: 0x4188a8,
      roughness: 0.64,
      metalness: 0.03,
      opacity: 0.72,
      transparent: true,
      side: THREE.DoubleSide
    });
    const mesh = new THREE.Mesh(geometry, material);
    mesh.userData.basePositions = vertices.slice();
    scalables.push({ type: "surface", object: mesh });
    root.add(mesh);

    const wire = new THREE.LineSegments(new THREE.WireframeGeometry(geometry), new THREE.LineBasicMaterial({ color: 0x24485a, transparent: true, opacity: 0.42 }));
    wire.userData.sourceGeometry = geometry;
    scalables.push({ type: "wire", object: wire, source: mesh });
    root.add(wire);
  }

  function createPieChart(THREE, root, rows, layout, scalables, labelScale) {
    const totals = [...rows.reduce((map, row) => {
      const country = String(row.country || "-");
      map.set(country, (map.get(country) || 0) + Math.abs(Number(row.value || 0)));
      return map;
    }, new Map())]
      .filter(([, value]) => value > 0)
      .sort((a, b) => b[1] - a[1]);
    const total = totals.reduce((sum, [, value]) => sum + value, 0) || 1;
    let start = -Math.PI / 2;
    totals.forEach(([country, value], index) => {
      const angle = value / total * Math.PI * 2;
      const shape = new THREE.Shape();
      shape.moveTo(0, 0);
      const steps = Math.max(8, Math.ceil(angle / (Math.PI / 20)));
      for (let i = 0; i <= steps; i++) {
        const a = start + angle * i / steps;
        shape.lineTo(Math.cos(a) * 6, Math.sin(a) * 6);
      }
      shape.lineTo(0, 0);
      const geometry = new THREE.ExtrudeGeometry(shape, { depth: 0.35, bevelEnabled: false });
      const material = new THREE.MeshStandardMaterial({ color: colorForSeries(index), roughness: 0.55 });
      const slice = new THREE.Mesh(geometry, material);
      slice.rotation.x = -Math.PI / 2;
      slice.position.y = 0.04;
      slice.userData.baseHeight = 0.35;
      scalables.push({ type: "pie", object: slice });
      root.add(slice);

      const labelAngle = start + angle / 2;
      addCanvasLabel(root, THREE, country, Math.cos(labelAngle) * 7.1, 0.25, Math.sin(labelAngle) * 7.1, 0.5 * labelScale);
      start += angle;
    });
  }

  function groupRowsByCountry(rows, layout) {
    const groups = new Map();
    rows.forEach(row => {
      const p = rowPosition(row, layout);
      if (!groups.has(p.country)) groups.set(p.country, []);
      groups.get(p.country).push(p);
    });
    return [...groups.values()].map(points => points.sort((a, b) => a.year - b.year));
  }

  function applyFactorToScalables(scalables, factor) {
    scalables.forEach(item => {
      if (item.type === "bar") {
        applyBarFactor(item.object, factor);
      } else if (item.type === "point") {
        applyPointFactor(item.object, factor);
      } else if (item.type === "line" || item.type === "surface") {
        applyPositionFactor(item.object, factor);
      } else if (item.type === "wire") {
        item.object.geometry.dispose();
        item.object.geometry = new window.THREE.WireframeGeometry(item.source.geometry);
      } else if (item.type === "pie") {
        item.object.scale.z = factor;
      }
    });
  }

  function applyBarFactor(bar, factor) {
    const height = Math.max(0.02, Number(bar.userData.baseHeight || 0.08) * factor);
    bar.scale.y = height;
    bar.position.y = height / 2;
  }

  function applyPointFactor(point, factor) {
    point.scale.set(1, 1, 1);
    point.position.set(
      Number(point.userData.baseX || 0),
      Math.max(0.02, Number(point.userData.baseHeight || 0.08) * factor),
      Number(point.userData.baseZ || 0));
  }

  function applyPositionFactor(object, factor) {
    const base = object.userData.basePositions;
    const attribute = object.geometry && object.geometry.attributes && object.geometry.attributes.position;
    if (!base || !attribute) return;
    for (let i = 0; i < base.length; i += 3) {
      attribute.array[i] = base[i];
      attribute.array[i + 1] = Math.max(0.02, base[i + 1] * factor);
      attribute.array[i + 2] = base[i + 2];
    }
    attribute.needsUpdate = true;
    object.geometry.computeBoundingSphere();
    if (object.geometry.computeVertexNormals) object.geometry.computeVertexNormals();
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

  function colorForSeries(index) {
    const colors = [0x2f6f9f, 0xc45a42, 0x6b8f3a, 0x8b6bb1, 0xd09b2c, 0x4d908e, 0x9d4edd, 0x577590];
    return colors[index % colors.length];
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
    const size = resolveCanvasSize(canvas);
    const width = size.width;
    const height = size.height;
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

  function resolveCanvasSize(canvas) {
    const parent = canvas.parentElement;
    const parentRect = parent ? parent.getBoundingClientRect() : null;
    const canvasRect = canvas.getBoundingClientRect();
    const width = Math.max(
      640,
      Math.floor(parentRect && parentRect.width > 320 ? parentRect.width : canvasRect.width || canvas.clientWidth || 900));
    const height = Math.max(
      520,
      Math.floor(parentRect && parentRect.height > 240 ? parentRect.height : canvasRect.height || canvas.clientHeight || 680));
    canvas.style.width = "100%";
    canvas.style.height = "100%";
    return { width, height };
  }

  function renderFallback(canvas, rows, options) {
    const ctx = canvas.getContext("2d");
    const rect = resolveCanvasSize(canvas);
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
      if (!canvas || typeof canvas.addEventListener !== "function") return;
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
      if (!state || !state.scalables) return;
      state.factor = normalizeFactor(factor);
      applyFactorToScalables(state.scalables, state.factor);
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
