# Teams-Nachricht an Rohail Munir, 2026-09-08

Entwurf zum Kopieren. Anlass: Rohail hat morgens nach der Datenaufbereitung fuer den
Bahnmarkt gefragt, obwohl Patrik die Zuordnung nicht gemacht hat. Hintergrund und Befunde:
`docs/BAHNMARKT_DE_ANBINDUNG_2026-09-08.md`.

Als **zwei** Nachrichten senden, das liest sich in Teams besser als ein Block.

## Nachricht 1: Danke, Fund, und was fehlt

> Hoi Rohail
>
> Danke für die Adressliste, die hat mir gleich etwas gezeigt, womit ich nicht gerechnet
> hatte: Ihr pflegt in Alphaplan eine Branche, und dort stehen **99 Adressen als Bahn** —
> Deutsche Bahn, Siemens Mobility, Knorr-Bremse, Bombardier, Wabtec, MAHLE und weitere. Das
> ist deutlich belastbarer als das, was wir aus den anderen Ländern haben, denn dort raten
> wir die Bahnkunden bisher über einen Namensabgleich.
>
> Eine Sache brauche ich aber noch, sonst kann ich die Namen nicht an die Umsatzdaten
> hängen. Deine Liste enthält die Adressnummer, also 10000 aufwärts. Unsere Verkaufszeilen
> aus Alphaplan tragen dagegen eine andere, interne Nummer, die bei 10 anfängt. Die beiden
> lassen sich nicht zuordnen.
>
> Zwei Möglichkeiten:
>
> Entweder du exportierst die Adressliste nochmal und hakst im Exportassistenten zusätzlich
> die interne ID an. Das Feld heisst meist `ID`, `AdressenID` oder `Datensatz-Nr.` und wird
> in der Maske nie angezeigt, lässt sich aber mitexportieren.
>
> Oder du gibst mir einen Rechnungsexport mit Belegnummer, Adressnummer und Name. Über die
> Belegnummer stelle ich die Verbindung dann selbst her.
>
> Sobald das da ist, sehen wir zum ersten Mal deutschen Bahnumsatz je Kunde. Bis dahin
> schicke ich dir gern schon die Liste der 99 Bahnkunden aus eurem System, falls das für den
> Moment hilft.
>
> Gruss
> Ingo

## Nachricht 2: Was schon steht

Anhang: `Bahnmarkt_Datenaufbereitung_2026-09-08.xlsx`

> Was schon steht: Für die anderen acht Standorte habe ich die Bahnauswertung fertig, ich
> hänge sie dir an. 172 Bahnkunden mit Umsatz 2025 und 2026, sortier- und filterbar.
>
> Die Zahlen für 2026 bisher: Schweiz 1'834'842 CHF über 54 Kunden, Spanien 641'928 EUR,
> Italien 281'087 EUR, Frankreich 230'594 EUR, UK 208'194 GBP, Österreich 176'169 EUR und
> Indien 26'298'606 INR über einen einzigen Kunden. Bewusst keine Gesamtsumme, die Währungen
> stehen nebeneinander.
>
> Ein Vorbehalt, den du kennen solltest, bevor die Zahlen weiterwandern: Von den 172
> Zuordnungen ist erst **eine** fachlich geprüft. Der Rest sind maschinelle Vorschläge aus
> einem Namensabgleich, und der liegt nachweislich auch mal daneben. In der Datei steht bei
> jeder Zeile, was gilt. Die Prüfung liegt bei Patrik und ist noch offen.
>
> Deutschland fehlt in dieser Datei aus dem Grund, den ich oben beschrieben habe. Mit eurer
> Branchenpflege wird das für Deutschland am Ende sogar die sauberste Zuordnung von allen —
> die anderen Länder haben so ein Feld nicht.

## Warum der letzte Absatz drin ist

Er macht aus „Deutschland fehlt" ein „Deutschland wird das beste" und gibt Rohail einen
Grund, die interne ID schnell zu liefern.
