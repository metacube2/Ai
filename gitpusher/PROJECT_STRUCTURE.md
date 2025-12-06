# Projektstruktur

## Übersicht

```
/gitpusher/
│
├── 📄 README.md                    # Hauptdokumentation
├── 📄 INSTALL.md                   # Schnellstart-Installation
├── 📄 PROJECT_STRUCTURE.md         # Diese Datei
├── 🔒 .htaccess                    # Hauptsicherheitskonfiguration
├── 📝 .gitignore                   # Git-Ignore-Regeln
│
├── 📁 public/                      # Web-Root (Apache DocumentRoot)
│   ├── 🌐 index.php               # Dashboard (Frontend)
│   ├── 🔗 webhook.php             # Webhook-Endpoint für GitHub
│   │
│   ├── 📁 api/                    # REST API Endpunkte
│   │   ├── repos.php              # Repository-CRUD-Operationen
│   │   ├── sync.php               # Manueller Sync-Trigger
│   │   ├── rollback.php           # Rollback zu älterem Commit
│   │   └── log.php                # Log-Abfrage
│   │
│   ├── 📁 css/
│   │   └── style.css              # Komplettes Dashboard-Styling
│   │
│   └── 📁 js/
│       └── app.js                 # Frontend-Logik & AJAX
│
├── 📁 data/                        # Datenspeicher (NICHT web-zugänglich!)
│   ├── 🔒 .htaccess               # Zugriff komplett verweigern
│   ├── 📊 config.json             # Repository-Konfigurationen
│   ├── 📜 log.json                # Alle Log-Einträge
│   └── 🔐 secrets.json            # GitHub PAT & Webhook Secrets
│
└── 📁 src/                         # PHP Backend-Klassen
    ├── 🔒 .htaccess               # Zugriff komplett verweigern
    ├── ConfigManager.php          # JSON-Dateiverwaltung
    ├── Logger.php                 # Logging-System
    └── GitHandler.php             # Git-Operationen (clone, pull, revert)
```

## 📂 Detaillierte Beschreibung

### `/public/` - Web-Root

**Apache DocumentRoot** - Einziger Ordner, der über HTTP erreichbar ist.

#### `index.php` - Dashboard
- Haupt-Frontend der Anwendung
- Zeigt alle Repositories mit Status
- Statistiken (Anzahl Repos, Sync-Status, etc.)
- Log-Anzeige der letzten Ereignisse
- Modals für: Repository hinzufügen, Rollback, Webhook-Info

#### `webhook.php` - GitHub Webhook Endpoint
- Empfängt POST-Requests von GitHub
- Verifiziert Webhook-Signatur (HMAC SHA-256)
- Prüft, ob Push zum konfigurierten Branch gehört
- Triggert automatischen `git pull`
- Loggt alle Ereignisse

#### `/api/` - REST API

##### `repos.php`
- **GET**: Liste aller Repositories oder einzelnes Repository
- **POST**: Neues Repository hinzufügen (+ initial clone)
- **PUT**: Repository-Einstellungen aktualisieren
- **DELETE**: Repository aus Config entfernen (optional: Dateien löschen)

##### `sync.php`
- **POST**: Manuellen Sync durchführen
- Führt `git pull` für angegebenes Repository aus
- Gibt Anzahl geänderter Dateien zurück

##### `rollback.php`
- **GET**: Liste der letzten Commits (für Rollback-Auswahl)
- **POST**: Rollback zu bestimmtem Commit durchführen
- Nutzt `git revert` (sicher, keine Commits gelöscht)

##### `log.php`
- **GET**: Log-Einträge abrufen
- Filter: Repository-ID, Log-Type, Limit, Offset
- Gibt Statistiken zurück (Erfolg/Fehler/Warnung)

#### `/css/style.css`
- Modernes, responsives Design
- CSS Custom Properties (CSS Variables)
- Mobile-First Ansatz
- Animationen für Toasts, Modals, Cards

#### `/js/app.js`
- Frontend-State-Management
- AJAX-Calls zu allen API-Endpunkten
- Modal-Handling
- Toast-Notifications
- Auto-Refresh (alle 30 Sekunden)

---

### `/data/` - Datenspeicher

**Sicherheit**: `.htaccess` verweigert jeden Web-Zugriff!

#### `config.json`
```json
{
  "repositories": [
    {
      "id": "repo_uniqueid123",
      "name": "Meine Website",
      "repo_url": "https://github.com/user/repo.git",
      "branch": "main",
      "target_path": "/var/www/website",
      "auto_sync": true,
      "status": "synced",
      "created_at": "2025-12-06 10:00:00",
      "last_sync": "2025-12-06 14:30:00",
      "last_commit": "abc123def456..."
    }
  ]
}
```

#### `log.json`
```json
{
  "entries": [
    {
      "id": "log_uniqueid456",
      "timestamp": "2025-12-06 14:30:00",
      "repo_id": "repo_uniqueid123",
      "type": "success",
      "message": "Pull completed successfully",
      "details": {
        "files_changed": 3,
        "old_commit": "abc123d",
        "new_commit": "def456a"
      }
    }
  ]
}
```

#### `secrets.json`
```json
{
  "github_pat": "ghp_YourPersonalAccessTokenHere",
  "webhook_secrets": {
    "repo_uniqueid123": "generatedWebhookSecretHere"
  }
}
```

**Berechtigungen**: `chmod 600` (nur Owner kann lesen/schreiben)

---

### `/src/` - Backend-Klassen

**Sicherheit**: `.htaccess` verweigert jeden Web-Zugriff!

#### `ConfigManager.php`
**Verantwortlichkeiten:**
- JSON-Dateien lesen/schreiben
- Repository-CRUD-Operationen
- Webhook-Secret-Verwaltung
- GitHub PAT verwalten

**Wichtige Methoden:**
```php
getRepositories()           // Alle Repos
getRepository($id)          // Einzelnes Repo
addRepository($data)        // Neues Repo
updateRepository($id, $updates)  // Repo aktualisieren
deleteRepository($id)       // Repo löschen
getGitHubToken()           // PAT abrufen
setWebhookSecret($id, $secret)  // Webhook Secret speichern
```

#### `Logger.php`
**Verantwortlichkeiten:**
- Log-Einträge erstellen
- Logs nach Typ/Repo filtern
- Statistiken generieren
- Auto-Bereinigung (max. 1000 Einträge)

**Wichtige Methoden:**
```php
success($repoId, $message, $details)  // ✅ Erfolg loggen
error($repoId, $message, $details)    // ❌ Fehler loggen
warning($repoId, $message, $details)  // ⚠️ Warnung loggen
info($repoId, $message, $details)     // ℹ️ Info loggen
getAll($limit, $offset)               // Alle Logs
getByRepository($repoId)              // Logs für Repo
getStats()                            // Statistiken
```

#### `GitHandler.php`
**Verantwortlichkeiten:**
- Git-Befehle ausführen
- Repository klonen
- Pull durchführen
- Revert zu älterem Commit
- Commit-Historie abrufen
- Merge-Konflikte erkennen

**Wichtige Methoden:**
```php
cloneRepository($repoId, $url, $path, $branch)  // Initial clone
pull($repoId, $path, $branch)                   // git pull
revert($repoId, $path, $commitHash)            // git revert
getCurrentCommit($path)                         // Aktueller Commit
getCommitHistory($path, $limit)                 // Commit-Liste
getStatus($path)                                // git status
getRemoteBranches($url)                         // Verfügbare Branches
```

**Sicherheit:**
- Alle Shell-Befehle mit `escapeshellarg()` escaped
- GitHub PAT wird in URL eingebettet für Auth
- Fehlerbehandlung mit Try-Catch

---

## 🔐 Sicherheitskonzept

### 1. Zugriffskontrolle

**Web-zugänglich**: Nur `/public/`

**Blockiert**:
- `/data/` (enthält Secrets & Konfiguration)
- `/src/` (PHP-Klassen)
- Alle `.json` Dateien
- `.git` Verzeichnisse

### 2. Webhook-Sicherheit

- HMAC SHA-256 Signatur-Verifizierung
- Unique Secret pro Repository
- Timing-Safe Vergleich (`hash_equals()`)

### 3. Datei-Berechtigungen

```bash
/gitpusher/                 755 (www-data:www-data)
/gitpusher/public/          755
/gitpusher/data/            755
/gitpusher/data/*.json      600 (nur Owner lesen/schreiben)
/gitpusher/src/             755
```

### 4. Input-Validierung

- Alle User-Inputs werden validiert
- JSON-Parsing mit Fehlerbehandlung
- Repository-URLs werden geprüft
- SQL-Injection nicht möglich (keine DB)
- XSS-Prevention durch `escapeHtml()` im Frontend

### 5. Git-Sicherheit

- Alle Git-Befehle laufen als `www-data` User
- Shell-Injection-Prevention durch `escapeshellarg()`
- GitHub PAT mit minimalen Berechtigungen (nur `repo`)

---

## 🔄 Datenfluss

### Automatischer Sync (Webhook)

```
GitHub Push Event
       ↓
   webhook.php
       ↓
1. Payload empfangen
2. JSON dekodieren
3. Signature verifizieren (HMAC SHA-256)
4. Repository in Config finden
5. Branch prüfen
       ↓
   GitHandler::pull()
       ↓
1. Aktuellen Commit speichern
2. git pull ausführen
3. Auf Merge-Konflikte prüfen
4. Geänderte Dateien zählen
       ↓
   Logger::success/error()
       ↓
   ConfigManager::updateRepository()
       ↓
Status aktualisiert in config.json
```

### Manueller Sync

```
User klickt "Sync"-Button
       ↓
JavaScript: syncRepository(repoId)
       ↓
AJAX POST → api/sync.php
       ↓
GitHandler::pull()
       ↓
Logger::log()
       ↓
Response → JavaScript
       ↓
Dashboard-Refresh
```

### Repository hinzufügen

```
User füllt Formular aus
       ↓
JavaScript: addRepository(event)
       ↓
AJAX POST → api/repos.php
       ↓
1. Input validieren
2. ConfigManager::addRepository()
3. Webhook Secret generieren
4. GitHandler::cloneRepository()
       ↓
5. Logger::success()
6. Webhook-Info zurückgeben
       ↓
Modal mit Webhook-Daten anzeigen
```

---

## 📊 Abhängigkeiten

### PHP-Klassen

```
webhook.php
├── ConfigManager
├── Logger
└── GitHandler
    └── Logger
    └── ConfigManager

api/repos.php
├── ConfigManager
├── Logger
└── GitHandler

api/sync.php
├── ConfigManager
├── Logger
└── GitHandler

api/rollback.php
├── ConfigManager
├── Logger
└── GitHandler

api/log.php
├── ConfigManager
└── Logger
```

### Frontend

```
index.php (HTML)
├── css/style.css
└── js/app.js
    ├── Fetch API (AJAX)
    └── REST API Endpoints
        ├── api/repos.php
        ├── api/sync.php
        ├── api/rollback.php
        └── api/log.php
```

---

## 🧪 Test-Checklist

- [ ] Repository hinzufügen funktioniert
- [ ] Initial Clone erfolgreich
- [ ] Webhook empfängt Push-Events
- [ ] Signatur-Verifizierung funktioniert
- [ ] Manueller Sync funktioniert
- [ ] Merge-Konflikte werden erkannt
- [ ] Rollback erstellt Revert-Commit
- [ ] Logs werden korrekt geschrieben
- [ ] Dashboard zeigt Status korrekt
- [ ] Repository löschen funktioniert
- [ ] .htaccess blockiert /data/ Zugriff
- [ ] .htaccess blockiert /src/ Zugriff

---

## 📚 Erweiterungsmöglichkeiten

### Mögliche Features

1. **Multi-User Support**
   - User-Login
   - Rollen-System (Admin, User)
   - Repository-Berechtigungen pro User

2. **E-Mail Benachrichtigungen**
   - Bei erfolgreicher Sync
   - Bei Fehlern/Konflikten
   - Tägliche Zusammenfassung

3. **Deployment Scripts**
   - Post-Sync Hooks (z.B. `npm install`, `composer install`)
   - Custom Shell-Befehle
   - Build-Prozesse

4. **Advanced Git Features**
   - Submodules Support
   - Tag/Release Tracking
   - Multi-Branch Sync

5. **Monitoring & Alerts**
   - Prometheus Metrics
   - Grafana Dashboard
   - Slack/Discord Webhooks

6. **API Authentication**
   - API Keys
   - JWT Tokens
   - Rate Limiting

7. **Backup System**
   - Automatische Backups vor Sync
   - Snapshot-Verwaltung
   - Restore-Funktion

---

**Version**: 1.0.0
**Erstellt**: 2025-12-06
**Autor**: Claude Code
