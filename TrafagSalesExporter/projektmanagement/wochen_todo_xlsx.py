# /// script
# requires-python = ">=3.12,<3.13"
# dependencies = ["openpyxl>=3.1"]
# ///
"""Schreibt `Wochen_Todo.tsv` in das erste Blatt von `Wochen_Todo.xlsx` fort.

Aufruf:
    uv run --python 3.12 projektmanagement/wochen_todo_xlsx.py

Die TSV ist die fuehrende Quelle, die Excel nur die lesbare Ausgabe. Das Skript
**oeffnet die vorhandene Datei und ersetzt nur das Datenblatt**, statt eine neue
Mappe zu schreiben. Sonst ginge das Blatt `Erläuterungen` verloren, das am
2026-09-02 dazugekommen ist und nicht aus der TSV stammt.
"""

from __future__ import annotations

import csv
from pathlib import Path

from openpyxl import load_workbook
from openpyxl.styles import Alignment, Font, PatternFill
from openpyxl.utils import get_column_letter

ORDNER = Path(__file__).resolve().parent
TSV = ORDNER / "Wochen_Todo.tsv"
XLSX = ORDNER / "Wochen_Todo.xlsx"

BREITEN = {
    "Nr": 5, "Dringlichkeit": 14, "Bereich": 20, "ID": 8, "Thema": 30,
    "Wer": 10, "Prio": 8, "Status": 12, "Fällig": 12,
    "Nächster Schritt": 90, "Erledigt am": 13,
}

FARBEN = {
    "Erledigt": "C6E0B4",
    "Läuft": "FFE699",
    "Offen": "F8CBAD",
}


def main() -> int:
    with TSV.open(encoding="utf-8-sig", newline="") as f:
        zeilen = list(csv.reader(f, delimiter="\t"))

    if not zeilen:
        print("TSV ist leer, nichts geschrieben.")
        return 1

    kopf = zeilen[0]
    mappe = load_workbook(XLSX)

    # Das Datenblatt wird ueber den Namen gesucht, nicht ueber die Position.
    # Das erste Blatt ist `Zusammenfassung` und wird von Hand gepflegt; es
    # versehentlich zu ueberschreiben war am 2026-09-03 ein Fehler.
    if "Wochen-Todo" not in mappe.sheetnames:
        print("Blatt 'Wochen-Todo' nicht gefunden. Vorhanden: "
              + ", ".join(mappe.sheetnames))
        return 2
    blatt = mappe["Wochen-Todo"]
    andere = [name for name in mappe.sheetnames if name != "Wochen-Todo"]

    blatt.delete_rows(1, blatt.max_row)

    for zeile in zeilen:
        blatt.append(zeile)

    for spalte, name in enumerate(kopf, start=1):
        zelle = blatt.cell(row=1, column=spalte)
        zelle.font = Font(bold=True)
        zelle.alignment = Alignment(vertical="top")
        blatt.column_dimensions[get_column_letter(spalte)].width = BREITEN.get(name, 18)

    try:
        status_spalte = kopf.index("Status") + 1
    except ValueError:
        status_spalte = None

    for nr in range(2, blatt.max_row + 1):
        for spalte in range(1, len(kopf) + 1):
            blatt.cell(row=nr, column=spalte).alignment = Alignment(
                vertical="top", wrap_text=True)
        if status_spalte:
            wert = blatt.cell(row=nr, column=status_spalte).value
            farbe = FARBEN.get(str(wert).strip()) if wert else None
            if farbe:
                blatt.cell(row=nr, column=status_spalte).fill = PatternFill(
                    "solid", fgColor=farbe)

    blatt.freeze_panes = "A2"
    mappe.save(XLSX)

    print(f"{blatt.max_row - 1} Zeilen in Blatt '{blatt.title}' geschrieben.")
    if andere:
        print("Unveraendert erhalten: " + ", ".join(andere))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
