# Ansprechpartner

Stand: 2026-07-31

Zweck: **eine** Stelle, an der steht, wer für was zuständig ist und wie man ihn erreicht. Vorher
lagen diese Angaben in drei Dokumenten verstreut (Rollen in einer Ticket-Beteiligtenliste,
Zuständigkeiten in der 180-Tage-Roadmap, Adressen nur in der Standort-Feldlückendoku) — im
Eskalationsfall, konkret beim BLP-Vorfall am 2026-07-30, war zwar klar *wer* zuständig ist, aber
nicht, wie er zu erreichen ist.

Zwei Regeln für diese Datei:

- **Nur bestätigte Angaben.** Adressen werden hier nicht aus einem Namensmuster abgeleitet.
  Was nicht belegt ist, steht als *offen* — eine geratene Adresse ist schlimmer als eine leere
  Zelle, weil sie ungeprüft weiterverwendet wird.
- **Jede Zeile nennt ihre Quelle.** Damit bleibt prüfbar, woher eine Zuordnung kommt und wie alt
  sie ist.

Diese Datei enthält personenbezogene Daten (Namen und Mailadressen von Mitarbeitenden und einem
externen Dienstleister). Sie liegt bewusst im Repo, weil das Repo die Arbeitsdokumentation ist —
aber sie gehört nicht in Exporte, Artefakte oder Weitergaben an Dritte.

## 1. Standorte / Tochtergesellschaften

Das ist die Liste aus der Diskussion vom 2026-07-30 (Feldlücken je Standort, Bitte um
Stammdatenpflege). Vollständiger fachlicher Kontext samt Zeilenzahlen und Mailtext:
`docs/FINANCE_FELDLUECKEN_STANDORTE_2026-07-30.md` Abschnitte 5 und 6.

| TSC | Standort | Person | Adresse | Thema | Status |
| --- | --- | --- | --- | --- | --- |
| `TRES` | Spanien | Santi Gomez | `Santi.Gomez@trafag.es` | Feldlücken; ausserdem Sage-Range-Export für fehlende Zeiträume | bestätigt |
| `TRDE` | Deutschland | Rohail Munir | `Rohail.Munir@trafag.de` | Feldlücken / Alphaplan | bestätigt, aber ob Rohail selbst der Alphaplan-/BI-Kontakt ist, ist ungeprüft |
| `TRIT` | Italien | Paola Castagna | `Paola.Castagna@trafag.com` | **zwei getrennte Vorgänge**, siehe Abschnitt 4 | bestätigt |
| `TRIN` | Indien | RanVijay Kumar | `RanVijay.Kumar@trafag.com` | Feldlücken, primärer und Trafag-interner Adressat | bestätigt |
| `TRIN` | Indien | Anurag Gupta | `agupta@tasc.co.in` | Feldlücken, **externe Domain** | bestätigt, Versand eingeschränkt (Abschnitt 4) |
| `TRIN` | Indien | Chandra Pratap Singh | `chandra.s@tasc.co.in` | Feldlücken, **externe Domain** | bestätigt, Versand eingeschränkt (Abschnitt 4) |
| `TRFR` | Frankreich | *offen* | *offen* | Feldlücken: 374 von 433 Artikeln ohne Preferred Vendor | **fehlt** — kleinster Aufwand aller Standorte, liegt nur am Empfänger |
| `TRUS` | USA | *offen* | *offen* | Feldlücken: 518 von 521 Artikeln | **fehlt** |
| `TRUK` | UK | *offen* | *offen* | nur „no action required" | fehlt, Versand optional |
| `TRCH` / `TRAT` | Schweiz / Österreich | — | — | keine Standortbitte, läuft über SAP und das Schweizer Team | entfällt |

Quelle der bestätigten Adressen: von Ingo am 2026-07-30 recherchiert und in
`docs/FINANCE_FELDLUECKEN_STANDORTE_2026-07-30.md:137` ff. festgehalten. Vorher enthielt kein
Dokument dieses Repos irgendeine Mailadresse.

Zu Italien gehört ein zweiter, übergeordneter Absender: nach Paolas Antwort kam am 2026-07-28
eine Mail von vorgesetzter Stelle mit der Bitte, die Moving-Average-Umstellung erst ab 2027 zu
starten (Bezug auf VARONE als B1-Partner, Paolas Arbeitslast, Area Sales Manager). **Name und
Adresse dieser Person sind nicht dokumentiert** — falls die Antwort dorthin geht, hier ergänzen.
Kontext: `lastchange.md`, Eintrag „ESKALATION AUS ITALIEN 2026-07-28".

## 2. Trafag Schweiz — intern

Namen und Rollen sind belegt, **Adressen fehlen durchweg**. Genau diese Lücke hat beim
BLP-Vorfall gestört.

| Person | Rolle | Domäne | Adresse |
| --- | --- | --- | --- |
| Lucas Castro | Senior Application Manager | SAP-Objekte, Z-Funktionen, S/4-Themen, SAP-B1-Applikationsprojekt, Auftraggeber ZZPRDAT | *offen* |
| Adil Lahrach | PP/VC-Seite | Produktionsplanung, Variantenkonfiguration, Kopierprogramm; Arbeitsplan-/Vorgabewert-Fragen | *offen* |
| Fabio Palma | Head of Supply Chain Ops | Dispo, operative Supply Chain; Melder des BLP-Ausfalls 2026-07-30 | *offen* |
| Marco Di Menco | Business Owner Etiketten | Fachseite Etikettendruck, liefert Testdaten | *offen* |
| Marco Widmer | Einkauf | Fachliche Abnahme Einkaufsdashboard, Review 2026-07-10 | *offen* |
| Florian Wächter | Change Request / Anforderung | formale Anforderungsseite SAP | *offen* |
| Andreas | Finance | Budget- und Margenlogik, Nachweise, Gruppenreporting, Review der Länderformeln | *offen*, **Nachname nicht dokumentiert** |
| Sonja | HR | HR-Fachabnahme, Phase-2-Priorisierung HR-Dashboard | *offen*, **Nachname nicht dokumentiert** |
| Alex | Infrastruktur | Netzwerk, Server, Security | *offen*, **Nachname nicht dokumentiert** |
| Ramon | Infrastruktur | Netzwerk, Server, Security | *offen*, **Nachname nicht dokumentiert** |
| Ingo Kohler | IT Analytics Lead | Analytics, BI, Reporting-/Z-Funktionsbezug, .NET/ASP-Web | `ingo.kohler@trafag.com` |

Quellen: `saptasks/zzprdat-kontext.md:189` ff. („## 11. Beteiligte", Rollen von Lucas, Marco
Di Menco, Adil, Florian, Fabio), `docs/INGO_TODOS_180_TAGE_2026-06-18.md:23-25` und `:159-166`
(Domänenzuständigkeiten, Andreas, Sonja, Alex, Ramon),
`docs/PURCHASING_DASHBOARD_REVIEW_MARCO_2026-07-10.md:3` (Marco Widmer).

## 3. Externe Partner

| Partei | Rolle | Kontakt |
| --- | --- | --- |
| Upgreat | Server, Zugriff, Netzwerk, SharePoint, Betrieb, Security-Rahmen; auch SAP-B1-Server | *offen* |
| NTT | SAP-B1-Server-/Applikationsprojekt | *offen* |
| ANG | SAP-B1-Server-/Applikationsprojekt | *offen* |
| VARONE | B1-Partner Trafag Italia (Moving-Average-Umstellung) | *offen* |
| TASC (`tasc.co.in`) | Dienstleister Indien, siehe Abschnitt 1 | Anurag Gupta, Chandra Pratap Singh |
| Georg Wagner (meey.ch) | externer Berater Altlösung Etiketten, steht für Prüfung bereit | *offen* |

## 4. Eskalationspfad und Abgrenzung

Aus der eigenen 180-Tage-Abgrenzung (`docs/INGO_TODOS_180_TAGE_2026-06-18.md:23-25`), damit im
Ernstfall nicht improvisiert werden muss:

| Thema | Zuständig | Nicht Ingo |
| --- | --- | --- |
| SAP-Applikationen, S/4-Themen, RFC-Abschaltungen, ca. 30 betroffene Applikationen | Lucas | ja |
| SAP Business One Server und Applikation | Lucas, Upgreat, NTT, ANG | ja |
| Netzwerk, Server, Security, Infrastruktur | Alex, Ramon, Upgreat | ja |
| ABAP-Coding fremder Applikationen (z. B. BLP) | Applikationsowner, im Zweifel Lucas | ja |
| Analytics, BI, Reporting, Z-Funktions-Relevanz, .NET/ASP-Web | Ingo | — |
| Fachliche Reporting-Auswirkung einer SAP-Änderung | Ingo liefert Impact und Testnachweis, Umsetzung bleibt bei Lucas | — |

**BLP hat keinen dokumentierten Owner.** Der Vorfall vom 2026-07-30 (ACM-Dump, `SY-UNAME`
überschrieben, SAP Note 2864552) wurde an Ingo gemeldet, weil Lucas nicht erreichbar und Adil in
den Ferien war. BLP ist im gesamten Repo nicht beschrieben, und wer die Applikation fachlich und
technisch besitzt, ist offen. **Sobald das geklärt ist, hier eintragen** — sonst landet der
nächste Ausfall wieder beim erstbesten Erreichbaren.

**Paola Castagna trägt zwei unabhängige Anliegen.** Nicht in einer Mail mischen:

1. Bewertungsmethode / Standardkosten, Zusage **Ende August 2026** —
   `docs/FINANCE_STANDARDKOSTEN_SITZUNG_ANDREAS_2026-07-27.md` Abschnitte 5c/5d.
2. Stammdatenpflege *Preferred Vendor* (`OITM.CardCode`), 939 von 3'280 TR-IT-Artikeln —
   `docs/FINANCE_FELDLUECKEN_STANDORTE_2026-07-30.md`.

Eine gemischte Mail gefährdet den klaren Termin von Punkt 1. Zeitlich zusätzlich: B1-Upgrade
Go-Live 2026-08-03, danach zwei Wochen Ferien.

**`tasc.co.in` ist keine Trafag-Domain.** Der Mailtext für die Standorte enthält das Angebot
„we can send the item list" — das sind **1'271 Artikelnummern** des TRIN-Stamms. Vor dem Versand
einer solchen Liste an die Fremddomain klären, ob das gedeckt ist; im Zweifel nur an
`RanVijay.Kumar@trafag.com` und von dort intern weiterverteilen lassen. **Offene Entscheidung.**

## 5. Verwechslungsgefahren

Alle drei sind in diesem Repo schon einmal echte Stolpersteine gewesen:

- **Zwei Marcos.** *Marco Di Menco* ist Fachseite Etiketten/PP (`saptasks/zzprdat-kontext.md`),
  *Marco Widmer* ist Einkauf und Autor des Dashboard-Reviews
  (`docs/PURCHASING_DASHBOARD_REVIEW_MARCO_2026-07-10.md`). Dateinamen mit „MARCO" beziehen sich
  auf Widmer.
- **`Hugo Cuesta` ist kein Ansprechpartner.** Der Name stammt aus einem spanischen Artikeltext
  der TRES-Daten (`- Entregado por Hugo Cuesta`, 377 Zeilen,
  `docs/FINANCE_FELDLUECKEN_STANDORTE_2026-07-30.md:81`).
- **Beteiligtenlisten in Ticketdokus sind nicht der Verteiler.** Die Tabelle in
  `saptasks/zzprdat-kontext.md` gilt für den ZZPRDAT-/Etikettenvorgang. Dass sie zufällig die
  Namen aus dem BLP-Fall enthält, ist kein Beleg für Zuständigkeit dort.

## 6. Offene Punkte dieser Datei

1. **Adressen der internen Ansprechpartner** — Lucas, Adil, Fabio, Andreas, Sonja, Alex, Ramon.
   Das ist die praktisch wichtigste Lücke.
2. **Nachnamen** von Andreas, Sonja, Alex, Ramon.
3. **Empfänger TRFR und TRUS** — beide haben eine echte, offene Bitte im Standort-Mailtext.
4. **Owner von BLP** (Abschnitt 4).
5. **Absender der italienischen Eskalation vom 2026-07-28** (Abschnitt 1).
6. **Kontakte bei Upgreat, NTT, ANG, VARONE** für den Eskalationsweg.
