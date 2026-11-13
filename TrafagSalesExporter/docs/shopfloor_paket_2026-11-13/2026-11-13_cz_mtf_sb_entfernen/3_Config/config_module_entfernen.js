#!/usr/bin/env node
// Shopfloor (13.11.2026, Entscheid F. Palma): Die Blätter «F1. CZ», «F2. Abteilung MTF» und «IN1. Sensorbestände (SB)»
// werden nicht mehr geführt. Das Skript entfernt die drei Module und ihre Agenda-Einträge aus shopfloor_config.json.
// Bereits erfasste Tageswerte in der Datenbank bleiben unberührt (sie werden nur nicht mehr angezeigt).
// Vorher Patch 08 auf app.js anwenden, sonst greifen PPA-Grafik und Forecast auf das fehlende Modul «cz» zu.
// Aufruf: node config_module_entfernen.js <Pfad zu shopfloor_config.json> [--check]
// Legt shopfloor_config.json.bak an. Mehrfach ausführen ist unschädlich.
const fs = require("fs");
const p = process.argv[2]; const check = process.argv.includes("--check");
if (!p) { console.error("Aufruf: node config_module_entfernen.js <Pfad zu shopfloor_config.json> [--check]"); process.exit(2); }
const raw = fs.readFileSync(p, "utf8"); const nl = raw.includes("\r\n") ? "\r\n" : "\n";
const bom = raw.charCodeAt(0) === 0xFEFF ? "﻿" : "";
const c = JSON.parse(raw.replace(/^﻿/, ""));
const log = entferne(c);
log.forEach(l => console.log(" - " + l));
if (!log.length) { console.log("Nichts zu tun (bereits entfernt)."); process.exit(0); }
if (check) { console.log("Prüfung ok, nichts geschrieben (--check)."); process.exit(0); }
fs.copyFileSync(p, p + ".bak");
fs.writeFileSync(p, bom + JSON.stringify(c, null, 1).replace(/\n/g, nl), "utf8");
console.log("Fertig. Backup: " + p + ".bak. Dienst/App-Pool neu starten, falls die Konfiguration nur beim Start gelesen wird.");

function entferne(c) {
  const WEG = ["cz", "mtf", "sensoren"];
  const log = [];
  if (!Array.isArray(c.modules)) { console.error("modules fehlt, nichts geändert."); process.exit(1); }
  const m0 = c.modules.length;
  c.modules.filter(m => WEG.includes(m.key)).forEach(m => log.push("Modul entfernt: " + m.key + " («" + m.title + "», " + (m.fields || []).length + " Felder)"));
  c.modules = c.modules.filter(m => !WEG.includes(m.key));
  if (Array.isArray(c.agenda)) {
    c.agenda.filter(a => WEG.includes(a.module)).forEach(a => log.push("Agenda-Eintrag entfernt: " + a.label));
    c.agenda = c.agenda.filter(a => !WEG.includes(a.module));
  }
  if (c.notify_rules) WEG.forEach(k => { if (c.notify_rules[k]) { delete c.notify_rules[k]; log.push("notify_rules entfernt: " + k); } });
  if (c.modules.length !== m0 - log.filter(l => l.startsWith("Modul entfernt")).length) { console.error("Zählfehler, nichts geändert."); process.exit(1); }
  return log;
}
