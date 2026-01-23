<?php
/**
 * Dashboard - Übersicht
 */

require_once dirname(__DIR__) . '/vendor/autoload.php';
require_once dirname(__DIR__) . '/SettingsManager.php';

if (file_exists(dirname(__DIR__) . '/src/bootstrap.php')) {
    require_once dirname(__DIR__) . '/src/bootstrap.php';
}

use AuroraLivecam\Auth\AuthManager;
use AuroraLivecam\Core\Database;
use AuroraLivecam\Core\TenantResolver;

$settingsManager = new SettingsManager();
$auth = new AuthManager();

// Login erforderlich
$auth->requireLogin();

$user = $auth->getUser();
$tenantId = $user['tenant_id'] ?? 0;

// Stats laden
$stats = [
    'viewers_current' => 0,
    'viewers_today' => 0,
    'viewers_peak' => 0,
    'stream_status' => 'unknown',
];

// Versuche Stats aus DB zu laden
try {
    $db = Database::getInstance();

    if ($tenantId > 0) {
        // Aktuelle Zuschauer (vereinfacht)
        $viewerFile = dirname(__DIR__) . '/active_viewers.json';
        if (file_exists($viewerFile)) {
            $viewers = json_decode(file_get_contents($viewerFile), true);
            $stats['viewers_current'] = count($viewers ?? []);
        }

        // Heute Stats
        $todayStats = $db->fetchOne(
            "SELECT SUM(viewer_count) as total, MAX(viewer_count) as peak
             FROM viewer_stats
             WHERE tenant_id = ? AND DATE(recorded_at) = CURDATE()",
            [$tenantId]
        );

        if ($todayStats) {
            $stats['viewers_today'] = $todayStats['total'] ?? 0;
            $stats['viewers_peak'] = $todayStats['peak'] ?? 0;
        }

        // Stream Status
        $stream = $db->fetchOne(
            "SELECT last_status FROM tenant_streams WHERE tenant_id = ? AND is_primary = 1",
            [$tenantId]
        );
        $stats['stream_status'] = $stream['last_status'] ?? 'unknown';
    }
} catch (\Exception $e) {
    // DB nicht verfügbar - Legacy-Modus
    $viewerFile = dirname(__DIR__) . '/active_viewers.json';
    if (file_exists($viewerFile)) {
        $viewers = json_decode(file_get_contents($viewerFile), true);
        $stats['viewers_current'] = count($viewers ?? []);
    }
}

// Page Setup
$pageTitle = 'Übersicht';
$currentPage = 'overview';

ob_start();
?>

<!-- Stats Grid -->
<div class="stats-grid">
    <div class="stat-card">
        <div class="stat-icon">👥</div>
        <div class="stat-value"><?php echo $stats['viewers_current']; ?></div>
        <div class="stat-label">Aktuelle Zuschauer</div>
    </div>

    <div class="stat-card">
        <div class="stat-icon">📊</div>
        <div class="stat-value"><?php echo $stats['viewers_today']; ?></div>
        <div class="stat-label">Zuschauer heute</div>
    </div>

    <div class="stat-card">
        <div class="stat-icon">🏆</div>
        <div class="stat-value"><?php echo $stats['viewers_peak']; ?></div>
        <div class="stat-label">Peak heute</div>
    </div>

    <div class="stat-card">
        <div class="stat-icon">
            <?php echo $stats['stream_status'] === 'online' ? '🟢' : ($stats['stream_status'] === 'offline' ? '🔴' : '⚪'); ?>
        </div>
        <div class="stat-value" style="font-size: 1.25rem; text-transform: capitalize;">
            <?php echo $stats['stream_status'] === 'online' ? 'Online' : ($stats['stream_status'] === 'offline' ? 'Offline' : 'Unbekannt'); ?>
        </div>
        <div class="stat-label">Stream Status</div>
    </div>
</div>

<!-- Quick Actions -->
<div class="card">
    <div class="card-header">
        <h3 class="card-title">Schnellzugriff</h3>
    </div>
    <div class="card-body">
        <div class="grid grid-3">
            <a href="/dashboard/stream.php" class="btn btn-secondary">
                📹 Stream bearbeiten
            </a>
            <a href="/dashboard/branding.php" class="btn btn-secondary">
                🎨 Branding anpassen
            </a>
            <a href="/dashboard/settings.php" class="btn btn-secondary">
                ⚙️ Einstellungen
            </a>
        </div>
    </div>
</div>

<!-- Recent Activity (Platzhalter) -->
<div class="card">
    <div class="card-header">
        <h3 class="card-title">Letzte Aktivitäten</h3>
    </div>
    <div class="card-body">
        <p style="color: var(--gray-500); text-align: center; padding: 2rem;">
            Aktivitäten werden hier angezeigt, sobald Analytics aktiviert ist.
        </p>
    </div>
</div>

<?php
$content = ob_get_clean();
include __DIR__ . '/templates/layout.php';
