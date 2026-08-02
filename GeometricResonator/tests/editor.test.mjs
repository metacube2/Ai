// Prueft das Einrasten und den Treffertest.
//
// Ausfuehren:  node tests/editor.test.mjs
//
// Das Einrasten ist kein Komfortdetail: seit Verbundenheit aus der
// Geometrie folgt, ist es der Weg, auf dem Teile ueberhaupt zu einem
// Klangkoerper zusammenfinden. Deshalb wird hier bis zur Rasterung
// durchgeprueft - eine Einrastung, die zwar Kanten ausrichtet, aber keine
// gemeinsame Komponente erzeugt, waere wertlos.

import { snapOffset, hitTest, unionBounds, SNAP_TOLERANCE } from "../web/editor.js";
import { analyzeGeometry } from "../web/geometry.js";

let failures = 0;
let checks = 0;

function report(name, ok, detail) {
  checks += 1;
  if (!ok) failures += 1;
  console.log(`${ok ? "  ok  " : " FAIL "} ${name}${detail ? `\n         ${detail}` : ""}`);
}

const anchor = { id: 1, type: "rect", x: 100, y: 100, w: 120, h: 80 };

// Kante fast an Kante: es muss auf Kontakt einrasten.
{
  const moving = { id: 2, type: "rect", x: 226, y: 100, w: 100, h: 80 };
  const offset = snapOffset([moving], [anchor]);
  report(
    "Nahe Kante rastet auf Kontakt ein",
    offset.dx === -6 && offset.contactX,
    `dx=${offset.dx}, dy=${offset.dy}, Kontakt=${offset.contactX}`,
  );
}

// Ausserhalb der Toleranz darf nichts passieren.
{
  const moving = { id: 2, type: "rect", x: 260, y: 100, w: 100, h: 80 };
  const offset = snapOffset([moving], [anchor]);
  report(
    "Weit entfernte Kante rastet nicht ein",
    offset.dx === 0 && offset.dy === 0,
    `Abstand 40 px bei Toleranz ${SNAP_TOLERANCE}`,
  );
}

// Kontakt hat Vorrang vor blosser Ausrichtung: beide Kandidaten liegen in
// der Toleranz, die Kontaktkante muss gewinnen.
{
  const moving = { id: 2, type: "rect", x: 216, y: 100, w: 120, h: 80 };
  const offset = snapOffset([moving], [anchor]);
  report(
    "Kontakt hat Vorrang vor Ausrichtung",
    offset.dx === 4 && offset.contactX,
    `dx=${offset.dx} (Kontakt bei +4, Kantenflucht bei -116)`,
  );
}

// Beide Achsen zugleich.
{
  const moving = { id: 2, type: "rect", x: 224, y: 183, w: 90, h: 60 };
  const offset = snapOffset([moving], [anchor]);
  report(
    "Einrasten wirkt auf beiden Achsen",
    offset.dx === -4 && offset.dy === -3,
    `dx=${offset.dx}, dy=${offset.dy}`,
  );
}

// Eine bewegte Mehrfachauswahl rastet als Block ein.
{
  const a = { id: 2, type: "rect", x: 227, y: 100, w: 60, h: 40 };
  const b = { id: 3, type: "rect", x: 287, y: 140, w: 60, h: 40 };
  const offset = snapOffset([a, b], [anchor]);
  const box = unionBounds([a, b]);
  report(
    "Mehrfachauswahl rastet als Block ein",
    offset.dx === -7 && box.w === 120 && box.h === 80,
    `dx=${offset.dx}, Blockmass ${box.w}x${box.h}`,
  );
}

// Der eigentliche Zweck: nach dem Einrasten muss ein Koerper entstehen.
//
// Der Spalt muss dafuer breiter als eine Rasterzelle sein, sonst schliesst
// ihn die Rasterung schon von allein. Die Zellbreite waechst mit der
// Zeichnung, die Einrasttoleranz ist dagegen fest - bei grossen Entwuerfen
// verschmelzen nahe Teile deshalb ohnehin, bei kleinen erledigt es das
// Einrasten.
{
  const small = { id: 1, type: "rect", x: 100, y: 100, w: 100, h: 70 };
  const moving = { id: 2, type: "rect", x: 208, y: 100, w: 90, h: 70 };
  const before = analyzeGeometry([small, moving], 34);
  const offset = snapOffset([moving], [small]);
  const snapped = { ...moving, x: moving.x + offset.dx, y: moving.y + offset.dy };
  const after = analyzeGeometry([small, snapped], 34);
  report(
    "Nach dem Einrasten sind zwei Teile ein Klangkoerper",
    before.parts.length === 2 && after.parts.length === 1 && offset.dx === -8,
    `Spalt 8 px bei Zellbreite ${before.grid.spacing.toFixed(1)} px: vorher ${before.parts.length} Komponenten, nachher ${after.parts.length}`,
  );
}

// Treffertest.
{
  const objects = [
    { id: 1, type: "rect", x: 0, y: 0, w: 100, h: 100 },
    { id: 2, type: "rect", x: 50, y: 50, w: 100, h: 100 },
  ];
  report(
    "Treffertest liefert das oberste Objekt",
    hitTest(objects, { x: 75, y: 75 })?.id === 2
      && hitTest(objects, { x: 20, y: 20 })?.id === 1
      && hitTest(objects, { x: 400, y: 400 }) === null,
    "Ueberlappung, Einzelfall und Leerraum",
  );
}

// Negative Ausdehnung entsteht beim Aufziehen nach links oder oben.
{
  const dragged = { id: 5, type: "rect", x: 300, y: 300, w: -80, h: -60 };
  const box = unionBounds([dragged]);
  report(
    "Nach links aufgezogene Form wird korrekt vermessen",
    box.x === 220 && box.y === 240 && box.w === 80 && box.h === 60,
    `x=${box.x}, y=${box.y}, ${box.w}x${box.h}`,
  );
}

console.log(`\n${checks - failures}/${checks} Pruefungen bestanden.`);
process.exit(failures ? 1 : 0);
