"""bildervon.ingok.ch - Linkseite fuer Familienfotos.

Jede Familie hat ein eigenes Login und sieht nur die Anlaesse, fuer die der
Admin sie freigegeben hat. Ein Anlass (Jahr, Fest) traegt Links zu Nextcloud,
Immich oder PhotoPrism. Nur der Admin legt Familien, Anlaesse und Links an.
"""

from __future__ import annotations

import os
import secrets
import sqlite3
import time
from collections import defaultdict
from functools import wraps
from pathlib import Path
from urllib.parse import urlparse

from flask import Flask, abort, flash, g, redirect, render_template, request, session, url_for
from werkzeug.middleware.proxy_fix import ProxyFix
from werkzeug.security import check_password_hash, generate_password_hash

DATA_DIR = Path(os.environ.get("BILDERVON_DATA", "/data"))
DB_PFAD = DATA_DIR / "bildervon.db"
DIENSTE = {
    "nextcloud": "Nextcloud",
    "immich": "Immich",
    "photoprism": "PhotoPrism",
    "andere": "Link",
}
LOGIN_FENSTER_S = 600
LOGIN_MAX_FEHLER = 5

app = Flask(__name__)
app.wsgi_app = ProxyFix(app.wsgi_app, x_for=1, x_proto=1, x_host=1)


def geheimer_schluessel() -> str:
    pfad = DATA_DIR / "secret.key"
    if not pfad.exists():
        pfad.write_text(secrets.token_hex(32))
        pfad.chmod(0o600)
    return pfad.read_text().strip()


def db() -> sqlite3.Connection:
    if "db" not in g:
        g.db = sqlite3.connect(DB_PFAD)
        g.db.row_factory = sqlite3.Row
        g.db.execute("PRAGMA foreign_keys = ON")
    return g.db


@app.teardown_appcontext
def db_schliessen(_exc) -> None:
    verbindung = g.pop("db", None)
    if verbindung is not None:
        verbindung.close()


def db_anlegen() -> None:
    DATA_DIR.mkdir(parents=True, exist_ok=True)
    with sqlite3.connect(DB_PFAD) as verbindung:
        verbindung.executescript(
            """
            CREATE TABLE IF NOT EXISTS konto (
                id INTEGER PRIMARY KEY,
                name TEXT NOT NULL,
                login TEXT NOT NULL UNIQUE COLLATE NOCASE,
                pw_hash TEXT NOT NULL,
                admin INTEGER NOT NULL DEFAULT 0
            );
            CREATE TABLE IF NOT EXISTS anlass (
                id INTEGER PRIMARY KEY,
                jahr INTEGER NOT NULL,
                titel TEXT NOT NULL,
                datum TEXT NOT NULL DEFAULT '',
                beschreibung TEXT NOT NULL DEFAULT ''
            );
            CREATE TABLE IF NOT EXISTS link (
                id INTEGER PRIMARY KEY,
                anlass_id INTEGER NOT NULL REFERENCES anlass(id) ON DELETE CASCADE,
                dienst TEXT NOT NULL,
                url TEXT NOT NULL,
                bezeichnung TEXT NOT NULL DEFAULT ''
            );
            CREATE TABLE IF NOT EXISTS sicht (
                anlass_id INTEGER NOT NULL REFERENCES anlass(id) ON DELETE CASCADE,
                konto_id INTEGER NOT NULL REFERENCES konto(id) ON DELETE CASCADE,
                PRIMARY KEY (anlass_id, konto_id)
            );
            """
        )
        hat_admin = verbindung.execute("SELECT 1 FROM konto WHERE admin = 1").fetchone()
        if not hat_admin:
            # Erststart: Admin-Passwort zufaellig erzeugen und nur in eine
            # root-lesbare Datei schreiben, nie ins Log.
            passwort = secrets.token_urlsafe(18)
            verbindung.execute(
                "INSERT INTO konto (name, login, pw_hash, admin) VALUES (?, ?, ?, 1)",
                ("Admin", "admin", generate_password_hash(passwort)),
            )
            datei = DATA_DIR / "admin-passwort.txt"
            datei.write_text(f"Login: admin\nPasswort: {passwort}\n")
            datei.chmod(0o600)


db_anlegen()
app.config.update(
    SECRET_KEY=geheimer_schluessel(),
    SESSION_COOKIE_HTTPONLY=True,
    SESSION_COOKIE_SAMESITE="Lax",
    SESSION_COOKIE_SECURE=os.environ.get("BILDERVON_COOKIE_SECURE", "1") == "1",
    SESSION_COOKIE_NAME="bildervon",
    PERMANENT_SESSION_LIFETIME=60 * 60 * 24 * 30,
)

# Einfacher Schutz gegen Passwortraten: je Absender-IP hoechstens
# LOGIN_MAX_FEHLER Fehlversuche in LOGIN_FENSTER_S Sekunden.
fehlversuche: dict[str, list[float]] = defaultdict(list)


def gesperrt(ip: str) -> bool:
    jetzt = time.monotonic()
    fehlversuche[ip] = [t for t in fehlversuche[ip] if jetzt - t < LOGIN_FENSTER_S]
    return len(fehlversuche[ip]) >= LOGIN_MAX_FEHLER


@app.after_request
def kopfzeilen(antwort):
    antwort.headers["X-Robots-Tag"] = "noindex, nofollow"
    antwort.headers["X-Frame-Options"] = "DENY"
    antwort.headers["X-Content-Type-Options"] = "nosniff"
    antwort.headers["Referrer-Policy"] = "no-referrer"
    antwort.headers["Content-Security-Policy"] = (
        "default-src 'self'; style-src 'self'; img-src 'self' data:; form-action 'self'; frame-ancestors 'none'"
    )
    return antwort


def csrf_token() -> str:
    if "csrf" not in session:
        session["csrf"] = secrets.token_urlsafe(24)
    return session["csrf"]


app.jinja_env.globals["csrf_token"] = csrf_token
app.jinja_env.globals["DIENSTE"] = DIENSTE


@app.before_request
def csrf_pruefen() -> None:
    if request.method == "POST":
        if not secrets.compare_digest(request.form.get("csrf", ""), session.get("csrf", "-")):
            abort(400)


def aktuelles_konto():
    konto_id = session.get("konto_id")
    if konto_id is None:
        return None
    return db().execute("SELECT * FROM konto WHERE id = ?", (konto_id,)).fetchone()


def login_noetig(funktion):
    @wraps(funktion)
    def huelle(*args, **kwargs):
        g.konto = aktuelles_konto()
        if g.konto is None:
            return redirect(url_for("login", weiter=request.path))
        return funktion(*args, **kwargs)
    return huelle


def admin_noetig(funktion):
    @wraps(funktion)
    @login_noetig
    def huelle(*args, **kwargs):
        if not g.konto["admin"]:
            abort(403)
        return funktion(*args, **kwargs)
    return huelle


def url_gueltig(url: str) -> bool:
    teile = urlparse(url)
    return teile.scheme == "https" and bool(teile.netloc)


def anlaesse_fuer(konto) -> list[dict]:
    if konto["admin"]:
        zeilen = db().execute("SELECT * FROM anlass ORDER BY jahr DESC, datum DESC, titel").fetchall()
    else:
        zeilen = db().execute(
            "SELECT a.* FROM anlass a JOIN sicht s ON s.anlass_id = a.id "
            "WHERE s.konto_id = ? ORDER BY a.jahr DESC, a.datum DESC, a.titel",
            (konto["id"],),
        ).fetchall()
    ergebnis = []
    for zeile in zeilen:
        eintrag = dict(zeile)
        eintrag["links"] = db().execute(
            "SELECT * FROM link WHERE anlass_id = ? ORDER BY id", (zeile["id"],)
        ).fetchall()
        eintrag["familien"] = [
            r["name"] for r in db().execute(
                "SELECT k.name FROM konto k JOIN sicht s ON s.konto_id = k.id "
                "WHERE s.anlass_id = ? ORDER BY k.name", (zeile["id"],)
            )
        ]
        ergebnis.append(eintrag)
    return ergebnis


@app.route("/login", methods=["GET", "POST"])
def login():
    weiter = request.args.get("weiter", "/")
    if not weiter.startswith("/") or weiter.startswith("//"):
        weiter = "/"
    if request.method == "POST":
        ip = request.remote_addr or "?"
        if gesperrt(ip):
            flash("Zu viele Fehlversuche. Bitte in zehn Minuten nochmals versuchen.")
            return render_template("login.html"), 429
        konto = db().execute(
            "SELECT * FROM konto WHERE login = ?", (request.form.get("login", "").strip(),)
        ).fetchone()
        if konto and check_password_hash(konto["pw_hash"], request.form.get("passwort", "")):
            session.clear()
            session.permanent = True
            session["konto_id"] = konto["id"]
            return redirect(weiter)
        fehlversuche[ip].append(time.monotonic())
        flash("Login oder Passwort stimmt nicht.")
    return render_template("login.html")


@app.post("/logout")
def logout():
    session.clear()
    return redirect(url_for("login"))


@app.route("/")
@login_noetig
def start():
    alle = anlaesse_fuer(g.konto)
    jahre = sorted({a["jahr"] for a in alle}, reverse=True)
    try:
        jahr = int(request.args.get("jahr", jahre[0] if jahre else 0))
    except ValueError:
        jahr = jahre[0] if jahre else 0
    return render_template(
        "start.html", jahre=jahre, jahr=jahr, anlaesse=[a for a in alle if a["jahr"] == jahr]
    )


# ---------------------------------------------------------------- Admin

@app.route("/admin")
@admin_noetig
def admin():
    familien = db().execute("SELECT * FROM konto WHERE admin = 0 ORDER BY name").fetchall()
    return render_template("admin.html", familien=familien, anlaesse=anlaesse_fuer(g.konto))


@app.post("/admin/familie")
@admin_noetig
def familie_speichern():
    form = request.form
    name = form.get("name", "").strip()
    login_name = form.get("login", "").strip()
    passwort = form.get("passwort", "")
    familie_id = form.get("id")
    if not name or not login_name:
        flash("Name und Login sind noetig.")
        return redirect(url_for("admin"))
    if passwort and len(passwort) < 8:
        flash("Das Passwort braucht mindestens 8 Zeichen.")
        return redirect(url_for("admin"))
    try:
        if familie_id:
            db().execute("UPDATE konto SET name = ?, login = ? WHERE id = ? AND admin = 0",
                         (name, login_name, familie_id))
            if passwort:
                db().execute("UPDATE konto SET pw_hash = ? WHERE id = ? AND admin = 0",
                             (generate_password_hash(passwort), familie_id))
        else:
            if not passwort:
                flash("Eine neue Familie braucht ein Passwort.")
                return redirect(url_for("admin"))
            db().execute("INSERT INTO konto (name, login, pw_hash) VALUES (?, ?, ?)",
                         (name, login_name, generate_password_hash(passwort)))
        db().commit()
        flash(f"Familie „{name}“ gespeichert.")
    except sqlite3.IntegrityError:
        flash("Dieser Login ist schon vergeben.")
    return redirect(url_for("admin"))


@app.post("/admin/familie/<int:familie_id>/loeschen")
@admin_noetig
def familie_loeschen(familie_id: int):
    db().execute("DELETE FROM konto WHERE id = ? AND admin = 0", (familie_id,))
    db().commit()
    flash("Familie geloescht.")
    return redirect(url_for("admin"))


@app.post("/admin/passwort")
@admin_noetig
def admin_passwort():
    neu = request.form.get("passwort", "")
    if len(neu) < 12:
        flash("Das Admin-Passwort braucht mindestens 12 Zeichen.")
    else:
        db().execute("UPDATE konto SET pw_hash = ? WHERE id = ?", (generate_password_hash(neu), g.konto["id"]))
        db().commit()
        (DATA_DIR / "admin-passwort.txt").unlink(missing_ok=True)
        flash("Admin-Passwort geaendert.")
    return redirect(url_for("admin"))


@app.route("/admin/anlass", methods=["GET", "POST"])
@app.route("/admin/anlass/<int:anlass_id>", methods=["GET", "POST"])
@admin_noetig
def anlass_bearbeiten(anlass_id: int | None = None):
    familien = db().execute("SELECT * FROM konto WHERE admin = 0 ORDER BY name").fetchall()
    anlass = None
    if anlass_id is not None:
        anlass = db().execute("SELECT * FROM anlass WHERE id = ?", (anlass_id,)).fetchone()
        if anlass is None:
            abort(404)
    if request.method == "POST":
        form = request.form
        titel = form.get("titel", "").strip()
        try:
            jahr = int(form.get("jahr", ""))
        except ValueError:
            jahr = 0
        if not titel or not 1900 <= jahr <= 2200:
            flash("Titel und ein gueltiges Jahr sind noetig.")
            return redirect(request.path)
        links = []
        for dienst, url, bezeichnung in zip(form.getlist("dienst"), form.getlist("url"), form.getlist("bezeichnung")):
            url = url.strip()
            if not url:
                continue
            if not url_gueltig(url):
                flash(f"Kein gueltiger https-Link: {url}")
                return redirect(request.path)
            links.append((dienst if dienst in DIENSTE else "andere", url, bezeichnung.strip()))
        werte = (jahr, titel, form.get("datum", "").strip(), form.get("beschreibung", "").strip())
        if anlass is None:
            anlass_id = db().execute(
                "INSERT INTO anlass (jahr, titel, datum, beschreibung) VALUES (?, ?, ?, ?)", werte
            ).lastrowid
        else:
            db().execute("UPDATE anlass SET jahr = ?, titel = ?, datum = ?, beschreibung = ? WHERE id = ?",
                         (*werte, anlass_id))
        db().execute("DELETE FROM link WHERE anlass_id = ?", (anlass_id,))
        db().executemany("INSERT INTO link (anlass_id, dienst, url, bezeichnung) VALUES (?, ?, ?, ?)",
                         [(anlass_id, *link) for link in links])
        db().execute("DELETE FROM sicht WHERE anlass_id = ?", (anlass_id,))
        gueltige = {str(f["id"]) for f in familien}
        db().executemany("INSERT INTO sicht (anlass_id, konto_id) VALUES (?, ?)",
                         [(anlass_id, int(k)) for k in form.getlist("familie") if k in gueltige])
        db().commit()
        flash(f"Anlass „{titel}“ gespeichert.")
        return redirect(url_for("admin"))
    links = []
    freigegeben = set()
    if anlass is not None:
        links = db().execute("SELECT * FROM link WHERE anlass_id = ? ORDER BY id", (anlass_id,)).fetchall()
        freigegeben = {r["konto_id"] for r in db().execute(
            "SELECT konto_id FROM sicht WHERE anlass_id = ?", (anlass_id,))}
    return render_template("anlass.html", anlass=anlass, familien=familien, links=links,
                           freigegeben=freigegeben, leere_zeilen=max(2, 4 - len(links)),
                           jahr_vorschlag=time.localtime().tm_year)


@app.post("/admin/anlass/<int:anlass_id>/loeschen")
@admin_noetig
def anlass_loeschen(anlass_id: int):
    db().execute("DELETE FROM anlass WHERE id = ?", (anlass_id,))
    db().commit()
    flash("Anlass geloescht.")
    return redirect(url_for("admin"))


@app.route("/gesund")
def gesund():
    return "ok"
