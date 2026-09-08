# Teams-Nachricht an Andreas Stoller, 2026-09-08

Entwurf zum Kopieren. Anlass: Andreas hat gefragt, ob es in SAP B1 so etwas wie ein
Clearing Date oder Date Paid gibt. Fachlicher Hintergrund und Feldnamen:
`docs/FINANCE_JOURNAL_KONSOLIDIERUNG_ANDREAS_2026-09-08.md`.

Die Tabellennamen sind im Text bewusst weggelassen; sie interessieren ihn nicht und machen
die Nachricht nur schwerer. Die Rueckfrage nach dem Verwendungszweck steht bewusst **vor**
jeder Zusage, sonst bauen wir ein Feld, das nachher niemand so meinte.

> Hoi Andreas
>
> Zu deiner Frage nach Clearing Date oder Date Paid: Beides gibt es in B1, aber **nicht als
> ein einzelnes Feld**. Es sind drei verschiedene Dinge, und die Unterscheidung ist wichtig:
>
> **Fälligkeitsdatum** — steht an der Buchungszeile selbst. Das haben wir seit heute drin,
> es ist in allen vier Gesellschaften vollständig gefüllt.
>
> **Ausgleichsdatum** — wann eine Rechnung gegen eine Zahlung ausgeglichen wurde. Das kommt
> dem klassischen Clearing Date am nächsten.
>
> **Zahlungsbelegdatum** — wann eine Zahlung gebucht wurde. Das ist nicht dasselbe: eine
> Rechnung kann durch mehrere Teilzahlungen, eine Gutschrift oder einen manuellen Ausgleich
> geschlossen werden.
>
> Ein pauschales „Date Paid" wäre deshalb fachlich nicht eindeutig. Bevor wir etwas
> einbauen, bräuchte ich von dir, **wofür** du es brauchst — die drei Verwendungen führen zu
> unterschiedlichen Feldern:
>
> Für eine **Fälligkeitsstaffel** reicht das Fälligkeitsdatum, das ist schon da. Für eine
> **Cashflow-Auswertung** wäre das letzte Zahlungsdatum das richtige. Für die
> **Altersstruktur der offenen Posten** brauchst du eigentlich kein Datum, sondern den
> offenen Betrag zum Stichtag — und der kommt aus der Ausgleichszuordnung, nicht aus einem
> einzelnen Feld.
>
> Sag mir welche der drei, dann bauen wir gezielt das eine statt drei halbe.
>
> Zum Stand: Das Journal ist für Frankreich, Italien, USA und Indien geladen, rund 470'000
> Buchungszeilen. Die Excel-Mappe `Finance_All` liegt bereit, mit einem Blatt `Konten` —
> dort stehen alle 915 lokalen Konten je Gesellschaft, und wenn du dort die Spalte
> Konzernkonto ausfüllst, haben wir den Kontenplan ohne dass du ihn von Hand aufstellen
> musst.
>
> Was noch fehlt: die Schweiz und Österreich. Deren Hauptbuch kommt aus SAP, und dort heisst
> die Schnittstelle anders als bei uns hinterlegt. Das lösen wir diese Woche.
>
> Gruss
> Ingo

## Technische Entsprechung, falls jemand nachfragt

| Fachbegriff | Feld in B1 | Stand bei uns |
|---|---|---|
| Faelligkeitsdatum | `JDT1.DueDate` | gelesen seit Commit `646a998`, produktiv seit 2026-09-08 14:02 |
| Ausgleichsdatum | `OITR.ReconDate` | nicht importiert |
| Zahlungsbelegdatum | `ORCT.DocDate` (Kunde), `OVPM.DocDate` (Lieferant) | nicht importiert |
| Datum der Banküberweisung | `TrsfrDate` | nicht importiert |

Alle vier sind laut der Feldprobe vom 2026-09-08 in den B1-Datenbanken vorhanden. Nicht
importiert heisst hier: bewusst nicht, solange die fachliche Definition offen ist.
