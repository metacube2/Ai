# PPWR und Stoffcompliance im SAP – Anlageprotokoll für Adil

> **NACHTRAG vom 18.08.2026 — Anlage in T76/090 war fehlerhaft, in T76/100 jetzt live belegt.**
> Ingo meldete, dass in T76/090 trotz der Erfolgsmeldung in Abschnitt 12 **keine Klassen**
> angelegt wurden. Codebefund: `create_class` prüfte die Existenz nur über
> `BAPI_CLASS_EXISTENCECHECK` mit Vorbelegung „vorhanden", und genau dieser Prüftyp
> lieferte laut Abschnitt 14 Punkt 1 schon bei den Merkmalen keinen auswertbaren Fehler.
> Als Nachweis diente ausschließlich die Selbstmeldung des Reports, die unabhängige
> `CL03`-Kontrolle aus Abschnitt 9 wurde nie ausgeführt.
> Zielmandant ist seit dem 18.08.2026 **T76/100**, weil 090 keine Daten trägt. Nach
> Korrektur der Existenzprüfung wurde der Report am 18.08.2026 in T76/100 mit `P_WRITE = X`
> ausgeführt; eine echte Tabellenzählung aus `CABN`/`KLAH`/`KSML` bestätigt danach 9+12
> Merkmale und beide Klassen mit vollständiger Merkmalszuordnung. Details und die
> Kennzahlentabelle: `docs/PPWR_MANDANT_100_ANALYSE_2026-08-18.md` Abschnitt 9.
> Offen bleibt die Sichtprüfung in `CL03`/`CT04` (Reihenfolge, Wertelisten, Bezeichnungen)
> und die Klärung des Programmnamens, unter dem der Lauf protokolliert wurde (`ZTEST55`).
> Der **fachliche** Teil dieses Dokuments — Merkmalskatalog, Wertelisten, Rechtsrahmen,
> Abnahmekriterien, offene Entscheidungen — gilt unverändert weiter.

Stand: 13.08.2026  
System für Aufbau und Pilot: **T76, Mandant 090** (überholt, jetzt Mandant 100)
Status: Anlage in T76/090 gemeldet, aber nicht unabhängig belegt; keine Freigabe für P76
oder Massenpflege

Quelle: `Verpackungsverordnung.docx` im Projektstamm sowie die Mailabstimmung zwischen
Fabio Palma und Florian Wächter. Ziel ist die Abbildung über die SAP-Klassifizierung,
ohne zusätzliche Felder im Materialstamm.

## 1. Entscheidungsvorschlag

1. Verpackungseigenschaften und Stoffcompliance werden in **zwei getrennten Klassen**
   der Klassenart `001` aufgebaut:
   - `ZPPWR_PACKMITTEL` für Eigenschaften eines Packmittels;
   - `ZCOMP_STOFF` als ausdrücklich befristete Zwischenlösung für stoffliche
     Compliance eines Materials.
2. Die Zuordnung zu `ZPPWR_PACKMITTEL` ist das verlässliche Kennzeichen
   „verpackungsrelevant“. `H*` und Materialart `VERP` dienen nur zum Ermitteln der
   Erstmenge. Sie dürfen keine dauerhaften Ausschlussfilter sein, weil beispielsweise
   Kabelbinder ebenfalls Verpackung sein können.
3. `MAGRV` bleibt das führende Standardfeld für die Verpackungsfraktion, sofern dessen
   Werteliste Papier/Karton/Kunststoff/Metall/Verbund fachlich abdeckt. Kein
   gleichbedeutendes Klassifizierungsmerkmal doppelt pflegen.
4. Gewichte werden zunächst aus `BRGEW`/`NTGEW` übernommen. Vor dem Reporting sind
   Mengeneinheit und Bezugsmenge an mindestens zehn Packmitteln zu prüfen.
5. Eine Klasse der Klassenart `001` hängt am Material. Sie bildet **nicht** die
   Beziehung Lieferant × Material ab. Lieferantenerklärungen in der Klassifizierung
   sind deshalb nur ein materialbezogener Zwischenstatus für den aktuell freigegebenen
   Lieferanten. Das Zielbild ist SAP Product Compliance beziehungsweise eine
   dokumentierte Lieferant-Material-Beziehung.
6. `CL30N` ist Suche und Auswertung, keine Dokumentablage. Im Pilot wird in der Klasse
   nur eine Dokumentreferenz gespeichert. Das Dokument selbst bleibt zunächst im
   freigegebenen Ablageort; Zielbild ist DMS oder Product Compliance.
7. Ohne eine gepflegte Kante Produkt ↔ Packmittel ist keine PPWR-Aggregation je
   verkauftem Sensor möglich. Vor einer Auswertung muss geklärt sein, ob diese Kante
   über Stückliste, Verpackungsvorschrift oder eine andere Zuordnung entsteht.

## 2. Rechtliche Leitplanken

Die PPWR ist die Verordnung (EU) 2025/40. Sie trat am 11.02.2025 in Kraft und gilt
grundsätzlich seit 12.08.2026. Die Einstufung A–E darf im SAP vorbereitet werden, ist
aber bis zur verbindlichen Methodik und internen Freigabe als **vorläufig** zu kennzeichnen.
Die Rezyklatvorgaben beziehen sich bei Kunststoffverpackungen auf Rezyklat aus
Post-Consumer-Kunststoffabfällen und die gesetzliche Berechnung ist nicht einfach nur
ein unveränderlicher Einzelwert je Material.

Wichtig für PFAS: Artikel 5 Absatz 5 der PPWR betrifft Verpackungen mit
Lebensmittelkontakt und nennt Grenzwerte in `ppb` beziehungsweise `ppm`, nicht in
Prozent. Für Trafag-Verpackungen ohne Lebensmittelkontakt ist dieses konkrete
PPWR-PFAS-Verbot daher nicht die passende Bewertungsbasis. Ein allgemeines Feld
`PFAS Content (%)` wird nicht freigegeben, solange Stoffumfang, Einheit,
Nachweismethode und Rechtsgrundlage fehlen.

REACH, SVHC und RoHS sind ein eigener Product-Compliance-Datenstrom und nicht einfach
weitere PPWR-Verpackungsmerkmale. Bei SVHC muss der Bezugsstand der Kandidatenliste
nachweisbar sein; die REACH-Schwelle von 0,1 Gewichtsprozent ist auf jedes Erzeugnis
eines komplexen Gegenstands anzuwenden.

Verbindliche Quellen:

- [Verordnung (EU) 2025/40 (PPWR), EUR-Lex](https://eur-lex.europa.eu/eli/reg/2025/40/oj?locale=de)
- [EU-Kommission: Packaging waste](https://environment.ec.europa.eu/topics/waste-and-recycling/packaging-waste_en)
- [ECHA: Requirements for substances in articles](https://echa.europa.eu/documents/10162/2324906/nutshell_guidance_articles2_en.pdf)
- [SAP Product Compliance: Supplier Compliance for Raw Material](https://help.sap.com/docs/SAP_S4HANA_ON-PREMISE/35751be3d6ee423197492574e016a512/2f7c05cf0da34e52af50296f47feb9fc.html)

Dieses Dokument ist ein SAP-Lösungsvorschlag und keine Rechtsberatung.

## 3. Einheitliche Statuswerte

Für REACH, SVHC und RoHS werden keine Ja/Nein-Felder verwendet. In CT04 werden
folgende vollständige Werte angelegt; zusätzliche Werte sind nicht erlaubt:

| Technischer Wert | Bedeutung |
| --- | --- |
| `COMPLIANT` | Anforderung auf dokumentierter Basis erfüllt |
| `NON_COMPLIANT` | Anforderung auf dokumentierter Basis nicht erfüllt |
| `UNDEFINED` | noch nicht geprüft oder Nachweis unzureichend |

Datentyp ist jeweils `CHAR`, Länge `13`, einwertig. Leere Werte gelten nicht als
`COMPLIANT`. Ein CT04-Vorschlagswert füllt bestehende Materialien nicht rückwirkend;
das ist im Pilot ausdrücklich zu testen. Für PFAS werden zunächst dieselben drei
Statuswerte verwendet. `NOT_RELEVANT` darf erst nach einer gemeinsam definierten
Relevanzregel ergänzt werden.

## 4. CT04 – Merkmale für `ZPPWR_PACKMITTEL`

### 4.1 Jetzt im T76-Pilot anlegen

| Reihenfolge | Merkmal | Bezeichnung | Format | Zulässige Werte / Prüfung | Pflicht im Pilot |
| ---: | --- | --- | --- | --- | --- |
| 10 | `ZPPWR_RECYCL_CLASS` | Recyclability Class | `CHAR 1` | `A`, `B`, `C`, `D`, `E`; keine Zusatzwerte; vorläufig | ja |
| 20 | `ZPPWR_RECYCLAT_PCT` | Total Recycled Content % | `NUM 5,2` | Intervall `0` bis `100`; einwertig | nein |
| 30 | `ZPPWR_PCR_PCT` | PCR Content % | `NUM 5,2` | Intervall `0` bis `100`; einwertig | nein |
| 40 | `ZPPWR_DECL_STATUS` | Lieferantenerklärung Status | `CHAR 9` | `YES`, `NO`, `UNDEFINED`; keine Zusatzwerte | ja |
| 50 | `ZPPWR_DECL_DATE` | Lieferantenerklärung Datum | `DATE` | kein Vorschlagswert | bei `YES` |
| 60 | `ZPPWR_VALID_TO` | Lieferantenerklärung gültig bis | `DATE` | kein Vorschlagswert | bei `YES` |
| 70 | `ZPPWR_DECL_REF` | Lieferantenerklärung Referenz | `CHAR 30` | Dokument-ID, kein Dateipfad | bei `YES` |
| 80 | `ZPPWR_DATA_DATE` | Datenstand Verpackung | `DATE` | kein Vorschlagswert | ja |
| 90 | `ZPPWR_FOOD_CONTACT` | Lebensmittelkontakt | `CHAR 9` | `YES`, `NO`, `UNDEFINED`; keine Zusatzwerte | ja |

Hinweise:

- `ZPPWR_RECYCLAT_PCT` und `ZPPWR_PCR_PCT` bleiben getrennt, weil PCR eine
  Teilmenge des gesamten Rezyklats ist.
- Bei `ZPPWR_DECL_STATUS = YES` müssen Datum, Gültigkeit und Referenz gemeinsam
  gepflegt sein. Diese Abhängigkeit lässt sich mit reiner Klassifizierung nicht
  zuverlässig erzwingen und gehört deshalb in die Pilotprüfung.
- Materialfraktion und Gewicht werden nicht als neue Merkmale dupliziert, solange
  `MAGRV`, `BRGEW` und `NTGEW` die Anforderungen erfüllen.

### 4.2 Noch nicht anlegen

| Vorschlag | Grund für Sperre |
| --- | --- |
| `ZPPWR_PFAS_PCT` | Prozent ist nicht die Einheit der PPWR-Grenzwerte; Lebensmittelkontakt, Messmethode und Stoffumfang fehlen |
| `ZPPWR_MAT_VERP` | würde `MAGRV` doppelt abbilden |
| freier URL-/Ordnerpfad | Klassifizierung ist kein Dokumentenarchiv; Referenz-ID genügt im Pilot |

## 5. CT04 – Merkmale für `ZCOMP_STOFF`

Diese Klasse ist eine Zwischenlösung bis zur Entscheidung über SAP Product Compliance.
Sie wird am bewerteten Kaufteil beziehungsweise Rohmaterial zugeordnet, nicht pauschal
am fertigen Sensor.

### 5.1 Jetzt im T76-Pilot anlegen

| Reihenfolge | Merkmal | Bezeichnung | Format | Zulässige Werte / Prüfung | Pflicht im Pilot |
| ---: | --- | --- | --- | --- | --- |
| 10 | `ZCOMP_REACH_STATUS` | REACH Status | `CHAR 13` | `COMPLIANT`, `NON_COMPLIANT`, `UNDEFINED` | ja |
| 20 | `ZCOMP_REACH_DATE` | REACH Bewertungsstand | `DATE` | kein Vorschlagswert | ja |
| 30 | `ZCOMP_SVHC_STATUS` | SVHC Status | `CHAR 13` | `COMPLIANT`, `NON_COMPLIANT`, `UNDEFINED` | ja |
| 40 | `ZCOMP_SVHC_LISTDAT` | SVHC Kandidatenliste Stand | `DATE` | kein Vorschlagswert | ja |
| 50 | `ZCOMP_ROHS_STATUS` | RoHS Status | `CHAR 13` | `COMPLIANT`, `NON_COMPLIANT`, `UNDEFINED` | ja |
| 60 | `ZCOMP_ROHS_DATE` | RoHS Bewertungsstand | `DATE` | kein Vorschlagswert | ja |
| 70 | `ZCOMP_PFAS_STATUS` | PFAS Status | `CHAR 13` | `COMPLIANT`, `NON_COMPLIANT`, `UNDEFINED` | ja |
| 80 | `ZCOMP_PFAS_DATE` | PFAS Bewertungsstand | `DATE` | kein Vorschlagswert | ja |
| 90 | `ZCOMP_DECL_STATUS` | Lieferantenerklärung Status | `CHAR 9` | `YES`, `NO`, `UNDEFINED` | ja |
| 100 | `ZCOMP_DECL_DATE` | Lieferantenerklärung Datum | `DATE` | kein Vorschlagswert | bei `YES` |
| 110 | `ZCOMP_VALID_TO` | Lieferantenerklärung gültig bis | `DATE` | kein Vorschlagswert | bei `YES` |
| 120 | `ZCOMP_DECL_REF` | Lieferantenerklärung Referenz | `CHAR 30` | Dokument-ID, kein Dateipfad | bei `YES` |

### 5.2 Noch nicht anlegen

| Vorschlag | Grund für Sperre |
| --- | --- |
| `ZCOMP_PFAS_PCT` | keine definierte PFAS-Stoffliste, Einheit, Nachweismethode oder Bewertungsregel |
| ein einziges `ZCOMP_STAND_DAT` | ein Datum wäre für vier unterschiedliche Rechts-/Prüfstände mehrdeutig |

## 6. CT04 – Anlagefolge je Merkmal

Für jedes freigegebene Merkmal:

1. Transaktion `CT04` öffnen, technischen Namen in Großbuchstaben eingeben und
   **Anlegen** wählen.
2. Deutsche Bezeichnung gemäß den Tabellen eintragen. Merkmalsgruppe und Status
   nach Trafag-Konvention setzen; falls keine Konvention existiert, im Pilot nicht
   improvisieren, sondern Adil/Lucas entscheiden lassen.
3. Datentyp, Stellenzahl und Dezimalstellen exakt gemäß Tabelle pflegen.
4. Merkmal als einwertig führen. Bei Statusmerkmalen und `YES/NO/UNDEFINED`
   ausschließlich die aufgeführten Werte zulassen; freie Zusatzwerte deaktivieren.
5. Prozentmerkmale mit Intervall `0` bis `100` begrenzen.
6. Keine automatische Übersetzung oder anderssprachige Kurztexte erfinden. Deutsche
   Texte sind Pflicht; englische Texte werden nur nach Freigabe ergänzt.
7. Speichern und technischen Namen, Format sowie Werteliste sofort über `CT04`
   im Anzeigemodus gegenprüfen.
8. Änderungen im Anlageprotokoll mit Datum, Benutzer und SAP-Auftrag beziehungsweise
   Verteilweg dokumentieren. Klassifizierungsstammdaten nicht stillschweigend wie
   normales Customizing behandeln.

## 7. CL01 – Klassen anlegen

### Klasse 1: `ZPPWR_PACKMITTEL`

- Transaktion: `CL01`
- Klassenart: `001` – Materialklasse
- Bezeichnung: `PPWR Packmittel`
- Merkmale: die neun freigegebenen `ZPPWR_*`-Merkmale in der Reihenfolge aus
  Abschnitt 4.1
- Status: im T76-Pilot nach Trafag-Konvention freigeben

### Klasse 2: `ZCOMP_STOFF`

- Transaktion: `CL01`
- Klassenart: `001` – Materialklasse
- Bezeichnung: `Stoffcompliance Interim`
- Merkmale: die zwölf freigegebenen `ZCOMP_*`-Merkmale in der Reihenfolge aus
  Abschnitt 5.1
- Klassenkurztext muss `Interim` enthalten, damit die Klasse nicht mit dem Zielbild
  Product Compliance verwechselt wird.

Nach jeder Anlage ist die Klasse zunächst in `CL03` zu prüfen. Erst danach werden
Materialien zugeordnet.

## 8. Pilotzuordnung und Erstbefüllung

1. Kandidatenliste für Packmittel aus mehreren Quellen bilden:
   - Materialart `VERP`;
   - Materialnummer `H*`;
   - gepflegte Verpackungsmaterialgruppe;
   - Verpackungspositionen aus Stücklisten oder Verpackungsvorschriften.
2. Dubletten entfernen und mit Einkauf/Operations fachlich bestätigen.
3. Zunächst nur **10 bis 20 Packmittel** der Klasse `ZPPWR_PACKMITTEL` zuordnen.
4. Für `ZCOMP_STOFF` nur wenige reale Kaufteile mit vorhandener Erklärung verwenden.
5. Neue/ungeprüfte Statuswerte werden explizit als `UNDEFINED` gepflegt. Nie
   `COMPLIANT` aus einem leeren Feld ableiten.
6. Klassenzuordnung über `MM02` Sicht Klassifizierung oder die bei Trafag freigegebene
   Massenpflege durchführen. Keine Massenpflege vor erfolgreicher Pilotabnahme.

## 9. Abnahme in T76/090

Der Pilot gilt erst als bestanden, wenn alle folgenden Punkte nachgewiesen sind:

- `CL03` zeigt beide Klassen mit den richtigen Merkmalen und Reihenfolgen.
- `CT04` zeigt bei jedem Statusmerkmal nur die freigegebenen Werte.
- Zahlen unter `0` und über `100` werden bei den Prozentmerkmalen abgewiesen.
- Ein ungeprüftes Material erscheint als `UNDEFINED`, nicht als konform.
- `CL30N` findet die Pilotmaterialien nach Klasse, Status und Recyclability Class.
- Die Ergebnisliste lässt sich für die gewünschte Ad-hoc-Auskunft nach Excel
  ausgeben.
- Zu jeder Erklärung mit Status `YES` ist das Dokument über `*_DECL_REF`
  auffindbar; `CL30N` selbst wird nicht als Ablage verwendet.
- `MAGRV`, Gewicht und Einheit sind für mindestens zehn Packmittel plausibel.
- Mindestens ein Kabelbinder beziehungsweise ein nicht über `H*` erkennbares
  Packmittel wird korrekt über die Klassenzuordnung erfasst.
- Ein Material mit zwei Lieferanten wird bewusst getestet und die Grenze der
  materialbezogenen Interimslösung dokumentiert.

## 10. Vor P76 zwingend entscheiden

| Frage | Warum sie den Produktivgang blockiert |
| --- | --- |
| Wie ist Produkt ↔ Packmittel gepflegt? | ohne Zuordnung kein Mengen-Rollup je Sensor |
| Ist `MAGRV` wirklich die Materialfraktion? | sonst fehlen Papier/Kunststoff/Metall für die Meldung |
| Wer ist Data Owner je Merkmal? | Einkauf, Operations und QM dürfen sich nicht gegenseitig überschreiben |
| Wo liegt die Lieferantenerklärung versioniert? | Referenz ohne auffindbares Dokument ist kein Nachweis |
| Wie wird Mehrlieferantenbezug behandelt? | Klassenart 001 kennt keinen Lieferantenbezug |
| Wird SAP Product Compliance lizenziert und eingeführt? | entscheidet über Laufzeit und Rückbau von `ZCOMP_STOFF` |
| Welche PFAS-Anforderung gilt für Trafag konkret? | PPWR-Food-Contact-Grenzwerte passen nicht automatisch zu Sensoren |
| Sind A–E und Berechnungsmethode intern freigegeben? | gesetzliche Methodik und Zeitpunkte sind dynamisch |

## 11. Rollen- und Terminplan

| Schritt | Verantwortlich | Ergebnis |
| --- | --- | --- |
| technische Anlage in T76 | Adil / SAP | CT04-Merkmale und CL01-Klassen |
| Verpackungskandidaten und Lieferantenerklärungen | Einkauf, Marco | belastbare Erstliste und Nachweise |
| operative Verpackungszuordnung | Stefan, Patrik, Marc | vollständige Produkt-Packmittel-Kante |
| Compliance-Bewertung und Freigabe | Florian / QM | Status plus Bewertungsstand |
| SAP-Architektur und Product-Compliance-Entscheid | Fabio, Lucas, Adil, Ingo | Zielbild und Ablösung der Interimsklasse |

Fabios Termin kann für nächste Woche eingeplant werden, sobald die T76-Bestandsprüfung
und der technische Pilotkatalog bestätigt sind. Im Termin wird keine Grundsatzfolie
mehr diskutiert, sondern der T76-Pilot, die drei blockierenden Datenbeziehungen und der
verantwortliche Pflegeprozess abgenommen.

## 12. Technisches Ausführungsprotokoll

| Datum/Zeit | System | Aktion | Ergebnis |
| --- | --- | --- | --- |
| 13.08.2026 | lokal | Quelldokument ausgewertet und Anlagekatalog erstellt | abgeschlossen |
| 13.08.2026 | T76/090 | Report `ZPPWR_CLASS_SETUP` mit Schreibmodus ausgeführt | 21 Merkmale erfolgreich angelegt und per BAPI-Commit gesichert |
| 13.08.2026 | T76/090 | Klassenart `001`: `ZPPWR_PACKMITTEL` und `ZCOMP_STOFF` angelegt | **STRITTIG.** Der Report meldete Erfolg, Ingo findet die Klassen am 18.08.2026 nicht. Ursachenverdacht: stilles `SKIP` durch die fehlerhafte Existenzprüfung |
| 18.08.2026 | lokal | Klassenprüfung auf `SELECT` aus `KLAH` umgestellt, Zielmandant auf 100, Rücklesekontrolle ergänzt | im Quelltext umgesetzt, im System noch nicht ausgeführt |
| offen | T76/100 | Iststand messen, Report einspielen, Pilotmaterialien zuordnen, CL30N-Abnahme | noch nicht ausgeführt |
| gesperrt | P76 | Transport/Verteilung und Massenpflege | erst nach Fachfreigabe |

Technischer Nachweis: Die abschließende SAP-Ausgabe meldete für jedes Merkmal
`wird angelegt`, für beide Klassen `wird angelegt` und abschließend
`FERTIG: Merkmale und Klassen angelegt/geprueft.` **Dieser Nachweis trägt nicht**, denn
es ist die Selbstmeldung genau des Programms, dessen Klassenanlage fehlerhaft war. Die
unabhängige Kontrolle über `CL03` steht bis heute aus. Der wiederholbare Quellcode liegt
unter `docs/abap/ZPPWR_CLASS_SETUP.abap`; er ist seit dem 18.08.2026 auf T76/100 gesperrt
und meldet `FERTIG` nur noch nach einer Rücklesekontrolle aus `CABN`, `KLAH` und `KSML`.

## 13. Anforderungsherkunft und Abdeckung

Die folgende Matrix hält fest, wie die Anforderungen aus der Maildiskussion und aus
`Verpackungsverordnung.docx` umgesetzt beziehungsweise bewusst abgegrenzt wurden.

| Ursprüngliche Anforderung | Entscheidung / SAP-Abbildung |
| --- | --- |
| keine zusätzlichen Z-Felder im Materialstamm | Umsetzung vollständig über zwei Klassen der Klassenart `001` und deren Merkmale |
| Recyclability Class | `ZPPWR_RECYCL_CLASS` mit `A` bis `E` |
| Total Recycled Content | `ZPPWR_RECYCLAT_PCT`, numerisch `0` bis `100` |
| PCR Content | `ZPPWR_PCR_PCT`, numerisch `0` bis `100` |
| REACH compliant | dreiwertiger Status plus Bewertungsdatum in `ZCOMP_STOFF` |
| SVHC compliant | dreiwertiger Status plus Stand der Kandidatenliste in `ZCOMP_STOFF` |
| RoHS compliant | zusätzlich aus Florians Diskussion aufgenommen: dreiwertiger Status plus Bewertungsdatum |
| PFAS Content (%) | Prozentfeld bewusst nicht angelegt; stattdessen vorläufig Status plus Bewertungsdatum, bis Stoffliste, Einheit und Rechtsgrundlage geklärt sind |
| Supplier Declaration available | nicht nur Ja/Nein: Status, Ausstellungsdatum, Gültigkeit und Dokumentreferenz in beiden fachlich passenden Klassen |
| Materialgruppe PM beibehalten | `MAGRV` bleibt führendes Standardfeld; keine doppelte Klassifizierung desselben Inhalts |
| dreiwertige Compliance statt leer/Ja/Nein | `COMPLIANT`, `NON_COMPLIANT`, `UNDEFINED`; bei Erklärungen `YES`, `NO`, `UNDEFINED` |
| Pflege im SAP GUI | Klassen und Merkmale funktionieren mit CT04/CL01 sowie später MM02/CL30N; keine Fiori-Custom-Fields vorausgesetzt |
| Reporting nur auf Anfrage als Excel-Liste | Zielweg ist CL30N mit Excel-Export; keine Dashboard-/OData-Integration im Pilot erforderlich |
| Adil baut zunächst die Klassifizierung | technischer Katalog und wiederholbarer ABAP-Report wurden für T76/090 bereitgestellt und ausgeführt |

Nicht Teil der technischen Anlage waren Materialzuordnungen, Massenpflege,
Produkt-Packmittel-Verknüpfungen, Dokumentmigration und ein Transport nach P76. Diese
Schritte benötigen die im Abschnitt 10 genannten fachlichen Entscheidungen.

## 14. ABAP-/BAPI-Learnings aus der tatsächlichen Ausführung

Diese Punkte gelten für das am 13.08.2026 verwendete Trafag-SAP-System und sollen bei
einer Wiederholung nicht erneut durch Versuch und Irrtum ermittelt werden:

1. `BAPI_CHARACT_EXISTENCECHECK` lieferte bei nicht vorhandenen Merkmalen in diesem
   System keinen auswertbaren Fehler vom Typ `E` oder `A`. Eine Prüfung nur über die
   Return-Tabelle führte deshalb fälschlich zu `SKIP Merkmal vorhanden`. Der finale
   Report prüft die Existenz idempotent über `CABN-ATNAM`.
2. `BAPI_CHARACT_CREATE` verlangt auch bei Datumsmerkmalen einen Eintrag in
   `CHARACTVALUESNUM`. Nur `ADDITIONAL_VALUES = 'X'` genügte nicht und erzeugte
   `C1 040 Bitte Werte einpflegen`. Der Report übergibt deshalb für DATE den Bereich
   `19000101` bis `99991231`.
3. Freie CHAR-Dokumentreferenzen wurden trotz `ADDITIONAL_VALUES = 'X'` ohne
   Wertetabelleneintrag ebenfalls mit `C1 040` abgewiesen. Deshalb wird der neutrale
   erlaubte Wert `-` mit der Beschreibung `Keine Referenz` mitgegeben; weitere
   Referenzwerte bleiben frei eingebbar.
4. SAP-Merkmalskurztexte sind längenbegrenzt. Der Text
   `Lieferantenerklaerung gueltig bis` war für den formalen ABAP-Parameter zu lang und
   wurde auf `Liefererklaerung gueltig bis` gekürzt.
5. Die BAPIs schreiben erst nach `BAPI_TRANSACTION_COMMIT` dauerhaft. Sämtliche
   fehlerhaften Probeläufe vor dem erfolgreichen Abschluss wurden per
   `BAPI_TRANSACTION_ROLLBACK` beendet. Die zunächst nur angekündigten Anlagen waren
   deshalb nicht in der Datenbank vorhanden.
6. Der finale Ablauf arbeitet in zwei Transaktionsphasen: zuerst alle Merkmale und
   deren Commit, danach beide Klassen und deren Commit. Dadurch sind die Merkmale bei
   der Klassenanlage sicher sichtbar. Der Report ist wiederholbar und überspringt
   bereits vorhandene Objekte.
7. `P_WRITE` ist standardmäßig leer. Ohne gesetztes Kennzeichen zeigt der Report nur
   den Katalog. Schreibzugriffe erfolgen ausschließlich mit `P_WRITE = X`.
8. Zusätzlich verhindert eine feste Prüfung von `SY-SYSID` und `SY-MANDT` jede
   Ausführung außerhalb von `T76/090`. Der Report enthält absichtlich keine
   Materialzuordnung und keine P76-Logik.

9. **Nachtrag 18.08.2026, der wichtigste Punkt.** Learning 1 wurde nur für die Merkmale
   umgesetzt, nicht für die Klassen. `create_class` prüfte weiter über
   `BAPI_CLASS_EXISTENCECHECK` mit der Vorbelegung `gv_exists = 'X'`, die nur ein Fehler
   vom Typ `E`/`A` widerlegt. Da dieser BAPI-Typ in diesem System bei fehlenden Objekten
   keinen solchen Fehler liefert, galt die Klasse als vorhanden, `BAPI_CLASS_CREATE` wurde
   nie gerufen und der Report meldete trotzdem `FERTIG`. Regel daraus: In diesem System
   ist **kein** `BAPI_*_EXISTENCECHECK` als Existenzbeweis brauchbar; immer direkt gegen
   die Tabelle prüfen (`CABN` für Merkmale, `KLAH` für Klassen).
10. Ein Programm, das sein eigenes Ergebnis meldet, ist kein Nachweis. Der Report zählt
    seit dem 18.08.2026 nach dem Commit aus `CABN`, `KLAH` und `KSML` zurück und meldet
    `FERTIG` nur bei Übereinstimmung mit dem Soll.

Die Schlussausgabe `FERTIG` galt zunächst als operativer Nachweis für 21 Merkmale und zwei
Klassen. Nach dem Befund vom 18.08.2026 ist diese Schlussfolgerung zurückgezogen: Für die
Merkmale ist sie plausibel, für die Klassen widerlegt der Codebefund sie. Verbindlich ist
erst die Messung nach `docs/PPWR_MANDANT_100_ANALYSE_2026-08-18.md` Abschnitt 5.

## 15. Nachtrag 2026-08-20: Rückmeldung Adil/Florian zu Umfang und Sprache

Ingo hat zwei Kritikpunkte aus einer Mail von Adil an Florian weitergegeben, nachdem die
21 Merkmale bereits in T76/100 angelegt waren (siehe Abschnitt 14 Punkt 9). Entscheidung
von Ingo: **keine Änderung an der SAP-Anlage**, nur hier dokumentiert, damit der Hintergrund
für spätere Rückfragen nachvollziehbar bleibt.

### 15a. „Zu viele Felder"

Adils Mail an Florian nennt acht vereinfachte Attribute für die PPWR-Klassifizierung:
Recyclability Class, Total Recycled Content, PCR Content, Reach Compliant, SVHC Compliant,
PFAS Content (%), Supplier Declaration available, Supplier Declarat. valid until. Adil ist
in diesem Protokoll selbst als Data Owner/SAP für die Anlage geführt; die Mail ist also
vermutlich seine vereinfachte Zusammenfassung fürs Business, nicht ein unabhängiger
Gegenentwurf.

Abgleich gegen die 21 angelegten Merkmale (Abschnitt 4 und 5):

- Fünf der acht Mail-Felder entsprechen 1:1 einem angelegten Merkmal: Recyclability Class,
  Total Recycled Content, PCR Content, Reach Compliant, SVHC Compliant.
- **PFAS Content (%) wurde bewusst NICHT als eigenes Merkmal angelegt** — siehe Abschnitt 4,
  Tabelle der verworfenen Merkmale: „Prozent ist nicht die Einheit der PPWR-Grenzwerte
  (ppb/ppm) und nur für Verpackungen mit Lebensmittelkontakt." Adils Mail-Feld ist damit
  exakt die Variante, die die fachliche Prüfung verworfen hat, nicht ein fehlendes Feld.
  Stattdessen gibt es `ZCOMP_PFAS_STATUS` + `ZCOMP_PFAS_DATE` und dafür `ZPPWR_FOOD_CONTACT`
  als Voraussetzung, ob die Grenzwerte überhaupt greifen.
- „Supplier Declaration available" und „valid until" existieren bei uns **zweimal**
  (`ZPPWR_*` für die Verpackung, `ZCOMP_*` für die Stoffcompliance), weil das zwei
  unterschiedliche Lieferantenerklärungen mit potenziell unterschiedlichem Stand sein
  können.
- Die übrigen zusätzlichen Merkmale ohne Entsprechung in der Mail: je ein eigenes
  Bewertungsdatum für REACH, SVHC, RoHS und PFAS (vier Felder, Begründung Abschnitt 5:
  „ein Datum wäre für vier unterschiedliche Rechts-/Prüfstände mehrdeutig"), zwei
  Dokument-Referenzfelder, `ZPPWR_DATA_DATE` (Datenstand Verpackung) und **RoHS Status +
  Datum**.

**RoHS ist der ehrlichste Kandidat für „zu viel":** RoHS ist keine PPWR-Anforderung, sondern
eine andere EU-Richtlinie (Schadstoffe in Elektro/Elektronik-Geräten). Sie steht trotzdem in
`ZCOMP_STOFF`, weil diese Klasse laut Abschnitt 3 bewusst als „stoffliche Compliance"
allgemein angelegt wurde, nicht als reine PPWR-Klasse. Das war eine bewusste
Scope-Erweiterung der damaligen Sitzung, keine versehentliche Übererfüllung — aber genau
der Punkt, den man vorher mit Adil/Florian hätte abstimmen sollen.

### 15b. „Wieso plötzlich deutsche Bezeichnungen"

Ursache steht bereits in Abschnitt 6 Punkt 2 und 6 dieses Dokuments: „Deutsche Bezeichnung
gemäß den Tabellen eintragen... Keine automatische Übersetzung oder anderssprachige
Kurztexte erfinden. Deutsche Texte sind Pflicht; englische Texte werden nur nach Freigabe
ergänzt." Das war eine bewusste Regel der Sitzung vom 13.08.2026, keine versehentliche
Übersetzung. Adils Mail an Florian verwendet durchgehend englische Fachbegriffe (REACH,
SVHC, PFAS, Recyclability Class); die vorgesehene Freigabe für englische Zusatztexte wurde
nie eingeholt oder mit Florian abgestimmt, deshalb wirkt der produktive Stand für ihn
unangekündigt deutsch.

### 15c. Lehre für künftige SAP-Merkmalsanlagen

Bei international standardisierten Fachbegriffen (PPWR, REACH, SVHC, RoHS, PFAS und
vergleichbar) vor der Anlage die Sprache der Kurztexte mit dem tatsächlichen fachlichen
Ansprechpartner klären, statt pauschal „Deutsch zuerst, Englisch nach Freigabe" zu setzen.
Ebenso den Klassenumfang (z. B. RoHS in einer PPWR-Klasse) vorher kurz rückfragen, wenn er
über die ursprüngliche Anforderung hinausgeht.

**Bewusst nicht umgesetzt:** weder Feldreduktion noch Sprachwechsel der bereits angelegten
Merkmale. Der Stand in T76/100 bleibt wie er ist; P76-Transport bleibt ohnehin gemäß
Abschnitt 10 bis zur Fachfreigabe gesperrt.
