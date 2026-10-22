# Einkauf: Sitzung vom 2026-10-22

Stand: 2026-10-22. Quelle: Whisper-Transkript der Sitzung (Ingo mit Einkauf, u. a. Armin und Marco; Erich, Sandro als
Feedbackgeber genannt). Das Transkript hat keine Sprecherzuordnung; Aussagen sind deshalb nur dort einer Person
zugeordnet, wo es aus dem Gespraech eindeutig hervorgeht. Kein Wortprotokoll, nur Ergebnisse.

## 1. Einkaufs-Dashboard: Zwischenstand Ende Oktober

- Bis zum 28.10. soll das Bestehende **stabil laufen**: nicht mehr Neues, sondern das Vorhandene zum Laufen bringen.
- Das ist ein **Zwischenstand, kein Abschluss**: Weiterentwicklung und Rueckfragen an Ingo bleiben danach moeglich.
- Feedback von **Erich, Sandro und Marco** ist in einer Liste gesammelt, je Punkt mit der Quelle. Die Liste wird mit
  Ingo geteilt; Ingo arbeitet sie ab und klaert Fragen **direkt mit der genannten Person** (nicht ueber Armin). Danach
  nochmals Austausch im selben Kreis.

## 2. Neu: Lieferantenbewertung und -entwicklung (Wunsch Einkauf)

Ausgangslage: Die bisherige Lieferantenbewertung (vier Kriterien Qualitaet, Liefertreue/Mengentreue, Preis,
Servicegrad) laeuft ueber klassische SAP-Transaktionen und wird nicht mehr unterstuetzt; kuenftig ueber Fiori-Apps.
Laut NTT-Berater muss das customisiert werden; fuer die Lieferanten**entwicklung** bietet SAP nichts Brauchbares.

Idee: **Bewertung selbst in SAP (Fiori), alles rundherum im Dashboard.**

| Teil | Quelle |
| --- | --- |
| Harte Faktoren: Lieferperformance, Qualitaet (Q2-Meldungen), Stammdaten Kreditor, Umsatz, Artikel | SAP |
| Lieferantenzertifikate nach ISO 9001: welche, Ablaufdaten, Ueberwachung | Eingabe im Dashboard |
| Entwicklungsmassnahmen aus Lieferantengespraechen als Aufgaben | Eingabe im Dashboard |
| Erweiterte Bewertung: Zusatzfragen ueber die vier harten Faktoren hinaus | Eingabe im Dashboard |
| Dokumente und Auditplan (heute separate Excel-Dateien) | Eingabe/Ablage im Dashboard |

- Anzeige- und Eingabeteil gehoeren zusammen; **nicht jeder mit Dashboard-Zugriff soll eingeben duerfen**. Lese- und
  Schreibrechte ueber das bestehende Berechtigungskonzept (Ingo bestaetigt: moeglich). Eingaben in der Dashboard-DB.
- Voraussetzung, aber **eigene Aufgabe**: Parametrisierung der neuen Lieferantenbewertung in Fiori, damit die SAP-Daten
  korrekt kommen.
- Vorgehen: Einkauf schickt Word-Dokument und Dashboard-Entwurf (mit ChatGPT erstellt, Kommentare fuer Ingo darin).
  Zuerst die Inputs von Erich und Marco einarbeiten, dann **kleiner Pilot**. Spaetere Aenderungen gelten als kleiner
  Aufwand (Delta mit Kai analysieren).
- **Ingo braucht dafuer:** Datenquellen, also SAP-Tabellen oder Transaktionen, in denen die Werte stehen.
- Spannung zu Punkt 1 (keine neuen Funktionen bis Ende Oktober): der Pilot gehoert in die Zeit danach, Termin offen.

## 3. Handbestellwesen

- Anlass: **Vorfall am 21.10.**; die PHP-Anwendung ist so alt, dass sie sich sicherheitstechnisch nicht mehr halten
  laesst (Passwort im Code). Ingo bietet die Migration an (`docs/HANDBESTELLWESEN_KONZEPT_2026-10-22.md`).
- Fachlich gab es eine **grundsaetzliche Gegenposition**: Das Handbestellwesen sei nur ein Zusatzschritt, weil
  Bestellungen sonst ohne Name und Kostenstelle in der Bestellreferenz eintreffen. Konsequent waere, es **abzuschalten**
  und die Erwartung an die Mitarbeitenden durchzusetzen (Name und Kostenstelle in der Referenz, Konsequenzen bei Paketen
  ohne Zuordnung). Alternative Idee: ein schlichtes Ticket, das nur festhaelt, wer welche Nummer gezogen hat.
- Geschichte: frueher Excel, das staendig durcheinanderkam; daraus entstand die einfache PHP-Anwendung.
- Ablauf heute bei neuem Lieferanten: Bestellung mit Referenz, Ware kommt, mit der Rechnung eroeffnet die Buchhaltung den
  Kreditor.
- **Offen: wer entscheidet ueber Abschalten oder Weiterbetrieb.** Ingo: kann migrieren, wartet aber auf den
  Abschaltentscheid; die Entscheidung muss auf passender Ebene gefaellt und dann durchgesetzt werden.

**Nachtrag 22.10. (Ingo, nach der Sitzung): Das Handbestellwesen lebt weiter; die Nutzung wird festgehalten (wer, wann,
wie viel).** Umgesetzt im Modul, `docs/HANDBESTELLWESEN_KONZEPT_2026-10-22.md` Abschnitt 6.1.

## 4. Aufgaben

| Nr | Aufgabe | Wer | Wochen_Todo |
| --- | --- | --- | --- |
| 1 | Feedbackliste Erich/Sandro/Marco erhalten und abarbeiten, Rueckfragen direkt an die Quelle | Ingo | 121 |
| 2 | Einkaufs-Dashboard bis 28.10. stabilisieren, keine neuen Funktionen | Ingo | 121 |
| 3 | Word-Dokument und Dashboard-Entwurf Lieferantenbewertung schicken | Einkauf (Armin/Marco) | 122 |
| 4 | Datenquellen (SAP-Tabellen/Transaktionen) fuer die Lieferantenbewertung nennen | Einkauf | 122 |
| 5 | Fiori-Parametrisierung der neuen Lieferantenbewertung (Voraussetzung) | Einkauf mit NTT | 122 |
| 6 | ~~Entscheid Handbestellwesen~~ entschieden 22.10.: weiterbetreiben, Nutzung festhalten | erledigt | 120 |
