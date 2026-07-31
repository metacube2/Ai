# Last Change

Stand: 2026-07-31

Diese Datei ist fuer tokenarme RAG-Nutzung komprimiert.

## Aktueller Kurzstand

- DOKU 2026-07-31, RAG-Inhaltsverzeichnis konsolidiert: `docs/RAG_ROUTER.md`
  von 289 auf 51 Zeilen reduziert und auf Vorrangregeln/Themenrouting
  beschraenkt. Detailquellen, Live-Werkzeuge und Suchbegriffe stehen jetzt in
  `docs/RAG_DETAIL_INDEX.md`. Der vorherige Router ist vollstaendig und
  zeilengleich in
  `docs/raw_md_archive/RAG_ROUTER_ARCHIV_2026-07-31.md` erhalten.

- DEPLOYED 2026-07-31, finaler Stand der Spend-Matrix fuer
  Praesentations-Screenshots (Code-Commits `4a3271b`, `f740eb9`, `4498bd4`):
  dunkler Primaertext und deutlichere Ebenenhintergruende; Tabellenkopf,
  Lieferanten, Warengruppen und Materialien fett (`700`); Lieferanten und
  Warengruppen `1.05rem`, Materialien `1rem`. `346/346` Tests gruen.
  Produktive `BiDashboard.dll` `31.07.2026 11:43:06`, `3'226'624` Bytes,
  SHA256 `E64BF04327D3FD7668D424C0FA52EC78A00F076E9118E253D57601730F24A247`;
  Release und Server bitgleich, Produktiv-DB unveraendert, Port 443 offen,
  authentifizierter HTTPS-Aufruf `200`.

- VERSAND 2026-07-31, Stand bei Chatende (Detail: `docs/FINANCE_FELDLUECKEN_MAILS_2026-07-31.md`
  Abschnitt „Stand bei Chatende"): INDIEN IST RAUS - gesendet 09:56 an `RanVijay.Kumar@trafag.com`,
  Betreff von Ingo ergaenzt auf „... (Trafag India) -> Supplier Name", NICHT an `tasc.co.in`, damit
  ist die Fremddomain-Frage fuer diesen Versand erledigt. In Entwuerfen liegen sechs: DE, ES, UK, IT
  versandfertig, FR und US mit LEERER An-Zeile. ZWEITE KORREKTUR an der Italien-Mail: sie nannte
  TR IT „the best-performing site on supplier data" - FALSCH, TRUK hat 100 % gegen TRIT 71 %.
  Eingeschraenkt auf „of our SAP Business One sites" und der zweite Balken ersetzt, weil Artikel-
  und Zeilenebene bei IT beide auf 71 % fielen und zwei identische Balken wie ein Copy-Paste-Fehler
  aussahen; jetzt Standortvergleich UK/IT/IN/FR/US. MUSTER HINTER BEIDEN FEHLERN (UK-2025 und
  IT-Superlativ): eine Behauptung war aus aelterer Doku uebernommen statt gemessen. Vor jedem
  Mailversand die Zahlen gegen das Audit-CSV nachrechnen, auch die scheinbar harmlosen Nebensaetze.
  NICHT ANGEFASST: Ingos eigener Entwurf „Missing supplier information in sales export data"
  (30.07., 48'708 B) - ueberholte Sammelfassung mit der falschen UK-Aussage, Loeschkandidat.
- PRUEFUNG 2026-07-31, alle sieben Standort-Entwuerfe gegen `Finance_Dashboard_Audit_All_2026-07-29.csv`
  nachgemessen. SECHS stimmen, EINER war falsch: die UK-Mail behauptete „the UK data we hold starts
  in January 2026, so 2025 is absent". FALSCH - TRUK hat **1'867 Zeilen fuer 2025** und 1'082 fuer
  2026 bis 27.07., der UK-2025-Backfill ist gelaufen. Fehlerkette: die Spalte „weitere
  Auffaelligkeit" in `FINANCE_FELDLUECKEN_STANDORTE_2026-07-30.md` Abschnitt 1 ist die EINZIGE
  Spalte, die nicht aus dem Reproduktionsskript stammt - dort war „2025 fehlt komplett" aus der
  ueberholten 28.07.-Analyse uebernommen und nie nachgemessen. Der Entwurf mit der Falschaussage
  ist geloescht und ersetzt (`-Only TRUK` am Skript ergaenzt), fuer TRUK ist damit NICHTS offen.
  ZWEITER BEFUND, gegen eine Fremdauswertung die fuer TRUK `0` Lieferanten zeigte: es gibt in dieser
  Quelle KEINEN Fall „SupplierNumber gepflegt, SupplierName leer" - beide Felder sind immer
  gemeinsam gefuellt (TRUK 2'955/2'955, TRIT 13'925/13'925, TRIN 809/809, TRFR 134/134, TRUS 6/6,
  CH/AT/DE/ES 0/0). Jene Tabelle stimmt in JEDER anderen Zelle mit dem Audit-CSV ueberein, nur die
  UK-Zeile weicht ab und traegt noch den alten Statustext „Mapping jetzt da - braucht noch den
  Reimport": mutmasslich eine unveraendert uebernommene Zeile, keine Messung. Trafag-Erkennung
  gegengeprueft: TRFR 83 intern / 51 extern, TRIN 677/132, TRIT 6'848/7'077, TRUK 2'803/152,
  TRUS 2/4 - der Regex greift ueberall, wo ein Lieferant steht; das fehlende Feld ist der Engpass.
- WERKZEUG 2026-07-31, grafische Mailfassung: `docs/mails/Build-StandortMails.ps1` baut die sieben
  Standortmails mit Outlook-taugliche Grafiken (Balken Artikelstamm/Rechnungszeilen, Feld-Schema
  `Purchasing Data` -> `Preferred Vendor`, Statustabelle DE, Vorher/Nachher-Kasten zum RTF-Muell,
  Monatsstreifen ES 2026, Standort- und Jahresvergleich UK). `-Mode Preview` (Default) schreibt
  `.tmp_standort_mails/Vorschau_Standortmails.html`, `-Mode Draft` legt Outlook-Entwuerfe an
  (schreibt ins Postfach, sendet nichts). Ausgabeordner ist gitignored - enthaelt Empfaengeradressen.
  BEFUND, WICHTIG FUER KUENFTIGE VERSUCHE: `MailItem.SaveAs` ist auf diesem Arbeitsplatz GESPERRT -
  `.msg`, `.oft` und `.txt`, jeder Zielordner, immer `E_ABORT` (0x80004004). Endpoint-Security/DLP,
  kein Skriptfehler; `MailItem.Save()` in Entwuerfe geht. `Word.Application`-COM haengt ebenfalls,
  daher kein automatisches .docx - Vorschau-HTML von Hand in Word oeffnen. Zwei Darstellungsregeln:
  Artikelbalken mit EXAKTEN Stueckzahlen, Zeilenbalken NUR in Prozent (die Zeilenzahlen je Kategorie
  waeren aus gerundeten Prozenten abgeleitet und wuerden Scheingenauigkeit erzeugen); keine Bilder,
  nur Tabellen mit `bgcolor`, weil Outlook externe Bilder beim Empfaenger blockiert.
- DOKU 2026-07-31, versandfertige Einzelmails je Standort (kein Code):
  `docs/FINANCE_FELDLUECKEN_MAILS_2026-07-31.md`. Der Sammeltext aus
  `FINANCE_FELDLUECKEN_STANDORTE_2026-07-30.md` Abschnitt 6 ist in SIEBEN Einzelmails aufgeteilt,
  jede mit To/Subject/Anrede; Zahlen unveraendert (Messung 29.07.2026, 95'168 Zeilen). Aenderungen
  an Zahlen kuenftig ZUERST im Sammeltext, dann uebernehmen. UK-EMPFAENGER NACHGETRAGEN:
  `Cornell.Williams@trafag.com` (von Ingo geliefert) - damit sind 5 von 7 versandfertig.
  NOCH BLOCKIERT: TRFR und TRUS, es fehlt AUSSCHLIESSLICH die Adresse, die Mails stehen.
  Drei bewusste Textentscheide: (1) Italien-Mail beginnt mit der ausdruecklichen Abgrenzung
  „a separate topic from the inventory valuation discussion ... no deadline attached", sonst liest
  Paola sie als Erinnerung an ihre Zusage Ende August; Verweis auf B1-Upgrade 03.08. (2) Indien geht
  per To NUR an RanVijay, `tasc.co.in` bleibt Cc-Option nach Klaerung, und das Listenangebot ist auf
  „I can send you the list directly" umformuliert, damit die 1'271 Artikelnummern nicht in einen
  Fremddomain-Cc laufen. (3) Spanien-Mail zieht den Range-Export von Punkt 3 auf Punkt 1 vor (das ist
  die sichtbare 2026-Luecke) und verweist auf den bereits dokumentierten Befehl in
  `FINANCE_BACKFILL_UK_ES_2026-07-28.md`. UK-Mail ohne den „was wir nicht brauchen"-Vorspann, weil
  reine Rueckfrage nach 2025. Am Dateiende eine Versandtabelle - ohne die ist spaeter nicht
  unterscheidbar, ob ein Standort nicht geantwortet oder nie eine Mail bekommen hat.
- DOKU 2026-07-31, zentrales Ansprechpartner-Register angelegt (kein Code): `docs/ANSPRECHPARTNER.md`,
  im `RAG_ROUTER.md` als eigenes Thema und ueber Suchwoerter verlinkt. Anlass: Kontaktangaben lagen
  in DREI Dokumenten verstreut - Rollen in der Ticket-Beteiligtenliste `saptasks/zzprdat-kontext.md:189`,
  Domaenenzustaendigkeiten in `docs/INGO_TODOS_180_TAGE_2026-06-18.md:23-25`/`:159-166`, Adressen nur
  in `docs/FINANCE_FELDLUECKEN_STANDORTE_2026-07-30.md:137`. Beim BLP-Vorfall 2026-07-30 war dadurch
  klar WER zustaendig ist, aber nicht wie er zu erreichen ist. Inhalt: Standortempfaenger (Abschnitt 1),
  interne Rollen (2), externe Partner (3), Eskalationspfad plus Paola-Doppelvorgang und
  `tasc.co.in`-Einschraenkung (4), Verwechslungsgefahren (5), offene Luecken (6).
  REGEL DER DATEI: keine aus Namensmustern abgeleiteten Adressen - was nicht belegt ist, steht als
  `offen`. GROESSTE LUECKE: fuer KEINEN internen Ansprechpartner (Lucas, Adil, Fabio, Andreas, Sonja,
  Alex, Ramon) ist eine Adresse dokumentiert, und Andreas/Sonja/Alex/Ramon haben nicht einmal einen
  belegten Nachnamen. Neu festgehaltene Verwechslungsgefahr: ZWEI Marcos - Marco Di Menco (Etiketten/PP)
  vs. Marco Widmer (Einkauf, Autor `PURCHASING_DASHBOARD_REVIEW_MARCO_2026-07-10.md`).
- DOKU 2026-07-30, Empfaenger fuer die Standort-Mail (kein Code): `docs/FINANCE_FELDLUECKEN_STANDORTE_2026-07-30.md`
  hat einen neuen Abschnitt 5 `Empfaenger` (alter Abschnitt 5 Mailtext -> 6, Reproduzierbar -> 7).
  Anlass: In KEINEM `.md` des Repos stand vorher eine Mailadresse (Regex ueber alle Markdown-Dateien:
  null Treffer) - benannt war als einziger Standortkontakt Paola (TR IT), und zwar nur fuer das
  Bewertungsthema. Von Ingo recherchiert und jetzt festgehalten: ES `Santi.Gomez@trafag.es`,
  DE `Rohail.Munir@trafag.de`, IT `Paola.Castagna@trafag.com`, IN `RanVijay.Kumar@trafag.com` plus
  `agupta@tasc.co.in` und `chandra.s@tasc.co.in`. DREI PUNKTE, die beim Versand zaehlen:
  (1) **FR und US fehlen weiterhin** - genau die zwei Standorte mit einer echten Bitte im Text
  (FR 374 von 433, US 518 von 521 Artikeln ohne `OITM.CardCode`), FR ist sogar der kleinste Aufwand
  aller Standorte. (2) **Paola laeuft doppelt**: sie ist bereits Adressatin des Standardkosten-/
  Bewertungsthemas mit Zusage Ende August (B1-Go-Live 2026-08-03 + 2 Wochen Ferien) - die Bitte hier
  ist Stammdatenpflege und ein anderer Vorgang; getrennt verschicken, sonst kostet es den klaren
  Termin des Bewertungsthemas. Querverweis dazu jetzt auch in
  `docs/FINANCE_STANDARDKOSTEN_SITZUNG_ANDREAS_2026-07-27.md` Abschnitt 5d. (3) **`tasc.co.in` ist
  keine Trafag-Domain** - zwei der drei indischen Adressen liegen bei einem externen Dienstleister.
  Der Mailtext selbst ist unkritisch, aber er enthaelt das Angebot "we can send the item list"
  (1'271 Artikelnummern des TRIN-Stamms); vor dem Versand einer solchen Liste an eine Fremddomain
  klaeren, ob das gedeckt ist, im Zweifel nur an die Trafag-Adresse und von dort weiterverteilen.
  Nicht verwechseln: `Hugo Cuesta` in Abschnitt 3 ist ein Artikeltext aus den Spanien-Daten
  (`- Entregado por Hugo Cuesta`, 377 Zeilen), kein Ansprechpartner.
- GEFIXT 2026-07-30 (Entscheid Ingo, Variante B): Das naechtliche Einkauf-Delta haengt nicht mehr an
  `Sites.IsActive`, sondern nur noch daran, DASS die Site `PURCHASING_SAP` konfiguriert ist. Damit
  bleibt `IsActive = 0` und der Sales-Export unveraendert - die Variante mit dem Ausfiltern in
  `ExportAllAsync` wurde bewusst NICHT genommen, weil sie die Strecke anfasst, die Andreas'
  Finanzzahlen fuettert. Zusaetzlich wird das Ueberspringen jetzt als `Warning` geloggt: der stille
  Aussteiger war der eigentliche Grund, warum der Ausfall sechs Tage unentdeckt blieb. Fehlende
  Zugangsdaten meldet `RunDeltaAsync` selbst als `Error`-Status, statt vorab geprueft zu werden -
  dann ist die Ursache im Refresh-Status sichtbar statt unsichtbar. NACHSORGE: Delta-Button im
  Einkaufs-Dashboard einmal druecken, damit nicht bis zum Nachtlauf gewartet werden muss; danach
  muss in `PurchasingSyncState` ein `Delta`-Eintrag stehen und die Meldung die Zahl der
  nachklassifizierten Cachezeilen nennen.
## Offene Punkte aus aelteren Eintraegen (Original im Archiv)

- Server/IIS (seit 2026-07-08, nur direkt am Server moeglich, WinRM gesperrt): App-Pool `startMode=AlwaysRunning` + `processModel.idleTimeout=00:00:00` setzen, damit der 12:00-Timer ohne vorherigen HTTP-Request laeuft. Bis dahin holt `CatchUpMissedRunAsync` verpasste Tageslaeufe beim naechsten Prozessstart nach.
- Betriebshinweis DE/Alphaplan (seit 2026-07-03): Der Alphaplan-Upload nach SharePoint muss VOR dem 12:00-Timer laufen, sonst verwendet der Tagesexport noch den vorherigen ZIP-Stand.

## Aeltere Eintraege / Historie

- Kurzstand-Eintraege 2026-06-04 bis 2026-07-08 und alle Nachtrag-Abschnitte (Mai/Juni 2026): verbatim in `docs/raw_md_archive/LASTCHANGE_ARCHIV_bis_2026-07-12.md`.
- Kurzstand-Eintraege 2026-07-13 bis 2026-07-30: verbatim in `docs/raw_md_archive/LASTCHANGE_ARCHIV_2026-07-13_bis_2026-07-30.md`.
- Kanonische Detailhistorie davor: `docs/raw_md_archive/HISTORY_CANONICAL.md.raw`; Original-Volltexte: `docs/raw_md_archive/original_history_raws.zip` (nur zur Wiederherstellung).

## Einstieg / Router

- Themenrouter (zuerst laden): `docs/RAG_ROUTER.md`.
- Fuehrender Kurzkontext: `docs/rag/PROJECT.md`.
- Naechster Chat: `docs/RAG_ROUTER.md` -> diese Datei -> passende Themen-Kurzdatei aus `docs/rag/`.
