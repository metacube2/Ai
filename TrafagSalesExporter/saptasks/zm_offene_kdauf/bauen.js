// Baut ZM_OFFENE_KDAUF_REF.abap und ZM_OFFENE_KDAUF_OPT.abap aus dem Original und den Teilen.
// Aufruf: node bauen.js   (im Ordner saptasks/zm_offene_kdauf)
// Jede Ersetzung prueft den erwarteten Zeileninhalt, damit ein geaendertes Original auffaellt.
const fs = require('fs');
const lies = p => fs.readFileSync(p, 'utf8').replace(/\r\n/g, '\n').replace(/\n$/, '').split('\n');
const orig = lies('ZM_OFFENE_KDAUF_original.abap');
const teil = n => lies('teile/' + n + '.abap');

function pruefe(nr, erwartet) {
  const ist = orig[nr - 1].trim();
  if (!ist.startsWith(erwartet)) throw new Error(`Zeile ${nr}: erwartet "${erwartet}", ist "${ist}"`);
}

// Aenderungen als Liste {von, bis, neu} auf Originalzeilen (1-basiert, bis inklusive).
// "neu" ersetzt die Zeilen; zum reinen Einfuegen nach Zeile n: von = n + 1, bis = n.
function anwenden(aenderungen) {
  const sortiert = [...aenderungen].sort((a, b) => b.von - a.von);
  const z = [...orig];
  for (const a of sortiert) z.splice(a.von - 1, a.bis - a.von + 1, ...a.neu);
  return z.join('\r\n') + '\r\n';
}

const pVgl = ['PARAMETERS p_vgl NO-DISPLAY. " Vergleichslauf aus ZM_OFFENE_KDAUF_CMP: Ergebnis ins Memory statt ALV'];

const start = [
  'START-OF-SELECTION.',
  '  GET TIME STAMP FIELD gv_ts_start.',
  '  PERFORM e01_fieldcat_init  USING gt_fieldcat[].',
  '  PERFORM bilden_selwochen.',
  '  GET TIME STAMP FIELD gv_ts_a.',
  '  PERFORM fill_ivbep.',
  '  GET TIME STAMP FIELD gv_ts_b.',
  '  gv_t_ivbep = cl_abap_tstmp=>subtract( tstmp1 = gv_ts_b tstmp2 = gv_ts_a ).',
  '  gv_n_ivbep = lines( ivbep[] ).',
  '',
  '  REFRESH gt_uebauf.',
  '  PERFORM fill_uebauf.',
  '  PERFORM vergleich_export.',
];

const exportForm = [
  '',
  '*&---------------------------------------------------------------------*',
  '*&      Form  VERGLEICH_EXPORT',
  '*&---------------------------------------------------------------------*',
  '* Nur im Vergleichslauf (P_VGL): Ergebnis und Laufzeiten ins ABAP-Memory,',
  '* dann Programmende ohne ALV und ohne Fortschreibung von ZPOWERBI_KDAUF.',
  'FORM vergleich_export.',
  "  CHECK p_vgl = 'X'.",
  '  GET TIME STAMP FIELD gv_ts_a.',
  '  gv_t_uebauf = cl_abap_tstmp=>subtract( tstmp1 = gv_ts_a tstmp2 = gv_ts_b ).',
  '  gv_t_gesamt = cl_abap_tstmp=>subtract( tstmp1 = gv_ts_a tstmp2 = gv_ts_start ).',
  '  EXPORT tab        = gt_uebauf[]',
  '         n_ivbep    = gv_n_ivbep',
  '         t_gesamt   = gv_t_gesamt',
  '         t_ivbep    = gv_t_ivbep',
  '         t_uebauf   = gv_t_uebauf',
  '         t_prefetch = gv_t_prefetch',
  '         t_bereich  = gv_t_bereich',
  '         t_fkdat    = gv_t_fkdat',
  '         t_bezug    = gv_t_bezug',
  '         t_dispo    = gv_t_dispo',
  '         t_conv     = gv_t_conv',
  '         n_mbew     = gv_n_mbew_mehrfach',
  '         n_flow_fm  = gv_n_flow_fm',
  '         n_flow_ohne = gv_n_flow_ohne',
  '         n_flow_leer = gv_n_flow_leer',
  '         t_par      = gv_t_par',
  '         n_par_pak  = gv_n_par_pakete',
  '         n_par_fehl = gv_n_par_fehler',
  '         n_par_tref = gv_n_par_treffer',
  '         n_bp_diff  = gv_n_bp_diff',
  '         n_fl_menge = gv_n_fl_menge',
  '         n_fl_folge = gv_n_fl_folge',
  '         n_fl_fm    = gv_n_fl_fm',
  '         fl_beisp   = gt_fl_beisp_exp',
  "    TO MEMORY ID 'ZM_OFFENE_KDAUF_VGL'.",
  '  LEAVE PROGRAM.',
  'ENDFORM.                    "vergleich_export',
];

const vor = n => ['GET RUN TIME FIELD gv_rt0.'];
const nach = feld => ['GET RUN TIME FIELD gv_rt1.', `${feld} = ${feld} + ( gv_rt1 - gv_rt0 ) / 1000000.`];
// Umrahmt Originalzeilen [von..bis] mit Zeitmessung, Einrueckung wie die erste Zeile.
function messen(von, bis, feld) {
  const einr = orig[von - 1].match(/^\s*/)[0];
  const mit = s => einr + s;
  return { von, bis, neu: [...vor().map(mit), ...orig.slice(von - 1, bis), ...nach(feld).map(mit)] };
}

pruefe(67, 'SELECTION-SCREEN END OF BLOCK b4.');
pruefe(70, 'DATA: gv_update_powerbi_table');
pruefe(190, 'START-OF-SELECTION.');
pruefe(196, 'PERFORM fill_uebauf.');
pruefe(1133, 'ENDFORM.');

const gemeinsam = [
  { von: 68, bis: 67, neu: pVgl },
  { von: 71, bis: 70, neu: teil('gemeinsam_global') },
  { von: 190, bis: 196, neu: start },
  { von: 1134, bis: 1133, neu: exportForm },
];

// REF: Original wortgetreu, nur Messpunkte um die Bausteine in der Schleife.
pruefe(357, 'SORT   ivbep BY vbeln posnr  edatu  ASCENDING.');
pruefe(439, "CALL FUNCTION 'CONVERT_TO_LOCAL_CURRENCY'");
pruefe(446, 'local_amount     = w_netwr.');
pruefe(459, 'PERFORM bereich USING gt_uebauf-range.');
pruefe(523, 'PERFORM erster_bezug.');
pruefe(544, 'PERFORM fill_fkdat USING');
pruefe(561, 'SELECT a~dispo a~dispo_kz b~descr');
pruefe(564, 'JOIN zdispo_spart AS b ON a~dispo = b~dispo.');
const ref = anwenden([
  ...gemeinsam,
  { von: 357, bis: 357, neu: teil('sort_etenr') },
  messen(439, 446, 'gv_t_conv'),
  messen(459, 459, 'gv_t_bereich'),
  messen(523, 523, 'gv_t_bezug'),
  messen(544, 544, 'gv_t_fkdat'),
  messen(561, 564, 'gv_t_dispo'),
]);

// OPT: gepufferte Zugriffe, Logik und Reihenfolge wie im Original.
pruefe(160, '* 24.03.2011, ERP_EDI, end of insert');
pruefe(321, 'DELETE ivbep.');
pruefe(322, 'ENDIF.');
pruefe(673, '" 2. Daten aus gt_uebauf');
pruefe(715, 'ENDLOOP.');
pruefe(274, 'LOOP AT ivbep.');
pruefe(275, 'SELECT SINGLE * FROM vbak');
pruefe(291, 'ENDLOOP.');
pruefe(353, 'FORM fill_uebauf.');
pruefe(637, 'ENDFORM.');
pruefe(870, '*-----');
pruefe(873, 'FORM bereich USING n.');
pruefe(908, 'ENDFORM.');
pruefe(910, '*&-----');
pruefe(913, 'FORM erster_bezug');
pruefe(924, 'ENDFORM.');
pruefe(1058, 'FORM get_standard_price');
pruefe(1056, 'ENDFORM.');
pruefe(1053, "ls_fieldcat-fieldname = 'PSMNG'.");
const opt = anwenden([
  { von: 68, bis: 67, neu: [...pVgl, ...teil('opt_korr_screen')] },
  { von: 71, bis: 70, neu: [...teil('gemeinsam_global'), ...teil('opt_global')] },
  { von: 161, bis: 160, neu: teil('opt_korr_init') },
  { von: 190, bis: 196, neu: start },
  { von: 274, bis: 291, neu: teil('opt_ivbep_vbak') },
  { von: 322, bis: 321, neu: teil('opt_korr_delete') },
  { von: 673, bis: 715, neu: teil('opt_schreiben') },
  { von: 1056, bis: 1055, neu: teil('opt_fieldcat') },
  { von: 353, bis: 637, neu: teil('opt_fill_uebauf') },
  { von: 870, bis: 908, neu: teil('opt_bereich') },
  { von: 910, bis: 924, neu: teil('opt_erster_bezug') },
  { von: 1058, bis: 1133, neu: [...teil('opt_preis_fkdat'), ...teil('opt_parallel'), ...exportForm] },
]);

fs.writeFileSync('ZM_OFFENE_KDAUF_REF.abap', ref);
fs.writeFileSync('ZM_OFFENE_KDAUF_OPT.abap', opt);
console.log('REF Zeilen', ref.split('\r\n').length - 1, 'OPT Zeilen', opt.split('\r\n').length - 1);
