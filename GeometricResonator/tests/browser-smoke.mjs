// Fuehrt tests/browser-smoke.html in einem echten Browser aus.
//
// Ausfuehren:  node tests/browser-smoke.mjs
//
// Die node-Tests decken Eigenloeser und Synthese ab. Drei Dinge gibt es
// aber nur im Browser: das Laden als ES-Modul, den Modul-Worker und den
// AudioWorklet. Diese Probe prueft genau die.
//
// Der Runner bringt einen eigenen Server mit, der die Dateien ausliefert
// und das Ergebnis der Seite per POST entgegennimmt. Ein blosses
// --dump-dom reicht nicht, weil die Ergebnisse asynchron entstehen, und
// --virtual-time-budget verfaelscht die Zeitmessung des Workers.

import { createServer } from "node:http";
import { spawn } from "node:child_process";
import { readFile, mkdtemp, rm } from "node:fs/promises";
import { fileURLToPath } from "node:url";
import { dirname, join, normalize, extname } from "node:path";
import { tmpdir } from "node:os";

const here = dirname(fileURLToPath(import.meta.url));
const root = join(here, "..");
const PORT = Number(process.env.SMOKE_PORT || 8794);
const TIMEOUT_MS = 90000;

const TYPES = {
  ".html": "text/html; charset=utf-8",
  ".js": "text/javascript; charset=utf-8",
  ".mjs": "text/javascript; charset=utf-8",
  ".json": "application/json",
  ".css": "text/css",
};

const CHROME_CANDIDATES = [
  process.env.CHROME,
  "/Applications/Google Chrome.app/Contents/MacOS/Google Chrome",
  "/Applications/Chromium.app/Contents/MacOS/Chromium",
  "/usr/bin/google-chrome",
  "/usr/bin/chromium",
  "/usr/bin/chromium-browser",
].filter(Boolean);

async function findChrome() {
  for (const candidate of CHROME_CANDIDATES) {
    try {
      await readFile(candidate);
      return candidate;
    } catch {
      try {
        const { constants, access } = await import("node:fs/promises");
        await access(candidate, constants.X_OK);
        return candidate;
      } catch { /* weiter suchen */ }
    }
  }
  return null;
}

let resolveResult;
const result = new Promise((resolve) => { resolveResult = resolve; });

const server = createServer(async (request, response) => {
  if (request.method === "POST" && request.url === "/__result") {
    const chunks = [];
    for await (const chunk of request) chunks.push(chunk);
    response.writeHead(204).end();
    try {
      resolveResult(JSON.parse(Buffer.concat(chunks).toString()));
    } catch (error) {
      resolveResult({ failures: 1, lines: [` FAIL  Ergebnis unlesbar: ${error.message}`] });
    }
    return;
  }
  const path = normalize(decodeURIComponent((request.url || "/").split("?")[0]));
  if (path.includes("..")) { response.writeHead(403).end(); return; }
  try {
    const body = await readFile(join(root, path));
    response.writeHead(200, {
      "Content-Type": TYPES[extname(path)] || "application/octet-stream",
      "Cache-Control": "no-store",
    });
    response.end(body);
  } catch {
    response.writeHead(404).end("nicht gefunden");
  }
});

const chrome = await findChrome();
if (!chrome) {
  console.log("Kein Chrome oder Chromium gefunden - Browserprobe uebersprungen.");
  console.log("Pfad ueber die Umgebungsvariable CHROME setzen, um sie auszufuehren.");
  process.exit(0);
}

await new Promise((resolve) => server.listen(PORT, "127.0.0.1", resolve));
const profile = await mkdtemp(join(tmpdir(), "resonator-smoke-"));
const child = spawn(chrome, [
  "--headless=new",
  "--disable-gpu",
  "--no-sandbox",
  "--no-first-run",
  "--no-default-browser-check",
  "--mute-audio",
  "--autoplay-policy=no-user-gesture-required",
  `--user-data-dir=${profile}`,
  `http://127.0.0.1:${PORT}/tests/browser-smoke.html`,
], { stdio: "ignore" });

const timeout = new Promise((resolve) => {
  setTimeout(() => resolve({
    failures: 1,
    lines: [` FAIL  Browserprobe: keine Antwort innerhalb von ${TIMEOUT_MS / 1000} s`],
  }), TIMEOUT_MS);
});

const outcome = await Promise.race([result, timeout]);

child.kill("SIGKILL");
server.close();
await rm(profile, { recursive: true, force: true }).catch(() => {});

for (const line of outcome.lines) console.log(line);
const total = outcome.lines.length;
console.log(`\n${total - outcome.failures}/${total} Pruefungen bestanden.`);
process.exit(outcome.failures ? 1 : 0);
