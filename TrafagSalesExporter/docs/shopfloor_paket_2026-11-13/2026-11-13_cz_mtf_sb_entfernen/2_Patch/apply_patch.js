#!/usr/bin/env node
// Shopfloor: Patches anwenden. Aufruf: node apply_patch.js <Ordner der Shopfloor-App mit app.js und style.css> [--check]
// Wendet alle patches/*.json der Reihe nach an. Jede Ersetzung muss genau 1x passen, sonst Abbruch ohne Änderung.
// Bereits angewendete Patches (Marker vorhanden) werden übersprungen. Vorher werden .bak-Kopien angelegt.
const fs = require("fs"), path = require("path");
const dir = process.argv[2]; const check = process.argv.includes("--check");
if (!dir) { console.error("Aufruf: node apply_patch.js <Ordner mit app.js> [--check]"); process.exit(2); }
const appP = path.join(dir, "app.js"), cssP = path.join(dir, "style.css");
let js = fs.readFileSync(appP, "utf8"), css = fs.readFileSync(cssP, "utf8");
const nlJ = js.includes("\r\n") ? "\r\n" : "\n", nlC = css.includes("\r\n") ? "\r\n" : "\n";
const files = fs.readdirSync(path.join(__dirname, "patches")).filter(f => f.endsWith(".json")).sort();
let applied = 0;
for (const f of files) {
  const p = JSON.parse(fs.readFileSync(path.join(__dirname, "patches", f), "utf8"));
  if (js.includes(p.marker)) { console.log("übersprungen (bereits drin): " + p.id); continue; }
  for (const x of p.app_js) {
    const o = x.old.replace(/\n/g, nlJ), n = x.new.replace(/\n/g, nlJ);
    const c = js.split(o).length - 1;
    if (c !== 1) { console.error(`ABBRUCH in ${p.id}: Stelle "${x.name}" ${c}x gefunden (erwartet 1). Nichts geändert.`); process.exit(1); }
    js = js.replace(o, () => n);
  }
  if (p.style_css) css += p.style_css.replace(/\n/g, nlC);
  console.log("ok: " + p.id + " – " + p.beschreibung); applied++;
}
try { new Function(js); } catch (e) { console.error("ABBRUCH: Syntaxfehler nach Patch: " + e.message); process.exit(1); }
if (check) { console.log("Prüfung ok, nichts geschrieben (--check)."); process.exit(0); }
if (!applied) { console.log("Nichts zu tun."); process.exit(0); }
fs.copyFileSync(appP, appP + ".bak"); fs.copyFileSync(cssP, cssP + ".bak");
fs.writeFileSync(appP, js, "utf8"); fs.writeFileSync(cssP, css, "utf8");
console.log(`Fertig: ${applied} Patch(es) angewendet. Backups: app.js.bak, style.css.bak. Browser-Cache leeren (Strg+F5).`);
