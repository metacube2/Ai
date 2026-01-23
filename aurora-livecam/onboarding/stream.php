<?php
/**
 * Onboarding - Stream Konfiguration (Schritt 3)
 */

require_once dirname(__DIR__) . '/vendor/autoload.php';
require_once dirname(__DIR__) . '/SettingsManager.php';

if (file_exists(dirname(__DIR__) . '/src/bootstrap.php')) {
    require_once dirname(__DIR__) . '/src/bootstrap.php';
}

use AuroraLivecam\Auth\AuthManager;
use AuroraLivecam\Onboarding\OnboardingManager;
use AuroraLivecam\Onboarding\StreamValidator;

$settingsManager = new SettingsManager();
$auth = new AuthManager();

// Login prüfen
if (!$auth->isLoggedIn()) {
    header('Location: /onboarding/register.php');
    exit;
}

$user = $auth->getUser();
$tenantId = $user['tenant_id'] ?? 0;

$error = '';
$streamUrl = '';
$streamType = 'hls';
$validationResult = null;

// Formular verarbeiten
if ($_SERVER['REQUEST_METHOD'] === 'POST') {
    $streamUrl = trim($_POST['stream_url'] ?? '');
    $streamType = $_POST['stream_type'] ?? 'hls';

    if (empty($streamUrl)) {
        $error = 'Bitte geben Sie eine Stream-URL ein';
    } else {
        try {
            // Stream validieren
            $validator = new StreamValidator();
            $validationResult = $validator->validate($streamUrl);

            if ($validationResult['valid']) {
                // Speichern
                $onboarding = new OnboardingManager();
                $result = $onboarding->saveStream($tenantId, $streamUrl, $streamType);

                if ($result['success']) {
                    header('Location: /onboarding/branding.php');
                    exit;
                } else {
                    $error = $result['error'];
                }
            } else {
                $error = $validationResult['error'] ?? 'Stream-URL konnte nicht validiert werden';
            }
        } catch (\Exception $e) {
            $error = 'Fehler: ' . $e->getMessage();
        }
    }
}

// Skip erlauben
if (isset($_GET['skip'])) {
    header('Location: /onboarding/branding.php');
    exit;
}
?>
<!DOCTYPE html>
<html lang="de">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Stream einrichten - Aurora Livecam</title>
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
        .onboarding-header {
            text-align: center;
            margin-bottom: 2rem;
        }
        .onboarding-header h1 {
            font-size: 1.5rem;
            margin-bottom: 0.5rem;
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
        .validation-result {
            margin-top: 1rem;
            padding: 1rem;
            border-radius: 0.5rem;
        }
        .validation-success {
            background: #c6f6d5;
            border: 1px solid #9ae6b4;
        }
        .validation-error {
            background: #fed7d7;
            border: 1px solid #feb2b2;
        }
        .validation-details {
            font-size: 0.875rem;
            margin-top: 0.5rem;
            color: var(--gray-600);
        }
        .stream-types {
            display: grid;
            grid-template-columns: repeat(2, 1fr);
            gap: 1rem;
            margin-bottom: 1.5rem;
        }
        .stream-type-card {
            border: 2px solid var(--gray-200);
            border-radius: 0.5rem;
            padding: 1rem;
            cursor: pointer;
            transition: all 0.2s;
        }
        .stream-type-card:hover {
            border-color: var(--primary);
        }
        .stream-type-card.selected {
            border-color: var(--primary);
            background: rgba(102, 126, 234, 0.05);
        }
        .stream-type-card input {
            display: none;
        }
        .stream-type-card h4 {
            margin: 0 0 0.25rem 0;
            font-size: 1rem;
        }
        .stream-type-card p {
            margin: 0;
            font-size: 0.75rem;
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
                <div class="step active"></div>
                <div class="step"></div>
            </div>

            <div class="onboarding-header">
                <h1>📹 Stream einrichten</h1>
                <p style="color: var(--gray-500);">Verbinden Sie Ihre Webcam oder Ihren Stream</p>
            </div>

            <?php if ($error): ?>
            <div class="alert alert-error"><?php echo htmlspecialchars($error); ?></div>
            <?php endif; ?>

            <form method="POST" action="" id="stream-form">
                <div class="form-group">
                    <label class="form-label">Stream-Typ wählen</label>
                    <div class="stream-types">
                        <label class="stream-type-card <?php echo $streamType === 'hls' ? 'selected' : ''; ?>">
                            <input type="radio" name="stream_type" value="hls" <?php echo $streamType === 'hls' ? 'checked' : ''; ?>>
                            <h4>🎬 HLS Stream</h4>
                            <p>.m3u8 Playlist (empfohlen)</p>
                        </label>
                        <label class="stream-type-card <?php echo $streamType === 'rtmp' ? 'selected' : ''; ?>">
                            <input type="radio" name="stream_type" value="rtmp" <?php echo $streamType === 'rtmp' ? 'checked' : ''; ?>>
                            <h4>📡 RTMP</h4>
                            <p>Real-Time Messaging Protocol</p>
                        </label>
                        <label class="stream-type-card <?php echo $streamType === 'iframe' ? 'selected' : ''; ?>">
                            <input type="radio" name="stream_type" value="iframe" <?php echo $streamType === 'iframe' ? 'checked' : ''; ?>>
                            <h4>🖼️ Embed</h4>
                            <p>YouTube, Vimeo, Twitch</p>
                        </label>
                        <label class="stream-type-card <?php echo $streamType === 'webrtc' ? 'selected' : ''; ?>">
                            <input type="radio" name="stream_type" value="webrtc" <?php echo $streamType === 'webrtc' ? 'checked' : ''; ?>>
                            <h4>⚡ WebRTC</h4>
                            <p>Ultra-niedrige Latenz</p>
                        </label>
                    </div>
                </div>

                <div class="form-group">
                    <label class="form-label" for="stream_url">Stream-URL</label>
                    <input type="url" id="stream_url" name="stream_url" class="form-input"
                           value="<?php echo htmlspecialchars($streamUrl); ?>"
                           placeholder="https://example.com/stream.m3u8" required>
                    <p class="form-help">Die vollständige URL zu Ihrem Stream</p>
                </div>

                <?php if ($validationResult): ?>
                <div class="validation-result <?php echo $validationResult['valid'] ? 'validation-success' : 'validation-error'; ?>">
                    <strong><?php echo $validationResult['valid'] ? '✓ Stream erreichbar' : '✗ Stream nicht erreichbar'; ?></strong>
                    <?php if (!empty($validationResult['details'])): ?>
                    <div class="validation-details">
                        <?php if (isset($validationResult['details']['detected_type'])): ?>
                        Erkannter Typ: <?php echo htmlspecialchars($validationResult['details']['detected_type']); ?>
                        <?php endif; ?>
                    </div>
                    <?php endif; ?>
                </div>
                <?php endif; ?>

                <button type="submit" class="btn btn-primary" style="width: 100%; margin-top: 1.5rem;">
                    Stream testen & weiter
                </button>
            </form>

            <a href="?skip=1" class="skip-link">
                Später einrichten →
            </a>
        </div>
    </div>

    <script>
    document.querySelectorAll('.stream-type-card').forEach(card => {
        card.addEventListener('click', () => {
            document.querySelectorAll('.stream-type-card').forEach(c => c.classList.remove('selected'));
            card.classList.add('selected');
        });
    });
    </script>
</body>
</html>
