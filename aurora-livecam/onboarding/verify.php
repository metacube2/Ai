<?php
/**
 * Onboarding - E-Mail Verifizierung
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

// Login prüfen
if (!$auth->isLoggedIn()) {
    header('Location: /onboarding/register.php');
    exit;
}

$user = $auth->getUser();
$message = '';
$error = '';
$verified = false;

// Token aus URL verarbeiten
if (isset($_GET['token'])) {
    try {
        $onboarding = new OnboardingManager();
        $result = $onboarding->verifyEmail($_GET['token']);

        if ($result['success']) {
            $verified = true;
            $message = 'E-Mail erfolgreich verifiziert!';
        } else {
            $error = $result['error'];
        }
    } catch (\Exception $e) {
        $error = 'Verifikation fehlgeschlagen';
    }
}

// E-Mail erneut senden
if ($_SERVER['REQUEST_METHOD'] === 'POST' && isset($_POST['resend'])) {
    try {
        $onboarding = new OnboardingManager();
        $result = $onboarding->resendVerification($user['id']);

        if ($result['success']) {
            $_SESSION['verification_token'] = $result['token'];
            $message = 'Verifikations-E-Mail wurde erneut gesendet!';
        } else {
            $error = $result['error'];
        }
    } catch (\Exception $e) {
        $error = 'Fehler beim Senden';
    }
}

// Demo: Token anzeigen (in Produktion würde eine E-Mail gesendet)
$demoToken = $_SESSION['verification_token'] ?? null;
?>
<!DOCTYPE html>
<html lang="de">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>E-Mail verifizieren - Aurora Livecam</title>
    <link rel="stylesheet" href="/dashboard/assets/dashboard.css">
    <style>
        .verify-container {
            min-height: 100vh;
            display: flex;
            align-items: center;
            justify-content: center;
            background: linear-gradient(135deg, var(--primary) 0%, var(--secondary) 100%);
            padding: 2rem;
        }
        .verify-box {
            background: var(--white);
            padding: 2.5rem;
            border-radius: 1rem;
            box-shadow: 0 25px 50px -12px rgba(0, 0, 0, 0.25);
            width: 100%;
            max-width: 500px;
            text-align: center;
        }
        .verify-icon {
            font-size: 4rem;
            margin-bottom: 1rem;
        }
        .verify-box h1 {
            font-size: 1.5rem;
            margin-bottom: 1rem;
        }
        .verify-box p {
            color: var(--gray-600);
            margin-bottom: 1.5rem;
        }
        .email-highlight {
            font-weight: 600;
            color: var(--gray-800);
        }
        .demo-box {
            background: var(--gray-100);
            border: 1px dashed var(--gray-300);
            border-radius: 0.5rem;
            padding: 1rem;
            margin: 1.5rem 0;
            text-align: left;
        }
        .demo-box h4 {
            font-size: 0.875rem;
            color: var(--warning);
            margin-bottom: 0.5rem;
        }
        .demo-link {
            word-break: break-all;
            font-family: monospace;
            font-size: 0.75rem;
            background: white;
            padding: 0.5rem;
            border-radius: 0.25rem;
            display: block;
            margin-top: 0.5rem;
        }
        .progress-steps {
            display: flex;
            justify-content: center;
            gap: 0.5rem;
            margin-bottom: 2rem;
        }
        .step {
            width: 12px;
            height: 12px;
            border-radius: 50%;
            background: var(--gray-300);
        }
        .step.active {
            background: var(--primary);
        }
        .step.completed {
            background: var(--success);
        }
    </style>
</head>
<body>
    <div class="verify-container">
        <div class="verify-box">
            <div class="progress-steps">
                <div class="step completed"></div>
                <div class="step active"></div>
                <div class="step"></div>
                <div class="step"></div>
            </div>

            <?php if ($verified): ?>
            <div class="verify-icon">✅</div>
            <h1>E-Mail verifiziert!</h1>
            <p>Ihre E-Mail-Adresse wurde erfolgreich bestätigt.</p>
            <a href="/onboarding/stream.php" class="btn btn-primary" style="width: 100%;">
                Weiter zur Stream-Konfiguration
            </a>
            <?php else: ?>
            <div class="verify-icon">📧</div>
            <h1>E-Mail bestätigen</h1>
            <p>
                Wir haben eine Bestätigungs-E-Mail an<br>
                <span class="email-highlight"><?php echo htmlspecialchars($user['email'] ?? ''); ?></span><br>
                gesendet.
            </p>

            <?php if ($message): ?>
            <div class="alert alert-success"><?php echo htmlspecialchars($message); ?></div>
            <?php endif; ?>

            <?php if ($error): ?>
            <div class="alert alert-error"><?php echo htmlspecialchars($error); ?></div>
            <?php endif; ?>

            <?php if ($demoToken): ?>
            <div class="demo-box">
                <h4>⚠️ Demo-Modus</h4>
                <p style="font-size: 0.875rem; margin: 0;">In der Produktion würde eine E-Mail gesendet. Für Demo-Zwecke:</p>
                <a href="/onboarding/verify.php?token=<?php echo urlencode($demoToken); ?>" class="demo-link">
                    Klicken Sie hier um zu verifizieren
                </a>
            </div>
            <?php endif; ?>

            <p style="margin-top: 1.5rem; color: var(--gray-500); font-size: 0.875rem;">
                Keine E-Mail erhalten?
            </p>

            <form method="POST" action="" style="display: inline;">
                <button type="submit" name="resend" class="btn btn-secondary">
                    Erneut senden
                </button>
            </form>
            <?php endif; ?>

            <p style="margin-top: 2rem;">
                <a href="/dashboard/logout.php" style="color: var(--gray-500); font-size: 0.875rem;">
                    Abmelden
                </a>
            </p>
        </div>
    </div>
</body>
</html>
