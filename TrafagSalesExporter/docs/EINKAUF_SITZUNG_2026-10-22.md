# Einkauf: Sitzung vom 2026-10-22

Stand: 2026-11-12 spaet (Abschnitt 6.2: **Rueckmeldung Armin: V6 ist die Grundlage, kein Word-Dokument, zwei Pakete, Pflege im Dashboard**); davor 2026-11-12 nachmittags (Abschnitt 6 und 6.1: Gespraech mit Marco Widmer, iteratives Vorgehen; Strukturvorschlag, Zertifikatsordner, ABC-Transaktion, SAP-Recherche). Davor 2026-11-02 (Abschnitt 5: Dashboard-Entwurf Lieferantenbewertung V6 erhalten). Davor 2026-10-22. Quelle: Whisper-Transkript der Sitzung (Ingo mit Einkauf, u. a. Armin und Marco; Erich, Sandro als
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

**Nachtrag 22.10.:** Feedbackliste erhalten (`Feedback Dashboard_20261022.docx`) und umgesetzt, Einzelheiten
`docs/PURCHASING_DASHBOARD_2026-06-05.md` Nachtrag 2026-10-22. Offen: Rueckfrage an Sandro zum Zeitraum.

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
| 3 | Word-Dokument und Dashboard-Entwurf Lieferantenbewertung schicken | Einkauf (Armin/Marco) | 122; *Dashboard-Entwurf V6 am 02.11. erhalten (Abschnitt 5), Word-Dokument noch nicht* |
| 4 | Datenquellen (SAP-Tabellen/Transaktionen) fuer die Lieferantenbewertung nennen | Einkauf | 122; *im Entwurf nur fachlich beschrieben (Abschnitt 5.3), Tabellen/Transaktionen fehlen weiter* |
| 5 | Fiori-Parametrisierung der neuen Lieferantenbewertung (Voraussetzung) | Einkauf mit NTT | 122 |
| 6 | ~~Entscheid Handbestellwesen~~ entschieden 22.10.: weiterbetreiben, Nutzung festhalten | erledigt | 120 |

## 5. Nachtrag 2026-11-02: Dashboard-Entwurf Lieferantenbewertung V6 erhalten

Von Ingo am 02.11. als `a.txt` (Begleittext) und `a.html` (Demo) weitergegeben; der Absender ist im Text nicht genannt
(nach Aufgabe 3 vermutlich Armin oder Marco). Die Demo liegt unveraendert unter
`docs/einkauf_lieferantenbewertung/Lieferantendashboard_V6_Entwurf_2026-11-02.html` (eine Datei, keine externen Quellen,
nur fiktive Lieferanten; Eingaben speichert sie nur im Browser, `localStorage`/IndexedDB). Die Originaldateien in der
Repo-Wurzel kann Ingo loeschen; alles Noetige steht hier.

### 5.1 Begleittext (sinngemaess)

Mit V6 komme man einem schlanken, Trafag-spezifischen Supplier-Relationship-Management naeher. Die wesentliche Verbesserung
sei die Trennung zwischen **laufender Lieferantenperformance** und **gezielter Lieferantenentwicklung**, ohne zwei Systeme
aufzubauen. Vor der produktiven Umsetzung seien drei Punkte zu entscheiden:

| Nr | Entscheid | Vorschlag im Text |
| --- | --- | --- |
| E1 | A/B/C-Berechnung: Einkaufsvolumen der letzten zwoelf Monate oder dynamisch auf den gewaehlten Zeitraum? | V6 rechnet dynamisch |
| E2 | Bewertungshistorie: wie oft werden die sieben Themenfelder beurteilt? | im Business Review, bei wesentlichen Veraenderungen unterjaehrig ergaenzen |
| E3 | Datenhaltung fuer Zertifikate, Bewertungen, Audits und Dokumente | zentrale Speicherung noetig (Demo nur lokal im Browser) |

### 5.2 Aufbau der Demo V6 (vier Reiter)

1. **Lieferantenperformance (alle Lieferanten):** Filter Von/Bis Monat, Disponent, Warengruppe, EVO-Klasse, Einzellieferant.
   EVO-Klasse nach Einkaufsvolumen (EVO) im gewaehlten Zeitraum und Filter: **A bis mindestens 70 %**, **B bis mindestens
   90 %**, C Rest; der Lieferant, der die Grenze ueberschreitet, bleibt in der Klasse. Ausdruecklich **keine Leistungsnote**.
   Tabelle: Lieferant, Disponent, Warengruppe, EVO-Klasse, EVO CHF, EVO kumuliert, SAP-Score, Q2 offen.
2. **Lieferantenbewertung:** Berichtsjahr, Auswahl alle oder nur A („EVO-70-%-Lieferanten“), Q2-Status. Kacheln Anzahl
   A-Lieferanten, EVO-Abdeckung kumuliert, Q2 gesamt/offen/abgeschlossen. Lieferantendetail mit **SAP-Score** aus den vier
   S/4HANA-Fiori-Kriterien (Qualitaet, Liefer-/Mengentreue, Preis, Servicegrad; Gewichtung offen), **Q2-Meldungen** je
   Lieferant und Material (Meldungsnummer, Beanstandungstext, Erfassungsdatum, Status), **Zertifikatsverwaltung** je
   Lieferant (Typ/Norm, Nummer, vorhanden, Ablaufdatum, Bemerkung; Ampel abgelaufen / hoechstens 90 Tage / gueltig / fehlt;
   CSV-Export; mehrere je Lieferant).
3. **Lieferantenentwicklung:** Massnahmenliste (Lieferant, Massnahme, Verantwortung, Termin, Status). **Sieben Themenfelder**
   aus dem „Supplier Rating Template“ (Excel) als Gespraechs- und Nachweisstruktur fuer Audit/Business Review: Skala
   1 ungenuegend, 2 Mindestanforderung, 4 gut, 5 sehr gut, „nicht beurteilt“ ohne Wert; **kein Gesamtwert**, keine
   Vermischung mit dem SAP-Score. Dazu Unterkriterien je Themenfeld mit datierter **Bewertungshistorie**.
   **Business Review** (in der Regel alle zwei Jahre, Follow-up nach etwa sechs Monaten): Rueckblick/Zielerreichung,
   Qualitaet/Q2/Lieferperformance/Kosten, Forecast/Kapazitaet/Risiken/Innovation/ESG, hoechstens fuenf Massnahmen, Status
   geplant/durchgefuehrt/abgeschlossen. **Auditplanung** angelehnt an VDA 6.3 (Diskussionsraster, keine offizielle
   Bewertung), Ziel 5 bis 10 Audits je Jahr, Auditart Lieferanten-/Prozessaudit/Follow-up, Ergebnis vierstufig, sechs
   Pruefelder (QM, Prozessbeherrschung, Reklamationsmanagement, Lieferfaehigkeit/Kapazitaet, Dokumentation/Zertifikate,
   Massnahmen/Follow-up).
4. **Dokumente:** Upload je Lieferant mit Typ (Zertifikat, Auditbericht, Business Review, Notiz, Q2/Reklamation,
   Massnahmen), Datum, Beschreibung. Produktiv gefordert: zentrale, berechtigte Ablage mit Links, Zertifikatseintrag und
   Dokument verknuepft.

### 5.3 Datenanforderungen aus der Demo (fachlich, ohne Tabellennamen)

1. EVO je Lieferant und Geschaeftsjahr, absteigend bis mindestens 70 % kumuliert; Rueckgaben, Waehrung, Gesellschaften und
   Grenzfall sind zu definieren.
2. Fiori-Lieferantenbewertung: vier Kriterien mit Einzelwerten, Gesamtwert, Bewertungszeitraum, Stammdaten.
3. Q2: Meldungsnummer, Lieferant, Material, Beanstandungstext, Erfassungsdatum, Status offen/abgeschlossen; Statusregel
   (System-/Anwenderstatus) und Mehrfachmaterialien pruefen; fehlendes Material sichtbar machen.
4. Zertifikate: Lieferant, Typ, vorhanden, gueltig bis, Dokumentenlink, Bemerkung, Pflegeverantwortung, Aenderungsdatum,
   zentral gespeichert.
5. Verknuepfung ueber den Lieferantenschluessel: EVO, Bewertung und Q2.

### 5.4 Einordnung (Claude, 02.11.)

- Passt zu Abschnitt 2: SAP-Teil (EVO, Fiori-Score, Q2) lesend, Eingabeteil (Zertifikate, Massnahmen, Themenfelder,
  Reviews, Audits, Dokumente) in der Dashboard-DB mit Schreibrecht nur fuer Berechtigte. Entscheid E3 ist damit
  fachlich vorgezeichnet (Dashboard-DB plus Dateiablage), formal aber noch offen.
- EVO je Lieferant und Disponent/Warengruppe liefert der Einkaufscache schon (Spend-Aufriss Lieferant -> Warengruppe ->
  Material). Die A/B/C-Klasse **je Lieferant** gibt es noch nicht; die bestehende ABC/XYZ-Klasse ist eine
  **Material**-Klasse aus SAP (`MaraAbc`) und darf nicht verwechselt werden.
- Voraussetzungen unveraendert: Fiori-Parametrisierung (Aufgabe 5), Q2 als Quelle (Meldungsart und Status) noch nicht
  angebunden, Word-Dokument steht aus. Danach kleiner Pilot (Abschnitt 2).

## 6. Nachtrag 2026-11-12: Gespraech Ingo mit Marco Widmer (Einkauf) zur Lieferantenbewertung

Quelle: Teams-Transkript (automatisch, teils unverstaendlich; erhalten sind nur die ersten Sekunden und die Minuten 20 bis 23).
Anlass: Marco bat um eine Auslegeordnung, was zum Lieferantenbewertungs- und Entwicklungstool vorliegt und was es braucht.
Kein Wortprotokoll, nur Ergebnisse.

- **Ingo braucht weiterhin:** das Word-Dokument von Armin (am 22.10. angekuendigt, bis heute nicht gekommen) und die
  Fiori-Lieferantenbewertung mit den gemessenen Kriterien (Liefertreue, Preis, Service, Qualitaet).
- **Marco hat noch keinen Fiori-Zugriff** (Kacheln nicht zugeordnet) und weiss nicht, welche Transaktionen bisher fuer die
  Lieferantenbewertung genutzt wurden. Er schlaegt vor, das beim Berater zu erfragen (im Transkript unverstaendlich, vermutlich
  NTT); die koennten es besser sagen als er. Einen Teil der bisherigen Bewertung verbindet er mit Erich (Wortlaut unklar).
- **Marcos Vorstellung zur Liefertreue:** statistisches Lieferdatum gegen Wareneingangsdatum mit Toleranzfenster, als Beispiel
  5 Tage zu frueh bis 1 Tag zu spaet = gruen, darueber schlechter; also ein Punktesystem. Beispielwerte, nicht festgelegt.
- **Ingo:** Die Lieferantenbewertung ist SAP-Standard; Tabellen und Transaktionen kann er selbst herausfinden, ohne auf den
  Termin mit dem Berater zu warten.
- **Vereinbart: iteratives Vorgehen.** Ingo baut eine erste Fassung und schickt Marco den Link; Marco schaut sie an, dann die
  naechste Runde. Marco raeumt ein, sich gedanklich noch zu wenig damit befasst zu haben.

**Naechste Schritte (Vorschlag Claude, nicht vereinbart):**
1. SAP lesend klaeren, ob und wie die klassische Lieferantenbeurteilung genutzt wurde (Transaktionen ME61 bis ME6x, Tabellen
   der Beurteilung; Namen vor dem Lesen im System pruefen, nicht aus Erinnerung) und welche Fiori-Apps zur Lieferantenbewertung
   in P76 vorhanden sind.
2. Erste Iteration ohne die offenen Entscheide: Reiter Lieferantenperformance mit Einkaufsvolumen, A/B/C-Klasse je Lieferant
   (70/90 %, Zeitraum nach E1 noch offen) und Liefertreue nach Marcos Toleranzfenster (statistisches Lieferdatum gegen
   Wareneingang); Q2-Meldungen, sobald `QmMeldungSet` Q2 liefert.
3. Link an Marco, Rueckmeldung einarbeiten. Der Eingabeteil (Zertifikate, Massnahmen, Audits, Dokumente) bleibt bis zum
   Entscheid, wo er liegt, aussen vor.

### 6.1 Weitere Stand 2026-11-12 (Ingo im Chat)

**Auslegeordnung fuer Marco:** Antwortentwurf mit Statusliste (vorhanden: Projektskizze Armin 30.10., Demo V6, Entscheid
03.11., Einkaufsvolumen je Lieferant im Dashboard; fehlt: Word-Dokument, SAP-Tabellen/Transaktionen, Fiori-Parametrisierung,
Entscheide E1 bis E3, Ort fuer den Eingabeteil) im Chat an Ingo; ob gesendet, ist nicht bekannt. Danach das Gespraech oben.

**Moegliche Struktur (vom Einkauf, von Ingo weitergegeben):**

* Lieferanten
  * Lieferantenperformance
  * Lieferantenbewertung
  * Zertifikate/Dokumentenablage (evtl. separat, wenn nicht im Rahmen der Lieferantenbewertung abgefragt)
  * Lieferantenentwicklung
    * Business Review
    * Auditplanung
    * (Zertifikate/Dokumentenablage)

Im Dashboard gibt es unter Einkauf bereits die Seiten `/einkauf/lieferanten` (`PurchasingDashboard.razor`) und
`/einkauf/lieferperformance` (`SupplyChainAnalysis.razor`); die Struktur liesse sich darauf aufbauen. Plan erst nach der
SAP-Recherche.

**Ablage ISO-Zertifikate:** `O:\MSE\MSE\Lieferanten_Dossier\! ISO Zertifizierungsdokumente der Zulieferer\aktuelle Zertifikate`
(`O:` = `\\trafagch.local\daten`). `TRAFAGCH\koi` hat darauf **keinen Lesezugriff** („Zugriff verweigert“, 12.11.). Ein Link
im Dashboard geht trotzdem (jeder oeffnet nur, wofuer er Rechte hat); soll das Dashboard Zertifikate und Ablaufdaten selbst
anzeigen, braucht sein Dienstkonto Leserechte (Freigabe Alex).

**ABC in SAP:** Laut Ingo gibt es fuer die ABC-Bewertung die Transaktion `/ITS/CA_MAT_ABC_CU1` „ABC-/XYZ-Analyse -
Schwellenwerte“ (Screenshot). Ob sie nur Materialien oder auch Lieferanten klassifiziert und welche Tabelle die Schwellen
haelt, klaert die Recherche (das Dashboard liest heute nur eine Material-ABC, `MaraAbc`).

**Termine und Zuarbeit:** Am 01.12. weiss man mehr zu Fiori. Das Word-Dokument fragt Ingo nochmals bei Armin nach.

**SAP-Recherche (Auftrag Ingo „SAP-Tabellen/Transaktionen der Werte rausfinden“):** laeuft nur lesend in T76 (Agent
sap-t76): `/ITS/CA_MAT_ABC_CU1`, klassische Lieferantenbeurteilung ME61 ff., Felder fuer Liefertreue, Q2-Meldungen, ECC oder
S/4, Hinweise auf die Fiori-Bewertung. Ergebnis kommt nach `saptasks/einkauf_lieferantenbewertung/SAP_QUELLEN_2026-11-12.md`.
P76 nicht gelesen (keine Freigabe).

### 6.2 Rueckmeldung Armin (Mail an Ingo, 2026-11-12)

Armin beantwortet die offenen Punkte (er nennt es selbst „meine KI-Antwort“). **Die Demo V6 ist die fachliche Grundlage.**

| Punkt | Antwort Armin | Folge |
| --- | --- | --- |
| Word-Dokument | Die Business-Review-Vorlage ist durch Masken und Raster in V6 abgebildet, **ein Word-Dokument braucht es nicht** | Punkt 3 der Aufgaben (Abschnitt 4) erledigt; fehlen Fachinfos, fragt Ingo nach |
| SAP-Tabellen/Transaktionen | Kennzahlen stehen in V6; die Quellen (Tabellen, CDS-Views, APIs) sollen **mit den SAP-Spezialisten** bestimmt werden, Einkauf und Qualitaet helfen bei der fachlichen Definition | liegt bei Ingo; Recherche in T76 laeuft (6.1) |
| SAP-Score | vier Kriterien bleiben (Qualitaet, Liefer-/Mengentreue, Preis, Servicegrad); **Fiori-Parametrisierung und Gewichtung in Arbeit mit Lucas und NTT** | weiter abhaengig, Info am 01.12. |
| Q2-Meldungen | Erweiterung von `QmMeldungSet` sinnvoll; je Lieferant und Material mit Beanstandungstext, Meldungsnummer, Datum und Status, **anzeig- und filterbar** | Teil von Paket 1 |
| A/B/C | **je Lieferant** nach Einkaufsvolumen, A ca. 70 %, B weitere 20 %, C restliche 10 %; aus dem bestehenden Cache; nicht mit der Material-ABC verwechseln | Teil von Paket 1; der Zeitraum (zwoelf Monate oder gewaehlter Zeitraum, E1) ist damit **nicht** beantwortet |
| Zertifikate, Massnahmen, Themenfelder, Reviews, Audits, Dokumente | in V6 vorgesehen; **IT soll einen Vorschlag fuer zentrale Datenhaltung und Bearbeitung machen**; Ziel: Einkauf und Qualitaet pflegen direkt im Dashboard | Paket 2; beantwortet E3 aus Sicht Einkauf |

**Zwei Pakete (Vorschlag Armin):**

* **Paket 1:** SAP-basierte Lieferantenperformance mit Q2-Meldungen und A/B/C-Klasse je Lieferant.
* **Paket 2:** Lieferantenbewertung und -entwicklung mit Zertifikaten, Audits, Business Reviews, Massnahmen und
  Dokumentenablage, gepflegt im Dashboard.

Ziel laut Armin: schlank und pragmatisch im bestehenden BI-Dashboard, ohne zusaetzliche Systeme und ohne doppelte Datenpflege.

**Einordnung (Claude):**

* Paket 1 kann sofort nach der SAP-Recherche geplant werden; es haengt nur an Q2 (Gateway-Erweiterung in T76 mit
  Transport) und am Zeitraum fuer A/B/C (E1, Rueckfrage an Armin).
* Paket 2 widerspricht dem Wortlaut des Entscheids vom 03.11. („keine Logik im Dashboard“) nur scheinbar: jener betraf
  Bewertung und Qualitaetsdaten aus SAP. Dass Eingaben in die Dashboard-DB gehen, ist eine Erweiterung und sollte von Oscar
  bestaetigt werden, bevor gebaut wird (Freigabe Fachabteilung). *Erledigt 12.11.: Oscar hat laut Ingo sein OK gegeben, Eingaben
  im Dashboard sind freigegeben.* Der IT-Vorschlag fuer Datenhaltung (Dashboard-DB,
  Dokumente als Dateiablage oder Link, Schreibrecht nur fuer Berechtigte) ist von Ingo zu liefern.
* Offen bleibt E2 (Rhythmus der Themenfelder); Armins Antwort nennt ihn nicht.
