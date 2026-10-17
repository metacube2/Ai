"""Einstieg fuer gunicorn: gunicorn app:app (siehe Dockerfile)."""
from kompass import create_app

app = create_app()
