// modes-worker.js
//
// Rechnet die Eigenmoden ausserhalb des Hauptthreads. Die Loesung dauert je
// nach Aufloesung einige zehn bis wenige hundert Millisekunden - genug, um
// das Zeichnen sichtbar stocken zu lassen, wenn sie im UI-Thread liefe.

import { analyzeGeometry, activePart } from "./geometry.js";
import { modalModel } from "./physics.js";

self.onmessage = (event) => {
  const request = event.data || {};
  const { token, objects, settings, resolution, excite, pickup } = request;
  try {
    const { grid, parts } = analyzeGeometry(objects || [], resolution || 38);
    if (!grid || !parts.length) {
      self.postMessage({ token, empty: true });
      return;
    }

    const part = activePart(parts, excite.x, excite.y);
    const model = modalModel(part, { ...settings, excite, pickup });

    // Zuordnung Zelle -> Komponente, damit die Oberflaeche zeigen kann,
    // welche Teile tatsaechlich einen gemeinsamen Koerper bilden.
    const labels = new Int32Array(grid.cols * grid.rows).fill(-1);
    parts.forEach((item, index) => {
      for (const cell of item.cells) labels[cell] = index;
    });

    const payload = {
      token,
      empty: false,
      modes: model.modes,
      diagnostics: {
        ...model.diagnostics,
        parts: parts.length,
        activePart: part.id,
        activeCells: part.count,
      },
      grid: {
        cols: grid.cols,
        rows: grid.rows,
        spacing: grid.spacing,
        originX: grid.originX,
        originY: grid.originY,
      },
      labels,
      activeIndex: part.id,
      shapes: model.shapes,
    };

    self.postMessage(payload, [
      labels.buffer,
      model.shapes.field.buffer,
      model.shapes.cells.buffer,
      model.shapes.frequencies.buffer,
      model.shapes.weights.buffer,
    ]);
  } catch (error) {
    self.postMessage({ token, error: error && error.message ? error.message : String(error) });
  }
};
