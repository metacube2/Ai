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
        ("Z_ZZPRDAT_SET als V1\nschreibt ZZPRDAT", GRAU),
        ("SAP-Standardverbuchung schreibt\ndie ganze AUFK-Zeile\naus ihrem eigenen Puffer", ROT),
        ("ZZPRDAT ist wieder leer", ROT),
    ]
    schritte_neu = [
        ("Auftrag wird freigegeben\nund gesichert", GRAU),
        ("BAdI WORKORDER_UPDATE\nAT_RELEASE läuft", GRAU),
        ("Z_ZZPRDAT_SET wird als V2\nnur vorgemerkt", GRUEN),
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
            "Verbuchungsart des Bausteins Z_ZZPRDAT_SET: "
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
    dok.add_paragraph("Stand: 4. September 2026, Ingo Kohler, IT Analytics")

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
        "Die Umsetzung erfolgte im Testsystem T76, Mandant 100. Die Objekte liegen im Paket "
        "ZPP1 und vollständig im Transportauftrag T76K912490. Dieser Auftrag ist bewusst "
        "nicht freigegeben: transportiert wird erst nach der fachlichen Abnahme. Das "
        "Produktivsystem P76 wurde zu keinem Zeitpunkt berührt."
    )

    ueberschrift(dok, "Warum es zunächst trotzdem nicht funktionierte", 1)
    dok.add_paragraph(
        "Die Erweiterung war korrekt eingebaut und aktiv, und trotzdem blieb das Feld leer. "
        "Zwölf Testaufträge waren nötig, um die Ursachen einzugrenzen. Es waren drei "
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

    ueberschrift(
        dok, "Ursache 3: Beim Anlegen hat der Auftrag noch keine Nummer", 2)
    dok.add_paragraph(
        "Wird ein Auftrag über CO01 in einem einzigen Vorgang angelegt und sofort "
        "freigegeben, trägt er zum Zeitpunkt der Freigabe noch eine vorläufige "
        "Kennung statt seiner endgültigen Nummer. Der Schreibvorgang findet den "
        "Auftrag deshalb nicht. Über CO02 freigegebene Aufträge waren davon nicht "
        "betroffen, weil ihre Nummer bereits vergeben war."
    )
    fett_dann_text(
        dok, "Lösung: ",
        "Die Erweiterung wurde zusätzlich an einer zweiten Stelle des Business "
        "Add-In verankert, die unmittelbar vor dem Speichern läuft und die "
        "endgültigen Auftragsdaten kennt. Damit sind das Anlegen mit sofortiger "
        "Freigabe, die spätere Freigabe und die Sammelfreigabe gleichermassen "
        "abgedeckt."
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
        "Geprüft wurden vier Wege, auf denen ein Fertigungsauftrag entstehen und "
        "freigegeben werden kann. Alle Aufträge im System T76, Material 36385, "
        "Werk 1100, Auftragsart PP21. Der Reiter „Trafag Daten“ wurde in keinem "
        "einzigen Fall geöffnet."
    )
    p = dok.add_paragraph()
    p.add_run("Der wichtigste Nachweis ist die Sammelfreigabe über COHV. ").bold = True
    p.add_run(
        "Dort gibt es überhaupt keine Bildschirmmaske, die man besuchen könnte — "
        "genau daran musste die bisherige Lösung scheitern. Dass das Datum auch dort "
        "gesetzt wird, zeigt, dass die Abhängigkeit vom Dynpro vollständig "
        "aufgelöst ist."
    )

    tab = dok.add_table(rows=1, cols=5)
    tab.style = "Light Grid Accent 1"
    kopf = tab.rows[0].cells
    for i, text in enumerate(
            ["Auftrag", "Schritt", "Eckendtermin", "Produktionsdatum", "Ergebnis"]):
        kopf[i].text = text
        for absatz in kopf[i].paragraphs:
            for lauf in absatz.runs:
                lauf.bold = True

    for zeile in [
        ("1241812", "angelegt, noch nicht freigegeben", "20.11.2026", "leer",
         "wie erwartet"),
        ("1241812", "in CO02 freigegeben", "20.11.2026", "20.11.2026",
         "wird gesetzt"),
        ("1241812", "Eckendtermin verschoben", "09.12.2026", "20.11.2026",
         "bleibt stehen"),
        ("1241813", "in CO01 angelegt und freigegeben", "27.11.2026", "27.11.2026",
         "wird gesetzt"),
        ("1241813", "Eckendtermin verschoben", "18.12.2026", "27.11.2026",
         "bleibt stehen"),
        ("1241814", "Sammelfreigabe über COHV", "04.12.2026", "04.12.2026",
         "wird gesetzt"),
        ("1241815", "Sammelfreigabe über COHV", "04.12.2026", "04.12.2026",
         "wird gesetzt"),
        ("1241816", "Planauftrag über CO40 umgesetzt", "09.12.2026", "09.12.2026",
         "wird gesetzt"),
        ("1241817", "angelegt, noch nicht freigegeben", "02.10.2026", "leer",
         "wie erwartet"),
        ("1241817", "in CO02 freigegeben", "02.10.2026", "02.10.2026",
         "wird gesetzt"),
        ("1241817", "Eckendtermin verschoben", "20.10.2026", "02.10.2026",
         "bleibt stehen"),
        ("1241818", "in CO01 angelegt und freigegeben", "05.10.2026", "05.10.2026",
         "wird gesetzt"),
        ("1241819", "Sammelfreigabe über COHV", "08.10.2026", "08.10.2026",
         "wird gesetzt"),
        ("1241820", "Planauftrag über CO40 umgesetzt", "12.10.2026", "12.10.2026",
         "wird gesetzt"),
        ("1241821", "Auftragsart PP22, angelegt und freigegeben", "15.10.2026",
         "15.10.2026", "wird gesetzt"),
        ("1241822", "Planauftrag über MD04 umgesetzt", "19.10.2026", "19.10.2026",
         "wird gesetzt"),
        ("1241823", "Sammelumsetzung über CO41", "22.10.2026", "leer",
         "CO41 gibt nicht frei"),
        ("1241823", "derselbe Auftrag in CO02 freigegeben", "22.10.2026",
         "22.10.2026", "wird gesetzt"),
    ]:
        zellen = tab.add_row().cells
        for i, text in enumerate(zeile):
            zellen[i].text = text

    dok.add_paragraph()
    dok.add_paragraph(
        "Die Aufträge 1241812 bis 1241816 wurden am 3. September auf dem Testaufbau "
        "gemessen. Danach sind dieselben Bausteine mit endgültigen Namen im Paket ZPP1 "
        "neu aufgebaut und in den Transportauftrag gelegt worden. Die Aufträge 1241817 "
        "bis 1241823 sind die Wiederholung auf genau diesem Stand, erweitert um die "
        "Auftragsart PP22 sowie die Wege MD04 und CO41 — "
        "also auf den Objekten, die später ausgeliefert werden."
    )
    p = dok.add_paragraph()
    p.add_run(
        "Damit ist die fachliche Anforderung erfüllt: Das Produktionsdatum entsteht "
        "mit der Freigabe und überlebt eine spätere Terminverschiebung."
    ).bold = True

    ueberschrift(dok, "Nachmessung vom 7. September nach einer Korrektur", 1)
    dok.add_paragraph(
        "Bei einer Gegenprüfung ist am 7. September ein Fehler in der Lösung aufgefallen, "
        "der in den bisherigen Tests nicht sichtbar war. Er ist am selben Tag behoben "
        "worden, und danach wurde die komplette Reihe wiederholt. Der Punkt gehört hierher, "
        "weil er zeigt, wonach bei der Abnahme zu schauen ist."
    )
    p = dok.add_paragraph()
    p.add_run("Was falsch war. ").bold = True
    p.add_run(
        "Die Logik hängt an einem Zeitpunkt, den SAP bei jedem Sichern eines "
        "Fertigungsauftrags durchläuft, nicht nur bei der Freigabe. Geprüft wurde bis dahin "
        "nur, ob der Auftrag überhaupt schon einmal freigegeben wurde und ob das Feld noch "
        "leer ist. Auf einen alten, längst freigegebenen Auftrag mit leerem Feld trifft "
        "beides zu: Er hätte beim nächsten beliebigen Sichern den heute gültigen "
        "Eckendtermin bekommen, nicht den vom Tag seiner Freigabe."
    )
    p = dok.add_paragraph()
    p.add_run("Warum das nicht auffiel. ").bold = True
    p.add_run(
        "Alle bisherigen Messungen liefen mit neu angelegten Aufträgen, bei denen Freigabe "
        "und erstes Sichern zusammenfallen. Der Fall „alter Auftrag wird erneut gesichert“ "
        "kam darin nicht vor."
    )
    p = dok.add_paragraph()
    p.add_run("Was geändert wurde. ").bold = True
    p.add_run(
        "Vor dem Schreiben wird jetzt nachgesehen, ob der Auftrag schon vor diesem Sichern "
        "freigegeben war. Wenn ja, passiert nichts. Damit bleibt das nachträgliche Füllen "
        "bestehender Aufträge das, was es sein sollte: ein eigener, bewusst zu "
        "entscheidender Schritt."
    )

    tab = dok.add_table(rows=1, cols=4)
    tab.style = "Light Grid Accent 1"
    kopf = tab.rows[0].cells
    for i, text in enumerate(["Auftrag", "Weg", "Produktionsdatum", "Ergebnis"]):
        kopf[i].text = text
        for absatz in kopf[i].paragraphs:
            for lauf in absatz.runs:
                lauf.bold = True

    for zeile in [
        ("1241819", "alter, bereits freigegebener Auftrag; Eckendtermin verschoben "
                    "und gesichert", "bleibt leer", "so soll es sein"),
        ("1241824", "CO01 anlegen und freigeben", "13.11.2026", "wird gesetzt"),
        ("1241824", "danach Eckendtermin verschoben", "13.11.2026", "bleibt stehen"),
        ("1241825", "in CO02 freigegeben", "27.11.2026", "wird gesetzt"),
        ("1241826", "Auftragsart PP22, angelegt und freigegeben", "04.12.2026",
         "wird gesetzt"),
        ("1241827", "Sammelfreigabe über COHV", "09.12.2026", "wird gesetzt"),
        ("1241828", "Planauftrag über CO40 umgesetzt", "15.12.2026", "wird gesetzt"),
        ("1241829", "Planauftrag über MD04 umgesetzt", "22.12.2026", "wird gesetzt"),
        ("1241830", "Sammelumsetzung über CO41", "leer", "CO41 gibt nicht frei"),
        ("1241830", "derselbe Auftrag in CO02 freigegeben", "23.12.2026",
         "wird gesetzt"),
    ]:
        zellen = tab.add_row().cells
        for i, text in enumerate(zeile):
            zellen[i].text = text

    dok.add_paragraph()
    dok.add_paragraph(
        "Die Werte sind diesmal nicht über den Nachweisreport abgelesen, sondern direkt aus "
        "der Datenbanktabelle des Auftragskopfs. Der Report hätte den Fehler nämlich nicht "
        "gezeigt: Ein so nachgefüllter Altauftrag sieht dort wie ein Erfolgsfall aus."
    )
    p = dok.add_paragraph()
    p.add_run(
        "Die zweite Zeile ist die wichtigste. Ohne einen Auftrag, der weiterhin ein Datum "
        "bekommt, wäre „das Feld bleibt leer“ auch damit erklärbar, dass die Lösung gar "
        "nicht mehr läuft."
    ).bold = True

    ueberschrift(dok, "Ein Sonderfall, den man kennen sollte: CO41", 1)
    dok.add_paragraph(
        "Die Sammelumsetzung über CO41 erzeugt den Fertigungsauftrag und sichert ihn, "
        "gibt ihn aber nicht frei. In diesem Zwischenzustand bleibt das Produktionsdatum "
        "leer — und das ist richtig so, denn ohne Freigabe darf kein Datum entstehen. "
        "Es wird gesetzt, sobald der Auftrag später freigegeben wird. Wer das nicht "
        "weiß, könnte den Zwischenzustand für einen Fehler halten."
    )
    dok.add_paragraph(
        "MD04 verhält sich anders: Der Weg „→ FertAuftrag“ führt direkt in die "
        "Anlagemaske und gibt beim Sichern frei, genau wie CO40."
    )

    ueberschrift(dok, "So testen Sie es selbst", 1)
    dok.add_paragraph(
        "Alles Folgende passiert im Testsystem T76, Mandant 100. Im Produktivsystem "
        "P76 ist nichts geändert, und es kann dort auch nichts passieren, solange der "
        "Transportauftrag nicht freigegeben ist."
    )
    dok.add_paragraph(
        "Das Produktionsdatum steht im Auftragskopf auf dem Reiter „Trafag Daten“. "
        "Der Reiter muss zum Setzen des Wertes nicht mehr geöffnet werden — genau das "
        "war der Fehler. Zum Nachsehen öffnen Sie ihn natürlich schon."
    )
    p = dok.add_paragraph()
    p.add_run("Vorschlag für die Testdaten: ").bold = True
    p.add_run(
        "Material 36385, Werk 1100, Auftragsart PP21 oder PP22. Jeder andere "
        "Fertigungsauftrag geht genauso. Wichtig ist nur, einen "
    )
    p.add_run("neu angelegten").bold = True
    p.add_run(
        " Auftrag zu nehmen: Die Logik schreibt ausschließlich in ein noch leeres Feld, "
        "und auf einem alten Auftrag mit bereits gefülltem Datum sähe man nicht, ob "
        "überhaupt etwas passiert ist."
    )

    ueberschrift(dok, "Testfall 1: Freigabe setzt das Datum", 2)
    for text in [
        "In CO01 einen Fertigungsauftrag anlegen, einen Eckendtermin eintragen und "
        "sichern, ohne freizugeben.",
        "Den Auftrag in CO02 öffnen und auf dem Reiter „Trafag Daten“ nachsehen: "
        "Das Produktionsdatum ist leer. So soll es sein, ohne Freigabe entsteht kein Datum.",
        "Denselben Auftrag in CO02 freigeben und sichern.",
        "Wieder auf „Trafag Daten“ nachsehen: Jetzt steht dort der Eckendtermin.",
    ]:
        dok.add_paragraph(text, style="List Number")

    ueberschrift(dok, "Testfall 2: Das Datum bleibt stehen", 2)
    dok.add_paragraph(
        "Das ist der eigentliche Kern der Anforderung und der Punkt, an dem die alte "
        "Lösung falsch lag."
    )
    for text in [
        "Denselben, jetzt freigegebenen Auftrag in CO02 öffnen.",
        "Den Eckendtermin auf ein anderes Datum verschieben und sichern.",
        "Auf „Trafag Daten“ nachsehen: Das Produktionsdatum steht unverändert auf dem "
        "ursprünglichen Termin. Es wandert nicht mit.",
    ]:
        dok.add_paragraph(text, style="List Number")

    ueberschrift(dok, "Testfall 3: Die Wege ohne Bildschirmmaske", 2)
    dok.add_paragraph(
        "Diese Wege sind uns am wichtigsten, weil es dort gar keine Maske gibt, die "
        "jemand hätte besuchen können. Jeder für sich mit einem neuen Auftrag:"
    )
    for text in [
        "In CO01 anlegen und gleich beim Sichern freigeben.",
        "Über COHV mehrere Aufträge auf einmal freigeben (Sammelfreigabe).",
        "Einen Planauftrag über CO40 in einen Fertigungsauftrag umsetzen.",
        "Einen Planauftrag über MD04 mit „→ FertAuftrag“ umsetzen.",
    ]:
        dok.add_paragraph(text, style="List Bullet")
    dok.add_paragraph(
        "In allen vier Fällen muss nach dem Sichern auf „Trafag Daten“ der Eckendtermin "
        "als Produktionsdatum stehen."
    )

    ueberschrift(dok, "Was ein Fehler wäre", 2)
    tab = dok.add_table(rows=1, cols=2)
    tab.style = "Light Grid Accent 1"
    kopf = tab.rows[0].cells
    for i, text in enumerate(["Beobachtung", "Bedeutung"]):
        kopf[i].text = text
        for absatz in kopf[i].paragraphs:
            for lauf in absatz.runs:
                lauf.bold = True
    for zeile in [
        ("Nach der Freigabe bleibt das Feld leer",
         "Fehler. Bitte Auftragsnummer und Weg notieren und uns melden."),
        ("Das Datum ändert sich beim Verschieben des Eckendtermins",
         "Fehler. Genau das soll nicht mehr passieren."),
        ("Ein über CO41 umgesetzter Auftrag hat kein Datum",
         "Kein Fehler. CO41 gibt nicht frei, siehe eigener Abschnitt."),
        ("Ein alter Auftrag bekommt kein Datum",
         "Kein Fehler. Bestehende Aufträge werden nicht nachträglich gefüllt, "
         "das ist ein eigener, später zu entscheidender Schritt."),
        ("Eine Meldung „Verbuchung wurde abgebrochen“",
         "Fehler, und zwar ein ernster. Bitte sofort melden."),
    ]:
        zellen = tab.add_row().cells
        for i, text in enumerate(zeile):
            zellen[i].text = text

    dok.add_paragraph()
    dok.add_paragraph(
        "Wer es technisch nachprüfen möchte, statt in jeden Auftrag zu schauen: Der "
        "Report Z_ZZPRDAT_CHECK in SE38 nimmt eine Liste von Auftragsnummern entgegen "
        "und zeigt je Auftrag den Eckendtermin, das Produktionsdatum und den "
        "Freigabestatus. Er liest nur und ändert nichts."
    )
    dok.add_paragraph(
        "Fällt etwas auf, lässt sich die Lösung sofort abschalten, auch nach einem "
        "Import: in SE19 die Implementierung Z_ZZPRDAT_UPDATE deaktivieren. Danach "
        "verhält sich das System wieder wie vorher."
    )

    ueberschrift(dok, "Was noch aussteht", 1)
    dok.add_paragraph(
        "Technisch ist nichts mehr offen. Alle sieben Wege sind gemessen, "
        "einschließlich MD04, CO41 und der Auftragsart PP22, die bei 10 der 34 Aufträge "
        "aus Marcos Liste vorkommt."
    )
    dok.add_paragraph(
        "Die verwendeten Planaufträge wurden mit dem Profil „Lagerauftrag“ angelegt. "
        "Das ist ein neutraler technischer Testfall. "
        "Ob der reale Ablauf im Werk CH mit diesem oder einem anderen Profil arbeitet, "
        "ist eine Stammdatenfrage und gehört zu Fabio Palma und Daniel Tobler. "
        "Für die Frage, ob die Umsetzung das Datum schreibt, spielt das Profil "
        "keine Rolle."
    )
    dok.add_paragraph(
        "Offen sind zwei fachliche Punkte: die Abnahme selbst, und Marcos Prüfung, "
        "ob Verpackungsetikett und Typenschild dasselbe Feld verwenden."
    )
    dok.add_paragraph(
        "Der Import nach P76 greift in Planung und Fertigung ein. Fabio Palma hat "
        "ausdrücklich darum gebeten, bei Änderungen im PP-Modul einbezogen zu werden."
    )
    dok.add_paragraph(
        "Die Freigabe des Transportauftrags und das nachträgliche Füllen der Altbestände "
        "sind bewusst zwei getrennte Entscheidungen. Beides erst nach fachlicher Abnahme "
        "durch Lucas Castro, Florian Wächter und Marco Di Menco."
    )

    ueberschrift(dok, "Nächste Schritte", 1)
    for text in [
        "Marco prüft, ob Verpackungsetikett und Typenschild dasselbe Feld verwenden.",
        "Erst danach den Transportauftrag T76K912490 freigeben.",
        "Getrennt davon und erst nach dem Import in P76: Altbestände nachfüllen, "
        "ausschliesslich für leere Felder.",
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
