// FPV-Fernpilot - Pausenspiel im BiDashboard.
//
// Der Wurm bleibt an seiner Fernsteuerung. Geflogen wird die Drohne direkt aus
// einer nahen Bordkamera bis zu einem fiktionalen Zielpunkt. Das Spiel laeuft
// vollstaendig im Browser; Namen, Ergebnisse und Einstellungen verlassen den
// Browser nicht. three.js wird bereits zentral durch App.razor geladen.
//
// Aller Spieltext steht absichtlich hier und nicht in der Razor-Komponente. Der
// Lokalisierungstest des Dashboards scannt Components/**/*.razor und wuerde sonst
// fuer jeden Spieltext sechs weitere Uebersetzungen verlangen.

import { parseMod, ModPlayer } from "./modplayer.js";

// ---------------------------------------------------------------- Texte

const TEXTS = {
  de: {
    title: "FPV-Fernpilot",
    subtitle: "Der Wurm bleibt am Sender. Steuere die Drohne durch alle Kontrollpunkte bis zum Ziel.",
    fiction: "Fiktionales Trainingsgebiet - keine realen Orte oder Einheiten",
    modeLabel: "Vergleich",
    modePc: "Gegen einen Referenzpiloten",
    modeHuman: "Gegen einen Kollegen",
    name1: "Dein Name",
    name2: "Name des Kollegen",
    difficulty: "Flugstrecke",
    start: "Training starten",
    player1: "Pilot 1",
    player2: "Pilot 2",
    levels: { easy: "Ruhiger Wind", normal: "Boeeiges Tal", hard: "Schwaches Signal" },
    controlsBody: "Pfeiltasten oder W A S D: steuern  ·  Umschalt: Schub  ·  R: Flug neu starten",
    sound: "Geraeusche",
    music: "Musik",
    musicPick: "MOD-Datei waehlen (.mod)",
    musicNone: "keine Datei geladen",
    musicLoaded: "Musik geladen",
    musicBad: "Datei ist kein lesbares MOD",
    ready: "Am Sender bereit",
    stationBody: "Der Wurm bleibt am Sender. Du uebernimmst jetzt die Bordkamera.",
    launch: "Drohne starten",
    watchReference: "Referenzflug ansehen",
    pilot: "Pilot",
    battery: "Akku",
    signal: "Signal",
    time: "Zeit",
    checkpoint: "Kontrollpunkt",
    target: "Zielpunkt",
    boost: "SCHUB",
    checkpointReached: "Kontrollpunkt erreicht",
    targetReached: "Ziel erreicht",
    crashed: "Drohne beschaedigt",
    batteryEmpty: "Akku leer",
    signalLost: "Funkverbindung verloren",
    timeout: "Zeitfenster abgelaufen",
    runComplete: "Flug abgeschlossen",
    runFailed: "Flug beendet",
    nextPilot: "Naechster Pilot",
    score: "Punkte",
    bestTime: "Flugzeit",
    remaining: "Restakku",
    wins: "gewinnt",
    draw: "Gleichstand",
    again: "Neue Strecke",
    back: "Zurueck zur Auswahl",
    reference: "Referenzpilot",
    fpv: "BORDKAMERA",
    lowSignal: "SIGNAL SCHWACH",
    lowBattery: "AKKU NIEDRIG",
    restartHint: "R startet diesen Flug neu",
  },
  en: {
    title: "FPV Remote Pilot",
    subtitle: "The worm stays at the controller. Guide the drone through every checkpoint to the destination.",
    fiction: "Fictional training area - no real locations or units",
    modeLabel: "Challenge",
    modePc: "Against a reference pilot",
    modeHuman: "Against a colleague",
    name1: "Your name",
    name2: "Colleague name",
    difficulty: "Flight course",
    start: "Start training",
    player1: "Pilot 1",
    player2: "Pilot 2",
    levels: { easy: "Calm wind", normal: "Gusty valley", hard: "Weak signal" },
    controlsBody: "Arrow keys or W A S D: steer  ·  Shift: boost  ·  R: restart flight",
    sound: "Sound effects",
    music: "Music",
    musicPick: "Pick a MOD file (.mod)",
    musicNone: "no file loaded",
    musicLoaded: "Music loaded",
    musicBad: "File is not a readable MOD",
    ready: "Ready at the controller",
    stationBody: "The worm stays at the controller. You now take over the onboard camera.",
    launch: "Launch drone",
    watchReference: "Watch reference flight",
    pilot: "Pilot",
    battery: "Battery",
    signal: "Signal",
    time: "Time",
    checkpoint: "Checkpoint",
    target: "Destination",
    boost: "BOOST",
    checkpointReached: "Checkpoint reached",
    targetReached: "Destination reached",
    crashed: "Drone damaged",
    batteryEmpty: "Battery empty",
    signalLost: "Radio link lost",
    timeout: "Time window expired",
    runComplete: "Flight complete",
    runFailed: "Flight ended",
    nextPilot: "Next pilot",
    score: "Score",
    bestTime: "Flight time",
    remaining: "Battery left",
    wins: "wins",
    draw: "Draw",
    again: "New course",
    back: "Back to setup",
    reference: "Reference pilot",
    fpv: "ONBOARD CAMERA",
    lowSignal: "WEAK SIGNAL",
    lowBattery: "LOW BATTERY",
    restartHint: "R restarts this flight",
  },
};

// ---------------------------------------------------------------- Flugmodell

const WORLD_W = 1600;
const WORLD_H = 650;
const SCALE = 8;
const DRONE_RADIUS = 12;
const ACCELERATION = 360;
const BOOST_FACTOR = 1.38;
const DRAG = 1.55;
const MAX_SPEED = 205;
const CHECKPOINT_RADIUS = 48;
const GOAL_RADIUS = 54;
const SIGNAL_GRACE = 2.4;
const FLIGHT_LIMIT = 72;

const DIFFICULTY = {
  easy: { wind: 9, battery: 100, signalPenalty: 0, aiError: 0.05 },
  normal: { wind: 20, battery: 92, signalPenalty: 7, aiError: 0.12 },
  hard: { wind: 31, battery: 84, signalPenalty: 20, aiError: 0.2 },
};

let G = null;

function clamp(value, low, high) {
  return value < low ? low : value > high ? high : value;
}

function makeRandom(seed) {
  let state = seed >>> 0;
  return () => {
    state = (state * 1664525 + 1013904223) >>> 0;
    return state / 4294967296;
  };
}

function groundHeightAt(course, x) {
  const index = clamp(Math.round(x), 0, WORLD_W);
  return course.ground[index];
}

function createCourse(seed = 1, difficulty = "normal") {
  const rand = makeRandom(seed);
  const ground = new Float32Array(WORLD_W + 1);
  const phaseA = rand() * Math.PI * 2;
  const phaseB = rand() * Math.PI * 2;
  for (let x = 0; x <= WORLD_W; x++) {
    ground[x] = 72
      + Math.sin(x / 145 + phaseA) * 18
      + Math.sin(x / 57 + phaseB) * 7;
  }

  const jitter = () => (rand() - 0.5) * 18;
  const checkpoints = [
    { x: 470, y: 320 + jitter() },
    { x: 720, y: 235 + jitter() },
    { x: 1020, y: 330 + jitter() },
    { x: 1260, y: 245 + jitter() },
  ];
  const obstacles = [
    { x: 372, y: groundHeightAt({ ground }, 392), w: 42, h: 168, kind: "tower" },
    { x: 642, y: 352, w: 72, h: WORLD_H - 352, kind: "overhang" },
    { x: 912, y: groundHeightAt({ ground }, 942), w: 58, h: 172, kind: "tower" },
    { x: 1162, y: 390, w: 78, h: WORLD_H - 390, kind: "overhang" },
  ];
  return {
    seed,
    difficulty,
    ground,
    obstacles,
    checkpoints,
    goal: { x: 1490, y: groundHeightAt({ ground }, 1490) + 82 },
    operator: { x: 64, y: groundHeightAt({ ground }, 64) + 20 },
    start: { x: 125, y: groundHeightAt({ ground }, 125) + 72 },
  };
}

function circleHitsRect(x, y, radius, rect) {
  const nearestX = clamp(x, rect.x, rect.x + rect.w);
  const nearestY = clamp(y, rect.y, rect.y + rect.h);
  const dx = x - nearestX;
  const dy = y - nearestY;
  return dx * dx + dy * dy <= radius * radius;
}

function collides(course, drone) {
  if (drone.x < DRONE_RADIUS || drone.x > WORLD_W - DRONE_RADIUS
    || drone.y > WORLD_H - DRONE_RADIUS) return true;
  if (drone.y - DRONE_RADIUS <= groundHeightAt(course, drone.x)) return true;
  return course.obstacles.some(rect => circleHitsRect(drone.x, drone.y, DRONE_RADIUS, rect));
}

function computeSignal(course, drone, checkpointIndex = 0) {
  const progressLoss = (drone.x / WORLD_W) * 65;
  const altitudeLoss = Math.max(0, drone.y - 430) * 0.08;
  const difficultyLoss = DIFFICULTY[course.difficulty]?.signalPenalty || 0;
  let shadowLoss = 0;
  for (const obstacle of course.obstacles) {
    if (drone.x > obstacle.x + obstacle.w && drone.x < obstacle.x + obstacle.w + 155
      && drone.y < obstacle.y + obstacle.h + 25) shadowLoss = Math.max(shadowLoss, 18);
  }
  const relayGain = Math.min(checkpointIndex, course.checkpoints.length) * 4;
  return clamp(100 - progressLoss - altitudeLoss - difficultyLoss - shadowLoss + relayGain, 0, 100);
}

function createDrone(course) {
  return {
    x: course.start.x,
    y: course.start.y,
    vx: 0,
    vy: 0,
    battery: DIFFICULTY[course.difficulty]?.battery || 92,
    signal: 100,
    signalLostFor: 0,
    elapsed: 0,
    checkpointIndex: 0,
    boost: false,
  };
}

function stepDrone(drone, input, course, dt, elapsed = drone.elapsed) {
  const profile = DIFFICULTY[course.difficulty] || DIFFICULTY.normal;
  const boost = input.boost ? BOOST_FACTOR : 1;
  const gustX = Math.sin(elapsed * 0.73 + course.seed * 0.01) * profile.wind;
  const gustY = Math.sin(elapsed * 1.37 + course.seed * 0.017) * profile.wind * 0.42;
  drone.vx += (input.x * ACCELERATION * boost + gustX) * dt;
  drone.vy += (input.y * ACCELERATION * boost + gustY) * dt;
  const damping = Math.exp(-DRAG * dt);
  drone.vx *= damping;
  drone.vy *= damping;
  const speed = Math.hypot(drone.vx, drone.vy);
  if (speed > MAX_SPEED) {
    drone.vx *= MAX_SPEED / speed;
    drone.vy *= MAX_SPEED / speed;
  }
  drone.x += drone.vx * dt;
  drone.y += drone.vy * dt;
  drone.elapsed += dt;
  const effort = Math.min(1, Math.hypot(input.x, input.y));
  drone.battery = Math.max(0, drone.battery - dt * (0.72 + effort * 0.48 + (input.boost ? 0.8 : 0)));
  drone.boost = !!input.boost;
  drone.signal = computeSignal(course, drone, drone.checkpointIndex);
  drone.signalLostFor = drone.signal <= 1 ? drone.signalLostFor + dt : 0;
  return drone;
}

function currentWaypoint(course, drone) {
  return drone.checkpointIndex < course.checkpoints.length
    ? course.checkpoints[drone.checkpointIndex]
    : course.goal;
}

function autopilotControl(drone, course, difficulty = "normal") {
  const waypoint = currentWaypoint(course, drone);
  const error = DIFFICULTY[difficulty]?.aiError || DIFFICULTY.normal.aiError;
  const wobble = Math.sin(drone.elapsed * 2.1 + course.seed) * error;
  return {
    x: clamp((waypoint.x - drone.x) / 92 - drone.vx / 135 + wobble, -1, 1),
    y: clamp((waypoint.y - drone.y) / 82 - drone.vy / 125 - wobble * 0.5, -1, 1),
    boost: difficulty === "hard" && waypoint.x - drone.x > 180,
  };
}

function updateCheckpoint(course, drone) {
  if (drone.checkpointIndex >= course.checkpoints.length) return false;
  const point = course.checkpoints[drone.checkpointIndex];
  if (Math.hypot(drone.x - point.x, drone.y - point.y) > CHECKPOINT_RADIUS) return false;
  drone.checkpointIndex++;
  return true;
}

function reachedGoal(course, drone) {
  return drone.checkpointIndex === course.checkpoints.length
    && Math.hypot(drone.x - course.goal.x, drone.y - course.goal.y) <= GOAL_RADIUS;
}

function scoreRun(run, checkpointCount = 4) {
  const progress = Math.min(checkpointCount, run.checkpoints || 0);
  if (!run.success) return Math.round(progress * 700 + Math.max(0, run.distance || 0) * 0.65);
  return Math.round(10000 + progress * 500 + (run.battery || 0) * 24 - (run.time || 0) * 62);
}

// ---------------------------------------------------------------- Ton

const MUSIC_CHUNK = 0.25;
const MUSIC_LEAD = 0.9;

function ensureAudio() {
  if (!G || G.audio) return G ? G.audio : null;
  const Ctx = window.AudioContext || window.webkitAudioContext;
  if (!Ctx) return null;
  const ctx = new Ctx();
  const master = ctx.createGain();
  master.gain.value = 0.85;
  master.connect(ctx.destination);
  const music = ctx.createGain();
  music.gain.value = 0.5;
  music.connect(master);
  const effects = ctx.createGain();
  effects.gain.value = 0.75;
  effects.connect(master);
  G.audio = { ctx, master, music, effects, player: null, nextTime: 0, song: null };
  return G.audio;
}

function sound(kind) {
  if (!G || !G.soundOn || !G.audio) return;
  const { ctx, effects } = G.audio;
  const now = ctx.currentTime;
  const oscillator = ctx.createOscillator();
  const gain = ctx.createGain();
  const settings = {
    launch: [120, 620, 0.35, "sawtooth"],
    checkpoint: [440, 880, 0.18, "sine"],
    success: [360, 1080, 0.55, "triangle"],
    crash: [150, 42, 0.5, "square"],
    alert: [680, 420, 0.2, "square"],
  }[kind] || [300, 500, 0.15, "sine"];
  oscillator.type = settings[3];
  oscillator.frequency.setValueAtTime(settings[0], now);
  oscillator.frequency.exponentialRampToValueAtTime(settings[1], now + settings[2]);
  gain.gain.setValueAtTime(kind === "crash" ? 0.24 : 0.14, now);
  gain.gain.exponentialRampToValueAtTime(0.001, now + settings[2]);
  oscillator.connect(gain).connect(effects);
  oscillator.start(now);
  oscillator.stop(now + settings[2] + 0.03);
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
      G.ui.musicLabel.textContent = G.musicName;
      flash(`${G.t.musicLoaded}: ${G.musicName}`);
    } catch (error) {
      G.musicName = "";
      G.ui.musicLabel.textContent = G.t.musicBad;
      flash(G.t.musicBad);
    }
  };
  reader.onerror = () => flash(G.t.musicBad);
  reader.readAsArrayBuffer(file);
}

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
    const source = ctx.createBufferSource();
    source.buffer = buffer;
    source.connect(music);
    source.start(G.audio.nextTime);
    G.audio.nextTime += MUSIC_CHUNK;
  }
}

function setSound(enabled) {
  G.soundOn = enabled;
  if (enabled) ensureAudio();
  if (G.audio && G.audio.ctx.state === "suspended") G.audio.ctx.resume();
}

function setMusic(enabled) {
  G.musicOn = enabled;
  if (enabled) ensureAudio();
  if (!G.audio) return;
  if (G.audio.ctx.state === "suspended") G.audio.ctx.resume();
  G.audio.music.gain.setTargetAtTime(enabled ? 0.5 : 0, G.audio.ctx.currentTime, 0.05);
  if (enabled) G.audio.nextTime = 0;
}

// ---------------------------------------------------------------- 3D-Szene

function px(x) { return x / SCALE - WORLD_W / (2 * SCALE); }
function py(y) { return y / SCALE - WORLD_H / (2 * SCALE); }

function gradientTexture(THREE, colors) {
  const canvas = document.createElement("canvas");
  canvas.width = 2;
  canvas.height = 256;
  const ctx = canvas.getContext("2d");
  const gradient = ctx.createLinearGradient(0, 0, 0, 256);
  colors.forEach(([position, color]) => gradient.addColorStop(position, color));
  ctx.fillStyle = gradient;
  ctx.fillRect(0, 0, 2, 256);
  const texture = new THREE.CanvasTexture(canvas);
  texture.colorSpace = THREE.SRGBColorSpace;
  return texture;
}

function buildScene(host) {
  const THREE = window.THREE;
  const canvas = document.createElement("canvas");
  canvas.style.cssText = "width:100%;height:100%;display:block;";
  host.appendChild(canvas);
  const renderer = new THREE.WebGLRenderer({ canvas, antialias: true });
  renderer.setPixelRatio(Math.min(window.devicePixelRatio || 1, 2));
  renderer.shadowMap.enabled = true;
  renderer.shadowMap.type = THREE.PCFSoftShadowMap;
  renderer.outputColorSpace = THREE.SRGBColorSpace;

  const scene = new THREE.Scene();
  scene.background = gradientTexture(THREE, [[0, "#10243c"], [0.55, "#537b91"], [1, "#d2c1a0"]]);
  scene.fog = new THREE.Fog(0x6f8791, 55, 175);
  const camera = new THREE.PerspectiveCamera(48, 1, 0.3, 500);
  camera.position.set(px(180), py(210), 44);

  const sun = new THREE.DirectionalLight(0xffe4bc, 1.35);
  sun.position.set(-40, 80, 90);
  sun.castShadow = true;
  sun.shadow.mapSize.set(1024, 1024);
  sun.shadow.camera.left = -45;
  sun.shadow.camera.right = 45;
  sun.shadow.camera.top = 35;
  sun.shadow.camera.bottom = -35;
  scene.add(sun);
  scene.add(new THREE.HemisphereLight(0xbad8ef, 0x4d3927, 0.85));

  return { THREE, canvas, renderer, scene, camera, sun, courseGroup: null };
}

function disposeObject(root) {
  if (!root) return;
  root.traverse(object => {
    if (object.geometry) object.geometry.dispose();
    if (object.material) {
      const materials = Array.isArray(object.material) ? object.material : [object.material];
      materials.forEach(material => material.dispose());
    }
  });
}

function makeWormOperator(THREE) {
  const group = new THREE.Group();
  const body = new THREE.Mesh(
    new THREE.CapsuleGeometry(1.15, 1.8, 7, 16),
    new THREE.MeshStandardMaterial({ color: 0xd88b43, roughness: 0.7 }));
  body.position.y = 1.7;
  body.castShadow = true;
  group.add(body);
  for (const x of [-0.38, 0.38]) {
    const eye = new THREE.Mesh(new THREE.SphereGeometry(0.28, 10, 10), new THREE.MeshStandardMaterial({ color: 0xffffff }));
    eye.position.set(x, 2.35, 0.9);
    group.add(eye);
    const pupil = new THREE.Mesh(new THREE.SphereGeometry(0.12, 8, 8), new THREE.MeshStandardMaterial({ color: 0x111820 }));
    pupil.position.set(x, 2.35, 1.13);
    group.add(pupil);
  }
  const controller = new THREE.Mesh(
    new THREE.BoxGeometry(2.7, 1.2, 0.7),
    new THREE.MeshStandardMaterial({ color: 0x26313a, roughness: 0.45, metalness: 0.25 }));
  controller.position.set(0, 0.7, 1.25);
  group.add(controller);
  for (const x of [-0.7, 0.7]) {
    const stick = new THREE.Mesh(new THREE.CylinderGeometry(0.07, 0.07, 0.65, 8), new THREE.MeshStandardMaterial({ color: 0xb9c3c9 }));
    stick.position.set(x, 1.45, 1.25);
    group.add(stick);
  }
  return group;
}

function makeDrone(THREE) {
  const group = new THREE.Group();
  const body = new THREE.Mesh(
    new THREE.BoxGeometry(2.1, 0.62, 1.4),
    new THREE.MeshStandardMaterial({ color: 0x456674, roughness: 0.38, metalness: 0.5 }));
  body.castShadow = true;
  group.add(body);
  const camera = new THREE.Mesh(
    new THREE.SphereGeometry(0.34, 10, 10),
    new THREE.MeshStandardMaterial({ color: 0x101820, emissive: 0x172b38, emissiveIntensity: 0.6 }));
  camera.position.set(1.1, -0.08, 0);
  group.add(camera);
  for (const [z, color] of [[-0.77, 0xff4d45], [0.77, 0x5dff87]]) {
    const light = new THREE.Mesh(
      new THREE.SphereGeometry(0.13, 8, 8),
      new THREE.MeshStandardMaterial({ color, emissive: color, emissiveIntensity: 2.2 }));
    light.position.set(0.75, 0.08, z);
    group.add(light);
  }
  const rotors = [];
  for (const [x, z] of [[-1.2, -0.9], [-1.2, 0.9], [1.2, -0.9], [1.2, 0.9]]) {
    const arm = new THREE.Mesh(new THREE.BoxGeometry(1.1, 0.12, 0.12), new THREE.MeshStandardMaterial({ color: 0x1d2429 }));
    arm.position.set(x * 0.55, 0, z * 0.55);
    arm.rotation.y = z > 0 ? 0.65 : -0.65;
    group.add(arm);
    const rotor = new THREE.Mesh(
      new THREE.CylinderGeometry(0.7, 0.7, 0.045, 18),
      new THREE.MeshStandardMaterial({ color: 0xe5edf0, transparent: true, opacity: 0.45 }));
    rotor.position.set(x, 0.28, z);
    group.add(rotor);
    rotors.push(rotor);
  }
  group.userData.rotors = rotors;
  return group;
}

function buildCourseScene(course) {
  const { THREE, scene } = G;
  if (G.courseGroup) {
    scene.remove(G.courseGroup);
    disposeObject(G.courseGroup);
  }
  const group = new THREE.Group();

  const shape = new THREE.Shape();
  shape.moveTo(px(0), py(0));
  for (let x = 0; x <= WORLD_W; x += 8) shape.lineTo(px(x), py(groundHeightAt(course, x)));
  shape.lineTo(px(WORLD_W), py(0));
  shape.closePath();
  const ground = new THREE.Mesh(
    new THREE.ShapeGeometry(shape),
    new THREE.MeshStandardMaterial({ color: 0x6d5134, roughness: 0.96 }));
  ground.position.z = 0;
  ground.receiveShadow = true;
  group.add(ground);

  for (const obstacle of course.obstacles) {
    const mesh = new THREE.Mesh(
      new THREE.BoxGeometry(obstacle.w / SCALE, obstacle.h / SCALE, 4.2),
      new THREE.MeshStandardMaterial({ color: obstacle.kind === "tower" ? 0x5f6667 : 0x4c5355, roughness: 0.86 }));
    mesh.position.set(px(obstacle.x + obstacle.w / 2), py(obstacle.y + obstacle.h / 2), -0.5);
    mesh.castShadow = true;
    mesh.receiveShadow = true;
    group.add(mesh);
    for (let y = obstacle.y + 24; y < obstacle.y + obstacle.h; y += 42) {
      const stripe = new THREE.Mesh(
        new THREE.BoxGeometry((obstacle.w + 2) / SCALE, 5 / SCALE, 4.35),
        new THREE.MeshStandardMaterial({ color: 0xc08a3e, roughness: 0.7 }));
      stripe.position.set(px(obstacle.x + obstacle.w / 2), py(y), 0);
      group.add(stripe);
    }
  }

  const checkpointMeshes = [];
  course.checkpoints.forEach((point, index) => {
    const ring = new THREE.Mesh(
      new THREE.TorusGeometry(CHECKPOINT_RADIUS / SCALE, 0.42, 10, 42),
      new THREE.MeshStandardMaterial({ color: 0x62d9ff, emissive: 0x167da0, emissiveIntensity: 1.2 }));
    ring.position.set(px(point.x), py(point.y), 0.5);
    ring.userData.index = index;
    group.add(ring);
    checkpointMeshes.push(ring);
  });

  const target = new THREE.Group();
  const targetRing = new THREE.Mesh(
    new THREE.TorusGeometry(GOAL_RADIUS / SCALE, 0.58, 12, 48),
    new THREE.MeshStandardMaterial({ color: 0xffc24a, emissive: 0xc46b12, emissiveIntensity: 1.1 }));
  target.add(targetRing);
  const beacon = new THREE.Mesh(
    new THREE.CylinderGeometry(0.45, 1.25, 5.5, 10),
    new THREE.MeshStandardMaterial({ color: 0xd69c31, emissive: 0x8f4a0a, emissiveIntensity: 0.65 }));
  beacon.position.y = -GOAL_RADIUS / SCALE - 1.8;
  target.add(beacon);
  target.position.set(px(course.goal.x), py(course.goal.y), 0.5);
  group.add(target);

  const operator = makeWormOperator(THREE);
  operator.scale.setScalar(1.4);
  operator.position.set(px(course.operator.x), py(course.operator.y), 1.2);
  group.add(operator);

  const droneMesh = makeDrone(THREE);
  droneMesh.scale.setScalar(1.15);
  droneMesh.position.set(px(course.start.x), py(course.start.y), 1.4);
  group.add(droneMesh);

  scene.add(group);
  G.courseGroup = group;
  G.checkpointMeshes = checkpointMeshes;
  G.targetMesh = target;
  G.operatorMesh = operator;
  G.droneMesh = droneMesh;
}

// ---------------------------------------------------------------- Partie und Wertung

function startMatch(config) {
  G.config = config;
  G.course = createCourse((Date.now() ^ Math.floor(Math.random() * 0x7fffffff)) >>> 0, config.difficulty);
  G.pilots = [
    { name: config.name1 || G.t.player1, human: true },
    { name: config.mode === "pc" ? G.t.reference : (config.name2 || G.t.player2), human: config.mode !== "pc" },
  ];
  G.runs = [];
  G.activePilot = 0;
  buildCourseScene(G.course);
  showReady();
}

function showReady() {
  const pilot = G.pilots[G.activePilot];
  G.phase = "ready";
  G.ui.readyTitle.textContent = `${G.t.ready}: ${pilot.name}`;
  G.ui.readyBody.textContent = pilot.human
    ? `${G.t.stationBody}\n${G.t.controlsBody}`
    : G.t.watchReference;
  G.ui.readyButton.textContent = pilot.human ? G.t.launch : G.t.watchReference;
  G.camera.position.set(px(150), py(160), 29);
  G.camera.lookAt(px(105), py(125), 0);
  setScreen("ready");
}

function beginRun() {
  G.drone = createDrone(G.course);
  G.phase = "flight";
  G.message = "";
  G.messageTime = 0;
  G.alertedBattery = false;
  G.alertedSignal = false;
  G.input = { left: false, right: false, up: false, down: false, boost: false };
  for (const mesh of G.checkpointMeshes) {
    mesh.visible = true;
    mesh.material.opacity = 1;
    mesh.material.transparent = false;
  }
  setScreen("game");
  sound("launch");
}

function restartRun() {
  if (G.phase !== "flight" || !G.pilots[G.activePilot].human) return;
  beginRun();
  flash(G.t.restartHint);
}

function finishRun(success, reason) {
  if (G.phase !== "flight") return;
  const run = {
    pilot: G.pilots[G.activePilot].name,
    success,
    reason,
    time: G.drone.elapsed,
    battery: G.drone.battery,
    checkpoints: G.drone.checkpointIndex,
    distance: G.drone.x,
  };
  run.score = scoreRun(run, G.course.checkpoints.length);
  G.runs.push(run);
  G.phase = "runEnd";
  success ? sound("success") : sound("crash");

  G.ui.runTitle.textContent = success ? G.t.runComplete : G.t.runFailed;
  G.ui.runReason.textContent = reason;
  G.ui.runStats.textContent = `${G.t.score}: ${run.score}  ·  ${G.t.bestTime}: ${run.time.toFixed(1)} s  ·  ${G.t.remaining}: ${run.battery.toFixed(0)} %`;
  if (G.activePilot === 0) {
    G.ui.runButton.textContent = G.t.nextPilot;
    setScreen("runEnd");
  } else {
    showResult();
  }
}

function advancePilot() {
  G.activePilot = 1;
  showReady();
}

function showResult() {
  G.phase = "result";
  const [first, second] = G.runs;
  const winner = first.score === second.score ? null : (first.score > second.score ? first : second);
  G.ui.resultTitle.textContent = winner ? `${winner.pilot} ${G.t.wins}` : G.t.draw;
  G.ui.resultBody.innerHTML = "";
  for (const run of G.runs) {
    const row = el("div", "padding:10px 0;border-bottom:1px solid #dbe3e9;text-align:left;");
    row.appendChild(el("div", "font-weight:700;color:#152532;", run.pilot));
    row.appendChild(el("div", "font-size:13px;color:#657582;", `${G.t.score}: ${run.score}  ·  ${run.time.toFixed(1)} s  ·  ${run.battery.toFixed(0)} %`));
    G.ui.resultBody.appendChild(row);
  }
  rememberResult(winner ? winner.pilot : null);
  setScreen("result");
}

function rememberResult(winner) {
  if (!winner) return;
  try {
    const raw = localStorage.getItem("pausegame.fpv.scores");
    const scores = raw ? JSON.parse(raw) : {};
    scores[winner] = (scores[winner] || 0) + 1;
    localStorage.setItem("pausegame.fpv.scores", JSON.stringify(scores));
  } catch (error) {
    // Die lokale Bestenliste ist optional und darf das Spiel nicht unterbrechen.
  }
}

// ---------------------------------------------------------------- Oberflaeche

function el(tag, style, text) {
  const node = document.createElement(tag);
  if (style) node.setAttribute("style", style);
  if (text !== undefined) node.textContent = text;
  return node;
}

const PANEL = "position:absolute;inset:0;display:flex;align-items:center;justify-content:center;background:rgba(7,17,25,.74);backdrop-filter:blur(4px);z-index:8;";
const CARD = "background:#f7f9fa;border-radius:12px;padding:24px 28px;min-width:340px;max-width:500px;font-family:Segoe UI,sans-serif;box-shadow:0 16px 46px rgba(0,0,0,.42);";
const FIELD = "width:100%;padding:8px 10px;margin:4px 0 13px;border:1px solid #bfcbd3;border-radius:6px;font-size:14px;box-sizing:border-box;background:#fff;";
const LABEL = "font-size:11px;color:#5a6b76;text-transform:uppercase;letter-spacing:.55px;";
const BUTTON = "width:100%;padding:11px;border:0;border-radius:6px;background:#2779a7;color:#fff;font-size:15px;font-weight:650;cursor:pointer;";

function buildUi(host) {
  const t = G.t;
  const setup = el("div", PANEL);
  const card = el("div", CARD);
  card.appendChild(el("div", "font-size:23px;font-weight:750;color:#142531;", t.title));
  card.appendChild(el("div", "font-size:13px;color:#60727e;margin:3px 0 5px;line-height:1.45;", t.subtitle));
  card.appendChild(el("div", "font-size:11px;color:#9a6b27;margin:0 0 16px;", t.fiction));

  card.appendChild(el("div", LABEL, t.modeLabel));
  const mode = el("select", FIELD);
  [["pc", t.modePc], ["human", t.modeHuman]].forEach(([value, text]) => {
    const option = el("option", null, text);
    option.value = value;
    mode.appendChild(option);
  });
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

  card.appendChild(el("div", LABEL, t.difficulty));
  const difficulty = el("select", FIELD);
  for (const key of ["easy", "normal", "hard"]) {
    const option = el("option", null, t.levels[key]);
    option.value = key;
    difficulty.appendChild(option);
  }
  difficulty.value = "normal";
  card.appendChild(difficulty);

  mode.addEventListener("change", () => {
    const visible = mode.value === "human";
    name2Label.style.display = visible ? "" : "none";
    name2.style.display = visible ? "" : "none";
  });
  mode.dispatchEvent(new Event("change"));

  const audioRow = el("div", "display:flex;gap:18px;margin:2px 0 10px;");
  const soundBox = el("input", "margin:0 6px 0 0;");
  soundBox.type = "checkbox";
  soundBox.checked = localStorage.getItem("pausegame.sound") === "1";
  const soundLabel = el("label", "font-size:13px;color:#33454f;display:flex;align-items:center;cursor:pointer;");
  soundLabel.append(soundBox, document.createTextNode(t.sound));
  const musicBox = el("input", "margin:0 6px 0 0;");
  musicBox.type = "checkbox";
  musicBox.checked = localStorage.getItem("pausegame.music") === "1";
  const musicToggle = el("label", "font-size:13px;color:#33454f;display:flex;align-items:center;cursor:pointer;");
  musicToggle.append(musicBox, document.createTextNode(t.music));
  audioRow.append(soundLabel, musicToggle);
  card.appendChild(audioRow);

  const musicPick = el("input", "font-size:12px;width:100%;margin-bottom:3px;");
  musicPick.type = "file";
  musicPick.accept = ".mod,.MOD";
  musicPick.title = t.musicPick;
  const musicLabel = el("div", "font-size:11px;color:#82909a;margin-bottom:13px;", t.musicNone);
  card.append(musicPick, musicLabel);

  const startButton = el("button", BUTTON, t.start);
  startButton.addEventListener("click", () => {
    localStorage.setItem("pausegame.name1", name1.value.trim());
    localStorage.setItem("pausegame.name2", name2.value.trim());
    localStorage.setItem("pausegame.sound", soundBox.checked ? "1" : "0");
    localStorage.setItem("pausegame.music", musicBox.checked ? "1" : "0");
    setSound(soundBox.checked);
    setMusic(musicBox.checked);
    startMatch({ mode: mode.value, name1: name1.value.trim(), name2: name2.value.trim(), difficulty: difficulty.value });
  });
  card.append(startButton, el("div", "font-size:11px;color:#768690;margin-top:12px;line-height:1.5;", t.controlsBody));
  setup.appendChild(card);
  host.appendChild(setup);

  const ready = el("div", PANEL + "display:none;justify-content:flex-end;padding-right:7%;box-sizing:border-box;");
  const readyCard = el("div", CARD + "text-align:center;");
  const readyTitle = el("div", "font-size:21px;font-weight:700;color:#142531;margin-bottom:8px;");
  const readyBody = el("div", "font-size:13px;color:#60727e;margin-bottom:18px;white-space:pre-line;line-height:1.55;");
  const readyButton = el("button", BUTTON);
  readyButton.addEventListener("click", beginRun);
  readyCard.append(readyTitle, readyBody, readyButton);
  ready.appendChild(readyCard);
  host.appendChild(ready);

  const hud = el("div", "position:absolute;inset:0;pointer-events:none;z-index:5;font-family:Consolas,Segoe UI,sans-serif;color:#e8fbff;text-shadow:0 1px 3px #000;");
  const fpvLabel = el("div", "position:absolute;top:14px;left:18px;font-size:12px;letter-spacing:1.4px;", `● REC  ${t.fpv}`);
  const hudPilot = el("div", "position:absolute;top:36px;left:18px;font-weight:700;font-size:15px;");
  const hudStatus = el("div", "position:absolute;top:15px;right:18px;text-align:right;font-size:13px;line-height:1.55;");
  const hudCheckpoint = el("div", "position:absolute;bottom:18px;left:18px;font-size:13px;");
  const hudControls = el("div", "position:absolute;bottom:18px;right:18px;font-size:11px;opacity:.85;", t.controlsBody);
  const warning = el("div", "position:absolute;top:50%;left:50%;transform:translate(-50%,-50%);font-size:24px;font-weight:800;letter-spacing:1.4px;color:#ffcb55;opacity:0;transition:opacity .15s;");
  const crosshair = el("div", "position:absolute;left:50%;top:50%;width:28px;height:28px;transform:translate(-50%,-50%);border:1px solid rgba(210,250,255,.65);border-radius:50%;box-shadow:0 0 0 1px rgba(0,0,0,.2);");
  const scanlines = el("div", "position:absolute;inset:0;background:repeating-linear-gradient(0deg,rgba(255,255,255,.025) 0,rgba(255,255,255,.025) 1px,transparent 1px,transparent 4px);opacity:.45;");
  hud.append(scanlines, fpvLabel, hudPilot, hudStatus, hudCheckpoint, hudControls, warning, crosshair);
  host.appendChild(hud);

  const message = el("div", "position:absolute;left:50%;top:74px;transform:translateX(-50%);padding:7px 15px;border-radius:18px;background:rgba(7,20,28,.82);color:#fff;font-family:Segoe UI,sans-serif;font-size:13px;z-index:6;opacity:0;transition:opacity .2s;pointer-events:none;");
  host.appendChild(message);

  const runEnd = el("div", PANEL + "display:none;");
  const runCard = el("div", CARD + "text-align:center;");
  const runTitle = el("div", "font-size:21px;font-weight:750;color:#142531;");
  const runReason = el("div", "font-size:14px;color:#536773;margin:6px 0 12px;");
  const runStats = el("div", "font-size:13px;color:#6c7e88;margin-bottom:18px;");
  const runButton = el("button", BUTTON);
  runButton.addEventListener("click", advancePilot);
  runCard.append(runTitle, runReason, runStats, runButton);
  runEnd.appendChild(runCard);
  host.appendChild(runEnd);

  const result = el("div", PANEL + "display:none;");
  const resultCard = el("div", CARD + "text-align:center;");
  const resultTitle = el("div", "font-size:23px;font-weight:750;color:#142531;margin-bottom:10px;");
  const resultBody = el("div", "margin-bottom:18px;");
  const againButton = el("button", BUTTON, t.again);
  const backButton = el("button", BUTTON + "background:#dfe7eb;color:#31444f;margin-top:8px;", t.back);
  againButton.addEventListener("click", () => startMatch(G.config));
  backButton.addEventListener("click", () => setScreen("setup"));
  resultCard.append(resultTitle, resultBody, againButton, backButton);
  result.appendChild(resultCard);
  host.appendChild(result);

  musicPick.addEventListener("change", () => {
    if (musicPick.files && musicPick.files[0]) loadMusicFile(musicPick.files[0]);
  });

  G.ui = {
    setup, ready, readyTitle, readyBody, readyButton, hud, hudPilot, hudStatus,
    hudCheckpoint, warning, message, runEnd, runTitle, runReason, runStats, runButton,
    result, resultTitle, resultBody, musicLabel,
  };
}

function setScreen(name) {
  for (const key of ["setup", "ready", "runEnd", "result"]) G.ui[key].style.display = key === name ? "flex" : "none";
  G.ui.hud.style.display = name === "game" ? "block" : "none";
}

function flash(text) {
  if (!G || !G.ui) return;
  G.message = text;
  G.messageTime = 2.1;
  G.ui.message.textContent = text;
}

function updateHud() {
  const drone = G.drone;
  const total = G.course.checkpoints.length;
  G.ui.hudPilot.textContent = `${G.t.pilot}: ${G.pilots[G.activePilot].name}`;
  G.ui.hudStatus.textContent = `${G.t.battery} ${drone.battery.toFixed(0)} %\n${G.t.signal} ${drone.signal.toFixed(0)} %\n${G.t.time} ${drone.elapsed.toFixed(1)} s`;
  G.ui.hudStatus.style.whiteSpace = "pre-line";
  G.ui.hudCheckpoint.textContent = drone.checkpointIndex < total
    ? `${G.t.checkpoint} ${drone.checkpointIndex + 1}/${total}`
    : G.t.target;
  let warning = "";
  if (drone.signal < 20) warning = G.t.lowSignal;
  else if (drone.battery < 18) warning = G.t.lowBattery;
  else if (drone.boost) warning = G.t.boost;
  G.ui.warning.textContent = warning;
  G.ui.warning.style.opacity = warning ? "1" : "0";
  G.ui.message.style.opacity = G.messageTime > 0 ? "1" : "0";
}

// ---------------------------------------------------------------- Eingabe und Schleife

function onKeyDown(event) {
  if (!G || !G.running) return;
  const controlled = ["ArrowLeft", "ArrowRight", "ArrowUp", "ArrowDown", " ", "Shift"];
  if (controlled.includes(event.key)) event.preventDefault();
  if (event.key.toLowerCase() === "r") restartRun();
  if (G.phase !== "flight" || !G.pilots[G.activePilot].human) return;
  if (event.key === "ArrowLeft" || event.key.toLowerCase() === "a") G.input.left = true;
  if (event.key === "ArrowRight" || event.key.toLowerCase() === "d") G.input.right = true;
  if (event.key === "ArrowUp" || event.key.toLowerCase() === "w") G.input.up = true;
  if (event.key === "ArrowDown" || event.key.toLowerCase() === "s") G.input.down = true;
  if (event.key === "Shift") G.input.boost = true;
}

function onKeyUp(event) {
  if (!G || !G.input) return;
  if (event.key === "ArrowLeft" || event.key.toLowerCase() === "a") G.input.left = false;
  if (event.key === "ArrowRight" || event.key.toLowerCase() === "d") G.input.right = false;
  if (event.key === "ArrowUp" || event.key.toLowerCase() === "w") G.input.up = false;
  if (event.key === "ArrowDown" || event.key.toLowerCase() === "s") G.input.down = false;
  if (event.key === "Shift") G.input.boost = false;
}

function humanControl() {
  return {
    x: (G.input.right ? 1 : 0) - (G.input.left ? 1 : 0),
    y: (G.input.up ? 1 : 0) - (G.input.down ? 1 : 0),
    boost: G.input.boost,
  };
}

function updateFlight(dt) {
  const pilot = G.pilots[G.activePilot];
  const input = pilot.human ? humanControl() : autopilotControl(G.drone, G.course, G.config.difficulty);
  stepDrone(G.drone, input, G.course, dt);
  if (G.messageTime > 0) G.messageTime -= dt;

  if (updateCheckpoint(G.course, G.drone)) {
    const mesh = G.checkpointMeshes[G.drone.checkpointIndex - 1];
    if (mesh) mesh.visible = false;
    sound("checkpoint");
    flash(`${G.t.checkpointReached}: ${G.drone.checkpointIndex}/${G.course.checkpoints.length}`);
  }
  if (reachedGoal(G.course, G.drone)) {
    finishRun(true, G.t.targetReached);
    return;
  }
  if (collides(G.course, G.drone)) {
    finishRun(false, G.t.crashed);
    return;
  }
  if (G.drone.battery <= 0) {
    finishRun(false, G.t.batteryEmpty);
    return;
  }
  if (G.drone.signalLostFor >= SIGNAL_GRACE) {
    finishRun(false, G.t.signalLost);
    return;
  }
  if (G.drone.elapsed >= FLIGHT_LIMIT) {
    finishRun(false, G.t.timeout);
    return;
  }
  if (!G.alertedBattery && G.drone.battery < 18) {
    G.alertedBattery = true;
    sound("alert");
  }
  if (!G.alertedSignal && G.drone.signal < 20) {
    G.alertedSignal = true;
    sound("alert");
  }
}

function syncScene(dt) {
  if (!G.drone || !G.droneMesh) return;
  const drone = G.drone;
  G.droneMesh.position.set(px(drone.x), py(drone.y), 1.4);
  G.droneMesh.rotation.z = clamp(drone.vy / MAX_SPEED, -0.5, 0.5) * 0.45;
  G.droneMesh.rotation.x = clamp(-drone.vx / MAX_SPEED, -0.35, 0.35) * 0.25;
  for (const rotor of G.droneMesh.userData.rotors) rotor.rotation.y += dt * (drone.boost ? 75 : 52);
  if (G.targetMesh) G.targetMesh.rotation.z += dt * 0.42;
  for (const mesh of G.checkpointMeshes) {
    if (mesh.visible) mesh.rotation.z -= dt * 0.32;
  }

  // Nicht nur stur vor die Drohne schauen: der naechste Kontrollring muss schon
  // vor dem Lenkmanoever sichtbar sein. Der Mittelpunkt zwischen Drohne und
  // aktuellem Wegpunkt haelt beides im Bild, ohne zu weit herauszuzoomen.
  const waypoint = currentWaypoint(G.course, drone);
  const waypointLead = clamp((waypoint.x - drone.x) * 0.48, 95, 185);
  const desiredX = px(clamp(drone.x + waypointLead, 185, WORLD_W - 90));
  const desiredY = py(clamp(
    drone.y + (waypoint.y - drone.y) * 0.42 + drone.vy * 0.08,
    155,
    WORLD_H - 110));
  G.camera.position.x += (desiredX - G.camera.position.x) * Math.min(1, dt * 3.6);
  G.camera.position.y += (desiredY - G.camera.position.y) * Math.min(1, dt * 3.6);
  G.camera.position.z += (46 - G.camera.position.z) * Math.min(1, dt * 2.8);
  G.camera.lookAt(desiredX + 2.5, desiredY, 0);
}

function resize() {
  if (!G) return;
  const width = G.host.clientWidth || 800;
  const height = G.host.clientHeight || 500;
  G.renderer.setSize(width, height, false);
  G.camera.aspect = width / Math.max(1, height);
  G.camera.updateProjectionMatrix();
}

function frame(now) {
  if (!G || !G.running) return;
  G.raf = requestAnimationFrame(frame);
  if (document.visibilityState === "hidden") {
    G.last = now;
    return;
  }
  const dt = Math.min(0.04, (now - G.last) / 1000 || 0);
  G.last = now;
  pumpMusic();
  if (G.phase === "flight") {
    updateFlight(dt);
    syncScene(dt);
    updateHud();
  }
  G.renderer.render(G.scene, G.camera);
}

// ---------------------------------------------------------------- Ein- und Ausstieg

export function start(host, language) {
  dispose();
  if (!window.THREE) {
    host.appendChild(el("div", "padding:20px;font-family:Segoe UI,sans-serif;color:#b3261e;", "three.js nicht geladen"));
    return;
  }
  const scene = buildScene(host);
  G = Object.assign({
    host,
    t: TEXTS[language === "en" ? "en" : "de"],
    running: true,
    phase: "setup",
    input: { left: false, right: false, up: false, down: false, boost: false },
    last: performance.now(),
    soundOn: false,
    musicOn: false,
    musicName: "",
    audio: null,
    message: "",
    messageTime: 0,
  }, scene);
  buildUi(host);
  setScreen("setup");
  resize();
  G.observer = new ResizeObserver(resize);
  G.observer.observe(host);
  window.addEventListener("keydown", onKeyDown);
  window.addEventListener("keyup", onKeyUp);
  G.raf = requestAnimationFrame(frame);
}

// Reiner Rechenkern fuer Tools/PauseGame.Probe. Diese Funktionen benoetigen weder
// DOM noch WebGL und koennen daher in Node als Regressionstest laufen.
export const __testHooks = {
  createCourse,
  groundHeightAt,
  circleHitsRect,
  collides,
  computeSignal,
  createDrone,
  stepDrone,
  currentWaypoint,
  autopilotControl,
  updateCheckpoint,
  reachedGoal,
  scoreRun,
  constants: {
    WORLD_W, WORLD_H, DRONE_RADIUS, MAX_SPEED, CHECKPOINT_RADIUS, GOAL_RADIUS,
    SIGNAL_GRACE, FLIGHT_LIMIT, DIFFICULTY,
  },
};

export function dispose() {
  if (!G) return;
  G.running = false;
  if (G.raf) cancelAnimationFrame(G.raf);
  window.removeEventListener("keydown", onKeyDown);
  window.removeEventListener("keyup", onKeyUp);
  if (G.observer) G.observer.disconnect();
  if (G.audio) {
    try {
      G.audio.master.gain.value = 0;
      G.audio.ctx.close();
    } catch (error) {
      // AudioContext ist bereits geschlossen.
    }
  }
  if (G.courseGroup) disposeObject(G.courseGroup);
  try {
    G.renderer.dispose();
  } catch (error) {
    // WebGL-Kontext kann beim Seitenwechsel bereits verloren sein.
  }
  if (G.host) G.host.innerHTML = "";
  G = null;
}
