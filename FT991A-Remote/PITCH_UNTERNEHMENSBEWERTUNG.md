# MacYaesu: Pitch und Unternehmensbewertung

Stand: 2. August 2026

## Kurzurteil

MacYaesu ist derzeit eher ein aussichtsreiches, profitables Nischenprodukt als
ein klassisches Venture-Unternehmen. Das technische Problem ist real und die
Loesung differenziert. Fuer einen Verkauf fehlen vor allem ein reproduzierbarer
Release-Build, Signierung und Notarisierung, externe Hardwaretests,
automatisierter Vertrieb und erste zahlende Kunden.

Als reines FT-991A-Produkt ist der Markt begrenzt. Investierbar wird das
Unternehmen vor allem durch eine spaetere Erweiterung auf weitere
Yaesu-Transceiver und eine gemeinsame CAT-, Diagnose- und Remote-Plattform fuer
macOS.

## 90-Sekunden-Pitch

> Hallo Loewen, ich bin Ingo und ich entwickle MacYaesu - eine native
> macOS-Steuerung fuer den Yaesu FT-991A.
>
> Funkamateure koennen ihren Transceiver zwar per USB mit dem Mac verbinden,
> doch die Praxis ist frustrierend: mehrere serielle Schnittstellen, unklare
> Treiber, falsche RTS-/DTR-Einstellungen, Echo-Antworten und Software, die eine
> geoeffnete Schnittstelle bereits als funktionierende Verbindung meldet.
>
> MacYaesu loest genau dieses Problem. Die App erkennt den richtigen
> CP2105-Enhanced-Port, prueft die Verbindung mit einem echten CAT-Handshake
> und bietet Frequenzsteuerung, Speicherkanale, Repeater- und
> Tone-Konfiguration, Logging sowie ein Support-Bundle fuer die Diagnose.
>
> Das Produkt richtet sich nicht an Millionen beliebiger Nutzer, sondern an
> eine zahlungsbereite technische Nische: Besitzer eines FT-991A, die ihren Mac
> ohne Windows, Wine oder virtuelle Maschine im Funkbetrieb verwenden wollen.
>
> MacYaesu wird als notarisiertes macOS-Produkt direkt verkauft. Der
> Einstiegspreis liegt bei 29 Franken, die Pro-Version bei 59 Franken. Spaeter
> wird die Plattform auf weitere Yaesu-Modelle erweitert.
>
> Ich suche 60.000 Franken fuer 20 Prozent. Das Kapital fliesst in
> Produktreife, Hardwaretests, Signierung und Notarisierung, automatisierten
> Verkauf sowie den Aufbau einer internationalen Amateurfunk-Community.

## Das geloeste Problem

Der technische Nutzen ist bereits konkret nachgewiesen:

- Der FT-991A praesentiert auf macOS zwei CP2105-Schnittstellen.
- Fuer den dokumentierten Aufbau ist der Enhanced-Port erforderlich.
- Funktionierendes Profil: `38400 Baud`, RTS an und DTR aus.
- Ein geoeffneter Port ist noch keine bestaetigte CAT-Verbindung.
- Echo, Command Overflow und zu aggressives Polling muessen behandelt werden.
- MacYaesu besitzt CAT-Trace, Ping, Auto-Ping und Support-Bundle.

Der staerkste Produktvorteil ist nicht allein das Senden von CAT-Kommandos,
sondern die verlaessliche Einrichtung und Diagnose einer problematischen
Hardware-/Software-Kombination.

## Positionierung

Schwache Positionierung:

> Noch eine Funkgeraete-App fuer den Mac.

Empfohlene Positionierung:

> Die zuverlaessige native Yaesu-Arbeitsumgebung fuer den Mac - angefangen mit
> dem FT-991A, bei dem bestehende Mac-Loesungen eine besonders schmerzhafte
> Luecke lassen.

## Geschaeftsmodell

> Umgesetzt ist seit 2026-10 ein Einheitspreis von 49 CHF (Vollversion,
> Updates der Hauptversion inklusive). Die Staffel unten ist eine spaetere
> Option, kein aktueller Stand.

| Angebot | Inhalt | Preisidee |
|---|---|---:|
| Connection Doctor | Porterkennung, CAT-Test und Diagnose | kostenlos |
| MacYaesu Standard | Steuerung, Speicher, Repeater und Basislogging | 29 CHF |
| MacYaesu Pro | erweitertes Logging, Profile, Export und Supportfunktionen | 59 CHF |
| Major Upgrade | neue Hauptversion | 19-29 CHF |

Die kostenlose Diagnoseversion soll beweisen, dass MacYaesu das angeschlossene
Funkgeraet erkennt. Nach einem erfolgreichen Verbindungstest kann die
Vollversion angeboten werden.

Eine Demo von nur 15 Minuten ist fuer ein Hardwareprodukt zu kurz. Treiber,
Kabel, Portwahl und CAT-Menue koennen bereits mehr Zeit beanspruchen. Bessere
Varianten:

- unbegrenzte Diagnose;
- sieben Tage Vollversion;
- oder dauerhaft nutzbare Grundfunktionen mit zeitlich begrenzten
  Pro-Funktionen.

Ein Abonnement wird fuer den Start nicht empfohlen. Einmalkauf und bezahlte
Hauptversions-Upgrades passen voraussichtlich besser zur Zielgruppe.

## Umsatzszenarien

Es gibt keine belastbare oeffentliche Zahl zur aktiven
FT-991A-macOS-Nutzerbasis. Deshalb handelt es sich um Szenarien und nicht um
Marktdaten.

| Szenario | Kaeufer | Durchschnittspreis | Umsatz |
|---|---:|---:|---:|
| vorsichtig | 250 | 39 CHF | 9.750 CHF |
| realistisch | 1.000 | 45 CHF | 45.000 CHF |
| stark | 3.000 | 49 CHF | 147.000 CHF |

Die Zahlen sprechen fuer ein moegliches Indie-Softwaregeschaeft. Fuer ein
groesseres Unternehmen muss der Produktkern weitere Funkgeraete unterstuetzen.

## Plattformvision

```text
MacYaesu fuer FT-991A
        -> weitere Yaesu-Modelle
        -> gemeinsame CAT- und Diagnoseplattform
        -> Audio- und Digimode-Integration
        -> lokale und echte Remote-Steuerung
```

Der FT-991A bleibt Einstiegsprodukt, Referenzhardware und technischer Beweis.
Die interne Architektur sollte deshalb modellabhaengige Faehigkeiten und
Geraeteprofile ermoeglichen, statt CAT-Logik dauerhaft fest mit dem FT-991A zu
verdrahten.

## Voraussetzungen fuer einen Verkauf

1. Projekt, Target und Scheme einheitlich von `MacYesu` auf `MacYaesu`
   umbenennen.
2. Buildprobleme durch `#Preview` bereinigen.
3. Reproduzierbaren Release-Build herstellen.
4. Developer-ID-Signierung und Apple-Notarisierung einrichten.
5. DMG mit sauberer Installation erstellen.
6. Automatische Updates vorsehen.
7. Mindestens 10 bis 20 externe FT-991A-Tester gewinnen.
8. Apple Silicon, Intel und mehrere macOS-Versionen testen.
9. PayPal und manuelle Lizenzmail durch einen automatisierten Checkout
   ersetzen.
10. Verbindungsabbruch, Wiederverbindung, CAT-Fehler und TX systematisch
    testen.

## TX- und Produktsicherheit

PTT ist sicherheitskritischer als eine normale Schaltflaeche. Eine unbeabsichtigte
Aussendung muss konstruktiv verhindert werden. Empfohlen:

- PTT standardmaessig deaktivieren;
- explizite Freigabe durch den Benutzer;
- deutlich sichtbarer TX-Zustand;
- konfigurierbare maximale TX-Zeit;
- sofortiger TX-Not-Aus;
- keine PTT-Aktivierung waehrend Texteingaben;
- Hardwaretests fuer `TX1`, `TX0`, Verbindungsabbruch und App-Absturz.

Die bisherige Shift-Taste als PTT-Ausloeser sollte vor dem Verkauf neu bewertet
werden.

## Support als Teil des Produkts

USB-Treiber, Kabel, CAT-Einstellungen und Portvarianten koennen hohe
Supportkosten erzeugen. Deshalb sind folgende Funktionen zentral und nicht nur
technische Extras:

- automatischer Connection Doctor;
- echter CAT-Handshake;
- klare Unterscheidung zwischen Port offen und Funkgeraet erreichbar;
- Erkennung von Echo und Command Overflow;
- CAT-Trace;
- Support-Bundle;
- gefuehrter Setup-Assistent;
- getestete Geraeteprofile.

Ziel ist, dass die App einen grossen Teil der Supportdiagnose selbst erledigt.

## Kritische Investorenfragen

### Warum verwendet der Kunde keine kostenlose Software?

Der Kunde bezahlt nicht nur fuer CAT-Kommandos, sondern fuer einen getesteten,
nativen Mac-Workflow, automatische Diagnose, geringere Einrichtungszeit und
verlaesslichen Support.

### Warum wird nur ein Funkgeraet unterstuetzt?

Der FT-991A ist der fokussierte Einstieg. Nach einem stabilen Produktkern
werden weitere Yaesu-Profile ergaenzt. Diese Erweiterung muss durch die
Architektur vorbereitet sein.

### Was schuetzt vor Kopien?

Nicht allein der Offline-Lizenzschluessel. Der wirksamere Schutz besteht aus
Hardwarewissen, getesteten Geraeteprofilen, Updates, Diagnosequalitaet,
Support und Community-Vertrauen.

### Wie hoch ist der Supportaufwand?

Das ist eines der groessten Geschaeftsrisiken. Er muss durch Connection Doctor,
Setup-Assistent und Support-Bundle reduziert und nach dem Start gemessen werden.

### Wofuer werden 60.000 CHF verwendet?

Die Summe ist fuer ein reines FT-991A-Produkt schwer zu rechtfertigen. Sie wird
erst plausibel, wenn damit mehrere Geraetemodelle, Hardwaretests,
automatisierter Vertrieb und internationale Vermarktung finanziert werden.
Die erste FT-991A-Version sollte nach Moeglichkeit eigenfinanziert werden.

## Nachweis vor einer Investition

Empfohlener Meilenstein:

> 100 zahlende Kunden, weniger als 10 Prozent Rueckerstattungen, weniger als
> eine Supportanfrage pro fuenf Verkaeufe und mindestens 30 aktive Nutzer nach
> drei Monaten.

Erst danach ist belegt, dass aus dem technischen Projekt ein wiederholbares
Geschaeft entstehen kann.

## Bewertung

| Bereich | Bewertung |
|---|---:|
| Problem | 9/10 |
| technische Differenzierung | 8/10 |
| Produktreife | 5/10 |
| aktueller Marktumfang | 4/10 |
| Erweiterungspotenzial | 8/10 |
| Chance als profitables Indie-Unternehmen | 8/10 |
| Chance als klassischer Investoren-Deal | 3/10 |

## Schlussfolgerung

MacYaesu loest ein glaubwuerdiges Problem und besitzt gute Voraussetzungen fuer
ein rentables Nischenprodukt. Der naechste Engpass ist nicht die Idee, sondern
Produktreife und Marktnachweis. Die richtige Reihenfolge lautet:

1. stabiler und sicherer Release;
2. externe Beta mit echter FT-991A-Hardware;
3. erste zahlende Kunden;
4. Supportaufwand und Nutzung messen;
5. erst danach weitere Yaesu-Modelle und groessere Finanzierung.

Ein Investor sollte nicht auf eine hypothetische Marktgroesse setzen, sondern
auf nachgewiesene technische Kompetenz, eine kleine loyale Zielgruppe und die
Faehigkeit, aus dem ersten Geraeteprofil eine breitere Mac-Funkplattform zu
entwickeln.
