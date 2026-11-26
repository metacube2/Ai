"""Baut die Methodenquelltexte fuer Shopfloor Welle 2 (Plan PLAN_WELLE2_2026-11-25.md, Abschnitt 11.3 und 11.4).

Aufruf (im Ordner saptasks/shopfloor_sap): python -X utf8 methoden_bauen_welle2_2026-11-26.py
Erwartet MPC_EXT_DEFINE_2026-11-26_welle2_vorher.abap und DPC_EXT_GET_ENTITYSET_2026-11-26_welle2_vorher.abap, gelesen am
2026-11-26 in T76 per RFC (abap-read ZCL_ZPOWERBI_EINKAUF_MPC_EXT==CM001 und ZCL_ZPOWERBI_EINKAUF_DPC_EXT==CM01W).
Die zwei Einfuegebloecke (M und D) werden woertlich aus den abap-Zaeunen des Plans geschnitten (nicht abgetippt).
Schreibt *_2026-11-26_welle2_neu.abap (ASCII, CRLF, ohne BOM) und docs/abap/ZSHOP_WELLE2_ADD.abap.
Es werden nur Zeilen eingefuegt, keine bestehende geaendert; die Asserts pruefen jeden Anker auf Eindeutigkeit und Nachbarzeilen.
Muster: methoden_bauen_layout_2026-11-13.py.
"""

PLAN = "PLAN_WELLE2_2026-11-25.md"


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


plan = open(PLAN, encoding="utf-8").read().replace("\r\n", "\n").split("\n")
a113 = genau_einmal(plan, lambda x: x.startswith("### 11.3 MPC"), "Abschnitt 11.3")
a115 = genau_einmal(plan, lambda x: x.startswith("### 11.5 Ablauf"), "Abschnitt 11.5")
bloecke = []
k = a113
while k < a115:
    if plan[k] == "```abap":
        e = next(j for j in range(k + 1, a115) if plan[j] == "```")
        bloecke.append(plan[k + 1:e])
        k = e + 1
    else:
        k += 1
assert len(bloecke) == 2, len(bloecke)
blk_m, blk_d = bloecke
for b in bloecke:
    assert b[-1] == "", "Block endet mit einer Leerzeile"
    for x in b:
        x.encode("ascii")
        assert x == x.rstrip(), ("Leerzeichen am Zeilenende", x)
        assert len(x) <= 255, ("Zeile zu lang", x)

mpc = lies("MPC_EXT_DEFINE_2026-11-26_welle2_vorher.abap")
dpc = lies("DPC_EXT_GET_ENTITYSET_2026-11-26_welle2_vorher.abap")
assert len(mpc) == 3062 and len(dpc) == 2887, (len(mpc), len(dpc))
for z in (mpc, dpc):
    assert not any("ShopMatBezug" in x or "ShopKdTermin" in x or "_kt_" in x or "_mb_" in x for x in z), "Namensraum belegt"

# MPC: vor der Trennlinie ueber "* LogKap: LogKapSet ..."
am = genau_einmal(mpc, lambda x: x == "* LogKap: LogKapSet, Pflichtfilter Werks und Datum, Tage 1..14", "MPC-Anker")
assert mpc[am - 1].startswith("* ----") and mpc[am - 2] == "", (mpc[am - 2], mpc[am - 1])
assert mpc[am - 3] == "  lo_entity_set->set_filter_required( abap_false ).", mpc[am - 3]
assert mpc[am - 12] == "  lo_entity_type->bind_structure( iv_structure_name = 'ZSTR_SHOP_AUF' iv_bind_conversions = abap_true ).", mpc[am - 12]
mpc_neu = mpc[:am - 1] + blk_m + mpc[am - 1:]

# DPC: direkt vor "* Logistik live Teil C, docs/abap/ZLOG_KAP_ADD.abap"
ad = genau_einmal(dpc, lambda x: x == "* Logistik live Teil C, docs/abap/ZLOG_KAP_ADD.abap", "DPC-Anker")
assert dpc[ad - 1] == "" and dpc[ad - 2] == "  ENDIF." and dpc[ad - 3] == "    RETURN.", dpc[ad - 3:ad]
assert dpc[ad - 4] == "    copy_data_to_ref( EXPORTING is_data = lt_sa_out CHANGING cr_data = er_entityset ).", dpc[ad - 4]
dpc_neu = dpc[:ad] + blk_d + dpc[ad:]

assert len(mpc_neu) == len(mpc) + len(blk_m)
assert len(dpc_neu) == len(dpc) + len(blk_d)
schreibe("MPC_EXT_DEFINE_2026-11-26_welle2_neu.abap", mpc_neu)
schreibe("DPC_EXT_GET_ENTITYSET_2026-11-26_welle2_neu.abap", dpc_neu)
kopf = ["* Shopfloor Welle 2 (Nenner MATDOC 201/261, VF/WE-Termine und Auftragsdaten je Kundenauftragsposition)",
        "* Quelle: saptasks/shopfloor_sap/PLAN_WELLE2_2026-11-25.md Abschnitt 11.3 (MPC_EXT->DEFINE) und 11.4 (DPC_EXT->GET_ENTITYSET)",
        "* Erzeugt mit saptasks/shopfloor_sap/methoden_bauen_welle2_2026-11-26.py", "",
        "* ===== MPC_EXT DEFINE: vor der Trennlinie ueber '* LogKap: LogKapSet ...' einfuegen ====="]
schreibe("../../docs/abap/ZSHOP_WELLE2_ADD.abap", kopf + blk_m +
         ["* ===== DPC_EXT GET_ENTITYSET: vor '* Logistik live Teil C, docs/abap/ZLOG_KAP_ADD.abap' einfuegen ====="] + blk_d)
print("Block M (MPC):", len(blk_m), "Zeilen, eingefuegt nach Zeile", am - 1, "->", len(mpc), "+", len(blk_m), "=", len(mpc_neu))
print("Block D (DPC):", len(blk_d), "Zeilen, eingefuegt nach Zeile", ad, "->", len(dpc), "+", len(blk_d), "=", len(dpc_neu))
