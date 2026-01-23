<?php
/**
 * TenantSettingsManager - DB-basierte Settings pro Tenant
 *
 * Erweitert/ersetzt SettingsManager für Multi-Tenant Betrieb
 * Fällt auf den alten SettingsManager zurück wenn DB nicht verfügbar
 */

namespace AuroraLivecam\Tenant;

use AuroraLivecam\Core\Database;
use AuroraLivecam\Core\TenantResolver;

class TenantSettingsManager
{
    private Database $db;
    private TenantResolver $resolver;
    private int $tenantId;
    private array $settings = [];
    private bool $loaded = false;
    private bool $dbAvailable = false;

    // Fallback auf Legacy-SettingsManager
    private ?\SettingsManager $legacyManager = null;

    public function __construct(?int $tenantId = null, ?Database $db = null, ?TenantResolver $resolver = null)
    {
        $this->db = $db ?? Database::getInstance();
        $this->resolver = $resolver ?? TenantResolver::getInstance();
        $this->tenantId = $tenantId ?? $this->resolver->getTenantId();

        $this->checkDbAvailability();
    }

    /**
     * Prüft ob die DB verfügbar ist
     */
    private function checkDbAvailability(): void
    {
        try {
            $this->db->fetchOne("SELECT 1 FROM tenant_settings LIMIT 1");
            $this->dbAvailable = true;
        } catch (\Exception $e) {
            $this->dbAvailable = false;
        }
    }

    /**
     * Lädt alle Settings für den Tenant
     */
    private function load(): void
    {
        if ($this->loaded) {
            return;
        }

        // Wenn keine DB, nutze Legacy
        if (!$this->dbAvailable || $this->tenantId === 0) {
            $this->loadFromLegacy();
            return;
        }

        $rows = $this->db->fetchAll(
            "SELECT setting_key, setting_value FROM tenant_settings WHERE tenant_id = ?",
            [$this->tenantId]
        );

        foreach ($rows as $row) {
            $value = $row['setting_value'];
            // JSON-Werte parsen
            if ($value !== null && ($value[0] === '{' || $value[0] === '[')) {
                $decoded = json_decode($value, true);
                if (json_last_error() === JSON_ERROR_NONE) {
                    $value = $decoded;
                }
            }
            // Booleans und Zahlen konvertieren
            elseif ($value === 'true') $value = true;
            elseif ($value === 'false') $value = false;
            elseif (is_numeric($value)) $value = strpos($value, '.') !== false ? (float)$value : (int)$value;

            $this->settings[$row['setting_key']] = $value;
        }

        // Defaults für fehlende Keys
        $this->settings = array_merge($this->getDefaults(), $this->settings);
        $this->loaded = true;
    }

    /**
     * Fallback auf Legacy SettingsManager
     */
    private function loadFromLegacy(): void
    {
        if ($this->legacyManager === null) {
            // Legacy-Manager einbinden
            $legacyFile = dirname(__DIR__, 2) . '/SettingsManager.php';
            if (file_exists($legacyFile) && !class_exists('\SettingsManager')) {
                require_once $legacyFile;
            }

            if (class_exists('\SettingsManager')) {
                $this->legacyManager = new \SettingsManager();
            }
        }

        if ($this->legacyManager) {
            // Konvertiere Legacy-Settings in unser Format
            $this->settings = $this->convertLegacySettings($this->legacyManager);
        } else {
            $this->settings = $this->getDefaults();
        }

        $this->loaded = true;
    }

    /**
     * Konvertiert Legacy-Settings
     */
    private function convertLegacySettings(\SettingsManager $legacy): array
    {
        $settings = $this->getDefaults();

        // Mappe Legacy-Werte
        $mappings = [
            'viewer_display.enabled' => 'viewer_display.enabled',
            'viewer_display.min_viewers' => 'viewer_display.min_viewers',
            'video_mode.play_in_player' => 'video_mode.play_in_player',
            'video_mode.allow_download' => 'video_mode.allow_download',
            'timelapse.default_speed' => 'timelapse.default_speed',
            'ui_display.show_recommendation_banner' => 'ui_display.show_recommendation_banner',
            'ui_display.show_qr_code' => 'ui_display.show_qr_code',
            'ui_display.show_social_media' => 'ui_display.show_social_media',
            'content.guestbook_enabled' => 'content.guestbook_enabled',
            'content.gallery_enabled' => 'content.gallery_enabled',
            'weather.enabled' => 'weather.enabled',
            'weather.location' => 'weather.location',
            'weather.lat' => 'weather.lat',
            'weather.lon' => 'weather.lon',
            'seo.custom_title' => 'seo.custom_title',
            'seo.meta_description' => 'seo.meta_description',
        ];

        foreach ($mappings as $legacyKey => $newKey) {
            $value = $legacy->get($legacyKey);
            if ($value !== null) {
                $settings[$newKey] = $value;
            }
        }

        return $settings;
    }

    /**
     * Gibt einen Setting-Wert zurück (mit Dot-Notation)
     */
    public function get(string $key, mixed $default = null): mixed
    {
        $this->load();

        // Direkte Keys
        if (isset($this->settings[$key])) {
            return $this->settings[$key];
        }

        // Dot-Notation auflösen
        $keys = explode('.', $key);
        $value = $this->settings;

        foreach ($keys as $k) {
            if (!is_array($value) || !isset($value[$k])) {
                return $default;
            }
            $value = $value[$k];
        }

        return $value;
    }

    /**
     * Setzt einen Setting-Wert
     */
    public function set(string $key, mixed $value): bool
    {
        $this->load();

        // Wenn keine DB, nutze Legacy
        if (!$this->dbAvailable || $this->tenantId === 0) {
            return $this->setLegacy($key, $value);
        }

        // Wert für DB vorbereiten
        $dbValue = $this->prepareValueForDb($value);

        // UPSERT
        $sql = "INSERT INTO tenant_settings (tenant_id, setting_key, setting_value)
                VALUES (?, ?, ?)
                ON DUPLICATE KEY UPDATE setting_value = VALUES(setting_value)";

        $result = $this->db->execute($sql, [$this->tenantId, $key, $dbValue]) > 0;

        if ($result) {
            $this->settings[$key] = $value;
        }

        return $result;
    }

    /**
     * Setzt Legacy-Setting
     */
    private function setLegacy(string $key, mixed $value): bool
    {
        if ($this->legacyManager) {
            return $this->legacyManager->set($key, $value);
        }
        return false;
    }

    /**
     * Bereitet einen Wert für die DB vor
     */
    private function prepareValueForDb(mixed $value): string
    {
        if (is_bool($value)) {
            return $value ? 'true' : 'false';
        }
        if (is_array($value) || is_object($value)) {
            return json_encode($value);
        }
        return (string)$value;
    }

    /**
     * Löscht ein Setting
     */
    public function delete(string $key): bool
    {
        if (!$this->dbAvailable || $this->tenantId === 0) {
            return false;
        }

        $result = $this->db->delete('tenant_settings', 'tenant_id = ? AND setting_key = ?', [$this->tenantId, $key]) > 0;

        if ($result) {
            unset($this->settings[$key]);
        }

        return $result;
    }

    /**
     * Gibt alle Settings zurück
     */
    public function all(): array
    {
        $this->load();
        return $this->settings;
    }

    /**
     * Setzt mehrere Settings auf einmal
     */
    public function setMany(array $settings): bool
    {
        foreach ($settings as $key => $value) {
            $this->set($key, $value);
        }
        return true;
    }

    /**
     * Default-Settings
     */
    private function getDefaults(): array
    {
        return [
            // Viewer Display
            'viewer_display.enabled' => true,
            'viewer_display.min_viewers' => 1,
            'viewer_display.update_interval' => 5,

            // Video Mode
            'video_mode.play_in_player' => true,
            'video_mode.allow_download' => true,

            // Timelapse
            'timelapse.default_speed' => 1,
            'timelapse.available_speeds' => [1, 10, 100],
            'timelapse.reverse_enabled' => true,

            // UI Display
            'ui_display.show_recommendation_banner' => true,
            'ui_display.show_qr_code' => true,
            'ui_display.show_social_media' => true,

            // Zoom
            'zoom.show_controls' => true,
            'zoom.max_level' => 4.0,

            // Content
            'content.guestbook_enabled' => true,
            'content.gallery_enabled' => true,
            'content.ai_events_enabled' => true,

            // Weather
            'weather.enabled' => true,
            'weather.location' => 'Zürich,CH',
            'weather.lat' => '47.3769',
            'weather.lon' => '8.5417',
            'weather.update_interval' => 5,
            'weather.units' => 'metric',

            // SEO
            'seo.custom_title' => '',
            'seo.meta_description' => '',
            'seo.meta_keywords' => '',

            // Theme
            'theme.default' => 'theme-legacy',
            'theme.show_switcher' => false,
        ];
    }

    // === Helper-Methoden (kompatibel mit altem SettingsManager) ===

    public function isWeatherEnabled(): bool
    {
        return $this->get('weather.enabled', true) === true;
    }

    public function getWeatherLocation(): string
    {
        return $this->get('weather.location', 'Zürich,CH');
    }

    public function getWeatherCoords(): array
    {
        return [
            'lat' => $this->get('weather.lat', '47.3769'),
            'lon' => $this->get('weather.lon', '8.5417'),
        ];
    }

    public function getWeatherUpdateInterval(): int
    {
        return (int)$this->get('weather.update_interval', 5);
    }

    public function shouldShowViewers(): bool
    {
        return $this->get('viewer_display.enabled', true) === true;
    }

    public function getMinViewers(): int
    {
        return (int)$this->get('viewer_display.min_viewers', 1);
    }

    public function isGuestbookEnabled(): bool
    {
        return $this->get('content.guestbook_enabled', true) === true;
    }

    public function isGalleryEnabled(): bool
    {
        return $this->get('content.gallery_enabled', true) === true;
    }

    /**
     * AJAX-Handler (kompatibel mit altem SettingsManager)
     */
    public function handleAjax(): void
    {
        if ($_SERVER['REQUEST_METHOD'] !== 'POST') return;
        if (!isset($_POST['settings_action'])) return;

        header('Content-Type: application/json');

        // Auth prüfen
        if (!$this->isAdmin()) {
            echo json_encode(['success' => false, 'error' => 'Unauthorized']);
            exit;
        }

        $action = $_POST['settings_action'];

        if ($action === 'update' && isset($_POST['key'], $_POST['value'])) {
            $key = $_POST['key'];
            $value = $_POST['value'];

            // Booleans konvertieren
            if ($value === 'true') $value = true;
            elseif ($value === 'false') $value = false;

            $success = $this->set($key, $value);
            echo json_encode(['success' => $success]);
            exit;
        }

        if ($action === 'get') {
            echo json_encode(['success' => true, 'data' => $this->all()]);
            exit;
        }

        echo json_encode(['success' => false, 'error' => 'Unknown action']);
        exit;
    }

    /**
     * Prüft ob der User Admin ist
     */
    private function isAdmin(): bool
    {
        return isset($_SESSION['admin']) && $_SESSION['admin'] === true;
    }

    /**
     * Lädt Settings neu aus der DB
     */
    public function reload(): void
    {
        $this->loaded = false;
        $this->settings = [];
        $this->load();
    }
}
