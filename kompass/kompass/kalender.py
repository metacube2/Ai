"""Google-Kalender lesen ueber die geheime iCal-Adresse (nur lesen, 14 Tage voraus)."""
from __future__ import annotations

import sqlite3
import urllib.request
from datetime import date, datetime, timedelta

import icalendar
import recurring_ical_events

from . import zeit

VORAUS_TAGE = 14
ZUSTAND = {"letzter_abruf": None, "fehler_seit": None, "anzahl": 0}


def holen(url: str, timeout: int = 20) -> bytes:
    if url.startswith("webcal://"):
        url = "https://" + url[len("webcal://"):]
    req = urllib.request.Request(url, headers={"User-Agent": "Kompass/1"})
    with urllib.request.urlopen(req, timeout=timeout) as r:
        return r.read(10 * 1024 * 1024)


def termine_aus_ics(daten: bytes, von: datetime, bis: datetime) -> list[dict]:
    kal = icalendar.Calendar.from_ical(daten)
    ergebnis = []
    for ev in recurring_ical_events.of(kal).between(von, bis):
        if str(ev.get("STATUS", "")).upper() == "CANCELLED":
            continue
        beginn = ev.get("DTSTART").dt
        ende_feld = ev.get("DTEND")
        ende = ende_feld.dt if ende_feld else None
        ganztags = isinstance(beginn, date) and not isinstance(beginn, datetime)
        if ganztags:
            b, e = beginn.isoformat(), (ende or beginn + timedelta(days=1)).isoformat()
        else:
            if beginn.tzinfo is None:
                beginn = beginn.replace(tzinfo=zeit.ZONE)
            if ende is not None and isinstance(ende, datetime) and ende.tzinfo is None:
                ende = ende.replace(tzinfo=zeit.ZONE)
            b = zeit.iso(beginn)
            e = zeit.iso(ende) if isinstance(ende, datetime) else None
        ergebnis.append({"uid": str(ev.get("UID", "")) or f"ohne-{b}", "beginn": b, "ende": e,
                         "titel": str(ev.get("SUMMARY", "")).strip()[:200] or "(ohne Titel)",
                         "ganztags": 1 if ganztags else 0, "ort": str(ev.get("LOCATION", "")).strip()[:200] or None})
    return ergebnis


def abgleichen(v: sqlite3.Connection, url: str) -> int:
    """Holt den Kalender und ersetzt den Zwischenspeicher. Fehler lassen den alten Stand stehen."""
    jetzt = zeit.jetzt()
    try:
        termine = termine_aus_ics(holen(url), jetzt - timedelta(days=1), jetzt + timedelta(days=VORAUS_TAGE))
    except Exception:  # Adresse nie loggen: nur den Zustand merken
        ZUSTAND["fehler_seit"] = ZUSTAND["fehler_seit"] or zeit.jetzt_iso()
        raise
    with v:
        v.execute("DELETE FROM termin")
        v.executemany("INSERT OR REPLACE INTO termin (uid, beginn, ende, titel, ganztags, ort) VALUES (:uid,:beginn,:ende,:titel,:ganztags,:ort)",
                      termine)
    ZUSTAND.update(letzter_abruf=zeit.jetzt_iso(), fehler_seit=None, anzahl=len(termine))
    return len(termine)


def leeren(v: sqlite3.Connection) -> None:
    with v:
        v.execute("DELETE FROM termin")
    ZUSTAND.update(letzter_abruf=None, fehler_seit=None, anzahl=0)

