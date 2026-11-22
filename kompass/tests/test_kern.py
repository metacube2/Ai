"""Kernlogik: Jetzt-Karte, Hauptaufgaben, Routinen, Papier, Zerleger, Fokus, Erinnerungen, Kalender."""
import json
from pathlib import Path

import pytest

from kompass import auswahl, db as dbm, erinnern, kalender, zerleger, zeit


def neu(client, text):
    r = client.p("/api/eintrag", {"text": text})
    assert r.status_code == 201
    return r.get_json()["id"]


def heute(client, ohne=""):
    return client.get(f"/api/heute?ohne={ohne}").get_json()


# ---- Erfassen, Einsortieren, Hauptaufgaben ---------------------------------------------

def test_erfassen_landet_im_eingang(client):
    eid = neu(client, "  Zahnarzt   anrufen ")
    liste = client.get("/api/eingang").get_json()["eintraege"]
    assert [e["text"] for e in liste] == ["Zahnarzt anrufen"] and liste[0]["id"] == eid
    assert heute(client)["eingang"] == 1


def test_erfassen_ohne_text_und_ohne_csrf(client):
    assert client.p("/api/eintrag", {"text": "  "}).status_code == 400
    assert client.post("/api/eintrag", json={"text": "x"}).status_code == 400  # ohne X-CSRF
    assert client.post("/api/eintrag", data={"text": "x"}, headers={"X-CSRF": "test-csrf"}).status_code == 415


def test_ohne_anmeldung_401(app):
    c = app.test_client()
    assert c.get("/api/heute").status_code == 401
    assert c.get("/").status_code == 302


def test_hoechstens_drei_hauptaufgaben(client):
    ids = [neu(client, f"Aufgabe {i}") for i in range(4)]
    for eid in ids[:3]:
        assert client.p(f"/api/eintrag/{eid}", {"aktion": "heute", "haupt": True}).status_code == 200
    r = client.p(f"/api/eintrag/{ids[3]}", {"aktion": "heute", "haupt": True})
    assert r.status_code == 409 and r.get_json()["voll"]
    # ohne ausdrueckliches haupt=True kommt sie als „auch heute“ dazu
    assert client.p(f"/api/eintrag/{ids[3]}", {"aktion": "heute"}).get_json()["heute"] == 0
    d = heute(client)
    assert len(d["haupt"]) == 3 and len(d["auch_heute"]) == 1


def test_heute_neu_entscheiden_ohne_schuldzaehler(client, uhr):
    eid = neu(client, "Steuern ordnen")
    client.p(f"/api/eintrag/{eid}", {"aktion": "heute"})
    uhr("2026-10-21 08:00")
    d = heute(client)
    assert [e["id"] for e in d["neu_entscheiden"]] == [eid] and d["haupt"] == []
    assert "ueberfaellig" not in json.dumps(d)
    client.p(f"/api/eintrag/{eid}", {"aktion": "heute"})
    assert heute(client)["neu_entscheiden"] == []


# ---- Jetzt-Karte -----------------------------------------------------------------------

def test_jetzt_karte_reihenfolge(client, uhr):
    uhr("2026-10-19 12:00")  # ausserhalb aller Routinen-Fenster
    assert heute(client)["jetzt"] is None
    aufgabe = neu(client, "Bewerbung schreiben")
    client.p(f"/api/eintrag/{aufgabe}", {"aktion": "heute", "haupt": True})
    assert heute(client)["jetzt"]["eintrag_id"] == aufgabe
    # Papier-Frist in 2 Tagen geht vor
    r = client.p("/api/papier", {"text": "Krankenkasse", "frist": "2026-10-21", "naechster_schritt": "Brief öffnen"})
    pid = r.get_json()["id"]
    k = heute(client)["jetzt"]
    assert k["art"] == "papier" and k["text"] == "Brief öffnen" and k["tage"] == 2
    # Routine im Zeitfenster geht vor allem
    uhr("2026-10-19 07:00")
    k = heute(client)["jetzt"]
    assert k["art"] == "routine" and k["titel"] == "Morgen" and k["text"] == "Ein Glas Wasser trinken"
    # „Nicht jetzt“ nimmt die naechste Karte
    assert heute(client, "routine:1")["jetzt"]["eintrag_id"] == pid
    assert heute(client, f"routine:1,eintrag:{pid}")["jetzt"]["eintrag_id"] == aufgabe


def test_zerlegte_aufgabe_zeigt_ersten_schritt(client, uhr):
    uhr("2026-10-19 12:00")
    eid = neu(client, "Keller aufräumen")
    client.p(f"/api/eintrag/{eid}", {"aktion": "heute", "haupt": True})
    client.p(f"/api/eintrag/{eid}/schritte", {"schritte": [{"text": "Licht anmachen", "min": 1}, {"text": "Eine Kiste wählen", "min": 3}]})
    k = heute(client)["jetzt"]
    assert k["art"] == "schritt" and k["text"] == "Licht anmachen" and k["titel"] == "Keller aufräumen"
    schritte = heute(client)["haupt"][0]["schritte"]
    for s in schritte:
        client.p(f"/api/eintrag/{s['id']}", {"aktion": "erledigt"})
    # letzter Schritt erledigt -> Aufgabe erledigt
    assert heute(client)["haupt"] == [] and heute(client)["erledigt_heute"] == 3


# ---- Routinen --------------------------------------------------------------------------

def test_routine_abhaken_und_mehrfach(client):
    rs = client.get("/api/routinen").get_json()["routinen"]
    haus = next(r for r in rs if r["name"] == "Haus verlassen")
    for s in haus["schritte"]:
        st = client.p(f"/api/routine/{haus['id']}/haken", {"schritt_id": s["id"]}).get_json()
    assert st["fertig"] and st["durchgang"] == 1
    st = client.p(f"/api/routine/{haus['id']}/neu").get_json()
    assert st["durchgang"] == 2 and not st["fertig"] and not any(s["erledigt"] for s in st["schritte"])
    fremd = rs[0]["schritte"][0]["id"]
    assert client.p(f"/api/routine/{haus['id']}/haken", {"schritt_id": fremd}).status_code == 400


def test_routine_bearbeiten_behaelt_haken(client):
    r = client.get("/api/routinen").get_json()["routinen"][0]
    client.p(f"/api/routine/{r['id']}/haken", {"schritt_id": r["schritte"][0]["id"]})
    texte = [s["text"] for s in r["schritte"]] + ["Fenster öffnen"]
    neu_r = next(x for x in client.p("/api/routine", {"id": r["id"], "name": r["name"], "zeitfenster": r["zeitfenster"],
                                                       "erinnern_um": r["erinnern_um"], "schritte": texte}).get_json()["routinen"]
                 if x["id"] == r["id"])
    assert neu_r["schritte"][0]["erledigt"] and len(neu_r["schritte"]) == 5
    assert client.p("/api/routine", {"name": "X", "zeitfenster": "25:00-26:00", "schritte": ["a"]}).status_code == 400


# ---- Papierkram ------------------------------------------------------------------------

def test_papier_braucht_naechsten_schritt_und_kettet(client):
    assert client.p("/api/papier", {"text": "Handy kündigen", "frist": "2026-11-30"}).status_code == 400
    eid = client.p("/api/papier", {"text": "Handy kündigen", "frist": "2026-11-30", "naechster_schritt": "Vertrag suchen",
                                   "kategorie": "kuendigung", "betrag": "29.90"}).get_json()["id"]
    e = client.p(f"/api/papier/{eid}/schritt", {"naechster_schritt": "Kündigung per Mail senden"}).get_json()
    assert e["papier"]["naechster_schritt"] == "Kündigung per Mail senden" and e["papier"]["betrag_rappen"] == 2990
    client.p(f"/api/papier/{eid}/schritt", {"fertig": True})
    d = client.get("/api/papier").get_json()
    assert d["offen"] == [] and d["erledigt"][0]["id"] == eid


def test_eingang_zu_papier(client):
    eid = neu(client, "Rechnung Elektriker")
    client.p("/api/papier", {"eintrag_id": eid, "frist": "2026-10-30", "naechster_schritt": "Betrag prüfen", "kategorie": "rechnung"})
    assert client.get("/api/eingang").get_json()["eintraege"] == []
    assert client.get("/api/papier").get_json()["offen"][0]["id"] == eid


# ---- Zerleger --------------------------------------------------------------------------

def test_zerleger_pruefung():
    s = zerleger.pruefen('Hier: [{"text": "Ordner  öffnen", "min": 1}, {"text": ""}, {"x": 1}, {"text": "Mail lesen", "min": "zwei"}]')
    assert s == [{"text": "Ordner öffnen", "min": 1}, {"text": "Mail lesen", "min": 5}]
    with pytest.raises(zerleger.ZerlegerFehler):
        zerleger.pruefen("leider nein")
    with pytest.raises(zerleger.ZerlegerFehler):
        zerleger.pruefen("[kaputt")
    lang = json.dumps([{"text": f"Schritt {i}", "min": 2} for i in range(30)])
    assert len(zerleger.pruefen(lang)) == zerleger.MAX_SCHRITTE


def test_zerleger_ohne_schluessel_und_bremse(monkeypatch):
    with pytest.raises(zerleger.ZerlegerFehler, match="Schlüssel"):
        zerleger.zerlegen("", "claude-haiku-5-5", "x")
    zerleger._letzter["t"] = 0
    gefragt = []
    s = zerleger.zerlegen("k", "m", "Keller", fragen=lambda *a: gefragt.append(a) or '[{"text":"Licht an","min":1}]')
    assert s[0]["text"] == "Licht an" and gefragt[0][2] == "Keller"
    with pytest.raises(zerleger.ZerlegerFehler, match="warten"):
        zerleger.zerlegen("k", "m", "Keller", fragen=lambda *a: "[]")
    zerleger._letzter["t"] = 0

    def kaputt(*a):
        raise OSError("api-key-im-text sk-ant-123")
    with pytest.raises(zerleger.ZerlegerFehler) as f:
        zerleger.zerlegen("sk-ant-123", "m", "x", fragen=kaputt)
    assert "sk-ant" not in str(f.value)


def test_zerlegen_api_ohne_schluessel(client):
    eid = neu(client, "Umzug planen")
    r = client.p("/api/zerlegen", {"eintrag_id": eid})
    assert r.status_code == 503 and "selbst eintragen" in r.get_json()["error"]


# ---- Fokus und Hyperfokus --------------------------------------------------------------

def test_fokus_kette_und_hyperfokus(client, app, uhr):
    eid = neu(client, "Bericht")
    uhr("2026-10-19 10:00")
    f = client.p("/api/fokus", {"aktion": "start", "minuten": 45, "eintrag_id": eid}).get_json()["fokus"]
    assert f["rest_s"] == 45 * 60 and f["text"] == "Bericht"
    uhr("2026-10-19 10:46")
    cfg = app.einst.alle() | {"ntfy_thema": "kompass-test"}
    gesendet = []
    with app.app_context():
        v = dbm.db()
        assert erinnern.durchlauf(v, cfg, sender=lambda *a: gesendet.append(a[1])) == [f"fokus:{f['id']}:ende"]
        client.p("/api/fokus", {"aktion": "verlaengern", "minuten": 50})
        uhr("2026-10-19 11:31")
        keys = erinnern.durchlauf(v, cfg, sender=lambda *a: gesendet.append(a[1]))
    assert f"hyper:{f['id']}" in keys and "Schon 91 Minuten am Stück" in gesendet
    client.p("/api/fokus", {"aktion": "erledigt"})
    assert client.get("/api/fokus").get_json()["fokus"] is None
    assert heute(client)["erledigt_heute"] == 1


# ---- Erinnerungen ----------------------------------------------------------------------

def test_erinnerungen_ruhezeit_und_doppelsperre(client, app, uhr):
    client.p("/api/papier", {"text": "Versicherung", "frist": "2026-10-22", "naechster_schritt": "Police suchen", "erinnern_tage_vorher": 3})
    cfg = app.einst.alle() | {"ntfy_thema": "kompass-test", "push_routine": False}
    gesendet = []
    with app.app_context():
        v = dbm.db()
        uhr("2026-10-19 08:59")
        assert erinnern.durchlauf(v, cfg, sender=lambda *a: gesendet.append(a)) == []
        uhr("2026-10-19 09:00")
        assert erinnern.durchlauf(v, cfg, sender=lambda *a: gesendet.append(a)) == ["papier:1:2026-10-19"]
        assert erinnern.durchlauf(v, cfg, sender=lambda *a: gesendet.append(a)) == []  # nur einmal
        assert gesendet[0][2] == "Nächster Schritt: Police suchen"
        # Routine Abend 21:00 wuerde in die Ruhezeit ab 20:30 fallen
        uhr("2026-10-19 21:00")
        cfg["push_routine"] = True
        assert "routine:2:2026-10-19" not in erinnern.durchlauf(v, cfg | {"ruhe_von": "20:30"}, sender=lambda *a: None)
        assert "routine:2:2026-10-19" in erinnern.durchlauf(v, cfg, sender=lambda *a: None)


def test_fehlgeschlagener_push_wird_wiederholt(client, app, uhr):
    cfg = app.einst.alle() | {"ntfy_thema": "kompass-test"}
    uhr("2026-10-19 07:31")

    def kaputt(*a):
        raise OSError
    with app.app_context():
        v = dbm.db()
        assert erinnern.durchlauf(v, cfg, sender=kaputt) == []
        assert erinnern.durchlauf(v, cfg, sender=lambda *a: None) == ["routine:1:2026-10-19"]


def test_sommerzeitwechsel_routine():
    # 25.10.2026: Ende der Sommerzeit. 07:30 lokal ist dann 06:30 UTC statt 05:30.
    from datetime import date
    assert zeit.um(date(2026, 10, 24), "07:30").hour == 5
    assert zeit.um(date(2026, 10, 25), "07:30").hour == 6


# ---- Kalender --------------------------------------------------------------------------

ICS = Path(__file__).with_name("beispiel.ics").read_bytes()


def test_kalender_wiederholung_und_ganztags(uhr):
    from datetime import datetime, timedelta
    von = datetime(2026, 10, 19, tzinfo=zeit.ZONE)
    t = kalender.termine_aus_ics(ICS, von, von + timedelta(days=14))
    titel = [x["titel"] for x in t]
    assert titel.count("Training") == 2  # woechentlich, Montag
    assert "Abgesagt" not in titel
    ganz = next(x for x in t if x["titel"] == "Ferien")
    assert ganz["ganztags"] == 1 and ganz["beginn"] == "2026-10-20" and ganz["ende"] == "2026-10-23"
    tr = next(x for x in t if x["titel"] == "Training")
    assert tr["beginn"] == "2026-10-19T16:30:00+00:00"  # 18:30 Zuerich, Sommerzeit


def test_naechster_termin_und_push(client, app, uhr, monkeypatch):
    monkeypatch.setattr(kalender, "holen", lambda url, timeout=20: ICS)
    uhr("2026-10-19 18:10")
    with app.app_context():
        v = dbm.db()
        kalender.abgleichen(v, "https://beispiel.invalid/x.ics")
        d = auswahl.naechste_termine(v)
        assert d["naechster"]["titel"] == "Training" and d["ganztags"] == []
        cfg = app.einst.alle() | {"ntfy_thema": "kompass-test"}
        assert erinnern.durchlauf(v, cfg, sender=lambda *a: None) == []  # 20 min vorher: noch nicht
        uhr("2026-10-19 18:16")
        assert erinnern.durchlauf(v, cfg, sender=lambda *a: None) == ["termin:training@kompass:2026-10-19T16:30:00+00:00"]
        uhr("2026-10-20 10:00")
        assert auswahl.naechste_termine(v)["ganztags"] == ["Ferien"]


def test_einstellungen_geheimnisse_maskiert(client):
    r = client.p("/api/einstellungen", {"claude_schluessel": "sk-ant-geheim-1234", "ruhe_von": "22:00"}).get_json()
    assert r["einstellungen"]["claude_schluessel"] == "…1234" and r["einstellungen"]["ruhe_von"] == "22:00"
    assert "geheim" not in json.dumps(client.get("/api/einstellungen").get_json())
    assert client.p("/api/einstellungen", {"ical_url": "ftp://x"}).status_code == 400
    assert client.p("/api/einstellungen", {"ruhe_von": "25:00"}).status_code == 400
    # leer = unveraendert, 'loeschen' = leeren
    assert client.p("/api/einstellungen", {"claude_schluessel": ""}).get_json()["einstellungen"]["claude_schluessel"] == "…1234"
    assert client.p("/api/einstellungen", {"claude_schluessel": "loeschen"}).get_json()["einstellungen"]["claude_schluessel"] == ""


def test_sicherung(app, tmp_path):
    from kompass import sichern
    ziel = sichern(app.config["DB_PFAD"], tmp_path / "sicherung", behalten=2)
    assert ziel.exists() and oct(ziel.stat().st_mode)[-3:] == "600"
