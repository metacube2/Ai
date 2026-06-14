import os
import json
from datetime import datetime
from decimal import Decimal
from functools import wraps
from pathlib import Path

from flask import Flask, redirect, render_template, request, session, url_for
from werkzeug.security import check_password_hash, generate_password_hash

from amount_extractor import best_amount_candidate
from config import Config
from extractor import DataAggregator, DocumentExtractor
from paperless_client import PaperlessClient


def create_app():
    app = Flask(__name__)
    app.secret_key = os.environ.get("FINANCE_SECRET_KEY", "change-me-for-local-dev")
    admin_user = os.environ.get("FINANCE_ADMIN_USER", "metacube")
    auth_file = Path(os.environ.get("FINANCE_AUTH_FILE", "/app/output/auth.json"))

    def load_password_hash():
        if auth_file.exists():
            try:
                data = json.loads(auth_file.read_text(encoding="utf-8"))
                if data.get("user") == admin_user and data.get("password_hash"):
                    return data["password_hash"]
            except (OSError, ValueError):
                pass
        env_hash = os.environ.get("FINANCE_ADMIN_PASSWORD_HASH")
        if env_hash:
            return env_hash
        env_password = os.environ.get("FINANCE_ADMIN_PASSWORD", "metacube")
        return generate_password_hash(env_password)

    def save_password(password):
        auth_file.parent.mkdir(parents=True, exist_ok=True)
        payload = {
            "user": admin_user,
            "password_hash": generate_password_hash(password),
        }
        auth_file.write_text(json.dumps(payload, indent=2), encoding="utf-8")

    def login_required(view):
        @wraps(view)
        def wrapped(*args, **kwargs):
            if session.get("user") == admin_user:
                return view(*args, **kwargs)
            return redirect(url_for("login", next=request.path))
        return wrapped

    def load_context(year=None, tag="rechnung"):
        config = Config()
        client = PaperlessClient(config)
        extractor = DocumentExtractor(client, config)
        aggregator = DataAggregator(config)
        raw_docs = client.get_documents(tags=[tag] if tag else None, year=year, ordering="-archive_date")
        documents = extractor.extract_documents(raw_docs)
        result = aggregator.aggregate(
            documents,
            ["tag", "correspondent", "category", "month", "year"],
        )
        return config, client, raw_docs, documents, result

    @app.get("/login")
    def login():
        return render_template("login.html", error=None)

    @app.post("/login")
    def login_post():
        username = request.form.get("username", "")
        password = request.form.get("password", "")
        password_hash = load_password_hash()
        password_ok = bool(password_hash and check_password_hash(password_hash, password))
        if username == admin_user and password_ok:
            session.clear()
            session["user"] = admin_user
            return redirect(request.args.get("next") or url_for("dashboard"))
        return render_template("login.html", error="Login fehlgeschlagen"), 401

    @app.post("/logout")
    def logout():
        session.clear()
        return redirect(url_for("login"))

    @app.get("/password")
    @login_required
    def password_form():
        return render_template("password.html", error=None, ok=None)

    @app.post("/password")
    @login_required
    def password_update():
        current = request.form.get("current_password", "")
        new_password = request.form.get("new_password", "")
        repeat_password = request.form.get("repeat_password", "")
        if not check_password_hash(load_password_hash(), current):
            return render_template("password.html", error="Aktuelles Passwort stimmt nicht.", ok=None), 400
        if len(new_password) < 8:
            return render_template("password.html", error="Neues Passwort muss mindestens 8 Zeichen haben.", ok=None), 400
        if new_password != repeat_password:
            return render_template("password.html", error="Neue Passwoerter stimmen nicht ueberein.", ok=None), 400
        save_password(new_password)
        return render_template("password.html", error=None, ok="Passwort wurde gespeichert.")

    @app.get("/")
    @login_required
    def dashboard():
        year = request.args.get("year", type=int) or datetime.now().year
        tag = request.args.get("tag", "rechnung")
        config, _client, _raw_docs, documents, result = load_context(year=year, tag=tag)

        suggestions = []
        missing_amounts = [doc for doc in documents if doc.betrag is None]
        for doc in missing_amounts[:60]:
            candidate = best_amount_candidate(doc.raw_data.get("content", ""), config.currency)
            suggestions.append(
                {
                    "doc": doc,
                    "candidate": candidate,
                }
            )

        return render_template(
            "dashboard.html",
            title="Paperless Finance Suite",
            year=year,
            tag=tag,
            currency=config.currency,
            result=result,
            suggestions=suggestions,
        )

    @app.post("/sync")
    @login_required
    def sync_amounts():
        year = request.form.get("year", type=int) or datetime.now().year
        tag = request.form.get("tag", "rechnung")
        minimum_confidence = request.form.get("minimum_confidence", type=int) or 70
        limit = request.form.get("limit", type=int) or 50

        config, client, raw_docs, _documents, _result = load_context(year=year, tag=tag)
        amount_field = client.ensure_custom_field("betrag", "monetary")
        synced = 0
        skipped = 0

        for raw_doc in raw_docs:
            existing_amount = None
            for item in raw_doc.get("custom_fields") or []:
                if item.get("field") == amount_field["id"]:
                    existing_amount = item.get("value")
                    break
            if existing_amount not in (None, "", []):
                skipped += 1
                continue

            full_doc = client.get_document(raw_doc["id"])
            candidate = best_amount_candidate(full_doc.get("content", ""), config.currency)
            if not candidate or candidate.confidence < minimum_confidence:
                skipped += 1
                continue

            client.update_document_custom_field(full_doc, amount_field["id"], str(candidate.amount))
            synced += 1
            if synced >= limit:
                break

        return redirect(url_for("dashboard", year=year, tag=tag, synced=synced, skipped=skipped))

    @app.template_filter("money")
    def money(value):
        if value is None:
            return "-"
        if not isinstance(value, Decimal):
            value = Decimal(str(value))
        return f"{value:,.2f}".replace(",", "'")

    return app


app = create_app()
