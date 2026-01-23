<?php
/**
 * Dashboard Layout Template
 *
 * Variablen:
 * - $pageTitle: Seitentitel
 * - $currentPage: Aktuelle Seite (für Navigation)
 * - $content: Hauptinhalt
 */

$user = $auth->getUser();
$tenantName = $user['tenant_name'] ?? 'Dashboard';
?>
<!DOCTYPE html>
<html lang="de">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title><?php echo htmlspecialchars($pageTitle ?? 'Dashboard'); ?> - <?php echo htmlspecialchars($tenantName); ?></title>
    <link rel="stylesheet" href="/dashboard/assets/dashboard.css">
</head>
<body>
    <div class="dashboard-container">
        <!-- Sidebar -->
        <aside class="sidebar">
            <div class="sidebar-header">
                <h2><?php echo htmlspecialchars($tenantName); ?></h2>
                <span class="role-badge"><?php echo htmlspecialchars($user['role'] ?? 'user'); ?></span>
            </div>

            <nav class="sidebar-nav">
                <a href="/dashboard/" class="nav-item <?php echo ($currentPage ?? '') === 'overview' ? 'active' : ''; ?>">
                    <span class="nav-icon">📊</span>
                    <span>Übersicht</span>
                </a>

                <a href="/dashboard/stream.php" class="nav-item <?php echo ($currentPage ?? '') === 'stream' ? 'active' : ''; ?>">
                    <span class="nav-icon">📹</span>
                    <span>Stream</span>
                </a>

                <a href="/dashboard/branding.php" class="nav-item <?php echo ($currentPage ?? '') === 'branding' ? 'active' : ''; ?>">
                    <span class="nav-icon">🎨</span>
                    <span>Branding</span>
                </a>

                <a href="/dashboard/settings.php" class="nav-item <?php echo ($currentPage ?? '') === 'settings' ? 'active' : ''; ?>">
                    <span class="nav-icon">⚙️</span>
                    <span>Einstellungen</span>
                </a>

                <?php if ($settingsManager->isAnalyticsEnabled()): ?>
                <a href="/dashboard/analytics.php" class="nav-item <?php echo ($currentPage ?? '') === 'analytics' ? 'active' : ''; ?>">
                    <span class="nav-icon">📈</span>
                    <span>Analytics</span>
                </a>
                <?php endif; ?>

                <?php if ($settingsManager->isCustomDomainEnabled()): ?>
                <a href="/dashboard/domains.php" class="nav-item <?php echo ($currentPage ?? '') === 'domains' ? 'active' : ''; ?>">
                    <span class="nav-icon">🌐</span>
                    <span>Domains</span>
                </a>
                <?php endif; ?>

                <?php if ($settingsManager->isBillingEnabled()): ?>
                <a href="/dashboard/billing.php" class="nav-item <?php echo ($currentPage ?? '') === 'billing' ? 'active' : ''; ?>">
                    <span class="nav-icon">💳</span>
                    <span>Abrechnung</span>
                </a>
                <?php endif; ?>

                <?php if ($auth->isSuperAdmin()): ?>
                <div class="nav-divider"></div>
                <span class="nav-label">Admin</span>

                <a href="/dashboard/admin/tenants.php" class="nav-item <?php echo ($currentPage ?? '') === 'admin-tenants' ? 'active' : ''; ?>">
                    <span class="nav-icon">👥</span>
                    <span>Kunden</span>
                </a>

                <a href="/dashboard/admin/plans.php" class="nav-item <?php echo ($currentPage ?? '') === 'admin-plans' ? 'active' : ''; ?>">
                    <span class="nav-icon">📋</span>
                    <span>Pläne</span>
                </a>
                <?php endif; ?>
            </nav>

            <div class="sidebar-footer">
                <a href="/" class="nav-item" target="_blank">
                    <span class="nav-icon">🔗</span>
                    <span>Zur Livecam</span>
                </a>
                <a href="/dashboard/logout.php" class="nav-item logout">
                    <span class="nav-icon">🚪</span>
                    <span>Abmelden</span>
                </a>
            </div>
        </aside>

        <!-- Main Content -->
        <main class="main-content">
            <header class="main-header">
                <h1><?php echo htmlspecialchars($pageTitle ?? 'Dashboard'); ?></h1>
                <div class="header-actions">
                    <span class="user-info">
                        <?php echo htmlspecialchars($user['email'] ?? ''); ?>
                    </span>
                </div>
            </header>

            <div class="content-wrapper">
                <?php if (isset($flashMessage)): ?>
                <div class="alert alert-<?php echo $flashType ?? 'info'; ?>">
                    <?php echo htmlspecialchars($flashMessage); ?>
                </div>
                <?php endif; ?>

                <?php echo $content ?? ''; ?>
            </div>
        </main>
    </div>

    <script src="/dashboard/assets/dashboard.js"></script>
</body>
</html>
