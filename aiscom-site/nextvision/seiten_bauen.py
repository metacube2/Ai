#!/usr/bin/env python3
"""Baut aus den archivierten Originalen (original/) die lauffähigen Seiten in site/.

Eingriffe gegenüber dem Archiv, sonst bleibt das HTML wie es war (Quirks-Modus,
Tabellenlayout, Schriften):
- ISO-8859-1 -> UTF-8, Meta-Zeichensatz angepasst
- Yahoo/GeoCities-Tracker, Norton-Popup-Blocker-Skript und Zähler entfernt
- absolute Links auf nextvision.ch.vu / goodbye.ch -> relative Links
- nicht archivierte Unterseiten zeigen eine Hinweisseite im Stil der Zeit
- PHP-Formulare laufen im Browser (localStorage), es gibt keinen Server-Endpunkt
"""
import html
import re
from pathlib import Path

HIER = Path(__file__).resolve().parent
ORIG = HIER / "original"
SITE = HIER / "site"
WB = "https://web.archive.org/web"

BLINK = "<style>blink{animation:nvblink 1s steps(1) infinite}@keyframes nvblink{50%{visibility:hidden}}</style>"
LESEN = lambda name: (ORIG / name).read_bytes().decode("latin-1")  # noqa: E731


def schreiben(pfad: str, text: str) -> None:
    ziel = SITE / pfad
    ziel.parent.mkdir(parents=True, exist_ok=True)
    ziel.write_text(text, encoding="utf-8")


# ---------------------------------------------------------------- 2002 GeoCities
UNTERSEITEN = {
    "myself.html": ("M-Y-S-E-L-F", "Über mich"),
    "mp3.html": ("Mp3's", "Musik-Downloads (Goa, Psytrance)"),
    "faceroom.html": ("Faceroom", "Fotos von Freunden"),
    "bilder.html": ("Bilder", "Party Review: Bilder"),
    "kollegen.html": ("Kollegen", "Party Review: Kollegen"),
    "c-coding.html": ("C Coding", "Programmieren in C"),
    "insider.html": ("Insider", "Insider"),
}


def geocities_start() -> None:
    s = LESEN("geocities_20030217232348.html")
    # Norton-Skript, Yahoo-Skripte und Tracker am Anfang
    s = re.sub(r"<script language=\"JavaScript\">\s*<!--\s*function SymError.*?</script>", "", s, flags=re.S)
    s = re.sub(r"<script language=\"JavaScript\">var PUpage.*?toto\.js\"></script>", "", s, flags=re.S)
    s = re.sub(r"<script language=\"JavaScript\">\s*<!--\s*window\.open = SymRealWinOpen;.*", "</body></html>\n", s, flags=re.S)
    s = re.sub(r"<a align=\"center\" href=\"\(EmptyReference!\)\"><img src=\"index_files/counter\.gif\"[^>]*>\s*</a>", "", s)
    s = s.replace('content="text/html; charset=ISO-8859-1"', 'content="text/html; charset=utf-8"')
    s = s.replace("</head>", '<script>function c(){return true}</script>' + BLINK + "</head>", 1)
    # Links der Navigation
    for alt, neu in (("myself.html", "myself.html"), ("mp3.html?", "mp3.html"), ("faceroom.html?", "faceroom.html"),
                     ("bilder.html?", "bilder.html"), ("kollegen.html?", "kollegen.html"), ("dirhp?hl=de", "insider.html")):
        s = s.replace(f'href="http://www.nextvision.ch.vu/{alt}"', f'href="{neu}"')
    s = s.replace('href="http://www.nextvision.ch.vu/?"', 'href="c-coding.html"')
    s = s.replace('href="http://gb.felice.de/cgi-bin/gb/multigb.cgi?gb=49071"', 'href="gaestebuch.html"')
    s = s.replace('background="(EmptyReference!)"', "").replace('src="(EmptyReference!)"', 'src="BILDER/white.jpg"')
    # eDonkey-Serverliste: Dienst existiert nicht mehr -> Archivlink statt toter dyndns-Adresse
    s = s.replace('<a href="http://ocbmaurice.dyndns.org/pl/slist.pl/server.met?download">',
                  f'<a href="{WB}/2003/http://ocbmaurice.dyndns.org/pl/slist.pl/server.met" target="_blank" '
                  'title="Server von 2003 - nur noch im Webarchiv">')
    assert "nextvision.ch.vu/" not in s.split("setHomePage")[0].split("isHomePage")[0], "absoluter Link übrig"
    schreiben("2002/index.html", s)


def hinweisseite(datei: str, titel: str, thema: str) -> str:
    return f"""<!DOCTYPE HTML PUBLIC "-//W3C//DTD HTML 4.0 Transitional//EN">
<html><head><meta http-equiv="content-type" content="text/html; charset=utf-8">
<title>Nextvision - {html.escape(titel)}</title>{BLINK}
<style>body,td,p{{font-family:arial,sans-serif}}</style></head>
<body text="#000000" link="#ff4236" vlink="#ff3366" bgcolor="#ffffff" leftmargin="0" topmargin="0">
<table cellspacing="0" cellpadding="0" border="0"><tr>
<td valign="bottom" align="right"><img height="79" alt="nextvision" src="BILDER/HAND.gif" width="123"></td>
<td valign="bottom"><img height="99" alt="" src="BILDER/white.jpg" width="62"></td>
<td valign="bottom"><img height="75" alt="" src="BILDER/next.jpg" width="336"><img height="62" alt="" src="BILDER/dechvu.gif" width="194"><img src="BILDER/HANDR.gif" width="108" height="75" border="0"></td>
</tr></table>
<center><br>
<table width="624" cellspacing="0" cellpadding="6" border="0"><tr>
<td background="Back.jpg" bgcolor="#3366cc"><font color="#ffffff"><b>-----{html.escape(titel)}------</b></font></td></tr>
<tr><td bgcolor="#efefef" background="grayback.jpg"><font size="-1">
<p><font size="4" color="red">D</font>iese Seite ({html.escape(thema)}) ist leider <b>nicht erhalten</b>.</p>
<p>GeoCities hat die Seiten von <i>ikohler_2002</i> 2004 gelöscht; die Wayback Machine hat damals nur die
Startseite gespeichert. Inhalt und Bilder dieser Unterseite sind verloren.</p>
<p><img src="BILDER/mice.jpg" width="49" height="28" align="middle"> <a href="index.html">zurück zur Startseite</a>
&nbsp;|&nbsp; <a href="{WB}/2003*/geocities.com/ikohler_2002/{datei}" target="_blank">im Webarchiv nachsehen</a></p>
</font></td></tr></table>
<p><font size="-2">©2002 Nextvision - Die Homepage von Ingo · Hinweisseite der Rekonstruktion 2026</font></p>
</center></body></html>
"""


def gaestebuch() -> None:
    """Einträge aus dem archivierten Gästebuch (gratis-gaestebuecher.de, ID 49071), ohne Werbung und ohne Links."""
    roh = LESEN("gaestebuch_20060215044946.html")
    text = re.sub(r"<[^>]+>", "\n", roh)
    text = html.unescape(re.sub(r"[ \t]+", " ", text))
    teil = text.split("Ich freue mich auf Ihren Eintrag !", 1)[1]
    teil = re.split(r"\[\s*G.stebuch\s+Admin", teil, maxsplit=1)[0]
    zeilen = [z.strip() for z in teil.splitlines() if z.strip()]
    eintraege, i = [], 0
    datum = re.compile(r"^\d{1,2}\. \w+ \d{4} - \d{2}:\d{2} Uhr$")
    while i < len(zeilen):
        if i + 1 < len(zeilen) and datum.match(zeilen[i + 1]):
            name, wann, rest = zeilen[i], zeilen[i + 1], []
            i += 2
            while i < len(zeilen) and not (i + 1 < len(zeilen) and datum.match(zeilen[i + 1])):
                if not zeilen[i].startswith("http"):
                    rest.append(zeilen[i])
                i += 1
            eintraege.append((name, wann, " ".join(rest)))
        else:
            i += 1
    eintraege = [e for e in eintraege if e[2] and not e[2].lower().startswith(("eintragen", "gästebuch"))]
    assert len(eintraege) == 10 and all(len(e[2]) < 300 for e in eintraege), eintraege
    zeilen_html = "".join(
        f"""<tr><td bgcolor="#F3F8FC" width="160" valign="top"><font size="2"><b>{html.escape(n)}</b><br>
<font size="1">{html.escape(w)}</font></font></td>
<td bgcolor="#FFFFFF" valign="top"><font size="2">{html.escape(t)}</font></td></tr>"""
        for n, w, t in eintraege)
    schreiben("2002/gaestebuch.html", f"""<!DOCTYPE HTML PUBLIC "-//W3C//DTD HTML 4.0 Transitional//EN">
<html><head><meta http-equiv="content-type" content="text/html; charset=utf-8">
<title>Gästebuch - Nextvision</title><style>body,td{{font-family:verdana,arial,sans-serif}}</style></head>
<body bgcolor="#E8F0F8" link="#3366cc" vlink="#3366cc">
<center><br><font size="4" color="#ff1c2d"><b>G ä s t e b u c h</b></font><br>
<font size="2">Willkommen in meinem Gästebuch. Ich freue mich auf Ihren Eintrag !</font><br><br>
<table width="560" border="1" bordercolor="#808080" cellpadding="6" cellspacing="0">{zeilen_html}</table>
<p><font size="1">Einträge 2002–2003 aus dem Webarchiv (gb.felice.de / gratis-gaestebuecher.de, ID 49071,
Aufnahme 15.02.2006). Neue Einträge sind nicht mehr möglich.</font></p>
<p><font size="2"><a href="index.html">zurück zu Nextvision</a></font></p></center></body></html>
""")


# ---------------------------------------------------------------- 2004 Flash-Intro + Mitgliederbereich
def intro_2004() -> None:
    s = LESEN("ois_index_20040709231131.html")
    s = s.replace('href="http://www.goodbye.ch/ingo/main2.php"', 'href="main2.html"')
    s = s.replace("nextvision%20logo.swf", "nextvision-logo.swf")
    s = s.replace("<head>", '<head>\n<meta http-equiv="content-type" content="text/html; charset=utf-8">', 1)
    s = s.replace("</head>", '<script src="../ruffle/ruffle.js"></script>\n</head>', 1)
    assert "goodbye.ch" not in s and "%20" not in s
    schreiben("2004/index.html", s)


DB_SKRIPT = """<script>
/* Nachbau 2026: Die MySQL-Datenbank von 2004 ist weg (schon damals: "Host ... is not allowed").
   Benutzer und Nachrichten liegen jetzt im Browser (localStorage) - nur auf diesem Gerät sichtbar. */
var NV = {
  lesen: function (k) { try { return JSON.parse(localStorage.getItem('nv2004_' + k) || '[]'); } catch (e) { return []; } },
  speichern: function (k, v) { try { localStorage.setItem('nv2004_' + k, JSON.stringify(v)); return true; } catch (e) { return false; } },
  esc: function (t) { return String(t).replace(/[&<>"]/g, function (c) { return {'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;'}[c]; }); }
};
</script>"""


def kopf_2004(titel: str) -> str:
    return f"""<!DOCTYPE HTML PUBLIC "-//W3C//DTD HTML 4.01 Transitional//EN"
"http://www.w3.org/TR/html4/loose.dtd">
<html>
<head>
<title>{titel}</title>
<meta http-equiv="Content-Type" content="text/html; charset=utf-8">
<link href="gui-design.css" rel="stylesheet" type="text/css">
<style>
a {{color:black ; border: 1px solid}}
a:link {{color : green; }}
a:hover {{color : orange}}
a:visited {{back-color: white }}
}}

</style>
{DB_SKRIPT}
</head>
"""


MENUE = """<span id="layer1">
<img src="nextvision-logo.jpg" width="139" height="108" class="amrand"> </p>
<p class="titelboxen">
<a href="main2.html?runmenu=yes">Benutzer suchen</a> </p>
<p class="titelboxen"><a href="eintragen1.html">Nachricht hinterlegen </a></p>
<p class="titelboxen"><a href="main2.html?runbuch=yes">Benutzer hinzuf&uuml;gen </a></a></p>
</span>
"""


def mitglieder_2004() -> None:
    orig = LESEN("goodbye_main2_20040805220556.html")
    assert "Benutzer suchen" in orig and "nextvision-logo.jpg" in orig  # Menü entspricht dem Archiv
    formular = LESEN("goodbye_eintragen_20050322062547.html")
    felder = re.search(r"<form.*?</form>", formular, flags=re.S).group(0)
    felder = felder.replace("action=auswertung.php method=POST", 'action="#" onsubmit="return nvBenutzer(this)"')
    schreiben("2004/main2.html", kopf_2004("Untitled Document") + "\n<body>\n" + MENUE + f"""


<span id='textlayer'>Verbinde mit Datenbank</span><br />
<div class="rechtsformatiert" id="inhalt" style="width:420px;padding:6px;color:#fff"></div>
<script>
var q = location.search, ziel = document.getElementById('inhalt');
var tl = document.getElementById('textlayer');
tl.innerHTML = 'Datenbank: Browser<br><font size="1">(MySQL von 2004 ersetzt)</font>';
function nvBenutzer(f) {{
  var b = NV.lesen('benutzer');
  b.push({{name: f.namee.value, alter: f.alter.value, email: f.email.value, hp: f.hp.value, wohnort: f.wohnort.value, zeit: new Date().toLocaleString('de-CH')}});
  NV.speichern('benutzer', b);
  location.href = 'main2.html?runmenu=yes';
  return false;
}}
function liste(filter) {{
  var b = NV.lesen('benutzer').filter(function (x) {{ return !filter || JSON.stringify(x).toLowerCase().indexOf(filter.toLowerCase()) >= 0; }});
  if (!b.length) return '<i>keine Benutzer gefunden</i>';
  return b.map(function (x) {{ return '<b>' + NV.esc(x.name) + '</b> (' + NV.esc(x.alter) + ') aus ' + NV.esc(x.wohnort) + (x.hp ? '<br><small>' + NV.esc(x.hp) + '</small>' : ''); }}).join('<hr>');
}}
if (q.indexOf('runbuch=yes') >= 0) {{
  ziel.innerHTML = 'Schreib hier deine Infos rein <br>' + {felder!r};
}} else if (q.indexOf('runmenu=yes') >= 0) {{
  ziel.innerHTML = '<form onsubmit="document.getElementById(\\'treffer\\').innerHTML=liste(this.s.value);return false">Benutzer suchen: <input name="s" type="text"> <input type="submit" value="suchen"></form><div id="treffer">' + liste('') + '</div>';
}} else {{
  var n = NV.lesen('nachrichten');
  ziel.innerHTML = '<b>Willkommen bei Nextvision!</b><br>Hinterlassene Nachrichten:<br><br>' +
    (n.length ? n.slice().reverse().map(function (x) {{ return '<b>' + NV.esc(x.name) + '</b> <small>' + NV.esc(x.zeit) + '</small><br>' + NV.esc(x.text); }}).join('<hr>') : '<i>noch keine</i>');
}}
</script>
</body></html>
""")
    nachricht = LESEN("goodbye_eintragen1_20040806023017.html")
    assert "auswertung1.php" in nachricht
    schreiben("2004/eintragen1.html", """<head>
<title> System </title>
<meta http-equiv="Content-Type" content="text/html; charset=utf-8"><style type="text/css">
<!--
body {
		background-color: #ECE9D8;
											width: 1px;
	border: medium dotted #00CC33;
}
-->
</style>""" + DB_SKRIPT + """</head>
<body>
Schreib hier deine Nachricht rein <br><form action="#" onsubmit="return nvNachricht(this)"><a align=center>name</a><br><input type='text' name='namee'><br>nachricht<br><textarea name='text' rows='5' cols='22'></textarea><br><input type='submit' value='senden'><br></form>
<a href="main2.html">zur&uuml;ck</a>
<script>
function nvNachricht(f) {
  var n = NV.lesen('nachrichten');
  n.push({name: f.namee.value || 'anonym', text: f.text.value, zeit: new Date().toLocaleString('de-CH')});
  NV.speichern('nachrichten', n);
  location.href = 'main2.html';
  return false;
}
</script>
</body>
</html>""")



def main() -> None:
    geocities_start()
    for datei, (titel, thema) in UNTERSEITEN.items():
        schreiben(f"2002/{datei}", hinweisseite(datei.replace("c-coding.html", "").replace("insider.html", "dirhp"), titel, thema))
    gaestebuch()
    intro_2004()
    mitglieder_2004()
    print("Seiten gebaut")


if __name__ == "__main__":
    main()
