# /// script
# requires-python = ">=3.12,<3.13"
# dependencies = [
#   "python-docx>=1.1.0",
#   "matplotlib>=3.8",
# ]
# ///
"""Erzeugt die Word-Dokumentation zur ZZPRDAT-Loesung samt Schaubild.

Aufruf:
    uv run --python 3.12 saptasks/zzprdat/erzeuge_doku.py

Anders als die Arbeitsdateien im Repository verwendet das Word-Dokument echte
Umlaute. Es geht an Fachbereich und Management; dort waeren umschriebene
Umlaute unangebracht.
"""

from __future__ import annotations

from pathlib import Path

import matplotlib

matplotlib.use("Agg")
import matplotlib.pyplot as plt
from matplotlib.patches import FancyArrowPatch, FancyBboxPatch

from docx import Document
from docx.enum.text import WD_ALIGN_PARAGRAPH
from docx.shared import Cm, Pt, RGBColor

WURZEL = Path(__file__).resolve().parents[2]
BILD = WURZEL / "saptasks" / "zzprdat" / "zzprdat_ablauf.png"
DOKUMENT = WURZEL / "docs" / "ZZPRDAT_Loesung_2026-09-03.docx"

ROT = "#B23A48"
GRUEN = "#2E7D5B"
GRAU = "#4A4A4A"


def kasten(ax, x, y, breite, hoehe, text, farbe, fontsize=9):
    ax.add_patch(
        FancyBboxPatch(
            (x, y), breite, hoehe,
            boxstyle="round,pad=0.02,rounding_size=0.08",
            linewidth=0, facecolor=farbe,
        )
    )
    ax.text(x + breite / 2, y + hoehe / 2, text,
            ha="center", va="center", fontsize=fontsize, color="white")


def pfeil(ax, x, y_von, y_bis, farbe=GRAU):
    ax.add_patch(
        FancyArrowPatch((x, y_von), (x, y_bis),
                        arrowstyle="-|>", mutation_scale=13,
                        linewidth=1.4, color=farbe)
    )


def schaubild_erzeugen() -> None:
    fig, ax = plt.subplots(figsize=(11, 6.0))
    ax.set_xlim(0, 10)
    ax.set_ylim(0.6, 6.4)
    ax.axis("off")

    ax.text(2.4, 6.05, "Vorher: das Datum verschwand", ha="center",
            fontsize=12.5, color=ROT, fontweight="bold")
    ax.text(7.6, 6.05, "Nachher: das Datum bleibt", ha="center",
            fontsize=12.5, color=GRUEN, fontweight="bold")

    schritte_alt = [
        ("Auftrag wird freigegeben\nund gesichert", GRAU),
        ("BAdI WORKORDER_UPDATE\nAT_RELEASE läuft", GRAU),
        ("Z_PP_PRDDAT_SET als V1\nschreibt ZZPRDAT", GRAU),
        ("SAP-Standardverbuchung schreibt\ndie ganze AUFK-Zeile\naus ihrem eigenen Puffer", ROT),
        ("ZZPRDAT ist wieder leer", ROT),
    ]
    schritte_neu = [
        ("Auftrag wird freigegeben\nund gesichert", GRAU),
        ("BAdI WORKORDER_UPDATE\nAT_RELEASE läuft", GRAU),
        ("Z_PP_PRDDAT_SET wird als V2\nnur vorgemerkt", GRUEN),
        ("SAP-Standardverbuchung\nschreibt die AUFK-Zeile", GRAU),
        ("V2 läuft danach und setzt\nZZPRDAT dauerhaft", GRUEN),
    ]

    for x_links, schritte in ((0.6, schritte_alt), (5.8, schritte_neu)):
        y = 5.15
        for nr, (text, farbe) in enumerate(schritte):
            kasten(ax, x_links, y, 3.6, 0.62, text, farbe)
            if nr < len(schritte) - 1:
                pfeil(ax, x_links + 1.8, y - 0.04, y - 0.30)
            y -= 0.92

    ax.text(5.0, 3.30, "einzige\nÄnderung", ha="center", va="center",
            fontsize=10.5, color=GRUEN, fontweight="bold")
    ax.add_patch(FancyArrowPatch((4.35, 2.72), (5.75, 2.72),
                                 arrowstyle="-|>", mutation_scale=15,
                                 linewidth=2, color=GRUEN))

    ax.text(5.0, 0.85,
            "Verbuchungsart des Bausteins Z_PP_PRDDAT_SET: "
            "von „Start sofort“ (V1) auf „Start verzögert“ (V2)",
            ha="center", fontsize=9.5, color=GRAU)

    fig.savefig(BILD, dpi=200, bbox_inches="tight", facecolor="white")
    plt.close(fig)


def ueberschrift(dok, text, ebene):
    p = dok.add_heading(text, level=ebene)
    for lauf in p.runs:
        lauf.font.color.rgb = RGBColor(0x1F, 0x30, 0x50)
    return p


def fett_dann_text(dok, fett, text):
    p = dok.add_paragraph()
    p.add_run(fett).bold = True
    p.add_run(text)
    return p


def dokument_erzeugen() -> None:
    dok = Document()
    stil = dok.styles["Normal"]
    stil.font.name = "Calibri"
    stil.font.size = Pt(10.5)

    titel = dok.add_heading("Produktionsdatum im Fertigungsauftrag", level=0)
    titel.alignment = WD_ALIGN_PARAGRAPH.LEFT
    unter = dok.add_paragraph(
        "Feld AUFK-ZZPRDAT – Analyse und Lösung im Testsystem T76")
    unter.runs[0].italic = True
    dok.add_paragraph("Stand: 3. September 2026, Ingo Kohler, IT Analytics")

    ueberschrift(dok, "Worum es geht", 1)
    dok.add_paragraph(
        "Seit Oktober 2025 gibt es im Fertigungsauftrag den Reiter „Trafag Daten“ "
        "mit dem Feld Produktionsdatum. Ziel ist ein einheitliches Datum je Auftrag, das "
        "nicht mehr vom Eckendtermin abhängt: Wird der Eckendtermin später "
        "verschoben, soll das ursprüngliche Produktionsdatum auf den Etiketten "
        "bestehen bleiben."
    )
    dok.add_paragraph(
        "Das Feld wurde jedoch nur dann gefüllt, wenn der Anwender beim Anlegen oder "
        "Ändern des Auftrags den Reiter „Trafag Daten“ tatsächlich "
        "anklickte. Bei der Sammelfreigabe über CO40 und COHV sowie bei der Umsetzung "
        "von Planaufträgen über MD04 und CO41 ist das gar nicht möglich. "
        "Marco Di Menco konnte die Etiketten deshalb nicht auf das Produktionsdatum "
        "umstellen."
    )
    fett_dann_text(
        dok, "Messbarer Ausgangszustand: ",
        "Von den 34 Aufträgen aus Marcos Liste (1214601 bis 1214634) waren 32 "
        "freigegeben – und kein einziger trug ein Produktionsdatum."
    )

    ueberschrift(dok, "Der Lösungsweg", 1)
    dok.add_paragraph(
        "Lucas Castro hatte den richtigen Weg bereits benannt: das Business Add-In "
        "WORKORDER_UPDATE. SAP ruft es bei jedem Speichern eines Fertigungsauftrags auf, "
        "unabhängig davon, welche Bildschirmmaske der Anwender besucht hat. Damit "
        "entfällt die Abhängigkeit vom Dynpro."
    )
    dok.add_paragraph(
        "Die Umsetzung erfolgte im Testsystem T76, Mandant 100, ausschliesslich als lokale "
        "Objekte im Paket $TMP. Es wurde kein Transportauftrag angelegt, und das "
        "Produktivsystem P76 wurde zu keinem Zeitpunkt berührt."
    )

    ueberschrift(dok, "Warum es zunächst trotzdem nicht funktionierte", 1)
    dok.add_paragraph(
        "Die Erweiterung war korrekt eingebaut und aktiv, und trotzdem blieb das Feld leer. "
        "Elf Testaufträge waren nötig, um die Ursache einzugrenzen. Es waren zwei "
        "voneinander unabhängige Ursachen, und keine davon war am Quelltext erkennbar."
    )

    ueberschrift(dok, "Ursache 1: Der SAP-Standard überschrieb den Wert", 2)
    dok.add_paragraph(
        "Unser Baustein war als Verbuchungsbaustein mit der Verarbeitungsart "
        "„Start sofort“ eingerichtet. Er schrieb das Produktionsdatum korrekt in "
        "die Tabelle AUFK. Unmittelbar danach schrieb die SAP-Standardverbuchung dieselbe "
        "Tabellenzeile vollständig aus ihrem eigenen Puffer, und in diesem Puffer war "
        "das Feld leer. Der Wert wurde also gesetzt und Sekundenbruchteile später "
        "wieder zugedeckt."
    )
    fett_dann_text(
        dok, "Lösung: ",
        "Die Verarbeitungsart des Bausteins wurde auf „Start verzögert“ "
        "umgestellt. Solche Bausteine laufen erst, wenn die gesamte Standardverbuchung "
        "abgeschlossen ist. Das Produktionsdatum wird damit als Letztes geschrieben und "
        "bleibt stehen."
    )

    ueberschrift(
        dok, "Ursache 2: Werte dürfen nicht als Festwert übergeben werden", 2)
    dok.add_paragraph(
        "Beim Vormerken eines Verbuchungsbausteins speichert SAP die Übergabewerte "
        "zwischen und liest sie beim späteren Ausführen wieder ein. Dabei muss "
        "der Datentyp exakt übereinstimmen. Ein als Text übergebenes Datum "
        "führte zum Abbruch der gesamten Verbuchung mit der Meldung "
        "„Verbuchung wurde abgebrochen“."
    )
    fett_dann_text(
        dok, "Lösung: ",
        "Alle Werte werden über typisierte Variablen übergeben. Dieser Punkt "
        "gehört ausdrücklich in die Dokumentation, weil der Fehler erst zur "
        "Laufzeit auftritt und die Syntaxprüfung ihn nicht findet."
    )

    ueberschrift(dok, "Der Ablauf vorher und nachher", 1)
    dok.add_picture(str(BILD), width=Cm(17))
    bu = dok.add_paragraph(
        "Der einzige Unterschied ist der Zeitpunkt, zu dem unser Baustein läuft."
    )
    bu.runs[0].italic = True
    bu.runs[0].font.size = Pt(9)

    ueberschrift(dok, "Nachweis", 1)
    dok.add_paragraph(
        "Auftrag 1241812 im System T76, Material 36385, Werk 1100, Auftragsart PP21. "
        "Der Reiter „Trafag Daten“ wurde zu keinem Zeitpunkt geöffnet."
    )

    tab = dok.add_table(rows=1, cols=4)
    tab.style = "Light Grid Accent 1"
    kopf = tab.rows[0].cells
    for i, text in enumerate(["Schritt", "Eckendtermin", "Produktionsdatum", "Ergebnis"]):
        kopf[i].text = text
        for absatz in kopf[i].paragraphs:
            for lauf in absatz.runs:
                lauf.bold = True

    for zeile in [
        ("Auftrag angelegt, noch nicht freigegeben", "20.11.2026", "leer", "wie erwartet"),
        ("Auftrag freigegeben", "20.11.2026", "20.11.2026", "wird gesetzt"),
        ("Eckendtermin verschoben", "09.12.2026", "20.11.2026", "bleibt stehen"),
    ]:
        zellen = tab.add_row().cells
        for i, text in enumerate(zeile):
            zellen[i].text = text

    dok.add_paragraph()
    p = dok.add_paragraph()
    p.add_run(
        "Damit ist die fachliche Anforderung erfüllt: Das Produktionsdatum entsteht "
        "mit der Freigabe und überlebt eine spätere Terminverschiebung."
    ).bold = True

    ueberschrift(dok, "Was noch offen ist", 1)
    dok.add_paragraph(
        "Ein Auftrag, der in einem einzigen Vorgang über CO01 angelegt und sofort "
        "freigegeben wird, bleibt bislang leer. Zum Zeitpunkt der Freigabe hat er noch "
        "keine endgültige Nummer, sondern eine vorläufige Kennung. Der "
        "Schreibvorgang findet den Auftrag deshalb nicht."
    )
    dok.add_paragraph(
        "Das ist kein Randfall. Laut Adil Lahrach entstehen die Aufträge im Werk CH "
        "über MD04 und in CZ teilweise über CO41, also ebenfalls durch Umsetzung "
        "von Planaufträgen. Ob dort die endgültige Nummer bereits vorliegt, ist "
        "noch nicht gemessen. Vor einer Zusage an die Disposition muss dieser Punkt "
        "geklärt sein."
    )
    dok.add_paragraph(
        "Ebenfalls offen und bewusst getrennt zu entscheiden: der Transport nach P76 und "
        "das nachträgliche Füllen der Altbestände. Beides erst nach "
        "fachlicher Abnahme durch Lucas Castro, Florian Wächter und Marco Di Menco."
    )

    ueberschrift(dok, "Nächste Schritte", 1)
    for text in [
        "Verhalten bei der Umsetzung von Planaufträgen über MD04 und CO41 messen.",
        "Sammelfreigabe über CO40 und COHV testen, mit der Disposition abgestimmt.",
        "Marco prüft, ob Verpackungsetikett und Typenschild dasselbe Feld verwenden.",
        "Erst danach über den Transport nach P76 entscheiden.",
        "Getrennt davon: Altbestände nachfüllen, ausschliesslich für leere Felder.",
    ]:
        dok.add_paragraph(text, style="List Number")

    fuss = dok.add_paragraph(
        "Technische Einzelheiten, Messwerte und der vollständige Verlauf der Analyse: "
        "saptasks/zzprdat-kontext.md im Repository TrafagSalesExporter."
    )
    fuss.runs[0].font.size = Pt(8.5)
    fuss.runs[0].italic = True

    DOKUMENT.parent.mkdir(parents=True, exist_ok=True)
    dok.save(DOKUMENT)
    print(f"Schaubild: {BILD}")
    print(f"Dokument:  {DOKUMENT}")


if __name__ == "__main__":
    schaubild_erzeugen()
    dokument_erzeugen()
