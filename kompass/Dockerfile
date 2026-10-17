FROM python:3.12-slim
ENV PYTHONDONTWRITEBYTECODE=1 PYTHONUNBUFFERED=1 TZ=Europe/Zurich
RUN apt-get update && apt-get install -y --no-install-recommends tzdata && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY requirements.txt .
RUN pip install --no-cache-dir -r requirements.txt
COPY app.py .
COPY kompass kompass
VOLUME /data
EXPOSE 8000
# Ein Worker: Login-Sperre und Hintergrund-Thread (Kalender, Erinnerungen, Sicherung) leben im Prozess.
CMD ["gunicorn", "-w", "1", "--threads", "4", "-b", "0.0.0.0:8000", "--access-logfile", "-", "app:app"]
