# Abschlussrunde Trafag Dashboard und naechster Schritt Data Lake, 2026-11-03

Stand: 2026-11-03. Teams-Sitzung 08:30 bis 10:00, eingeladen am 2026-10-20 (Agenda in `lastchange.md` 2026-10-20).
Quelle: Whisper-Transkript (large-v3) von Ingo, `OneDrive - Trafag AG\Desktop\abschluss\Data\audio.wav`. Das Transkript
hat Hoerfehler (z. B. „Arteum“ = Artjom Yahno, „Set-Objekte“ = Z-Objekte, „Rezeptifikat“ = Zertifikat, „Dingo“ = Ingo);
hier sinngemaess zusammengefasst, keine woertlichen Zusagen ausser wo als Zitat markiert.

**Teilnehmende:** Ingo Kohler (Leitung), Oscar Schwark, Armin Guentensperger (Einkauf), Fabio Palma (Operations/Supply
Chain), Dragan Radanovic (Produktion, neu dabei), Nadja Brandenberger und Sonja Richter (HR), Andreas Stoller (Finance).
Lucas Castro nicht dabei. Verkauf nicht vertreten.

## 1. Rueckmeldungen je Bereich

**Einkauf (Armin):**
- Aufbau gut (Kennzahlen oben: Einkaufsvolumen, Bestaende); man beginnt aktiv damit zu arbeiten, Armin ist mehrmals taeglich drauf.
- **Datenqualitaet noch nicht abgeschlossen**; Wunsch nach Ressourcen, um sie fertig zu machen. Ursachen teils Stammdaten
  (Beispiel: eigener Disponent fuer Erstserien bei Druckwaechtern, dadurch nicht von konventionellen unterscheidbar), teils
  Logik der Datenverknuepfung.
- **Materialklassifizierung in SAP umgesetzt**: Fertigproduktgruppen, Materialanteile und Lieferanten in den Materialanteilen
  sind jetzt auswertbar. Budget 2027 wurde noch von Hand gemacht, kuenftig aus dem Dashboard.
- **Wunsch:** neben dem Nettoumsatz auch die **Herstellkosten (HK) bzw. den Standardpreis des Fertigprodukts** und den
  Materialanteil sehen („wo die Musik spielt“), um Preisaenderungen auf das Fertigprodukt durchzurechnen.
- **Streamlining:** „wir haben es ein wenig ueberschossen“; Dinge, die nicht gebraucht werden oder nicht vom Einkauf kamen,
  bereinigen, damit das Wichtige drin ist und niemand von der Arbeit abgehalten wird. Credo: Nutzen generieren, dann bleibt es am Leben.
- **Lieferantenmanagement** (Projekt, am Freitag an Ingo geschickt): Lieferantenbewertung in SAP ist sehr rudimentaer, ISO
  fordert Lieferantenentwicklung (ablaufende Zertifikate, Zielvereinbarungen). Der SAP-Berater riet ab, das in SAP zu bauen.
- **Entscheid Oscar/Armin:** Keine Logik im Dashboard. Bewertung und Qualitaetsdaten bleiben in SAP und werden dort genutzt;
  das Dashboard stellt dar.
- Ein Einkaufs-Alert-Cockpit (Idee an Armin geschickt) ist noch nicht angeschaut; „Schritt fuer Schritt“.

**Operations (Fabio):**
- OTD/OTIF/OTP live aus SAP, seit heute mit Filter nach Kunden. Shopfloor-Dashboard (heute Excel) in Arbeit, es fehlen noch
  SAP-Daten; vieles kommt heute aus Z-Transaktionen, Ziel sind Standardquellen.
- **Wunsch Master Data:** eine Seite mit Stammdatenfehlern (z. B. falscher Disponent, falsche Felder), die das Team einmal
  taeglich oder periodisch abarbeitet und in SAP korrigiert, statt Reports zu ziehen. Noch nicht begonnen; Fabio wuerde es
  selbst entwickeln, Ingo bindet es ein.

**Oscar zu allen:**
- Wird eine Z-Transaktion bzw. ein Z-Objekt durch das Dashboard ueberfluessig, **das Z-Objekt loeschen**.
- **Laufend festhalten, wo schlechte Stammdaten gefunden wurden**, sichtbar machen, dass kontinuierlich bereinigt wird.
- Archivierung alter Daten (Belege seit 2002): auf Oscars und Lucas' Pendenzenliste, ausserhalb des Dashboards.

**HR (Nadja, Sonja):**
- Ziel: alle HR-Kennzahlen auf einen Blick (Headcount, FTE, Fluktuation, Unfall, Krankheit, Ferien, Gleitzeit), statt Excel
  mit Formeln (z. B. Listen fuer Fit-und-Wohl-Gespraeche und Ferientage an die Teamleiter); kuenftig per Klick je Abteilung und Zeitraum.
- **Problem: Live-Daten aus Rexx.** Unklar, wer die Schnittstelle baut. Ansprechpartner bei Rexx ist seit rund einem Jahr
  **Artjom Yahno**, auch fuer Schnittstellen zustaendig. Einzelschnittstelle rund 1'400, Rexx Hub rund 5'000 pro Jahr fuer
  beliebig viele. Laut Nadja hat Alex den Hub nicht empfohlen (Grund unbekannt); seither haengt der Entscheid.
- **Vereinbart:** Ingo setzt eine Sitzung mit Nadja, Alex und Artjom auf (Entwurf am 03.11. vorbereitet, Ingo bearbeitet ihn).
- Naechster Schritt HR: Live-Daten, dann testen.

**Finance (Andreas):**
- Sales auf Gruppenstufe und Standardkosten: die letzten vier Wochen nicht angeschaut (andere Prioritaeten). Aus Ingos
  Sicht erledigt, es fehlt Test und Nutzung durch Finance. Ziel: erstmals Transparenz ueber Margen auf Gruppen- und Artikelstufe.
- Data Lake ist aus Andreas' Bedarf entstanden (zehn ERP-Systeme, Excel waere „der Tod“).

**Bestaende konzernweit (Fabio, Andreas, Oscar):**
- Heute nicht sichtbar. Ingo kann Bestaende aus Alphaplan, Sage und B1 holen, braucht aber Zugriff und Mithilfe der Verantwortlichen
  vor Ort; Excel-Laender muessten automatisch angebunden werden („es muss alles automatisch sein“).
- Offen: Zugriff externer Verkaufsorganisationen auf das Dashboard; Entscheid mit Heiko bzw. Patrick (Verkauf).
- Vorschlag Andreas: zuerst Produktionstoechter (Tschechien, Indien), danach Vertriebstoechter; fuer Andreas Thema Anfang 2027.
- Wunsch fuer spaeter: globale HR-Kennzahlen (schwierig, unterschiedliche Systeme und Treuhaender).

**Produktion (Dragan):**
- Erstmals gestern mit Fabio angeschaut (Shopfloor). Liefertreue, Produktivitaet, Qualitaet, First Pass Yield, Ausschuss
  direkt aus SAP waeren „sensationell“; Teamleiter und Teamsprecher verbringen heute Stunden mit Strichlisten und Excel.
- Nach der Sitzung: Dragan hat den Link zu Logistik live nicht erhalten; **Ingo schickt ihn nochmals**.

## 2. Power BI und Smartsheet

- **Oscar:** zuerst ein **Inventar**: welche Power-BI-Berichte gibt es und wofuer (QM und Verkauf haben viele), dasselbe fuer
  Smartsheet. Alles, was aus Smartsheet ins Dashboard kommen kann, ist willkommen; Smartsheet wird 2027 angeschaut.
- **Vereinbart:** Ingo stoesst das Inventar an; Hinweise ueber Instanzen und Lizenzen, mit Alex. Vorarbeit:
  `projektmanagement/PM-14_Inventar_Abloesung.tsv`.

## 3. Datenverantwortung (Datenherr je Bereich)

Oscar: es braucht je Datentopf eine klar verantwortliche Person („nicht alle halb schwanger“).

| Daten | Verantwortlich laut Sitzung | Bemerkung |
| --- | --- | --- |
| HR (Rexx und SAP-HR) | HR (Nadja, Sonja) | klar |
| Produktionsdaten | **Dragan Radanovic** | in der Produktion keine Key User; Daten nicht den Teamleitern neben dem Tagesgeschaeft aufbuerden |
| Logistikdaten (WM) | **separat**, in der Sitzung offen; *Nachtrag 03.11. (Ingo): Neva Acikalin* | Dragan will klare Trennung Produktion/Logistik |
| Materialstamm | Fabios Bereich (Team Stefan); *Nachtrag 03.11. (Ingo): Armin Guentensperger* | Name Fabio oder Teamleitung, „irrelevant“ |
| Debitorenstamm | Fabios Bereich (Verkaufsadministration) | dito |
| Lieferanten- und Einkaufsdaten | **Marco Widmer** | SAP-Key-User MM |
| Finance | nicht besprochen; naheliegend Andreas Stoller | |

Fehlende Key-User-Funktion in der Organisation als Thema fuer das Berechtigungs- bzw. Beratungskonzept.

## 4. Abschluss und Auftraege

- Ziel: in den naechsten Monaten abschliessen, weiterarbeiten wie bisher.
- **Oscar will Transparenz ueber das Offene:** eine **Taskliste aller offenen Wuensche mit grob geschaetztem Aufwand**, die
  **Datenqualitaets-Befunde**, die **Datenverantwortlichen/Key User** mit Namen und einen Bereich im Dashboard, der zeigt, wo
  inkonsistente Daten gefunden wurden.
- **Ingo:** legt das in **Trafag Projekte** im Dashboard ab („da hat man sogar schon ein Projektmanagement“), grafisch und
  visuell. *Hinweis: Am selben Tag war PM-14 auf Ingos Wahl im Ordner `projektmanagement` angelegt worden; die Listen dort sind
  die Vorlage fuer das Projekt in Trafag Projekte.*
- Oscar zum Schluss: Nutzung in der Produktion foerdern (Strichlisten und Excel abloesen); Datenqualitaet; weitere Nutzniesser
  gewinnen; Berechtigungen (wer darf was) kommen ins Konzept.
- Stimmung: Andreas und Dragan beeindruckt vom Tempo; Dragan wusste vorab wenig und wuenscht kuenftig ein kurzes Briefing.
  „Jedes Excel und jedes Power BI, das wir abgestellt haben, feiern; jedes Z-Objekt zweimal.“

## 5. Aufgaben aus der Sitzung

| Nr | Aufgabe | Wer | Wochen_Todo |
| --- | --- | --- | --- |
| 1 | Link Logistik live an Dragan senden | Ingo | 137 |
| 2 | Sitzung Rexx Hub mit Nadja, Alex, Artjom | Ingo | 55 |
| 3 | Inventar Power BI und Smartsheet (Lizenzen/Instanzen mit Alex) | Ingo, Alex | 138 |
| 4 | Projekt in Trafag Projekte: offene Wuensche mit Aufwand, Datenqualitaet, Datenverantwortliche/Key User, Bereich „inkonsistente Daten“ | Ingo | 139 |
| 5 | Einkauf: HK/Standardpreis des Fertigprodukts zum Materialanteil; nicht genutzte Seiten mit Armin bereinigen (Nutzungsmessung als Grundlage) | Ingo, Armin | 140 |
| 6 | Einkauf: Lieferantenmanagement (Armins Projekt vom Freitag), Logik in SAP, Dashboard zeigt an | Armin, Ingo | 141 |
| 7 | Stammdatenfehler-Seite fuer Master Data; abgeloeste Z-Objekte loeschen | Fabio, Ingo | 142 |
| 8 | Bestaende konzernweit: Entscheid Verkauf (Heiko/Patrick), zuerst Produktionstoechter CZ/IN | Ingo, Andreas | 143 |
| 9 | Finance: Sales Gruppenstufe und Standardkosten testen und abnehmen | Andreas | 144 |
| 10 | HR: Live-Daten, danach Test durch HR | Ingo, Sonja | 55, 133 |

## 6. Nachtrag: Aufgabenliste des Chefs (03.11., von Ingo weitergegeben)

| Punkt | Stand | Wochen_Todo |
| --- | --- | --- |
| Dataowner-Dashboard | neu | 145 |
| Projektmanagement deaktivieren | Bedeutung unklar, Rueckfrage an den Chef; nichts geaendert (dort liegt PM14) | 146 |
| Datenqualitaet: Befunde, wer bereinigt was, Wuensche zur Datenbereinigung als Taskliste | erfasst in PM14 (Trafag Projekte) | 139 |
| Wo sind welche Power BI und Smartsheets; abgeloeste Excel zaehlen, eingesparte Stunden | Inventar mit Alex; Stunden noch zu schaetzen | 138 |
| Berechtigungskonzept: Abgleich mit dem AD, wer darf was | neu | 147 |

**Nachtrag 2026-11-03 nachmittags, Dataowner-Dashboard (Wochen_Todo 145):** neue Seite **Datenverantwortung**
(`/datenverantwortung`, Menuepunkt auf oberster Ebene nach Trafag Projekte). Je Datentopf: System/Quelle, Verantwortlich,
Stellvertretung, Key User, Status vorlaeufig/bestaetigt, Bemerkung, geaendert am/von. Lesen fuer alle, Bearbeiten nach
Admin-Anmeldung (`IAdminAccessService`). Tabelle `DataOwners`, beim ersten Anlegen gefuellt mit der vorlaeufigen Belegung
aus Abschnitt 3 (Produktion, Logistik offen, Materialstamm, Debitoren, Lieferanten/Einkauf, HR, Finance zu bestaetigen);
spaeter geloeschte Eintraege kommen nicht zurueck. Code `c267651` (`Components/Pages/DataOwners.razor`,
`Services/DataOwnerRules.cs`, `Models/DataOwner.cs`, Test `DataOwnerTests`), Release-Worktree 1360/1360.

**Nachtrag 2026-11-03 (Ingo):** Logistikdaten Neva Acikalin, Materialstamm Armin Guentensperger; so in der Startbelegung der Seite.
