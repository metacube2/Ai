# ZLO03: Was „Exklusiv“ bedeutet

Stand: 2026-10-08. Anlass: Frage von Ann-Katrin Michel (Mail „Verständnisfrage Exklusiv-Komponenten“, 08.10.2026).
Top-Down meldete für die VKNR 44902 und 43457 unter anderem den Print B63491 als exklusiv. Bottom-Up zeigte für
B63491 aber rund 250 VKNR an.

Quelle ist der Quelltext von `Z_ZLO03_TURBO2` in T76, per RFC gelesen. Massgebend sind die Forms
`load_exclusivity_topdown` und `load_global_exclusivity`.

**Befund am Rand:** Die Transaktion ZLO03 startet in T76 laut `TSTC` das Programm `Z_ZLO03_TURBO2`, nicht mehr
`ZM_LZCODE20_OPT`, wie es in `zlo03/CLAUDE.md` steht. Welches Programm in P76 hinter ZLO03 steht, ist nicht geprüft.

## Regel Top-Down

Für jede Komponente liest das Programm alle VKNR, die sie laut `ZPOWERBI_VC_TXT` enthalten. Drei Gruppen davon
zählen nicht mit:

1. VKNR aus der eigenen Selektion,
2. VKNR mit Materialstatus `99`,
3. VKNR mit einer anderen Materialart als `FERT`. Dazu zählen auch VKNR, die in MARA nicht gefunden werden; sie
   fallen still heraus.

Bleibt danach keine andere VKNR übrig, ist die Komponente „exklusiv“. Das ist also ein Merkmal **relativ zur
Selektion**. Es heisst: Ausserhalb der selektierten VKNR braucht kein aktiver FERT-Artikel die Komponente.
Löschvorgemerkte VKNR (`LVORM`) werden in dieser Prüfung nicht ausgefiltert und zählen deshalb als „andere“.

## Bottom-Up

Bottom-Up prüft keine Exklusivität, sondern zählt die VKNR je Komponente. Gezählt wird jede Materialart und jeder
Status. Löschvorgemerkte fehlen nur, solange `p_lvorm` nicht gesetzt ist. Dass Bottom-Up mehr VKNR zeigt als
Top-Down übrig lässt, ist deshalb kein Widerspruch.

## Fall B63491

Wenn nur 44902 und 43457 selektiert waren, müssen die übrigen rund 248 VKNR Status 99 haben oder keine FERT sein.
Bei einem alten Print ist das plausibel. Prüfen lässt sich das nur in P76, weil `ZPOWERBI_VC_TXT` in T76 leer ist.
Ann-Katrin wurde gebeten, in der Bottom-Up-Liste auf Status ungleich 99 zu filtern. Die Antwort hat Ingo am
08.10.2026 verschickt (vorbereitet als `.eml`, weil Outlook-Entwürfe gesperrt sind).

## Offen

- Antwort von Ann-Katrin: Bleiben aktive FERT übrig, ist das ein Programmfehler. Dann ihre Top-Down-Selektion
  nachvollziehen.
- Vorschlag ohne Entscheid: Einen Hinweistext im Report ergänzen, etwa „exklusiv innerhalb Selektion, ohne Status
  99 und Nicht-FERT“.
