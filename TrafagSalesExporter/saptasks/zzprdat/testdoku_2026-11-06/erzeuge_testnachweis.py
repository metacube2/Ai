"""Erzeugt den Word-Testnachweis ZZPRDAT vom 06.11.2026 (T76, Auftrag 1241851).

Aufruf aus der Repository-Wurzel (Python mit python-docx aus dem uv-Cache):
    C:/Users/koi/AppData/Local/uv/cache/archive-v0/gQxxjztGPDnpKX0M/Scripts/python.exe -I -X utf8 \
        saptasks/zzprdat/testdoku_2026-11-06/erzeuge_testnachweis.py

Die Bilder stammen aus SAP GUI (HardCopy in eigener T76-Sitzung), die Ausschnitte
aus ausschnitte.ps1 (nur zugeschnitten, nicht bearbeitet). Das Dokument verwendet
echte Umlaute, weil es an den Fachbereich geht.
"""

from __future__ import annotations

from pathlib import Path

from docx import Document
from docx.enum.table import WD_TABLE_ALIGNMENT
from docx.enum.text import WD_ALIGN_PARAGRAPH
from docx.oxml import OxmlElement
from docx.oxml.ns import qn
from docx.shared import Cm, Pt, RGBColor

HIER = Path(__file__).resolve().parent
BILDER = HIER / "bilder"
AUSSCHNITT = BILDER / "ausschnitt"
DOKUMENT = HIER / "ZZPRDAT_Testnachweis_2026-11-06.docx"

ROT = RGBColor(0xB2, 0x3A, 0x48)
GRUEN = RGBColor(0x2E, 0x7D, 0x5B)
GRAU = RGBColor(0x55, 0x55, 0x55)


def zelle_schattieren(zelle, farbe_hex: str) -> None:
    tc_pr = zelle._tc.get_or_add_tcPr()
    shd = OxmlElement("w:shd")
    shd.set(qn("w:val"), "clear")
    shd.set(qn("w:color"), "auto")
    shd.set(qn("w:fill"), farbe_hex)
    tc_pr.append(shd)


def tabelle(doc: Document, kopf: list[str], zeilen: list[list[str]], breiten_cm: list[float]):
    t = doc.add_table(rows=1, cols=len(kopf))
    t.style = "Table Grid"
    t.alignment = WD_TABLE_ALIGNMENT.LEFT
    for i, text in enumerate(kopf):
        z = t.rows[0].cells[i]
        z.text = ""
        r = z.paragraphs[0].add_run(text)
        r.bold = True
        zelle_schattieren(z, "D9E2EC")
    for zeile in zeilen:
        zellen = t.add_row().cells
        for i, text in enumerate(zeile):
            zellen[i].text = text
    for zeile in t.rows:
        for i, b in enumerate(breiten_cm):
            zeile.cells[i].width = Cm(b)
    for zeile in t.rows:
        for z in zeile.cells:
            for p in z.paragraphs:
                for r in p.runs:
                    r.font.size = Pt(9.5)
    doc.add_paragraph()
    return t


def bild(doc: Document, pfad: Path, breite_cm: float, unterschrift: str) -> None:
    doc.add_picture(str(pfad), width=Cm(breite_cm))
    doc.paragraphs[-1].alignment = WD_ALIGN_PARAGRAPH.LEFT
    p = doc.add_paragraph()
    r = p.add_run(unterschrift)
    r.italic = True
    r.font.size = Pt(8.5)
    r.font.color.rgb = GRAU


def merker(doc: Document, text: str, farbe: RGBColor) -> None:
    p = doc.add_paragraph()
    r = p.add_run(text)
    r.bold = True
    r.font.size = Pt(13)
    r.font.color.rgb = farbe


def satz(doc: Document, beschriftung: str, text: str) -> None:
    p = doc.add_paragraph()
    r = p.add_run(beschriftung + " ")
    r.bold = True
    p.add_run(text)


def schritt(doc: Document, nr: int, titel: str, eingabe: str, ergebnis: str) -> None:
    doc.add_heading(f"Schritt {nr} – {titel}", level=2)
    satz(doc, "Eingegeben/geklickt:", eingabe)
    satz(doc, "Erwartet/Ergebnis:", ergebnis)


def main() -> None:
    doc = Document()
    abschnitt = doc.sections[0]
    abschnitt.page_width = Cm(21.0)
    abschnitt.page_height = Cm(29.7)
    for rand in ("left_margin", "right_margin"):
        setattr(abschnitt, rand, Cm(2.0))
    abschnitt.top_margin = Cm(1.8)
    abschnitt.bottom_margin = Cm(1.8)
    doc.styles["Normal"].font.name = "Calibri"
    doc.styles["Normal"].font.size = Pt(10.5)

    doc.add_heading("ZZPRDAT – Testnachweis Produktionsdatum im Fertigungsauftrag (T76, 06.11.2026)", level=1)

    doc.add_heading("Zweck", level=2)
    doc.add_paragraph(
        "Dieses Dokument zeigt Schritt für Schritt mit Bildschirmfotos aus SAP, wie sich das "
        "Produktionsdatum (Feld ZZPRDAT, Reiter „TRAFAG Daten“) im Fertigungsauftrag verhält. "
        "Geprüft werden drei Aussagen: Erstens wird das Datum bei der Freigabe gesetzt, und zwar auf "
        "den Eckendtermin, der zu diesem Zeitpunkt gilt. Zweitens wird es im Reiter „TRAFAG Daten“ "
        "angezeigt, in CO02 und CO03. Drittens bleibt es stehen, wenn man die Reiter wechselt und "
        "wenn der Eckendtermin später verschoben wird."
    )
    p = doc.add_paragraph()
    r = p.add_run(
        "Hinweis: Der Test lief im Testsystem T76 (Mandant 100). Im Produktivsystem P76 wurde nichts "
        "geändert. Die Lösung liegt im Transportauftrag T76K912490, der noch nicht freigegeben und "
        "nicht nach P76 importiert ist."
    )
    r.bold = True

    doc.add_heading("Testdaten", level=2)
    tabelle(
        doc,
        ["Angabe", "Wert"],
        [
            ["System", "T76, Mandant 100 (Testsystem, Datenstand Kopie Frühjahr 2026)"],
            ["Material", "36385 (9M0 PICOSTAT REGLER MEMBRAN)"],
            ["Werk", "1100"],
            ["Auftragsart", "PP22"],
            ["Menge", "5 ST"],
            ["Eckendtermin bei der Freigabe", "27.11.2026"],
            ["Auftragsnummer", "1241851"],
            ["Datum und Uhrzeit", "06.11.2026, 10:29 bis 10:37 Uhr"],
            ["Benutzer", "KOI"],
            ["Transportauftrag der Lösung", "T76K912490 (nicht freigegeben)"],
        ],
        [5.5, 11.5],
    )

    # ---------------------------------------------------------------- CO01
    doc.add_heading("Teil A – Auftrag anlegen und freigeben (CO01)", level=1)

    schritt(
        doc, 1, "CO01 – Einstieg ausfüllen",
        "Transaktion CO01, Material 36385, Produktionswerk 1100, Auftragsart PP22, danach Enter.",
        "Das Kopfbild des neuen Auftrags öffnet sich.",
    )
    bild(doc, BILDER / "01_co01_einstieg.png", 16, "Bild 1: CO01 Einstieg mit Material, Werk und Auftragsart.")

    schritt(
        doc, 2, "CO01 – Menge und Eckendtermin im Reiter Allgemein",
        "Gesamtmenge 5, Ecktermin Ende 27.11.2026, danach Enter.",
        "Die Werte stehen im Reiter Allgemein; der Auftrag ist noch nicht gesichert und hat noch keine Nummer.",
    )
    bild(doc, BILDER / "02_co01_kopf_eingabe.png", 16, "Bild 2: Kopf mit Menge 5 ST und Ecktermin Ende 27.11.2026.")
    bild(doc, AUSSCHNITT / "02_co01_kopf_eingabe.png", 11, "Bild 2a: Ausschnitt aus Bild 2 (Mengen und Termine).")

    schritt(
        doc, 3, "CO01 – Meldungen nach Enter",
        "Materialstatusprüfung (Komponente D24057 mit Status „Zeichn.änd. (aufbrauchen)“): „Ja“, "
        "Komponente übernehmen. Information „Fehlende Materialverfügbarkeit“: „Weiter“. Bei der Auftragsart "
        "PP22 bietet SAP danach sofort „Auftrag freigeben“ an. Hier wurde bewusst „Abbrechen“ gewählt, "
        "damit der Zustand vor der Freigabe festgehalten werden kann.",
        "SAP meldet „Freigabe abgelehnt (Bitte Protokoll beachten)“; der Auftrag bleibt eröffnet (Status EROF).",
    )
    bild(doc, BILDER / "03_dialog_materialstatus.png", 10, "Bild 3: Materialstatusprüfung, beantwortet mit „Ja“.")
    bild(doc, BILDER / "04_dialog_information.png", 10, "Bild 4: Information zur Materialverfügbarkeit, bestätigt mit „Weiter“.")
    bild(doc, BILDER / "05_dialog_freigeben_nach_enter.png", 10,
         "Bild 5: Angebot „Auftrag freigeben“ direkt nach Enter; hier „Abbrechen“, um den Stand vor der Freigabe zu zeigen.")

    schritt(
        doc, 4, "CO01 – Reiter „TRAFAG Daten“ vor der Freigabe",
        "Reiter „TRAFAG Daten“ angeklickt.",
        "Status EROF (eröffnet, nicht freigegeben). Das Produktionsdatum ist leer.",
    )
    merker(doc, "VORHER: Produktionsdatum leer (Status EROF)", ROT)
    bild(doc, BILDER / "06_co01_trafag_daten_vorher_leer.png", 16, "Bild 6: Reiter „TRAFAG Daten“ vor der Freigabe.")
    bild(doc, AUSSCHNITT / "06_co01_trafag_daten_vorher_leer.png", 16, "Bild 6a: Ausschnitt aus Bild 6, Feld Produktionsdatum leer.")

    schritt(
        doc, 5, "CO01 – Auftrag freigeben",
        "Knopf „Freigeben“ (Strg+F1) im Auftragskopf, im Dialog „Auftrag freigeben“ die Schaltfläche „Auftrag freigeben“.",
        "SAP meldet „Freigabe ausgeführt (Bitte Protokoll beachten)“, Status FREI. Das Produktionsdatum ist auf dem "
        "Bildschirm noch leer, weil es erst beim Sichern in die Datenbank geschrieben wird (Schritt 6 und 7).",
    )
    bild(doc, BILDER / "07_dialog_auftrag_freigeben.png", 10, "Bild 7: Dialog „Auftrag freigeben“, gewählt wurde „Auftrag freigeben“.")
    bild(doc, BILDER / "08_co01_freigegeben_status_frei.png", 16, "Bild 8: Status FREI nach der Freigabe, noch vor dem Sichern.")
    bild(doc, AUSSCHNITT / "08_co01_freigegeben_status_frei.png", 16, "Bild 8a: Ausschnitt aus Bild 8.")

    schritt(
        doc, 6, "CO01 – Sichern",
        "Knopf „Sichern“.",
        "Statusleiste: „Auftrag wurde mit der Nummer 1241851 gesichert“.",
    )
    bild(doc, BILDER / "09_co01_gesichert_1241851.png", 16, "Bild 9: Zurück auf dem Einstiegsbild nach dem Sichern.")
    bild(doc, AUSSCHNITT / "09_co01_gesichert_1241851.png", 16, "Bild 9a: Statusleiste mit der Auftragsnummer 1241851.")

    # ---------------------------------------------------------------- CO03
    doc.add_heading("Teil B – Anzeige prüfen (CO03)", level=1)

    schritt(
        doc, 7, "CO03 – Reiter „TRAFAG Daten“ nach der Freigabe",
        "Transaktion CO03, Auftrag 1241851, Enter, Reiter „TRAFAG Daten“.",
        "Das Produktionsdatum zeigt 27.11.2026, also den Eckendtermin zum Zeitpunkt der Freigabe. "
        "Es stand schon beim ersten Öffnen da.",
    )
    bild(doc, BILDER / "10_co03_einstieg.png", 16, "Bild 10: CO03 Einstieg mit Auftrag 1241851.")
    merker(doc, "NACHHER: Produktionsdatum 27.11.2026", GRUEN)
    bild(doc, BILDER / "11_co03_trafag_daten_nachher_27112026.png", 16, "Bild 11: CO03, Reiter „TRAFAG Daten“ nach Freigabe und Sichern.")
    bild(doc, AUSSCHNITT / "11_co03_trafag_daten_nachher_27112026.png", 16, "Bild 11a: Ausschnitt aus Bild 11, Produktionsdatum 27.11.2026.")

    schritt(
        doc, 8, "CO03 – Reiter wechseln und zurück",
        "Reiter „Allgemein“ angeklickt, danach wieder „TRAFAG Daten“.",
        "Im Reiter Allgemein steht der Ecktermin Ende 27.11.2026. Nach dem Zurückwechseln steht das "
        "Produktionsdatum weiter auf 27.11.2026.",
    )
    bild(doc, AUSSCHNITT / "12_co03_reiter_allgemein.png", 11, "Bild 12: Ausschnitt Reiter Allgemein, Ecktermin Ende 27.11.2026.")
    merker(doc, "NACH REITERWECHSEL: Produktionsdatum 27.11.2026", GRUEN)
    bild(doc, BILDER / "13_co03_trafag_daten_nach_reiterwechsel.png", 16, "Bild 13: Reiter „TRAFAG Daten“ nach dem Wechsel über „Allgemein“.")
    bild(doc, AUSSCHNITT / "13_co03_trafag_daten_nach_reiterwechsel.png", 16, "Bild 13a: Ausschnitt aus Bild 13.")

    # ---------------------------------------------------------------- CO02
    doc.add_heading("Teil C – Eckendtermin verschieben (CO02)", level=1)

    schritt(
        doc, 9, "CO02 – Eckendtermin auf 11.12.2026 ändern",
        "Transaktion CO02, Auftrag 1241851, im Reiter Allgemein Ecktermin Ende von 27.11.2026 auf 11.12.2026, Enter.",
        "SAP warnt: „Datum 11.12.2026 ist kein Arbeitstag (vorheriger Arbeitstag: 09.12.2026)“. Das ist eine "
        "Warnung, kein Fehler; mit Enter bestätigt. Der Wert 11.12.2026 bleibt im Feld stehen.",
    )
    bild(doc, AUSSCHNITT / "14_co02_eckendtermin_neu_eingetragen.png", 11, "Bild 14: Ausschnitt CO02, neuer Ecktermin Ende 11.12.2026 eingetragen.")
    bild(doc, BILDER / "15_co02_warnung_kein_arbeitstag.png", 16, "Bild 15: Warnung des Fabrikkalenders nach Enter.")
    bild(doc, AUSSCHNITT / "15_co02_warnung_kein_arbeitstag.png", 16, "Bild 15a: Statusleiste mit der Warnung.")

    schritt(
        doc, 10, "CO02 – Reiter „TRAFAG Daten“ vor dem Sichern, dann Sichern",
        "Reiter „TRAFAG Daten“ angeklickt, danach „Sichern“.",
        "Vor dem Sichern steht das Produktionsdatum weiter auf 27.11.2026. Statusleiste nach dem Sichern: "
        "„Auftrag wurde mit der Nummer 1241851 gesichert“.",
    )
    merker(doc, "IN CO02 VOR DEM SICHERN: Produktionsdatum 27.11.2026", GRUEN)
    bild(doc, BILDER / "16_co02_trafag_daten_vor_sichern.png", 16, "Bild 16: CO02, Reiter „TRAFAG Daten“ nach der Terminänderung, vor dem Sichern.")
    bild(doc, AUSSCHNITT / "16_co02_trafag_daten_vor_sichern.png", 16, "Bild 16a: Ausschnitt aus Bild 16.")
    bild(doc, AUSSCHNITT / "17_co02_gesichert.png", 16, "Bild 17: Statusleiste nach dem Sichern in CO02.")

    schritt(
        doc, 11, "CO03 – Kontrolle nach der Terminverschiebung",
        "Transaktion CO03, Auftrag 1241851, Reiter Allgemein, danach Reiter „TRAFAG Daten“.",
        "Der Ecktermin Ende steht jetzt auf 14.12.2026. SAP hat den eingegebenen 11.12.2026 beim Sichern auf den "
        "nächsten Arbeitstag gelegt: Werk 1100 verwendet den Fabrikkalender CH, und dort sind im Testsystem der "
        "10. bis 13.12.2026 keine Arbeitstage. Das Produktionsdatum ist unverändert 27.11.2026.",
    )
    bild(doc, BILDER / "18_co03_allgemein_eckendtermin_neu.png", 16, "Bild 18: CO03, Reiter Allgemein nach der Verschiebung.")
    bild(doc, AUSSCHNITT / "18_co03_allgemein_eckendtermin_neu.png", 11, "Bild 18a: Ausschnitt, Ecktermin Ende 14.12.2026.")
    merker(doc, "NACH TERMINVERSCHIEBUNG: Produktionsdatum bleibt 27.11.2026", GRUEN)
    bild(doc, BILDER / "19_co03_trafag_daten_unveraendert.png", 16, "Bild 19: CO03, Reiter „TRAFAG Daten“ nach der Verschiebung.")
    bild(doc, AUSSCHNITT / "19_co03_trafag_daten_unveraendert.png", 16, "Bild 19a: Ausschnitt aus Bild 19, Produktionsdatum 27.11.2026.")

    # ---------------------------------------------------------------- Beleg
    doc.add_heading("Teil D – Datenbankbeleg", level=1)
    doc.add_paragraph(
        "Zusätzlich zur Anzeige wurden die gespeicherten Werte direkt aus den SAP-Tabellen gelesen "
        "(AUFK: Auftragsstamm mit dem Feld ZZPRDAT; AFKO: Auftragskopf mit GLTRP = Ecktermin Ende, "
        "FTRMI = tatsächliches Freigabedatum). Datumsangaben in der Datenbank im Format JJJJMMTT."
    )
    tabelle(
        doc,
        ["Zeitpunkt", "AUFK-ZZPRDAT", "AFKO-GLTRP", "AFKO-FTRMI"],
        [
            ["nach CO01 (Freigabe und Sichern), 10:32", "20261127", "20261127", "20261106"],
            ["nach CO02 (Terminverschiebung), 10:35", "20261127", "20261214", "20261106"],
        ],
        [6.5, 3.5, 3.5, 3.5],
    )
    doc.add_paragraph(
        "Der Nachweisreport Z_ZZPRDAT_CHECK (nur lesend) zeigt dasselbe: Auftrag 1241851, GLTRP 14.12.2026, "
        "FTRMI 06.11.2026, ZZPRDAT 27.11.2026, Freigabe vorhanden (REL = X). Die Beobachtung „Datum weicht von "
        "GLTRP ab“ ist hier das erwartete Ergebnis: Das Produktionsdatum hält den Eckendtermin der Freigabe fest "
        "und folgt der späteren Verschiebung nicht."
    )
    bild(doc, BILDER / "20_report_selektion.png", 16, "Bild 20: Selektion des Reports Z_ZZPRDAT_CHECK (Auftrag 1241851, Werk 1100, Anlage ab 06.11.2026).")
    bild(doc, BILDER / "21_report_ergebnis.png", 16, "Bild 21: Ergebnis des Reports.")

    # ---------------------------------------------------------------- Ergebnis
    doc.add_heading("Ergebnis", level=1)
    tabelle(
        doc,
        ["Schritt", "Erwartet", "Ist", "ok"],
        [
            ["4  CO01 vor der Freigabe", "Produktionsdatum leer", "leer, Status EROF", "ja"],
            ["5–6  CO01 Freigabe und Sichern", "Status FREI, Auftrag gesichert", "FREI, Nummer 1241851", "ja"],
            ["7  CO03 Reiter „TRAFAG Daten“", "27.11.2026 (Eckendtermin bei Freigabe)", "27.11.2026", "ja"],
            ["8  CO03 Reiterwechsel", "Datum bleibt", "27.11.2026", "ja"],
            ["10  CO02 nach Terminänderung, vor Sichern", "Datum bleibt", "27.11.2026", "ja"],
            ["11  CO03 nach Verschiebung", "Ecktermin neu, Produktionsdatum unverändert",
             "Ecktermin 14.12.2026, Produktionsdatum 27.11.2026", "ja"],
            ["Datenbank AUFK/AFKO", "ZZPRDAT 20261127 bleibt, GLTRP ändert sich",
             "ZZPRDAT 20261127, GLTRP 20261127 → 20261214", "ja"],
        ],
        [5.0, 4.6, 5.6, 1.4],
    )

    doc.add_heading("Hinweise zu diesem Nachweis", level=2)
    for text in (
        "Der Test lief im Testsystem T76. Die Ergebnisse gelten für P76 erst nach dem Import des "
        "Transportauftrags T76K912490; danach empfiehlt sich derselbe Ablauf einmal in P76 mit einem echten Auftrag.",
        "Der Eckendtermin 11.12.2026 wurde von SAP auf 14.12.2026 gelegt, weil der Fabrikkalender CH im Testsystem "
        "den 10. bis 13.12.2026 nicht als Arbeitstage führt. Das betrifft nur den Eckendtermin, nicht das Produktionsdatum.",
        "Bei der Auftragsart PP22 bietet SAP die Freigabe schon nach dem Ausfüllen des Kopfs an. Im Test wurde sie dort "
        "abgelehnt und danach über den Knopf „Freigeben“ ausgeführt, damit der Zustand davor sichtbar ist. Im Alltag "
        "kann man direkt im ersten Dialog freigeben; das Ergebnis ist dasselbe.",
        "Technischer Hinweis: Im Testsystem ist neben der Lösung noch ein früherer Kunden-Exit aktiv (ZXCO1U06), der "
        "laut Quelltext ein leeres Produktionsdatum am Freigabetag beim Sichern ebenfalls mit dem Eckendtermin füllen "
        "kann; ob SAP diesen Wert übernimmt, ist nicht geprüft. Welcher der "
        "beiden Wege das Datum in diesem Test geschrieben hat, unterscheidet der Test nicht; der Wert ist derselbe.",
    ):
        doc.add_paragraph(text, style="List Bullet")

    doc.save(str(DOKUMENT))
    print(f"geschrieben: {DOKUMENT}")


if __name__ == "__main__":
    main()
