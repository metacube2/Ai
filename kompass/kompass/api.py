"""JSON-API unter /api. Alle Routen brauchen die Sitzung; POST braucht JSON und X-CSRF."""
from __future__ import annotations

import json
import re
from datetime import date

from flask import Blueprint, current_app, jsonify, request
from werkzeug.security import check_password_hash, generate_password_hash

from . import auswahl, erinnern, fokus, kalender, zeit, zerleger
from . import db as dbm

api = Blueprint("api", __name__, url_prefix="/api")
KATEGORIEN = ("rechnung", "kuendigung", "versicherung", "amt", "steuern", "sonstiges")
UHR = re.compile(r"([01]\d|2[0-3]):[0-5]\d")


class Fehler(Exception):
    def __init__(self, text: str, code: int = 400, **extra):
        super().__init__(text)
        self.text, self.code, self.extra = text, code, extra


@api.errorhandler(Fehler)
def _fehler(e: Fehler):
    return jsonify(error=e.text, **e.extra), e.code


@api.before_request
def _anmeldung():
    from flask import session
    if session.get("konto") != "admin":
        return jsonify(error="Bitte neu anmelden."), 401
    return None


def daten() -> dict:
    d = request.get_json(silent=True)
    return d if isinstance(d, dict) else {}


def text_aus(wert, name: str = "Text", maximal: int = 500) -> str:
    if not isinstance(wert, str) or not wert.strip():
        raise Fehler(f"{name} fehlt.")
    t = " ".join(wert.split())
    if len(t) > maximal:
        raise Fehler(f"{name} ist zu lang (höchstens {maximal} Zeichen).")
    return t


def tag_aus(wert) -> str:
    try:
        return date.fromisoformat(str(wert)).isoformat()
    except ValueError:
        raise Fehler("Datum als JJJJ-MM-TT.")


def eintrag_holen(v, eid: int):
    e = v.execute("SELECT * FROM eintrag WHERE id = ?", (eid,)).fetchone()
    if not e:
        raise Fehler("Eintrag nicht gefunden.", 404)
    return e


def haupt_anzahl(v, ohne: int | None = None) -> int:
    return v.execute("SELECT COUNT(*) FROM eintrag WHERE status = 'offen' AND heute = 1 AND tag = ? AND eltern_id IS NULL AND id != ?",
                     (zeit.heute().isoformat(), ohne or -1)).fetchone()[0]


# ---- Heute und Eintraege -----------------------------------------------------------

@api.get("/heute")
def heute():
    ohne = {s for s in request.args.get("ohne", "").split(",") if s}
    v = dbm.db()
    with v:  # routine_lauf kann angelegt werden
        d = auswahl.heute_daten(v, ohne)
    d["fokus"] = fokus.zustand(v)
    d["kalender"] = {**kalender.ZUSTAND, "eingerichtet": bool(current_app.einst.get("ical_url"))}
    return jsonify(d)


@api.post("/eintrag")
def eintrag_neu():
    d = daten()
    v = dbm.db()
    with v:
        eid = dbm.neu_eintrag(v, text_aus(d.get("text")))
    return jsonify(id=eid), 201


@api.get("/eingang")
def eingang():
    v = dbm.db()
    zeilen = v.execute("SELECT * FROM eintrag WHERE art = 'eingang' AND status = 'offen' ORDER BY id DESC").fetchall()
    return jsonify(eintraege=[auswahl.eintrag_dict(v, e) for e in zeilen])


@api.post("/eintrag/<int:eid>")
def eintrag_aktion(eid: int):
    d = daten()
    aktion = d.get("aktion")
    v = dbm.db()
    e = eintrag_holen(v, eid)
    heute_tag = zeit.heute().isoformat()
    jetzt = zeit.jetzt_iso()
    art = "aufgabe" if e["art"] == "eingang" else e["art"]
    with v:
        if aktion == "heute":
            haupt = bool(d.get("haupt", True)) and e["eltern_id"] is None
            if haupt and haupt_anzahl(v, eid) >= auswahl.MAX_HAUPT:
                if d.get("haupt") is True:
                    raise Fehler("Heute sind schon 3 Hauptaufgaben geplant. Eine davon zuerst verschieben?", 409, voll=True)
                haupt = False
            dbm.eintrag_aendern(v, eid, art=art, tag=heute_tag, heute=1 if haupt else 0, status="offen")
        elif aktion == "haupt_weg":
            dbm.eintrag_aendern(v, eid, heute=0)
        elif aktion == "tag":
            dbm.eintrag_aendern(v, eid, art=art, tag=tag_aus(d.get("tag")), heute=0, status="offen")
        elif aktion == "irgendwann":
            dbm.eintrag_aendern(v, eid, art=art, tag=None, heute=0, status="offen")
        elif aktion == "erledigt":
            dbm.eintrag_aendern(v, eid, status="erledigt", erledigt_am=jetzt)
            if e["eltern_id"]:  # letzter Schritt erledigt: Aufgabe auch
                rest = v.execute("SELECT COUNT(*) FROM eintrag WHERE eltern_id = ? AND status = 'offen'", (e["eltern_id"],)).fetchone()[0]
                if not rest:
                    dbm.eintrag_aendern(v, e["eltern_id"], status="erledigt", erledigt_am=jetzt)
        elif aktion == "losgelassen":
            dbm.eintrag_aendern(v, eid, status="losgelassen")
        elif aktion == "offen":
            dbm.eintrag_aendern(v, eid, status="offen", erledigt_am=None)
        elif aktion == "text":
            dbm.eintrag_aendern(v, eid, text=text_aus(d.get("text")))
        else:
            raise Fehler("unbekannte Aktion")
    return jsonify(auswahl.eintrag_dict(v, eintrag_holen(v, eid)))


@api.post("/eintrag/<int:eid>/schritte")
def schritte_uebernehmen(eid: int):
    schritte = daten().get("schritte")
    if not isinstance(schritte, list) or not 1 <= len(schritte) <= zerleger.MAX_SCHRITTE:
        raise Fehler(f"1 bis {zerleger.MAX_SCHRITTE} Schritte.")
    v = dbm.db()
    e = eintrag_holen(v, eid)
    if e["eltern_id"]:
        raise Fehler("Ein Schritt lässt sich nicht weiter zerlegen.")
    with v:
        start = v.execute("SELECT COALESCE(MAX(reihenfolge), -1) + 1 FROM eintrag WHERE eltern_id = ?", (eid,)).fetchone()[0]
        for i, s in enumerate(schritte):
            text = text_aus(s.get("text") if isinstance(s, dict) else s, "Schritt", 160)
            minuten = s.get("min") if isinstance(s, dict) else None
            dbm.neu_eintrag(v, text, art="aufgabe", eltern_id=eid, reihenfolge=start + i,
                            geschaetzt_min=int(minuten) if isinstance(minuten, int) and 0 < minuten <= 60 else None)
        if e["art"] == "eingang":
            dbm.eintrag_aendern(v, eid, art="aufgabe")
    return jsonify(auswahl.eintrag_dict(v, eintrag_holen(v, eid)))


@api.post("/zerlegen")
def zerlegen():
    v = dbm.db()
    e = eintrag_holen(v, int(daten().get("eintrag_id") or 0))
    cfg = current_app.einst.alle()
    try:
        schritte = zerleger.zerlegen(cfg["claude_schluessel"], cfg["claude_modell"], e["text"])
    except zerleger.ZerlegerFehler as f:
        raise Fehler(f"{f} Schritte selbst eintragen geht immer.", 503)
    return jsonify(schritte=schritte)


# ---- Papierkram ----------------------------------------------------------------------

def papier_felder(d: dict) -> dict:
    kategorie = d.get("kategorie") or "sonstiges"
    if kategorie not in KATEGORIEN:
        raise Fehler("unbekannte Kategorie")
    frist = tag_aus(d["frist"]) if d.get("frist") else None
    betrag = d.get("betrag")
    rappen = None
    if betrag not in (None, ""):
        try:
            rappen = round(float(str(betrag).replace("'", "").replace(",", ".")) * 100)
        except ValueError:
            raise Fehler("Betrag als Zahl, z. B. 49.90")
    try:
        vorher = int(d.get("erinnern_tage_vorher", 3))
    except (TypeError, ValueError):
        raise Fehler("Erinnerung: Anzahl Tage")
    return {"kategorie": kategorie, "frist": frist, "betrag_rappen": rappen,
            "naechster_schritt": text_aus(d.get("naechster_schritt"), "Der nächste Schritt", 200),
            "erinnern_tage_vorher": max(0, min(30, vorher))}


@api.post("/papier")
def papier_neu():
    """Neuer Papier-Eintrag, oder einen vorhandenen (eintrag_id) zu Papier machen."""
    d = daten()
    f = papier_felder(d)
    v = dbm.db()
    with v:
        if d.get("eintrag_id"):
            eid = eintrag_holen(v, int(d["eintrag_id"]))["id"]
            felder = {"art": "papier", "heute": 0, "status": "offen"}
            if d.get("text"):
                felder["text"] = text_aus(d.get("text"))
            dbm.eintrag_aendern(v, eid, **felder)
        else:
            eid = dbm.neu_eintrag(v, text_aus(d.get("text")), art="papier")
        v.execute("INSERT OR REPLACE INTO papier (eintrag_id, kategorie, frist, betrag_rappen, naechster_schritt, erinnern_tage_vorher) "
                  "VALUES (:e, :kategorie, :frist, :betrag_rappen, :naechster_schritt, :erinnern_tage_vorher)", {"e": eid, **f})
    return jsonify(auswahl.eintrag_dict(v, eintrag_holen(v, eid))), 201


@api.get("/papier")
def papier_liste():
    v = dbm.db()
    zeilen = v.execute("SELECT e.* FROM eintrag e JOIN papier p ON p.eintrag_id = e.id WHERE e.status = 'offen' "
                       "ORDER BY p.frist IS NULL, p.frist, e.id").fetchall()
    erledigt = v.execute("SELECT e.* FROM eintrag e JOIN papier p ON p.eintrag_id = e.id WHERE e.status = 'erledigt' "
                         "ORDER BY e.erledigt_am DESC LIMIT 10").fetchall()
    return jsonify(offen=[auswahl.eintrag_dict(v, e) for e in zeilen], erledigt=[auswahl.eintrag_dict(v, e) for e in erledigt],
                   kategorien=KATEGORIEN)


@api.post("/papier/<int:eid>/schritt")
def papier_schritt(eid: int):
    """Naechster Schritt erledigt: entweder neuer naechster Schritt oder ganz fertig."""
    d = daten()
    v = dbm.db()
    eintrag_holen(v, eid)
    with v:
        if d.get("fertig"):
            dbm.eintrag_aendern(v, eid, status="erledigt", erledigt_am=zeit.jetzt_iso())
        else:
            v.execute("UPDATE papier SET naechster_schritt = ? WHERE eintrag_id = ?",
                      (text_aus(d.get("naechster_schritt"), "Der nächste Schritt", 200), eid))
            dbm.eintrag_aendern(v, eid)
    return jsonify(auswahl.eintrag_dict(v, eintrag_holen(v, eid)))


# ---- Routinen ------------------------------------------------------------------------

@api.get("/routinen")
def routinen():
    v = dbm.db()
    with v:
        liste = [{**auswahl.routine_stand(v, r), "aktiv": bool(r["aktiv"])}
                 for r in v.execute("SELECT * FROM routine WHERE aktiv >= 0 ORDER BY reihenfolge, id")]
    return jsonify(routinen=liste)


@api.post("/routine/<int:rid>/haken")
def routine_haken(rid: int):
    d = daten()
    v = dbm.db()
    r = v.execute("SELECT * FROM routine WHERE id = ?", (rid,)).fetchone()
    if not r:
        raise Fehler("Routine nicht gefunden.", 404)
    sid = int(d.get("schritt_id") or 0)
    schritte = [s["id"] for s in v.execute("SELECT id FROM routine_schritt WHERE routine_id = ?", (rid,))]
    if sid not in schritte:
        raise Fehler("Schritt gehört nicht zu dieser Routine.")
    with v:
        lauf = auswahl.routine_lauf(v, rid, zeit.heute().isoformat())
        haken = set(json.loads(lauf["abgehakt"]))
        (haken.add if d.get("an", True) else haken.discard)(sid)
        fertig = zeit.jetzt_iso() if set(schritte) <= haken else None
        v.execute("UPDATE routine_lauf SET abgehakt = ?, fertig_am = ? WHERE id = ?", (json.dumps(sorted(haken)), fertig, lauf["id"]))
        st = auswahl.routine_stand(v, r)
    return jsonify(st)


@api.post("/routine/<int:rid>/neu")
def routine_neuer_durchgang(rid: int):
    v = dbm.db()
    r = v.execute("SELECT * FROM routine WHERE id = ?", (rid,)).fetchone()
    if not r:
        raise Fehler("Routine nicht gefunden.", 404)
    with v:
        tag = zeit.heute().isoformat()
        lauf = auswahl.routine_lauf(v, rid, tag)
        if lauf["fertig_am"]:
            auswahl.routine_lauf(v, rid, tag, neu=True)
        else:  # angefangener Durchgang: einfach von vorn
            v.execute("UPDATE routine_lauf SET abgehakt = '[]' WHERE id = ?", (lauf["id"],))
        st = auswahl.routine_stand(v, r)
    return jsonify(st)


@api.post("/routine")
def routine_speichern():
    d = daten()
    name = text_aus(d.get("name"), "Name", 60)
    fenster = d.get("zeitfenster") or None
    if fenster and not re.fullmatch(r"([01]\d|2[0-3]):[0-5]\d-([01]\d|2[0-3]):[0-5]\d", fenster):
        raise Fehler("Zeitfenster als HH:MM-HH:MM")
    um = d.get("erinnern_um") or None
    if um and not UHR.fullmatch(um):
        raise Fehler("Erinnerung als HH:MM")
    schritte = d.get("schritte")
    if not isinstance(schritte, list) or not 1 <= len(schritte) <= 20:
        raise Fehler("1 bis 20 Schritte.")
    texte = [text_aus(s, "Schritt", 120) for s in schritte]
    v = dbm.db()
    with v:
        if d.get("id"):
            rid = int(d["id"])
            if not v.execute("SELECT 1 FROM routine WHERE id = ?", (rid,)).fetchone():
                raise Fehler("Routine nicht gefunden.", 404)
            v.execute("UPDATE routine SET name = ?, zeitfenster = ?, erinnern_um = ?, mehrfach = ?, aktiv = ? WHERE id = ?",
                      (name, fenster, um, 1 if d.get("mehrfach") else 0, 0 if d.get("aktiv") is False else 1, rid))
            alt = {s["text"]: s["id"] for s in v.execute("SELECT id, text FROM routine_schritt WHERE routine_id = ?", (rid,))}
            v.execute("DELETE FROM routine_schritt WHERE routine_id = ?", (rid,))
            for i, t in enumerate(texte):  # gleiche Texte behalten ihre id (Haken bleiben)
                v.execute("INSERT INTO routine_schritt (id, routine_id, text, reihenfolge) VALUES (?,?,?,?)", (alt.pop(t, None), rid, t, i))
        else:
            reih = v.execute("SELECT COALESCE(MAX(reihenfolge), -1) + 1 FROM routine").fetchone()[0]
            rid = v.execute("INSERT INTO routine (name, zeitfenster, erinnern_um, mehrfach, reihenfolge) VALUES (?,?,?,?,?)",
                            (name, fenster, um, 1 if d.get("mehrfach") else 0, reih)).lastrowid
            v.executemany("INSERT INTO routine_schritt (routine_id, text, reihenfolge) VALUES (?,?,?)", [(rid, t, i) for i, t in enumerate(texte)])
    return routinen()


@api.post("/routine/<int:rid>/loeschen")
def routine_loeschen(rid: int):
    v = dbm.db()
    with v:
        v.execute("UPDATE routine SET aktiv = -1 WHERE id = ?", (rid,))  # weich: Verlauf bleibt
    return routinen()


# ---- Fokus-Timer ---------------------------------------------------------------------

@api.get("/fokus")
def fokus_zustand():
    return jsonify(fokus=fokus.zustand(dbm.db()))


@api.post("/fokus")
def fokus_aktion():
    d = daten()
    v = dbm.db()
    aktion = d.get("aktion")
    try:
        if aktion == "start":
            eid = d.get("eintrag_id")
            if eid:
                eintrag_holen(v, int(eid))
            fokus.starten(v, int(d.get("minuten", 25)), int(eid) if eid else None)
        elif aktion == "verlaengern":
            fokus.verlaengern(v, int(d.get("minuten", 10)))
        elif aktion in ("erledigt", "pause", "abgebrochen"):
            fokus.beenden(v, aktion)
        else:
            raise Fehler("unbekannte Aktion")
    except (ValueError, TypeError) as e:
        raise Fehler(str(e))
    return jsonify(fokus=fokus.zustand(v))


# ---- Einstellungen -------------------------------------------------------------------

@api.get("/einstellungen")
def einstellungen_lesen():
    return jsonify(einstellungen=current_app.einst.fuer_anzeige(), kalender=kalender.ZUSTAND)


@api.post("/einstellungen")
def einstellungen_setzen():
    d = daten()
    alt_url = current_app.einst.get("ical_url")
    try:
        anzeige = current_app.einst.setzen(d)
    except ValueError as e:
        raise Fehler(str(e))
    neu_url = current_app.einst.get("ical_url")
    meldung = None
    if neu_url != alt_url:
        v = dbm.db()
        if neu_url:
            try:
                meldung = f"Kalender verbunden: {kalender.abgleichen(v, neu_url)} Termine in den nächsten 14 Tagen."
            except Exception:
                meldung = "Gespeichert, aber der Kalender war nicht erreichbar. Adresse prüfen."
        else:
            kalender.leeren(v)
    return jsonify(einstellungen=anzeige, kalender=kalender.ZUSTAND, meldung=meldung)


@api.post("/einstellungen/testpush")
def testpush():
    cfg = current_app.einst.alle()
    try:
        erinnern.senden_ntfy(cfg, "Kompass: Test", "Wenn du das liest, kommen die Erinnerungen an.", "https://kompass.aiscom.ch/")
    except Exception as e:
        raise Fehler(f"ntfy nicht erreichbar ({type(e).__name__}).", 502)
    return jsonify(ok=True, thema=cfg["ntfy_thema"], server=cfg["ntfy_server"])


@api.post("/kalender/abgleichen")
def kalender_abgleichen():
    url = current_app.einst.get("ical_url")
    if not url:
        raise Fehler("Keine iCal-Adresse eingetragen.")
    try:
        n = kalender.abgleichen(dbm.db(), url)
    except Exception:
        raise Fehler("Kalender nicht erreichbar.", 502)
    return jsonify(anzahl=n, kalender=kalender.ZUSTAND)


@api.post("/passwort")
def passwort():
    d = daten()
    v = dbm.db()
    k = v.execute("SELECT * FROM konto WHERE name = 'admin'").fetchone()
    if not check_password_hash(k["hash"], str(d.get("alt", ""))):
        raise Fehler("Das bisherige Passwort stimmt nicht.", 403)
    neu = d.get("neu")
    if not isinstance(neu, str) or len(neu) < 12:
        raise Fehler("Das neue Passwort braucht mindestens 12 Zeichen.")
    with v:
        v.execute("UPDATE konto SET hash = ? WHERE id = ?", (generate_password_hash(neu), k["id"]))
    (current_app.config["DATEN"] / "admin-passwort.txt").unlink(missing_ok=True)
    return jsonify(ok=True)
