"""Anmeldung, Seiten, Sicherheits-Kopfzeilen, PWA-Dateien."""
import re

from kompass import fehlversuche


def passwort(app):
    return re.search(r"Passwort: (\S+)", (app.config["DATEN"] / "admin-passwort.txt").read_text()).group(1)


def csrf(seite: bytes) -> str:
    return re.search(rb'name="csrf" value="([^"]+)"', seite).group(1).decode()


def test_login_ablauf_und_passwortwechsel(app):
    fehlversuche.clear()
    c = app.test_client()
    seite = c.get("/login").data
    r = c.post("/login", data={"csrf": csrf(seite), "login": "admin", "passwort": passwort(app)})
    assert r.status_code == 302 and r.headers["Location"].endswith("/")
    start = c.get("/")
    assert start.status_code == 200 and b'data-erstpasswort="1"' in start.data
    token = re.search(rb'name="csrf" content="([^"]+)"', start.data).group(1).decode()
    assert c.post("/api/passwort", json={"alt": "falsch", "neu": "x" * 12}, headers={"X-CSRF": token}).status_code == 403
    assert c.post("/api/passwort", json={"alt": passwort(app), "neu": "kurz"}, headers={"X-CSRF": token}).status_code == 400
    alt = passwort(app)
    assert c.post("/api/passwort", json={"alt": alt, "neu": "ein-langes-test-passwort"}, headers={"X-CSRF": token}).status_code == 200
    assert not (app.config["DATEN"] / "admin-passwort.txt").exists()
    assert b'data-erstpasswort="0"' in c.get("/").data


def test_login_sperre(app):
    fehlversuche.clear()
    c = app.test_client()
    for _ in range(5):
        seite = c.get("/login").data
        assert c.post("/login", data={"csrf": csrf(seite), "login": "admin", "passwort": "nein"}).status_code == 200
    seite = c.get("/login").data
    assert c.post("/login", data={"csrf": csrf(seite), "login": "admin", "passwort": passwort(app)}).status_code == 429
    fehlversuche.clear()


def test_login_ohne_csrf_abgelehnt(app):
    assert app.test_client().post("/login", data={"login": "admin", "passwort": "x"}).status_code == 400


def test_kopfzeilen_und_pwa(client):
    r = client.get("/")
    csp = r.headers["Content-Security-Policy"]
    assert "script-src 'self'" in csp and "unsafe-inline" not in csp and r.headers["X-Frame-Options"] == "DENY"
    assert b"<script>" not in r.data and b"style=" not in r.data  # CSP: nichts Inline
    assert client.get("/api/heute").headers["Cache-Control"] == "no-store"
    m = client.get("/manifest.webmanifest")
    assert m.status_code == 200 and m.headers["Content-Type"].startswith("application/manifest+json") and b"standalone" in m.data
    sw = client.get("/sw.js")
    assert sw.status_code == 200 and sw.headers["Service-Worker-Allowed"] == "/"
    for datei in ("app.js", "stil.css", "icon.svg", "icon-192.png", "icon-512.png", "icon-512-maskable.png", "icon-180.png"):
        assert client.get(f"/static/{datei}").status_code == 200, datei
    assert client.get("/gesund").data == b"ok"


def test_login_seite_ohne_inline(app):
    seite = app.test_client().get("/login")
    assert b"<script" not in seite.data and b"style=" not in seite.data


def test_geheimnisse_nicht_im_log(client, app, caplog, monkeypatch):
    from kompass import zerleger

    def kaputt(schluessel, *a):
        raise OSError(f"HTTP 401 fuer {schluessel}")
    monkeypatch.setattr(zerleger, "anfragen", kaputt)
    zerleger._letzter["t"] = 0
    client.p("/api/einstellungen", {"claude_schluessel": "sk-ant-geheim-9999"})
    eid = client.p("/api/eintrag", {"text": "x"}).get_json()["id"]
    r = client.p("/api/zerlegen", {"eintrag_id": eid})
    assert r.status_code == 503 and "geheim" not in r.get_data(as_text=True)
    assert "geheim" not in caplog.text
    assert oct((app.config["DATEN"] / "config.json").stat().st_mode)[-3:] == "600"
