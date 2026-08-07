// Kopfloser Test des MOD-Abspielers. Baut ein vollstaendiges ProTracker-Modul im
// Speicher, laesst es zerlegen und mischen und rechnet nach, ob wirklich der Ton
// herauskommt, der drinsteht - Tonhoehe ueber Nulldurchgaenge, Lautstaerke ueber den
// Effektivwert. Ohne Browser, ohne WebAudio.
//
// Aufruf:  node Tools/PauseGame.Probe/modprobe.mjs

import { parseMod, ModPlayer } from "../../wwwroot/js/modplayer.js";

let failures = 0;
function check(name, ok, detail) {
  console.log(`${ok ? "PASS" : "FAIL"}  ${name}  ->  ${detail}`);
  if (!ok) failures++;
}

const PAL_CLOCK = 7093789.2;
const PERIOD_C2 = 428;      // C-2 in der ProTracker-Tabelle
const PERIOD_A2 = 254;

// --------------------------------------------------- Testmodul bauen

function buildMod({ patternCount = 1, rows = [], sampleLoop = true } = {}) {
  const SAMPLE_LEN = 64;                       // eine Rechteckperiode
  const bytes = new Uint8Array(1084 + patternCount * 64 * 4 * 4 + SAMPLE_LEN);

  const put = (offset, text) => {
    for (let i = 0; i < text.length; i++) bytes[offset + i] = text.charCodeAt(i);
  };
  put(0, "TESTMODUL");

  // Sample 1: Rechteck, halbe Lautstaerke, schleifend.
  const s = 20;
  put(s, "square");
  bytes[s + 22] = 0; bytes[s + 23] = SAMPLE_LEN / 2;      // Laenge in Woertern
  bytes[s + 24] = 0;                                       // Finetune 0
  bytes[s + 25] = 32;                                      // Lautstaerke 32 von 64
  bytes[s + 26] = 0; bytes[s + 27] = 0;                    // Loop ab 0
  bytes[s + 28] = 0; bytes[s + 29] = sampleLoop ? SAMPLE_LEN / 2 : 0;

  bytes[950] = 1;              // Songlaenge
  bytes[951] = 127;
  for (let i = 0; i < 128; i++) bytes[952 + i] = 0;
  put(1080, "M.K.");

  // Musterdaten
  let p = 1084;
  for (let pat = 0; pat < patternCount; pat++) {
    for (let row = 0; row < 64; row++) {
      for (let ch = 0; ch < 4; ch++) {
        const spec = rows.find(r => r.pattern === pat && r.row === row && r.channel === ch);
        if (spec) {
          const sample = spec.sample ?? 0;
          const period = spec.period ?? 0;
          bytes[p] = (sample & 0xf0) | ((period >> 8) & 0x0f);
          bytes[p + 1] = period & 0xff;
          bytes[p + 2] = ((sample & 0x0f) << 4) | (spec.effect ?? 0);
          bytes[p + 3] = spec.param ?? 0;
        }
        p += 4;
      }
    }
  }

  // Sampledaten: Rechteck von +100 auf -100
  for (let i = 0; i < SAMPLE_LEN; i++) {
    bytes[p + i] = i < SAMPLE_LEN / 2 ? 100 : 256 - 100;
  }
  return bytes.buffer;
}

function renderSeconds(player, seconds, rate) {
  const frames = Math.floor(seconds * rate);
  const left = new Float32Array(frames);
  const right = new Float32Array(frames);
  player.render(left, right, frames);
  return { left, right, frames };
}

function rms(arr, from = 0, to = arr.length) {
  let sum = 0;
  for (let i = from; i < to; i++) sum += arr[i] * arr[i];
  return Math.sqrt(sum / Math.max(1, to - from));
}

function zeroCrossingHz(arr, rate, from, to) {
  let crossings = 0;
  for (let i = from + 1; i < to; i++) {
    if ((arr[i - 1] <= 0 && arr[i] > 0)) crossings++;
  }
  return (crossings * rate) / (to - from);
}

const RATE = 44100;

// --------------------------------------------------- Zerlegen

{
  const song = parseMod(buildMod({ rows: [{ pattern: 0, row: 0, channel: 0, sample: 1, period: PERIOD_C2 }] }));
  check("Kennung und Kanalzahl erkannt", song.tag === "M.K." && song.channels === 4,
    `${song.tag}, ${song.channels} Kanaele`);
  check("Titel gelesen", song.title === "TESTMODUL", song.title);
  check("31 Sampleplaetze gelesen", song.samples.length === 31, `${song.samples.length}`);
  check("Sample 1 hat Daten und die richtige Lautstaerke",
    song.samples[0].data && song.samples[0].length === 64 && song.samples[0].volume === 32,
    `${song.samples[0].length} Bytes, Lautstaerke ${song.samples[0].volume}`);
  check("Ein Muster mit 64 Zeilen zu 4 Kanaelen",
    song.patterns.length === 1 && song.patterns[0].length === 64 && song.patterns[0][0].length === 4,
    `${song.patterns.length} Muster`);
  const cell = song.patterns[0][0][0];
  check("Notenzelle richtig entschluesselt",
    cell.sample === 1 && cell.period === PERIOD_C2,
    `Sample ${cell.sample}, Periode ${cell.period}`);
}

// --------------------------------------------------- Tonhoehe

{
  const song = parseMod(buildMod({ rows: [{ pattern: 0, row: 0, channel: 0, sample: 1, period: PERIOD_C2 }] }));
  const player = new ModPlayer(song, RATE);
  const { left, frames } = renderSeconds(player, 0.5, RATE);

  check("Es kommt ueberhaupt Ton heraus", rms(left) > 0.02, `Effektivwert ${rms(left).toFixed(3)}`);

  // Das Sample ist EINE Rechteckperiode ueber 64 Bytes. Bei Periode 428 liest der
  // Abspieler mit PAL_CLOCK/(428*2) Hz, die hoerbare Grundfrequenz ist also
  // Lesefrequenz / 64.
  const expected = PAL_CLOCK / (PERIOD_C2 * 2) / 64;
  const measured = zeroCrossingHz(left, RATE, 2000, frames);
  check("Tonhoehe stimmt mit der Periode ueberein",
    Math.abs(measured - expected) / expected < 0.03,
    `gemessen ${measured.toFixed(1)} Hz, erwartet ${expected.toFixed(1)} Hz`);

  const songHigh = parseMod(buildMod({ rows: [{ pattern: 0, row: 0, channel: 0, sample: 1, period: PERIOD_A2 }] }));
  const high = new ModPlayer(songHigh, RATE);
  const r2 = renderSeconds(high, 0.5, RATE);
  const measuredHigh = zeroCrossingHz(r2.left, RATE, 2000, r2.frames);
  check("Kleinere Periode klingt hoeher", measuredHigh > measured * 1.5,
    `${measuredHigh.toFixed(1)} Hz gegen ${measured.toFixed(1)} Hz`);
}

// --------------------------------------------------- Lautstaerke und Effekte

{
  // C00 setzt die Lautstaerke auf 0 - danach muss Stille herrschen.
  const song = parseMod(buildMod({
    rows: [
      { pattern: 0, row: 0, channel: 0, sample: 1, period: PERIOD_C2 },
      { pattern: 0, row: 8, channel: 0, effect: 0xc, param: 0 },
    ],
  }));
  const player = new ModPlayer(song, RATE);
  const { left } = renderSeconds(player, 1.5, RATE);
  // Bei 125 BPM und Speed 6 dauert eine Zeile 6 * 2.5/125 s = 0.12 s.
  const rowSamples = Math.round(0.12 * RATE);
  const before = rms(left, rowSamples * 2, rowSamples * 6);
  const after = rms(left, rowSamples * 10, rowSamples * 12);
  check("Effekt C00 schaltet den Kanal stumm", before > 0.02 && after < 0.001,
    `vorher ${before.toFixed(4)}, nachher ${after.toFixed(4)}`);
}

{
  // F-Effekt setzt das Tempo. Bei doppelter Geschwindigkeit muss der Stummschalt-
  // punkt aus dem vorigen Test halb so spaet kommen.
  const song = parseMod(buildMod({
    rows: [
      { pattern: 0, row: 0, channel: 0, sample: 1, period: PERIOD_C2, effect: 0xf, param: 3 },
      { pattern: 0, row: 8, channel: 0, effect: 0xc, param: 0 },
    ],
  }));
  const player = new ModPlayer(song, RATE);
  const { left } = renderSeconds(player, 1.5, RATE);
  const rowSamples = Math.round(0.06 * RATE);           // Speed 3 statt 6
  const after = rms(left, rowSamples * 10, rowSamples * 12);
  check("Effekt F setzt die Geschwindigkeit", player.speed === 3 && after < 0.001,
    `Speed ${player.speed}, Effektivwert nach Zeile 8: ${after.toFixed(4)}`);
}

{
  // Zwei Kanaele gleichzeitig, hart nach links und rechts verteilt.
  const song = parseMod(buildMod({
    rows: [
      { pattern: 0, row: 0, channel: 0, sample: 1, period: PERIOD_C2 },
      { pattern: 0, row: 0, channel: 1, sample: 1, period: PERIOD_A2 },
    ],
  }));
  const player = new ModPlayer(song, RATE);
  const { left, right } = renderSeconds(player, 0.4, RATE);
  check("Beide Kanaele klingen", rms(left) > 0.02 && rms(right) > 0.02,
    `links ${rms(left).toFixed(3)}, rechts ${rms(right).toFixed(3)}`);

  // Panorama nur mit EINEM klingenden Kanal pruefen. Kanal 1 liegt links, Kanal 2
  // rechts - laufen beide, heben sich die Seiten in der Summe gerade auf und der
  // Test kann grundsaetzlich nichts zeigen (erst so gemessen, dann korrigiert).
  const onlyLeftCh = parseMod(buildMod({
    rows: [{ pattern: 0, row: 0, channel: 0, sample: 1, period: PERIOD_C2 }],
  }));
  const solo = new ModPlayer(onlyLeftCh, RATE);
  const s1 = renderSeconds(solo, 0.4, RATE);
  check("Kanal 1 liegt hoerbar links", rms(s1.left) > rms(s1.right) * 1.8,
    `links ${rms(s1.left).toFixed(3)} gegen rechts ${rms(s1.right).toFixed(3)}`);

  const onlyRightCh = parseMod(buildMod({
    rows: [{ pattern: 0, row: 0, channel: 1, sample: 1, period: PERIOD_C2 }],
  }));
  const solo2 = new ModPlayer(onlyRightCh, RATE);
  const s2 = renderSeconds(solo2, 0.4, RATE);
  check("Kanal 2 liegt hoerbar rechts", rms(s2.right) > rms(s2.left) * 1.8,
    `rechts ${rms(s2.right).toFixed(3)} gegen links ${rms(s2.left).toFixed(3)}`);
}

// --------------------------------------------------- Dauerlauf

{
  const song = parseMod(buildMod({
    rows: [
      { pattern: 0, row: 0, channel: 0, sample: 1, period: PERIOD_C2 },
      { pattern: 0, row: 32, channel: 0, sample: 1, period: PERIOD_A2 },
    ],
  }));
  const player = new ModPlayer(song, RATE);
  // 30 Sekunden am Stueck: das Modul ist 64 Zeilen lang, es muss also mehrfach
  // umlaufen, ohne haengen zu bleiben oder still zu werden.
  const { left } = renderSeconds(player, 30, RATE);
  let bad = 0;
  for (let i = 0; i < left.length; i++) {
    if (!Number.isFinite(left[i]) || Math.abs(left[i]) > 1.001) bad++;
  }
  check("30 Sekunden Dauerlauf ohne Aussetzer", player.loops >= 3 && rms(left) > 0.02,
    `${player.loops} Durchlaeufe, Effektivwert ${rms(left).toFixed(3)}`);
  check("Keine ungueltigen oder uebersteuerten Werte", bad === 0, `${bad} Ausreisser`);
}

// --------------------------------------------------- Robustheit

{
  let threw = false;
  try { parseMod(new Uint8Array(200).buffer); } catch (e) { threw = true; }
  check("Zu kurze Datei wird abgelehnt", threw, "Ausnahme geworfen");

  // Zufallsdaten in Moduldateigroesse: darf nicht abstuerzen. Ein Anwender kann
  // jede beliebige Datei auswaehlen, und ein Absturz wuerde die Seite mitnehmen.
  const junk = new Uint8Array(20000);
  for (let i = 0; i < junk.length; i++) junk[i] = (i * 137) & 0xff;
  let ok = true;
  try {
    const song = parseMod(junk.buffer);
    const player = new ModPlayer(song, RATE);
    renderSeconds(player, 0.3, RATE);
  } catch (e) {
    ok = /zu kurz|Datei/i.test(String(e.message));
  }
  check("Beliebige Datei stuerzt nicht ab", ok, "sauber behandelt");
}

console.log("");
console.log(failures === 0 ? "ALLE PRUEFUNGEN GRUEN" : `${failures} PRUEFUNG(EN) ROT`);
process.exit(failures === 0 ? 0 : 1);
