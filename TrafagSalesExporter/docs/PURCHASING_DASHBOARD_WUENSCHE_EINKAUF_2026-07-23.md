# Einkaufs-Dashboard — Wuensche aus der Einkaufssitzung 2026-07-23

## Umsetzungsstand 2026-07-23 (SAP-Erweiterung transportiert, C#-Ladestrecke angepasst)

Alle neuen SAP-Felder am 2026-07-23 live gegen travp762 verifiziert:

| Feld | OData | Live-Status | C#-Ladestrecke | Datenlage Werk 1100 |
| --- | --- | --- | --- | --- |
| Lieferantenland | `LFA1Set.Land1` | OK (z.B. CH) | -> `PurchasingEkkoCache.SupplierCountry` | gefuellt |
| Warengruppe Stamm | `MARA001Set.Matkl` | OK (schon 07-23 frueh) | -> `MaraMatkl` | 65 % leer, 24 % `01`, ~10 % echt |
| ABC | `MARCSet.Maabc` | OK | -> `MaraAbc` | 86 % leer; A=438/B=742/C=8136 |
| XYZ | `ZSTR_MAT_XYZSet.Maxyz` (eigenes Set, mein Rumpf) | OK | -> `MaraXyz` | Set hat 4'388 Materialien, davon 99 % klassifiziert (Z 69/Y 16/X 14) |
| Disponent Kopfmaterial | `ZSTR_LZCODE_USAGE.VknrDispo` | **FEHLT** | noch nicht | Struktur-Feld `VKNR_DISPO` in SE11 noch nicht angelegt |

- C#-Ladestrecke (`PurchasingDataRefreshService`) liest Land1 (LFA1), ABC (MARCSet, ungepaged +
  client-seitiger Werk-1100-Filter, weil MARCSet $top/$skip/$filter ignoriert) und XYZ (eigenes
  Set, paged). Neue additive Cache-Spalten `SupplierCountry`, `MaraAbc`, `MaraXyz`. Die Felder
  FUELLEN sich erst mit dem naechsten Einkauf-Full-Load (mit Marco/Andreas abstimmen).
- UI/Visuals (Region-Kuchen, ABC/XYZ-Sichten, mehrstufiger Aufriss) sind noch NICHT gebaut -
  bewusst, weil Marco eine Sicht nach der anderen abnehmen will und die Daten erst nach dem Full
  Load da sind.
- **VknrDispo bleibt offen**: SE11-Struktur `ZSTR_LZCODE_USAGE` braucht das Feld `VKNR_DISPO`
  (Datenelement `DISPO`), dann den ZLO03-USAG-Methodenrumpf (Version 22b, hat die vknr_dispo-Zeile
  schon) erneut aktivieren. Erst danach traegt der Produktgruppen-Aufriss.


Quelle: Whisper-Transkript einer Einkaufssitzung (Ingo, Marco, Armin), Modell `medium`,
Audio `…/einka/Data/audio.wav`. Diskussionsstand, KEINE finalisierte Spezifikation.
Leitplanke Marco: **eine Sicht nach der anderen fertig machen, nicht verzetteln** — zuerst
der Reiter Spend.

## Grundkonzept: Perspektiven vs. Aufriss (Drilldown/Kaskadierung)

Marco trennt zwei Dinge sauber:

- **Perspektive** = der Standpunkt, von dem aus man auf die Zahlen schaut (Lieferant,
  Warengruppe, Produktgruppe, Region, Artikel, ABC/XYZ). Das sind die verschiedenen
  Register/Sichten.
- **Aufriss / Drilldown / Kaskadierung** = innerhalb einer Sicht Detaildaten stufenweise
  aufklappen. Beispiel aus der Sitzung: alle Lieferanten sehen -> auf CPT filtern/aufklappen
  -> innerhalb CPT den Spend nach Produktgruppe X aggregiert sehen -> ggf. noch eine Stufe
  tiefer. Also mehrstufig aufklappbar (Pivot-artig), wieder zuklappbar.

Heutiger Stand in der App: Der Reiter Spend hat die Matrix „Kaskadierung Lieferant / Jahr"
mit EINER Aufklapp-Ebene (Lieferant -> Warengruppe). Der Wunsch ist eine flexiblere,
tiefere Kaskadierung und dieselbe Mechanik auch fuer andere Einstiegsdimensionen.

## Dimensionen fuer den Aufriss (mit Status/Quelle)

| Dimension | Quelle laut Sitzung | Status in der App | Bemerkung |
| --- | --- | --- | --- |
| Zeit / Periode | EKKO.Bedat | **funktioniert** (Zeitschalter bestaetigt) | Basis-Filter, wirkt auf alle Ebenen |
| Lieferant / Kreditor | Beleg (EKKO.Lifnr) + LFA1-Name | **live** | aus Beleg korrekt; Material kann bei mehreren Kreditoren laufen, aber wenn man auf den Kreditor aufreisst, interessiert nur dessen Anteil |
| Warengruppe / Materialgruppe (MATKL) | **Materialstamm** MARA-MATKL, NICHT Beleg | **2026-07-23 umgesetzt** (Loader liest MARA001Set.Matkl) | Marco bestaetigt exakt diesen Weg: „im Materialstamm abfragen und nicht auf dem Beleg", weil alte Belege nur die Dummy-Warengruppe haben. ACHTUNG Datenlage: MARA-MATKL ist im Stamm zu ~65 % leer + ~24 % `01`; nur ~10 % echte Gruppen. Fuellung ist ein SAP-Stammdaten-Thema. |
| Beschaffungsregion / Land | Land des Lieferanten (LFA1) | **fehlt** (LFA1 laedt nur Name1, nicht Land1; kein Feld im Cache) | Wunsch: Kuchendiagramm je Materialgruppe/Warengruppe -> Anteil je Region/Land. Braucht LFA1.Land1 im Loader + Cache-Spalte. |
| Produktgruppe | ueber Disponenten-Gruppe (CC23/ZC23) bzw. ZLO03-Verwendungsnachweis | **offen, komplex** | siehe eigener Abschnitt unten |
| Materialnummer / Artikel | EKPO.Matnr/Txz01 | **live** (unterste Aufriss-Stufe) | |
| ABC / XYZ | ABC = `MARC-MAABC`; XYZ = eigene Tabelle `ZCA_MAT_ABC_XYZ`, Feld `/ITS/CA_M_MAXYZ` | **Quellen 2026-07-23 live gefunden** | siehe eigener Abschnitt unten |

## Produktgruppe — der schwierige Punkt

Kernproblem: Auf **Komponenten-/Einkaufsteil-Stufe** gibt es keine direkte
Produktgruppen-Zuordnung. Die Produktgruppe haengt im Trafag-Modell am **Disponenten**
(gepflegt in CC23/ZC23), aber Einkaufskomponenten tragen den Disponenten nicht als
Produktgruppe.

Zwei diskutierte Loesungswege:

1. **Ueber die Stuecklisten-Verwendung (ZLO03).** Eine Komponente wird in Stuecklisten
   bestimmter Produkte/Produktgruppen verwendet. Ueber den ZLO03-Bottom-Up-Verwendungs-
   nachweis (der gerade als Webservice `ZSTR_LZCODE_USAGE`/`_PARENT` gebaut wurde!) laesst
   sich eine Komponente den Produktgruppen zuordnen, in deren Stuecklisten sie vorkommt.
   FALLE: Komponenten wie Schrauben/Schieber kommen in vielen Produkten/Gruppen vor ->
   Abgrenzung/Zurechnung des Spends ist nicht eindeutig (aufwendige Analyse noetig).
2. **Referenzliste im Hintergrund.** Ein z.B. woechentlich laufender Job loest die
   Stuecklisten auf und schreibt je Komponente ein Kennzeichen/eine Referenzliste
   (Komponente -> Disponent/Produktgruppe). Das Dashboard liest dann nur die fertige Liste
   (unkritisch fuer die Ladezeit). Marcos Idee, um den teuren ZLO03-Lauf aus dem
   Online-Pfad herauszuhalten.

Einordnung: nutzt direkt den heute fertiggestellten ZLO03-Webservice, ist aber die
aufwendigste Dimension (Mehrfachverwendung, Zurechnungslogik). Bewusst NICHT als erstes.

### ZLO03-Feld fuer die Produktgruppe (2026-07-23b, an travp762 verifiziert)

Fuer den Produktgruppen-Aufriss braucht der ZLO03-Webservice GENAU EIN neues Feld:
**`VKNR_DISPO`** = Disponent des Kopfmaterials (`MARC-DISPO`, Werk 1100). Der ABAP-Rumpf
`docs/abap/ZSTR_LZCODE_USAG_GET_ENTITYSET.abap` ist bereits angepasst (DISPO im MARC-SELECT,
in `lt_stamm`, `VknrDispo` in Schritt 7/9 gesetzt). SAP-seitig noch noetig: DDIC-Struktur
`ZSTR_LZCODE_USAGE` in SE11 um Feld `VKNR_DISPO` (Datenelement `DISPO`) erweitern, dann Rumpf
einfuegen. Live verifiziert: die Bottom-Up-VKNRs einer Komponente sind FERT-Endprodukte und
haben MARC-DISPO gefuellt (Beispiel Disponent `019`).

Das Feld allein macht die Produktgruppe NICHT fertig - es fehlt weiterhin:
1. Referenzliste **Disponent -> Produktgruppe** (ZC23) als lesbare Daten (Client-/Job-seitig).
2. Zurechnungsregel fuer Komponenten, die in mehreren Produktgruppen verbaut sind
   (Marco: „wird nicht ganz einfach sein mit der Abgrenzung").

## ABC/XYZ — Quellen (2026-07-23 am Live-System travp762 verifiziert)

- **ABC = SAP-Standard**: `MARC-MAABC` (ABC-Kennzeichen, werkabhaengig). Ingo hat `Maabc` in ein
  MARC-EntitySet (`MARCSet`) aufgenommen - kommt mit dem naechsten Transport auf P (aktuell noch
  404 auf `Maabc`, wie erwartet).
- **XYZ = NICHT SAP-Standard**, sondern ein Add-on im `/ITS/`-Namensraum. Standard-SAP hat KEIN
  XYZ-Feld - Internet-/Standarddoku hilft hier nicht. Live gefundene Quelle:
  - **Tabelle `ZCA_MAT_ABC_XYZ`** (transparent, TRANSP), Schluessel `MANDT, MATNR, WERKS`.
  - **XYZ-Kennzeichen: Feld `/ITS/CA_M_MAXYZ`** (CHAR 1, Datenelement `/ITS/CA_MAT_ABC_MAXYZ_D`).
    Stichprobe Werk 1100: gefuellt mit `Y`/`Z` (und `X`). Zusaetzlich fuehrt die Tabelle die
    Analyse-Zeitraeume (Von/Bis Monat+Jahr) je ABC und XYZ.
  - Das ist Marcos „ITSCH-MAT-ABC-XYZ" aus der Sitzung (die `/ITS/CA_MAT_ABC_XYZ_*`-Objekte sind
    nur Strukturen/ALV; die Daten liegen in `ZCA_MAT_ABC_XYZ`). Marcos bestehender Report nutzt
    dieselbe Tabelle.
- **Fuer die App exponieren - EMPFEHLUNG: eigenes, schlankes XYZ-Set** (nicht ins MARC-Set). Grund:
  MARCSet liest die Standardtabelle MARC automatisch; XYZ liegt in einer ANDEREN Tabelle
  (`ZCA_MAT_ABC_XYZ`). Es ins MARC-Set zu holen wuerde dessen Auto-Read ueberschreiben und das
  bereits funktionierende `Maabc` gefaehrden. Ein eigenes Set ist risikofrei; die C#-Seite fuehrt
  ABC (MARCSet.Maabc) und XYZ (eigenes Set) ueber die Materialnummer zusammen (gleiches Muster wie
  die MARA001Set-Statusmap). Fertiger Methodenrumpf + SE11-/SEGW-Anleitung:
  `docs/abap/ZSTR_MAT_XYZ_GET_ENTITYSET.abap`. Struktur `ZSTR_MAT_XYZ` (Matnr/Werks/Maxyz), Set-
  Name loest die C#-Seite dynamisch auf.

## Visualisierung (Wunsch)

- **Kuchendiagramm** je Materialgruppe/Warengruppe -> Anteil je **Beschaffungsregion/Land**
  (setzt LFA1.Land1 voraus).
- Bestehende Balken/Matrix bleiben; der Aufriss ist die Hauptneuerung.

## Vorgeschlagene Reihenfolge (zur Abstimmung mit Marco/Ingo)

1. **Warengruppen-Aufriss scharf schalten**: MARA-MATKL ist geladen (heute), sobald ein
   Einkauf-Full-Load gegen travp762 gelaufen ist. Danach zeigt das neue „Volumen nach
   Warengruppe"-Diagramm + die Matrix-Drilldown echte Stamm-Warengruppen. Zuerst diese eine
   Sicht mit Marco abnehmen (seine Leitplanke).
2. **Beschaffungsregion**: LFA1.Land1 in Loader + Cache aufnehmen, dann Kuchendiagramm
   Region je Warengruppe. Kleiner, klar abgegrenzter Ausbau.
3. **Mehrstufiger Aufriss**: Spend-Matrix von einer auf zwei aufklappbare Ebenen erweitern
   (z.B. Lieferant -> Warengruppe -> Artikel), plus Einstieg auch ueber andere Dimension.
4. **ABC/XYZ**: MARC-MAABC (Sicht O2) + XYZ-Tabelle anbinden (Marcos Report als Vorlage).
5. **Produktgruppe** (aufwendigste): ZLO03-Verwendungsnachweis + Referenzliste
   (Komponente -> Produktgruppe), moeglichst als Hintergrund-Job.

Offene Zuarbeit Einkauf (aus der Sitzung): Disponenten-/Produktgruppen-Zuordnung aus
CC23/ZC23 als Referenzliste; Klaerung, welcher konkrete Dashboard-Nutzen ABC/XYZ haben soll.
