<?php
/**
 * Onboarding - Branding (Schritt 4)
 */

require_once dirname(__DIR__) . '/vendor/autoload.php';
require_once dirname(__DIR__) . '/SettingsManager.php';

if (file_exists(dirname(__DIR__) . '/src/bootstrap.php')) {
    require_once dirname(__DIR__) . '/src/bootstrap.php';
}

use AuroraLivecam\Auth\AuthManager;
use AuroraLivecam\Onboarding\OnboardingManager;

$settingsManager = new SettingsManager();
$auth = new AuthManager();

if (!$auth->isLoggedIn()) {
    header('Location: /onboarding/register.php');
    exit;
}

$user = $auth->getUser();
$tenantId = $user['tenant_id'] ?? 0;

$error = '';
$branding = [
    'site_name' => $user['tenant_name'] ?? '',
    'tagline' => '',
    'primary_color' => '#667eea',
    'secondary_color' => '#764ba2',
];

// Formular verarbeiten
if ($_SERVER['REQUEST_METHOD'] === 'POST') {
    $branding = [
        'site_name' => trim($_POST['site_name'] ?? ''),
        'site_name_full' => trim($_POST['site_name'] ?? ''),
        'tagline' => trim($_POST['tagline'] ?? ''),
        'primary_color' => $_POST['primary_color'] ?? '#667eea',
        'secondary_color' => $_POST['secondary_color'] ?? '#764ba2',
    ];

    try {
        $onboarding = new OnboardingManager();
        $result = $onboarding->saveBranding($tenantId, $branding);

        if ($result['success']) {
            header('Location: /onboarding/complete.php');
            exit;
        } else {
            $error = $result['error'] ?? 'Fehler beim Speichern';
        }
    } catch (\Exception $e) {
        $error = 'Fehler: ' . $e->getMessage();
    }
}

// Skip
if (isset($_GET['skip'])) {
    header('Location: /onboarding/complete.php');
    exit;
}
?>
<!DOCTYPE html>
<html lang="de">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Branding - Aurora Livecam</title>
    <link rel="stylesheet" href="/dashboard/assets/dashboard.css">
    <style>
        .onboarding-container {
            min-height: 100vh;
            display: flex;
            align-items: center;
            justify-content: center;
            background: linear-gradient(135deg, var(--primary) 0%, var(--secondary) 100%);
            padding: 2rem;
        }
        .onboarding-box {
            background: var(--white);
            padding: 2.5rem;
            border-radius: 1rem;
            box-shadow: 0 25px 50px -12px rgba(0, 0, 0, 0.25);
            width: 100%;
            max-width: 600px;
        }
        .progress-steps {
            display: flex;
            justify-content: center;
            gap: 0.5rem;
            margin-bottom: 1.5rem;
        }
        .step {
            width: 12px;
            height: 12px;
            border-radius: 50%;
            background: var(--gray-300);
        }
        .step.active { background: var(--primary); }
        .step.completed { background: var(--success); }
        .onboarding-header {
            text-align: center;
            margin-bottom: 2rem;
        }
        .onboarding-header h1 {
            font-size: 1.5rem;
            margin-bottom: 0.5rem;
        }
        .color-row {
            display: grid;
            grid-template-columns: 1fr 1fr;
            gap: 1rem;
        }
        .preview-card {
            margin-top: 1.5rem;
            border-radius: 0.75rem;
            overflow: hidden;
            box-shadow: 0 4px 6px rgba(0,0,0,0.1);
        }
        .preview-header {
            padding: 1.5rem;
            color: white;
            text-align: center;
        }
        .preview-header h3 {
            margin: 0;
            font-size: 1.25rem;
        }
        .preview-header p {
            margin: 0.5rem 0 0 0;
            opacity: 0.9;
            font-size: 0.875rem;
        }
        .preview-body {
            padding: 1rem;
            background: var(--gray-100);
            text-align: center;
            font-size: 0.875rem;
            color: var(--gray-500);
        }
        .skip-link {
            display: block;
            text-align: center;
            margin-top: 1.5rem;
            color: var(--gray-500);
            font-size: 0.875rem;
        }
    </style>
</head>
<body>
    <div class="onboarding-container">
        <div class="onboarding-box">
            <div class="progress-steps">
                <div class="step completed"></div>
                <div class="step completed"></div>
                <div class="step completed"></div>
                <div class="step active"></div>
            </div>

            <div class="onboarding-header">
                <h1>🎨 Branding</h1>
                <p style="color: var(--gray-500);">Personalisieren Sie Ihre Livecam</p>
            </div>

            <?php if ($error): ?>
            <div class="alert alert-error"><?php echo htmlspecialchars($error); ?></div>
            <?php endif; ?>

            <form method="POST" action="">
                <div class="form-group">
                    <label class="form-label" for="site_name">Name Ihrer Livecam</label>
                    <input type="text" id="site_name" name="site_name" class="form-input"
                           value="<?php echo htmlspecialchars($branding['site_name']); ?>"
                           placeholder="z.B. Berghütte Webcam">
                </div>

                <div class="form-group">
                    <label class="form-label" for="tagline">Slogan / Beschreibung</label>
                    <input type="text" id="tagline" name="tagline" class="form-input"
                           value="<?php echo htmlspecialchars($branding['tagline']); ?>"
                           placeholder="z.B. Live aus den Schweizer Alpen">
                </div>

                <div class="color-row">
                    <div class="form-group">
                        <label class="form-label">Primärfarbe</label>
                        <div class="color-picker-wrapper">
                            <input type="color" name="primary_color" id="primary_color" class="color-picker"
                                   value="<?php echo htmlspecialchars($branding['primary_color']); ?>">
                            <span class="color-value"><?php echo htmlspecialchars($branding['primary_color']); ?></span>
                        </div>
                    </div>

                    <div class="form-group">
                        <label class="form-label">Sekundärfarbe</label>
                        <div class="color-picker-wrapper">
                            <input type="color" name="secondary_color" id="secondary_color" class="color-picker"
                                   value="<?php echo htmlspecialchars($branding['secondary_color']); ?>">
                            <span class="color-value"><?php echo htmlspecialchars($branding['secondary_color']); ?></span>
                        </div>
                    </div>
                </div>

                <!-- Live Preview -->
                <div class="preview-card">
                    <div class="preview-header" id="preview-header" style="background: linear-gradient(135deg, <?php echo htmlspecialchars($branding['primary_color']); ?> 0%, <?php echo htmlspecialchars($branding['secondary_color']); ?> 100%);">
                        <h3 id="preview-name"><?php echo htmlspecialchars($branding['site_name'] ?: 'Ihre Livecam'); ?></h3>
                        <p id="preview-tagline"><?php echo htmlspecialchars($branding['tagline'] ?: 'Ihr Slogan hier'); ?></p>
                    </div>
                    <div class="preview-body">
                        Live-Vorschau
                    </div>
                </div>

                <button type="submit" class="btn btn-primary" style="width: 100%; margin-top: 1.5rem;">
                    Speichern & abschliessen
                </button>
            </form>

            <a href="?skip=1" class="skip-link">
                Später anpassen →
            </a>
        </div>
    </div>

    <script>
    // Live preview updates
    document.getElementById('site_name').addEventListener('input', (e) => {
        document.getElementById('preview-name').textContent = e.target.value || 'Ihre Livecam';
    });

    document.getElementById('tagline').addEventListener('input', (e) => {
        document.getElementById('preview-tagline').textContent = e.target.value || 'Ihr Slogan hier';
    });

    document.getElementById('primary_color').addEventListener('input', updateColors);
    document.getElementById('secondary_color').addEventListener('input', updateColors);

    function updateColors() {
        const primary = document.getElementById('primary_color').value;
        const secondary = document.getElementById('secondary_color').value;
        document.getElementById('preview-header').style.background =
            `linear-gradient(135deg, ${primary} 0%, ${secondary} 100%)`;

        document.querySelectorAll('.color-value')[0].textContent = primary;
        document.querySelectorAll('.color-value')[1].textContent = secondary;
    }
    </script>
</body>
</html>
