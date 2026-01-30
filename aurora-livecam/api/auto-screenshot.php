<?php
/**
 * Auto-Screenshot API
 *
 * Kann als Cron-Job aufgerufen werden:
 * Beispiel: 0,10,20,30,40,50 * * * * curl -s http://localhost/api/auto-screenshot.php?key=YOUR_SECRET_KEY
 *
 * Oder via Webhook/Timer
 */

require_once dirname(__DIR__) . '/vendor/autoload.php';
require_once dirname(__DIR__) . '/SettingsManager.php';

header('Content-Type: application/json');

$settingsManager = new SettingsManager();

// Prüfe ob Feature aktiviert
if (!$settingsManager->isAutoScreenshotEnabled()) {
    echo json_encode(['success' => false, 'error' => 'Auto-Screenshot deaktiviert']);
    exit;
}

// Optionale API-Key Validierung
$configFile = dirname(__DIR__) . '/config.php';
if (file_exists($configFile)) {
    $config = require $configFile;
    $apiKey = $config['auto_screenshot_key'] ?? '';

    if (!empty($apiKey) && ($_GET['key'] ?? '') !== $apiKey) {
        http_response_code(403);
        echo json_encode(['success' => false, 'error' => 'Ungültiger API-Key']);
        exit;
    }
}

// Galerie-Verzeichnis erstellen
$galleryDir = dirname(__DIR__) . '/gallery/auto/';
if (!is_dir($galleryDir)) {
    mkdir($galleryDir, 0755, true);
}

// Screenshot-Dateiname
$filename = 'auto_' . date('Y-m-d_H-i-s') . '.jpg';
$filepath = $galleryDir . $filename;

// Video-Stream URL
$streamUrl = 'test_video.m3u8';
$logoPath = dirname(__DIR__) . '/logo.png';

// FFmpeg-Befehl zum Erstellen des Screenshots
$command = sprintf(
    'ffmpeg -i %s -vframes 1 -q:v 2 %s 2>&1',
    escapeshellarg($streamUrl),
    escapeshellarg($filepath)
);

exec($command, $output, $returnVar);

if ($returnVar !== 0 || !file_exists($filepath)) {
    echo json_encode([
        'success' => false,
        'error' => 'Screenshot fehlgeschlagen',
        'command' => $command,
        'output' => implode("\n", $output)
    ]);
    exit;
}

// Alte Screenshots aufräumen (max. Anzahl einhalten)
$maxImages = $settingsManager->getAutoScreenshotMaxImages();
$existingFiles = glob($galleryDir . 'auto_*.jpg');
rsort($existingFiles); // Neueste zuerst

if (count($existingFiles) > $maxImages) {
    $filesToDelete = array_slice($existingFiles, $maxImages);
    foreach ($filesToDelete as $file) {
        @unlink($file);
    }
}

// Metadaten speichern
$metaFile = $galleryDir . 'metadata.json';
$metadata = [];
if (file_exists($metaFile)) {
    $metadata = json_decode(file_get_contents($metaFile), true) ?? [];
}

$metadata[$filename] = [
    'created_at' => date('Y-m-d H:i:s'),
    'timestamp' => time(),
    'size' => filesize($filepath)
];

// Nur die letzten maxImages behalten
$metadata = array_slice($metadata, -$maxImages, null, true);
file_put_contents($metaFile, json_encode($metadata, JSON_PRETTY_PRINT));

echo json_encode([
    'success' => true,
    'file' => $filename,
    'path' => '/gallery/auto/' . $filename,
    'total_images' => count(glob($galleryDir . 'auto_*.jpg'))
]);
