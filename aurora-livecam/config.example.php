<?php
/**
 * Aurora Livecam - Konfigurationsdatei
 *
 * Kopiere diese Datei zu config.php und passe die Werte an.
 * WICHTIG: config.php niemals in Git committen!
 */

return [
    // Datenbank-Konfiguration
    'database' => [
        'host' => 'localhost',
        'port' => 3306,
        'database' => 'aurora_livecam',
        'username' => 'root',
        'password' => '',
        'charset' => 'utf8mb4',
    ],

    // Anwendungs-Einstellungen
    'app' => [
        'name' => 'Aurora Livecam',
        'url' => 'https://aurora-weather-livecam.com',
        'debug' => false,
        'timezone' => 'Europe/Zurich',
    ],

    // Multi-Tenant Einstellungen
    'tenant' => [
        'default_subdomain_suffix' => '.aurora-livecam.com',
        'allow_custom_domains' => true,
        'trial_days' => 14,
    ],

    // Stripe (für Billing)
    'stripe' => [
        'public_key' => '',
        'secret_key' => '',
        'webhook_secret' => '',
        'currency' => 'chf',
    ],

    // E-Mail Einstellungen (für Onboarding)
    'mail' => [
        'host' => 'smtp.example.com',
        'port' => 587,
        'username' => '',
        'password' => '',
        'from_address' => 'noreply@aurora-livecam.com',
        'from_name' => 'Aurora Livecam',
    ],

    // Sicherheit
    'security' => [
        'session_lifetime' => 7200, // 2 Stunden
        'remember_me_days' => 30,
        'password_min_length' => 8,
    ],
];
