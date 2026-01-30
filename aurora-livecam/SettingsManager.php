<?php
/**
 * SettingsManager - Verwaltet Admin-Einstellungen
 * Speichert in settings.json, lädt ohne Reload
 */
class SettingsManager {
    private $settingsFile;
    private $settings = [];

    public function __construct($file = null) {
        $this->settingsFile = $file ?: (__DIR__ . '/settings.json');
        $this->load();
    }

    private function load() {
        if (file_exists($this->settingsFile)) {
            $content = file_get_contents($this->settingsFile);
            $this->settings = json_decode($content, true) ?? $this->getDefaults();
        } else {
            $this->settings = $this->getDefaults();
            $this->save();
        }
    }

    private function getDefaults() {
        return [
            'viewer_display' => [
                'enabled' => true,
                'min_viewers' => 1,
                'update_interval' => 5 // Sekunden
            ],
            'video_mode' => [
                'play_in_player' => true,
                'allow_download' => true
            ],
            'timelapse' => [
                'default_speed' => 1,
                'available_speeds' => [1, 10, 100]
            ],
            // Punkt 2: UI-Anzeige Features
            'ui_display' => [
                'show_recommendation_banner' => true,
                'show_qr_code' => true,
                'show_social_media' => true,
                'show_patrouille_suisse' => true
            ],
            // Punkt 3: Zoom & Timelapse
            'zoom_timelapse' => [
                'show_zoom_controls' => true,
                'max_zoom_level' => 4.0,
                'timelapse_reverse_enabled' => true,
                'weekly_timelapse_enabled' => true // Wochenzeitraffer Button
            ],
            // Auto-Screenshot für Galerie
            'auto_screenshot' => [
                'enabled' => false,
                'interval_minutes' => 10,
                'max_images' => 144, // 24h bei 10min Intervall
                'save_to_gallery' => true
            ],
            // Email-Sharing
            'sharing' => [
                'email_enabled' => false,
                'share_link_expiry_hours' => 24
            ],
            // Punkt 5: Content Management
            'content' => [
                'guestbook_enabled' => true,
                'gallery_enabled' => true,
                'ai_events_enabled' => true,
                'max_guestbook_entries' => 50
            ],
            // Punkt 6: Technische Settings
            'technical' => [
                'viewer_update_interval' => 5, // Sekunden
                'session_timeout' => 30 // Sekunden
            ],
            // Punkt 7: Theme & Design
            'theme' => [
                'default_theme' => 'theme-legacy',
                'show_theme_switcher' => false
            ],
            // Punkt 8: SEO & Meta
            'seo' => [
                'custom_title' => '',
                'meta_description' => '',
                'meta_keywords' => ''
            ],
            // Weather Widget
            'weather' => [
                'enabled' => true,
                'api_key' => '',
                'location' => 'Oberdürnten,CH',
                'lat' => '47.2833',
                'lon' => '8.7167',
                'update_interval' => 5, // Minuten
                'units' => 'metric' // metric (Celsius) oder imperial (Fahrenheit)
            ],
            // SaaS Features - alle aktivierbar/deaktivierbar
            'saas_features' => [
                // Multi-Tenant
                'multi_tenant_enabled' => false, // Aktiviert DB-basierte Tenant-Verwaltung
                'customer_management_enabled' => false,

                // Onboarding
                'self_registration_enabled' => false,
                'email_verification_required' => true,
                'trial_enabled' => true,
                'trial_days' => 14,

                // Billing
                'billing_enabled' => false,
                'stripe_enabled' => false,
                'free_plan_available' => true,

                // Dashboard
                'tenant_dashboard_enabled' => false,
                'analytics_enabled' => false,
                'custom_domain_enabled' => false,
                'custom_branding_enabled' => false,

                // Landing
                'landing_page_enabled' => false,
                'demo_mode_enabled' => false,

                // Limits (Default für Free-Plan)
                'default_max_viewers' => 50,
                'default_storage_mb' => 500,
                'default_retention_days' => 7
            ],
            'last_updated' => null,
            'updated_by' => null
        ];
    }

    public function get($key = null) {
        if ($key === null) return $this->settings;
        $keys = explode('.', $key);
        $value = $this->settings;
        foreach ($keys as $k) {
            if (!isset($value[$k])) return null;
            $value = $value[$k];
        }
        return $value;
    }

    public function set($key, $value) {
        $keys = explode('.', $key);
        $ref = &$this->settings;
        foreach ($keys as $i => $k) {
            if ($i === count($keys) - 1) {
                $ref[$k] = $value;
            } else {
                if (!isset($ref[$k])) $ref[$k] = [];
                $ref = &$ref[$k];
            }
        }
        $this->settings['last_updated'] = date('Y-m-d H:i:s');
        return $this->save();
    }

    private function save() {
        $payload = json_encode($this->settings, JSON_PRETTY_PRINT | JSON_UNESCAPED_UNICODE);
        if ($payload === false) {
            return false;
        }

        return file_put_contents($this->settingsFile, $payload, LOCK_EX) !== false;
    }

    // Für AJAX-Anfragen
    public function handleAjax() {
        if ($_SERVER['REQUEST_METHOD'] !== 'POST') return;
        if (!isset($_POST['settings_action'])) return;

        header('Content-Type: application/json');

        switch ($_POST['settings_action']) {
            case 'get':
                echo json_encode(['success' => true, 'settings' => $this->settings]);
                exit;

            case 'update':
                $key = $_POST['key'] ?? null;
                $value = $_POST['value'] ?? null;

                // Boolean-Werte konvertieren
                if ($value === 'true') $value = true;
                if ($value === 'false') $value = false;
                if (is_numeric($value)) $value = intval($value);

                if ($key && $this->set($key, $value)) {
                    echo json_encode(['success' => true, 'message' => 'Einstellung gespeichert']);
                } else {
                    echo json_encode([
                        'success' => false,
                        'message' => 'Fehler beim Speichern. Bitte Dateirechte prüfen.'
                    ]);
                }
                exit;
        }
    }

    // Viewer-Anzeige prüfen
    public function shouldShowViewers($currentCount) {
        if (!$this->get('viewer_display.enabled')) return false;
        return $currentCount >= $this->get('viewer_display.min_viewers');
    }

    // Video-Modus prüfen
    public function shouldPlayInPlayer() {
        return $this->get('video_mode.play_in_player') === true;
    }

    public function shouldAllowDownload() {
        return $this->get('video_mode.allow_download') === true;
    }

    // UI Display Helper
    public function shouldShowRecommendationBanner() {
        return $this->get('ui_display.show_recommendation_banner') === true;
    }

    public function shouldShowQRCode() {
        return $this->get('ui_display.show_qr_code') === true;
    }

    public function shouldShowSocialMedia() {
        return $this->get('ui_display.show_social_media') === true;
    }

    public function shouldShowPatrouillesuisse() {
        return $this->get('ui_display.show_patrouille_suisse') === true;
    }

    // Content Management Helper
    public function isGuestbookEnabled() {
        return $this->get('content.guestbook_enabled') === true;
    }

    public function isGalleryEnabled() {
        return $this->get('content.gallery_enabled') === true;
    }

    public function isAIEventsEnabled() {
        return $this->get('content.ai_events_enabled') === true;
    }

    public function getMaxGuestbookEntries() {
        return $this->get('content.max_guestbook_entries') ?? 50;
    }

    // Theme Helper
    public function getDefaultTheme() {
        return $this->get('theme.default_theme') ?? 'theme-legacy';
    }

    public function shouldShowThemeSwitcher() {
        return $this->get('theme.show_theme_switcher') === true;
    }

    // Technical Helper
    public function getViewerUpdateInterval() {
        return $this->get('technical.viewer_update_interval') ?? 5;
    }

    public function getSessionTimeout() {
        return $this->get('technical.session_timeout') ?? 30;
    }

    // Zoom & Timelapse Helper
    public function shouldShowZoomControls() {
        return $this->get('zoom_timelapse.show_zoom_controls') === true;
    }

    public function getMaxZoomLevel() {
        return $this->get('zoom_timelapse.max_zoom_level') ?? 4.0;
    }

    public function isTimelapseReverseEnabled() {
        return $this->get('zoom_timelapse.timelapse_reverse_enabled') === true;
    }

    public function isWeeklyTimelapseEnabled() {
        return $this->get('zoom_timelapse.weekly_timelapse_enabled') !== false;
    }

    // Auto-Screenshot Helper
    public function isAutoScreenshotEnabled() {
        return $this->get('auto_screenshot.enabled') === true;
    }

    public function getAutoScreenshotInterval() {
        return $this->get('auto_screenshot.interval_minutes') ?? 10;
    }

    public function getAutoScreenshotMaxImages() {
        return $this->get('auto_screenshot.max_images') ?? 144;
    }

    // Sharing Helper
    public function isEmailSharingEnabled() {
        return $this->get('sharing.email_enabled') === true;
    }

    public function getShareLinkExpiryHours() {
        return $this->get('sharing.share_link_expiry_hours') ?? 24;
    }

    // SEO Helper
    public function getCustomTitle() {
        $title = $this->get('seo.custom_title');
        return !empty($title) ? $title : null;
    }

    public function getMetaDescription() {
        return $this->get('seo.meta_description') ?? '';
    }

    public function getMetaKeywords() {
        return $this->get('seo.meta_keywords') ?? '';
    }

    // Weather Helper
    public function isWeatherEnabled() {
        return $this->get('weather.enabled') === true;
    }

    public function getWeatherApiKey() {
        return $this->get('weather.api_key') ?? '';
    }

    public function getWeatherLocation() {
        return $this->get('weather.location') ?? 'Oberdürnten,CH';
    }

    public function getWeatherCoords() {
        return [
            'lat' => $this->get('weather.lat') ?? '47.2833',
            'lon' => $this->get('weather.lon') ?? '8.7167'
        ];
    }

    public function getWeatherUpdateInterval() {
        return $this->get('weather.update_interval') ?? 5;
    }

    public function getWeatherUnits() {
        return $this->get('weather.units') ?? 'metric';
    }

    // SaaS Feature Helper
    public function isMultiTenantEnabled() {
        return $this->get('saas_features.multi_tenant_enabled') === true;
    }

    public function isSelfRegistrationEnabled() {
        return $this->get('saas_features.self_registration_enabled') === true;
    }

    public function isBillingEnabled() {
        return $this->get('saas_features.billing_enabled') === true;
    }

    public function isStripeEnabled() {
        return $this->get('saas_features.stripe_enabled') === true;
    }

    public function isTenantDashboardEnabled() {
        return $this->get('saas_features.tenant_dashboard_enabled') === true;
    }

    public function isAnalyticsEnabled() {
        return $this->get('saas_features.analytics_enabled') === true;
    }

    public function isCustomDomainEnabled() {
        return $this->get('saas_features.custom_domain_enabled') === true;
    }

    public function isCustomBrandingEnabled() {
        return $this->get('saas_features.custom_branding_enabled') === true;
    }

    public function isLandingPageEnabled() {
        return $this->get('saas_features.landing_page_enabled') === true;
    }

    public function getTrialDays() {
        return $this->get('saas_features.trial_days') ?? 14;
    }

    public function getDefaultMaxViewers() {
        return $this->get('saas_features.default_max_viewers') ?? 50;
    }
}
