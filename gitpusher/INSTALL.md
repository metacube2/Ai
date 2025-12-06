# Schnellstart-Installation

Schritt-für-Schritt Anleitung zur Installation von GitHub Sync auf deinem Ubuntu LXC Container.

## ⚡ Express-Installation (5 Minuten)

### 1. System vorbereiten

```bash
# System aktualisieren
sudo apt update && sudo apt upgrade -y

# Benötigte Pakete installieren
sudo apt install -y apache2 php libapache2-mod-php php-cli php-json php-mbstring git
```

### 2. Apache Module aktivieren

```bash
sudo a2enmod rewrite
sudo systemctl restart apache2
```

### 3. Virtual Host konfigurieren

```bash
# Virtual Host Datei erstellen
sudo nano /etc/apache2/sites-available/github-sync.conf
```

Kopiere diese Konfiguration:

```apache
<VirtualHost *:80>
    ServerName github-sync.local
    DocumentRoot /gitpusher/public

    <Directory /gitpusher/public>
        Options -Indexes +FollowSymLinks
        AllowOverride All
        Require all granted
    </Directory>

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

```bash
# Site aktivieren
sudo a2ensite github-sync.conf
sudo a2dissite 000-default.conf  # Optional: Default-Site deaktivieren
sudo systemctl reload apache2
```

### 4. Berechtigungen setzen

```bash
# Eigentümer ändern
sudo chown -R www-data:www-data /gitpusher

# Berechtigungen setzen
sudo chmod 755 /gitpusher
sudo chmod 755 /gitpusher/public
sudo chmod 755 /gitpusher/data
sudo chmod 755 /gitpusher/src
sudo chmod 600 /gitpusher/data/*.json
```

### 5. GitHub Personal Access Token erstellen

1. Gehe zu: https://github.com/settings/tokens
2. Klicke: **"Generate new token (classic)"**
3. Name: `GitHub Sync Server`
4. Scope: ✅ **repo** (Full control of private repositories)
5. Klicke: **"Generate token"**
6. **Kopiere den Token** (ghp_...)

### 6. Token hinterlegen

```bash
sudo nano /gitpusher/data/secrets.json
```

Ersetze die leere Zeile:

```json
{
  "github_pat": "ghp_DEIN_TOKEN_HIER",
  "webhook_secrets": {}
}
```

Speichern: `Ctrl+O` → `Enter` → `Ctrl+X`

### 7. Testen

```bash
# Apache Status prüfen
sudo systemctl status apache2

# PHP testen
php -v

# Git testen
git --version
```

### 8. Im Browser öffnen

Öffne in deinem Browser:
- Wenn du eine Domain hast: `http://github-sync.deine-domain.de`
- Sonst mit IP: `http://DEINE-SERVER-IP`

**Du solltest jetzt das Dashboard sehen!** 🎉

## 🎯 Erstes Repository hinzufügen

1. Klicke im Dashboard auf **"+ Repository hinzufügen"**
2. Fülle aus:
   ```
   Name: Test-Projekt
   Repository URL: https://github.com/dein-username/dein-repo.git
   Branch: main
   Ziel-Pfad: /var/www/test-projekt
   Auto-Sync: ✅ Aktiviert
   ```
3. Klicke **"Repository hinzufügen"**

Das Repository wird automatisch geklont!

## 🔗 GitHub Webhook einrichten

Nach dem Hinzufügen siehst du ein Modal mit Webhook-Informationen.

1. Kopiere **Payload URL** und **Secret**
2. Gehe zu deinem GitHub Repo → **Settings** → **Webhooks** → **Add webhook**
3. Füge ein:
   - **Payload URL**: (kopiert)
   - **Content type**: `application/json`
   - **Secret**: (kopiert)
   - **Events**: "Just the push event"
4. Klicke **"Add webhook"**

Fertig! Bei jedem Push wird automatisch synchronisiert.

## ✅ Erfolgs-Check

Teste die Synchronisation:

1. Ändere eine Datei in deinem GitHub-Repo
2. Committe und pushe die Änderung
3. Schau im Dashboard → Log-Einträge
4. Du solltest sehen: "✅ Sync OK (X Dateien)"

Prüfe die Datei auf dem Server:
```bash
ls -la /var/www/test-projekt
```

## 🔧 Erweiterte Konfiguration

### SSL/HTTPS einrichten (empfohlen für Produktion)

```bash
# Let's Encrypt installieren
sudo apt install certbot python3-certbot-apache

# Zertifikat erstellen
sudo certbot --apache -d github-sync.deine-domain.de

# Auto-Renewal testen
sudo certbot renew --dry-run
```

### Firewall konfigurieren

```bash
# UFW Firewall aktivieren
sudo ufw allow 'Apache Full'
sudo ufw enable
```

### Log-Rotation einrichten

```bash
sudo nano /etc/logrotate.d/github-sync
```

```
/var/log/apache2/github-sync-*.log {
    daily
    missingok
    rotate 14
    compress
    delaycompress
    notifempty
    create 0640 root adm
    sharedscripts
    postrotate
        systemctl reload apache2 > /dev/null
    endscript
}
```

## 🚨 Häufige Probleme

### Problem: "403 Forbidden" beim Öffnen

**Lösung:**
```bash
sudo chown -R www-data:www-data /gitpusher
sudo chmod 755 /gitpusher/public
```

### Problem: Repository kann nicht geklont werden

**Lösung:**
```bash
# Prüfe, ob www-data git nutzen kann
sudo -u www-data git --version

# Prüfe, ob Ziel-Ordner Schreibrechte hat
sudo -u www-data mkdir -p /var/www/test
```

### Problem: Webhook kommt nicht an

**Lösung:**
1. Prüfe GitHub Webhook Deliveries auf Fehler
2. Prüfe Firewall: `sudo ufw status`
3. Prüfe Apache Logs:
   ```bash
   sudo tail -f /var/log/apache2/github-sync-error.log
   ```

### Problem: JSON-Dateien leer oder defekt

**Lösung:**
```bash
# Setze Standardwerte zurück
cd /gitpusher/data

echo '{"repositories":[]}' | sudo tee config.json
echo '{"entries":[]}' | sudo tee log.json
echo '{"github_pat":"","webhook_secrets":{}}' | sudo tee secrets.json

sudo chmod 600 *.json
sudo chown www-data:www-data *.json
```

## 📋 Checkliste

- [ ] Apache installiert und läuft
- [ ] PHP installiert (Version 7.4+)
- [ ] Git installiert
- [ ] Virtual Host konfiguriert
- [ ] Site aktiviert und Apache neu geladen
- [ ] Berechtigungen gesetzt (www-data)
- [ ] GitHub PAT erstellt und hinterlegt
- [ ] Dashboard im Browser erreichbar
- [ ] Erstes Repository hinzugefügt
- [ ] Webhook in GitHub eingerichtet
- [ ] Test-Push erfolgreich synchronisiert

## 🎓 Nächste Schritte

1. Lies die vollständige [README.md](README.md)
2. Füge weitere Repositories hinzu
3. Teste Rollback-Funktion
4. Richte SSL/HTTPS ein (für Produktion)
5. Konfiguriere Monitoring

---

**Viel Erfolg!** 🚀
