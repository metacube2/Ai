# Rexx-Schnittstelle nach SAP: Befund und Idee Tagesjob

Stand: 2026-11-24 10:10 (Abschnitt 22 Punkt 8: erster Tageslauf geschrieben 10:08, 4'417 Abwesenheiten, nach Wiederholung je Seite `142d304`); davor 2026-11-24 (Abschnitt 22: #777 eingeschaltet, Ursache Seite 56 war Exportfeld time_away, Stichtag 07.04.2026, `5b3774f`/`21d11b4` deployed); davor 2026-11-23 nachmittags (Abschnitt 21: /v1/requests eingerichtet, `001646b`/`df7b874` deployed, Schalter aus, Rexx 500 auf Seite 56); davor 2026-11-23 (Abschnitt 20: Antwort Artjom, /v1/requests liefert alle Abwesenheiten auch ohne Workflow, sieben Schnittstellen mit IDs, Lohnarten-Endpunkt bis Ende 2026, SFTP laut Artjom durch Upgreat); davor 2026-11-17 nachmittags (Abschnitt 15: Admin-Zugang erhalten, keine Testumgebung, Anleitung gelesen, Rueckfrage an Artjom gesendet); davor 2026-11-17 (Abschnitt 12: Hub bestellt, Offerte AG37140 am 11.11. unterschrieben, Mail an Artjom wegen Zugang; Nachtrag: Vorgesetzte je Person als zusaetzliches Lieferfeld, Quelle Rexx Hub statt AD, Wochen_Todo 164); davor 2026-11-12 (Doku-Pruefung: Abschnitt 14, „Nicht deployed“ ueberholt, Abruf seit dem Deploy vom 2026-11-12 11:33 auf dem Server, Schalter aus); davor 2026-11-10 (Entscheid Rexx Hub und Lieferfelder, Abschnitt 12; Hub-Mail und Produktbeschreibung gelesen, Abschnitt 13; Abruf ueber den Hub gebaut, Schalter aus, Abschnitt 14); Befund vom 2026-11-03. Untersucht auf Frage Ingo „finde heraus, wie die Daten von Rexx in SAP gelangen,
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

**Nachtrag 2026-11-17 (Entscheid Ingo nach der Sitzung mit Oscar):** Zusaetzlich gebraucht wird **der oder die Vorgesetzte je Person** (Personalnummer oder Name). Zweck: die Klick-Auswertung des Phishing-Trainings um Vorgesetzte und Hierarchie ergaenzen (Wochen_Todo 164). Quelle ist der Rexx Hub und nicht das AD, weil HR Aenderungen im AD selten nachtraegt. Ob Rexx das Feld fuehrt und wie es heisst, ist offen; mit Rexx (Artjom Yahno) zusammen mit dem Freigabelink fuer den Hub klaeren (Wochen_Todo 167). Die Mail vom 17.11. an Artjom (Zugang, Admin-Zugriff,
technischer Zugang zur Schnittstelle, Testumgebung) fragt das Feld noch nicht; es kommt mit der Feldliste. In den Excel-Listen
vom 10.11. (`HR-Cockpit_Felder_Rexx_nach_SAP_2026-11-10*.xlsx`) steht es nicht.

*(Aufgeloest am 2026-11-17: Der Hub ist bestellt. Nadja hat die Offerte AG37140 (neues Angebot mit Rabatt) am 2026-11-11 unterschrieben an Rexx zurueckgeschickt.)* Bis dahin galt: **Offen, widerspruechlich belegt:** ob
der Hub schon bestellt ist. `docs/HR_KPI.md` 8.3 nennt einen Auftrag Nadjas an Rexx
vom 2026-07-29; Abschnitt 11 oben sagt, sie habe am 19.08. Alex um die IT-Pruefung gebeten, bevor sie das Go gibt. Nicht
aufgeloest, bei Nadja nachfragen.

**Naechste Schritte:** Mail an Nadja senden (Ingo); Stand des Hub-Auftrags mit Nadja klaeren (bestellt oder nicht; *erledigt 17.11.: bestellt*); Lieferfelder, Format und taegliche
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

**Umschalten, Schritt fuer Schritt:** *(Ueberholt am 2026-11-19: es gilt Abschnitt 17; Pfad ist `v1/employees` statt `datawarehouse/{id}`, #777 nicht ueber den Hub.)*
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

## 15. Rexx Hub: Admin-Zugang erhalten, Anleitung gelesen, Rueckfragen an Artjom (2026-11-17)

**Mail Artjom Yahno (rexx systems) 17.11. 13:46** auf Ingos Anfrage von 11:02: Ingo hat den **HR-Admin-Zugang** mit Zugriff auf
den Rexx Hub. **Es gibt keine Testumgebung**, gearbeitet wird direkt im produktiven System. Zugangsdaten (Adresse, Benutzer, Token)
stehen nicht in der Mail. Anhang `DokumentationrexxHub_v17` (im Anhang als `.cdt`, von Ingo als `.docx` gespeichert, 55 Seiten,
Stand 13.08.2026; Kopie unversioniert in der Repo-Wurzel, Herstellerdokument nicht ins Repository).

**Was die Anleitung beantwortet (nur gelesen, kein Aufruf an Rexx):**
- API-Benutzer und Token legt ein Admin selbst an: „Tools > Rexx Hub“, Widget „API“ (neuer Benutzer erhaelt einen Token und steht auf
  „Aktiv = Nein“). Je Benutzer: Verfallsdatum, vertrauenswuerdige IP-Adressen, Ausgabesprache. Endpunkte werden je Benutzer freigegeben
  (gruen = frei, gelb = Benutzer inaktiv, rot = nicht frei); Swagger-Testseite je Endpunkt (Aufrufe erscheinen in den Hub-Logs).
- Berechtigungen: #1411 Endpunkte anzeigen, #1412 bearbeiten, #1417 Konfigurationslogs, #1418 Rexx Hub Log.
- Adresse nach Muster `https://KUNDE.rexx-systems.com/rexx_hub/v1/...`, Anmeldung `Authorization: Bearer <token>`.
- `GET /v1/datawarehouse/{dataWarehouseQueryId}` steht in der Endpunktliste (HR und ER), also der Weg, den `RexxHubClient` (Abschnitt 14)
  benutzt. Ob er bei uns grau (noch nicht verfuegbar) ist, zeigt erst das Widget „API Endpunkte“.
- Limit: eine Anfrage gleichzeitig ueber alle API-Benutzer, 50'000 Anfragen je Monat (Webhooks zaehlen mit); Hub-Logs 90 Tage.
- Antworten mit `data`, `_links` (Blaettern) und `_meta` (Anzahl, Seiten, request id), JSON oder XML; Fehlercodes 400/401/403/404/422/429/500/504.

**Mail Ingo an Artjom 17.11. (gesendet, laut Ingo; Text im Chat von Claude entworfen):** Bitte um Bestaetigung (1) der Basisadresse
`https://<Rexx-Adresse>/rexx_hub/v1`, (2) der Berechtigungen #1411/#1412/#1417/#1418 fuer Ingos Benutzer, (3) ob
`/v1/datawarehouse/{id}` freigeschaltet ist und die DWH-Abfragen #775 und #777 abrufbar sind oder freigegeben werden muessen. Angekuendigt:
nur lesender Zugriff, einmal am Tag frueh, eigener API-Benutzer mit IP-Beschraenkung auf unseren Server. Antwort offen.

**Naechste Schritte, wenn die Antwort da ist:** API-Benutzer anlegen (Verfallsdatum, IP des Servers), `datawarehouse` fuer #775/#777
freigeben, Token und Adresse in die Server-Einstellungen `HrKpi:RexxHub` (nicht ins Repository), Testabruf `/BiDashboard/admin/rexxhub/test`,
dann `Enabled = true` (Abschnitt 14).
## 16. Rexx Hub live angesehen: Data Warehouse liefert nur Kennzahlen, #775 gibt 404 (2026-11-19 10:50)

Auftrag Ingo 19.11. („wir muessen die warehouse abfragen bauen … einrichten in chrome“). In Rexx (`https://trafag.rexx-systems.com`,
produktiv) nur angesehen; geaendert wurde nichts. Zwei Testaufrufe ueber die Swagger-Testseite des Hubs (Freigabe Ingo im Chat, je ein
Aufruf von 50'000 im Monat, erscheinen in den Hub-Logs).

- **Hub ist eingerichtet, nicht leer:** „API User, #1“, angelegt vom Rexx-Support am 18.11.2026 10:05/10:08, Aktiv = Ja, Token
  vorhanden. Freigegeben ist nur `GET /v1/datawarehouse/{dataWarehouseQueryId}` (gruen), alle anderen Endpunkte rot. Parameter-Mapping
  `dataWarehouseQueryId` = „Fest definiert“. **Keine erlaubten IP-Adressen, kein Verfallsdatum** (vor produktiver Nutzung setzen).
- **Basisadresse bestaetigt:** `https://trafag.rexx-systems.com/rexx_hub/v1/` (nicht `rexx-suite.com`, Abschnitt 13 Punkt 4 ueberholt).
- **Data-Warehouse-Abfragen in Rexx sind Kennzahlentabellen** (Start > Admin Services > Data Warehouse): Zeilen = Kennzahl, Spalten =
  Monate, z. B. #65 „HR Standardabfrage“ (Personal total, weiblich, maennlich, Frauenquote, Frauenquote Leitung, Ø Alter, FTE,
  Headcount, Fluktuation %), weitere #359 bis #373 (u. a. #364 Ein-/Austritte, #365 Krankheit, #370 Arbeitsverhaeltnis). Keine Listen
  je Person.
- **Test `datawarehouse/775`: 404 `NOT_FOUND`**, `parameter_name: dataWarehouseQueryId` (request id `req_68b5444993d6af3c8469f829e25d9a11`).
  #775 (Personal > Abfrage) und damit vermutlich auch #777 (Antragsliste) sind keine Data-Warehouse-Abfragen.
- **Test `datawarehouse/65`: 200**, Form `{"data":[{"kennzahl":"Personal total","grenzwert":null,"januar_2026":254, … "august_2026":270}, …],
  "_links":…, "_meta":…}`; Monatsspalten als technische Namen `monat_jahr`.

**Folge:** Die Annahme aus Abschnitt 13/14 („#775/#777 gelten als Data-Warehouse-Abfragen“) ist widerlegt. Der gebaute
`RexxHubRefreshService` kann so keine Personenliste und keine Abwesenheiten je Fall holen; Schalter bleibt aus. Ein Nachbau von #775/#777
als DWH-Abfrage ist nicht moeglich, weil das Data Warehouse nur Kennzahlen je Monat fuehrt. Moegliche Wege (Entscheid Ingo, Rueckfrage Artjom):
1. Stamm je Person ueber `/v1/employees` (+ `/employments`, `/org-units`, `/cost-accounts`) freigeben, Export-Felder waehlen.
2. Abwesenheiten je Fall und Salden: kein eigener Endpunkt bekannt; bei Artjom fragen (ggf. `/v1/requests`, oder Personal-Abfragen ueber den Hub).
3. DWH-Kennzahlen (#365 Krankheit, #364 Ein-/Austritte) nur als Gegenprobe zum Cockpit, nicht als Quelle.

**Nachtrag 2026-11-19 11:05: vollstaendige Endpunktliste aus dem Hub** (API User #1, Seite „Rexx Hub API“, nur gelesen; massgeblich vor
beiden Herstellerdokumenten). Alle HR, Stand heute; nur `datawarehouse` ist freigegeben.
- Lesen je Person: `GET /v1/employees` (+ `/recent`, `/updated`, `/{employeeId}`), je Person `/occupations`, `/org-units`,
  `/cost-accounts`, `/projects`, `/employments`, `/contracts`, `/probation`, `/limitations`, `/skills`, `/relationships`,
  `/events/{eventType?}` (laut Anleitung v17 Veranstaltungen wie E-Learning, **keine Abwesenheiten**), `/historic/{fieldId}`
  (Verlauf einzelner historischer Felder, Liste in Anleitung v17 5.5, ohne Abwesenheit).
- Stammdaten: `/v1/org-units`, `/v1/org-locations`, `/v1/positions`, `/v1/cost-accounts`, `/v1/persons`, `/v1/projects`,
  `/v1/job-families`, `/v1/payments`, `/v1/configuration/*` (u. a. `employment-types`, `personnel-groups`, `person-types`,
  `position-types`, `org-unit-types`), `/v1/logbook/masterdata`.
- Sonstiges: `/v1/requests` (+ `/recent`, `/updated`, `/{applicationId}`; Parameter heisst `applicationId`, eher Bewerbungs- oder
  Anfrage-Vorgaenge als Abwesenheitsantraege, ungeprueft), `/v1/calendar/{userId}`, `/v1/staff-appraisals`, `/v1/skills*`,
  `/v1/talent-pools*`, `/v1/houses*`, `/v1/communication/*`, `/v1/upload`, `GET /v1/test` (Verbindungstest).
- **Neu, in keinem der beiden Dokumente:** `GET /v1/time-management/entries` („Retrieve all time entries for employees“),
  `GET/PATCH/DELETE /v1/time-management/entries/{entryId}`, `POST /v1/time-management/entries`. **Einziger Kandidat fuer Zeit- bzw.
  Abwesenheitsdaten**; ob darin Abwesenheiten (Krankheit, Ferien) oder nur Kommen/Gehen stehen, ist offen (Testaufruf nach Freigabe
  des Endpunkts oder Frage an Artjom). Kein Endpunkt fuer Zeitsalden (Urlaub Rest, Stunden Saldo).

**Nachtrag 2026-11-19 11:10: zwei Endpunkte freigegeben, Feldkatalog geprueft** (Ja Ingo im Chat).
- **Freigegeben beim API User #1:** `GET /v1/employees` (10:56) und `GET /v1/time-management/entries` (10:54). Ohne Testaufruf.
- **`time-management/entries` taugt nicht fuer Abwesenheiten:** Laut OpenAPI der Swagger-Seite kennt der Parameter `types[]` nur
  `comego` („Arbeitszeit“, Kommen/Gehen); Felder `entryId, employeeId, date, time, bookingType, Type, comment, comment_portal,
  employeeName, createdAt`; Filter `employeeId, employeeName, dateFrom, dateTo, timeAccount, timeFrom, timeTo`. **Zuruecksetzen auf
  „nein“ hat zweimal nicht gegriffen** (Auswahl „nein“, Haken, danach wieder „Ja“, 10:57 und 10:58); steht weiter frei, von Hand
  zuruecksetzen.
- **`/v1/employees` hat denselben Feldkatalog wie die Personal-Abfrage (694 Felder), alle 25 Spalten von #775 sind waehlbar**
  (`RexxHubColumns.cs` Gesamtliste: Personalnummer bis „Unfall angetreten (Zeitraum)“, einschliesslich Austrittsart, Stunden Saldo,
  Urlaubsanspruch, Urlaub Rest, Ferien ausstehend/angetreten, Krankheit, Krankheit ueber 60 Tage, Unfall, Kompensation, Militaer).
  Dazu **Vorgesetzte** (Wochen_Todo 164): u. a. „Personalnummer (Vorgesetzte)“, „ID Personal (Vorgesetzte)“, „Primaere Vorgesetzte 1“
  bis 5. Heute 9 Export-Felder voreingestellt (ID Personal, Vorname, Nachname, Stelle, Organisation, Leitung j/n, Arbeitsverhaeltnis,
  Eintrittsdatum, Personal Status), technische Namen `unknown`, `unknown_1` … (in Rexx umbenennbar). Feldwahl nur angesehen, abgebrochen.
- **Folge:** #775 laesst sich als Export-Felder von `/v1/employees` nachbauen. Offen: Zeitraum der Summen „angetreten“ (Export-Filter
  pruefen), Grenze 422 bei zu vielen Feldern, Abruf im Code von `datawarehouse/{id}` auf `employees` umstellen. **#777 (Abwesenheiten je
  Fall) hat weiter keinen Endpunkt**, Frage an Artjom.

**Nachtrag 2026-11-19 11:20: eigener API-Benutzer angelegt (Auftrag Ingo: Bestehendes nicht ueberschreiben, lieber neu anlegen).**
- **Neu: „BI-Dashboard HR-Cockpit, #2“**, Aktiv = Nein, ohne Verfallsdatum und IP-Sperre, Token von Rexx erzeugt (nicht gelesen,
  nicht notiert). Freigegeben nur `GET /v1/employees` (gelb, weil Benutzer inaktiv). API User #1 (Support) bleibt wie vorgefunden,
  ausser den beiden heutigen Freigaben (`employees`, `time-management/entries`).
- **Ziel (Ingo 19.11.):** alle Felder, die heute per Datei eingelesen werden, ueber den Hub, wie in Abschnitt 12 und 14 beschrieben.
- **Export-Felder bei #2 noch nicht gespeichert:** Im Dialog „Felder auswaehlen“ waren die 22 fehlenden Spalten von #775 plus
  „Personalnummer (Vorgesetzte)“ zu den 9 Vorgaben hinzugefuegt; „Speichern“ blieb mit rotem Ladebalken haengen, nach Neuladen
  weiter 9 Felder (11:12). Gleiches Haengen wie beim Zuruecksetzen von `time-management` bei #1. Nicht weiter probiert.

**Nachtrag 2026-11-19 11:30: #775 im Hub nachgebaut, Testaufruf erfolgreich.**
- API User #2 „BI-Dashboard HR-Cockpit“: **31 Export-Felder** bei `GET /v1/employees` gespeichert (das Speichern von 11:12 kam
  verspaetet doch an; Rexx speichert teils erst nach ueber einer Minute). Technische Namen gesetzt (11:26), Schema: Kleinbuchstaben,
  Umlaute ausgeschrieben, Unterstrich: `personalnummer`, `nachname_vorname_link_personal`, `personal_status`, `stelle`, `organisation`,
  `kostenstelle`, `eintrittsdatum`, `austrittsdatum`, `austrittsart`, `geburtsdatum`, `o_taegliche_sollarbeitszeit_woche`,
  `arbeitszeitmodell`, `stunden_saldo`, `urlaubsanspruch`, `urlaub_rest`, `ferien_ausstehend_tage`, `ferien_angetreten_tage`,
  `krankheit_angetreten_stunden`, `krankheit_ueber_60_tage_angetreten_stunden`, `unfall_angetreten_stunden`,
  `kompensation_angetreten_stunden`, `militaer_zivildienst_angetreten_stunden`, `krankheit_angetreten_zeitraum`,
  `krankheit_ueber_60_tage_angetreten_zeitraum`, `unfall_angetreten_zeitraum`, `personalnummer_vorgesetzte`; dazu die Vorgaben
  `employee_id`, `vorname`, `nachname`, `leitung_j_n`, `arbeitsverhaeltnis`.
- **Aktiv = Ja seit 11:29** (noetig fuer den Test), ohne IP-Sperre und Verfallsdatum; vor dem Server-Einsatz IP von `tragvapp401` setzen.
- **Testaufruf 11:30** (`/v1/employees?page=1`, Swagger, nur Zaehlung ausgewertet, keine Werte gelesen): 200, `_meta` = `total_items 396,
  items_per_page 50, current_page, total_pages 8, timestamp, request_id`, `_links` = `self, first, last, prev, next` (Objekte mit `href`).
  Seite 1: 50 Zeilen; Austrittsdatum bei 21 von 50 gefuellt (Ausgeschiedene sind enthalten). Typen: `kostenstelle`, `stelle`,
  `organisation` sind **Listen** (array), `ferien_*`, `urlaub*`, `o_taegliche_sollarbeitszeit_woche` Zahlen, `stunden_saldo` und die
  Stunden-Summen Text, Datumsfelder Text. Vorgesetzte bei 48 von 50 gefuellt.
- **Offen fuer den Code:** Abruf von `datawarehouse/{id}` auf `employees` mit `page` umstellen, `ColumnMap` technisch -> Ueberschrift,
  Listenfelder zusammenfuegen, Zeilen je Seite in Rexx auf 500 (heute 50, 8 Anfragen je Abruf). Zu klaeren: Zeitraum der Summen
  „angetreten“ (laufendes Jahr?) und ob 396 Personen alle Faelle von #775 abdecken (Handexport-Zeilenzahl vergleichen).
- **User #1:** `time-management/entries` und `employees` stehen weiter auf frei; „nein“ wurde dreimal gewaehlt und gespeichert, blieb aber
  nie stehen. Ingo setzt es von Hand zurueck oder fragt Artjom.

## 17. Code auf GET v1/employees umgestellt, deployed, Schalter aus (2026-11-19 11:48, `b7f52a9`)

Auftrag Ingo 19.11. („mach alles was du machen kannst, 777 schaue ich mit Artjom an“). Zweig `rexxhub-employees-1911` ab Produktivstand
`7b2ff9f`, gemergt `656bb63`, deployed 11:48 (`docs/rag/DEPLOYMENT.md`). Suite 1'669/1'669. **Abschnitt 14 gilt weiter bis auf das hier
Geaenderte** (Sicherungen, Testabruf, Protokoll).

**Geaendert:**
- `RexxHubOptions`: `GesamtPathTemplate`, `CasePathTemplate` (leer = `PathTemplate`), `CaseEnabled`, `ForKind()`.
- Tagesdienst und Testabruf holen die Gesamtdatei ueber `GesamtPathTemplate`; mit `CaseEnabled = false` wird #777 nicht abgerufen, der
  Testabruf meldet „uebersprungen“.
- Parser: JSON-Listen (Stelle, Organisation, Kostenstelle) werden zu einem Text mit „, “; leere Liste = leer.
- `RexxHubColumns.Normalize`: ISO-Daten in den Spalten „… angetreten (Zeitraum)“ werden zu `dd.MM.yyyy` (der Leser kennt nur das).
- `appsettings.json` (Abschnitt `HrKpi:RexxHub`): `GesamtPathTemplate = v1/employees`, `CaseEnabled = false`, `PageParameter = page`,
  `QueryParameters.page = 1` (der Endpunkt verlangt `page`), `ColumnMap` der 31 technischen Namen auf die Ueberschriften von #775.
  `arbeitszeitmodell` wird bewusst auf „Arbeitszeitmodell (Hub, nicht gelesen)“ abgebildet: #775 fuehrt die Spalte nicht, und der Leser
  saehe sonst „Vollzeit“ als FTE 1 vor der Sollzeit; Kennzahlen bleiben damit wie beim Handexport. **`Enabled` bleibt false.**

**Umschalten, ab jetzt (ersetzt Abschnitt 14 Schritte 1 bis 5):**
1. Rexx (erledigt bis auf IP und Verfallsdatum): API-Benutzer #2 „BI-Dashboard HR-Cockpit“, aktiv, `GET /v1/employees` frei, 31 Felder.
   Noch setzen: erlaubte IP = IP von `tragvapp401`, Verfallsdatum, „Zeilenanzahl pro Seite“ *(erledigt 19.11. 12:20: 100, mehr bietet Rexx nicht)*.
2. Dashboard: Admin > Settings > Quellsysteme, Code `REXXHUB`, Anschlussart „SAP Gateway“, URL `https://trafag.rexx-systems.com/rexx_hub/`,
   „Zentrales Passwort“ = Token von API-Benutzer #2 (in Rexx kopieren, **nicht ins Repository**), Aktiv. Das macht Ingo.
3. Testabruf `/BiDashboard/admin/rexxhub/test` (Admin-Freischaltung): Gesamt `Ok` und `WuerdeGeschrieben`, `FehlendePflichtspalten` leer,
   Formatpruefung „Erkannt“ = „Gefuellt“; Faelle „uebersprungen“.
4. **Zeitraum pruefen, bevor `GesamtZeitraumGeprueft = true`:** frischen Handexport #775 (01.01. bis heute) neben den Testabruf legen und
   Spaltensummen der Summen „angetreten“ und die Personalnummern vergleichen (Werkzeug, nicht von Hand). Weicht der Hub ab (z. B. nur
   Vorjahr oder laufender Monat), in Rexx beim Endpunkt einen Export-Filter setzen oder Artjom fragen.
5. `"Enabled": true` in `appsettings.json`, committen, mit dem Deploy-Werkzeug deployen. Danach kommt die Gesamtdatei taeglich ab 05:30;
   #777 bleibt Handexport, bis Artjom einen Weg nennt. *(Nachtrag 23.11.: Weg genannt, `/v1/requests`, Abschnitt 20.)*

**Nachtrag 2026-11-19 12:20 (Auftrag Ingo „alles fertigmachen“):**
- **Zeilen je Seite bei API-Benutzer #2 `/v1/employees` auf 100 gesetzt** (Rexx bietet nur 5/10/25/50/75/100; 500 aus Abschnitt 17
  Schritt 1 geht nicht). 396 Personen = 4 Anfragen je Abruf. Ursache der frueheren Speicherprobleme: der Klick traf den Auswahlknopf
  (`ui-multiselect`) statt des Speicherknopfs `button.save` im Bearbeitungsformular; mit `button.save` speichert Rexx sofort.
- **User #1 (Support): `employees` und `time-management/entries` lassen sich nicht zuruecknehmen**, auch mit `button.save` bleibt „Ja“
  (Liste nach Neuladen weiter gruen). Vermutlich darf nur der Rexx-Support diesen Benutzer aendern. Artjom bitten, beide wieder zu sperren.
- **IP-Sperre nicht gesetzt:** Der Server `tragvapp401` hat intern `10.120.1.17`; Rexx sieht aber die oeffentliche Adresse, ueber die
  Trafag ins Internet geht, und die ist nirgends dokumentiert. Eine falsche Adresse sperrt den Abruf. Vorgehen: nach dem ersten Testabruf
  mit Token die Quelladresse in „Rexx Hub Logs“ ablesen und erst dann bei Benutzer #2 unter „Erlaubte IP-Adressen“ eintragen.
- **Verfallsdatum nicht gesetzt:** Laeuft der Token ab, steht der Abruf still. Entscheid Ingo (Vorschlag: ein Jahr, Erinnerung in Wochen_Todo).
- Der Token wird nicht von Claude ins Dashboard eingetragen (Regel: keine Tokens in Felder eingeben); Schritt 2 in Abschnitt 17 macht Ingo.

## 18. Eingeschaltet (2026-11-19 13:25, `a848f93`)

Auftrag Ingo „mach weiter, alles fertigmachen“. Ablauf:
- **Quellsystem `REXXHUB`** im Dashboard angelegt (Claude: Code, Name, Anschlussart „SAP Gateway“, URL `https://trafag.rexx-systems.com/rexx_hub/`,
  Aktiv; **Token von Ingo eingefuegt**, Username leer; „Quellsysteme speichern“ 13:10).
- **Testabruf 13:11:** 200, 396 Zeilen, 4 Seiten zu 100, alle Pflichtspalten, alle Formate erkannt; **verworfen wegen 1 doppelter
  Personalnummer** (v1/employees liefert eine Person mit zwei Arbeitsverhaeltnissen zweimal; Handexport 04.11.: 393 Zeilen, 0 doppelt).
- **IP-Sperre:** Quelladresse des Servers laut „Rexx Hub Logs“ `212.243.171.23` (intern 10.120.1.17), bei Benutzer #2 unter „Erlaubte
  IP-Adressen“ eingetragen; Testabruf danach weiter 200. Swagger aus dem Browser (andere Adresse) geht fuer #2 damit nicht mehr.
- **Nachbesserung `b907402`** (auf Produktivstand `e8a8743` gemergt `85b2449`, deployed 13:23): bei doppelter Personalnummer bleibt die Zeile
  mit dem juengsten Eintritt (Protokollhinweis); Testabruf zeigt `Summen` und `EntfernteDoppelzeilen`. Testabruf danach: 395 Zeilen,
  1 entfernt, `WuerdeGeschrieben = true`.
- **Zeitraum geprueft** (Summen Hub 19.11. gegen Handexport 04.11., Zeitraum dort 01.01. bis 04.11.): Krankheit 14'374,8 h gegen 13'540,5 h
  (+6 %), Ferien angetreten 4'998 gegen 4'854,5 Tage (+3 %), Unfall 1'711,8 gegen 1'659,8 h, Kompensation 9'467,6 gegen 9'089,2 h,
  Militaer 528 gegen 496 h, Urlaubsanspruch 6'747,5 gegen 6'743,5, Urlaub Rest 861 gegen 1'027. Passt zu „01.01. bis heute“ plus 15 Tage.
- **`a848f93`:** `HrKpi:RexxHub:Enabled = true`, `GesamtZeitraumGeprueft = true`, deployed 13:25. Tagesabruf ab 05:30; nach jedem Start
  rund 3 Minuten spaeter, wenn faellig. Die bisherige Handexport-Datei wird beim ersten Schreiben als `Hrcockpitgesamtdashboard.manuell.xlsx`
  gesichert. **#777 bleibt Handexport** (`CaseEnabled = false`), Ingo klaert mit Artjom. *(Nachtrag 23.11.: Artjom nennt `/v1/requests`, auch fuer direkt gebuchte Abwesenheiten; noch nicht umgestellt, Abschnitt 20.)*
- Offen: Verfallsdatum fuer Benutzer #2 (Entscheid Ingo), User #1 Freigaben `employees`/`time-management` durch Artjom sperren lassen.
- **Erster Abruf 13:28:59** (Ereignisprotokoll HR „Rexx-Hub #775 geschrieben“): 395 Zeilen, 31 Spalten, 4 Seiten, 9,3 s, bisher 393 Zeilen,
  1 Doppelzeile entfernt. Handexport gesichert als `hrdata\Hrcockpitgesamtdashboard.manuell.xlsx`. Nachgezaehlt: 395 Zeilen, 0 doppelt,
  Summen gleich dem Testabruf. Die Sichtpruefung im HR-Cockpit (HR-Login) macht Ingo.

## 19. Rexx-Schnittstellen in P76 nur gelesen (2026-11-19, 13:49 bis 14:40, Agent sap-t76, Freigabe Ingo „P76 und T76 lesen“)

Nichts geschrieben, kein Job, kein Report. Eigene P76-Sitzung, am Ende geschlossen. Exporte im Scratchpad der Sitzung (nicht im
Repository). **Abweichung:** T76 las der Agent ueber `RunSapProbe.ps1` (Passwort aus der lokalen DPAPI-Ablage, nichts eingegeben) statt
nur per GUI; in P76 zusaetzlich AL11 sowie TEVEN/PA2001 (nur Anzahl, Datum, Kuerzel, Art).

| Schnittstelle | Datei | Import in SAP | Letzter Lauf | Haeufigkeit | Benutzer |
| --- | --- | --- | --- | --- | --- |
| Stammdaten Rexx->SAP (`Z_HR_PA_REXX`) | **automatisch jede Nacht** nach `/usr/sap/rexx/Stammdaten` (944 Dateien `JJJJMMTThhmmss_Export Stammdaten.csv` seit 01.06.2024, Namenszeit 23:00, Dateizeit 23:50, Besitzer `root`) | **von Hand**, Dialog, kein Job | 17.11.2026 09:08 | 58 Laeufe seit 04/2024; keiner zwischen 23.04.2025 und 03.09.2026; 2026: 09 (12), 10 (1), 11 (2) | RIS, XHA |
| Absenzen (`Z_HR_REXX_PA_ABS`, Mappe `ABS_2001`) | `/usr/sap/rexx/Abwesenheiten`, 20 Dateien, letzte 09.07.2026 | **von Hand** | **09.07.2026** | 2026 nur 22.02. und 09.07. | RIS |
| Zeitereignisse (`Z_HR_REXX_PA_ZE`, Mappe `ZEII2011`) | `/usr/sap/rexx/Zeitbuchungen`, monatlich, letzte 21.10.2026 | von Hand (vermutet) | 21.10.2026 | monatlich (TEVEN: 22.09. 24'317, 21.10. 23'924 Saetze) | RIS |
| Lohnarten SAP->Rexx (`Z_HR_PA_REXX_LOHNDATEN_EXPORT`) | nach `/usr/sap/rexx/Benefits_Gehaltsdaten` (vermutet, Ordner leer) | **einziger Job** `LOHNDATEN NACH REXX`, monatlich am 28. 06:00 | 28.10.2026 (aus Ordnerzeit) | monatlich | eingeplant unter **`EXPRIS_SIWY`** (externer Berater) |

**Befunde:**
- **Seit 09.07.2026 kein Absenz-Import mehr** (PA2001: einzige Massenaenderung der Rexx-Arten 0100/0400/0500/0600 am 09.07., 693 Saetze).
  Ferien, Kompensation und Militaer sind in SAP seit Juli nicht nachgefuehrt; das Cockpit gleicht es ueber Rexx #775 (Hoechstwert) aus.
- Rexx legt Stammdaten taeglich ab, eingelesen wird selten. Alle Dateien gehoeren `root`, die Ablage kommt von aussen (naechtlicher
  Kopierlauf auf Betriebssystemebene, Weg aus SAP nicht sichtbar). Handimporte nehmen die Datei vermutlich vom PC (Protokoll Minuten
  nach der Namenszeit, Serverkopie erst 23:50).
- Lohnarten-Job haengt am Konto `EXPRIS_SIWY`; faellt es weg, laeuft der Export nicht mehr.
- Leere Ordner `Lohnabrechnung` (26.10.2026) und `Lohnausweis` (02.02.2026), kein Programm des Pakets schreibt dort.
- P76 hat keine REXX-Objekte, die T76 nicht hat; zusaetzlich Funktionsgruppe `ZHR_REXX`, Tabelle `ZHR_REXX_MAPPING` (Paket `ZHR1`, EXPRIS 2023).
- Zaehlungen aus APQI/TBTCO sind Untergrenzen (erfolgreiche Mappen und alte Jobs werden geloescht).

**Folge fuer die Migration auf den Hub:** Stammdaten ueber `/v1/employees` (Felder der Importdatei erweitern), Kommen/Gehen ueber
`/v1/time-management/entries`, Absenzen ohne Endpunkt (Frage Artjom) *(ueberholt 23.11.: `/v1/requests`, Abschnitt 20)*, Lohnarten SAP->Rexx unklar (`/v1/payments` nur lesen). Offen:
wer die `root`-Ablage betreibt (Basis/IT), welche 8 Schnittstellen Rexx verrechnet (Artjom), Job vom Berater-Konto loesen, Sonja wegen
Absenz-Import seit Juli ansprechen, Zweck der Ordner Lohnabrechnung/Lohnausweis.

**Nachtrag 2026-11-19 15:00: Schreibweg fuer Lohnarten SAP->Rexx im Hub geprueft (nur angesehen, nichts freigegeben oder gespeichert).**
- Schreibende Endpunkte (POST/PATCH/DELETE) gibt es fuer calendar, communication, cost-accounts, employees, houses, job-families,
  org-locations, org-units, persons, positions, projects, skill-groups, skills, talent-pools, time-management/entries und upload.
  **Fuer payments gibt es nur GET; einen Endpunkt fuer Lohnarten oder Abrechnungsergebnisse gibt es nicht.**
- `PATCH /v1/employees/{employeeId}` schreibt feste Datenfelder (Name, Vertrag, Adresse, Kontakt, external_id) und waehlbare
  „Import Felder“ (Katalog 49 Felder, heute 0 gewaehlt). Darunter **Lohn, Lohnart, Monatslohn 100 %, Basis Stundenlohn, Basis Praemie,
  ZGP-Lohnstufe**, ausserdem z. B. Abrechnungskreis, Mitarbeiterkreis, Planstelle, Pensionskasse, „Export SAP“. Das sind Einzelwerte je
  Person (jeder Aufruf ueberschreibt), keine Monatsliste je Lohnart mit Datum und Betrag wie im heutigen Export
  (`Personalnummer;Lohnart ID;Abrechnungsdatum;Betrag;Currency`).
- `POST /v1/upload` legt nur eine Datei ab; ob Rexx eine hochgeladene Lohndatei weiterverarbeitet, steht nirgends.
- Folge: Ein aktueller Lohnwert je Person liesse sich per PATCH schreiben; die Monatshistorie je Lohnart (Ordner `Benefits_Gehaltsdaten`)
  nicht. Frage an Artjom: welches Rexx-Modul liest die Lohnarten-Datei, und gibt es dafuer einen Hub-Weg.
- Hinweis: Beim PATCH-Endpunkt von Benutzer #2 steht „Zuletzt bearbeitet 19.11.26 14:10, Ingo Kohler“ (Parameter-Mapping employeeId =
  ID Personal); von Claude nicht geaendert, Freigegeben = Nein.

## 20. Antwort Artjom (Rexx) auf die Mail vom 19.11. (eingegangen 23.11., von Ingo in den Chat kopiert)

Ingo hat die Mail mit vier Fragen am 19.11. um 14:23 gesendet (Entwurf aus Abschnitt 19). Artjom Yahno (Consultant, rexx systems
Schweiz AG) antwortet:

1. **Abwesenheiten:** Im Hub stehen **saemtliche Abwesenheiten unter `/v1/requests`**, auch solche, die HR direkt bucht und die keinen
   Workflow durchlaufen; Rexx legt auch diese als Request ab. Damit hat #777 (Abwesenheiten je Fall) einen Hub-Weg. *Ueberholt damit:*
   „#777 hat keinen Hub-Endpunkt“ (Abschnitte 17, 18) und die Grenze „direkt gebuchte Krankheit ohne Datum“ (`docs/HR_KPI.md` 8.14),
   soweit `/v1/requests` die Faelle mit Datum liefert. **Noch nicht geprueft:** Felder von `/v1/requests` (Art, Status, Von/Bis, Tage,
   Tagessoll, Buchungs-ID wie in #777), Freigabe fuer API-Benutzer #2, Menge und Zeitraum gegen den Handexport #777.
2. **Bestehende Schnittstellen laut Rexx (sieben, nicht acht):**

   | ID | Name bei Rexx | Zuordnung (Abschnitt 19) |
   | --- | --- | --- |
   | 3 | Kaba Import Zeit | Stempeluhrtransfer dormakaba -> Rexx; bleibt (Entscheid 10.11., Abschnitt 12) |
   | 23 | Import Lohnabrechnungen | vermutlich Ordner `/usr/sap/rexx/Lohnabrechnung` (leer, 26.10.2026); Sender in SAP unbekannt |
   | 39 | SAP Gehaltsdaten Import | Lohnarten SAP -> Rexx, Job `LOHNDATEN NACH REXX` unter `EXPRIS_SIWY`, Ordner `Benefits_Gehaltsdaten` |
   | 117 | SAP Export Stammdaten | Stammdaten Rexx -> SAP (`Z_HR_PA_REXX`) -> Hub `/v1/employees` |
   | 118 | SAP Export Zeit | Zeitereignisse Rexx -> SAP (`Z_HR_REXX_PA_ZE`) -> Hub `/v1/time-management/entries` |
   | 119 | SAP Export Abwesenheiten | Absenzen Rexx -> SAP (`Z_HR_REXX_PA_ABS`) -> Hub `/v1/requests` |
   | 121 | Import Lohnausweis | vermutlich Ordner `/usr/sap/rexx/Lohnausweis` (leer, 02.02.2026); Sender in SAP unbekannt |

   Das Angebot AG36071 nannte acht verrechnete Schnittstellen (11'200 EUR/Jahr, Abschnitt 11); Artjom sieht sieben. **Offen:** ob
   Trafag fuer acht oder sieben zahlt (Rechnung bzw. Riccardo Schwick). Die Zuordnung der Spalte rechts ist Ableitung aus Namen und
   Ordnern, nicht von Rexx bestaetigt; wer 23 und 121 befuellt (Lohnabrechnungen und Lohnausweise, vermutlich PDF aus der Lohnabrechnung),
   ist offen.
3. **Lohnarten SAP -> Rexx:** Im Hub gibt es dafuer noch keine Schnittstelle; sie soll **voraussichtlich bis Ende 2026** kommen. Bis dahin
   bleibt Schnittstelle 39 mit dem Job unter `EXPRIS_SIWY`. Die Frage, welches Rexx-Modul die Datei liest und ob der Monatsverlauf noetig
   ist, hat Artjom nicht beantwortet.
4. **Dateiuebertragung:** Die Dateien legt laut Artjom **der Provider auf dem SFTP-Server** ab, „meines Wissens Upgreat“. Das passt zu
   den `root`-Dateien mit Zeit 23:50 (Abschnitt 19), **widerspricht aber dem Upgreat-Ticket #12699584** („Unable to solve“, Upgreat
   betreut Rexx nicht, Abschnitt 11). Bei Upgreat bzw. Basis/IT klaeren, welcher Transfer laeuft und was nach der Umstellung abgestellt
   wird.

**Folge fuer die Migration:** 117, 118 und 119 lassen sich ueber den Hub abloesen (employees, time-management/entries, requests); 3 bleibt;
39 wartet auf den Hub-Endpunkt Ende Jahr; 23 und 121 sind zu klaeren. **Naechster Schritt (Entscheid Ingo):** `/v1/requests` fuer
Benutzer #2 freigeben, Felder und Mengen gegen #777 pruefen, dann den Fall-Abruf im Code darauf umstellen (heute `CaseEnabled = false`).
Nichts davon ist am 23.11. umgesetzt; nur Doku.

## 21. #777 ueber GET v1/requests: eingerichtet, deployed, Schalter aus; Rexx liefert Seite 56 mit Fehler 500 (2026-11-23)

Auftrag Ingo 23.11. („mach 1-4“ nach Abschnitt 20, dann „mach weiter so weit du kannst“). **Ergebnis: alles vorbereitet und deployed,
`CaseEnabled` bleibt false, weil Rexx bei `GET /v1/requests` ab Seite 56 mit HTTP 500 antwortet.** #777 bleibt bis dahin Handexport.

**1. Rexx (produktiv, API-Benutzer #2 „BI-Dashboard HR-Cockpit“, 13:19 bis 13:24):** `GET /v1/requests` freigegeben, Zeilen je Seite 100,
zehn Export-Felder ergaenzt (zu den 11 voreingestellten `application_*`, zusammen 21): Personalnummer `personnel_number`,
Abwesenheit von `absence_of`, bis `absence_until`, Buchung (Art) `absence_entry`, Status `absence_status`, Tage `days_of_absence`,
Tagessoll `absence_from_daily_target`, halbe Tage `absences_for_half_a_day`, ID Buchung `absence_id_booking`, Zeit `time_away` (Liste).
Format laut Swagger: Datum ISO, Tage Zahl, Personalnummer Text. Nach jedem Speichern neu geladen und geprueft.
*Export Filter:* um 14:28 „Abwesenheit Buchung“ (`absence_entry`) angelegt, um 14:46 wieder entfernt (0 Filter, Stand wie vorher). Laut
Handbuch ist ein Export Filter nur ein Query-Parameter, den der Aufruf mitschicken kann, kein fester Filter in Rexx.

**2. Code (Zweig `rexxhub-requests-2311` ab Produktivstand `155276e`, Worktree `C:\TMP\TrafagSalesExporter_rx2311`):**
- `001646b` (deployed 13:34): `CasePathTemplate = v1/requests`, ColumnMap der zehn technischen Namen auf die Ueberschriften von #777;
  `Normalize` entfernt bei der Fall-Datei Antraege ohne „Abwesenheit Buchung“ und ohne „Abwesenheit von“ (Homeoffice, Berechtigungen
  usw.), sonst verfehlten die Pflichtspalten den Fuellanteil; der Testabruf prueft die Fall-Datei auch bei `CaseEnabled = false`, sobald
  ein eigener Pfad gesetzt ist, und zeigt `Verteilung` (Zeilen und Tage je „Art | Status“, Datumsbereich) ohne Personenwerte.
- `df7b874` (deployed 14:36): Testabruf im Hintergrund, `/BiDashboard/admin/rexxhub/test?hintergrund=1` startet und holt das Ergebnis,
  `&neu=1` startet nach einem fertigen Lauf neu. Grund: der direkte Testabruf brach nach rund 2 Minuten mit **502** ab (Zeitlimit des
  Webservers; die Seiten kamen bis dahin alle mit 200, je ~2,2 s), und mit der Anfrage brach auch der Abruf ab.
- Tests 1'821/1'821 (Release-Worktree). Beide Deploys mit dem Werkzeug, Protokolle `.tmp_deploy_2026-11-23_rexx-requests.log` und
  `_rexx-hintergrund.log`, Commit-Zeile geprueft, Alarm nur `-wal`/`-shm` (wieder da). **Nicht in `feature/supplier-overrides-trit`
  gemergt** (wie `einkauf-tempo-2011`): ein Deploy vom Hauptzweig nimmt die Aenderungen zurueck.

**3. Testabruf im Hintergrund (14:40 und 14:46, je einmal):** #775 ok (200, 395 Zeilen, wuerde geschrieben). **#777: Rexx antwortet 500**,
request ids `req_7341f6cf6b82843630d70c4afe15c93e` (14:40) und `req_de972e1f38698e0d88d71c0fe284e250` (14:46). Laut „Rexx Hub Logs“
liefern die Seiten 1 bis 55 je 200, **Seite 56** (Antraege 5'501 bis 5'600, Sortierung `application_id` aufsteigend) bricht ab, auch ohne
Export Filter. Damit hat `/v1/requests` ueber 5'500 Antraege aller Workflows; der Handexport #777 vom 04.11. hat 2'683 Abwesenheiten.
Ein Vergleich mit #777 war deshalb noch nicht moeglich (Handexport-Summen 04.11. liegen bereit: z. B. Krankheit genommen 398 Zeilen /
954 Tage, Ferien genommen 614 / 1'710,5, Kompensation genommen 384 / 592, Unfall genommen 60 / 247,5).

**Kontingent:** zwei ueberlagerte direkte Testlaeufe um 13:4x (Fehler: zweiter Start, waehrend der erste lief) und zwei
Hintergrundlaeufe, zusammen rund 250 Anfragen von 50'000 im Monat.

**Offen, naechste Schritte:**
1. Artjom (Rexx) mit den beiden request ids: 500 bei `GET /v1/requests?page=56` (API-Benutzer #2), Bitte um Korrektur; dazu fragen, ob
   sich `/v1/requests` serverseitig auf Abwesenheiten eingrenzen laesst (Wert fuer den Export Filter, Workflow-Klasse).
2. Danach `?hintergrund=1&neu=1`, Verteilung gegen die Summen oben bzw. einen frischen Handexport #777 pruefen.
3. Passt es: `"CaseEnabled": true` in appsettings, Tests, Deploy. Achtung: der Tagesdienst schreibt dann sofort beim Start, weil die
   Fall-Datei vom 04.11. ist; der Handexport wird als `*.manuell.xlsx` gesichert (Abschnitt 14).


*(Die offenen Schritte 1 bis 3 oben sind am 2026-11-24 erledigt, siehe Abschnitt 22. Eine Meldung an Artjom war nicht noetig.)*

## 22. #777 laeuft: Ursache Seite 56 war das Exportfeld time_away; Stichtag 07.04.2026; eingeschaltet (2026-11-24)

Auftrag Ingo 24.11. („versuchen wir das nochmal ev zu fixen, rexx ist im chrome offen“, danach „mach alle vorschlaege“).

**1. Ursache eingegrenzt (Rexx produktiv, nur gelesen):** In den Rexx Hub Logs steht der Absturz als Zeile ohne Antwortcode und ohne
Antwortinhalt (Dauer 0.00); Rexx bricht intern ab. `/v1/requests` kennt nur den Parameter `page` (Parameter Mapping „Fest definiert“),
keine Seitengroesse und kein Datum; eingrenzen liesse er sich nur ueber Export Filter oder die Feldauswahl.

**2. Korrektur in Rexx (08:21, API-Benutzer #2, Endpunkt `/v1/requests`):** Exportfeld „Abwesenheit Zeit“ (`time_away`, Listenfeld)
entfernt, 21 -> 20 Felder, nach dem Speichern neu geladen und geprueft. Das Cockpit liest die Spalte nicht (`RexxHubColumns`, Leser).
Ruecknahme: „Felder auswaehlen“, Feld wieder hinzufuegen.

**3. Testabruf 08:25 (ohne Stichtag):** #777 **168 Seiten, 200**, 14'494 Abwesenheiten nach Entfernen von 2'213 Antraegen ohne
Abwesenheit, Pflichtspalten 100 %, Format und Werte erkannt, wuerde geschrieben, rund 6 Minuten. Bereich „Abwesenheit von“
03.01.2024 bis 27.12.2027. **Ursache bestaetigt: das Listenfeld `time_away` liess Rexx bei einzelnen Antraegen abstuerzen.**

**4. Stichtag (Code `5b3774f`, deployed 08:57 ohne Wochensicherung):** `HrKpi:RexxHub:CaseFromDate = 2026-04-07`; die Fall-Datei
behaelt nur Abwesenheiten, die an oder nach dem Stichtag enden („bis“, ersatzweise „von“). Grund: `MergeRexxCases` ergaenzt jeden
Rexx-Fall ohne SAP-Gegenstueck unabhaengig vom Datum; ohne Grenze haetten 2024/2025 den Vorjahresvergleich veraendert.

**5. Testabruf 08:58 (mit Stichtag):** 168 Seiten ok, **4'416 Abwesenheiten**, 10'078 vor dem Stichtag entfernt, wuerde geschrieben.
Vergleich mit dem Handexport #777 vom 04.11. (Zeilen / Tage, Status Genommen):

| Art | Handexport 04.11. | Hub 24.11. |
| --- | --- | --- |
| Krankheit | 398 / 954 | 546 / 1'314 |
| Krankheit ueber 60 Tage | 2 / 32 | 6 / 47 |
| Unfall | 60 / 247,5 | 66 / 249 |
| Ferien | 614 / 1'710,5 | 1'341 / 4'266,5 |
| Kompensation | 384 / 592 | 584 / 970,5 |
| Militaer / Zivildienst | 5 / 13 | 13 / 50 |

**Abweichung erklaert:** Der Handexport enthielt nur Antraege mit **Antragsdatum ab 04.05.2026** (gelesen: Antragsdatum 04.05. bis
04.11.2026); frueher beantragte Ferien fehlten (Ferien genommen im April 3, Mai 42). Der Hub liefert alle Antraege und ist damit
vollstaendiger; dazu kommen 20 Tage seit dem 04.11. Unfall passt fast genau (60/66 Faelle, 247,5/249 Tage).

**6. Eingeschaltet (Code `21d11b4`, deployed 09:10 ohne Wochensicherung):** `CaseEnabled = true`. Tests 1'823/1'823 (Worktree und
Release-Worktree). Beide Deploys mit dem Werkzeug, Protokolle `.tmp_deploy_2026-11-24_rexx-stichtag.log` und
`.tmp_deploy_2026-11-24_rexx-777-ein.log`, Commit-Zeile geprueft, Alarm nur `-wal`/`-shm` (wieder da). **Wochensicherung bleibt
faellig** (letzte 16.11.; Wunsch Ingo, beim ersten Deploy abgebrochen). Zweig `rexxhub-requests-2311` ist weiter **nicht** im
Hauptzweig.

**7. SFTP, Hinweis Alex Donato (an Ingo, 24.11.):** Die **Rexx-Terminals senden die Stempelungen selbst an den FTP-Server**. Das bleibt
so (Stempeluhrtransfer, Schnittstelle 3, Entscheid 10.11.). Fuer die Hub-Umstellung muss daran nichts geaendert werden. *Offen bleibt*
nur, wer die uebrigen Dateien auf dem SFTP-Server ablegt (Artjom nannte Upgreat, Abschnitt 20 Punkt 4) und ob Trafag acht oder
sieben Schnittstellen bezahlt; beides klaert Ingo mit Rexx.

**8. Erster Tageslauf brach zweimal ab, behoben mit Wiederholung je Seite (Code `142d304`, deployed 09:58 ohne Wochensicherung):**
Der Dienst holte #777 um 09:13 und 09:43; beide Laeufe brachen ab, weil Rexx eine einzelne Seite unbeantwortet liess (09:13 nach 5
Seiten, 09:48 nach 138 Seiten; im Rexx-Hub-Protokoll Zeile ohne Antwortcode, Dauer 0.00). Wahrscheinliche Ursache: Rexx erlaubt
nur eine Anfrage gleichzeitig ueber alle API-Benutzer (Datei-Lock, hoechstens 3 s Wartezeit) [V]. Der Abruf wiederholte bisher nur
429; jeder andere Fehler brach den ganzen Lauf von 168 Seiten ab, der naechste Versuch kam erst 30 Minuten spaeter (hoechstens 4
je Tag). **Neu:** Eine Seite wird bei 500/502/503/504, Verbindungsabbruch oder Zeitueberschreitung nach 10, 30 und 60 s erneut
angefragt (`RexxHubClient.ServerErrorWaits`). Tests 1'829/1'829 (Worktree und Release-Worktree). **Ergebnis:** Nach dem Neustart
hat der Dienst die Fall-Datei um **10:08** geschrieben, **4'417 Abwesenheiten**, 20 Spalten; der Handexport vom 04.11. liegt als
`Hr-cockpit-absenzen-je-fall.manuell.xlsx` daneben. Damit kommen **#775 und #777 taeglich ueber den Hub**. Das Cockpit selbst
(hinter dem HR-Login) ist nicht im Browser angesehen.
Nebenbei: Ein versehentlicher direkter Testabruf (Testseite ohne `?hintergrund=1`) lief 09:31 bis 09:33 ueber 36 Seiten und brach
mit dem Webserver-Timeout ab; geaendert hat er nichts.
