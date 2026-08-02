// eigen.js
//
// Loest das Eigenwertproblem des diskreten Dirichlet-Laplace-Operators auf
// einem beliebigen Rastergebiet:
//
//     -Delta phi = lambda phi        im Gebiet
//              phi = 0               auf dem Rand
//
// Das ist der physikalische Kern des Projekts. Die Eigenwerte lambda_i sind
// die Resonanzen der tatsaechlich gezeichneten Flaeche, die Eigenvektoren
// phi_i deren Schwingungsformen.
//
// Diskretisierung: Finite Differenzen (Fuenf-Punkte-Stern) auf einem
// regelmaessigen Raster. Kein FEM-Netz, aber dieselbe partielle
// Differentialgleichung.
//
// Verfahren: Blockweise inverse Iteration mit Rayleigh-Ritz-Projektion.
// Die Systemloesung erfolgt exakt ueber eine Band-Cholesky-Zerlegung, nicht
// iterativ, weil die Bandbreite bei zeilenweiser Knotennummerierung durch
// die Rasterbreite beschraenkt ist. Dadurch ist ein Durchlauf schnell genug
// fuer interaktive Bedienung.
//
// Reines ES-Modul: kein DOM, kein AudioWorklet. Laeuft unter node und ist
// dadurch gegen geschlossene Loesungen testbar.

/**
 * Zyklisches Jacobi-Verfahren fuer kleine dichte symmetrische Matrizen.
 * Wird nur fuer die m x m Projektion im Rayleigh-Ritz-Schritt gebraucht.
 *
 * @param {ArrayLike<number>} input zeilenweise, symmetrisch, n x n
 * @param {number} n
 * @returns {{values: Float64Array, vectors: Float64Array}} aufsteigend
 *          sortiert; vectors[k * n + j] ist Komponente k des j-ten Vektors.
 */
export function jacobiEigen(input, n, sweeps = 100) {
  const a = Float64Array.from(input);
  const v = new Float64Array(n * n);
  for (let i = 0; i < n; i += 1) v[i * n + i] = 1;

  for (let sweep = 0; sweep < sweeps; sweep += 1) {
    let off = 0;
    for (let p = 0; p < n; p += 1) {
      for (let q = p + 1; q < n; q += 1) off += a[p * n + q] * a[p * n + q];
    }
    if (off <= 1e-26) break;

    for (let p = 0; p < n - 1; p += 1) {
      for (let q = p + 1; q < n; q += 1) {
        const apq = a[p * n + q];
        if (Math.abs(apq) < 1e-300) continue;
        const theta = (a[q * n + q] - a[p * n + p]) / (2 * apq);
        const sign = theta >= 0 ? 1 : -1;
        const t = sign / (Math.abs(theta) + Math.sqrt(theta * theta + 1));
        const c = 1 / Math.sqrt(t * t + 1);
        const s = t * c;
        // A <- J^T A J, spaltenweise dann zeilenweise.
        for (let k = 0; k < n; k += 1) {
          const akp = a[k * n + p];
          const akq = a[k * n + q];
          a[k * n + p] = c * akp - s * akq;
          a[k * n + q] = s * akp + c * akq;
        }
        for (let k = 0; k < n; k += 1) {
          const apk = a[p * n + k];
          const aqk = a[q * n + k];
          a[p * n + k] = c * apk - s * aqk;
          a[q * n + k] = s * apk + c * aqk;
        }
        for (let k = 0; k < n; k += 1) {
          const vkp = v[k * n + p];
          const vkq = v[k * n + q];
          v[k * n + p] = c * vkp - s * vkq;
          v[k * n + q] = s * vkp + c * vkq;
        }
      }
    }
  }

  const raw = new Float64Array(n);
  for (let i = 0; i < n; i += 1) raw[i] = a[i * n + i];
  const order = Array.from({ length: n }, (_unused, i) => i)
    .sort((x, y) => raw[x] - raw[y]);

  const values = new Float64Array(n);
  const vectors = new Float64Array(n * n);
  order.forEach((source, target) => {
    values[target] = raw[source];
    for (let k = 0; k < n; k += 1) vectors[k * n + target] = v[k * n + source];
  });
  return { values, vectors };
}

/**
 * Multiplikation mit M = 4I - Nachbarschaft. Das ist h^2 * (-Delta); der
 * Faktor 1/h^2 wird erst ganz am Ende auf die Eigenwerte angewendet, was
 * numerisch guenstiger ist.
 */
export function laplacianMatvec(mesh, x, out) {
  const { count, neighbors } = mesh;
  for (let i = 0; i < count; i += 1) {
    let value = 4 * x[i];
    const base = i * 4;
    for (let k = 0; k < 4; k += 1) {
      const j = neighbors[base + k];
      if (j >= 0) value -= x[j];
    }
    out[i] = value;
  }
  return out;
}

/**
 * Cholesky-Zerlegung von M in Bandspeicherung. Erwartet, dass M positiv
 * definit ist; das gilt fuer jedes Gebiet mit Dirichlet-Rand, weil M dann
 * diagonaldominant mit mindestens einer strikten Zeile ist.
 *
 * Speicherlayout: lower[i * (b + 1) + (i - j)] = L[i][j] fuer i-b <= j <= i.
 */
export function bandCholesky(mesh) {
  const { count, neighbors } = mesh;
  let bandwidth = 0;
  for (let i = 0; i < count; i += 1) {
    for (let k = 0; k < 4; k += 1) {
      const j = neighbors[i * 4 + k];
      if (j >= 0) bandwidth = Math.max(bandwidth, Math.abs(i - j));
    }
  }

  const width = bandwidth + 1;
  const lower = new Float64Array(count * width);
  // Bandmatrix von M aufbauen.
  for (let i = 0; i < count; i += 1) {
    lower[i * width] = 4;
    for (let k = 0; k < 4; k += 1) {
      const j = neighbors[i * 4 + k];
      if (j >= 0 && j < i) lower[i * width + (i - j)] = -1;
    }
  }

  for (let i = 0; i < count; i += 1) {
    const low = Math.max(0, i - bandwidth);
    for (let j = low; j <= i; j += 1) {
      let sum = lower[i * width + (i - j)];
      const start = Math.max(low, j - bandwidth);
      for (let k = start; k < j; k += 1) {
        sum -= lower[i * width + (i - k)] * lower[j * width + (j - k)];
      }
      if (i === j) {
        if (sum <= 0) throw new Error("Matrix ist nicht positiv definit");
        lower[i * width] = Math.sqrt(sum);
      } else {
        lower[i * width + (i - j)] = sum / lower[j * width];
      }
    }
  }
  return { lower, width, bandwidth, count };
}

/** Loest M x = b mit der Band-Cholesky-Zerlegung. */
export function bandSolve(factor, b, out) {
  const { lower, width, bandwidth, count } = factor;
  for (let i = 0; i < count; i += 1) {
    let sum = b[i];
    const low = Math.max(0, i - bandwidth);
    for (let j = low; j < i; j += 1) sum -= lower[i * width + (i - j)] * out[j];
    out[i] = sum / lower[i * width];
  }
  for (let i = count - 1; i >= 0; i -= 1) {
    let sum = out[i];
    const high = Math.min(count - 1, i + bandwidth);
    for (let j = i + 1; j <= high; j += 1) sum -= lower[j * width + (j - i)] * out[j];
    out[i] = sum / lower[i * width];
  }
  return out;
}

function seededRandom(seed) {
  let state = (seed >>> 0) || 1;
  return () => {
    state ^= state << 13; state >>>= 0;
    state ^= state >> 17;
    state ^= state << 5; state >>>= 0;
    return state / 4294967296;
  };
}

/** Modifiziertes Gram-Schmidt auf den Spalten eines n x m Blocks. */
function orthonormalize(block, count, vectors) {
  let kept = 0;
  for (let j = 0; j < vectors; j += 1) {
    const column = block.subarray(j * count, (j + 1) * count);
    for (let k = 0; k < kept; k += 1) {
      const other = block.subarray(k * count, (k + 1) * count);
      let dot = 0;
      for (let i = 0; i < count; i += 1) dot += column[i] * other[i];
      for (let i = 0; i < count; i += 1) column[i] -= dot * other[i];
    }
    let norm = 0;
    for (let i = 0; i < count; i += 1) norm += column[i] * column[i];
    norm = Math.sqrt(norm);
    if (norm < 1e-10) continue;
    const inverse = 1 / norm;
    for (let i = 0; i < count; i += 1) column[i] *= inverse;
    if (kept !== j) block.copyWithin(kept * count, j * count, (j + 1) * count);
    kept += 1;
  }
  return kept;
}

/**
 * Die niedrigsten Eigenpaare des Gebiets.
 *
 * @param {object} mesh aus geometry.js: {count, neighbors, spacing, ...}
 * @param {number} wanted Anzahl gesuchter Moden
 * @returns {{lambdas: Float64Array, shapes: Float64Array, count: number}}
 *          lambdas in 1/px^2; shapes[k * count + i] ist phi_k am Knoten i,
 *          normiert auf Maximalbetrag 1.
 */
export function lowestModes(mesh, wanted, options = {}) {
  const { count } = mesh;
  const maxOuter = options.maxOuter ?? 80;
  const tolerance = options.tolerance ?? 1e-10;
  const modes = Math.min(wanted, count);
  if (modes <= 0) return { lambdas: new Float64Array(0), shapes: new Float64Array(0), count: 0 };

  // Zusaetzliche Schutzvektoren beschleunigen die Konvergenz der untersten
  // Moden erheblich, weil die Konvergenzrate von lambda_i / lambda_(block+1)
  // abhaengt.
  const block = Math.min(count, modes + 6);
  const factor = bandCholesky(mesh);
  const random = seededRandom(options.seed ?? 12345);

  let current = new Float64Array(block * count);
  for (let i = 0; i < current.length; i += 1) current[i] = random() * 2 - 1;
  let live = orthonormalize(current, count, block);

  let next = new Float64Array(block * count);
  const product = new Float64Array(block * count);
  const projection = new Float64Array(block * block);
  let previous = new Float64Array(modes).fill(Infinity);
  let values = new Float64Array(block);
  let vectors = new Float64Array(block * block);

  for (let outer = 0; outer < maxOuter; outer += 1) {
    for (let j = 0; j < live; j += 1) {
      bandSolve(factor,
        current.subarray(j * count, (j + 1) * count),
        next.subarray(j * count, (j + 1) * count));
    }
    live = orthonormalize(next, count, live);
    if (live === 0) break;

    for (let j = 0; j < live; j += 1) {
      laplacianMatvec(mesh,
        next.subarray(j * count, (j + 1) * count),
        product.subarray(j * count, (j + 1) * count));
    }
    for (let p = 0; p < live; p += 1) {
      for (let q = p; q < live; q += 1) {
        let dot = 0;
        for (let i = 0; i < count; i += 1) {
          dot += next[p * count + i] * product[q * count + i];
        }
        projection[p * live + q] = dot;
        projection[q * live + p] = dot;
      }
    }

    const solved = jacobiEigen(projection.subarray(0, live * live), live);
    values = solved.values;
    vectors = solved.vectors;

    // Ritz-Vektoren: current <- next * vectors
    current.fill(0);
    for (let j = 0; j < live; j += 1) {
      for (let k = 0; k < live; k += 1) {
        const weight = vectors[k * live + j];
        if (weight === 0) continue;
        for (let i = 0; i < count; i += 1) {
          current[j * count + i] += next[k * count + i] * weight;
        }
      }
    }

    let converged = true;
    const checked = Math.min(modes, live);
    for (let i = 0; i < checked; i += 1) {
      const scale = Math.max(1e-12, Math.abs(values[i]));
      if (Math.abs(values[i] - previous[i]) / scale > tolerance) { converged = false; break; }
    }
    for (let i = 0; i < checked; i += 1) previous[i] = values[i];
    if (converged) break;
  }

  const produced = Math.min(modes, live);
  const inverseSpacing = 1 / (mesh.spacing * mesh.spacing);
  const lambdas = new Float64Array(produced);
  const shapes = new Float64Array(produced * count);
  for (let k = 0; k < produced; k += 1) {
    lambdas[k] = values[k] * inverseSpacing;
    let peak = 0;
    for (let i = 0; i < count; i += 1) {
      peak = Math.max(peak, Math.abs(current[k * count + i]));
    }
    const inverse = peak > 1e-12 ? 1 / peak : 0;
    for (let i = 0; i < count; i += 1) {
      shapes[k * count + i] = current[k * count + i] * inverse;
    }
  }
  return { lambdas, shapes, count: produced };
}
