<?php
/**
 * Video Search API
 *
 * Suche nach Videos nach Datum und Uhrzeit
 *
 * GET /api/video-search.php?date=2024-01-30
 * GET /api/video-search.php?date=2024-01-30&time=14:30
 * GET /api/video-search.php?from=2024-01-01&to=2024-01-31
 * GET /api/video-search.php?time_from=08:00&time_to=18:00
 */

require_once dirname(__DIR__) . '/vendor/autoload.php';
require_once dirname(__DIR__) . '/SettingsManager.php';

header('Content-Type: application/json');
header('Access-Control-Allow-Origin: *');

$settingsManager = new SettingsManager();

$videoDir = dirname(__DIR__) . '/videos/';
$aiDir = dirname(__DIR__) . '/ai/';

// Parameter
$date = $_GET['date'] ?? null; // Format: YYYY-MM-DD
$time = $_GET['time'] ?? null; // Format: HH:MM
$fromDate = $_GET['from'] ?? null;
$toDate = $_GET['to'] ?? null;
$timeFrom = $_GET['time_from'] ?? null;
$timeTo = $_GET['time_to'] ?? null;
$type = $_GET['type'] ?? 'all'; // all, daily, ai
$aiCategory = $_GET['ai_category'] ?? null;
$limit = min(100, (int)($_GET['limit'] ?? 50));

$results = [
    'daily_videos' => [],
    'ai_videos' => [],
    'gallery_images' => []
];

// AI-Kategorien
$aiCategories = ['sunny', 'rainy', 'snowy', 'planes', 'birds', 'sunset', 'sunrise', 'rainbow'];

// === TAGESVIDEOS SUCHEN ===
if ($type === 'all' || $type === 'daily') {
    $pattern = $videoDir . 'daily_video_*.mp4';
    $dailyVideos = glob($pattern);

    foreach ($dailyVideos as $video) {
        $filename = basename($video);

        // Extrahiere Datum aus Dateinamen: daily_video_YYYYMMDD_HHMMSS.mp4
        if (preg_match('/daily_video_(\d{4})(\d{2})(\d{2})_(\d{2})(\d{2})(\d{2})\.mp4/', $filename, $matches)) {
            $videoDate = $matches[1] . '-' . $matches[2] . '-' . $matches[3];
            $videoTime = $matches[4] . ':' . $matches[5];
            $videoDateTime = $videoDate . ' ' . $videoTime . ':' . $matches[6];

            // Datumsfilter
            if ($date !== null && $videoDate !== $date) {
                continue;
            }

            if ($fromDate !== null && $videoDate < $fromDate) {
                continue;
            }

            if ($toDate !== null && $videoDate > $toDate) {
                continue;
            }

            // Uhrzeitfilter
            if ($timeFrom !== null && $videoTime < $timeFrom) {
                continue;
            }

            if ($timeTo !== null && $videoTime > $timeTo) {
                continue;
            }

            // Spezifische Uhrzeit (mit 30 Min Toleranz)
            if ($time !== null) {
                $searchMinutes = intval(substr($time, 0, 2)) * 60 + intval(substr($time, 3, 2));
                $videoMinutes = intval($matches[4]) * 60 + intval($matches[5]);

                if (abs($searchMinutes - $videoMinutes) > 30) {
                    continue;
                }
            }

            $results['daily_videos'][] = [
                'type' => 'daily',
                'filename' => $filename,
                'path' => '/videos/' . $filename,
                'date' => $videoDate,
                'time' => $videoTime,
                'datetime' => $videoDateTime,
                'timestamp' => strtotime($videoDateTime),
                'size' => filesize($video),
                'size_mb' => round(filesize($video) / (1024 * 1024), 2)
            ];
        }
    }
}

// === AI-VIDEOS SUCHEN ===
if ($type === 'all' || $type === 'ai') {
    $searchCategories = $aiCategory ? [$aiCategory] : $aiCategories;

    foreach ($searchCategories as $category) {
        $categoryDir = $aiDir . $category . '/';
        if (!is_dir($categoryDir)) continue;

        $pattern = $categoryDir . $category . '_*.mp4';
        $aiVideos = glob($pattern);

        foreach ($aiVideos as $video) {
            $filename = basename($video);

            // Extrahiere Datum aus Dateinamen: category_YYYYMMDD_HHMMSS.mp4
            if (preg_match('/' . $category . '_(\d{4})(\d{2})(\d{2})_?(\d{2})?(\d{2})?(\d{2})?\.mp4/', $filename, $matches)) {
                $videoDate = $matches[1] . '-' . $matches[2] . '-' . $matches[3];
                $videoTime = isset($matches[4]) ? ($matches[4] . ':' . ($matches[5] ?? '00')) : '00:00';
                $videoDateTime = $videoDate . ' ' . $videoTime;

                // Datumsfilter
                if ($date !== null && $videoDate !== $date) {
                    continue;
                }

                if ($fromDate !== null && $videoDate < $fromDate) {
                    continue;
                }

                if ($toDate !== null && $videoDate > $toDate) {
                    continue;
                }

                // Uhrzeitfilter
                if ($timeFrom !== null && $videoTime < $timeFrom) {
                    continue;
                }

                if ($timeTo !== null && $videoTime > $timeTo) {
                    continue;
                }

                $results['ai_videos'][] = [
                    'type' => 'ai',
                    'category' => $category,
                    'filename' => $filename,
                    'path' => '/ai/' . $category . '/' . $filename,
                    'date' => $videoDate,
                    'time' => $videoTime,
                    'datetime' => $videoDateTime,
                    'timestamp' => strtotime($videoDateTime),
                    'size' => filesize($video),
                    'size_mb' => round(filesize($video) / (1024 * 1024), 2)
                ];
            }
        }
    }
}

// Sortieren nach Datum/Zeit (neueste zuerst)
usort($results['daily_videos'], fn($a, $b) => $b['timestamp'] - $a['timestamp']);
usort($results['ai_videos'], fn($a, $b) => $b['timestamp'] - $a['timestamp']);

// Limit anwenden
$results['daily_videos'] = array_slice($results['daily_videos'], 0, $limit);
$results['ai_videos'] = array_slice($results['ai_videos'], 0, $limit);

// Statistiken
$results['stats'] = [
    'total_daily' => count($results['daily_videos']),
    'total_ai' => count($results['ai_videos']),
    'total' => count($results['daily_videos']) + count($results['ai_videos'])
];

$results['filters'] = [
    'date' => $date,
    'time' => $time,
    'from' => $fromDate,
    'to' => $toDate,
    'time_from' => $timeFrom,
    'time_to' => $timeTo,
    'type' => $type,
    'ai_category' => $aiCategory
];

$results['success'] = true;

echo json_encode($results, JSON_PRETTY_PRINT);
