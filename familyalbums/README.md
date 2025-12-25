# FamilyAlbums - Familien-Fotoalbum-Portal

Ein einfaches, PHP-basiertes Portal zur Verwaltung und Anzeige von Familien-Fotoalben mit Links zu Nextcloud.

## Features

- Öffentliche Galerie-Ansicht mit Jahr/Monat-Filter
- Stichwortsuche über Titel, Tags und Beschreibung
- Kommentarfunktion für Familienmitglieder
- Admin-Interface zur Albumverwaltung
- Responsive Design (Tailwind CSS)
- Flat-File Datenbank (JSON) - kein MySQL erforderlich
- Spam-Schutz (Honeypot + Rate-Limiting)
- CSRF-Schutz für Admin-Aktionen

## Installation

### 1. Dateien kopieren

```bash
# Auf den Webserver kopieren
sudo cp -r familyalbums /var/www/

# Berechtigungen setzen
sudo chown -R www-data:www-data /var/www/familyalbums
sudo chmod -R 755 /var/www/familyalbums
sudo chmod 770 /var/www/familyalbums/data
sudo chmod 770 /var/www/familyalbums/thumbnails
```

### 2. Admin-Passwort ändern

**WICHTIG:** Das Standard-Passwort muss vor dem produktiven Einsatz geändert werden!

```bash
# Neuen Passwort-Hash generieren
php -r "echo password_hash('DeinSicheresPasswort', PASSWORD_DEFAULT);"
```

Den generierten Hash in `config.php` eintragen:

```php
define('ADMIN_PASSWORD_HASH', '$2y$10$DEIN_GENERIERTER_HASH_HIER');
```

### 3. Apache Virtual Host (optional)

```apache
<VirtualHost *:80>
    ServerName familyalbums.example.com
    DocumentRoot /var/www/familyalbums

    <Directory /var/www/familyalbums>
        AllowOverride All
        Require all granted
    </Directory>
</VirtualHost>
```

## Verwendung

### Öffentliche Galerie

- URL: `https://deine-domain.ch/`
- Filter nach Jahr und Monat
- Stichwortsuche
- Kommentare zu Alben hinterlassen

### Admin-Bereich

- URL: `https://deine-domain.ch/admin.php`
- Login mit dem konfigurierten Passwort
- Alben hinzufügen, bearbeiten, löschen
- Optional: Vorschaubilder hochladen
- Kommentare moderieren

## Datenstruktur

### albums.json

```json
{
  "albums": [
    {
      "id": "uuid",
      "title": "Albumtitel",
      "url": "https://nextcloud.../apps/photos/public/...",
      "date": "2024-12-25",
      "tags": ["tag1", "tag2"],
      "description": "Beschreibung",
      "thumbnail": "thumbnails/bild.jpg",
      "created_at": "2024-12-26T10:00:00+01:00"
    }
  ]
}
```

### comments.json

```json
{
  "comments": [
    {
      "id": "uuid",
      "album_id": "album-uuid",
      "author": "Name",
      "text": "Kommentar",
      "created_at": "2024-12-27T14:30:00+01:00"
    }
  ]
}
```

## Sicherheit

- Admin-Passwort mit bcrypt gehasht
- CSRF-Token für alle Admin-Aktionen
- XSS-Schutz durch `htmlspecialchars()`
- Rate-Limiting für Kommentare (5/Minute pro IP)
- Honeypot-Feld gegen Spam-Bots
- `.htaccess` schützt config.php und data/

## Anforderungen

- PHP 8.0+
- Apache mit mod_rewrite (optional)
- Schreibrechte für data/ und thumbnails/

## Lizenz

Privates Projekt für Familien-Nutzung.
