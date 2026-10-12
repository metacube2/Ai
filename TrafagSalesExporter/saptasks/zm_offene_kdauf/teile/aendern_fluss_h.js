// Mischform H fuer den Belegfluss: eigene Lesung, ausser bei gleichem Rechnungsdatum.
const fs = require('fs');
function ers(f, a, b) {
  let s = fs.readFileSync(f, 'utf8').replace(/\r\n/g, '\n');
  if (!s.includes(a)) throw new Error(f + ': ' + a.slice(0, 60));
  s = s.replace(a, b);
  fs.writeFileSync(f, s);
}
const f = 'Z_KDAUF_PAR_RECHNEN.abap';
ers(f, "*   sonst Original RV_ORDER_FLOW_INFORMATION.\n",
  "*   H = Mischform: eigene Lesung; haben zwei Fakturazeilen dasselbe ERDAT,\n" +
  "*       fuer diese Position doch RV_ORDER_FLOW_INFORMATION (die Reihenfolge\n" +
  "*       gleich datierter Zeilen haengt am ganzen Baum des Originals).\n" +
  "*       FL_FM zaehlt diese Rueckfaelle.\n" +
  "*   sonst Original RV_ORDER_FLOW_INFORMATION.\n");
ers(f, "        lv_fl_vor   TYPE i,\n", "        lv_fl_vor   TYPE i,\n        lv_fl_fm    TYPE i,\n        lv_gleich   TYPE abap_bool,\n");
ers(f, "    IF lv_flmod = 'K' OR lv_flmod = 'P'.\n      PERFORM fluss_kurz USING ls_pos-vbeln ls_pos-posnr CHANGING lt_fl_kurz.\n    ENDIF.\n    IF lv_flmod <> 'K'.\n",
  "    IF lv_flmod = 'K' OR lv_flmod = 'P' OR lv_flmod = 'H'.\n" +
  "      PERFORM fluss_kurz USING ls_pos-vbeln ls_pos-posnr CHANGING lt_fl_kurz.\n" +
  "    ENDIF.\n" +
  "    lv_gleich = abap_false.\n" +
  "    IF lv_flmod = 'H'.\n" +
  "      PERFORM fluss_gleiches_datum USING lt_fl_kurz CHANGING lv_gleich.\n" +
  "      IF lv_gleich = abap_true.\n" +
  "        lv_fl_fm = lv_fl_fm + 1.\n" +
  "      ENDIF.\n" +
  "    ENDIF.\n" +
  "    IF ( lv_flmod <> 'K' AND lv_flmod <> 'H' ) OR lv_gleich = abap_true.\n");
ers(f, "fl_beisp = lt_beisp TO DATA BUFFER ev_out.", "fl_beisp = lt_beisp fl_fm = lv_fl_fm TO DATA BUFFER ev_out.");

fs.appendFileSync('LZKDAUF_PARTOP.abap',
  "\n*----------------------------------------------------------------------*\n" +
  "* FLUSS_GLEICHES_DATUM: haben zwei Fakturazeilen dasselbe ERDAT?\n" +
  "*----------------------------------------------------------------------*\n" +
  "FORM fluss_gleiches_datum USING it_vbfa TYPE STANDARD TABLE\n" +
  "                          CHANGING cv_gleich TYPE abap_bool.\n" +
  "  DATA: lt_d   TYPE STANDARD TABLE OF vbfa-erdat,\n" +
  "        ls_v   TYPE vbfa,\n" +
  "        lv_n   TYPE i.\n" +
  "  cv_gleich = abap_false.\n" +
  "  LOOP AT it_vbfa INTO ls_v WHERE ('VBTYP_N = ''M''').\n" +
  "    APPEND ls_v-erdat TO lt_d.\n" +
  "  ENDLOOP.\n" +
  "  lv_n = lines( lt_d ).\n" +
  "  SORT lt_d.\n" +
  "  DELETE ADJACENT DUPLICATES FROM lt_d.\n" +
  "  IF lines( lt_d ) < lv_n.\n" +
  "    cv_gleich = abap_true.\n" +
  "  ENDIF.\n" +
  "ENDFORM.\n");

// OPT: Zaehler; Vorgabe bleibt O bis zur Freigabe
ers('teile/gemeinsam_global.abap', "      gv_n_fl_folge      TYPE i,\n", "      gv_n_fl_folge      TYPE i,\n      gv_n_fl_fm         TYPE i,\n");
ers('teile/opt_parallel.abap', "        lv_flf   TYPE i,\n", "        lv_flf   TYPE i,\n        lv_flfm  TYPE i,\n");
ers('teile/opt_parallel.abap', "fl_beisp = lt_beisp FROM DATA BUFFER lv_out.\n",
  "fl_beisp = lt_beisp fl_fm = lv_flfm FROM DATA BUFFER lv_out.\n  gv_n_fl_fm = gv_n_fl_fm + lv_flfm.\n");
ers('teile/opt_global.abap', "* Belegfluss im Parallelbaustein: O Original (Vorgabe), P Pruefen, K eigene Lesung\n",
  "* Belegfluss im Parallelbaustein: O Original (Vorgabe), P Pruefen, K eigene Lesung,\n* H eigene Lesung ausser bei gleichem Rechnungsdatum\n");
ers('bauen.js', "  '         n_fl_folge = gv_n_fl_folge',\n", "  '         n_fl_folge = gv_n_fl_folge',\n  '         n_fl_fm    = gv_n_fl_fm',\n");
const c = 'ZM_OFFENE_KDAUF_CMP.abap';
ers(c, "         n_fl_folge TYPE i,\n", "         n_fl_folge TYPE i,\n         n_fl_fm    TYPE i,\n");
ers(c, "         n_fl_folge = cs_info-n_fl_folge\n", "         n_fl_folge = cs_info-n_fl_folge\n         n_fl_fm    = cs_info-n_fl_fm\n");
ers(c, "  WRITE: / 'Belegfluss ohne Fakturazeile'",
  "  WRITE: / 'Fluss H: Rueckfall Original', AT 34(20) gs_ia-n_fl_fm, AT 58(20) gs_ib-n_fl_fm.\n  WRITE: / 'Belegfluss ohne Fakturazeile'");
console.log('ok');
