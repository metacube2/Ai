#!/usr/bin/env node
// Shopfloor Verkauf (01.11.2026, Entscheid F. Palma): Aufruf: node config_verkauf_fiori.js <Pfad zu shopfloor_config.json> [--check]
// - AE/Umsatz TR5 separat entfernt (nie befüllt): Transmitter = TR5 + TX zusammen (Produktsparte 0005)
// - «Geplanter Umsatz Monat (zc23)» wird «Erwarteter Umsatz Monat» aus Fiori «Umsatz prüfen» (weg von ZC23).
//   Feldschlüssel umsatz_plan bleibt, damit der Verlauf erhalten bleibt.
// Legt shopfloor_config.json.bak an. Mehrfach ausführen ist unschädlich.
const fs = require("fs");
const p = process.argv[2]; const check = process.argv.includes("--check");
if (!p) { console.error("Aufruf: node config_verkauf_fiori.js <Pfad zu shopfloor_config.json> [--check]"); process.exit(2); }
const raw = fs.readFileSync(p, "utf8"); const nl = raw.includes("\r\n") ? "\r\n" : "\n";
const bom = raw.charCodeAt(0) === 0xFEFF ? "﻿" : "";
const c = JSON.parse(raw.replace(/^﻿/, ""));
const v = (c.modules || []).find(m => m.key === "verkauf");
if (!v) { console.error("Modul verkauf nicht gefunden, nichts geändert."); process.exit(1); }
const log = []; const F = k => v.fields.find(f => f.key === k);
const n0 = v.fields.length; v.fields = v.fields.filter(f => f.key !== "ae_tr5" && f.key !== "u_tr5");
if (v.fields.length < n0) log.push("ae_tr5 / u_tr5 entfernt");
const ar = F("ae_rest"); if (ar) { ar.calc = "v.ae_aktuell - v.ae_tx - v.ae_dw"; log.push("ae_rest = " + ar.calc); }
const ur = F("u_rest"); if (ur) { ur.calc = "v.umsatz_ist - v.u_tx - v.u_dw"; log.push("u_rest = " + ur.calc); }
const ax = F("ae_tx"); if (ax) { ax.label = "AE Transmitter (TR5 + TX)"; }
const ux = F("u_tx"); if (ux) { ux.label = "Umsatz Transmitter (TR5 + TX)"; }
const up = F("umsatz_plan"); if (up) { up.label = "Erwarteter Umsatz Monat (Fiori Umsatz prüfen)"; up.planned_source = "sap"; log.push("umsatz_plan -> " + up.label); }
const ab = F("abw_plan"); if (ab) ab.label = "Abweichung IST zu erwartet";
const ep = F("erf_plan"); if (ep) ep.label = "Umsatz IST zu erwartet";
if (ep && !ep.unit) ep.unit = "%";
log.forEach(l => console.log(" - " + l));
if (check) { console.log("Prüfung ok, nichts geschrieben (--check)."); process.exit(0); }
fs.copyFileSync(p, p + ".bak");
fs.writeFileSync(p, bom + JSON.stringify(c, null, 1).replace(/\n/g, nl), "utf8");
console.log("Fertig. Backup: " + p + ".bak. Dienst/App-Pool neu starten, falls die Konfiguration nur beim Start gelesen wird.");
