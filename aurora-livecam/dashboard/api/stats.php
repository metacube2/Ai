<?php
/**
 * Dashboard API - Stats
 */

header('Content-Type: application/json');

require_once dirname(__DIR__, 2) . '/vendor/autoload.php';
require_once dirname(__DIR__, 2) . '/SettingsManager.php';

if (file_exists(dirname(__DIR__, 2) . '/src/bootstrap.php')) {
    require_once dirname(__DIR__, 2) . '/src/bootstrap.php';
}

use AuroraLivecam\Auth\AuthManager;
use AuroraLivecam\Core\Database;

$auth = new AuthManager();

// Auth check
if (!$auth->isLoggedIn()) {
    http_response_code(401);
    echo json_encode(['success' => false, 'error' => 'Unauthorized']);
    exit;
}

$user = $auth->getUser();
$tenantId = $user['tenant_id'] ?? 0;

$stats = [
    'viewers_current' => 0,
    'viewers_today' => 0,
    'viewers_peak' => 0,
    'stream_status' => 'unknown',
];

// Aktuelle Zuschauer aus Datei
$viewerFile = dirname(__DIR__, 2) . '/active_viewers.json';
if (file_exists($viewerFile)) {
    $viewers = json_decode(file_get_contents($viewerFile), true);
    $stats['viewers_current'] = count($viewers ?? []);
}

// DB Stats falls verfügbar
try {
    $db = Database::getInstance();

    if ($tenantId > 0) {
        $todayStats = $db->fetchOne(
            "SELECT SUM(viewer_count) as total, MAX(viewer_count) as peak
             FROM viewer_stats
             WHERE tenant_id = ? AND DATE(recorded_at) = CURDATE()",
            [$tenantId]
        );

        if ($todayStats) {
            $stats['viewers_today'] = (int)($todayStats['total'] ?? 0);
            $stats['viewers_peak'] = (int)($todayStats['peak'] ?? 0);
        }

        $stream = $db->fetchOne(
            "SELECT last_status FROM tenant_streams WHERE tenant_id = ? AND is_primary = 1",
            [$tenantId]
        );
        $stats['stream_status'] = $stream['last_status'] ?? 'unknown';
    }
} catch (\Exception $e) {
    // DB nicht verfügbar - Stats bleiben auf Defaults
}

echo json_encode([
    'success' => true,
    'stats' => $stats,
    'timestamp' => time(),
]);
