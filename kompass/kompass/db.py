"""SQLite: Verbindung, nummerierte Migrationen, Vorlagen beim ersten Start."""
from __future__ import annotations

import secrets
import sqlite3
from pathlib import Path

from flask import current_app, g
from werkzeug.security import generate_password_hash

from . import zeit

MIGRATIONEN = [
    # 1: Grundschema Stufe 1
    """
    CREATE TABLE konto (id INTEGER PRIMARY KEY, name TEXT UNIQUE NOT NULL, hash TEXT NOT NULL);

    CREATE TABLE eintrag (
      id INTEGER PRIMARY KEY,
      text TEXT NOT NULL,
      art TEXT NOT NULL DEFAULT 'eingang',      -- eingang | aufgabe | papier
      status TEXT NOT NULL DEFAULT 'offen',     -- offen | erledigt | losgelassen
      heute INTEGER NOT NULL DEFAULT 0,         -- Hauptaufgabe fuer 'tag'
      tag TEXT,                                 -- geplanter Tag, NULL = irgendwann
      geschaetzt_min INTEGER,
      eltern_id INTEGER REFERENCES eintrag(id),
      reihenfolge INTEGER NOT NULL DEFAULT 0,
      erledigt_am TEXT,
      erstellt TEXT NOT NULL, geaendert TEXT NOT NULL
    );
    CREATE INDEX eintrag_status ON eintrag(status, art);
    CREATE INDEX eintrag_eltern ON eintrag(eltern_id);

    CREATE TABLE papier (
      eintrag_id INTEGER PRIMARY KEY REFERENCES eintrag(id),
      kategorie TEXT NOT NULL DEFAULT 'sonstiges',
      frist TEXT,
      betrag_rappen INTEGER,
      naechster_schritt TEXT NOT NULL,
      erinnern_tage_vorher INTEGER NOT NULL DEFAULT 3
    );

    CREATE TABLE routine (
      id INTEGER PRIMARY KEY,
      name TEXT NOT NULL,
      zeitfenster TEXT,                -- 'HH:MM-HH:MM' oder NULL
      erinnern_um TEXT,                -- 'HH:MM' oder NULL
      mehrfach INTEGER NOT NULL DEFAULT 0,  -- mehrere Durchgaenge am Tag (Haus verlassen)
      aktiv INTEGER NOT NULL DEFAULT 1,
      reihenfolge INTEGER NOT NULL DEFAULT 0
    );
    CREATE TABLE routine_schritt (
      id INTEGER PRIMARY KEY,
      routine_id INTEGER NOT NULL REFERENCES routine(id),
      text TEXT NOT NULL,
      reihenfolge INTEGER NOT NULL DEFAULT 0
    );
    CREATE TABLE routine_lauf (
      id INTEGER PRIMARY KEY,
      routine_id INTEGER NOT NULL REFERENCES routine(id),
      tag TEXT NOT NULL,
      durchgang INTEGER NOT NULL DEFAULT 1,
      abgehakt TEXT NOT NULL DEFAULT '[]',
      fertig_am TEXT,
      UNIQUE (routine_id, tag, durchgang)
    );

    CREATE TABLE fokus (
      id INTEGER PRIMARY KEY,
      kette_id INTEGER,                -- erste id einer Folge von Verlaengerungen
      eintrag_id INTEGER REFERENCES eintrag(id),
      start TEXT NOT NULL,
      geplant_min INTEGER NOT NULL,
      ende TEXT,
      ergebnis TEXT                    -- erledigt | verlaengert | pause | abgebrochen
    );

    CREATE TABLE termin (
      uid TEXT NOT NULL, beginn TEXT NOT NULL, ende TEXT, titel TEXT NOT NULL,
      ganztags INTEGER NOT NULL DEFAULT 0, ort TEXT,
      PRIMARY KEY (uid, beginn)
    );

    CREATE TABLE gesendet (schluessel TEXT PRIMARY KEY, am TEXT NOT NULL);
    """,
]

VORLAGEN = [
    ("Morgen", "06:30-09:30", "07:30", 0, ["Ein Glas Wasser trinken", "Medikamente nehmen", "Zähne putzen", "Tagesplan ansehen"]),
    ("Abend", "20:00-23:00", "21:00", 0, ["Handy ans Ladekabel", "Sachen für morgen bereitlegen", "Wecker prüfen"]),
    ("Haus verlassen", None, None, 1, ["Schlüssel", "Portemonnaie", "Handy", "Kopfhörer", "Herd und Licht aus"]),
]


def verbinden(pfad: Path) -> sqlite3.Connection:
    v = sqlite3.connect(pfad, timeout=10)
    v.row_factory = sqlite3.Row
    v.execute("PRAGMA foreign_keys = ON")
    v.execute("PRAGMA journal_mode = WAL")
    return v


def db() -> sqlite3.Connection:
    if "db" not in g:
        g.db = verbinden(current_app.config["DB_PFAD"])
    return g.db


def schliessen(_exc=None) -> None:
    v = g.pop("db", None)
    if v is not None:
        v.close()


def anlegen(pfad: Path, daten: Path) -> None:
    """Migrationen ausfuehren; beim ersten Start Admin-Konto und Routine-Vorlagen anlegen."""
    with verbinden(pfad) as v:
        v.execute("CREATE TABLE IF NOT EXISTS schema_version (nr INTEGER NOT NULL)")
        stand = v.execute("SELECT MAX(nr) FROM schema_version").fetchone()[0] or 0
        for nr, sql in enumerate(MIGRATIONEN, start=1):
            if nr > stand:
                v.executescript(sql)
                v.execute("INSERT INTO schema_version (nr) VALUES (?)", (nr,))
        if not v.execute("SELECT 1 FROM konto").fetchone():
            # Erststart: Passwort nur in eine root-lesbare Datei, nie ins Log
            passwort = secrets.token_urlsafe(15)
            v.execute("INSERT INTO konto (name, hash) VALUES ('admin', ?)", (generate_password_hash(passwort),))
            datei = daten / "admin-passwort.txt"
            datei.write_text(f"Login: admin\nPasswort: {passwort}\n")
            datei.chmod(0o600)
        if not v.execute("SELECT 1 FROM routine").fetchone():
            for i, (name, fenster, um, mehrfach, schritte) in enumerate(VORLAGEN):
                rid = v.execute("INSERT INTO routine (name, zeitfenster, erinnern_um, mehrfach, reihenfolge) VALUES (?,?,?,?,?)",
                                (name, fenster, um, mehrfach, i)).lastrowid
                v.executemany("INSERT INTO routine_schritt (routine_id, text, reihenfolge) VALUES (?,?,?)",
                              [(rid, s, j) for j, s in enumerate(schritte)])


def neu_eintrag(v: sqlite3.Connection, text: str, **felder) -> int:
    jetzt = zeit.jetzt_iso()
    spalten = {"text": text, "erstellt": jetzt, "geaendert": jetzt, **felder}
    namen = ", ".join(spalten)
    return v.execute(f"INSERT INTO eintrag ({namen}) VALUES ({', '.join('?' * len(spalten))})",
                     tuple(spalten.values())).lastrowid


def eintrag_aendern(v: sqlite3.Connection, eid: int, **felder) -> None:
    felder["geaendert"] = zeit.jetzt_iso()
    v.execute(f"UPDATE eintrag SET {', '.join(f'{k} = ?' for k in felder)} WHERE id = ?", (*felder.values(), eid))
