# Sitzung SAP PS und Projektmanagement-Werkzeug (2026-10-29)

Stand: 2026-10-29. Quelle: Aufnahme `Neue Aufnahme 26.m4a`, mit Whisper `large-v3` transkribiert, Zusammenfassung durch
Claude, von Ingo angefordert („für zukünftige Tasks“). Das Transkript hat Hörfehler: „PES-Modul“ = SAP PS, „Entity“ =
vermutlich NTT Data, „Trafalgar“ = Trafag, „Andrea Stoller“ = vermutlich Andreas Stoller. Wer „Oskar“ ist, geht aus dem
Transkript nicht sicher hervor (argumentiert kaufmännisch). Bezug: Vorhaben **PM-13** (SAP PS, Demo vom 2026-10-20,
`docs/SAP_PS_EINFUEHRUNG_DEMO_2026-10-20.md`) und **PM-10** (Ablösung Smartsheet).

## Teilnehmende

Philip Steiger (Entwicklung, testet Project Power Pack), Oskar (kaufmännische Sicht), Lucas Castro (SAP), Ingo; eine Person
online zugeschaltet.

## Ausgangslage

Andreas Stoller will Zeitrückmeldungen und alle Kosten (Stunden, externe Leistungen, Lizenzen, Rechnungen) möglichst schnell
in SAP; heute laufen sie über Innenaufträge. Philip betont, dass die Zeitrückmeldung nur ein Baustein ist; sein eigentliches
Ziel ist ein sauberes Projektmanagement-Werkzeug als Ersatz für Smartsheet („nicht mehr zukunftsfähig“).

## Grundbild: zwei Ebenen

| Ebene | System | Inhalt |
| --- | --- | --- |
| oben | Projektmanagement-Werkzeug (im Test: Project Power Pack, „PPP“) | führendes System des Projekts: Zeitplan, Meilensteine, Lieferobjekte, Risiken, Ressourcen, Decision Log |
| unten | SAP PS | nur die finanzielle Abbildung: Geld und Zeit; kein Projektmanagement, keine Risikobetrachtung |
| Schnittstelle | Projektstrukturplan (WBS) | Elemente mit Franken, Stunden und weiteren Merkmalen |

## Offene Fragen

1. Was geschieht in SAP, wenn sich der Projektstrukturplan während des Projekts ändert (Replanning) und schon gebucht wurde?
2. Welche Merkmale braucht der Projektstrukturplan für den Projektabschluss (Abrechnung CAPEX/OPEX, Anlagenbuchhaltung)?
3. Wo erfassen Mitarbeitende ihre Zeit: im Werkzeug, in einer Maske, direkt in SAP oder in einer eigenen App? Lucas hat ein
   Konzept, das Projekte aus SAP liest (schreibt noch nichts zurück).
4. Wie bildet Trafag Risiken finanziell ab? Nach Oskar gehört ein beziffertes Risiko aus kaufmännischer Vorsicht in die
   Buchhaltung bzw. als Reserve (Contingency) ins Projektbudget. Haltung Trafag offen.
5. Die heutige Projekterfassung bei Finance (Kostenobjekte) ist vom PS-Modul getrennt und müsste mit Finance zusammen neu
   gedacht werden.

## Standpunkte

- **Lucas:** schrittweise; zuerst PS verstehen und einrichten (Projektdefinition, Profile, Einschränkungen), Zeiterfassung
  danach. Bei Trafag fehle Grundwissen zum PS-Modul, externe Einführung nötig. Risikovorlagen gehören zum PMO.
- **Philip:** Projektmethodik besteht seit rund zwanzig Jahren, Vorlagen (Risiken, Phasen, Entscheide) gibt es schon in
  Smartsheet; gesucht ist eine saubere Plattform mit Datenbank, auch für KI-Abfragen und Risiko-Burndown. Das PPP soll auch
  Bereiche ohne Projektmanagement abdecken (Beispiel Fabio). Smartsheet empfindet er weiterhin als sehr intuitiv und
  kollaborativ.
- **Oskar:** stützt die Zweiteilung; Risikomanagement oben, finanzielle Abbildung der Risiken in SAP.

## Philips Vorlage Produktentwicklung (Smartsheet-Export)

- Phasen: Projektstart, Spezifikation, **Meilenstein 2 = Design-Freeze**, Industrialisierung, **Meilenstein 3 = produktiv**
  (Produkt verkauft bzw. Prozess in der Produktion), Optimierung (Projektleitung noch nicht entbunden, Kinderkrankheiten,
  First-Pass-Yield).
- Arbeitspakete (Ressourcenbehälter): Fertigungskonzept, Softwarekonzept (Betriebsmittelbau), Prototypen, Design,
  Design-FMEA, Mechanik, Hardware/PCB, Firmware, Konzept Produktverpackung, Typenschild.
- Tiefer soll der Projektstrukturplan nicht gehen; je Element kommen Soll/Ist in Franken und Stunden dazu.

## Vereinbart

| Wer | Was | Stand |
| --- | --- | --- |
| Lucas | bei NTT Data eine Offerte bzw. Einführung zum PS-Modul anfragen (was es braucht, Customizing, Anbindung); Oskar mitnehmen | offen |
| Philip | zwei echte Projekte als Testfälle liefern: **133.2** (klein) und **103.46** (abgebrochen, viele Kosten und Stunden), dazu die Standardvorlage; beide probeweise in PS abbilden | offen |
| Philip | PPP-Test weiterführen wie bei einer Einführung, mit Oskar abstimmen; Entscheid für das PPP noch nicht gefallen (Starterpaket im März gekauft, offen sind die Anbindungskosten) | läuft |
| alle | danach Finance einbeziehen | später |

## Folgen für die eigene Planung

- **PM-13 (SAP PS):** Ingos Demo vom 2026-10-20 in T76 (`DEMO-PS`, Zeit per KB21N, externe Kosten, CJI3) ist mehr Praxis, als
  in der Sitzung angenommen. Naheliegender nächster Schritt: 133.2 und 103.46 in T76 als Projektstruktur abbilden.
- **PM-10 / Trafag Projekte:** Überschneidung mit dem PPP-Test (Philip will das PPP auch in Bereiche ohne Projektmanagement
  bringen). Der Smartsheet-Modus von Trafag Projekte (Bau ab 2026-10-29) sollte vor einer breiten Ankündigung mit Philip bzw.
  Oskar abgestimmt werden.
- **Übernommen in Trafag Projekte (Auftrag Ingo 2026-10-29):** Risiken mit Franken (Erwartungswert, Reserve, Burndown,
  PMO-Vorlage), Vorlage Produktentwicklung nach Philips Phasen, Projektstrukturplan mit Soll-Stunden/-Franken und
  Kostenart als Export für SAP PS, Decision Log je Projekt. Siehe `docs/TRAFAG_PROJEKTE_2026-10-09.md`.
