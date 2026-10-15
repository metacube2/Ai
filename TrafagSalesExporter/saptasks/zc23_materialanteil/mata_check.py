"""Unabhaengige Nachrechnung des Materialanteils (Regel wie MATA_KALKULATION) ueber SapProbe-Tabellenlesen in T76."""
import subprocess
import sys
from datetime import date

R = r"C:\Users\koi\source\repos\Ai\TrafagSalesExporter\.tmp_sap_probe\RunSapProbe.ps1"


def read(table, fields, where, rows=500):
    out = subprocess.run(["powershell", "-ExecutionPolicy", "Bypass", "-File", R, "table-read", table,
                          "--fields", ",".join(fields), "--where", where, "--rowcount", str(rows), "--quiet"],
                         capture_output=True, text=True, encoding="utf-8", errors="replace").stdout
    lines = [l for l in out.splitlines() if " | " in l]
    if not lines:
        return []
    head = [h.strip() for h in lines[0].split("|")]
    res = []
    for l in lines[1:]:
        if set(l.strip()) <= set("-+|"):
            continue
        vals = [v.strip() for v in l.split("|")]
        res.append(dict(zip(head, vals)))
    return res


def num(v):
    v = (v or "0").replace(",", "")
    neg = v.endswith("-")
    v = v.rstrip("-")
    return -float(v) if neg else float(v or 0)


memo = {}
CZ = {r["OBJID"] for r in read("CRHD", ["OBJID", "ARBPL"], "OBJTY = 'A' AND ARBPL LIKE 'CZ%'", 5000)}
today = date.today().strftime("%Y%m%d")


def kalk(kalnr, kalka, kadky, tvers, bwvar, depth=0):
    key = (kalnr, kalka, kadky, tvers, bwvar)
    if key in memo:
        return memo[key]
    memo[key] = (0.0, 0.0)
    k = read("KEKO", ["LOSGR", "WERKS"], f"BZOBJ = '0' AND KALNR = '{kalnr}' AND KALKA = '{kalka}' AND KADKY = '{kadky}' AND TVERS = '{tvers}' AND BWVAR = '{bwvar}'", 2)
    if not k or num(k[0]["LOSGR"]) <= 0 or depth > 30:
        return (0.0, 0.0)
    losgr, werks = num(k[0]["LOSGR"]), k[0]["WERKS"]
    pos = read("CKIS", ["TYPPS", "MATNR", "WERKS", "MENGE", "WRTFW_KPF", "ARBID", "UKALN", "UKALKA", "UKADKY", "UTVERS", "UBWVAR"],
               f"LEDNR = '00' AND BZOBJ = '0' AND KALNR = '{kalnr}' AND KALKA = '{kalka}' AND KADKY = '{kadky}' AND TVERS = '{tvers}' AND BWVAR = '{bwvar}'")
    s = 0.0
    cz = 0.0
    for p in pos:
        w = num(p["WRTFW_KPF"])
        if w == 0:
            continue
        if p["TYPPS"] in ("M", "I"):
            m = read("MARC", ["MATNR", "BESKZ"], f"MATNR = '{p['MATNR']}' AND WERKS = '{p['WERKS']}'", 2)
            beskz = m[0]["BESKZ"] if m else ""
            if p["UKALN"].strip("0") == "" or beskz == "F" or p["WERKS"] != werks:
                s += w
            else:
                sm, sc = kalk(p["UKALN"], p["UKALKA"], p["UKADKY"], p["UTVERS"], p["UBWVAR"], depth + 1)
                s += sm * num(p["MENGE"])
                cz += sc * num(p["MENGE"])
        elif p["TYPPS"] == "E" and p["ARBID"] in CZ:
            cz += w
    memo[key] = (s / losgr, cz / losgr)
    return memo[key]


for mat in sys.argv[1:]:
    rows = read("KEKO", ["KALNR", "KALKA", "KADKY", "TVERS", "BWVAR", "KADAT", "LOSGR"],
                f"MATNR = '{mat}' AND WERKS = '1100' AND KALKA = '01' AND TVERS = '01' AND FREIG = 'X' AND LOEKZ = ' ' AND KADAT <= '{today}'", 200)
    rows.sort(key=lambda r: r["KADAT"])
    if not rows:
        print(mat, "keine freigegebene Kalkulation")
        continue
    r = rows[-1]
    mbew = read("MBEW", ["STPRS", "PEINH"], f"MATNR = '{mat}' AND BWKEY = '1100'", 2)
    v, vcz = kalk(r["KALNR"], r["KALKA"], r["KADKY"], r["TVERS"], r["BWVAR"])
    stprs = num(mbew[0]["STPRS"]) if mbew else 0
    peinh = mbew[0]["PEINH"] if mbew else "?"
    print(f"{mat}: Kalk {r['KADKY']} Material je Einheit {v:.4f}; Arbeit CZ je Einheit {vcz:.4f}; STPRS {stprs} PEINH {peinh}; Anteil {v / stprs if stprs else 0:.4f}")
