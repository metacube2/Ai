"""Baut die Methodenquelltexte fuer die Layoutfilter ZC22/ZC27 (Plan PLAN_LAYOUTFILTER_2026-11-13.md, Abschnitt 3.4/3.5).

Aufruf (im Ordner saptasks/shopfloor_sap): python -X utf8 methoden_bauen_layout_2026-11-13.py
Erwartet MPC_EXT_DEFINE_vorher_2026-11-13_layout.abap und DPC_EXT_GET_ENTITYSET_vorher_2026-11-13_layout.abap, gelesen am
2026-11-13 (Phase 2, Schritt 0) in T76 per RFC (abap-read ZCL_ZPOWERBI_EINKAUF_MPC_EXT==CM001 und
ZCL_ZPOWERBI_EINKAUF_DPC_EXT==CM01W), byte-gleich mit saptasks/copa_verkauf/*_neu_2026-11-13.abap.
Die neun Einfuegebloecke werden woertlich aus den abap-Zaeunen des Plans geschnitten (nicht abgetippt).
Schreibt *_neu_2026-11-13_layout.abap (ASCII, CRLF, ohne BOM) und docs/abap/ZSHOP_LAYOUTFILTER_ADD.abap.
Es werden nur Zeilen eingefuegt, keine bestehende geaendert; die Asserts pruefen jeden Anker auf Eindeutigkeit,
die Blocklaengen und die Zeilensummen aus dem Plan. Muster: saptasks/copa_verkauf/methoden_bauen_hzdat_2026-11-13.py.
"""
import re

PLAN = "PLAN_LAYOUTFILTER_2026-11-13.md"
VERWEIS = "saptasks/shopfloor_sap/PLAN_LAYOUTFILTER_2026-11-13.md"


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


# ---- Bloecke aus dem Plan schneiden (Abschnitt 3.4 und 3.5) ----
plan = open(PLAN, encoding="utf-8").read().replace("\r\n", "\n").split("\n")
a34 = genau_einmal(plan, lambda x: x.startswith("### 3.4 Schritt 3"), "Abschnitt 3.4")
a36 = genau_einmal(plan, lambda x: x.startswith("### 3.6 Schritt 5"), "Abschnitt 3.6")
bloecke = []
k = a34
while k < a36:
    if plan[k] == "```abap":
        e = next(j for j in range(k + 1, a36) if plan[j] == "```")
        bloecke.append(plan[k + 1:e])
        k = e + 1
    else:
        k += 1
assert len(bloecke) == 9, len(bloecke)
mpc_auf, mpc_rm, rm_a, rm_b, rm_c, rm_d, sa_a, sa_b, sa_c = bloecke
laengen = [len(b) for b in bloecke]
assert laengen == [26, 7, 12, 9, 21, 1, 28, 1, 77], laengen
assert rm_c[0] == "", "ShopRm (c) beginnt im Plan mit einer Leerzeile"
for b in bloecke:
    for x in b:
        x.encode("ascii")
        assert x == x.rstrip(), ("Leerzeichen am Zeilenende", x)

mpc = lies("MPC_EXT_DEFINE_vorher_2026-11-13_layout.abap")
dpc = lies("DPC_EXT_GET_ENTITYSET_vorher_2026-11-13_layout.abap")
assert len(mpc) == 3027 and len(dpc) == 2737, (len(mpc), len(dpc))

# Neue Namen duerfen im Vorher-Stand nicht vorkommen (Plan 3.5: "Alle neuen Namen kommen in der Methode bisher nicht vor")
neu_namen = ["ty_rm_mat", "lt_rm_mat", "ls_rm_mat", "lt_rm_teil", "lt_rm_mx", "lv_rm_mx", "lv_rm_aus", "lv_rm_matex",
             "ty_sa_ao", "ty_sa_st", "lv_sa_avo", "lt_sa_ao", "ls_sa_ao", "lt_sa_af", "lt_sa_jsto", "ls_sa_jsto",
             "lt_sa_jest", "ls_sa_jest", "lt_sa_tj02", "ls_sa_tj02", "lt_sa_tj02t", "ls_sa_tj02t", "lt_sa_tj04",
             "ls_sa_tj04", "lt_sa_st", "ls_sa_st", "lv_sa_linep", "<ls_sa_a>"]
for n in neu_namen:
    muster = re.compile(r"(?<![a-z0-9_])" + re.escape(n) + r"(?![a-z0-9_])", re.I)
    assert not any(muster.search(x) for x in dpc), ("Name schon im Vorher-Stand", n)
# ---- MPC_EXT DEFINE (3.4): je Block direkt vor bind_structure, danach eine Leerzeile ----
ba = genau_einmal(mpc, lambda x: "bind_structure( iv_structure_name = 'ZSTR_SHOP_AUF'" in x, "bind ZSTR_SHOP_AUF")
br = genau_einmal(mpc, lambda x: "bind_structure( iv_structure_name = 'ZSTR_SHOP_RM'" in x, "bind ZSTR_SHOP_RM")
# Die neuen Properties duerfen in der jeweiligen Entity noch nicht stehen ('Vornr' gibt es in LogRueck, das ist erlaubt)
for b_idx, namen in ((ba, ("'Sttxt'", "'Arbpl'", "'Vornr'", "'Avo'")), (br, ("'Matex'",))):
    e_start = max(k for k in range(b_idx) if "create_entity_type(" in mpc[k])
    assert not any(n in x for x in mpc[e_start:b_idx] for n in namen), ("Property schon in der Entity", namen)
assert mpc[ba - 1].strip() == "" and mpc[br - 1].strip() == "", (mpc[ba - 1], mpc[br - 1])
einf_mpc = {ba: mpc_auf + [""], br: mpc_rm + [""]}  # vor Zeile ba / br einfuegen
mpc_neu = []
for k, x in enumerate(mpc):
    mpc_neu.extend(einf_mpc.get(k, []))
    mpc_neu.append(x)
assert len(mpc_neu) == 3027 + 27 + 8 == 3062, len(mpc_neu)

# ---- DPC_EXT GET_ENTITYSET (3.5): sieben Einfuegungen ----
start_rm = genau_einmal(dpc, lambda x: x == "  IF iv_entity_set_name = 'ShopRmSet'.", "Zweig ShopRmSet")
ende_rm = next(k for k in range(start_rm, len(dpc)) if dpc[k] == "  ENDIF.")
start_sa = genau_einmal(dpc, lambda x: x == "  IF iv_entity_set_name = 'ShopAufSet'.", "Zweig ShopAufSet")
zweig_rm = range(start_rm, ende_rm + 1)

p_rm_a = genau_einmal(dpc, lambda x: x == "          lv_rm_typcd TYPE char10.", "RM a lv_rm_typcd")
assert p_rm_a in zweig_rm and dpc[p_rm_a + 1].startswith("    FIELD-SYMBOLS: <ls_rm_f>"), dpc[p_rm_a + 1]

p_rm_b = genau_einmal(dpc, lambda x: x == "            APPEND ls_rm_typ TO lr_rm_typ.", "RM b APPEND ls_rm_typ")
assert p_rm_b in zweig_rm and dpc[p_rm_b + 1] == "          WHEN OTHERS.", dpc[p_rm_b + 1]

p_chk = genau_einmal(dpc, lambda x: x == "          CHECK lv_rm_zztyp IN lr_rm_typ.", "RM c CHECK")
p_rm_c = p_chk + 1
assert p_rm_c in zweig_rm and dpc[p_rm_c] == "        ENDIF." and dpc[p_rm_c + 1] == "", (dpc[p_rm_c], dpc[p_rm_c + 1])

p_rm_d = genau_einmal(dpc, lambda x: x == "          ls_rm_out-typcd = lv_rm_typcd.", "RM d typcd")
assert p_rm_d in zweig_rm

p_sa_a = genau_einmal(dpc, lambda x: x == "          ls_sa_vz TYPE ty_sa_vz.", "SA a ls_sa_vz")
assert p_sa_a > start_sa and dpc[p_sa_a + 1].startswith("    FIELD-SYMBOLS: <ls_sa_f>"), dpc[p_sa_a + 1]

p_sa_b = genau_einmal(dpc, lambda x: x == "          WHEN 'TYP'. lv_sa_typ = <ls_sa_o>-low.", "SA b WHEN TYP")
assert p_sa_b > start_sa and dpc[p_sa_b + 1] == "          WHEN OTHERS.", dpc[p_sa_b + 1]

p_vzvr = genau_einmal(dpc, lambda x: x == "    IF lv_sa_typ = 'VZ' OR lv_sa_typ = 'VR'.", "SA IF VZ/VR")
copy_zeile = "      copy_data_to_ref( EXPORTING is_data = lt_sa_out CHANGING cr_data = er_entityset )."
alle_copy = [k for k, x in enumerate(dpc) if "copy_data_to_ref( EXPORTING is_data = lt_sa_out" in x]
assert len(alle_copy) == 3, alle_copy
p_sa_c = next(k for k in alle_copy if k > p_vzvr)
assert dpc[p_sa_c] == copy_zeile and dpc[p_sa_c + 1] == "      RETURN." and dpc[p_sa_c + 2] == "    ENDIF.", dpc[p_sa_c:p_sa_c + 3]
assert dpc[p_sa_c - 1] == "      ENDIF." and dpc[p_sa_c - 2] == "        DELETE lt_sa_out FROM lv_sa_max.", dpc[p_sa_c - 2:p_sa_c]

nach = {p_rm_a: rm_a, p_rm_b: rm_b, p_rm_c: rm_c, p_rm_d: rm_d, p_sa_a: sa_a, p_sa_b: sa_b}  # nach Zeile einfuegen
vor = {p_sa_c: sa_c}  # vor Zeile einfuegen
dpc_neu = []
for k, x in enumerate(dpc):
    dpc_neu.extend(vor.get(k, []))
    dpc_neu.append(x)
    dpc_neu.extend(nach.get(k, []))
assert len(dpc_neu) == 2737 + 12 + 9 + 21 + 1 + 28 + 1 + 77 == 2886, len(dpc_neu)

# Gegenprobe: neu ohne die eingefuegten Zeilen ist wieder vorher (nur Zugaenge)
import difflib
for alt, neu, was in ((mpc, mpc_neu, "MPC"), (dpc, dpc_neu, "DPC")):
    sm = difflib.SequenceMatcher(a=alt, b=neu, autojunk=False)
    for tag, i1, i2, j1, j2 in sm.get_opcodes():
        assert tag in ("equal", "insert"), (was, tag, i1, i2, j1, j2)

schreibe("MPC_EXT_DEFINE_neu_2026-11-13_layout.abap", mpc_neu)
schreibe("DPC_EXT_GET_ENTITYSET_neu_2026-11-13_layout.abap", dpc_neu)

# ---- docs/abap/ZSHOP_LAYOUTFILTER_ADD.abap: Quelltext des Zusatzes aus denselben Bloecken ----
def kopf(text):
    return ["*" + "-" * 69, "* " + text, "*" + "-" * 69]

add = [
    "*&---------------------------------------------------------------------*",
    "*& ZSHOP_LAYOUTFILTER_ADD: Layoutfilter ZC22 / P00 TX und ZC27 /NIN im ZPOWERBI_EINKAUF_SRV",
    "*& Plan " + VERWEIS + " (Abschnitt 3.4/3.5), T76 2026-11-13,",
    "*& Auftrag T76K912906 (Aufgabe T76K912907). Quelle der Wahrheit fuer die Einfuegungen; die ganzen Methoden",
    "*& stehen in saptasks/shopfloor_sap/*_neu_2026-11-13_layout.abap (erzeugt mit methoden_bauen_layout_2026-11-13.py).",
    "*& DDIC: ZSTR_SHOP_RM + MATEX CHAR40; ZSTR_SHOP_AUF + STTXT TEXT100, ARBPL ARBPL, VORNR CHAR4, AVO CHAR1.",
    "*&---------------------------------------------------------------------*",
    "",
]
add += kopf("MPC_EXT->DEFINE, Entity ShopAuf: direkt vor bind_structure( 'ZSTR_SHOP_AUF' ), danach Leerzeile") + mpc_auf + [""]
add += kopf("MPC_EXT->DEFINE, Entity ShopRm: direkt vor bind_structure( 'ZSTR_SHOP_RM' ), danach Leerzeile") + mpc_rm + [""]
add += kopf("DPC_EXT->GET_ENTITYSET, ShopRmSet (a): nach 'lv_rm_typcd TYPE char10.'") + rm_a + [""]
add += kopf("DPC_EXT->GET_ENTITYSET, ShopRmSet (b): nach 'APPEND ls_rm_typ TO lr_rm_typ.' (vor WHEN OTHERS)") + rm_b + [""]
add += kopf("DPC_EXT->GET_ENTITYSET, ShopRmSet (c): nach dem ENDIF des Typencode-Blocks (beginnt mit Leerzeile)") + rm_c + [""]
add += kopf("DPC_EXT->GET_ENTITYSET, ShopRmSet (d): nach 'ls_rm_out-typcd = lv_rm_typcd.'") + rm_d + [""]
add += kopf("DPC_EXT->GET_ENTITYSET, ShopAufSet (a): nach 'ls_sa_vz TYPE ty_sa_vz.'") + sa_a + [""]
add += kopf("DPC_EXT->GET_ENTITYSET, ShopAufSet (b): nach WHEN 'TYP'.") + sa_b + [""]
add += kopf("DPC_EXT->GET_ENTITYSET, ShopAufSet (c): vor dem ersten copy_data_to_ref nach IF lv_sa_typ = 'VZ' OR 'VR' (nach dem Paging)") + sa_c
schreibe("../../docs/abap/ZSHOP_LAYOUTFILTER_ADD.abap", add)

print("MPC", len(mpc), "->", len(mpc_neu), " DPC", len(dpc), "->", len(dpc_neu))
print("Anker DPC (1-basiert): RM a", p_rm_a + 1, "RM b", p_rm_b + 1, "RM c", p_rm_c + 1, "RM d", p_rm_d + 1,
      "SA a", p_sa_a + 1, "SA b", p_sa_b + 1, "SA c vor", p_sa_c + 1, " MPC vor", ba + 1, br + 1)
