#!/usr/bin/env node
// Shopfloor: Konfiguration anpassen (Patch 04, 15.10.2026). Aufruf: node apply_config.js <Pfad zu shopfloor_config.json> [--check]
// Ändert: PPD Verzug MTS entfernt + Summe ohne MTS; Verlaufskacheln (trend) für SEH/TR5/TX/DW/PPD; Ampel-Felder pro Bereich;
// Lieferzeit-Grund (lz_grund) + Zielwerte (ziel, sobald in config_patch.js eingetragen); Forecast DW ohne DS/DW1/DL6, neu DS separat.
// Legt shopfloor_config.json.bak an. Mehrfach ausführen ist unschädlich (bereits vorhandene Felder werden übersprungen).
const fs = require("fs"); const patchConfig = require("./config_patch.js");
const p = process.argv[2]; const check = process.argv.includes("--check");
if (!p) { console.error("Aufruf: node apply_config.js <Pfad zu shopfloor_config.json> [--check]"); process.exit(2); }
const raw = fs.readFileSync(p, "utf8"); const nl = raw.includes("\r\n") ? "\r\n" : "\n";
const bom = raw.charCodeAt(0) === 0xFEFF ? "﻿" : "";
const c = JSON.parse(raw.replace(/^﻿/, ""));
const log = patchConfig(c);
log.forEach(l => console.log(" - " + l));
if (check) { console.log("Prüfung ok, nichts geschrieben (--check)."); process.exit(0); }
fs.copyFileSync(p, p + ".bak");
fs.writeFileSync(p, bom + JSON.stringify(c, null, 1).replace(/\n/g, nl), "utf8");
console.log("Fertig. Backup: " + p + ".bak. Dienst/App-Pool neu starten, falls die Konfiguration nur beim Start gelesen wird.");
