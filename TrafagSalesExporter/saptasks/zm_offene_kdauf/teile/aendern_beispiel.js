// Einmalige Aenderung 2026-10-12: Beispiele abweichender Belegfluesse im Pruefmodus ausgeben.
const fs = require('fs');
function ers(f, a, b) {
  let s = fs.readFileSync(f, 'utf8').replace(/\r\n/g, '\n');
  if (!s.includes(a)) throw new Error(f + ': ' + a.slice(0, 60));
  s = s.replace(a, b);
  fs.writeFileSync(f, s);
}
fs.appendFileSync('LZKDAUF_PARTOP.abap', fs.readFileSync('teile/fg_fluss_beispiel.abap', 'utf8'));

const f = 'Z_KDAUF_PAR_RECHNEN.abap';
ers(f, "        lv_fl_folge TYPE i,\n", "        lv_fl_folge TYPE i,\n        lv_fl_vor   TYPE i,\n        lt_beisp    TYPE string_table,\n        lv_beisp_n  TYPE i,\n");
ers(f, "      PERFORM fluss_vergleich USING lt_vbfa lt_fl_kurz CHANGING lv_fl_menge lv_fl_folge.\n",
  "      lv_fl_vor = lv_fl_menge + lv_fl_folge.\n" +
  "      PERFORM fluss_vergleich USING lt_vbfa lt_fl_kurz CHANGING lv_fl_menge lv_fl_folge.\n" +
  "      IF lv_fl_menge + lv_fl_folge > lv_fl_vor AND lv_beisp_n < 3.\n" +
  "        lv_beisp_n = lv_beisp_n + 1.\n" +
  "        PERFORM fluss_beispiel USING ls_pos-vbeln ls_pos-posnr lt_vbfa lt_fl_kurz CHANGING lt_beisp.\n" +
  "      ENDIF.\n");
ers(f, "         fl_menge = lv_fl_menge fl_folge = lv_fl_folge TO DATA BUFFER ev_out.\n",
  "         fl_menge = lv_fl_menge fl_folge = lv_fl_folge fl_beisp = lt_beisp TO DATA BUFFER ev_out.\n");

ers('teile/opt_global.abap', "      gv_par_offen    TYPE i.\n", "      gv_par_offen    TYPE i,\n      gt_o_fl_beisp   TYPE string_table.\n");
ers('teile/opt_parallel.abap', "        lv_flf   TYPE i.\n", "        lv_flf   TYPE i,\n        lt_beisp TYPE string_table.\n");
ers('teile/opt_parallel.abap', "fl_folge = lv_flf FROM DATA BUFFER lv_out.\n",
  "fl_folge = lv_flf fl_beisp = lt_beisp FROM DATA BUFFER lv_out.\n" +
  "  IF lines( gt_o_fl_beisp ) < 200.\n    APPEND LINES OF lt_beisp TO gt_o_fl_beisp.\n  ENDIF.\n");
// Export: nur OPT hat gt_o_fl_beisp; REF exportiert eine leere Tabelle gleichen Typs
ers('teile/gemeinsam_global.abap', "      gv_n_fl_folge      TYPE i.\n", "      gv_n_fl_folge      TYPE i,\n      gt_fl_beisp_exp    TYPE string_table.\n");
ers('bauen.js', "  '         n_fl_folge = gv_n_fl_folge',\n", "  '         n_fl_folge = gv_n_fl_folge',\n  '         fl_beisp   = gt_fl_beisp_exp',\n");
ers('teile/opt_parallel.abap', "  WAIT UNTIL gv_par_offen = 0.\n\n", "  WAIT UNTIL gv_par_offen = 0.\n  gt_fl_beisp_exp = gt_o_fl_beisp.\n\n");

const c = 'ZM_OFFENE_KDAUF_CMP.abap';
ers(c, "      gs_ib  TYPE ty_info.\n", "      gs_ib  TYPE ty_info,\n      gt_fl_beisp TYPE string_table.\n");
ers(c, "         n_fl_folge = cs_info-n_fl_folge\n", "         n_fl_folge = cs_info-n_fl_folge\n         fl_beisp   = lt_fb\n");
ers(c, "  PERFORM laufzeiten_zeigen.\n",
  "  PERFORM laufzeiten_zeigen.\n" +
  "  IF gt_fl_beisp IS NOT INITIAL.\n" +
  "    DATA lv_bz TYPE c LENGTH 250.\n" +
  "    WRITE: / 'Beispiele abweichender Belegfluesse (Pruefmodus):'.\n" +
  "    LOOP AT gt_fl_beisp INTO lv_bz.\n" +
  "      WRITE: / lv_bz(200).\n" +
  "    ENDLOOP.\n" +
  "    ULINE.\n" +
  "  ENDIF.\n");
ers(c, "        lv_t1 TYPE timestampl.\n", "        lv_t1 TYPE timestampl,\n        lt_fb TYPE string_table.\n");
ers(c, "  IF sy-subrc = 0.\n    cs_info-ok = abap_true.\n  ENDIF.\n",
  "  IF sy-subrc = 0.\n    cs_info-ok = abap_true.\n  ENDIF.\n  APPEND LINES OF lt_fb TO gt_fl_beisp.\n");
console.log('ok');
