<?php
/**
 * Share API - Teilen von Bildern/Videos per E-Mail
 *
 * POST /api/share.php
 * Body: { email: "friend@example.com", type: "video|image", path: "/videos/...", message: "Schau dir das an!" }
 */

use PHPMailer\PHPMailer\PHPMailer;
use PHPMailer\PHPMailer\Exception;

require_once dirname(__DIR__) . '/vendor/autoload.php';
require_once dirname(__DIR__) . '/SettingsManager.php';

header('Content-Type: application/json');
header('Access-Control-Allow-Origin: *');
header('Access-Control-Allow-Methods: POST, GET, OPTIONS');
header('Access-Control-Allow-Headers: Content-Type');

if ($_SERVER['REQUEST_METHOD'] === 'OPTIONS') {
    http_response_code(200);
    exit;
}

$settingsManager = new SettingsManager();

// Prüfe ob Feature aktiviert
if (!$settingsManager->isEmailSharingEnabled()) {
    echo json_encode(['success' => false, 'error' => 'E-Mail-Sharing ist deaktiviert']);
    exit;
}

// Config laden
$configFile = dirname(__DIR__) . '/config.php';
$config = file_exists($configFile) ? require $configFile : [];
$mailConfig = $config['mail'] ?? [];

if (empty($mailConfig['host']) || empty($mailConfig['username'])) {
    echo json_encode(['success' => false, 'error' => 'E-Mail-Server nicht konfiguriert']);
    exit;
}

// === GET: Share-Link generieren ===
if ($_SERVER['REQUEST_METHOD'] === 'GET' && isset($_GET['generate'])) {
    $path = $_GET['path'] ?? '';
    $type = $_GET['type'] ?? 'video';

    if (empty($path)) {
        echo json_encode(['success' => false, 'error' => 'Kein Pfad angegeben']);
        exit;
    }

    // Token generieren
    $expiryHours = $settingsManager->getShareLinkExpiryHours();
    $expiry = time() + ($expiryHours * 3600);
    $token = hash_hmac('sha256', $path . $expiry, session_id() . 'share_secret');

    // Share-Link speichern
    $shareDir = dirname(__DIR__) . '/data/shares/';
    if (!is_dir($shareDir)) {
        mkdir($shareDir, 0755, true);
    }

    $shareId = bin2hex(random_bytes(16));
    $shareData = [
        'id' => $shareId,
        'path' => $path,
        'type' => $type,
        'token' => $token,
        'expiry' => $expiry,
        'created_at' => date('Y-m-d H:i:s')
    ];

    file_put_contents($shareDir . $shareId . '.json', json_encode($shareData));

    // URL generieren
    $baseUrl = (isset($_SERVER['HTTPS']) && $_SERVER['HTTPS'] === 'on' ? 'https' : 'http')
        . '://' . $_SERVER['HTTP_HOST'];
    $shareUrl = $baseUrl . '/api/share.php?view=' . $shareId;

    echo json_encode([
        'success' => true,
        'share_url' => $shareUrl,
        'share_id' => $shareId,
        'expires_at' => date('Y-m-d H:i:s', $expiry)
    ]);
    exit;
}

// === GET: Share-Link anzeigen ===
if ($_SERVER['REQUEST_METHOD'] === 'GET' && isset($_GET['view'])) {
    $shareId = preg_replace('/[^a-f0-9]/', '', $_GET['view']);
    $shareFile = dirname(__DIR__) . '/data/shares/' . $shareId . '.json';

    if (!file_exists($shareFile)) {
        header('Content-Type: text/html; charset=utf-8');
        echo '<!DOCTYPE html><html><head><title>Link ungültig</title></head><body style="font-family:sans-serif;text-align:center;padding:50px;"><h1>❌ Link nicht gefunden</h1><p>Dieser Share-Link existiert nicht oder wurde bereits gelöscht.</p></body></html>';
        exit;
    }

    $shareData = json_decode(file_get_contents($shareFile), true);

    // Ablauf prüfen
    if (time() > $shareData['expiry']) {
        @unlink($shareFile);
        header('Content-Type: text/html; charset=utf-8');
        echo '<!DOCTYPE html><html><head><title>Link abgelaufen</title></head><body style="font-family:sans-serif;text-align:center;padding:50px;"><h1>⏰ Link abgelaufen</h1><p>Dieser Share-Link ist abgelaufen. Bitte fordere einen neuen Link an.</p></body></html>';
        exit;
    }

    // Datei existiert?
    $filePath = dirname(__DIR__) . $shareData['path'];
    if (!file_exists($filePath)) {
        header('Content-Type: text/html; charset=utf-8');
        echo '<!DOCTYPE html><html><head><title>Datei nicht gefunden</title></head><body style="font-family:sans-serif;text-align:center;padding:50px;"><h1>📭 Datei nicht gefunden</h1><p>Die geteilte Datei existiert nicht mehr.</p></body></html>';
        exit;
    }

    // Redirect zur Datei oder HTML-Seite mit eingebettetem Player
    $isVideo = in_array(pathinfo($filePath, PATHINFO_EXTENSION), ['mp4', 'webm', 'mov']);
    $isImage = in_array(pathinfo($filePath, PATHINFO_EXTENSION), ['jpg', 'jpeg', 'png', 'gif', 'webp']);

    $siteName = $config['app']['name'] ?? 'Aurora Livecam';
    $baseUrl = (isset($_SERVER['HTTPS']) && $_SERVER['HTTPS'] === 'on' ? 'https' : 'http')
        . '://' . $_SERVER['HTTP_HOST'];

    header('Content-Type: text/html; charset=utf-8');
    echo '<!DOCTYPE html>
<html lang="de">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Geteilte ' . ($isVideo ? 'Video' : 'Bild') . ' - ' . htmlspecialchars($siteName) . '</title>
    <style>
        * { margin: 0; padding: 0; box-sizing: border-box; }
        body {
            font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, sans-serif;
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            min-height: 100vh;
            display: flex;
            flex-direction: column;
            align-items: center;
            justify-content: center;
            padding: 20px;
        }
        .container {
            background: white;
            border-radius: 16px;
            padding: 30px;
            max-width: 900px;
            width: 100%;
            box-shadow: 0 20px 60px rgba(0,0,0,0.3);
        }
        h1 { font-size: 1.5rem; margin-bottom: 20px; color: #333; }
        video, img {
            width: 100%;
            max-height: 70vh;
            object-fit: contain;
            border-radius: 8px;
            background: #000;
        }
        .download-btn {
            display: inline-block;
            margin-top: 20px;
            padding: 12px 30px;
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            color: white;
            text-decoration: none;
            border-radius: 8px;
            font-weight: 600;
        }
        .download-btn:hover { opacity: 0.9; }
        .footer {
            margin-top: 20px;
            color: rgba(255,255,255,0.8);
            font-size: 0.9rem;
        }
        .footer a { color: white; }
    </style>
</head>
<body>
    <div class="container">
        <h1>📤 Geteilte' . ($isVideo ? 's Video' : 's Bild') . '</h1>';

    if ($isVideo) {
        echo '<video controls autoplay><source src="' . htmlspecialchars($shareData['path']) . '" type="video/mp4">Ihr Browser unterstützt kein Video.</video>';
    } else {
        echo '<img src="' . htmlspecialchars($shareData['path']) . '" alt="Geteiltes Bild">';
    }

    echo '
        <a href="' . htmlspecialchars($shareData['path']) . '" download class="download-btn">⬇️ Herunterladen</a>
    </div>
    <div class="footer">
        Geteilt von <a href="' . $baseUrl . '">' . htmlspecialchars($siteName) . '</a>
    </div>
</body>
</html>';
    exit;
}

// === POST: E-Mail senden ===
if ($_SERVER['REQUEST_METHOD'] !== 'POST') {
    http_response_code(405);
    echo json_encode(['success' => false, 'error' => 'Nur POST erlaubt']);
    exit;
}

// JSON-Body parsen
$input = json_decode(file_get_contents('php://input'), true);
if (!$input) {
    $input = $_POST;
}

$email = filter_var($input['email'] ?? '', FILTER_VALIDATE_EMAIL);
$path = $input['path'] ?? '';
$type = $input['type'] ?? 'video';
$message = htmlspecialchars($input['message'] ?? '');
$senderName = htmlspecialchars($input['sender_name'] ?? 'Ein Freund');

if (!$email) {
    echo json_encode(['success' => false, 'error' => 'Ungültige E-Mail-Adresse']);
    exit;
}

if (empty($path)) {
    echo json_encode(['success' => false, 'error' => 'Kein Pfad angegeben']);
    exit;
}

// Share-Link generieren
$expiryHours = $settingsManager->getShareLinkExpiryHours();
$expiry = time() + ($expiryHours * 3600);

$shareDir = dirname(__DIR__) . '/data/shares/';
if (!is_dir($shareDir)) {
    mkdir($shareDir, 0755, true);
}

$shareId = bin2hex(random_bytes(16));
$shareData = [
    'id' => $shareId,
    'path' => $path,
    'type' => $type,
    'expiry' => $expiry,
    'created_at' => date('Y-m-d H:i:s'),
    'shared_to' => $email
];

file_put_contents($shareDir . $shareId . '.json', json_encode($shareData));

$baseUrl = (isset($_SERVER['HTTPS']) && $_SERVER['HTTPS'] === 'on' ? 'https' : 'http')
    . '://' . $_SERVER['HTTP_HOST'];
$shareUrl = $baseUrl . '/api/share.php?view=' . $shareId;
$siteName = $config['app']['name'] ?? 'Aurora Livecam';

// E-Mail senden
try {
    $mail = new PHPMailer(true);

    // SMTP Konfiguration
    $mail->isSMTP();
    $mail->Host = $mailConfig['host'];
    $mail->SMTPAuth = true;
    $mail->Username = $mailConfig['username'];
    $mail->Password = $mailConfig['password'];
    $mail->SMTPSecure = PHPMailer::ENCRYPTION_STARTTLS;
    $mail->Port = $mailConfig['port'] ?? 587;
    $mail->CharSet = 'UTF-8';

    // Absender/Empfänger
    $mail->setFrom($mailConfig['from_address'], $mailConfig['from_name'] ?? $siteName);
    $mail->addAddress($email);

    // Inhalt
    $mail->isHTML(true);
    $mail->Subject = $senderName . ' hat ' . ($type === 'video' ? 'ein Video' : 'ein Bild') . ' mit dir geteilt';

    $mail->Body = '
    <div style="font-family: -apple-system, BlinkMacSystemFont, \'Segoe UI\', Roboto, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px;">
        <div style="background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); padding: 30px; border-radius: 12px 12px 0 0; text-align: center;">
            <h1 style="color: white; margin: 0; font-size: 24px;">📤 ' . htmlspecialchars($siteName) . '</h1>
        </div>
        <div style="background: #f7f7f7; padding: 30px; border-radius: 0 0 12px 12px;">
            <p style="font-size: 18px; color: #333; margin-bottom: 20px;">
                <strong>' . htmlspecialchars($senderName) . '</strong> hat ' . ($type === 'video' ? 'ein Video' : 'ein Bild') . ' mit dir geteilt!
            </p>
            ' . (!empty($message) ? '<div style="background: white; padding: 15px; border-radius: 8px; border-left: 4px solid #667eea; margin-bottom: 20px;"><em>"' . nl2br($message) . '"</em></div>' : '') . '
            <a href="' . htmlspecialchars($shareUrl) . '" style="display: inline-block; background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 15px 30px; text-decoration: none; border-radius: 8px; font-weight: 600; font-size: 16px;">
                ▶️ Jetzt ansehen
            </a>
            <p style="margin-top: 20px; color: #888; font-size: 12px;">
                Dieser Link ist ' . $expiryHours . ' Stunden gültig.
            </p>
        </div>
    </div>';

    $mail->AltBody = $senderName . ' hat ' . ($type === 'video' ? 'ein Video' : 'ein Bild') . ' mit dir geteilt: ' . $shareUrl;

    $mail->send();

    echo json_encode([
        'success' => true,
        'message' => 'E-Mail wurde gesendet',
        'share_url' => $shareUrl
    ]);

} catch (Exception $e) {
    error_log('Share email error: ' . $e->getMessage());
    echo json_encode([
        'success' => false,
        'error' => 'E-Mail konnte nicht gesendet werden',
        'share_url' => $shareUrl // URL trotzdem zurückgeben
    ]);
}
