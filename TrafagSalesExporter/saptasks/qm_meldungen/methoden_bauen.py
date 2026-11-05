"""Baut die vollstaendigen Methodenquelltexte fuer QmMeldungSet aus dem Live-Stand plus Zusatz.

Aufruf (im Ordner saptasks/qm_meldungen): python -X utf8 methoden_bauen.py
Erwartet hier MPC_EXT_DEFINE_vorher.abap und DPC_EXT_GET_ENTITYSET_vorher.abap, gelesen am 2026-11-05
in T76 per RFC (abap-read ZCL_ZPOWERBI_EINKAUF_MPC_EXT==CM001 und ZCL_ZPOWERBI_EINKAUF_DPC_EXT==CM01W),
und ZQM_MELDUNG_ADD.abap. Schreibt MPC_EXT_DEFINE_neu.abap und DPC_EXT_GET_ENTITYSET_neu.abap.
Muster: saptasks/shopfloor_sap/methoden_bauen.py.
"""

zusatz = open("ZQM_MELDUNG_ADD.abap", encoding="utf-8").read().replace("\r\n", "\n").split("\n")


def lies(name):
    return open(name, encoding="utf-8-sig").read().replace("\r\n", "\n").rstrip("\n").split("\n")


def block(start, ende):
    i = next(k for k, x in enumerate(zusatz) if x.startswith(start))
    j = next(k for k, x in enumerate(zusatz) if k > i and x.startswith(ende))
    return zusatz[i:j + 1]


def schreibe(name, zeilen):
    open(name, "w", encoding="utf-8", newline="\r\n").write("\n".join(zeilen) + "\n")


mpc = lies("MPC_EXT_DEFINE_vorher.abap")
dpc = lies("DPC_EXT_GET_ENTITYSET_vorher.abap")
assert not any("'QmMeldungSet'" in x for x in mpc + dpc), "Zusatz ist schon drin"

e = max(k for k, x in enumerate(mpc) if x.strip().upper() == "ENDMETHOD.")
mpc_neu = mpc[:e] + [""] + block("* ===== MPC_EXT DEFINE", "* ===== Ende MPC") + [""] + mpc[e:]
d = [k for k, x in enumerate(dpc) if x.strip() == "IF iv_entity_set_name <> 'FinanzJournalSet'."]
assert len(d) == 1, d
dpc_neu = dpc[:d[0]] + block("* ===== DPC_EXT GET_ENTITYSET", "* ===== Ende DPC") + [""] + dpc[d[0]:]
schreibe("MPC_EXT_DEFINE_neu.abap", mpc_neu)
schreibe("DPC_EXT_GET_ENTITYSET_neu.abap", dpc_neu)
print("MPC", len(mpc), "->", len(mpc_neu), " DPC", len(dpc), "->", len(dpc_neu))
