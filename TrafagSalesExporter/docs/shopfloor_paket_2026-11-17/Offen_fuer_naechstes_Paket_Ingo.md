# Offen für das nächste Paket an Ingo

**Fortschreibung 17.11.2026:** Das vorbereitete nächste Paket liegt unter [2026-11-17_ergaenzung](2026-11-17_ergaenzung/Ergaenzung_und_Pruefpunkte.md). Es enthält Anh-Nhis Terminantwort, Michaels TX-Mengen-/Schichtregeln und neue FPY-Abgrenzung sowie die Live-Prüfung. Noch nicht versendet. Die älteren Tabellen unten sind historisch: Michael/Anh Nhi haben inzwischen geantwortet. FPY TX «alle Anlagen» muss anhand Michaels Auswahl überprüft werden. VkNr, Einkauf und Patch 08 stehen bereits in der als gesendet dokumentierten Mail vom 13.11.; nicht pauschal als neue Aufträge behandeln.


Stand 13.11.2026. Die Mail an Ingo mit dem Abgleich ist am 13.11.2026 von Fabio gesendet worden. Die folgenden Punkte sind erst danach entschieden worden und gehören ins nächste Paket.

| Nr. | Thema | Inhalt | Entscheid |
|---|---|---|---|
| 1 | Listenfeld «VkNr» (Rückstandslisten SEH/TR5/TX/DW, TVAV) | VkNr = Materialnummer aus SAP. Vorschlag: AFPO-MATNR bzw. PLAF-MATNR bzw. VBAP-MATNR (nicht in SAP geprüft). | Fabio 13.11.2026 |
| 2 | Kommunikationstool Einkauf, «WE aktuell gem. SAP» | Datum der Bestellung aus SAP, nachgeschlagen über Bestellnummer und Material der Zeile. Vorschlag: EKET-EINDT, falls vorhanden bestätigter Termin aus EKES (nicht in SAP geprüft). Gemeint ist der geplante Wareneingang, nicht der gebuchte. | Fabio 13.11. und 16.11.2026 |
| 3 | Kommunikationstool Einkauf, «Bestellnummer» | Bleibt Handeingabe; in der Config Quelle von SAP auf manuell stellen. | Fabio 13.11.2026 |
| 4 | Feldliste | Aktualisierte Fassung mitschicken: `2026-11-13_abgleich\Shopfloor_SAP_Felder_Status_2026-11-13.xlsx` (106 Felder: 10 stimmen, 28 falsch, 55 fehlen mit Definition, 13 Quelle offen). | – |
| 5 | FPY TR5/TX/DW | Fabio fragt die Teamleiter nach Anlage/Messplätzen und will den Wert daraus selbst berechnen. Definition folgt nach den Antworten (Frist 20.11.2026). | Fabio 13.11.2026 |
| 6 | Antworten der Teamleiter | Tagesmenge TX, Schichtmengen TX, «Offene K-Auf», Wunschtermin, «Datum WE geplant»: nach Eingang in die Feldliste übernehmen. | offen |

Noch nicht an Ingo gegangen (prüfen, ob in der Mail vom 13.11. enthalten): Paket `2026-11-13_cz_mtf_sb_entfernen` (Patch 08 und Config-Skript).

## Antworten auf die Nachfrage vom 13.11.2026 (Mail «AW: Shopfloor: Woher kommen eure Zahlen?»)

| Datum | Von | Feld | Antwort | Noch offen |
|---|---|---|---|---|
| 13.11.2026 | M.-T. Oana | FPY TR5 | Gemessen an 10 Messplätzen: MP2, MP3, MP7, MP8, MP10, MP11, MP12, MP13, MP14, MP15. Gleiche Liste wie in der Anleitung vom 15.10.2026. | Rechenweg über die 10 Messplätze (ein Gesamtwert oder Mittel je Messplatz) und Zugriff auf die MES-Daten |
| | M. Scalet | Tagesmenge TX, Schichtmengen, FPY TX | ausstehend | |
| | S. Nadarevic | FPY DW | ausstehend | |
| | N. Acikalin | «Offene K-Auf» | ausstehend | |
| | A. N. Tran | Wunschtermin, «Datum WE geplant» | ausstehend | |

## FPY aus dem MES (Stand 16.11.2026)

Quelle: Reportico `https://trch-webapp-reportico.trafagch.local/`, Projekt YIELDGESAMT, Report «Transmitter Yield Gesamt pro Tag und Sparte Detail» (TRSM_YIELD_GES_TAG_SPARTE_DETAIL.xml). Exporte vom 16.11.2026 liegen in Downloads (02.–12.11.2026, mit 10 Messplätzen und mit allen Prüfanlagen).

| Feld | Definition | Abgleich mit Excel 02.–12.11. | Stand |
|---|---|---|---|
| FPY TR5 | Pass / Anzahl je Tag, Sparte ASIC_TR5, Prüfanlagen Messplatz 2, 3, 7, 8, 10, 11, 12, 13, 14, 15 (IDs 2, 1, 23, 6, 8, 9, 10, 11, 12, 13) | 8 von 9 Tagen innerhalb 1.7 Punkten; 12.11.: Report 81.0 %, Excel 87.1 % | Definition steht; Restdifferenz vermutlich Ablesezeitpunkt (nicht belegt) |
| FPY TX | Pass / Anzahl je Tag, Sparte ASIC_TX, alle Prüfanlagen | 6 von 8 Tagen innerhalb 0.1 Punkten; 02. und 04.11. je 0.6 Punkte; 12.11. keine TX-Zeile im Report | Definition steht; 12.11. klären |
| FPY DW | offen | – | Antwort S. Nadarevic ausstehend; DW ist nicht in diesem Report |

Laut zweiter Claude-Sitzung (Lauf je Messplatz, Datei nicht auf dem Rechner gefunden, von mir nicht nachgerechnet): keine Teilmenge der 10 Messplätze passt besser als alle zehn; am 12.11. ergibt der Tag ohne Messplatz 15 89.5 %.

Für Ingo: Die SQL-Abfrage steht in der Report-Definition auf dem Reportico-Server; er kann sie dort übernehmen, statt sie neu zu bauen.

**Freigabe F. Palma 16.11.2026:** FPY TR5 und FPY TX gehen mit den oben stehenden Definitionen an Ingo. Die Feldliste ist nachgeführt (beide Felder bei Ingo).
