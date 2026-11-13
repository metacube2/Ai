"""Baut die Methodenquelltexte fuer den Stichtagsfilter Hzdat im CoPaSumSet (Vorgabe ZCOPA_SUM_HZDAT_2026-11-13.abap).

Aufruf (im Ordner saptasks/copa_verkauf): python -X utf8 methoden_bauen_hzdat_2026-11-13.py
Erwartet MPC_EXT_DEFINE_vorher_2026-11-13.abap und DPC_EXT_GET_ENTITYSET_vorher_2026-11-13.abap, gelesen am
2026-11-13 in T76 per RFC (abap-read ZCL_ZPOWERBI_EINKAUF_MPC_EXT==CM001 und ZCL_ZPOWERBI_EINKAUF_DPC_EXT==CM01W).
Schreibt *_neu_2026-11-13.abap (ASCII, CRLF, ohne BOM). Es werden nur Zeilen eingefuegt, keine bestehende geaendert;
die Asserts pruefen jeden Anker auf Eindeutigkeit. Muster: methoden_bauen.py (10.11.).
"""

QUELLE = "saptasks/copa_verkauf/ZCOPA_SUM_HZDAT_2026-11-13.abap"


def lies(name):
    return open(name, encoding="ascii").read().replace("\r\n", "\n").rstrip("\n").split("\n")


def schreibe(name, zeilen):
    text = "\n".join(zeilen) + "\n"
    text.encode("ascii")  # bricht ab, wenn ein Nicht-ASCII-Zeichen hineinkam
    open(name, "w", encoding="ascii", newline="\r\n").write(text)


def genau_einmal(zeilen, pruef, was):
    treffer = [k for k, x in enumerate(zeilen) if pruef(x)]
    assert len(treffer) == 1, (was, treffer)
    return treffer[0]


mpc = lies("MPC_EXT_DEFINE_vorher_2026-11-13.abap")
dpc = lies("DPC_EXT_GET_ENTITYSET_vorher_2026-11-13.abap")
assert not any("hzdat" in x.lower() for x in mpc + dpc), "Hzdat ist schon drin"

# ---- 2. MPC_EXT DEFINE: im CoPaSum-Block nach der Property 'Anzahl', vor bind_structure ----
b = genau_einmal(mpc, lambda x: "bind_structure( iv_structure_name = 'ZSTR_COPA_SUM'" in x, "bind ZSTR_COPA_SUM")
assert "'Anzahl'" in mpc[b - 5] and mpc[b - 1].strip() == "", (mpc[b - 5], mpc[b - 1])
mpc_zusatz = [
    "* Hzdat: optionaler Stichtag CE11000-HZDAT (Angelegt am), " + QUELLE,
    "  lo_property = lo_entity_type->create_property( iv_property_name = 'Hzdat' iv_abap_fieldname = 'HZDAT' ).",
    "  lo_property->set_type_edm_string( ).",
    "  lo_property->set_maxlength( iv_max_length = 8 ).",
    "  lo_property->set_creatable( abap_false ).",
    "  lo_property->set_updatable( abap_false ).",
    "",
]
mpc_neu = mpc[:b] + mpc_zusatz + mpc[b:]

# ---- 3. DPC_EXT GET_ENTITYSET, Zweig CoPaSumSet ----
start = genau_einmal(dpc, lambda x: x.strip() == "IF iv_entity_set_name = 'CoPaSumSet'.", "Zweig CoPaSumSet")
ende = next(k for k in range(start, len(dpc)) if dpc[k] == "  ENDIF.")
zweig = range(start, ende + 1)

# 3a. DATA-Block ergaenzen (eigene DATA-Anweisung nach lv_cp_msg)
a = genau_einmal(dpc, lambda x: x == "          lv_cp_msg   TYPE bapi_msg.", "lv_cp_msg")
assert a in zweig
zusatz_a = [
    "    DATA: lr_cp_hzdat TYPE RANGE OF ce11000-hzdat,",
    "          ls_cp_hzdat LIKE LINE OF lr_cp_hzdat,",
    "          lv_cp_hzdat TYPE char8.",
]

# 3b. Nach der bestehenden Filterschleife
lp = genau_einmal(dpc, lambda x: x == "    LOOP AT it_filter_select_options ASSIGNING <ls_cp_f>.", "Filterschleife")
assert lp in zweig
el = next(k for k in range(lp + 1, ende) if dpc[k] == "    ENDLOOP.")
assert dpc[el - 1] == "      ENDLOOP." and dpc[el + 1].strip() == "", (dpc[el - 1], dpc[el + 1])
zusatz_b = [
    "",
    "*   Optionaler Stichtag Hzdat (CE11000-HZDAT), jede Option (Cockpit: le); " + QUELLE,
    "    LOOP AT it_filter_select_options ASSIGNING <ls_cp_f>.",
    "      CHECK to_upper( <ls_cp_f>-property ) = 'HZDAT'.",
    "      LOOP AT <ls_cp_f>-select_options ASSIGNING <ls_cp_o>.",
    "        CLEAR ls_cp_hzdat.",
    "        ls_cp_hzdat-sign   = <ls_cp_o>-sign.",
    "        ls_cp_hzdat-option = <ls_cp_o>-option.",
    "        ls_cp_hzdat-low    = <ls_cp_o>-low.",
    "        ls_cp_hzdat-high   = <ls_cp_o>-high.",
    "        APPEND ls_cp_hzdat TO lr_cp_hzdat.",
    "        lv_cp_hzdat = <ls_cp_o>-low.",
    "      ENDLOOP.",
    "    ENDLOOP.",
]

# 3c. In beiden SELECTs nach "AND perde    = lv_cp_perde"
pe = [k for k, x in enumerate(dpc) if x == "          AND perde    = lv_cp_perde"]
assert len(pe) == 2 and all(k in zweig for k in pe), pe
zusatz_c = ["          AND hzdat    IN lr_cp_hzdat"]

# 3d. Ausgabe
d = genau_einmal(dpc, lambda x: x == "      ls_cp_out-anzahl = ls_cp_sum-anzahl.", "anzahl")
assert d in zweig
zusatz_d = ["      ls_cp_out-hzdat  = lv_cp_hzdat."]

einfuegen = {a: zusatz_a, el: zusatz_b, pe[0]: zusatz_c, pe[1]: zusatz_c, d: zusatz_d}
dpc_neu = []
for k, x in enumerate(dpc):
    dpc_neu.append(x)
    dpc_neu.extend(einfuegen.get(k, []))

schreibe("MPC_EXT_DEFINE_neu_2026-11-13.abap", mpc_neu)
schreibe("DPC_EXT_GET_ENTITYSET_neu_2026-11-13.abap", dpc_neu)
print("MPC", len(mpc), "->", len(mpc_neu), " DPC", len(dpc), "->", len(dpc_neu))
