import fs from 'fs';
import path from 'path';
import { unstable_noStore as noStore } from 'next/cache';

/**
 * Designschalter der oeffentlichen Seite (2026-10-07).
 *
 * "turbo"     Turbo-Vision-Oberflaeche (DOS, Menueleiste, Fenster) - Standard
 * "klassisch" bisheriges Design mit Polarlicht, Drucker und Glaskarten
 *
 * Die Wahl liegt als kleine JSON-Datei neben der SQLite-DB, damit sie ohne
 * Neubau wirkt und Apache sie nicht ausliefert (/aiscom/database ist gesperrt).
 * Umschalten ueber /admin/design (nur per Tunnel erreichbar, mit Login).
 */
export type Design = 'turbo' | 'klassisch';
export const DESIGNS: Design[] = ['turbo', 'klassisch'];
const STANDARD: Design = 'turbo';

const DATEI = path.join(process.cwd(), '..', 'database', 'design.json');

export function aktuellesDesign(): Design {
  // Jede Seite, die das Design liest, wird pro Anfrage gerendert
  noStore();
  try {
    const wert = JSON.parse(fs.readFileSync(DATEI, 'utf-8'))?.design;
    return DESIGNS.includes(wert) ? wert : STANDARD;
  } catch {
    return STANDARD;
  }
}

export function designSetzen(design: Design): void {
  if (!DESIGNS.includes(design)) {
    throw new Error('unbekanntes_design');
  }
  fs.mkdirSync(path.dirname(DATEI), { recursive: true });
  const temp = `${DATEI}.tmp`;
  fs.writeFileSync(temp, JSON.stringify({ design, geaendert: new Date().toISOString() }, null, 2));
  fs.renameSync(temp, DATEI);
}
