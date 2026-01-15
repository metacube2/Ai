<?php
use PHPMailer\PHPMailer\PHPMailer;
use PHPMailer\PHPMailer\Exception;

require __DIR__ . '/vendor/autoload.php';
require_once 'SettingsManager.php';

// SettingsManager initialisieren
$settingsManager = new SettingsManager();

// AJAX-Handler für Settings (VOR anderen Ausgaben!)
$settingsManager->handleAjax();

if (isset($_GET['download_video'])) {
    $videoDir = './videos/';
    $latestVideo = null;
    $latestTime = 0;

    // Finde das neueste Video
    foreach (glob($videoDir . '*.mp4') as $video) {
        $mtime = filemtime($video);
        if ($mtime > $latestTime) {
            $latestTime = $mtime;
            $latestVideo = $video;
        }
    }

    if ($latestVideo) {
        header('Content-Description: File Transfer');
        header('Content-Type: application/octet-stream');
        header('Content-Disposition: attachment; filename="'.basename($latestVideo).'"');
        header('Expires: 0');
        header('Cache-Control: must-revalidate');
        header('Pragma: public');
        header('Content-Length: ' . filesize($latestVideo));
        readfile($latestVideo);
        exit;
    } else {
        echo "Kein Video zum Herunterladen gefunden.";
        exit;
    }
}




// Funktion zur sicheren Umleitung
function safeRedirect($url) {
    if (!headers_sent()) {
        header("HTTP/1.1 301 Moved Permanently");
        header("Location: " . $url);
    } else {
        echo '<script>window.location.href="' . $url . '";</script>';
    }
    exit();
}

// Hauptlogik
$oldDomains = [
    'www.aurora-wetter-lifecam.ch',
    'www.aurora-wetter-livecam.ch'
];
$newDomain = 'www.aurora-weather-livecam.com';

if (in_array($_SERVER['HTTP_HOST'], $oldDomains)) {
    $protocol = isset($_SERVER['HTTPS']) && $_SERVER['HTTPS'] === 'on' ? 'https' : 'http';
    $newUrl = $protocol . '://' . $newDomain . $_SERVER['REQUEST_URI'];

    // Logging für Debugging
    error_log("Umleitung von {$_SERVER['HTTP_HOST']} nach $newUrl");

    if (!headers_sent()) {
        header("HTTP/1.1 301 Moved Permanently");
        header("Location: " . $newUrl);
    } else {
        echo '<script>window.location.href="' . $newUrl . '";</script>';
    }
    exit();
}





session_start();
error_reporting(E_ALL);
ini_set('display_errors', 1);
$imageDir = "./image"; // Angepasst an das Ausgabeverzeichnis des Bash-Skripts
$imageFiles = glob("$imageDir/screenshot_*.jpg");
rsort($imageFiles); // Sortiert die Dateien in umgekehrter Reihenfolge (neueste zuerst)
$imageFilesJson = json_encode($imageFiles);


class ViewerCounter {
    private $file = 'active_viewers.json';
    private $timeout = 30; // Zeit in Sekunden, bis ein User als "offline" gilt

    public function handleHeartbeat() {
        $ip = md5($_SERVER['REMOTE_ADDR'] . $_SERVER['HTTP_USER_AGENT']); // Anonymisierte ID
        $now = time();

        $viewers = [];

        // 1. Datei lesen (mit Lock für Sicherheit bei vielen Zugriffen)
        if (file_exists($this->file)) {
            $content = file_get_contents($this->file);
            $viewers = json_decode($content, true) ?? [];
        }

        // 2. Aktuellen User updaten
        $viewers[$ip] = $now;

        // 3. Alte User entfernen & Zählen
        $activeCount = 0;
        $newViewers = [];

        foreach ($viewers as $userIp => $lastSeen) {
            if ($now - $lastSeen < $this->timeout) {
                $newViewers[$userIp] = $lastSeen;
                $activeCount++;
            }
        }

        // 4. Speichern
        file_put_contents($this->file, json_encode($newViewers));

        // 5. Ergebnis zurückgeben
        header('Content-Type: application/json');
        echo json_encode(['count' => $activeCount]);
        exit;
    }

    public function getInitialCount() {
        if (file_exists($this->file)) {
            $viewers = json_decode(file_get_contents($this->file), true) ?? [];
            // Nur grob zählen, genaues Update macht das JS sofort nach Laden
            return count($viewers);
        }
        return 1; // Zumindest man selbst ist da
    }
}

// Instanz erstellen
$viewerCounter = new ViewerCounter();



class WebcamManager {
    private $videoSrc = 'test_video.m3u8';
    private $logoPath = 'logo.png';

    // Zeigt NUR das Video ohne Schnickschnack
    public function displayWebcam() {
        return '
        <video id="webcam-player"
            autoplay
            muted
            playsinline
            webkit-playsinline
            x-webkit-airplay="allow"
            x5-video-player-type="h5"
            x5-video-player-fullscreen="true"
            style="width: 100%; height: 100%; object-fit: contain;">
        </video>';
    }

    // Das ist die neue Anzeige für unten links
    public function displayStreamStats() {
        return '
        <div class="info-badge tech-stat" id="bitrate-display" style="display:none;">
            <i class="fas fa-tachometer-alt bitrate-icon"></i>
            <span>Stream: <span id="bitrate-value">0.00</span> MBit/s</span>
        </div>';
    }

    public function captureSnapshot() {
        $outputFile = 'snapshot_' . date('YmdHis') . '.jpg';
        $src = escapeshellarg($this->videoSrc);
        $logo = escapeshellarg($this->logoPath);
        $command = "ffmpeg -i {$src} -i {$logo} -filter_complex 'overlay=main_w-overlay_w-10:10' -vframes 1 -q:v 2 {$outputFile}";
        exec($command, $output, $returnVar);
        if ($returnVar !== 0) return "Fehler";

        $uploadDir = "uploads/";
        if (!file_exists($uploadDir)) mkdir($uploadDir, 0777, true);
        $uploadFile = $uploadDir . $outputFile;
        copy($outputFile, $uploadFile);

        header('Content-Type: application/octet-stream');
        header('Content-Disposition: attachment; filename="' . $outputFile . '"');
        readfile($outputFile);
        unlink($outputFile);
        exit;
    }

    public function getImageFiles() {
        $imageFiles = glob("image/screenshot_*.jpg");
        if ($imageFiles) rsort($imageFiles); else $imageFiles = [];
        return json_encode($imageFiles);
    }

    public function captureVideoSequence($duration = 10) {
        $outputFile = 'sequence_' . date('YmdHis') . '.mp4';
        $src = escapeshellarg($this->videoSrc);
        $logo = escapeshellarg($this->logoPath);
        $dur = intval($duration);
        $command = "ffmpeg -i {$src} -i {$logo} -filter_complex 'overlay=10:10' -t {$dur} -c:v libx264 -preset fast -crf 23 {$outputFile}";
        exec($command, $output, $returnVar);
        if ($returnVar !== 0) return "Fehler";

        $uploadDir = "uploads/";
        if (!file_exists($uploadDir)) mkdir($uploadDir, 0777, true);
        $uploadFile = $uploadDir . $outputFile;
        copy($outputFile, $uploadFile);

        header('Content-Type: video/mp4');
        header('Content-Disposition: attachment; filename="' . $outputFile . '"');
        readfile($outputFile);
        unlink($outputFile);
        exit;
    }

    public function getJavaScript() {
        return "
        document.addEventListener('DOMContentLoaded', function () {
            var video = document.getElementById('webcam-player');
            var videoSrc = '{$this->videoSrc}';
            var bitrateBadge = document.getElementById('bitrate-display');
            var bitrateValue = document.getElementById('bitrate-value');
            var isMobile = /iPhone|iPad|iPod|Android/i.test(navigator.userAgent);
            var isIOS = /iPhone|iPad|iPod/i.test(navigator.userAgent);

            if(video) {
                video.controls = false;
                if (isIOS) {
                    video.src = videoSrc;
                    video.setAttribute('playsinline', '');
                    video.setAttribute('webkit-playsinline', '');
                    video.muted = true;
                    if(bitrateBadge) bitrateBadge.style.display = 'none';
                    video.addEventListener('loadedmetadata', function() { video.play().catch(console.log); });
                } else if (Hls.isSupported()) {
                    var hls = new Hls({ enableWorker: !isMobile, lowLatencyMode: false });
                    hls.loadSource(videoSrc);
                    hls.attachMedia(video);
                    hls.on(Hls.Events.MANIFEST_PARSED, function () {
                        if (isMobile) video.muted = true;
                        video.play().catch(console.log);
                        if(bitrateBadge) bitrateBadge.style.display = 'inline-flex';
                    });
                    hls.on(Hls.Events.FRAG_LOADED, function(event, data) {
                        var bandwidth = hls.bandwidthEstimate;
                        if (bandwidth && !isNaN(bandwidth) && bitrateValue) {
                            var mbs = bandwidth / 8 / 1024 / 1024;
                            if (mbs > 0) bitrateValue.textContent = mbs.toFixed(2);
                        }
                    });
                }
            }
        });
        ";
    }

    public function setVideoSrc($src) { $this->videoSrc = $src; }
}



class VisualCalendarManager {
    private $videoDir;
    private $aiDir;
    private $monthNames;
    private $settingsManager;

    // AI-Kategorien mit Icons und Farben
    private $aiCategories = [
        'sunny'   => ['icon' => '☀️', 'name' => 'Sonnig', 'color' => '#FFD700'],
        'rainy'   => ['icon' => '🌧️', 'name' => 'Regen', 'color' => '#4682B4'],
        'snowy'   => ['icon' => '❄️', 'name' => 'Schnee', 'color' => '#E0FFFF'],
        'planes'  => ['icon' => '✈️', 'name' => 'Flugzeuge', 'color' => '#87CEEB'],
        'birds'   => ['icon' => '🐦', 'name' => 'Vögel', 'color' => '#98FB98'],
        'sunset'  => ['icon' => '🌅', 'name' => 'Sonnenuntergang', 'color' => '#FF6347'],
        'sunrise' => ['icon' => '🌄', 'name' => 'Sonnenaufgang', 'color' => '#FFA07A'],
        'rainbow' => ['icon' => '🌈', 'name' => 'Regenbogen', 'color' => '#FF69B4'],
    ];

    public function __construct($videoDir = './videos/', $aiDir = './ai/', $settingsManager = null) {
        $this->videoDir = $videoDir;
        $this->aiDir = $aiDir;
        $this->settingsManager = $settingsManager;
        $this->monthNames = [
            1 => ['de' => 'Januar', 'en' => 'January', 'it' => 'Gennaio', 'fr' => 'Janvier', 'zh' => '一月'],
            2 => ['de' => 'Februar', 'en' => 'February', 'it' => 'Febbraio', 'fr' => 'Février', 'zh' => '二月'],
            3 => ['de' => 'März', 'en' => 'March', 'it' => 'Marzo', 'fr' => 'Mars', 'zh' => '三月'],
            4 => ['de' => 'April', 'en' => 'April', 'it' => 'Aprile', 'fr' => 'Avril', 'zh' => '四月'],
            5 => ['de' => 'Mai', 'en' => 'May', 'it' => 'Maggio', 'fr' => 'Mai', 'zh' => '五月'],
            6 => ['de' => 'Juni', 'en' => 'June', 'it' => 'Giugno', 'fr' => 'Juin', 'zh' => '六月'],
            7 => ['de' => 'Juli', 'en' => 'July', 'it' => 'Luglio', 'fr' => 'Juillet', 'zh' => '七月'],
            8 => ['de' => 'August', 'en' => 'August', 'it' => 'Agosto', 'fr' => 'Août', 'zh' => '八月'],
            9 => ['de' => 'September', 'en' => 'September', 'it' => 'Settembre', 'fr' => 'Septembre', 'zh' => '九月'],
            10 => ['de' => 'Oktober', 'en' => 'October', 'it' => 'Ottobre', 'fr' => 'Octobre', 'zh' => '十月'],
            11 => ['de' => 'November', 'en' => 'November', 'it' => 'Novembre', 'fr' => 'Novembre', 'zh' => '十一月'],
            12 => ['de' => 'Dezember', 'en' => 'December', 'it' => 'Dicembre', 'fr' => 'Décembre', 'zh' => '十二月']
        ];
    }

    /**
     * Holt AI-Events für ein bestimmtes Datum
     */
    public function getAiEventsForDate($year, $month, $day) {
        $events = [];
        $dateStr = sprintf('%04d%02d%02d', $year, $month, $day);

        foreach ($this->aiCategories as $category => $info) {
            $categoryDir = $this->aiDir . $category . '/';
            if (!is_dir($categoryDir)) continue;

            // Suche nach Videos für dieses Datum
            $pattern = $categoryDir . "{$category}_{$dateStr}*.mp4";
            $videos = glob($pattern);

            if (!empty($videos)) {
                $events[$category] = [
                    'icon' => $info['icon'],
                    'name' => $info['name'],
                    'color' => $info['color'],
                    'videos' => $videos,
                    'count' => count($videos)
                ];
            }
        }

        return $events;
    }

    /**
     * Prüft ob AI-Events für ein Datum existieren
     */
    public function hasAiEventsForDate($year, $month, $day) {
        $dateStr = sprintf('%04d%02d%02d', $year, $month, $day);

        foreach (array_keys($this->aiCategories) as $category) {
            $pattern = $this->aiDir . $category . "/{$category}_{$dateStr}*.mp4";
            if (count(glob($pattern)) > 0) {
                return true;
            }
        }
        return false;
    }

    /**
     * Holt kurze Icon-Liste für Kalender-Anzeige
     */
    public function getAiIconsForDate($year, $month, $day) {
        $icons = [];
        $events = $this->getAiEventsForDate($year, $month, $day);

        foreach ($events as $category => $info) {
            $icons[] = $info['icon'];
        }

        return $icons;
    }

    public function getVideosForDate($year, $month, $day) {
        $videos = [];
        $dateStr = sprintf('%04d%02d%02d', $year, $month, $day);

        foreach (glob($this->videoDir . "daily_video_{$dateStr}_*.mp4") as $video) {
            $videos[] = [
                'path' => $video,
                'filename' => basename($video),
                'filesize' => filesize($video),
                'time' => date('H:i', filemtime($video))
            ];
        }

        return $videos;
    }

    public function hasVideosForDate($year, $month, $day) {
        $dateStr = sprintf('%04d%02d%02d', $year, $month, $day);
        $pattern = $this->videoDir . "daily_video_{$dateStr}_*.mp4";
        return count(glob($pattern)) > 0;
    }

    public function displayVisualCalendar() {
        $currentYear = isset($_GET['cal_year']) ? intval($_GET['cal_year']) : date('Y');
        $currentMonth = isset($_GET['cal_month']) ? intval($_GET['cal_month']) : date('n');
        $selectedDay = isset($_GET['cal_day']) ? intval($_GET['cal_day']) : null;

        // Settings für Video-Modus holen
        $playInPlayer = $this->settingsManager ? $this->settingsManager->get('video_mode.play_in_player') : true;
        $allowDownload = $this->settingsManager ? $this->settingsManager->get('video_mode.allow_download') : true;

        $output = '<div class="visual-calendar-container">';

        // Navigation
        $output .= '<div class="calendar-navigation">';
        $output .= '<button onclick="changeMonth(' . $currentYear . ',' . ($currentMonth - 1) . ')" class="cal-nav-btn">◀</button>';
        $output .= '<h3>' . $this->monthNames[$currentMonth]['de'] . ' ' . $currentYear . '</h3>';
        $output .= '<button onclick="changeMonth(' . $currentYear . ',' . ($currentMonth + 1) . ')" class="cal-nav-btn">▶</button>';
        $output .= '</div>';

        // AI-Legende
        $output .= '<div class="ai-legend">';
        $output .= '<span class="legend-title">🤖 AI-Erkennung:</span>';
        foreach ($this->aiCategories as $cat => $info) {
            $output .= '<span class="legend-item" title="' . $info['name'] . '">' . $info['icon'] . '</span>';
        }
        $output .= '</div>';

        // Kalender-Grid
        $output .= '<div class="calendar-grid">';

        // Wochentage Header
        $weekdays = ['Mo', 'Di', 'Mi', 'Do', 'Fr', 'Sa', 'So'];
        foreach ($weekdays as $day) {
            $output .= '<div class="calendar-weekday">' . $day . '</div>';
        }

        // Tage des Monats
        $firstDay = mktime(0, 0, 0, $currentMonth, 1, $currentYear);
        $daysInMonth = date('t', $firstDay);
        $dayOfWeek = date('N', $firstDay) - 1;

        // Leere Zellen vor dem ersten Tag
        for ($i = 0; $i < $dayOfWeek; $i++) {
            $output .= '<div class="calendar-day empty"></div>';
        }

        // Tage mit Videos/AI-Events markieren
        for ($day = 1; $day <= $daysInMonth; $day++) {
            $hasVideos = $this->hasVideosForDate($currentYear, $currentMonth, $day);
            $hasAiEvents = $this->hasAiEventsForDate($currentYear, $currentMonth, $day);
            $aiIcons = $this->getAiIconsForDate($currentYear, $currentMonth, $day);
            $isSelected = ($selectedDay == $day);
            $isToday = ($currentYear == date('Y') && $currentMonth == date('n') && $day == date('j'));

            $classes = 'calendar-day';
            if ($hasVideos) $classes .= ' has-video';
            if ($hasAiEvents) $classes .= ' has-ai-events';
            if ($isSelected) $classes .= ' selected';
            if ($isToday) $classes .= ' today';

            $output .= '<div class="' . $classes . '" onclick="selectDay(' . $currentYear . ',' . $currentMonth . ',' . $day . ')">';
            $output .= '<span class="day-number">' . $day . '</span>';

            // Video-Indikator
            if ($hasVideos) {
                $output .= '<span class="video-indicator">📹</span>';
            }

            // AI-Event-Icons (max 3 anzeigen)
            if (!empty($aiIcons)) {
                $output .= '<div class="ai-icons">';
                $displayIcons = array_slice($aiIcons, 0, 3);
                foreach ($displayIcons as $icon) {
                    $output .= '<span class="ai-icon">' . $icon . '</span>';
                }
                if (count($aiIcons) > 3) {
                    $output .= '<span class="ai-more">+' . (count($aiIcons) - 3) . '</span>';
                }
                $output .= '</div>';
            }

            $output .= '</div>';
        }

        $output .= '</div>'; // calendar-grid

        // Video-Liste + AI-Events für ausgewählten Tag
        if ($selectedDay) {
            $videos = $this->getVideosForDate($currentYear, $currentMonth, $selectedDay);
            $aiEvents = $this->getAiEventsForDate($currentYear, $currentMonth, $selectedDay);

            $output .= '<div class="day-details">';
            $output .= '<h4>📅 ' . sprintf('%02d.%02d.%04d', $selectedDay, $currentMonth, $currentYear) . '</h4>';

            // === TAGESVIDEOS ===
            if (!empty($videos)) {
                $output .= '<div class="day-videos">';
                $output .= '<h5>📹 Tagesvideos</h5>';
                $output .= '<ul class="video-download-list">';

                foreach ($videos as $video) {
                    $sizeInMb = round($video['filesize'] / (1024 * 1024), 2);
                    $token = hash_hmac('sha256', $video['path'], session_id());
                    $videoUrl = '?download_specific_video=' . urlencode($video['path']) . '&token=' . $token;

                    $output .= '<li>';
                    $output .= '<span class="video-time">🕐 ' . $video['time'] . ' Uhr</span>';
                    $output .= '<span class="video-size">' . $sizeInMb . ' MB</span>';
                    $output .= '<div class="video-actions">';

                    // Play Button (wenn aktiviert)
                    if ($playInPlayer) {
                        $output .= '<a href="#" onclick="DailyVideoPlayer.playVideo(\'' . htmlspecialchars($video['path']) . '\', ' . ($allowDownload ? 'true' : 'false') . '); return false;" class="play-link">';
                        $output .= '▶️ Abspielen';
                        $output .= '</a>';
                    }

                    // Download Button (wenn aktiviert)
                    if ($allowDownload) {
                        $output .= '<a href="' . $videoUrl . '" class="download-link">';
                        $output .= '⬇️ Download';
                        $output .= '</a>';
                    }

                    $output .= '</div>';
                    $output .= '</li>';
                }

                $output .= '</ul>';
                $output .= '</div>';
            }

            // === AI-EREIGNISSE ===
            if (!empty($aiEvents)) {
                $output .= '<div class="ai-events-section">';
                $output .= '<h5>🤖 AI-erkannte Ereignisse</h5>';
                $output .= '<div class="ai-events-grid">';

                foreach ($aiEvents as $category => $event) {
                    $output .= '<div class="ai-event-card" style="border-left: 4px solid ' . $event['color'] . ';">';
                    $output .= '<div class="ai-event-header">';
                    $output .= '<span class="ai-event-icon">' . $event['icon'] . '</span>';
                    $output .= '<span class="ai-event-name">' . $event['name'] . '</span>';
                    $output .= '</div>';

                    // Videos für dieses Event
                    $output .= '<div class="ai-event-videos">';
                    foreach ($event['videos'] as $video) {
                        $filename = basename($video);
                        $sizeInMb = round(filesize($video) / (1024 * 1024), 2);
                        $token = hash_hmac('sha256', $video, session_id());

                        // Play Button
                        if ($playInPlayer) {
                            $output .= '<a href="#" onclick="DailyVideoPlayer.playVideo(\'' . htmlspecialchars($video) . '\', ' . ($allowDownload ? 'true' : 'false') . '); return false;" class="ai-video-link">';
                            $output .= '▶️ Abspielen (' . $sizeInMb . ' MB)';
                            $output .= '</a>';
                        }

                        // Download Button
                        if ($allowDownload) {
                            $output .= '<a href="?download_ai_video=' . urlencode($video) . '&token=' . $token . '" class="ai-video-link" style="background: #4CAF50;">';
                            $output .= '⬇️ Download';
                            $output .= '</a>';
                        }
                    }
                    $output .= '</div>';

                    $output .= '</div>'; // ai-event-card
                }

                $output .= '</div>'; // ai-events-grid
                $output .= '</div>'; // ai-events-section
            }

            // Keine Inhalte
            if (empty($videos) && empty($aiEvents)) {
                $output .= '<div class="no-content">';
                $output .= '<p>📭 Keine Videos oder AI-Ereignisse für diesen Tag verfügbar.</p>';
                $output .= '</div>';
            }

            $output .= '</div>'; // day-details
        }

        $output .= '</div>'; // visual-calendar-container

        return $output;
    }

    /**
     * Handler für AI-Video Downloads
     */
    public function handleAiVideoDownload() {
        if (isset($_GET['download_ai_video']) && isset($_GET['token'])) {
            $videoPath = $_GET['download_ai_video'];
            $token = $_GET['token'];

            // Token-Validierung
            $expectedToken = hash_hmac('sha256', $videoPath, session_id());
            if (!hash_equals($expectedToken, $token)) {
                echo "Ungültiger Token. Zugriff verweigert.";
                exit;
            }

            // Sicherheitsüberprüfung
            $aiDir = realpath($this->aiDir);
            $requestedPath = realpath($videoPath);

            if ($requestedPath && strpos($requestedPath, $aiDir) === 0 && file_exists($requestedPath)) {
                $extension = pathinfo($requestedPath, PATHINFO_EXTENSION);
                if (strtolower($extension) !== 'mp4') {
                    echo "Nur MP4-Dateien können heruntergeladen werden.";
                    exit;
                }

                header('Content-Description: File Transfer');
                header('Content-Type: video/mp4');
                header('Content-Disposition: attachment; filename="'.basename($requestedPath).'"');
                header('Expires: 0');
                header('Cache-Control: must-revalidate');
                header('Pragma: public');
                header('Content-Length: ' . filesize($requestedPath));
                readfile($requestedPath);
                exit;
            } else {
                echo "Datei nicht gefunden oder ungültiger Dateipfad.";
                exit;
            }
        }
    }
}



























class GuestbookManager {
    private $entries = [];
    private $dbFile = 'guestbook.json';

    public function __construct() {
        if (file_exists($this->dbFile)) {
            $this->entries = json_decode(file_get_contents($this->dbFile), true);
        }
    }

    public function handleFormSubmission() {
        if (isset($_POST['guestbook'], $_POST['guest-name'], $_POST['guest-message'])) {
            $this->addEntry($_POST['guest-name'], $_POST['guest-message']);
            $this->saveEntries();
        }
    }

    private function addEntry($name, $message) {
        $this->entries[] = [
            'name' => $name,
            'message' => $message,
            'date' => date('Y-m-d H:i:s')
        ];
    }

    public function deleteEntry($index) {
        if (isset($this->entries[$index])) {
            unset($this->entries[$index]);
            $this->entries = array_values($this->entries); // Re-indizieren des Arrays
            $this->saveEntries();
            return true;
        }
        return false;
    }



    private function saveEntries() {
        file_put_contents($this->dbFile, json_encode($this->entries));
    }

    public function displayForm() {
        return '
        <form method="post">
            <input type="hidden" name="guestbook" value="1">

          <label for="guest-name"
       data-en="Name:"
       data-de="Name:"
       data-it="Nome:"   data-zh="姓名：
       data-fr="Nom:">
    Name:
</label>
<input type="text" id="guest-name" name="guest-name" required>
<label for="guest-message"
       data-en="Message:"
       data-de="Nachricht:"
       data-it="Messaggio:"
       data-fr="Message:">
    Nachricht:
</label>
<textarea id="guest-message" name="guest-message" required></textarea>
<button type="submit"
        data-en="Add Entry"
        data-de="Eintrag hinzufügen"
        data-it="Aggiungi Voce"
        data-fr="Ajouter une entrée">
    Eintrag hinzufügen
</button>

        </form>';
    }
public function displayEntries($isAdmin = false) {
    $output = '<div id="guestbook-entries">';
    foreach ($this->entries as $index => $entry) {
        $output .= "
        <div class='guestbook-entry'>
            <h4><i class='fas fa-user'></i> {$entry['name']}</h4>
            <p><i class='fas fa-comment'></i> {$entry['message']}</p>
            <small><i class='fas fa-clock'></i> {$entry['date']}</small>";
            if ($isAdmin) {
                $output .= "<form method='post' style='display:inline;'>
                    <input type='hidden' name='action' value='delete_guestbook'>
                    <input type='hidden' name='delete_entry' value='{$index}'>
                    <button type='submit' class='delete-btn'>Löschen</button>
                </form>";
            }

    }
    $output .= '</div>';
    return $output;
}


}


class ContactManager {
    private $adminEmail = 'metacube@gmail.com';
    private $feedbackFile = 'feedbacks.json';
    private $gmailUser = 'metacube@gmail.com';
    private $gmailAppPassword = 'qggk hsxz fdkq jgxa';

    public function displayForm() {
        return '
        <form method="post" id="contact-form">
            <input type="hidden" name="contact" value="1">
            <label for="name"
                   data-en="Name:"
                   data-de="Name:"
                   data-it="Nome:"
                   data-fr="Nom:"
                   data-zh="姓名：">
                Name:
            </label>
            <input type="text" id="name" name="name" required minlength="2">

            <label for="email"
                   data-en="E-Mail:"
                   data-de="E-Mail:"
                   data-it="Email:"
                   data-fr="E-mail:"
                   data-zh="电子邮件：">
                E-Mail:
            </label>
            <input type="email" id="email" name="email" required>

            <label for="message"
                   data-en="Message:"
                   data-de="Nachricht:"
                   data-it="Messaggio:"
                   data-fr="Message:"
                   data-zh="消息：">
                Nachricht:
            </label>
            <textarea id="message" name="message" required minlength="10"></textarea>

            <button type="submit"
                    data-en="Send Message"
                    data-de="Nachricht senden"
                    data-it="Invia Messaggio"
                    data-fr="Envoyer le message"
                    data-zh="发送消息">
                Nachricht senden
            </button>
        </form>
        <div id="contact-feedback" style="margin-top: 15px;"></div>';
    }

    public function handleSubmission($name, $email, $message) {
        if (empty($name) || empty($email) || empty($message)) {
            return ['success' => false, 'message' => 'Alle Felder sind erforderlich'];
        }

        if (!filter_var($email, FILTER_VALIDATE_EMAIL)) {
            return ['success' => false, 'message' => 'Ungültige E-Mail-Adresse'];
        }

        if (strlen($message) < 10) {
            return ['success' => false, 'message' => 'Nachricht zu kurz (mindestens 10 Zeichen)'];
        }

        $name = htmlspecialchars(trim($name), ENT_QUOTES, 'UTF-8');
        $email = filter_var(trim($email), FILTER_SANITIZE_EMAIL);
        $message = htmlspecialchars(trim($message), ENT_QUOTES, 'UTF-8');

        $feedback = [
            'name' => $name,
            'email' => $email,
            'message' => $message,
            'date' => date('Y-m-d H:i:s'),
            'ip' => $_SERVER['REMOTE_ADDR'] ?? 'unknown',
            'user_agent' => $_SERVER['HTTP_USER_AGENT'] ?? 'unknown'
        ];

        $feedbacks = file_exists($this->feedbackFile)
            ? json_decode(file_get_contents($this->feedbackFile), true)
            : [];

        if (!is_array($feedbacks)) $feedbacks = [];

        $feedbacks[] = $feedback;
        file_put_contents($this->feedbackFile, json_encode($feedbacks, JSON_PRETTY_PRINT));

        $mailSent = $this->sendEmailViaGmail($name, $email, $message, $feedback['date'], $feedback['ip']);

        if ($mailSent) {
            return ['success' => true, 'message' => 'Vielen Dank! Ihre Nachricht wurde gesendet.'];
        } else {
            error_log("Mail-Fehler: Nachricht von {$email} konnte nicht gesendet werden");
            return ['success' => false, 'message' => 'Nachricht wurde gespeichert, aber E-Mail konnte nicht gesendet werden.'];
        }
    }

    private function sendEmailViaGmail($name, $email, $message, $date, $ip) {
        $mail = new PHPMailer(true);

        try {
            $mail->SMTPDebug = 2;
            $mail->Debugoutput = function($str, $level) {
                error_log("PHPMailer Debug: $str");
            };

            $mail->isSMTP();
            $mail->Host = 'smtp.gmail.com';
            $mail->SMTPAuth = true;
            $mail->Username = $this->gmailUser;
            $mail->Password = $this->gmailAppPassword;
            $mail->SMTPSecure = PHPMailer::ENCRYPTION_STARTTLS;
            $mail->Port = 587;

            $mail->setFrom($this->gmailUser, 'Aurora Livecam');
            $mail->addAddress($this->adminEmail);
            $mail->addReplyTo($email, $name);

            $mail->isHTML(true);
            $mail->CharSet = 'UTF-8';
            $mail->Subject = '🔔 Neue Kontaktanfrage von ' . $name;
            $mail->Body = $this->getEmailTemplate($name, $email, $message, $date, $ip);

            $mail->send();
            return true;
        } catch (Exception $e) {
            error_log("PHPMailer Error: {$mail->ErrorInfo}");
            return false;
        }
    }

    public function deleteFeedback($index) {
        if (!file_exists($this->feedbackFile)) return false;

        $feedbacks = json_decode(file_get_contents($this->feedbackFile), true);
        if (isset($feedbacks[$index])) {
            unset($feedbacks[$index]);
            $feedbacks = array_values($feedbacks);
            file_put_contents($this->feedbackFile, json_encode($feedbacks, JSON_PRETTY_PRINT));
            return true;
        }
        return false;
    }

    private function getEmailTemplate($name, $email, $message, $date, $ip) {
        return "
        <!DOCTYPE html>
        <html>
        <head>
            <meta charset='UTF-8'>
            <style>
                body { font-family: Arial, sans-serif; line-height: 1.6; color: #333; background-color: #f4f4f4; margin: 0; padding: 0; }
                .container { max-width: 600px; margin: 20px auto; background: white; border-radius: 10px; overflow: hidden; box-shadow: 0 4px 15px rgba(0,0,0,0.1); }
                .header { background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 30px 20px; text-align: center; }
                .header h2 { margin: 0; font-size: 24px; }
                .content { padding: 30px 20px; }
                .info-box { background: #f9f9f9; border-left: 4px solid #667eea; padding: 15px; margin: 15px 0; border-radius: 5px; }
                .info-box strong { color: #667eea; display: block; margin-bottom: 5px; }
                .message-box { background: white; border: 1px solid #e0e0e0; padding: 20px; margin: 20px 0; border-radius: 5px; line-height: 1.8; }
                .footer { background: #f9f9f9; padding: 20px; text-align: center; font-size: 12px; color: #999; border-top: 1px solid #e0e0e0; }
                .button { display: inline-block; padding: 12px 30px; background: #4CAF50; color: white; text-decoration: none; border-radius: 5px; margin: 15px 0; font-weight: bold; }
            </style>
        </head>
        <body>
            <div class='container'>
                <div class='header'><h2>📧 Neue Kontaktanfrage</h2><p style='margin: 10px 0 0 0; opacity: 0.9;'>Aurora Weather Livecam</p></div>
                <div class='content'>
                    <div class='info-box'><strong>👤 Name:</strong>{$name}</div>
                    <div class='info-box'><strong>📧 E-Mail:</strong><a href='mailto:{$email}' style='color: #667eea;'>{$email}</a></div>
                    <div class='info-box'><strong>💬 Nachricht:</strong></div>
                    <div class='message-box'>" . nl2br($message) . "</div>
                    <div style='text-align: center; margin: 30px 0;'><a href='mailto:{$email}' class='button'>↩️ Direkt antworten</a></div>
                </div>
                <div class='footer'>
                    <p><strong>📅 Gesendet am:</strong> {$date}</p>
                    <p><strong>🌐 IP-Adresse:</strong> {$ip}</p>
                    <p style='margin-top: 15px;'>Diese E-Mail wurde automatisch vom Kontaktformular auf<br><a href='https://www.aurora-live-weathercam.com' style='color: #667eea;'>www.aurora-live-weathercam.com</a> generiert.</p>
                </div>
            </div>
        </body>
        </html>";
    }
}


class AdminManager {
    public function isAdmin() {
        return isset($_SESSION['admin']) && $_SESSION['admin'] === true;
    }

    public function handleLogin($username, $password) {
        if ($username === 'admin' && $password === 'sonne4000$$$$Q') {
            $_SESSION['admin'] = true;
            return true;
        }
        return false;
    }

    public function handleImageUpload($file) {
        if (!$this->isAdmin()) return false;
        if (!isset($file['tmp_name']) || empty($file['tmp_name'])) { echo "Keine Datei."; return false; }

        $target_dir = "uploads/";
        if (!file_exists($target_dir)) mkdir($target_dir, 0777, true);

        $target_file = $target_dir . basename($file["name"]);
        $imageFileType = strtolower(pathinfo($target_file,PATHINFO_EXTENSION));

        $check = @getimagesize($file["tmp_name"]);
        if($check === false) { echo "Kein Bild."; return false; }
        if ($file["size"] > 5000000) { echo "Zu groß (>5MB)."; return false; }
        if(!in_array($imageFileType, ['jpg','png','jpeg','gif'])) { echo "Falsches Format."; return false; }

        if (move_uploaded_file($file["tmp_name"], $target_file)) {
            echo "<p style='color:green; font-weight:bold;'>✅ Bild erfolgreich hochgeladen.</p>";
            return true;
        } else {
            echo "Upload Fehler.";
            return false;
        }
    }

    public function displayLoginForm() {
        return '
        <form id="login-form" method="post">
            <input type="hidden" name="admin-login" value="1">
            <label>Benutzername:</label><input type="text" name="username" required>
            <label>Passwort:</label><input type="password" name="password" required>
            <button type="submit">Einloggen</button>
        </form>';
    }

    public function displayAdminContent() {
        global $settingsManager;

        $feedbacks = json_decode(file_get_contents('feedbacks.json') ?: '[]', true);

        // NEUES SETTINGS PANEL
        $output = '<div id="admin-settings-panel">';
        $output .= '<h3>⚙️ Anzeige-Einstellungen</h3>';

        // Zuschauer-Anzeige Settings
        $output .= '<div class="settings-group">';
        $output .= '<h4>👥 Zuschauer-Anzeige</h4>';

        $output .= '<div class="setting-row">';
        $output .= '<span class="setting-label">Zuschauer-Anzahl anzeigen</span>';
        $output .= '<div class="setting-input">';
        $output .= '<label class="toggle-switch">';
        $output .= '<input type="checkbox" id="setting-viewer-enabled" ' . ($settingsManager->get('viewer_display.enabled') ? 'checked' : '') . '>';
        $output .= '<span class="toggle-slider"></span>';
        $output .= '</label>';
        $output .= '</div>';
        $output .= '</div>';

        $output .= '<div class="setting-row">';
        $output .= '<span class="setting-label">Mindestanzahl für Anzeige</span>';
        $output .= '<div class="setting-input">';
        $output .= '<input type="number" id="setting-min-viewers" class="number-input" min="1" max="100" value="' . $settingsManager->get('viewer_display.min_viewers') . '">';
        $output .= '</div>';
        $output .= '</div>';
        $output .= '</div>'; // settings-group

        // Video-Modus Settings
        $output .= '<div class="settings-group">';
        $output .= '<h4>🎬 Video-Modus</h4>';

        $output .= '<div class="setting-row">';
        $output .= '<span class="setting-label">Videos im Player abspielen</span>';
        $output .= '<div class="setting-input">';
        $output .= '<label class="toggle-switch">';
        $output .= '<input type="checkbox" id="setting-play-in-player" ' . ($settingsManager->get('video_mode.play_in_player') ? 'checked' : '') . '>';
        $output .= '<span class="toggle-slider"></span>';
        $output .= '</label>';
        $output .= '</div>';
        $output .= '</div>';

        $output .= '<div class="setting-row">';
        $output .= '<span class="setting-label">Download erlauben</span>';
        $output .= '<div class="setting-input">';
        $output .= '<label class="toggle-switch">';
        $output .= '<input type="checkbox" id="setting-allow-download" ' . ($settingsManager->get('video_mode.allow_download') ? 'checked' : '') . '>';
        $output .= '<span class="toggle-slider"></span>';
        $output .= '</label>';
        $output .= '</div>';
        $output .= '</div>';
        $output .= '</div>'; // settings-group

        $output .= '</div>'; // admin-settings-panel

        // Bestehender Admin-Content
        $output .= '<div style="background:white; padding:20px; border-radius:10px; margin-bottom:30px;">';
        $output .= '<h3 style="border-bottom:2px solid #667eea; padding-bottom:10px;">📩 Posteingang (Kontaktformular)</h3>';

        if (empty($feedbacks)) {
            $output .= '<p>Keine Nachrichten vorhanden.</p>';
        } else {
            $output .= '<div id="message-list" style="display:grid; gap:15px; max-height:500px; overflow-y:auto;">';
            foreach ($feedbacks as $index => $feedback) {
                $ip = isset($feedback['ip']) ? $feedback['ip'] : 'Unbekannt';

                $output .= "<div style='background:#f9f9f9; padding:15px; border-left:4px solid #667eea; border-radius:5px;'>";
                $output .= "<div style='display:flex; justify-content:space-between; align-items:flex-start;'>";
                $output .= "<div><strong>{$feedback['name']}</strong> (<a href='mailto:{$feedback['email']}'>{$feedback['email']}</a>)</div>";

                $output .= "<form method='post' onsubmit='return confirm(\"Nachricht wirklich löschen?\");' style='margin:0;'>
                                <input type='hidden' name='action' value='delete_feedback'>
                                <input type='hidden' name='delete_index' value='{$index}'>
                                <button type='submit' class='delete-btn' style='padding:5px 10px; font-size:12px;'>🗑️ Löschen</button>
                            </form>";
                $output .= "</div>";

                $output .= "<p style='margin:10px 0; white-space:pre-wrap;'>{$feedback['message']}</p>";
                $output .= "<small style='color:#888;'>📅 {$feedback['date']} | IP: {$ip}</small>";
                $output .= "</div>";
            }
            $output .= '</div>';
        }
        $output .= '</div>';

        $output .= '<div style="background:white; padding:20px; border-radius:10px; margin-bottom:30px;">';
        $output .= '<h3 style="border-bottom:2px solid #4CAF50; padding-bottom:10px;">🖼️ Bildergalerie verwalten</h3>';

        $output .= $this->displayGalleryImages(true);

        $output .= '<h4 style="margin-top:20px;">⬆️ Neues Bild hochladen</h4>
        <form action="" method="post" enctype="multipart/form-data" style="background:#f0f0f0; padding:15px; border-radius:5px;">
            <input type="file" name="fileToUpload" id="fileToUpload" required>
            <input type="submit" value="Hochladen" name="submit" class="button" style="margin-top:10px;">
        </form>';
        $output .= '</div>';

        $output .= '<div style="background:white; padding:20px; border-radius:10px;">';
        $output .= '<h3>📲 Social Media Links</h3>
        <form id="social-media-form" method="post">
            <input type="hidden" name="update-social-media" value="1">
            <select name="social-platform" required style="padding:8px; margin-right:10px;">
                <option value="facebook">Facebook</option>
                <option value="instagram">Instagram</option>
                <option value="tiktok">TikTok</option>
            </select>
            <input type="url" name="social-url" placeholder="Profil URL" required style="padding:8px; width:200px;">
            <button type="submit" class="button">Link aktualisieren</button>
        </form>';
        $output .= '</div>';

        return $output;
    }

    public function displayGalleryImages($isAdmin = false) {
        $output = '<div id="gallery-images" style="display:flex; flex-wrap:wrap; gap:10px;">';
        $files = glob("uploads/*.{jpg,jpeg,png,gif}", GLOB_BRACE);

        if ($files) {
            foreach($files as $file) {
                $filename = basename($file);
                $output .= '<div style="position:relative; display:inline-block;">';
                $output .= '<img src="'.$file.'" alt="'.$filename.'" style="width:150px; height:100px; object-fit:cover; border-radius:5px; border:1px solid #ddd;">';

                if ($isAdmin) {
                    $output .= '
                    <form method="post" onsubmit="return confirm(\'Bild wirklich löschen?\');" style="position:absolute; top:-5px; right:-5px; margin:0;">
                        <input type="hidden" name="action" value="delete_image">
                        <input type="hidden" name="image_name" value="'.$filename.'">
                        <button type="submit" style="background:red; color:white; border:2px solid white; border-radius:50%; width:24px; height:24px; cursor:pointer; font-weight:bold; display:flex; align-items:center; justify-content:center; padding:0; box-shadow:0 2px 4px rgba(0,0,0,0.3);">X</button>
                    </form>';
                }

                $output .= '</div>';
            }
        } else {
            $output .= '<p>Keine Bilder in der Galerie.</p>';
        }
        $output .= '</div>';
        return $output;
    }

    public function handleSocialMediaUpdate($platform, $url) {
        $socialLinks = json_decode(file_get_contents('social_links.json') ?: '{}', true);
        $socialLinks[$platform] = $url;
        file_put_contents('social_links.json', json_encode($socialLinks));
    }
}


class VideoArchiveManager {
    private $videoDir;
    private $monthNames;

    public function __construct($videoDir = './videos/') {
        $this->videoDir = $videoDir;
        $this->monthNames = [
            '01' => 'Januar', '02' => 'Februar', '03' => 'März', '04' => 'April',
            '05' => 'Mai', '06' => 'Juni', '07' => 'Juli', '08' => 'August',
            '09' => 'September', '10' => 'Oktober', '11' => 'November', '12' => 'Dezember'
        ];
    }

    public function getVideosGroupedByDate() {
        $videos = [];

        foreach (glob($this->videoDir . 'daily_video_*.mp4') as $video) {
            if (preg_match('/daily_video_(\d{8})_\d{6}\.mp4/', basename($video), $matches)) {
                $dateStr = $matches[1];
                $year = substr($dateStr, 0, 4);
                $month = substr($dateStr, 4, 2);
                $day = substr($dateStr, 6, 2);

                $videos[$year][$month][] = [
                    'path' => $video,
                    'filename' => basename($video),
                    'day' => $day,
                    'filesize' => filesize($video),
                    'modified' => filemtime($video)
                ];
            }
        }

        foreach ($videos as $year => $months) {
            foreach ($months as $month => $days) {
                usort($videos[$year][$month], function($a, $b) {
                    return $b['day'] - $a['day'];
                });
            }
        }

        return $videos;
    }

    public function handleSpecificVideoDownload() {
        if (isset($_GET['download_specific_video']) && isset($_GET['token'])) {
            $videoPath = $_GET['download_specific_video'];
            $token = $_GET['token'];

            $expectedToken = hash_hmac('sha256', $videoPath, session_id());
            if (!hash_equals($expectedToken, $token)) {
                echo "Ungültiger Token. Zugriff verweigert.";
                exit;
            }

            $videoDir = realpath($this->videoDir);
            $requestedPath = realpath($videoPath);

            if ($requestedPath && strpos($requestedPath, $videoDir) === 0 && file_exists($requestedPath)) {
                $extension = pathinfo($requestedPath, PATHINFO_EXTENSION);
                if (strtolower($extension) !== 'mp4') {
                    echo "Nur MP4-Dateien können heruntergeladen werden.";
                    exit;
                }

                header('Content-Description: File Transfer');
                header('Content-Type: video/mp4');
                header('Content-Disposition: attachment; filename="'.basename($requestedPath).'"');
                header('Expires: 0');
                header('Cache-Control: must-revalidate');
                header('Pragma: public');
                header('Content-Length: ' . filesize($requestedPath));
                readfile($requestedPath);
                exit;
            } else {
                echo "Datei nicht gefunden oder ungültiger Dateipfad.";
                exit;
            }
        }
    }
}


$webcamManager = new WebcamManager();
$imageFilesJson = $webcamManager->getImageFiles();
$guestbookManager = new GuestbookManager();
$contactManager = new ContactManager();
$adminManager = new AdminManager();

$videoArchiveManager = new VideoArchiveManager('./videos/');
$videoArchiveManager->handleSpecificVideoDownload();

// AI-Video Download Handler
if (isset($_GET['download_ai_video']) && isset($_GET['token'])) {
    $videoPath = $_GET['download_ai_video'];
    $token = $_GET['token'];

    $expectedToken = hash_hmac('sha256', $videoPath, session_id());
    if (hash_equals($expectedToken, $token)) {
        $aiDir = realpath('./ai/');
        $requestedPath = realpath($videoPath);

        if ($requestedPath && strpos($requestedPath, $aiDir) === 0 && file_exists($requestedPath)) {
            header('Content-Description: File Transfer');
            header('Content-Type: video/mp4');
            header('Content-Disposition: attachment; filename="'.basename($requestedPath).'"');
            header('Expires: 0');
            header('Cache-Control: must-revalidate');
            header('Pragma: public');
            header('Content-Length: ' . filesize($requestedPath));
            readfile($requestedPath);
            exit;
        }
    }
    echo "Download fehlgeschlagen.";
    exit;
}

if (isset($_GET['action'])) {
    switch ($_GET['action']) {
        case 'snapshot':
            $webcamManager->captureSnapshot();
            break;
        case 'sequence':
            $webcamManager->captureVideoSequence();
            break;
    }
}


if ($_SERVER['REQUEST_METHOD'] === 'POST') {
    // Viewer Heartbeat
    if (isset($_POST['action']) && $_POST['action'] === 'viewer_heartbeat') {
        $viewerCounter->handleHeartbeat();
    }

    // GÄSTEBUCH
    if (isset($_POST['guestbook'])) {
        $guestbookManager->handleFormSubmission();
        header("Location: " . $_SERVER['PHP_SELF'] . "#guestbook");
        exit;
    }

    // KONTAKTFORMULAR
    elseif (isset($_POST['contact'])) {
        $result = $contactManager->handleSubmission($_POST['name'], $_POST['email'], $_POST['message']);

        if (isset($_SERVER['HTTP_X_REQUESTED_WITH']) && strtolower($_SERVER['HTTP_X_REQUESTED_WITH']) === 'xmlhttprequest') {
            header('Content-Type: application/json');
            echo json_encode($result);
            exit;
        }
        $_SESSION['contact_result'] = $result;
        header('Location: ' . $_SERVER['PHP_SELF'] . '#kontakt');
        exit;
    }

    // ADMIN LOGIN
    elseif (isset($_POST['admin-login'])) {
        $adminManager->handleLogin($_POST['username'], $_POST['password']);
        header('Location: ' . $_SERVER['PHP_SELF'] . '#admin');
        exit;
    }

    // ADMIN AKTIONEN
    elseif ($adminManager->isAdmin()) {
        if (isset($_POST['action']) && $_POST['action'] === 'delete_guestbook' && isset($_POST['delete_entry'])) {
            $guestbookManager->deleteEntry(intval($_POST['delete_entry']));
            header("Location: " . $_SERVER['PHP_SELF'] . "#guestbook");
            exit;
        }

        if (isset($_POST['action']) && $_POST['action'] === 'delete_feedback' && isset($_POST['delete_index'])) {
            $contactManager->deleteFeedback(intval($_POST['delete_index']));
            header("Location: " . $_SERVER['PHP_SELF'] . "#admin");
            exit;
        }

        if (isset($_POST['action']) && $_POST['action'] === 'delete_image' && isset($_POST['image_name'])) {
            $filename = basename($_POST['image_name']);
            $filepath = "uploads/" . $filename;
            if (file_exists($filepath)) { unlink($filepath); }
            header("Location: " . $_SERVER['PHP_SELF'] . "#admin");
            exit;
        }

        if (isset($_POST['update-social-media'])) {
            $adminManager->handleSocialMediaUpdate($_POST['social-platform'], $_POST['social-url']);
            header("Location: " . $_SERVER['PHP_SELF'] . "#admin");
            exit;
        }

        if (isset($_FILES["fileToUpload"])) {
            $adminManager->handleImageUpload($_FILES["fileToUpload"]);
        }
    }
}

// Viewer-Anzeige Einstellungen für JavaScript
$viewerCount = $viewerCounter->getInitialCount();
$showViewers = $settingsManager->shouldShowViewers($viewerCount);
$minViewersToShow = $settingsManager->get('viewer_display.min_viewers');
?>



<!DOCTYPE html>
<html lang="de">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">

    <title>Aurora Livecam Zürich Oberland | Live Wetter Webcam Schweiz 24/7</title>
    <meta name="description" content="Live Webcam aus dem Zürcher Oberland. Echtzeit-Wetter, Tagesvideos, Timelapse und einzigartige Einblicke auf die Schweizer Alpen. Kostenlos 24/7.">
    <meta name="keywords" content="Webcam Zürich, Live Wetter Schweiz, Zürcher Oberland, Livecam Alpen, Wetter Webcam, Aurora Webcam, Schweiz Livestream, Patrouille Suisse, Wetter heute">
    <meta name="author" content="Aurora Wetter Livecam">
    <meta name="robots" content="index, follow">
    <link rel="canonical" href="https://www.aurora-weather-livecam.com/">

    <meta property="og:type" content="website">
    <meta property="og:title" content="Aurora Livecam | Live Wetter Webcam Zürich Oberland">
    <meta property="og:description" content="Erleben Sie das Wetter im Zürcher Oberland live! 24/7 Webcam auf 616m Höhe mit Blick auf die Schweizer Alpen.">
    <meta property="og:image" content="https://www.aurora-weather-livecam.com/og-image.jpg">
    <meta property="og:url" content="https://www.aurora-weather-livecam.com/">
    <meta property="og:site_name" content="Aurora Wetter Livecam">

    <meta name="twitter:card" content="summary_large_image">
    <meta name="twitter:title" content="Aurora Livecam | Live Wetter Webcam Schweiz">
    <meta name="twitter:description" content="24/7 Live-Webcam aus dem Zürcher Oberland. Wetter, Timelapse & mehr.">

    <meta name="google-site-verification" content="gzs2HE9hbMKbHYKSf2hZjXvDd7iDUeA4Jb2zngzNIZM" />
    <meta name="geo.region" content="CH-ZH">
    <meta name="geo.placename" content="Zürich Oberland">
    <meta name="geo.position" content="47.278;8.870">

    <link rel="icon" type="image/png" sizes="32x32" href="/favicon-32x32.png">
    <link rel="apple-touch-icon" sizes="180x180" href="/apple-touch-icon.png">
    <meta name="theme-color" content="#667eea">

    <link rel="stylesheet" href="https://cdnjs.cloudflare.com/ajax/libs/font-awesome/5.15.3/css/all.min.css">
    <link rel="preconnect" href="https://cdn.jsdelivr.net">
    <script src="https://cdn.jsdelivr.net/npm/hls.js@latest"></script>
    <link rel="stylesheet" href="css/player-controls.css">

<style>
/* ========== HAUPTSTYLES (gekürzt für Übersicht) ========== */
body {
    font-family: Arial, sans-serif;
    margin: 0;
    padding: 0;
    color: #333;
    line-height: 1.6;
    background-image: url('main.jpg');
    background-size: cover;
    background-position: center;
    background-attachment: fixed;
}

.container { max-width: 1200px; margin: 0 auto; padding: 0 20px; }
.section { padding: 80px 0; background-color: rgba(255, 255, 255, 0.8); margin-bottom: 20px; position: relative; z-index: 10; }
.section h2 { font-size: 36px; margin-bottom: 40px; text-align: center; color: #333; }

header {
    background-color: rgba(255, 255, 255, 0.95);
    padding: 10px 0;
    box-shadow: 0 2px 5px rgba(0, 0, 0, 0.1);
    position: sticky;
    top: 0;
    z-index: 100;
}
header .container {
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 20px;
    flex-wrap: wrap;
}

.logo img { height: 50px; }
.logo-wrapper { display: flex; align-items: center; gap: 15px; }
.swiss-cross {
    width: 28px;
    height: 28px;
    background: #d52b1e;
    border-radius: 6px;
    position: relative;
    box-shadow: 0 4px 10px rgba(213, 43, 30, 0.4);
}
.swiss-cross::before,
.swiss-cross::after {
    content: "";
    position: absolute;
    background: #fff;
}
.swiss-cross::before {
    width: 16px;
    height: 4px;
    top: 12px;
    left: 6px;
    border-radius: 2px;
}
.swiss-cross::after {
    width: 4px;
    height: 16px;
    top: 6px;
    left: 12px;
    border-radius: 2px;
}

nav ul { list-style: none; padding: 0; display: flex; justify-content: space-around; flex-wrap: wrap; margin: 0; }
nav ul li { margin: 5px 10px; }
nav ul li a { text-decoration: none; color: #333; font-weight: bold; padding: 5px 10px; transition: color 0.3s; }
nav ul li a:hover { color: #4CAF50; }
.theme-switcher {
    display: flex;
    align-items: center;
    gap: 8px;
    background: rgba(255, 255, 255, 0.85);
    padding: 6px 10px;
    border-radius: 999px;
    box-shadow: 0 6px 20px rgba(0, 0, 0, 0.1);
}
.theme-switcher span {
    font-size: 12px;
    font-weight: 700;
    color: #333;
    letter-spacing: 0.4px;
}
.theme-button {
    border: none;
    background: transparent;
    padding: 6px 12px;
    border-radius: 999px;
    font-weight: 600;
    cursor: pointer;
    color: #333;
    transition: all 0.2s ease;
}
.theme-button.active {
    background: linear-gradient(135deg, #667eea, #764ba2);
    color: #fff;
    box-shadow: 0 6px 15px rgba(102, 126, 234, 0.35);
}

.button {
    display: inline-block;
    padding: 10px 20px;
    margin: 10px;
    background-color: #4CAF50;
    color: white;
    text-decoration: none;
    border-radius: 5px;
    transition: background-color 0.3s;
    font-weight: bold;
    text-align: center;
    border: none;
    cursor: pointer;
}
.button:hover { background-color: #45a049; }

.video-container {
    position: relative;
    padding-bottom: 56.25%;
    height: 0;
    overflow: hidden;
    margin-bottom: 20px;
    background-color: #000;
    border-radius: 8px;
    z-index: 30;
}

#webcam-player, #timelapse-viewer, #daily-video-player {
    position: absolute;
    top: 0;
    left: 0;
    width: 100%;
    height: 100%;
    border-radius: 8px;
}

.video-info-bar {
    display: grid;
    grid-template-columns: 1fr auto 1fr;
    align-items: center;
    margin-top: 15px;
    margin-bottom: 25px;
    padding: 0 10px;
    gap: 15px;
}

.info-badge {
    background: #ffffff;
    padding: 8px 20px;
    border-radius: 50px;
    color: #333;
    font-family: 'Arial', sans-serif;
    font-size: 14px;
    font-weight: bold;
    display: inline-flex;
    align-items: center;
    justify-content: center;
    gap: 10px;
    box-shadow: 0 4px 6px rgba(0,0,0,0.05);
    border: 1px solid #e0e0e0;
    height: 40px;
    white-space: nowrap;
}
.zoom-controls {
    display: flex;
    align-items: center;
    gap: 12px;
    justify-content: center;
    margin: 15px 0 5px;
    padding: 10px 16px;
    background: rgba(255, 255, 255, 0.85);
    border-radius: 999px;
    box-shadow: 0 6px 18px rgba(0, 0, 0, 0.08);
}
.zoom-controls label {
    font-weight: 700;
    font-size: 14px;
    color: #333;
    margin: 0;
}
.zoom-slider {
    width: 220px;
}
.zoom-value {
    font-weight: 700;
    min-width: 50px;
    text-align: center;
    color: #333;
}
.zoom-btn {
    width: 36px;
    height: 36px;
    border: none;
    border-radius: 50%;
    background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
    color: white;
    font-size: 18px;
    font-weight: bold;
    cursor: pointer;
    transition: transform 0.2s, box-shadow 0.2s;
    display: flex;
    align-items: center;
    justify-content: center;
}
.zoom-btn:hover {
    transform: scale(1.1);
    box-shadow: 0 4px 12px rgba(102, 126, 234, 0.4);
}
.zoom-btn:active {
    transform: scale(0.95);
}
.video-container {
    cursor: default;
}
.video-container.zoomed {
    cursor: grab;
}
.video-container.zoomed:active {
    cursor: grabbing;
}

.tech-stat { justify-self: start; font-family: monospace; color: #555; }
.bitrate-icon { color: #4CAF50; }
.viewer-stat { justify-self: center; background: #fff5f5; border-color: #ffcccc; color: #d32f2f; }

.live-dot {
    width: 8px;
    height: 8px;
    background-color: #ff4136;
    border-radius: 50%;
    display: inline-block;
    animation: pulse-red 2s infinite;
}

@keyframes pulse-red {
    0% { box-shadow: 0 0 0 0 rgba(255, 65, 54, 0.7); }
    70% { box-shadow: 0 0 0 6px rgba(255, 65, 54, 0); }
    100% { box-shadow: 0 0 0 0 rgba(255, 65, 54, 0); }
}

.title-section {
    text-align: center;
    padding: 50px 0;
    color: #fff;
    text-shadow: 2px 2px 4px rgba(0,0,0,0.7);
    position: relative;
    z-index: 20;
}

.flag-title-container { display: flex; justify-content: center; align-items: center; gap: 20px; }
.flag-image { width: 50px; height: auto; box-shadow: 0 2px 5px rgba(0,0,0,0.2); border-radius: 3px; }
.title-section h1 { font-size: 2.5em; margin-bottom: 10px; }
.title-section p { font-size: 1.2em; }

/* Kalender Styles */
.visual-calendar-container {
    max-width: 800px;
    margin: 0 auto;
    background: rgba(255, 255, 255, 0.95);
    border-radius: 12px;
    padding: 20px;
    box-shadow: 0 5px 20px rgba(0,0,0,0.1);
}

.calendar-navigation {
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin-bottom: 20px;
    padding: 10px;
    background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
    border-radius: 8px;
    color: white;
}

.cal-nav-btn {
    background: rgba(255,255,255,0.2);
    border: none;
    color: white;
    font-size: 24px;
    padding: 5px 15px;
    border-radius: 5px;
    cursor: pointer;
    transition: all 0.3s;
}

.calendar-grid { display: grid; grid-template-columns: repeat(7, 1fr); gap: 5px; margin-bottom: 20px; }
.calendar-weekday { text-align: center; font-weight: bold; padding: 10px; background: #f0f0f0; border-radius: 5px; color: #666; }

.calendar-day {
    aspect-ratio: 1;
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    background: white;
    border: 2px solid #e0e0e0;
    border-radius: 8px;
    cursor: pointer;
    transition: all 0.3s;
    position: relative;
    min-height: 60px;
}

.calendar-day:hover:not(.empty) { transform: scale(1.05); box-shadow: 0 3px 10px rgba(0,0,0,0.15); border-color: #667eea; }
.calendar-day.empty { background: transparent; border: none; cursor: default; }
.calendar-day.has-video { background: linear-gradient(135deg, #e3f2fd 0%, #bbdefb 100%); border-color: #2196F3; font-weight: bold; }
.calendar-day.selected { background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; border-color: #764ba2; transform: scale(1.1); }
.calendar-day.today { border: 3px solid #4CAF50; }

.day-number { font-size: 18px; font-weight: 600; }
.video-indicator { position: absolute; bottom: 2px; right: 2px; font-size: 12px; }

.day-details { background: #f9f9f9; border-radius: 8px; padding: 20px; margin-top: 20px; }
.day-details h4 { color: #333; margin-bottom: 20px; padding-bottom: 10px; border-bottom: 2px solid #667eea; }

.video-download-list { list-style: none; padding: 0; }
.video-download-list li { display: flex; justify-content: space-between; align-items: center; padding: 12px; margin-bottom: 10px; background: white; border-radius: 6px; flex-wrap: wrap; gap: 10px; }
.video-actions { display: flex; gap: 10px; }

.play-link {
    background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
    color: white;
    padding: 8px 16px;
    border-radius: 20px;
    text-decoration: none;
    font-weight: bold;
    transition: all 0.3s;
}
.play-link:hover { transform: scale(1.05); }

.download-link {
    background: linear-gradient(135deg, #4CAF50 0%, #45a049 100%);
    color: white;
    padding: 8px 16px;
    border-radius: 20px;
    text-decoration: none;
    font-weight: bold;
    transition: all 0.3s;
}
.download-link:hover { transform: scale(1.05); }

/* Forms */
form { display: grid; gap: 20px; background-color: rgba(255, 255, 255, 0.7); padding: 25px; border-radius: 8px; max-width: 600px; margin: 0 auto; }
input, textarea, select { width: 100%; padding: 12px; border: 1px solid #ddd; border-radius: 5px; font-size: 16px; background-color: white; }
textarea { min-height: 120px; resize: vertical; }
label { font-weight: bold; margin-bottom: 5px; display: block; }

button[type="submit"] { background-color: #4CAF50; color: #fff; border: none; padding: 10px 20px; border-radius: 5px; cursor: pointer; font-size: 16px; transition: background-color 0.3s; }
button[type="submit"]:hover { background-color: #45a049; }

.delete-btn { background-color: #ff4136; color: white; border: none; padding: 5px 10px; cursor: pointer; font-size: 0.8em; margin-left: 10px; border-radius: 3px; }

/* Guestbook */
.guestbook-entry { background-color: #f9f9f9; border-left: 5px solid #4CAF50; margin-bottom: 20px; padding: 15px; border-radius: 5px; box-shadow: 0 2px 5px rgba(0,0,0,0.1); }

/* Gallery */
.gallery-wrapper { position: relative; max-width: 100%; margin: 0 auto; padding: 0 50px; }
#gallery-images { display: flex; flex-wrap: nowrap !important; overflow-x: auto; gap: 15px; scroll-behavior: smooth; padding: 10px 0; scrollbar-width: none; }
#gallery-images::-webkit-scrollbar { display: none; }
#gallery-images img { flex: 0 0 auto; width: 250px !important; height: 180px !important; object-fit: cover; border-radius: 8px; cursor: pointer; transition: transform 0.3s; }
#gallery-images img:hover { transform: scale(1.05); }

.gallery-nav-btn { position: absolute; top: 50%; transform: translateY(-50%); background: rgba(0, 0, 0, 0.6); color: white; border: none; width: 40px; height: 40px; border-radius: 50%; font-size: 20px; cursor: pointer; z-index: 10; display: flex; align-items: center; justify-content: center; }
.gallery-nav-btn.left { left: 0; }
.gallery-nav-btn.right { right: 0; }

/* Modal */
.modal { display: none; position: fixed; z-index: 1000; padding-top: 50px; left: 0; top: 0; width: 100%; height: 100%; overflow: auto; background-color: rgba(0,0,0,0.9); }
.modal-content { margin: auto; display: block; width: 95vw; max-height: 90vh; object-fit: contain; border-radius: 5px; }
.close { position: absolute; top: 15px; right: 35px; color: #f1f1f1; font-size: 40px; font-weight: bold; cursor: pointer; z-index: 1010; }
.modal-prev, .modal-next { cursor: pointer; position: absolute; top: 50%; padding: 16px; color: white; font-weight: bold; font-size: 40px; z-index: 1010; background-color: rgba(0, 0, 0, 0.3); }
.modal-next { right: 0; }
.modal-prev { left: 0; }

/* Language Switch */
#language-switch { position: fixed; top: 10px; right: 10px; z-index: 1000; background-color: rgba(255, 255, 255, 0.8); border-radius: 5px; padding: 5px; }
.lang-button { background: none; border: none; cursor: pointer; padding: 5px; opacity: 0.7; transition: opacity 0.3s; margin: 0 2px; }
.lang-button:hover, .lang-button.active { opacity: 1; }
.flag-icon { width: 30px; height: 20px; object-fit: cover; border-radius: 2px; }

/* Footer */
footer { background-color: rgba(51, 51, 51, 0.9); color: #fff; padding: 40px 0; text-align: center; margin-top: 40px; }
.footer-links { margin-bottom: 20px; }
.footer-links a { color: #fff; text-decoration: none; margin: 0 15px; transition: color 0.3s; }
.footer-links a:hover { color: #4CAF50; }

/* QR Code */
#qrcode { display: flex; justify-content: center; margin-top: 20px; cursor: pointer; }
#qrcode img { border: 10px solid white; border-radius: 5px; box-shadow: 0 4px 8px rgba(0,0,0,0.15); }

/* AI Events */
.ai-legend { display: flex; align-items: center; justify-content: center; gap: 10px; padding: 10px 15px; background: #f8f9fa; border-radius: 8px; margin-bottom: 15px; flex-wrap: wrap; }
.ai-legend .legend-title { font-weight: bold; color: #667eea; }
.ai-icons { display: flex; gap: 2px; justify-content: center; margin-top: 2px; }
.ai-icon { font-size: 10px; }

.ai-events-section { margin-top: 20px; padding-top: 15px; border-top: 1px dashed #ddd; }
.ai-events-grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(200px, 1fr)); gap: 15px; margin-top: 10px; }
.ai-event-card { background: white; border-radius: 8px; padding: 15px; box-shadow: 0 2px 8px rgba(0,0,0,0.1); }
.ai-event-header { display: flex; align-items: center; gap: 10px; margin-bottom: 10px; }
.ai-event-icon { font-size: 28px; }
.ai-event-name { font-weight: bold; color: #333; }
.ai-event-videos { display: flex; flex-direction: column; gap: 5px; }
.ai-video-link { display: inline-block; padding: 8px 12px; background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; text-decoration: none; border-radius: 5px; font-size: 13px; }

/* Responsive */
@media (max-width: 768px) {
    .container { padding: 0 15px; }
    .section { padding: 40px 0; }
    nav ul { flex-direction: column; align-items: center; }
    header .container { justify-content: center; }
    .theme-switcher { width: 100%; justify-content: center; }
    .flag-title-container { flex-direction: column; }
    .title-section h1 { font-size: 1.8em; }
    .video-info-bar { display: flex; flex-direction: column-reverse; gap: 10px; }
    .info-badge { width: 100%; }
    .calendar-grid { gap: 2px; }
    .calendar-day { min-height: 45px; }
    .day-number { font-size: 14px; }
}

@media (max-width: 480px) {
    .section h2 { font-size: 28px; }
    .button { width: 100%; margin: 5px 0; }
}

/* WERBEBANNER STYLES */
.recommendation-banner {
    text-align: center;
    padding: 20px;
    background-color: rgba(255, 255, 255, 0.9);
    border-radius: 8px;
    margin-bottom: 20px;
    box-shadow: 0 2px 8px rgba(0,0,0,0.1);
}

.recommendation-banner h2 {
    margin-bottom: 15px;
    color: #333;
}

.sponsor-logos {
    display: flex;
    flex-direction: column;
    align-items: center;
}

.ad-row {
    display: flex;
    justify-content: center;
    flex-wrap: wrap;
    margin-bottom: 10px;
    width: 100%;
}

.ad-item {
    margin: 0 10px 10px;
    text-align: center;
}

.ad-item img {
    max-height: 40px;  /* <-- DAS IST DIE WICHTIGE ZEILE */
    width: auto;
    transition: transform 0.3s;
}

.ad-item img:hover {
    transform: scale(1.1);
}

/* ========== DESIGN SWITCH THEMES ========== */
.sun-overlay {
    position: fixed;
    inset: -20% auto auto -10%;
    width: 320px;
    height: 320px;
    background: radial-gradient(circle at center, rgba(255, 214, 98, 0.95), rgba(255, 185, 64, 0.65) 45%, rgba(255, 185, 64, 0) 70%);
    filter: blur(2px);
    border-radius: 50%;
    z-index: 1;
    pointer-events: none;
    animation: sunFloat 12s ease-in-out infinite;
}
@keyframes sunFloat {
    0% { transform: translate(0, 0) scale(1); opacity: 0.9; }
    50% { transform: translate(40px, 10px) scale(1.08); opacity: 1; }
    100% { transform: translate(0, 0) scale(1); opacity: 0.9; }
}

body.theme-legacy .sun-overlay { display: none; }

body.theme-alpine {
    background-image: linear-gradient(135deg, rgba(221, 234, 255, 0.8), rgba(196, 223, 255, 0.9)), url('main.jpg');
    color: #102a43;
}
body.theme-alpine header {
    background: rgba(255, 255, 255, 0.85);
    backdrop-filter: blur(8px);
}
body.theme-alpine .section {
    background: rgba(255, 255, 255, 0.88);
    border: 1px solid rgba(180, 205, 230, 0.6);
}
body.theme-alpine .button {
    background: linear-gradient(135deg, #2f80ed, #56ccf2);
}
body.theme-alpine nav ul li a:hover { color: #2f80ed; }
body.theme-alpine .info-badge { border-color: #d2e3f9; }

body.theme-neo {
    background-image: radial-gradient(circle at top, rgba(40, 90, 140, 0.7), rgba(7, 16, 34, 0.95) 55%), url('main.jpg');
    color: #e6f1ff;
}
body.theme-neo header {
    background: rgba(7, 16, 34, 0.9);
    border-bottom: 1px solid rgba(96, 165, 250, 0.3);
}
body.theme-neo nav ul li a { color: #e6f1ff; }
body.theme-neo nav ul li a:hover { color: #60a5fa; }
body.theme-neo .section {
    background: rgba(7, 16, 34, 0.75);
    border: 1px solid rgba(96, 165, 250, 0.2);
}
body.theme-neo .button {
    background: linear-gradient(135deg, #00c6ff, #0072ff);
}
body.theme-neo .info-badge {
    background: rgba(15, 30, 60, 0.9);
    color: #e6f1ff;
    border-color: rgba(96, 165, 250, 0.4);
}
body.theme-neo footer {
    background: rgba(7, 16, 34, 0.95);
}
/* WERBEBANNER STYLES */
.recommendation-banner {
    text-align: center;
    padding: 20px;
    background-color: rgba(255, 255, 255, 0.9);
    border-radius: 8px;
    margin-bottom: 20px;
    box-shadow: 0 2px 8px rgba(0,0,0,0.1);
}

.recommendation-banner h2 {
    margin-bottom: 15px;
    color: #333;
}

.sponsor-logos {
    display: flex;
    flex-direction: column;
    align-items: center;
}

.ad-row {
    display: flex;
    justify-content: center;
    flex-wrap: wrap;
    margin-bottom: 10px;
    width: 100%;
}

.ad-item {
    margin: 0 10px 10px;
    text-align: center;
}

.ad-item img {
    max-height: 40px;  /* <-- DAS IST DIE WICHTIGE ZEILE */
    width: auto;
    transition: transform 0.3s;
}

.ad-item img:hover {
    transform: scale(1.1);
}


</style>

    <script src="https://cdn.jsdelivr.net/npm/qrcode-generator@1.4.4/qrcode.min.js"></script>
</head>
<body class="theme-legacy">

<div class="sun-overlay" aria-hidden="true"></div>

<div id="language-switch">
    <button id="lang-de" class="lang-button" aria-label="Deutsch">
        <img src="images/swiss-flag-new.ico" alt="Schweizer Flagge" class="flag-icon">
    </button>
    <button id="lang-en" class="lang-button" aria-label="English">
        <img src="images/uk-flag.ico" alt="UK Flagge" class="flag-icon">
    </button>
    <button id="lang-it" class="lang-button" aria-label="Italiano">
        <img src="images/italian-flag.ico" alt="Flagge Italien" class="flag-icon">
    </button>
    <button id="lang-fr" class="lang-button" aria-label="Français">
        <img src="images/french-flag.ico" alt="Flagge Frankreich" class="flag-icon">
    </button>
    <button id="lang-zh" class="lang-button" aria-label="中文">
        <img src="images/chinese-flag.ico" alt="中国国旗" class="flag-icon">
    </button>
</div>

<header>
    <div class="container">
        <div class="logo-wrapper">
            <div class="logo">
                <img src="logo.png" alt="Aurora Wetter Livecam">
            </div>
            <div class="swiss-cross" aria-hidden="true"></div>
        </div>
        <nav>
            <ul>
                <li><a href="#webcams" data-en="Webcam" data-de="Webcam">Webcam</a></li>
                <li><a href="#guestbook" data-en="Guestbook" data-de="Gästebuch">Gästebuch</a></li>
                <li><a href="#kontakt" data-en="Contact" data-de="Kontakt">Kontakt</a></li>
                <li><a href="#gallery" data-en="Gallery" data-de="Galerie">Galerie</a></li>
                <li><a href="#archive" data-en="Video Archive" data-de="Videoarchiv">Videoarchiv</a></li>
                <?php if ($adminManager->isAdmin()): ?>
                <li><a href="#admin">Admin</a></li>
                <?php endif; ?>
            </ul>
        </nav>
        <div class="theme-switcher" aria-label="Design wechseln">
            <span>Design</span>
            <button class="theme-button active" data-theme="theme-legacy" type="button">Klassisch</button>
            <button class="theme-button" data-theme="theme-alpine" type="button">Alpin</button>
            <button class="theme-button" data-theme="theme-neo" type="button">Modern</button>
        </div>
    </div>
</header>

<div class="main-content">
    <section class="title-section">
        <div class="container">
            <div class="flag-title-container">
                <img src="images/swiss.jpg" alt="Schweizer Flagge" class="flag-image">
                <h1 data-en="Welcome to Aurora Weather Livecam" data-de="Willkommen bei Aurora Wetter Livecam">
                    Willkommen bei Aurora Wetter Livecam
                </h1>
                <img src="local-flag.jpg" alt="Ortsflagge" class="flag-image">
            </div>
            <p data-en="Experience fascinating views of the Zurich region - in real time!"
               data-de="Erleben Sie faszinierende Ausblicke der Züricher Region - in Echtzeit!">
                Erleben Sie faszinierende Ausblicke der Züricher Region - in Echtzeit!
            </p>
        </div>
    </section>

    <div class="banner-container">
        <div class="recommendation-banner">
            <h2>Unsere Empfehlungen</h2>
            <div class="sponsor-logos">
                <div class="ad-row">
                    <div class="ad-item"><a href="https://pizzaforyou.ch/" target="_blank"><img src="pizza.png" alt="Pizza for You"></a></div>
                    <div class="ad-item"><a href="#"><img src="werbung.png" alt="Werbung"></a></div>
                    <div class="ad-item"><a href="#"><img src="werbung.png" alt="Werbung"></a></div>
                    <div class="ad-item"><a href="#"><img src="werbung.png" alt="Werbung"></a></div>
                </div>
            </div>
        </div>
    </div>
</div>

<!-- WEBCAM SECTION -->
<section id="webcams" class="section">
    <div class="container">
        <!-- VIDEO PLAYER -->
        <div class="video-container" style="border-radius: 12px; overflow: hidden; box-shadow: 0 10px 30px rgba(0,0,0,0.15);">
            <?php echo $webcamManager->displayWebcam(); ?>

            <!-- Timelapse Overlay -->
            <div id="timelapse-viewer" style="display: none;">
                <img id="timelapse-image" src="" alt="Timelapse Image" style="width: 100%; height: 100%; object-fit: cover;">
                <div id="timelapse-time-overlay"></div>
            </div>

            <!-- Daily Video Player (für Tagesvideos) -->
            <div id="daily-video-player" style="display: none;">
                <video id="daily-video" controls playsinline style="width: 100%; height: 100%; object-fit: contain;">
                    <source src="" type="video/mp4">
                </video>
            </div>
        </div>

        <!-- TIMELAPSE CONTROLS (NEU!) -->
        <div id="timelapse-controls"></div>

        <!-- 
 CONTROLS -->
        <div id="zoom-controls" class="zoom-controls" aria-label="Zoom Steuerung">
            <button type="button" onclick="adjustZoom(-0.5)" class="zoom-btn" title="Zoom out">−</button>
            <input type="range" id="zoom-range" class="zoom-slider" min="1" max="4" value="1" step="0.5">
            <span id="zoom-value" class="zoom-value">1.0x</span>
            <button type="button" onclick="adjustZoom(0.5)" class="zoom-btn" title="Zoom in">+</button>
            <button type="button" onclick="resetZoom()" class="zoom-btn" title="Reset">⟲</button>
        </div>

        <!-- VIDEO PLAYER CONTROLS (für Tagesvideos) -->
        <div id="daily-video-controls" style="display: none; text-align: center; margin-top: 15px;">
            <button id="dvp-back-live" class="button" style="background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);">
                <i class="fas fa-video"></i> Zurück zu Live
            </button>
            <a id="dvp-download" class="button" style="display: none;">
                <i class="fas fa-download"></i> Download
            </a>
        </div>

        <!-- INFO LEISTE -->
        <div class="video-info-bar">
            <?php echo $webcamManager->displayStreamStats(); ?>

            <?php if ($showViewers): ?>
            <div class="info-badge viewer-stat" id="viewer-stat-container">
                <span class="live-dot"></span>
                <strong id="viewer-count-display"><?php echo $viewerCount; ?></strong>
                <span data-en="Watching" data-de="Zuschauer">Zuschauer</span>
            </div>
            <?php endif; ?>

            <div style="justify-self: end;"></div>
        </div>

        <!-- STEUERUNG BUTTONS -->
        <div class="webcam-controls" style="text-align: center;">
            <a href="?action=snapshot" class="button" data-en="Save Snapshot" data-de="Snapshot speichern">
                Snapshot speichern
            </a>
            <a href="#" class="button" id="timelapse-button" data-en="Week Timelapse" data-de="Wochenzeitraffer">
                Wochenzeitraffer
            </a>
            <a href="?action=sequence" class="button" data-en="Save Video Clip" data-de="Videoclip speichern">
                Videoclip speichern
            </a>
            <a href="?download_video=1" class="button" data-en="Download Latest Video" data-de="Tagesvideo downloaden">
                Tagesvideo downloaden
            </a>
        </div>
    </div>
</section>

<!-- ARCHIVE SECTION -->
<section id="archive" class="section">
    <div class="container">
        <h2 data-en="Video Archive" data-de="Videoarchiv Tagesvideos">Videoarchiv Tagesvideos</h2>
        <?php
        $visualCalendar = new VisualCalendarManager('./videos/', './ai/', $settingsManager);
        echo $visualCalendar->displayVisualCalendar();
        ?>
    </div>
</section>

<!-- STANDORT -->
<section id="standort" class="section" style="padding: 40px 0;">
    <div class="container" style="text-align: center;">
        <h2 data-en="Camera Direction" data-de="Kamera-Blickrichtung">Kamera-Blickrichtung</h2>
        <div style="display: flex; justify-content: center; align-items: center; flex-wrap: wrap; gap: 30px; margin-top: 30px;">
            <div style="max-width: 350px;">
                <img src="kompass1.png" alt="Kompass" style="width: 100%; border-radius: 12px; box-shadow: 0 10px 30px rgba(0,0,0,0.3);">
            </div>
            <div style="background: rgba(255,255,255,0.95); padding: 25px 35px; border-radius: 12px; text-align: left;">
                <h3 style="margin-bottom: 20px; color: #667eea;">📍 Ungefährer Standort</h3>
                <p><strong>⛰️ Höhe:</strong> ca. 616 m ü.M.</p>
                <p><strong>🧭 Blickrichtung:</strong> Südwest (SW)</p>
                <p><strong>📍 Region:</strong> Zürich Oberland</p>
            </div>
        </div>
    </div>
</section>

<!-- QR CODE -->
<section id="qr-code" class="section">
    <div class="container" style="text-align: center;">
        <h1>
            <p data-en="Follow us and share with friends" data-de="Folge uns und teile mit Freunden">
                Folge uns und kopiere den Code und sende es deinen Freunden
            </p>
        </h1>
        <div id="qrcode" data-url="https://www.aurora-weather-livecam.com/"></div>
        <p data-en="Click QR code to copy URL" data-de="Klicke auf den QR-Code um die URL zu kopieren">
            Klicke auf den QR-Code, um die URL zu kopieren
        </p>
    </div>
</section>

<!-- GUESTBOOK -->
<section id="guestbook" class="section">
    <div class="container">
        <h2 data-en="Guestbook" data-de="Gästebuch">Gästebuch</h2>
        <?php
        echo $guestbookManager->displayForm();
        echo $guestbookManager->displayEntries($adminManager->isAdmin());
        ?>
    </div>
</section>

<!-- CONTACT -->
<section id="kontakt" class="section">
    <div class="container">
        <h2 data-en="Contact" data-de="Kontakt">Kontakt</h2>
        <p data-en="Questions or suggestions? We look forward to hearing from you!"
           data-de="Haben Sie Fragen, Anregungen oder möchten uns unterstützen? Wir freuen uns auf Ihre Nachricht!">
            Haben Sie Fragen, Anregungen oder möchten uns unterstützen? Wir freuen uns auf Ihre Nachricht!
        </p>
        <?php echo $contactManager->displayForm(); ?>
    </div>
</section>

<!-- GALLERY -->
<section id="gallery" class="section">
    <div class="container">
        <h2 data-en="Image Gallery" data-de="Bildergalerie">Bildergalerie</h2>
        <div class="gallery-wrapper">
            <button class="gallery-nav-btn left" onclick="scrollGallery('left')"><i class="fas fa-chevron-left"></i></button>
            <?php echo $adminManager->displayGalleryImages(); ?>
            <button class="gallery-nav-btn right" onclick="scrollGallery('right')"><i class="fas fa-chevron-right"></i></button>
        </div>
    </div>
</section>

<!-- ABOUT -->
<section id="ueber-uns" class="section">
    <div class="container">
        <h2 data-en="About Our Project" data-de="Über unser Projekt">Über unser Projekt</h2>
        <div class="about-grid">
            <div class="about-item">
                <p data-en="Aurora Weather Livecam is a passion project..."
                   data-de="Aurora Wetter Livecam ist ein Herzensprojekt von Wetterbegeisterten...">
                    Aurora Wetter Livecam ist ein Herzensprojekt von Wetterbegeisterten. Wir möchten Ihnen die Schönheit der Natur und Faszination des Wetters näher bringen.
                </p>
                <p>Dazu betreiben wir seit 2010 rund um die Uhr hochauflösende Webcams. Besonders stolz sind wir auf einzigartige Einblicke, wie z.B. die Trainingsflüge der Patrouille Suisse jeden Montagmorgen.</p>
            </div>
        </div>
    </div>
</section>

<!-- ADMIN SECTION -->
<?php if ($adminManager->isAdmin()): ?>
<section id="admin" class="section">
    <div class="container">
        <h2>Admin-Bereich</h2>
        <?php echo $adminManager->displayAdminContent(); ?>
    </div>
</section>
<?php else: ?>
<section id="admin-login" class="section">
    <div class="container">
        <h2>Admin Login</h2>
        <?php echo $adminManager->displayLoginForm(); ?>
    </div>
</section>
<?php endif; ?>

<!-- IMPRESSUM -->
<section id="impressum" class="section">
    <div class="container">
        <h2 data-en="Imprint" data-de="Impressum">Impressum</h2>
        <p>Aurora Wetter Livecam</p>
        <p>M. Kessler</p>
        <p>Dürnten, Schweiz</p>
        <p data-en="Inquiries via contact form" data-de="Anfragen per Kontaktformular">Anfragen per Kontaktformular</p>
    </div>
</section>

</main>

<footer>
    <div class="container">
        <div class="footer-links">
            <a href="#webcams">Webcam</a>
            <a href="#guestbook">Gästebuch</a>
            <a href="#kontakt">Kontakt</a>
            <a href="#gallery">Galerie</a>
            <a href="#impressum">Impressum</a>
        </div>
        <div class="footer-bottom">
            <p>&copy; 2024 Aurora Wetter Lifecam</p>
        </div>
    </div>
</footer>

<!-- MODAL -->
<div id="imageModal" class="modal">
    <span class="close">&times;</span>
    <a class="modal-prev" onclick="changeModalImage(-1)">&#10094;</a>
    <a class="modal-next" onclick="changeModalImage(1)">&#10095;</a>
    <img class="modal-content" id="modalImage">
    <div id="caption"></div>
    <a id="downloadLink" href="#" download class="button" style="display: block; width: 200px; margin: 20px auto;">Download</a>
</div>

<!-- JAVASCRIPT -->
<script src="https://www.youtube.com/iframe_api"></script>
<script>
// Webcam-Player-Logik
<?php echo $webcamManager->getJavaScript(); ?>

// QR-Code Generator
function generateQRCode() {
    var qr = qrcode(0, 'M');
    qr.addData(window.location.href);
    qr.make();
    document.getElementById('qrcode').innerHTML = qr.createImgTag(5);
}

document.addEventListener('DOMContentLoaded', function() {
    generateQRCode();
});

// QR-Code Klick zum Kopieren
document.getElementById('qrcode')?.addEventListener('click', function() {
    var url = this.getAttribute('data-url') || window.location.href;
    navigator.clipboard.writeText(url).then(function() {
        alert('URL wurde in die Zwischenablage kopiert!');
    });
});
</script>

<script>
    window.zoomConfig = {
        enabled: true,
        minZoom: 1,
        maxZoom: 4,
        defaultZoom: 1
    };
</script>
<script src="js/video-zoom.js"></script>

<!-- TIMELAPSE CONTROLLER -->
<script>
const TimelapseController = {
    imageFiles: <?php echo $imageFilesJson; ?>,
    currentIndex: 0,
    isPlaying: false,
    isReverse: false,
    speed: 1,
    availableSpeeds: [1, 10, 100],
    intervalId: null,
    baseInterval: 200,

    init: function() {
        this.setupControls();
        this.updateSlider();
    },

    setupControls: function() {
        const container = document.getElementById('timelapse-controls');
        if (!container) return;

        container.innerHTML = `
            <div class="timelapse-control-bar">
                <button id="tl-play-pause" class="tl-btn" title="Play/Pause">
                    <i class="fas fa-play"></i>
                </button>
                <button id="tl-reverse" class="tl-btn" title="Rückwärts">
                    <i class="fas fa-backward"></i>
                </button>
                <div class="tl-slider-container">
                    <input type="range" id="tl-slider" min="0" max="${this.imageFiles.length - 1}" value="0">
                    <span id="tl-time-display">00:00:00</span>
                </div>
                <div class="tl-speed-container">
                    <button id="tl-speed" class="tl-btn tl-speed-btn">1x</button>
                </div>
                <button id="tl-back-live" class="tl-btn tl-back-btn" title="Zurück zu Live">
                    <i class="fas fa-video"></i> Live
                </button>
            </div>
        `;

        document.getElementById('tl-play-pause').onclick = () => this.togglePlay();
        document.getElementById('tl-reverse').onclick = () => this.toggleReverse();
        document.getElementById('tl-speed').onclick = () => this.cycleSpeed();
        document.getElementById('tl-back-live').onclick = () => this.backToLive();

        const slider = document.getElementById('tl-slider');
        slider.oninput = (e) => this.seekTo(parseInt(e.target.value));
    },

    togglePlay: function() {
        this.isPlaying = !this.isPlaying;
        const btn = document.getElementById('tl-play-pause');
        btn.innerHTML = this.isPlaying ? '<i class="fas fa-pause"></i>' : '<i class="fas fa-play"></i>';

        if (this.isPlaying) {
            this.startPlayback();
        } else {
            this.stopPlayback();
        }
    },

    toggleReverse: function() {
        this.isReverse = !this.isReverse;
        const btn = document.getElementById('tl-reverse');
        btn.classList.toggle('active', this.isReverse);
        btn.innerHTML = this.isReverse ? '<i class="fas fa-forward"></i>' : '<i class="fas fa-backward"></i>';
    },

    cycleSpeed: function() {
        const idx = this.availableSpeeds.indexOf(this.speed);
        this.speed = this.availableSpeeds[(idx + 1) % this.availableSpeeds.length];
        document.getElementById('tl-speed').textContent = this.speed + 'x';

        if (this.isPlaying) {
            this.stopPlayback();
            this.startPlayback();
        }
    },

    startPlayback: function() {
        const interval = this.baseInterval / this.speed;
        this.intervalId = setInterval(() => this.nextFrame(), interval);
    },

    stopPlayback: function() {
        if (this.intervalId) {
            clearInterval(this.intervalId);
            this.intervalId = null;
        }
    },

    nextFrame: function() {
        if (this.isReverse) {
            this.currentIndex--;
            if (this.currentIndex < 0) this.currentIndex = this.imageFiles.length - 1;
        } else {
            this.currentIndex++;
            if (this.currentIndex >= this.imageFiles.length) this.currentIndex = 0;
        }
        this.showFrame(this.currentIndex);
    },

    seekTo: function(index) {
        this.currentIndex = index;
        this.showFrame(index);
    },

    showFrame: function(index) {
        const img = document.getElementById('timelapse-image');
        if (img && this.imageFiles[index]) {
            img.src = this.imageFiles[index];
        }
        this.updateSlider();
        this.updateTimeDisplay();
    },

    updateSlider: function() {
        const slider = document.getElementById('tl-slider');
        if (slider) slider.value = this.currentIndex;
    },

    updateTimeDisplay: function() {
        const display = document.getElementById('tl-time-display');
        const overlay = document.getElementById('timelapse-time-overlay');
        if (!this.imageFiles[this.currentIndex]) return;

        const filename = this.imageFiles[this.currentIndex];
        const match = filename.match(/(\d{4})(\d{2})(\d{2})_(\d{2})(\d{2})(\d{2})/);
        if (match) {
            const [_, y, m, d, h, min, s] = match;
            const timeStr = `${d}.${m}.${y} ${h}:${min}:${s}`;
            if (display) display.textContent = timeStr;
            if (overlay) overlay.textContent = timeStr;
        }
    },

    backToLive: function() {
        this.stopPlayback();
        this.isPlaying = false;

        document.getElementById('timelapse-viewer').style.display = 'none';
        document.getElementById('webcam-player').style.display = 'block';
        document.getElementById('timelapse-controls').style.display = 'none';
        document.getElementById('timelapse-button').textContent = 'Wochenzeitraffer';
    },

    show: function() {
        document.getElementById('timelapse-viewer').style.display = 'block';
        document.getElementById('webcam-player').style.display = 'none';
        document.getElementById('daily-video-player').style.display = 'none';
        document.getElementById('daily-video-controls').style.display = 'none';
        document.getElementById('timelapse-controls').style.display = 'block';

        this.currentIndex = 0;
        this.showFrame(0);
    }
};

// Timelapse Button Event
document.getElementById('timelapse-button')?.addEventListener('click', function(e) {
    e.preventDefault();

    if (document.getElementById('timelapse-viewer').style.display === 'none' ||
        document.getElementById('timelapse-viewer').style.display === '') {
        TimelapseController.init();
        TimelapseController.show();
        this.textContent = 'Zurück zur Live-Webcam';
    } else {
        TimelapseController.backToLive();
    }
});
</script>

<!-- DAILY VIDEO PLAYER -->
<script>
const DailyVideoPlayer = {
    videoElement: null,

    init: function() {
        this.videoElement = document.getElementById('daily-video');
        document.getElementById('dvp-back-live')?.addEventListener('click', () => this.backToLive());
    },

    playVideo: function(videoPath, allowDownload = true) {
        // Andere Player verstecken
        document.getElementById('webcam-player').style.display = 'none';
        document.getElementById('timelapse-viewer').style.display = 'none';
        document.getElementById('timelapse-controls').style.display = 'none';

        // Video Player anzeigen
        document.getElementById('daily-video-player').style.display = 'block';
        document.getElementById('daily-video-controls').style.display = 'block';

        // Video laden
        this.videoElement.src = videoPath;
        this.videoElement.load();
        this.videoElement.play();

        // Download Button
        const downloadBtn = document.getElementById('dvp-download');
        if (allowDownload && downloadBtn) {
            downloadBtn.style.display = 'inline-block';
            downloadBtn.href = videoPath;
            downloadBtn.download = videoPath.split('/').pop();
        } else if (downloadBtn) {
            downloadBtn.style.display = 'none';
        }

        // Scroll zum Video
        document.getElementById('webcams').scrollIntoView({ behavior: 'smooth' });
    },

    backToLive: function() {
        if (this.videoElement) {
            this.videoElement.pause();
            this.videoElement.src = '';
        }

        document.getElementById('daily-video-player').style.display = 'none';
        document.getElementById('daily-video-controls').style.display = 'none';
        document.getElementById('webcam-player').style.display = 'block';
    }
};

document.addEventListener('DOMContentLoaded', function() {
    DailyVideoPlayer.init();
});
</script>

<!-- ADMIN SETTINGS (AJAX) -->
<?php if ($adminManager->isAdmin()): ?>
<script>
const AdminSettings = {
    init: function() {
        this.setupEventListeners();
    },

    updateSetting: function(key, value) {
        fetch(window.location.href, {
            method: 'POST',
            headers: {'Content-Type': 'application/x-www-form-urlencoded'},
            body: `settings_action=update&key=${encodeURIComponent(key)}&value=${encodeURIComponent(value)}`
        })
        .then(r => r.json())
        .then(data => {
            if (data.success) {
                this.showNotification('✓ Einstellung gespeichert', 'success');
                this.applySettingImmediately(key, value);
            } else {
                this.showNotification('✗ Fehler beim Speichern', 'error');
            }
        })
        .catch(err => {
            console.error('Settings update error:', err);
            this.showNotification('✗ Netzwerkfehler', 'error');
        });
    },

    applySettingImmediately: function(key, value) {
        switch(key) {
            case 'viewer_display.enabled':
                const viewerEl = document.getElementById('viewer-stat-container');
                if (viewerEl) {
                    viewerEl.style.display = (value === true || value === 'true') ? 'inline-flex' : 'none';
                }
                break;
            case 'viewer_display.min_viewers':
                window.minViewersToShow = parseInt(value);
                break;
        }
    },

    setupEventListeners: function() {
        document.getElementById('setting-viewer-enabled')?.addEventListener('change', (e) => {
            this.updateSetting('viewer_display.enabled', e.target.checked);
        });

        document.getElementById('setting-min-viewers')?.addEventListener('change', (e) => {
            this.updateSetting('viewer_display.min_viewers', e.target.value);
        });

        document.getElementById('setting-play-in-player')?.addEventListener('change', (e) => {
            this.updateSetting('video_mode.play_in_player', e.target.checked);
        });

        document.getElementById('setting-allow-download')?.addEventListener('change', (e) => {
            this.updateSetting('video_mode.allow_download', e.target.checked);
        });
    },

    showNotification: function(message, type) {
        const notification = document.createElement('div');
        notification.textContent = message;
        notification.style.cssText = `
            position: fixed; top: 20px; right: 20px; padding: 15px 25px; border-radius: 8px;
            background: ${type === 'success' ? '#4CAF50' : '#f44336'}; color: white;
            font-weight: bold; z-index: 10000; animation: slideIn 0.3s ease;
        `;
        document.body.appendChild(notification);
        setTimeout(() => notification.remove(), 3000);
    }
};

document.addEventListener('DOMContentLoaded', function() {
    AdminSettings.init();
});
</script>
<?php endif; ?>

<!-- VIEWER COUNTER -->
<script>
document.addEventListener('DOMContentLoaded', function() {
    const viewerDisplay = document.getElementById('viewer-count-display');
    const viewerContainer = document.getElementById('viewer-stat-container');
    const minViewers = <?php echo $minViewersToShow; ?>;

    function updateViewerCount() {
        const formData = new FormData();
        formData.append('action', 'viewer_heartbeat');

        fetch(window.location.href, {
            method: 'POST',
            body: formData
        })
        .then(r => r.json())
        .then(data => {
            if (data.count && viewerDisplay) {
                viewerDisplay.textContent = data.count;

                // Mindestanzahl prüfen
                const currentMin = window.minViewersToShow || minViewers;
                if (viewerContainer) {
                    viewerContainer.style.display = data.count >= currentMin ? 'inline-flex' : 'none';
                }
            }
        })
        .catch(err => console.error('Viewer update failed', err));
    }

    setTimeout(updateViewerCount, 2000);
    setInterval(updateViewerCount, 10000);
});
</script>

<!-- DESIGN SWITCHER -->
<script>
document.addEventListener('DOMContentLoaded', function() {
    const buttons = document.querySelectorAll('.theme-button');
    const body = document.body;
    const themeMeta = document.querySelector('meta[name="theme-color"]');
    const themeColors = {
        'theme-legacy': '#667eea',
        'theme-alpine': '#2f80ed',
        'theme-neo': '#0b1220'
    };

    const applyTheme = (theme) => {
        body.classList.remove('theme-legacy', 'theme-alpine', 'theme-neo');
        body.classList.add(theme);
        buttons.forEach((button) => {
            button.classList.toggle('active', button.dataset.theme === theme);
        });
        if (themeMeta) {
            themeMeta.setAttribute('content', themeColors[theme] || '#667eea');
        }
        localStorage.setItem('aurora-theme', theme);
    };

    const savedTheme = localStorage.getItem('aurora-theme');
    if (savedTheme && ['theme-legacy', 'theme-alpine', 'theme-neo'].includes(savedTheme)) {
        applyTheme(savedTheme);
    }

    buttons.forEach((button) => {
        button.addEventListener('click', () => {
            applyTheme(button.dataset.theme);
        });
    });
});
</script>

<!-- GALLERY & MODAL -->
<script>
function scrollGallery(direction) {
    const container = document.getElementById('gallery-images');
    const scrollAmount = 300;
    container.scrollBy({ left: direction === 'left' ? -scrollAmount : scrollAmount, behavior: 'smooth' });
}

document.addEventListener('DOMContentLoaded', function() {
    const modal = document.getElementById('imageModal');
    const modalImg = document.getElementById("modalImage");
    const captionText = document.getElementById("caption");
    const downloadLink = document.getElementById("downloadLink");
    const closeBtn = document.getElementsByClassName("close")[0];

    let galleryImages = [];
    let currentModalIndex = 0;

    function initGallery() {
        galleryImages = Array.from(document.querySelectorAll('#gallery-images img'));
        galleryImages.forEach((img, index) => {
            img.style.cursor = "pointer";
            img.onclick = () => openModal(index);
        });
    }

    window.openModal = function(index) {
        currentModalIndex = index;
        updateModalContent();
        modal.style.display = "block";
        document.body.style.overflow = "hidden";
    };

    function updateModalContent() {
        if (currentModalIndex < 0) currentModalIndex = galleryImages.length - 1;
        if (currentModalIndex >= galleryImages.length) currentModalIndex = 0;

        const currentImg = galleryImages[currentModalIndex];
        modalImg.src = currentImg.src;
        captionText.innerHTML = currentImg.alt || `Bild ${currentModalIndex + 1}`;
        downloadLink.href = currentImg.src;
        downloadLink.download = currentImg.alt || `image_${currentModalIndex + 1}.jpg`;
    }

    window.changeModalImage = function(n) {
        currentModalIndex += n;
        modalImg.style.opacity = 0;
        setTimeout(() => {
            updateModalContent();
            modalImg.style.opacity = 1;
        }, 150);
    };

    closeBtn.onclick = () => { modal.style.display = "none"; document.body.style.overflow = "auto"; };
    modal.onclick = (e) => { if (e.target === modal) { modal.style.display = "none"; document.body.style.overflow = "auto"; } };

    document.addEventListener('keydown', (e) => {
        if (modal.style.display === 'block') {
            if (e.key === 'ArrowLeft') changeModalImage(-1);
            if (e.key === 'ArrowRight') changeModalImage(1);
            if (e.key === 'Escape') { modal.style.display = "none"; document.body.style.overflow = "auto"; }
        }
    });

    initGallery();
});
</script>

<!-- LANGUAGE SWITCH -->
<script>
document.addEventListener('DOMContentLoaded', function() {
    const langButtons = document.querySelectorAll('.lang-button');

    langButtons.forEach(button => {
        button.addEventListener('click', function() {
            const lang = this.id.split('-')[1];
            setLanguage(lang);
            langButtons.forEach(btn => btn.classList.remove('active'));
            this.classList.add('active');
        });
    });
});

function setLanguage(lang) {
    document.querySelectorAll('[data-en], [data-de], [data-it], [data-fr], [data-zh]').forEach(elem => {
        const text = elem.getAttribute(`data-${lang}`);
        if (text) elem.textContent = text;
    });
}
</script>

<!-- CALENDAR NAVIGATION -->
<script>
function changeMonth(year, month) {
    if (month < 1) { month = 12; year--; }
    else if (month > 12) { month = 1; year++; }
    window.location.href = '?cal_year=' + year + '&cal_month=' + month + '#archive';
}

function selectDay(year, month, day) {
    window.location.href = '?cal_year=' + year + '&cal_month=' + month + '&cal_day=' + day + '#archive';
}
</script>

<!-- CONTACT FORM AJAX -->
<script>
document.addEventListener('DOMContentLoaded', function() {
    const contactForm = document.getElementById('contact-form');
    if (contactForm) {
        contactForm.addEventListener('submit', function(e) {
            e.preventDefault();

            const formData = new FormData(this);
            const feedback = document.getElementById('contact-feedback');
            const submitBtn = this.querySelector('button[type="submit"]');

            submitBtn.disabled = true;
            submitBtn.textContent = '⏳ Wird gesendet...';
            feedback.innerHTML = '<p style="color: #666;">⏳ Nachricht wird gesendet...</p>';

            fetch(window.location.href, {
                method: 'POST',
                body: formData,
                headers: { 'X-Requested-With': 'XMLHttpRequest' }
            })
            .then(response => response.json())
            .then(data => {
                if (data.success) {
                    feedback.innerHTML = `<p style="color: #4CAF50; padding: 15px; background: #e8f5e9; border-radius: 5px;">✓ ${data.message}</p>`;
                    contactForm.reset();
                } else {
                    feedback.innerHTML = `<p style="color: #f44336; padding: 15px; background: #ffebee; border-radius: 5px;">✗ ${data.message}</p>`;
                }
            })
            .catch(error => {
                feedback.innerHTML = '<p style="color: #f44336;">✗ Fehler beim Senden.</p>';
            })
            .finally(() => {
                submitBtn.disabled = false;
                submitBtn.textContent = 'Nachricht senden';
            });
        });
    }
});
</script>
 

</body>
</html>
