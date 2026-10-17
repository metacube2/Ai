"""Kompass – ADHS-Alltags-Dashboard (kompass.aiscom.ch). Siehe PLAN.md und README.md."""
from __future__ import annotations

import os
import secrets
import sqlite3
import threading
import time
from collections import defaultdict
from datetime import timedelta
from functools import wraps
from pathlib import Path

from flask import Flask, abort, flash, g, jsonify, redirect, render_template, request, session, url_for
from werkzeug.middleware.proxy_fix import ProxyFix
from werkzeug.security import check_password_hash

from . import db as dbm
from . import einstellungen, erinnern, kalender, zeit

LOGIN_FENSTER_S = 600
LOGIN_MAX_FEHLER = 5
fehlversuche: dict[str, list[float]] = defaultdict(list)


def geheimer_schluessel(daten: Path) -> str:
    pfad = daten / "secret.key"
    if not pfad.exists():
        fd = os.open(pfad, os.O_WRONLY | os.O_CREAT, 0o600)
        with os.fdopen(fd, "w") as f:
            f.write(secrets.token_hex(32))
    return pfad.read_text().strip()


def gesperrt(ip: str) -> bool:
    jetzt = time.monotonic()
    fehlversuche[ip] = [t for t in fehlversuche[ip] if jetzt - t < LOGIN_FENSTER_S]
    return len(fehlversuche[ip]) >= LOGIN_MAX_FEHLER


def csrf_token() -> str:
    if "csrf" not in session:
        session["csrf"] = secrets.token_urlsafe(24)
    return session["csrf"]


def login_noetig(f):
    @wraps(f)
    def huelle(*a, **k):
        if session.get("konto") != "admin":
            if request.path.startswith("/api/"):
                return jsonify(error="Bitte neu anmelden."), 401
            return redirect(url_for("login"))
        return f(*a, **k)
    return huelle


def create_app(daten: Path | str | None = None, hintergrund: bool | None = None, cookie_secure: bool | None = None) -> Flask:
    daten = Path(daten or os.environ.get("KOMPASS_DATA", "/data"))
    daten.mkdir(parents=True, exist_ok=True)
    app = Flask(__name__, static_folder="static", template_folder="templates")
    app.wsgi_app = ProxyFix(app.wsgi_app, x_for=1, x_proto=1, x_host=1)
    if cookie_secure is None:
        cookie_secure = os.environ.get("KOMPASS_COOKIE_SECURE", "1") == "1"
    app.config.update(
        DATEN=daten, DB_PFAD=daten / "kompass.db",
        SECRET_KEY=geheimer_schluessel(daten),
        SESSION_COOKIE_HTTPONLY=True, SESSION_COOKIE_SAMESITE="Lax", SESSION_COOKIE_SECURE=cookie_secure,
        SESSION_COOKIE_NAME="kompass", PERMANENT_SESSION_LIFETIME=timedelta(days=30),
        MAX_CONTENT_LENGTH=256 * 1024,
    )
    dbm.anlegen(app.config["DB_PFAD"], daten)
    app.einst = einstellungen.Einstellungen(daten / "config.json")
    app.teardown_appcontext(dbm.schliessen)
    app.jinja_env.globals["csrf_token"] = csrf_token

    @app.after_request
    def kopfzeilen(antwort):
        h = antwort.headers
        h["X-Robots-Tag"] = "noindex, nofollow"
        h["X-Frame-Options"] = "DENY"
        h["X-Content-Type-Options"] = "nosniff"
        h["Referrer-Policy"] = "same-origin"
        h["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()"
        h["Content-Security-Policy"] = ("default-src 'self'; script-src 'self'; style-src 'self'; img-src 'self' data:; "
                                        "connect-src 'self'; manifest-src 'self'; worker-src 'self'; form-action 'self'; "
                                        "frame-ancestors 'none'; base-uri 'none'")
        if request.path.startswith("/api/"):
            h["Cache-Control"] = "no-store"
        return antwort

    @app.before_request
    def csrf_pruefen():
        if request.method != "POST":
            return None
        if request.path.startswith("/api/"):
            if not request.is_json:
                return jsonify(error="nur JSON"), 415
            gesendet = request.headers.get("X-CSRF", "")
        else:
            gesendet = request.form.get("csrf", "")
        if not secrets.compare_digest(gesendet, session.get("csrf", "-")):
            if request.path.startswith("/api/"):
                return jsonify(error="Sitzung abgelaufen, bitte Seite neu laden."), 400
            abort(400)
        return None

    @app.get("/gesund")
    def gesund():
        dbm.db().execute("SELECT 1")
        return "ok"

    @app.route("/login", methods=["GET", "POST"])
    def login():
        if request.method == "POST":
            ip = request.remote_addr or "?"
            if gesperrt(ip):
                flash("Zu viele Fehlversuche. Bitte in zehn Minuten nochmals versuchen.")
                return render_template("login.html"), 429
            k = dbm.db().execute("SELECT * FROM konto WHERE name = ?", (request.form.get("login", "").strip().lower(),)).fetchone()
            if k and check_password_hash(k["hash"], request.form.get("passwort", "")):
                session.clear()
                session.permanent = True
                session["konto"] = k["name"]
                return redirect(url_for("start"))
            fehlversuche[ip].append(time.monotonic())
            flash("Login oder Passwort stimmt nicht.")
        return render_template("login.html")

    @app.post("/logout")
    def logout():
        session.clear()
        return redirect(url_for("login"))

    @app.get("/")
    @login_noetig
    def start():
        return render_template("app.html", erstpasswort=(daten / "admin-passwort.txt").exists())

    @app.get("/manifest.webmanifest")
    def manifest():
        return app.send_static_file("manifest.webmanifest"), 200, {"Content-Type": "application/manifest+json"}

    @app.get("/sw.js")
    def service_worker():
        # Service Worker vom Wurzelpfad, damit er die ganze App abdeckt
        antwort = app.send_static_file("sw.js")
        antwort.headers["Cache-Control"] = "no-cache"
        antwort.headers["Service-Worker-Allowed"] = "/"
        return antwort

    from .api import api  # noqa: E402
    app.register_blueprint(api)
    app.login_noetig = login_noetig

    if hintergrund is None:
        hintergrund = os.environ.get("KOMPASS_HINTERGRUND", "1") == "1"
    if hintergrund:
        threading.Thread(target=hintergrund_schleife, args=(app,), daemon=True, name="kompass-hintergrund").start()
    return app


def sichern(db_pfad: Path, ordner: Path, behalten: int = 14) -> Path:
    ordner.mkdir(exist_ok=True)
    ziel = ordner / f"kompass-{zeit.lokal().strftime('%Y%m%d')}.db"
    quelle = sqlite3.connect(db_pfad)
    try:
        with sqlite3.connect(ziel) as z:
            quelle.backup(z)
    finally:
        quelle.close()
    ziel.chmod(0o600)
    for alt in sorted(ordner.glob("kompass-*.db"))[:-behalten]:
        alt.unlink()
    return ziel


def hintergrund_schleife(app: Flask) -> None:
    """Alle 30 s Erinnerungen, alle 10 min Kalender, taeglich 03:30 Sicherung. Darf nie stehen bleiben."""
    letzter_kalender = 0.0
    letzte_sicherung = None
    while True:
        try:
            cfg = app.einst.alle()
            v = dbm.verbinden(app.config["DB_PFAD"])
            try:
                if cfg["ical_url"] and time.monotonic() - letzter_kalender > 600:
                    letzter_kalender = time.monotonic()
                    try:
                        kalender.abgleichen(v, cfg["ical_url"])
                    except Exception as e:
                        app.logger.warning("Kalender nicht erreichbar (%s)", type(e).__name__)
                erinnern.durchlauf(v, cfg)
            finally:
                v.close()
            heute = zeit.heute().isoformat()
            if letzte_sicherung != heute and zeit.lokal().strftime("%H:%M") >= "03:30":
                sichern(app.config["DB_PFAD"], app.config["DATEN"] / "sicherung")
                letzte_sicherung = heute
        except Exception as e:
            app.logger.error("Hintergrund-Fehler: %s", type(e).__name__)
        time.sleep(30)
