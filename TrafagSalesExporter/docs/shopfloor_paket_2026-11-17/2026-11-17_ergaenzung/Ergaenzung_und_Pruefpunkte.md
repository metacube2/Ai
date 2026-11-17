# Shopfloor: Ergänzung zum Paket vom 13.11.2026

Stand 17.11.2026. Arbeits- und Übergabepaket für Ingo; noch nicht versendet. Kein Deployment-Patch. Bestehende 106-Felder-Datei bleibt unverändert; dieses Dokument ergänzt bzw. präzisiert sie. Keine neue Gesamtzahl nach Status behauptet.

## Quellen und Prüfgrenzen

- Live-Board am 17.11., ca. 13:39–13:44: Rückstandslisten, Schnittstelleninformation, TR5, TX, Verkauf und Einkauf-Kommunikation. Je ein Detaildatensatz pro Rückstandsliste geprüft; keine Vollprüfung aller Datensätze oder Backend-Abfragen.
- Anh-Nhis Antwort, von Fabio im Chat bereitgestellt; Antwortdatum nicht mitgeliefert.
- Michael Scalet, Mail 17.11.2026 13:37, vier Screenshots (beigefügt).
- Originaldatei ProdMengePerDisponent.xlsm: gespeicherte Zellwerte und Struktur lesend untersucht. Makros nicht ausgeführt, Daten nicht aktualisiert, VBA-Logik nicht extrahiert. Datei liegt unverändert unter Quellen bei.
- Vorpaket: lokal als am 13.11. versendet dokumentierte Mail und Feedback. Tatsächlich versendete Anhänge nicht unabhängig verifiziert.

## 1. Rückstandslisten: fachliche Ergänzung und Datenbereinigung

Anh Nhi bestätigt: Wunschtermin = letzter bestätigter Liefertermin, im verwendeten Ablauf der VF-Termin (Eckentermin Verkauf). Datum WE geplant = WE-Termin als Eckendtermin des Produktionsauftrags. Noch zu prüfen: genaue Transaktion und technische SAP-Felder, Verknüpfung zum richtigen Auftrag und Verhalten bei mehreren Einteilungen/Teilbestätigungen sowie Planaufträgen. Kein pauschaler Rückgriff auf Kundenwunschdatum oder erste Einteilung ohne Prüfung. Vorschlag für die Beschriftung: «Bestätigter Liefertermin (VF)».

Das ist vom Einkaufsfeld «WE aktuell gem. SAP» zu unterscheiden: Dort ist der geplante Eingang einer Bestellposition gemeint, nachgeschlagen über Bestellung und Material.

| Stichprobe | Online-Befund |
| --- | --- |
| RS-SEH-26-004 | Letzte Änderung 05.10.2026 06:33, Excel-Import. Material 44562, FA 1249097. Eckendtermin 16.06., Wunschtermin 01.07., WE geplant 23.06.2026. Abschlussdatum enthält Text ERLEDIGT und Hinweis «Kein gültiges Datum (aus Excel übernommen)». Trotzdem offen/überfällig. |
| RS-TR5-26-204 | Gleicher Importzeitpunkt. Material 54151, FA leer. Eckendtermin 01.10., Wunschtermin 16.09., WE geplant 24.09.2026. |
| RS-TX-26-296 | Gleicher Importzeitpunkt. FA leer. Wunschtermin 14.10., WE geplant 22.10.2026. |
| RS-DW-26-091 | Gleicher Importzeitpunkt. Wunschtermin 22.10.2026, WE geplant leer. |

Listenübersicht: SEH 4 Einträge/1 offen, TR5 204/36, TX 296/86, DW 92/53. Die Stichproben belegen alte Importdaten, nicht die Aktualität jeder einzelnen Zeile.

Zusätzlicher Importbefund: RS-DW-26-689 enthält «Einträge fehlen ab 29.04.22 ... Fortschrittskontrolle ... 2022» als Datums-/Hinweisinhalt. Diesen Text nicht als echten Rückstandsfall behandeln. Auch TR5 zeigt in der offenen Ansicht Einträge mit Status «storniert» bzw. «Abgesagt». Abschluss-/Statusregeln gemeinsam mit Dispo abstimmen; keine Daten ungeprüft löschen und kein Abschlussdatum erfinden.

Abnahme: aktuelle offene Fälle mit Dispo abstimmen, abgeschlossene Fälle korrekt ausschliessen, Importhinweise separat behandeln. Je SEH/TR5/TX/DW einen aktuellen Fall anhand Auftrag, Material, VF und WE gegen SAP prüfen. SAP-Felder gegen manuelles Überschreiben schützen; vorhandene editierbare UI allein belegt den Backend-Schutz nicht.

## 2. TX-Produktionsmengen: Michael und Excel

Quelle im Arbeitsablauf: ProdMengePerDisponent.xlsm, Blatt ProdMenge. Parameter B2 = EL106, B3 = 11, B4 = 2026; Schaltfläche GetData. Sichtbarer Vorgang VORN = 0600.

Gespeicherte Werte A76:AE84 für 12.11.2026:

| Disponent | Tagesmenge (AB) |
| --- | ---: |
| EL1 | 401 |
| EL2 | 328 |
| EL3 | 274 |
| EL4 | 13 |
| EL5 | 49 |
| EL6 | 117 |
| EL7 | 104 |
| EL8 | 17 |
| TL1 | 2 |
| Summe | 1305 |

AD76:AD84 wiederholt jeweils die Tagessumme 1305. Diese Spalte nicht über die Disponenten aufsummieren. Ohne TL1 wären es nur 1303; die fachliche Abgrenzung muss diese Zeile berücksichtigen bzw. explizit klären.

| Modell | Frühschicht | Spätschicht | Nachtschicht |
| --- | --- | --- | --- |
| 2 Schichten | 05:00–14:30 | 14:30–24:00 | – |
| 3 Schichten | 06:00–14:30 | 14:30–23:00 | 23:00–06:00 |

Laut Michael wird der Stundenblock 14:00–15:00 jeweils hälftig Schicht 1/2 zugeordnet. Implementierung bei Stundenwerten: Früh = volle Stunden ab 05 bzw. 06 bis 14 + 0,5 × Block 14–15; Spät = 0,5 × Block 14–15 + volle Stunden 15–24 bzw. 15–23.

Kontrolle aus den gespeicherten Stundenwerten D76:AA84 für das Zweischichtmodell: 05–14 = 517, Block 14–15 = 65, 15–24 = 723. Daraus Früh 549,5 und Spät 755,5, zusammen 1305. Das sind rechnerische Kontrollwerte, keine von Michael bestätigten Schichtmeldungen. Halbe Stückzahlen entstehen aus seiner Aufteilungsregel; Rundung noch festlegen, Summe erhalten.

Offen: welches Schichtmodell gilt an welchem Tag, Zuordnung der Nachtschicht zum Kalendertag (23–24 Vortag oder aktueller Tag), Umgang mit Mengen ausserhalb der Zweischichtzeiten, Rundung und Schicht-3-Darstellung im Board.

Technische Quelle noch offen: Die Datei enthält VBA und die versteckten Blätter «Manuell Eröffnen», «DLZ Daten (KW)», «DLZ SQL (KW)». Letzteres enthält SQL gegen TMS_MES mit P76.LinkVorgänge und P76.SensorIDLink, gehört aber zur DLZ-Auswertung. Daraus lässt sich die Quelle des Produktionsmengen-GetData-Makros nicht sicher ableiten. Ingo soll dessen Abfrage prüfen und übernehmen. Eine SAP-Transaktion wurde von Michael nicht genannt; SAP als Quelle der TX-Mengen deshalb nicht als bestätigt behandeln.

## 3. FPY TX: bisherige Definition überprüfen

Bisher (Fabio 16.11.): Pass/Anzahl, ASIC_TX, alle Prüfanlagen. Michaels neue Anleitung weicht in der Anlagenabgrenzung ab und zeigt eine andere Reportansicht. Sie ist eine neue fachliche Quelle, keine bereits geprüfte Änderung der produktiven Berechnung.

1. Reportico, Projekt YIELDGESAMT.
2. Report «TRANSMITTER YIELD GESAMT PRO TAG UND SPARTE» (ohne «Detail» im sichtbaren Titel).
3. Für den Tag 17.11. wählt Michael im Datumsdialog 17.11.–18.11. und Apply. Technische Inklusiv-/Exklusivgrenzen und Zeitzone anhand Reportabfrage verifizieren.
4. Anlagenbereich EAP1_1 bis EAP3_3 auswählen, EAP1_9 (Kabelendkontrolle) und EAP2_6 (Warmabgleich) deaktivieren. Exakte Anlagen-IDs aus der Reportdefinition ermitteln; keine unvollständige Liste aus dem sichtbaren Ausschnitt erfinden.
5. Angezeigten Wert «Avg Asictx» übernehmen. Screenshot: 99.22077922077 (ca. 99,22 %). Lauf am 17.11. tagsüber, daher kein gesicherter Tagesendwert.

Im Screenshot sind mehrere Sparten angehakt; ausgelesen wird spezifisch Avg Asictx. Vor Umsetzung prüfen, ob dieser Wert ein gewichtetes Pass/Anzahl-Verhältnis, Mittelwert oder anders aggregierter Yield ist. Nicht allein aus dem Spaltennamen ableiten. Mit denselben Anlagen und Zeitgrenzen gegen den bisherigen Detailreport nachrechnen. Früherer Kontrollfall: 12.11. im Detailreport ohne TX-Zeile, Excel 97,9 %; weiter klären.

FPY TR5 bleibt unverändert gemäss Freigabe 16.11.: Pass/Anzahl je Tag, ASIC_TR5, MP2, MP3, MP7, MP8, MP10 bis MP15. FPY DW bleibt offen bei Sanel.

## 4. Abgleich mit dem letzten Paket und Online-Stand

| Thema | Vorpaket / neuer Inhalt | Befund / nächste Aktion |
| --- | --- | --- |
| Listenanbindung, VkNr, Typencode | Im Paket 13.11. enthalten | Stichproben noch Excel-Import; VkNr = Materialnummer und MARA-ZZTYP in Online-Doku nachführen; Anbindung und aktuellen Bestand prüfen. |
| VF/WE | Neu: Anh-Nhis Antwort | Fachlich geklärt, technisches Mapping und Abnahme fehlen. |
| TX Tages-/Schichtmengen | Neu: Michael 17.11. + Datei | Für 16./17.11. im Board leer. GetData-Quelle anbinden, Schichtgrenzen wie oben, Restfragen klären. |
| FPY TR5/TX | Definitionen/Freigabe 16.11.; neue TX-Abgrenzung 17.11. | Beide am 16./17.11. leer; TX-Definition vor Umsetzung abgleichen. MES-Zugang mit Marco Di Menco klären. |
| AE/Umsatz IST, sechs Felder | Bauauftrag 13.11. | Jetzt SAP-geliefert und gesperrt. 17.11.: AE 4072455, IST 3514315, AE TX 1375255, AE DW 1711890, Umsatz TX 1136200, DW 1761926 CHF. Anzeige bestätigt Lieferung, nicht CE11000-Formel, HZDAT oder Monatswechsel. Fachlich abnehmen. |
| CZ/MTF/Sensorbestände | Patch 08 im dokumentierten Anhang 13.11. | Module in Navigation entfernt; kein erneuter pauschaler Bauauftrag. PPA-Grafik/Forecast und Backend-Ausnahme gesondert abnehmen. CZ↔CH bleibt absichtlich. |
| Einkauf-Kommunikation | Bestellnummer manuell / WE über Bestellung und Material bereits in dokumentierter Mail 13.11. | 0 Einträge, daher Datenübernahme nicht getestet. Online-Mapping Bestellnummer noch SAP. Konfiguration prüfen; Marcos Nutzungsfeedback bleibt offen. |
| Vorrat, Verzug, Logistik, TX-Planmenge | Korrekturen 13.11. | Lieferung teilweise sichtbar, korrekte Berechnung nicht aus UI belegbar. Gegen gleiche SAP-Stichtage prüfen. |
| Schnittstellen-Dokumentation | Bestehende und neue Definitionen | Online noch «Stand 14.10.» / «7 von 106» / Push-Beschreibung, alte Verkaufsquellen und Abgrenzungen. Vollständig mit Umsetzung synchronisieren. |

Weitere offene Rückmeldungen: Sanel FPY DW; Neva Bedarf/Quelle Offene K-Auf; Marco Widmer Bedarf/Funktionen Kommunikationstool; Daniel Tobler Lieferzeit-Ziele/Leistung Dichtesensoren. Michael und Anh Nhi nicht mehr pauschal als «Antwort ausstehend» führen; nur die oben genannten Restfragen.

## 5. Übergabe und Abnahme

Priorität: Rückstandslisten und Importstatus, TX-Mengen/Schichten, FPY-Definition/Anbindung, Online-Dokumentation. Bereits gelieferte Verkaufswerte fachlich abnehmen. Ingo soll pro bestehender Korrektur aus 13.11. Umsetzungsdatum und Kontrollwert liefern.

Dieses Paket enthält keine ausführbaren Änderungen am BI-Board. Es liefert fachliche Ergänzungen und Prüffälle. Ein Frontend-/Config-Patch ist erst gegen den aktuellen Quellstand sinnvoll; aus UI-Befunden wurde kein ungeprüfter Patch erzeugt.
