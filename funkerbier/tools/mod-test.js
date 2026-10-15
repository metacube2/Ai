// Rendert public/funkerbier.mod offline (Node) nach /tmp-Ordner als WAV und prueft Pegel.
const fs = require('fs');
const path = require('path');
const { ModPlayer } = require('../public/mod-worklet.js');
const rate = 44100, sek = Number(process.argv[3] || 80);
const p = new ModPlayer(rate);
p.laden(fs.readFileSync(path.join(__dirname, '../public/funkerbier.mod')).buffer);
const l = new Float32Array(rate * sek), r = new Float32Array(rate * sek);
let zeilen = 0, noten = 0, positionen = new Set();
for (let a = 0; a < l.length; a += 128) {
  p.rendern(l, r, a, Math.min(a + 128, l.length), (pos, row, ev) => { zeilen++; noten += ev.length; positionen.add(pos); });
}
const pegel = [];
for (let s = 0; s < sek; s += 8) {
  let q = 0, n = 0, max = 0;
  for (let i = s * rate; i < Math.min((s + 8) * rate, l.length); i++) { q += l[i] * l[i]; n++; max = Math.max(max, Math.abs(l[i]) * 0.45); if (Number.isNaN(l[i])) throw new Error('NaN'); }
  pegel.push(`${s}s rms=${Math.sqrt(q / n * 0.2025).toFixed(3)} max=${max.toFixed(2)}`);
}
console.log(`zeilen=${zeilen} noten=${noten} positionen=${[...positionen].join(',')}`);
console.log(pegel.join('\n'));
const ziel = process.argv[2];
if (ziel) {
  const buf = Buffer.alloc(44 + l.length * 4);
  buf.write('RIFF', 0); buf.writeUInt32LE(36 + l.length * 4, 4); buf.write('WAVEfmt ', 8);
  buf.writeUInt32LE(16, 16); buf.writeUInt16LE(1, 20); buf.writeUInt16LE(2, 22); buf.writeUInt32LE(rate, 24);
  buf.writeUInt32LE(rate * 4, 28); buf.writeUInt16LE(4, 32); buf.writeUInt16LE(16, 34); buf.write('data', 36); buf.writeUInt32LE(l.length * 4, 40);
  for (let i = 0; i < l.length; i++) {
    buf.writeInt16LE(Math.max(-32768, Math.min(32767, Math.round(l[i] * 0.45 * 32767))), 44 + i * 4);
    buf.writeInt16LE(Math.max(-32768, Math.min(32767, Math.round(r[i] * 0.45 * 32767))), 46 + i * 4);
  }
  fs.writeFileSync(ziel, buf);
}
