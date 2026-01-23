<?php
/**
 * Dashboard - Einstellungen
 */

require_once dirname(__DIR__) . '/vendor/autoload.php';
require_once dirname(__DIR__) . '/SettingsManager.php';

if (file_exists(dirname(__DIR__) . '/src/bootstrap.php')) {
    require_once dirname(__DIR__) . '/src/bootstrap.php';
}

use AuroraLivecam\Auth\AuthManager;
use AuroraLivecam\Tenant\TenantSettingsManager;

$settingsManager = new SettingsManager();
$auth = new AuthManager();
$auth->requireLogin();

$user = $auth->getUser();
$tenantId = $user['tenant_id'] ?? 0;

$flashMessage = null;
$flashType = 'info';

// Tenant-Settings laden
try {
    $tenantSettings = new TenantSettingsManager($tenantId);
} catch (\Exception $e) {
    $tenantSettings = null;
}

// Einstellungen für das Template
$settings = [
    'viewer_display_enabled' => $settingsManager->get('viewer_display.enabled') ?? true,
    'viewer_min' => $settingsManager->get('viewer_display.min_viewers') ?? 1,
    'weather_enabled' => $settingsManager->get('weather.enabled') ?? true,
    'weather_location' => $settingsManager->get('weather.location') ?? 'Zürich,CH',
    'weather_lat' => $settingsManager->get('weather.lat') ?? '47.3769',
    'weather_lon' => $settingsManager->get('weather.lon') ?? '8.5417',
    'guestbook_enabled' => $settingsManager->get('content.guestbook_enabled') ?? true,
    'gallery_enabled' => $settingsManager->get('content.gallery_enabled') ?? true,
    'ai_events_enabled' => $settingsManager->get('content.ai_events_enabled') ?? true,
    'show_qr_code' => $settingsManager->get('ui_display.show_qr_code') ?? true,
    'show_social_media' => $settingsManager->get('ui_display.show_social_media') ?? true,
    'timelapse_reverse' => $settingsManager->get('zoom_timelapse.timelapse_reverse_enabled') ?? true,
    'max_zoom' => $settingsManager->get('zoom_timelapse.max_zoom_level') ?? 4.0,
];

// Formular verarbeiten
if ($_SERVER['REQUEST_METHOD'] === 'POST') {
    $updates = [
        'viewer_display.enabled' => isset($_POST['viewer_display_enabled']),
        'viewer_display.min_viewers' => (int)($_POST['viewer_min'] ?? 1),
        'weather.enabled' => isset($_POST['weather_enabled']),
        'weather.location' => trim($_POST['weather_location'] ?? ''),
        'weather.lat' => trim($_POST['weather_lat'] ?? ''),
        'weather.lon' => trim($_POST['weather_lon'] ?? ''),
        'content.guestbook_enabled' => isset($_POST['guestbook_enabled']),
        'content.gallery_enabled' => isset($_POST['gallery_enabled']),
        'content.ai_events_enabled' => isset($_POST['ai_events_enabled']),
        'ui_display.show_qr_code' => isset($_POST['show_qr_code']),
        'ui_display.show_social_media' => isset($_POST['show_social_media']),
        'zoom_timelapse.timelapse_reverse_enabled' => isset($_POST['timelapse_reverse']),
        'zoom_timelapse.max_zoom_level' => (float)($_POST['max_zoom'] ?? 4.0),
    ];

    $success = true;
    foreach ($updates as $key => $value) {
        if (!$settingsManager->set($key, $value)) {
            $success = false;
        }
    }

    if ($success) {
        $flashMessage = 'Einstellungen gespeichert!';
        $flashType = 'success';

        // Reload settings
        $settings = [
            'viewer_display_enabled' => $updates['viewer_display.enabled'],
            'viewer_min' => $updates['viewer_display.min_viewers'],
            'weather_enabled' => $updates['weather.enabled'],
            'weather_location' => $updates['weather.location'],
            'weather_lat' => $updates['weather.lat'],
            'weather_lon' => $updates['weather.lon'],
            'guestbook_enabled' => $updates['content.guestbook_enabled'],
            'gallery_enabled' => $updates['content.gallery_enabled'],
            'ai_events_enabled' => $updates['content.ai_events_enabled'],
            'show_qr_code' => $updates['ui_display.show_qr_code'],
            'show_social_media' => $updates['ui_display.show_social_media'],
            'timelapse_reverse' => $updates['zoom_timelapse.timelapse_reverse_enabled'],
            'max_zoom' => $updates['zoom_timelapse.max_zoom_level'],
        ];
    } else {
        $flashMessage = 'Fehler beim Speichern einiger Einstellungen.';
        $flashType = 'error';
    }
}

$pageTitle = 'Einstellungen';
$currentPage = 'settings';

ob_start();
?>

<form method="POST" action="">
    <div class="grid grid-2">
        <!-- Viewer-Anzeige -->
        <div class="card">
            <div class="card-header">
                <h3 class="card-title">Zuschauer-Anzeige</h3>
            </div>
            <div class="card-body">
                <div class="form-group">
                    <label class="toggle-wrapper">
                        <span class="toggle">
                            <input type="checkbox" name="viewer_display_enabled"
                                   <?php echo $settings['viewer_display_enabled'] ? 'checked' : ''; ?>>
                            <span class="toggle-slider"></span>
                        </span>
                        <span>Zuschauer-Anzahl anzeigen</span>
                    </label>
                </div>

                <div class="form-group">
                    <label class="form-label" for="viewer_min">Mindestanzahl für Anzeige</label>
                    <input type="number" id="viewer_min" name="viewer_min" class="form-input"
                           value="<?php echo (int)$settings['viewer_min']; ?>" min="0" max="100">
                    <p class="form-help">Zuschauer werden erst ab dieser Anzahl angezeigt</p>
                </div>
            </div>
        </div>

        <!-- Wetter-Widget -->
        <div class="card">
            <div class="card-header">
                <h3 class="card-title">Wetter-Widget</h3>
            </div>
            <div class="card-body">
                <div class="form-group">
                    <label class="toggle-wrapper">
                        <span class="toggle">
                            <input type="checkbox" name="weather_enabled"
                                   <?php echo $settings['weather_enabled'] ? 'checked' : ''; ?>>
                            <span class="toggle-slider"></span>
                        </span>
                        <span>Wetter-Widget aktivieren</span>
                    </label>
                </div>

                <div class="form-group">
                    <label class="form-label" for="weather_location">Standort-Name</label>
                    <input type="text" id="weather_location" name="weather_location" class="form-input"
                           value="<?php echo htmlspecialchars($settings['weather_location']); ?>">
                </div>

                <div class="grid grid-2">
                    <div class="form-group">
                        <label class="form-label" for="weather_lat">Breitengrad</label>
                        <input type="text" id="weather_lat" name="weather_lat" class="form-input"
                               value="<?php echo htmlspecialchars($settings['weather_lat']); ?>">
                    </div>
                    <div class="form-group">
                        <label class="form-label" for="weather_lon">Längengrad</label>
                        <input type="text" id="weather_lon" name="weather_lon" class="form-input"
                               value="<?php echo htmlspecialchars($settings['weather_lon']); ?>">
                    </div>
                </div>
            </div>
        </div>

        <!-- Content -->
        <div class="card">
            <div class="card-header">
                <h3 class="card-title">Inhalte</h3>
            </div>
            <div class="card-body">
                <div class="form-group">
                    <label class="toggle-wrapper">
                        <span class="toggle">
                            <input type="checkbox" name="guestbook_enabled"
                                   <?php echo $settings['guestbook_enabled'] ? 'checked' : ''; ?>>
                            <span class="toggle-slider"></span>
                        </span>
                        <span>Gästebuch aktivieren</span>
                    </label>
                </div>

                <div class="form-group">
                    <label class="toggle-wrapper">
                        <span class="toggle">
                            <input type="checkbox" name="gallery_enabled"
                                   <?php echo $settings['gallery_enabled'] ? 'checked' : ''; ?>>
                            <span class="toggle-slider"></span>
                        </span>
                        <span>Galerie aktivieren</span>
                    </label>
                </div>

                <div class="form-group">
                    <label class="toggle-wrapper">
                        <span class="toggle">
                            <input type="checkbox" name="ai_events_enabled"
                                   <?php echo $settings['ai_events_enabled'] ? 'checked' : ''; ?>>
                            <span class="toggle-slider"></span>
                        </span>
                        <span>AI-Events aktivieren</span>
                    </label>
                </div>
            </div>
        </div>

        <!-- UI -->
        <div class="card">
            <div class="card-header">
                <h3 class="card-title">Oberfläche</h3>
            </div>
            <div class="card-body">
                <div class="form-group">
                    <label class="toggle-wrapper">
                        <span class="toggle">
                            <input type="checkbox" name="show_qr_code"
                                   <?php echo $settings['show_qr_code'] ? 'checked' : ''; ?>>
                            <span class="toggle-slider"></span>
                        </span>
                        <span>QR-Code anzeigen</span>
                    </label>
                </div>

                <div class="form-group">
                    <label class="toggle-wrapper">
                        <span class="toggle">
                            <input type="checkbox" name="show_social_media"
                                   <?php echo $settings['show_social_media'] ? 'checked' : ''; ?>>
                            <span class="toggle-slider"></span>
                        </span>
                        <span>Social Media Links anzeigen</span>
                    </label>
                </div>

                <div class="form-group">
                    <label class="toggle-wrapper">
                        <span class="toggle">
                            <input type="checkbox" name="timelapse_reverse"
                                   <?php echo $settings['timelapse_reverse'] ? 'checked' : ''; ?>>
                            <span class="toggle-slider"></span>
                        </span>
                        <span>Timelapse Rückwärts erlauben</span>
                    </label>
                </div>

                <div class="form-group">
                    <label class="form-label" for="max_zoom">Maximaler Zoom</label>
                    <input type="number" id="max_zoom" name="max_zoom" class="form-input"
                           value="<?php echo (float)$settings['max_zoom']; ?>" min="1" max="10" step="0.5">
                </div>
            </div>
        </div>
    </div>

    <div style="margin-top: 1.5rem;">
        <button type="submit" class="btn btn-primary">
            Einstellungen speichern
        </button>
    </div>
</form>

<?php
$content = ob_get_clean();
include __DIR__ . '/templates/layout.php';
