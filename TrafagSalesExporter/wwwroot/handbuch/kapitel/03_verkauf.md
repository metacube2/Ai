# Verkauf

Das Modul Verkauf beantwortet alle Fragen rund um die Kunden und den Umsatz der Trafag-Gruppe. Es richtet sich an Vertriebsleiter, Verkäufer, Controller und die Geschäftsleitung. Im Menü liegt es als eigener Reiter **Verkauf** mit neun Unterreitern: Kunden, Rückgang, Neue und verlorene Kunden, Konzentration, Cross-Selling, Preisstreuung, Saison und Prognose, Weltkarte und Interaktiv. Der Unterreiter Interaktiv enthält wiederum neun animierte Ansichten mit eigener Reiterleiste.

Die Daten stammen aus derselben Quelle wie das Finance-Cockpit, nämlich aus den verarbeiteten Verkaufsdaten aller Trafag-Standorte. Es gelten dieselben Finance-Regeln für den Einschluss einer Rechnungszeile und für den Nettoumsatz, Gutschriften zählen also negativ. Alle Beträge sind in CHF zum Kurs des Belegdatums. Konzernkunden, also andere Gesellschaften der Trafag-Gruppe, sind ausgeschlossen, damit nur der Umsatz mit externen Kunden erscheint. Fehlt für eine Zeile der Wechselkurs, wird sie weggelassen und am Seitenende gezählt.

Die Zeitfenster enden immer mit dem letzten vollständigen Monat. Ein Monat gilt als vollständig, sobald die jüngste Rechnung im Datenbestand den letzten Werktag (Montag bis Freitag) dieses Monats erreicht hat. Ein angebrochener Monat verzerrt damit keine Vergleiche. Alle Standorte liefern Daten ab Januar 2025. Vergleiche mit dem Vorjahr laufen deshalb über gleich lange Zeiträume, die ganz in den vorhandenen Daten liegen. Die Berechnung wird bis zu einem neuen Quellstand, höchstens aber zwei Stunden lang, zwischengespeichert. Der erste Aufruf kann bis zu einer Minute dauern. Auf jeder Seite stehen unten der Datenstand, die Zahl der Positionen, die Zahl der ohne Kurs weggelassenen Zeilen und die Zahl der ausgeschlossenen Konzernkunden-Positionen.

Zwei Regeln gelten für alle Seiten. Erstens werden Kunden über alle Gesellschaften hinweg zusammengefasst, und zwar über den Namen ohne Rechtsform (AG, GmbH, Ltd und ähnliche), ohne Satzzeichen und ohne Akzente. Zwei verschiedene Firmen mit gleichem Namen können dabei zusammenfallen, und dieselbe Firma mit abweichender Schreibweise kann getrennt erscheinen. Kunden ohne Namen erscheinen mit Gesellschaft und Kundennummer. Zweitens zählen Leistungen, Platzhalter-Nummern und Versandpauschalen (zum Beispiel „Versand bis 2,9 kg“) nicht als Artikel. Das wirkt auf die Preisstreuung und das Artikel-Netzwerk.

Der Reiter ist ohne Finance-Passwort zugänglich. Wer das Cockpit öffnet, sieht also Kundenumsätze. Die meisten Seiten haben einen Excel-Export.

## Kunden

### Wozu

Die Seite ist der Einstieg in den Kundenumsatz der Gruppe. Sie zeigt, welche Kunden wie viel Umsatz bringen, wie sich dieser gegenüber dem Vorjahr verändert hat und bei welchen Kunden mehrere Gesellschaften beteiligt sind.

### Was man sieht

Oben stehen Kennzahlen: Umsatz der letzten 12 Monate, Umsatz im Vergleichszeitraum, derselbe Zeitraum im Vorjahr, die prozentuale Veränderung, die Zahl der Kunden mit Umsatz und die Zahl der Kunden, die bei mehreren Gesellschaften kaufen. Darunter folgt die Veränderung je Gesellschaft. Ein extremer Wert bei einer einzelnen Gesellschaft deutet laut Hinweis auf der Seite eher auf einen Datenfehler als auf echtes Wachstum.

Links steht die durchsuchbare Kundenliste mit Land, beteiligten Gesellschaften, Umsatz der letzten 12 Monate, Veränderung in Prozent und einer kleinen Verlaufslinie. Daneben stehen die zehn grössten Kunden als animierte Balken. Ein Klick auf einen Kunden öffnet seine Details: erste und letzte Rechnung, Zahl der Rechnungen, Umsatz je Monat über 36 Monate, Sparten der letzten 24 Monate und Top-Artikel der letzten 12 Monate.

### Wie gerechnet wird

Der Umsatz der letzten 12 Monate ist die Summe aller Nettoumsätze eines Kunden in den 12 vollständigen Monaten vor dem Stichtag. Der Vergleichszeitraum umfasst höchstens 12 Monate und ist so gewählt, dass der gleiche Zeitraum im Vorjahr vollständig in den Daten liegt. Die Veränderung ist die Differenz zwischen aktuellem Zeitraum und Vorjahreszeitraum, geteilt durch den Vorjahreswert. Erste und letzte Rechnung berücksichtigen nur Zeilen mit positivem Umsatz, Gutschriften gelten nicht als Kauf. Als Hauptland eines Kunden gilt das Land mit dem grössten Umsatz.

### Einsatz im Arbeitsalltag

**Vertriebsleiter:** Man sieht auf einen Blick, ob der Gesamtumsatz gegenüber dem Vorjahr wächst und welche Gesellschaft dazu beiträgt. Typische Frage: Wie viel Umsatz macht ein bestimmter Kunde gruppenweit, nicht nur bei einer Gesellschaft?

**Verkäufer:** Vor einem Kundengespräch lässt sich der Kunde suchen und sein Verlauf, seine Sparten und seine wichtigsten Artikel ansehen.

**Controller:** Die Veränderung je Gesellschaft dient als erste Plausibilitätsprüfung der Verkaufsdaten.

### Grenzen und Fallstricke

Die Summen können von Finance abweichen, weil Finance teils in Hauswährung oder mit dem Kurs vom 31.12. rechnet und Konzernkunden dort enthalten sind. Die Zusammenfassung über den Namen ist nicht fehlerfrei (siehe Einleitung). Eine Marge zeigt der Reiter nicht, sie gehört zur Gruppenmarge in Finance. Es gibt auch keine Auswertung nach Verkäufern.

## Rückgang

### Wozu

Die Seite zeigt Kunden, deren Umsatz deutlich gesunken ist oder die gar nichts mehr bestellen, damit der Vertrieb rechtzeitig nachfassen kann.

### Was man sieht

Oben stehen die Zahl der Kunden mit Rückgang, davon die Zahl der Kunden ohne Umsatz seit 6 Monaten und der gesamte fehlende Umsatz gegenüber dem Vorjahr. Darunter steht die Liste, sortiert nach fehlendem Umsatz, mit Land, Gesellschaften und einem roten Kennzeichen „eingeschlafen“ bei Kunden ohne Rechnung seit 6 Monaten. Es werden höchstens 150 Kunden angezeigt. Über die Auswahl „Vorjahreszeitraum mindestens CHF“ lässt sich die Mindestgrösse einstellen (1'000, 5'000, 10'000, 25'000, 50'000 oder 100'000 CHF, Standard 10'000).

### Wie gerechnet wird

Aufgenommen wird, wer im Vorjahreszeitraum mindestens den eingestellten Betrag umgesetzt hat. Ein Kunde gilt als „eingeschlafen“, wenn er in den letzten 6 Monaten keinen Umsatz hatte und der aktuelle Zeitraum unter dem Vorjahr liegt. Alle anderen erscheinen, wenn ihr Umsatz im Vergleichszeitraum um mindestens 30 Prozent unter dem gleichen Zeitraum des Vorjahres liegt. Der fehlende Umsatz ist Vorjahreswert minus aktueller Wert.

### Einsatz im Arbeitsalltag

**Vertriebsleiter:** Die Liste ist eine Nachfassliste. Typische Frage: Welche grossen Kunden haben wir stillschweigend verloren, und wie viel Umsatz fehlt dadurch?

**Verkäufer:** Man prüft bei den eigenen Kunden, ob der Rückgang bekannt ist (Projektende, Lagerabbau, Wechsel zum Mitbewerber) und plant einen Anruf.

**Geschäftsleitung:** Die Summe des fehlenden Umsatzes zeigt, wie viel Geschäft im Bestand gefährdet ist.

### Grenzen und Fallstricke

Ein Rückgang kann auch eine verschobene Bestellung oder ein einmaliges Grossprojekt im Vorjahr sein. Kunden mit seltenen, grossen Bestellungen erscheinen leicht zu Unrecht. Wegen des Datenbeginns im Januar 2025 sind Vergleiche erst nach und nach über längere Zeiträume möglich.

## Neue und verlorene Kunden

### Wozu

Die Seite zeigt, ob die Gruppe mehr Kunden gewinnt als verliert. Sie macht die Neukundengewinnung und die Abwanderung je Quartal sichtbar.

### Was man sieht

Ein Balkendiagramm zeigt die letzten 12 vollständigen Quartale. Grüne Balken nach oben stehen für neue Kunden, rote Balken nach unten für verlorene. Mit den Schaltern „Anzahl“ und „Umsatz CHF“ wechselt man zwischen der Zahl der Kunden und ihrem Umsatz. Schraffierte Balken sind noch nicht endgültig, graue Balken stammen aus dem ersten Datenjahr und sind nicht belastbar. Darunter stehen die Namen der wichtigsten neuen und verlorenen Kunden der letzten vier Quartale.

### Wie gerechnet wird

Ein Kunde ist in dem Quartal „neu“, in dem sein erster Kauf im Datenbestand liegt. Als Umsatz zählen seine ersten 12 Monate. Ein Kunde ist in dem Quartal „verloren“, in dem sein letzter Kauf liegt und seither nichts mehr folgte. Als Umsatz zählen die 12 Monate vor dem letzten Kauf. Verluste werden erst 6 Monate nach Quartalsende angezeigt, vorher steht „noch offen“, weil die meisten Kunden einfach noch nicht wieder bestellt haben. Endgültig gilt ein Verlust erst, wenn nach dem Quartal 12 Monate vergangen sind, bis dahin ist der Balken schraffiert, und der Kunde kann zurückkommen. „Neu“ ist erst belastbar, wenn davor mindestens 12 Monate Daten liegen, sonst wirkt jeder bestehende Kunde neu. Nur Zeilen mit positivem Umsatz zählen als Kauf.

### Einsatz im Arbeitsalltag

**Vertriebsleiter:** Man vergleicht Gewinn und Verlust und sieht, ob das Wachstum auf neuen Kunden oder auf höherem Umsatz bei bestehenden beruht.

**Verkäufer:** Die Namenslisten zeigen, welche Neukunden nach dem Erstauftrag weiter betreut werden sollten und welche verlorenen Kunden eine Rückgewinnung lohnen.

**Geschäftsleitung:** Eine anhaltend negative Bilanz in Anzahl und Umsatz ist ein Warnsignal für die Marktbearbeitung.

### Grenzen und Fallstricke

Bei Kunden mit seltenen Bestellungen ist die Unterscheidung „verloren“ und „bestellt noch nicht wieder“ unscharf. Im ersten Datenjahr (2025) ist die Zahl der Neukunden nicht aussagekräftig. Ein Kunde, der unter anderem Namen auftritt, kann als neu und gleichzeitig als verloren erscheinen.

## Konzentration

### Wozu

Die Seite zeigt, wie stark der Umsatz von wenigen Kunden abhängt. Das ist eine Frage des Klumpenrisikos.

### Was man sieht

Sechs Kennzahlen stehen oben: Anteil des grössten Kunden, Anteil der Top 5, Top 10 und Top 20 sowie die Zahl der Kunden, die zusammen 80 Prozent des Umsatzes machen. Dazu kommt der HHI, der unter 1500 als breit gestreut gilt und darüber rot erscheint. Die Pareto-Kurve zeigt die Kunden nach Umsatz sortiert und den kumulierten Anteil am Gesamtumsatz. Je steiler der Anfang, desto abhängiger ist die Gruppe von wenigen Kunden. Die Kundenachse ist logarithmisch. Eine zweite Grafik zeigt den Anteil der zehn grössten Kunden je Gesellschaft.

### Wie gerechnet wird

Grundlage ist der Umsatz der letzten 12 Monate je Kunde, nur Kunden mit positivem Umsatz. Der Anteil der Top n ist die Summe der n grössten Kundenumsätze geteilt durch den Gesamtumsatz. Der HHI (Herfindahl-Hirschman-Index) ist die Summe der quadrierten Prozentanteile aller Kunden. Ein einziger Kunde mit 100 Prozent ergäbe 10'000, viele gleich kleine Kunden ergeben einen Wert nahe null.

### Einsatz im Arbeitsalltag

**Geschäftsleitung:** Vor strategischen Entscheiden oder Kreditgesprächen zeigt die Seite die Abhängigkeit von Grosskunden. Typische Frage: Wie viele Kunden brauchen wir für 80 Prozent des Umsatzes?

**Controller:** Der Vergleich der Gesellschaften zeigt, welche Gesellschaft besonders an wenigen Kunden hängt.

**Vertriebsleiter:** Die Kunden hinter den obersten Anteilen sind die Schlüsselkunden, die besonders betreut werden sollten.

### Grenzen und Fallstricke

Weil Kunden über den Namen zusammengefasst werden, kann ein Konzern mit mehreren Gesellschaften als ein Kunde erscheinen, oder ein Kunde mit abweichender Schreibweise als mehrere. Das verändert die Anteile.

## Cross-Selling

### Wozu

Die Seite zeigt, welche Produktsparten Kunden zusammen kaufen, und leitet daraus Verkaufschancen ab: Kunden, die eine Sparte nicht kaufen, obwohl vergleichbare Kunden es tun.

### Was man sieht

Eine Heatmap „Wer A kauft, kauft auch B“ zeigt in Zeile A und Spalte B den Anteil der Kunden mit Sparte A, die auch B kaufen. Darunter steht die Liste „Ansätze je Kunde“ mit dem Kunden, der fehlenden Sparte, der Sparte, aus der der Hinweis stammt, der Wahrscheinlichkeit in Prozent und dem typischen Umsatz der Käufer dieser Sparte.

### Wie gerechnet wird

Betrachtet werden die letzten 24 Monate und nur Zeilen mit positivem Umsatz. Jeder Kunde hat einen Warenkorb aus den Sparten, die er gekauft hat. Ein Paar A und B erscheint nur, wenn mindestens 5 Kunden beide kaufen. Der Anteil in der Heatmap (Konfidenz) ist die Zahl der Kunden mit A und B geteilt durch die Zahl der Kunden mit A. Ein Ansatz erscheint, wenn die Konfidenz mindestens 40 Prozent beträgt und B bei A-Käufern häufiger vorkommt als im Durchschnitt aller Kunden (Lift über 1). Die Liste ist nach Kundenumsatz mal Wahrscheinlichkeit sortiert. „Typisch“ ist der mittlere Umsatz der Käufer der fehlenden Sparte. Nicht zugeordnete Sparten, „Others“ und reine Nummern bleiben weg.

### Einsatz im Arbeitsalltag

**Vertriebsleiter:** Man gewinnt Argumente für Zusatzverkäufe und kann Verkäufer gezielt auf Kunden ansetzen. Typische Frage: Welchem Kunden mit Sparte A lässt sich zusätzlich Sparte B anbieten?

**Verkäufer:** Die Ansatzliste dient als Gesprächsvorbereitung. Der typische Umsatz zeigt die Grössenordnung des möglichen Zusatzgeschäfts.

### Grenzen und Fallstricke

Die Sparte ist für einen grossen Teil der Zeilen ausserhalb der Schweiz leer. Cross-Selling ist deshalb vor allem für die Schweiz aussagekräftig. Ein Ansatz ist ein statistischer Hinweis, kein Beweis für Bedarf. Der Kunde kann die Sparte bei einem Mitbewerber oder über eine andere Gesellschaft decken.

## Preisstreuung

### Wozu

Die Seite zeigt, bei welchen Artikeln verschiedene Kunden sehr unterschiedliche Stückpreise bezahlen. Sie dient als Grundlage für Preisdisziplin und Konditionsgespräche.

### Was man sieht

Links steht die Liste der Artikel mit grosser Preisstreuung. Pro Artikel stehen die Zahl der Kunden, der Umsatz und der Faktor. Rechts erscheint nach Auswahl eines Artikels ein Streudiagramm. Jeder Punkt ist ein Kunde, seine Grösse zeigt die Menge. Die Achse ist logarithmisch. Ein Band zeigt den Bereich von 10 bis 90 Prozent, eine Linie den Median. Darüber stehen Median, P10 und P90 in CHF.

### Wie gerechnet wird

Je Artikel und Kunde wird der durchschnittliche Stückpreis in CHF der letzten 12 Monate berechnet, also Umsatz geteilt durch Menge, nur für Zeilen mit positiver Menge und positivem Wert. Der Faktor ist der Preis, unter dem 90 Prozent der Kunden liegen, geteilt durch den Preis, unter dem 10 Prozent liegen. So zählen einzelne Ausreisser nicht. Berücksichtigt werden Artikel mit mindestens 5 Kunden. Platzhalter-Nummern und Leistungen wie Zertifikate, Bearbeitung, Fracht oder Versand sind ausgeschlossen. Die Liste ist nach Faktor und Umsatz geordnet.

### Einsatz im Arbeitsalltag

**Vertriebsleiter:** Vor Preisrunden zeigt die Seite, wo Preise auseinanderlaufen. Typische Frage: Zahlt ein Grosskunde für denselben Artikel deutlich weniger als ein Kleinkunde?

**Verkäufer:** Vor einer Offerte vergleicht man den eigenen Preis mit Median und Band.

**Controller:** Auffällige Streuungen können auf Preislistenfehler oder Währungseffekte hindeuten.

### Grenzen und Fallstricke

Verschiedene Mengen, Länder, Währungen und Rabattstufen erklären einen Teil der Streuung legitim. Ein hoher Faktor ist daher ein Anlass zur Prüfung und kein Beleg für falsche Preise. Der Preis wird zum Kurs des Belegdatums umgerechnet, Kursschwankungen fliessen also ein.

## Saison und Prognose

### Wozu

Die Seite zeigt den Umsatzverlauf, das typische Saisonmuster und eine einfache Prognose für die nächsten 12 Monate. Sie ist bewusst nachvollziehbar gehalten und ersetzt keine Planung.

### Was man sieht

Über eine Auswahl „Sicht“ wählt man Gesamt, eine der grössten zehn Sparten oder eine Gesellschaft. Oben stehen der Umsatz der letzten 12 Monate, das Wachstum zum Vorjahr, die Prognose der nächsten 12 Monate und die typische Abweichung der Rückrechnung. Das Diagramm zeigt den Ist-Verlauf ab Datenbeginn (höchstens 36 Monate), danach die Prognose mit einem Unsicherheitsband. Das Saisonrad zeigt den Anteil jedes Monats am Monatsmittel. Ein Monat über dem Kreis ist stärker als der Durchschnitt.

### Wie gerechnet wird

Die Prognose eines Monats ist der Umsatz des gleichen Monats im Vorjahr mal dem Wachstum. Das Wachstum ist der Umsatz des aktuellen Vergleichszeitraums geteilt durch den gleichen Zeitraum des Vorjahres und auf den Bereich 0.5 bis 2 begrenzt. Das Saisonmuster ist der Durchschnitt der Monatsanteile aus bis zu drei vollen Jahren. Mit weniger als drei Jahren Daten ist es noch unsicher. Das Band beruht auf einer Rückrechnung: Das Verfahren wird auf das letzte Jahr angewendet und mit dem tatsächlichen Umsatz verglichen. Die mittlere prozentuale Abweichung je Monat steht als „±“ Wert da. Die Rückrechnung braucht drei Jahre Daten und fehlt vorerst, wenn diese nicht vorliegen.

### Einsatz im Arbeitsalltag

**Controller:** Die Prognose liefert einen schnellen Richtwert für den Jahresausblick. Typische Frage: In welchen Monaten ist mit schwachem oder starkem Umsatz zu rechnen?

**Vertriebsleiter:** Das Saisonrad hilft bei der Ferien- und Aktionsplanung.

**Geschäftsleitung:** Man vergleicht die Prognose mit dem Budget, um die Zielerreichung früh einzuschätzen.

### Grenzen und Fallstricke

Die Prognose schreibt das Vorjahr fort. Sie kennt weder Aufträge im Bestand noch Preisänderungen, Projekte oder Wechselkurse. Ein starkes Wachstum der letzten Monate wird auf alle 12 Monate übertragen und ist deshalb eher optimistisch. Weil die Begrenzung des Wachstums je Sicht gilt, ist die Summe der Prognosen der Gesellschaften oder Sparten nicht gleich der Prognose der Sicht Gesamt.

## Weltkarte

### Wozu

Die Seite zeigt, in welchen Ländern die Kunden sitzen und wohin die Gesellschaften der Gruppe liefern.

### Was man sieht

Eine stilisierte Weltkarte mit angedeuteten Grenzen zeigt Blasen, deren Fläche dem Umsatz im jeweiligen Kundenland entspricht. Mit dem Schalter „Warenfluss“ erscheinen Bögen vom Land der verkaufenden Gesellschaft zum Kundenland (die 25 grössten Verbindungen). Mit ▶ laufen die Monate ab Datenbeginn ab (höchstens 24), ein Regler wählt einen einzelnen Monat. Rechts steht die Länderliste mit Umsatz und Zahl der Kunden. Darunter steht der Umsatz ohne Kundenland.

### Wie gerechnet wird

Der Umsatz je Kundenland ist die Summe der letzten 12 Monate (oder des gewählten Monats). Die Zahl der Kunden zählt die verschiedenen Kunden mit Umsatz im Land. Die Bögen fassen den Umsatz je Paar aus Land der verkaufenden Gesellschaft und Kundenland zusammen. Lieferungen im eigenen Land erscheinen nicht als Bogen. Das Kundenland stammt aus den Rechnungsdaten.

### Einsatz im Arbeitsalltag

**Vertriebsleiter:** Man erkennt Schwerpunktmärkte und Länder, in denen die Gruppe Potenzial hat. Typische Frage: Welche Gesellschaft bedient welche Märkte?

**Geschäftsleitung:** Die Karte eignet sich als anschauliche Übersicht für Gespräche über Marktstrategie.

**Controller:** Der Umsatz ohne Kundenland zeigt die Datenqualität.

### Grenzen und Fallstricke

Das Kundenland fehlt bei rund 8 Prozent der Zeilen (Stichprobe aus einer früheren Auswertung). Dieser Umsatz erscheint nur in der Zeile „ohne Kundenland“. Die Karte ist eine Skizze, Grenzen sind nur angedeutet, Kleinstaaten sind kaum sichtbar.

## Interaktiv

### Wozu

Der Unterreiter Interaktiv ergänzt die acht klassischen Seiten um neun animierte Ansichten. Sie sollen Zusammenhänge im Kundenumsatz erlebbar machen, etwa Entwicklungen über die Zeit, Flüsse zwischen Gesellschaften, Sparten und Ländern oder die Wirkung von Preisänderungen. Sie ergänzen die übrigen Seiten und ersetzen keine davon.

### Was man sieht

Eine eigene Reiterleiste wechselt zwischen den neun Ansichten: Kunden-Galaxie, Rennen, Umsatzfluss, Sonnenstrahl, Was wäre wenn, Bestellrhythmus, Kalender, Artikel-Netzwerk und 3D-Landschaft. Die Ansichten verwenden dieselben Verkaufsdaten und Regeln wie der übrige Reiter (Finance-Regeln, ohne Konzernkunden, CHF). Die Folgeabschnitte beschreiben jede Ansicht.

### Wie gerechnet wird

Die Rechnung steht bei jeder Ansicht einzeln. Gemeinsam ist der Stichtag: Zeitfenster enden mit dem letzten vollständigen Monat, und die Animationen laufen ab dem Datenbeginn (Januar 2025).

### Einsatz im Arbeitsalltag

**Vertriebsleiter und Geschäftsleitung:** Die Ansichten eignen sich für Besprechungen, weil sie sich vorführen und anklicken lassen.

**Controller:** Die Zahlen hinter den Grafiken erscheinen beim Darüberfahren mit der Maus.

### Grenzen und Fallstricke

Einige Ansichten brauchen Daten über mehrere Quartale (Galaxie, Rhythmus). Die Animationen sind für den Bildschirm gedacht, für Berichte sind die klassischen Seiten mit Excel-Export besser geeignet.

## Kunden-Galaxie

### Wozu

Die Ansicht zeigt für die 60 grössten Kunden, wie sich Umsatz und Wachstum von Quartal zu Quartal verändern. Man sieht, welche Kunden aufsteigen und welche absteigen.

### Was man sieht

Jede Blase ist ein Kunde unter den 60 grössten der letzten 12 Monate. Waagrecht steht der Umsatz im Quartal (logarithmisch), senkrecht die Veränderung zum Vorquartal. Die Grösse der Blase zeigt die Zahl der Rechnungen, die Farbe das Land. Mit ▶ laufen die Quartale ab Datenbeginn ab, ein Regler wählt ein Quartal. Ein Klick auf eine Blase hebt den Kunden hervor, der Schalter „Spuren“ zeigt seinen Weg. Beschriftet werden nur die acht grössten Kunden je Quartal.

### Wie gerechnet wird

Der Umsatz ist die Quartalssumme des Kunden in CHF. Die Veränderung ist (Quartalsumsatz minus Vorquartal) geteilt durch Vorquartal, begrenzt auf den Bereich von minus 100 bis plus 300 Prozent. Sie fehlt, wenn es kein vollständiges Vorquartal mit Umsatz gibt oder das Quartal unvollständig ist. Ein unvollständiges Quartal ist als solches gekennzeichnet.

### Einsatz im Arbeitsalltag

**Vertriebsleiter:** Typische Frage: Welche Grosskunden wachsen stark, welche schrumpfen? Blasen weit oben und rechts sind Wachstumskunden, Blasen unten Kunden mit Einbruch.

**Verkäufer:** Mit den Spuren sieht man, ob ein Kunde nur ein schwaches Quartal hatte oder langfristig abwandert.

### Grenzen und Fallstricke

Die Auswahl der 60 Kunden beruht auf den letzten 12 Monaten. Früher grosse, inzwischen abgewanderte Kunden fehlen. Die Veränderung zum Vorquartal ist bei Kunden mit saisonalem oder projektbezogenem Geschäft stark schwankend. Die Ansicht braucht Daten über mehrere Quartale.

## Rennen

### Wozu

Die Ansicht zeigt als „Bar Chart Race“, wie sich die Rangfolge der grössten Kunden, Artikel oder Länder über die Zeit verändert.

### Was man sieht

Die 15 grössten Einträge erscheinen als Balken, die beim Abspielen mit ▶ im Monatstakt auf ihre neue Rangposition gleiten und sich überholen. Über eine Auswahl wechselt man zwischen Kunden, Artikeln und Ländern. Ein Regler wählt einen bestimmten Monat.

### Wie gerechnet wird

Für jeden Monat ab dem dritten Datenmonat wird die Summe der letzten drei Monate berechnet (gleitende 3-Monats-Summe). Daraus werden die 15 grössten Werte gezeigt. Die Glättung über drei Monate verhindert, dass ein einzelner Grossauftrag die Rangfolge allein bestimmt.

### Einsatz im Arbeitsalltag

**Geschäftsleitung:** Die Ansicht eignet sich für Präsentationen, um zu zeigen, wie sich das Kundenportfolio verschoben hat.

**Vertriebsleiter:** Typische Frage: Seit wann ist Kunde X unter den Grössten, und wer ist abgestiegen?

### Grenzen und Fallstricke

Das Rennen zeigt Rangfolgen, keine Anteile: Ein Balken auf Platz 3 kann sehr nah oder weit hinter Platz 1 liegen, das zeigt nur die Balkenlänge. Bei den Artikeln gelten dieselben Namen wie in den Rechnungen, ohne Bereinigung von Leistungen.

## Umsatzfluss

### Wozu

Die Ansicht zeigt als Sankey-Diagramm, wie der Umsatz von den Gesellschaften über die Sparten zu den Kundenländern fliesst.

### Was man sieht

Drei Spalten: links die Gesellschaften, in der Mitte die Sparten, rechts die Kundenländer (die 12 grössten, der Rest als „Übrige“). Die Breite der Ströme entspricht dem Umsatz der letzten 12 Monate. Bewegung in den Strömen zeigt die Richtung. Ein Klick auf einen Knoten zeigt nur dessen Ströme.

### Wie gerechnet wird

Es zählen nur Zeilen mit positivem Umsatz der letzten 12 Monate. Die Ströme summieren den Umsatz je Gesellschaft und Sparte sowie je Sparte und Kundenland. Zeilen ohne Sparte erscheinen als „Nicht zugeordnet“, Zeilen ohne Kundenland nur bei den „Übrigen“ bzw. mit „–“.

### Einsatz im Arbeitsalltag

**Vertriebsleiter:** Typische Frage: Welche Sparte einer Gesellschaft liefert in welche Länder? Ein Klick auf eine Gesellschaft zeigt ihr Produktmix und ihre Märkte.

**Controller:** Der grosse Block „Nicht zugeordnet“ zeigt, wie unvollständig die Sparten in den Quelldaten gepflegt sind.

### Grenzen und Fallstricke

Gutschriften sind ausgeblendet, die Summen können deshalb unter dem Nettoumsatz der anderen Seiten liegen. Die Sparte fehlt bei vielen Zeilen ausserhalb der Schweiz.

## Sonnenstrahl

### Wozu

Die Ansicht zeigt den Umsatz als kreisförmige Hierarchie von der Gesellschaft über die Sparte bis zum einzelnen Kunden. Man kann stufenweise hineinzoomen.

### Was man sieht

Innen liegen die Gesellschaften, in der Mitte die Sparten, aussen die Kunden (je Sparte die acht grössten, der Rest als „Übrige“). Ein Klick auf ein Stück zoomt hinein, ein Klick in die Mitte führt zurück. Eine Navigationsleiste oben zeigt den Pfad, eine Liste daneben die Werte und den Anteil vom Total. Beim Darüberfahren erscheint der Betrag in CHF.

### Wie gerechnet wird

Der Umsatz der letzten 12 Monate wird für jede Ebene summiert, nur Zeilen mit positivem Umsatz. Der Anteil vom Total ist der Wert des Stücks geteilt durch den Gesamtumsatz.

### Einsatz im Arbeitsalltag

**Vertriebsleiter:** Typische Frage: Welche Kunden tragen die Sparte X der Gesellschaft Y? Man zoomt von der Gesellschaft bis zum Kunden.

**Controller:** Ein schneller Weg, um Umsätze nach drei Ebenen zu prüfen, ohne Excel-Tabellen zu filtern.

### Grenzen und Fallstricke

Ein Kunde, der bei mehreren Gesellschaften kauft, erscheint mehrfach. Dieselbe Firma wird in jeder Gesellschaft und Sparte eigens ausgewiesen. Gutschriften sind ausgeblendet.

## Was wäre wenn

### Wozu

Die Ansicht ist ein Simulator. Man verändert Preis, Menge und Wechselkurs und sieht sofort, wie sich die Jahreshochrechnung verschiebt. Sie ist eine Spielwiese und keine Planung.

### Was man sieht

Links stehen Regler für Preis (minus 20 bis plus 20 Prozent), Menge (minus 30 bis plus 30 Prozent) und Fremdwährungen gegen CHF (minus 15 bis plus 15 Prozent), jeweils für alle Sparten gemeinsam. Darunter lässt sich je Sparte (die acht grössten) ein eigener Preis- und Mengenhebel einstellen, der den allgemeinen überschreibt. Rechts steht die Hochrechnung des laufenden Jahres: Wert ohne Hebel, Wert mit Hebeln, Unterschied und Veränderung gegenüber dem Vorjahr. Ein Balken zeigt Ist seit Jahresbeginn, Rest ohne Hebel und Rest mit Hebeln. Eine Tabelle listet die Wirkung je Sparte und den Währungseffekt.

### Wie gerechnet wird

Grundlage ist die Hochrechnung aus dem Controlling: Ist bis zum letzten vollständigen Monat plus die Restmonate des Jahres. Jeder Restmonat entspricht dem gleichen Monat im Vorjahr mal dem Wachstum seit Jahresbeginn (begrenzt auf 0.5 bis 2). Preis und Menge wirken multiplikativ auf die Restmonate der Sparte, der Faktor ist also (1 plus Preis) mal (1 plus Menge). Der Währungshebel wirkt auf den Teil der simulierten Restmonate, der in Fremdwährung fakturiert wird. Das bereits fakturierte Ist bleibt unverändert.

### Einsatz im Arbeitsalltag

**Controller:** Man schätzt ab, was eine Preiserhöhung von 2 Prozent oder ein schwächerer Euro für das Jahresergebnis bedeutet. Typische Frage: Wie viel Umsatz bringt ein Prozentpunkt Preis in der grössten Sparte?

**Vertriebsleiter:** Vor Preisrunden lässt sich die Wirkung gegen einen möglichen Mengenverlust abwägen.

**Geschäftsleitung:** Der Simulator zeigt die Empfindlichkeit des Jahresumsatzes auf Preis, Menge und Währung.

### Grenzen und Fallstricke

Der Simulator rechnet keine Reaktion der Kunden: Eine Preiserhöhung senkt die Menge nicht von selbst. Er rechnet mit Umsatz, nicht mit Marge. Die Basis ist eine einfache Fortschreibung und keine Planung. Der Währungshebel gilt pauschal für alle Fremdwährungen zugleich.

## Bestellrhythmus

### Wozu

Die Ansicht erkennt, welche Kunden nach ihrem üblichen Rhythmus eigentlich schon wieder hätten bestellen sollen. Das ist ein Frühwarnsystem für ausbleibende Folgeaufträge.

### Was man sieht

Links steht die Liste „Sollte eigentlich bestellt haben“ mit dem Kunden und der Überfälligkeit in Tagen („+33 Tage“ oder „im Takt“). Rot bedeutet mehr als ein üblicher Abstand überfällig, orange leicht überfällig. Ein Schalter blendet nur überfällige Kunden ein, eine Suche findet einen Kunden. Rechts steht für den gewählten Kunden die Lebenslinie: jeder Punkt ist ein Tag mit Rechnung, ein Balken zeigt den üblichen Abstand ab der letzten Bestellung, eine Linie das erwartete Datum, eine weitere den Datenstand. Dazu stehen Land, üblicher Abstand, letzte Bestellung und erwartetes Datum.

### Wie gerechnet wird

Berücksichtigt werden Kunden mit mindestens vier Bestelltagen. Der übliche Abstand ist der Median der Abstände zwischen aufeinanderfolgenden Bestelltagen, auf ganze Tage gerundet. Das erwartete Datum ist die letzte Bestellung plus dieser Abstand. Überfällig sind die Tage zwischen dem erwarteten Datum und dem Stichtag, also dem jüngsten Tag in den Verkaufsdaten. Die Sortierung folgt der Zahl der überfälligen Abstände mal dem Umsatz der letzten 12 Monate, damit grosse Kunden oben stehen.

### Einsatz im Arbeitsalltag

**Verkäufer:** Die Liste ist eine tägliche Anrufliste. Typische Frage: Welcher regelmässige Kunde hat sich länger als üblich nicht gemeldet?

**Vertriebsleiter:** Eine Häufung roter Einträge bei einer Gesellschaft kann auf ein Lieferproblem oder einen Marktwechsel deuten.

### Grenzen und Fallstricke

Der Rhythmus kennt keine Saison und keine Rahmenverträge. Ein Kunde mit Abrufen aus einem Rahmenvertrag oder mit Projektgeschäft hat keinen echten Takt, die Überfälligkeit ist dann kein Alarm. Die Ansicht braucht mehrere Bestelltage und damit eine genügende Datenhistorie.

## Kalender

### Wozu

Die Ansicht zeigt den Umsatz je Tag eines Jahres als Wärmebild. Man erkennt Wochenmuster, Monatsenden und Ausreisser auf einen Blick.

### Was man sieht

Ein Kästchen steht für jeden Tag des gewählten Jahres. Je dunkler die Farbe, desto höher der Umsatz. Das Jahr wird über Schaltflächen gewählt. Beim Darüberfahren erscheinen Datum, Umsatz und Zahl der Rechnungen, ein Klick listet die Rechnungen des Tages. Eine Legende von „wenig“ bis „viel“ und der beste Tag des Jahres stehen darunter.

### Wie gerechnet wird

Je Tag wird der Nettoumsatz in CHF aller Rechnungen mit diesem Belegdatum summiert, dazu die Zahl der verschiedenen Rechnungen. Die Farbskala endet beim 95-Prozent-Wert aller Tage, damit einzelne Rekordtage die Farben nicht flach drücken.

### Einsatz im Arbeitsalltag

**Controller:** Typische Frage: Wann fallen die grossen Rechnungen an, und gibt es ein Muster zum Monatsende? Ein Monatsende-Stau kann für die Abschlussplanung wichtig sein.

**Vertriebsleiter:** Der Kalender zeigt Lücken ohne Fakturierung und Spitzentage.

### Grenzen und Fallstricke

Das Datum richtet sich nach den Finance-Regeln, bei Spanien ist es das Rechnungsdatum. Es entspricht nicht dem Bestelleingang. Im laufenden, noch nicht vollständigen Monat zeigt der Kalender die bisher vorhandenen Tage, die letzten Tage können fehlen.

## Artikel-Netzwerk

### Wozu

Die Ansicht zeigt, welche Artikel von denselben Kunden gekauft werden. So lassen sich Produktfamilien und Zusatzverkäufe auf Artikelebene erkennen.

### Was man sieht

Jeder Punkt ist einer der 40 Artikel mit den meisten Kunden der letzten 12 Monate. Eine Linie verbindet zwei Artikel, wenn sie von mindestens so vielen gemeinsamen Kunden gekauft wurden, wie der Regler vorgibt (zwischen 2 und 15). Dickere Linien bedeuten engere Verbindungen. Die Farbe bezeichnet eine zusammenhängende Gruppe. Fährt man mit der Maus über einen Artikel, werden die Nachbarn hervorgehoben, ein Klick hält die Auswahl und zeigt rechts die Kundenzahl, den Umsatz und die Artikel, die oft zusammen gekauft werden.

### Wie gerechnet wird

Berücksichtigt werden Zeilen mit positivem Umsatz der letzten 12 Monate, ohne Leistungen und Platzhalter-Nummern (Versandpauschalen eingeschlossen). Die Verbindungsstärke ist die Zahl der gemeinsamen Kunden geteilt durch die Kundenzahl des kleineren Artikels. Die Lage der Punkte ergibt sich aus einem Kräfteverfahren, bei dem sich Punkte abstossen und Linien sie zusammenziehen. Die Lage ist auf dem Server berechnet und fest, ein Verschieben mit der Maus ist nicht möglich.

### Einsatz im Arbeitsalltag

**Verkäufer:** Typische Frage: Welche Artikel verkauft man zusammen mit Artikel X? Das hilft beim Zusatzangebot.

**Vertriebsleiter:** Zusammenhängende Gruppen deuten auf Produktfamilien oder Branchenlösungen hin, die sich gemeinsam vermarkten lassen.

### Grenzen und Fallstricke

Die Ansicht ist auf 40 Artikel begrenzt. Seltene Artikel mit wenigen Kunden erscheinen nicht. Gemeinsames Kaufen bedeutet nicht, dass ein Artikel den anderen bedingt. Beide können einfach beliebt sein. Die Abstände auf der Karte sind nur ein Hinweis auf Nähe und kein Mass.

## 3D-Landschaft

### Wozu

Die Ansicht stellt den Umsatz nach Monat und Sparte als räumliche Säulenlandschaft dar. Man sieht Entwicklung und Verteilung auf einmal.

### Was man sieht

Jede Säule steht für einen Monat einer Sparte (die acht grössten Sparten, alle Monate ab Datenbeginn). Die Höhe zeigt den Umsatz. Die Landschaft lässt sich automatisch drehen oder mit Reglern für Blickwinkel und Neigung einstellen. Ein Klick auf eine Säule zeigt den Wert in CHF und den Vergleich mit dem gleichen Monat des Vorjahres in Prozent.

### Wie gerechnet wird

Der Umsatz je Monat und Sparte wird summiert. Die Säulenhöhe ist proportional zur Wurzel des Anteils am grössten Wert. So bleiben kleine Werte sichtbar, grosse Säulen werden aber optisch gestaucht. Negative Werte (zum Beispiel durch Gutschriften) und Null erhalten eine Mindesthöhe. Der Vorjahresvergleich ist der Wert geteilt durch den gleichen Monat des Vorjahres, minus eins.

### Einsatz im Arbeitsalltag

**Geschäftsleitung:** Die Ansicht eignet sich für Präsentationen, um die Entwicklung der Sparten eindrücklich zu zeigen.

**Vertriebsleiter:** Typische Frage: Welche Sparte ist saisonal, welche wächst stetig? Der Klick liefert den genauen Wert.

### Grenzen und Fallstricke

Durch die Wurzelskala sind Höhenverhältnisse nicht proportional: Eine doppelt so hohe Säule bedeutet nicht doppelten Umsatz. Für genaue Werte ist der Klick auf die Säule oder die Seite Saison und Prognose besser geeignet. Zeilen ohne Sparte erscheinen als „Nicht zugeordnet“.

## Kennzahlen-Glossar

| Kennzahl | Bedeutung | Berechnung in Worten |
|---|---|---|
| Umsatz CHF, 12 Monate | Nettoumsatz der letzten 12 vollständigen Monate | Summe aller Rechnungszeilen nach Finance-Regeln, ohne Konzernkunden, in CHF zum Kurs des Belegdatums, Gutschriften negativ |
| Vergleichszeitraum | Zeitraum für den Vorjahresvergleich | Höchstens 12 Monate, so gewählt, dass der Vorjahreszeitraum ganz in den Daten liegt |
| Veränderung | Wachstum gegenüber dem Vorjahr | (aktueller Zeitraum minus gleicher Zeitraum im Vorjahr) geteilt durch den Vorjahreswert |
| Eingeschlafen | Kunde ohne Umsatz seit 6 Monaten | Kein Umsatz in den letzten 6 Monaten und aktueller Zeitraum unter Vorjahr |
| Fehlender Umsatz | Umsatzverlust eines Kunden gegenüber dem Vorjahr | Vorjahreswert minus aktueller Wert |
| Neuer Kunde | Kunde mit erstem Kauf im Quartal | Erster Kauf mit positivem Umsatz im Datenbestand liegt im Quartal |
| Verlorener Kunde | Kunde ohne Kauf nach einem Quartal | Letzter Kauf liegt im Quartal, seither nichts; angezeigt ab 6 Monaten, endgültig nach 12 Monaten |
| Top-n-Anteil | Anteil der n grössten Kunden am Umsatz | Summe der n grössten Kundenumsätze geteilt durch den Gesamtumsatz der letzten 12 Monate |
| HHI | Mass für die Konzentration der Kundenumsätze | Summe der quadrierten Prozentanteile aller Kunden, unter 1500 breit gestreut |
| Konfidenz (Cross-Selling) | Wahrscheinlichkeit, dass ein Kunde mit Sparte A auch B kauft | Kunden mit A und B geteilt durch Kunden mit A, über 24 Monate |
| Lift | Verhältnis der Konfidenz zur allgemeinen Häufigkeit von B | Konfidenz geteilt durch den Anteil aller Kunden, die B kaufen; über 1 heisst überdurchschnittlich |
| Faktor (Preisstreuung) | Breite der Preisspanne eines Artikels | 90-%-Wert des Stückpreises geteilt durch den 10-%-Wert, je Kunde |
| Prognose | Erwarteter Umsatz der nächsten 12 Monate | Gleicher Monat im Vorjahr mal Wachstum (begrenzt auf 0.5 bis 2) |
| Rückrechnung (MAPE) | Typische Abweichung der Prognose | Mittlere prozentuale Abweichung bei Anwendung des Verfahrens auf das letzte Jahr, erst ab drei Jahren Daten |
| Saisonindex | Stärke eines Monats gegenüber dem Durchschnitt | Monatsumsatz geteilt durch das Monatsmittel, gemittelt über bis zu drei Jahre |
| Üblicher Abstand | Typischer Zeitraum zwischen zwei Bestellungen eines Kunden | Median der Abstände zwischen den Bestelltagen, ab vier Bestelltagen |
| Überfällig (Tage) | Verspätung gegenüber dem Rhythmus | Stichtag minus (letzte Bestellung plus üblicher Abstand) |
| Gleitende 3-Monats-Summe | Glättung für das Rennen | Umsatz des Monats plus der beiden Vormonate |
