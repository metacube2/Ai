#!/usr/bin/env node
// Shopfloor Logistik (04.11.2026, Entscheid F. Palma): SOLL Verpacken DW = Lieferbereite Tagesmenge DLQ72 (statt DL790).
// DLQ72 ist der Arbeitsplatz vor der Verpackung MLE04; was dort am Tag gemeldet wird, soll die Logistik am gleichen Tag verpacken.
// Aufruf: node config_logistik_dw_soll.js <Pfad zu shopfloor_config.json> [--check]
// Legt shopfloor_config.json.bak an. Mehrfach ausführen ist unschädlich.
const fs = require("fs");
const p = process.argv[2]; const check = process.argv.includes("--check");
if (!p) { console.error("Aufruf: node config_logistik_dw_soll.js <Pfad zu shopfloor_config.json> [--check]"); process.exit(2); }
const raw = fs.readFileSync(p, "utf8"); const nl = raw.includes("\r\n") ? "\r\n" : "\n";
const bom = raw.charCodeAt(0) === 0xFEFF ? "﻿" : "";
const c = JSON.parse(raw.replace(/^﻿/, ""));
const lg = (c.modules || []).find(m => m.key === "logistik");
const f = lg && (lg.fields || []).find(x => x.key === "dw_soll");
if (!f) { console.error("logistik.dw_soll nicht gefunden, nichts geändert."); process.exit(1); }
console.log(" - logistik.dw_soll: " + f.calc + " -> ref('dw','lieferbereit')");
f.calc = "ref('dw','lieferbereit')"; f.label = "DW SOLL Verpacken (lieferbereit DLQ72)";
if (check) { console.log("Prüfung ok, nichts geschrieben (--check)."); process.exit(0); }
fs.copyFileSync(p, p + ".bak");
fs.writeFileSync(p, bom + JSON.stringify(c, null, 1).replace(/\n/g, nl), "utf8");
console.log("Fertig. Backup: " + p + ".bak. Dienst/App-Pool neu starten, falls die Konfiguration nur beim Start gelesen wird.");
