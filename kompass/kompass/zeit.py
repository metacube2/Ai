"""Zeit: gespeichert wird UTC (ISO, Sekunden), angezeigt und geplant in Europe/Zurich."""
from __future__ import annotations

from datetime import date, datetime, time, timedelta, timezone
from zoneinfo import ZoneInfo

ZONE = ZoneInfo("Europe/Zurich")

# In Tests ersetzbar: liefert die aktuelle Zeit als UTC-datetime
_jetzt_quelle = None


def jetzt() -> datetime:
    return _jetzt_quelle() if _jetzt_quelle else datetime.now(timezone.utc)


def iso(dt: datetime) -> str:
    return dt.astimezone(timezone.utc).replace(microsecond=0).isoformat()


def aus_iso(text: str) -> datetime:
    dt = datetime.fromisoformat(text)
    return dt if dt.tzinfo else dt.replace(tzinfo=timezone.utc)


def jetzt_iso() -> str:
    return iso(jetzt())


def lokal(dt: datetime | None = None) -> datetime:
    return (dt or jetzt()).astimezone(ZONE)


def heute() -> date:
    return lokal().date()


def tag_text(d: date) -> str:
    return d.isoformat()


def um(tag: date, hhmm: str) -> datetime:
    """Lokale Uhrzeit an einem Tag als UTC-datetime (Sommerzeit richtig)."""
    h, m = (int(x) for x in hhmm.split(":"))
    return datetime.combine(tag, time(h, m), ZONE).astimezone(timezone.utc)


def in_fenster(hhmm_von: str, hhmm_bis: str, dt: datetime | None = None) -> bool:
    """Liegt die lokale Uhrzeit im Fenster? Fenster ueber Mitternacht (21:30-07:00) erlaubt."""
    t = lokal(dt).strftime("%H:%M")
    if hhmm_von <= hhmm_bis:
        return hhmm_von <= t < hhmm_bis
    return t >= hhmm_von or t < hhmm_bis


def tage_bis(tag_iso: str) -> int:
    return (date.fromisoformat(tag_iso) - heute()).days


def wochentag(d: date) -> str:
    return ["Montag", "Dienstag", "Mittwoch", "Donnerstag", "Freitag", "Samstag", "Sonntag"][d.weekday()]


def plus_tage(n: int) -> str:
    return (heute() + timedelta(days=n)).isoformat()
