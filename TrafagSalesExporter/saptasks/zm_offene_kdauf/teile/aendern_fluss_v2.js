// Ersetzt FLUSS_KURZ im TOP-Include durch die Fassung v2 (STUFE 00, Belegtyp, Kopf).
const fs = require('fs');
const f = 'LZKDAUF_PARTOP.abap';
let s = fs.readFileSync(f, 'utf8').replace(/\r\n/g, '\n');
const kopf = '*----------------------------------------------------------------------*\n* FLUSS_KURZ:';
const a = s.indexOf(kopf);
const formStart = s.indexOf('FORM fluss_kurz', a);
const ende = s.indexOf('ENDFORM.', formStart) + 'ENDFORM.'.length;
if (a < 0 || formStart < 0 || ende < formStart) throw new Error('Block nicht gefunden');
let neu = fs.readFileSync('teile/fg_fluss_formen_v2.abap', 'utf8').replace(/\r\n/g, '\n').replace(/^\n/, '').replace(/\n$/, '');
s = s.slice(0, a) + neu + s.slice(ende);
fs.writeFileSync(f, s);
console.log('ok', (s.match(/FORM fluss_kurz/g) || []).length);
