"""Fokus-Timer: liegt auf dem Server, damit er auf allen Geraeten gleich laeuft."""
from __future__ import annotations

import sqlite3
from datetime import timedelta

from . import db as dbm
from . import zeit


def laufend(v: sqlite3.Connection):
    return v.execute("SELECT * FROM fokus WHERE ende IS NULL ORDER BY id DESC LIMIT 1").fetchone()


def kette_start(v: sqlite3.Connection, kette_id: int) -> str:
    return v.execute("SELECT MIN(start) FROM fokus WHERE kette_id = ?", (kette_id,)).fetchone()[0]


def zustand(v: sqlite3.Connection) -> dict | None:
    f = laufend(v)
    if not f:
        return None
    start = zeit.aus_iso(f["start"])
    ziel = start + timedelta(minutes=f["geplant_min"])
    jetzt = zeit.jetzt()
    text = None
    if f["eintrag_id"]:
        e = v.execute("SELECT text FROM eintrag WHERE id = ?", (f["eintrag_id"],)).fetchone()
        text = e["text"] if e else None
    seit_kette = (jetzt - zeit.aus_iso(kette_start(v, f["kette_id"]))).total_seconds()
    return {"id": f["id"], "eintrag_id": f["eintrag_id"], "text": text, "geplant_min": f["geplant_min"],
            "start": f["start"], "ziel": zeit.iso(ziel), "rest_s": max(0, int((ziel - jetzt).total_seconds())),
            "abgelaufen": jetzt >= ziel, "kette_min": int(seit_kette // 60)}


def starten(v: sqlite3.Connection, minuten: int, eintrag_id: int | None = None) -> dict:
    if not 1 <= minuten <= 240:
        raise ValueError("Dauer zwischen 1 und 240 Minuten")
    with v:
        alt = laufend(v)
        if alt:
            v.execute("UPDATE fokus SET ende = ?, ergebnis = 'abgebrochen' WHERE id = ?", (zeit.jetzt_iso(), alt["id"]))
        fid = v.execute("INSERT INTO fokus (eintrag_id, start, geplant_min) VALUES (?,?,?)",
                        (eintrag_id, zeit.jetzt_iso(), minuten)).lastrowid
        v.execute("UPDATE fokus SET kette_id = ? WHERE id = ?", (fid, fid))
    return zustand(v)


def verlaengern(v: sqlite3.Connection, minuten: int = 10) -> dict:
    """Neuer Abschnitt derselben Kette (zaehlt fuer den Hyperfokus-Schutz weiter)."""
    with v:
        f = laufend(v)
        if not f:
            raise ValueError("Es läuft kein Timer.")
        v.execute("UPDATE fokus SET ende = ?, ergebnis = 'verlaengert' WHERE id = ?", (zeit.jetzt_iso(), f["id"]))
        v.execute("INSERT INTO fokus (kette_id, eintrag_id, start, geplant_min) VALUES (?,?,?,?)",
                  (f["kette_id"], f["eintrag_id"], zeit.jetzt_iso(), minuten))
    return zustand(v)


def beenden(v: sqlite3.Connection, ergebnis: str) -> None:
    if ergebnis not in ("erledigt", "pause", "abgebrochen"):
        raise ValueError("unbekanntes Ergebnis")
    with v:
        f = laufend(v)
        if not f:
            return
        v.execute("UPDATE fokus SET ende = ?, ergebnis = ? WHERE id = ?", (zeit.jetzt_iso(), ergebnis, f["id"]))
        if ergebnis == "erledigt" and f["eintrag_id"]:
            dbm.eintrag_aendern(v, f["eintrag_id"], status="erledigt", erledigt_am=zeit.jetzt_iso())
