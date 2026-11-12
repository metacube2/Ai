# Rexx-Schnittstelle nach SAP: Befund und Idee Tagesjob

Stand: 2026-11-12 (Doku-Pruefung: Abschnitt 14, „Nicht deployed“ ueberholt, Abruf seit dem Deploy vom 2026-11-12 11:33 auf dem Server, Schalter aus); davor 2026-11-10 (Entscheid Rexx Hub und Lieferfelder, Abschnitt 12; Hub-Mail und Produktbeschreibung gelesen, Abschnitt 13; Abruf ueber den Hub gebaut, Schalter aus, Abschnitt 14); Befund vom 2026-11-03. Untersucht auf Frage Ingo „finde heraus, wie die Daten von Rexx in SAP gelangen,
wo die Schnittstelle liegt“; festgehalten auf Auftrag Ingo „halte alles in md fest, falls der Chat
crasht“. **Gelesen nur in T76** (Mandant 100, per SapProbe/RFC, Benutzer KOI). P76 ist nicht
angefasst. T76 ist eine Kopie mit Daten bis etwa April 2026; Laeufe und Jobs in P76 koennen
abweichen.

Zusammenhang: HR-Cockpit, `docs/HR_KPI.md` Abschnitt 8.9 (Kurzfassung) und 8.3/8.7 (Vorgeschichte),
Wochen_Todo 55 (Rexx-Export ueber Upgreat).

## 1. Ergebnis in einem Satz

Die Rexx-Daten kommen ueber eine **Eigenentwicklung der EXPRIS AG** (2023/24, Paket `Z_HR_PA_REXX`)
nach SAP. **Sonja Richter (Benutzer `RIS`) startet die Importe von Hand** im Dialog; es gibt in T76
keinen Job und keinen automatischen Lieferweg. Wer Dateien in `/usr/sap/rexx` ablegt, laesst sich
aus SAP heraus nicht feststellen.

## 2. Die Objekte (Paket `Z_HR_PA_REXX`)

| Transaktion | Programm | Autor, angelegt | Richtung, Inhalt |
| --- | --- | --- | --- |
| `Z_HR_PA_REXX` „Interface REXX“ | `Z_HR_PA_INTERFACE_REXX` (Klassen `ZCL_HR_PA_INTERFACE_REXX_READ`, `..._COMP`, `ZCX_...`, `ZIF_...`) | `EXPRIS`, 26.10.2023 | Rexx → SAP, **Stammdaten**: Datei lesen, Person ueber AHV-Nummer (PA0036) finden, Feld fuer Feld mit SAP vergleichen (`ZHR_PA_REXX_DELTA_S`: Infotyp, Feld, Wert SAP, Wert Rexx), Abweichungen schreiben: IT 0002, 0006, 0007, 0008, 0009, 0032, 0036, 0038, 0039, 0040, 0046, 0048, 0050, 0105; Massnahmen Z1 „Einstellung aus REXX“, Z2 „Wiedereintritt aus REXX“, Z3 „Org. Wechsel aus Rexx“, Z5 „Austritt aus Rexx“ |
| `Z_HR_REXX_PA_ABS` „Import Absenzdaten von REXX“ | `Z_HR_PA_IMP_ABSENZDATEN` | `EXPRIS_SIWY`, 22.02.2024 | Rexx → SAP, **Abwesenheiten**: CSV mit `;`, Batch-Input ueber PA30 (Mappe `ABS_2001`), Testlauf als Vorgabe |
| `Z_HR_REXX_PA_ZE` „Import Zeitereignisse von REXX“ | `Z_HR_PA_IMP_ZEITEREIGNISSE` | `EXPRIS_SIWY`, 15.02.2024 | Rexx → SAP, **Kommen/Gehen** nach PA2011 (`TEVEN`) |
| `Z_HR_REXX_PA_LA_EXP` | `Z_HR_PA_REXX_LOHNDATEN_EXPORT` | `EXPRIS_SIWY`, 30.11.2024 | SAP → Rexx, **Lohnarten** aus den Abrechnungsergebnissen (`Personalnummer;Lohnart ID;Abrechnungsdatum;Betrag;Currency`), Ablage lokal oder Server; fuenf Varianten von `EXPRIS_SIWY`, darunter „TEST SERVER“ |

Weitere Objekte: Strukturen `ZHR_PA_REXX_IMPORT_S` (Satzaufbau der Stammdatendatei, rund 60 Felder),
`ZHR_PA_REXX_DELTA_S`, `ZHR_PA_REXX_ACTIONS_S`; Tabellentypen; Nachrichtenklasse `Z_HR_PA_REXX`.
Im Code steht „gem. Besprechung mit S. Wyden vom 18.04.2024“ (Abrechnungskreis 99 wird nicht
uebernommen). `EXPRIS_SIWY` ist vermutlich diese Person; der volle Name ist nicht belegt.

Quelltexte aus T76 (Stand 2026-11-03, nur gelesen): `quelltexte_T76_2026-11-03/`.

## 3. Wie die Dateien hineinkommen

- Alle drei Importe lesen **entweder vom PC** (`GUI_UPLOAD`, Vorgabe `c:\temp\test.csv`) **oder vom
  Server** (`OPEN DATASET`). Das Stammdatenprogramm nimmt im Hintergrund automatisch den Serverweg
  (`UPLOAD_APPL`, wenn `sy-batch`), ueber den Verzeichnisalias **`DIR_REXX` = `/usr/sap/rexx`**
  (Tabelle `USER_DIR`, Server „all“).
- **Wer `/usr/sap/rexx` beschreibt, ist offen.** Die Verzeichnisliste per RFC
  (`EPS2_GET_DIRECTORY_LISTING`, `EPS_GET_DIRECTORY_LISTING`) verweigert T76 mit
  `READ_DIRECTORY_FAILED`, auch fuer `/usr/sap/trans`, vermutlich fehlende Berechtigung.
  Sicher klaeren laesst es sich in **P76, AL11 `/usr/sap/rexx`**: Dateiname, Datum und
  Betriebssystem-Besitzer. Gehoert die Datei dem SAP-Benutzer (etwa `p76adm`), hat SAP sie
  geschrieben; ein anderer Besitzer (etwa ein SFTP-Konto) heisst Ablage von aussen.
- Jobs: `TBTCP` und `TBTCO` in T76 ohne Eintrag fuer `Z_HR_PA*` und ohne Job des Benutzers `RIS`.
- Anwendungsprotokoll `BALHDR` der Stammdaten-Schnittstelle in T76: 03.09.2026 14:06 und 14:11
  (`RIS`), 14:48 (`XHA`, Anesa Xheladini), 22.09.2026 13:58 (`RIS`), alle im **Dialog**
  (`ALMODE = D`) ueber die Transaktion. Diese Laeufe fanden in der Testkopie statt; warum, ist offen
  (Test oder Schulung?).
- Batch-Input-Mappen (`APQI`): nur `ZEII2011` von `EXPRIS_SIWY` vom 24.06.2024 (Test). Mappen
  werden nach der Verarbeitung offenbar geloescht.

## 4. Wer was angelegt hat (Aenderer seit 2025-10, T76)

| Infotyp | Aenderer |
| --- | --- |
| PA2001 Abwesenheiten | `RIS` 1'594 von 1'594 |
| PA0000 Massnahmen | `RIS` 97, `XHA` 65 |
| PA0001 Org. Zuordnung | `RIS` 142, `XHA` 102 |
| PA0002 Personaldaten | `RIS` 229, `XHA` 41 |
| PA0007 Sollarbeitszeit | `XHA` 64, `RIS` 34 |
| PA0008 Basisbezuege | `RIS` 79, `XHA` 77 |
| PA2006 Kontingente | `RIS` 499, `I001071` (Ruedy Ruetsche) 482, `XHA` 9 |
| TEVEN Zeitereignisse (ab 2025) | `RIS`, monatlich 3'000 bis 23'000 Saetze; 07/2025 `XHA` |

`RIS` = Sonja Richter, `XHA` = Anesa Xheladini, `I001071` = Ruedy Ruetsche, `I201072` = John Gann
(`USR21`/`ADRP`). `RIS` und `XHA` sind Dialogbenutzer (`USTYP A`).

**PA2001 nach Art und Anlagetag** zeigt zwei Wege:

| Art | Anlagetage |
| --- | --- |
| 0100 Ferien, 0400 Kompensation, 0500 bezahlte Absenzen, 0600 Militaer | nur 20.11.2025, 17.12.2025, 22.02.2026, je 200 bis 480 Faelle: **Rexx-Import** |
| 0260/0270 Krankheit, 0290 Unfall | 14.10., 23.10., 21.11., 17.12.2025, 22.01., 23.03., 09.04.2026, je 20 bis 90 Faelle: **anderer Weg** |

## 5. Was der Absenz-Import uebernimmt und was nicht

Aus `Z_HR_PA_IMP_ABSENZDATEN` (Satzaufbau: AHV-Nr; Personalnummer; ID; Workflow; Kostenstelle;
Bezug; Status; Antragstyp; Antragsteller; Absenz; Zeitstempel; Von; Bis; Anzahl Tage; Halbtage;
Zeitstempel):

- **Nur Status „Genehmigt“** und nur, wenn das Ende im gewaehlten Datumsfenster liegt.
- Zuordnung: Ferien → 0100; Kompensation und Sonderurlaub → 0400; Militaer/Zivildienst und
  Mutterschaftsurlaub → 0600; Unbezahlter Urlaub → 0900; Bezahlte Absenzen → 0500;
  Aus- und Weiterbildung → PA2002 0710; Dienstreise → PA2002 0700. Halbe Tage werden einzeln
  mit Stunden gebucht.
- **Krankheit, Kind-Krank und Unfall schliesst das Programm ausdruecklich aus**
  (`IF lv_absenz NE 'Krankheit' AND ... NE 'Kind-Krank' AND ... NE 'Unfall'`, die Zweige sind
  auskommentiert). Die Krankheitsfaelle in PA2001 kommen also anders hinein, vermutlich von Hand
  durch Sonja (PA61 steht in ihren Favoriten). **Nicht belegt**, bei Sonja nachfragen.

## 6. Folgen fuer das HR-Cockpit

1. **`HrAbsenzSet` hinkt fuer Ferien, Kompensation und Militaer nach.** Diese Arten stehen in SAP
   erst nach Sonjas Import (in T76 sichtbar: etwa monatlich bis seltener). Die Annahme „P76 ist
   immer aktuell“ (`docs/HR_KPI.md` 8.7) gilt dafuer nicht; der laufende Monat ist in der Regel
   unvollstaendig. Fuer Krankheit haengt es vom Erfassungsweg ab (Abschnitt 5).
2. **Stammdaten: Rexx ist fuehrend, SAP bekommt eine Kopie** (Eintritt, Austritt, Org., Kostenstelle,
   Sollzeit, Lohn, Bank, Adresse). Die Personenbasis koennte das Cockpit deshalb aus SAP nehmen,
   sie ist dann so aktuell wie Sonjas letzter Stammdaten-Lauf.
3. **Austrittsart in SAP nur teilweise:** seit 2025 in T76 52 Austritte als `ZC` mit Grund
   (01 Kuendigung durch Arbeitnehmer, 02 durch Arbeitgeber, 03 Pensionierung, 09 Tod, 20 fristlos),
   43 als `Z5` „Austritt aus Rexx“ **ohne Grund**.
4. **Ferienkonten liegen in SAP PA2006:** Kontingentarten 10 „Ferien Anspruch“, 11 „Ferien Rest
   Vorjahr“, 20 „Kompensationsstunden“, 30 „Kader Zusatz Stunden“. Ob Anspruch und Rest mit Rexx
   uebereinstimmen, ist nicht geprueft.
5. **GLZ-Saldo:** Kommen/Gehen kommt monatlich nach SAP (TEVEN), und Sonja hat PT60
   (Zeitauswertung) und PT_BAL00 (Zeitsalden) in den Favoriten. Moeglicherweise rechnet SAP den
   Saldo selbst; nicht geprueft.

## 7. Idee Ingo: eigenes Programm, eigene Z-Tabelle, taeglicher Job (2026-11-03)

Ingo: ein neues Programm, das die drei Transaktionen buendelt, die fehlenden Felder mitnimmt und
in eine neue Z-Tabelle schreibt, damit es dem bestehenden Ablauf nicht in die Quere kommt; falls
etwas falsch laeuft, duerfen SAP-Loehne usw. nicht veraendert werden, das waere fatal. Als Job
taeglich laufen lassen. **Noch nichts gebaut.**

Bewertung und Leitplanken (mit Ingo besprochen, nicht entschieden):

- **Entscheidend ist die Lieferung der Dateien.** Ein Job kann nicht vom PC lesen, nur aus einem
  Serververzeichnis. Taeglich frische Daten gibt es nur, wenn Rexx die Dateien taeglich selbst in
  `/usr/sap/rexx` ablegt. Erster Schritt deshalb: P76 AL11 lesend pruefen (Abschnitt 3), sonst
  bei EXPRIS oder Rexx die Ablage per SFTP anfragen.
- **Nur lesen, nur in die eigene Tabelle schreiben:** kein `CALL TRANSACTION`, kein Batch-Input, kein
  `HR_INFOTYPE_OPERATION`, kein BAPI; Dateien nur `OPEN DATASET ... FOR INPUT`, nie loeschen oder
  verschieben, damit Sonjas Import sie unveraendert findet. Vor dem Transport im Quelltext pruefen.
- **Eigener Hintergrundbenutzer** mit genau zwei Rechten (Dateien in `/usr/sap/rexx` lesen,
  Z-Tabellen schreiben), ohne Infotyp-Berechtigung: zweite Sicherung neben dem Quelltext.
- **Nur die Cockpit-Felder speichern**, kein Lohn, keine IBAN, keine Adresse; Tabelle mit
  Berechtigungsgruppe. Namen eindeutig, etwa `ZHR_REXX_STAMM`, `ZHR_REXX_ABSENZ`, `ZHR_REXX_SALDO`,
  je mit Ladedatum. Faellt eine Datei aus, bleibt der alte Stand, das Cockpit zeigt das Ladedatum.
- Was es abdecken wuerde: Krankheit/Kind-Krank/Unfall (stehen in der Absenz-CSV, werden heute nur
  uebersprungen) ja; Austrittsart nur, wenn die Stammdatendatei sie enthaelt (die Struktur hat
  kein solches Feld); GLZ-Saldo und Restferien nein (nicht in diesen Dateien, dafuer `Saldiperstichdatum`
  oder SAP selbst, Abschnitt 6).
- Ablauf: T76 mit eigenem Transport, Import P76 durch Ingo; Zustimmung Sonja, weil es
  HR-Personendaten sind.

Offene Entscheide Ingo: P76 lesend pruefen (ja/nein); Z-Tabelle nur Cockpit-Felder oder ganze
Dateien spiegeln.

## 8. Mails und Internet (2026-11-03)

**Outlook, nur Betreffzeilen.** Der M365-Connector war in der Sitzung nicht verbunden; ueber das
lokale Outlook (COM) sperrt der Outlook-Sicherheitsschutz Mailtext und Absender, deshalb nur
Datum und Betreff. Relevante Faeden:

| Datum | Betreff | Bemerkung |
| --- | --- | --- |
| 08.07. bis 30.07.2026 | „Rexx automatischer Berichtsexport“ | Faden zum Rexx-Hub (bekannt, `HR_KPI.md` 8.3) |
| 19.08. / 14.09.2026 | „Fragen zu HR Dashboard“ | Inhalt nicht gelesen |
| 30.09.2026 | „HR-Cockpit: automatische Übermittlung der Rexx-Exporte“ (gesendet) | Mail an Ueli Kunzmann |
| 01.10.2026 | Ticket eroeffnet, **Upgreat Request #12699584** | aus der Mail wurde ein Ticket |
| 06.10., 12.10.2026 | „New Comment for Request #12699584“ | Inhalt nicht gelesen |
| 19.10.2026 | „Please review the solution …“ und „Your ticket has been closed …“ | **Upgreat hat das Ticket geschlossen** |
| 20.10.2026 | „AW: Your ticket has been closed …“ (gesendet) | Antwort Ingo, Inhalt nicht gelesen |
| 29.10.2026 | „WG: Angebot: AG36071 / Rexx Hub“ | weitergeleitetes Angebot zum Rexx-Hub, Inhalt nicht gelesen |

Damit ist Wochen_Todo 55 („Ueli nicht mehr zustaendig, Antwort fehlt“, Stand 13.10.) ueberholt:
Es gibt ein Upgreat-Ticket, das am 19.10. geschlossen wurde, und am 29.10. ein Angebot zum Rexx-Hub.
**Was Upgreat geloest hat und was das Angebot sagt, ist nicht gelesen.** Naechster Schritt: die
vier Mails im Outlook oeffnen oder den M365-Connector neu verbinden (`/mcp`).

**Internet.** Oeffentlich ist keine SAP-Schnittstelle von Rexx dokumentiert und keine oeffentliche
API-Dokumentation. Rexx nennt „Export Mitarbeiterstammdaten“, „Schnittstellen“, „Zeitkonten“
([softguide](https://www.softguide.de/programm/rexx-hr-personalmanagement-digitale-akte),
[apitracker](https://apitracker.io/a/rexx-systems)). Fertige Anbindungen gibt es fuer DATEV, dort
mit WebDAV-Ordner fuer den Dateiaustausch
([DATEV-Marktplatz](https://www.datev.de/web/de/marktplatz/rexx-suite/),
[DATEV-Community](https://www.datev-community.de/t5/Personalwirtschaft/Schnittstelle-Datev-Rexx-Import-der-Gehaltsdaten-nach-DATEV-nach/td-p/351052)).
Die Trafag-Verbindung ist also eine Eigenentwicklung; Auskunft geben nur EXPRIS, Rexx oder Sonja.

## 9. Naechste Schritte

1. Die Mails zum Upgreat-Ticket #12699584 (06.10. bis 20.10.) und zum Angebot AG36071 (29.10.) lesen.
2. P76, AL11 `/usr/sap/rexx` lesend: Dateien, Datum, Besitzer; dazu Jobs (SM37) fuer `Z_HR_PA*`.
3. Sonja fragen: Woher kommen die CSV-Dateien, und wie werden Krankheitsfaelle erfasst?
4. EXPRIS (S. Wyden) fragen: Ist eine automatische Ablage in `/usr/sap/rexx` vorgesehen oder moeglich?
5. Danach Entscheid Ingo ueber das eigene Programm mit Z-Tabelle und Tagesjob (Abschnitt 7).

## 10. Werkzeug-Hinweise aus dieser Untersuchung

- `RunSapProbe.ps1 table-read --fields` braucht **Kommas** (`"UNAME,AEDTM"`). Mit Leerzeichen meldet
  SAP `TABLE_WITHOUT_DATA`, was wie eine leere Tabelle aussieht (`docs/rag/LEARNINGS.md`).
- Klassen-Includes: Name auf 30 Zeichen mit `=` auffuellen, dann `CMnnn` (Methodenindex aus `TMDIR`
  zur Basis 36), z. B. `ZCL_HR_PA_INTERFACE_REXX_READ=CM00E` fuer `START_PROCESS`.
- Favoriten eines Benutzers: Tabelle `SMEN_BUFFC` (`UNAME`, `REPORT`, `TEXT`); hat hier die
  Z-Transaktionen verraten.

## 11. Nachtrag 2026-11-03: Mailinhalte gelesen (Outlook Web im Browser, nur gelesen)

- **Upgreat-Ticket #12699584:** eroeffnet 01.10. (Mail an Ueli), zugewiesen 02.10. (Gian-Luca Schmutz), 06.10. Noel Hagger
  „pruefen intern“, am 19.10. geschlossen mit **„Unable to solve“**: nach Absprache mit Dardan Hasani betreut Upgreat Rexx
  nicht, die Administration laufe ueber Trafag selbst, Ingo solle die eigene IT fragen.
- **20.10. Ingo an Alex Donato:** wer administriert Rexx, kann Rexx die vier Berichte zeitgesteuert (SFTP/Netzwerkordner)
  bereitstellen, gibt es eine Ausfallmeldung. Eine direkte Antwort von Alex ist nicht zu finden.
- **29.10. Alex leitet „Angebot: AG36071 / Rexx Hub“ an Dardan Hasani und Ingo weiter, ohne eigenen Text.** Inhalt: Nadja hatte
  Alex am 19.08. gebeten, den Hub aus IT-Sicht zu pruefen, bevor sie das Go gibt. Zahlen laut Mails: heute **8 Schnittstellen
  in Rexx fuer 11'200 EUR pro Jahr**; 4 weitere einzeln je 1'400 EUR = 5'600 EUR pro Jahr; der **Hub 4'800 EUR pro Jahr** mit
  beliebig vielen Schnittstellen. Rexx (Riccardo Schwick, Managing Partner Rexx Systems Schweiz AG, 10.08.): der Hub ersetzt
  die bisherigen Schnittstellen nicht automatisch, sie lassen sich aber migrieren, dann entfallen deren Kosten. Beschreibung
  `RexxHUB_Deutsch_27.07.2026.pdf` (5 MB) haengt an, nicht gelesen.
- **15.10. Rexx-Preiserhoehung 2 % ab 01.01.2027**; Oscar Schwark am 27.10. an Rino Straessler: Budget 2027 mit Reserve fuer
  Preiserhoehungen planen.
- **Folgerungen:** (1) Der Weg ueber Upgreat ist zu, Rexx administriert Trafag selbst (HR, Nadja). (2) „8 Schnittstellen in
  Rexx“ sind vermutlich auch die Dateien, die Sonja per `Z_HR_PA_REXX` nach SAP importiert (nicht belegt). (3) Ingos Entscheid
  vom 30.09. „Hub wollen wir nicht“ steht gegen Nadjas Rechnung, dass der Hub guenstiger ist als vier Einzelschnittstellen;
  das ist mit Nadja zu klaeren. (4) Die „4 weiteren Schnittstellen“ sind vermutlich Ingos vier Berichte.
- **Sonja Richter 02.11.:** „stehen mittlerweile Live-Daten fuer uns zur Verfuegung? Weil testen konnten wir bis anhin ja noch
  nichts.“ Ingo hat am 03.11. 07:03 geantwortet (Inhalt nicht gelesen). HR hat das Cockpit also noch nicht getestet.

**Nachtrag 2026-11-03 nachmittags:** In der Abschlussrunde nannte Nadja Artjom Yahno als Rexx-Betreuer (seit rund einem Jahr,
auch fuer Schnittstellen). Ingo hat die Sitzung zum Rexx Hub fuer naechste Woche geplant, mit Alex und Artjom; Nadja und Sonja
braucht es laut Alex nicht. Davon haengen der Tagesexport (Abschnitt 7) und die Idee mit eigenem Programm ab.

## 12. Entscheid 2026-11-10: wir nehmen den Rexx Hub; Felder, die Rexx liefern muss

**Entscheid Ingo, 2026-11-10 (im Chat):** Der Rexx Hub wird genommen, weil er guenstiger ist (Angebot AG36071, Abschnitt 11:
Hub 4'800 EUR pro Jahr mit beliebig vielen Schnittstellen; heute 8 Schnittstellen 11'200 EUR pro Jahr, 4 weitere einzeln
5'600 EUR pro Jahr). Ingo migriert die bestehenden Schnittstellen selbst auf den Hub; als einzige bleibt der
Stempeluhrtransfer in Rexx wie bisher. *Damit ueberholt:* der Entscheid vom 2026-09-30 „Hub wollen wir nicht“ (Abschnitt 11
Folgerung 3, `docs/HR_KPI.md` 8.3). Mail an Nadja im Chat entworfen, **nicht gesendet** (Versand durch Ingo). Was der
Stempeluhrtransfer weiter kostet, ist nicht bekannt; ob die Sitzung zum Hub (Alex, Artjom) stattgefunden hat, ist hier
nicht festgehalten.

**Felder, die Rexx liefern muss** (Kurzauskunft 10.11. aus der Doku und aus `Services/HrKpi/HrKpiDashboardBuilder.cs`; nichts
neu in SAP gemessen, SAP-Seite weiter nur T76). Die Namen sind die Spaltenueberschriften der Rexx-Abfragen #775 und #777.
Excel dazu: `HR-Cockpit_Felder_Rexx_nach_SAP_2026-11-10_v2.xlsx` in Ingos Downloads (nicht im Repository).

*Lieferung A, Stamm und Salden je Person* (eine Zeile je Person, inklusive Ausgeschiedener; Summen 01.01. bis Liefertag):
Personalnummer; Nachname, Vorname (Link Personal); Personal Status; Eintrittsdatum; Austrittsdatum; **Austrittsart**;
Geburtsdatum; Organisation; Kostenstelle; Stelle; Ø tägliche Sollarbeitszeit (Woche); **Stunden Saldo**; Urlaubsanspruch;
Urlaub Rest; Ferien ausstehend (Tage); Ferien angetreten (Tage); Krankheit angetreten (Stunden); Krankheit (über 60 Tage)
angetreten (Stunden); Unfall angetreten (Stunden); Kompensation angetreten (Stunden); Militär / Zivildienst angetreten
(Stunden).

*Lieferung B, Abwesenheiten je Fall* (eine Zeile je Abwesenheit): Personalnummer; **Abwesenheit Buchung** (Art, einschliesslich
Krankheit, Kind-Krank, Unfall); Abwesenheit Status; Abwesenheit von; Abwesenheit bis; Abwesenheit Tage; Abwesenheit halbe
Tage; Abwesenheit Tagessoll; Abwesenheit ID Buchung. „halbe Tage“ und „ID Buchung“ liest das Cockpit heute nicht (Namen aus
`docs/HR_KPI.md` 8.14).

| Ampel | Feld | Stand in SAP | Noetig |
| --- | --- | --- | --- |
| rot | Austrittsart | fehlt bei Austritten „aus Rexx“ (Z5 ohne Grund, 43 von 95 seit 2025 in T76); `ZHR_PA_REXX_IMPORT_S` hat kein solches Feld | Rexx: neues Feld in der Stammdatendatei |
| rot | Krankheit, Kind-Krank, Unfall je Fall | steht in der Absenz-CSV, `Z_HR_PA_IMP_ABSENZDATEN` schliesst sie aus (Abschnitt 5) | SAP-seitig (Import oder Z-Tabelle), keine Rexx-Lieferung |
| rot | Direkt gebuchte Krankheit mit Datum (ohne Antrag) | rund 100 Tage bei 13 Personen fehlen (`docs/HR_KPI.md` 8.14) | offen, Rexx-Feldname unbekannt; mit Rexx (Artjom) und Sonja klaeren |
| rot | Stunden Saldo (GLZ) | in keiner Importdatei | Rexx liefert, oder SAP rechnet selbst (PT60/PT_BAL00, ungeprueft) |
| gelb | Urlaubsanspruch, Urlaub Rest | PA2006 Art 10 und 11 vorhanden | Abgleich mit Rexx fehlt |
| gelb | Ferien ausstehend | Import nimmt nur „Genehmigt“ mit Ende im Datumsfenster | pruefen |
| gelb | Ferien, Kompensation, Militaer, bezahlte Absenzen je Fall | Felder da, am 04.11. nur bis Ende Juli | taegliche Ablage in `/usr/sap/rexx`, Import als Job |
| gelb | Militaer / Zivildienst | am 04.11. 3 Personen in SAP, 7 in Rexx | Artzuordnung klaeren |
| gruen | Personalnummer, Name, Eintritt, Austritt, Geburtsdatum, Organisation, Kostenstelle, Stelle, Sollzeit | Stammdatenimport `Z_HR_PA_REXX` | regelmaessiger Lauf |
| gruen | Beschaeftigungsgrad, Mitarbeiterkreis, Geschlecht, Planstelle, BU/NBU | SAP fuehrt sie selbst | nichts |

Befristung und Mitarbeitertyp brauchen kein eigenes Feld: Das Cockpit leitet beides aus „Austrittsart“ und „Stelle“ ab.
Nicht noetig: Lohn, Bank, Adresse, „Leitung j/n“, Arbeitszeitmodell.

**Offen, widerspruechlich belegt:** ob der Hub schon bestellt ist. `docs/HR_KPI.md` 8.3 nennt einen Auftrag Nadjas an Rexx
vom 2026-07-29; Abschnitt 11 oben sagt, sie habe am 19.08. Alex um die IT-Pruefung gebeten, bevor sie das Go gibt. Nicht
aufgeloest, bei Nadja nachfragen.

**Naechste Schritte:** Mail an Nadja senden (Ingo); Stand des Hub-Auftrags mit Nadja klaeren (bestellt oder nicht); Lieferfelder, Format und taegliche
Ablage mit Rexx (Artjom Yahno) klaeren; danach Entscheid zum eigenen Programm (Abschnitt 7).

## 13. Rexx Hub: Mailverlauf AG36071 und Produktbeschreibung (gelesen 2026-11-10, fuer die Umsetzung)

**Mail.** Alex hat den Verlauf am 10.11. 09:06 nochmals mit „FYI“ an Ingo geschickt (zuerst am 29.10. an Dardan Hasani und
Ingo). Der Verlauf, gelesen in Outlook Web:

| Datum | Von / an | Inhalt |
| --- | --- | --- |
| 06.08.2026 | Riccardo Schwick (Rexx, Managing Partner) an Nadja | Offerte Rexx Hub „gemaess Absprache mit Artjom“. **Fuer die Beauftragung soll Nadja eine unterschriebene Auftragsbestaetigung als Scan zurueckmailen.** |
| 07.08.2026 | Nadja an Riccardo | Entfallen mit dem Hub sofort die Kosten der bisherigen Schnittstellen? |
| 10.08.2026 | Riccardo an Nadja, Artjom | Nein, nicht direkt. Der Hub ist aber schon guenstiger als vier zusaetzliche Einzelschnittstellen (je 1'400 EUR, 5'600 EUR pro Jahr). Werden die bisherigen Schnittstellen auf den Hub migriert, entfallen deren Kosten. |
| 19.08.2026 | Nadja an Alex | 8 Schnittstellen heute 11'200 EUR pro Jahr, 4 weitere noetig; Hub 4'800 EUR pro Jahr fuer beliebig viele, auch die bestehenden. **Vor ihrem Go soll Alex die Beschreibung aus IT-Sicht pruefen.** |
| 29.10. / 10.11.2026 | Alex an Dardan, Ingo / an Ingo | Weiterleitung ohne eigenen Text bzw. „FYI“ |

Folgerung: Nach diesem Verlauf ist der Hub **nicht bestellt**; Nadjas Go wartet auf die IT-Pruefung, eine Antwort von Alex steht
nicht im Verlauf. Die Angabe „Nadja hat am 2026-07-29 den Auftrag erteilt“ (`docs/HR_KPI.md` 8.3) deckt die Mail nicht. Die
Auftragsbestaetigung selbst haengt nicht an der Weiterleitung. *Wochen_Todo 133 bleibt auf Wunsch Ingo unveraendert.*

**Produktbeschreibung** `RexxHUB_Deutsch_27.07.2026.pdf` (Rexx Systems GmbH, Version 27.07.26, 43 Seiten, 5,4 MB). Liegt in
Ingos Downloads; der Textauszug liegt unversioniert in `.tmp_tools/rexx_hub/RexxHUB_Deutsch_27.07.2026_text.txt`. Was fuer
unsere Umsetzung zaehlt:

- **Art:** REST-API mit CRUD (GET lesen, POST anlegen, PATCH aendern, DELETE loeschen), Ausgabe JSON oder XML. **Kein
  Dateiexport und keine Ablage in einem Ordner**: Wir rufen ab, Rexx schiebt nichts. Webhooks sind erst „Ausblick“. Damit
  entfaellt die Idee „Rexx legt Dateien in `/usr/sap/rexx` ab“ (Abschnitt 7) fuer den Hub; der Abruf kaeme vom Dashboard-Server
  (oder von SAP per HTTP).
- **Kosten:** eigenes Modul, keine Preise je Schnittstelle; Aktivierung durch den Rexx-Ansprechpartner im Modul-Editor.
  Voraussetzung mindestens „ER Only Unified“, bei der Rexx Suite stehen ER- und HR-Endpunkte bereit.
- **Einrichtung durch Trafag selbst** unter „Tools > Rexx Hub“: API-Benutzer mit Token (auch eigener Schluessel moeglich),
  Aktiv-Schalter, Verfallsdatum, **erlaubte IP-Adressen**, Ausgabesprache, bevorzugte Waehrung. Je Endpunkt: freigeben,
  Zeilen je Seite, Sortierung, **Export-Felder** (auswaehlbar, mit eigenem Feldnamen im Ergebnis; zu viele Felder ergeben
  Fehler 422 „Please reduce the requested fields“), **Export-Filter** (z. B. Eintrittsdatum ab), **Parameter-Mapping**
  (welche ID als `employeeid` gilt; Rexx empfiehlt die Rexx-Personal-ID, moeglich sind Personalnummer oder externe ID).
  Rechte in Rexx: #1411, #1412, #1417, #1418 (Recruiting #10400 bis #10403).
- **Pruefen und Doku:** Swagger-Testseite je Endpunkt, OpenAPI-Datei (JSON) je API-Benutzer exportierbar (ohne Token).
  Rexx meldet taeglich „disruptive Aenderungen“ (geaenderte Endpunkte, geloeschte Zusatzfelder) als Systemnachricht.
- **Formate:** Datum `2009-11-04`, Zeitstempel `2009-11-04T19:55:41Z`, Uhrzeit `19:55:41`, Dezimalpunkt (`42.80`).
  Antwort mit `data`, `_links` (Blaettern), `_meta` (Anzahl, Seite, request id fuer den Support). Fehlercodes 400, 401
  (Endpunkt nicht freigegeben), 403 (Token ungueltig), 404, 422, 429 (zu viele Anfragen), 500, 504.
- **Grenzen:** **hoechstens eine Anfrage gleichzeitig ueber alle API-Benutzer** (File-Lock, bis 3 s Wartezeit);
  **50'000 Anfragen pro Monat** (mehr kostenpflichtig, „Rate Limit Extender“), ungenutzte verfallen, jede Anfrage zaehlt.
  Logs mit Anfrage und Antwort 90 Tage („Rexx Hub Logs“).
- **Aenderungen abholen:** `/recent` (neue Datensaetze seit Zeitstempel, nach Erstellungsdatum) und `/updated` (geaenderte,
  ueber einen Hash je API-Benutzer, Endpunkt und Datensatz; ohne Parameter seit dem letzten Abruf, sonst jetzt minus 1 h;
  zum Blaettern muss ein Zeitstempel mitgegeben werden).
- **Endpunkte, die fuer das HR-Cockpit in Frage kommen** (Liste Kapitel 11; welche davon grau, also noch nicht verfuegbar,
  sind, zeigt der Textauszug nicht, nur das PDF-Bild): `GET /v1/employees` (+ `/recent`, `/updated`, `/{employeeId}`),
  `/v1/employees/{employeeId}/employments`, `/contracts`, `/occupations`, `/org-units`, `/cost-accounts`, `/probation`,
  `/limitations`; `/v1/org-units`, `/v1/cost-accounts`, `/v1/positions`; Konfiguration `employment-types`,
  `personnel-groups`, `person-types`; `/v1/requests` (+ `/recent`, `/updated`, `/{applicationId}`, „Anfragen“; ob das
  Abwesenheitsantraege sind, steht nicht drin); **`GET /v1/datawarehouse/{dataWarehouseQueryId}`**: liefert das Ergebnis
  einer Rexx-Data-Warehouse-Abfrage ueber ihre ID.
- **Es gibt keinen eigenen Endpunkt fuer Abwesenheiten, Zeitkonten, Salden oder Kommen/Gehen.** Abwesenheitsbuchungstypen
  kommen nur als Beispiel fuer das Auswahlwerte-Mapping vor.

**Folgerung fuer die Umsetzung (Vorschlag, nicht entschieden):** Am naechsten an heute liegt
`/v1/datawarehouse/{id}` mit den Abfragen #775 (Stamm und Salden je Person) und #777 (Abwesenheiten je Fall), falls diese als
Data-Warehouse-Abfragen gelten und ueber den Hub freigegeben werden koennen. Dann bleiben Spaltennamen und Cockpit-Logik
gleich, nur die Datei kommt per API statt per Handexport; zwei Abrufe taeglich sind weit unter 50'000 im Monat. Offen, mit
Rexx (Artjom Yahno) zu klaeren:

1. Sind #775 und #777 (Personal › Abfrage bzw. Antragsliste) Data-Warehouse-Abfragen im Sinne des Endpunkts, und gilt die
   50-Spalten-Grenze dort auch? Wenn nicht: wie kommen Abwesenheiten je Fall und Salden (Urlaub Rest, Stunden Saldo) ueber den Hub?
2. Was liefert `/v1/requests`, und sind direkt gebuchte Abwesenheiten (ohne Antrag) irgendwo abrufbar?
3. Welche Endpunkte sind heute grau (nicht verfuegbar)?
4. Feste IP des Abrufers (Dashboard-Server `tragvapp401`) fuer die IP-Freigabe; Firewall vom Server nach Rexx (`trafag.rexx-suite.com`).
5. Laufen die bestehenden 8 Schnittstellen (darunter die Dateien fuer `Z_HR_PA_REXX`) danach ueber den Hub, und wer migriert sie
   (Entscheid Ingo 10.11.: Ingo selbst)? Der Stempeluhrtransfer bleibt.

## 14. Rexx-Hub-Abruf gebaut, Schalter aus (2026-11-10, `1e99745`, `d99fbe6`)

Auftrag Ingo: „Bereite alles vor fuer den Rexx Hub, wenn wir ihn haben, nur noch Schalter umlegen.“ Gebaut von einem Agenten im
eigenen Worktree, gegengeprueft von vier Pruefern (Robustheit, Schalter, Sicherheit, Einbau), Befunde eingearbeitet. Suite
1547/1547. ~~**Nicht deployed.**~~ *(Ueberholt am 2026-11-12: seit dem Deploy um 11:33 auf dem Server, `e956183`, Schalter `HrKpi:RexxHub:Enabled` weiter aus, es wird nichts abgerufen; `docs/rag/DEPLOYMENT.md`.)* Es gab keinen einzigen Aufruf an Rexx (kein Zugang); alle Testdaten sind erfunden.

**Was es tut.** `RexxHubRefreshService` holt die Rexx-Abfragen #775 und #777 einmal am Tag ab 05:30 ueber
`GET v1/datawarehouse/{id}` (Bearer-Token) und schreibt daraus genau die Dateien des Handexports in `hrdata`
(`Hrcockpitgesamtdashboard.xlsx`, `Hr-cockpit-absenzen-je-fall.xlsx`, Blatt `Rexx775`/`Rexx777`). Die Leser im
`HrKpiDashboardBuilder` sind unveraendert; ein Rundreise-Test belegt, dass die API-Datei dieselben Kennzahlen ergibt wie ein
Handexport mit denselben Werten.

**Dateien:** `Services/HrKpi/RexxHubClient.cs` (Abruf, Blaettern, Fehlertexte, prozessweit eine Anfrage gleichzeitig),
`RexxHubResponseParser.cs` (Antwortformen), `RexxHubColumns.cs` (Leser- und Pflichtspalten, Pruefungen), `RexxHubWorkbook.cs`
(Excel ueber `.tmp.xlsx`), `RexxHubRefreshService.cs` (Tagesdienst, Testabruf `RexxHubProbe`, Registrierung `AddRexxHub`,
Endpunkt `MapRexxHubTest`); Optionen `RexxHubOptions` in `Models/HrKpiModels.cs`; Tests `RexxHubTests.cs`, `RexxHubEndpointTests.cs`.

**Sicherungen** (alle getestet):
- `HrKpi:RexxHub:Enabled = false` (Vorgabe): kein Abruf, kein Datenbank- und kein Dateizugriff, auch nicht beim Start.
- Es wird **nicht geschrieben** und die bisherige Datei bleibt, wenn: eine Pflichtspalte fehlt oder zu weniger als 95 % gefuellt
  ist (`MinShare`); keine Zeile kommt; die Antwort weniger als 80 % der Zeilen der bisherigen Datei hat (`MinRowShareOfPrevious`);
  bei #775 kein Status als angestellt erkannt wird oder Personalnummern doppelt sind (`MaxDuplicatePersonalnummern`); Blaettern
  unvollstaendig ist (Gesamtzahl passt nicht, weitere Seiten ohne Weg, Link auf fremden Server).
  Pflichtspalten #775: Personalnummer, Personal Status, Eintrittsdatum, Austrittsdatum, Austrittsart, Stelle, Name;
  #777: Personalnummer, Abwesenheit von, Abwesenheit Buchung, Abwesenheit Status.
- Eine von Hand gelegte Datei (anderer Blattname) wird vor dem Ueberschreiben gesichert: zuerst `*.manuell.xlsx`, danach mit Zeitstempel.
- Voruebergehende Fehler (429, 5xx, Zeitueberschreitung, Netz) werden im naechsten 30-Minuten-Durchgang wiederholt, hoechstens
  `MaxAttemptsPerDay` (4) Mal; bei 429 zusaetzlich zwei Wiederholungen mit Wartezeit (Retry-After beachtet, hoechstens 60 s).
- Nur https (`AllowHttp` nur fuer Tests). Token nie in Protokoll, Fehlertext oder Testabruf; ins Protokoll kommen nur Zeilenzahl,
  Spaltennamen, Dauer und request id.
- Testabruf `/BiDashboard/admin/rexxhub/test` braucht die Richtlinie AdminOnly **und** die Admin-Freischaltung (Admin > Passwoerter),
  weil `Security:Enabled` produktiv `false` ist und AdminOnly dann niemanden abhaelt. Er schreibt nichts, geht auch bei
  `Enabled = false` und gibt keine Werte aus, nur Status, Zeilen, Seiten, Spaltennamen, fehlende Pflicht- und Leserspalten,
  Formatpruefung (erkannt gegen gefuellt), Meta- und Links-Schluessel, request id.

**Einstellungen** (`appsettings.json`, Abschnitt `HrKpi:RexxHub`; sichtbar ist nur `Enabled`): `GesamtQueryId` 775, `CaseQueryId` 777,
`PathTemplate` `v1/datawarehouse/{0}`, `PageParameter`/`PageSizeParameter` (leer: kein Seitenparameter, es gilt die Zeilenzahl je Seite
aus Rexx), `PageSize` 500, `MaxPages` 500, `DailyAt` 05:30, `TimeoutSeconds` 120, `RetryDelaySeconds` 5, `MaxAttemptsPerDay` 4,
`MinRowShareOfPrevious` 0.8, `MinShare` 0.95, `MaxDuplicatePersonalnummern` 0, `GesamtZeitraumGeprueft` false, `ColumnMap`
(technischer Name -> Ueberschrift), `QueryParameters` (mit Platzhaltern `{jahresanfang}`, `{heute}`), `Meta…Keys`/`NextLinkKeys`
(ergaenzen die eingebauten Namenslisten), `AllowHttp` false.

**Zugang:** Admin > Settings > Quellsysteme, Code `REXXHUB`, Anschlussart „SAP Gateway“ (nur dann zeigt der Dialog das URL-Feld),
URL = Basis-URL bis vor `v1/`, „Zentrales Passwort“ = Token, Aktiv. Den Knopf „Testen“ dort nicht druecken (macht einen SAP-Test).
Achtung: Ein Konfigurationsimport unter Settings ersetzt alle Quellsysteme; der Token liegt wie das SAP-Passwort im Klartext in der
Cockpit-Datenbank (`docs/AGENT_COORDINATION.md`/Wochen_Todo 135, gleiches Thema).

**Umschalten, Schritt fuer Schritt:**
1. Rexx (Tools > Rexx Hub): API-Benutzer anlegen und aktiv schalten, `GET /v1/datawarehouse/{id}` freigeben, IP des
   Dashboard-Servers (`tragvapp401`) als vertrauenswuerdig eintragen, „Zeilenanzahl pro Seite“ hoch setzen. OpenAPI-Datei exportieren:
   Sie zeigt Basis-URL, Form von `data`, Namen in `_meta`/`_links`.
2. Dashboard: Quellsystem `REXXHUB` wie oben anlegen.
3. Admin-Freischaltung, dann Testabruf `/BiDashboard/admin/rexxhub/test`. Gut, wenn beide Abfragen `Ok` und `WuerdeGeschrieben`
   melden, `FehlendePflichtspalten` leer ist und in der Formatpruefung „Erkannt“ gleich „Gefuellt“. Liefert die API technische
   Namen: in Rexx unter Export-Felder umbenennen oder `ColumnMap` setzen. Werte der Auswahlfelder muessen Rexx-Texte sein
   (kein Auswahlwerte-Mapping auf den API-Benutzer).
4. Zeitraum von #775 klaeren (der gespeicherte Filter stand am 04.11. fest bis 04.11.2026, `docs/HR_KPI.md` 8.13): ueber
   `QueryParameters` mit `{jahresanfang}`/`{heute}`, falls Rexx das annimmt, sonst in Rexx dynamisch stellen. Erst dann
   `GesamtZeitraumGeprueft = true` (bis dahin warnt jeder Schreibvorgang). Grund: Das Cockpit nimmt das Dateidatum als Stand;
   ein taeglich neu geschriebener, aber inhaltlich alter Export sieht sonst frisch aus.
5. `"HrKpi": { "RexxHub": { "Enabled": true } }` im Repository setzen, committen, mit dem Deploy-Werkzeug deployen (nicht nur auf
   dem Server aendern). Erster Abruf rund 3 Minuten nach dem Start, wenn es nach 05:30 ist. Ergebnis im Ereignisprotokoll, Kategorie HR
   („Rexx-Hub #775 geschrieben“ / „nicht geschrieben“ / „fehlgeschlagen“).
Zurueck: `Enabled = false`, deployen; die zuletzt geschriebenen Dateien bleiben, ein Handexport ersetzt sie wie bisher.

**Offen, Annahmen ueber die API** (stehen nicht im Dokument, im Testabruf zu bestaetigen): #775/#777 gelten als Data-Warehouse-
Abfragen; Form von `data` (Liste von Objekten, Objekt mit Spalten und Zeilen oder Liste von Listen werden erkannt); Namen in
`_meta` und `_links`; Seitenparameter; Datum als `2009-11-04` statt UTC-Zeitstempel (Zeitstempel werden gezaehlt, nicht umgerechnet);
Stunden als `hh:mm`, Zeitraum-Texte wie im Handexport; Netzweg vom Server zu Rexx (Proxy, Firewall). Die Spalte „Arbeitszeitmodell“
liest der Leser, sie fehlt aber vermutlich in #775 (harmlos, keine Pflicht). Jeder Testabruf kostet mindestens zwei der 50'000 Anfragen
im Monat, der Tagesdienst mindestens zwei pro Tag.

**Befund am bestehenden Leser (nicht geaendert, betrifft auch den Handexport):** `HrKpiDashboardBuilder.ReadDate` wirft bei einer Zahl
ausserhalb des Excel-Datumsbereichs, und `ReadRows` verwirft dann die ganze Datei; `ReadDecimalNullable` liest Text-Zahlen nach der
Serverkultur („4.5“ wird unter de-DE zu 45). Beides vom Agenten gemessen. Der Hub-Abruf schreibt deshalb Daten nur als echtes Datum
und Zahlen als Zahl.
