// geometry.js
//
// Wandelt die gezeichneten Primitive in ein Rastergebiet um und zerlegt
// dieses in Zusammenhangskomponenten.
//
// Das Raster hat zwei Aufgaben zugleich:
//   1. es ist das Rechengitter fuer den Eigenloeser in eigen.js;
//   2. es entscheidet, was "verbunden" heisst.
//
// Dadurch braucht es keinen Verbinden-Knopf mehr: Teile, die sich beruehren
// oder ueberlappen, belegen benachbarte Rasterzellen und werden damit
// automatisch zu einem einzigen Klangkoerper. Das ist zugleich die
// physikalisch richtige Aussage, denn eine Membran leitet Schwingung nur
// ueber tatsaechlichen Kontakt weiter.
//
// Reines ES-Modul, unter node testbar.

/** Vier-Nachbarschaft. Eine reine Eckberuehrung uebertraegt nichts. */
const NEIGHBOR_OFFSETS = [[1, 0], [-1, 0], [0, 1], [0, -1]];

function sign(px, py, ax, ay, bx, by) {
  return (px - bx) * (ay - by) - (ax - bx) * (py - by);
}

function pointInTriangle(px, py, ax, ay, bx, by, cx, cy) {
  const d1 = sign(px, py, ax, ay, bx, by);
  const d2 = sign(px, py, bx, by, cx, cy);
  const d3 = sign(px, py, cx, cy, ax, ay);
  const negative = d1 < 0 || d2 < 0 || d3 < 0;
  const positive = d1 > 0 || d2 > 0 || d3 > 0;
  return !(negative && positive);
}

/** Normalisierte Achsenbox eines Objekts, unabhaengig vom Vorzeichen. */
export function boundsOf(object) {
  const w = Math.abs(object.w);
  const h = Math.abs(object.h);
  return {
    x: Math.min(object.x, object.x + object.w),
    y: Math.min(object.y, object.y + object.h),
    w,
    h,
  };
}

/** Liegt der Punkt innerhalb des Primitivs? */
export function shapeContains(object, px, py) {
  const box = boundsOf(object);
  if (box.w <= 0 || box.h <= 0) return false;
  if (object.type === "ellipse") {
    const rx = box.w / 2;
    const ry = box.h / 2;
    const dx = (px - (box.x + rx)) / rx;
    const dy = (py - (box.y + ry)) / ry;
    return dx * dx + dy * dy <= 1;
  }
  if (object.type === "triangle") {
    return pointInTriangle(
      px, py,
      box.x + box.w / 2, box.y,
      box.x + box.w, box.y + box.h,
      box.x, box.y + box.h,
    );
  }
  return px >= box.x && px <= box.x + box.w && py >= box.y && py <= box.y + box.h;
}

export function containedInAny(objects, px, py) {
  for (const object of objects) {
    if (shapeContains(object, px, py)) return true;
  }
  return false;
}

/** Umschliessende Box aller Objekte. */
export function overallBounds(objects) {
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
 * Rastert die Objekte.
 *
 * Eine Zelle gilt als belegt, wenn die Mehrheit von drei mal drei
 * Teilproben innerhalb irgendeines Objekts liegt. Die Mehrheitsregel
 * naehert den Flaechenanteil an und vergroessert das Gebiet - anders als
 * eine Eckenprobe - nicht systematisch.
 *
 * Die Proben pruefen gegen die Vereinigung aller Objekte, nicht gegen jedes
 * einzeln. Zwei Teile mit gemeinsamer Kante belegen deshalb zuverlaessig
 * benachbarte Zellen.
 *
 * @param {Array} objects
 * @param {number} resolution Zellen auf der laengsten Seite
 * @returns {?{cols, rows, spacing, originX, originY, filled: Uint8Array}}
 */
export function rasterize(objects, resolution = 40) {
  const bounds = overallBounds(objects);
  if (!bounds || bounds.w <= 0 || bounds.h <= 0) return null;

  const spacing = Math.max(bounds.w, bounds.h) / resolution;
  if (!(spacing > 0)) return null;

  // Eine Randspur bleibt frei; sie traegt die Dirichlet-Bedingung phi = 0.
  const margin = 1.5;
  const cols = Math.ceil(bounds.w / spacing) + 2 * Math.ceil(margin);
  const rows = Math.ceil(bounds.h / spacing) + 2 * Math.ceil(margin);
  const originX = bounds.x - margin * spacing;
  const originY = bounds.y - margin * spacing;

  const filled = new Uint8Array(cols * rows);
  const step = spacing / 4;
  for (let row = 0; row < rows; row += 1) {
    for (let col = 0; col < cols; col += 1) {
      const cellX = originX + col * spacing;
      const cellY = originY + row * spacing;
      let hits = 0;
      for (let sy = 1; sy <= 3; sy += 1) {
        for (let sx = 1; sx <= 3; sx += 1) {
          if (containedInAny(objects, cellX + sx * step, cellY + sy * step)) hits += 1;
        }
      }
      if (hits >= 5) filled[row * cols + col] = 1;
    }
  }
  return { cols, rows, spacing, originX, originY, filled };
}

/**
 * Zerlegt das Raster in Zusammenhangskomponenten (Vier-Nachbarschaft) und
 * baut fuer jede die Knotenstruktur, die eigen.js erwartet.
 *
 * Die Knotennummerierung ist zeilenweise. Dadurch bleibt die Bandbreite der
 * Systemmatrix durch die Rasterbreite beschraenkt, was die Band-Cholesky in
 * eigen.js erst effizient macht.
 */
export function components(grid) {
  const { cols, rows, filled, spacing, originX, originY } = grid;
  const label = new Int32Array(cols * rows).fill(-1);
  const found = [];
  const queue = new Int32Array(cols * rows);

  for (let start = 0; start < filled.length; start += 1) {
    if (!filled[start] || label[start] >= 0) continue;
    const id = found.length;
    let head = 0;
    let tail = 0;
    queue[tail] = start;
    tail += 1;
    label[start] = id;
    const cells = [];
    while (head < tail) {
      const cell = queue[head];
      head += 1;
      cells.push(cell);
      const col = cell % cols;
      const row = (cell - col) / cols;
      for (const [dx, dy] of NEIGHBOR_OFFSETS) {
        const nextCol = col + dx;
        const nextRow = row + dy;
        if (nextCol < 0 || nextCol >= cols || nextRow < 0 || nextRow >= rows) continue;
        const next = nextRow * cols + nextCol;
        if (!filled[next] || label[next] >= 0) continue;
        label[next] = id;
        queue[tail] = next;
        tail += 1;
      }
    }
    cells.sort((a, b) => a - b);
    found.push(cells);
  }

  return found.map((cells, id) => {
    const count = cells.length;
    const nodeOf = new Int32Array(cols * rows).fill(-1);
    cells.forEach((cell, index) => { nodeOf[cell] = index; });
    const neighbors = new Int32Array(count * 4).fill(-1);
    let area = 0;
    let sumX = 0;
    let sumY = 0;
    cells.forEach((cell, index) => {
      const col = cell % cols;
      const row = (cell - col) / cols;
      area += spacing * spacing;
      sumX += originX + (col + 0.5) * spacing;
      sumY += originY + (row + 0.5) * spacing;
      NEIGHBOR_OFFSETS.forEach(([dx, dy], slot) => {
        const nextCol = col + dx;
        const nextRow = row + dy;
        if (nextCol < 0 || nextCol >= cols || nextRow < 0 || nextRow >= rows) return;
        neighbors[index * 4 + slot] = nodeOf[nextRow * cols + nextCol];
      });
    });
    return {
      id,
      count,
      cells,
      nodeOf,
      neighbors,
      spacing,
      originX,
      originY,
      cols,
      rows,
      area,
      centroid: { x: sumX / count, y: sumY / count },
    };
  });
}

/** Rastern und zerlegen in einem Schritt. */
export function analyzeGeometry(objects, resolution = 40) {
  const grid = rasterize(objects, resolution);
  if (!grid) return { grid: null, parts: [] };
  return { grid, parts: components(grid) };
}

/** Der Knoten einer Komponente, der einem Punkt in Pixeln am naechsten ist. */
export function nodeNearest(part, px, py) {
  let best = -1;
  let bestDistance = Infinity;
  for (let index = 0; index < part.count; index += 1) {
    const cell = part.cells[index];
    const col = cell % part.cols;
    const row = (cell - col) / part.cols;
    const x = part.originX + (col + 0.5) * part.spacing;
    const y = part.originY + (row + 0.5) * part.spacing;
    const distance = (x - px) * (x - px) + (y - py) * (y - py);
    if (distance < bestDistance) { bestDistance = distance; best = index; }
  }
  return { node: best, distance: Math.sqrt(bestDistance) };
}

/** Enthaelt die Komponente den Punkt? */
export function partContains(part, px, py) {
  const col = Math.floor((px - part.originX) / part.spacing);
  const row = Math.floor((py - part.originY) / part.spacing);
  if (col < 0 || col >= part.cols || row < 0 || row >= part.rows) return false;
  return part.nodeOf[row * part.cols + col] >= 0;
}

/**
 * Waehlt die Komponente, die den Anregungspunkt traegt. Liegt der Punkt in
 * keiner, gewinnt die flaechengroesste - so bleibt immer etwas hoerbar.
 */
export function activePart(parts, px, py) {
  if (!parts.length) return null;
  for (const part of parts) {
    if (partContains(part, px, py)) return part;
  }
  return parts.reduce((best, part) => (part.count > best.count ? part : best), parts[0]);
}
