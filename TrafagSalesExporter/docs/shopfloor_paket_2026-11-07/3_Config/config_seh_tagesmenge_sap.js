#!/usr/bin/env node
// Shopfloor SEH (07.11.2026, Entscheid F. Palma): «Offene Tagesmenge / Tagesziel» kommt aus SAP (ZC22, Bucket «Tag»).
// Setzt beim Feld planned_source = "sap" (SAP-Badge; nach Lieferung mit Patch 07 schreibgeschützt).
// Aufruf: node config_seh_tagesmenge_sap.js <Pfad zu shopfloor_config.json> [--check]
const fs = require("fs");
const p = process.argv[2]; const check = process.argv.includes("--check");
if (!p) { console.error("Aufruf: node config_seh_tagesmenge_sap.js <Pfad zu shopfloor_config.json> [--check]"); process.exit(2); }
const raw = fs.readFileSync(p, "utf8"); const nl = raw.includes("\r\n") ? "\r\n" : "\n";
const bom = raw.charCodeAt(0) === 0xFEFF ? "﻿" : "";
const c = JSON.parse(raw.replace(/^﻿/, ""));
const seh = (c.modules || []).find(m => m.key === "seh");
const f = seh && (seh.fields || []).find(x => x.key === "ziel" || /Offene Tagesmenge/i.test(x.label || ""));
if (!f) { console.error("SEH-Feld «Offene Tagesmenge» nicht gefunden, nichts geändert."); process.exit(1); }
console.log(" - seh." + f.key + " («" + f.label + "»): planned_source " + JSON.stringify(f.planned_source) + " -> \"sap\"");
f.planned_source = "sap";
if (check) { console.log("Prüfung ok, nichts geschrieben (--check)."); process.exit(0); }
fs.copyFileSync(p, p + ".bak");
fs.writeFileSync(p, bom + JSON.stringify(c, null, 1).replace(/\n/g, nl), "utf8");
console.log("Fertig. Backup: " + p + ".bak");
