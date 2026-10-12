  " 2026-10-12: ETENR als letztes Sortierfeld. Ohne es ist die Reihenfolge von Einteilungen
  " mit gleichem Lieferdatum zufaellig (REF gegen REF belegt), und mit ihr die Zuordnung der
  " Fertigungsauftraege, die Auftragsfelder und das Fakturadatum dieser Einteilungen.
  SORT   ivbep BY vbeln posnr  edatu etenr ASCENDING.
