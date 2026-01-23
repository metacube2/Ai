<?php
/**
 * Dashboard - Stream Einstellungen
 */

require_once dirname(__DIR__) . '/vendor/autoload.php';
require_once dirname(__DIR__) . '/SettingsManager.php';

if (file_exists(dirname(__DIR__) . '/src/bootstrap.php')) {
    require_once dirname(__DIR__) . '/src/bootstrap.php';
}

use AuroraLivecam\Auth\AuthManager;
use AuroraLivecam\Core\Database;

$settingsManager = new SettingsManager();
$auth = new AuthManager();
$auth->requireLogin();

$user = $auth->getUser();
$tenantId = $user['tenant_id'] ?? 0;

$flashMessage = null;
$flashType = 'info';

// Stream-Daten laden
$stream = [
    'stream_url' => '',
    'stream_type' => 'hls',
    'is_active' => true,
    'last_status' => 'unknown',
];

try {
    $db = Database::getInstance();
    if ($tenantId > 0) {
        $dbStream = $db->fetchOne(
            "SELECT * FROM tenant_streams WHERE tenant_id = ? AND is_primary = 1",
            [$tenantId]
        );
        if ($dbStream) {
            $stream = $dbStream;
        }
    }
} catch (\Exception $e) {
    // DB nicht verfügbar
}

// Formular verarbeiten
if ($_SERVER['REQUEST_METHOD'] === 'POST') {
    $streamUrl = trim($_POST['stream_url'] ?? '');
    $streamType = $_POST['stream_type'] ?? 'hls';

    if (empty($streamUrl)) {
        $flashMessage = 'Bitte geben Sie eine Stream-URL ein.';
        $flashType = 'error';
    } else {
        try {
            $db = Database::getInstance();

            if ($tenantId > 0) {
                // Prüfe ob Stream existiert
                $existing = $db->fetchOne(
                    "SELECT id FROM tenant_streams WHERE tenant_id = ? AND is_primary = 1",
                    [$tenantId]
                );

                if ($existing) {
                    $db->update('tenant_streams', [
                        'stream_url' => $streamUrl,
                        'stream_type' => $streamType,
                    ], 'id = ?', [$existing['id']]);
                } else {
                    $db->insert('tenant_streams', [
                        'tenant_id' => $tenantId,
                        'stream_url' => $streamUrl,
                        'stream_type' => $streamType,
                        'is_primary' => 1,
                    ]);
                }

                $flashMessage = 'Stream-Einstellungen gespeichert!';
                $flashType = 'success';

                // Reload stream data
                $stream['stream_url'] = $streamUrl;
                $stream['stream_type'] = $streamType;
            } else {
                $flashMessage = 'Stream-Einstellungen können im Legacy-Modus nicht gespeichert werden.';
                $flashType = 'warning';
            }
        } catch (\Exception $e) {
            $flashMessage = 'Fehler beim Speichern: ' . $e->getMessage();
            $flashType = 'error';
        }
    }
}

$pageTitle = 'Stream Einstellungen';
$currentPage = 'stream';

ob_start();
?>

<div class="card">
    <div class="card-header">
        <h3 class="card-title">Stream Konfiguration</h3>
        <span class="badge badge-<?php echo $stream['last_status'] === 'online' ? 'success' : ($stream['last_status'] === 'offline' ? 'danger' : 'info'); ?>">
            <?php echo ucfirst($stream['last_status'] ?? 'Unbekannt'); ?>
        </span>
    </div>
    <div class="card-body">
        <form method="POST" action="">
            <div class="form-group">
                <label class="form-label" for="stream_url">Stream URL</label>
                <input type="url" id="stream_url" name="stream_url" class="form-input"
                       value="<?php echo htmlspecialchars($stream['stream_url']); ?>"
                       placeholder="https://example.com/stream.m3u8">
                <p class="form-help">Die URL zu Ihrem HLS-Stream (.m3u8) oder RTMP-Stream</p>
            </div>

            <div class="form-group">
                <label class="form-label" for="stream_type">Stream Typ</label>
                <select id="stream_type" name="stream_type" class="form-select">
                    <option value="hls" <?php echo ($stream['stream_type'] ?? 'hls') === 'hls' ? 'selected' : ''; ?>>
                        HLS (.m3u8)
                    </option>
                    <option value="rtmp" <?php echo ($stream['stream_type'] ?? '') === 'rtmp' ? 'selected' : ''; ?>>
                        RTMP
                    </option>
                    <option value="webrtc" <?php echo ($stream['stream_type'] ?? '') === 'webrtc' ? 'selected' : ''; ?>>
                        WebRTC
                    </option>
                    <option value="iframe" <?php echo ($stream['stream_type'] ?? '') === 'iframe' ? 'selected' : ''; ?>>
                        iFrame Embed
                    </option>
                </select>
            </div>

            <button type="submit" class="btn btn-primary">
                Speichern
            </button>
        </form>
    </div>
</div>

<div class="card">
    <div class="card-header">
        <h3 class="card-title">Stream Vorschau</h3>
    </div>
    <div class="card-body">
        <?php if (!empty($stream['stream_url'])): ?>
        <div style="aspect-ratio: 16/9; background: #000; border-radius: 0.5rem; overflow: hidden;">
            <video id="preview-player" controls style="width: 100%; height: 100%;">
                <source src="<?php echo htmlspecialchars($stream['stream_url']); ?>" type="application/x-mpegURL">
            </video>
        </div>
        <p class="form-help" style="margin-top: 1rem;">
            Hinweis: Die Vorschau funktioniert nur mit HLS-Streams und wenn Ihr Browser HLS unterstützt.
        </p>
        <?php else: ?>
        <div class="preview-box">
            <p>Keine Stream-URL konfiguriert</p>
        </div>
        <?php endif; ?>
    </div>
</div>

<div class="card">
    <div class="card-header">
        <h3 class="card-title">Stream Monitoring</h3>
    </div>
    <div class="card-body">
        <p style="color: var(--gray-500);">
            Stream-Monitoring zeigt automatische Verfügbarkeitsprüfungen an.
            Diese Funktion wird demnächst verfügbar sein.
        </p>
    </div>
</div>

<?php
$content = ob_get_clean();
include __DIR__ . '/templates/layout.php';
