// Verifikation des Eigenloesers gegen geschlossene Loesungen.
//
// Ausfuehren:  node tests/eigen.test.mjs
//
// Ohne diese Pruefung ist jede Klangaussage wertlos: ein Loeser, der die
// analytisch bekannten Moden von Quadrat und Kreis verfehlt, liefert auch
// fuer gezeichnete Formen keine physikalisch sinnvollen Resonanzen.

import { analyzeGeometry } from "../web/geometry.js";
import { lowestModes } from "../web/eigen.js";

let failures = 0;
let checks = 0;

function report(name, ok, detail) {
  checks += 1;
  if (!ok) failures += 1;
  const mark = ok ? "  ok  " : " FAIL ";
  console.log(`${mark} ${name}${detail ? `\n         ${detail}` : ""}`);
}

function ratios(lambdas) {
  // f ~ sqrt(lambda); Verhaeltnisse der Frequenzen, nicht der Eigenwerte.
  const root = Array.from(lambdas, (value) => Math.sqrt(value));
  return root.map((value) => value / root[0]);
}

function solveFor(objects, resolution, wanted = 10) {
  const { parts } = analyzeGeometry(objects, resolution);
  if (!parts.length) throw new Error("kein Gebiet gerastert");
  const part = parts.reduce((best, item) => (item.count > best.count ? item : best), parts[0]);
  const solved = lowestModes(part, wanted);
  return { part, parts, solved };
}

function maxRelativeError(actual, expected) {
  let worst = 0;
  for (let i = 0; i < expected.length; i += 1) {
    worst = Math.max(worst, Math.abs(actual[i] - expected[i]) / expected[i]);
  }
  return worst;
}

// ---------------------------------------------------------------- Quadrat
// Dirichlet-Quadrat: lambda_mn = pi^2 (m^2 + n^2) / S^2
// Frequenzverhaeltnisse: sqrt((m^2 + n^2) / 2)
{
  const expected = [];
  for (let m = 1; m <= 6; m += 1) {
    for (let n = 1; n <= 6; n += 1) expected.push(Math.sqrt((m * m + n * n) / 2));
  }
  expected.sort((a, b) => a - b);
  const want = expected.slice(0, 10);

  const { solved } = solveFor([{ type: "rect", x: 0, y: 0, w: 200, h: 200 }], 40, 10);
  const got = ratios(solved.lambdas);
  const error = maxRelativeError(got, want);
  report(
    "Quadrat: Modenverhaeltnisse gegen sqrt((m^2+n^2)/2)",
    error < 0.05,
    `max. Fehler ${(error * 100).toFixed(2)} %\n         erwartet ${want.map((v) => v.toFixed(4)).join(" ")}\n         erhalten ${got.map((v) => v.toFixed(4)).join(" ")}`,
  );

  // Entartete Paare (1,2)/(2,1) und (1,3)/(3,1) muessen beide auftauchen.
  const pairGap = Math.abs(got[1] - got[2]) / got[1];
  report(
    "Quadrat: entartetes Paar (1,2)/(2,1) doppelt vorhanden",
    pairGap < 0.02,
    `Abstand der beiden Moden ${(pairGap * 100).toFixed(2)} %`,
  );

  // Absolute Lage: lambda_1 = 2 pi^2 / S^2
  const analytic = (2 * Math.PI * Math.PI) / (200 * 200);
  const absolute = Math.abs(solved.lambdas[0] - analytic) / analytic;
  report(
    "Quadrat: absoluter Grundeigenwert",
    absolute < 0.12,
    `analytisch ${analytic.toExponential(4)}, berechnet ${solved.lambdas[0].toExponential(4)}, Abweichung ${(absolute * 100).toFixed(2)} %`,
  );
}

// ------------------------------------------------------------------ Kreis
// Dirichlet-Kreis: lambda ~ (j_mn / R)^2, Verhaeltnisse der Besselnullstellen.
// Vielfachheiten: j01 einfach, j11 doppelt, j21 doppelt, j02 einfach.
{
  const want = [1, 1.59334, 1.59334, 2.13560, 2.13560, 2.29542];
  const { solved } = solveFor([{ type: "ellipse", x: 0, y: 0, w: 240, h: 240 }], 44, 6);
  const got = ratios(solved.lambdas);
  const error = maxRelativeError(got, want);
  report(
    "Kreis: Modenverhaeltnisse gegen Besselnullstellen",
    error < 0.05,
    `max. Fehler ${(error * 100).toFixed(2)} %\n         erwartet ${want.map((v) => v.toFixed(4)).join(" ")}\n         erhalten ${got.map((v) => v.toFixed(4)).join(" ")}`,
  );
}

// ------------------------------------------------------- Gitterverfeinerung
{
  const series = [24, 32, 44].map((resolution) => {
    const { solved } = solveFor([{ type: "rect", x: 0, y: 0, w: 200, h: 140 }], resolution, 6);
    return ratios(solved.lambdas);
  });
  let drift = 0;
  for (let i = 0; i < 6; i += 1) {
    drift = Math.max(drift, Math.abs(series[2][i] - series[1][i]) / series[2][i]);
  }
  report(
    "Rechteck 200x140: Verhaeltnisse stabil bei Verfeinerung 32 -> 44",
    drift < 0.03,
    `groesste Drift ${(drift * 100).toFixed(2)} %`,
  );
}

// ----------------------------------------- Seitenverhaeltnis wirkt wirklich
// Rechteck 2:1, Dirichlet: f_mn ~ sqrt(m^2/4 + n^2) normiert.
{
  const expected = [];
  for (let m = 1; m <= 8; m += 1) {
    for (let n = 1; n <= 6; n += 1) expected.push(Math.sqrt((m * m) / 4 + n * n));
  }
  expected.sort((a, b) => a - b);
  const want = expected.slice(0, 6).map((value) => value / expected[0]);
  const { solved } = solveFor([{ type: "rect", x: 0, y: 0, w: 240, h: 120 }], 40, 6);
  const got = ratios(solved.lambdas);
  const error = maxRelativeError(got, want);
  report(
    "Rechteck 2:1: Moden folgen dem Seitenverhaeltnis",
    error < 0.05,
    `max. Fehler ${(error * 100).toFixed(2)} %\n         erwartet ${want.map((v) => v.toFixed(4)).join(" ")}\n         erhalten ${got.map((v) => v.toFixed(4)).join(" ")}`,
  );
}

// ------------------------------------------------ Form aendert den Klang
// Die eigentliche Regression fuer "der Klang ist immer gleich": gleiche
// Bounding Box, andere Form -> deutlich andere Moden.
{
  const box = { x: 0, y: 0, w: 200, h: 200 };
  const square = ratios(solveFor([{ type: "rect", ...box }], 40, 6).solved.lambdas);
  const circle = ratios(solveFor([{ type: "ellipse", ...box }], 40, 6).solved.lambdas);
  const triangle = ratios(solveFor([{ type: "triangle", ...box }], 40, 6).solved.lambdas);

  const spread = (a, b) => {
    let worst = 0;
    for (let i = 1; i < 6; i += 1) worst = Math.max(worst, Math.abs(a[i] - b[i]) / b[i]);
    return worst;
  };
  const sc = spread(square, circle);
  const st = spread(square, triangle);
  const ct = spread(circle, triangle);
  report(
    "Gleiche Box, andere Form: Moden unterscheiden sich deutlich",
    sc > 0.02 && st > 0.02 && ct > 0.02,
    `Quadrat/Kreis ${(sc * 100).toFixed(1)} %, Quadrat/Dreieck ${(st * 100).toFixed(1)} %, Kreis/Dreieck ${(ct * 100).toFixed(1)} %`,
  );

  // Groesse verschiebt die absolute Lage, nicht die Verhaeltnisse.
  const small = solveFor([{ type: "rect", x: 0, y: 0, w: 100, h: 100 }], 40, 3).solved;
  const large = solveFor([{ type: "rect", x: 0, y: 0, w: 400, h: 400 }], 40, 3).solved;
  const factor = Math.sqrt(small.lambdas[0] / large.lambdas[0]);
  report(
    "Vierfache Kantenlaenge senkt die Frequenz auf ein Viertel",
    Math.abs(factor - 4) / 4 < 0.05,
    `Faktor ${factor.toFixed(3)} statt 4`,
  );
}

// ------------------------------------------------------------- Verbinden
// Getrennt gezeichnete Teile sind zwei Koerper. Beruehren sie sich, werden
// sie ein groesserer Koerper mit tieferem Grundton.
{
  const apart = analyzeGeometry([
    { type: "rect", x: 0, y: 0, w: 120, h: 120 },
    { type: "rect", x: 200, y: 0, w: 120, h: 120 },
  ], 40);
  report(
    "Getrennte Teile ergeben zwei Komponenten",
    apart.parts.length === 2,
    `${apart.parts.length} Komponente(n)`,
  );

  const touching = analyzeGeometry([
    { type: "rect", x: 0, y: 0, w: 120, h: 120 },
    { type: "rect", x: 120, y: 0, w: 120, h: 120 },
  ], 40);
  report(
    "Beruehrende Teile verschmelzen ohne Verbinden-Knopf zu einer Komponente",
    touching.parts.length === 1,
    `${touching.parts.length} Komponente(n)`,
  );

  const single = solveFor([{ type: "rect", x: 0, y: 0, w: 120, h: 120 }], 40, 1).solved;
  const merged = solveFor([
    { type: "rect", x: 0, y: 0, w: 120, h: 120 },
    { type: "rect", x: 120, y: 0, w: 120, h: 120 },
  ], 40, 1).solved;
  const drop = Math.sqrt(merged.lambdas[0] / single.lambdas[0]);
  report(
    "Verbundener Koerper klingt tiefer als das Einzelteil",
    drop < 0.95,
    `Grundton faellt auf das ${drop.toFixed(3)}-fache`,
  );

  // Ein sichtbarer Spalt darf nicht koppeln. Das ist die Eigenschaft, auf
  // die es bei der Bedienung ankommt: was getrennt aussieht, klingt getrennt.
  const gapped = analyzeGeometry([
    { type: "rect", x: 0, y: 0, w: 120, h: 120 },
    { type: "rect", x: 132, y: 0, w: 120, h: 120 },
  ], 40);
  report(
    "Spalt von 12 px koppelt nicht",
    gapped.parts.length === 2,
    `${gapped.parts.length} Komponente(n)`,
  );

  // Bekannte Toleranz: eine reine Eckberuehrung uebertraegt auf einer echten
  // Membran nichts, das Raster erzeugt dort aber eine Bruecke von einer
  // Zellbreite. Der Test haelt das Verhalten fest, statt eine Genauigkeit zu
  // behaupten, die die Aufloesung nicht hergibt.
  const corner = analyzeGeometry([
    { type: "rect", x: 0, y: 0, w: 120, h: 120 },
    { type: "rect", x: 120, y: 120, w: 120, h: 120 },
  ], 40);
  report(
    "Eckberuehrung koppelt auf Rasterebene (dokumentierte Toleranz)",
    corner.parts.length === 1,
    `${corner.parts.length} Komponente(n); Bruecke ist eine Zelle breit`,
  );
}

// --------------------------------------------------------- Eigenvektoren
// Die Grundmode darf keinen Knoten im Inneren haben (Satz von Courant):
// phi_1 hat einheitliches Vorzeichen. Das prueft die Eigenvektoren, nicht
// nur die Eigenwerte.
{
  const { part, solved } = solveFor([{ type: "ellipse", x: 0, y: 0, w: 200, h: 140 }], 36, 4);
  let positive = 0;
  let negative = 0;
  for (let i = 0; i < part.count; i += 1) {
    const value = solved.shapes[i];
    if (value > 1e-6) positive += 1;
    if (value < -1e-6) negative += 1;
  }
  const uniform = Math.min(positive, negative) / part.count;
  report(
    "Grundmode ist knotenfrei (Satz von Courant)",
    uniform < 0.01,
    `${(uniform * 100).toFixed(2)} % der Knoten mit abweichendem Vorzeichen`,
  );

  // Die zweite Mode muss genau eine Knotenlinie haben, also beide Vorzeichen.
  let secondPositive = 0;
  let secondNegative = 0;
  for (let i = 0; i < part.count; i += 1) {
    const value = solved.shapes[part.count + i];
    if (value > 1e-6) secondPositive += 1;
    if (value < -1e-6) secondNegative += 1;
  }
  const balance = Math.min(secondPositive, secondNegative) / part.count;
  report(
    "Zweite Mode hat eine Knotenlinie",
    balance > 0.2,
    `${(balance * 100).toFixed(1)} % auf der kleineren Seite der Knotenlinie`,
  );
}

console.log(`\n${checks - failures}/${checks} Pruefungen bestanden.`);
process.exit(failures ? 1 : 0);
