# GIB Dispo-Cockpit in T76: Bestand und Testuser UBIG

Stand: 2026-10-19 (Claude im Auftrag von Ingo)

Zusammenfassung, was das Add-on fachlich macht: `docs/GIB_DispoCockpit_DCP_Zusammenfassung_2026-10-19.docx`.
Laut Ingo (19.10.) sind die Module installiert und die Pilotphase ist abgeschlossen.

## 1. Was in T76 vorhanden ist (gelesen 19.10., Mandant 200)

- Namensraeume: `/GIB/`, `/GIBKPI/`, `/GIBKPIUI/`, `/GIBPR/`, `/GIBSCM/`, `/GIBSCMUI/`.
- Transaktionen der Module DCB, DCC, DCF, DCO und DCP. Einstieg DCP ist `/GIB/DCP`, dazu gehoeren unter
  anderem `/GIB/DCPN`, `/GIB/DCP_DASH`, `/GIB/DCP_SLOTP`, `/GIB/DCP_RELEASE` und `/GIB/DCP_CUST`
  (insgesamt 57 Transaktionen `/GIB/DCP*`).
- Rollen, alle als Auslieferungsrollen im Namensraum `/GIB/`, keine Z-Kopien:

| Rolle | Kurzbeschreibung |
| --- | --- |
| `/GIB/DCP_ADMIN` | GIB Manufacturing Administrator |
| `/GIB/DCP_SUPERVISOR` | GIB Manufacturing Supervisor |
| `/GIB/DCP_FOREMAN` | GIB Manufacturing Foreman |
| `/GIB/DCP_SKILLED_WORKER` | GIB Manufacturing Skilled Worker |
| `/GIB/DCP_MACH_OP` | GIB Manufacturing Maschine Operator |
| `/GIB/DCP_FOX_USER` | GIB Manufacturing FOX Controller |
| `/GIB/DCP_FOX_ADMIN` | GIB Manufacturing FOX Administrator |

Weitere GIB-Rollen: DCB/DCC/DCF/DCO/DCV je Admin und User, `/GIB/UI_USER` (SCX UI), `/GIB/DC_RAC_USER`,
`/GIB/DC_ADMIN`, `/GIB/SUPPORT`, `/GIBKPI/ADMIN`, `/GIBKPIUI/USER`.

## 2. Testuser UBIG (angelegt 19.10.2026, T76 Mandant 200)

- Auftrag Ingo: neuer User nur mit GIB-DCP-Rechten. Rolle gewaehlt von Ingo: `/GIB/DCP_SUPERVISOR`.
- Angelegt in SU01 in Claudes eigener Sitzung: Benutzertyp Dialog, Nachname „GIB DCP Test“, Abteilung
  „Testuser GIB DCP Supervisor“, keine Gueltigkeitsgrenze, keine weiteren Rollen oder Profile.
- Rolle gueltig 19.10.2026 bis 31.12.9999, Status „Benutzerstamm abgeglichen“, Profil `T-P4550180`
  (generiert, „Profil zur Rolle /GIB/DCP_SUPERVISOR“).
- Kennwort: von SAP zufaellig erzeugt und niemandem bekannt. **Ingo setzt das Initialkennwort selbst**
  (SU01, Kennwort aendern), erst dann ist eine Anmeldung moeglich.

Offen:

1. Ingo: Initialkennwort setzen, mit UBIG anmelden, `/GIB/DCP` muss gehen, eine Standardtransaktion
   wie MM02 muss abgewiesen werden.
2. Ob die Rolle allein reicht oder fuer die Oberflaeche zusaetzlich `/GIB/UI_USER` oder
   `/GIB/DC_RAC_USER` noetig ist, zeigt erst der Test (SU53 bei einer Abweisung).
3. Mittelfristig: Auslieferungsrollen werden ueblicherweise nach Z kopiert. Das ist eine
   PFCG-Aenderung und Sache des Hauptadmins oder des GIB-Projekts.

## 3. Stolperstein Lesewerkzeug

SapProbe `table-read` (RFC_READ_TABLE) lieferte in T76/200 fuer `AGR_USERS`, `AGR_TEXTS`, `AGR_1016`,
`AGR_TCODES` und `USR02` immer `TABLE_WITHOUT_DATA`, auch fuer Ingos eigene Rollen. Ausserdem
scheiterte `LIKE '/GIB/%'`, waehrend `LIKE '/GIB%'` ging. Rollentexte und Zuordnungen deshalb in der
GUI gelesen (PFCG-Einstieg zeigt die Kurzbeschreibung, SU01-Rollengrid zeigt Status und Profil).
