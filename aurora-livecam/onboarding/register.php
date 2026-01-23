<?php
/**
 * Onboarding - Registrierung
 */

require_once dirname(__DIR__) . '/vendor/autoload.php';
require_once dirname(__DIR__) . '/SettingsManager.php';

if (file_exists(dirname(__DIR__) . '/src/bootstrap.php')) {
    require_once dirname(__DIR__) . '/src/bootstrap.php';
}

use AuroraLivecam\Onboarding\OnboardingManager;
use AuroraLivecam\Auth\AuthManager;

$settingsManager = new SettingsManager();

// Prüfe ob Self-Registration aktiviert ist
if (!$settingsManager->isSelfRegistrationEnabled()) {
    header('Location: /');
    exit;
}

$auth = new AuthManager();

// Bereits eingeloggt?
if ($auth->isLoggedIn()) {
    header('Location: /dashboard/');
    exit;
}

$errors = [];
$formData = [];
$success = false;

// Formular verarbeiten
if ($_SERVER['REQUEST_METHOD'] === 'POST') {
    $formData = [
        'name' => trim($_POST['name'] ?? ''),
        'company_name' => trim($_POST['company_name'] ?? ''),
        'email' => trim($_POST['email'] ?? ''),
        'password' => $_POST['password'] ?? '',
        'password_confirm' => $_POST['password_confirm'] ?? '',
        'stream_url' => trim($_POST['stream_url'] ?? ''),
        'accept_terms' => isset($_POST['accept_terms']),
    ];

    try {
        $onboarding = new OnboardingManager();
        $result = $onboarding->register($formData);

        if ($result['success']) {
            // Session starten und User einloggen
            $auth->login($formData['email'], $formData['password']);

            // Zur nächsten Seite weiterleiten
            if ($onboarding->requiresEmailVerification()) {
                // Token für Demo-Zwecke in Session speichern
                $_SESSION['verification_token'] = $result['verification_token'];
                header('Location: /onboarding/verify.php');
            } else {
                header('Location: /onboarding/stream.php');
            }
            exit;
        } else {
            $errors = $result['errors'];
        }
    } catch (\Exception $e) {
        $errors['general'] = 'Registrierung fehlgeschlagen: ' . $e->getMessage();
    }
}

$trialDays = $settingsManager->getTrialDays();
?>
<!DOCTYPE html>
<html lang="de">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Registrierung - Aurora Livecam</title>
    <link rel="stylesheet" href="/dashboard/assets/dashboard.css">
    <style>
        .register-container {
            min-height: 100vh;
            display: flex;
            align-items: center;
            justify-content: center;
            background: linear-gradient(135deg, var(--primary) 0%, var(--secondary) 100%);
            padding: 2rem;
        }
        .register-box {
            background: var(--white);
            padding: 2.5rem;
            border-radius: 1rem;
            box-shadow: 0 25px 50px -12px rgba(0, 0, 0, 0.25);
            width: 100%;
            max-width: 500px;
        }
        .register-header {
            text-align: center;
            margin-bottom: 2rem;
        }
        .register-header h1 {
            font-size: 1.75rem;
            margin-bottom: 0.5rem;
        }
        .register-header p {
            color: var(--gray-500);
        }
        .trial-badge {
            display: inline-block;
            background: linear-gradient(135deg, var(--success) 0%, #38a169 100%);
            color: white;
            padding: 0.25rem 0.75rem;
            border-radius: 9999px;
            font-size: 0.875rem;
            margin-top: 0.5rem;
        }
        .form-row {
            display: grid;
            grid-template-columns: 1fr 1fr;
            gap: 1rem;
        }
        .error-text {
            color: var(--danger);
            font-size: 0.875rem;
            margin-top: 0.25rem;
        }
        .input-error {
            border-color: var(--danger) !important;
        }
        .terms-text {
            font-size: 0.875rem;
            color: var(--gray-600);
        }
        .terms-text a {
            color: var(--primary);
        }
        .divider {
            display: flex;
            align-items: center;
            margin: 1.5rem 0;
            color: var(--gray-400);
        }
        .divider::before,
        .divider::after {
            content: '';
            flex: 1;
            height: 1px;
            background: var(--gray-300);
        }
        .divider span {
            padding: 0 1rem;
            font-size: 0.875rem;
        }
        @media (max-width: 500px) {
            .form-row {
                grid-template-columns: 1fr;
            }
        }
    </style>
</head>
<body>
    <div class="register-container">
        <div class="register-box">
            <div class="register-header">
                <h1>Jetzt starten</h1>
                <p>Erstellen Sie Ihre eigene Live-Webcam</p>
                <span class="trial-badge"><?php echo $trialDays; ?> Tage kostenlos testen</span>
            </div>

            <?php if (!empty($errors['general'])): ?>
            <div class="alert alert-error"><?php echo htmlspecialchars($errors['general']); ?></div>
            <?php endif; ?>

            <form method="POST" action="" novalidate>
                <div class="form-row">
                    <div class="form-group">
                        <label class="form-label" for="name">Ihr Name *</label>
                        <input type="text" id="name" name="name" class="form-input <?php echo isset($errors['name']) ? 'input-error' : ''; ?>"
                               value="<?php echo htmlspecialchars($formData['name'] ?? ''); ?>" required>
                        <?php if (isset($errors['name'])): ?>
                        <p class="error-text"><?php echo htmlspecialchars($errors['name']); ?></p>
                        <?php endif; ?>
                    </div>

                    <div class="form-group">
                        <label class="form-label" for="company_name">Webcam / Firma *</label>
                        <input type="text" id="company_name" name="company_name" class="form-input <?php echo isset($errors['company_name']) ? 'input-error' : ''; ?>"
                               value="<?php echo htmlspecialchars($formData['company_name'] ?? ''); ?>"
                               placeholder="z.B. Berghütte Webcam" required>
                        <?php if (isset($errors['company_name'])): ?>
                        <p class="error-text"><?php echo htmlspecialchars($errors['company_name']); ?></p>
                        <?php endif; ?>
                    </div>
                </div>

                <div class="form-group">
                    <label class="form-label" for="email">E-Mail-Adresse *</label>
                    <input type="email" id="email" name="email" class="form-input <?php echo isset($errors['email']) ? 'input-error' : ''; ?>"
                           value="<?php echo htmlspecialchars($formData['email'] ?? ''); ?>" required>
                    <?php if (isset($errors['email'])): ?>
                    <p class="error-text"><?php echo htmlspecialchars($errors['email']); ?></p>
                    <?php endif; ?>
                </div>

                <div class="form-row">
                    <div class="form-group">
                        <label class="form-label" for="password">Passwort *</label>
                        <input type="password" id="password" name="password" class="form-input <?php echo isset($errors['password']) ? 'input-error' : ''; ?>"
                               minlength="8" required>
                        <?php if (isset($errors['password'])): ?>
                        <p class="error-text"><?php echo htmlspecialchars($errors['password']); ?></p>
                        <?php endif; ?>
                    </div>

                    <div class="form-group">
                        <label class="form-label" for="password_confirm">Passwort bestätigen *</label>
                        <input type="password" id="password_confirm" name="password_confirm" class="form-input <?php echo isset($errors['password_confirm']) ? 'input-error' : ''; ?>"
                               required>
                        <?php if (isset($errors['password_confirm'])): ?>
                        <p class="error-text"><?php echo htmlspecialchars($errors['password_confirm']); ?></p>
                        <?php endif; ?>
                    </div>
                </div>

                <div class="divider"><span>Optional</span></div>

                <div class="form-group">
                    <label class="form-label" for="stream_url">Stream-URL</label>
                    <input type="url" id="stream_url" name="stream_url" class="form-input <?php echo isset($errors['stream_url']) ? 'input-error' : ''; ?>"
                           value="<?php echo htmlspecialchars($formData['stream_url'] ?? ''); ?>"
                           placeholder="https://example.com/stream.m3u8">
                    <p class="form-help">Sie können die Stream-URL auch später im Dashboard hinzufügen</p>
                    <?php if (isset($errors['stream_url'])): ?>
                    <p class="error-text"><?php echo htmlspecialchars($errors['stream_url']); ?></p>
                    <?php endif; ?>
                </div>

                <div class="form-group">
                    <label class="toggle-wrapper">
                        <input type="checkbox" name="accept_terms" <?php echo !empty($formData['accept_terms']) ? 'checked' : ''; ?> required>
                        <span class="terms-text">
                            Ich akzeptiere die <a href="/terms" target="_blank">AGB</a> und
                            <a href="/privacy" target="_blank">Datenschutzerklärung</a> *
                        </span>
                    </label>
                    <?php if (isset($errors['accept_terms'])): ?>
                    <p class="error-text"><?php echo htmlspecialchars($errors['accept_terms']); ?></p>
                    <?php endif; ?>
                </div>

                <button type="submit" class="btn btn-primary" style="width: 100%; margin-top: 1rem;">
                    Kostenlos registrieren
                </button>
            </form>

            <p style="text-align: center; margin-top: 1.5rem; color: var(--gray-500);">
                Bereits registriert?
                <a href="/dashboard/login.php" style="color: var(--primary);">Anmelden</a>
            </p>
        </div>
    </div>
</body>
</html>
