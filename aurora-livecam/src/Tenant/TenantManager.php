<?php
/**
 * TenantManager - CRUD-Operationen für Tenants
 */

namespace AuroraLivecam\Tenant;

use AuroraLivecam\Core\Database;
use Exception;

class TenantManager
{
    private Database $db;

    public function __construct(?Database $db = null)
    {
        $this->db = $db ?? Database::getInstance();
    }

    /**
     * Erstellt einen neuen Tenant
     */
    public function create(array $data): int
    {
        $this->db->beginTransaction();

        try {
            // UUID generieren
            $uuid = $this->generateUuid();

            // Slug generieren falls nicht vorhanden
            $slug = $data['slug'] ?? $this->generateSlug($data['name']);

            // Tenant erstellen
            $tenantId = $this->db->insert('tenants', [
                'uuid' => $uuid,
                'name' => $data['name'],
                'slug' => $slug,
                'email' => $data['email'],
                'status' => $data['status'] ?? 'trial',
                'plan_id' => $data['plan_id'] ?? $this->getDefaultPlanId(),
                'trial_ends_at' => $data['trial_ends_at'] ?? $this->calculateTrialEnd(),
            ]);

            // Domain hinzufügen
            if (!empty($data['domain'])) {
                $this->addDomain($tenantId, $data['domain'], true);
            }

            // Default-Subdomain erstellen
            if (!empty($data['subdomain'])) {
                $subdomain = $data['subdomain'] . '.aurora-livecam.com';
                $this->addDomain($tenantId, $subdomain, empty($data['domain']));
            }

            // Branding mit Defaults initialisieren
            $this->db->insert('tenant_branding', [
                'tenant_id' => $tenantId,
                'site_name' => $data['name'],
                'site_name_full' => $data['name'],
            ]);

            // Onboarding initialisieren
            $this->db->insert('tenant_onboarding', [
                'tenant_id' => $tenantId,
                'current_step' => 1,
            ]);

            // Stream hinzufügen falls vorhanden
            if (!empty($data['stream_url'])) {
                $this->db->insert('tenant_streams', [
                    'tenant_id' => $tenantId,
                    'name' => 'Main Stream',
                    'stream_url' => $data['stream_url'],
                    'stream_type' => $data['stream_type'] ?? 'hls',
                    'is_primary' => 1,
                ]);
            }

            $this->db->commit();

            return $tenantId;

        } catch (Exception $e) {
            $this->db->rollback();
            throw $e;
        }
    }

    /**
     * Aktualisiert einen Tenant
     */
    public function update(int $tenantId, array $data): bool
    {
        $allowedFields = ['name', 'email', 'status', 'plan_id'];
        $updateData = array_intersect_key($data, array_flip($allowedFields));

        if (empty($updateData)) {
            return false;
        }

        return $this->db->update('tenants', $updateData, 'id = ?', [$tenantId]) > 0;
    }

    /**
     * Löscht einen Tenant (Soft-Delete durch Status-Änderung)
     */
    public function delete(int $tenantId): bool
    {
        return $this->db->update('tenants', ['status' => 'cancelled'], 'id = ?', [$tenantId]) > 0;
    }

    /**
     * Hard-Delete (wirklich löschen - Vorsicht!)
     */
    public function hardDelete(int $tenantId): bool
    {
        return $this->db->delete('tenants', 'id = ?', [$tenantId]) > 0;
    }

    /**
     * Gibt einen Tenant anhand der ID zurück
     */
    public function getById(int $id): ?array
    {
        return $this->db->fetchOne(
            "SELECT t.*, p.name as plan_name, p.features as plan_features
             FROM tenants t
             LEFT JOIN plans p ON t.plan_id = p.id
             WHERE t.id = ?",
            [$id]
        );
    }

    /**
     * Gibt einen Tenant anhand des Slugs zurück
     */
    public function getBySlug(string $slug): ?array
    {
        return $this->db->fetchOne(
            "SELECT t.*, p.name as plan_name, p.features as plan_features
             FROM tenants t
             LEFT JOIN plans p ON t.plan_id = p.id
             WHERE t.slug = ?",
            [$slug]
        );
    }

    /**
     * Gibt einen Tenant anhand der UUID zurück
     */
    public function getByUuid(string $uuid): ?array
    {
        return $this->db->fetchOne(
            "SELECT t.*, p.name as plan_name, p.features as plan_features
             FROM tenants t
             LEFT JOIN plans p ON t.plan_id = p.id
             WHERE t.uuid = ?",
            [$uuid]
        );
    }

    /**
     * Listet alle Tenants auf
     */
    public function getAll(array $filters = []): array
    {
        $sql = "SELECT t.*, p.name as plan_name, p.features as plan_features
                FROM tenants t
                LEFT JOIN plans p ON t.plan_id = p.id
                WHERE 1=1";
        $params = [];

        if (!empty($filters['status'])) {
            $sql .= " AND t.status = ?";
            $params[] = $filters['status'];
        }

        if (!empty($filters['search'])) {
            $sql .= " AND (t.name LIKE ? OR t.email LIKE ?)";
            $params[] = '%' . $filters['search'] . '%';
            $params[] = '%' . $filters['search'] . '%';
        }

        $sql .= " ORDER BY t.created_at DESC";

        if (!empty($filters['limit'])) {
            $sql .= " LIMIT " . (int)$filters['limit'];
            if (!empty($filters['offset'])) {
                $sql .= " OFFSET " . (int)$filters['offset'];
            }
        }

        return $this->db->fetchAll($sql, $params);
    }

    /**
     * Zählt Tenants
     */
    public function count(array $filters = []): int
    {
        $sql = "SELECT COUNT(*) FROM tenants WHERE 1=1";
        $params = [];

        if (!empty($filters['status'])) {
            $sql .= " AND status = ?";
            $params[] = $filters['status'];
        }

        return (int) $this->db->fetchColumn($sql, $params);
    }

    /**
     * Fügt eine Domain zu einem Tenant hinzu
     */
    public function addDomain(int $tenantId, string $domain, bool $isPrimary = false): int
    {
        // Normalisiere Domain
        $domain = strtolower(trim($domain));

        // Prüfe ob Domain bereits existiert
        $existing = $this->db->fetchOne(
            "SELECT id FROM tenant_domains WHERE domain = ?",
            [$domain]
        );

        if ($existing) {
            throw new Exception("Domain '$domain' is already in use");
        }

        // Wenn primary, setze alle anderen auf non-primary
        if ($isPrimary) {
            $this->db->execute(
                "UPDATE tenant_domains SET is_primary = 0 WHERE tenant_id = ?",
                [$tenantId]
            );
        }

        return $this->db->insert('tenant_domains', [
            'tenant_id' => $tenantId,
            'domain' => $domain,
            'is_primary' => $isPrimary ? 1 : 0,
        ]);
    }

    /**
     * Entfernt eine Domain von einem Tenant
     */
    public function removeDomain(int $tenantId, string $domain): bool
    {
        return $this->db->delete('tenant_domains', 'tenant_id = ? AND domain = ?', [$tenantId, $domain]) > 0;
    }

    /**
     * Gibt alle Domains eines Tenants zurück
     */
    public function getDomains(int $tenantId): array
    {
        return $this->db->fetchAll(
            "SELECT * FROM tenant_domains WHERE tenant_id = ? ORDER BY is_primary DESC",
            [$tenantId]
        );
    }

    /**
     * Aktualisiert das Branding eines Tenants
     */
    public function updateBranding(int $tenantId, array $data): bool
    {
        $allowedFields = [
            'site_name', 'site_name_full', 'tagline', 'logo_path', 'favicon_path',
            'primary_color', 'secondary_color', 'accent_color',
            'welcome_text_de', 'welcome_text_en', 'footer_text',
            'custom_css', 'custom_js',
            'social_facebook', 'social_instagram', 'social_youtube'
        ];

        $updateData = array_intersect_key($data, array_flip($allowedFields));

        if (empty($updateData)) {
            return false;
        }

        // Prüfe ob Branding existiert
        $exists = $this->db->fetchColumn(
            "SELECT tenant_id FROM tenant_branding WHERE tenant_id = ?",
            [$tenantId]
        );

        if ($exists) {
            return $this->db->update('tenant_branding', $updateData, 'tenant_id = ?', [$tenantId]) > 0;
        } else {
            $updateData['tenant_id'] = $tenantId;
            return $this->db->insert('tenant_branding', $updateData) > 0;
        }
    }

    /**
     * Gibt das Branding eines Tenants zurück
     */
    public function getBranding(int $tenantId): ?array
    {
        return $this->db->fetchOne(
            "SELECT * FROM tenant_branding WHERE tenant_id = ?",
            [$tenantId]
        );
    }

    /**
     * Prüft ob ein Slug verfügbar ist
     */
    public function isSlugAvailable(string $slug, ?int $excludeTenantId = null): bool
    {
        $sql = "SELECT id FROM tenants WHERE slug = ?";
        $params = [$slug];

        if ($excludeTenantId) {
            $sql .= " AND id != ?";
            $params[] = $excludeTenantId;
        }

        return $this->db->fetchOne($sql, $params) === null;
    }

    /**
     * Prüft ob eine Domain verfügbar ist
     */
    public function isDomainAvailable(string $domain, ?int $excludeTenantId = null): bool
    {
        $sql = "SELECT td.id FROM tenant_domains td WHERE td.domain = ?";
        $params = [$domain];

        if ($excludeTenantId) {
            $sql .= " AND td.tenant_id != ?";
            $params[] = $excludeTenantId;
        }

        return $this->db->fetchOne($sql, $params) === null;
    }

    /**
     * Generiert einen URL-sicheren Slug aus einem Namen
     */
    private function generateSlug(string $name): string
    {
        $slug = strtolower($name);
        $slug = preg_replace('/[^a-z0-9]+/', '-', $slug);
        $slug = trim($slug, '-');

        // Sicherstellen dass Slug einzigartig ist
        $baseSlug = $slug;
        $counter = 1;
        while (!$this->isSlugAvailable($slug)) {
            $slug = $baseSlug . '-' . $counter;
            $counter++;
        }

        return $slug;
    }

    /**
     * Generiert eine UUID v4
     */
    private function generateUuid(): string
    {
        $data = random_bytes(16);
        $data[6] = chr(ord($data[6]) & 0x0f | 0x40);
        $data[8] = chr(ord($data[8]) & 0x3f | 0x80);
        return vsprintf('%s%s-%s-%s-%s-%s%s%s', str_split(bin2hex($data), 4));
    }

    /**
     * Berechnet das Trial-Ende (14 Tage)
     */
    private function calculateTrialEnd(): string
    {
        return date('Y-m-d H:i:s', strtotime('+14 days'));
    }

    /**
     * Gibt die ID des Default-Plans (Free) zurück
     */
    private function getDefaultPlanId(): int
    {
        $plan = $this->db->fetchOne("SELECT id FROM plans WHERE slug = 'free' LIMIT 1");
        return $plan ? (int)$plan['id'] : 1;
    }

    /**
     * Aktiviert einen Tenant (z.B. nach Zahlung)
     */
    public function activate(int $tenantId): bool
    {
        return $this->db->update('tenants', ['status' => 'active'], 'id = ?', [$tenantId]) > 0;
    }

    /**
     * Suspendiert einen Tenant (z.B. bei Zahlungsausfall)
     */
    public function suspend(int $tenantId): bool
    {
        return $this->db->update('tenants', ['status' => 'suspended'], 'id = ?', [$tenantId]) > 0;
    }
}
