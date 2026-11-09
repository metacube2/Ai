#!/usr/bin/env node
// Shopfloor Patch 04: Terminverschiebungen aus Rückstandslisten ein- oder ausschalten.
// Aufruf: node tv_aus_rs_schalten.js <Pfad zu shopfloor_config.json> [--aus] [--check]
// Setzt beim Modul ppd "tv_from_rs": true (bzw. false mit --aus). Legt shopfloor_config.json.bak an.
const fs = require("fs");
const p = process.argv[2]; const aus = process.argv.includes("--aus"); const check = process.argv.includes("--check");
if (!p) { console.error("Aufruf: node tv_aus_rs_schalten.js <Pfad zu shopfloor_config.json> [--aus] [--check]"); process.exit(2); }
const raw = fs.readFileSync(p, "utf8"); const nl = raw.includes("\r\n") ? "\r\n" : "\n";
const bom = raw.charCodeAt(0) === 0xFEFF ? "﻿" : "";
const c = JSON.parse(raw.replace(/^﻿/, ""));
const ppd = (c.modules || []).find(m => m.key === "ppd");
if (!ppd) { console.error("Modul ppd nicht gefunden, nichts geändert."); process.exit(1); }
console.log(" - ppd.tv_from_rs: " + JSON.stringify(ppd.tv_from_rs) + " -> " + !aus);
ppd.tv_from_rs = !aus;
if (check) { console.log("Prüfung ok, nichts geschrieben (--check)."); process.exit(0); }
fs.copyFileSync(p, p + ".bak");
fs.writeFileSync(p, bom + JSON.stringify(c, null, 1).replace(/\n/g, nl), "utf8");
console.log("Fertig. Backup: " + p + ".bak. Dienst/App-Pool neu starten, falls die Konfiguration nur beim Start gelesen wird.");
