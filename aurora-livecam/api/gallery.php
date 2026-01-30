<?php
/**
 * Gallery API
 *
 * GET /api/gallery.php - Liste alle Galerie-Bilder
 * GET /api/gallery.php?date=2024-01-30 - Bilder eines bestimmten Datums
 * GET /api/gallery.php?from=2024-01-01&to=2024-01-31 - Bilder in einem Zeitraum
 */

require_once dirname(__DIR__) . '/vendor/autoload.php';
require_once dirname(__DIR__) . '/SettingsManager.php';

header('Content-Type: application/json');
header('Access-Control-Allow-Origin: *');

$settingsManager = new SettingsManager();

$galleryDir = dirname(__DIR__) . '/gallery/auto/';

// Prüfe ob Galerie existiert
if (!is_dir($galleryDir)) {
    echo json_encode(['success' => true, 'images' => [], 'total' => 0]);
    exit;
}

// Parameter
$date = $_GET['date'] ?? null;
$from = $_GET['from'] ?? null;
$to = $_GET['to'] ?? null;
$limit = min(100, (int)($_GET['limit'] ?? 50));
$offset = max(0, (int)($_GET['offset'] ?? 0));

// Alle Bilder holen
$allFiles = glob($galleryDir . 'auto_*.jpg');
rsort($allFiles); // Neueste zuerst

$images = [];

foreach ($allFiles as $file) {
    $filename = basename($file);
    // Extrahiere Datum aus Dateinamen: auto_2024-01-30_14-30-00.jpg
    if (preg_match('/auto_(\d{4}-\d{2}-\d{2})_(\d{2}-\d{2}-\d{2})\.jpg/', $filename, $matches)) {
        $fileDate = $matches[1];
        $fileTime = str_replace('-', ':', $matches[2]);

        // Datumsfilter
        if ($date !== null && $fileDate !== $date) {
            continue;
        }

        if ($from !== null && $fileDate < $from) {
            continue;
        }

        if ($to !== null && $fileDate > $to) {
            continue;
        }

        $images[] = [
            'filename' => $filename,
            'path' => '/gallery/auto/' . $filename,
            'date' => $fileDate,
            'time' => $fileTime,
            'datetime' => $fileDate . ' ' . $fileTime,
            'timestamp' => strtotime($fileDate . ' ' . $fileTime),
            'size' => filesize($file)
        ];
    }
}

$total = count($images);

// Pagination
$images = array_slice($images, $offset, $limit);

// Verfügbare Daten (für Kalender/Filter)
$availableDates = [];
foreach (glob($galleryDir . 'auto_*.jpg') as $file) {
    if (preg_match('/auto_(\d{4}-\d{2}-\d{2})/', basename($file), $m)) {
        $availableDates[$m[1]] = ($availableDates[$m[1]] ?? 0) + 1;
    }
}
krsort($availableDates);

echo json_encode([
    'success' => true,
    'images' => $images,
    'total' => $total,
    'offset' => $offset,
    'limit' => $limit,
    'available_dates' => $availableDates,
    'filters' => [
        'date' => $date,
        'from' => $from,
        'to' => $to
    ]
]);
