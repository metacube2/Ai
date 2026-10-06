# Vorgabe fuer alle Kapitel des Anwenderhandbuchs „Trafag Cockpit"

Ziel: Ein Handbuch aus **betriebswirtschaftlicher Sicht**. Der Leser (Controller, Einkaeufer,
Verkaeufer/Vertriebsleiter, Logistiker, Disponent, Geschaeftsleitung, HR) soll verstehen,
**was eine Seite/Grafik zeigt, warum es sie gibt, wie die Zahl zustande kommt (in Worten, nicht
Code) und in welcher Arbeitssituation er sie einsetzt**. Kein Entwicklerhandbuch: keine Klassen-
oder Methodennamen, keine SQL. SAP-Tabellen/Felder nur, wenn sie dem Fachanwender helfen
(z. B. „Bestellungen aus SAP ME21N, Endlieferkennzeichen").

Sprache: Deutsch (Schweiz, also „ss" statt „ß"), echte Umlaute (ä ö ü), ganze Saetze, wenig
Gedankenstriche, sachlich. Anrede neutral („man", „Sie" vermeiden wo moeglich, sonst „Sie").

## Arbeitsweise

- Repository: `C:\Users\koi\source\repos\Ai\TrafagSalesExporter`. Nur LESEN. Keine Datei im
  Repository aendern, nichts committen, nichts deployen, kein SAP, keine Produktiv-DB.
- Einstieg: `router.md`, dann der passende Unterrouter unter `docs/router/`, dann Detaildateien.
  Danach die Razor-Seiten (`Components/Pages/*.razor`, `Components/**`) und Services lesen, um
  jede Aussage zu pruefen. **Nichts erfinden.** Was unklar bleibt, als „Hinweis: nicht geprueft"
  kennzeichnen statt raten.
- Erklaerungstexte, die schon in der Oberflaeche stehen (Untertitel, Hinweise, Tooltips), sind die
  beste Quelle fuer Absicht und Lesart.
- Stand der Software: Commit `3ad0dae` (2026-10-05), inklusive der Pruefbefund-Korrekturen.

## Ausgabeformat (strikt, wird automatisch nach Word umgesetzt)

Eine Markdown-Datei mit nur diesen Elementen:
- `# ` Kapiteltitel (genau einmal, erste Zeile)
- `## ` Seite/Unterreiter (Menuename, z. B. `## Offene Bestellungen`)
- `### ` feste Unterabschnitte je Seite, in dieser Reihenfolge:
  `### Wozu`, `### Was man sieht`, `### Wie gerechnet wird`, `### Einsatz im Arbeitsalltag`,
  `### Grenzen und Fallstricke`
- Absaetze (Leerzeile dazwischen), Aufzaehlungen mit `- `, Fettdruck `**...**`
- Einfache Tabellen mit `|` (Kopfzeile + `|---|` Zeile), sparsam
- Keine Bilder, keine Links, kein Code-Block, keine weiteren Ebenen.

Am Anfang des Kapitels (nach `#`) ein Einleitungsabsatz: wofuer das Modul da ist, wer es
hauptsaechlich nutzt, wo es im Menue liegt, woher die Daten kommen und wie aktuell sie sind.

Unter „Einsatz im Arbeitsalltag" konkret nach Rolle schreiben, z. B.
„**Controller:** vor dem Monatsabschluss …", „**Einkäufer:** vor der Lieferantenverhandlung …".
Typische Fragen nennen, die die Seite beantwortet.

Umfang: je Seite etwa 150 bis 350 Woerter; Uebersichtsseiten duerfen laenger sein, reine
Admin-/Technikseiten kuerzer (dann nur „Wozu" und „Einsatz" ausfuehrlich).

Am Ende des Kapitels ein Abschnitt `## Kennzahlen-Glossar` mit einer Tabelle
`| Kennzahl | Bedeutung | Berechnung in Worten |` fuer die wichtigsten Kennzahlen des Moduls.
