# Fragebogen: Railway-Excel-Export fuer Patrik

Stand: 2026-09-01
Zieltermin: 2026-09-08 (PM-08, Statuspraesentation von Rohail Munir)

## Zweck

Der geplante Knopf im Dashboard soll Patrik eine Excel-Arbeitsdatei liefern, damit er
Railway-Kunden und -Umsaetze aufbereiten und die Zuordnungen anschliessend fachlich
bestaetigen kann. Heute gibt es keine eigene Exportfunktion auf `/marktsegmente`.
Die vorhandene Pflege im Dashboard bleibt die fuehrende Stelle fuer Bestaetigen und
Verwerfen.

Bitte die Fragen ausfuellen; die markierte Empfehlung ist der Vorschlag fuer den Termin.

## Entscheidungen

| Nr. | Frage | Auswahl / Antwort | Empfehlung |
| --- | --- | --- | --- |
| 1 | Wofuer nutzt Patrik die Datei? | Nur lesen/analysieren; offline entscheiden und danach im Dashboard pflegen; offline bearbeiten und wieder importieren | **Offline analysieren, Entscheidungen im Dashboard pflegen.** Kein zweiter, fehleranfaelliger Rueckimport vor dem Termin. |
| 2 | Welche Kunden gehoeren in die Datei? | Nur die 173 offenen Vorschlaege; offene plus bereits bestaetigte; alle Kunden des Sales-Datenbestands | **Offene und bestaetigte Railway-Zuordnungen.** So bleiben Pruefmenge und bisheriger Fortschritt sichtbar. |
| 3 | Sollen verworfene Vorschlaege enthalten sein? | Ja; nein | **Ja, in einem separaten Blatt.** Das verhindert, dass verworfene Fehltreffer erneut geprueft werden. |
| 4 | Welche Jahre? | Nur 2026; alle vorhandenen Jahre; Zeitraum: ___ | **Alle vorhandenen Jahre, mit Jahr als eigene Spalte.** Der Export bleibt mit der Ergebnisansicht vergleichbar. |
| 5 | Welche Umsatzdarstellung? | Rohwerte je Waehrung; umgerechnete Konzernwaehrung; beides | **Rohwerte je Waehrung.** Waehrungen werden nicht still addiert; Umrechnung ist eine Finance-Entscheidung. |
| 6 | Sollen einzelne Verkaufszeilen exportiert werden? | Ja, vollstaendig; nur Kundensummen; beides | **Beides:** ein Detailblatt und ein Summenblatt je Kunde/Jahr/Standort/Waehrung. |
| 7 | Soll die Marktumfrage enthalten sein? | Nein; als separates Blatt; mit den Umsatzzeilen vermischt | **Separates Blatt.** Interessenten und Schaetzwerte duerfen nicht mit fakturiertem Umsatz vermischt werden. |
| 8 | Wie werden breit einkaufende Kunden behandelt (z. B. Siemens)? | Pauschal Railway; einzeln entscheiden; ausschliessen | **Einzeln entscheiden und im Export deutlich markieren.** |
| 9 | Darf die Datei sensible Felder enthalten? | Alle technischen Quellfelder; nur fachlich benoetigte Felder; Liste: ___ | **Nur fachlich benoetigte Felder.** Vollstaendig bedeutet alle fuer die Pruefung relevanten Felder, nicht pauschal jede Datenbankspalte. |
| 10 | Wer darf den Export ausloesen? | Alle Nutzer mit Finance-Unlock; nur Patrik; Rolle/Gruppe: ___ | **Gleiche Berechtigung wie `/marktsegmente`**, falls keine engere Vorgabe kommt. |
| 11 | Wie soll die Datei heissen und gespeichert werden? | Browser-Download; zentraler Ablageort: ___; beides | **Browser-Download mit Zeitstempel.** Keine unkontrollierte Ablage personenbezogener Arbeitsdateien auf dem Server. |
| 12 | Ist ein Rueckimport bis 08.09. zwingend? | Ja; nein | **Nein.** Falls ja, braucht es eine eindeutige Importvorlage, Validierung und Konfliktregel; das ist ein eigener Lieferumfang. |

## Vorgeschlagener Aufbau der Excel-Datei

| Blatt | Inhalt |
| --- | --- |
| `Anleitung` | Erzeugungszeitpunkt, Filter, Begriffsdefinitionen, Hinweis: unbestaetigt ist kein Reporting-Fakt. |
| `Pruefung` | Alle offenen Vorschlaege mit Standort, Kundennummer, Kundenname, Segment, Quelle, Bestatigungsstatus, Warnung bei vier oder mehr Produktsparten sowie Umsatz-/Zeilenindikatoren. |
| `Bestaetigt` | Bereits bestaetigte Railway-Zuordnungen mit denselben Prueffeldern. |
| `Verworfen` | Verworfene Vorschlaege, getrennt zur Nachvollziehbarkeit. |
| `Umsatz_Detail` | Verkaufszeilen der in den obigen Blaettern enthaltenen Kunden; keine Waehrungsaddition. |
| `Umsatz_Summen` | Summe nach Kunde, Jahr, Standort und Waehrung. |
| `Marktumfrage` | Die 269 Umfrageeintraege, getrennt von Ist-Umsatz. |
| `Datenluecken` | Insbesondere TRDE: Kundennummer vorhanden, Kundenname und Kundenland derzeit in allen Verkaufszeilen leer. |

## Abnahmekriterien

- Der Export enthaelt einen sichtbaren Erzeugungszeitpunkt und die gewaehlten Filter.
- Jede Zuordnung ist ueber `TSC` plus `CustomerNumber` eindeutig nachvollziehbar.
- Unbestaetigte Vorschlaege werden klar als unbestaetigt markiert und nicht als Railway-Umsatz ausgegeben.
- Umsatz wird nur innerhalb derselben Waehrung summiert.
- Die TRDE-Datenluecke wird offen ausgewiesen; sie darf nicht als Umsatz von null erscheinen.
- Patrik kann die 30 wichtigsten Vorschlaege aus der bestehenden Pruefliste im Export wiederfinden.

## Noch ausserhalb des Exporters zu klaeren

1. Der deutsche Quell-Export muss einen Kundennamen liefern; ohne ihn kann die Railway-Umfrage nicht automatisch mit TRDE-Umsaetzen verknuepft werden.
2. Patrik bzw. der Vertrieb muss die Vorschlaege fachlich bestaetigen oder verwerfen. Das darf niemand stellvertretend automatisiert tun.
3. Falls ein Rueckimport gewuenscht ist, sind Vorlagenformat, Konfliktregel und Freigabeprozess separat festzulegen.
