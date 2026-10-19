// Word-Doku: ZC23 Materialanteil, Beschleunigung von 47,7 s auf 13,5 s (2026-10-19)
const fs = require('fs');
const {
  Document, Packer, Paragraph, TextRun, HeadingLevel, Table, TableRow, TableCell, WidthType, ShadingType,
  BorderStyle, AlignmentType, LevelFormat, Footer, PageNumber, TableOfContents
} = require('docx');

const RED = 'A6192E';
const GREY = '595959';
const FONT = 'Arial';
const MONO = 'Consolas';

const p = (text, opts = {}) => new Paragraph({ spacing: { after: 120 }, ...opts, children: runs(text) });
function runs(text) {
  // **fett** und `code` im Fliesstext
  const out = [];
  const re = /(\*\*[^*]+\*\*|`[^`]+`)/g;
  let last = 0, m;
  while ((m = re.exec(text)) !== null) {
    if (m.index > last) out.push(new TextRun({ text: text.slice(last, m.index), font: FONT, size: 21 }));
    const t = m[0];
    if (t.startsWith('**')) out.push(new TextRun({ text: t.slice(2, -2), bold: true, font: FONT, size: 21 }));
    else out.push(new TextRun({ text: t.slice(1, -1), font: MONO, size: 19 }));
    last = m.index + t.length;
  }
  if (last < text.length) out.push(new TextRun({ text: text.slice(last), font: FONT, size: 21 }));
  return out;
}
const h1 = t => new Paragraph({ heading: HeadingLevel.HEADING_1, children: [new TextRun({ text: t })] });
const h2 = t => new Paragraph({ heading: HeadingLevel.HEADING_2, children: [new TextRun({ text: t })] });
const bullet = t => new Paragraph({ numbering: { reference: 'pkt', level: 0 }, spacing: { after: 60 }, children: runs(t) });
const num = (t, ref) => new Paragraph({ numbering: { reference: ref, level: 0 }, spacing: { after: 60 }, children: runs(t) });

function code(lines) {
  return lines.map((l, i) => new Paragraph({
    spacing: { before: i === 0 ? 60 : 0, after: i === lines.length - 1 ? 160 : 0, line: 240 },
    shading: { type: ShadingType.CLEAR, color: 'auto', fill: 'F2F2F2' },
    indent: { left: 200 },
    children: [new TextRun({ text: l.length ? l : ' ', font: MONO, size: 17 })]
  }));
}

const border = { style: BorderStyle.SINGLE, size: 4, color: 'BFBFBF' };
const borders = { top: border, bottom: border, left: border, right: border };
function table(widths, rows, headFill = RED) {
  const total = widths.reduce((a, b) => a + b, 0);
  return new Table({
    width: { size: total, type: WidthType.DXA },
    columnWidths: widths,
    rows: rows.map((r, ri) => new TableRow({
      tableHeader: ri === 0,
      children: r.map((c, ci) => new TableCell({
        width: { size: widths[ci], type: WidthType.DXA },
        borders,
        shading: ri === 0 ? { type: ShadingType.CLEAR, color: 'auto', fill: headFill } : undefined,
        margins: { top: 60, bottom: 60, left: 100, right: 100 },
        children: [new Paragraph({
          alignment: ci > 0 && /^[\d'.,\s]+(s|%)?$/.test(c) ? AlignmentType.RIGHT : AlignmentType.LEFT,
          children: ri === 0
            ? [new TextRun({ text: c, bold: true, color: 'FFFFFF', font: FONT, size: 19 })]
            : runs(c).map(x => x)
        })]
      }))
    }))
  });
}

const children = [
  new Paragraph({ spacing: { after: 60 }, children: [new TextRun({ text: 'ZC23 Materialanteil', font: FONT, size: 44, bold: true, color: RED })] }),
  new Paragraph({ spacing: { after: 60 }, children: [new TextRun({ text: 'Beschleunigung von 47,7 s auf 13,5 s', font: FONT, size: 30, color: GREY })] }),
  new Paragraph({
    spacing: { after: 300 },
    border: { bottom: { style: BorderStyle.SINGLE, size: 8, color: RED, space: 4 } },
    children: [new TextRun({ text: 'Technische Dokumentation · Programm ZM_OFFENE_KDAUF · Stand 19.10.2026 · Ingo Kohler, SAP', font: FONT, size: 18, color: GREY })]
  }),
  new TableOfContents('Inhalt', { hyperlink: true, headingStyleRange: '1-2' }),

  h1('1. Ausgangslage'),
  p('ZC23 (`ZM_OFFENE_KDAUF`) zeigt die offenen Kundenaufträge mit dem **Standardwert** (Standardpreis × Menge). Seit dem 15.10.2026 steht daneben der **Materialanteil**: der Teil der Herstellkosten, der auf eingekauftes Material entfällt, gerechnet wie die Preisliste Tschechien (ZM03), sowie die **Arbeit CZ** (Eigenleistung an den Arbeitsplätzen `CZ*`). Transport `T76K912736`.'),
  p('Für den Materialanteil muss das Programm je Material die Standardkalkulation lesen und Eigenfertigung über alle Stücklistenstufen auflösen. Die erste Fassung tat das korrekt, aber zu langsam: Ein Quartal brauchte im Dialog fast eine Minute statt rund fünf Sekunden.'),

  h1('2. Ergebnis'),
  table([4200, 2600, 2226], [
    ['Lauf (T76, Q2 2026, Werk 1100, Dialog)', 'Dauer', 'Faktor'],
    ['ZC23 vor dem Ausbau (ohne Materialanteil, ohne Fortschreibung)', '5,1 s', ''],
    ['Erste Fassung, mit Materialanteil und Fortschreibung', '47,7 s', '1,0'],
    ['**Schnelle Fassung**, mit Materialanteil und Fortschreibung', '**13,5 s**', '3,5 × schneller'],
    ['Schnelle Fassung, Schalter `P_MATA` aus, ohne Fortschreibung', '7,5 s', '']
  ]),
  p(''),
  p('**Die Werte sind in beiden Fassungen gleich:** 25 Stichproben in `ZPOWERBI_KDAUF` stimmen auf den Rappen überein, und der Vergleich mit dem unveränderten Programm (4\'392 Zeilen Q2, alle Felder) ergab keine Abweichung. Geändert hat sich nur, **wie oft und wie** das Programm auf die Datenbank zugreift.'),

  h1('3. Warum die erste Fassung langsam war'),
  p('Die erste Fassung arbeitete **Material für Material** und innerhalb jeder Kalkulation **Position für Position**. Je Auftragsposition wurde die Kalkulation des Materials gesucht, dann rekursiv jede Stufe einzeln gelesen. Ein Puffer je Kalkulation verhinderte zwar doppeltes Rechnen, aber jede neue Kalkulation kostete eigene Datenbankzugriffe, und die Beschaffungsart wurde für jede Materialposition einzeln aus MARC geholt.'),
  ...code([
    '* Erste Fassung (gekürzt): je Kalkulation einzeln, je Position ein MARC-Zugriff',
    'SELECT SINGLE losgr werks FROM keko INTO (lv_losgr, lv_werks)',
    '  WHERE bzobj = \'0\' AND kalnr = iv_kalnr AND kalka = iv_kalka ...',
    'SELECT typps matnr werks menge wrtfw_kpf ukaln ... FROM ckis INTO TABLE lt_pos',
    '  WHERE lednr = \'00\' AND kalnr = iv_kalnr ...',
    'LOOP AT lt_pos INTO ls_pos WHERE wrtfw_kpf <> 0.',
    '  SELECT SINGLE beskz FROM marc INTO lv_beskz            " je Position!',
    '    WHERE matnr = ls_pos-matnr AND werks = ls_pos-werks.',
    '  IF ... eigengefertigt ...',
    '    PERFORM mata_kalkulation USING ls_pos-ukaln ...       " Rekursion: wieder SELECTs',
    '  ENDIF.',
    'ENDLOOP.'
  ]),
  p('Bei einigen tausend Materialien mit mehreren Stufen ergibt das **zehntausende kleine Zugriffe**. Jeder einzelne ist schnell, aber jeder kostet einen Rundlauf zwischen Anwendungsserver und Datenbank. Die Menge macht die Laufzeit.'),

  h1('4. Wie die schnelle Fassung arbeitet'),
  p('Die schnelle Fassung trennt **Laden** und **Rechnen**. Zuerst werden alle nötigen Daten stufenweise und gesammelt in interne Tabellen geladen, danach wird nur noch im Speicher gerechnet. Quelltext: `saptasks/zm_offene_kdauf/teile/opt_mata_forms.abap`, Formen `MATA_LADEN`, `MATA_WERT`, `MATA_IST_MATERIAL`.'),

  h2('4.1 Kopfkalkulationen aller Materialien mit einem Zugriff'),
  p('Statt je Material ein `SELECT … UP TO 1 ROWS` liest ein einziger `FOR ALL ENTRIES` die Standardkalkulationen aller Materialien der ZC23-Liste. Welche Kalkulation gilt (freigegeben und heute gültig oder die jüngste wie ZM03), wird danach im Speicher mit `SORT` und `DELETE ADJACENT DUPLICATES` entschieden.'),
  ...code([
    'SELECT matnr kalnr kalka kadky tvers bwvar kadat freig feh_sta',
    '  FROM keko INTO TABLE lt_keko',
    '  FOR ALL ENTRIES IN lt_matnr                " alle Listenmaterialien auf einmal',
    '  WHERE matnr = lt_matnr-matnr AND werks = p_werk',
    '    AND kalka = \'01\' AND tvers = \'01\' AND loekz = space.',
    'IF p_mfrei = abap_true.                     " freigegebene, heute gültig',
    '  DELETE lt_keko WHERE freig <> \'X\' OR kadat > sy-datum.',
    '  SORT lt_keko BY matnr ASCENDING kadat DESCENDING kadky DESCENDING.',
    'ELSE.                                       " jüngste wie ZM03',
    '  DELETE lt_keko WHERE feh_sta = \'KF\' OR kadky > sy-datum.',
    '  SORT lt_keko BY matnr ASCENDING kadky DESCENDING.',
    'ENDIF.',
    'DELETE ADJACENT DUPLICATES FROM lt_keko COMPARING matnr.'
  ]),

  h2('4.2 Stufenweise statt materialweise'),
  p('Die Stücklistenauflösung läuft **je Stufe**, nicht je Material. In jeder Stufe werden die Köpfe und Positionen aller offenen Kalkulationen mit je einem Zugriff gelesen, dazu die Beschaffungsart nur der Materialien, die noch nicht bekannt sind. Die Unterkalkulationen der Eigenfertigung bilden die nächste Stufe. Höchstens 30 Stufen.'),
  ...code([
    'DO 30 TIMES.',
    '  IF lt_offen IS INITIAL. EXIT. ENDIF.',
    '  SELECT ... FROM keko INTO TABLE lt_kopf FOR ALL ENTRIES IN lt_offen ...  " Köpfe',
    '  SELECT ... FROM ckis INTO TABLE lt_pos  FOR ALL ENTRIES IN lt_offen ...  " Positionen',
    '  DELETE lt_pos WHERE wrtfw_kpf = 0.                       " wertlose sofort weg',
    '  SELECT matnr werks beskz FROM marc INTO TABLE lt_beskz',
    '    FOR ALL ENTRIES IN lt_mw ...                    " nur noch unbekannte Materialien',
    '  " nächste Stufe: Unterkalkulationen der Eigenfertigung, die noch nicht geladen sind',
    '  ... INSERT ls_key INTO TABLE lt_neu.',
    '  lt_offen = lt_neu.',
    'ENDDO.'
  ]),
  p('Pro Stufe sind es damit **drei Zugriffe**, unabhängig davon, wie viele Materialien die Stufe hat. Bei einer üblichen Tiefe von vier bis sechs Stufen kommen insgesamt rund 15 bis 20 Datenbankzugriffe zusammen statt zehntausender.'),

  h2('4.3 Jede Kalkulation und jede Beschaffungsart nur einmal'),
  bullet('Ein Schlüssel, der schon geladen ist, kommt nicht wieder in die nächste Stufe. Ein Halbfabrikat, das in hundert Geräten steckt, wird nur einmal gelesen.'),
  bullet('Die Beschaffungsart je Material und Werk liegt in `gt_mata_beskz` und wird nur für neue Materialien aus MARC geholt.'),

  h2('4.4 Rechnen nur im Speicher, mit Puffer'),
  p('Die Rekursion über die Stufen (`MATA_WERT`) liest danach nur noch die internen Tabellen `gt_mata_kopf` und `gt_mata_pos`. Das Ergebnis je Kalkulation landet im Puffer `gt_mata_kalk`; wird dieselbe Kalkulation wieder gebraucht, steht der Wert sofort bereit. Vor dem Rechnen wird die Kalkulation mit 0 eingetragen, das schützt vor Zyklen in der Kalkulationsstruktur.'),
  ...code([
    'READ TABLE gt_mata_kalk INTO ls_kalk',
    '     WITH TABLE KEY kalnr = iv_kalnr kalka = iv_kalka kadky = iv_kadky',
    '                    tvers = iv_tvers bwvar = iv_bwvar.',
    'IF sy-subrc = 0.',
    '  cv_wert = ls_kalk-wert.  cv_cz = ls_kalk-cz.  RETURN.     " schon gerechnet',
    'ENDIF.',
    'INSERT ls_kalk INTO TABLE gt_mata_kalk.                 " vorab mit 0: Zyklenschutz'
  ]),

  h2('4.5 Je Material statt je Auftragsposition'),
  p('Der Materialanteil gehört zum Material, nicht zur Position. Gerechnet wird deshalb je Material einmal; auf die rund 4\'400 Auftragspositionen wird am Schluss nur noch mit der Menge verteilt.'),
  ...code([
    'LOOP AT gt_uebauf ASSIGNING <ls_ueb>.',
    '  READ TABLE gt_mata_mat INTO ls_mat WITH TABLE KEY matnr = <ls_ueb>-matnr.',
    '  CHECK sy-subrc = 0.',
    '  <ls_ueb>-matpreis  = ls_mat-wert.',
    '  <ls_ueb>-matanteil = ls_mat-wert * <ls_ueb>-bmeng.',
    '  <ls_ueb>-arbeit_cz = ls_mat-cz   * <ls_ueb>-bmeng.',
    'ENDLOOP.'
  ]),

  h2('4.6 Schalter für Anwender ohne Materialanteil'),
  p('Im Selektionsbild steht `P_MATA` „Materialanteil rechnen“, Vorgabe **ein**. Wer die Spalten nicht braucht, schaltet ihn aus; dann entfällt das Laden ganz, und ZC23 ist so schnell wie vorher (7,5 s inklusive Fortschreibung). Gespeicherte Varianten wie `BI UPDATE` kennen das Feld nicht und übernehmen die Vorgabe, Power BI bekommt den Wert also weiter.'),

  h1('5. Übersicht der Zugriffe'),
  table([3000, 3013, 3013], [
    ['Zugriff', 'Erste Fassung', 'Schnelle Fassung'],
    ['KEKO Kopfkalkulation je Material', 'je Material ein SELECT', 'ein FOR ALL ENTRIES für alle'],
    ['KEKO Losgrösse, Werk', 'je Kalkulation ein SELECT SINGLE', 'je Stufe ein FOR ALL ENTRIES'],
    ['CKIS Positionen', 'je Kalkulation ein SELECT', 'je Stufe ein FOR ALL ENTRIES'],
    ['MARC Beschaffungsart', 'je Materialposition ein SELECT SINGLE', 'je Stufe ein FOR ALL ENTRIES, nur neue Materialien'],
    ['Rechnen', 'zwischen den Zugriffen, rekursiv', 'nur im Speicher, gepuffert']
  ]),

  h1('6. Grenzen und weitere Möglichkeiten'),
  bullet('Die restlichen rund 6 s für den Materialanteil entfallen vor allem auf CKIS: Die Tabelle hat je Kalkulation viele Positionen, und `FOR ALL ENTRIES` zerlegt die Abfrage intern in Pakete.'),
  bullet('Noch schneller ginge es mit einem Join oder einer CDS-Sicht über KEKO und CKIS. Das würde die Struktur stärker umbauen und war am 15.10.2026 nicht nötig.'),
  bullet('Fachliche Vereinfachungen wie in ZM03: Positionsmenge und Mengeneinheit der Unterkalkulation werden nicht umgerechnet; der Standardwert nutzt den Standardpreis ohne Preiseinheit (bei den geprüften Materialien ist die Preiseinheit 1).'),
  bullet('Gespeicherte ALV-Layouts blenden neue Spalten aus. Wer die Spalten sehen will, blendet sie im eigenen Layout einmal ein.'),

  h1('7. Nachweise und Ablage'),
  table([3300, 5726], [
    ['Was', 'Wo'],
    ['Quelltext schnelle Fassung', '`saptasks/zm_offene_kdauf/teile/opt_mata_forms.abap`'],
    ['Quelltext erste Fassung', 'Git-Commit `bed8097`, gleiche Datei'],
    ['Messungen und Prüfprotokoll', '`saptasks/zc23_materialanteil/NOTES.md`'],
    ['Unabhängige Nachrechnung', '`saptasks/zc23_materialanteil/mata_check.py`'],
    ['Transport', '`T76K912736` (ZMM_UEB_KDAUF, ZPOWERBI_KDAUF, MPC_EXT DEFINE, ZM_OFFENE_KDAUF), in P76 importiert 16.10.2026']
  ])
];

const doc = new Document({
  creator: 'Ingo Kohler',
  title: 'ZC23 Materialanteil: Beschleunigung',
  styles: {
    default: { document: { run: { font: FONT, size: 21 } } },
    paragraphStyles: [
      { id: 'Heading1', name: 'Heading 1', basedOn: 'Normal', next: 'Normal', quickFormat: true,
        run: { size: 30, bold: true, color: RED, font: FONT }, paragraph: { spacing: { before: 320, after: 140 }, outlineLevel: 0 } },
      { id: 'Heading2', name: 'Heading 2', basedOn: 'Normal', next: 'Normal', quickFormat: true,
        run: { size: 24, bold: true, color: '333333', font: FONT }, paragraph: { spacing: { before: 220, after: 100 }, outlineLevel: 1 } }
    ]
  },
  numbering: {
    config: [{ reference: 'pkt', levels: [{ level: 0, format: LevelFormat.BULLET, text: '•', alignment: AlignmentType.LEFT,
      style: { paragraph: { indent: { left: 540, hanging: 270 } } } }] }]
  },
  sections: [{
    properties: { page: { size: { width: 11906, height: 16838 }, margin: { top: 1300, bottom: 1300, left: 1440, right: 1440 } } },
    footers: {
      default: new Footer({ children: [new Paragraph({ alignment: AlignmentType.RIGHT, children: [
        new TextRun({ text: 'Trafag AG · ZC23 Materialanteil, Beschleunigung · Seite ', font: FONT, size: 16, color: GREY }),
        new TextRun({ children: [PageNumber.CURRENT], font: FONT, size: 16, color: GREY })] })] })
    },
    children
  }]
});

Packer.toBuffer(doc).then(b => { fs.writeFileSync(process.argv[2], b); console.log('geschrieben', b.length); });
