# MacYaesu Forumstexte (Entwürfe)

Zum Posten durch den Entwickler unter eigenem Namen und Rufzeichen. Reihenfolge
und Regeln stehen in `MARKETING_PLAN.md`. Ton: Projektvorstellung, keine Werbung.
Vor dem Posten die Platzhalter `[CALL]` und `[VIDEO]` füllen und die Regeln der
jeweiligen Gruppe zu Eigenwerbung lesen.

Hinweis zum Gatekeeper: Solange die App nicht notarisiert ist, steht in jedem
Text ein Satz zum ersten Start. Nach der Notarisierung diesen Satz streichen.

## Regeln fürs Posten

Viele Amateurfunk-Gruppen reagieren allergisch auf Werbung von Fremden, nicht
aber auf einen OM, der offen sagt, was er gebaut hat und was es kostet.

**Vorher**
- Die Regeln der Gruppe lesen. Viele groups.io-Gruppen verbieten kommerzielle
  Posts oder moderieren sie. Im Zweifel zuerst den Moderator fragen (Vorlage
  unten).
- Reddit: Es gibt Regeln gegen Eigenwerbung. Ein Konto, das nur zum Verlinken
  auftaucht, wird gelöscht. Vorher im Subreddit mitreden.
- Höchstens ein bis zwei Gruppen pro Woche, nicht alle am selben Tag.

**Im Post**
- Mit Rufzeichen auftreten und offen sagen, dass du der Entwickler bist, was
  die App kostet und dass es eine Demo gibt.
- Mit dem Problem anfangen, nicht mit dem Produkt.
- Etwas mitbringen, das auch ohne Kauf hilft: die Anleitung und die Tabelle
  zur Fehlersuche.
- Keine Werbesprache („revolutionär“, „das beste“).
- **Angebot:** Die ersten 10 Tester bekommen eine kostenlose Lizenz gegen
  ehrliches Feedback. Schlüssel mit `./generate-license.command <mail>` erzeugen
  und in der Tabelle unten eintragen.

**Danach**
- Jede Frage beantworten, am besten am selben Tag. Kritik sachlich aufnehmen.
- Wirksamer als eigene Posts sind Antworten auf bestehende Threads. In den
  Foren nach „991A Mac“ / „991A macOS CAT“ suchen und mit dem Link auf die
  Anleitung (nicht auf die Verkaufsseite) helfen:
  `https://www.aiscom.ch/macyaesu/ft-991a-mac/` bzw.
  `https://www.aiscom.ch/macyaesu/ft-991a-cp2105-macos/`.

## Tester-Lizenzen

| Nr. | Rufzeichen | Gruppe/Kanal | Datum | Feedback erhalten |
|---|---|---|---|---|
| 1 | | | | |

(E-Mail-Adressen nicht hier eintragen, dieses Repo ist öffentlich.)

---

## Anfrage an den Moderator, Englisch

**Subject:** OK to introduce a Mac app for the FT-991A?

Hi,

I'm [CALL], a member of this group. I wrote a native macOS app to control the
FT-991A over CAT. It's a paid app (49 CHF) with a free 15-minute demo, and I'd
like to offer free licenses to the first 10 members who test it and give
feedback.

Would a short introduction post be OK here, or would you prefer I don't post
it? Happy to follow whatever the group rules say.

73, [CALL]

---

## groups.io (FT-991A / Yaesu-Gruppen), Englisch

**Subject:** Native macOS app for the FT-991A, free licenses for the first 10 testers

Hi all,

I run my FT-991A from a Mac and got tired of VMs and Wine, so I wrote a
native macOS app for it: MacYaesu.

What it does today:
- CAT over the built-in CP210x USB port, with auto-reconnect
- VFO A/B, mode, AF/RF gain, squelch, power, NB/NR/notch, ATU, split
- S, PO and SWR meters
- 100 memory channels with tone, offset and power, repeater setup
- QSO log (CSV) and a CAT trace for troubleshooting
- Audio routing hints for WSJT-X/fldigi via BlackHole

It needs macOS 15 (Sequoia) or newer. There is a 15-minute demo with no
registration, so you can check it with your own cable and setup first:
https://www.aiscom.ch/macyaesu/

The app is not notarized yet. On first launch macOS will block it; go to
System Settings → Privacy & Security and click "Open Anyway".

Full disclosure: I'm the developer, and the full version is a paid app
(49 CHF one-time). I'm mainly after feedback from people actually running a
991A on a Mac, so the first 10 members who test it and report back get a free
license. Just reply or mail me. Connection problems, missing functions,
anything odd: a CAT trace from the app is the most useful bug report.

If you only need to get CAT working on a Mac with other software, the
setup guide may help anyway:
https://www.aiscom.ch/macyaesu/ft-991a-mac/

73, [CALL]

---

## QRZ.com Forum (Software / Mac), Englisch

**Title:** MacYaesu: FT-991A control on macOS without Wine or a VM

Same text as groups.io, plus at the end:

Short demo video: [VIDEO]
Screenshot attached (`main.png`).

---

## eHam.net Produkteintrag, Englisch

**Name:** MacYaesu
**Category:** Software, Rig Control
**Short description:** Native macOS app for CAT control of the Yaesu FT-991A:
VFO, modes, levels, meters, 100 memories with repeater setup, QSO log and
CAT trace. 15-minute free demo, full version 49 CHF one-time.
**Link:** https://www.aiscom.ch/macyaesu/

---

## Reddit r/amateurradio, Englisch

**Title:** I wrote a native Mac app for the FT-991A because I was tired of running it in a VM

Body: groups.io text without the greeting. Add one paragraph on *why*,
for example:

> The hardest part was CAT over the CP2105 dual port on macOS. One of the
> two ports is the right one, and many apps guess wrong. MacYaesu detects it and
> remembers it.

Clearly mention that it is a paid app with a demo (Reddit rules on self-promotion).

---

## Reddit r/macapps, Englisch

**Title:** MacYaesu: native SwiftUI app to control a Yaesu FT-991A ham radio

Shorter and less technical: what a FT-991A is (one sentence), which
Mac-specific problem the app solves (serial ports, no Windows), screenshot,
link, price and demo. Mention Gatekeeper openly.

---

## Lokale Vereine / USKA / DARC, Deutsch

**Betreff:** MacYaesu: FT-991A nativ am Mac steuern, Tester gesucht

Hallo zusammen,

ich steuere meinen FT-991A vom Mac aus und habe dafür eine native macOS-App
geschrieben: MacYaesu. CAT über den eingebauten USB-Port, VFO, Betriebsarten,
Pegel, Meter, 100 Speicher mit Relais-Ablage, QSO-Log und CAT-Trace.

Voraussetzung ist macOS 15 (Sequoia) oder neuer. Die Demo läuft 15 Minuten
ohne Anmeldung: https://www.aiscom.ch/macyaesu/

Beim ersten Start blockiert macOS die App, weil sie noch nicht notarisiert ist.
Unter Systemeinstellungen → Datenschutz & Sicherheit auf „Trotzdem öffnen“
klicken.

Offen gesagt: Ich bin der Entwickler, die Vollversion kostet einmalig 49 CHF.
Mir geht es vor allem um Rückmeldungen von echten 991A-Mac-Nutzern. Die ersten
10 Tester, die mir Feedback geben, bekommen deshalb eine kostenlose Lizenz.

Wer CAT am Mac nur mit anderer Software zum Laufen bringen will, findet die
Anleitung hier: https://www.aiscom.ch/macyaesu/de/ft-991a-mac/

73, [CALL]
