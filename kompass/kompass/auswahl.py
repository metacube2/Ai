"""Startansicht: Jetzt-Karte, naechster Termin, Hauptaufgaben, „Heute neu entscheiden“.

Auswahl der Jetzt-Karte (PLAN 5.1), jeweils das Erste, was zutrifft:
1. eine Routine in ihrem Zeitfenster, heute noch nicht fertig
2. die naechste Papier-Frist in hoechstens 3 Tagen (Karte zeigt den naechsten Schritt)
3. die erste Hauptaufgabe von heute (ist sie zerlegt: ihr erster offener Schritt)
4. der erste offene Schritt irgendeiner zerlegten Aufgabe
„Nicht jetzt“ uebergibt die Schluessel der weggeklickten Karten in `ohne`.
"""
from __future__ import annotations

import json
import sqlite3
from datetime import timedelta

from . import zeit

MAX_HAUPT = 3
FRIST_TAGE = 3


def routine_lauf(v: sqlite3.Connection, rid: int, tag: str, neu: bool = False):
    """Aktueller Durchgang einer Routine an einem Tag (legt ihn bei Bedarf an)."""
    lauf = v.execute("SELECT * FROM routine_lauf WHERE routine_id = ? AND tag = ? ORDER BY durchgang DESC LIMIT 1",
                     (rid, tag)).fetchone()
    if lauf and not (neu and lauf["fertig_am"]):
        return lauf
    nr = (lauf["durchgang"] + 1) if lauf else 1
    v.execute("INSERT INTO routine_lauf (routine_id, tag, durchgang) VALUES (?,?,?)", (rid, tag, nr))
    return v.execute("SELECT * FROM routine_lauf WHERE routine_id = ? AND tag = ? AND durchgang = ?", (rid, tag, nr)).fetchone()


def routine_stand(v: sqlite3.Connection, r) -> dict:
    tag = zeit.heute().isoformat()
    schritte = [dict(s) for s in v.execute("SELECT id, text FROM routine_schritt WHERE routine_id = ? ORDER BY reihenfolge, id", (r["id"],))]
    lauf = v.execute("SELECT * FROM routine_lauf WHERE routine_id = ? AND tag = ? ORDER BY durchgang DESC LIMIT 1",
                     (r["id"], tag)).fetchone()
    abgehakt = set(json.loads(lauf["abgehakt"])) if lauf else set()
    for s in schritte:
        s["erledigt"] = s["id"] in abgehakt
    return {"id": r["id"], "name": r["name"], "zeitfenster": r["zeitfenster"], "erinnern_um": r["erinnern_um"],
            "mehrfach": bool(r["mehrfach"]), "schritte": schritte,
            "fertig": bool(lauf and lauf["fertig_am"]), "durchgang": lauf["durchgang"] if lauf else 0,
            "im_fenster": bool(r["zeitfenster"]) and zeit.in_fenster(*r["zeitfenster"].split("-"))}


def eintrag_dict(v: sqlite3.Connection, e) -> dict:
    d = dict(e)
    kinder = v.execute("SELECT id, text, status FROM eintrag WHERE eltern_id = ? AND status != 'losgelassen' ORDER BY reihenfolge, id",
                       (e["id"],)).fetchall()
    d["schritte"] = [dict(k) for k in kinder]
    if e["art"] == "papier":
        p = v.execute("SELECT * FROM papier WHERE eintrag_id = ?", (e["id"],)).fetchone()
        d["papier"] = dict(p) if p else None
    return d


def erster_offener_schritt(v: sqlite3.Connection, eid: int):
    return v.execute("SELECT * FROM eintrag WHERE eltern_id = ? AND status = 'offen' ORDER BY reihenfolge, id LIMIT 1",
                     (eid,)).fetchone()


def jetzt_karte(v: sqlite3.Connection, ohne: set[str]) -> dict | None:
    heute = zeit.heute().isoformat()
    for r in v.execute("SELECT * FROM routine WHERE aktiv = 1 AND zeitfenster IS NOT NULL ORDER BY reihenfolge"):
        st = routine_stand(v, r)
        if st["im_fenster"] and not st["fertig"] and f"routine:{r['id']}" not in ohne:
            offen = [s for s in st["schritte"] if not s["erledigt"]]
            return {"schluessel": f"routine:{r['id']}", "art": "routine", "titel": r["name"],
                    "text": offen[0]["text"] if offen else "Abschliessen", "routine_id": r["id"]}
    bis = (zeit.heute() + timedelta(days=FRIST_TAGE)).isoformat()
    for p in v.execute("SELECT e.id, e.text, p.frist, p.naechster_schritt FROM eintrag e JOIN papier p ON p.eintrag_id = e.id "
                       "WHERE e.status = 'offen' AND p.frist IS NOT NULL AND p.frist <= ? ORDER BY p.frist, e.id", (bis,)):
        if f"eintrag:{p['id']}" not in ohne:
            return {"schluessel": f"eintrag:{p['id']}", "art": "papier", "titel": p["text"], "text": p["naechster_schritt"],
                    "eintrag_id": p["id"], "frist": p["frist"], "tage": zeit.tage_bis(p["frist"])}
    kandidaten = list(v.execute("SELECT * FROM eintrag WHERE status = 'offen' AND heute = 1 AND tag = ? AND eltern_id IS NULL "
                                "ORDER BY reihenfolge, id", (heute,)))
    kandidaten += list(v.execute("SELECT DISTINCT e.* FROM eintrag e JOIN eintrag k ON k.eltern_id = e.id "
                                 "WHERE e.status = 'offen' AND k.status = 'offen' AND NOT (e.heute = 1 AND e.tag = ?) "
                                 "ORDER BY e.tag IS NULL, e.tag, e.id", (heute,)))
    for e in kandidaten:
        schritt = erster_offener_schritt(v, e["id"])
        ziel = schritt or e
        if f"eintrag:{ziel['id']}" in ohne:
            continue
        return {"schluessel": f"eintrag:{ziel['id']}", "art": "schritt" if schritt else "aufgabe",
                "titel": e["text"] if schritt else "Hauptaufgabe", "text": ziel["text"], "eintrag_id": ziel["id"],
                "eltern_id": e["id"] if schritt else None}
    return None


def naechste_termine(v: sqlite3.Connection) -> dict:
    jetzt = zeit.jetzt_iso()
    heute = zeit.heute()
    morgen_beginn = zeit.iso(zeit.um(heute + timedelta(days=1), "00:00"))
    naechster = v.execute("SELECT * FROM termin WHERE ganztags = 0 AND COALESCE(ende, beginn) > ? ORDER BY beginn LIMIT 1",
                          (jetzt,)).fetchone()
    ganztags = v.execute("SELECT titel FROM termin WHERE ganztags = 1 AND beginn <= ? AND COALESCE(ende, beginn) > ? ORDER BY titel",
                         (heute.isoformat(), heute.isoformat())).fetchall()
    rest_heute = v.execute("SELECT beginn, titel FROM termin WHERE ganztags = 0 AND beginn > ? AND beginn < ? ORDER BY beginn LIMIT 6",
                           (jetzt, morgen_beginn)).fetchall()
    return {"naechster": dict(naechster) if naechster else None, "ganztags": [g["titel"] for g in ganztags],
            "heute": [dict(t) for t in rest_heute]}


def heute_daten(v: sqlite3.Connection, ohne: set[str] | None = None) -> dict:
    heute = zeit.heute().isoformat()
    offen = "status = 'offen' AND eltern_id IS NULL AND art = 'aufgabe'"
    haupt = v.execute(f"SELECT * FROM eintrag WHERE {offen} AND heute = 1 AND tag = ? ORDER BY reihenfolge, id", (heute,)).fetchall()
    auch = v.execute(f"SELECT * FROM eintrag WHERE {offen} AND heute = 0 AND tag = ? ORDER BY reihenfolge, id", (heute,)).fetchall()
    neu = v.execute(f"SELECT * FROM eintrag WHERE {offen} AND tag < ? ORDER BY tag, id", (heute,)).fetchall()
    geplant = v.execute(f"SELECT * FROM eintrag WHERE {offen} AND tag > ? ORDER BY tag, id LIMIT 30", (heute,)).fetchall()
    irgendwann = v.execute(f"SELECT * FROM eintrag WHERE {offen} AND tag IS NULL ORDER BY id DESC LIMIT 30").fetchall()
    erledigt = v.execute("SELECT COUNT(*) FROM eintrag WHERE status = 'erledigt' AND erledigt_am >= ?",
                         (zeit.iso(zeit.um(zeit.heute(), "00:00")),)).fetchone()[0]
    return {
        "datum": heute, "wochentag": zeit.wochentag(zeit.heute()),
        "termine": naechste_termine(v),
        "jetzt": jetzt_karte(v, ohne or set()),
        "haupt": [eintrag_dict(v, e) for e in haupt],
        "auch_heute": [eintrag_dict(v, e) for e in auch],
        "neu_entscheiden": [eintrag_dict(v, e) for e in neu],
        "geplant": [eintrag_dict(v, e) for e in geplant],
        "irgendwann": [eintrag_dict(v, e) for e in irgendwann],
        "erledigt_heute": erledigt,
        "eingang": v.execute("SELECT COUNT(*) FROM eintrag WHERE art = 'eingang' AND status = 'offen'").fetchone()[0],
    }
