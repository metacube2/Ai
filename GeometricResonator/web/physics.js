// physics.js
//
// Uebersetzt die Eigenloesung des Gebiets in hoerbare Modalparameter.
//
// Entscheidend sind drei Punkte, an denen frueher der Klang verloren ging:
//
//  1. Die Frequenzen sind ABSOLUT. Sie folgen aus Geometrie und Material,
//     nicht aus dem Tonhoehenregler. Der Koerper hat feste Resonanzen, so
//     wie ein realer Korpus. Der Regler steuert nur noch die Anregung.
//
//  2. Die Lautstaerke jeder Mode kommt aus dem EIGENVEKTOR, ausgewertet am
//     Anregungs- und am Abnahmepunkt:  g_i = phi_i(x_a) * phi_i(x_b).
//     Eine Mode mit einem Schwingungsknoten am Anregungspunkt bleibt stumm.
//     Genau das macht verschiedene Formen unterscheidbar; eine feste
//     Huellkurve ueber dem Modenindex wuerde jede Form gleich klingen
//     lassen.
//
//  3. Keine Entartungsbereinigung. Ein Quadrat hat die Moden (1,2) und
//     (2,1) wirklich beide, und leicht aufgespaltene Paare erzeugen die
//     Schwebungen, die einen Koerper lebendig klingen lassen.

import { lowestModes } from "./eigen.js";
import { nodeNearest } from "./geometry.js";

/** 1 px der Zeichenflaeche entspricht 1 mm. */
export const PIXELS_PER_METRE = 1000;

/**
 * Materialien. `speed` ist die effektive Wellenausbreitungsgeschwindigkeit
 * der Flaechenschwingung in m/s, `q0` die Guete der Grundmode und `alpha`
 * deren Abfall zu hohen Frequenzen. Die Werte sind so gewaehlt, dass
 * typische Zeichnungen in einem musikalisch brauchbaren Bereich landen,
 * und nicht aus einem Werkstoffdatenblatt entnommen.
 */
export const MATERIALS = {
  wood: { label: "Holz", speed: 105, q0: 160, alpha: 0.55 },
  metal: { label: "Metall", speed: 200, q0: 2600, alpha: 0.30 },
  glass: { label: "Glas", speed: 168, q0: 1000, alpha: 0.42 },
  plastic: { label: "Kunststoff", speed: 76, q0: 40, alpha: 0.85 },
};

export const DEFAULT_MATERIAL = "wood";

const MIN_FREQUENCY = 22;
const MAX_FREQUENCY = 15500;

/**
 * Berechnet die Modalparameter eines Klangkoerpers.
 *
 * @param {object} part Zusammenhangskomponente aus geometry.js
 * @param {object} settings {material, damping, brightness, excite:{x,y}, pickup:{x,y}, modes}
 * @returns {{modes: Array<{frequency,q,gain}>, shapes: object, diagnostics: object}}
 */
export function modalModel(part, settings) {
  const material = MATERIALS[settings.material] ?? MATERIALS[DEFAULT_MATERIAL];
  const wanted = settings.modes ?? 18;
  const started = Date.now();
  const solved = lowestModes(part, wanted);

  const excite = nodeNearest(part, settings.excite.x, settings.excite.y);
  const pickup = nodeNearest(part, settings.pickup.x, settings.pickup.y);

  const scale = (material.speed * PIXELS_PER_METRE) / (2 * Math.PI);
  const damping = clamp(settings.damping ?? 0.45, 0, 1);
  const brightness = clamp(settings.brightness ?? 0.55, 0, 1);
  const dampingScale = 0.05 + 1.6 * (1 - damping) * (1 - damping);
  const tilt = 2.0 * (1 - brightness);

  const raw = [];
  let fundamental = 0;
  for (let index = 0; index < solved.count; index += 1) {
    const lambda = solved.lambdas[index];
    if (!(lambda > 0)) continue;
    const frequency = scale * Math.sqrt(lambda);
    if (!Number.isFinite(frequency)) continue;
    if (frequency < MIN_FREQUENCY || frequency > MAX_FREQUENCY) continue;
    if (!fundamental) fundamental = frequency;

    const base = index * part.count;
    // Der Kern: Beteiligung der Mode an Anregung und Abnahme.
    const coupling = solved.shapes[base + excite.node] * solved.shapes[base + pickup.node];
    const relative = frequency / fundamental;
    const gain = coupling * Math.pow(relative, -tilt);
    const q = clamp(material.q0 * Math.pow(relative, -material.alpha) * dampingScale, 2.5, 9000);
    raw.push({ frequency, q, gain, index });
  }

  let peak = 0;
  for (const mode of raw) peak = Math.max(peak, Math.abs(mode.gain));
  const inverse = peak > 1e-9 ? 1 / peak : 0;
  const modes = raw.map((mode) => ({
    frequency: mode.frequency,
    q: mode.q,
    gain: mode.gain * inverse,
  }));

  return {
    modes,
    shapes: displayField(part, solved, modes, raw),
    diagnostics: {
      material: material.label,
      nodes: part.count,
      spacing: part.spacing,
      area: part.area,
      solved: solved.count,
      kept: modes.length,
      fundamental,
      exciteDistance: excite.distance,
      pickupDistance: pickup.distance,
      milliseconds: Date.now() - started,
    },
  };
}

/**
 * Bereitet die untersten Schwingungsformen fuer die Darstellung auf. Die
 * Oberflaeche animiert daraus die tatsaechlich berechnete Bewegung des
 * Koerpers, nicht eine dekorative Ersatzanimation.
 */
function displayField(part, solved, modes, raw, limit = 6) {
  const used = Math.min(limit, raw.length);
  const field = new Float32Array(used * part.count);
  const frequencies = new Float64Array(used);
  const weights = new Float64Array(used);
  for (let k = 0; k < used; k += 1) {
    const source = raw[k].index * part.count;
    field.set(solved.shapes.subarray(source, source + part.count), k * part.count);
    frequencies[k] = modes[k].frequency;
    weights[k] = modes[k].gain;
  }
  return {
    used,
    nodes: part.count,
    cols: part.cols,
    rows: part.rows,
    spacing: part.spacing,
    originX: part.originX,
    originY: part.originY,
    cells: Int32Array.from(part.cells),
    field,
    frequencies,
    weights,
  };
}

function clamp(value, low, high) {
  return Math.min(high, Math.max(low, value));
}
