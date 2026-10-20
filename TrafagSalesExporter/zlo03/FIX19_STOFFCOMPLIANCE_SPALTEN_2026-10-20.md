# ZLO03 FIX 19: Spalten REACH, SVHC, PFAS-Gehalt (Wunsch Florian Waechter)

Stand: 2026-10-20. Status: **in T76 aktiv, im Auftrag `T76K912742` zusammen mit FIX 12** (Entscheid Ingo,
Variante B). Gelaufen ist es bisher nur mit leeren Spalten, weil in T76 kein Material Werte hat. Test mit
gepflegten Werten offen. RoHS fehlt, weil es in P76 kein Merkmal dafuer gibt (Adil legt es an, Vorschlag `ROHSCOM`).

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
| RoHS | – | – | – | **gibt es nicht** | folgt, sobald das Merkmal existiert |
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
3. **RoHS:** Adil legt ein Merkmal an (Vorschlag `ROHSCOM`, CHAR 3, COM/UNC/UND, Klasse `Z_PACK_REGULATION`); danach
   eine Spalte RoHS in ZLO03 (eine Zeile in `load_compliance_cache` und eine Spalte in `create_excel_output`).
