"""Erinnerungen ausrechnen und per ntfy senden (JSON-Publish, damit Umlaute im Titel gehen).

Ruhezeiten gelten fuer Routinen, Papier und Termine. Timer, die man selbst gestartet hat,
melden sich auch in der Ruhezeit. Jede Erinnerung hat einen Schluessel in `gesendet`,
damit sie nur einmal kommt.
"""
from __future__ import annotations

import json
import sqlite3
import urllib.request
from datetime import timedelta

from . import auswahl, fokus, zeit

ADRESSE = "https://kompass.aiscom.ch"


def faellig(v: sqlite3.Connection, cfg: dict) -> list[dict]:
    jetzt = zeit.jetzt()
    heute = zeit.heute()
    tag = heute.isoformat()
    ruhe = zeit.in_fenster(cfg["ruhe_von"], cfg["ruhe_bis"], jetzt)
    out = []

    if cfg["push_routine"] and not ruhe:
        for r in v.execute("SELECT * FROM routine WHERE aktiv = 1 AND erinnern_um IS NOT NULL"):
            um = zeit.um(heute, r["erinnern_um"])
            if um <= jetzt < um + timedelta(hours=3) and not auswahl.routine_stand(v, r)["fertig"]:
                out.append({"schluessel": f"routine:{r['id']}:{tag}", "titel": f"Routine: {r['name']}",
                            "text": "Kurz durchgehen? Ein Tipp öffnet die Liste.", "klick": f"{ADRESSE}/#routinen"})

    if cfg["push_papier"] and not ruhe and jetzt >= zeit.um(heute, "09:00"):
        for p in v.execute("SELECT e.id, e.text, p.frist, p.naechster_schritt, p.erinnern_tage_vorher FROM eintrag e "
                           "JOIN papier p ON p.eintrag_id = e.id WHERE e.status = 'offen' AND p.frist IS NOT NULL"):
            tage = zeit.tage_bis(p["frist"])
            if tage == 0 or tage == p["erinnern_tage_vorher"]:
                wann = "heute" if tage == 0 else f"in {tage} Tagen"
                out.append({"schluessel": f"papier:{p['id']}:{tag}", "titel": f"{p['text']} ({wann})",
                            "text": f"Nächster Schritt: {p['naechster_schritt']}", "klick": f"{ADRESSE}/#papier"})

    if cfg["push_termin"] and not ruhe:
        vorlauf = timedelta(minutes=cfg["termin_vorlauf_min"])
        for t in v.execute("SELECT * FROM termin WHERE ganztags = 0 AND beginn > ? AND beginn <= ?",
                           (zeit.iso(jetzt), zeit.iso(jetzt + vorlauf))):
            minuten = max(1, round((zeit.aus_iso(t["beginn"]) - jetzt).total_seconds() / 60))
            out.append({"schluessel": f"termin:{t['uid']}:{t['beginn']}", "titel": f"In {minuten} min: {t['titel']}",
                        "text": t["ort"] or "Zeit, langsam aufzubrechen.", "klick": f"{ADRESSE}/"})

    if cfg["push_fokus"]:
        f = fokus.zustand(v)
        if f and f["abgelaufen"]:
            out.append({"schluessel": f"fokus:{f['id']}:ende", "titel": "Timer abgelaufen",
                        "text": f"{f['text'] or 'Fokuszeit'}: erledigt, noch 10 Minuten oder Pause?", "klick": f"{ADRESSE}/#fokus",
                        "prioritaet": 4})
        if f and f["kette_min"] >= cfg["hyperfokus_min"]:
            kette = v.execute("SELECT kette_id FROM fokus WHERE id = ?", (f["id"],)).fetchone()[0]
            out.append({"schluessel": f"hyper:{kette}", "titel": f"Schon {f['kette_min']} Minuten am Stück",
                        "text": "Trinken, aufstehen, kurz raus?", "klick": f"{ADRESSE}/#fokus", "prioritaet": 4})
    return out


def senden_ntfy(cfg: dict, titel: str, text: str, klick: str | None = None, prioritaet: int = 3) -> None:
    daten = {"topic": cfg["ntfy_thema"], "title": titel[:200], "message": text[:1000], "priority": prioritaet, "tags": ["compass"]}
    if klick:
        daten["click"] = klick
    req = urllib.request.Request(cfg["ntfy_server"].rstrip("/") + "/", data=json.dumps(daten).encode(),
                                 headers={"Content-Type": "application/json"}, method="POST")
    with urllib.request.urlopen(req, timeout=10) as r:
        r.read()


def durchlauf(v: sqlite3.Connection, cfg: dict, sender=senden_ntfy) -> list[str]:
    """Sendet alle faelligen, noch nicht gesendeten Erinnerungen. Liefert die gesendeten Schluessel."""
    if not cfg.get("ntfy_thema"):
        return []
    gesendet = []
    for e in faellig(v, cfg):
        with v:
            neu = v.execute("INSERT OR IGNORE INTO gesendet (schluessel, am) VALUES (?, ?)", (e["schluessel"], zeit.jetzt_iso())).rowcount
        if not neu:
            continue
        try:
            sender(cfg, e["titel"], e["text"], e.get("klick"), e.get("prioritaet", 3))
            gesendet.append(e["schluessel"])
        except Exception:
            with v:  # beim naechsten Durchlauf nochmals versuchen
                v.execute("DELETE FROM gesendet WHERE schluessel = ?", (e["schluessel"],))
    with v:
        v.execute("DELETE FROM gesendet WHERE am < ?", (zeit.iso(zeit.jetzt() - timedelta(days=30)),))
    return gesendet
