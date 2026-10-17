import sys
from datetime import datetime
from pathlib import Path

import pytest

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))

from kompass import create_app, zeit  # noqa: E402


@pytest.fixture
def uhr(monkeypatch):
    """Feste lokale Uhrzeit setzen: uhr("2026-10-19 10:00")."""
    stand = {"dt": datetime(2026, 10, 19, 10, 0, tzinfo=zeit.ZONE)}

    def setzen(text):
        stand["dt"] = datetime.fromisoformat(text).replace(tzinfo=zeit.ZONE)
    monkeypatch.setattr(zeit, "_jetzt_quelle", lambda: stand["dt"].astimezone(zeit.timezone.utc))
    setzen.stand = stand
    return setzen


@pytest.fixture(autouse=True)
def kein_netz(monkeypatch):
    """Tests gehen nie ins Netz (Claude, ntfy, iCal werden ersetzt oder schlagen hier laut fehl)."""
    import urllib.request

    def gesperrt(*a, **k):
        raise AssertionError("Netzzugriff im Test")
    monkeypatch.setattr(urllib.request, "urlopen", gesperrt)


@pytest.fixture
def app(tmp_path, uhr):
    return create_app(tmp_path, hintergrund=False, cookie_secure=False)


@pytest.fixture
def client(app):
    c = app.test_client()
    with c.session_transaction() as s:
        s["konto"] = "admin"
        s["csrf"] = "test-csrf"

    def post(pfad, daten=None):
        return c.post(pfad, json=daten or {}, headers={"X-CSRF": "test-csrf"})
    c.p = post
    return c
