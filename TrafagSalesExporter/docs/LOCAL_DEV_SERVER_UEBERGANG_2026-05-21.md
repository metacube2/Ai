# Lokaler Uebergangsserver

Stand: 2026-05-21

Diese Doku beschreibt, wie die Trafag-Cockpit-App voruebergehend auf dem Entwicklungs-PC bereitgestellt werden kann, solange der zentrale IIS-Server noch nicht korrekt erreichbar ist.

## Zweck

Der zentrale IIS-Server ist aktuell nicht ueber HTTPS erreichbar, weil der TLS-Handshake ein Client-Zertifikat fordert. Bis Marco/IT die IIS-SSL-Einstellungen korrigiert hat, kann die App temporaer lokal auf dem Entwicklungs-PC laufen.

## Aktueller lokaler Zugriff

Aktuelle IP im Trafag-WLAN/Firmennetz:

```text
172.16.9.185
```

URL fuer andere Mitarbeitende:

```text
http://172.16.9.185:5000
```

Hinweis: Die IP kann sich nach Neustart, WLAN-Wechsel, VPN-Wechsel oder DHCP-Erneuerung aendern. Neue IP mit `ipconfig` pruefen.

## Start der App

Im Projektordner:

```powershell
cd C:\Users\koi\source\repos\Ai\TrafagSalesExporter
dotnet run
```

Das Entwicklungsprofil wurde in `Properties/launchSettings.json` erweitert:

```json
"applicationUrl": "https://localhost:55415;http://localhost:55416;http://0.0.0.0:5000"
```

Damit bleiben die lokalen Entwicklungs-URLs aktiv:

```text
https://localhost:55415
http://localhost:55416
```

Zusaetzlich lauscht die App fuer andere PCs auf:

```text
http://0.0.0.0:5000
```

Falls eine alte Visual-Studio-Instanz bereits laeuft, App stoppen und neu starten, damit die neue Portbindung aktiv wird.

## Firewall

Am 2026-05-21 wurde in einer Admin-CMD eine allgemeine Port-5000-Regel angelegt und auf alle Profile erweitert.

Aktueller Regelstand:

```text
Regelname: Local Dev Web Port 5000
Aktiviert: Ja
Richtung: Eingehend
Profile: Domaene, Privat, Oeffentlich
Protokoll: TCP
Lokaler Port: 5000
Remote-IP: Beliebig
Aktion: Zulassen
```

Pruefen:

```cmd
netsh advfirewall firewall show rule name="Local Dev Web Port 5000"
```

Falls die Regel neu angelegt werden muss:

```cmd
netsh advfirewall firewall add rule name="Local Dev Web Port 5000" dir=in action=allow protocol=TCP localport=5000 profile=any
```

Falls eine bestehende Regel auf alle Profile erweitert werden muss:

```cmd
netsh advfirewall firewall set rule name="Local Dev Web Port 5000" new profile=any
```

Regel spaeter entfernen:

```cmd
netsh advfirewall firewall delete rule name="Local Dev Web Port 5000"
```

Die Firewall-Regel bleibt nach Neustart und normalerweise auch nach Windows-Updates aktiv. Die App selbst startet nach einem Neustart nicht automatisch.

## HTTP / HTTPS

Die Freigabe fuer andere PCs erfolgt bewusst ueber HTTP:

```text
http://<PC-IP>:5000
```

Lokales HTTPS ueber `https://localhost:55415` funktioniert nur auf dem Entwicklungs-PC. Fuer andere PCs waere HTTPS mit Zertifikat und Trust-Aufwand verbunden. Fuer den temporaeren internen Betrieb reicht HTTP.

## VPN-Hinweis

Wenn der Entwicklungs-PC zuhause per AlwaysOnVPN verbunden ist, kann der Zugriff aus dem Buero auf die VPN-IP funktionieren, ist aber nicht garantiert.

Wenn `http://<VPN-IP>:5000` nicht erreichbar ist und die lokale Firewall-Regel aktiv ist, liegt es wahrscheinlich am AlwaysOnVPN-/Firmennetz-Routing. Das kann lokal auf dem PC nicht sicher freigeschaltet werden.

## Sicherheit

- Nur im Trafag-Firmennetz bzw. ueber interne VPN-Szenarien verwenden.
- Nicht oeffentlich freigeben.
- Der PC muss eingeschaltet bleiben.
- Das PowerShell-/Visual-Studio-Fenster mit der laufenden App muss offen bleiben.
- Finance Cockpit und HR KPI bleiben ueber ihre App-internen Logins geschuetzt.

## Zentraler IIS-Server

Aktueller Befund:

```text
TLS RequestedClientCert=True
SEC_E_NO_CREDENTIALS
```

Das bedeutet: Der Server fordert beim HTTPS/TLS-Handshake ein Client-Zertifikat. Dadurch erreichen Requests weder `diag.txt` noch `BiDashboard.dll`.

Marco/IT soll in IIS bei der Website bzw. Application die SSL Settings pruefen:

```text
Client certificates: Ignore
```

oder hoechstens:

```text
Client certificates: Accept
```

Nicht:

```text
Client certificates: Require
```

Danach testen:

```text
https://trch-webapp-bidashboard.trafagch.local/BiDashboard/diag.txt
https://trch-webapp-bidashboard.trafagch.local/BiDashboard/
```
