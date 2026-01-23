<?php
/**
 * Onboarding - Abgeschlossen
 */

require_once dirname(__DIR__) . '/vendor/autoload.php';
require_once dirname(__DIR__) . '/SettingsManager.php';

if (file_exists(dirname(__DIR__) . '/src/bootstrap.php')) {
    require_once dirname(__DIR__) . '/src/bootstrap.php';
}

use AuroraLivecam\Auth\AuthManager;
use AuroraLivecam\Onboarding\OnboardingManager;
use AuroraLivecam\Core\Database;

$settingsManager = new SettingsManager();
$auth = new AuthManager();

if (!$auth->isLoggedIn()) {
    header('Location: /onboarding/register.php');
    exit;
}

$user = $auth->getUser();
$tenantId = $user['tenant_id'] ?? 0;

// Onboarding abschliessen
try {
    $onboarding = new OnboardingManager();
    $onboarding->complete($tenantId);
} catch (\Exception $e) {
    // Ignorieren wenn DB nicht verfügbar
}

// Tenant-Info laden
$tenantSlug = 'demo';
$subdomain = '';

try {
    $db = Database::getInstance();
    $tenant = $db->fetchOne("SELECT slug FROM tenants WHERE id = ?", [$tenantId]);
    if ($tenant) {
        $tenantSlug = $tenant['slug'];
        $subdomain = $tenantSlug . '.aurora-livecam.com';
    }
} catch (\Exception $e) {
    // Fallback
}
?>
<!DOCTYPE html>
<html lang="de">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Fertig! - Aurora Livecam</title>
    <link rel="stylesheet" href="/dashboard/assets/dashboard.css">
    <style>
        .complete-container {
            min-height: 100vh;
            display: flex;
            align-items: center;
            justify-content: center;
            background: linear-gradient(135deg, var(--primary) 0%, var(--secondary) 100%);
            padding: 2rem;
        }
        .complete-box {
            background: var(--white);
            padding: 3rem;
            border-radius: 1rem;
            box-shadow: 0 25px 50px -12px rgba(0, 0, 0, 0.25);
            width: 100%;
            max-width: 600px;
            text-align: center;
        }
        .complete-icon {
            font-size: 5rem;
            margin-bottom: 1.5rem;
            animation: bounce 0.5s ease;
        }
        @keyframes bounce {
            0%, 100% { transform: translateY(0); }
            50% { transform: translateY(-10px); }
        }
        .complete-box h1 {
            font-size: 2rem;
            margin-bottom: 1rem;
            color: var(--success);
        }
        .complete-box p {
            color: var(--gray-600);
            margin-bottom: 2rem;
            font-size: 1.1rem;
        }
        .url-box {
            background: var(--gray-100);
            border-radius: 0.5rem;
            padding: 1rem;
            margin-bottom: 2rem;
        }
        .url-box label {
            display: block;
            font-size: 0.875rem;
            color: var(--gray-500);
            margin-bottom: 0.5rem;
        }
        .url-box .url {
            font-family: monospace;
            font-size: 1rem;
            color: var(--primary);
            word-break: break-all;
        }
        .action-buttons {
            display: flex;
            gap: 1rem;
            justify-content: center;
            flex-wrap: wrap;
        }
        .next-steps {
            margin-top: 2.5rem;
            text-align: left;
            background: var(--gray-50);
            border-radius: 0.5rem;
            padding: 1.5rem;
        }
        .next-steps h3 {
            font-size: 1rem;
            margin-bottom: 1rem;
            color: var(--gray-700);
        }
        .next-steps ul {
            list-style: none;
            padding: 0;
            margin: 0;
        }
        .next-steps li {
            padding: 0.5rem 0;
            padding-left: 1.5rem;
            position: relative;
            color: var(--gray-600);
        }
        .next-steps li::before {
            content: '→';
            position: absolute;
            left: 0;
            color: var(--primary);
        }
        .confetti {
            position: fixed;
            top: 0;
            left: 0;
            width: 100%;
            height: 100%;
            pointer-events: none;
            overflow: hidden;
            z-index: 1000;
        }
        .confetti-piece {
            position: absolute;
            width: 10px;
            height: 10px;
            background: var(--primary);
            animation: confetti-fall 3s ease-out forwards;
        }
        @keyframes confetti-fall {
            0% { transform: translateY(-100px) rotate(0deg); opacity: 1; }
            100% { transform: translateY(100vh) rotate(720deg); opacity: 0; }
        }
    </style>
</head>
<body>
    <div class="confetti" id="confetti"></div>

    <div class="complete-container">
        <div class="complete-box">
            <div class="complete-icon">🎉</div>
            <h1>Herzlichen Glückwunsch!</h1>
            <p>Ihre Livecam ist jetzt eingerichtet und bereit.</p>

            <?php if ($subdomain): ?>
            <div class="url-box">
                <label>Ihre Livecam-Adresse:</label>
                <div class="url">https://<?php echo htmlspecialchars($subdomain); ?></div>
            </div>
            <?php endif; ?>

            <div class="action-buttons">
                <a href="/dashboard/" class="btn btn-primary">
                    Zum Dashboard
                </a>
                <a href="/" class="btn btn-secondary" target="_blank">
                    Livecam ansehen
                </a>
            </div>

            <div class="next-steps">
                <h3>Nächste Schritte</h3>
                <ul>
                    <li>Stream-URL im Dashboard anpassen (falls noch nicht geschehen)</li>
                    <li>Logo und Farben im Branding-Bereich hochladen</li>
                    <li>Wetter-Widget konfigurieren</li>
                    <li>Eigene Domain verbinden (optional)</li>
                    <?php if ($settingsManager->isBillingEnabled()): ?>
                    <li>Abo auswählen für mehr Funktionen</li>
                    <?php endif; ?>
                </ul>
            </div>
        </div>
    </div>

    <script>
    // Confetti Animation
    function createConfetti() {
        const container = document.getElementById('confetti');
        const colors = ['#667eea', '#764ba2', '#f093fb', '#48bb78', '#ed8936'];

        for (let i = 0; i < 50; i++) {
            const piece = document.createElement('div');
            piece.className = 'confetti-piece';
            piece.style.left = Math.random() * 100 + '%';
            piece.style.background = colors[Math.floor(Math.random() * colors.length)];
            piece.style.animationDelay = Math.random() * 2 + 's';
            piece.style.width = (Math.random() * 10 + 5) + 'px';
            piece.style.height = piece.style.width;
            container.appendChild(piece);
        }

        // Cleanup after animation
        setTimeout(() => {
            container.innerHTML = '';
        }, 5000);
    }

    createConfetti();
    </script>
</body>
</html>
