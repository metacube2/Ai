# PPWR und Stoffcompliance im SAP – Anlageprotokoll für Adil

Stand: 13.08.2026  
System für Aufbau und Pilot: **T76, Mandant 090**  
Status: Technische Anlage in T76/090 abgeschlossen; keine Freigabe für P76 oder Massenpflege

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
| 13.08.2026 | T76/090 | Klassenart `001`: `ZPPWR_PACKMITTEL` und `ZCOMP_STOFF` angelegt | beide Klassen mit den vorgesehenen Merkmalen erfolgreich angelegt und committed |
| offen | T76/090 | Pilotmaterialien zuordnen und CL30N-Abnahme | noch nicht ausgeführt |
| gesperrt | P76 | Transport/Verteilung und Massenpflege | erst nach Fachfreigabe |

Technischer Nachweis: Die abschließende SAP-Ausgabe meldete für jedes Merkmal
`wird angelegt`, für beide Klassen `wird angelegt` und abschließend
`FERTIG: Merkmale und Klassen angelegt/geprueft.` Der wiederholbare Quellcode liegt
unter `docs/abap/ZPPWR_CLASS_SETUP.abap`. Der Report enthält eine feste Systemsperre
für alle Systeme und Mandanten außer T76/090.
