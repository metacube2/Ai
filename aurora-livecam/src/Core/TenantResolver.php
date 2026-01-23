<?php
/**
 * TenantResolver - Ermittelt den aktuellen Tenant basierend auf Domain
 *
 * Ersetzt den hardcoded Domain-Switch in index.php
 */

namespace AuroraLivecam\Core;

class TenantResolver
{
    private Database $db;
    private ?array $currentTenant = null;
    private ?array $currentBranding = null;
    private static ?TenantResolver $instance = null;

    // Cache für Domain-Lookups (vermeidet DB-Anfragen bei jedem Request)
    private static array $domainCache = [];

    public function __construct(?Database $db = null)
    {
        $this->db = $db ?? Database::getInstance();
    }

    /**
     * Singleton für globalen Zugriff
     */
    public static function getInstance(): TenantResolver
    {
        if (self::$instance === null) {
            self::$instance = new self();
        }
        return self::$instance;
    }

    /**
     * Löst die aktuelle Domain auf und gibt den Tenant zurück
     */
    public function resolve(?string $domain = null): ?array
    {
        $domain = $domain ?? $this->getCurrentDomain();

        if ($this->currentTenant !== null && ($this->currentTenant['domain'] ?? '') === $domain) {
            return $this->currentTenant;
        }

        // Cache prüfen
        if (isset(self::$domainCache[$domain])) {
            $this->currentTenant = self::$domainCache[$domain];
            return $this->currentTenant;
        }

        // Aus DB laden
        $this->currentTenant = $this->loadTenantByDomain($domain);

        // In Cache speichern
        self::$domainCache[$domain] = $this->currentTenant;

        return $this->currentTenant;
    }

    /**
     * Lädt einen Tenant anhand der Domain aus der Datenbank
     */
    private function loadTenantByDomain(string $domain): ?array
    {
        // Normalisiere Domain (ohne www.)
        $normalizedDomain = $this->normalizeDomain($domain);

        try {
            $sql = "
                SELECT
                    t.*,
                    td.domain,
                    td.is_primary,
                    p.name as plan_name,
                    p.slug as plan_slug,
                    p.features as plan_features
                FROM tenant_domains td
                JOIN tenants t ON td.tenant_id = t.id
                LEFT JOIN plans p ON t.plan_id = p.id
                WHERE td.domain = ? OR td.domain = ?
                LIMIT 1
            ";

            $tenant = $this->db->fetchOne($sql, [$domain, $normalizedDomain]);

            if ($tenant && isset($tenant['plan_features'])) {
                $tenant['plan_features'] = json_decode($tenant['plan_features'], true);
            }

            return $tenant;
        } catch (\Exception $e) {
            // Fallback: Keine DB-Verbindung oder Tabelle existiert nicht
            return $this->getFallbackTenant($domain);
        }
    }

    /**
     * Fallback für Legacy-Modus (ohne Datenbank)
     * Unterstützt die alten hardcoded Domains
     */
    private function getFallbackTenant(string $domain): ?array
    {
        $normalizedDomain = $this->normalizeDomain($domain);

        // Alte seecam.ch Konfiguration
        if (str_contains($normalizedDomain, 'seecam.ch')) {
            return [
                'id' => 0,
                'uuid' => 'legacy-seecam',
                'name' => 'Seecam',
                'slug' => 'seecam',
                'status' => 'active',
                'domain' => $domain,
                'is_legacy' => true,
                'branding' => [
                    'site_name' => 'Seecam',
                    'site_name_full' => 'Seecam.ch - Live Webcam',
                    'tagline' => 'Ihre Live-Webcam',
                    'primary_color' => '#667eea',
                    'secondary_color' => '#764ba2',
                ],
            ];
        }

        // Default: Aurora
        if (str_contains($normalizedDomain, 'aurora') ||
            str_contains($normalizedDomain, 'localhost') ||
            $normalizedDomain === '127.0.0.1') {
            return [
                'id' => 0,
                'uuid' => 'legacy-aurora',
                'name' => 'Aurora Weather Livecam',
                'slug' => 'aurora',
                'status' => 'active',
                'domain' => $domain,
                'is_legacy' => true,
                'branding' => [
                    'site_name' => 'Aurora',
                    'site_name_full' => 'Aurora Weather Livecam - Zürich Oberland',
                    'tagline' => 'Wetter Webcam Schweiz',
                    'primary_color' => '#667eea',
                    'secondary_color' => '#764ba2',
                ],
            ];
        }

        // Unbekannte Domain - Default Tenant
        return [
            'id' => 0,
            'uuid' => 'default',
            'name' => 'Livecam',
            'slug' => 'default',
            'status' => 'active',
            'domain' => $domain,
            'is_legacy' => true,
            'branding' => [
                'site_name' => 'Livecam',
                'site_name_full' => 'Livecam',
                'primary_color' => '#667eea',
                'secondary_color' => '#764ba2',
            ],
        ];
    }

    /**
     * Gibt das Branding des aktuellen Tenants zurück
     */
    public function getBranding(): array
    {
        if ($this->currentBranding !== null) {
            return $this->currentBranding;
        }

        $tenant = $this->resolve();

        if (!$tenant) {
            return $this->getDefaultBranding();
        }

        // Legacy-Tenant hat Branding inline
        if (isset($tenant['is_legacy']) && $tenant['is_legacy']) {
            $this->currentBranding = $tenant['branding'] ?? $this->getDefaultBranding();
            return $this->currentBranding;
        }

        // Aus DB laden
        try {
            $branding = $this->db->fetchOne(
                "SELECT * FROM tenant_branding WHERE tenant_id = ?",
                [$tenant['id']]
            );

            $this->currentBranding = $branding ?: $this->getDefaultBranding();
        } catch (\Exception $e) {
            $this->currentBranding = $this->getDefaultBranding();
        }

        return $this->currentBranding;
    }

    /**
     * Default Branding
     */
    private function getDefaultBranding(): array
    {
        return [
            'site_name' => 'Livecam',
            'site_name_full' => 'Live Webcam',
            'tagline' => '',
            'logo_path' => null,
            'favicon_path' => null,
            'primary_color' => '#667eea',
            'secondary_color' => '#764ba2',
            'accent_color' => '#f093fb',
            'welcome_text_de' => '',
            'welcome_text_en' => '',
            'footer_text' => '',
            'custom_css' => '',
        ];
    }

    /**
     * Gibt die aktuelle Domain zurück
     */
    public function getCurrentDomain(): string
    {
        return $_SERVER['HTTP_HOST'] ?? 'localhost';
    }

    /**
     * Normalisiert eine Domain (entfernt www.)
     */
    private function normalizeDomain(string $domain): string
    {
        return preg_replace('/^www\./i', '', strtolower($domain));
    }

    /**
     * Prüft ob der aktuelle Tenant aktiv ist
     */
    public function isActive(): bool
    {
        $tenant = $this->resolve();
        return $tenant && in_array($tenant['status'], ['active', 'trial']);
    }

    /**
     * Prüft ob der Tenant im Trial ist
     */
    public function isTrial(): bool
    {
        $tenant = $this->resolve();
        return $tenant && $tenant['status'] === 'trial';
    }

    /**
     * Gibt die Tenant-ID zurück (oder 0 für Legacy)
     */
    public function getTenantId(): int
    {
        $tenant = $this->resolve();
        return $tenant['id'] ?? 0;
    }

    /**
     * Gibt den Tenant-Slug zurück
     */
    public function getTenantSlug(): string
    {
        $tenant = $this->resolve();
        return $tenant['slug'] ?? 'default';
    }

    /**
     * Prüft ob Multi-Tenant-Modus aktiv ist (DB vorhanden)
     */
    public function isMultiTenantEnabled(): bool
    {
        $tenant = $this->resolve();
        return $tenant && !isset($tenant['is_legacy']);
    }

    /**
     * Gibt alle Domains eines Tenants zurück
     */
    public function getTenantDomains(int $tenantId): array
    {
        try {
            return $this->db->fetchAll(
                "SELECT * FROM tenant_domains WHERE tenant_id = ? ORDER BY is_primary DESC",
                [$tenantId]
            );
        } catch (\Exception $e) {
            return [];
        }
    }

    /**
     * Setzt den aktuellen Tenant manuell (für Tests oder CLI)
     */
    public function setTenant(array $tenant): void
    {
        $this->currentTenant = $tenant;
        $this->currentBranding = null;
    }

    /**
     * Leert den Cache
     */
    public static function clearCache(): void
    {
        self::$domainCache = [];
    }
}
