<?php
/**
 * FamilyAlbums - Konfiguration
 *
 * WICHTIG: Nach erster Installation Passwort ändern!
 * Neuen Hash generieren: php -r "echo password_hash('deinPasswort', PASSWORD_DEFAULT);"
 */

// Standard-Passwort: "familie2024" - BITTE ÄNDERN!
define('ADMIN_PASSWORD_HASH', '$2y$10$YxQx8B7GkDqNmPrC4VzKH.qN4tQ8WvX5kF7mZ3hJ9aE1bC2dR6uYO');

define('SITE_TITLE', 'Familien-Fotoalben');
define('DATA_PATH', __DIR__ . '/data/');
define('THUMBNAIL_PATH', __DIR__ . '/thumbnails/');
define('THUMBNAIL_URL', 'thumbnails/');

define('ALBUMS_FILE', DATA_PATH . 'albums.json');
define('COMMENTS_FILE', DATA_PATH . 'comments.json');

// Session-Einstellungen
define('SESSION_LIFETIME', 3600); // 1 Stunde

// Zeitzone
date_default_timezone_set('Europe/Zurich');

/**
 * JSON-Datei lesen
 */
function read_json(string $file): array {
    if (!file_exists($file)) {
        return [];
    }
    $content = file_get_contents($file);
    return json_decode($content, true) ?? [];
}

/**
 * JSON-Datei schreiben
 */
function write_json(string $file, array $data): bool {
    $dir = dirname($file);
    if (!is_dir($dir)) {
        mkdir($dir, 0770, true);
    }
    return file_put_contents($file, json_encode($data, JSON_PRETTY_PRINT | JSON_UNESCAPED_UNICODE)) !== false;
}

/**
 * UUID generieren
 */
function generate_uuid(): string {
    return sprintf('%04x%04x-%04x-%04x-%04x-%04x%04x%04x',
        mt_rand(0, 0xffff), mt_rand(0, 0xffff),
        mt_rand(0, 0xffff),
        mt_rand(0, 0x0fff) | 0x4000,
        mt_rand(0, 0x3fff) | 0x8000,
        mt_rand(0, 0xffff), mt_rand(0, 0xffff), mt_rand(0, 0xffff)
    );
}

/**
 * XSS-sichere Ausgabe
 */
function e(string $str): string {
    return htmlspecialchars($str, ENT_QUOTES, 'UTF-8');
}

/**
 * CSRF-Token generieren
 */
function csrf_token(): string {
    if (empty($_SESSION['csrf_token'])) {
        $_SESSION['csrf_token'] = bin2hex(random_bytes(32));
    }
    return $_SESSION['csrf_token'];
}

/**
 * CSRF-Token validieren
 */
function csrf_validate(string $token): bool {
    return isset($_SESSION['csrf_token']) && hash_equals($_SESSION['csrf_token'], $token);
}

// Initialisiere Daten-Dateien falls nicht vorhanden
if (!file_exists(ALBUMS_FILE)) {
    write_json(ALBUMS_FILE, ['albums' => []]);
}
if (!file_exists(COMMENTS_FILE)) {
    write_json(COMMENTS_FILE, ['comments' => []]);
}
