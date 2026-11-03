"""PM-14: Listen Datenqualitaet und Abloesung (Excel/Power BI/Smartsheet).

Quelle sind die beiden TSV-Dateien in diesem Ordner, Ziel ist `PM-14_Datenqualitaet_Abloesung.xlsx`
mit drei Blaettern: Uebersicht (Zaehlungen fuer die Sitzung mit dem Chef), Datenqualitaet, Inventar.
Angelegt am 2026-11-03 (Auftrag Ingo). Aufruf mit dem uv-Python mit openpyxl
(`memory/reference_python_ohne_installation`): python projektmanagement/pm14_listen_xlsx.py
Die TSV-Dateien sind die Quelle; die .xlsx wird bei jeder Aenderung neu erzeugt.
"""
from collections import Counter
from pathlib import Path

from openpyxl import Workbook
from openpyxl.styles import Alignment, Font, PatternFill
from openpyxl.utils import get_column_letter

ORDNER = Path(__file__).resolve().parent
DQ = ORDNER / "PM-14_Datenqualitaet_Tasks.tsv"
INV = ORDNER / "PM-14_Inventar_Abloesung.tsv"
ZIEL = ORDNER / "PM-14_Datenqualitaet_Abloesung.xlsx"

KOPF = PatternFill("solid", fgColor="8B1E2D")
FARBEN = {
    "Offen": "F8D7DA", "Wartet": "FFF3CD", "Entscheid": "FFF3CD", "Befund": "E2E3E5",
    "abgelöst": "D4EDDA", "parallel": "FFF3CD", "läuft": "D1ECF1", "abzulösen": "F8D7DA", "unklar": "E2E3E5",
}


def lies(pfad: Path) -> list[list[str]]:
    zeilen = [z.split("\t") for z in pfad.read_text(encoding="utf-8").splitlines() if z.strip()]
    return zeilen


def blatt(mappe, name: str, zeilen: list[list[str]], breiten: list[int], farbspalte: str) -> None:
    ws = mappe.create_sheet(name)
    for r, zeile in enumerate(zeilen, start=1):
        for c, wert in enumerate(zeile, start=1):
            zelle = ws.cell(row=r, column=c, value=wert)
            zelle.alignment = Alignment(wrap_text=True, vertical="top")
            if r == 1:
                zelle.font = Font(bold=True, color="FFFFFF")
                zelle.fill = KOPF
    idx = zeilen[0].index(farbspalte) + 1
    for r in range(2, len(zeilen) + 1):
        wert = ws.cell(row=r, column=idx).value or ""
        for schluessel, farbe in FARBEN.items():
            if wert.startswith(schluessel):
                ws.cell(row=r, column=idx).fill = PatternFill("solid", fgColor=farbe)
                break
    for i, b in enumerate(breiten, start=1):
        ws.column_dimensions[get_column_letter(i)].width = b
    ws.freeze_panes = "A2"
    ws.auto_filter.ref = ws.dimensions


def main() -> int:
    dq, inv = lies(DQ), lies(INV)
    mappe = Workbook()
    ue = mappe.active
    ue.title = "Übersicht"
    ue["A1"] = "PM-14 Datenqualität und Ablösung Excel / Power BI / Smartsheet"
    ue["A1"].font = Font(bold=True, size=14, color="8B1E2D")
    ue["A2"] = "Stand aus der Projektdoku, nicht neu gemessen. Quelle: die beiden TSV-Dateien im Ordner projektmanagement."
    r = 4
    ue.cell(row=r, column=1, value="Datenqualität: Befunde je Status").font = Font(bold=True)
    s = dq[0].index("Status")
    def gruppe(wert: str) -> str:
        for praefix, name in (("Wartet", "Wartet auf andere"), ("Entscheid", "Fachentscheid offen"),
                              ("Offen", "Offen, bereinigen"), ("Befund", "Befund ohne Massnahme")):
            if wert.startswith(praefix):
                return name
        return wert

    for status, n in Counter(gruppe(z[s]) for z in dq[1:]).most_common():
        r += 1
        ue.cell(row=r, column=1, value=status)
        ue.cell(row=r, column=2, value=n)
    r += 1
    ue.cell(row=r, column=1, value="Summe")
    ue.cell(row=r, column=2, value=len(dq) - 1).font = Font(bold=True)
    zb = dq[0].index("Zuständig belegt")
    r += 1
    ue.cell(row=r, column=1, value="davon ohne belegte Zuständigkeit")
    ue.cell(row=r, column=2, value=sum(1 for z in dq[1:] if z[zb] != "ja"))
    r += 2
    ue.cell(row=r, column=1, value="Inventar: Art und Zustand").font = Font(bold=True)
    a, zu = inv[0].index("Art"), inv[0].index("Zustand")
    for (art, zustand), n in sorted(Counter((z[a], z[zu]) for z in inv[1:]).items()):
        r += 1
        ue.cell(row=r, column=1, value=f"{art}: {zustand}")
        ue.cell(row=r, column=2, value=n)
    r += 2
    ue.cell(row=r, column=1, value="Eingesparte Stunden: im Repo nirgends beziffert; Spalten im Blatt Inventar sind zum Schätzen da.")
    ue.column_dimensions["A"].width = 70
    ue.column_dimensions["B"].width = 10
    blatt(mappe, "Datenqualität", dq, [5, 16, 60, 30, 26, 12, 22, 34], "Status")
    blatt(mappe, "Inventar", inv, [5, 14, 34, 14, 34, 40, 12, 14, 14, 40], "Zustand")
    mappe.save(ZIEL)
    print(f"{len(dq) - 1} Befunde, {len(inv) - 1} Inventarzeilen -> {ZIEL.name}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
