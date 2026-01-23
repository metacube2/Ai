<?php
/**
 * Dashboard Login
 */

require_once dirname(__DIR__) . '/vendor/autoload.php';
require_once dirname(__DIR__) . '/SettingsManager.php';

if (file_exists(dirname(__DIR__) . '/src/bootstrap.php')) {
    require_once dirname(__DIR__) . '/src/bootstrap.php';
}

use AuroraLivecam\Auth\AuthManager;

$settingsManager = new SettingsManager();

// Prüfe ob Dashboard aktiviert ist
if (!$settingsManager->isTenantDashboardEnabled() && !$settingsManager->isMultiTenantEnabled()) {
    // Fallback auf Legacy-Admin
    header('Location: /?admin=1');
    exit;
}

$auth = new AuthManager();

// Bereits eingeloggt?
if ($auth->isLoggedIn()) {
    header('Location: /dashboard/');
    exit;
}

$error = '';

// Login verarbeiten
if ($_SERVER['REQUEST_METHOD'] === 'POST') {
    $email = $_POST['email'] ?? '';
    $password = $_POST['password'] ?? '';
    $remember = isset($_POST['remember']);

    if ($auth->login($email, $password, $remember)) {
        header('Location: /dashboard/');
        exit;
    } else {
        $error = 'Ungültige Anmeldedaten';
    }
}
?>
<!DOCTYPE html>
<html lang="de">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Login - Dashboard</title>
    <link rel="stylesheet" href="/dashboard/assets/dashboard.css">
</head>
<body>
    <div class="login-container">
        <div class="login-box">
            <div class="login-title">
                <h1>Dashboard Login</h1>
                <p>Melden Sie sich an, um fortzufahren</p>
            </div>

            <?php if ($error): ?>
            <div class="alert alert-error"><?php echo htmlspecialchars($error); ?></div>
            <?php endif; ?>

            <form method="POST" action="">
                <div class="form-group">
                    <label class="form-label" for="email">E-Mail / Benutzername</label>
                    <input type="text" id="email" name="email" class="form-input"
                           value="<?php echo htmlspecialchars($_POST['email'] ?? ''); ?>"
                           required autofocus>
                </div>

                <div class="form-group">
                    <label class="form-label" for="password">Passwort</label>
                    <input type="password" id="password" name="password" class="form-input" required>
                </div>

                <div class="form-group">
                    <label class="toggle-wrapper">
                        <span class="toggle">
                            <input type="checkbox" name="remember">
                            <span class="toggle-slider"></span>
                        </span>
                        <span>Angemeldet bleiben</span>
                    </label>
                </div>

                <button type="submit" class="btn btn-primary" style="width: 100%;">
                    Anmelden
                </button>
            </form>

            <p style="text-align: center; margin-top: 1.5rem; color: var(--gray-500);">
                <a href="/" style="color: var(--primary);">Zurück zur Livecam</a>
            </p>
        </div>
    </div>
</body>
</html>
