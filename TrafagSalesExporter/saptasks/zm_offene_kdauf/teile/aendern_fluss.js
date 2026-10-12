// Einmalige Aenderung 2026-10-12: eigene Belegfluss-Lesung (FLUSS_KURZ) mit Pruefmodus
// in Funktionsgruppe, OPT, CMP und Startskript. Aufruf im Ordner saptasks/zm_offene_kdauf.
const fs = require('fs');
function ers(f, a, b) {
  let s = fs.readFileSync(f, 'utf8').replace(/\r\n/g, '\n');
  if (!s.includes(a)) throw new Error(f + ': ' + a.slice(0, 60));
  s = s.replace(a, b);
  fs.writeFileSync(f, s);
}

// TOP-Include: Formen anhaengen
fs.appendFileSync('LZKDAUF_PARTOP.abap', fs.readFileSync('teile/fg_fluss_formen.abap', 'utf8'));

// Baustein
const f = 'Z_KDAUF_PAR_RECHNEN.abap';
ers(f, "*   sonst Original Z_BASISPREISLISTE.\n",
  "*   sonst Original Z_BASISPREISLISTE.\n" +
  "* FLMOD (Belegfluss):\n" +
  "*   K = eigene Lesung FLUSS_KURZ statt RV_ORDER_FLOW_INFORMATION\n" +
  "*   P = Pruefen: beide, Rueckgabe des Originals; FL_MENGE und FL_FOLGE zaehlen\n" +
  "*       Positionen mit anderen Fakturazeilen bzw. anderer Reihenfolge\n" +
  "*   sonst Original RV_ORDER_FLOW_INFORMATION.\n");
ers(f, "        lv_bp_diff TYPE i,\n",
  "        lv_bp_diff TYPE i,\n" +
  "        lv_flmod   TYPE c LENGTH 1,\n" +
  "        lt_fl_kurz TYPE TABLE OF vbfa,\n" +
  "        lv_fl_menge TYPE i,\n" +
  "        lv_fl_folge TYPE i,\n");
ers(f, "bpmod = lv_bpmod FROM DATA BUFFER iv_in.", "bpmod = lv_bpmod flmod = lv_flmod FROM DATA BUFFER iv_in.");
ers(f,
  "    CLEAR lt_vbfa.\n" +
  "    ls_vbco6 = VALUE vbco6( vbeln = ls_pos-vbeln  posnr = ls_pos-posnr ).\n" +
  "    CALL FUNCTION 'RV_ORDER_FLOW_INFORMATION'        \"#EC CI_USAGE_OK[2198647]\n" +
  "      EXPORTING\n" +
  "        comwa         = ls_vbco6\n" +
  "      TABLES\n" +
  "        vbfa_tab      = lt_vbfa\n" +
  "      EXCEPTIONS\n" +
  "        no_vbfa       = 1\n" +
  "        no_vbuk_found = 2\n" +
  "        OTHERS        = 3.\n",
  "    CLEAR: lt_vbfa, lt_fl_kurz.\n" +
  "    IF lv_flmod = 'K' OR lv_flmod = 'P'.\n" +
  "      PERFORM fluss_kurz USING ls_pos-vbeln ls_pos-posnr CHANGING lt_fl_kurz.\n" +
  "    ENDIF.\n" +
  "    IF lv_flmod <> 'K'.\n" +
  "      ls_vbco6 = VALUE vbco6( vbeln = ls_pos-vbeln  posnr = ls_pos-posnr ).\n" +
  "      CALL FUNCTION 'RV_ORDER_FLOW_INFORMATION'        \"#EC CI_USAGE_OK[2198647]\n" +
  "        EXPORTING\n" +
  "          comwa         = ls_vbco6\n" +
  "        TABLES\n" +
  "          vbfa_tab      = lt_vbfa\n" +
  "        EXCEPTIONS\n" +
  "          no_vbfa       = 1\n" +
  "          no_vbuk_found = 2\n" +
  "          OTHERS        = 3.\n" +
  "    ELSE.\n" +
  "      lt_vbfa = lt_fl_kurz.\n" +
  "    ENDIF.\n" +
  "    IF lv_flmod = 'P'.\n" +
  "      PERFORM fluss_vergleich USING lt_vbfa lt_fl_kurz CHANGING lv_fl_menge lv_fl_folge.\n" +
  "    ENDIF.\n");
ers(f, "  EXPORT fluss = lt_fluss bp = lt_bp bp_diff = lv_bp_diff TO DATA BUFFER ev_out.\n",
  "  EXPORT fluss = lt_fluss bp = lt_bp bp_diff = lv_bp_diff\n" +
  "         fl_menge = lv_fl_menge fl_folge = lv_fl_folge TO DATA BUFFER ev_out.\n");

// OPT
ers('teile/gemeinsam_global.abap', "      gv_n_bp_diff       TYPE i.\n",
  "      gv_n_bp_diff       TYPE i,\n      gv_n_fl_menge      TYPE i,\n      gv_n_fl_folge      TYPE i.\n");
ers('teile/opt_global.abap', "            p_bpmod TYPE c LENGTH 1 DEFAULT 'K' NO-DISPLAY.\n",
  "            p_bpmod TYPE c LENGTH 1 DEFAULT 'K' NO-DISPLAY,\n" +
  "* Belegfluss im Parallelbaustein: O Original (Vorgabe), P Pruefen, K eigene Lesung\n" +
  "            p_flmod TYPE c LENGTH 1 DEFAULT 'O' NO-DISPLAY.\n");
ers('teile/opt_parallel.abap', "bpmod = p_bpmod TO DATA BUFFER lv_in.", "bpmod = p_bpmod flmod = p_flmod TO DATA BUFFER lv_in.");
ers('teile/opt_parallel.abap', "        lv_diff  TYPE i.\n", "        lv_diff  TYPE i,\n        lv_flm   TYPE i,\n        lv_flf   TYPE i.\n");
ers('teile/opt_parallel.abap',
  "bp_diff = lv_diff FROM DATA BUFFER lv_out.\n  gv_n_bp_diff = gv_n_bp_diff + lv_diff.\n",
  "bp_diff = lv_diff fl_menge = lv_flm fl_folge = lv_flf FROM DATA BUFFER lv_out.\n" +
  "  gv_n_bp_diff = gv_n_bp_diff + lv_diff.\n" +
  "  gv_n_fl_menge = gv_n_fl_menge + lv_flm.\n" +
  "  gv_n_fl_folge = gv_n_fl_folge + lv_flf.\n");
ers('bauen.js', "  '         n_bp_diff  = gv_n_bp_diff',\n",
  "  '         n_bp_diff  = gv_n_bp_diff',\n  '         n_fl_menge = gv_n_fl_menge',\n  '         n_fl_folge = gv_n_fl_folge',\n");

// CMP
const c = 'ZM_OFFENE_KDAUF_CMP.abap';
ers(c, "         n_bp_diff  TYPE i,\n", "         n_bp_diff  TYPE i,\n         n_fl_menge TYPE i,\n         n_fl_folge TYPE i,\n");
ers(c, "         n_bp_diff  = cs_info-n_bp_diff\n",
  "         n_bp_diff  = cs_info-n_bp_diff\n         n_fl_menge = cs_info-n_fl_menge\n         n_fl_folge = cs_info-n_fl_folge\n");
ers(c, "  WRITE: / 'Belegfluss ohne Fakturazeile'",
  "  WRITE: / 'Fluss kurz: andere Fakturazeilen', AT 34(20) gs_ia-n_fl_menge, AT 58(20) gs_ib-n_fl_menge.\n" +
  "  WRITE: / 'Fluss kurz: andere Reihenfolge', AT 34(20) gs_ia-n_fl_folge, AT 58(20) gs_ib-n_fl_folge.\n" +
  "  WRITE: / 'Belegfluss ohne Fakturazeile'");
ers(c, "              p_korr  TYPE c LENGTH 1 DEFAULT 'A'.",
  "              p_flmod TYPE c LENGTH 1 DEFAULT 'O',  \" OPT-Belegfluss: O original, P pruefen, K eigene Lesung\n" +
  "              p_korr  TYPE c LENGTH 1 DEFAULT 'A'.");
ers(c, "  DELETE gt_par WHERE selname = 'P_BPMOD'.\n",
  "  DELETE gt_par WHERE selname = 'P_FLMOD'.\n" +
  "  CLEAR ls_par. ls_par-kind = 'P'. ls_par-selname = 'P_FLMOD'. ls_par-low = p_flmod. APPEND ls_par TO gt_par.\n" +
  "  DELETE gt_par WHERE selname = 'P_BPMOD'.\n");

// Startskript
const v = '../../.tmp_sap_probe/SapGuiKdaufVergleich.vbs';
let s = fs.readFileSync(v, 'utf8');
const a = 'If WScript.Arguments.Count > 10 Then Setze "P_BPMOD", WScript.Arguments(10)';
if (!s.includes(a)) throw new Error('vbs');
s = s.replace(a, a + '\r\nIf WScript.Arguments.Count > 11 Then Setze "P_FLMOD", WScript.Arguments(11)');
s = s.replace('[Basispreis K|P|O]', '[Basispreis K|P|O|-] [Fluss O|P|K]');
fs.writeFileSync(v, s);
console.log('ok');
