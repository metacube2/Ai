// editor.js
//
// Zeichenflaeche: Treffertests, Einrasten und Darstellung.
//
// Das Einrasten ist hier kein Komfortdetail, sondern der Ersatz fuer den
// frueheren Verbinden-Knopf. Verbunden ist, was sich beruehrt; also muss
// sich Beruehrung muehelos herstellen lassen, ohne pixelgenau zu zielen.

import { boundsOf } from "./geometry.js";

export const SNAP_TOLERANCE = 9;

const PART_COLORS = [
  { fill: "rgba(85,228,255,.16)", line: "#55e4ff" },
  { fill: "rgba(255,95,189,.16)", line: "#ff5fbd" },
  { fill: "rgba(255,209,102,.16)", line: "#ffd166" },
  { fill: "rgba(126,255,168,.16)", line: "#7effa8" },
  { fill: "rgba(178,152,255,.16)", line: "#b298ff" },
];

export function partColor(index) {
  return PART_COLORS[((index % PART_COLORS.length) + PART_COLORS.length) % PART_COLORS.length];
}

/** Umschliessende Box einer Objektliste. */
export function unionBounds(objects) {
  if (!objects.length) return null;
  let minX = Infinity;
  let minY = Infinity;
  let maxX = -Infinity;
  let maxY = -Infinity;
  for (const object of objects) {
    const box = boundsOf(object);
    minX = Math.min(minX, box.x);
    minY = Math.min(minY, box.y);
    maxX = Math.max(maxX, box.x + box.w);
    maxY = Math.max(maxY, box.y + box.h);
  }
  return { x: minX, y: minY, w: maxX - minX, h: maxY - minY };
}

/**
 * Bestimmt die Korrektur, mit der die bewegte Auswahl an den uebrigen
 * Objekten einrastet.
 *
 * Kontaktkanten - rechts an links, unten an oben - haben Vorrang vor blosser
 * Ausrichtung, weil sie den Klangkoerper tatsaechlich zusammenfuegen. Sie
 * bekommen deshalb eine kuenstlich verkleinerte Distanz.
 */
export function snapOffset(moved, others, tolerance = SNAP_TOLERANCE) {
  const box = unionBounds(moved);
  if (!box || !others.length) return { dx: 0, dy: 0, contactX: false, contactY: false };

  let bestX = { distance: Infinity, delta: 0, contact: false };
  let bestY = { distance: Infinity, delta: 0, contact: false };

  const consider = (best, from, to, contact) => {
    const delta = to - from;
    const distance = Math.abs(delta) - (contact ? tolerance * 0.35 : 0);
    if (Math.abs(delta) <= tolerance && distance < best.distance) {
      return { distance, delta, contact };
    }
    return best;
  };

  for (const other of others) {
    const target = boundsOf(other);
    // Kontakt: die bewegte Kante trifft die gegenueberliegende Kante.
    bestX = consider(bestX, box.x + box.w, target.x, true);
    bestX = consider(bestX, box.x, target.x + target.w, true);
    bestY = consider(bestY, box.y + box.h, target.y, true);
    bestY = consider(bestY, box.y, target.y + target.h, true);
    // Ausrichtung: Kanten und Mitten fluchten.
    bestX = consider(bestX, box.x, target.x, false);
    bestX = consider(bestX, box.x + box.w, target.x + target.w, false);
    bestX = consider(bestX, box.x + box.w / 2, target.x + target.w / 2, false);
    bestY = consider(bestY, box.y, target.y, false);
    bestY = consider(bestY, box.y + box.h, target.y + target.h, false);
    bestY = consider(bestY, box.y + box.h / 2, target.y + target.h / 2, false);
  }

  return {
    dx: Number.isFinite(bestX.distance) ? bestX.delta : 0,
    dy: Number.isFinite(bestY.distance) ? bestY.delta : 0,
    contactX: bestX.contact && Number.isFinite(bestX.distance),
    contactY: bestY.contact && Number.isFinite(bestY.distance),
  };
}

/** Oberstes Objekt unter dem Punkt. */
export function hitTest(objects, point) {
  for (let index = objects.length - 1; index >= 0; index -= 1) {
    const box = boundsOf(objects[index]);
    if (point.x >= box.x && point.x <= box.x + box.w
      && point.y >= box.y && point.y <= box.y + box.h) return objects[index];
  }
  return null;
}

function tracePrimitive(context, object) {
  const box = boundsOf(object);
  context.beginPath();
  if (object.type === "ellipse") {
    context.ellipse(box.x + box.w / 2, box.y + box.h / 2, box.w / 2, box.h / 2, 0, 0, Math.PI * 2);
  } else if (object.type === "triangle") {
    context.moveTo(box.x + box.w / 2, box.y);
    context.lineTo(box.x + box.w, box.y + box.h);
    context.lineTo(box.x, box.y + box.h);
    context.closePath();
  } else {
    context.rect(box.x, box.y, box.w, box.h);
  }
}

/** Hintergrundraster. */
function drawGrid(context, width, height) {
  context.strokeStyle = "#0f2836";
  context.lineWidth = 1;
  context.beginPath();
  for (let x = 0; x < width; x += 25) { context.moveTo(x, 0); context.lineTo(x, height); }
  for (let y = 0; y < height; y += 25) { context.moveTo(0, y); context.lineTo(width, y); }
  context.stroke();
}

/**
 * Zeichnet die berechnete Schwingung.
 *
 * Dargestellt wird die Ueberlagerung der untersten Eigenmoden, gewichtet
 * mit ihrer Anregungsstaerke. Die Zeit ist so skaliert, dass die Grundmode
 * gut sichtbar schwingt; die Frequenzverhaeltnisse der Moden bleiben dabei
 * erhalten. Das ist die tatsaechlich gerechnete Bewegung des Koerpers, kein
 * Ersatzeffekt.
 */
function drawVibration(context, analysis, time, intensity) {
  const { shapes, grid } = analysis;
  if (!shapes || !shapes.used) return;
  const { field, cells, frequencies, weights, used, nodes } = shapes;
  const base = frequencies[0] || 1;
  const size = grid.spacing;

  const amplitude = new Float64Array(used);
  for (let k = 0; k < used; k += 1) {
    amplitude[k] = weights[k] * Math.sin(2 * Math.PI * 1.1 * (frequencies[k] / base) * time);
  }

  // Die Auslenkung wird auf Farbstufen quantisiert und nach Stufe gebuendelt
  // gezeichnet. Ein fillStyle je Zelle wuerde pro Bild tausend Farbstrings
  // erzeugen und den Bildaufbau ausbremsen.
  const buckets = new Map();
  for (let node = 0; node < nodes; node += 1) {
    let value = 0;
    for (let k = 0; k < used; k += 1) value += amplitude[k] * field[k * nodes + node];
    const level = Math.max(-1, Math.min(1, value * 1.6)) * intensity;
    const step = Math.round(level * SHADES);
    if (step === 0) continue;
    let bucket = buckets.get(step);
    if (!bucket) { bucket = []; buckets.set(step, bucket); }
    bucket.push(cells[node]);
  }

  for (const [step, list] of buckets) {
    context.fillStyle = shadeColor(step);
    for (const cell of list) {
      const col = cell % grid.cols;
      const row = (cell - col) / grid.cols;
      context.fillRect(
        grid.originX + col * size,
        grid.originY + row * size,
        size + 0.6,
        size + 0.6,
      );
    }
  }
}

const SHADES = 14;
const SHADE_CACHE = new Map();

function shadeColor(step) {
  let color = SHADE_CACHE.get(step);
  if (!color) {
    const alpha = (Math.abs(step) / SHADES) * 0.5;
    color = step > 0
      ? `rgba(255,95,189,${alpha.toFixed(3)})`
      : `rgba(85,228,255,${alpha.toFixed(3)})`;
    SHADE_CACHE.set(step, color);
  }
  return color;
}

/** Umriss jeder Zusammenhangskomponente aus den Rasterkanten. */
function drawParts(context, analysis) {
  const { grid, labels, activeIndex } = analysis;
  const { cols, rows, spacing, originX, originY } = grid;

  // Flaechen und Randkanten je Komponente in einen Pfad sammeln und einmal
  // zeichnen. Ein fill() und stroke() je Zelle waere bei ueber tausend
  // Zellen der teuerste Teil des Bildaufbaus.
  const fills = new Map();
  const outlines = new Map();
  for (let row = 0; row < rows; row += 1) {
    for (let col = 0; col < cols; col += 1) {
      const label = labels[row * cols + col];
      if (label < 0) continue;
      let path = outlines.get(label);
      if (!path) {
        path = new Path2D();
        outlines.set(label, path);
        fills.set(label, new Path2D());
      }
      fills.get(label).rect(
        originX + col * spacing, originY + row * spacing, spacing + 0.6, spacing + 0.6,
      );
      const x = originX + col * spacing;
      const y = originY + row * spacing;
      if (row === 0 || labels[(row - 1) * cols + col] !== label) {
        path.moveTo(x, y); path.lineTo(x + spacing, y);
      }
      if (row === rows - 1 || labels[(row + 1) * cols + col] !== label) {
        path.moveTo(x, y + spacing); path.lineTo(x + spacing, y + spacing);
      }
      if (col === 0 || labels[row * cols + col - 1] !== label) {
        path.moveTo(x, y); path.lineTo(x, y + spacing);
      }
      if (col === cols - 1 || labels[row * cols + col + 1] !== label) {
        path.moveTo(x + spacing, y); path.lineTo(x + spacing, y + spacing);
      }
    }
  }

  for (const [label, path] of fills) {
    context.fillStyle = label === activeIndex ? partColor(label).fill : "rgba(120,150,168,.08)";
    context.fill(path);
  }
  context.lineWidth = 2;
  for (const [label, path] of outlines) {
    context.strokeStyle = label === activeIndex ? partColor(label).line : "rgba(140,170,190,.45)";
    context.stroke(path);
  }
}

function drawPoint(context, point, color, label, active) {
  context.save();
  context.beginPath();
  context.arc(point.x, point.y, active ? 10 : 8, 0, Math.PI * 2);
  context.fillStyle = "#061018";
  context.fill();
  context.lineWidth = 3;
  context.strokeStyle = color;
  context.stroke();
  context.beginPath();
  context.arc(point.x, point.y, 3, 0, Math.PI * 2);
  context.fillStyle = color;
  context.fill();
  context.font = "600 11px system-ui,sans-serif";
  context.fillStyle = color;
  context.fillText(label, point.x + 13, point.y + 4);
  context.restore();
}

/** Vollstaendige Darstellung eines Bildes. */
export function drawScene(context, size, state, time) {
  context.clearRect(0, 0, size.width, size.height);
  drawGrid(context, size.width, size.height);

  if (state.analysis && !state.analysis.empty) {
    drawParts(context, state.analysis);
    const intensity = state.sounding ? 0.55 + Math.min(1, state.level * 9) * 0.45 : 0.4;
    drawVibration(context, state.analysis, time, intensity);
  }

  // Die gezeichneten Primitive bleiben als duenne Kontur sichtbar, damit
  // erkennbar ist, aus welchen Teilen der Koerper besteht.
  for (const object of state.objects) {
    const chosen = state.selected.has(object.id);
    context.save();
    tracePrimitive(context, object);
    context.setLineDash(chosen ? [] : [5, 4]);
    context.lineWidth = chosen ? 2.5 : 1.2;
    context.strokeStyle = chosen ? "#ffd166" : "rgba(190,215,230,.55)";
    context.stroke();
    if (chosen) {
      const box = boundsOf(object);
      context.setLineDash([]);
      context.fillStyle = "#ffd166";
      for (const [x, y] of [
        [box.x, box.y], [box.x + box.w, box.y],
        [box.x + box.w, box.y + box.h], [box.x, box.y + box.h],
      ]) context.fillRect(x - 3.5, y - 3.5, 7, 7);
    }
    context.restore();
  }

  if (state.snapHint) {
    context.save();
    context.strokeStyle = "rgba(126,255,168,.85)";
    context.lineWidth = 1.5;
    context.setLineDash([4, 4]);
    const box = state.snapHint;
    context.strokeRect(box.x - 3, box.y - 3, box.w + 6, box.h + 6);
    context.restore();
  }

  if (state.objects.length) {
    drawPoint(context, state.excite, "#ff9f45", "Anregung", state.dragging === "excite");
    drawPoint(context, state.pickup, "#7effa8", "Abnahme", state.dragging === "pickup");
  }
}
