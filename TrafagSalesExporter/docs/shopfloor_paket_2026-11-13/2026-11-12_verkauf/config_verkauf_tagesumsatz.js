#!/usr/bin/env node
// Shopfloor Verkauf (12.11.2026): «Tagesumsatz» am Monatswechsel korrigieren.
// Bisher: v.umsatz_ist - prev('umsatz_ist'). Umsatz IST ist kumuliert pro Monat; am ersten Tag des neuen Monats
// wurde deshalb der ganze Vormonat abgezogen (Beispiel 02.10.2026: 1'140'280 - 7'878'114 = -6'737'834).
// Neu: fällt der kumulierte Wert unter die Hälfte des Vorwerts, beginnt ein neuer Monat -> Tagesumsatz = Umsatz IST.
// Aufruf: node config_verkauf_tagesumsatz.js <Pfad zu shopfloor_config.json> [--check]
// Legt shopfloor_config.json.bak an. Mehrfach ausführen ist unschädlich.
const fs = require("fs");
const p = process.argv[2]; const check = process.argv.includes("--check");
if (!p) { console.error("Aufruf: node config_verkauf_tagesumsatz.js <Pfad zu shopfloor_config.json> [--check]"); process.exit(2); }
const raw = fs.readFileSync(p, "utf8"); const nl = raw.includes("\r\n") ? "\r\n" : "\n";
const bom = raw.charCodeAt(0) === 0xFEFF ? "\uFEFF" : "";
const c = JSON.parse(raw.replace(/^\uFEFF/, ""));
const v = (c.modules || []).find(m => m.key === "verkauf");
const f = v && (v.fields || []).find(x => x.key === "tagesumsatz");
if (!f) { console.error("Feld verkauf.tagesumsatz nicht gefunden, nichts geändert."); process.exit(1); }
const NEU = "(p => v.umsatz_ist < p * 0.5 ? v.umsatz_ist : v.umsatz_ist - p)(prev('umsatz_ist'))";
if (f.calc === NEU) { console.log("Nichts zu tun (bereits umgestellt)."); process.exit(0); }
console.log(" - verkauf.tagesumsatz: " + JSON.stringify(f.calc) + " -> " + JSON.stringify(NEU));
f.calc = NEU;
if (check) { console.log("Prüfung ok, nichts geschrieben (--check)."); process.exit(0); }
fs.copyFileSync(p, p + ".bak");
fs.writeFileSync(p, bom + JSON.stringify(c, null, 1).replace(/\n/g, nl), "utf8");
console.log("Fertig. Backup: " + p + ".bak. Dienst/App-Pool neu starten, falls die Konfiguration nur beim Start gelesen wird.");
