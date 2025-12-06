# GitHub Sync - Automatische Repository-Synchronisation

Eine einfache und sichere Lösung zur automatischen Synchronisation von GitHub-Repositories auf deinem Server mittels Webhooks.

## 📋 Features

- ✅ **Automatische Synchronisation** via GitHub Webhooks
- ✅ **Mehrere Repositories** gleichzeitig verwalten
- ✅ **Branch-Auswahl** pro Repository
- ✅ **Webhook-Sicherheit** mit Secret-Verifizierung
- ✅ **Manueller Sync** über das Dashboard
- ✅ **Rollback-Funktion** via `git revert`
- ✅ **Konflikt-Erkennung** mit Warnungen
- ✅ **Log-System** für alle Ereignisse
- ✅ **Datei-basiert** - keine Datenbank erforderlich
- ✅ **Responsives Dashboard** mit Echtzeit-Updates

## 🔧 Systemanforderungen

- **Server**: Ubuntu Server (LXC Container auf Proxmox)
- **Webserver**: Apache 2.4+
- **PHP**: 7.4+ (8.0+ empfohlen)
- **Git**: 2.0+
- **Berechtigungen**: Root-Zugriff für Installation

## 📦 Installation

### 1. Voraussetzungen prüfen

```bash
# PHP Version prüfen
php -v

# Git Version prüfen
git --version

# Apache Status prüfen
systemctl status apache2
```

### 2. Benötigte PHP-Module installieren

```bash
sudo apt update
sudo apt install php php-cli php-json php-mbstring
```

### 3. Apache-Konfiguration

#### Virtual Host erstellen

```bash
sudo nano /etc/apache2/sites-available/github-sync.conf
```

Füge folgende Konfiguration ein:

```apache
<VirtualHost *:80>
    ServerName github-sync.deine-domain.de
    DocumentRoot /gitpusher/public

    <Directory /gitpusher/public>
        Options -Indexes +FollowSymLinks
        AllowOverride All
        Require all granted
    </Directory>

    # Deny access to data and src directories
    <Directory /gitpusher/data>
        Require all denied
    </Directory>

    <Directory /gitpusher/src>
        Require all denied
    </Directory>

    ErrorLog ${APACHE_LOG_DIR}/github-sync-error.log
    CustomLog ${APACHE_LOG_DIR}/github-sync-access.log combined
</VirtualHost>
```

#### Site aktivieren

```bash
sudo a2ensite github-sync.conf
sudo a2enmod rewrite
sudo systemctl reload apache2
```

### 4. Berechtigungen setzen

```bash
# Eigentümer auf www-data setzen
sudo chown -R www-data:www-data /gitpusher

# Schreibrechte für data-Verzeichnis
sudo chmod 755 /gitpusher/data
sudo chmod 600 /gitpusher/data/*.json

# Ausführrechte für public-Verzeichnis
sudo chmod 755 /gitpusher/public
```

### 5. GitHub Personal Access Token erstellen

1. Gehe zu GitHub → Settings → Developer settings → Personal access tokens
2. Klicke auf "Generate new token (classic)"
3. Name: `GitHub Sync Server`
4. Wähle Scopes:
   - ✅ `repo` (Full control of private repositories)
5. Klicke auf "Generate token"
6. **Kopiere den Token sofort** - er wird nur einmal angezeigt!

### 6. Token in der Anwendung hinterlegen

Bearbeite `/gitpusher/data/secrets.json`:

```bash
sudo nano /gitpusher/data/secrets.json
```

Füge deinen GitHub PAT ein:

```json
{
  "github_pat": "ghp_deinTokenHier1234567890",
  "webhook_secrets": {}
}
```

Speichern mit `Ctrl+O`, beenden mit `Ctrl+X`.

## 🚀 Verwendung

### Dashboard öffnen

Öffne deinen Browser und navigiere zu:
```
http://github-sync.deine-domain.de
```

### Repository hinzufügen

1. Klicke auf **"+ Repository hinzufügen"**
2. Fülle das Formular aus:
   - **Name**: Ein aussagekräftiger Name (z.B. "Meine Website")
   - **GitHub Repository URL**: `https://github.com/user/repo.git`
   - **Branch**: z.B. `main` oder `master`
   - **Ziel-Pfad**: z.B. `/var/www/meine-website`
   - **Auto-Sync**: Aktiviert für automatische Webhooks
3. Klicke auf **"Repository hinzufügen"**

Die App klont das Repository automatisch und zeigt dir die Webhook-Konfiguration an.

### GitHub Webhook einrichten

1. Gehe zu deinem GitHub Repository → **Settings** → **Webhooks** → **Add webhook**
2. Füge die Informationen aus dem Modal ein:
   - **Payload URL**: (aus dem Modal kopieren)
   - **Content type**: `application/json`
   - **Secret**: (aus dem Modal kopieren)
   - **Events**: "Just the push event"
3. Klicke auf **"Add webhook"**

Ab jetzt wird bei jedem Push automatisch synchronisiert!

### Manueller Sync

Klicke auf den Button **"🔄 Manueller Sync"** bei einem Repository, um sofort zu synchronisieren.

### Rollback durchführen

1. Klicke auf **"⏪ Rollback"** bei einem Repository
2. Wähle den Commit aus, zu dem du zurückkehren möchtest
3. Bestätige die Aktion

**Wichtig**: Es wird ein neuer Revert-Commit erstellt, keine Commits werden gelöscht!

### Repository entfernen

1. Klicke auf **"🗑️ Entfernen"**
2. Wähle, ob auch die Dateien gelöscht werden sollen
3. Bestätige die Aktion

## 📁 Dateistruktur

```
/gitpusher/
├── public/                     # Web-Root (Apache DocumentRoot)
│   ├── index.php              # Dashboard
│   ├── webhook.php            # Webhook-Endpoint
│   ├── api/
│   │   ├── repos.php          # Repository-Verwaltung
│   │   ├── sync.php           # Manueller Sync
│   │   ├── rollback.php       # Rollback-Funktion
│   │   └── log.php            # Logs abrufen
│   ├── css/
│   │   └── style.css          # Styling
│   └── js/
│       └── app.js             # Frontend-Logik
│
├── data/                       # Daten (nicht web-zugänglich)
│   ├── config.json            # Repository-Konfiguration
│   ├── log.json               # Log-Einträge
│   ├── secrets.json           # GitHub PAT & Webhook Secrets
│   └── .htaccess              # Zugriff verweigern
│
├── src/                        # PHP-Klassen
│   ├── ConfigManager.php      # Konfigurationsverwaltung
│   ├── Logger.php             # Logging
│   ├── GitHandler.php         # Git-Operationen
│   └── .htaccess              # Zugriff verweigern
│
├── .htaccess                   # Hauptkonfiguration
└── README.md                   # Diese Datei
```

## 🔒 Sicherheit

### Webhook-Signatur-Verifizierung

Alle Webhooks werden mit HMAC SHA-256 signiert und verifiziert. Ohne gültiges Secret werden Requests abgelehnt.

### Datei-Berechtigungen

- `/gitpusher/data/`: Nur von PHP lesbar (600)
- `/gitpusher/src/`: Nicht web-zugänglich
- `.htaccess`: Schützt sensitive Verzeichnisse

### GitHub PAT

- Wird verschlüsselt in `secrets.json` gespeichert
- Nur `repo`-Scope erforderlich
- Kann jederzeit in GitHub widerrufen werden

## 🐛 Troubleshooting

### "Permission denied" beim Clonen

```bash
# Stelle sicher, dass www-data Schreibrechte hat
sudo chown -R www-data:www-data /var/www
sudo chmod 755 /var/www
```

### Webhook wird nicht empfangen

1. Prüfe GitHub Webhook Deliveries auf Fehler
2. Überprüfe Apache Error Log:
   ```bash
   sudo tail -f /var/log/apache2/github-sync-error.log
   ```
3. Teste Webhook manuell:
   ```bash
   curl -X POST http://github-sync.deine-domain.de/webhook.php \
        -H "Content-Type: application/json" \
        -d '{"repository":{"clone_url":"https://github.com/user/repo.git"}}'
   ```

### Merge-Konflikt

Bei Konflikten zeigt das Dashboard eine Warnung. Löse den Konflikt manuell:

```bash
cd /var/www/dein-repo
sudo -u www-data git status
# Konflikt manuell lösen
sudo -u www-data git add .
sudo -u www-data git commit -m "Konflikt gelöst"
```

### Logs prüfen

```bash
# PHP Error Log
sudo tail -f /var/log/apache2/error.log

# App Logs
cat /gitpusher/data/log.json | jq
```

## 📊 API-Endpunkte

### GET /api/repos.php
Listet alle Repositories auf

### POST /api/repos.php
Fügt neues Repository hinzu

### PUT /api/repos.php
Aktualisiert Repository

### DELETE /api/repos.php
Löscht Repository

### POST /api/sync.php
Führt manuellen Sync durch

### GET /api/rollback.php
Listet Commits für Rollback

### POST /api/rollback.php
Führt Rollback durch

### GET /api/log.php
Ruft Logs ab

### POST /webhook.php
Empfängt GitHub Webhooks

## 🔄 Updates

Um das System zu aktualisieren:

1. Backup erstellen:
   ```bash
   sudo cp -r /gitpusher/data /gitpusher/data.backup
   ```

2. Neue Dateien deployen

3. Berechtigungen prüfen:
   ```bash
   sudo chown -R www-data:www-data /gitpusher
   ```

## 📝 Lizenz

Dieses Projekt ist für den persönlichen und kommerziellen Gebrauch frei verfügbar.

## 🙋 Support

Bei Fragen oder Problemen:
1. Prüfe die Logs im Dashboard
2. Prüfe Apache Error Logs
3. Prüfe GitHub Webhook Delivery Logs

---

Erstellt mit ❤️ für einfache GitHub-Synchronisation
