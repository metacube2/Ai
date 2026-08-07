// Kopfloser Test des Spielkerns von wwwroot/js/pausegame.js.
//
// Es gibt in dieser Umgebung keine Browser-Automatisierung, das Spielgefuehl kann
// also niemand ausser Ingo pruefen. Was sich OHNE Browser pruefen laesst, ist der
// rechnende Teil: Gelaendeerzeugung, Einschlag, Bodenhoehe und Ballistik. Genau der
// entscheidet, ob das Spiel ueberhaupt funktioniert - Licht und Kamera sind Optik.
//
// Aufruf:  node Tools/PauseGame.Probe/probe.mjs

import { __testHooks as H } from "../../wwwroot/js/pausegame.js";

const { MASK_W, MASK_H, MAX_CLIMB } = H.constants;

let failures = 0;
function check(name, ok, detail) {
  console.log(`${ok ? "PASS" : "FAIL"}  ${name}  ->  ${detail}`);
  if (!ok) failures++;
}

// Ersatz fuer den Masken-Canvas: paintMask braucht nur diese drei Faehigkeiten.
function fakeContext() {
  return {
    createImageData: (w, h) => ({ width: w, height: h, data: new Uint8ClampedArray(w * h * 4) }),
    putImageData: () => {},
  };
}

function freshState(seed) {
  const terrain = H.createTerrain(seed);
  const state = {
    terrain,
    maskCtx: fakeContext(),
    maskTexture: { needsUpdate: false },
    worms: [],
    wind: 0,
    waterLevel: H.constants.WATER_LEVEL_START,
  };
  H.setState(state);
  return state;
}

// ---------------------------------------------------------- Gelaende

freshState(1234);

let minTop = Infinity;
let maxTop = -Infinity;
let maxStep = 0;
let previous = H.groundHeightAt(0);
for (let x = 0; x < MASK_W; x++) {
  const top = H.groundHeightAt(x);
  minTop = Math.min(minTop, top);
  maxTop = Math.max(maxTop, top);
  maxStep = Math.max(maxStep, Math.abs(top - previous));
  previous = top;
}
check("Gelaende hat ueberall Boden", minTop > 0, `niedrigste Stelle ${minTop} px`);
check("Gelaende bleibt unter der Decke", maxTop < MASK_H - 40, `hoechste Stelle ${maxTop} px`);
check("Gelaende ist begehbar (keine Stufe ueber MAX_CLIMB)", maxStep <= MAX_CLIMB,
  `groesste Stufe ${maxStep} px, erlaubt ${MAX_CLIMB}`);
check("Gelaende liegt ueber dem Wasserstand", minTop > H.constants.WATER_LEVEL_START,
  `${minTop} > ${H.constants.WATER_LEVEL_START}`);

// Ueber mehrere Startwerte, damit nicht ein guenstiger Zufall das Ergebnis traegt.
let worstStep = 0;
let lowest = Infinity;
for (let seed = 1; seed <= 40; seed++) {
  freshState(seed * 7919);
  let prev = H.groundHeightAt(0);
  for (let x = 1; x < MASK_W; x++) {
    const top = H.groundHeightAt(x);
    worstStep = Math.max(worstStep, Math.abs(top - prev));
    lowest = Math.min(lowest, top);
    prev = top;
  }
}
check("40 Zufallskarten alle begehbar", worstStep <= MAX_CLIMB, `groesste Stufe ${worstStep} px`);
check("40 Zufallskarten alle ueber Wasser", lowest > H.constants.WATER_LEVEL_START, `tiefste Stelle ${lowest} px`);

// ---------------------------------------------------------- Einschlag

const state = freshState(4242);
const holeX = 600;
const surface = H.groundHeightAt(holeX);
const solidBefore = H.isSolid(holeX, surface - 20);
H.carve(holeX, surface - 20, 40);
const solidAfter = H.isSolid(holeX, surface - 20);
const edgeUntouched = H.isSolid(holeX + 120, surface - 20);

check("Vor dem Einschlag ist dort Boden", solidBefore, "solide");
check("Einschlag entfernt Boden an der richtigen Stelle", !solidAfter, "Loch vorhanden");
check("Einschlag laesst entfernten Boden in Ruhe", edgeUntouched, "120 px daneben noch solide");
check("Bodenhoehe faellt nach dem Einschlag", H.groundHeightAt(holeX) < surface,
  `${H.groundHeightAt(holeX)} < ${surface}`);
check("Textur wurde zum Auffrischen vorgemerkt", state.maskTexture.needsUpdate === true, "needsUpdate");

// ---------------------------------------------------------- Ballistik

freshState(777);
const shooter = { x: 300, y: H.groundHeightAt(300) + 8, alive: true, team: 0 };
const hit = H.simulateShot(shooter, 45, 360, 1, 1);
check("Wurf landet irgendwo im Gelaende", hit !== null && hit.x > shooter.x,
  hit ? `bei x=${hit.x.toFixed(0)}` : "kein Treffer");

// Der eigentliche Test des Rechnergegners: findet die Rastersuche eine Loesung, die
// nahe genug am Ziel landet, um Schaden zu machen? Sprengdrohnenradius ist 44 px.
const st = H.getState();
const target = { x: 780, y: H.groundHeightAt(780) + 8, alive: true, team: 1, marked: false };
st.worms = [shooter, target];
st.config = { difficulty: "hard" };

// planAiShot streut absichtlich zufaellig. Fuer einen Regressionstest muss das
// reproduzierbar sein, sonst schwankt das Ergebnis von Lauf zu Lauf.
const realRandom = Math.random;
let rngState = 20260807;
Math.random = () => {
  rngState = (rngState * 1664525 + 1013904223) >>> 0;
  return rngState / 4294967296;
};

// Ueber viele Zufallslagen messen, nicht an einer festen Geometrie. Gemessen und
// nachgerechnet: steht das Ziel genau auf einer Kuppe, ist die Flugbahn zweigipflig -
// haarscharf zu flach heisst Hang, haarscharf zu hoch heisst weit dahinter. Dort
// verfehlt JEDER Schuetze regelmaessig, auch ein perfekter. Ein Test auf einzelne
// Treffer misst deshalb die Landschaft, nicht den Gegner; darum Median ueber viele
// Lagen und ein Vergleich gegen einen Schuetzen, der einfach stur 45 Grad nimmt.
function measureAi(difficulty, samples) {
  st.config = { difficulty };
  const distances = [];
  let naiveTotal = 0;
  for (let i = 0; i < samples; i++) {
    const terrain = H.createTerrain(1000 + i * 131);
    H.getState().terrain = terrain;
    // In Wurfweite: die groesste Reichweite liegt bei rund 560 px (Schub hoechstens
    // 540). Weiter entfernte Ziele sind kein Zielproblem, sondern ein Laufproblem -
    // die stehen im Anmarsch-Test darunter.
    const sx = 150 + (i * 37) % 400;
    const tx = sx + 200 + (i * 53) % 260;
    const a = { x: sx, y: H.groundHeightAt(sx) + 8, alive: true, team: 0 };
    const b = { x: tx, y: H.groundHeightAt(tx) + 8, alive: true, team: 1, marked: false };
    H.getState().worms = [a, b];
    H.getState().wind = ((i % 13) - 6) * 15;

    const plan = H.planAiShot(a);
    const land = plan ? H.simulateShot(a, plan.angle, plan.power, plan.dir, 1) : null;
    distances.push(land ? Math.hypot(land.x - b.x, land.y - b.y) : 9999);

    const naive = H.simulateShot(a, 45, 350, 1, 1);
    naiveTotal += naive ? Math.hypot(naive.x - b.x, naive.y - b.y) : 9999;
  }
  distances.sort((p, q) => p - q);
  return {
    median: distances[Math.floor(distances.length / 2)],
    hits: distances.filter(d => d < 66).length,
    naiveAverage: naiveTotal / samples,
    samples,
  };
}

const hard = measureAi("hard", 40);
const easy = measureAi("easy", 40);

check("Rechnergegner zielt wirklich (besser als stur 45 Grad)",
  hard.median < hard.naiveAverage / 3,
  `Median ${hard.median.toFixed(0)} px gegen ${hard.naiveAverage.toFixed(0)} px bei sturem Schuss`);
check("Hoechste Stufe trifft ueberwiegend im Wirkradius",
  hard.hits >= hard.samples * 0.7,
  `${hard.hits} von ${hard.samples} unter 66 px, Median ${hard.median.toFixed(0)} px`);
check("Leichter Grad trifft spuerbar schlechter", easy.hits < hard.hits,
  `leicht ${easy.hits} gegen schwer ${hard.hits} von ${hard.samples}`);
check("Rechnergegner haelt den Schub im Bedienbereich (120-540)",
  (() => {
    for (let i = 0; i < 40; i++) {
      H.getState().wind = ((i % 13) - 6) * 15;
      const plan = H.planAiShot(H.getState().worms[0]);
      if (plan && (plan.power < 120 || plan.power > 540)) return false;
    }
    return true;
  })(),
  "kein Schuss staerker als ein Mensch schiessen kann");

// Anmarsch: ausser Wurfweite muss der Gegner laufen statt ins Leere zu schiessen.
{
  const terrain = H.createTerrain(31337);
  H.getState().terrain = terrain;
  H.getState().wind = 0;
  H.getState().config = { difficulty: "hard" };

  const shooterFar = { x: 200, y: H.groundHeightAt(200) + 8, alive: true, team: 0 };
  const targetFar = { x: 1000, y: H.groundHeightAt(1000) + 8, alive: true, team: 1, marked: false };
  H.getState().worms = [shooterFar, targetFar];
  const far = H.aiApproach(shooterFar);
  check("Ausser Reichweite laeuft der Gegner auf das Ziel zu", far.dir === 1,
    `Richtung ${far.dir}, Abstand ${(targetFar.x - shooterFar.x)} px`);

  // Spiegelbildlich: Schuetze rechts, Ziel weit links - ebenfalls ausser Reichweite.
  const shooterRight = { x: 1000, y: H.groundHeightAt(1000) + 6, alive: true, team: 0 };
  const targetLeft = { x: 100, y: H.groundHeightAt(100) + 6, alive: true, team: 1, marked: false };
  H.getState().worms = [shooterRight, targetLeft];
  const leftward = H.aiApproach(shooterRight);
  check("Er laeuft auch nach links, wenn das Ziel links steht", leftward.dir === -1,
    `Richtung ${leftward.dir}, Abstand ${(shooterRight.x - targetLeft.x)} px`);

  const near = { x: 480, y: H.groundHeightAt(480) + 8, alive: true, team: 1, marked: false };
  H.getState().worms = [shooterFar, near];
  const close = H.aiApproach(shooterFar);
  check("In Reichweite laeuft er nicht mehr, sondern zielt",
    close.dir === 0 && close.plan !== null,
    `Richtung ${close.dir}, bester Abstand ${close.plan ? close.plan.dist.toFixed(0) : "-"} px`);

  // Anmarsch genau so getaktet wie in update(): erst walk, dann die Schwerkraft.
  // walk hebt den Wurm nur an, herunter zieht ihn ausschliesslich stepWorm - ohne
  // den zweiten Teil bliebe er beim ersten Gefaelle in der Luft stehen.
  const walker = { x: 200, y: H.groundHeightAt(200) + 6, alive: true, team: 0, facing: 1, vx: 0, vy: 0, health: 100 };
  H.getState().worms = [walker, targetFar];
  H.getState().teams = [{ name: "A" }, { name: "B" }];
  const startX = walker.x;
  for (let step = 0; step < 420; step++) {          // 7 s bei 60 Hz
    H.walk(walker, 1, 1 / 60);
    H.stepWorm(walker, 1 / 60);
  }
  check("Sieben Sekunden Anmarsch verkuerzen den Abstand deutlich",
    walker.x - startX > 260,
    `${(walker.x - startX).toFixed(0)} px in 7 s gelaufen`);
  check("Der Wurm bleibt beim Laufen auf dem Boden", H.onGround(walker),
    `y=${walker.y.toFixed(0)}, Boden bei ${H.groundHeightAt(walker.x)}`);
  check("Der Wurm ueberlebt den Anmarsch", walker.alive && walker.health === 100,
    `Leben ${walker.health}`);
}

Math.random = realRandom;

console.log("");
console.log(failures === 0 ? "ALLE PRUEFUNGEN GRUEN" : `${failures} PRUEFUNG(EN) ROT`);
process.exit(failures === 0 ? 0 : 1);
