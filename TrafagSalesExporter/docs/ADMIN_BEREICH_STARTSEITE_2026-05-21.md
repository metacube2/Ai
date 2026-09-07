# Admin Bereich und Startseite, Stand 2026-05-21

## Admin Bereich

Der Menüpunkt `Admin Bereich` ist ein eigener Hauptmenüpunkt und liegt nicht unter `Finance`.

Wichtig:

- Der Admin Bereich darf nicht durch den Finance-Cockpit-Login blockiert werden.
- Route: `/admin/sessions`
- Schutz: eigener App-interner Admin-Login über `AdminAccess`
- Initialer Benutzer: `admin`
- Initiales Passwort: `TrafagAdmin2026!`
- Das Admin-Passwort ist unabhängig vom Finance-Cockpit-Passwort.
- Das Passwort kann direkt im Admin-Loginbereich geändert werden.

Technische Dateien:

- `Components/Pages/AdminSessions.razor`
- `Components/AdminAccessPanel.razor`
- `Services/AdminAccessService.cs`
- `Security/AdminAccessOptions.cs`
- `appsettings.json`

Korrektur 2026-05-21:

- `/admin/sessions` wurde aus der globalen Finance-Sperrliste in `Components/Routes.razor` entfernt.
- Dadurch erscheint im Admin Bereich nicht mehr zuerst der Text `Finance Cockpit ist geschützt. Bitte separat anmelden.`
- Der Admin Bereich bleibt trotzdem geschützt, aber mit dem separaten Admin-Passwort.

## Aktive Logins

Der Admin Bereich zeigt aktive HR-/Finance-App-Entsperrungen seit dem letzten App-Start.

Einschränkung:

- HR und Finance verwenden aktuell gemeinsame App-Logins.
- Die Anzeige zeigt deshalb den verwendeten Login-Namen, IP-Adresse und Session-Zeitpunkte.
- Sie beweist nicht zwingend die echte Windows-Person hinter dem Zugriff.

## Startseite

Die Startseite `/` ist bewusst neutral und verlangt keinen Finance-Login.

Aktueller Aufbau:

- weißer Hintergrund
- schwarzer animierter Manometer
- Trafag-Schriftzug im Manometer
- Willkommenstext in der gewählten Sprache
- optionales animiertes Strichmännchen mit Kittel unter dem Willkommenstext

Die Corporate-Schrift wurde an die Trafag-Webseite angenähert:

- Google Font `Open Sans`
- Fallbacks: `Helvetica Neue`, `Helvetica`, `Arial`, `sans-serif`

Technische Dateien:

- `Components/App.razor`
- `wwwroot/css/app.css`
- `Components/Pages/Dashboard.razor`
- `Services/UiTextService.cs`

## Schalter für Strichmännchen

Das Strichmännchen ist standardmäßig deaktiviert.

Aktivierung:

1. `Admin Bereich` öffnen.
2. Mit Admin-Passwort anmelden.
3. Schalter `Strichmännchen anzeigen` aktivieren.

Speicherung:

- Einstellung: `LandingPage.ShowWalkingLabFigure`
- Datei: `appsettings.json`
- Service: `LandingPageSettingsService`

Technische Dateien:

- `Security/LandingPageOptions.cs`
- `Services/LandingPageSettingsService.cs`
- `Program.cs`
- `Components/Pages/AdminSessions.razor`

## Lokaler Übergangsserver

Für Tests auf dem eigenen PC wurde Port `5000` vorbereitet.

Firewall-Regel:

```text
Name: Local Dev Web Port 5000
Richtung: Eingehend
Protokoll: TCP
Lokaler Port: 5000
Profile: Domäne, Privat, Öffentlich
Aktion: Zulassen
```

Startprofil:

- `Properties/launchSettings.json`
- enthält `http://0.0.0.0:5000`

Aufruf für andere Benutzer im Netzwerk/VPN:

```text
http://172.16.9.185:5000/
```

Hinweise:

- Die IP kann sich nach Neustart oder Netzwerkwechsel ändern.
- Die Firewall-Regel bleibt nach Neustart aktiv.
- Die Anwendung muss trotzdem auf dem PC laufen.
- Lokal ist das ohne Zertifikat `http`, nicht `https`.

## Serverproblem

Die Veröffentlichung auf den Server wurde technisch ausgeführt, aber HTTPS-Aufrufe werden vor der App durch IIS/TLS blockiert.

Beobachtung:

- IIS fordert ein Client-Zertifikat an.
- Der Fehler passiert vor der Blazor-App.
- Kopieren ins Root-Verzeichnis löst das nicht.

Marco/IT muss auf dem IIS für die Site bzw. Anwendung prüfen:

- SSL Settings
- Client certificates: `Ignore` oder höchstens `Accept`
- nicht `Require`

Erst danach ist sinnvoll zu prüfen, ob die veröffentlichte App normal erreichbar ist.
