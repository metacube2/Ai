# ZLO03 FIX 19: Spalten RoHS, REACH, SVHC, PFAS-Gehalt (Wunsch Florian Waechter)

Stand: 2026-10-22. Status: **in T76 aktiv, im Auftrag `T76K912742` zusammen mit FIX 12** (Entscheid Ingo,
Variante B). **Nachtrag 2026-10-22: Spalte RoHS** aus dem Merkmal `ROHS`, das Adil am 21.10. angelegt hat (Abschnitt 6).
Gelaufen ist es bisher nur mit leeren Spalten, weil in T76 kein Material Werte hat. Test mit gepflegten Werten offen.

*Ueberholt am 2026-10-22:* Hier stand, RoHS fehle, weil es kein Merkmal gebe (Vorschlag `ROHSCOM`). Adil hat das
Merkmal unter dem Namen `ROHS` angelegt; ZLO03 gibt es seit dem 22.10. aus.

## 1. Anfrage

Florian Waechter an Fabio, 20.10.: ZLO03 soll das Feld „RoHs/Reach/PFAS“ ausgeben. Auf Ingos Rueckfrage, in welcher
Tabelle das liegt, antwortete Florian, Ingo habe die Felder vor einem Monat angelegt und Fabio habe sie befuellt.

## 2. Befund je Feld (gelesen 20.10., T76/100 und P76/100, nur lesend)

Die Felder sind kein Materialstammfeld, sondern **Merkmale der Klassifizierung** (Klassenart `001`, Objekt Material).
Werte stehen in `AUSP`, Merkmale in `CABN`/`CABNT`/`CAWN`, Klassen in `KLAH`/`KSML`, Zuordnungen in `KSSK`.

In P76 gibt es nur die Klasse **`Z_PACK_REGULATION`**, 362 Materialien zugeordnet (290 H-, 34 C-, 19 R-Nummern, Rest
D/E/K/V). Unsere PPWR-Klasse `ZCOMP_STOFF` (mit RoHS) gibt es nur in T76, ohne ein einziges Material; sie wurde nie
transportiert. Die interne Merkmalsnummer `ATINN` ist in T76 und P76 verschieden (zum Beispiel `REACHCOM` 1410 in T76,
1413 in P76), die Klasse wurde in P76 also separat angelegt.

| Merkmal P76 | Bezeichnung | Format | Werte | an Materialien (P76) | in ZLO03 |
| --- | --- | --- | --- | --- | --- |
| `REACHCOM` | Reach Compliant | CHAR 3 | COM / UNC / UND | 53, alle COM | Spalte **REACH** |
| `SVHCCOMP` | SVHC Compliant | CHAR 3 | COM / UNC / UND | 141, alle COM | Spalte **SVHC** |
| `PFAS` | PFAS Content | NUM 3 | frei | 168, alle 0 | Spalte **PFAS-Gehalt** |
| RoHS | – | – | – | **gab es am 20.10. nicht** | seit 22.10. Spalte **RoHS**, Merkmal `ROHS` (Abschnitt 6) |
| `RECYCLAB` | Recyclability Class | CHAR 8 | A bis E | 5 | nicht ausgegeben |
| `RECYCONT` | Total Recycled Content | NUM 3 | frei | 167 | nicht ausgegeben |
| `PCR` | PCR Content | NUM 3 | frei | 169 | nicht ausgegeben |
| `SUPPLDEC` | Supplier Declaration available | CHAR 1 | N / Y | 1 | nicht ausgegeben |
| `SUPPLDECVD` | Supplier Declarat. valid until | DATE | – | 1 | nicht ausgegeben |

Die acht P76-Merkmale entsprechen genau Adils vereinfachter Feldliste aus seiner Mail an Florian (August), siehe
`docs/PPWR_SAP_KLASSIFIZIERUNG_ANLAGEPROTOKOLL_2026-08-13.md` Abschnitt 15a. RoHS stand dort nicht drin; in unserer
Klasse `ZCOMP_STOFF` war es zusaetzlich aufgenommen (`ZCOMP_ROHS_STATUS`). PFAS als Prozentfeld hatte die damalige
fachliche Pruefung bewusst verworfen (Einheit ppb/ppm, nur Lebensmittelkontakt); ZLO03 zeigt den Wert so, wie er gepflegt ist.

Lesewerkzeuge: `.tmp_sap_probe/P76Pruefung_ZCOMP_Stoff.ps1` und `P76Pruefung_PackRegulation.ps1` (Start
`P76Pruefung_ZCOMP_Start.cmd`, Passwort tippt Ingo).

## 3. Umsetzung in `Z_ZLO03_TURBO2`

- Neue FORM `load_compliance_cache`: sucht die Merkmale **ueber den Namen** in `CABN` (`REACHCOM`, `SVHCCOMP`, `PFAS`),
  liest `AUSP` mit `KLART = '001'`, `MAFID = 'O'`, `LKENZ = space` fuer alle Zeilenmaterialien, jeweils in beiden
  Schreibweisen (wie geschrieben und ueber `CONVERSION_EXIT_MATN1_INPUT`, weil `KOMPNR` keinen Konvertierungsexit hat).
- Neue FORM `get_compliance`: Zugriff je Zeile, erst wie geschrieben, dann in interner Schreibweise.
- `create_excel_output`: Kopf `REACH`, `SVHC`, `PFAS-Gehalt` direkt hinter `Postyp`; je Datenzeile die Werte des
  Zeilenmaterials (Top-Down: Komponente, Bottom-Up: Zeilenmaterial). Leer heisst „nicht gepflegt“. PFAS ohne
  nachlaufende Nullen (`0`, `0.5`).
- Die Spalten „Weitere VKNR“ ruecken um drei nach hinten. Feste Spaltenpositionen gab es dort nie, weil die Zahl der
  VKNR-Spalten je Lauf schwankt.
- Quelltext: `Z_ZLO03_TURBO2_FIX19_2026-10-20.abap` (Vorstand: `Z_ZLO03_TURBO2_FIX12_2026-10-16.abap`), 164 Zeilen mehr.

## 4. Einspielen und Test in T76

| Schritt | Ergebnis |
| --- | --- |
| Mandant 200 | abgewiesen: Objekt gesperrt in Aufgabe `T76K912743` (Mandant 100) |
| Aendern in T76/100, eigene Sitzung | ohne Auftragsabfrage, Objekt liegt schon in `T76K912743` |
| Aktivieren | nur `Z_ZLO03_TURBO2` markiert (`Z_REICHW` uebergangen), „Objekt(e) wurde(n) aktiviert“, REPOSRC `A` 20.10. 08:51:58 |
| Rueckgelesen | identisch mit der Patchdatei (3'523 Zeilen) |
| Lauf Top-Down `8475*` | 123 Datenzeilen wie beim FIX-12-Test, B63485 enthalten, Kopf und Datenzeilen 181 Spalten, neue Spalten an der richtigen Stelle, alle leer (T76 ohne Werte) |

Falle beim Lauf: Die Sicherheitsabfrage „SAP GUI Sicherheit“ beim Speichern ist ein Windows-Fenster und fuer das
Skripting unerreichbar; Ingo klickt „Zulassen“ (ohne „Entscheidung merken“).

## 5. Offen

1. **Test mit Werten:** entweder in T76 an B63485 eine Testklassifizierung `Z_PACK_REGULATION` anlegen (Entscheid Ingo
   offen) oder nach dem Import in P76 an einem gepflegten Material pruefen (zum Beispiel H05256: REACH COM, SVHC COM, PFAS 0).
2. **Import:** `T76K912742` bringt jetzt FIX 12 **und** FIX 19 nach P76.
3. ~~**RoHS:** Adil legt ein Merkmal an~~ *Erledigt am 2026-10-22*, siehe Abschnitt 6.
4. **RoHS in P76:** ob das Merkmal `ROHS` in P76 schon existiert, ist ungeprueft (P76 nur auf Auftrag Ingo). Das Programm
   sucht die Merkmale ueber den Namen; fehlt `ROHS` in P76, bleibt die Spalte dort leer, ohne Fehler.

## 6. Nachtrag 2026-10-22: Spalte RoHS

Adil meldete am 22.10. per Mail an Ingo und Fabio („erledigt“), Weiterleitung von Florians Wunsch „RoHs/Reach/PFAS“.

Befund T76/100, nur lesend per SapProbe:

| Tabelle | Inhalt |
| --- | --- |
| `CABN` | `ROHS`, ATINN 1438, CHAR 3, angelegt 21.10.2026 (Name `ROHS`, nicht der Vorschlag `ROHSCOM`) |
| `CABNT` | „RoHS Compliant“ |
| `CAWN` | COM, UNC, UND (wie REACH und SVHC) |
| `KSML` | Klasse `Z_PACK_REGULATION` (CLINT 489), Position 009 |
| `AUSP` | noch kein Material mit Wert |

Daneben gibt es in T76 die alten Merkmale `ZCOMP_ROHS_STATUS` und `ZCOMP_ROHS_DATE` der nie transportierten Klasse
`ZCOMP_STOFF`; ZLO03 liest sie nicht.

Aenderung (`*** FIX 19`-Kommentar ergaenzt, Quelltext `Z_ZLO03_TURBO2_FIX19_ROHS_2026-10-22.abap`, 8 Zeilen mehr als der
Stand vom 20.10.): Feld `rohs` in `ty_compl`, `ROHS` in der Merkmalsliste von `load_compliance_cache` und im `CASE`,
Kopf `RoHS` **vor** `REACH` (Reihenfolge wie in Florians Mail), Wert je Datenzeile.

| Schritt | Ergebnis |
| --- | --- |
| Vorher | `REPOSRC` aktiv 20.10. 08:51:58, Repo-Datei identisch mit dem damals Rueckgelesenen |
| Sitzung | `CreateSession` legte keine Sitzung an (ohne Fehler); Ingo hat eine eigene T76-Verbindung geoeffnet, Sitzungs-Id `/app/con[1]/ses[0]` |
| Aendern, Sichern | ohne Auftragsabfrage (Objekt in `T76K912743`) |
| Aktivieren | nur `Z_ZLO03_TURBO2` markiert, `Z_REICHW` uebergangen; „Objekt(e) wurde(n) aktiviert“, `REPOSRC` A 22.10. 08:00:06 |
| Rueckgelesen | identisch mit der Patchdatei (3'531 Zeilen) |
| Lauf Top-Down `8475*` | 123 Datenzeilen, B63485 enthalten, Kopf und Datenzeilen 182 Spalten (vorher 181), Spalten RoHS, REACH, SVHC, PFAS-Gehalt hinter `Postyp`, alle leer (T76 ohne Werte) |
| Vergleich mit Lauf 20.10. | ohne die neue Spalte gleich bis auf Stammdaten, die sich in T76 seither geaendert haben (Sicherheitsbestand 39 Zeilen, ein Materialstatus, eine Bezeichnung) |
