// Drohnen-Duell - Pausenspiel im BiDashboard.
//
// Laeuft vollstaendig im Browser. Der Blazor-Server bekommt nichts davon mit ausser
// dem einmaligen start()-Aufruf: kein Zustand, kein Speichern, keine Uebertragung
// pro Bild. Grund steht in docs/PAUSENSPIEL_DROHNEN_KONZEPT_2026-08-07.md Abschnitt 4.
//
// Gespielt wird auf einer Ebene, dargestellt wird in 3D ueber das bereits global
// geladene three.js (wwwroot/js/vendor/three.min.js, r160).
//
// Aller Text steht hier und nicht in der Razor - siehe Konzept Abschnitt 10.

import { parseMod, ModPlayer } from "./modplayer.js";

// ---------------------------------------------------------------- Texte

const TEXTS = {
  de: {
    title: "Drohnen-Duell",
    subtitle: "Rundenkampf mit Drohnen. Wind beachten.",
    modeLabel: "Gegner",
    modePc: "Gegen den Rechner",
    modeHuman: "Gegen einen Kollegen",
    name1: "Dein Name",
    name2: "Name des Gegners",
    difficulty: "Schwierigkeit",
    teamSize: "Wuermer je Mannschaft",
    start: "Partie starten",
    player1: "Spieler 1",
    player2: "Spieler 2",
    turnOf: "Am Zug",
    wind: "Wind",
    time: "Zeit",
    drone: "Drohne",
    ammo: "Vorrat",
    unlimited: "frei",
    power: "Schub",
    angle: "Winkel",
    wins: "gewinnt",
    draw: "Unentschieden",
    again: "Nochmal",
    back: "Zurueck zur Auswahl",
    marked: "Ziel markiert: naechster Treffer verstaerkt",
    controls: "Steuerung",
    controlsBody:
      "Pfeil links/rechts laufen  ·  Pfeil hoch/runter zielen  ·  Leertaste halten und loslassen: Schub  ·  Enter springen  ·  1 2 3 Drohne waehlen",
    dropHint: "Leertaste: Drohne starten, dann Leertaste zum Ausklinken",
    scoutHint: "Pfeiltasten steuern die Drohne, Leertaste markiert das Ziel",
    hitWater: "im Wasser",
    suddenDeath: "Sudden Death - das Wasser steigt",
    thinking: "zielt...",
    reduceEffects: "Effekte reduzieren",
    sound: "Geraeusche",
    music: "Musik",
    musicPick: "MOD-Datei waehlen (.mod)",
    musicNone: "keine Datei geladen",
    musicLoaded: "Musik geladen",
    musicBad: "Datei ist kein lesbares MOD",
    drones: {
      blast: { name: "Sprengdrohne", hint: "Wurf mit Winkel und Schub" },
      drop: { name: "Abwurfdrohne", hint: "Ueberflug, drei Bomben" },
      scout: { name: "Spaehdrohne", hint: "Freier Flug, markiert ein Ziel" },
    },
    levels: { easy: "Drohnenpilot", normal: "Schwarmfuehrer", hard: "Luftwacht" },
  },
  en: {
    title: "Drone Duel",
    subtitle: "Turn-based drone combat. Mind the wind.",
    modeLabel: "Opponent",
    modePc: "Against the computer",
    modeHuman: "Against a colleague",
    name1: "Your name",
    name2: "Opponent name",
    difficulty: "Difficulty",
    teamSize: "Worms per team",
    start: "Start match",
    player1: "Player 1",
    player2: "Player 2",
    turnOf: "Turn",
    wind: "Wind",
    time: "Time",
    drone: "Drone",
    ammo: "Ammo",
    unlimited: "free",
    power: "Power",
    angle: "Angle",
    wins: "wins",
    draw: "Draw",
    again: "Play again",
    back: "Back to setup",
    marked: "Target marked: next hit amplified",
    controls: "Controls",
    controlsBody:
      "Arrow left/right walk  ·  Arrow up/down aim  ·  Hold and release Space: power  ·  Enter jump  ·  1 2 3 pick drone",
    dropHint: "Space: launch drone, then Space to release",
    scoutHint: "Arrow keys steer the drone, Space marks the target",
    hitWater: "in the water",
    suddenDeath: "Sudden death - the water is rising",
    thinking: "aiming...",
    reduceEffects: "Reduce effects",
    sound: "Sound effects",
    music: "Music",
    musicPick: "Pick a MOD file (.mod)",
    musicNone: "no file loaded",
    musicLoaded: "Music loaded",
    musicBad: "File is not a readable MOD",
    drones: {
      blast: { name: "Blast drone", hint: "Throw with angle and power" },
      drop: { name: "Bomber drone", hint: "Fly-over, three bombs" },
      scout: { name: "Scout drone", hint: "Free flight, marks a target" },
    },
    levels: { easy: "Drone pilot", normal: "Swarm leader", hard: "Air watch" },
  },
};

// ---------------------------------------------------------------- Konstanten

const MASK_W = 1200;
const MASK_H = 600;
const UNITS = 6;              // Maskenpixel je Weltenheit
const GRAVITY = -520;         // Pixel je Sekunde im Quadrat
const WATER_LEVEL_START = 40;
const WALK_SPEED = 62;
const JUMP_SPEED = 210;
const TURN_SECONDS = 25;
const RETREAT_SECONDS = 3;
const SUDDEN_DEATH_ROUND = 15;
const MAX_CLIMB = 14;         // Stufenhoehe, die ein Wurm noch hochlaeuft

const DRONE_TYPES = [
  { id: "blast", key: "1", ammo: -1, radius: 44, damage: 42, mass: 1.0, control: "throw" },
  { id: "drop", key: "2", ammo: 3, radius: 30, damage: 22, mass: 1.6, control: "pass" },
  { id: "scout", key: "3", ammo: 2, radius: 0, damage: 0, mass: 0.5, control: "free" },
];

const TEAM_COLORS = [0x2f6fed, 0xe8563f];

// ---------------------------------------------------------------- Modulzustand

let G = null;   // gesamter Spielzustand, oder null wenn nichts laeuft

// ---------------------------------------------------------------- Ton
//
// Alles selbst erzeugt: keine Audiodateien im Repository, kein Nachladen. Der
// AudioContext entsteht erst beim ersten Klick - vorher lehnt ihn jeder Browser ab.
// Ton ist standardmaessig AUS, das hier ist ein Buero.

const MUSIC_CHUNK = 0.25;     // Sekunden je gerenderter Block
const MUSIC_LEAD = 0.9;       // so weit im Voraus, dass 60-Hz-Ruckler nichts ausmachen

function ensureAudio() {
  if (!G || G.audio) return G ? G.audio : null;
  const Ctx = window.AudioContext || window.webkitAudioContext;
  if (!Ctx) return null;
  const ctx = new Ctx();
  const master = ctx.createGain();
  master.gain.value = 0.9;
  master.connect(ctx.destination);
  const music = ctx.createGain();
  music.gain.value = 0.55;
  music.connect(master);
  const sfx = ctx.createGain();
  sfx.gain.value = 0.8;
  sfx.connect(master);
  G.audio = { ctx, master, music, sfx, player: null, nextTime: 0, song: null };
  return G.audio;
}

function noiseBuffer(ctx, seconds) {
  const frames = Math.floor(ctx.sampleRate * seconds);
  const buffer = ctx.createBuffer(1, frames, ctx.sampleRate);
  const data = buffer.getChannelData(0);
  for (let i = 0; i < frames; i++) data[i] = Math.random() * 2 - 1;
  return buffer;
}

function sfx(kind, strength = 1) {
  if (!G || !G.soundOn || !G.audio) return;
  const { ctx, sfx: out } = G.audio;
  const now = ctx.currentTime;
  const gain = ctx.createGain();
  gain.connect(out);

  if (kind === "explosion") {
    // Rauschstoss durch ein absinkendes Tiefpassfilter, dazu ein kurzer Bass.
    const src = ctx.createBufferSource();
    src.buffer = noiseBuffer(ctx, 0.6);
    const filter = ctx.createBiquadFilter();
    filter.type = "lowpass";
    filter.frequency.setValueAtTime(1800 * strength, now);
    filter.frequency.exponentialRampToValueAtTime(90, now + 0.5);
    gain.gain.setValueAtTime(0.9 * strength, now);
    gain.gain.exponentialRampToValueAtTime(0.001, now + 0.6);
    src.connect(filter).connect(gain);
    src.start(now);
    src.stop(now + 0.62);

    const thump = ctx.createOscillator();
    const thumpGain = ctx.createGain();
    thump.frequency.setValueAtTime(120, now);
    thump.frequency.exponentialRampToValueAtTime(38, now + 0.25);
    thumpGain.gain.setValueAtTime(0.7 * strength, now);
    thumpGain.gain.exponentialRampToValueAtTime(0.001, now + 0.3);
    thump.connect(thumpGain).connect(out);
    thump.start(now);
    thump.stop(now + 0.32);
    return;
  }

  if (kind === "launch") {
    const osc = ctx.createOscillator();
    osc.type = "sawtooth";
    osc.frequency.setValueAtTime(160, now);
    osc.frequency.exponentialRampToValueAtTime(680, now + 0.28);
    gain.gain.setValueAtTime(0.001, now);
    gain.gain.linearRampToValueAtTime(0.22, now + 0.05);
    gain.gain.exponentialRampToValueAtTime(0.001, now + 0.3);
    osc.connect(gain);
    osc.start(now);
    osc.stop(now + 0.32);
    return;
  }

  if (kind === "rotor") {
    // Kurzes Surren beim Start der Drohne, kein Dauerton - der nervt im Buero.
    const osc = ctx.createOscillator();
    osc.type = "square";
    osc.frequency.setValueAtTime(58, now);
    const lfo = ctx.createOscillator();
    const lfoGain = ctx.createGain();
    lfo.frequency.value = 32;
    lfoGain.gain.value = 14;
    lfo.connect(lfoGain).connect(osc.frequency);
    gain.gain.setValueAtTime(0.14, now);
    gain.gain.exponentialRampToValueAtTime(0.001, now + 0.5);
    osc.connect(gain);
    osc.start(now); lfo.start(now);
    osc.stop(now + 0.52); lfo.stop(now + 0.52);
    return;
  }

  if (kind === "jump") {
    const osc = ctx.createOscillator();
    osc.type = "triangle";
    osc.frequency.setValueAtTime(300, now);
    osc.frequency.exponentialRampToValueAtTime(680, now + 0.12);
    gain.gain.setValueAtTime(0.18, now);
    gain.gain.exponentialRampToValueAtTime(0.001, now + 0.16);
    osc.connect(gain);
    osc.start(now);
    osc.stop(now + 0.18);
    return;
  }

  if (kind === "splash") {
    const src = ctx.createBufferSource();
    src.buffer = noiseBuffer(ctx, 0.5);
    const filter = ctx.createBiquadFilter();
    filter.type = "bandpass";
    filter.frequency.setValueAtTime(900, now);
    filter.frequency.exponentialRampToValueAtTime(220, now + 0.4);
    filter.Q.value = 1.4;
    gain.gain.setValueAtTime(0.5, now);
    gain.gain.exponentialRampToValueAtTime(0.001, now + 0.45);
    src.connect(filter).connect(gain);
    src.start(now);
    src.stop(now + 0.48);
    return;
  }

  if (kind === "hurt") {
    const osc = ctx.createOscillator();
    osc.type = "square";
    osc.frequency.setValueAtTime(420, now);
    osc.frequency.exponentialRampToValueAtTime(140, now + 0.18);
    gain.gain.setValueAtTime(0.16, now);
    gain.gain.exponentialRampToValueAtTime(0.001, now + 0.2);
    osc.connect(gain);
    osc.start(now);
    osc.stop(now + 0.22);
  }
}

function loadMusicFile(file) {
  const audio = ensureAudio();
  if (!audio || !file) return;
  const reader = new FileReader();
  reader.onload = () => {
    try {
      const song = parseMod(reader.result);
      audio.song = song;
      audio.player = new ModPlayer(song, audio.ctx.sampleRate);
      audio.nextTime = 0;
      G.musicName = song.title || file.name;
      if (G.ui && G.ui.musicLabel) G.ui.musicLabel.textContent = G.musicName;
      flash(`${G.t.musicLoaded}: ${G.musicName}`);
    } catch (err) {
      G.musicName = "";
      if (G.ui && G.ui.musicLabel) G.ui.musicLabel.textContent = G.t.musicBad;
      flash(G.t.musicBad);
    }
  };
  reader.onerror = () => flash(G.t.musicBad);
  reader.readAsArrayBuffer(file);
}

// Blockweise im Voraus rendern und einreihen. Kein ScriptProcessor (veraltet, laeuft
// im Haupt-Thread und knackst, sobald WebGL ruckelt) und kein AudioWorklet (eigene
// Moduldatei, in der der Mischer ein zweites Mal liegen muesste).
function pumpMusic() {
  if (!G || !G.musicOn || !G.audio || !G.audio.player) return;
  const { ctx, music, player } = G.audio;
  if (ctx.state === "suspended") ctx.resume();
  if (G.audio.nextTime < ctx.currentTime) G.audio.nextTime = ctx.currentTime + 0.05;

  let guard = 0;
  while (G.audio.nextTime < ctx.currentTime + MUSIC_LEAD && guard++ < 12) {
    const frames = Math.floor(ctx.sampleRate * MUSIC_CHUNK);
    const buffer = ctx.createBuffer(2, frames, ctx.sampleRate);
    player.render(buffer.getChannelData(0), buffer.getChannelData(1), frames);
    const src = ctx.createBufferSource();
    src.buffer = buffer;
    src.connect(music);
    src.start(G.audio.nextTime);
    G.audio.nextTime += MUSIC_CHUNK;
  }
}

function setSound(on) {
  G.soundOn = on;
  if (on) ensureAudio();
  if (G.audio && G.audio.ctx.state === "suspended") G.audio.ctx.resume();
}

function setMusic(on) {
  G.musicOn = on;
  if (on) {
    ensureAudio();
    if (G.audio) {
      G.audio.nextTime = 0;
      if (G.audio.ctx.state === "suspended") G.audio.ctx.resume();
    }
  } else if (G.audio) {
    // Bereits eingereihte Bloecke laufen noch bis zu MUSIC_LEAD weiter; ueber die
    // Lautstaerke ist es sofort still.
    G.audio.music.gain.setTargetAtTime(0, G.audio.ctx.currentTime, 0.05);
  }
  if (on && G.audio) {
    G.audio.music.gain.setTargetAtTime(0.55, G.audio.ctx.currentTime, 0.05);
  }
}

// ---------------------------------------------------------------- Gelaende

function createTerrain(seed) {
  const solid = new Uint8Array(MASK_W * MASK_H);
  const rand = makeRandom(seed);

  // Mehrere ueberlagerte Sinuswellen ergeben eine Huegellandschaft, die immer
  // begehbar ist - Rauschen allein liefert zu oft unerreichbare Spitzen.
  const waves = [];
  for (let i = 0; i < 5; i++) {
    waves.push({
      amp: 18 + rand() * 46,
      len: 140 + rand() * 620,
      phase: rand() * Math.PI * 2,
    });
  }

  const heights = new Float32Array(MASK_W);
  for (let x = 0; x < MASK_W; x++) {
    let h = 210;
    for (const w of waves) {
      h += Math.sin((x / w.len) * Math.PI * 2 + w.phase) * w.amp;
    }
    // Raender leicht anheben, damit niemand direkt am Bildrand ins Wasser rutscht.
    const edge = Math.min(x, MASK_W - 1 - x) / 160;
    h += Math.max(0, 1 - edge) * 55;
    heights[x] = Math.max(70, Math.min(MASK_H - 90, h));
  }

  for (let x = 0; x < MASK_W; x++) {
    const top = Math.floor(heights[x]);
    for (let y = 0; y < top; y++) {
      solid[y * MASK_W + x] = 1;
    }
  }
  return { solid, heights };
}

function makeRandom(seed) {
  let s = seed >>> 0;
  return function () {
    s = (s * 1664525 + 1013904223) >>> 0;
    return s / 4294967296;
  };
}

function isSolid(x, y) {
  const xi = x | 0;
  const yi = y | 0;
  if (xi < 0 || xi >= MASK_W || yi < 0) return false;
  if (yi >= MASK_H) return false;
  return G.terrain.solid[yi * MASK_W + xi] === 1;
}

// cx/cy in WELTKOORDINATEN (y nach oben), wie isSolid und groundHeightAt. Die
// Umrechnung auf Canvas-Zeilen passiert nur fuer das Nachzeichnen der Textur -
// vorher standen hier beide Konventionen gemischt und das Loch entstand gespiegelt.
function carve(cx, cy, radius) {
  const r2 = radius * radius;
  const x0 = Math.max(0, Math.floor(cx - radius));
  const x1 = Math.min(MASK_W - 1, Math.ceil(cx + radius));
  const y0 = Math.max(0, Math.floor(cy - radius));
  const y1 = Math.min(MASK_H - 1, Math.ceil(cy + radius));
  for (let y = y0; y <= y1; y++) {
    for (let x = x0; x <= x1; x++) {
      const dx = x - cx;
      const dy = y - cy;
      if (dx * dx + dy * dy <= r2) {
        G.terrain.solid[y * MASK_W + x] = 0;
      }
    }
  }
  paintMask(x0, MASK_H - 1 - y1, x1 - x0 + 1, y1 - y0 + 1);
}

// Die Maske ist zugleich die Alpha-Textur des Gelaendes. Neu gezeichnet wird nur der
// betroffene Ausschnitt, nicht das ganze Bild - sonst kostet jede Explosion ein
// volles 1200x600-Update.
function paintMask(x0, y0, w, h) {
  const ctx = G.maskCtx;
  const image = ctx.createImageData(w, h);
  const data = image.data;
  for (let y = 0; y < h; y++) {
    for (let x = 0; x < w; x++) {
      const worldY = MASK_H - 1 - (y0 + y);
      const on = G.terrain.solid[worldY * MASK_W + (x0 + x)] === 1;
      const i = (y * w + x) * 4;
      data[i] = on ? 255 : 0;
      data[i + 1] = on ? 255 : 0;
      data[i + 2] = on ? 255 : 0;
      data[i + 3] = 255;
    }
  }
  ctx.putImageData(image, x0, y0);
  G.maskTexture.needsUpdate = true;
}

function groundHeightAt(x) {
  const xi = Math.max(0, Math.min(MASK_W - 1, x | 0));
  for (let y = MASK_H - 1; y >= 0; y--) {
    if (G.terrain.solid[y * MASK_W + xi] === 1) return y + 1;
  }
  return 0;
}

// ---------------------------------------------------------------- Welt <-> Szene

function wx(x) { return x / UNITS - MASK_W / (2 * UNITS); }
function wy(y) { return y / UNITS - MASK_H / (2 * UNITS); }

// ---------------------------------------------------------------- Szene

function gradientTexture(THREE, stops) {
  const c = document.createElement("canvas");
  c.width = 4;
  c.height = 256;
  const ctx = c.getContext("2d");
  const grad = ctx.createLinearGradient(0, 0, 0, 256);
  for (const [pos, color] of stops) grad.addColorStop(pos, color);
  ctx.fillStyle = grad;
  ctx.fillRect(0, 0, 4, 256);
  return new THREE.CanvasTexture(c);
}

function rockTexture(THREE) {
  const c = document.createElement("canvas");
  c.width = 64;
  c.height = MASK_H;
  const ctx = c.getContext("2d");
  const grad = ctx.createLinearGradient(0, 0, 0, MASK_H);
  grad.addColorStop(0, "#6fae4a");
  grad.addColorStop(0.06, "#5d9440");
  grad.addColorStop(0.2, "#8a6a44");
  grad.addColorStop(1, "#5a4630");
  ctx.fillStyle = grad;
  ctx.fillRect(0, 0, 64, MASK_H);
  // Etwas Koernung, damit die Flaeche im Licht nicht wie Plastik wirkt.
  for (let i = 0; i < 5000; i++) {
    ctx.fillStyle = `rgba(0,0,0,${Math.random() * 0.10})`;
    ctx.fillRect(Math.random() * 64, Math.random() * MASK_H, 2, 2);
  }
  const tex = new THREE.CanvasTexture(c);
  tex.wrapS = THREE.RepeatWrapping;
  tex.repeat.set(18, 1);
  return tex;
}

function labelSprite(THREE, text, color) {
  // Gleiche Technik wie wwwroot/js/finance3d.js: Text auf ein Canvas, daraus ein
  // Sprite. Sprites drehen sich immer zur Kamera, die Namen bleiben also lesbar.
  const c = document.createElement("canvas");
  c.width = 256;
  c.height = 64;
  const ctx = c.getContext("2d");
  ctx.font = "bold 30px Segoe UI, sans-serif";
  ctx.textAlign = "center";
  ctx.textBaseline = "middle";
  ctx.lineWidth = 6;
  ctx.strokeStyle = "rgba(255,255,255,0.9)";
  ctx.strokeText(text, 128, 32);
  ctx.fillStyle = color;
  ctx.fillText(text, 128, 32);
  const sprite = new THREE.Sprite(new THREE.SpriteMaterial({
    map: new THREE.CanvasTexture(c),
    transparent: true,
    depthTest: false,
  }));
  sprite.scale.set(11, 2.75, 1);
  sprite.renderOrder = 10;
  return sprite;
}

function buildScene(host) {
  const THREE = window.THREE;
  const canvas = document.createElement("canvas");
  canvas.style.width = "100%";
  canvas.style.height = "100%";
  canvas.style.display = "block";
  host.appendChild(canvas);

  const renderer = new THREE.WebGLRenderer({ canvas, antialias: true });
  renderer.setPixelRatio(Math.min(window.devicePixelRatio || 1, 2));
  renderer.shadowMap.enabled = true;
  renderer.shadowMap.type = THREE.PCFSoftShadowMap;

  const scene = new THREE.Scene();
  scene.background = gradientTexture(THREE, [
    [0, "#0d2340"], [0.45, "#3f7fbf"], [1, "#bfe0f2"],
  ]);

  const camera = new THREE.PerspectiveCamera(42, 1, 0.5, 900);

  const sun = new THREE.DirectionalLight(0xfff3e0, 1.15);
  sun.position.set(-60, 90, 110);
  sun.castShadow = true;
  sun.shadow.mapSize.set(1024, 1024);
  sun.shadow.camera.left = -110;
  sun.shadow.camera.right = 110;
  sun.shadow.camera.top = 70;
  sun.shadow.camera.bottom = -70;
  sun.shadow.camera.far = 400;
  scene.add(sun);
  scene.add(new THREE.HemisphereLight(0xbfd8ff, 0x4a3b2a, 0.75));

  const maskCanvas = document.createElement("canvas");
  maskCanvas.width = MASK_W;
  maskCanvas.height = MASK_H;
  const maskCtx = maskCanvas.getContext("2d", { willReadFrequently: true });
  const maskTexture = new THREE.CanvasTexture(maskCanvas);

  const worldW = MASK_W / UNITS;
  const worldH = MASK_H / UNITS;
  const geo = new THREE.PlaneGeometry(worldW, worldH);

  // Vordere Platte traegt die Gesteinstextur, die hintere sitzt ein Stueck dahinter
  // und ist dunkler. Durch die perspektivische Kamera bekommen Kraterraender dadurch
  // sichtbare Tiefe, ohne dass echte Geometrie erzeugt werden muss.
  const front = new THREE.Mesh(geo, new THREE.MeshStandardMaterial({
    map: rockTexture(THREE),
    alphaMap: maskTexture,
    alphaTest: 0.5,
    roughness: 0.95,
    metalness: 0.0,
  }));
  front.receiveShadow = true;
  scene.add(front);

  const back = new THREE.Mesh(geo, new THREE.MeshStandardMaterial({
    color: 0x3a2c1e,
    alphaMap: maskTexture,
    alphaTest: 0.5,
    roughness: 1.0,
  }));
  back.position.z = -2.4;
  scene.add(back);

  const water = new THREE.Mesh(
    new THREE.PlaneGeometry(worldW * 1.6, worldH),
    new THREE.MeshStandardMaterial({
      color: 0x1f6ea8, transparent: true, opacity: 0.78, roughness: 0.25, metalness: 0.1,
    }));
  water.position.z = 3;
  scene.add(water);

  return {
    THREE, canvas, renderer, scene, camera, sun, water,
    maskCanvas, maskCtx, maskTexture, front, back, worldW, worldH,
  };
}

function makeWormMesh(THREE, color) {
  const group = new THREE.Group();
  const body = new THREE.Mesh(
    new THREE.CapsuleGeometry(0.85, 1.1, 6, 14),
    new THREE.MeshStandardMaterial({ color, roughness: 0.55 }));
  body.castShadow = true;
  group.add(body);
  const eyeGeo = new THREE.SphereGeometry(0.24, 10, 10);
  const eyeMat = new THREE.MeshStandardMaterial({ color: 0xffffff });
  const pupilGeo = new THREE.SphereGeometry(0.11, 8, 8);
  const pupilMat = new THREE.MeshStandardMaterial({ color: 0x101010 });
  for (const dx of [-0.32, 0.32]) {
    const eye = new THREE.Mesh(eyeGeo, eyeMat);
    eye.position.set(dx, 0.55, 0.72);
    group.add(eye);
    const pupil = new THREE.Mesh(pupilGeo, pupilMat);
    pupil.position.set(dx, 0.55, 0.92);
    group.add(pupil);
  }
  return group;
}

function makeDroneMesh(THREE, color) {
  const group = new THREE.Group();
  const body = new THREE.Mesh(
    new THREE.BoxGeometry(1.5, 0.5, 1.1),
    new THREE.MeshStandardMaterial({ color, roughness: 0.4, metalness: 0.35 }));
  body.castShadow = true;
  group.add(body);
  const armMat = new THREE.MeshStandardMaterial({ color: 0x2a2a2a, roughness: 0.6 });
  const rotors = [];
  for (const [ax, az] of [[-1, -0.8], [1, -0.8], [-1, 0.8], [1, 0.8]]) {
    const arm = new THREE.Mesh(new THREE.BoxGeometry(0.16, 0.16, 0.16), armMat);
    arm.position.set(ax * 0.85, 0.15, az * 0.5);
    group.add(arm);
    const rotor = new THREE.Mesh(
      new THREE.CylinderGeometry(0.55, 0.55, 0.05, 12),
      new THREE.MeshStandardMaterial({ color: 0xdddddd, transparent: true, opacity: 0.55 }));
    rotor.position.set(ax * 0.85, 0.32, az * 0.5);
    group.add(rotor);
    rotors.push(rotor);
  }
  group.userData.rotors = rotors;
  return group;
}

// ---------------------------------------------------------------- Aufbau der Partie

function startMatch(config) {
  const THREE = G.THREE;
  for (const w of G.worms) {
    G.scene.remove(w.mesh);
    G.scene.remove(w.label);
  }
  G.worms = [];
  G.round = 1;
  G.waterLevel = WATER_LEVEL_START;
  G.config = config;
  G.terrain = createTerrain((Date.now() & 0xffff) ^ 0x5bd1);
  paintMask(0, 0, MASK_W, MASK_H);

  G.teams = [
    { name: config.name1 || G.t.player1, color: TEAM_COLORS[0], human: true, ammo: freshAmmo() },
    {
      name: config.name2 || (config.mode === "pc" ? G.t.levels[config.difficulty] : G.t.player2),
      color: TEAM_COLORS[1],
      human: config.mode !== "pc",
      ammo: freshAmmo(),
    },
  ];

  // Mannschaften abwechselnd ueber die Karte verteilen, mit Abstand zueinander.
  const slots = [];
  const count = config.teamSize * 2;
  for (let i = 0; i < count; i++) {
    slots.push(120 + ((MASK_W - 240) * (i + 0.5)) / count);
  }
  for (let i = 0; i < count; i++) {
    const team = i % 2;
    const x = slots[i] + (Math.random() - 0.5) * 40;
    const worm = {
      team,
      index: Math.floor(i / 2) + 1,
      x,
      y: groundHeightAt(x) + 8,
      vx: 0,
      vy: 0,
      health: 100,
      facing: team === 0 ? 1 : -1,
      alive: true,
      marked: false,
    };
    worm.mesh = makeWormMesh(THREE, G.teams[team].color);
    worm.label = labelSprite(THREE, `${G.teams[team].name} ${worm.index}`, "#1b2733");
    G.scene.add(worm.mesh);
    G.scene.add(worm.label);
    G.worms.push(worm);
  }

  G.activeTeam = 0;
  G.phase = "aim";
  G.turnTime = TURN_SECONDS;
  G.wind = (Math.random() * 2 - 1) * 90;
  G.angle = 45;
  G.power = 0;
  G.charging = false;
  G.droneIndex = 0;
  G.projectiles = [];
  G.particles = [];
  G.aiTimer = 0;
  G.aiShot = null;
  G.aiWalkLeft = undefined;
  G.aiApproach = null;
  G.aiReplan = 0;
  G.message = "";
  G.messageTime = 0;
  G.activeWorm = firstAliveOfTeam(0);
  setScreen("game");
}

function freshAmmo() {
  const ammo = {};
  for (const d of DRONE_TYPES) ammo[d.id] = d.ammo;
  return ammo;
}

function firstAliveOfTeam(team) {
  return G.worms.find(w => w.team === team && w.alive) || null;
}

function aliveCount(team) {
  return G.worms.filter(w => w.team === team && w.alive).length;
}

// ---------------------------------------------------------------- Physik

function stepWorm(worm, dt) {
  if (!worm.alive) return;
  const wasAir = !onGround(worm);
  worm.vy += GRAVITY * dt;
  let ny = worm.y + worm.vy * dt;
  if (worm.vy <= 0) {
    const ground = groundHeightAt(worm.x);
    if (ny <= ground + 6) {
      if (wasAir && worm.vy < -260) {
        applyDamage(worm, Math.min(35, Math.round((-worm.vy - 260) / 12)));
      }
      ny = ground + 6;
      worm.vy = 0;
    }
  } else if (isSolid(worm.x, ny + 10)) {
    worm.vy = 0;
  }
  worm.y = ny;
  if (worm.y < G.waterLevel) {
    worm.alive = false;
    sfx("splash");
    flash(`${G.teams[worm.team].name} ${worm.index} ${G.t.hitWater}`);
  }
}

function onGround(worm) {
  return worm.y <= groundHeightAt(worm.x) + 7;
}

function walk(worm, dir, dt) {
  if (!onGround(worm)) return;
  worm.facing = dir;
  const nx = worm.x + dir * WALK_SPEED * dt;
  if (nx < 12 || nx > MASK_W - 12) return;
  const target = groundHeightAt(nx);
  if (target - worm.y > MAX_CLIMB) return;   // zu steil
  worm.x = nx;
  // Dem Gelaende folgen, auch BERGAB. Vorher stand hier Math.max(...), der Wurm
  // wurde also nur angehoben und blieb bergab in der Luft haengen, bis ihn die
  // Schwerkraft nachzog - gemessen: dauerhaft rund 2 px ueber Grund, damit ausserhalb
  // der Toleranz von onGround. Folge waere gewesen: Laufen setzt mitten im Zug aus,
  // und ein Gefaelle haette Fallschaden ausgeloest.
  worm.y = target + 6;
  worm.vy = 0;
}

function launch(worm, type, angleDeg, power, dir) {
  sfx("launch");
  const rad = (angleDeg * Math.PI) / 180;
  const speed = power;
  return spawnProjectile(type, {
    x: worm.x + dir * 10,
    y: worm.y + 10,
    vx: Math.cos(rad) * speed * dir,
    vy: Math.sin(rad) * speed,
    team: worm.team,
  });
}

function spawnProjectile(type, init) {
  const p = {
    type,
    x: init.x, y: init.y, vx: init.vx, vy: init.vy,
    team: init.team,
    life: 12,
    mesh: makeDroneMesh(G.THREE, G.teams[init.team].color),
    steer: init.steer || null,
  };
  G.scene.add(p.mesh);
  G.projectiles.push(p);
  sfx("rotor");
  return p;
}

function stepProjectile(p, dt) {
  if (p.steer === "free") {
    // Spaehdrohne: kein Fall, dafuer traege Steuerung.
    p.vx += G.input.right ? 220 * dt : 0;
    p.vx -= G.input.left ? 220 * dt : 0;
    p.vy += G.input.up ? 220 * dt : 0;
    p.vy -= G.input.down ? 220 * dt : 0;
    p.vx *= 0.985;
    p.vy *= 0.985;
  } else if (p.steer === "pass") {
    // Abwurfdrohne haelt ihre Hoehe und faehrt durch.
    p.vy += (p.targetY - p.y) * 1.6 * dt;
    p.vy *= 0.9;
  } else {
    p.vy += GRAVITY * dt;
  }
  const massFactor = DRONE_TYPES.find(d => d.id === p.type).mass;
  p.vx += (G.wind / massFactor) * dt;

  p.x += p.vx * dt;
  p.y += p.vy * dt;
  p.life -= dt;

  if (p.x < -40 || p.x > MASK_W + 40 || p.y < -60 || p.life <= 0) return "gone";
  if (p.steer === "free") return "fly";
  if (p.y < G.waterLevel) return "water";
  if (isSolid(p.x, p.y)) return "hit";
  for (const w of G.worms) {
    if (!w.alive) continue;
    const dx = w.x - p.x;
    const dy = w.y - p.y;
    if (dx * dx + dy * dy < 90) return "hit";
  }
  return "fly";
}

function explode(p) {
  const def = DRONE_TYPES.find(d => d.id === p.type);
  if (def.radius > 0) carve(p.x, p.y, def.radius);
  for (const w of G.worms) {
    if (!w.alive) continue;
    const dist = Math.hypot(w.x - p.x, w.y - p.y);
    if (dist > def.radius * 1.5) continue;
    const falloff = Math.max(0, 1 - dist / (def.radius * 1.5));
    let dmg = def.damage * falloff;
    if (w.marked) {
      dmg *= 1.5;
      w.marked = false;
      w.label.material.color.set(0xffffff);
    }
    applyDamage(w, Math.round(dmg));
    const push = 210 * falloff;
    w.vx += ((w.x - p.x) / (dist || 1)) * push;
    w.vy += ((w.y - p.y) / (dist || 1)) * push + 40 * falloff;
  }
  spawnBlast(p.x, p.y, def.radius);
  sfx("explosion", Math.min(1.2, def.radius / 44));
}

function applyDamage(worm, amount) {
  if (amount <= 0 || !worm.alive) return;
  sfx("hurt");
  worm.health -= amount;
  if (worm.health <= 0) {
    worm.health = 0;
    worm.alive = false;
  }
}

function spawnBlast(x, y, radius) {
  const THREE = G.THREE;
  const mesh = new THREE.Mesh(
    new THREE.SphereGeometry(Math.max(4, radius) / UNITS, 14, 14),
    new THREE.MeshBasicMaterial({ color: 0xffb347, transparent: true, opacity: 0.9 }));
  mesh.position.set(wx(x), wy(y), 1.5);
  G.scene.add(mesh);
  G.particles.push({ mesh, life: 0.45, max: 0.45 });
}

// ---------------------------------------------------------------- Zuege

function currentDrone() { return DRONE_TYPES[G.droneIndex]; }

function ammoLeft(teamIndex, droneId) {
  return G.teams[teamIndex].ammo[droneId];
}

function fireCurrent() {
  const worm = G.activeWorm;
  if (!worm || G.phase !== "aim") return;
  const def = currentDrone();
  const left = ammoLeft(G.activeTeam, def.id);
  if (left === 0) return;
  if (left > 0) G.teams[G.activeTeam].ammo[def.id] = left - 1;

  if (def.control === "throw") {
    launch(worm, def.id, G.angle, 120 + G.power * 420, worm.facing);
  } else if (def.control === "pass") {
    const p = spawnProjectile(def.id, {
      x: worm.facing > 0 ? 10 : MASK_W - 10,
      y: worm.y + 90 + G.angle * 1.4,
      vx: worm.facing * 210,
      vy: 0,
      team: worm.team,
    });
    p.steer = "pass";
    p.targetY = p.y;
    p.bombs = 3;
    G.passDrone = p;
  } else {
    const p = spawnProjectile(def.id, {
      x: worm.x + worm.facing * 12,
      y: worm.y + 16,
      vx: worm.facing * 120,
      vy: 60,
      team: worm.team,
    });
    p.steer = "free";
    p.life = 8;
    G.scoutDrone = p;
  }
  G.phase = "flying";
  G.power = 0;
  G.charging = false;
}

function releaseBombs() {
  const p = G.passDrone;
  if (!p || p.bombs <= 0) return;
  p.bombs--;
  spawnProjectile("drop", { x: p.x, y: p.y - 4, vx: p.vx * 0.5, vy: -20, team: p.team });
  if (p.bombs <= 0) p.life = Math.min(p.life, 1.2);
}

function markTarget() {
  const p = G.scoutDrone;
  if (!p) return;
  let best = null;
  let bestDist = 1e9;
  for (const w of G.worms) {
    if (!w.alive || w.team === p.team) continue;
    const d = Math.hypot(w.x - p.x, w.y - p.y);
    if (d < bestDist) { bestDist = d; best = w; }
  }
  if (best && bestDist < 90) {
    best.marked = true;
    best.label.material.color.set(0xffd54f);
    flash(G.t.marked);
  }
  p.life = 0;
}

function endTurn() {
  G.phase = "retreat";
  G.retreat = RETREAT_SECONDS;
}

function nextTurn() {
  const other = G.activeTeam === 0 ? 1 : 0;
  if (aliveCount(0) === 0 || aliveCount(1) === 0) {
    G.phase = "over";
    setScreen("result");
    return;
  }
  G.activeTeam = other;
  if (other === 0) G.round++;
  if (G.round >= SUDDEN_DEATH_ROUND) {
    G.waterLevel += 6;
    flash(G.t.suddenDeath);
  }
  const team = G.worms.filter(w => w.team === other && w.alive);
  const previous = G.lastWormIndex ? G.lastWormIndex[other] || 0 : 0;
  const pick = team[previous % team.length];
  G.lastWormIndex = G.lastWormIndex || [0, 0];
  G.lastWormIndex[other] = previous + 1;
  G.activeWorm = pick;
  G.phase = "aim";
  G.turnTime = TURN_SECONDS;
  G.wind = (Math.random() * 2 - 1) * 90;
  G.angle = 45;
  G.power = 0;
  G.charging = false;
  G.droneIndex = 0;
  G.passDrone = null;
  G.scoutDrone = null;
  G.aiTimer = G.teams[other].human ? 0 : 1.4;
  G.aiShot = null;
  G.aiWalkLeft = undefined;
  G.aiApproach = null;
  G.aiReplan = 0;
}

function flash(text) {
  G.message = text;
  G.messageTime = 2.6;
}

// ---------------------------------------------------------------- Rechnergegner

// Grobe Rastersuche mit derselben Flugbahnrechnung, die auch das Spiel benutzt.
// Kein Lernen, keine Wegfindung - das reicht fuer eine Pause.
function planAiShot(worm) {
  const targets = G.worms.filter(w => w.alive && w.team !== worm.team);
  if (targets.length === 0) return null;
  const target = targets[Math.floor(Math.random() * targets.length)];
  const dir = target.x >= worm.x ? 1 : -1;
  const level = G.config.difficulty;
  const windFactor = level === "easy" ? 0.5 : 1.0;

  const spread = level === "easy" ? 9 : level === "normal" ? 4 : 1.5;

  // Bewertung einer Flugbahn: nicht nur der Abstand zum Ziel, sondern der
  // SCHLECHTESTE Abstand, wenn man den Winkel um die eigene Streuung verwackelt.
  // Ohne das waehlt die Suche gern eine Bahn, die knapp ueber einen Huegel geht -
  // die Streuung laesst sie dann dagegenklatschen, und der Gegner wirkt nicht
  // schwer, sondern dumm (gemessen: drei von zwoelf Schuessen ueber 200 px daneben).
  function robustDistance(angle, power) {
    let worst = 0;
    for (const wobble of [0, spread * 0.8, -spread * 0.8]) {
      const land = simulateShot(worm, angle + wobble, power, dir, windFactor);
      if (!land) return Infinity;
      worst = Math.max(worst, Math.hypot(land.x - target.x, land.y - target.y));
    }
    return worst;
  }

  let best = null;
  for (let angle = 12; angle <= 80; angle += 3) {
    for (let power = 180; power <= 540; power += 18) {
      const land = simulateShot(worm, angle, power, dir, windFactor);
      if (!land) continue;
      const dist = Math.hypot(land.x - target.x, land.y - target.y);
      if (!best || dist < best.dist) best = { angle, power, dist };
    }
  }
  if (!best) return null;
  best = { angle: best.angle, power: best.power, dist: robustDistance(best.angle, best.power) };

  // Verfeinerung um den groben Treffer herum. Das Raster von 3 Grad und 18 Schub
  // laesst auf weite Entfernung noch gut 60 px stehen - mehr als der Wirkradius
  // der Sprengdrohne. Ohne diesen zweiten Durchgang verfehlt selbst die hoechste
  // Stufe bei starkem Wind regelmaessig.
  const coarse = best;
  for (let angle = coarse.angle - 3; angle <= coarse.angle + 3; angle += 0.5) {
    for (let power = coarse.power - 18; power <= coarse.power + 18; power += 4) {
      // Derselbe Schubbereich, den ein Mensch ueber die Leertaste erreicht
      // (120 + power * 420, power hoechstens 1). Ohne die Grenze schoesse der
      // Rechner staerker, als ueberhaupt bedienbar ist.
      if (power < 120 || power > 540) continue;
      const dist = robustDistance(angle, power);
      if (dist < best.dist) best = { angle, power, dist };
    }
  }
  return {
    angle: best.angle + (Math.random() * 2 - 1) * spread,
    power: Math.min(540, Math.max(120, best.power * (1 + (Math.random() * 2 - 1) * spread * 0.012))),
    dir,
    dist: best.dist,
  };
}

function simulateShot(worm, angleDeg, power, dir, windFactor) {
  const rad = (angleDeg * Math.PI) / 180;
  let x = worm.x + dir * 10;
  let y = worm.y + 10;
  let vx = Math.cos(rad) * power * dir;
  let vy = Math.sin(rad) * power;
  const dt = 1 / 40;
  for (let i = 0; i < 400; i++) {
    vy += GRAVITY * dt;
    vx += G.wind * windFactor * dt;
    x += vx * dt;
    y += vy * dt;
    if (x < 0 || x > MASK_W || y < G.waterLevel) return null;
    if (isSolid(x, y)) return { x, y };
    for (const w of G.worms) {
      if (!w.alive) continue;
      if (Math.hypot(w.x - x, w.y - y) < 9) return { x, y };
    }
  }
  return null;
}

// Gemessen: die groesste Wurfweite liegt bei rund 560 px, die Karte ist 1200 px breit.
// Ein Gegner, der nur zielt und nie laeuft, schiesst aus unerreichbarer Entfernung ins
// Leere - in der Messung ueber 40 Zufallslagen war genau das die Haelfte aller Schuesse.
// Deshalb naehert er sich erst an, solange kein brauchbarer Schuss existiert.
const AI_GOOD_SHOT = 70;      // px Abstand, ab dem sich Laufen nicht mehr lohnt
const AI_WALK_BUDGET = 7;     // Sekunden je Zug, der Rest bleibt fuers Zielen

function aiApproach(worm) {
  const plan = planAiShot(worm);
  if (plan && plan.dist <= AI_GOOD_SHOT) return { dir: 0, plan };
  let nearest = null;
  let bestDist = Infinity;
  for (const w of G.worms) {
    if (!w.alive || w.team === worm.team) continue;
    const d = Math.abs(w.x - worm.x);
    if (d < bestDist) { bestDist = d; nearest = w; }
  }
  if (!nearest) return { dir: 0, plan };
  return { dir: nearest.x > worm.x ? 1 : -1, plan };
}

function stepAi(dt) {
  if (G.teams[G.activeTeam].human || G.phase !== "aim") return;
  G.aiTimer -= dt;

  if (G.aiWalkLeft === undefined) G.aiWalkLeft = AI_WALK_BUDGET;
  if (G.aiShot === null && G.aiWalkLeft > 0) {
    G.aiReplan = (G.aiReplan || 0) - dt;
    if (G.aiReplan <= 0) {
      G.aiReplan = 0.4;
      G.aiApproach = aiApproach(G.activeWorm);
    }
    if (G.aiApproach && G.aiApproach.dir !== 0) {
      walk(G.activeWorm, G.aiApproach.dir, dt);
      G.aiWalkLeft -= dt;
      G.aiTimer = Math.max(G.aiTimer, 1.2);   // nach dem Laufen noch sichtbar zielen
      return;
    }
    G.aiWalkLeft = 0;
  }

  if (G.aiShot === null && G.aiTimer < 0.9) {
    G.aiShot = planAiShot(G.activeWorm) || { angle: 45, power: 320, dir: 1 };
  }
  if (G.aiShot && G.aiTimer > 0) {
    // Sichtbar zielen, bevor geschossen wird - eine sofortige perfekte Antwort
    // fuehlt sich deutlich unangenehmer an als eine kurze Bedenkzeit.
    G.angle += (G.aiShot.angle - G.angle) * Math.min(1, dt * 4);
    G.activeWorm.facing = G.aiShot.dir;
    G.power = Math.min(1, G.power + dt * 0.6);
  }
  if (G.aiShot && G.aiTimer <= 0) {
    G.droneIndex = 0;
    G.angle = G.aiShot.angle;
    launch(G.activeWorm, "blast", G.aiShot.angle, G.aiShot.power, G.aiShot.dir);
    G.phase = "flying";
    G.power = 0;
    G.aiShot = null;
  }
}

// ---------------------------------------------------------------- Oberflaeche

function el(tag, style, text) {
  const node = document.createElement(tag);
  if (style) node.setAttribute("style", style);
  if (text !== undefined) node.textContent = text;
  return node;
}

const PANEL = "position:absolute;inset:0;display:flex;align-items:center;justify-content:center;"
  + "background:rgba(9,20,34,0.72);backdrop-filter:blur(3px);z-index:5;";
const CARD = "background:#fff;border-radius:12px;padding:24px 28px;min-width:340px;max-width:460px;"
  + "font-family:Segoe UI,sans-serif;box-shadow:0 12px 40px rgba(0,0,0,0.35);";
const FIELD = "width:100%;padding:8px 10px;margin:4px 0 14px 0;border:1px solid #c8d2dc;"
  + "border-radius:6px;font-size:14px;box-sizing:border-box;";
const LABEL = "font-size:12px;color:#5a6b7b;text-transform:uppercase;letter-spacing:0.4px;";
const BUTTON = "width:100%;padding:11px;border:0;border-radius:6px;background:#2f6fed;color:#fff;"
  + "font-size:15px;font-weight:600;cursor:pointer;";

function buildUi(host) {
  const t = G.t;

  // --- Startbildschirm
  const setup = el("div", PANEL);
  const card = el("div", CARD);
  card.appendChild(el("div", "font-size:22px;font-weight:700;color:#16222e;", t.title));
  card.appendChild(el("div", "font-size:13px;color:#6b7b8b;margin:2px 0 18px 0;", t.subtitle));

  card.appendChild(el("div", LABEL, t.modeLabel));
  const mode = el("select", FIELD);
  const optPc = el("option", null, t.modePc); optPc.value = "pc";
  const optHuman = el("option", null, t.modeHuman); optHuman.value = "human";
  mode.appendChild(optPc);
  mode.appendChild(optHuman);

  card.appendChild(mode);

  card.appendChild(el("div", LABEL, t.name1));
  const name1 = el("input", FIELD);
  name1.maxLength = 20;
  name1.value = localStorage.getItem("pausegame.name1") || "";
  name1.placeholder = t.player1;
  card.appendChild(name1);

  const name2Label = el("div", LABEL, t.name2);
  const name2 = el("input", FIELD);
  name2.maxLength = 20;
  name2.value = localStorage.getItem("pausegame.name2") || "";
  name2.placeholder = t.player2;
  card.appendChild(name2Label);
  card.appendChild(name2);

  const diffLabel = el("div", LABEL, t.difficulty);
  const diff = el("select", FIELD);
  for (const key of ["easy", "normal", "hard"]) {
    const o = el("option", null, t.levels[key]);
    o.value = key;
    diff.appendChild(o);
  }
  card.appendChild(diffLabel);
  card.appendChild(diff);

  card.appendChild(el("div", LABEL, t.teamSize));
  const size = el("select", FIELD);
  for (const n of [3, 4]) {
    const o = el("option", null, String(n));
    o.value = String(n);
    size.appendChild(o);
  }
  card.appendChild(size);

  function syncMode() {
    const pc = mode.value === "pc";
    name2Label.style.display = pc ? "none" : "";
    name2.style.display = pc ? "none" : "";
    diffLabel.style.display = pc ? "" : "none";
    diff.style.display = pc ? "" : "none";
  }
  mode.addEventListener("change", syncMode);
  syncMode();

  // --- Ton. Standardmaessig aus: das hier ist ein Buero.
  const audioRow = el("div", "display:flex;gap:16px;align-items:center;margin:2px 0 12px 0;");
  const soundBox = el("input", "margin:0 6px 0 0;");
  soundBox.type = "checkbox";
  soundBox.checked = localStorage.getItem("pausegame.sound") === "1";
  const soundLbl = el("label", "font-size:13px;color:#2c3e50;cursor:pointer;display:flex;align-items:center;");
  soundLbl.appendChild(soundBox);
  soundLbl.appendChild(document.createTextNode(t.sound));
  const musicBox = el("input", "margin:0 6px 0 0;");
  musicBox.type = "checkbox";
  musicBox.checked = localStorage.getItem("pausegame.music") === "1";
  const musicLbl = el("label", "font-size:13px;color:#2c3e50;cursor:pointer;display:flex;align-items:center;");
  musicLbl.appendChild(musicBox);
  musicLbl.appendChild(document.createTextNode(t.music));
  audioRow.appendChild(soundLbl);
  audioRow.appendChild(musicLbl);
  card.appendChild(audioRow);

  const musicPick = el("input", "font-size:12px;width:100%;margin-bottom:4px;");
  musicPick.type = "file";
  musicPick.accept = ".mod,.MOD";
  musicPick.title = t.musicPick;
  card.appendChild(musicPick);
  const musicLabel = el("div", "font-size:11px;color:#8a97a4;margin-bottom:14px;", t.musicNone);
  card.appendChild(musicLabel);

  const startBtn = el("button", BUTTON, t.start);
  startBtn.addEventListener("click", () => {
    localStorage.setItem("pausegame.name1", name1.value.trim());
    localStorage.setItem("pausegame.name2", name2.value.trim());
    localStorage.setItem("pausegame.sound", soundBox.checked ? "1" : "0");
    localStorage.setItem("pausegame.music", musicBox.checked ? "1" : "0");
    // Der Klick ist die Nutzeraktion, ohne die kein Browser Ton zulaesst.
    setSound(soundBox.checked);
    setMusic(musicBox.checked);
    startMatch({
      mode: mode.value,
      name1: name1.value.trim(),
      name2: mode.value === "pc" ? "" : name2.value.trim(),
      difficulty: diff.value,
      teamSize: Number(size.value),
    });
  });
  card.appendChild(startBtn);
  card.appendChild(el("div", "font-size:11px;color:#8a97a4;margin-top:14px;line-height:1.5;", t.controlsBody));
  setup.appendChild(card);
  host.appendChild(setup);

  // --- Kopfzeile im Spiel
  const hud = el("div",
    "position:absolute;top:0;left:0;right:0;padding:10px 14px;display:flex;gap:18px;align-items:center;"
    + "font-family:Segoe UI,sans-serif;font-size:13px;color:#fff;z-index:4;pointer-events:none;"
    + "background:linear-gradient(rgba(9,20,34,0.55),rgba(9,20,34,0));");
  const hudTurn = el("div", "font-weight:700;font-size:15px;");
  const hudWind = el("div", "");
  const hudTime = el("div", "");
  const hudDrone = el("div", "margin-left:auto;text-align:right;");
  hud.appendChild(hudTurn);
  hud.appendChild(hudWind);
  hud.appendChild(hudTime);
  hud.appendChild(hudDrone);
  host.appendChild(hud);

  const powerBar = el("div",
    "position:absolute;left:14px;bottom:16px;width:220px;height:12px;border-radius:6px;"
    + "background:rgba(255,255,255,0.25);z-index:4;overflow:hidden;");
  const powerFill = el("div", "height:100%;width:0%;background:linear-gradient(90deg,#7ed957,#ffd400,#ff5a36);");
  powerBar.appendChild(powerFill);
  host.appendChild(powerBar);

  const message = el("div",
    "position:absolute;left:50%;top:64px;transform:translateX(-50%);padding:7px 16px;border-radius:16px;"
    + "background:rgba(9,20,34,0.8);color:#fff;font-family:Segoe UI,sans-serif;font-size:13px;z-index:4;"
    + "opacity:0;transition:opacity 0.2s;pointer-events:none;");
  host.appendChild(message);

  // --- Abspann
  const result = el("div", PANEL + "display:none;");
  const resultCard = el("div", CARD + "text-align:center;");
  const resultText = el("div", "font-size:22px;font-weight:700;color:#16222e;margin-bottom:18px;");
  const againBtn = el("button", BUTTON, t.again);
  const backBtn = el("button", BUTTON + "background:#e8edf3;color:#2c3e50;margin-top:8px;", t.back);
  resultCard.appendChild(resultText);
  resultCard.appendChild(againBtn);
  resultCard.appendChild(backBtn);
  result.appendChild(resultCard);
  host.appendChild(result);

  againBtn.addEventListener("click", () => startMatch(G.config));
  backBtn.addEventListener("click", () => setScreen("setup"));

  musicPick.addEventListener("change", () => {
    if (musicPick.files && musicPick.files[0]) loadMusicFile(musicPick.files[0]);
  });

  G.ui = { setup, result, resultText, hud, hudTurn, hudWind, hudTime, hudDrone, powerBar, powerFill, message, musicLabel };
}

function setScreen(name) {
  const u = G.ui;
  u.setup.style.display = name === "setup" ? "flex" : "none";
  u.result.style.display = name === "result" ? "flex" : "none";
  const playing = name === "game";
  u.hud.style.display = playing ? "flex" : "none";
  u.powerBar.style.display = playing ? "block" : "none";
  if (name === "result") {
    const a = aliveCount(0);
    const b = aliveCount(1);
    const text = a === b
      ? G.t.draw
      : `${G.teams[a > b ? 0 : 1].name} ${G.t.wins}`;
    u.resultText.textContent = text;
    rememberScore(a > b ? 0 : 1);
  }
}

// Bestenliste bleibt im Browser. Kein Serveraufruf, kein Eintrag in der Datenbank -
// serverseitig ist damit nicht nachvollziehbar, wer wann gegen wen gespielt hat.
function rememberScore(winnerTeam) {
  try {
    const raw = localStorage.getItem("pausegame.scores");
    const scores = raw ? JSON.parse(raw) : {};
    const name = G.teams[winnerTeam].name;
    scores[name] = (scores[name] || 0) + 1;
    localStorage.setItem("pausegame.scores", JSON.stringify(scores));
  } catch (e) {
    // Bestenliste ist Beiwerk; ein voller oder gesperrter localStorage darf das
    // Spielende nicht kippen.
  }
}

function updateHud() {
  const u = G.ui;
  const t = G.t;
  const team = G.teams[G.activeTeam];
  const worm = G.activeWorm;
  u.hudTurn.textContent = `${t.turnOf}: ${team.name}${worm ? " " + worm.index : ""}`
    + (team.human ? "" : ` (${t.thinking})`);
  const dir = G.wind >= 0 ? "▶" : "◀";
  u.hudWind.textContent = `${t.wind} ${dir} ${Math.abs(G.wind).toFixed(0)}`;
  u.hudTime.textContent = `${t.time} ${Math.max(0, Math.ceil(G.turnTime))}`;
  const def = currentDrone();
  const left = ammoLeft(G.activeTeam, def.id);
  u.hudDrone.textContent = `${t.drone}: ${t.drones[def.id].name}`
    + `  (${t.ammo} ${left < 0 ? t.unlimited : left})   ${t.angle} ${G.angle.toFixed(0)}°`;
  u.powerFill.style.width = `${Math.round(G.power * 100)}%`;
  u.message.style.opacity = G.messageTime > 0 ? "1" : "0";
  if (G.messageTime > 0) u.message.textContent = G.message;
}

// ---------------------------------------------------------------- Eingabe

function pickDrone(index) {
  if (G.phase !== "aim" || !G.teams[G.activeTeam].human) return;
  if (index < 0 || index >= DRONE_TYPES.length) return;
  if (ammoLeft(G.activeTeam, DRONE_TYPES[index].id) === 0) return;
  G.droneIndex = index;
  flash(G.t.drones[DRONE_TYPES[index].id].hint);
}

function onKeyDown(e) {
  if (!G || !G.running) return;
  const human = G.teams && G.teams[G.activeTeam] && G.teams[G.activeTeam].human;
  const keys = ["ArrowLeft", "ArrowRight", "ArrowUp", "ArrowDown", " ", "Enter"];
  if (keys.includes(e.key)) e.preventDefault();
  if (e.key === "ArrowLeft") G.input.left = true;
  if (e.key === "ArrowRight") G.input.right = true;
  if (e.key === "ArrowUp") G.input.up = true;
  if (e.key === "ArrowDown") G.input.down = true;
  if (e.key === "1") pickDrone(0);
  if (e.key === "2") pickDrone(1);
  if (e.key === "3") pickDrone(2);
  if (!human) return;
  if (e.key === "Enter" && G.phase === "aim" && G.activeWorm && onGround(G.activeWorm)) {
    G.activeWorm.vy = JUMP_SPEED;
    G.activeWorm.vx = G.activeWorm.facing * 42;
    sfx("jump");
  }
  if (e.key === " " && !e.repeat) {
    const def = currentDrone();
    if (G.phase === "aim" && def.control === "throw") {
      G.charging = true;
      G.power = 0;
    } else if (G.phase === "aim") {
      fireCurrent();
    } else if (G.phase === "flying" && G.passDrone) {
      releaseBombs();
    } else if (G.phase === "flying" && G.scoutDrone) {
      markTarget();
    }
  }
}

function onKeyUp(e) {
  if (!G || !G.running) return;
  if (e.key === "ArrowLeft") G.input.left = false;
  if (e.key === "ArrowRight") G.input.right = false;
  if (e.key === "ArrowUp") G.input.up = false;
  if (e.key === "ArrowDown") G.input.down = false;
  if (e.key === " " && G.charging) {
    G.charging = false;
    fireCurrent();
  }
}

// ---------------------------------------------------------------- Schleife

function update(dt) {
  if (G.messageTime > 0) G.messageTime -= dt;
  const human = G.teams[G.activeTeam].human;

  if (G.phase === "aim") {
    G.turnTime -= dt;
    if (human && G.activeWorm) {
      if (G.input.left) walk(G.activeWorm, -1, dt);
      if (G.input.right) walk(G.activeWorm, 1, dt);
      if (G.input.up) G.angle = Math.min(89, G.angle + 55 * dt);
      if (G.input.down) G.angle = Math.max(-20, G.angle - 55 * dt);
      if (G.charging) G.power = Math.min(1, G.power + dt * 0.85);
    }
    if (G.turnTime <= 0) endTurn();
  }

  stepAi(dt);

  for (const w of G.worms) {
    if (!w.alive) continue;
    w.x += w.vx * dt;
    w.vx *= 0.86;
    if (w.x < 12) { w.x = 12; w.vx = 0; }
    if (w.x > MASK_W - 12) { w.x = MASK_W - 12; w.vx = 0; }
    stepWorm(w, dt);
  }

  for (let i = G.projectiles.length - 1; i >= 0; i--) {
    const p = G.projectiles[i];
    const state = stepProjectile(p, dt);
    if (state === "hit") {
      explode(p);
      removeProjectile(i);
    } else if (state === "gone" || state === "water") {
      if (state === "water") spawnBlast(p.x, G.waterLevel, 12);
      removeProjectile(i);
    }
  }

  if (G.phase === "flying" && G.projectiles.length === 0) endTurn();

  if (G.phase === "retreat") {
    G.retreat -= dt;
    if (G.retreat <= 0) nextTurn();
  }

  for (let i = G.particles.length - 1; i >= 0; i--) {
    const q = G.particles[i];
    q.life -= dt;
    const k = Math.max(0, q.life / q.max);
    q.mesh.scale.setScalar(1 + (1 - k) * 1.8);
    q.mesh.material.opacity = k * 0.9;
    if (q.life <= 0) {
      G.scene.remove(q.mesh);
      q.mesh.geometry.dispose();
      q.mesh.material.dispose();
      G.particles.splice(i, 1);
    }
  }
}

function removeProjectile(index) {
  const p = G.projectiles[index];
  G.scene.remove(p.mesh);
  if (G.passDrone === p) G.passDrone = null;
  if (G.scoutDrone === p) G.scoutDrone = null;
  G.projectiles.splice(index, 1);
}

function syncScene(dt) {
  for (const w of G.worms) {
    if (!w.alive) {
      if (w.mesh.parent) {
        G.scene.remove(w.mesh);
        G.scene.remove(w.label);
      }
      continue;
    }
    w.mesh.position.set(wx(w.x), wy(w.y), 0);
    w.mesh.rotation.y = w.facing > 0 ? 0.25 : -0.25;
    w.label.position.set(wx(w.x), wy(w.y) + 3.1, 2);
    const active = w === G.activeWorm && G.phase === "aim";
    w.mesh.scale.setScalar(active ? 1.06 : 1.0);
  }
  for (const p of G.projectiles) {
    p.mesh.position.set(wx(p.x), wy(p.y), 0.4);
    p.mesh.rotation.z = Math.atan2(p.vy, p.vx) * 0.25;
    for (const rotor of p.mesh.userData.rotors) rotor.rotation.y += dt * 45;
  }
  G.water.position.y = wy(G.waterLevel) - G.worldH / 2;

  // Kamera: beim Zielen senkrecht auf die Spielebene, sonst laesst sich der Winkel
  // nicht abschaetzen. Erst im Flug, wenn niemand mehr eingibt, darf sie schwenken.
  const focus = G.projectiles.length > 0 ? G.projectiles[G.projectiles.length - 1] : G.activeWorm;
  if (focus) {
    const fx = wx(focus.x);
    const fy = wy(focus.y);
    const flying = G.projectiles.length > 0;
    const targetX = clamp(fx, -G.worldW / 2 + 30, G.worldW / 2 - 30);
    const targetY = clamp(fy, -G.worldH / 2 + 18, G.worldH / 2 - 12);
    const desired = flying
      ? { x: targetX + 4, y: targetY + 5, z: 62 }
      : { x: targetX, y: targetY + 2, z: 74 };
    G.camera.position.x += (desired.x - G.camera.position.x) * Math.min(1, dt * 3.4);
    G.camera.position.y += (desired.y - G.camera.position.y) * Math.min(1, dt * 3.4);
    G.camera.position.z += (desired.z - G.camera.position.z) * Math.min(1, dt * 2.2);
    G.camera.lookAt(targetX, targetY, 0);
  }
}

function clamp(v, lo, hi) { return v < lo ? lo : v > hi ? hi : v; }

function resize() {
  const w = G.host.clientWidth || 800;
  const h = G.host.clientHeight || 500;
  G.renderer.setSize(w, h, false);
  G.camera.aspect = w / Math.max(1, h);
  G.camera.updateProjectionMatrix();
}

function frame(now) {
  if (!G || !G.running) return;
  G.raf = requestAnimationFrame(frame);
  if (document.visibilityState === "hidden") { G.last = now; return; }
  const dt = Math.min(0.05, (now - G.last) / 1000 || 0);
  G.last = now;
  pumpMusic();
  if (G.phase && G.phase !== "over" && G.worms.length > 0) {
    update(dt);
    syncScene(dt);
    updateHud();
  }
  G.renderer.render(G.scene, G.camera);
}

// ---------------------------------------------------------------- Ein- und Ausstieg

export function start(host, language) {
  dispose();
  if (!window.THREE) {
    host.appendChild(el("div", "padding:20px;font-family:Segoe UI,sans-serif;color:#b3261e;",
      "three.js nicht geladen"));
    return;
  }
  const scene = buildScene(host);
  G = Object.assign({
    host,
    t: TEXTS[language === "en" ? "en" : "de"],
    worms: [],
    projectiles: [],
    particles: [],
    phase: null,
    input: { left: false, right: false, up: false, down: false },
    running: true,
    audio: null,
    soundOn: false,      // Buero: Ton ist aus, bis jemand ihn einschaltet
    musicOn: false,
    musicName: "",
    last: performance.now(),
    waterLevel: WATER_LEVEL_START,
    terrain: { solid: new Uint8Array(MASK_W * MASK_H), heights: new Float32Array(MASK_W) },
  }, scene);

  buildUi(host);
  setScreen("setup");
  resize();
  G.observer = new ResizeObserver(() => resize());
  G.observer.observe(host);
  window.addEventListener("keydown", onKeyDown);
  window.addEventListener("keyup", onKeyUp);
  G.raf = requestAnimationFrame(frame);
}

// Schmaler Zugang fuer den kopflosen Test (Tools/PauseGame.Probe): Gelaende,
// Einschlag und Ballistik lassen sich damit ohne Browser und ohne WebGL pruefen.
// Nichts davon wird im Spiel benutzt.
export const __testHooks = {
  setState: (state) => { G = state; },
  getState: () => G,
  createTerrain,
  paintMask,
  isSolid,
  carve,
  groundHeightAt,
  simulateShot,
  planAiShot,
  aiApproach,
  walk,
  stepWorm,
  onGround,
  constants: { MASK_W, MASK_H, GRAVITY, MAX_CLIMB, WATER_LEVEL_START, DRONE_TYPES },
};

export function dispose() {
  if (!G) return;
  G.running = false;
  if (G.raf) cancelAnimationFrame(G.raf);
  window.removeEventListener("keydown", onKeyDown);
  window.removeEventListener("keyup", onKeyUp);
  if (G.observer) G.observer.disconnect();
  // Ton zuerst: ein offener AudioContext ueberlebt den Seitenwechsel sonst und
  // spielt weiter, waehrend jemand im Cockpit arbeitet.
  if (G.audio) {
    try {
      G.audio.master.gain.value = 0;
      G.audio.ctx.close();
    } catch (e) {
      // Bereits geschlossen.
    }
    G.audio = null;
  }
  try {
    G.renderer.dispose();
  } catch (e) {
    // Kontext kann bereits verloren sein, wenn der Reiter geschlossen wurde.
  }
  if (G.host) G.host.innerHTML = "";
  G = null;
}
