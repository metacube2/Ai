# Trafag Reddit

Trafag Reddit ist das interne Forum des Cockpits. Hier ist Platz für alles, was in SharePoint keinen Ort findet: Fragen an Kolleginnen und Kollegen, Ideen und Verbesserungen, SAP- und Excel-Tipps, lesenswerte Links, der Marktplatz und Themen rund um Kantine und Freizeit. Es verbindet zwei bekannte Vorbilder: von Reddit die Communities, das Abstimmen mit Pfeil hoch und Pfeil runter und die verschachtelten Kommentare, von StackOverflow die Fragen mit akzeptierter Antwort, die Tags und die Reputation. Der Reiter liegt auf oberster Menüebene und ist für alle sichtbar. Geschrieben wird immer mit dem eigenen Windows-Namen; ohne Windows-Anmeldung kann man nur lesen.

## Startseite und Feed

### Wozu

Der Feed zeigt alle Beiträge, die besten zuoberst. Wer eine Frage hat, sieht zuerst, ob es dazu schon eine Antwort gibt; wer eine Idee hat, sieht, ob andere sie unterstützen.

### Was man sieht

Oben ein orangefarbenes Band mit Suche, der Schaltfläche „Beitrag erstellen“ und Zahlen zu Beiträgen, Kommentaren, Stimmen, Mitwirkenden und offenen Fragen. Darunter die Sortierleiste: Angesagt, Neu, Top (mit Zeitraum Heute bis Seit Beginn), Aktiv, Offene Fragen und Kontrovers. Jeder Beitrag ist eine Karte mit den Abstimmungspfeilen links, der Community, der Art (Diskussion, Frage, Link, Idee; eine Frage mit akzeptierter Antwort heisst „Gelöst“), dem Titel, einer Vorschau des Textes, den Tags, dem Namen mit Stufe und Reputation sowie der Zahl der Kommentare und Aufrufe. Rechts die eigene Reputation mit Fortschrittsbalken zur nächsten Stufe, die Liste der Communities, die Ruhmeshalle der Mitwirkenden mit der höchsten Reputation, beliebte Tags und die Spielregeln.

### Wie gerechnet wird

- **Punkte eines Beitrags:** Stimmen hoch minus Stimmen runter. Ein zweiter Klick auf denselben Pfeil nimmt die eigene Stimme zurück. Für eigene Beiträge kann man nicht abstimmen.
- **Angesagt:** wie bei Reddit. Der Zehnerlogarithmus der Punkte plus das Alter, wobei 12,5 Stunden so viel wiegen wie eine Zehnerpotenz an Punkten. Neue Beiträge mit wenigen Stimmen stehen so eine Weile oben, alte rutschen nach unten. Angeheftete Beiträge stehen immer zuerst.
- **Top:** nach Punkten im gewählten Zeitraum.
- **Aktiv:** nach der letzten Aktivität (neuer Kommentar, akzeptierte Antwort).
- **Offene Fragen:** Fragen ohne akzeptierte Antwort, die mit den wenigsten Kommentaren zuerst.
- **Kontrovers:** viele Stimmen, beide Seiten ähnlich stark.
- **Suche:** alle eingegebenen Wörter müssen in Titel, Text, Tags oder Autorname vorkommen.

### Einsatz im Arbeitsalltag

- **Alle Mitarbeitenden:** Erst suchen, dann fragen. Beim Tippen des Titels zeigt das Formular ähnliche Beiträge.
- **Fachspezialisten (SAP, IT, Einkauf):** Unter „Offene Fragen“ nachsehen, wo eine Antwort fehlt; gute Antworten bringen Reputation.
- **Abteilungsleiter und Geschäftsleitung:** Unter „Ideen“ mit „Top“ sehen, welche Verbesserungen die meisten Stimmen haben.

### Grenzen und Fallstricke

Die Inhalte sind für alle im Cockpit sichtbar. Vertrauliche Kunden-, Personal- oder Finanzdaten gehören nicht hinein. Es gibt keine Benachrichtigung per Mail; neue Beiträge sieht man beim nächsten Besuch, offene Seiten zeigen einen Hinweis „Neues im Forum“. Bilder und Dateianhänge sind nicht vorgesehen, Links schon.

## Beitrag und Antworten

### Wozu

Auf der Beitragsseite wird diskutiert und geantwortet. Bei Fragen markiert die fragende Person die Antwort, die geholfen hat.

### Was man sieht

Titel, Autor, der vollständige Text mit Formatierung, ein Link als Karte, Tags und darunter die Aktionen Speichern, Teilen (kopiert den Link), Bearbeiten und Löschen (nur eigene Beiträge). Darunter das Antwortfeld und die Kommentare als Baum. Jede Ebene hat eine Linie, mit der man den Zweig einklappt. Die akzeptierte Antwort steht grün umrandet zuoberst. Die Autorin oder der Autor des Beitrags ist bei den Kommentaren mit „OP“ markiert.

### Wie gerechnet wird

- **Reihenfolge der Kommentare:** akzeptierte Antwort zuerst, dann nach der Wilson-Untergrenze der Stimmen (wie Reddit „Beste“). Ein Kommentar mit 50 Stimmen hoch und 2 runter steht vor einem mit 3 zu 0.
- **Reputation:** Jede Stimme hoch für einen eigenen Beitrag oder Kommentar gibt 10 Punkte, jede Stimme runter kostet 2. Eine akzeptierte Antwort gibt der antwortenden Person 15 Punkte und der fragenden 2. Nie unter 0.
- **Stufen:** Neuling unter 50, Mitglied ab 50, Kenner ab 200, Experte ab 1000, Legende ab 5000 Punkten.
- **Aufrufe:** jeder Besuch der Beitragsseite zählt einmal.

### Einsatz im Arbeitsalltag

- **Fragende:** Wenn eine Antwort geholfen hat, mit dem Haken akzeptieren. So sehen andere sofort die Lösung, und die Frage verschwindet aus „Offene Fragen“.
- **Antwortende:** Kurz, konkret, mit Transaktion oder Menüpfad. Formatierung: `**fett**`, `*kursiv*`, `- Liste`, `> Zitat`, Code in Backticks und Links als `[Text](https://...)`.

### Grenzen und Fallstricke

Gelöschte Kommentare mit Antworten bleiben als „[gelöscht]“ stehen, damit der Faden lesbar bleibt. Bearbeitete Beiträge tragen den Vermerk „bearbeitet“, eine Änderungshistorie gibt es nicht. Anheften und das Löschen fremder Beiträge sind Admins vorbehalten (Admin-Passwort entsperrt).

## Communities

### Wozu

Communities ordnen die Beiträge nach Thema, damit man nur sieht, was einen interessiert.

### Was man sieht

Zu Beginn gibt es acht Communities: Allgemein, Fragen & Antworten, Ideen & Verbesserungen, SAP-Tipps, IT & Tools, Fundstücke & Links, Marktplatz, Kantine & Freizeit. Jede hat eine Farbe, ein Symbol und eine Kurzadresse wie r/sap. Wer angemeldet ist, kann mit dem Plus eine neue Community anlegen (Name, Beschreibung, Symbol, Farbe).

### Wie gerechnet wird

- **Beiträge:** Anzahl nicht gelöschter Beiträge der Community.
- **Mitwirkende:** Anzahl verschiedener Personen, die dort geschrieben oder kommentiert haben.

### Einsatz im Arbeitsalltag

- **Teams und Abteilungen:** Für wiederkehrende Themen eine eigene Community anlegen, zum Beispiel für Lean, 5S oder ein Projekt.

### Grenzen und Fallstricke

Doppelte Namen sind nicht möglich. Communities lassen sich in der Oberfläche nicht umbenennen oder löschen; das erledigt bei Bedarf ein Admin.
