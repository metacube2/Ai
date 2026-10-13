# Berechtigung: Abruf gegen Rahmenvertrag 46010612 (Wolfurt) und Vorgehen bei Berechtigungsfaellen

Stand: 2026-10-13. Befund und Uebergangsloesung festgehalten; die saubere Loesung wartet auf
den Hauptadmin (Wochen_Todo Zeile 95). Alles hier wurde **nur lesend** in P76/100 ermittelt,
Claude hat keine Rolle geaendert.

## 1. Der Fall

Mail „WG: DRINGEND Berechtigung für Rahmenauftrag Hinterlegung fehlt“: Sarah Blematl (TRAFAG
GmbH Wolfurt) kann „Artikel für Hainzl 64776 lt. Rahmenvertrag 46010612“ nicht bestellen.
Fabio bat um die Berechtigung fuer Sarah und Alexandra und nannte **BLS** und **GAL**.

**BLS und GAL sind die beiden betroffenen Benutzer** (Blematl Sarah, Alexandra G.), nicht zwei
funktionierende Vorlagen. Ein funktionierender Vergleichsbenutzer (CH, rund 60 Rollen) kam
spaeter von Ingo.

## 2. Befund

### Der Beleg

| Feld | Wert |
| --- | --- |
| Art | **Einkaufs-Mengenkontrakt** (`EKKO-BSTYP K`, `BSART MK`), kein SD-Kontrakt (VA43 kennt ihn nicht) |
| Buchungskreis / Einkaufsorganisation / Einkaeufergruppe | **1200 / 1200 / 120** |
| Lieferant | 70953 Trafag AG, Lieferwerk 1100 (konzernintern CH -> AT) |
| Positionen | 10: Material 53888 DPS160.0PAP1; 20: **Material 64776** 8381 Pressure Transmitter DPS; je 100 ST zu 87.50 EUR, Werk 1200, Lagerort 0001 |
| Angelegt | 08.10.2026 von SUB; deshalb nicht im Testsystem T76 (Kopie rund ein halbes Jahr alt) |

„Hainzl 64776“ ist die **Materialnummer**, kein Kunde. Sarah ruft per ME21N mit Bezug auf den
Kontrakt ab.

### Die Rolle der Betroffenen: `ZD_SD_SUPAUS_ADMIN_RW_AT` (abgeleitet von `ZM_SD_SUPAUS_ADMIN_RW`)

| Objekt | Wert in der AT-Rolle | Fuer 46010612 |
| --- | --- | --- |
| Transaktion ME21N (`AGR_TCODES`) | enthalten | ok |
| `M_BEST_EKO` (Bestellung je EKORG) | 1100 **und 1200**, von Hand gepflegt (Status M), alle Aktivitaeten | ok |
| `M_BEST_WRK` | `$WERKS` = 1200 | ok |
| `M_BEST_BSA` / `M_RAHM_BSA` | `*` | ok |
| **`M_RAHM_EKO`** (Rahmenvertrag je EKORG), Auspraegungen T-T649015100 und T-T649015101 | nur Org-Ebene `$EKORG` = **1100** | **fehlt 1200** |

Org-Ebenen der Rolle (`AGR_1252`): `$BUKRS` 1200, `$EKORG` 1100, `$WERKS` 1200, `$VKORG` 1200.
Die CH-Variante `ZD_SD_SUPAUS_ADMIN_RW_CH` hat `$EKORG` = `*`; deshalb geht es beim CH-Benutzer.

**Ursache (abgeleitet, durch SU53 noch nicht bestaetigt):** Bestellen in EKORG 1200 ist erlaubt,
Rahmenvertraege der EKORG 1200 zu verwenden nicht.

### Gibt es eine fertige Rolle zum Hinzufuegen? (Ingos Regel: Rollen nur zuordnen/entfernen)

Alle Z-Rollen mit `M_RAHM_EKO` am 2026-10-13 (`AGR_1251` + `AGR_1252`):

| Rolle | EKORG | Aktivitaeten | Bewertung |
| --- | --- | --- | --- |
| `ZD_SD_SUPAUS_ADMIN_RW_CH` | * | alle | ganzer CH-Verkauf mit Schreibrecht, zu breit |
| `Z_MM_EINKAUF_RW`, `Z_LOGI_ALLES_RW`, `Z_LOGI_ALLES_RW_TEMP` | 1100 | alle | hilft nicht |
| **`Z_MM_BESTAND_RW`** | * | 03, 08, 09 | **hilft, aber zu breit**: 158 Transaktionen, Warenbewegungen (MB1A/MB1B/MB1C ...) in allen Werken |
| `ZF_BR_BUYER`, `ZF_BR_EMPLOYEE_PROCUREMENT` | * | 03 | Fiori-Geschaeftsrollen fuer alle Buchungskreise, Restinhalt nicht geprueft |
| `ZF_BR_PURCHASER`, `ZF_BR_PURCHASING_MANAGER` | * | alle bzw. 01-06 | volle Einkaeuferrolle, zu breit |
| `ZD_MM_PP_KANBAN_PKMC_1100/_1200`, viele `ZF_BR_*`, `ZZ_005_*` | – | nur F4 | reicht nicht |

**Keine Z-Rolle hat `$EKORG` gezielt = 1200.**

## 3. Entscheid und Loesung

* **Uebergang (Entscheid Ingo 2026-10-13, bis der Hauptadmin zurueck ist):** BLS und GAL in SU01
  zusaetzlich `Z_MM_BESTAND_RW` zuordnen, **mit Enddatum „Gueltig bis“**. Danach testet Sarah den
  Abruf; scheitert er, SU53-Bild holen. Annahme dahinter: der Abruf braucht auf dem Kontrakt nur
  Aktivitaet 03.
* **Saubere Loesung (Hauptadmin):** in PFCG `ZD_SD_SUPAUS_ADMIN_RW_AT`, Objekt `M_RAHM_EKO`
  (beide Auspraegungen) EKORG 1100 **und 1200** von Hand pflegen, wie bei `M_BEST_EKO` schon
  geschehen; Profil generieren, Benutzerabgleich, ueblicher Weg T76 -> Transport -> P76. Danach
  `Z_MM_BESTAND_RW` bei BLS und GAL wieder entfernen.
* Fabio informieren, dass `Z_MM_BESTAND_RW` eine befristete Notloesung mit zu breiten Rechten ist.

## 4. Strategie fuer den naechsten Berechtigungsfall (in dieser Reihenfolge)

1. **Beleg klaeren, bevor man Rollen vergleicht.** Welche Transaktion, welcher Belegtyp? Eine
   Nummer wie 46010612 kann SD-Kontrakt (VA43/VBAK) oder Einkaufskontrakt (ME33K/EKKO) sein; eine
   Zahl neben einem Firmennamen kann Material, Kunde oder Lieferant sein. Zuerst `EKKO`/`VBAK`
   lesen: Buchungskreis, EKORG/VKORG, Werk.
2. **Klaeren, welcher Benutzer betroffen ist.** Von Fachabteilungen genannte Kuerzel sind oft
   die Betroffenen selbst (BLS = Blematl Sarah). Nachfragen statt annehmen.
3. **SU53 vom Betroffenen** direkt nach dem Fehler ist der kuerzeste Weg und bestaetigt jede
   Ableitung. Ohne SU53 ist das Ergebnis nur abgeleitet.
4. **Transaktion pruefen:** `AGR_TCODES` mit TCODE = ... zeigt, welche Rollen sie enthalten.
   Hat der Betroffene sie schon, liegt es an einem Org-Wert, nicht an der Transaktion.
5. **Objekte und Werte pruefen:** `AGR_1251` (AGR_NAME, OBJECT, FIELD) liefert die Werte; steht
   dort `$EKORG`, `$WERKS` usw., kommt der echte Wert aus `AGR_1252` (AGR_NAME, VARBL). Status
   `M` = von Hand gepflegt, `S` = Standard, `G` = geaendert. SAP prueft jedes Objekt ueber alle
   Rollen des Benutzers hinweg.
6. **Fehlendes Objekt suchen:** `AGR_1251` mit `AGR_NAME Z*`, OBJECT, FIELD = EKORG bzw. ACTVT;
   dann `AGR_1252` fuer die `$`-Werte. Breite der Kandidaten ueber `AGR_TCODES` (Anzahl
   Transaktionen) und Org-Ebenen beurteilen.
7. **Ingos Regel:** Rollen nur zuordnen oder entfernen, nicht selbst anpassen. Rollenaenderungen
   macht der Hauptadmin. Eine zu breite Uebergangsrolle immer mit Enddatum zuordnen.

Einkaufsobjekte zum Merken: `M_BEST_BSA/EKG/EKO/WRK` (Bestellung), `M_RAHM_BSA/EKG/EKO/WRK`
(Rahmenvertrag), `M_BANF_*` (Bestellanforderung), `M_ANFR_*` (Anfrage). Vertrieb: `V_VBAK_VKO`,
`V_VBAK_AAT`.

## 5. Werkzeug: lesend in P76 ueber die angemeldete GUI

Die Skripte unter `.tmp_sap_probe` sperren P76 absichtlich (Guard auf T76). Fuer reines Lesen in
P76 gibt es seit 2026-10-13:

| Datei | Zweck |
| --- | --- |
| `.tmp_sap_probe/P76Lesen.vbs` | oeffnet mit `neu:<TX>` eine **neue** P76-Sitzung (nur Anzeige-TX: SE16, SE16N, SUIM, SU01D, ME33K, VA43, PFCG) oder nutzt `ses:<Id>`; Aktionen `set:<id>=<wert>`, `key:<vkey>`, `ok:/nSE16`, `dump`, `dump1`/`key1:` fuer Popups, `sleep:`. Ingos Sitzungen werden nicht angefasst |
| `.tmp_sap_probe/P76SitzungSchliessen.vbs <SitzungsId>` | schliesst die eigene Lesesitzung, nur wenn sie SE16/VA43/ME33K zeigt |
| `.tmp_sap_probe/SapListeZeilen.py <datei> [muster]` | baut aus dem `dump` einer SE16-Liste Zeilen (`|`-getrennt); mit Muster auch ueber mehrere Seiten (`key:82` zwischen den `dump`s) |

Beispiel (PowerShell, aus der Repository-Wurzel):

```powershell
cscript //nologo .tmp_sap_probe\P76Lesen.vbs neu:SE16 `
  set:wnd[0]/usr/ctxtDATABROWSE-TABLENAME=AGR_1251 key:0 `
  set:wnd[0]/usr/ctxtI1-LOW=Z* set:wnd[0]/usr/txtI3-LOW=M_RAHM_EKO set:wnd[0]/usr/txtI6-LOW=ACTVT `
  set:wnd[0]/usr/txtMAX_SEL=500 key:8 dump key:82 dump key:82 dump > out.txt
python .tmp_sap_probe\SapListeZeilen.py out.txt M_RAHM_EKO
```

Feld-Ids der SE16-Selektionsbilder (P76, 2026-10-13):

| Tabelle | Felder |
| --- | --- |
| `AGR_TCODES` | `ctxtI1` AGR_NAME, `ctxtI2` TYPE, `txtI3` TCODE |
| `AGR_1251` | `ctxtI1` AGR_NAME, `txtI3` OBJECT, `txtI4` AUTH, `txtI6` FIELD, `txtI7` LOW |
| `AGR_1252` | `ctxtI1` AGR_NAME, **`ctxtI3`** VARBL, `txtI4` LOW |
| `EKKO` | Popup „Felder fuer Selektion auswaehlen“ (mehr als 40 Felder): `key1:0` uebernimmt die Vorauswahl, danach `ctxtI1` EBELN |

Fallen:

* Ein `dump` sieht nur den sichtbaren Bildausschnitt. Mehr Zeilen: `key:82` (Bild ab) und
  erneut `dump`, dann mit Muster auswerten; ohne Muster ueberschreiben sich die Seiten.
* Ist die Selektion falsch (z. B. `txtI4` statt `ctxtI3` bei AGR_1252), bleibt SE16 auf dem
  Selektionsbild mit „keine Tabelleneintraege“; das sieht aus wie „nichts gefunden“.
* SE16 hat bis zu sechs Sitzungen je Verbindung; Ingo hatte vier offen. Eigene Sitzung am Ende
  schliessen.
* Der Auto-Modus von Claude Code hat SE16 und `RunSapProbe.ps1` gegen Geschaeftsdaten mehrfach
  abgelehnt („Production Reads“), auch in T76; erst nach Ingos ausdruecklichem Auftrag und
  Moduswechsel lief es. Nicht umgehen, Ingo fragen.
