<?php
/**
 * Dashboard - Branding Einstellungen
 */

require_once dirname(__DIR__) . '/vendor/autoload.php';
require_once dirname(__DIR__) . '/SettingsManager.php';

if (file_exists(dirname(__DIR__) . '/src/bootstrap.php')) {
    require_once dirname(__DIR__) . '/src/bootstrap.php';
}

use AuroraLivecam\Auth\AuthManager;
use AuroraLivecam\Core\Database;
use AuroraLivecam\Tenant\TenantManager;

$settingsManager = new SettingsManager();
$auth = new AuthManager();
$auth->requireLogin();

$user = $auth->getUser();
$tenantId = $user['tenant_id'] ?? 0;

$flashMessage = null;
$flashType = 'info';

// Branding-Daten laden
$branding = [
    'site_name' => '',
    'site_name_full' => '',
    'tagline' => '',
    'primary_color' => '#667eea',
    'secondary_color' => '#764ba2',
    'accent_color' => '#f093fb',
    'welcome_text_de' => '',
    'welcome_text_en' => '',
    'footer_text' => '',
    'custom_css' => '',
];

try {
    $db = Database::getInstance();
    if ($tenantId > 0) {
        $tenantManager = new TenantManager($db);
        $dbBranding = $tenantManager->getBranding($tenantId);
        if ($dbBranding) {
            $branding = array_merge($branding, $dbBranding);
        }
    }
} catch (\Exception $e) {
    // DB nicht verfügbar
}

// Formular verarbeiten
if ($_SERVER['REQUEST_METHOD'] === 'POST') {
    $newBranding = [
        'site_name' => trim($_POST['site_name'] ?? ''),
        'site_name_full' => trim($_POST['site_name_full'] ?? ''),
        'tagline' => trim($_POST['tagline'] ?? ''),
        'primary_color' => $_POST['primary_color'] ?? '#667eea',
        'secondary_color' => $_POST['secondary_color'] ?? '#764ba2',
        'accent_color' => $_POST['accent_color'] ?? '#f093fb',
        'welcome_text_de' => trim($_POST['welcome_text_de'] ?? ''),
        'welcome_text_en' => trim($_POST['welcome_text_en'] ?? ''),
        'footer_text' => trim($_POST['footer_text'] ?? ''),
        'custom_css' => trim($_POST['custom_css'] ?? ''),
    ];

    try {
        $db = Database::getInstance();
        if ($tenantId > 0) {
            $tenantManager = new TenantManager($db);
            $tenantManager->updateBranding($tenantId, $newBranding);

            $flashMessage = 'Branding gespeichert!';
            $flashType = 'success';
            $branding = array_merge($branding, $newBranding);
        } else {
            $flashMessage = 'Branding kann im Legacy-Modus nicht gespeichert werden.';
            $flashType = 'warning';
        }
    } catch (\Exception $e) {
        $flashMessage = 'Fehler beim Speichern: ' . $e->getMessage();
        $flashType = 'error';
    }
}

$pageTitle = 'Branding';
$currentPage = 'branding';

ob_start();
?>

<form method="POST" action="">
    <div class="grid grid-2">
        <!-- Grundeinstellungen -->
        <div class="card">
            <div class="card-header">
                <h3 class="card-title">Grundeinstellungen</h3>
            </div>
            <div class="card-body">
                <div class="form-group">
                    <label class="form-label" for="site_name">Site Name (kurz)</label>
                    <input type="text" id="site_name" name="site_name" class="form-input"
                           value="<?php echo htmlspecialchars($branding['site_name']); ?>"
                           placeholder="MeineCam">
                </div>

                <div class="form-group">
                    <label class="form-label" for="site_name_full">Site Name (vollständig)</label>
                    <input type="text" id="site_name_full" name="site_name_full" class="form-input"
                           value="<?php echo htmlspecialchars($branding['site_name_full']); ?>"
                           placeholder="Meine Wetter Livecam">
                </div>

                <div class="form-group">
                    <label class="form-label" for="tagline">Tagline / Slogan</label>
                    <input type="text" id="tagline" name="tagline" class="form-input"
                           value="<?php echo htmlspecialchars($branding['tagline']); ?>"
                           placeholder="Ihre Live-Webcam 24/7">
                </div>
            </div>
        </div>

        <!-- Farben -->
        <div class="card">
            <div class="card-header">
                <h3 class="card-title">Farben</h3>
            </div>
            <div class="card-body">
                <div class="form-group">
                    <label class="form-label">Primärfarbe</label>
                    <div class="color-picker-wrapper">
                        <input type="color" name="primary_color" class="color-picker"
                               value="<?php echo htmlspecialchars($branding['primary_color']); ?>">
                        <span class="color-value"><?php echo htmlspecialchars($branding['primary_color']); ?></span>
                    </div>
                </div>

                <div class="form-group">
                    <label class="form-label">Sekundärfarbe</label>
                    <div class="color-picker-wrapper">
                        <input type="color" name="secondary_color" class="color-picker"
                               value="<?php echo htmlspecialchars($branding['secondary_color']); ?>">
                        <span class="color-value"><?php echo htmlspecialchars($branding['secondary_color']); ?></span>
                    </div>
                </div>

                <div class="form-group">
                    <label class="form-label">Akzentfarbe</label>
                    <div class="color-picker-wrapper">
                        <input type="color" name="accent_color" class="color-picker"
                               value="<?php echo htmlspecialchars($branding['accent_color']); ?>">
                        <span class="color-value"><?php echo htmlspecialchars($branding['accent_color']); ?></span>
                    </div>
                </div>

                <!-- Vorschau -->
                <div style="margin-top: 1rem; padding: 1rem; border-radius: 0.5rem;
                            background: linear-gradient(135deg, <?php echo htmlspecialchars($branding['primary_color']); ?> 0%, <?php echo htmlspecialchars($branding['secondary_color']); ?> 100%);">
                    <span style="color: white; font-weight: bold;">Farbvorschau</span>
                </div>
            </div>
        </div>
    </div>

    <!-- Texte -->
    <div class="card">
        <div class="card-header">
            <h3 class="card-title">Willkommenstexte</h3>
        </div>
        <div class="card-body">
            <div class="grid grid-2">
                <div class="form-group">
                    <label class="form-label" for="welcome_text_de">Willkommenstext (Deutsch)</label>
                    <textarea id="welcome_text_de" name="welcome_text_de" class="form-textarea"
                              placeholder="Willkommen bei unserer Livecam..."><?php echo htmlspecialchars($branding['welcome_text_de']); ?></textarea>
                </div>

                <div class="form-group">
                    <label class="form-label" for="welcome_text_en">Welcome Text (English)</label>
                    <textarea id="welcome_text_en" name="welcome_text_en" class="form-textarea"
                              placeholder="Welcome to our livecam..."><?php echo htmlspecialchars($branding['welcome_text_en']); ?></textarea>
                </div>
            </div>

            <div class="form-group">
                <label class="form-label" for="footer_text">Footer Text</label>
                <input type="text" id="footer_text" name="footer_text" class="form-input"
                       value="<?php echo htmlspecialchars($branding['footer_text']); ?>"
                       placeholder="© 2024 Ihre Livecam">
            </div>
        </div>
    </div>

    <!-- Custom CSS -->
    <div class="card">
        <div class="card-header">
            <h3 class="card-title">Eigenes CSS</h3>
        </div>
        <div class="card-body">
            <div class="form-group">
                <label class="form-label" for="custom_css">Custom CSS (optional)</label>
                <textarea id="custom_css" name="custom_css" class="form-textarea"
                          style="font-family: monospace; min-height: 150px;"
                          placeholder="/* Eigene CSS-Regeln hier */"><?php echo htmlspecialchars($branding['custom_css']); ?></textarea>
                <p class="form-help">Fortgeschrittene Benutzer können hier eigene CSS-Regeln hinzufügen.</p>
            </div>
        </div>
    </div>

    <div style="margin-top: 1.5rem;">
        <button type="submit" class="btn btn-primary">
            Branding speichern
        </button>
    </div>
</form>

<script>
// Color picker update
document.querySelectorAll('.color-picker').forEach(picker => {
    picker.addEventListener('input', (e) => {
        e.target.parentNode.querySelector('.color-value').textContent = e.target.value;
    });
});
</script>

<?php
$content = ob_get_clean();
include __DIR__ . '/templates/layout.php';
