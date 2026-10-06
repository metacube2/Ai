# Einkauf

Das Modul Einkauf im Trafag Cockpit zeigt, wie viel bestellt wurde, was noch auf dem Weg ist, bei welchen Lieferanten sich der Einkauf konzentriert und wo Material knapp wird. Hauptsächlich nutzen es Einkäufer, Disponenten, Controller und die Geschäftsleitung. Es liegt im Menü unter «Einkauf». Die Seiten sind: Einkauf Dashboard, Spend, Offene Bestellungen, Kontrakte, Lieferanten, Spend-Aufriss, Bestellbedarf & Deckung, Materialabhängigkeit, Lieferperformance, die Gruppe «Ideen», Kennzahlen-Katalog, PBIX Vorlage, 3D Simulation, Interaktiv und Datenquellen.

Die Daten stammen aus SAP: Bestellköpfe (EKKO), Bestellpositionen (EKPO) und Liefereinteilungen mit Terminen (EKET), dazu Lieferantenstamm, Materialstamm und der Lagerwert. Das Cockpit fragt SAP beim Seitenaufruf nicht live ab. Es liest einen gespeicherten Datenbestand, den ein Ladelauf auffrischt. Nachts läuft ein Delta-Lauf, der nur neue oder geänderte Belege nachlädt. Ein vollständiger Neuaufbau (Full Load) wird von Hand gestartet. Die Seite zeigt oben, wann der letzte Lauf endete. Die berechneten Zahlen bleiben bis zu 60 Minuten im Zwischenspeicher. Ist der Stand abgelaufen, zeigt die Seite zuerst den alten Stand und weist darauf hin, dass neu gerechnet wird. Die Zahlen wechseln dann nach etwa 20 bis 30 Sekunden von selbst.

Alle Beträge stehen in CHF. Fremdwährungsbestellungen werden mit dem Kurs umgerechnet, der im Bestellbeleg selbst festgehalten ist (Belegkurs), nicht mit einem Tageskurs. Zwei Grundregeln gelten für fast alle Seiten:

- **Nur Bestellungen:** Gezählt werden echte Bestellungen. Anfragen, Kontraktbelege und Umlagerungsbestellungen zwischen Werken (Belegart UB) sind ausgeschlossen.
- **Endgelieferte Positionen sind nicht offen:** Eine Position, die in SAP als endgeliefert gekennzeichnet ist, zählt nicht mehr zum offenen Bestellwert, auch wenn rechnerisch noch eine Restmenge übrig wäre. Seit dem 05.10.2026 wirkt dieser Ausschluss tatsächlich. Vorher griff er nicht, weil das Kennzeichen anders gespeichert war als erwartet. Der offene Bestellwert sank dadurch von rund 28.7 auf rund 12.8 Mio CHF. Ältere Auswertungen oder Notizen mit dem höheren Wert sind überholt.

## Einkauf Dashboard

### Wozu

Das Dashboard ist die Einstiegsseite. Es beantwortet in einem Blick: Wie viel wurde im gewählten Zeitraum bestellt, wie viele Bestellungen und Lieferanten stecken dahinter, was ist noch offen und wie hoch ist der Lagerwert der Einkaufsteile.

### Was man sieht

Oben stehen eine Kopfzeile mit dem Ladestand der drei SAP-Quellen und ein Prozentring «Geladen». Darunter folgt der Zeitraumfilter mit «Von Monat» und «Bis Monat», dem Schalter für stornierte Positionen und den Knöpfen «Anwenden» und «2020 bis heute». Ein ausklappbarer Abschnitt «Kennzahlen verstehen» erklärt jede Kennzahl mit ihrer SAP-Herkunft.

Fünf Kacheln zeigen die Hauptzahlen: Bestellwert im Zeitraum, Bestellungen im Zeitraum, offener Bestellwert inklusive Rückstände, aktive Lieferanten im Zeitraum und Lagerwert Einkaufsteile. Unter jeder Kachel steht ein Satz, was genau gezählt wird und was nicht.

Weiter unten liegt der Verlauf des Lagerwerts je Kalenderwoche als Liniengrafik mit Tabelle. Dazu kommen der «SAP Datenfluss» mit dem Stand der drei Quellen, die «Management Insights» als Kurzaussagen und die «Analyseachsen» (Jahr, Lieferant, Warengruppe, Artikel, Region).

### Wie gerechnet wird

- **Bestellwert im Zeitraum:** Summe der Netto-Positionswerte aller Bestellungen, deren Bestelldatum im Zeitraum liegt, in CHF. Das ist bestelltes Volumen, kein Wareneingang und keine Rechnung.
- **Bestellungen:** Anzahl der unterschiedlichen Bestellnummern im Zeitraum. Eine Bestellung mit zwanzig Positionen zählt einmal.
- **Aktive Lieferanten:** Anzahl der Lieferanten mit mindestens einer Bestellposition ungleich null im Zeitraum. Das ist keine Lieferantenbewertung.
- **Offener Bestellwert:** Je Liefereinteilung die bestellte Menge minus die gebuchte Wareneingangsmenge, mal der Stückwert der Position in CHF. Stornierte Positionen, endgelieferte Positionen und Material mit Status 98 oder 99 (gesperrt oder auslaufend) sind ausgeschlossen. Der Wert ist unabhängig vom gewählten Zeitraum und enthält alle Jahre, damit alte Rückstände nicht verschwinden.
- **Lagerwert Einkaufsteile:** bewerteter Gesamtbestand der Materialien der Disponenten 001 bis 005 im Bewertungskreis 1100. Der Wert zeigt immer den Stand des letzten erfolgreichen Abrufs, ein frei wählbarer Stichtag ist nicht möglich.
- **Lagerwert-Verlauf:** Je Kalenderwoche wird der Stand des Einkauf-Laufs gespeichert und als Stand Ende Woche angezeigt. Der erste Verlaufspunkt stammt vom 10.09.2026, einen Rückblick davor gibt es nicht. Wochen ohne Lauf bleiben leer. Die senkrechte Achse beginnt nicht bei null, sondern beim tiefsten Wert.

### Einsatz im Arbeitsalltag

**Geschäftsleitung:** Die Kacheln geben in einer Minute das Gesamtbild: Einkaufsvolumen, offene Verpflichtungen, Lagerbindung. Der Lagerwert-Verlauf zeigt, ob Kapital im Lager auf- oder abgebaut wird.

**Controller:** Vor dem Monatsabschluss lässt sich der Zeitraum auf den Berichtsmonat stellen und der Bestellwert mit der Planung vergleichen. Der offene Bestellwert zeigt die Verpflichtung, die noch nicht im Wareneingang angekommen ist.

**Einkäufer:** Typische Fragen sind: Wie viele Bestellungen habe ich in diesem Jahr ausgelöst, und wie hoch ist die Rückstandslast?

### Grenzen und Fallstricke

- Der Bestellwert ist kein Rechnungs- und kein Zahlungswert.
- Fehlen für Fremdwährungspositionen die Kurse, erscheint oben eine gelbe Warnung mit der Anzahl betroffener Positionen. Diese Werte werden dann 1:1 übernommen und sind nicht belastbar. Der Zähler gilt für den gesamten Bestand, nicht nur für den Zeitraum.
- Ist der Datenbestand unvollständig, zeigt die Seite keine SAP-Stichproben als Kennzahl, sondern weist darauf hin, dass zuerst ein vollständiger Ladelauf nötig ist. Gekennzeichnete Simulationswerte sind keine SAP-Istwerte.
- Der Lagerwert ist fachlich noch nicht gegen die SAP-Auswertung MB5L abgeglichen.
- Der Lagerwert und der offene Bestellwert haben eigene Zeitlogiken und folgen dem Zeitraumfilter nicht.

## Spend

### Wozu

Die Seite zeigt das Beschaffungsvolumen nach Lieferant und Jahr. Sie dient dazu, die grossen Lieferanten, deren Entwicklung und die Verteilung auf Warengruppen, Länder und Währungen zu erkennen.

### Was man sieht

Vier Kennzahlen stehen oben: Spend in CHF, Jahre, Dimensionen und SAP-Status. Das Hauptdiagramm zeigt die zehn grössten Lieferanten des laufenden Jahres bis heute. Darunter folgen Balkenblöcke für das Volumen nach Warengruppe (die zwölf grössten), nach Beschaffungsregion (die zwölf grössten Lieferländer) und nach Belegwährung.

Die Matrix «Kaskadierung Lieferant / Jahr» listet alle Lieferanten mit einer Spalte je Jahr und einer Gesamtspalte. Ein Klick auf den Pfeil klappt den Lieferanten in seine Warengruppen auf, ein weiterer Klick die Warengruppe in ihre Materialnummern. Seit dem 27.08.2026 wird nichts mehr abgeschnitten. Ein Materialeintrag mit Bindestrich ist ein echter Artikel, ein Eintrag ohne Bindestrich eine Textbestellung ohne Artikelstamm. Eine kursiv gesetzte Zeile «übrige» ist eine Sammelzeile und keine Materialnummer.

### Wie gerechnet wird

Spend ist die Summe der Netto-Positionswerte (Bestelldatum im Zeitraum), umgerechnet in CHF mit dem Belegkurs. Stornierte Positionen sind ausgeschlossen. Der heutige Materialstatus 98 oder 99 filtert den historischen Spend dagegen nicht: Ein 2023 eingekaufter Artikel behält seinen Anteil, auch wenn er heute auslaufend ist.

Die Warengruppe stammt aus dem Materialstamm, ersatzweise aus der Bestellposition. Der Text kommt aus einem Katalog der SAP-Warengruppentexte. Unbekannte Codes bleiben als Code sichtbar. Die Gruppe «01» ist die SAP-Sammelgruppe «Dummy» für unklassifizierte Altbestände. Die Beschaffungsregion ist das Land des Lieferanten, nicht die Fakturawährung. Ein Schweizer Lieferant kann in EUR fakturieren. Beim Währungsblock steht zuerst der CHF-Wert und in Klammern die Summe in der Belegwährung, also das tatsächliche Währungsrisiko.

### Einsatz im Arbeitsalltag

**Einkäufer:** Vor der Lieferantenverhandlung zeigt die Matrix, wie sich das Volumen des Lieferanten über die Jahre und über Warengruppen entwickelt hat. Das ist die Gesprächsgrundlage für Konditionen.

**Controller:** Für die Budgetüberprüfung dient die Jahresspalte. Der Währungsblock beantwortet, welcher Anteil des Einkaufs in Fremdwährung läuft.

**Geschäftsleitung:** Die Regionenverteilung zeigt Abhängigkeiten von Ländern.

### Grenzen und Fallstricke

- Das Hauptdiagramm zeigt nur die zehn grössten Lieferanten des laufenden Jahres, die Matrix dagegen alle. Die Summe der zehn Balken ist daher nicht der Gesamtspend.
- Der Spend ist Bestellvolumen am Bestelldatum. Ein 2024 bestellter Auftrag, der erst 2025 geliefert wird, steht im Jahr 2024.
- Fehlende Wechselkurse führen zu einer Warnung und zu nicht belastbaren CHF-Werten.
- Der Zeitraumfilter wirkt auf alle Spend-Zahlen.

## Offene Bestellungen

### Wozu

Die Seite beantwortet: Was ist bestellt und noch nicht eingetroffen, und wie viel davon ist schon überfällig? Sie ist die Arbeitsgrundlage für das Nachfassen bei Lieferanten.

### Was man sieht

Vier Kennzahlen: Bestellungen, Lieferanten, offener Wert und offene Menge. Das Diagramm «Bestellaktivität und offene Positionen» stellt den Bestellwert im Zeitraum dem noch offenen Zulauf gegenüber. Eine Tabelle zeigt Bestellungen im Zeitraum, Bestellwert, offenen Bestellwert (gleichbedeutend mit dem disponierten Zulauf), überfälligen Wert und Anzahl überfälliger Positionen. Die Tabelle trägt rechts je Zeile die Quelle als farbige Marke.

### Wie gerechnet wird

Offene Menge je Liefereinteilung ist die eingeplante Menge minus die gebuchte Wareneingangsmenge, nie negativ. Der offene Wert ist diese Menge mal dem Stückwert der Position in CHF. Bei Bestellmenge null ist der Stückwert null. Überfällig ist der Teil, dessen geplanter Liefertermin vor heute liegt. Die Positionszahl zählt unterschiedliche Bestellpositionen mit mindestens einer überfälligen offenen Einteilung.

Gemäss der geltenden Regel sind endgelieferte Positionen, stornierte Positionen und Material mit Status 98 oder 99 ausgeschlossen, und es zählen nur Bestellungen (keine Anfragen, Kontrakte, Umlagerungen). Der Wert ist zeitraumunabhängig und enthält alle Jahre.

### Einsatz im Arbeitsalltag

**Einkäufer:** Vor dem Wochenmeeting zeigt die Seite den überfälligen Wert und die Positionen, bei denen ein Lieferant nachzufassen ist. Für die Detailliste nach Lieferant und Artikel dient die Idee «Liefertermin-Risiko».

**Disponent:** Die offene Menge ist die Basis für die Frage, ob eine Lieferung den Bedarf noch deckt (siehe «Bestellbedarf & Deckung»).

**Controller:** Der offene Wert ist die Verpflichtung, die in den nächsten Monaten zu Wareneingang und später zu Verbindlichkeiten führt.

### Grenzen und Fallstricke

- Der Liefertermin ist der geplante Termin. Was wirklich geliefert wurde, steht nicht darin. Eine echte Liefertermintreue wird nicht berechnet.
- Ist in SAP ein Wareneingang nicht gebucht oder eine Position nicht als endgeliefert gekennzeichnet, bleibt sie offen. Die Pflege in SAP wirkt erst nach dem nächsten vollständigen Ladelauf vollständig.
- Alte, nie geschlossene Einteilungen bleiben sichtbar. Das ist beabsichtigt, kann aber zu einem hohen Rückstand führen, den es bereinigen sollte.
- Der offene Wert hängt am Kurs des Belegs. Fehlen Kurse, ist er nicht belastbar.

## Kontrakte

### Wozu

Die Seite zeigt Abrufe zu Rahmenverträgen, also was zu einem Kontrakt bestellt, aber noch nicht geliefert ist. Sie dient der Steuerung offener Restverpflichtungen.

### Was man sieht

Vier Kennzahlen: offener Wert der Kontraktabrufe, Einteilungen im Zeitraum (alle Belege), Top Verpflichtung und Letztes Bestelldatum. Das Diagramm «Top Verpflichtungen nach Lieferant und Artikel» zeigt die sechs grössten offenen Abrufe. Eine Tabelle und eine Statusliste ergänzen das Bild.

### Wie gerechnet wird

Es wird die Formel des offenen Bestellwerts verwendet, aber eingeschränkt auf Bestellungen, die einen Kontraktbezug tragen. Kachel, Diagramm und Top Verpflichtung beruhen auf derselben Grundmenge. «Letztes Bestelldatum» ist das jüngste Bestelldatum im Zeitraum und kein Fälligkeitsdatum. Die Kachel «Einteilungen im Zeitraum» zählt alle Belege und ist nicht auf Kontraktabrufe begrenzt.

### Einsatz im Arbeitsalltag

**Einkäufer:** Für die Pflege von Rahmenverträgen zeigt die Seite, bei welchem Lieferanten und Artikel die grösste offene Abrufverpflichtung liegt.

**Controller:** Der Wert ist ein Näherungswert für die abgerufene, aber noch nicht gelieferte Verpflichtung.

### Grenzen und Fallstricke

- Der Wert ist eine Näherung für offene Abrufe und kein noch verfügbarer Vertragsrahmen. Echte Mengenkontrakte mit Ablaufdatum und Restmenge werden nicht abgebildet, weil die Kontraktbelege selbst bewusst ausgeschlossen sind.
- Ohne Kontraktbezug in den Bestellungen steht der Wert auf null und das Diagramm bleibt leer. Das ist die richtige Aussage und kein Fehler.
- Eine Vertragsauslastung in Prozent wird noch nicht berechnet.

## Lieferanten

### Wozu

Die Seite gibt den Überblick über die Lieferantenbasis: Wer liefert, wie viel Volumen steckt dahinter und wie entwickeln sich die Stückpreise.

### Was man sieht

Vier Kennzahlen: aktive Lieferanten, Performance Score, Preisindikator und Qualität. Das Diagramm zeigt die Top-Lieferanten nach Spend im gewählten Zeitraum. Dazu kommen Statuszeilen (Lieferantenbasis, Preisentwicklung, Termintreue) und eine Detailtabelle.

### Wie gerechnet wird

Aktive Lieferanten sind die Lieferanten mit mindestens einer Bestellposition ungleich null im Zeitraum, der Name stammt aus dem Lieferantenstamm. Der Preisindikator ist der mengengewichtete Durchschnitts-Stückpreis des jüngsten Jahres in CHF (Summe der Positionswerte geteilt durch Summe der Mengen), mit der Veränderung zum Vorjahr in Klammern. Er mischt Schrauben und Baugruppen und ist daher als Niveau wenig aussagekräftig, die Veränderung ist die eigentliche Aussage.

### Einsatz im Arbeitsalltag

**Einkäufer:** Das Diagramm zeigt, bei wem das Volumen im Zeitraum liegt. Die Preisveränderung dient als erstes Warnsignal für Preissteigerungen.

**Geschäftsleitung:** Die Liste zeigt, welche Lieferanten für den Betrieb relevant sind.

### Grenzen und Fallstricke

- «Performance Score» und «Qualität» stehen bewusst auf «-». Es gibt keine angebundene Quelle für Ist-Wareneingangsdaten oder Reklamationen. Die frühere Anzeige mit einem festen Prozentwert war ein Platzhalter und wurde am 07.08.2026 entfernt.
- Es gibt keine Lieferantenbewertung. «Aktiv» bedeutet nur, dass im Zeitraum bestellt wurde.

## Spend-Aufriss

### Wozu

Der Spend-Aufriss zerlegt den Einkaufsspend in frei wählbare Ebenen. Er beantwortet: Wo genau steckt das Geld, nach Lieferant, Warengruppe, Material, Region, Währung oder Produktgruppe?

### Was man sieht

Oben wählt man den Einstieg: Lieferant, Warengruppe, Material, Produktgruppe, Beschaffungsregion oder Währung. Die Matrix zeigt dann den Spend je Jahr und lässt sich Ebene für Ebene aufklappen. Darunter liegen Kuchendiagramme je Top-Warengruppe mit dem Anteil nach Lieferland, Balken für das Volumen nach ABC und nach XYZ und die ABC/XYZ-Massnahmenmatrix. Bei der Produktgruppen-Sicht erscheinen Marken für zugeordneten Spend, Spend ohne Produktgruppe, Mehrfachverwendung und den Stand der SAP-Zuordnung.

### Wie gerechnet wird

Grundlage ist wie im Spend der Netto-Positionswert in CHF zum Bestelldatum. Die Sicht «Produktgruppe» ordnet ein Einkaufsmaterial über die Stücklisten-Verwendung dem Disponenten des verwendenden Kopfmaterials und damit der SAP-Produktgruppe zu. Wird eine Komponente in mehreren Produktgruppen verwendet, wird ihr Spend gleichmässig auf diese Gruppen verteilt (ein Drittel bei drei Gruppen). Dadurch bleibt die Gesamtsumme exakt erhalten. Spend ohne Zuordnung bleibt sichtbar. Fehlt der Produktname einer Gruppe, steht «Disponent» mit dem Code.

ABC ist das Wertklassen-Kennzeichen des Materialstamms, XYZ die Regelmässigkeit des Bedarfs aus einer SAP-Zusatztabelle. Die Massnahmenmatrix kombiniert beide mit Spend, Anzahl Materialien und Lieferanten und nennt je Klasse einen Prüfauftrag mit Priorität. Sie ändert nichts in SAP.

### Einsatz im Arbeitsalltag

**Einkäufer:** Die Produktgruppen-Sicht zeigt, wie viel Einkaufsspend an einer Produktgruppe hängt, zum Beispiel für die Frage nach Bündelungspotenzial. Die Massnahmenmatrix liefert konkrete Prüfaufträge, zum Beispiel für teure, unregelmässig bezogene Teile.

**Controller:** Die Währungssicht und die Regionensicht zeigen, wie der Spend verteilt ist.

**Disponent:** ABC/XYZ zeigt, welche Teile viel Wert binden und wie planbar sie sind.

### Grenzen und Fallstricke

- ABC, XYZ und Lieferantenland sind nur so vollständig wie der letzte Full Load. Zuletzt dokumentiert waren Lieferantenland 100 %, ABC 78 %, XYZ 65 % und Warengruppe 80.7 % gefüllt. Wo die Spalte leer ist, erscheint «ohne ABC» oder ein Hinweis.
- Die Produktgruppen-Zuordnung beruht auf dem Disponenten, nicht auf einer Stammdatenpflege je Material. Bis zu einem vollständigen Lauf der Stücklistendaten ist sie unvollständig. Der zuletzt dokumentierte Stand hatte nur 105 Verwendungszeilen mit Disponent.
- Die Texte für die Disponentengruppen D1 und D5 sind Ersatztexte des Cockpits. Ein später in SAP gepflegter Text hat Vorrang.

## Bestellbedarf & Deckung

### Wozu

Die Seite verbindet den SAP-Endbestand eines Materials mit seinen offenen Bestellungen, den Planterminen und den Lieferanten. Sie zeigt, wo trotz offener Bestellung eine Deckungslücke bleibt und wo Bestellungen überfällig sind.

### Was man sieht

Ein Filterfeld nimmt Material, Text oder Lieferant, dazu kommen Disponent und Produktgruppe sowie der Schalter «Nur Handlungsbedarf». Vier Kennzahlen folgen: Deckungshinweise, offene Bestellmenge, überfällige Menge und offener Wert in CHF. Die Prioritätsbalken P1 bis P3 und «Ohne akuten Hinweis» zeigen die Verteilung. Eine Detailtabelle nennt je Material Bestand, Verbrauch, Endbestand, Sicherheits- und Meldebestand, offene und überfällige Menge, nächsten Termin und Lieferant. Ein Statusfeld nennt den Ladezeitpunkt der Stücklisten- und der Bestelldaten.

### Wie gerechnet wird

Der Endbestand aus der SAP-Disposition bleibt führend. Die Bestellzugänge werden nicht noch einmal dazugezählt, damit nichts doppelt erscheint. Die Priorität lautet:

- **P1:** Endbestand negativ, entweder ohne offene Bestellung oder trotz offener Bestellung.
- **P2:** offene Menge mit überfälligem Plantermin.
- **P3:** Endbestand unter dem Sicherheits- oder Meldebestand oder kein Endbestand von SAP geliefert.
- **OK:** kein akuter Hinweis.

Die offene Menge berücksichtigt nur Bestellungen, keine endgelieferten und keine stornierten Positionen. Die Detailtabelle zeigt höchstens die 1000 wichtigsten Treffer. Kennzahlen und Balken gelten für den gesamten gefilterten Umfang. Suche, Disponent und Produktgruppe wirken auf Kennzahlen, Balken und Tabelle. Der Schalter «Nur Handlungsbedarf» blendet die OK-Zeilen nur in Kennzahlen und Tabelle aus, nicht in den Balken.

### Einsatz im Arbeitsalltag

**Disponent:** Zum Wochenstart den eigenen Disponentencode eintragen und die P1-Zeilen abarbeiten: Lücke trotz Bestellung heisst, den Termin vorziehen oder zusätzlich bestellen.

**Einkäufer:** Die Spalten «Offen / überfällig» und «Lieferant» zeigen, bei wem nachzufassen ist.

### Grenzen und Fallstricke

- Die Seite ist eine Auswertung und schreibt nichts nach SAP zurück.
- Bleibt der Endbestand leer, ist das eine Datenlücke und kein Nullbestand.
- Für eine unternehmensweite Aussage braucht es einen vollständigen Ladelauf der Stücklistenverwendung.

## Materialabhängigkeit

### Wozu

Die Seite zeigt, bei welchen Materialien der Einkauf von einem einzigen Lieferanten oder von einer starken Konzentration abhängt und wie breit das Material verwendet wird. Sie hilft, Versorgungsrisiken früh zu erkennen.

### Was man sieht

Wie bei den anderen Versorgungsseiten gibt es Filter, Kennzahlen (Risikomaterialien, nur ein Lieferant, breite Wirkung, analysierter Spend) und Prioritätsbalken. Die Tabelle nennt je Material den Top-Lieferanten, die Anzahl Lieferanten, den Top-Anteil in Prozent, den Spend in CHF und die Anzahl Elternmaterialien.

### Wie gerechnet wird

Grundlage ist die Bestellhistorie, nicht eine freigegebene Bezugsquellenliste. Je Material werden die Lieferanten und ihr Spend verglichen. Die Priorität lautet:

- **P1:** nur ein beobachteter Lieferant und das Material wird in mindestens fünf übergeordneten Materialien verwendet.
- **P2:** nur ein beobachteter Lieferant bei geringerer Verwendung.
- **P3:** ein Lieferant hat mindestens 80 Prozent des Spends.
- **OK:** mehrere beobachtete Lieferanten.

Stornierte Positionen sind ausgeschlossen, und es zählen nur echte Bestellungen. Der Spend umfasst die gesamte Historie.

### Einsatz im Arbeitsalltag

**Einkäufer:** Vor Rahmenvertrags- oder Preisgesprächen zeigt die Seite, wo ein Lieferant nicht ersetzbar ist, und hilft, Zweitquellen zu priorisieren.

**Geschäftsleitung:** Die Zahl «Nur ein Lieferant» ist eine Kennzahl für das Versorgungsrisiko.

### Grenzen und Fallstricke

- «Ein Lieferant» heisst nur, dass historisch ein Lieferant beobachtet wurde. Zweitquellen, die in SAP hinterlegt, aber nie bestellt wurden, sieht die Seite nicht.
- Die Spalte «Spend CHF» trägt in der Tabelle intern den Wert der Historie, nicht den der offenen Bestellungen.
- Die Breite der Verwendung hängt von der Vollständigkeit der Stücklistendaten ab.

## Lieferperformance

### Wozu

Die Seite zeigt den belastbaren Teil der Liefertreue: offene Bestellmengen, deren geplanter Termin überschritten ist oder bald fällig wird. Sie trennt dies ausdrücklich von der echten Termintreue, die heute nicht messbar ist.

### Was man sieht

Ein gelber Hinweis oben stellt klar: Echte Liefertermintreue (OTIF) wird nicht berechnet, weil das tatsächliche Wareneingangsdatum fehlt. Vier Kennzahlen folgen: Plantermin-Risiken, überfällige Menge, offener Wert in CHF und Ist-Termin-Abdeckung, die konstant 0 % zeigt. Die Prioritätsbalken und die Tabelle (Lieferant, Plantermin, offene Menge, überfällig, offener Wert) vervollständigen die Seite. In der Spalte «Ist-Wareneingang» steht «Quelle fehlt».

### Wie gerechnet wird

Je Material und Lieferant wird die offene Menge aus dem Bestellbestand gebildet. Priorität P1 bedeutet, dass eine Menge überfällig ist, P2, dass der nächste Plantermin innerhalb der nächsten 30 Tage liegt, und OK, dass der offene Termin später liegt. Gezählt werden nur offene Bestellungen ohne endgelieferte und stornierte Positionen.

### Einsatz im Arbeitsalltag

**Einkäufer:** Die P1-Liste ist die Mahnliste: Wer ist mit welcher Menge und welchem Wert im Verzug?

**Qualitäts- und Lieferantenmanagement:** Die Seite ist eine Vorstufe. Eine echte Bewertung braucht das Ist-Wareneingangsdatum.

### Grenzen und Fallstricke

- Die Seite zeigt Plantermin-Rückstand, nicht Termintreue. Ein Lieferant, dessen Termin im System nie nachgepflegt wurde, erscheint als überfällig, auch wenn er geliefert hat und der Wareneingang noch nicht gebucht ist.
- Die Ergänzung um Wareneingangsbelege ist offen, deshalb steht die Ist-Termin-Abdeckung auf null Prozent.

## Ideen: Übersicht

### Wozu

Die Gruppe «Ideen» sammelt weitere Auswertungen, die der Einkauf neben Power BI für Steuerung, Risiko und Sparpotenzial nutzen kann. Die Übersicht zeigt den Reifegrad jeder Idee.

### Was man sieht

Eine Kartenliste nennt zehn Ideen: Lieferantenrisiko, Preisentwicklung CHF, Maverick Buying, Rahmenvertragsnutzung, Working Capital, Datenqualität, Liefertermin-Risiko, Spend-Konzentration, Savings Tracker und Bestellrhythmus. Je Karte stehen eine Statusmarke (zum Beispiel «berechenbar», «Konzept», «teilweise», «startklar»), die benötigten Daten und der Nutzen. Daneben steht eine Prioritätenliste. Darunter liegen aufklappbare Bausteine mit Ziel, Datenbasis, Kennzahlen, Berechnung, Visualisierung und nächstem Schritt.

### Wie gerechnet wird

Die Übersicht rechnet nichts. Der Status hängt davon ab, ob die nötigen SAP-Daten geladen sind. Der Status «Konzept» heisst, dass die Idee nur beschrieben und noch nicht berechnet wird. Das gilt für Lieferantenrisiko, Maverick Buying, Working Capital und Savings Tracker. «Berechenbar» heisst, dass es eine eigene Seite mit echten Zahlen gibt.

### Einsatz im Arbeitsalltag

**Einkaufsleiter:** Die Übersicht hilft zu entscheiden, welche Auswertung als Nächstes ausgebaut werden soll.

**Fachabteilung:** Wer eine Auswertung vermisst, findet hier, ob sie bereits angedacht ist.

### Grenzen und Fallstricke

- Karten mit «Konzept» liefern keine Zahlen.
- Der Kennzahlen-Katalog nennt den Umsetzungsstand je Kennzahl genauer.

## Ideen: Einkauf-Datenservice

### Wozu

Die Seite steuert die Datenbasis des ganzen Moduls: Hier wird der Einkaufsbestand aus SAP neu aufgebaut oder aufgefrischt.

### Was man sieht

Vier Kacheln zeigen die Zeilenzahlen im Bestand (Bestellköpfe, Positionen, Einteilungen) und den letzten Stand mit Modus. Zwei Knöpfe starten einen vollständigen Ladelauf oder einen Delta-Lauf. Darunter steht der Status und die letzte Meldung.

### Wie gerechnet wird

Der Full Load baut die korrekte Basis komplett neu auf. Der Delta-Lauf aktualisiert danach nur neue und geänderte Belege. Beide Läufe laufen im Hintergrund weiter, auch wenn man die Seite verlässt. Läuft schon ein Lauf, startet kein zweiter. Nach einem erfolgreichen Lauf wird der Zwischenspeicher der Auswertungen geleert.

### Einsatz im Arbeitsalltag

**Administrator oder Key-User:** Nach grösseren Nachpflegen in SAP, zum Beispiel Endlieferkennzeichen oder Stammdaten, ist ein Full Load nötig, weil das Delta nur Belege berücksichtigt, die sich geändert haben. Ein Full Load ist aufwendig und sollte mit dem Einkauf und dem SAP-Betrieb abgestimmt werden.

### Grenzen und Fallstricke

- Das Datum in SAP, an dem ein Beleg angelegt wurde, ist kein Änderungsdatum. Ein Wareneingang auf einem älteren Beleg wird deshalb nur vom Full Load zuverlässig erfasst.
- Die Laufzeit eines Full Loads ist nicht verlässlich bekannt.

## Ideen: Liefertermin-Risiko

### Wozu

Die Seite klassiert den offenen Bestellwert nach Fälligkeit und zeigt, bei welchen Lieferanten und Artikeln der grösste Wert überfällig oder bald fällig ist.

### Was man sieht

Vier Kennzahlen: überfällig (offener Wert), 0 bis 7 Tage, Hotlist (Anzahl Zeilen) und Datenbasis. Ein Balkendiagramm zeigt den offenen Wert in den Klassen überfällig, 0 bis 7 Tage, 8 bis 30 Tage und später. Eine «Detail-Hotlist» listet die zehn grössten Kombinationen aus Lieferant und Artikel mit Fälligkeitsdatum und Ampel (rot, wenn überfällig, sonst gelb).

### Wie gerechnet wird

Offene Menge mal Stückwert in CHF je Einteilung, eingeteilt nach dem geplanten Liefertermin gegenüber heute. Es gelten die Regeln der offenen Bestellungen (nur Bestellungen, ohne endgelieferte Positionen). Der Zeitraumfilter wirkt nicht.

### Einsatz im Arbeitsalltag

**Einkäufer:** Montags die Hotlist von oben abarbeiten und bei den grössten überfälligen Werten nachfassen.

**Controller:** Die Klasse «später» zeigt die zukünftige Zulaufverpflichtung nach Zeit.

### Grenzen und Fallstricke

- Nur die zehn grössten Zeilen stehen in der Hotlist.
- Die Klassen beruhen auf dem Plantermin, nicht auf einer zugesagten Lieferung.

## Ideen: Preisentwicklung

### Wozu

Die Seite zeigt, ob Einkaufspreise steigen oder fallen, als Entwicklung über die Jahre und je Artikel.

### Was man sieht

Vier Kennzahlen: Artikel im Trend, Anzahl Artikel mit Preisanstieg (über 2 Prozent gegenüber dem Vorjahr), Jahre und Datenbasis. Ein Balkendiagramm zeigt den Stückpreis je Jahr. Eine Hotlist zeigt die Artikel mit dem höchsten Spend und der Preisveränderung zum Vorjahr, mit Ampel: rot bei Anstieg über 2 Prozent, grün bei Rückgang über 2 Prozent, sonst gelb.

### Wie gerechnet wird

Der Stückpreis ist der mengengewichtete Durchschnitt in CHF je Jahr: Summe der Positionswerte geteilt durch Summe der Mengen. Je Artikel wird die Veränderung gegenüber dem letzten Vorjahr mit Daten gebildet. Zeilen ohne Menge bleiben unberücksichtigt.

### Einsatz im Arbeitsalltag

**Einkäufer:** Vor der Preisverhandlung zeigt die Hotlist, welche Artikel teurer geworden sind und wie stark.

**Controller:** Die Jahresreihe hilft, Preiseffekte von Mengeneffekten zu trennen.

### Grenzen und Fallstricke

- Das Diagramm zeigt den **mengengewichteten Durchschnitt** je Jahr («Ø Stückpreis nach Jahr»), die Hotlist dagegen das **Minimum** je Artikel und Jahr wie in Power BI. Beide Zahlen sind also nicht direkt vergleichbar. Ein steigender Durchschnitt kann auch daher kommen, dass mehr teure Artikel bestellt wurden (Mix), nicht nur von Preiserhöhungen.
- Ein Durchschnitt über alle Artikel mischt sehr unterschiedliche Teile. Die Aussage liegt in der Veränderung, nicht im Niveau.

## Ideen: Spend-Konzentration

### Wozu

Die Seite zeigt die Abhängigkeit von den grössten Lieferanten und das Bündelungspotenzial.

### Was man sieht

Vier Kennzahlen: Top-10-Spend, Top-Anteil am Gesamtspend, Anzahl Lieferanten und Datenbasis. Ein Balkendiagramm zeigt die zehn grössten Lieferanten. Die Hotlist nennt je Lieferant Spend, Anzahl Warengruppen, Rang und Anteil mit Ampel.

### Wie gerechnet wird

Der Spend je Lieferant wird absteigend sortiert, daraus ergeben sich Rang und Anteil am Gesamtspend des Zeitraums. Die Ampel hängt am absoluten Spend: über 1 Mio CHF rot, über 250'000 CHF gelb, sonst grün.

### Einsatz im Arbeitsalltag

**Einkaufsleiter:** Der Top-Anteil beantwortet, wie stark der Einkauf von wenigen Lieferanten abhängt.

**Geschäftsleitung:** Hohe Konzentration bedeutet Verhandlungshebel, aber auch Abhängigkeit.

### Grenzen und Fallstricke

- Die Ampel ist eine reine Betragsschwelle und sagt nichts über das Risiko des Lieferanten.
- Es werden nur die zehn grössten Lieferanten gezeigt.

## Ideen: Datenqualität

### Wozu

Die Seite zählt Lücken in den Einkaufsdaten, damit Auswertungen nicht auf unvollständigen Daten beruhen.

### Was man sieht

Vier Kennzahlen: Prüfungen, Fehler gesamt, Anzahl Positionen und Anzahl Bestellköpfe. Das Diagramm und die Liste zeigen die Prüfungen: fehlender Lieferant, fehlende Warengruppe, fehlender Artikel oder Text, Nullmenge und Nullwert. Zu jeder Prüfung gibt es eine Ampel.

### Wie gerechnet wird

Gezählt werden Zeilen im gesamten gespeicherten Bestand, ohne Zeitraumfilter und ohne Rücksicht darauf, ob die Position storniert ist. Bei fehlendem Lieferanten und fehlendem Artikel steht die Ampel bei jedem Fund auf rot, bei den anderen Prüfungen auf gelb.

### Einsatz im Arbeitsalltag

**Key-User Einkauf:** Die Seite zeigt, wo die Stammdaten- oder Belegpflege in SAP nachzubessern ist. Nach der Korrektur ist ein Full Load nötig, damit die Zahl sinkt.

**Controller:** Eine hohe Zahl bei «Nullwert» erklärt, warum manche Auswertungen weniger Spend zeigen als erwartet.

### Grenzen und Fallstricke

- Die Zahlen folgen nicht dem Zeitraumfilter, sie sind Bestandszahlen.
- Nullwert und Nullmenge können fachlich korrekt sein, zum Beispiel bei Gratislieferungen oder Textpositionen.

## Kennzahlen-Katalog

### Wozu

Der Katalog ist der fachliche Ausbauplan des Moduls: eine Tabelle aller geplanten und umgesetzten Kennzahlen mit Dimension, Datenbasis und Umsetzungsstand.

### Was man sieht

Eine Tabelle mit den Spalten Analyse, Kennzahl, Dimension, Datenbasis und Status. Der Status ist eine Marke, zum Beispiel «bereit», «teilweise», «wartet auf EKET» oder «Konzept».

### Wie gerechnet wird

Der Katalog rechnet nichts. Der Status hängt davon ab, ob die nötigen Quellen geladen sind.

### Einsatz im Arbeitsalltag

**Controller und Einkaufsleiter:** Vor der Abnahme von Auswertungen sieht man, welche Kennzahl tatsächlich berechnet wird und welche nur geplant ist.

### Grenzen und Fallstricke

- Der Katalog zeigt den Soll- und Planungsstand. Massgebend für die Zahlen sind die Seiten selbst.

## PBIX Vorlage

### Wozu

Die Seite zeigt, welche Seiten aus der bisherigen Power-BI-Datei in das Cockpit übernommen wurden, damit sich die Zahlen vergleichen lassen.

### Was man sieht

Eine Tabelle mit sieben Power-BI-Seiten (zum Beispiel «Besch.Volumen CHF/Lieferant», «Preisentwicklung CHF», «Matrix Vol./WG»), den Visualtypen, dem Mass und den Dimensionen.

### Wie gerechnet wird

Es wird nichts gerechnet. Die Seite dokumentiert die Herkunft. Bei der Preisentwicklung verwendete Power BI das Minimum des Netto-Stückpreises, das Cockpit rechnet heute den mengengewichteten Durchschnitt.

### Einsatz im Arbeitsalltag

**Controller:** Weicht eine Zahl vom Power-BI-Bericht ab, zeigt die Seite, welche Seite und welches Mass verglichen werden müssen.

### Grenzen und Fallstricke

- Unterschiede zu Power BI sind möglich, weil das Cockpit Anfragen, Kontrakte und Umlagerungen ausschliesst und mit dem Belegkurs rechnet.

## 3D Simulation

### Wozu

Die Seite stellt Einkaufskennzahlen räumlich dar und erlaubt, Preis- oder Kursannahmen spielerisch durchzurechnen.

### Was man sieht

Man wählt einen Indikator (Spend, offener Bestellwert, offene Menge, offener Wert der Kontraktabrufe, Liefertermin-Risiko, Preisentwicklung, Spend-Konzentration, Datenqualität, Lieferantenperformance) und die Grafikart (Balken, Linie, Fläche, Kreis). Ein Regler zwischen 0.5 und 1.5 simuliert ein Preis- oder Wechselkurs-Szenario, die Schaltfläche «-10%» setzt es auf 0.9. Ein zweiter Regler skaliert die Beschriftung. Die Grafik lässt sich drehen. Unter dem Regler steht die Differenz in Zahlen.

### Wie gerechnet wird

Das Szenario multipliziert die dargestellten Werte mit dem Faktor. Bei den Indikatoren Spend, offener Wert, Kontraktwert, Liefertermin-Risiko, Preisentwicklung und Spend-Konzentration wirkt der Regler, bei den übrigen nicht («nicht auf diesen Indikator angewendet»). Die Werte stammen aus denselben Auswertungen wie die Seiten Spend, Offene Bestellungen und Ideen, also zum Beispiel den grössten Lieferanten.

### Einsatz im Arbeitsalltag

**Controller:** Für die Frage «Was passiert, wenn alle Preise um 10 Prozent steigen?» liefert der Regler eine schnelle Grössenordnung.

**Geschäftsleitung:** Die Grafik eignet sich zur Präsentation.

### Grenzen und Fallstricke

- Das ist eine Spielwiese und keine Planung.
- Für die Indikatoren «Offene Menge» und «Lieferantenperformance» gibt es keine Livewerte. Die Grafik zeigt dort feste Beispielzahlen (Simulation) und ist keine Aussage über Trafag.
- Ohne geladenen Bestand zeigen auch die anderen Indikatoren Beispielwerte.

## Interaktiv

### Wozu

«Interaktiv» ist eine Sammlung von neun animierten Ansichten auf die Bestellungen. Sie sind für das Entdecken von Mustern gedacht: Wer wächst, wer fällt zurück, wo wird nicht mehr nachbestellt. Die Ansichten entsprechen denen im Verkauf, mit vertauschten Rollen: Lieferant statt Kunde, Lieferland statt Kundenland, Hauptwarengruppe statt Sparte, Buchungskreis statt Gesellschaft, Bestellung statt Rechnung.

### Was man sieht

Oben wählt man eine von neun Ansichten:

- **Lieferanten-Galaxie:** Jede Blase ist einer der 60 grössten Lieferanten der letzten 12 Monate. Rechts steht mehr Einkaufsvolumen im Quartal, oben die Veränderung gegenüber dem Vorquartal, die Grösse zeigt die Anzahl Bestellungen, die Farbe das Lieferland. Die Wiedergabe spielt die Quartale ab, Spuren zeigen den Weg.
- **Rennen:** Die Top 15 nach Einkaufsvolumen der jeweils letzten drei Monate, wählbar nach Lieferanten, Warengruppen oder Ländern. Die Balken überholen sich im Zeitverlauf.
- **Einkaufsfluss:** Ein Flussdiagramm vom Buchungskreis über die Hauptwarengruppe zum Lieferland (Top 12) der letzten 12 Monate.
- **Sonnenstrahl:** Ringe von innen nach aussen: Buchungskreise, Hauptwarengruppen, Lieferanten (je Gruppe die acht grössten). Ein Klick zoomt hinein.
- **Was wäre wenn:** Hochrechnung des Einkaufsvolumens mit Hebeln für Preis und Menge, für alle oder einzelne Hauptwarengruppen, und einem Währungshebel auf den Fremdwährungsanteil.
- **Bestellrhythmus:** Lieferanten mit mindestens vier Bestelltagen. Aus dem üblichen Abstand zwischen den Bestellungen ergibt sich das erwartete Datum der nächsten Bestellung. Die Liste «Wiederbestellung überfällig» sortiert nach überfälligen Takten mal Volumen.
- **Kalender:** Ein Kästchen je Bestelltag, Farbe nach Bestellwert. Ein Klick listet die Bestellungen des Tages.
- **Warengruppen-Netzwerk:** Die 40 Warengruppen mit den meisten Lieferanten. Eine Linie verbindet zwei Gruppen, wenn mindestens so viele Lieferanten (einstellbar) beide Gruppen liefern.
- **3D-Landschaft:** Säulen je Monat und Hauptwarengruppe, drehbar. Die Höhe folgt der Wurzel des Werts, damit kleine Werte sichtbar bleiben.

### Wie gerechnet wird

Grundlage sind die Bestellpositionen der letzten 30 Monate aus dem Einkaufsbestand, ohne stornierte Positionen, nur echte Bestellungen, mit dem Netto-Bestellwert in CHF wie im Dashboard. Die Hauptwarengruppe ist die oberste Stufe der Warengruppe (aus «10.04.00» wird «10.00.00»). Der Rhythmus nimmt den Median der Abstände zwischen Bestelltagen. Der Datenstand wird 30 Minuten gemerkt. Die Fusszeile zeigt Anzahl Positionen und Ladezeit.

### Einsatz im Arbeitsalltag

**Einkaufsleiter:** Die Galaxie zeigt auf einen Blick, welche Lieferanten stark wachsen oder einbrechen. Der Bestellrhythmus zeigt auslaufende Beziehungen oder fällige Nachbestellungen.

**Controller:** Der Simulator liefert die Grössenordnung, wenn Preise oder Währungen sich ändern. Steigt eine Fremdwährung, wird der Einkauf teurer, deshalb gilt im Simulator ein Rückgang als gut.

**Einkäufer:** Das Warengruppen-Netzwerk zeigt, wo Lieferanten breit aufgestellt sind und sich Bestellungen bündeln lassen.

### Grenzen und Fallstricke

- Es geht um Bestellvolumen, nicht um Wareneingang oder offene Mengen. Die Regel zu endgelieferten Positionen spielt hier keine Rolle.
- Der Simulator ist eine Spielwiese und keine Planung. Der Rhythmus kennt keine Saison und keine Rahmenverträge.
- Galaxie und Rhythmus brauchen Daten über mehrere Quartale. Das laufende Quartal ist als unvollständig gekennzeichnet.
- Das Netzwerk ist nicht per Ziehen veränderbar.

## Datenquellen

### Wozu

Die Seite pflegt die Verbindung zwischen dem Cockpit und dem SAP-System für den Einkauf und startet die Ladeläufe. Sie richtet sich an Administratoren und Key-User, nicht an Endanwender.

### Was man sieht

Links stehen die Verbindungsdaten (Quellsystem, optionale abweichende Service-Adresse und Zugangsdaten, Schalter «Einkaufsquelle für Import aktivieren») mit den Knöpfen «Speichern», «Verbindung testen» und «Defaults wiederherstellen». Rechts steht eine Übersicht mit Anzahl Quellen, Verknüpfungen und Zuordnungen. Darunter starten die Knöpfe «Full Load starten» und «Delta aktualisieren» die Ladeläufe, mit Status und Zeilenzahlen. Reiter listen die SAP-Quellen, den Join-Fluss und die Zuordnungen.

### Wie gerechnet wird

Hier wird nichts gerechnet. Ein Full Load ist der vollständige Neuaufbau und aufwendig, der Delta-Lauf holt nur neue und geänderte Belege. Nachts läuft automatisch ein Delta, solange die Einkaufsquelle aktiv ist.

### Einsatz im Arbeitsalltag

**Administrator:** Bei Störungen zuerst «Verbindung testen» ausführen und den Status des letzten Laufs prüfen. Einen Full Load nur nach Absprache mit dem Einkauf starten.

### Grenzen und Fallstricke

- Leere Override-Felder bedeuten, dass die zentrale SAP-Verbindung gilt.
- Fiel der nächtliche Lauf aus, bleiben die Zahlen auf dem alten Stand. Das Datum des letzten erfolgreichen Laufs steht auf dem Dashboard.

## Kennzahlen-Glossar

| Kennzahl | Bedeutung | Berechnung in Worten |
|---|---|---|
| Bestellwert / Spend | Bestelltes Volumen im Zeitraum, kein Wareneingang und keine Rechnung | Summe der Netto-Positionswerte aller Bestellungen mit Bestelldatum im Zeitraum, in CHF zum Belegkurs, ohne stornierte Positionen |
| Bestellungen | Anzahl Bestellbelege im Zeitraum | Unterschiedliche Bestellnummern, eine Bestellung zählt einmal |
| Aktive Lieferanten | Lieferanten, bei denen im Zeitraum bestellt wurde | Unterschiedliche Lieferanten mit mindestens einer Position ungleich null |
| Offene Menge | Bestellt, aber noch nicht im Wareneingang gebucht | Je Liefereinteilung eingeplante Menge minus gebuchte Wareneingangsmenge, nie negativ |
| Offener Bestellwert | Wert des noch ausstehenden Zulaufs | Offene Menge mal Stückwert in CHF. Zeitraumunabhängig, ohne endgelieferte, stornierte und gesperrte Positionen, nur Bestellungen |
| Überfälliger Wert | Teil des offenen Werts mit überschrittenem Plantermin | Offener Wert der Einteilungen mit Liefertermin vor heute |
| Offener Wert der Kontraktabrufe | Näherung für offene Abrufe zu Rahmenverträgen | Offener Wert, beschränkt auf Bestellungen mit Kontraktbezug |
| Stückwert in CHF | Preis je Einheit einer Position | Positionswert in CHF geteilt durch Bestellmenge, null bei Menge null |
| Preisindikator | Preisniveau und Preisveränderung | Mengengewichteter Durchschnitts-Stückpreis des jüngsten Jahres, mit Veränderung zum Vorjahr |
| Top-Anteil | Konzentration auf einen Lieferanten | Anteil des grössten Lieferanten am Spend des Materials oder am Gesamtspend |
| Lagerwert Einkaufsteile | Gebundenes Kapital in Einkaufsteilen | Bewerteter Gesamtbestand der Materialien der Disponenten 001 bis 005 im Bewertungskreis 1100, Stand des letzten Abrufs |
| Endbestand | Voraussichtlicher Bestand nach Zu- und Abgängen | Wert aus der SAP-Disposition, nicht durch Bestellzugänge ergänzt |
| Fehlmenge / Fehlwert | Unterdeckung bei negativem Endbestand | Fehlmenge ist der negative Endbestand, Fehlwert Fehlmenge mal Stückkosten. Fehlen die Stückkosten, wird nicht bewertet |
| P1 / P2 / P3 | Prioritätsstufen der Versorgungsseiten | P1 sofort, P2 hoch, P3 prüfen, die Regeln stehen je Seite |
| Ist-Termin-Abdeckung | Anteil der Lieferungen mit bekanntem tatsächlichen Wareneingangsdatum | Heute 0 %, weil die Quelle fehlt |
