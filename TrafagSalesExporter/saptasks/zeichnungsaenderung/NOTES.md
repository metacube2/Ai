# Zeichnungsaenderung: SAP-Daten fuer die MS-Liste

Stand: 2026-11-02. Auftrag Ingo 02.11. Status: **Weg entschieden, SAP-Teil in Arbeit, Dashboard-Teil wartet
auf SharePoint-Berechtigung und Ablageort.**

## 1. Anfrage

Die Entwicklung (Simon Klaey, Entwicklungsingenieur Mechanik) erfasst Zeichnungsaenderungen seit Oktober in einer
MS-Liste, die Andreas Wild betreut. Beim Anlegen kopiert Simon Benennung, Index und Materialstatus von Hand aus SAP.
Er wuenscht, dass sich diese Felder nach Eingabe der Materialnummer selbst fuellen (Mail 30.10.). Andreas fragte bei
Alex Donato nach, Alex bei Lucas Castro. Lucas (02.11.): technisch moeglich, aber nur als OData-Abfrage ohne
Schreibrecht mit abgestimmten Filtern, damit SAP nicht belastet wird; Ingo stellt die Daten bereit. Simon schlug als
Ausweichweg eine naechtlich aktualisierte Datenliste im SharePoint vor, aus der die MS-Liste liest.

Beteiligte: Simon Klaey (Anforderung), Andreas Wild (MS-Liste, Power Automate), Alex Donato (IT, M365), Lucas Castro
(SAP), Cc Stefan Heinrich, Remo Halbheer.

## 2. Befund an Simons Excel `Zeichnungsänderung_DatenVonSAP.xlsx` (02.11.)

Die Datei lag in der Repo-Wurzel und ist bewusst nicht versioniert (P76-Geschaeftsdaten eines Kollegen).

- **Direkter Datenbankzugriff.** Zwei Power-Query-Abfragen lesen per `Sql.Database("tragvsqlc400", "SAP")`
  `SELECT MATNR, MSTAE, ZEIVR FROM P76.MARA` und `SELECT MATNR, MAKTX FROM P76.MAKT`, jeweils den ganzen Bestand,
  ohne Mandant. Genau diesen Weg will Lucas vermeiden.
- **Gebrauchte Felder:** Materialnummer, Benennung `MAKT-MAKTX`, Index `MARA-ZEIVR`, Materialstatus `MARA-MSTAE`.
  Blatt `EingabeTab` holt sie per `XLOOKUP(Matnr; LEFT(Spalte;6); …)`.
- **Trefferquote:** Index und Status fehlen bei 21'352 von 68'717 Materialien. `MARA-MATNR` kommt mit Leerzeichen
  aufgefuellt, `LEFT(…;6)` schneidet zudem Nummern mit mehr als sechs Stellen ab (3'288 Materialien).
- **Sprache:** `MAKT` ohne `SPRAS`-Filter, 169'357 Texte zu 68'716 Materialien, meist drei je Material. Die Formel
  nimmt den ersten Text, das ist teils der franzoesische (Beispiel 63397).
- Verteilung im Abzug: `ZEIVR` leer 47'214, `A` 9'020, `B` 4'125, `C` 2'721; `MSTAE` leer 36'228, `99` 28'054,
  `00` 1'682.

Simon hat beide Fehler am 02.11. bestaetigt (schnell gebaut) und will immer nur die deutsche Benennung.

Die KI-Antwort in Andreas' Mail vom 30.10. sucht ueber `MARA-ZEINR` und nennt ein Feld `VPR`, das es in `MARA` nicht
gibt. Sie ist als Vorlage nicht zu verwenden.

## 3. Was es in SAP schon gibt

Im Dienst `ZPOWERBI_EINKAUF_SRV`, gelesen vom Einkauf-Lauf des Dashboards (`Services/PurchasingDataRefreshService.cs`):

| Bedarf | Set und Feld | Stand 02.11. |
| --- | --- | --- |
| Materialstatus | `MARA001Set.Mstae` | vorhanden |
| Benennung | `MAKTSet.Maktx` mit `Spras`, Auswahl Deutsch vor Englisch in `SelectMaterialTexts` | vorhanden |
| Index | `MARA-ZEIVR` | **fehlt** in `MARA001Set` |

Beide Sets ignorieren `$filter`, `$top` und `$skip` und liefern immer den ganzen Bestand (rund 68'000 Materialien).
Fuer den Server, der einmal pro Lauf liest, ist das unerheblich. Fuer eine Einzelabfrage je Listeneintrag aus Power
Automate waere es genau die Last, vor der Lucas warnt. Dazu kaeme ein Datengateway, weil SAP nur im Firmennetz
erreichbar ist, und eine Premium-Lizenz fuer den Connector.

## 4. Entscheid (Mail Ingo 02.11. 09:58, bestaetigt von Simon 02.11.)

1. `Zeivr` wird additiv an `MARA001Set` angehaengt. `Zeinr` braucht es nicht.
2. Das Dashboard holt die Daten einmal pro Nacht und legt eine fertige Liste im SharePoint ab.
3. Die MS-Liste liest diese Liste mit Standard-Connectoren. Kein Gateway, keine Premium-Lizenz, kein Direktzugriff auf
   die SAP-Datenbank. Die Daten sind dafuer bis zu einem Tag alt.

Simons Antworten (02.11.):

- Suche **immer ueber die Materialnummer**.
- Feld „SAP Betroffene Materialien / VKNR“ muss **nicht** gefuellt werden.
- **Keine weiteren Felder.** Die Liste hat vier Spalten: Materialnummer (ohne fuehrende Nullen und Leerzeichen),
  Benennung Deutsch, Index `Zeivr`, Materialstatus `Mstae`.

## 5. Offen

| Punkt | Wer | Stand 02.11. |
| --- | --- | --- |
| Zustimmung zum Weg | Lucas Castro | keine Antwort |
| Schreibrecht des Dashboard-Servers im SharePoint (App-Berechtigung) | Alex Donato, laut Mail von Ingo beauftragt | keine Antwort |
| Ablageort der Liste, Format fuer den Flow | Andreas Wild | keine Antwort |
| `Zeivr` an `MARA001Set` in T76, danach Import P76 | Claude in T76, Import Ingo | siehe Abschnitt 6 |
| Naechtlicher Abzug im Dashboard | Claude | wartet auf `Zeivr` in P76 und auf Ablageort und Berechtigung |

Der Dashboard-Teil wird bewusst noch nicht gebaut: Ohne Ziel und Berechtigung waere er halb, und seine Registrierung
beruehrt `Program.cs` bzw. `TimerBackgroundService.cs`, in denen die unfertige Finance_All-Automatik unkommittiert
liegt.

## 6. SAP-Umsetzung

Noch nicht begonnen. Wird hier nachgetragen (Transport, Objekte, Rueckleseprobe).

**Blocker 02.11. (Auskunft der parallelen Einkauf-Sitzung):** `ZCL_ZPOWERBI_EINKAUF_MPC_EXT` und `_DPC_EXT` liegen im
offenen Auftrag `T76K912785` (Aufgabe `T76K912786`, ZLO03 FIX 24 und `ZloTopDownSet`). Solange er offen ist, fragt SAP
beim Aendern dieser Klassen nicht nach dem Auftrag und legt die Aenderung still dort hinein (LEARNINGS 2026-10-08 und
2026-10-16). Ein eigener Transport geht fuer diese Klassen erst nach Freigabe von `T76K912785`, oder `Zeivr` geht
bewusst in denselben Auftrag (Entscheid Ingo).

Vor dem Aendern zu klaeren: Steckt `MARA001Set` in `MPC_EXT`/`DPC_EXT` oder im SEGW-generierten Teil
(`ZCL_ZPOWERBI_EINKAUF_MPC`, Projekt in SEGW)? Im zweiten Fall sind es andere Objekte, und der Weg ist die
SEGW-Property plus Neugenerierung, wie beim Anhaengen von `Matkl` am 2026-07-23.
