"""Baut die vollstaendigen neuen Methodenquelltexte aus dem Live-Stand (abap-read aus T76) plus Zusatz.

Aufruf (im Ordner saptasks/fiori_zc23):  python -X utf8 methoden_bauen.py <Ordner mit abap-read-Ausgaben>
Erwartet dort ZCL_ZPOWERBI_EINKAUF_MPC_EXT__CM001.txt (DEFINE) und ZCL_ZPOWERBI_EINKAUF_DPC_EXT__CM01W.txt
(/IWBEP/IF_MGW_APPL_SRV_RUNTIME~GET_ENTITYSET), gelesen am 2026-10-15.
"""
import os
import sys

ordner = sys.argv[1]


def lies(name):
    z = open(os.path.join(ordner, name), encoding="utf-8-sig").read().replace("\r\n", "\n").split("\n")
    i = next(k for k, x in enumerate(z) if x.startswith("*&") or x.upper().startswith("METHOD"))
    j = max(k for k, x in enumerate(z) if x.strip().upper() == "ENDMETHOD.")
    return z[i:j + 1]


def zusatz(name):
    return open(name, encoding="utf-8").read().replace("\r\n", "\n").rstrip("\n").split("\n")


def schreibe(name, zeilen):
    open(name, "w", encoding="utf-8", newline="\r\n").write("\n".join(zeilen) + "\n")


mpc = lies("ZCL_ZPOWERBI_EINKAUF_MPC_EXT__CM001.txt")
dpc = lies("ZCL_ZPOWERBI_EINKAUF_DPC_EXT__CM01W.txt")
schreibe("MPC_EXT_DEFINE_vorher.abap", mpc)
schreibe("DPC_EXT_GET_ENTITYSET_vorher.abap", dpc)

assert mpc[-1].strip().upper() == "ENDMETHOD."
neu_m = mpc[:-1] + zusatz("ZC23_MPC_DEFINE_ADD.abap") + [""] + [mpc[-1]]

anker = "  IF iv_entity_set_name <> 'FinanzJournalSet'."
assert dpc.count(anker) == 1, "Anker FinanzJournalSet nicht genau einmal gefunden"
k = dpc.index(anker)
neu_d = dpc[:k] + zusatz("ZC23_DPC_GET_ENTITYSET_ADD.abap") + [""] + dpc[k:]

schreibe("MPC_EXT_DEFINE_neu.abap", neu_m)
schreibe("DPC_EXT_GET_ENTITYSET_neu.abap", neu_d)
print("MPC", len(mpc), "->", len(neu_m), " DPC", len(dpc), "->", len(neu_d))
