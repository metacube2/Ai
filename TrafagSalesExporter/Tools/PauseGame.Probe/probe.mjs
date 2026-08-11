// Kopfloser Regressionstest fuer den rechnenden Kern des FPV-Pausenspiels.
//
// Aufruf: node Tools/PauseGame.Probe/probe.mjs

import { __testHooks as H } from "../../wwwroot/js/pausegame.js";

const {
  WORLD_W, WORLD_H, DRONE_RADIUS, MAX_SPEED, CHECKPOINT_RADIUS, GOAL_RADIUS,
  FLIGHT_LIMIT,
} = H.constants;

let failures = 0;
function check(name, ok, detail) {
  console.log(`${ok ? "PASS" : "FAIL"}  ${name}  ->  ${detail}`);
  if (!ok) failures++;
}

// ---------------------------------------------------------- Strecke

let lowestGround = Infinity;
let highestGround = -Infinity;
let steepestStep = 0;
let everyWaypointClear = true;
let everyGoalClear = true;

for (let seed = 1; seed <= 40; seed++) {
  const course = H.createCourse(seed * 7919, seed % 3 === 0 ? "hard" : "normal");
  let previous = H.groundHeightAt(course, 0);
  for (let x = 0; x <= WORLD_W; x++) {
    const ground = H.groundHeightAt(course, x);
    lowestGround = Math.min(lowestGround, ground);
    highestGround = Math.max(highestGround, ground);
    steepestStep = Math.max(steepestStep, Math.abs(ground - previous));
    previous = ground;
  }
  for (const point of course.checkpoints) {
    everyWaypointClear &&= !H.collides(course, { x: point.x, y: point.y });
  }
  everyGoalClear &&= !H.collides(course, { x: course.goal.x, y: course.goal.y });
}

check("40 Strecken haben durchgehend Boden", lowestGround > 35,
  `niedrigster Boden ${lowestGround.toFixed(1)} px`);
check("Gelaende laesst genuegend Flugraum", highestGround < WORLD_H * 0.25,
  `hoechster Boden ${highestGround.toFixed(1)} px`);
check("Gelaendeprofil hat keine Pixelzacken", steepestStep < 2,
  `groesster Einzelschritt ${steepestStep.toFixed(2)} px`);
check("Alle Kontrollpunkte liegen kollisionsfrei", everyWaypointClear, "40 Zufallsstrecken");
check("Alle Zielpunkte liegen kollisionsfrei", everyGoalClear, "40 Zufallsstrecken");

const course = H.createCourse(20260811, "normal");
check("Strecke enthaelt vier Kontrollpunkte", course.checkpoints.length === 4,
  `${course.checkpoints.length} Kontrollpunkte`);
check("Strecke enthaelt wechselnde Hindernisse", course.obstacles.length >= 4,
  `${course.obstacles.length} Hindernisse`);

// ---------------------------------------------------------- Kollision

const firstObstacle = course.obstacles[0];
check("Kreistest erkennt Treffer auf Rechteck",
  H.circleHitsRect(firstObstacle.x - 5, firstObstacle.y + 20, 8, firstObstacle),
  "5 px vor der Kante bei Radius 8");
check("Kreistest laesst entfernte Rechtecke in Ruhe",
  !H.circleHitsRect(firstObstacle.x - 50, firstObstacle.y + 20, 8, firstObstacle),
  "50 px vor der Kante");
check("Drohne kollidiert mit dem Boden",
  H.collides(course, { x: 220, y: H.groundHeightAt(course, 220) + DRONE_RADIUS - 1 }),
  "Unterkante liegt einen Pixel im Boden");
check("Drohne kollidiert mit einem Bauwerk",
  H.collides(course, { x: firstObstacle.x + firstObstacle.w / 2, y: firstObstacle.y + 40 }),
  firstObstacle.kind);
check("Drohne ist am Start frei",
  !H.collides(course, H.createDrone(course)),
  `Start bei ${course.start.x}/${course.start.y.toFixed(0)}`);

// ---------------------------------------------------------- Steuerung und Ressourcen

const moving = H.createDrone(course);
const startX = moving.x;
const startY = moving.y;
for (let i = 0; i < 60; i++) H.stepDrone(moving, { x: 1, y: 1, boost: false }, course, 1 / 60);
check("Direkte Steuerung bewegt nach rechts", moving.x > startX + 70,
  `${(moving.x - startX).toFixed(1)} px in einer Sekunde`);
check("Direkte Steuerung bewegt nach oben", moving.y > startY + 70,
  `${(moving.y - startY).toFixed(1)} px in einer Sekunde`);
check("Geschwindigkeit bleibt begrenzt", Math.hypot(moving.vx, moving.vy) <= MAX_SPEED + 0.001,
  `${Math.hypot(moving.vx, moving.vy).toFixed(1)} von ${MAX_SPEED}`);
check("Flug verbraucht Akku", moving.battery < 92,
  `${moving.battery.toFixed(1)} % verbleiben`);

const normalBattery = H.createDrone(course);
const boostBattery = H.createDrone(course);
for (let i = 0; i < 120; i++) {
  H.stepDrone(normalBattery, { x: 1, y: 0, boost: false }, course, 1 / 60);
  H.stepDrone(boostBattery, { x: 1, y: 0, boost: true }, course, 1 / 60);
}
check("Schub kostet mehr Akku", boostBattery.battery < normalBattery.battery - 1,
  `Schub ${boostBattery.battery.toFixed(1)} %, normal ${normalBattery.battery.toFixed(1)} %`);

const startSignal = H.computeSignal(course, H.createDrone(course), 0);
const goalSignal = H.computeSignal(course, { x: course.goal.x, y: course.goal.y }, course.checkpoints.length);
check("Funksignal ist am Sender stark", startSignal > 85, `${startSignal.toFixed(0)} %`);
check("Funksignal nimmt mit Entfernung ab", goalSignal < startSignal - 25 && goalSignal > 15,
  `Start ${startSignal.toFixed(0)} %, Ziel ${goalSignal.toFixed(0)} %`);

const hardCourse = H.createCourse(20260811, "hard");
const offRoute = H.createDrone(hardCourse);
offRoute.x = 1580;
offRoute.y = 630;
const offRouteSignal = H.computeSignal(hardCourse, offRoute, 0);
for (let i = 0; i < 6; i++) H.stepDrone(offRoute, { x: 0, y: 0, boost: false }, hardCourse, 0.5);
check("Falsche Hochroute ohne Relais kann das Signal verlieren", offRouteSignal <= 1,
  `${offRouteSignal.toFixed(0)} % Signal`);
check("Vollstaendiger Signalausfall sammelt Abbruchzeit", offRoute.signalLostFor >= 2.4,
  `${offRoute.signalLostFor.toFixed(1)} s ohne Verbindung`);

// ---------------------------------------------------------- Kontrollpunkte und Ziel

const progressDrone = H.createDrone(course);
progressDrone.x = course.checkpoints[0].x + CHECKPOINT_RADIUS - 1;
progressDrone.y = course.checkpoints[0].y;
check("Kontrollring wird innerhalb des Radius gewertet", H.updateCheckpoint(course, progressDrone),
  `Radius ${CHECKPOINT_RADIUS}`);
check("Kontrollringe muessen in Reihenfolge durchflogen werden",
  progressDrone.checkpointIndex === 1 && !H.reachedGoal(course, progressDrone),
  `Fortschritt ${progressDrone.checkpointIndex}/${course.checkpoints.length}`);

progressDrone.checkpointIndex = course.checkpoints.length;
progressDrone.x = course.goal.x + GOAL_RADIUS - 1;
progressDrone.y = course.goal.y;
check("Zielzone beendet einen vollstaendigen Flug", H.reachedGoal(course, progressDrone),
  `Zielradius ${GOAL_RADIUS}`);

// ---------------------------------------------------------- Referenzpilot

function flyReference(seed, difficulty) {
  const referenceCourse = H.createCourse(seed, difficulty);
  const drone = H.createDrone(referenceCourse);
  let status = "Zeitlimit";
  for (let frame = 0; frame < FLIGHT_LIMIT * 60; frame++) {
    const input = H.autopilotControl(drone, referenceCourse, difficulty);
    H.stepDrone(drone, input, referenceCourse, 1 / 60);
    H.updateCheckpoint(referenceCourse, drone);
    if (H.reachedGoal(referenceCourse, drone)) { status = "Ziel"; break; }
    if (H.collides(referenceCourse, drone)) { status = "Kollision"; break; }
    if (drone.battery <= 0) { status = "Akku"; break; }
  }
  return { status, drone };
}

let referenceSuccesses = 0;
let slowestReference = 0;
for (const difficulty of ["easy", "normal", "hard"]) {
  for (let seed = 1; seed <= 20; seed++) {
    const result = flyReference(seed * 3571, difficulty);
    if (result.status === "Ziel") referenceSuccesses++;
    slowestReference = Math.max(slowestReference, result.drone.elapsed);
  }
}
check("Referenzpilot absolviert 60 Zufallsstrecken", referenceSuccesses === 60,
  `${referenceSuccesses}/60 im Ziel`);
check("Referenzflug bleibt pausentauglich kurz", slowestReference < 20,
  `langsamster Flug ${slowestReference.toFixed(1)} s`);

// ---------------------------------------------------------- Wertung

const successful = { success: true, checkpoints: 4, battery: 65, time: 18, distance: WORLD_W };
const failed = { success: false, checkpoints: 3, battery: 80, time: 12, distance: 1180 };
const careful = { success: true, checkpoints: 4, battery: 78, time: 18, distance: WORLD_W };
check("Erfolgreicher Flug schlaegt Teilfortschritt",
  H.scoreRun(successful) > H.scoreRun(failed),
  `${H.scoreRun(successful)} gegen ${H.scoreRun(failed)} Punkte`);
check("Mehr Restakku verbessert die Wertung",
  H.scoreRun(careful) > H.scoreRun(successful),
  `${H.scoreRun(careful)} gegen ${H.scoreRun(successful)} Punkte`);

console.log("");
console.log(failures === 0 ? "ALLE PRUEFUNGEN GRUEN" : `${failures} PRUEFUNG(EN) ROT`);
process.exit(failures === 0 ? 0 : 1);
