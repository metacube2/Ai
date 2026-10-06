# Logistik

Das Modul Logistik unterstützt Disponenten, Logistiker, Arbeitsvorbereitung und Einkauf bei drei Fragen: Woraus besteht ein Produkt und wo wird ein Bauteil verwendet, wo fehlt Material, und wie läuft die Kommissionierung und Produktion gerade jetzt. Die Seiten liegen im Menü unter Logistik. Es sind fünf Seiten: Stücklistenanalyse, Materialdisposition und Fehlteile, Logistik live, Verwendung und Risiko sowie Dispositionsprüfung.

Die Daten kommen aus SAP, aber nicht alle gleich aktuell. Die Stücklisten-, Dispositions- und Verwendungsseiten arbeiten mit einem lokal zwischengespeicherten Datenstand, der bei einem SAP-Ladevorgang oder dem Einkaufsabgleich erneuert wird. Der Stand wird auf den Seiten angezeigt und sollte vor jeder Entscheidung geprüft werden. Logistik live dagegen liest SAP (Produktivsystem) etwa alle 30 Sekunden, solange jemand die Seite geöffnet hat. Keine dieser Seiten schreibt etwas nach SAP zurück.

## Stücklistenanalyse

### Wozu

Die Seite beantwortet zwei Fragen aus dem SAP-Stücklistenbericht der Logistik (LZ-Code-Auswertung): Aus welchen Komponenten besteht ein Kopfmaterial (Top-Down), und in welchen Elternmaterialien steckt eine Komponente (Bottom-Up). Damit lässt sich einschätzen, wie breit eine Änderung oder ein Lieferausfall wirkt und welche Bauteile besonders abhängig sind.

### Was man sieht

Oben steht ein Kopfbereich, der die gewählte Richtung beschreibt. Darunter folgen die Eingabe der Materialnummern (aus Excel einfügbar, auch Bereiche wie 35-40), der Umschalter zwischen Top-Down und Bottom-Up, die Option „Auch gelöschte Materialien“ und die Schaltfläche „Von SAP laden“. Ein Statusfeld zeigt den letzten Lauf mit Uhrzeit und Zeilenzahl. Ein Lauf, der zwar erfolgreich endet, aber keine einzige Zeile findet, wird gelb statt grün gezeigt, weil er fachlich kein Erfolg ist.

Vier Kennzahlenkacheln hängen von der Richtung ab. Top-Down: Kopfmaterialien, unterschiedliche Komponenten, Kopf-Komponenten-Beziehungen und exklusive Komponenten. Bottom-Up: geladene Komponenten, gefundene Elternmaterialien, mehrfach verwendete Komponenten und nur einmal verwendete Komponenten. Dazu kommen ein Balkendiagramm der zwölf Positionen mit der grössten Breite, die Bestandslage der Komponenten (positiver Endbestand, Endbestand null, negativer Endbestand, Bestand nicht geliefert), die Verteilung der LZ-Codes und eine Detailtabelle mit Richtung, Kopfmaterial, Komponente, Bezeichnung, Menge, Exklusivität, Bestand, Endbestand, Stückkosten, Wert Endbestand, Materialstatus und LZ-Code.

### Wie gerechnet wird

Die Kennzahlen und Grafiken werden aus dem gesamten geladenen und gefilterten Datenstand gezählt. Nur die Detailtabelle ist auf 200 Zeilen begrenzt. Jede Komponente wird genau einer Bestandsklasse zugeordnet. Bestandswerte werden bewusst nicht über mehrere Stücklisten addiert, weil eine gemeinsam verwendete Komponente sonst mehrfach bewertet würde; deshalb gibt es auch keine Gesamtsumme in CHF. Das Kennzeichen „exklusiv“ liefert der SAP-Bericht mit; seine genaue fachliche Definition ist hier nicht näher beschrieben (Hinweis: nicht geprüft). Die Detailsuche arbeitet mit Teilzeichenfolgen auf Kopf- und Komponentennummer.

### Einsatz im Arbeitsalltag

**Disponent:** Vor einer Stücklistenänderung die Komponente Bottom-Up laden und prüfen, in wie vielen Elternmaterialien sie steckt. Hohe Balken bedeuten breite Wirkung.

**Einkäufer:** Vor einer Lieferantenverhandlung die Komponenten eines Produkts Top-Down ansehen. Exklusive Komponenten mit Endbestand null oder negativem Endbestand zuerst klären.

**Arbeitsvorbereitung:** Bei der Frage „wird dieses Teil noch irgendwo verbaut?“ Bottom-Up laden. Vor dem Schluss „nirgends verbaut“ zusätzlich „Auch gelöschte Materialien“ aktivieren.

### Grenzen und Fallstricke

Ein SAP-Ladevorgang ersetzt den Datenstand der gewählten Richtung vollständig; ein Delta gibt es nicht. Ohne Materialeingabe wird bewusst alles geladen, was entsprechend lange dauern kann. Die Seite zeigt flache Paare aus Kopf und Komponente und keinen aufklappbaren Baum über mehrere Baugruppenebenen; dafür dient die Seite „Verwendung und Risiko“. Mehrfach oder nur einmal verwendet gilt nur für das, was geladen wurde. Gegen das SAP-Testsystem bleibt der Datenstand leer. Hinweis: Wie viele Materialien aktuell im produktiven Datenstand liegen, wurde nicht geprüft; der dokumentierte Stand vom 2. September war sehr klein (Teilmenge).

## Materialdisposition & Fehlteile

### Wozu

Die Seite priorisiert Materialien, bei denen die Deckung nicht reicht, und zeigt, wie viele Elternmaterialien davon betroffen sind. Sie ersetzt keine Disposition in SAP, sondern liefert die Arbeitsliste für den Disponenten.

### Was man sieht

Oben ein Filterbereich mit Suche (Material, Text oder Lieferant), Disponent, Produktgruppe und dem Schalter „Nur Handlungsbedarf“, der standardmässig eingeschaltet ist. Vier Kacheln: Kritische Materialien, Fehlmenge, Fehlwert in CHF und Datenumfang (Materialien im Datenstand). Daneben ein Balkenpanel mit den Prioritäten P1 (sofort), P2 (hoch), P3 (prüfen) und „Ohne akuten Hinweis“, sowie ein Feld „Datenstand und Abgrenzung“ mit den Zeitstempeln der Stücklistendaten und des Einkaufsabgleichs.

Die Tabelle zeigt je Material: Priorität, Nummer, Bezeichnung, Disponent und Produktgruppe, Bestand und Verbrauch, Endbestand, Sicherheits- und Meldebestand, feste und geplante Zugänge, Fehlmenge und Fehlwert, die Wirkung (Anzahl Elternmaterialien, gegebenenfalls „exklusiv“) und die Quelle.

### Wie gerechnet wird

Der Endbestand stammt aus der SAP-Dispositionsrechnung. Offene Bestellungen dienen nur als Beleg; sie werden nicht noch einmal zum Endbestand addiert, damit nichts doppelt zählt. Die Prioritäten:

- P1: negativer Endbestand ohne festen Zugang.
- P2: negativer Endbestand, aber mit festem Zugang.
- P3: Endbestand unter Sicherheits- oder Meldebestand, eine exklusive Komponente ohne positive Deckung, oder ein von SAP nicht geliefertes Endbestandsfeld.

Die Fehlmenge ist der negative Endbestand als positive Zahl, der Fehlwert die Fehlmenge mal Stückkosten. Fehlen die Stückkosten, ist der Fehlwert unbekannt; die Tabelle zeigt dann einen Strich statt einer 0, und die Kachel nennt die Anzahl nicht bewerteter Fälle. Ein leerer Endbestand ist eine Datenlücke, kein Nullbestand. Die Sortierung läuft nach Priorität, dann nach Wert.

### Einsatz im Arbeitsalltag

**Disponent:** Morgens nach dem eigenen Disponentenkürzel filtern und die P1-Zeilen abarbeiten. Die Spalte „Wirkung“ zeigt, wie viele Elternmaterialien an einem Fehlteil hängen.

**Logistikleiter:** Die Kachel Fehlwert gibt eine Grössenordnung des Problems, sofern die Stückkosten gepflegt sind.

**Einkäufer:** Bei P1-Teilen im Einkauf prüfen, ob eine Bestellung unterwegs ist und wann sie fällig wird.

### Grenzen und Fallstricke

Die Aussage gilt nur für Materialien im Datenstand. Nach der Dokumentation lagen zuletzt nur wenige Materialien mit Disponent vor; für eine Gesamtaussage wäre ein vollständiger Ladevorgang nötig (Hinweis: aktueller Umfang nicht geprüft, er steht in der Kachel Datenumfang). Die Tabelle zeigt höchstens die 1'000 wichtigsten Treffer, Kachelzahlen und Balken gelten für den ganzen Filter. Der Schalter „Nur Handlungsbedarf“ wirkt auf Kacheln und Tabelle, nicht auf die Prioritätsbalken; der grüne Balken bleibt deshalb sichtbar. Die Disponentennamen stammen aus den SAP-Zuordnungen des Einkaufs; fehlt ein Name, steht „Disponent“ mit Code.

## Logistik live

### Wozu

Die Seite zeigt Kommissionierung und Produktion so zeitnah wie möglich. Wer im Lager oder in der Fertigung arbeitet, sieht auf einen Blick, was erledigt ist, was offen ist und wo etwas steht. Sie entstand aus dem Wunsch der Logistik, „Echtzeit“ zu sehen.

### Was man sieht

Die Seite hat oben einen Statusstreifen mit „live“ oder „pausiert“, der Uhrzeit des letzten Abrufs und einen Umschalter mit vier Ansichten: Original, 3D-Lager, Produktivität und Kapazität. Fünf Kacheln stehen über allen Ansichten: Lagerpositionen quittiert gegenüber total, Lagerpositionen offen, Lieferungen fertig kommissioniert, Rückmeldungen heute und aktive Arbeitsplätze (Rückmeldung innerhalb von 15 Minuten).

Die Daten kommen direkt aus dem produktiven SAP: Transportaufträge der Lagerverwaltung, Lieferungen mit Warenausgang des Tages und Rückmeldungen der Fertigungsaufträge. Der Abruf erfolgt alle 30 Sekunden und nur, solange mindestens eine Person die Seite offen hat; alle Betrachter teilen sich denselben Abruf. Nach einem Fehler oder einer sehr langsamen Antwort pausiert der Abruf fünf Minuten, was als „pausiert“ erscheint. Um Mitternacht beginnt ein neuer Tag mit leerem Stand.

### Wie gerechnet wird

Gezählt wird immer ein einzelner Tag. Eine Lagerposition gilt als quittiert, wenn sie in SAP quittiert wurde, sonst ist sie offen. Eine Lieferung gilt als fertig kommissioniert, wenn alle ihre Positionen kommissioniert sind; Lieferungen ohne Positionen werden nicht mitgezählt. Aktive Arbeitsplätze sind solche mit Rückmeldung in den letzten 15 Minuten.

### Einsatz im Arbeitsalltag

**Lagerleiter:** Auf dem Bildschirm im Lager die Seite offen lassen und den Fortschritt der Lieferungen verfolgen.

**Produktionsleiter:** Arbeitsplätze, die längere Zeit nicht zurückgemeldet haben, fallen ruhig auf.

**Controller und Geschäftsleitung:** Ein Blick vor Besprechungen genügt, um den Tag einzuordnen.

### Grenzen und Fallstricke

Ein Wert wird erst sichtbar, wenn SAP ihn gebucht hat; Arbeit, die nicht quittiert oder zurückgemeldet wird, fehlt. Die Werte wurden noch nicht Zeile für Zeile gegen SAP abgeglichen (Stand der Dokumentation). Die Seite ist nur lesend und belastet SAP bewusst gering: Pro Abruf ein Tag, nur die jüngsten Änderungen, mit festen Obergrenzen.

## Original- und Live-Übersicht

### Wozu

Die Originalansicht ist die Betriebsübersicht: links Kommissionierung, rechts Produktion.

### Was man sieht

Links die Lagerpositionen je Stunde als Säulen (angelegt und quittiert), darunter Lieferungen mit Warenausgang heute mit Fortschrittsbalken und einem Lieferwagen-Symbol, sobald der Warenausgang gebucht ist, und ein Laufband „Zuletzt quittiert“ mit Uhrzeit, Transportauftrag, Material, Lagertyp von nach und Lieferung. Rechts die Produktion: alle Arbeitsplätze, eingefärbt nach der letzten Rückmeldung (bis 15 Minuten aktiv, bis 60 Minuten etwas ruhiger, sonst ruhig), danach die Aufträge mit Gutmenge gegen Sollmenge und ein Laufband der letzten Rückmeldungen mit Gut- und Ausschussmenge.

### Wie gerechnet wird

Die Säulen zählen angelegte und quittierte Positionen je Stunde. Die Lieferungen werden so sortiert, dass nicht fertige zuerst stehen, höchstens 25 Zeilen. Die Gutmenge je Auftrag stammt aus der letzten Rückmeldung des Auftrags; stornierte Rückmeldungen werden entwertet.

### Einsatz im Arbeitsalltag

**Schichtleiter Lager:** Zeigt, ob die Stunden-Spitzen abgearbeitet werden. **Schichtleiter Produktion:** Ein dauerhaft ruhiger Arbeitsplatz mitten in der Schicht ist ein Anlass nachzufragen.

### Grenzen und Fallstricke

Die Rückmeldezeit ist der Zeitpunkt der SAP-Buchung, nicht der Zeitpunkt der tatsächlichen Arbeit. Ruhig heisst nur „keine Rückmeldung“, nicht „steht still“.

## 3D-Lager

### Wozu

Die 3D-Ansicht zeigt die Lagerplätze so, wie man sie sich im Lager vorstellt: als Regale mit Paletten. Man sieht, wo heute Bewegung war und wo noch Positionen offen sind. Die Originalsicht bleibt jederzeit über den Umschalter erreichbar.

### Was man sieht

Jede Zeile ist ein Regalgang, jedes Feld ein Platz, an dem seit Tagesbeginn etwas bewegt wurde. Eine Palette mit Kiste je Platz: orange blinkend bedeutet offene Position, grün quittiert; die Höhe richtet sich nach der Zahl der Bewegungen. Der Blickwinkel ist per Regler oder durch Drehen einstellbar. Ein Klick auf eine Palette öffnet den Platz gross mit den letzten 15 Bewegungen (Material, Menge, Richtung, Status). Darunter stehen die Zonen (Lagertypen der Reihe 9xx wie Versand, Wareneingang und Schnittstellen) mit Plätzen und Bewegungen sowie die aktivsten Plätze.

### Wie gerechnet wird

Die Daten sind dieselben Transportauftragspositionen wie auf der übrigen Seite. Gang, Feld und Ebene werden aus dem Platznamen abgeleitet. Plätze, deren Namen keinem erkennbaren Muster folgen, stehen der Reihe nach im Gang „~“. Dargestellt werden höchstens 24 Gänge, die aktivsten, in bis zu vier Spalten; weitere werden als Hinweis genannt.

### Einsatz im Arbeitsalltag

**Lagerleiter:** Ballungen und offene Positionen in einzelnen Gängen erkennen. **Lagermitarbeiter:** Einen Platz anklicken und sehen, was dort zuletzt gebucht wurde.

### Grenzen und Fallstricke

Die Lage im Regal ist eine Ableitung aus dem Platznamen und kann bei unüblichen Namen vom wirklichen Standort abweichen. Die Ansicht zeigt nur Bewegungen seit Tagesbeginn, keinen Lagerbestand.

## Produktivität

### Wozu

Die Ansicht misst, wie lange das Rüsten einer Lieferung dauert und wie viel pro Stunde quittiert wird. Der Wunsch der Logistik war eine Produktivitätssicht vom Rüsten bis zum Warenausgang, ohne Personenbezug.

### Was man sieht

Kacheln: Lieferungen fertig gerüstet gegenüber allen mit Transportauftrag, Durchlaufzeit je Lieferung (Median), Durchlaufzeit, unter der acht von zehn Lieferungen liegen, Zeit bis Warenausgang (Median), Anzahl Lieferungen ohne Uhrzeit des Warenausgangs oder mit widersprüchlichen Zeiten (nicht gerechnet) und Positionen je aktive Stunde. Darunter: quittierte Positionen je Stunde und Lagernummer sowie die offenen Rüstvorgänge, die ältesten zuerst; rot gilt, was länger offen ist als 80 Prozent der fertigen Rüstvorgänge.

### Wie gerechnet wird

Die Durchlaufzeit einer Lieferung läuft vom Anlegen des ersten Transportauftrags bis zur Quittierung der letzten Position. Der Median ist der mittlere Wert; die Kachel „8 von 10 darunter“ zeigt die Zeit, die vier von fünf Lieferungen unterschreiten. Die Zeit bis zum Warenausgang läuft vom ersten angelegten Transportauftrag bis zur Warenbewegung; sie wird nur gerechnet, wenn SAP die Uhrzeit des Warenausgangs liefert und der Warenausgang nicht vor dem ersten Transportauftrag liegt. Positionen je aktive Stunde sind die quittierten Positionen geteilt durch die Stunden mit mindestens einer Quittierung.

### Einsatz im Arbeitsalltag

**Logistikleiter:** Die Mediane im Wochenvergleich beobachten und lange offene Rüstvorgänge nachverfolgen. **Disponent:** Zeitpunkte erkennen, an denen Lieferungen zu lange im Rüsten stehen.

### Grenzen und Fallstricke

Die Durchlaufzeit ist keine Arbeitszeit. Wartezeiten zählen mit, mehrere Personen an einer Lieferung sind nicht berücksichtigt. Echte Personenzeit würde eine Zeiterfassung brauchen (WM-Zeiterfassung, Fiori-App oder MES). Diese Zeitfelder sind in SAP derzeit nicht gefüllt. Die Seite sagt dies auch selbst. Früher hiess die Kennzahl „Rüstzeit“, was missverständlich war.

## Leistung je Person (nur nach Anmeldung)

### Wozu

Unter der Produktivität steht ein Abschnitt „Leistung je Person“. Er ist nur sichtbar, wenn man sich eigens anmeldet. Ohne Anmeldung bleibt die ganze Seite anonym, auch in allen anderen Ansichten. Die Freigabe durch HR liegt nach Angabe des Projektverantwortlichen vom 5. Oktober 2026 vor (Hinweis: schriftlicher Nachweis nicht geprüft).

### Was man sieht

Nach Anmeldung (Benutzer und eigenes Passwort, ähnlich wie bei HR KPI) zeigt die Tabelle je SAP-Benutzer: quittierte Positionen, angelegte Transportaufträge, erste und letzte Quittierung, geschätzte Stunden und Positionen je Stunde. Ein Schalter blendet die Namen wieder aus. Ist die Anmeldung noch nicht eingerichtet, sagt die Seite das; liefert SAP noch keine Benutzer, erscheint ebenfalls ein Hinweis.

### Wie gerechnet wird

Geschätzte Stunden: Aufeinanderfolgende Quittierungen mit höchstens 15 Minuten Abstand gelten als ein Arbeitsabschnitt; eine einzelne Quittierung zählt als zwei Minuten. Positionen je Stunde sind quittierte Positionen geteilt durch diese geschätzten Stunden.

### Einsatz im Arbeitsalltag

**Logistikleiter:** Zur Einsatzplanung und für das Gespräch im Team, nicht für Einzelvergleiche ohne Kontext. **HR:** Darf zur Kontrolle die Darstellung einsehen.

### Grenzen und Fallstricke

Die Zahlen sind eine Schätzung und keine Zeiterfassung. Sie liegen nur im Arbeitsspeicher für den laufenden Tag; es gibt keine Speicherung und keinen Export. Wer eine Position für jemand anderen quittiert, verzerrt den Wert. Die Kennzahl ist in Bezug auf Leistungs- und Verhaltenskontrolle sensibel; sie sollte nur im Rahmen der mit HR vereinbarten Verwendung eingesetzt werden und ist allein kein Urteil über Leistung.

## Kapazität

### Wozu

Die Ansicht beantwortet die Frage der Kapazitätsplanung: Wie viel Arbeit steht an, und reichen Personen und Maschinen dafür? Sie umfasst vier Teile: die Erfassung je Bereich, den Arbeitsvorrat der Kommissionierung, die Belastung der Arbeitsplätze in der Produktion und die Vorschau auf den Warenausgang.

### Was man sieht

**Warenausgang nach Termin, nächste 14 Tage:** Säulen mit den offenen kommissionierrelevanten Positionen je geplantem Warenausgangstag. Der erste Balken ist der laufende Tag. Ein Ausrufezeichen steht für überfällige Lieferungen (Termin vorbei, Warenausgang nicht gebucht). Lieferungen stehen im Tooltip. Lieferungen ohne Kommissionierpositionen werden separat genannt. Die Abfrage läuft höchstens alle 15 Minuten.

**Kapazität Produktion aus SAP, nächste 14 Tage:** Je Kapazität (mit Namen und Arbeitsplätzen) und Tag die Belastung in Prozent mit Ampel. Der Tooltip nennt Bedarf, Angebot in Stunden und Anzahl Vorgänge. Ohne Prozentangabe fehlt ein Angebot, zum Beispiel am Wochenende, an Feiertagen oder bei Kapazitäten mit Schichtintervallen. Die Montagelinien MLE01 und MLE02 teilen sich eine Personalkapazität.

**Kapazität je Bereich (Neva-Kennzahlen):** Eine Erfassungstabelle für die Bereiche Wareneingang, Versorgung Abteilungen, Vorverpackung, MLE01, MLE02, MLE04, Rüsten Kundenaufträge und Sammellisten. Spalten: verfügbare Stunden, eingesetzte Stunden, erledigt, offen, Planzeit min je Einheit, Produktivität je Stunde, gemessene Minuten je Einheit, Bedarf in Stunden, Belastung und Lücke in Stunden. Mit „Speichern“ werden die Tageswerte abgelegt.

### Wie gerechnet wird

Der Warenausgang zählt offene Positionen je geplantem Warenausgangstag. Die Belastung der Produktionskapazität ist der Restbedarf aus den Fertigungsaufträgen (Bearbeiten und Rüsten), geteilt durch das Standardangebot der Kapazität an Arbeitstagen. Der Bedarf eines mehrtägigen Vorgangs wird gleichmässig auf die Arbeitstage zwischen Start- und Endtermin verteilt (Korrektur im SAP-Code, siehe Grenzen).

Die Bereichstabelle rechnet: Produktivität gleich erledigt geteilt durch eingesetzte Personenstunden; gemessene Zeit je Einheit gleich eingesetzte Minuten geteilt durch erledigt; Bedarf gleich offen mal Planzeit; Belastung gleich Bedarf geteilt durch verfügbare Stunden; Lücke gleich Bedarf minus verfügbare Stunden. Die Belastung ist grün bis 85 Prozent, orange bis 100 Prozent und rot darüber. Zwei Personen, die 30 Minuten gemeinsam arbeiten, sind eine Personenstunde. Beim Rüsten am laufenden Tag zählt das Cockpit offene und quittierte Positionen selbst. Planzeiten werden vom letzten erfassten Tag übernommen.

### Einsatz im Arbeitsalltag

**Logistikleiter:** Mit der Warenausgangsvorschau die Schichtbesetzung für die nächsten Tage planen und überfällige Lieferungen früh erkennen. **Arbeitsvorbereitung:** Die Produktionskapazität mit der Belastung je Tag ansehen, um Engpässe zu erkennen. **Bereichsleiter:** Tageswerte erfassen; die Spalte „gemessen min/Einheit“ hilft, die Planzeiten aus überprüften Messungen abzuleiten.

### Grenzen und Fallstricke

**Wichtig: Die Belastung je Tag der Produktionskapazität ist derzeit überhöht.** Die Prüfung vom 5. Oktober 2026 zeigte, dass SAP den ganzen Restbedarf eines Vorgangs auf dessen Starttermin legt. Ein mehrtägiger Vorgang landet so an einem einzigen Tag; der laufende Tag zeigte teils sehr hohe Werte. Die Korrektur (gleichmässige Verteilung auf die Arbeitstage) ist im SAP-Code vorbereitet, wirkt aber erst, sobald sie mit dem nächsten Transport in das produktive SAP importiert ist. Bis dahin sind die Prozentwerte als Obergrenze zu lesen und mit der Arbeitsvorbereitung gegenzuprüfen. Die Vorschau Warenausgang und die Bereichstabelle sind davon nicht betroffen.

Die Bereichstabelle kann jeder erfassen, der die Seite öffnet; ein Login für diesen Teil wurde als mögliche spätere Massnahme genannt, ist aber nicht eingerichtet. Das MES der Produktion ist nicht angebunden. Nur was das MES nach SAP zurückmeldet, erscheint hier. Die Ampelgrenzen der SAP-Kapazität sind in der Oberfläche nicht beschrieben (Hinweis: Grenzwerte nicht geprüft).

## Verwendung & Risiko

### Wozu

Die Seite zeigt, wie weit sich der Ausfall eines Bauteils nach oben auswirkt. Sie geht über mehrere Stücklistenstufen: von der Komponente über die Baugruppen bis zu den Endprodukten. Zusätzlich erbt jedes Endprodukt das Lieferantenrisiko seiner Komponenten. Sie ergänzt die Stücklistenanalyse, die nur die direkte Beziehung zeigt.

### Was man sieht

Ein Hinweis oben nennt die wichtigste Einschränkung: Es sind nur Komponenten mit LZ-Code enthalten. Darunter sechs Kacheln: Komponenten, Endprodukte (oberste Stufe), Baugruppen dazwischen, grösste Tiefe in Stufen, Komponenten mit nur einem Lieferanten und Komponenten mit überfälliger Bestellung. Eine Suche nach Material oder Text grenzt ein.

Links die Liste „Komponenten mit der grössten Wirkung“: direkte Eltern, betroffene Endprodukte über alle Stufen, Tiefe und Lieferanten mit Marken „eine Quelle“ und „überfällig“. Ein Klick auf eine Zeile öffnet rechts den Verwendungsbaum nach oben (höchstens 4 Stufen, 25 Einträge je Stufe). Darunter steht die Liste „Endprodukte mit vererbtem Risiko“, je Endprodukt mit der Zahl der Komponenten darunter, jener mit einer einzigen Quelle und jener mit überfälliger Bestellung.

### Wie gerechnet wird

Die Wirkung einer Komponente ist die Zahl der Endprodukte, die sie über alle Stufen erreicht. Lieferanten stammen aus den Normalbestellungen der letzten 24 Monate. „Kein Einkauf“ bedeutet meist Eigenfertigung oder eine Bestellung, die älter als 24 Monate ist. Eine Komponente mit genau einem Lieferanten gilt als „eine Quelle“. Überfällig ist ein Bestellabruf, dessen Liefertermin in den letzten zwölf Monaten liegt, der noch nicht voll geliefert ist und dessen Position nicht endgeliefert ist; ältere Altlasten werden bewusst ausgeblendet. Texte kommen aus dem Einkauf, sonst aus den Schweizer Verkaufszeilen. Zirkuläre Stücklisten in den Daten werden abgefangen.

### Einsatz im Arbeitsalltag

**Einkäufer:** Vor der Lieferantenverhandlung nach der Spitzenreiter-Komponente suchen und sehen, wie viele Endprodukte an einem Lieferanten hängen. **Disponent:** Bei einer Lieferverzögerung den Verwendungsbaum öffnen und betroffene Endprodukte benennen. **Geschäftsleitung:** Die Liste der Endprodukte mit vererbtem Risiko zeigt, wo Lieferketten besonders dünn sind.

### Grenzen und Fallstricke

Es fehlen Teile ohne LZ-Code. Deshalb gibt es hier bewusst keinen Umsatz je Komponente und keine Gleichteil- oder Komplexitätsauswertung: Bei 94 Prozent fehlendem Umsatz in den erreichbaren Daten wären solche Zahlen falsch. Eine vollständige mehrstufige Stückliste aus SAP ist als spätere Ausbaustufe vorgesehen. Die Verwendungsdaten tragen einen Stand (steht oben); die Lieferantenzahl zeigt nur beobachtete, nicht freigegebene Bezugsquellen.

## Dispositionsprüfung

### Wozu

Die Seite erzeugt aus den Dispositionsparametern in SAP konkrete Prüfaufträge. Sie findet fehlende oder auffällige Einstellungen, bevor sie zu Fehlteilen führen. Es wird nichts automatisch geändert; jede Zeile ist eine fachliche Prüfaufgabe.

### Was man sieht

Aufbau wie bei der Materialdisposition mit denselben Filtern. Vier Kacheln: Prüfaufträge (Regeltreffer), betroffene Materialien, Priorität P1 und „Keine Auto-Änderung“. Die Tabelle zeigt je Prüfauftrag: Priorität, Material, Bezeichnung, die Art des Prüfauftrags, Dispositionsmerkmal und Beschaffungsart, Sicherheits- und Meldebestand, Losgrösse, Materialstatus und LZ-Code.

### Wie gerechnet wird

Ein Material kann mehrere Prüfaufträge auslösen. Die Regeln:

- P1: negativer Endbestand ohne Sicherheitsbestand.
- P1: negativer Endbestand ohne Meldebestand.
- P1: Material mit Status 98 oder 99 (gesperrt oder auslaufend), das noch in Elternmaterialien verwendet wird.
- P2: Dispositionsmerkmal fehlt.
- P2: Beschaffungsart fehlt.
- P3: Fixlosgrösse bei bestehender Deckungslücke prüfen.

### Einsatz im Arbeitsalltag

**Disponent:** Einmal pro Woche die P1-Aufträge durchgehen und die Parameter in SAP korrigieren. **Stammdatenpflege:** Fehlende Dispositionsmerkmale und Beschaffungsarten bereinigen. **Einkäufer:** Bei auslaufenden, aber noch verwendeten Materialien über Ersatz entscheiden.

### Grenzen und Fallstricke

Die Seite schreibt keine Parameter nach SAP zurück. Die Regeln sind Plausibilitätsprüfungen; ein Treffer heisst nicht, dass der Wert falsch ist. Der Umfang hängt vom geladenen Datenstand ab. Wie die Tabelle zeigt, ist sie auf 1'000 Treffer begrenzt.

## Kennzahlen-Glossar

| Kennzahl | Bedeutung | Berechnung in Worten |
|---|---|---|
| Endbestand | Bestand nach Berücksichtigung der Disposition | Wert aus der SAP-Dispositionsrechnung; Bestellungen werden nicht nochmals addiert |
| Fehlmenge | Menge, die zur Deckung fehlt | Negativer Endbestand als positive Zahl |
| Fehlwert CHF | Wert der fehlenden Menge | Fehlmenge mal Stückkosten; ohne Stückkosten unbekannt |
| Priorität P1/P2/P3 | Dringlichkeit eines Hinweises | Regelbasiert, siehe die einzelnen Seiten |
| Exklusive Komponente | Kennzeichen aus dem SAP-Stücklistenbericht, Hinweis auf besondere Abhängigkeit | Wird unverändert übernommen und gezählt; genaue Definition nicht geprüft |
| LZ-Code | Kennzeichen des Logistikberichts für Komponenten | Wird je Komponente geführt und nur gezählt |
| Wirkung (Eltern, Endprodukte) | Wie viele übergeordnete Teile an einer Komponente hängen | Anzahl unterschiedlicher Eltern, bei Verwendung und Risiko über alle Stufen bis zu den Endprodukten |
| Eine Quelle | Komponente mit nur einem Lieferanten | Ein einziger Lieferant in den Normalbestellungen der letzten 24 Monate |
| Überfällig (Einkauf) | Offene Bestellmenge nach Termin | Liefertermin in den letzten zwölf Monaten vorbei und noch nicht voll geliefert |
| Durchlaufzeit je Lieferung | Zeit vom ersten Transportauftrag bis zur letzten Quittierung | Mediane und 80-Prozent-Grenze über alle gerüsteten Lieferungen des Tages |
| Zeit bis Warenausgang | Zeit vom ersten Transportauftrag bis zur Warenbewegung | Nur für Lieferungen mit plausibler Warenausgangs-Uhrzeit |
| Positionen je aktive Stunde | Quittierleistung des Lagers | Quittierte Positionen geteilt durch Stunden mit mindestens einer Quittierung |
| Geschätzte Stunden (Person) | Näherung der Arbeitszeit | Quittierungen mit höchstens 15 Minuten Abstand bilden einen Abschnitt, Einzelquittierung zwei Minuten |
| Belastung (Produktion) | Auslastung einer Kapazität pro Tag | Restbedarf der Fertigungsaufträge geteilt durch Standardangebot; derzeit überhöht, siehe Kapazität |
| Belastung (Bereich) | Auslastung eines Bereichs | Bedarf (offen mal Planzeit) geteilt durch verfügbare Stunden; grün bis 85 %, orange bis 100 %, rot darüber |
| Lücke (Std.) | Fehlende Personenstunden | Bedarf minus verfügbare Stunden |
