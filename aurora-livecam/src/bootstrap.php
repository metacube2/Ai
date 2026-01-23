<?php
/**
 * Bootstrap - Initialisiert die Multi-Tenant Umgebung
 *
 * Einbinden am Anfang von index.php:
 *   require_once __DIR__ . '/src/bootstrap.php';
 */

// Autoloader für src/ Klassen
spl_autoload_register(function ($class) {
    // Namespace-Präfix
    $prefix = 'AuroraLivecam\\';
    $baseDir = __DIR__ . '/';

    // Prüfe ob die Klasse unseren Namespace verwendet
    $len = strlen($prefix);
    if (strncmp($prefix, $class, $len) !== 0) {
        return;
    }

    // Relativer Klassenname
    $relativeClass = substr($class, $len);

    // Pfad zur Datei
    $file = $baseDir . str_replace('\\', '/', $relativeClass) . '.php';

    if (file_exists($file)) {
        require $file;
    }
});

use AuroraLivecam\Core\TenantResolver;
use AuroraLivecam\Core\Database;

/**
 * Gibt die Site-Konfiguration basierend auf dem aktuellen Tenant zurück
 * Ersetzt den hardcoded Domain-Switch in index.php
 */
function getSiteConfig(): array
{
    // Legacy SettingsManager laden
    $settingsFile = dirname(__DIR__) . '/SettingsManager.php';
    if (!class_exists('SettingsManager') && file_exists($settingsFile)) {
        require_once $settingsFile;
    }

    $settingsManager = new \SettingsManager();

    // Wenn Multi-Tenant nicht aktiviert, nutze Legacy-Modus
    if (!$settingsManager->isMultiTenantEnabled()) {
        return getLegacySiteConfig();
    }

    // Multi-Tenant Modus
    try {
        $resolver = TenantResolver::getInstance();
        $tenant = $resolver->resolve();
        $branding = $resolver->getBranding();

        if (!$tenant) {
            return getLegacySiteConfig();
        }

        return [
            'tenant_id' => $tenant['id'],
            'tenant_slug' => $tenant['slug'],
            'is_multi_tenant' => true,
            'site_name' => $branding['site_name'] ?? $tenant['name'],
            'site_name_full' => $branding['site_name_full'] ?? $tenant['name'],
            'tagline' => $branding['tagline'] ?? '',
            'logo_path' => $branding['logo_path'] ?? null,
            'favicon_path' => $branding['favicon_path'] ?? null,
            'primary_color' => $branding['primary_color'] ?? '#667eea',
            'secondary_color' => $branding['secondary_color'] ?? '#764ba2',
            'accent_color' => $branding['accent_color'] ?? '#f093fb',
            'welcome_de' => $branding['welcome_text_de'] ?? '',
            'welcome_en' => $branding['welcome_text_en'] ?? '',
            'footer_text' => $branding['footer_text'] ?? '',
            'custom_css' => $branding['custom_css'] ?? '',
            'social' => [
                'facebook' => $branding['social_facebook'] ?? '',
                'instagram' => $branding['social_instagram'] ?? '',
                'youtube' => $branding['social_youtube'] ?? '',
            ],
        ];

    } catch (\Exception $e) {
        // Fallback auf Legacy bei Fehlern
        return getLegacySiteConfig();
    }
}

/**
 * Legacy Site-Konfiguration (hardcoded Domains)
 * Kompatibilität mit bestehendem Code
 */
function getLegacySiteConfig(): array
{
    $host = $_SERVER['HTTP_HOST'] ?? 'localhost';
    $isSeecam = (stripos($host, 'seecam.ch') !== false);

    if ($isSeecam) {
        return [
            'tenant_id' => 0,
            'tenant_slug' => 'seecam',
            'is_multi_tenant' => false,
            'site_name' => 'Seecam',
            'site_name_full' => 'Seecam.ch - Live Webcam am See',
            'tagline' => 'Ihre Live-Webcam am See',
            'logo_path' => null,
            'favicon_path' => null,
            'primary_color' => '#667eea',
            'secondary_color' => '#764ba2',
            'accent_color' => '#f093fb',
            'welcome_de' => 'Willkommen bei Seecam - Ihrer Live-Webcam am See!',
            'welcome_en' => 'Welcome to Seecam - Your Live Webcam at the Lake!',
            'footer_text' => '',
            'custom_css' => '',
            'social' => [
                'facebook' => '',
                'instagram' => '',
                'youtube' => '',
            ],
        ];
    }

    // Default: Aurora
    return [
        'tenant_id' => 0,
        'tenant_slug' => 'aurora',
        'is_multi_tenant' => false,
        'site_name' => 'Aurora',
        'site_name_full' => 'Aurora Weather Livecam - Zürich Oberland',
        'tagline' => 'Wetter Webcam Schweiz - Zürich Oberland',
        'logo_path' => null,
        'favicon_path' => null,
        'primary_color' => '#667eea',
        'secondary_color' => '#764ba2',
        'accent_color' => '#f093fb',
        'welcome_de' => 'Willkommen bei Aurora Weather Livecam - Ihre Wetter-Webcam im Zürcher Oberland mit AI-Erkennung für Aurora, Starlink und mehr!',
        'welcome_en' => 'Welcome to Aurora Weather Livecam - Your weather webcam in the Zurich Oberland with AI detection for Aurora, Starlink and more!',
        'footer_text' => '',
        'custom_css' => '',
        'social' => [
            'facebook' => '',
            'instagram' => '',
            'youtube' => '',
        ],
    ];
}

/**
 * Redirect Handler für alte Domains
 */
function handleDomainRedirects(): void
{
    $host = $_SERVER['HTTP_HOST'] ?? '';

    // Alte Aurora-Domains auf neue Domain umleiten
    $oldDomains = [
        'www.aurora-wetter-lifecam.ch',
        'aurora-wetter-lifecam.ch',
        'www.aurora-wetter-livecam.ch',
        'aurora-wetter-livecam.ch'
    ];

    $newDomain = 'www.aurora-weather-livecam.com';

    if (in_array($host, $oldDomains)) {
        $protocol = (!empty($_SERVER['HTTPS']) && $_SERVER['HTTPS'] !== 'off') ? 'https' : 'http';
        $requestUri = $_SERVER['REQUEST_URI'] ?? '/';
        header("HTTP/1.1 301 Moved Permanently");
        header("Location: {$protocol}://{$newDomain}{$requestUri}");
        exit;
    }
}

// Domain-Redirects automatisch ausführen
handleDomainRedirects();
