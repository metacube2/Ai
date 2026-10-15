<?php
use PHPMailer\PHPMailer\PHPMailer;
use PHPMailer\PHPMailer\Exception;

require __DIR__ . '/vendor/autoload.php';
require_once 'SettingsManager.php';
require_once 'WeatherManager.php';

$appSecrets = require '/var/www/config.local.php';
$publicDomain = $appSecrets['site']['public_domain'] ?? 'www.aurora-weather-livecam.com';
$publicBaseUrl = $appSecrets['site']['public_base_url'] ?? 'https://www.aurora-weather-livecam.com';

// ============================================================
// REMOTE CACHE SYSTEM - verhindert SMB-Blocking bei Seitenaufruf
// ============================================================
class RemoteCache {
    private static $instance = null;
    private $cache = null;
    private $cacheFile;
    private $lockFile;
    private $maxAge;

    private function __construct() {
        $this->cacheFile = __DIR__ . '/remote_cache.json';
        $this->lockFile  = __DIR__ . '/remote_cache.lock';
        $this->maxAge    = 300;
        $this->loadCache();
    }

    public static function getInstance() {
        if (self::$instance === null) self::$instance = new self();
        return self::$instance;
    }

    private function loadCache() {
        if (file_exists($this->cacheFile)) {
            $raw = @file_get_contents($this->cacheFile);
            $this->cache = $raw ? json_decode($raw, true) : null;
        }
        if ($this->cache === null || (time() - ($this->cache['updated_at'] ?? 0)) > $this->maxAge) {
            $this->triggerBackgroundUpdate();
        }
    }

    private function triggerBackgroundUpdate() {
        if (file_exists($this->lockFile) && (time() - filemtime($this->lockFile)) < 120) return;
        $script = __DIR__ . '/remote_cache_update.php';
        if (file_exists($script)) exec('php ' . escapeshellarg($script) . ' > /dev/null 2>&1 &');
    }

    public function isAvailable() { return ($this->cache !== null && !empty($this->cache['available'])); }

    public function getVideos() {
        if (!$this->isAvailable()) return [];
        return $this->cache['videos'] ?? [];
    }

    public function getImageWebPaths() {
        if (!$this->isAvailable()) return [];
        $paths = [];
        foreach (($this->cache['images'] ?? []) as $img) $paths[] = $img['web_path'];
        return $paths;
    }

    public function getVideosForDate($dateStr) {
        $result = [];
        foreach ($this->getVideos() as $v) {
            if (strpos($v['filename'], "daily_video_{$dateStr}_") === 0) $result[] = $v;
        }
        return $result;
    }

    public function hasVideosForDate($dateStr) {
        foreach ($this->getVideos() as $v) {
            if (strpos($v['filename'], "daily_video_{$dateStr}_") === 0) return true;
        }
        return false;
    }

    public function getLatestVideo() {
        $latest = null; $latestTime = 0;
        foreach ($this->getVideos() as $v) {
            if (($v['mtime'] ?? 0) > $latestTime) { $latestTime = $v['mtime']; $latest = $v; }
        }
        return $latest;
    }

    public function getCacheAge() {
        if ($this->cache === null) return PHP_INT_MAX;
        return time() - ($this->cache['updated_at'] ?? 0);
    }
}

$remoteCache = RemoteCache::getInstance();

// Multi-Tenant Bootstrap laden (falls vorhanden)
if (file_exists(__DIR__ . '/src/bootstrap.php')) {
    require_once __DIR__ . '/src/bootstrap.php';
}

// SettingsManager initialisieren
$settingsManager = new SettingsManager();

// WeatherManager initialisieren
$weatherManager = new WeatherManager($settingsManager);

// AJAX-Handler für Settings und Weather (VOR anderen Ausgaben!)
$settingsManager->handleAjax();
$weatherManager->handleAjax();

if (isset($_GET['download_video'])) {
    $latestVideo = null;
    $latestTime = 0;

    // Lokale Videos direkt (schnell)
    foreach (glob('./videos/*.mp4') as $video) {
        $mtime = filemtime($video);
        if ($mtime > $latestTime) {
            $latestTime = $mtime;
            $latestVideo = $video;
        }
    }

    // Remote-Videos aus Cache (kein glob auf SMB!)
    $remoteLatest = $remoteCache->getLatestVideo();
    if ($remoteLatest && ($remoteLatest['mtime'] ?? 0) > $latestTime) {
        if (file_exists($remoteLatest['path'])) {
            $latestVideo = $remoteLatest['path'];
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

// Hauptlogik - Domain Redirects werden jetzt in bootstrap.php behandelt
// (Legacy-Redirect bleibt als Fallback falls Bootstrap nicht geladen)
$oldDomains = [
    'www.aurora-wetter-lifecam.ch',
    'www.aurora-wetter-livecam.ch'
];
$newDomain = 'www.aurora-weather-livecam.com';

if (in_array($_SERVER['HTTP_HOST'] ?? '', $oldDomains)) {
    $protocol = isset($_SERVER['HTTPS']) && $_SERVER['HTTPS'] === 'on' ? 'https' : 'http';
    $newUrl = $protocol . '://' . $newDomain . $_SERVER['REQUEST_URI'];
    if (!headers_sent()) {
        header("HTTP/1.1 301 Moved Permanently");
        header("Location: " . $newUrl);
        exit();
    }
}

// Site-Konfiguration: Nutze Multi-Tenant System falls verfügbar, sonst Legacy
if (function_exists('getSiteConfig')) {
    // Multi-Tenant Modus (aus bootstrap.php)
    $tenantConfig = getSiteConfig();
    $isSeecam = ($tenantConfig['tenant_slug'] === 'seecam');

    $siteConfig = [
        'domain' => $publicDomain,
        'domainUrl' => $publicBaseUrl,
        'logo' => $tenantConfig['logo_path'] ?? ($isSeecam ? 'seecam.jpg' : 'logo.png'),
        'siteName' => $tenantConfig['site_name'],
        'siteNameFull' => $tenantConfig['site_name_full'],
        'siteNameFullEn' => $tenantConfig['site_name_full'],
        'siteTitle' => $tenantConfig['site_name_full'] . ' - Live Webcam',
        'author' => $tenantConfig['site_name_full'],
        'alternateName' => $tenantConfig['site_name'] . ' Webcam Schweiz',
        'welcomeDe' => $tenantConfig['welcome_de'] ?: ('Willkommen bei ' . $tenantConfig['site_name_full']),
        'welcomeEn' => $tenantConfig['welcome_en'] ?: ('Welcome to ' . $tenantConfig['site_name_full']),
        'aboutDe' => $tenantConfig['site_name_full'] . ' ist ein Herzensprojekt von Wetterbegeisterten.',
        'aboutEn' => $tenantConfig['site_name_full'] . ' is a passion project by weather enthusiasts.',
        'blogTitle' => $tenantConfig['site_name'] . ' Wetter Blog',
        'footerName' => $tenantConfig['site_name_full'],
        'copyright' => '© ' . date('Y') . ' ' . $tenantConfig['site_name_full'],
        // Zusätzliche Multi-Tenant Felder
        'tenant_id' => $tenantConfig['tenant_id'] ?? 0,
        'primary_color' => $tenantConfig['primary_color'] ?? '#667eea',
        'secondary_color' => $tenantConfig['secondary_color'] ?? '#764ba2',
        'custom_css' => $tenantConfig['custom_css'] ?? '',
    ];
} else {
    // Legacy-Modus (hardcoded)
    $isSeecam = ($_SERVER['HTTP_HOST'] === 'www.seecam.ch' || $_SERVER['HTTP_HOST'] === 'seecam.ch');

    if ($isSeecam) {
        $siteConfig = [
            'domain' => 'www.seecam.ch',
            'domainUrl' => 'https://www.seecam.ch',
            'logo' => 'seecam.jpg',
            'siteName' => 'Seecam',
            'siteNameFull' => 'Seecam Wetter Livecam',
            'siteNameFullEn' => 'Seecam Weather Livecam',
            'siteTitle' => 'Zürich Oberland Webcam Live - Zürichsee & Patrouille Suisse | Seecam 24/7',
            'author' => 'Seecam Wetter Livecam',
            'alternateName' => 'Seecam Webcam Schweiz',
            'welcomeDe' => 'Willkommen bei Seecam Wetter Livecam',
            'welcomeEn' => 'Welcome to Seecam Weather Livecam',
            'aboutDe' => 'Seecam Wetter Livecam ist ein Herzensprojekt von Wetterbegeisterten.',
            'aboutEn' => 'Seecam Weather Livecam is a passion project.',
            'blogTitle' => 'Seecam Wetter Blog',
            'footerName' => 'Seecam Wetter Livecam',
            'copyright' => '© 2024 Seecam Wetter Livecam - Webcam Zürich Oberland',
            'tenant_id' => 0,
            'primary_color' => '#667eea',
            'secondary_color' => '#764ba2',
            'custom_css' => '',
        ];
    } else {
        $siteConfig = [
            'domain' => $publicDomain,
            'domainUrl' => $publicBaseUrl,
            'logo' => 'logo.png',
            'siteName' => 'Aurora',
            'siteNameFull' => 'Aurora Wetter Livecam',
            'siteNameFullEn' => 'Aurora Weather Livecam',
            'siteTitle' => 'Zürich Oberland Webcam Live - Zürichsee & Patrouille Suisse | Aurora Livecam 24/7',
            'author' => 'Aurora Wetter Livecam',
            'alternateName' => 'Aurora Webcam Schweiz',
            'welcomeDe' => 'Willkommen bei Aurora Wetter Livecam',
            'welcomeEn' => 'Welcome to Aurora Weather Livecam',
            'aboutDe' => 'Aurora Wetter Livecam ist ein Herzensprojekt von Wetterbegeisterten.',
            'aboutEn' => 'Aurora Weather Livecam is a passion project.',
            'blogTitle' => 'Aurora Wetter Blog',
            'footerName' => 'Aurora Wetter Livecam',
            'copyright' => '© 2024 Aurora Wetter Lifecam - Webcam Zürich Oberland',
            'tenant_id' => 0,
            'primary_color' => '#667eea',
            'secondary_color' => '#764ba2',
            'custom_css' => '',
        ];
    }
}



// Editierbare Textbausteine (Admin-Panel)
$heroWelcomeText = $settingsManager->get('content.hero_welcome_de');
if ($heroWelcomeText === null || $heroWelcomeText === '') {
    $heroWelcomeText = $siteConfig['welcomeDe'];
}

$heroRegionText = $settingsManager->get('content.hero_region_text_de');
if ($heroRegionText === null || $heroRegionText === '') {
    $heroRegionText = 'Erleben Sie faszinierende Ausblicke der Züricher Region - in Echtzeit!';
}

$localFlagAltText = $settingsManager->get('content.local_flag_alt_de');
if ($localFlagAltText === null || $localFlagAltText === '') {
    $localFlagAltText = 'Ortsflagge';
}

$donationCalloutText = $settingsManager->get('content.donation_callout_de');
if ($donationCalloutText === null || $donationCalloutText === '') {
    $donationCalloutText = "Gefällt dir die Livecam? Unterstütze das Projekt –\njeder Franken zählt und hilft die Betriebskosten\nzu decken. So kannst du auch weiterhin\nFotos und Videos kostenlos nutzen.";
}

$twintSupportText = $settingsManager->get('content.twint_support_text_de');
if ($twintSupportText === null || $twintSupportText === '') {
    $twintSupportText = 'Unterstütze die Aurora Livecam mit deiner Spende. Du hilfst dabei, Kamera, Server, Internetverbindung und die 100 % autarke Stromversorgung mit Solarenergie und EcoFlow-Speichern zu finanzieren. So bleibt das Projekt nachhaltig, unabhängig und für alle frei zugänglich.';
}
$showTwintSupport = $settingsManager->shouldShowTwintSupport();
// Spendenknoepfe (2026-10-10): PayPal aktiv, Buy Me a Coffee bleibt einstellbar, standardmaessig aus
$showPaypalDonation = $settingsManager->shouldShowPaypalDonation();
$paypalDonationUrl = $settingsManager->getSocialLink('paypal', 'https://www.paypal.com/donate/?business=metacube%40gmail.com&currency_code=CHF&item_name=Aurora+Livecam');
$showBuyMeACoffee = $settingsManager->shouldShowBuyMeACoffee();

session_start();
error_reporting(E_ALL);
ini_set('display_errors', 0); // Fehler ins Log, nie zu Besuchern (2026-09-27)
ini_set('log_errors', 1);
$imageDir = "./image"; // Angepasst an das Ausgabeverzeichnis des Bash-Skripts
$localImages = glob("$imageDir/screenshot_*.jpg");
$remoteImagesWeb = $remoteCache->getImageWebPaths(); // Aus Cache statt glob auf SMB
$imageFiles = array_merge($localImages ?: [], $remoteImagesWeb);
// Neueste zuerst nach Dateiname (screenshot_JJJJMMTT_HHMMSS), nicht nach Pfad:
// sonst sortiert "remote-image/" vor "./image/" und die neuesten lokalen
// Bilder fielen hinter die Obergrenze.
usort($imageFiles, fn($a, $b) => strcmp(basename($b), basename($a)));
// Lokale Bilder liegen zusaetzlich als Kopie auf dem Share: je Dateiname nur
// einmal (lokal zuerst, weil array_merge die lokalen voranstellt).
$imageFiles = array_values(array_intersect_key($imageFiles, array_unique(array_map('basename', $imageFiles))));
// Nur die neuesten Bilder in die Seite (~1 Tag bei einem Bild alle 38 s).
// Die ganze Liste (>140'000) sprengte am 2026-09-27 das Speicherlimit.
$imageFiles = array_slice($imageFiles, 0, 1500);
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
        <div id="live-video-wrapper" class="video-zoom-wrapper">
            <video id="webcam-player"
                autoplay
                muted
                playsinline
                webkit-playsinline
                x-webkit-airplay="allow"
                x5-video-player-type="h5"
                x5-video-player-fullscreen="true"
                style="width: 100%; height: 100%; object-fit: contain;">
            </video>
        </div>';
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
        if (!copy($outputFile, $uploadFile)) {
            unlink($outputFile);
            return "Fehler";
        }

        unlink($outputFile);
        header('Location: /?snapshot_saved=' . rawurlencode($outputFile) . '#gallery');
        exit;
    }

    public function getImageFiles() {
        global $remoteCache;
        $localFiles = glob("image/screenshot_*.jpg") ?: [];
        $remoteWeb = $remoteCache->getImageWebPaths(); // Aus Cache statt glob auf SMB
        $imageFiles = array_merge($localFiles, $remoteWeb);
        usort($imageFiles, fn($a, $b) => strcmp(basename($b), basename($a))); // neueste zuerst, siehe oben
        $imageFiles = array_values(array_intersect_key($imageFiles, array_unique(array_map('basename', $imageFiles))));
        return json_encode(array_slice($imageFiles, 0, 1500)); // siehe Limit oben
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

            // Ton (2026-10-11): erst mit Ton starten; blockiert der Browser das,
            // stumm starten und beim ersten Klick/Tippen/Taste einschalten.
            // Der Knopf haengt am Elternelement, damit er beim Fingerzoom nicht mitskaliert.
            function auroraTonStart(v) {
                var knopf = document.getElementById('aurora-ton-knopf');
                if (!knopf) {
                    knopf = document.createElement('button');
                    knopf.type = 'button';
                    knopf.id = 'aurora-ton-knopf';
                    var wrap = document.getElementById('live-video-wrapper');
                    var halter = wrap && wrap.parentElement ? wrap.parentElement : v.parentElement;
                    if (window.getComputedStyle(halter).position === 'static') halter.style.position = 'relative';
                    halter.appendChild(knopf);
                }
                var ereignisse = ['pointerdown', 'click', 'keydown', 'touchend'];
                var texte = { de: ['Ton an', 'Ton aus'], en: ['Sound on', 'Sound off'], it: ['Audio on', 'Audio off'],
                              fr: ['Son activé', 'Son coupé'], es: ['Activar sonido', 'Silenciar'], zh: ['开启声音', '静音'] };
                function zeigen() {
                    var t = texte[(document.documentElement.lang || 'de').slice(0, 2)] || texte.de;
                    knopf.textContent = v.muted ? '🔇 ' + t[0] : '🔊';
                    knopf.setAttribute('aria-label', v.muted ? t[0] : t[1]);
                    knopf.title = v.muted ? t[0] : t[1];
                    knopf.classList.toggle('stumm', v.muted);
                }
                function aufraeumen() {
                    ereignisse.forEach(function (t) { document.removeEventListener(t, freigeben, true); });
                }
                function freigeben(e) {
                    if (e && e.target === knopf) return;
                    if (v.muted) { v.muted = false; v.play().catch(console.log); }
                    aufraeumen();
                    zeigen();
                }
                knopf.onclick = function (e) {
                    e.stopPropagation();
                    aufraeumen();
                    v.muted = !v.muted;
                    if (!v.muted) v.play().catch(console.log);
                    zeigen();
                };
                v.addEventListener('volumechange', zeigen);
                new MutationObserver(zeigen).observe(document.documentElement, { attributes: true, attributeFilter: ['lang'] });
                v.muted = false;
                v.play().then(zeigen).catch(function () {
                    v.muted = true;
                    v.play().catch(console.log);
                    zeigen();
                    ereignisse.forEach(function (t) { document.addEventListener(t, freigeben, true); });
                });
                zeigen();
            }

            // Gleichlauf (2026-10-14): Webseite und App zeigen das Bild gleich weit hinter
            // der Kamera, gemessen an der Uhrzeit im Segment (EXT-X-PROGRAM-DATE-TIME),
            // also an derselben Uhr wie die eingeblendete Zeit. Gleicher Wert wie
            // LIVE_VERZUG_MS in der App (LiveScreen.java).
            var AURORA_VERZUG_S = 8;
            function auroraGleichlauf(v, bildzeit) {
                setInterval(function () {
                    if (v.paused) return;
                    var t = bildzeit();
                    if (!t) return;
                    var abweichung = (Date.now() - t) / 1000 - AURORA_VERZUG_S;
                    if (Math.abs(abweichung) > 6 && v.seekable.length) {
                        var ziel = v.currentTime + abweichung;
                        var ende = v.seekable.end(v.seekable.length - 1) - 1;
                        v.currentTime = Math.max(v.seekable.start(0), Math.min(ziel, ende));
                        v.playbackRate = 1;
                    } else if (abweichung > 0.5) {
                        v.playbackRate = 1.05;
                    } else if (abweichung < -0.5) {
                        v.playbackRate = 0.95;
                    } else {
                        v.playbackRate = 1;
                    }
                    v.dataset.verzug = (abweichung + AURORA_VERZUG_S).toFixed(1);
                }, 1000);
            }

            if(video) {
                video.controls = false;
                if (isIOS) {
                    video.src = videoSrc;
                    video.setAttribute('playsinline', '');
                    video.setAttribute('webkit-playsinline', '');
                    if(bitrateBadge) bitrateBadge.style.display = 'none';
                    video.addEventListener('loadedmetadata', function() { auroraTonStart(video); });
                    auroraGleichlauf(video, function () {
                        var start = video.getStartDate ? video.getStartDate().getTime() : NaN;
                        return isNaN(start) ? null : start + video.currentTime * 1000;
                    });
                } else if (Hls.isSupported()) {
                    var hls = new Hls({ enableWorker: !isMobile, lowLatencyMode: false, liveSyncDuration: AURORA_VERZUG_S });
                    auroraGleichlauf(video, function () { return hls.playingDate ? hls.playingDate.getTime() : null; });
                    hls.loadSource(videoSrc);
                    hls.attachMedia(video);
                    hls.on(Hls.Events.MANIFEST_PARSED, function () {
                        auroraTonStart(video);
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
    private $videoDirs;
    private $aiDir;
    private $monthNames;
    private $settingsManager;

    // AI-Kategorien mit Icons und Farben
    private $aiCategories = [
        'persons' => ['icon' => '👤', 'name' => 'Personen', 'color' => '#FFB74D'],
        'cars'    => ['icon' => '🚗', 'name' => 'Autos', 'color' => '#64B5F6'],
        'buses'   => ['icon' => '🚌', 'name' => 'Busse', 'color' => '#4DB6AC'],
        'cats'    => ['icon' => '🐱', 'name' => 'Katzen', 'color' => '#BA68C8'],
        'cows'    => ['icon' => '🐄', 'name' => 'Kühe', 'color' => '#A1887F'],
        'planes'  => ['icon' => '✈️', 'name' => 'Flugzeuge', 'color' => '#87CEEB'],
        'birds'   => ['icon' => '🐦', 'name' => 'Vögel', 'color' => '#98FB98'],
    ];

    public function __construct($videoDir = './videos/', $aiDir = './ai/', $settingsManager = null) {
        $this->videoDirs = is_array($videoDir) ? $videoDir : [$videoDir];
        $this->videoDir = $this->videoDirs[0];
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
        if ($this->settingsManager && !$this->settingsManager->isAIEventsEnabled()) {
            return [];
        }

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
     * Prüft ob AI-Events für einen Monat existieren
     */
    public function hasAiEventsForMonth($year, $month) {
        $monthStr = sprintf('%04d%02d', $year, $month);

        foreach (array_keys($this->aiCategories) as $category) {
            $pattern = $this->aiDir . $category . "/{$category}_{$monthStr}*.mp4";
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
    
    /**
 * Wandelt Dateisystem-Pfad in Web-URL um
 */
private function videoPathToUrl($path) {
    // Remote-Videos: HLS-Stream wenn verfügbar, sonst Fallback auf direkten Pfad
    if (strpos($path, '/mnt/aurora-remote/videos/') === 0) {
        $basename = basename($path, '.mp4');
        $videoId = str_replace('daily_video_', '', $basename);
        $hlsMaster = '/mnt/aurora-remote/hls/' . $videoId . '/master.m3u8';
        if (file_exists($hlsMaster)) {
            return '/remote-hls/' . $videoId . '/master.m3u8';
        }
        // Fallback: direkter Apache-Alias
        return str_replace('/mnt/aurora-remote/videos/', '/remote-videos/', $path);
    }
    return $path;
}

    public function getVideosForDate($year, $month, $day) {
        global $remoteCache;
        $videos = [];
        $dateStr = sprintf('%04d%02d%02d', $year, $month, $day);

        // Lokale Videos direkt (schnell)
        foreach (glob($this->videoDirs[0] . "daily_video_{$dateStr}_*.mp4") as $video) {
            $videos[] = [
                'path' => $video,
                'filename' => basename($video),
                'filesize' => filesize($video),
                'time' => date('H:i', filemtime($video))
            ];
        }

        // Remote-Videos aus Cache (kein glob auf SMB!)
        foreach ($remoteCache->getVideosForDate($dateStr) as $rv) {
            $videos[] = [
                'path' => $rv['path'],
                'filename' => $rv['filename'],
                'filesize' => $rv['filesize'],
                'time' => date('H:i', $rv['mtime'])
            ];
        }

        return $videos;
    }

    public function hasVideosForDate($year, $month, $day) {
        global $remoteCache;
        $dateStr = sprintf('%04d%02d%02d', $year, $month, $day);
        // Lokal prüfen (schnell)
        if (count(glob($this->videoDirs[0] . "daily_video_{$dateStr}_*.mp4")) > 0) return true;
        // Remote aus Cache
        return $remoteCache->hasVideosForDate($dateStr);
    }

    public function displayVisualCalendar() {
        global $adminManager;

        $currentYear = isset($_GET['cal_year']) ? intval($_GET['cal_year']) : date('Y');
        $currentMonth = isset($_GET['cal_month']) ? intval($_GET['cal_month']) : date('n');
        $selectedDay = isset($_GET['cal_day']) ? intval($_GET['cal_day']) : null;
        $isAdmin = isset($adminManager) && $adminManager->isAdmin();

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
        
        //$output .= '<div class="ai-legend">';  ai wieder einblende das unten rausnehmendas rein
        $showAiLegend = (!$this->settingsManager || $this->settingsManager->isAIEventsEnabled()) &&
            $this->hasAiEventsForMonth($currentYear, $currentMonth);
        $output .= '<div class="ai-legend" style="display:' . ($showAiLegend ? 'block' : 'none') . ';">'; 
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
            $hasAiEvents = $showAiLegend ? $this->hasAiEventsForDate($currentYear, $currentMonth, $day) : false;
            $aiIcons = $showAiLegend ? $this->getAiIconsForDate($currentYear, $currentMonth, $day) : [];
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
            $aiEvents = $showAiLegend ? $this->getAiEventsForDate($currentYear, $currentMonth, $selectedDay) : [];

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
                    $videoWebUrl = $this->videoPathToUrl($video['path']);
                    $deleteToken = hash_hmac('sha256', 'delete_daily_video|' . $video['path'], session_id());

                    $output .= '<li>';
                    $output .= '<span class="video-time">🕐 ' . $video['time'] . ' Uhr</span>';
                    $output .= '<span class="video-size">' . $sizeInMb . ' MB</span>';
                    $output .= '<div class="video-actions">';

                    // Play Button (wenn aktiviert)
                    if ($playInPlayer) {
                      $output .= '<a href="#" onclick="DailyVideoPlayer.playVideo(\'' . htmlspecialchars($videoWebUrl) . '\', ' . ($allowDownload ? 'true' : 'false') . '); return false;" class="play-link">';

                        $output .= '▶️ Abspielen';
                        $output .= '</a>';
                    }

                    // Download Button (wenn aktiviert)
                    if ($allowDownload) {
                        $output .= '<a href="' . $videoUrl . '" class="download-link">';
                        $output .= '⬇️ Download';
                        $output .= '</a>';
                    }

                    if ($isAdmin) {
                        $output .= '<form method="post" class="video-delete-form" onsubmit="return confirm(\'Tagesvideo wirklich löschen?\');">';
                        $output .= '<input type="hidden" name="action" value="delete_daily_video">';
                        $output .= '<input type="hidden" name="video_path" value="' . htmlspecialchars($video['path'], ENT_QUOTES) . '">';
                        $output .= '<input type="hidden" name="delete_token" value="' . htmlspecialchars($deleteToken, ENT_QUOTES) . '">';
                        $output .= '<input type="hidden" name="cal_year" value="' . $currentYear . '">';
                        $output .= '<input type="hidden" name="cal_month" value="' . $currentMonth . '">';
                        $output .= '<input type="hidden" name="cal_day" value="' . $selectedDay . '">';
                        $output .= '<button type="submit" class="video-delete-link">🗑️ Löschen</button>';
                        $output .= '</form>';
                    }

                    $output .= '</div>';
                    $output .= '</li>';
                }

                $output .= '</ul>';
                $output .= '</div>';
            }

            // === AI-EREIGNISSE ===
            if (!empty($aiEvents) && (!$this->settingsManager || $this->settingsManager->isAIEventsEnabled())) {
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
    // Schutz vor Spam (2026-09-27): vom 23. bis 27.09. kamen 32 Casino-/Wett-
    // Eintraege mit [url=...]-Links, alle von Bots. Mehrere Stufen, die echte
    // Besucher nicht bemerken: Honigtopf-Feld, signiertes Zeit-Token (min. 4 s,
    // max. 2 h), keine Links, Spam-Begriffe, Tempolimit je IP, Laengengrenzen.
    // Ausgabe wird escaped (vorher ungefiltert: gespeichertes XSS moeglich).
    // Seit 2026-09-27 zusaetzlich Freigabe: neue Eintraege sind erst nach
    // Freigabe durch einen Admin oeffentlich; der Admin bekommt eine Mail.
    // Die Datei liegt ausserhalb des Webroots, sonst waeren wartende
    // Eintraege ueber /guestbook.json lesbar.
    private $entries = [];
    private $dbFile = '/var/www/guestbook/guestbook.json';
    private const KEY_FILE = '/etc/caurora/guestbook.key';
    private const PROXY_IP = '192.168.178.111';
    private const MIN_SECONDS = 4;
    private const MAX_SECONDS = 7200;
    private const MAX_PER_HOUR = 3;
    private const MAX_NAME = 40;
    private const MAX_MESSAGE = 800;
    private const SPAM_WORDS = [
        'casino', 'mostbet', 'melbet', '1win', '1вин', 'мостбет', 'pinco', 'aviator', 'olymp',
        'chicken road', 'chicken-road', 'plinko', 'lucky jet', 'bet365', 'slots', 'freespin',
        'free spins', 'bonus code', 'crypto', 'bitcoin', 'viagra', 'cialis', 'loan', 'porn',
        'ставк', 'казино', 'букмекер', 'вывод', 'депозит',
    ];

    public function __construct() {
        if (file_exists($this->dbFile)) {
            $this->entries = json_decode(file_get_contents($this->dbFile), true) ?: [];
        }
    }

    public function handleFormSubmission() {
        if (!isset($_POST['guestbook'], $_POST['guest-name'], $_POST['guest-message'])) {
            return;
        }
        $name = trim((string) $_POST['guest-name']);
        $message = trim((string) $_POST['guest-message']);
        $reason = $this->spamReason($name, $message);
        if ($reason !== null) {
            $this->logBlocked($reason);
            // Bots bekommen keine Rueckmeldung. Echte Besucher, die nur einen
            // Link oder zu viel Text eingetragen haben, sehen einen Hinweis.
            if (in_array($reason, ['link', 'laenge', 'tempo'], true)) {
                $_SESSION['guestbook_notice'] = $reason;
            }
            return;
        }
        $this->addEntry($name, $message);
        $this->saveEntries();
        $this->rememberPost();
        $_SESSION['guestbook_notice'] = 'freigabe';
        $this->notifyAdmin($name, $message);
    }

    private function notifyAdmin(string $name, string $message): void {
        global $appSecrets;
        $gmail = $appSecrets['gmail'] ?? [];
        if (empty($gmail['admin_email']) || empty($gmail['user']) || empty($gmail['app_password'])) {
            return;
        }
        try {
            $mail = new \PHPMailer\PHPMailer\PHPMailer(true);
            $mail->isSMTP();
            $mail->Host = 'smtp.gmail.com';
            $mail->SMTPAuth = true;
            $mail->Username = $gmail['user'];
            $mail->Password = $gmail['app_password'];
            $mail->SMTPSecure = \PHPMailer\PHPMailer\PHPMailer::ENCRYPTION_STARTTLS;
            $mail->Port = 587;
            $mail->Timeout = 10;
            $mail->CharSet = 'UTF-8';
            $mail->setFrom($gmail['user'], 'Aurora Livecam');
            $mail->addAddress($gmail['admin_email']);
            $mail->Subject = 'Gästebuch: neuer Eintrag wartet auf Freigabe';
            $mail->Body = "Von: {$name}\n\n{$message}\n\nFreigeben oder löschen: als Admin anmelden, Abschnitt Gästebuch.";
            $mail->send();
        } catch (\Throwable $e) {
            error_log('Gaestebuch-Benachrichtigung fehlgeschlagen: ' . $e->getMessage());
        }
    }

    public function approveEntry($index) {
        if (isset($this->entries[$index])) {
            $this->entries[$index]['approved'] = true;
            $this->saveEntries();
            return true;
        }
        return false;
    }

    public function pendingCount(): int {
        return count(array_filter($this->entries, fn($e) => ($e['approved'] ?? true) === false));
    }

    private function spamReason(string $name, string $message): ?string {
        if (!empty($_POST['website'])) {
            return 'honigtopf';
        }
        $issued = $this->verifyToken((string) ($_POST['gb_token'] ?? ''));
        if ($issued === null) {
            return 'token';
        }
        $age = time() - $issued;
        if ($age < self::MIN_SECONDS || $age > self::MAX_SECONDS) {
            return 'zeit';
        }
        if ($name === '' || $message === '' || mb_strlen($name) > self::MAX_NAME || mb_strlen($message) > self::MAX_MESSAGE) {
            return 'laenge';
        }
        $text = mb_strtolower($name . ' ' . $message);
        if (preg_match('~\[url|\[link|https?://|www\.|<a\s|\b[a-z0-9-]+\.(com|net|org|ru|kg|shop|xyz|top|site|online|info|biz|io|club|pro|app|link|cc|tk)\b~iu', $text)) {
            return 'link';
        }
        foreach (self::SPAM_WORDS as $word) {
            if (mb_strpos($text, $word) !== false) {
                return 'begriff';
            }
        }
        // Namen wie "mostbet_yxpt": Marke + Unterstrich + Zufallskuerzel.
        if (preg_match('~^[a-z0-9-]+_[A-Za-z]{4}$~', $name)) {
            return 'name';
        }
        if ($this->recentPosts() >= self::MAX_PER_HOUR) {
            return 'tempo';
        }
        return null;
    }

    // ---------------------------------------------------------------- Token

    private function key(): string {
        $key = @file_get_contents(self::KEY_FILE);
        return $key !== false && strlen(trim($key)) >= 32 ? trim($key) : hash('sha256', __FILE__ . php_uname());
    }

    private function token(): string {
        $time = (string) time();
        return $time . '.' . hash_hmac('sha256', $time, $this->key());
    }

    private function verifyToken(string $token): ?int {
        if (!preg_match('~^(\d{9,11})\.([a-f0-9]{64})$~', $token, $m)) {
            return null;
        }
        return hash_equals(hash_hmac('sha256', $m[1], $this->key()), $m[2]) ? (int) $m[1] : null;
    }

    // ---------------------------------------------------------------- Tempo

    private function clientIp(): string {
        $remote = $_SERVER['REMOTE_ADDR'] ?? '';
        // Nur dem eigenen Reverse Proxy glauben, sonst waere die IP faelschbar.
        if ($remote === self::PROXY_IP && !empty($_SERVER['HTTP_X_REAL_IP'])) {
            return (string) $_SERVER['HTTP_X_REAL_IP'];
        }
        return $remote;
    }

    private function rateFile(): string {
        return sys_get_temp_dir() . '/caurora_guestbook_rate.json';
    }

    private function recentPosts(): int {
        $data = json_decode((string) @file_get_contents($this->rateFile()), true) ?: [];
        $ip = hash('sha256', $this->clientIp()); // keine Klartext-IPs speichern
        return count(array_filter($data[$ip] ?? [], fn($t) => $t > time() - 3600));
    }

    private function rememberPost(): void {
        $file = $this->rateFile();
        $data = json_decode((string) @file_get_contents($file), true) ?: [];
        $ip = hash('sha256', $this->clientIp());
        $data[$ip][] = time();
        foreach ($data as $k => $times) {
            $data[$k] = array_values(array_filter($times, fn($t) => $t > time() - 3600));
            if (!$data[$k]) unset($data[$k]);
        }
        @file_put_contents($file, json_encode($data), LOCK_EX);
    }

    private function logBlocked(string $reason): void {
        @file_put_contents(sys_get_temp_dir() . '/caurora_guestbook_blocked.log',
            date('Y-m-d H:i:s') . ' ' . $reason . "\n", FILE_APPEND | LOCK_EX);
    }

    // ---------------------------------------------------------------- Daten

    private function addEntry($name, $message) {
        $this->entries[] = [
            'name' => $name,
            'message' => $message,
            'date' => date('Y-m-d H:i:s'),
            'approved' => false,
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
        file_put_contents($this->dbFile, json_encode($this->entries, JSON_UNESCAPED_UNICODE), LOCK_EX);
    }

    public function displayForm() {
        $notice = '';
        $reason = $_SESSION['guestbook_notice'] ?? null;
        unset($_SESSION['guestbook_notice']);
        $texts = [
            'link' => ['Bitte ohne Links oder Webadressen.', 'Please leave out links and web addresses.'],
            'laenge' => ['Name bis 40 und Nachricht bis 800 Zeichen.', 'Name up to 40 and message up to 800 characters.'],
            'tempo' => ['Bitte etwas später erneut versuchen.', 'Please try again a little later.'],
            'freigabe' => ['Danke! Dein Eintrag erscheint, sobald er freigegeben ist.', 'Thank you! Your entry will appear once it has been approved.'],
        ];
        if (isset($texts[$reason])) {
            $notice = '<p class="guestbook-notice" data-de="' . $texts[$reason][0] . '" data-en="' . $texts[$reason][1] . '">'
                . $texts[$reason][0] . '</p>';
        }
        return $notice . '
        <form method="post">
            <input type="hidden" name="guestbook" value="1">
            <input type="hidden" name="gb_token" value="' . $this->token() . '">
            <div style="position:absolute;left:-10000px;top:auto;width:1px;height:1px;overflow:hidden;" aria-hidden="true">
                <label for="gb-website">Website</label>
                <input type="text" id="gb-website" name="website" tabindex="-1" autocomplete="off">
            </div>

          <label for="guest-name"
       data-en="Name:"
       data-de="Name:"
       data-it="Nome:"
       data-fr="Nom:" data-es="Nombre:"
       data-zh="姓名：">
    Name:
</label>
<input type="text" id="guest-name" name="guest-name" maxlength="' . self::MAX_NAME . '" required>
<label for="guest-message"
       data-en="Message:"
       data-de="Nachricht:"
       data-it="Messaggio:"
       data-fr="Message :" data-es="Mensaje:"
       data-zh="留言：">
    Nachricht:
</label>
<textarea id="guest-message" name="guest-message" maxlength="' . self::MAX_MESSAGE . '" required></textarea>
<button type="submit"
        data-en="Add Entry"
        data-de="Eintrag hinzufügen"
        data-it="Aggiungi Voce"
        data-fr="Ajouter une entrée" data-es="Añadir entrada"
        data-zh="添加留言">
    Eintrag hinzufügen
</button>

        </form>';
    }
public function displayEntries($isAdmin = false) {
    $output = '<div id="guestbook-entries">';
    if ($isAdmin && $this->pendingCount() > 0) {
        $output .= "<p class='guestbook-notice'><strong>" . $this->pendingCount() . " Eintrag/Einträge warten auf Freigabe.</strong></p>";
    }
    // Neueste zuerst; der Index bleibt der Speicherindex (fuer Freigabe/Loeschen).
    foreach (array_reverse($this->entries, true) as $index => $entry) {
        $approved = ($entry['approved'] ?? true) !== false; // Alt-Eintraege gelten als freigegeben
        if (!$approved && !$isAdmin) {
            continue;
        }
        $name = htmlspecialchars((string) ($entry['name'] ?? ''), ENT_QUOTES, 'UTF-8');
        $message = nl2br(htmlspecialchars((string) ($entry['message'] ?? ''), ENT_QUOTES, 'UTF-8'));
        $date = htmlspecialchars((string) ($entry['date'] ?? ''), ENT_QUOTES, 'UTF-8');
        $index = (int) $index;
        $pending = $approved ? '' : " style='border-left:4px solid #e3a32d;opacity:.85'";
        $output .= "
        <div class='guestbook-entry'{$pending}>" . ($approved ? '' : "<small><strong>Wartet auf Freigabe</strong></small>") . "
            <h4><i class='fas fa-user'></i> {$name}</h4>
            <p><i class='fas fa-comment'></i> {$message}</p>
            <small><i class='fas fa-clock'></i> {$date}</small>";
            if ($isAdmin && !$approved) {
                $output .= "<form method='post' style='display:inline;'>
                    <input type='hidden' name='action' value='approve_guestbook'>
                    <input type='hidden' name='approve_entry' value='{$index}'>
                    <button type='submit' class='delete-btn' style='background:#2e7d32'>Freigeben</button>
                </form> ";
            }
            if ($isAdmin) {
                $output .= "<form method='post' style='display:inline;'>
                    <input type='hidden' name='action' value='delete_guestbook'>
                    <input type='hidden' name='delete_entry' value='{$index}'>
                    <button type='submit' class='delete-btn'>Löschen</button>
                </form>";
            }
        $output .= "</div>";
    }
    $output .= '</div>';
    return $output;
}


}


class ContactManager {
    private $adminEmail;
    private $feedbackFile = 'feedbacks.json';
    private $gmailUser;
    private $gmailAppPassword;

    public function __construct() {
        global $appSecrets;
        $gmail = $appSecrets['gmail'] ?? [];
        $this->adminEmail = $gmail['admin_email'] ?? '';
        $this->gmailUser = $gmail['user'] ?? '';
        $this->gmailAppPassword = $gmail['app_password'] ?? '';
    }

    public function displayForm() {
        return '
        <form method="post" id="contact-form">
            <input type="hidden" name="contact" value="1">
            <label for="name"
                   data-en="Name:"
                   data-de="Name:"
                   data-it="Nome:"
                   data-fr="Nom:" data-es="Nombre:"
                   data-zh="姓名：">
                Name:
            </label>
            <input type="text" id="name" name="name" required minlength="2">

            <label for="email"
                   data-en="E-Mail:"
                   data-de="E-Mail:"
                   data-it="Email:"
                   data-fr="E-mail:" data-es="Correo electrónico:"
                   data-zh="电子邮件：">
                E-Mail:
            </label>
            <input type="email" id="email" name="email" required>

            <label for="message"
                   data-en="Message:"
                   data-de="Nachricht:"
                   data-it="Messaggio:"
                   data-fr="Message:" data-es="Mensaje:"
                   data-zh="消息：">
                Nachricht:
            </label>
            <textarea id="message" name="message" required minlength="10"></textarea>

            <button type="submit"
                    data-en="Send Message"
                    data-de="Nachricht senden"
                    data-it="Invia Messaggio"
                    data-fr="Envoyer le message" data-es="Enviar mensaje"
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
            $mail->SMTPDebug = 0; // Stufe 2 schrieb ganze Kontaktanfragen ins error.log
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
                    <p style='margin-top: 15px;'>Diese E-Mail wurde automatisch vom Kontaktformular auf<br><a href='https://www.aurora-weather-livecam.com' style='color: #667eea;'>www.aurora-weather-livecam.com</a> generiert.</p>
                </div>
            </div>
        </body>
        </html>";
    }
}


class AdminManager {
    private $authFile = '/var/www/admin_auth.json';
    private $adminEmail;
    private $gmailUser;
    private $gmailAppPassword;

    public function __construct() {
        global $appSecrets;
        $gmail = $appSecrets['gmail'] ?? [];
        $this->adminEmail = $gmail['admin_email'] ?? '';
        $this->gmailUser = $gmail['user'] ?? '';
        $this->gmailAppPassword = $gmail['app_password'] ?? '';
    }

    private function loadAuth(): array {
        if (!file_exists($this->authFile)) {
            return [
                'username' => 'admin',
                'password_hash' => '',
                'reset_token' => null,
                'reset_expires' => null,
            ];
        }
        $data = json_decode(file_get_contents($this->authFile), true);
        return is_array($data) ? $data : [];
    }

    private function saveAuth(array $auth): bool {
        $auth['updated_at'] = date('c');
        return file_put_contents($this->authFile, json_encode($auth, JSON_PRETTY_PRINT | JSON_UNESCAPED_SLASHES), LOCK_EX) !== false;
    }

    private function setFlash(string $message, string $type = 'info'): void {
        $_SESSION['admin_message'] = ['text' => $message, 'type' => $type];
    }

    private function consumeFlash(): ?array {
        if (!isset($_SESSION['admin_message'])) {
            return null;
        }
        $message = $_SESSION['admin_message'];
        unset($_SESSION['admin_message']);
        return is_array($message) ? $message : null;
    }

    private function buildResetLink(string $token): string {
        $scheme = (!empty($_SERVER['HTTPS']) && $_SERVER['HTTPS'] !== 'off') ? 'https' : 'http';
        global $publicBaseUrl;
        $path = $_SERVER['PHP_SELF'] ?? '/index.php';
        return rtrim($publicBaseUrl, '/') . $path . '?admin_reset=' . urlencode($token) . '#admin';
    }

    private function sendResetEmail(string $token): bool {
        if ($this->gmailUser === '' || $this->gmailAppPassword === '' || $this->adminEmail === '') {
            return false;
        }
        $mail = new PHPMailer(true);
        try {
            $mail->isSMTP();
            $mail->Host = 'smtp.gmail.com';
            $mail->SMTPAuth = true;
            $mail->Username = $this->gmailUser;
            $mail->Password = $this->gmailAppPassword;
            $mail->SMTPSecure = PHPMailer::ENCRYPTION_STARTTLS;
            $mail->Port = 587;
            $mail->CharSet = 'UTF-8';
            $mail->setFrom($this->gmailUser, 'Aurora Admin');
            $mail->addAddress($this->adminEmail);
            $mail->isHTML(true);
            $resetLink = $this->buildResetLink($token);
            $mail->Subject = 'Aurora Admin Passwort zurücksetzen';
            $mail->Body = '<p>Es wurde eine Zurücksetzung für den Aurora-Admin angefordert.</p><p><a href="' . htmlspecialchars($resetLink, ENT_QUOTES, 'UTF-8') . '">Neues Passwort setzen</a></p><p>Der Link ist 60 Minuten gültig.</p>';
            $mail->AltBody = "Passwort zurücksetzen: $resetLink\nDer Link ist 60 Minuten gültig.";
            return $mail->send();
        } catch (Exception $e) {
            error_log('Admin reset mail error: ' . $e->getMessage());
            return false;
        }
    }

    public function isAdmin() {
        return isset($_SESSION['admin']) && $_SESSION['admin'] === true;
    }

    // Fehlversuche je IP (Hash) drosseln (2026-09-28): 5 in 15 Minuten.
    private function loginFailFile(): string {
        return sys_get_temp_dir() . '/caurora_admin_login_fail.json';
    }

    private function clientKey(): string {
        $ip = $_SERVER['REMOTE_ADDR'] ?? '';
        if ($ip === '192.168.178.111' && !empty($_SERVER['HTTP_X_REAL_IP'])) {
            $ip = (string) $_SERVER['HTTP_X_REAL_IP']; // nur dem eigenen Proxy glauben
        }
        return hash('sha256', $ip);
    }

    private function recentLoginFailures(): array {
        $data = json_decode((string) @file_get_contents($this->loginFailFile()), true) ?: [];
        $cutoff = time() - 900;
        foreach ($data as $k => $times) {
            $data[$k] = array_values(array_filter((array) $times, fn($t) => $t > $cutoff));
            if (!$data[$k]) unset($data[$k]);
        }
        return $data;
    }

    public function handleLogin($username, $password) {
        $failures = $this->recentLoginFailures();
        $key = $this->clientKey();
        if (count($failures[$key] ?? []) >= 5) {
            $this->setFlash('Zu viele Fehlversuche. Bitte 15 Minuten warten.', 'error');
            return false;
        }
        $auth = $this->loadAuth();
        $validUser = hash_equals((string) ($auth['username'] ?? ''), (string) $username);
        $validPassword = isset($auth['password_hash']) && password_verify((string) $password, (string) $auth['password_hash']);
        if ($validUser && $validPassword) {
            session_regenerate_id(true); // Schutz gegen Session Fixation
            unset($failures[$key]);
            @file_put_contents($this->loginFailFile(), json_encode($failures), LOCK_EX);
            $_SESSION['admin'] = true;
            $this->setFlash('Admin-Login erfolgreich.', 'success');
            return true;
        }
        $failures[$key][] = time();
        @file_put_contents($this->loginFailFile(), json_encode($failures), LOCK_EX);
        $this->setFlash('Login fehlgeschlagen.', 'error');
        return false;
    }

    public function requestPasswordReset(): bool {
        $auth = $this->loadAuth();
        // Hoechstens eine Reset-Mail alle 10 Minuten (sonst Mail-Flut per Knopfdruck).
        if (!empty($auth['reset_expires']) && ((int) $auth['reset_expires'] - 3600) > time() - 600) {
            $this->setFlash('Reset-Mail wurde gerade erst angefordert. Bitte Postfach prüfen.', 'error');
            return false;
        }
        $token = bin2hex(random_bytes(32));
        $auth['reset_token'] = hash('sha256', $token);
        $auth['reset_expires'] = time() + 3600;
        if (!$this->saveAuth($auth)) {
            $this->setFlash('Reset konnte nicht gespeichert werden.', 'error');
            return false;
        }
        if ($this->sendResetEmail($token)) {
            $this->setFlash('Reset-Link wurde per Mail gesendet.', 'success');
            return true;
        }
        $this->setFlash('Reset-Mail konnte nicht gesendet werden.', 'error');
        return false;
    }

    public function isResetTokenValid(?string $token): bool {
        if (!$token) return false;
        $auth = $this->loadAuth();
        if (empty($auth['reset_token']) || empty($auth['reset_expires'])) return false;
        if ((int) $auth['reset_expires'] < time()) return false;
        return hash_equals((string) $auth['reset_token'], hash('sha256', $token));
    }

    public function handlePasswordReset(string $token, string $newPassword, string $confirmPassword): bool {
        if (!$this->isResetTokenValid($token)) {
            $this->setFlash('Reset-Link ist ungültig oder abgelaufen.', 'error');
            return false;
        }
        if ($newPassword !== $confirmPassword) {
            $this->setFlash('Die Passwörter stimmen nicht überein.', 'error');
            return false;
        }
        if (strlen($newPassword) < 12) {
            $this->setFlash('Das neue Passwort muss mindestens 12 Zeichen haben.', 'error');
            return false;
        }
        $auth = $this->loadAuth();
        $auth['password_hash'] = password_hash($newPassword, PASSWORD_DEFAULT);
        $auth['reset_token'] = null;
        $auth['reset_expires'] = null;
        if ($this->saveAuth($auth)) {
            $this->setFlash('Passwort wurde erfolgreich geändert.', 'success');
            return true;
        }
        $this->setFlash('Passwort konnte nicht gespeichert werden.', 'error');
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
        $flash = $this->consumeFlash();
        $flashHtml = '';
        if ($flash) {
            $bg = ($flash['type'] ?? '') === 'success' ? '#e8f7ee' : '#fdecea';
            $color = ($flash['type'] ?? '') === 'success' ? '#1f7a3d' : '#a12622';
            $flashHtml = '<div style="margin-bottom:15px;padding:12px 14px;border-radius:8px;background:' . $bg . ';color:' . $color . ';font-weight:600;">' . htmlspecialchars((string) ($flash['text'] ?? ''), ENT_QUOTES, 'UTF-8') . '</div>';
        }

        $resetToken = isset($_GET['admin_reset']) ? (string) $_GET['admin_reset'] : '';
        if ($resetToken !== '') {
            if ($this->isResetTokenValid($resetToken)) {
                return $flashHtml . '
                <form id="admin-reset-form" method="post">
                    <input type="hidden" name="admin-reset-submit" value="1">
                    <input type="hidden" name="reset_token" value="' . htmlspecialchars($resetToken, ENT_QUOTES, 'UTF-8') . '">
                    <label>Neues Passwort:</label><input type="password" name="new_password" minlength="12" required>
                    <label>Neues Passwort wiederholen:</label><input type="password" name="new_password_confirm" minlength="12" required>
                    <button type="submit">Passwort setzen</button>
                </form>';
            }
            $flashHtml .= '<div style="margin-bottom:15px;padding:12px 14px;border-radius:8px;background:#fdecea;color:#a12622;font-weight:600;">Reset-Link ist ungültig oder abgelaufen.</div>';
        }

        return $flashHtml . '
        <form id="login-form" method="post">
            <input type="hidden" name="admin-login" value="1">
            <label>Benutzername:</label><input type="text" name="username" required>
            <label>Passwort:</label><input type="password" name="password" required>
            <button type="submit">Einloggen</button>
        </form>
        <form method="post" style="margin-top: 12px;">
            <input type="hidden" name="admin-reset-request" value="1">
            <button type="submit" style="background:#f3f4f6;color:#333;">Passwort vergessen</button>
        </form>';
    }

    public function displayAdminContent() {
        global $settingsManager, $siteConfig;

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

        // UI Display Settings (Punkt 2)
        $output .= '<div class="settings-group">';
        $output .= '<h4>🖼️ UI Anzeige</h4>';

        $output .= '<div class="setting-row">';
        $output .= '<span class="setting-label">Empfehlungs-Banner anzeigen</span>';
        $output .= '<div class="setting-input">';
        $output .= '<label class="toggle-switch">';
        $output .= '<input type="checkbox" id="setting-show-banner" ' . ($settingsManager->get('ui_display.show_recommendation_banner') ? 'checked' : '') . '>';
        $output .= '<span class="toggle-slider"></span>';
        $output .= '</label>';
        $output .= '</div>';
        $output .= '</div>';

        $output .= '<div class="setting-row">';
        $output .= '<span class="setting-label">QR-Code Section anzeigen</span>';
        $output .= '<div class="setting-input">';
        $output .= '<label class="toggle-switch">';
        $output .= '<input type="checkbox" id="setting-show-qr" ' . ($settingsManager->get('ui_display.show_qr_code') ? 'checked' : '') . '>';
        $output .= '<span class="toggle-slider"></span>';
        $output .= '</label>';
        $output .= '</div>';
        $output .= '</div>';

        $output .= '<div class="setting-row">';
        $output .= '<span class="setting-label">Social Media Links anzeigen</span>';
        $output .= '<div class="setting-input">';
        $output .= '<label class="toggle-switch">';
        $output .= '<input type="checkbox" id="setting-show-social" ' . ($settingsManager->get('ui_display.show_social_media') ? 'checked' : '') . '>';
        $output .= '<span class="toggle-slider"></span>';
        $output .= '</label>';
        $output .= '</div>';
        $output .= '</div>';

        $output .= '<div class="setting-row">';
        $output .= '<span class="setting-label">Instagram URL</span>';
        $output .= '<div class="setting-input">';
        $output .= '<input type="url" id="setting-social-instagram" class="text-input" placeholder="https://www.instagram.com/deinprofil" value="' . htmlspecialchars((string) ($settingsManager->get('social_links.instagram') ?? '')) . '">';
        $output .= '</div>';
        $output .= '</div>';

        $output .= '<div class="setting-row">';
        $output .= '<span class="setting-label">Facebook URL</span>';
        $output .= '<div class="setting-input">';
        $output .= '<input type="url" id="setting-social-facebook" class="text-input" placeholder="https://www.facebook.com/deineseite" value="' . htmlspecialchars((string) ($settingsManager->get('social_links.facebook') ?? '')) . '">';
        $output .= '</div>';
        $output .= '</div>';

        $output .= '<div class="setting-row">';
        $output .= '<span class="setting-label">YouTube URL</span>';
        $output .= '<div class="setting-input">';
        $output .= '<input type="url" id="setting-social-youtube" class="text-input" placeholder="https://www.youtube.com/@deinkanal" value="' . htmlspecialchars((string) ($settingsManager->get('social_links.youtube') ?? '')) . '">';
        $output .= '</div>';
        $output .= '</div>';

        $output .= '<div class="setting-row">';
        $output .= '<span class="setting-label">TikTok URL</span>';
        $output .= '<div class="setting-input">';
        $output .= '<input type="url" id="setting-social-tiktok" class="text-input" placeholder="https://www.tiktok.com/@deinprofil" value="' . htmlspecialchars((string) ($settingsManager->get('social_links.tiktok') ?? '')) . '">';
        $output .= '</div>';
        $output .= '</div>';

        $output .= '<div class="setting-row">';
        $output .= '<span class="setting-label">Buy me a Coffee URL</span>';
        $output .= '<div class="setting-input">';
        $output .= '<input type="url" id="setting-social-buymeacoffee" class="text-input" placeholder="https://buymeacoffee.com/deinname" value="' . htmlspecialchars((string) ($settingsManager->get('social_links.buymeacoffee') ?? '')) . '">';
        $output .= '</div>';
        $output .= '</div>';

        $output .= '<div class="setting-row">';
        $output .= '<span class="setting-label">PayPal-Spendenlink URL</span>';
        $output .= '<div class="setting-input">';
        $output .= '<input type="url" id="setting-social-paypal" class="text-input" placeholder="https://www.paypal.com/donate/?business=..." value="' . htmlspecialchars((string) ($settingsManager->get('social_links.paypal') ?? '')) . '">';
        $output .= '</div>';
        $output .= '</div>';

        $output .= '<div class="setting-row">';
        $output .= '<span class="setting-label">Patrouille Suisse Section anzeigen</span>';
        $output .= '<div class="setting-input">';
        $output .= '<label class="toggle-switch">';
        $output .= '<input type="checkbox" id="setting-show-patrouille" ' . ($settingsManager->get('ui_display.show_patrouille_suisse') ? 'checked' : '') . '>';
        $output .= '<span class="toggle-slider"></span>';
        $output .= '</label>';
        $output .= '</div>';
        $output .= '</div>';
        $output .= '</div>'; // settings-group

        // Zoom & Timelapse Settings (Punkt 3)
        $output .= '<div class="settings-group">';
        $output .= '<h4>🔍 Zoom & Timelapse</h4>';

        $output .= '<div class="setting-row">';
        $output .= '<span class="setting-label">Zoom-Controls anzeigen</span>';
        $output .= '<div class="setting-input">';
        $output .= '<label class="toggle-switch">';
        $output .= '<input type="checkbox" id="setting-show-zoom" ' . ($settingsManager->get('zoom_timelapse.show_zoom_controls') ? 'checked' : '') . '>';
        $output .= '<span class="toggle-slider"></span>';
        $output .= '</label>';
        $output .= '</div>';
        $output .= '</div>';

        $output .= '<div class="setting-row">';
        $output .= '<span class="setting-label">Max Zoom-Level</span>';
        $output .= '<div class="setting-input">';
        $output .= '<input type="number" id="setting-max-zoom" class="number-input" min="1.5" max="4.0" step="0.5" value="' . $settingsManager->get('zoom_timelapse.max_zoom_level') . '">';
        $output .= '</div>';
        $output .= '</div>';

        $output .= '<div class="setting-row">';
        $output .= '<span class="setting-label">Timelapse Rückwärts-Modus</span>';
        $output .= '<div class="setting-input">';
        $output .= '<label class="toggle-switch">';
        $output .= '<input type="checkbox" id="setting-timelapse-reverse" ' . ($settingsManager->get('zoom_timelapse.timelapse_reverse_enabled') ? 'checked' : '') . '>';
        $output .= '<span class="toggle-slider"></span>';
        $output .= '</label>';
        $output .= '</div>';
        $output .= '</div>';
        $output .= '</div>'; // settings-group

        // Content Management Settings (Punkt 5)
        $output .= '<div class="settings-group">';
        $output .= '<h4>📝 Content Management</h4>';

        $output .= '<div class="setting-row">';
        $output .= '<span class="setting-label">Gästebuch aktivieren</span>';
        $output .= '<div class="setting-input">';
        $output .= '<label class="toggle-switch">';
        $output .= '<input type="checkbox" id="setting-guestbook-enabled" ' . ($settingsManager->get('content.guestbook_enabled') ? 'checked' : '') . '>';
        $output .= '<span class="toggle-slider"></span>';
        $output .= '</label>';
        $output .= '</div>';
        $output .= '</div>';

        $output .= '<div class="setting-row">';
        $output .= '<span class="setting-label">Galerie aktivieren</span>';
        $output .= '<div class="setting-input">';
        $output .= '<label class="toggle-switch">';
        $output .= '<input type="checkbox" id="setting-gallery-enabled" ' . ($settingsManager->get('content.gallery_enabled') ? 'checked' : '') . '>';
        $output .= '<span class="toggle-slider"></span>';
        $output .= '</label>';
        $output .= '</div>';
        $output .= '</div>';

        $output .= '<div class="setting-row">';
        $output .= '<span class="setting-label">KI-Erkennung aktivieren</span>';
        $output .= '<div class="setting-input">';
        $output .= '<label class="toggle-switch">';
        $output .= '<input type="checkbox" id="setting-ai-detection-enabled" ' . ($settingsManager->get('content.ai_detection_enabled') !== false ? 'checked' : '') . '>';
        $output .= '<span class="toggle-slider"></span>';
        $output .= '</label>';
        $output .= '</div>';
        $output .= '</div>';

        $output .= '<div class="setting-row">';
        $output .= '<span class="setting-label">KI-Erkannte Bilder anzeigen</span>';
        $output .= '<div class="setting-input">';
        $output .= '<label class="toggle-switch">';
        $output .= '<input type="checkbox" id="setting-ai-events-enabled" ' . ($settingsManager->get('content.ai_events_enabled') ? 'checked' : '') . '>';
        $output .= '<span class="toggle-slider"></span>';
        $output .= '</label>';
        $output .= '</div>';
        $output .= '</div>';

        $output .= '<div class="setting-row">';
        $output .= '<span class="setting-label">Max Gästebuch-Einträge</span>';
        $output .= '<div class="setting-input">';
        $output .= '<input type="number" id="setting-max-guestbook" class="number-input" min="10" max="200" step="10" value="' . $settingsManager->get('content.max_guestbook_entries') . '">';
        $output .= '</div>';
        $output .= '</div>';

        $output .= '<div class="setting-row">';
        $output .= '<span class="setting-label">Hero-Titel (DE)</span>';
        $output .= '<div class="setting-input">';
        $output .= '<input type="text" id="setting-hero-welcome" class="text-input" placeholder="' . htmlspecialchars($siteConfig['welcomeDe']) . '" value="' . htmlspecialchars((string) ($settingsManager->get('content.hero_welcome_de') ?? '')) . '">';
        $output .= '</div>';
        $output .= '</div>';

        $output .= '<div class="setting-row">';
        $output .= '<span class="setting-label">Hero-Untertitel (DE)</span>';
        $output .= '<div class="setting-input">';
        $output .= '<textarea id="setting-hero-region-text" class="textarea-input" rows="2" placeholder="Erleben Sie faszinierende Ausblicke der Züricher Region - in Echtzeit!">' . htmlspecialchars((string) ($settingsManager->get('content.hero_region_text_de') ?? '')) . '</textarea>';
        $output .= '</div>';
        $output .= '</div>';

        $output .= '<div class="setting-row">';
        $output .= '<span class="setting-label">Ortsflagge ALT-Text (DE)</span>';
        $output .= '<div class="setting-input">';
        $output .= '<input type="text" id="setting-local-flag-alt" class="text-input" placeholder="Ortsflagge" value="' . htmlspecialchars((string) ($settingsManager->get('content.local_flag_alt_de') ?? '')) . '">';
        $output .= '</div>';
        $output .= '</div>';

        $output .= '<div class="setting-row">';
        $output .= '<span class="setting-label">Spenden-Text (DE)</span>';
        $output .= '<div class="setting-input">';
        $output .= '<textarea id="setting-donation-callout" class="textarea-input" rows="4" placeholder="Gefällt dir die Livecam? Unterstütze das Projekt ...">' . htmlspecialchars((string) ($settingsManager->get('content.donation_callout_de') ?? '')) . '</textarea>';
        $output .= '</div>';
        $output .= '</div>';

        $output .= '<div class="setting-row">';
        $output .= '<span class="setting-label">PayPal-Spendenknopf anzeigen</span>';
        $output .= '<div class="setting-input">';
        $output .= '<input type="checkbox" id="setting-paypal-enabled" ' . ($settingsManager->shouldShowPaypalDonation() ? 'checked' : '') . '>';
        $output .= '</div>';
        $output .= '</div>';

        $output .= '<div class="setting-row">';
        $output .= '<span class="setting-label">Buy-me-a-Coffee-Knopf anzeigen (alt)</span>';
        $output .= '<div class="setting-input">';
        $output .= '<input type="checkbox" id="setting-buymeacoffee-enabled" ' . ($settingsManager->shouldShowBuyMeACoffee() ? 'checked' : '') . '>';
        $output .= '</div>';
        $output .= '</div>';

        $output .= '<div class="setting-row">';
        $output .= '<span class="setting-label">TWINT-Spendenlink anzeigen</span>';
        $output .= '<div class="setting-input">';
        $output .= '<input type="checkbox" id="setting-twint-enabled" ' . ($settingsManager->get('content.twint_enabled') ? 'checked' : '') . '>';
        $output .= '</div>';
        $output .= '</div>';

        $output .= '<div class="setting-row">';
        $output .= '<span class="setting-label">TWINT-Infotext (DE)</span>';
        $output .= '<div class="setting-input">';
        $output .= '<textarea id="setting-twint-support-text" class="textarea-input" rows="4" placeholder="Unterstütze die Aurora Livecam mit deiner Spende...">' . htmlspecialchars((string) ($settingsManager->get('content.twint_support_text_de') ?? '')) . '</textarea>';
        $output .= '</div>';
        $output .= '</div>';
        $output .= '</div>'; // settings-group

        // Technical Settings (Punkt 6)
        $output .= '<div class="settings-group">';
        $output .= '<h4>⚙️ Technische Einstellungen</h4>';

        $output .= '<div class="setting-row">';
        $output .= '<span class="setting-label">Viewer Update-Intervall (Sekunden)</span>';
        $output .= '<div class="setting-input">';
        $output .= '<input type="number" id="setting-viewer-interval" class="number-input" min="1" max="60" value="' . $settingsManager->get('technical.viewer_update_interval') . '">';
        $output .= '</div>';
        $output .= '</div>';

        $output .= '<div class="setting-row">';
        $output .= '<span class="setting-label">Session Timeout (Sekunden)</span>';
        $output .= '<div class="setting-input">';
        $output .= '<input type="number" id="setting-session-timeout" class="number-input" min="10" max="300" value="' . $settingsManager->get('technical.session_timeout') . '">';
        $output .= '</div>';
        $output .= '</div>';
        $output .= '</div>'; // settings-group

        // Theme Settings (Punkt 7)
        $output .= '<div class="settings-group">';
        $output .= '<h4>🎨 Theme & Design</h4>';

        $output .= '<div class="setting-row">';
        $output .= '<span class="setting-label">Standard-Theme</span>';
        $output .= '<div class="setting-input">';
        $output .= '<select id="setting-default-theme" class="select-input">';
        $currentTheme = $settingsManager->get('theme.default_theme');
        $output .= '<option value="theme-legacy" ' . ($currentTheme === 'theme-legacy' ? 'selected' : '') . '>Klassisch</option>';
        $output .= '<option value="theme-alpine" ' . ($currentTheme === 'theme-alpine' ? 'selected' : '') . '>Alpin</option>';
        $output .= '<option value="theme-neo" ' . ($currentTheme === 'theme-neo' ? 'selected' : '') . '>Modern</option>';
        $output .= '</select>';
        $output .= '</div>';
        $output .= '</div>';

        $output .= '<div class="setting-row">';
        $output .= '<span class="setting-label">Theme-Switcher anzeigen</span>';
        $output .= '<div class="setting-input">';
        $output .= '<label class="toggle-switch">';
        $output .= '<input type="checkbox" id="setting-show-theme-switcher" ' . ($settingsManager->get('theme.show_theme_switcher') ? 'checked' : '') . '>';
        $output .= '<span class="toggle-slider"></span>';
        $output .= '</label>';
        $output .= '</div>';
        $output .= '</div>';
        $output .= '</div>'; // settings-group

        // SEO Settings (Punkt 8)
        $output .= '<div class="settings-group">';
        $output .= '<h4>🔍 SEO & Meta</h4>';

        $output .= '<div class="setting-row">';
        $output .= '<span class="setting-label">Custom Title (leer = Standard)</span>';
        $output .= '<div class="setting-input">';
        $output .= '<input type="text" id="setting-custom-title" class="text-input" placeholder="' . $siteConfig['siteTitle'] . '" value="' . htmlspecialchars((string) ($settingsManager->get('seo.custom_title') ?? '')) . '">';
        $output .= '</div>';
        $output .= '</div>';

        $output .= '<div class="setting-row">';
        $output .= '<span class="setting-label">Meta Description</span>';
        $output .= '<div class="setting-input">';
        $output .= '<textarea id="setting-meta-description" class="textarea-input" rows="2" placeholder="SEO Beschreibung für Google...">' . htmlspecialchars((string) ($settingsManager->get('seo.meta_description') ?? '')) . '</textarea>';
        $output .= '</div>';
        $output .= '</div>';

        $output .= '<div class="setting-row">';
        $output .= '<span class="setting-label">Meta Keywords</span>';
        $output .= '<div class="setting-input">';
        $output .= '<input type="text" id="setting-meta-keywords" class="text-input" placeholder="webcam, zürich, wetter..." value="' . htmlspecialchars((string) ($settingsManager->get('seo.meta_keywords') ?? '')) . '">';
        $output .= '</div>';
        $output .= '</div>';
        $output .= '</div>'; // settings-group

        // Weather Settings
        $output .= '<div class="settings-group">';
        $output .= '<h4>🌤️ Wetter-Widget <span style="font-size:12px; color:#4CAF50;">(Open-Meteo - kostenlos, kein API-Key nötig)</span></h4>';

        $output .= '<div class="setting-row">';
        $output .= '<span class="setting-label">Wetter-Widget anzeigen</span>';
        $output .= '<div class="setting-input">';
        $output .= '<label class="toggle-switch">';
        $output .= '<input type="checkbox" id="setting-weather-enabled" ' . ($settingsManager->get('weather.enabled') ? 'checked' : '') . '>';
        $output .= '<span class="toggle-slider"></span>';
        $output .= '</label>';
        $output .= '</div>';
        $output .= '</div>';

        // API-KEY FELD KOMPLETT ENTFERNT

        $output .= '<div class="setting-row">';
        $output .= '<span class="setting-label">Standort (Anzeigename)</span>';
        $output .= '<div class="setting-input">';
        $output .= '<input type="text" id="setting-weather-location" class="text-input" placeholder="Oberdürnten,CH" value="' . htmlspecialchars((string) ($settingsManager->get('weather.location') ?? '')) . '">';
        $output .= '</div>';
        $output .= '</div>';

        $output .= '<div class="setting-row">';
        $output .= '<span class="setting-label">Latitude (Breitengrad)</span>';
        $output .= '<div class="setting-input">';
        $output .= '<input type="text" id="setting-weather-lat" class="text-input" placeholder="47.2833" value="' . htmlspecialchars((string) ($settingsManager->get('weather.lat') ?? '')) . '">';
        $output .= '</div>';
        $output .= '</div>';

        $output .= '<div class="setting-row">';
        $output .= '<span class="setting-label">Longitude (Längengrad)</span>';
        $output .= '<div class="setting-input">';
        $output .= '<input type="text" id="setting-weather-lon" class="text-input" placeholder="8.7167" value="' . htmlspecialchars((string) ($settingsManager->get('weather.lon') ?? '')) . '">';
        $output .= '</div>';
        $output .= '</div>';

        $output .= '<div class="setting-row">';
        $output .= '<span class="setting-label">Update-Intervall (Minuten)</span>';
        $output .= '<div class="setting-input">';
        $output .= '<input type="number" id="setting-weather-interval" class="number-input" min="5" max="60" value="' . $settingsManager->get('weather.update_interval') . '">';
        $output .= '</div>';
        $output .= '</div>';

        $output .= '<div class="setting-row">';
        $output .= '<span class="setting-label">Einheit</span>';
        $output .= '<div class="setting-input">';
        $output .= '<select id="setting-weather-units" class="select-input">';
        $currentUnits = $settingsManager->get('weather.units');
        $output .= '<option value="metric" ' . ($currentUnits === 'metric' ? 'selected' : '') . '>Metrisch (°C, km/h)</option>';
        $output .= '<option value="imperial" ' . ($currentUnits === 'imperial' ? 'selected' : '') . '>Imperial (°F, mph)</option>';
        $output .= '</select>';
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
            usort($files, static function ($a, $b) {
                return filemtime($b) <=> filemtime($a);
            });
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
    private $videoDirs;
    private $monthNames;

    public function __construct($videoDir = './videos/') {
        $this->videoDirs = is_array($videoDir) ? $videoDir : [$videoDir];
        $this->videoDir = $this->videoDirs[0];
        $this->monthNames = [
            '01' => 'Januar', '02' => 'Februar', '03' => 'März', '04' => 'April',
            '05' => 'Mai', '06' => 'Juni', '07' => 'Juli', '08' => 'August',
            '09' => 'September', '10' => 'Oktober', '11' => 'November', '12' => 'Dezember'
        ];
    }

    public function getVideosGroupedByDate() {
        global $remoteCache;
        $videos = [];

        // Lokale Videos direkt (schnell)
        foreach (glob($this->videoDirs[0] . 'daily_video_*.mp4') as $video) {
            if (preg_match('/daily_video_(\d{8})_\d{6}\.mp4/', basename($video), $matches)) {
                $dateStr = $matches[1];
                $year = substr($dateStr, 0, 4);
                $month = substr($dateStr, 4, 2);
                $day = substr($dateStr, 6, 2);
                $videos[$year][$month][] = [
                    'path' => $video, 'filename' => basename($video),
                    'day' => $day, 'filesize' => filesize($video), 'modified' => filemtime($video)
                ];
            }
        }

        // Remote-Videos aus Cache (kein glob auf SMB!)
        foreach ($remoteCache->getVideos() as $rv) {
            if (preg_match('/daily_video_(\d{8})_\d{6}\.mp4/', $rv['filename'], $matches)) {
                $dateStr = $matches[1];
                $year = substr($dateStr, 0, 4);
                $month = substr($dateStr, 4, 2);
                $day = substr($dateStr, 6, 2);
                $videos[$year][$month][] = [
                    'path' => $rv['path'], 'filename' => $rv['filename'],
                    'day' => $day, 'filesize' => $rv['filesize'], 'modified' => $rv['mtime']
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

        $validPath = false;
        foreach ($this->videoDirs as $vDir) {
            $resolvedDir = realpath($vDir);
            $requestedPath = realpath($videoPath);
            if ($requestedPath && $resolvedDir && strpos($requestedPath, $resolvedDir) === 0) {
                $validPath = true;
                break;
            }
        }
        if ($validPath && file_exists($videoPath)) {
            $extension = pathinfo($videoPath, PATHINFO_EXTENSION);  // ← DAS FEHLT
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

    private function deleteDirectoryRecursive($dir) {
        if (!is_dir($dir)) {
            return true;
        }

        $items = scandir($dir);
        if ($items === false) {
            return false;
        }

        foreach ($items as $item) {
            if ($item === '.' || $item === '..') {
                continue;
            }

            $path = $dir . DIRECTORY_SEPARATOR . $item;
            if (is_dir($path)) {
                if (!$this->deleteDirectoryRecursive($path)) {
                    return false;
                }
            } elseif (!unlink($path)) {
                return false;
            }
        }

        return rmdir($dir);
    }

    private function deleteHlsForDailyVideo($videoPath) {
        $basename = basename($videoPath, '.mp4');
        if (!preg_match('/^daily_video_(\d{8}_\d{6})$/', $basename, $matches)) {
            return true;
        }

        $videoId = $matches[1];
        $videoDir = dirname($videoPath);
        $parentDir = dirname($videoDir);
        $hlsDir = $parentDir . '/hls/' . $videoId;

        if (!is_dir($hlsDir)) {
            return true;
        }

        $resolvedHlsRoot = realpath($parentDir . '/hls');
        $resolvedHlsDir = realpath($hlsDir);
        if (!$resolvedHlsRoot || !$resolvedHlsDir || strpos($resolvedHlsDir, rtrim($resolvedHlsRoot, '/') . '/') !== 0) {
            return false;
        }

        return $this->deleteDirectoryRecursive($resolvedHlsDir);
    }

    public function deleteSpecificVideo($videoPath, $token) {
        $expectedToken = hash_hmac('sha256', 'delete_daily_video|' . $videoPath, session_id());
        if (!hash_equals($expectedToken, (string) $token)) {
            return false;
        }

        $requestedPath = realpath($videoPath);
        if (!$requestedPath || !is_file($requestedPath)) {
            return false;
        }

        if (strtolower(pathinfo($requestedPath, PATHINFO_EXTENSION)) !== 'mp4') {
            return false;
        }

        if (!preg_match('/^daily_video_\d{8}_\d{6}\.mp4$/', basename($requestedPath))) {
            return false;
        }

        foreach ($this->videoDirs as $vDir) {
            $resolvedDir = realpath($vDir);
            if ($resolvedDir && strpos($requestedPath, rtrim($resolvedDir, '/') . '/') === 0) {
                if (!unlink($requestedPath)) {
                    return false;
                }
                return $this->deleteHlsForDailyVideo($requestedPath);
            }
        }

        return false;
    }

}


$webcamManager = new WebcamManager();
$imageFilesJson = $webcamManager->getImageFiles();
$guestbookManager = new GuestbookManager();
$contactManager = new ContactManager();
$adminManager = new AdminManager();

$videoArchiveManager = new VideoArchiveManager(['./videos/', '/mnt/aurora-remote/videos/']);
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
        $adminManager->handleLogin($_POST['username'] ?? '', $_POST['password'] ?? '');
        header('Location: ' . $_SERVER['PHP_SELF'] . '#admin');
        exit;
    }

    elseif (isset($_POST['admin-reset-request'])) {
        $adminManager->requestPasswordReset();
        header('Location: ' . $_SERVER['PHP_SELF'] . '#admin');
        exit;
    }

    elseif (isset($_POST['admin-reset-submit'])) {
        $adminManager->handlePasswordReset($_POST['reset_token'] ?? '', $_POST['new_password'] ?? '', $_POST['new_password_confirm'] ?? '');
        header('Location: ' . $_SERVER['PHP_SELF'] . '#admin');
        exit;
    }

    // ADMIN AKTIONEN
    elseif ($adminManager->isAdmin()) {
        if (isset($_POST['action']) && $_POST['action'] === 'approve_guestbook' && isset($_POST['approve_entry'])) {
            $guestbookManager->approveEntry(intval($_POST['approve_entry']));
            header("Location: " . $_SERVER['PHP_SELF'] . "#guestbook");
            exit;
        }

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

        if (isset($_POST['action']) && $_POST['action'] === 'delete_daily_video' && isset($_POST['video_path'], $_POST['delete_token'])) {
            $videoArchiveManager->deleteSpecificVideo($_POST['video_path'], $_POST['delete_token']);
            $year = isset($_POST['cal_year']) ? intval($_POST['cal_year']) : intval(date('Y'));
            $month = isset($_POST['cal_month']) ? intval($_POST['cal_month']) : intval(date('n'));
            $day = isset($_POST['cal_day']) ? intval($_POST['cal_day']) : 1;
            header("Location: " . $_SERVER['PHP_SELF'] . "?cal_year={$year}&cal_month={$month}&cal_day={$day}#archive");
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

    <!-- SEO-optimierter Title -->
    <title><?php echo $settingsManager->getCustomTitle() ?: $siteConfig['siteTitle']; ?></title>

    <!-- SEO Meta-Tags -->
    <meta name="description" content="<?php echo !empty($settingsManager->getMetaDescription()) ? htmlspecialchars($settingsManager->getMetaDescription()) : 'Live Webcam Zürich Oberland mit Blick auf Zürichsee. 24/7 Livestream, Tagesvideos, AI-Wettererkennung. Patrouille Suisse Trainingsflüge jeden Montag live verfolgen. Webcam Dürnten auf 616m.'; ?>">
    <meta name="keywords" content="<?php echo !empty($settingsManager->getMetaKeywords()) ? htmlspecialchars($settingsManager->getMetaKeywords()) : 'Webcam Zürich, Zürichsee Webcam, Zürich Oberland Webcam, Live Webcam Schweiz, Patrouille Suisse Livestream, Wetter Zürich live, Webcam Dürnten, Rapperswil Webcam, Schweizer Alpen Webcam, Wetter Zürich Oberland, ' . $siteConfig['siteName'] . ' Webcam, Timelapse Zürich'; ?>">
    <meta name="author" content="<?php echo $siteConfig['author']; ?>">
    <meta name="robots" content="index, follow, max-image-preview:large">
    <link rel="canonical" href="<?php echo $siteConfig['domainUrl']; ?>/">

    <!-- Lokales SEO -->
    <meta name="geo.region" content="CH-ZH">
    <meta name="geo.placename" content="Dürnten, Zürich Oberland">
    <meta name="geo.position" content="47.278;8.870">
    <meta name="ICBM" content="47.278, 8.870">

    <!-- Open Graph für Social Media -->
    <meta property="og:type" content="website">
    <meta property="og:title" content="Zürich Oberland Webcam Live - Zürichsee & Patrouille Suisse">
    <meta property="og:description" content="24/7 Live-Webcam aus dem Zürcher Oberland auf 616m Höhe. Patrouille Suisse Trainings jeden Montag. AI-Wettererkennung für Sonnenaufgänge, Regenbögen und mehr.">
    <meta property="og:image" content="<?php echo $siteConfig['domainUrl']; ?>/og-image.jpg">
    <meta property="og:image:width" content="1200">
    <meta property="og:image:height" content="630">
    <meta property="og:url" content="<?php echo $siteConfig['domainUrl']; ?>/">
    <meta property="og:site_name" content="<?php echo $siteConfig['siteNameFull']; ?> Zürich">
    <meta property="og:locale" content="de_CH">

    <!-- Twitter Card -->
    <meta name="twitter:card" content="summary_large_image">
    <meta name="twitter:title" content="Zürich Oberland Webcam Live | Patrouille Suisse & Zürichsee">
    <meta name="twitter:description" content="24/7 Live-Webcam mit AI-Wettererkennung. Jeden Montag Patrouille Suisse Trainingsflüge live!">
    <meta name="twitter:image" content="<?php echo $siteConfig['domainUrl']; ?>/og-image.jpg">

    <meta name="google-site-verification" content="gzs2HE9hbMKbHYKSf2hZjXvDd7iDUeA4Jb2zngzNIZM" />

    <!-- Schema.org JSON-LD für Webcam -->
    <script type="application/ld+json">
    {
        "@context": "https://schema.org",
        "@type": "WebSite",
        "name": "<?php echo $siteConfig['siteNameFull']; ?> Zürich Oberland",
        "alternateName": "<?php echo $siteConfig['alternateName']; ?>",
        "url": "<?php echo $siteConfig['domainUrl']; ?>",
        "description": "24/7 Live Webcam aus dem Zürcher Oberland mit Blick auf den Zürichsee. AI-gestützte Wettererkennung und Patrouille Suisse Trainingsflüge.",
        "inLanguage": "de-CH"
    }
    </script>

    <!-- Schema.org für Lokales Business -->
    <script type="application/ld+json">
    {
        "@context": "https://schema.org",
        "@type": "LocalBusiness",
        "name": "<?php echo $siteConfig['siteNameFull']; ?>",
        "description": "Live Webcam Service aus dem Zürcher Oberland mit 24/7 Livestream, Tagesvideos und AI-Wettererkennung",
        "url": "<?php echo $siteConfig['domainUrl']; ?>",
        "address": {
            "@type": "PostalAddress",
            "addressLocality": "Dürnten",
            "addressRegion": "Zürich",
            "addressCountry": "CH"
        },
        "geo": {
            "@type": "GeoCoordinates",
            "latitude": "47.278",
            "longitude": "8.870",
            "elevation": "616"
        },
        "areaServed": {
            "@type": "GeoCircle",
            "geoMidpoint": {
                "@type": "GeoCoordinates",
                "latitude": "47.278",
                "longitude": "8.870"
            },
            "geoRadius": "50000"
        }
    }
    </script>

    <!-- Schema.org für Video/Livestream -->
    <script type="application/ld+json">
    {
        "@context": "https://schema.org",
        "@type": "VideoObject",
        "name": "Zürich Oberland Live Webcam Stream",
        "description": "24/7 Live-Webcam aus dem Zürcher Oberland mit Blick auf Zürichsee und Schweizer Alpen",
        "thumbnailUrl": "<?php echo $siteConfig['domainUrl']; ?>/og-image.jpg",
        "uploadDate": "2024-01-01",
        "contentUrl": "<?php echo $siteConfig['domainUrl']; ?>/test_video.m3u8",
        "embedUrl": "<?php echo $siteConfig['domainUrl']; ?>/",
        "publication": {
            "@type": "BroadcastEvent",
            "isLiveBroadcast": true,
            "startDate": "2024-01-01"
        }
    }
    </script>

    <link rel="icon" type="image/png" sizes="32x32" href="/favicon-32x32.png">
    <link rel="apple-touch-icon" sizes="180x180" href="/apple-touch-icon.png">
    <meta name="theme-color" content="#667eea">

    <link rel="stylesheet" href="https://cdnjs.cloudflare.com/ajax/libs/font-awesome/5.15.3/css/all.min.css">
    <link rel="preconnect" href="https://cdn.jsdelivr.net">
    <script src="https://cdn.jsdelivr.net/npm/hls.js@1.7.3"></script>
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

/* Screen-reader only (Accessibility) */
.sr-only {
    position: absolute;
    width: 1px;
    height: 1px;
    padding: 0;
    margin: -1px;
    overflow: hidden;
    clip: rect(0, 0, 0, 0);
    white-space: nowrap;
    border: 0;
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
    justify-content: center;
    gap: 20px;
    flex-wrap: wrap;
    padding-right: 120px; /* Platz für Sprachauswahl */
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

nav ul { list-style: none; padding: 0; display: flex; justify-content: center; flex-wrap: wrap; margin: 0; }
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

#live-video-wrapper, #timelapse-viewer, #daily-video-player {
    position: absolute;
    top: 0;
    left: 0;
    width: 100%;
    height: 100%;
    border-radius: 8px;
    overflow: hidden;
}

.video-zoom-wrapper {
    width: 100%;
    height: 100%;
    display: flex;
    align-items: center;
    justify-content: center;
    transition: transform 0.2s ease;
    transform-origin: center center;
}

#webcam-player, #timelapse-image, #daily-video {
    width: 100%;
    height: 100%;
    object-fit: contain;
}

/* Ton-Knopf im Live-Player (2026-10-11) */
#aurora-ton-knopf {
    position: absolute; left: 12px; bottom: 12px; z-index: 35;
    border: 0; border-radius: 999px; padding: 8px 14px; cursor: pointer;
    font: 600 14px/1 system-ui, -apple-system, sans-serif; color: #fff;
    background: rgba(0, 0, 0, .55); -webkit-backdrop-filter: blur(6px); backdrop-filter: blur(6px);
    transition: background .2s, transform .2s;
}
#aurora-ton-knopf:hover { background: rgba(0, 0, 0, .75); transform: scale(1.05); }
#aurora-ton-knopf.stumm { animation: auroraTonPuls 2s ease-in-out 3; }
@keyframes auroraTonPuls { 50% { box-shadow: 0 0 0 6px rgba(255, 255, 255, .25); } }

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
.video-actions { display: flex; gap: 10px; flex-wrap: wrap; align-items: center; }

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

.video-delete-form {
    display: inline-block;
    margin: 0;
    padding: 0;
    background: transparent;
    max-width: none;
}
.video-delete-link {
    background: linear-gradient(135deg, #e53935 0%, #b71c1c 100%);
    color: white;
    padding: 8px 16px;
    border: 0;
    border-radius: 20px;
    text-decoration: none;
    font-weight: bold;
    cursor: pointer;
    transition: all 0.3s;
}
.video-delete-link:hover { transform: scale(1.05); }

/* Forms */
form { display: grid; gap: 20px; background-color: rgba(255, 255, 255, 0.7); padding: 25px; border-radius: 8px; max-width: 600px; margin: 0 auto; }
input, textarea, select { width: 100%; padding: 12px; border: 1px solid #ddd; border-radius: 5px; font-size: 16px; background-color: white; }
textarea { min-height: 120px; resize: vertical; }
label { font-weight: bold; margin-bottom: 5px; display: block; }

button[type="submit"] { background-color: #4CAF50; color: #fff; border: none; padding: 10px 20px; border-radius: 5px; cursor: pointer; font-size: 16px; transition: background-color 0.3s; }
button[type="submit"]:hover { background-color: #45a049; }

.delete-btn { background-color: #ff4136; color: white; border: none; padding: 5px 10px; cursor: pointer; font-size: 0.8em; margin-left: 10px; border-radius: 3px; }

/* Weather Widget */
.weather-widget {
    display: flex;
    flex-wrap: wrap;
    justify-content: center;
    align-items: center;
    gap: 20px;
    background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
    padding: 20px;
    border-radius: 12px;
    margin-bottom: 20px;
    box-shadow: 0 10px 30px rgba(102, 126, 234, 0.3);
    animation: weatherFadeIn 0.5s ease;
}
.weather-widget.weather-error {
    background: linear-gradient(135deg, #f44336 0%, #e91e63 100%);
    color: white;
    font-weight: bold;
    justify-content: center;
}
.weather-item {
    display: flex;
    flex-direction: column;
    align-items: center;
    gap: 5px;
    padding: 10px 15px;
    background: rgba(255, 255, 255, 0.15);
    border-radius: 8px;
    backdrop-filter: blur(10px);
    min-width: 120px;
    transition: transform 0.3s ease, background 0.3s ease;
}
.weather-item:hover {
    transform: translateY(-3px);
    background: rgba(255, 255, 255, 0.25);
}
.weather-icon {
    font-size: 32px;
    line-height: 1;
}
.weather-value {
    font-size: 18px;
    font-weight: bold;
    color: white;
    white-space: nowrap;
}
.weather-label {
    font-size: 12px;
    color: rgba(255, 255, 255, 0.9);
    text-transform: uppercase;
    letter-spacing: 0.5px;
}
.weather-description {
    flex: 1 1 auto;
    min-width: 180px;
}
.weather-description .weather-value {
    font-size: 16px;
    text-align: center;
}
@keyframes weatherFadeIn {
    from { opacity: 0; transform: translateY(-10px); }
    to { opacity: 1; transform: translateY(0); }
}
@media (max-width: 768px) {
    .weather-widget {
        gap: 10px;
        padding: 15px 10px;
    }
    .weather-item {
        min-width: 90px;
        padding: 8px 10px;
    }
    .weather-icon {
        font-size: 24px;
    }
    .weather-value {
        font-size: 14px;
    }
    .weather-label {
        font-size: 10px;
    }
}

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

/* Admin Settings Panel */
#admin-settings-panel {
    background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
    padding: 30px;
    border-radius: 12px;
    margin-bottom: 30px;
    box-shadow: 0 10px 30px rgba(102, 126, 234, 0.3);
}
#admin-settings-panel h3 {
    color: white;
    margin: 0 0 25px 0;
    font-size: 24px;
    text-align: center;
}
.settings-group {
    background: rgba(255, 255, 255, 0.95);
    padding: 20px;
    border-radius: 8px;
    margin-bottom: 15px;
}
.settings-group h4 {
    margin: 0 0 15px 0;
    color: #667eea;
    font-size: 18px;
    border-bottom: 2px solid #667eea;
    padding-bottom: 8px;
}
.setting-row {
    display: flex;
    justify-content: space-between;
    align-items: center;
    padding: 12px 0;
    border-bottom: 1px solid #f0f0f0;
}
.setting-row:last-child {
    border-bottom: none;
}
.setting-label {
    font-weight: 500;
    color: #333;
    flex: 1;
}
.setting-input {
    display: flex;
    align-items: center;
    min-width: 200px;
}
.number-input, .text-input, .select-input {
    width: 100%;
    padding: 8px 12px;
    border: 2px solid #ddd;
    border-radius: 5px;
    font-size: 14px;
    transition: border-color 0.3s;
}
.number-input:focus, .text-input:focus, .select-input:focus {
    outline: none;
    border-color: #667eea;
}
.textarea-input {
    width: 100%;
    padding: 8px 12px;
    border: 2px solid #ddd;
    border-radius: 5px;
    font-size: 14px;
    font-family: Arial, sans-serif;
    resize: vertical;
    transition: border-color 0.3s;
}
.textarea-input:focus {
    outline: none;
    border-color: #667eea;
}
.toggle-switch {
    position: relative;
    display: inline-block;
    width: 50px;
    height: 24px;
}
.toggle-switch input {
    opacity: 0;
    width: 0;
    height: 0;
}
.toggle-slider {
    position: absolute;
    cursor: pointer;
    top: 0;
    left: 0;
    right: 0;
    bottom: 0;
    background-color: #ccc;
    transition: 0.3s;
    border-radius: 24px;
}
.toggle-slider:before {
    position: absolute;
    content: "";
    height: 18px;
    width: 18px;
    left: 3px;
    bottom: 3px;
    background-color: white;
    transition: 0.3s;
    border-radius: 50%;
}
.toggle-switch input:checked + .toggle-slider {
    background: linear-gradient(135deg, #667eea, #764ba2);
}
.toggle-switch input:checked + .toggle-slider:before {
    transform: translateX(26px);
}

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





.starlink-banner-wrapper {
    text-align: center;
    margin: 30px 0 20px 0;
}

.starlink-banner {
    max-width: 700px;
    margin: 0 auto;
    padding: 20px;
    background: linear-gradient(135deg, #1e3c72 0%, #2a5298 100%);
    border-radius: 12px;
    box-shadow: 0 10px 25px rgba(0,0,0,0.2);
}

.starlink-banner h3 {
    color: white;
    margin: 0 0 15px 0;
}

.starlink-banner p {
    color: white;
    margin: 0 0 15px 0;
    line-height: 1.6;
}

.starlink-banner a {
    display: inline-block;
}

.starlink-banner img {
    max-width: 220px;
    width: 100%;
    height: auto;
    border-radius: 8px;
    box-shadow: 0 3px 10px rgba(0,0,0,0.3);
    background: white;
    padding: 6px;
    transition: transform 0.3s;
}

.starlink-banner img:hover {
    transform: scale(1.05);
}

.donation-buttons {
    display: flex;
    justify-content: center;
    align-items: center;
    gap: 16px;
    flex-wrap: wrap;
}

.paypal-donation-link {
    display: inline-flex;
    flex-direction: column;
    align-items: center;
    gap: 6px;
    min-width: 220px;
    padding: 18px 32px;
    border-radius: 999px;
    background: #ffc439;
    color: #111;
    font: 600 18px/1.2 Arial, Helvetica, sans-serif;
    text-decoration: none;
    box-shadow: 0 6px 18px rgba(0, 0, 0, 0.25);
    transition: transform 0.2s, background 0.2s;
}

.paypal-donation-link:hover,
.paypal-donation-link:focus-visible {
    transform: scale(1.05);
    background: #f2ba36;
}

.paypal-logo {
    font: italic 800 30px/1 Verdana, Arial, sans-serif;
    letter-spacing: -0.5px;
}

.paypal-logo b { color: #003087; }
.paypal-logo i { color: #009cde; }

.twint-box {
    display: flex;
    flex-direction: column;
    align-items: center;
    text-align: center;
    gap: 14px;
    margin: 30px auto 20px auto;
    max-width: 760px;
}

.twint-image {
    max-width: 320px;
    width: 100%;
    height: auto;
    display: block;
    border-radius: 10px;
    box-shadow: 0 8px 20px rgba(0,0,0,0.12);
}

.green-label-badge {
    display: inline-block;
    background: linear-gradient(135deg, #2e7d32, #43a047);
    color: white;
    font-size: 12px;
    font-weight: bold;
    padding: 7px 14px;
    border-radius: 20px;
    letter-spacing: 0.3px;
    box-shadow: 0 4px 10px rgba(46,125,50,0.25);
}

.eco-business-box {
    background: rgba(255,255,255,0.92);
    border-radius: 14px;
    padding: 22px 24px;
    max-width: 700px;
    width: 100%;
    box-shadow: 0 10px 24px rgba(0,0,0,0.10);
    border: 1px solid rgba(46,125,50,0.15);
}

.eco-business-box h3 {
    margin: 0 0 8px 0;
    font-size: 24px;
    color: #1b5e20;
    line-height: 1.3;
}

.eco-subtitle {
    margin: 0 0 16px 0;
    font-size: 16px;
    font-weight: bold;
    color: #2e7d32;
}

.eco-business-box p {
    font-size: 15px;
    color: #333;
    line-height: 1.7;
    margin-bottom: 18px;
}

.eco-feature-list {
    list-style: none;
    padding: 0;
    margin: 0;
    display: grid;
    gap: 10px;
}

.eco-feature-list li {
    background: #f4faf4;
    border-left: 4px solid #43a047;
    padding: 12px 14px;
    border-radius: 8px;
    font-size: 14px;
    color: #2d2d2d;
    text-align: left;
    line-height: 1.5;
}

@media (max-width: 768px) {
    .eco-business-box {
        padding: 18px 16px;
    }

    .eco-business-box h3 {
        font-size: 20px;
    }

    .eco-subtitle {
        font-size: 15px;
    }

    .eco-feature-list li {
        font-size: 13px;
    }
}

</style>

    <script src="https://cdn.jsdelivr.net/npm/qrcode-generator@1.4.4/qrcode.min.js"></script>
<link rel="stylesheet" href="css/aurora-refresh.css?v=20261007">
</head>
<body class="<?php echo $settingsManager->getDefaultTheme(); ?>">

<div class="sun-overlay" aria-hidden="true"></div>

<div id="language-switch">
    <button id="lang-de" class="lang-button" data-code="DE" aria-label="Deutsch">
        <img src="images/swiss-flag-new.ico" alt="Schweizer Flagge" class="flag-icon">
    </button>
    <button id="lang-en" class="lang-button" data-code="EN" aria-label="English">
        <img src="images/uk-flag.ico" alt="UK Flagge" class="flag-icon">
    </button>
    <button id="lang-it" class="lang-button" data-code="IT" aria-label="Italiano">
        <img src="images/italian-flag.ico" alt="Flagge Italien" class="flag-icon">
    </button>
    <button id="lang-fr" class="lang-button" data-code="FR" aria-label="Français">
        <img src="images/french-flag.ico" alt="Flagge Frankreich" class="flag-icon">
    </button>
    <button id="lang-es" class="lang-button" aria-label="Español" data-code="ES">
        <img src="images/spanish-flag.svg" alt="Bandera de España" class="flag-icon">
    </button>
    <button id="lang-zh" class="lang-button" data-code="CN" aria-label="中文">
        <img src="images/chinese-flag.ico" alt="中国国旗" class="flag-icon">
    </button>
</div>

<header>
    <div class="container">
        <div class="logo-wrapper">
            <div class="logo">
                <img src="<?php echo $siteConfig['logo']; ?>" alt="<?php echo $siteConfig['siteNameFull']; ?> - 24/7 Zürich Oberland Webcam Logo">
            </div>
            <div class="swiss-cross" aria-hidden="true"></div>
        </div>
        <nav>
            <ul>
                <li><a href="#webcams" data-en="Webcam" data-de="Webcam" data-it="Webcam" data-fr="Webcam" data-es="Webcam" data-zh="摄像头">Webcam</a></li>
                <li><a href="#guestbook" data-en="Guestbook" data-de="Gästebuch" data-it="Libro degli ospiti" data-fr="Livre d'or" data-es="Libro de visitas" data-zh="留言簿">Gästebuch</a></li>
                <li><a href="#kontakt" data-en="Contact" data-de="Kontakt" data-it="Contatto" data-fr="Contact" data-es="Contacto" data-zh="联系">Kontakt</a></li>
                <li><a href="#gallery" data-en="Gallery" data-de="Galerie" data-it="Galleria" data-fr="Galerie" data-es="Galería" data-zh="图库">Galerie</a></li>
                <li><a href="#archive" data-en="Video Archive" data-de="Videoarchiv" data-it="Archivio video" data-fr="Archive vidéo" data-es="Archivo de vídeos" data-zh="视频档案">Videoarchiv</a></li>
                <li><a href="/app/" style="display:inline-flex;align-items:center;gap:6px;"><img src="/app/icon.svg" alt="" width="20" height="20" style="border-radius:5px;"><span data-en="App (Beta)" data-de="App (Beta)" data-it="App (Beta)" data-fr="Appli (bêta)" data-es="App (beta)" data-zh="应用（测试版）">App (Beta)</span></a></li>
                <?php if ($adminManager->isAdmin()): ?>
                <li><a href="#admin" data-en="Admin" data-de="Admin" data-it="Admin" data-fr="Admin" data-es="Admin" data-zh="管理员">Admin</a></li>
                <?php endif; ?>
            </ul>
        </nav>
        <div class="theme-switcher" aria-label="Design wechseln" style="display: <?php echo $settingsManager->shouldShowThemeSwitcher() ? 'flex' : 'none'; ?>;">
            <span data-en="Design" data-de="Design" data-it="Design" data-fr="Design" data-es="Diseño" data-zh="设计">Design</span>
            <button class="theme-button active" data-theme="theme-legacy" type="button" data-en="Classic" data-de="Klassisch" data-it="Classico" data-fr="Classique" data-es="Clásico" data-zh="经典">Klassisch</button>
            <button class="theme-button" data-theme="theme-alpine" type="button" data-en="Alpine" data-de="Alpin" data-it="Alpino" data-fr="Alpin" data-es="Alpino" data-zh="高山">Alpin</button>
            <button class="theme-button" data-theme="theme-neo" type="button" data-en="Modern" data-de="Modern" data-it="Moderno" data-fr="Moderne" data-es="Moderno" data-zh="现代">Modern</button>
        </div>
    </div>
</header>

<div class="main-content">
    <section class="title-section">
        <div class="container">
            <div class="flag-title-container">
                <img src="images/swiss.jpg" alt="Schweizer Flagge" class="flag-image">
                <h1 id="hero-welcome-text" data-en="<?php echo $siteConfig['welcomeEn']; ?>" data-de="<?php echo htmlspecialchars($heroWelcomeText); ?>" data-it="Benvenuti su <?php echo $siteConfig['siteNameFullEn']; ?>" data-fr="Bienvenue sur <?php echo $siteConfig['siteNameFullEn']; ?>" data-es="Bienvenido a <?php echo $siteConfig['siteNameFullEn']; ?>" data-zh="欢迎来到<?php echo $siteConfig['siteNameFullEn']; ?>">
                    <?php echo htmlspecialchars($heroWelcomeText); ?>
                </h1>
                <img src="local-flag.jpg" alt="<?php echo htmlspecialchars($localFlagAltText); ?>" class="flag-image" id="local-flag-image">
            </div>
            <p id="hero-region-text"
               data-en="Experience fascinating views of the Zurich region - in real time!"
               data-de="<?php echo htmlspecialchars($heroRegionText); ?>"
               data-it="Vivi affascinanti panorami della regione di Zurigo in tempo reale!"
               data-fr="Découvrez des panoramas fascinants de la région de Zurich en temps réel !" data-es="¡Descubra fascinantes vistas de la región de Zúrich en tiempo real!"
               data-zh="实时欣赏苏黎世地区的迷人景色！">
                <?php echo htmlspecialchars($heroRegionText); ?>
            </p>
            <!-- Android-App Beta (2026-09-30) -->
            <a href="/app/" style="text-shadow:none;display:flex;align-items:center;gap:14px;max-width:560px;margin:18px auto 0;padding:12px 16px;border-radius:18px;text-decoration:none;color:#eef4ff;background:linear-gradient(135deg,#050b24,#16325e);box-shadow:0 8px 24px rgba(5,11,36,.35);">
                <img src="/app/icon.svg" alt="cAurora App" width="52" height="52" style="border-radius:13px;flex:none;">
                <span style="flex:1;text-align:left;line-height:1.35;">
                    <strong data-en="cAurora for Android" data-de="cAurora für Android" data-it="cAurora per Android" data-fr="cAurora pour Android" data-es="cAurora para Android" data-zh="cAurora 安卓版">cAurora für Android</strong><br>
                    <span style="font-size:.9em;opacity:.8;" data-en="Live view, time-lapse and archive in your pocket – test the beta now" data-de="Livebild, Zeitraffer und Archiv in der Hosentasche – jetzt als Beta testen" data-it="Diretta, timelapse e archivio in tasca – prova ora la beta" data-fr="Direct, timelapse et archives dans la poche – testez la bêta" data-es="Imagen en directo, timelapse y archivo en el bolsillo: pruebe ya la beta" data-zh="随身查看直播、延时和档案——立即试用测试版">Livebild, Zeitraffer und Archiv in der Hosentasche – jetzt als Beta testen</span>
                </span>
                <span style="flex:none;padding:8px 14px;border-radius:12px;font-weight:700;color:#04130e;background:linear-gradient(90deg,#23f5b1,#1fd3e6);" data-en="Get" data-de="Laden" data-it="Scarica" data-fr="Obtenir" data-es="Descargar" data-zh="下载">Laden</span>
            </a>
        </div>
    </section>

    <div class="banner-container" style="display: <?php echo $settingsManager->shouldShowRecommendationBanner() ? 'block' : 'none'; ?>;">
        <div class="recommendation-banner">
            <h2>Unsere Empfehlungen</h2>
            <div class="sponsor-logos">
                <div class="ad-row">
                    <div class="ad-item"><a href="https://www.radiozuerisee.ch/" target="_blank"><img src="zsee.jpg" alt="Radio Zürichsee"></a></div>
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
        <!-- WEATHER WIDGET -->
        <?php if ($settingsManager->isWeatherEnabled()): ?>
        <?php
            try {
                $weather = $weatherManager->getCurrentWeather();
            } catch (Exception $e) {
                $weather = ['error' => 'Fehler: ' . $e->getMessage()];
            }
            if ($weather && !isset($weather['error'])):
        ?>
        <div id="weather-widget" class="weather-widget">
            <div class="weather-item weather-temp">
                <span class="weather-icon">🌡️</span>
                <span class="weather-value"><?php echo $weather['temp']; ?>°C</span>
                <span class="weather-label" data-de="Temperatur" data-en="Temperature" data-it="Temperatura" data-fr="Température" data-es="Temperatura" data-zh="温度">Temperatur</span>
            </div>
            <div class="weather-item weather-wind">
                <span class="weather-icon">💨</span>
                <span class="weather-value"><?php echo $weather['wind_speed']; ?> km/h <?php echo $weather['wind_direction']; ?></span>
                <span class="weather-label" data-de="Wind" data-en="Wind" data-it="Vento" data-fr="Vent" data-es="Viento" data-zh="风">Wind</span>
            </div>
            <div class="weather-item weather-pressure">
                <span class="weather-icon">🔽</span>
                <span class="weather-value"><?php echo $weather['pressure']; ?> hPa</span>
                <span class="weather-label" data-de="Luftdruck" data-en="Pressure" data-it="Pressione" data-fr="Pression" data-es="Presión" data-zh="气压">Luftdruck</span>
            </div>
            <div class="weather-item weather-humidity">
                <span class="weather-icon">💧</span>
                <span class="weather-value"><?php echo $weather['humidity']; ?>%</span>
                <span class="weather-label" data-de="Luftfeuchtigkeit" data-en="Humidity" data-it="Umidità" data-fr="Humidité" data-es="Humedad" data-zh="湿度">Luftfeuchtigkeit</span>
            </div>
            <div class="weather-item weather-description">
                <span class="weather-icon"><?php echo $weatherManager->getWeatherEmoji($weather['icon']); ?></span>
                <span class="weather-value"><?php echo $weather['description']; ?></span>
                <span class="weather-label"><?php echo $weather['location']; ?></span>
            </div>
            <?php if ($weather['rain_1h'] > 0 || $weather['snow_1h'] > 0): ?>
            <div class="weather-item weather-precipitation">
                <span class="weather-icon"><?php echo $weather['rain_1h'] > 0 ? '🌧️' : '❄️'; ?></span>
                <span class="weather-value"><?php echo $weather['rain_1h'] > 0 ? $weather['rain_1h'] : $weather['snow_1h']; ?> mm</span>
                <span class="weather-label" data-de="Niederschlag" data-en="Precipitation" data-it="Precipitazioni" data-fr="Précipitations" data-es="Precipitación" data-zh="降水">Niederschlag</span>
            </div>
            <?php endif; ?>
        </div>
        <?php elseif ($weather && isset($weather['error'])): ?>
        <div class="weather-widget weather-error">
            <span>⚠️ Wetterdaten nicht verfügbar: <?php echo $weather['error']; ?></span>
        </div>
        <?php endif; ?>
        <?php endif; ?>

        <!-- VIDEO PLAYER -->
        <div class="video-container" style="border-radius: 12px; overflow: hidden; box-shadow: 0 10px 30px rgba(0,0,0,0.15);">
            <?php echo $webcamManager->displayWebcam(); ?>

            <!-- Timelapse Overlay -->
            <div id="timelapse-viewer" style="display: none;">
                <div id="timelapse-wrapper" class="video-zoom-wrapper">
                    <img id="timelapse-image" src="" alt="Zeitraffer Wetter Zürich Oberland - Aktuelle Aufnahme" style="width: 100%; height: 100%; object-fit: cover;">
                </div>
                <div id="timelapse-time-overlay"></div>
            </div>

            <!-- Daily Video Player (für Tagesvideos) -->
            <div id="daily-video-player" style="display: none;">
                <div id="daily-video-wrapper" class="video-zoom-wrapper">
                    <video id="daily-video" controls playsinline style="width: 100%; height: 100%; object-fit: contain;">
                        <source src="" type="video/mp4">
                    </video>
                </div>
            </div>
               </div>

        <!-- EMBED-LINK FÜR EXTERNE WETTER-APPS -->
        <!-- <div class="embed-link-box" style="text-align: center; margin: 20px 0; padding: 15px; background: rgba(255,255,255,0.95); border-radius: 10px; box-shadow: 0 2px 10px rgba(0,0,0,0.1);">
            <p style="margin-bottom: 10px; font-weight: bold; color: #667eea;">
                📷 Webcam-Bild einbetten:
            </p>
            <div style="display: flex; justify-content: center; align-items: center; gap: 10px; flex-wrap: wrap;">
                <input type="text" 
                       id="embed-url" 
                       value="https://www.aurora-weather-livecam.com/image/current.jpg" 
                       readonly 
                       style="padding: 10px 15px; border: 2px solid #667eea; border-radius: 8px; width: 400px; max-width: 100%; font-size: 14px; background: #f9f9f9;">
                <button onclick="copyEmbedUrl()" 
                        style="padding: 10px 20px; background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; border: none; border-radius: 8px; cursor: pointer; font-weight: bold; transition: transform 0.2s;">
                    📋 Kopieren
                </button>
            </div>
            <p id="copy-feedback" style="margin-top: 10px; color: #4CAF50; font-size: 14px; display: none;">
                ✅ Link kopiert!
            </p>
        </div> -->

        <!-- TIMELAPSE CONTROLS (NEU!) -->
        <div id="timelapse-controls"></div>


        <!-- 
 CONTROLS -->
        <div id="zoom-controls" class="zoom-controls" aria-label="Zoom Steuerung" style="display: <?php echo $settingsManager->shouldShowZoomControls() ? 'flex' : 'none'; ?>;">
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
                <span data-en="Watching" data-de="Zuschauer" data-it="Spettatori" data-fr="Spectateurs" data-es="Viendo" data-zh="观看人数">Zuschauer</span>
            </div>
            <?php endif; ?>

            <div style="justify-self: end;"></div>
        </div>

        <!-- STEUERUNG BUTTONS -->
        <div class="webcam-controls" style="text-align: center;">
            <a href="?action=snapshot" class="button" data-en="Save Snapshot" data-de="Snapshot speichern" data-it="Salva istantanea" data-fr="Enregistrer l'instantané" data-es="Guardar captura" data-zh="保存截图">
                Snapshot speichern
            </a>
<?php if ($settingsManager->isWeeklyTimelapseEnabled()): ?>
          <!--   <a href="#" class="button" id="timelapse-button" data-en="Week Timelapse" data-de="Wochenzeitraffer" data-it="Timelapse settimanale" data-fr="Timelapse hebdomadaire" data-es="Timelapse semanal" data-zh="一周延时">
                Wochenzeitraffer
            </a> -->
            <?php endif; ?>
            <a href="?action=sequence" class="button" data-en="Save Video Clip" data-de="Videoclip speichern" data-it="Salva clip video" data-fr="Enregistrer le clip vidéo" data-es="Guardar clip de vídeo" data-zh="保存视频片段">
                Videoclip speichern
            </a>
            <a href="?download_video=1" class="button" data-en="Download Latest Video" data-de="Tagesvideo downloaden" data-it="Scarica l'ultimo video" data-fr="Télécharger la dernière vidéo" data-es="Descargar el último vídeo" data-zh="下载最新视频">
                Tagesvideo downloaden
            </a>
        </div>
    </div>

<!-- YouTube-Livestream (2026-10-09): Link per Kanal-ID, /live zeigt immer die laufende Uebertragung -->
<div class="yt-live-cta" style="display:flex;flex-wrap:wrap;gap:12px;justify-content:center;margin:22px auto 6px;">
  <a href="https://www.youtube.com/channel/UCkAIp4Vh1X6cvN2DPrxFBeA/live" target="_blank" rel="noopener" style="display:inline-flex;align-items:center;gap:10px;background:#FF0000;color:#fff;font:700 16px/1.2 -apple-system,BlinkMacSystemFont,'Segoe UI',Helvetica,Arial,sans-serif;padding:12px 20px;border-radius:999px;text-decoration:none;box-shadow:0 4px 14px rgba(255,0,0,.25);">
    <svg width="22" height="16" viewBox="0 0 24 17" aria-hidden="true"><path fill="#fff" d="M23.5 2.7A3 3 0 0 0 21.4.6C19.5 0 12 0 12 0S4.5 0 2.6.6A3 3 0 0 0 .5 2.7 31 31 0 0 0 0 8.5a31 31 0 0 0 .5 5.8 3 3 0 0 0 2.1 2.1c1.9.6 9.4.6 9.4.6s7.5 0 9.4-.6a3 3 0 0 0 2.1-2.1 31 31 0 0 0 .5-5.8 31 31 0 0 0-.5-5.8z"/><path fill="#FF0000" d="M9.6 12.1 15.8 8.5 9.6 4.9z"/></svg>
    <span data-de="Live auf YouTube ansehen" data-en="Watch live on YouTube" data-it="Guarda in diretta su YouTube" data-fr="Regarder en direct sur YouTube" data-es="Ver en directo en YouTube" data-zh="在 YouTube 观看直播">Live auf YouTube ansehen</span>
  </a>
  <a href="https://www.youtube.com/channel/UCkAIp4Vh1X6cvN2DPrxFBeA?sub_confirmation=1" target="_blank" rel="noopener" style="display:inline-flex;align-items:center;gap:8px;background:#fff;color:#222;border:1px solid #ddd;font:700 16px/1.2 -apple-system,BlinkMacSystemFont,'Segoe UI',Helvetica,Arial,sans-serif;padding:12px 20px;border-radius:999px;text-decoration:none;">
    <span aria-hidden="true">🔔</span>
    <span data-de="Kanal abonnieren" data-en="Subscribe" data-it="Iscriviti al canale" data-fr="S’abonner à la chaîne" data-es="Suscribirse al canal" data-zh="订阅频道">Kanal abonnieren</span>
  </a>
</div>
</section>

<div style="text-align: center; margin-bottom: 20px;">
    <div class="donation-buttons">
<?php if ($showPaypalDonation): ?>
        <a href="<?php echo htmlspecialchars($paypalDonationUrl); ?>" target="_blank" rel="noopener" class="paypal-donation-link">
            <span class="paypal-logo" aria-hidden="true"><b>Pay</b><i>Pal</i></span>
            <span data-de="Jetzt spenden" data-en="Donate now" data-it="Dona ora" data-fr="Faire un don" data-es="Donar ahora" data-zh="立即捐赠">Jetzt spenden</span>
        </a>
<?php endif; ?>
<?php if ($showBuyMeACoffee): ?>
        <a href="<?php echo htmlspecialchars($settingsManager->getSocialLink('buymeacoffee', 'https://buymeacoffee.com/auroracam')); ?>" target="_blank" rel="noopener" class="donation-link">
            <img src="buy.png" alt="Buy me a coffee" style="height: 267px; border-radius: 8px; transition: transform 0.2s;" onmouseover="this.style.transform='scale(1.05)'" onmouseout="this.style.transform='scale(1)'">
        </a>
<?php endif; ?>
    </div>
    <p class="donation-callout-text" style="font-size: 14px; color: #333; background: rgba(255,255,255,0.85); padding: 8px 16px; border-radius: 8px; margin: 10px auto 0; max-width: 400px; line-height: 1.6;">
        <?php echo nl2br(htmlspecialchars($donationCalloutText)); ?>
    </p>
</div>


<div class="twint-box">
<?php if ($showTwintSupport): ?>
<div class="twint-payment-content">
    <img src="twint1.jpg" alt="TWINT" class="twint-image">


<p
   class="twint-support-text"
   data-de="<?php echo htmlspecialchars($twintSupportText); ?>"
   data-en="Support the Aurora livecam with your donation. You help fund the camera, server, internet connection, and the 100% off-grid power system based on solar energy and EcoFlow storage. This keeps the project sustainable, independent, and freely accessible to everyone."
   data-fr="Soutenez la webcam Aurora par votre don. Vous contribuez au financement de la caméra, du serveur, de la connexion Internet et du système d'alimentation 100 % autonome basé sur l'énergie solaire et le stockage EcoFlow. Cela permet au projet de rester durable, indépendant et accessible à tous." data-es="Apoye la webcam Aurora con su donación. Ayuda a financiar la cámara, el servidor, la conexión a internet y el sistema eléctrico 100 % autónomo basado en energía solar y baterías EcoFlow. Así el proyecto sigue siendo sostenible, independiente y de libre acceso para todos."
   data-it="Sostieni la livecam Aurora con la tua donazione. Aiuti a finanziare la telecamera, il server, la connessione Internet e il sistema di alimentazione 100% autonomo basato su energia solare e accumulo EcoFlow. Così il progetto resta sostenibile, indipendente e accessibile a tutti."
   data-zh="通过捐助支持 Aurora 实时摄像头。您的支持将帮助承担摄像头、服务器、网络连接，以及基于太阳能和 EcoFlow 储能的 100% 自给供电系统成本，让项目保持可持续、独立并持续免费开放。"
   style="font-size: 14px; color: #333; background: rgba(255,255,255,0.92); padding: 14px 18px; border-radius: 10px; margin: 12px auto 0; max-width: 640px; line-height: 1.7;">
    <?php echo htmlspecialchars($twintSupportText); ?>
</p>
</div>
<?php endif; ?>

    <span class="green-label-badge">
        🌱 724 Green Label
    </span>

    <div class="eco-business-box">
        <h3
            data-de="Live & 100% autark: Unsere Aurora-Webcam"
            data-en="Live & 100% Off-Grid: Our Aurora Webcam"
            data-fr="Live & 100% autonome : Notre Webcam Aurora" data-es="En directo y 100 % autónoma: nuestra webcam Aurora"
            data-it="Live & 100% autonoma: La nostra Webcam Aurora"
            data-zh="实时在线 & 100% 能源自给：Aurora 实时摄像头">
            Live & 100% autark: Unsere Aurora-Webcam
        </h3>

        <p class="eco-subtitle"
           data-de="Powered by EcoFlow & Swiss Sun"
           data-en="Powered by EcoFlow & Swiss Sun"
           data-fr="Propulsée par EcoFlow & le Soleil Suisse" data-es="Impulsada por EcoFlow y el sol suizo"
           data-it="Alimentata da EcoFlow & Sole Svizzero"
           data-zh="由 EcoFlow 与瑞士阳光驱动">
            Powered by EcoFlow & Swiss Sun
        </p>

        <p
            data-de="Erleben Sie Dürnten in Echtzeit – angetrieben durch die reine Kraft der Natur. Unsere Infrastruktur wird zu 100 % mit Solarstrom und EcoFlow-Batteriespeichern betrieben."
            data-en="Experience Dürnten in real-time – powered by the pure force of nature. Our infrastructure runs 100% on solar power and EcoFlow battery storage."
            data-fr="Découvrez Dürnten en temps réel, propulsé par l'énergie pure de la nature. Notre infrastructure fonctionne à 100 % à l'énergie solaire avec le stockage par batterie EcoFlow." data-es="Viva Dürnten en tiempo real, con la fuerza pura de la naturaleza. Nuestra infraestructura funciona al 100 % con energía solar y baterías EcoFlow."
            data-it="Scoprite Dürnten in tempo reale, alimentata dalla pura forza della natura. La nostra infrastruttura è gestita al 100 % con energia solare e sistemi di accumulo EcoFlow."
            data-zh="实时感受迪恩滕（Dürnten）—— 由纯净自然能量驱动。我们的基础设施 100% 采用太阳能和 EcoFlow 电池储能系统。">
            Erleben Sie Dürnten in Echtzeit – angetrieben durch die reine Kraft der Natur. Unsere Infrastruktur wird zu 100 % mit Solarstrom und EcoFlow-Batteriespeichern betrieben.
        </p>

        <ul class="eco-feature-list">
            <li
                data-de="724 Green Label Swiss: "
                data-en="724 Green Label Swiss: "
                data-fr="724 Green Label Swiss: " data-es="724 Green Label Swiss: "
                data-it="724 Green Label Swiss: "
                data-zh="724 Green Label Swiss：">
                724 Green Label Swiss:.
            </li>

            <li
                data-de="24/7 Green Power: Dank modernster Speichertechnologie."
                data-en="24/7 Green Energy: Thanks to state-of-the-art storage technology."
                data-fr="Énergie Verte 24/7 : Grâce à une technologie de stockage de pointe." data-es="Energía verde 24/7: gracias a la tecnología de almacenamiento más moderna."
                data-it="Energia Verde 24/7: Grazie a una tecnologia di accumulo all'avanguardia."
                data-zh="24/7 绿色能源：得益于尖端的储能技术。">
                24/7 Green Power: Dank modernster Speichertechnologie.
            </li>

            <li
                data-de="Emissionsfrei: Innovation direkt aus dem Herzen der Schweiz."
                data-en="Zero Emissions: Innovation straight from the heart of Switzerland."
                data-fr="Zéro Émission : L'innovation au cœur de la Suisse." data-es="Cero emisiones: innovación desde el corazón de Suiza."
                data-it="Zero Emissioni: Innovazione direttamente dal cuore della Svizzera."
                data-zh="零排放：源自瑞士心脏地带的创新。">
                Emissionsfrei: Innovation direkt aus dem Herzen der Schweiz.
            </li>
        </ul>
    </div>
</div>

<!-- Starlink-Banner ausgeblendet (2026-09-30); wieder zeigen: style entfernen -->
<div class="starlink-banner-wrapper" style="display:none">
    <div class="starlink-banner">
        <h3>🛰️ Schnelles Internet für Ihre Livestreams</h3>
        <p>
            Für hochwertige Webcam-Übertragungen empfehlen wir Starlink –
            Highspeed-Internet überall verfügbar, perfekt für abgelegene Standorte!
        </p>
        <a href="https://www.starlink.com/" target="_blank" rel="noopener noreferrer">
            <img src="starlink.png" alt="Starlink - Schnelles Internet überall">
        </a>
    </div>
</div>
 


<!-- ARCHIVE SECTION -->
<!-- ZEITREISE (2026-10-09): gleiches Bild wie jetzt, vor N Tagen. API /api/zeitreise.php -->
<section id="zeitreise" class="section">
    <div class="container">
        <h2 data-de="Zeitreise" data-en="Time travel" data-it="Viaggio nel tempo" data-fr="Voyage dans le temps" data-es="Viaje en el tiempo" data-zh="时光回溯">Zeitreise</h2>
        <p class="zr-intro" data-de="Derselbe Blick zur gleichen Uhrzeit – wie sah es damals aus? Linie ziehen zum Vergleichen."
           data-en="The same view at the same time of day – what did it look like back then? Drag the line to compare."
           data-it="La stessa vista alla stessa ora – com’era allora? Trascina la linea per confrontare."
           data-fr="La même vue à la même heure – à quoi cela ressemblait-il ? Faites glisser la ligne pour comparer."
           data-es="La misma vista a la misma hora: ¿cómo era entonces? Arrastra la línea para comparar."
           data-zh="同一时刻的同一视角——当时是什么样子？拖动分割线进行对比。">Derselbe Blick zur gleichen Uhrzeit – wie sah es damals aus? Linie ziehen zum Vergleichen.</p>
        <div class="zr-knoepfe" id="zrKnoepfe" role="group" aria-label="Zeitreise"></div>
        <div class="zr-vergleich" id="zrVergleich" hidden>
            <img id="zrJetzt" alt="" loading="lazy" decoding="async">
            <img id="zrDamals" alt="" decoding="async">
            <div class="zr-linie" id="zrLinie" aria-hidden="true"></div>
            <span class="zr-marke zr-links" id="zrMarkeDamals"></span>
            <span class="zr-marke zr-rechts" id="zrMarkeJetzt"></span>
            <input type="range" id="zrRegler" min="0" max="100" value="50" aria-label="Vergleich">
        </div>
        <p class="zr-hinweis" id="zrHinweis"></p>
    </div>
    <style>
        #zeitreise .zr-intro { text-align: center; color: #555; margin: 0 auto 16px; max-width: 640px; }
        #zeitreise .zr-knoepfe { display: flex; flex-wrap: wrap; gap: 8px; justify-content: center; margin-bottom: 16px; }
        #zeitreise .zr-knoepfe button { border: 1px solid #c9b8b5; background: #fff; color: #222; border-radius: 999px; padding: 8px 16px; font: 600 14px/1.2 -apple-system, BlinkMacSystemFont, 'Segoe UI', Helvetica, Arial, sans-serif; cursor: pointer; }
        #zeitreise .zr-knoepfe button[aria-pressed="true"] { background: #b3122e; border-color: #b3122e; color: #fff; }
        #zeitreise .zr-vergleich { position: relative; max-width: 1100px; margin: 0 auto; aspect-ratio: 16 / 9; border-radius: 14px; overflow: hidden; background: #0b1424; user-select: none; touch-action: pan-y; }
        #zeitreise .zr-vergleich img { position: absolute; inset: 0; width: 100%; height: 100%; object-fit: cover; }
        #zeitreise #zrDamals { clip-path: inset(0 50% 0 0); }
        #zeitreise .zr-linie { position: absolute; top: 0; bottom: 0; left: 50%; width: 3px; margin-left: -1.5px; background: #fff; box-shadow: 0 0 6px rgba(0,0,0,.5); pointer-events: none; }
        #zeitreise .zr-linie::after { content: '⟷'; position: absolute; top: 50%; left: 50%; transform: translate(-50%, -50%); background: #fff; color: #b3122e; border-radius: 999px; width: 40px; height: 40px; display: grid; place-items: center; font-size: 20px; box-shadow: 0 2px 8px rgba(0,0,0,.35); }
        #zeitreise .zr-marke { position: absolute; top: 12px; background: rgba(0,0,0,.6); color: #fff; padding: 4px 10px; border-radius: 999px; font: 600 13px/1.3 -apple-system, BlinkMacSystemFont, 'Segoe UI', Helvetica, Arial, sans-serif; pointer-events: none; }
        #zeitreise .zr-links { left: 12px; } #zeitreise .zr-rechts { right: 12px; }
        #zeitreise #zrRegler { position: absolute; inset: 0; width: 100%; height: 100%; opacity: 0; cursor: ew-resize; margin: 0; }
        #zeitreise .zr-hinweis { text-align: center; color: #777; font-size: 13px; margin-top: 10px; min-height: 1em; }
    </style>
    <script>
    (function () {
        var T = {
            de: {1: 'Gestern', 7: 'Vor 1 Woche', 30: 'Vor 1 Monat', 91: 'Vor 3 Monaten', 182: 'Vor 6 Monaten', 365: 'Vor 1 Jahr', 730: 'Vor 2 Jahren', jetzt: 'Jetzt', laedt: 'Bild wird geladen …', keins: 'Für diesen Zeitpunkt gibt es noch kein Archivbild.', daneben: 'nächstes Archivbild', loc: 'de-CH'},
            en: {1: 'Yesterday', 7: '1 week ago', 30: '1 month ago', 91: '3 months ago', 182: '6 months ago', 365: '1 year ago', 730: '2 years ago', jetzt: 'Now', laedt: 'Loading image …', keins: 'No archive image for this moment yet.', daneben: 'nearest archive image', loc: 'en-GB'},
            it: {1: 'Ieri', 7: '1 settimana fa', 30: '1 mese fa', 91: '3 mesi fa', 182: '6 mesi fa', 365: '1 anno fa', 730: '2 anni fa', jetzt: 'Adesso', laedt: 'Caricamento …', keins: 'Ancora nessuna immagine d’archivio per questo momento.', daneben: 'immagine d’archivio più vicina', loc: 'it-CH'},
            fr: {1: 'Hier', 7: 'Il y a 1 semaine', 30: 'Il y a 1 mois', 91: 'Il y a 3 mois', 182: 'Il y a 6 mois', 365: 'Il y a 1 an', 730: 'Il y a 2 ans', jetzt: 'Maintenant', laedt: 'Chargement …', keins: 'Pas encore d’image d’archive pour ce moment.', daneben: 'image d’archive la plus proche', loc: 'fr-CH'},
            es: {1: 'Ayer', 7: 'Hace 1 semana', 30: 'Hace 1 mes', 91: 'Hace 3 meses', 182: 'Hace 6 meses', 365: 'Hace 1 año', 730: 'Hace 2 años', jetzt: 'Ahora', laedt: 'Cargando …', keins: 'Todavía no hay imagen de archivo para este momento.', daneben: 'imagen de archivo más cercana', loc: 'es-ES'},
            zh: {1: '昨天', 7: '1 周前', 30: '1 个月前', 91: '3 个月前', 182: '6 个月前', 365: '1 年前', 730: '2 年前', jetzt: '现在', laedt: '正在加载 …', keins: '此时刻暂无存档图像。', daneben: '最接近的存档图像', loc: 'zh-CN'}
        };
        var angebot = [], gewaehlt = null;
        var $ = function (id) { return document.getElementById(id); };
        function sprache() { var l = (document.documentElement.lang || 'de').slice(0, 2); return T[l] ? l : 'de'; }
        function t(k) { return T[sprache()][k]; }
        function datum(iso) {
            try { return new Date(iso).toLocaleString(t('loc'), {day: 'numeric', month: 'short', year: 'numeric', hour: '2-digit', minute: '2-digit', timeZone: 'Europe/Zurich'}); }
            catch (e) { return iso; }
        }
        function knoepfe() {
            var box = $('zrKnoepfe'); box.innerHTML = '';
            angebot.forEach(function (e) {
                if (!e.ok) return;
                var b = document.createElement('button'); b.type = 'button';
                b.textContent = t(e.tage) || (e.tage + ' d');
                b.setAttribute('aria-pressed', gewaehlt && gewaehlt.tage === e.tage ? 'true' : 'false');
                b.addEventListener('click', function () { zeigen(e); });
                box.appendChild(b);
            });
        }
        function marken() {
            if (!gewaehlt) return;
            $('zrMarkeDamals').textContent = datum(gewaehlt.zeit);
            $('zrMarkeJetzt').textContent = t('jetzt');
            $('zrHinweis').textContent = gewaehlt.tage_daneben ? '(' + t('daneben') + ')' : '';
        }
        function zeigen(e) {
            gewaehlt = e; knoepfe();
            $('zrVergleich').hidden = false;
            $('zrHinweis').textContent = t('laedt');
            $('zrJetzt').src = '/image/current.jpg?t=' + Math.floor(Date.now() / 60000);
            var img = $('zrDamals');
            img.onload = marken;
            img.onerror = function () { $('zrHinweis').textContent = t('keins'); };
            img.src = e.url;
        }
        function regler() {
            var v = $('zrRegler').value;
            $('zrDamals').style.clipPath = 'inset(0 ' + (100 - v) + '% 0 0)';
            $('zrLinie').style.left = v + '%';
        }
        $('zrRegler').addEventListener('input', regler);
        // Sprachwahl der Seite setzt <html lang>; Beschriftungen nachziehen.
        new MutationObserver(function () { knoepfe(); marken(); }).observe(document.documentElement, {attributes: true, attributeFilter: ['lang']});
        fetch('/api/zeitreise.php', {cache: 'no-store'}).then(function (r) { return r.json(); }).then(function (d) {
            angebot = (d && d.angebot) || [];
            var erstes = angebot.filter(function (e) { return e.ok; })[0];
            if (!erstes) { $('zrHinweis').textContent = t('keins'); return; }
            knoepfe();
            // Bild erst laden, wenn der Bereich in Sicht kommt (4K-Archivbilder).
            var beob = new IntersectionObserver(function (eintr) {
                if (eintr[0].isIntersecting) { beob.disconnect(); zeigen(angebot.filter(function (e) { return e.ok && e.tage === 7; })[0] || erstes); }
            }, {rootMargin: '200px'});
            beob.observe($('zeitreise'));
        }).catch(function () { $('zeitreise').style.display = 'none'; });
    })();
    </script>
</section>

<section id="archive" class="section">
    <div class="container">
        <h2 data-en="Video Archive" data-de="Videoarchiv Tagesvideos" data-it="Archivio video giornalieri" data-fr="Archive des vidéos quotidiennes" data-es="Archivo de vídeos diarios" data-zh="每日视频档案">Videoarchiv Tagesvideos</h2>

        <?php
        $visualCalendar = new VisualCalendarManager(['./videos/', '/mnt/aurora-remote/videos/'], './ai/', $settingsManager);
        echo $visualCalendar->displayVisualCalendar();
        ?>
    </div>
</section>


<!-- STANDORT -->
<section id="standort" class="section" style="padding: 40px 0;">
    <div class="container" style="text-align: center;">
        <h2 data-en="Camera Direction" data-de="Kamera-Blickrichtung" data-it="Direzione della camera" data-fr="Direction de la caméra" data-es="Orientación de la cámara" data-zh="摄像头方向">Kamera-Blickrichtung</h2>
        <div style="display: flex; justify-content: center; align-items: center; flex-wrap: wrap; gap: 30px; margin-top: 30px;">
            <div style="max-width: 350px;">
                <img src="kompass1.png" alt="Kompass zeigt Blickrichtung der Webcam Richtung Zürichsee und Schweizer Alpen" style="width: 100%; border-radius: 12px; box-shadow: 0 10px 30px rgba(0,0,0,0.3);">
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
<section id="qr-code" class="section" style="display: <?php echo $settingsManager->shouldShowQRCode() ? 'block' : 'none'; ?>;">
    <div class="container" style="text-align: center;">
        <h1>
            <p data-en="Follow us and share with friends" data-de="Folge uns und teile mit Freunden" data-it="Seguici e condividi con gli amici" data-fr="Suivez-nous et partagez avec vos amis" data-es="Síganos y compártalo con sus amigos" data-zh="关注我们并分享给朋友">
                Folge uns und kopiere den Code und sende es deinen Freunden
            </p>
        </h1>
        <div id="qrcode" data-url="<?php echo $siteConfig['domainUrl']; ?>/"></div>
        <p data-en="Click QR code to copy URL" data-de="Klicke auf den QR-Code um die URL zu kopieren" data-it="Fai clic sul codice QR per copiare l'URL" data-fr="Cliquez sur le code QR pour copier l'URL" data-es="Haga clic en el código QR para copiar la URL" data-zh="点击二维码复制网址">
            Klicke auf den QR-Code, um die URL zu kopieren
        </p>


      
    </div>
</section>

<!-- GUESTBOOK -->
<section id="guestbook" class="section" style="display: <?php echo $settingsManager->isGuestbookEnabled() ? 'block' : 'none'; ?>;">
    <div class="container">
        <h2 data-en="Guestbook" data-de="Gästebuch" data-it="Libro degli ospiti" data-fr="Livre d'or" data-es="Libro de visitas" data-zh="留言簿">Gästebuch</h2>
        <?php
        echo $guestbookManager->displayForm();
        echo $guestbookManager->displayEntries($adminManager->isAdmin());
        ?>
    </div>
</section>

<!-- CONTACT -->
<section id="kontakt" class="section">
    <div class="container">
        <h2 data-en="Contact" data-de="Kontakt" data-it="Contatto" data-fr="Contact" data-es="Contacto" data-zh="联系">Kontakt</h2>
        <p data-en="Questions or suggestions? We look forward to hearing from you!"
           data-de="Haben Sie Fragen, Anregungen oder möchten uns unterstützen? Wir freuen uns auf Ihre Nachricht!"
           data-it="Domande o suggerimenti? Saremo felici di sentirti!"
           data-fr="Des questions ou des suggestions ? Nous serions ravis d'avoir de vos nouvelles !" data-es="¿Preguntas o sugerencias? ¡Nos encantará saber de usted!"
           data-zh="有问题或建议吗？期待您的来信！">
            Haben Sie Fragen, Anregungen oder möchten uns unterstützen? Wir freuen uns auf Ihre Nachricht!
        </p>
        <?php echo $contactManager->displayForm(); ?>
    </div>
</section>

<!-- GALLERY -->
<section id="gallery" class="section" style="display: <?php echo $settingsManager->isGalleryEnabled() ? 'block' : 'none'; ?>;">
    <div class="container">
        <h2 data-en="Image Gallery" data-de="Bildergalerie" data-it="Galleria immagini" data-fr="Galerie d'images" data-es="Galería de imágenes" data-zh="图片库">Bildergalerie</h2>
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
        <h2 data-en="About Our Project" data-de="Über unser Projekt" data-it="Il nostro progetto" data-fr="À propos de notre projet" data-es="Sobre nuestro proyecto" data-zh="关于我们的项目">Über unser Projekt</h2>
        <div class="about-grid">
            <div class="about-item">
                <p data-en="<?php echo $siteConfig['aboutEn']; ?>"
                   data-de="<?php echo $siteConfig['aboutDe']; ?>"
                   data-it="Aurora Weather Livecam è un progetto del cuore di appassionati di meteorologia. Vogliamo avvicinarvi alla bellezza della natura e al fascino del tempo."
                   data-fr="Aurora Weather Livecam est un projet de passionnés de météo. Nous souhaitons vous faire découvrir la beauté de la nature et la fascination du temps." data-es="Aurora Weather Livecam es un proyecto de apasionados de la meteorología. Queremos acercarle la belleza de la naturaleza y la fascinación del tiempo."
                   data-zh="Aurora Weather Livecam 是天气爱好者的热情项目。我们希望让您更贴近自然之美与天气的魅力。">
                    <?php echo $siteConfig['aboutDe']; ?>
                </p>
                <p data-en="We have been operating high-resolution webcams around the clock since 2010. We are particularly proud of unique insights, such as the Patrouille Suisse training flights every Monday morning."
                   data-de="Dazu betreiben wir seit 2010 rund um die Uhr hochauflösende Webcams. Besonders stolz sind wir auf einzigartige Einblicke, wie z.B. die Trainingsflüge der Patrouille Suisse jeden Montagmorgen."
                   data-it="Dal 2010 gestiamo webcam ad alta risoluzione 24 ore su 24. Siamo particolarmente orgogliosi di scorci unici, come i voli di addestramento della Patrouille Suisse ogni lunedì mattina."
                   data-fr="Depuis 2010, nous exploitons des webcams haute résolution 24h/24. Nous sommes particulièrement fiers d'aperçus uniques, comme les vols d'entraînement de la Patrouille Suisse chaque lundi matin." data-es="Desde 2010 operamos webcams de alta resolución las 24 horas. Estamos especialmente orgullosos de vistas únicas, como los vuelos de entrenamiento de la Patrouille Suisse cada lunes por la mañana."
                   data-zh="自2010年以来，我们全天候运行高分辨率摄像头。我们尤其自豪于独特的视角，例如每周一早上的瑞士巡逻兵训练飞行。">
                    Dazu betreiben wir seit 2010 rund um die Uhr hochauflösende Webcams. Besonders stolz sind wir auf einzigartige Einblicke, wie z.B. die Trainingsflüge der Patrouille Suisse jeden Montagmorgen.
                </p>
            </div>
        </div>
    </div>
</section>

<!-- ADMIN SECTION -->
<?php if ($adminManager->isAdmin()): ?>
<section id="admin" class="section">
    <div class="container">
        <h2 data-en="Admin Area" data-de="Admin-Bereich" data-it="Area admin" data-fr="Espace admin" data-es="Área de administración" data-zh="管理员区域">Admin-Bereich</h2>
        <?php echo $adminManager->displayAdminContent(); ?>
    </div>
</section>
<?php else: ?>
<section id="admin-login" class="section">
    <div class="container">
        <h2 data-en="Admin Login" data-de="Admin Login" data-it="Accesso admin" data-fr="Connexion admin" data-es="Acceso de administración" data-zh="管理员登录">Admin Login</h2>
        <?php echo $adminManager->displayLoginForm(); ?>
    </div>
</section>
<?php endif; ?>

<!-- PATROUILLE SUISSE SEKTION -->
<section id="patrouille-suisse" class="section" style="background: linear-gradient(135deg, #1a1a2e 0%, #16213e 100%); display: <?php echo $settingsManager->shouldShowPatrouillesuisse() ? 'block' : 'none'; ?>;">
    <div class="container">
        <h2 style="color: #fff; text-align: center;" data-en="Patrouille Suisse Live - Watch Training Flights" data-de="Patrouille Suisse Live - Trainingsflüge Beobachten" data-it="Patrouille Suisse Live - Guarda i voli di addestramento" data-fr="Patrouille Suisse en direct - Regardez les vols d'entraînement" data-es="Patrouille Suisse en directo: vea los vuelos de entrenamiento" data-zh="瑞士巡逻兵直播 - 观看训练飞行">Patrouille Suisse Live - Trainingsflüge Beobachten</h2>
        <div style="display: grid; grid-template-columns: repeat(auto-fit, minmax(300px, 1fr)); gap: 30px; margin-top: 30px;">
            <div style="background: rgba(255,255,255,0.1); padding: 25px; border-radius: 15px; backdrop-filter: blur(10px);">
                <h3 style="color: #ff6b6b; margin-bottom: 15px;" data-en="Every Monday Live!" data-de="Jeden Montag Live!" data-it="Ogni lunedì in diretta!" data-fr="Tous les lundis en direct !" data-es="¡Cada lunes en directo!" data-zh="每周一直播！">Jeden Montag Live!</h3>
                <p style="color: #ddd; line-height: 1.8;" data-en="The Patrouille Suisse, the official aerobatic team of the Swiss Air Force, trains every Monday morning in the Zurich Oberland region. Our webcam offers a unique view of the spectacular flight maneuvers of the six F-5E Tiger II jets." data-de="Die Patrouille Suisse, das offizielle Kunstflugteam der Schweizer Luftwaffe, trainiert jeden Montagmorgen in der Region Zürich Oberland. Unsere Webcam bietet einen einzigartigen Blick auf die spektakulären Flugmanöver der sechs F-5E Tiger II Jets." data-it="La Patrouille Suisse, il team acrobatico ufficiale dell'Aeronautica militare svizzera, si addestra ogni lunedì mattina nella regione dell'Oberland di Zurigo. La nostra webcam offre una vista unica delle spettacolari manovre di volo dei sei F-5E Tiger II." data-fr="La Patrouille Suisse, l'équipe officielle de voltige des Forces aériennes suisses, s'entraîne chaque lundi matin dans la région de l'Oberland zurichois. Notre webcam offre une vue unique des spectaculaires manœuvres de vol des six F-5E Tiger II." data-es="La Patrouille Suisse, el equipo acrobático oficial de las Fuerzas Aéreas suizas, entrena cada lunes por la mañana en la región del Oberland de Zúrich. Nuestra webcam ofrece una vista única de las espectaculares maniobras de los seis aviones F-5E Tiger II." data-zh="瑞士巡逻兵是瑞士空军的官方特技飞行队，每周一早上在苏黎世高地地区训练。我们的摄像头提供了观赏六架 F-5E Tiger II 喷气机精彩机动的独特视角。">
                    Die Patrouille Suisse, das offizielle Kunstflugteam der Schweizer Luftwaffe, trainiert jeden <strong style="color: #fff;">Montagmorgen</strong> in der Region Zürich Oberland. Unsere Webcam bietet einen einzigartigen Blick auf die spektakulären Flugmanöver der sechs F-5E Tiger II Jets.
                </p>
                <ul style="color: #ccc; margin-top: 15px; padding-left: 20px;">
                    <li data-en="Training time: approx. 09:00 - 11:00" data-de="Trainingszeit: ca. 09:00 - 11:00 Uhr" data-it="Orario di addestramento: circa 09:00 - 11:00" data-fr="Heure d'entraînement : env. 09:00 - 11:00" data-es="Horario de entrenamiento: aprox. 09:00 - 11:00" data-zh="训练时间：约 09:00 - 11:00">Trainingszeit: ca. 09:00 - 11:00 Uhr</li>
                    <li data-en="Visible in good weather" data-de="Bei gutem Wetter sichtbar" data-it="Visibile con bel tempo" data-fr="Visible par beau temps" data-es="Visible con buen tiempo" data-zh="天气良好时可见">Bei gutem Wetter sichtbar</li>
                    <li data-en="Unique perspective from Zurich Oberland" data-de="Einzigartige Perspektive aus dem Zürcher Oberland" data-it="Prospettiva unica dall'Oberland di Zurigo" data-fr="Perspective unique depuis l'Oberland zurichois" data-es="Perspectiva única desde el Oberland de Zúrich" data-zh="来自苏黎世高地的独特视角">Einzigartige Perspektive aus dem Zürcher Oberland</li>
                </ul>
            </div>
            <div style="background: rgba(255,255,255,0.1); padding: 25px; border-radius: 15px; backdrop-filter: blur(10px);">
                <h3 style="color: #4ecdc4; margin-bottom: 15px;" data-en="History of Patrouille Suisse" data-de="Geschichte der Patrouille Suisse" data-it="Storia della Patrouille Suisse" data-fr="Histoire de la Patrouille Suisse" data-es="Historia de la Patrouille Suisse" data-zh="瑞士巡逻兵历史">Geschichte der Patrouille Suisse</h3>
                <p style="color: #ddd; line-height: 1.8;" data-en="Founded in 1964, the Patrouille Suisse is one of Europe's most renowned aerobatic teams. The team has been flying the Northrop F-5E Tiger II since 1995 and delights audiences at shows throughout Switzerland and internationally." data-de="Gegründet 1964, ist die Patrouille Suisse eines der renommiertesten Kunstflugteams Europas. Das Team fliegt seit 1995 die Northrop F-5E Tiger II und begeistert bei Shows in der ganzen Schweiz und international." data-it="Fondata nel 1964, la Patrouille Suisse è uno dei team acrobatici più rinomati d'Europa. Dal 1995 il team vola con i Northrop F-5E Tiger II e entusiasma il pubblico in Svizzera e all'estero." data-fr="Fondée en 1964, la Patrouille Suisse est l'une des équipes de voltige les plus renommées d'Europe. L'équipe vole sur Northrop F-5E Tiger II depuis 1995 et séduit le public en Suisse et à l'international." data-es="Fundada en 1964, la Patrouille Suisse es uno de los equipos acrobáticos más prestigiosos de Europa. Desde 1995 vuela con el Northrop F-5E Tiger II y entusiasma al público en exhibiciones en Suiza y en el extranjero." data-zh="瑞士巡逻兵成立于1964年，是欧洲最著名的特技飞行队之一。该队自1995年以来驾驶 Northrop F-5E Tiger II，在瑞士及国际航展上深受观众喜爱。">
                    Gegründet 1964, ist die Patrouille Suisse eines der renommiertesten Kunstflugteams Europas. Das Team fliegt seit 1995 die Northrop F-5E Tiger II und begeistert bei Shows in der ganzen Schweiz und international.
                </p>
                <p style="color: #ddd; margin-top: 15px;" data-en="Home base: Payerne (VD) | Aircraft: 6x F-5E Tiger II | Team size: 6 pilots + crew" data-de="Heimatbasis: Payerne (VD) | Flugzeuge: 6x F-5E Tiger II | Teamgrösse: 6 Piloten + Crew" data-it="Base: Payerne (VD) | Aeromobili: 6x F-5E Tiger II | Team: 6 piloti + personale" data-fr="Base : Payerne (VD) | Avions : 6x F-5E Tiger II | Équipe : 6 pilotes + équipe" data-es="Base: Payerne (VD) | Aviones: 6x F-5E Tiger II | Equipo: 6 pilotos + personal" data-zh="基地：Payerne (VD) | 飞机：6 架 F-5E Tiger II | 团队规模：6 名飞行员 + 机组">
                    <strong style="color: #fff;" data-en="Home base:" data-de="Heimatbasis:" data-it="Base:" data-fr="Base :" data-es="Base:" data-zh="基地：">Heimatbasis:</strong> Payerne (VD)<br>
                    <strong style="color: #fff;" data-en="Aircraft:" data-de="Flugzeuge:" data-it="Aeromobili:" data-fr="Avions :" data-es="Aviones:" data-zh="飞机：">Flugzeuge:</strong> 6x F-5E Tiger II<br>
                    <strong style="color: #fff;" data-en="Team size:" data-de="Teamgrösse:" data-it="Team:" data-fr="Équipe :" data-es="Equipo:" data-zh="团队规模：">Teamgrösse:</strong> 6 <span data-en="pilots + crew" data-de="Piloten + Crew" data-it="piloti + personale" data-fr="pilotes + équipe" data-es="pilotos + personal" data-zh="飞行员 + 机组">Piloten + Crew</span>
                </p>
            </div>
            <div style="background: rgba(255,255,255,0.1); padding: 25px; border-radius: 15px; backdrop-filter: blur(10px);">
                <h3 style="color: #ffd93d; margin-bottom: 15px;" data-en="Best Viewing Tips" data-de="Beste Beobachtungstipps" data-it="Consigli per la migliore visione" data-fr="Conseils pour une meilleure observation" data-es="Consejos para verlo mejor" data-zh="最佳观看提示">Beste Beobachtungstipps</h3>
                <p style="color: #ddd; line-height: 1.8;" data-en="For the best view of the training flights, we recommend:" data-de="Für die beste Sicht auf die Trainingsflüge empfehlen wir:" data-it="Per la migliore visione dei voli di addestramento, consigliamo:" data-fr="Pour la meilleure vue des vols d'entraînement, nous recommandons :" data-es="Para ver mejor los vuelos de entrenamiento, recomendamos:" data-zh="为获得最佳的训练飞行观赏效果，我们建议：">
                    Für die beste Sicht auf die Trainingsflüge empfehlen wir:
                </p>
                <ul style="color: #ccc; margin-top: 15px; padding-left: 20px;">
                    <li data-en="Use the zoom function of our webcam" data-de="Nutzen Sie die Zoom-Funktion unserer Webcam" data-it="Usa la funzione zoom della nostra webcam" data-fr="Utilisez la fonction zoom de notre webcam" data-es="Use la función de zoom de nuestra webcam" data-zh="使用我们摄像头的缩放功能">Nutzen Sie die Zoom-Funktion unserer Webcam</li>
                    <li data-en="Timelapse mode for accelerated view" data-de="Timelapse-Modus für beschleunigte Ansicht" data-it="Modalità timelapse per una vista accelerata" data-fr="Mode timelapse pour une vue accélérée" data-es="Modo timelapse para una vista acelerada" data-zh="使用延时模式加速观看">Timelapse-Modus für beschleunigte Ansicht</li>
                    <li data-en="Daily videos to watch later" data-de="Tagesvideos zum Nachschauen" data-it="Video giornalieri da rivedere" data-fr="Vidéos quotidiennes à revoir" data-es="Vídeos diarios para ver más tarde" data-zh="每日视频可供回看">Tagesvideos zum Nachschauen</li>
                    <li data-en="AI detection marks aircraft sightings" data-de="AI-Erkennung markiert Flugzeug-Sichtungen" data-it="Il rilevamento AI segnala gli avvistamenti di aerei" data-fr="La détection IA signale les observations d'avions" data-es="La detección por IA marca los avistamientos de aviones" data-zh="AI 检测会标记飞机出现">AI-Erkennung markiert Flugzeug-Sichtungen</li>
                </ul>
                <p style="color: #aaa; margin-top: 15px; font-size: 14px;" data-en="Note: Trainings may be cancelled in bad weather." data-de="Hinweis: Bei schlechtem Wetter können Trainings abgesagt werden." data-it="Nota: gli addestramenti possono essere annullati in caso di maltempo." data-fr="Remarque : les entraînements peuvent être annulés en cas de mauvais temps." data-es="Nota: los entrenamientos pueden cancelarse con mal tiempo." data-zh="注意：恶劣天气时训练可能会取消。">
                    <em>Hinweis: Bei schlechtem Wetter können Trainings abgesagt werden.</em>
                </p>
            </div>
        </div>
    </div>
</section>

<!-- BLOG SEKTION -->
<section id="blog" class="section" style="background: #f8f9fa;">
    <div class="container">
        <h2 style="text-align: center; margin-bottom: 10px;"><?php echo $siteConfig['blogTitle']; ?></h2>
        <p style="text-align: center; color: #666; margin-bottom: 40px;" data-en="Latest weather news, webcam updates and nature observations from Zurich Oberland" data-de="Aktuelle Wetter-News, Webcam-Updates und Naturbeobachtungen aus dem Zürcher Oberland" data-it="Ultime notizie meteo, aggiornamenti della webcam e osservazioni naturalistiche dall'Oberland di Zurigo" data-fr="Dernières actualités météo, mises à jour de la webcam et observations de la nature depuis l'Oberland zurichois" data-es="Noticias del tiempo, novedades de la webcam y observaciones de la naturaleza del Oberland de Zúrich" data-zh="来自苏黎世高地的最新天气资讯、摄像头更新和自然观察">Aktuelle Wetter-News, Webcam-Updates und Naturbeobachtungen aus dem Zürcher Oberland</p>

        <div style="display: grid; grid-template-columns: repeat(auto-fit, minmax(320px, 1fr)); gap: 30px;">
            <!-- Blog Artikel 1 -->
            <article style="background: #fff; border-radius: 15px; overflow: hidden; box-shadow: 0 5px 20px rgba(0,0,0,0.1);">
                <div style="height: 180px; background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); display: flex; align-items: center; justify-content: center;">
                    <span style="font-size: 60px;">🌅</span>
                </div>
                <div style="padding: 25px;">
                    <h3 style="margin-bottom: 10px; color: #333;" data-en="Sunrises over Lake Zurich" data-de="Sonnenaufgänge über dem Zürichsee" data-it="Albe sul Lago di Zurigo" data-fr="Levers de soleil sur le lac de Zurich" data-es="Amaneceres sobre el lago de Zúrich" data-zh="苏黎世湖日出">Sonnenaufgänge über dem Zürichsee</h3>
                    <p style="color: #666; font-size: 14px; margin-bottom: 15px;" data-en="January 2024" data-de="Januar 2024" data-it="Gennaio 2024" data-fr="Janvier 2024" data-es="Enero de 2024" data-zh="2024年1月">Januar 2024</p>
                    <p style="color: #555; line-height: 1.7;" data-en="The winter months offer spectacular sunrises over Lake Zurich. Our AI detection automatically identifies the most beautiful moments and saves them in the gallery." data-de="Die Wintermonate bieten spektakuläre Sonnenaufgänge über dem Zürichsee. Unsere AI-Erkennung identifiziert automatisch die schönsten Momente und speichert sie in der Galerie." data-it="I mesi invernali offrono spettacolari albe sul Lago di Zurigo. Il nostro rilevamento AI identifica automaticamente i momenti più belli e li salva nella galleria." data-fr="Les mois d'hiver offrent des levers de soleil spectaculaires sur le lac de Zurich. Notre détection IA identifie automatiquement les plus beaux moments et les enregistre dans la galerie." data-es="Los meses de invierno ofrecen amaneceres espectaculares sobre el lago de Zúrich. Nuestra detección por IA identifica automáticamente los momentos más bellos y los guarda en la galería." data-zh="冬季在苏黎世湖上空可见壮观的日出。我们的 AI 检测会自动识别最美瞬间并保存到图库。">
                        Die Wintermonate bieten spektakuläre Sonnenaufgänge über dem Zürichsee. Unsere AI-Erkennung identifiziert automatisch die schönsten Momente und speichert sie in der Galerie.
                    </p>
                    <p style="color: #555; line-height: 1.7; margin-top: 10px;" data-en="Especially with high fog, impressive lighting moods are created when the sun breaks through the cloud cover." data-de="Besonders bei Hochnebel entstehen eindrucksvolle Lichtstimmungen, wenn die Sonne durch die Wolkendecke bricht." data-it="Soprattutto con la nebbia alta si creano suggestive atmosfere di luce quando il sole rompe la coltre di nubi." data-fr="Surtout en cas de brouillard élevé, des ambiances lumineuses impressionnantes se créent lorsque le soleil perce la couverture nuageuse." data-es="Sobre todo con niebla alta se crean ambientes de luz impresionantes cuando el sol atraviesa las nubes." data-zh="尤其在高雾天气，当太阳穿透云层时会形成迷人的光影氛围。">
                        Besonders bei Hochnebel entstehen eindrucksvolle Lichtstimmungen, wenn die Sonne durch die Wolkendecke bricht.
                    </p>
                </div>
            </article>

            <!-- Blog Artikel 2 -->
            <article style="background: #fff; border-radius: 15px; overflow: hidden; box-shadow: 0 5px 20px rgba(0,0,0,0.1);">
                <div style="height: 180px; background: linear-gradient(135deg, #11998e 0%, #38ef7d 100%); display: flex; align-items: center; justify-content: center;">
                    <span style="font-size: 60px;">🏔️</span>
                </div>
                <div style="padding: 25px;">
                    <h3 style="margin-bottom: 10px; color: #333;" data-en="Alpine Panorama in Winter" data-de="Alpenpanorama im Winter" data-it="Panorama alpino in inverno" data-fr="Panorama alpin en hiver" data-es="Panorama alpino en invierno" data-zh="冬季阿尔卑斯全景">Alpenpanorama im Winter</h3>
                    <p style="color: #666; font-size: 14px; margin-bottom: 15px;" data-en="December 2023" data-de="Dezember 2023" data-it="Dicembre 2023" data-fr="Décembre 2023" data-es="Diciembre de 2023" data-zh="2023年12月">Dezember 2023</p>
                    <p style="color: #555; line-height: 1.7;" data-en="On clear winter days, the view from our webcam at 616m altitude extends to the snow-covered peaks of the Glarus Alps. Säntis, Glärnisch and other mountain peaks are visible." data-de="An klaren Wintertagen reicht die Sicht von unserer Webcam auf 616m Höhe bis zu den schneebedeckten Gipfeln der Glarner Alpen. Säntis, Glärnisch und weitere Bergspitzen sind sichtbar." data-it="Nelle limpide giornate invernali, la vista dalla nostra webcam a 616 m di altitudine si estende alle vette innevate delle Alpi di Glarona. Si vedono Säntis, Glärnisch e altre cime." data-fr="Par temps clair en hiver, la vue depuis notre webcam à 616 m d'altitude s'étend jusqu'aux sommets enneigés des Alpes glaronnaises. Le Säntis, le Glärnisch et d'autres sommets sont visibles." data-es="En los días despejados de invierno, la vista desde nuestra webcam a 616 m de altitud llega hasta las cumbres nevadas de los Alpes de Glaris. Se ven el Säntis, el Glärnisch y otras cimas." data-zh="在晴朗的冬日，从我们海拔616米的摄像头可远眺格拉鲁斯阿尔卑斯的雪峰，可见 Säntis、Glärnisch 等山峰。">
                        An klaren Wintertagen reicht die Sicht von unserer Webcam auf 616m Höhe bis zu den schneebedeckten Gipfeln der Glarner Alpen. Säntis, Glärnisch und weitere Bergspitzen sind sichtbar.
                    </p>
                    <p style="color: #555; line-height: 1.7; margin-top: 10px;" data-en="Use the zoom function for detailed views of the mountain landscape." data-de="Nutzen Sie die Zoom-Funktion für detaillierte Ansichten der Berglandschaft." data-it="Usa la funzione zoom per viste dettagliate del paesaggio montano." data-fr="Utilisez la fonction zoom pour des vues détaillées du paysage montagneux." data-es="Use la función de zoom para ver el paisaje montañoso en detalle." data-zh="使用缩放功能可查看更细致的山景。">
                        Nutzen Sie die Zoom-Funktion für detaillierte Ansichten der Berglandschaft.
                    </p>
                </div>
            </article>

            <!-- Blog Artikel 3 -->
            <article style="background: #fff; border-radius: 15px; overflow: hidden; box-shadow: 0 5px 20px rgba(0,0,0,0.1);">
                <div style="height: 180px; background: linear-gradient(135deg, #ee0979 0%, #ff6a00 100%); display: flex; align-items: center; justify-content: center;">
                    <span style="font-size: 60px;">✈️</span>
                </div>
                <div style="padding: 25px;">
                    <h3 style="margin-bottom: 10px; color: #333;" data-en="Patrouille Suisse Season 2024" data-de="Patrouille Suisse Saison 2024" data-it="Stagione 2024 della Patrouille Suisse" data-fr="Saison 2024 de la Patrouille Suisse" data-es="Temporada 2024 de la Patrouille Suisse" data-zh="2024年瑞士巡逻兵季">Patrouille Suisse Saison 2024</h3>
                    <p style="color: #666; font-size: 14px; margin-bottom: 15px;" data-en="March 2024" data-de="März 2024" data-it="Marzo 2024" data-fr="Mars 2024" data-es="Marzo de 2024" data-zh="2024年3月">März 2024</p>
                    <p style="color: #555; line-height: 1.7;" data-en="The new flight season of Patrouille Suisse has begun! Every Monday the aerobatic team trains over Zurich Oberland - our webcam captures the flight maneuvers live." data-de="Die neue Flugsaison der Patrouille Suisse hat begonnen! Jeden Montag trainiert das Kunstflugteam über dem Zürcher Oberland - unsere Webcam fängt die Flugmanöver live ein." data-it="È iniziata la nuova stagione di volo della Patrouille Suisse! Ogni lunedì il team acrobatico si addestra sopra l'Oberland di Zurigo: la nostra webcam cattura le manovre in diretta." data-fr="La nouvelle saison de vol de la Patrouille Suisse a commencé ! Chaque lundi, l'équipe de voltige s'entraîne au-dessus de l'Oberland zurichois : notre webcam capture les manœuvres en direct." data-es="¡Ha comenzado la nueva temporada de vuelo de la Patrouille Suisse! Cada lunes el equipo acrobático entrena sobre el Oberland de Zúrich y nuestra webcam capta las maniobras en directo." data-zh="瑞士巡逻兵新一季飞行已开始！每周一特技飞行队在苏黎世高地训练，我们的摄像头会实时捕捉飞行动作。">
                        Die neue Flugsaison der Patrouille Suisse hat begonnen! Jeden Montag trainiert das Kunstflugteam über dem Zürcher Oberland - unsere Webcam fängt die Flugmanöver live ein.
                    </p>
                    <p style="color: #555; line-height: 1.7; margin-top: 10px;" data-en="AI detection automatically marks aircraft sightings in our gallery." data-de="Die AI-Erkennung markiert Flugzeug-Sichtungen automatisch in unserer Galerie." data-it="Il rilevamento AI contrassegna automaticamente gli avvistamenti di aerei nella nostra galleria." data-fr="La détection IA marque automatiquement les observations d'avions dans notre galerie." data-es="La detección por IA marca automáticamente los avistamientos de aviones en nuestra galería." data-zh="AI 检测会在我们的图库中自动标记飞机出现。">
                        Die AI-Erkennung markiert Flugzeug-Sichtungen automatisch in unserer Galerie.
                    </p>
                </div>
            </article>
        </div>

        <div style="text-align: center; margin-top: 40px;">
            <p style="color: #888; font-size: 14px;" data-en="More weather updates and observations can be found on our social media channels." data-de="Weitere Wetter-Updates und Beobachtungen finden Sie auf unseren Social Media Kanälen." data-it="Altri aggiornamenti meteo e osservazioni sono disponibili sui nostri canali social." data-fr="D'autres mises à jour météo et observations sont disponibles sur nos réseaux sociaux." data-es="Encontrará más novedades del tiempo y observaciones en nuestras redes sociales." data-zh="更多天气更新和观测内容请关注我们的社交媒体渠道。">
                Weitere Wetter-Updates und Beobachtungen finden Sie auf unseren Social Media Kanälen.
            </p>
        </div>
    </div>
</section>

<!-- IMPRESSUM -->
<section id="impressum" class="section">
    <div class="container">
        <h2 data-en="Imprint" data-de="Impressum" data-it="Note legali" data-fr="Mentions légales" data-es="Aviso legal" data-zh="法律声明">Impressum</h2>
        <p><?php echo $siteConfig['footerName']; ?></p>
        <p>M. Kessler</p>
        <p>Dürnten, Schweiz</p>
        <p data-en="Inquiries via contact form" data-de="Anfragen per Kontaktformular" data-it="Richieste tramite modulo di contatto" data-fr="Demandes via le formulaire de contact" data-es="Consultas a través del formulario de contacto" data-zh="通过联系表单咨询">Anfragen per Kontaktformular</p>
    </div>
</section>

</main>

<footer>
    <div class="container">
        <!-- Social Media Icons -->
        <div class="footer-social social-media-container" style="text-align: center; margin-bottom: 20px; display: <?php echo $settingsManager->shouldShowSocialMedia() ? 'block' : 'none'; ?>;">
            <a href="<?php echo htmlspecialchars($settingsManager->getSocialLink('instagram', 'https://www.instagram.com/auroraweatherlivecam')); ?>" target="_blank" class="social-link social-link-instagram" rel="noopener noreferrer" title="Folge uns auf Instagram" style="display: inline-block; margin: 0 10px; color: #E1306C; font-size: 24px;">
                <i class="fab fa-instagram" aria-hidden="true"></i>
                <span class="sr-only">Instagram</span>
            </a>
            <a href="<?php echo htmlspecialchars($settingsManager->getSocialLink('facebook', 'https://www.facebook.com/auroraweatherlivecam')); ?>" target="_blank" class="social-link social-link-facebook" rel="noopener noreferrer" title="Folge uns auf Facebook" style="display: inline-block; margin: 0 10px; color: #1877F2; font-size: 24px;">
                <i class="fab fa-facebook" aria-hidden="true"></i>
                <span class="sr-only">Facebook</span>
            </a>
            <a href="<?php echo htmlspecialchars($settingsManager->getSocialLink('youtube', 'https://www.youtube.com/channel/UCkAIp4Vh1X6cvN2DPrxFBeA')); ?>" target="_blank" class="social-link social-link-youtube" rel="noopener noreferrer" title="Abonniere unseren YouTube Kanal" style="display: inline-block; margin: 0 10px; color: #FF0000; font-size: 24px;">
                <i class="fab fa-youtube" aria-hidden="true"></i>
                <span class="sr-only">YouTube</span>
            </a>
            <a href="<?php echo htmlspecialchars($settingsManager->getSocialLink('tiktok', 'https://www.tiktok.com/@auroraweatherlivecam')); ?>" target="_blank" class="social-link social-link-tiktok" rel="noopener noreferrer" title="Folge uns auf TikTok" style="display: inline-block; margin: 0 10px; color: #000000; font-size: 24px;">
                <i class="fab fa-tiktok" aria-hidden="true"></i>
                <span class="sr-only">TikTok</span>
            </a>
        </div>

<div style="text-align: center; margin-bottom: 20px;">
    <div class="donation-buttons">
<?php if ($showPaypalDonation): ?>
        <a href="<?php echo htmlspecialchars($paypalDonationUrl); ?>" target="_blank" rel="noopener" class="paypal-donation-link">
            <span class="paypal-logo" aria-hidden="true"><b>Pay</b><i>Pal</i></span>
            <span data-de="Jetzt spenden" data-en="Donate now" data-it="Dona ora" data-fr="Faire un don" data-es="Donar ahora" data-zh="立即捐赠">Jetzt spenden</span>
        </a>
<?php endif; ?>
<?php if ($showBuyMeACoffee): ?>
        <a href="<?php echo htmlspecialchars($settingsManager->getSocialLink('buymeacoffee', 'https://buymeacoffee.com/auroracam')); ?>" target="_blank" rel="noopener" class="donation-link">
            <img src="buy.png" alt="Buy me a coffee" style="height: 267px; border-radius: 8px; transition: transform 0.2s;" onmouseover="this.style.transform='scale(1.05)'" onmouseout="this.style.transform='scale(1)'">
        </a>
<?php endif; ?>
    </div>
    <p class="donation-callout-text" style="font-size: 14px; color: #333; background: rgba(255,255,255,0.85); padding: 8px 16px; border-radius: 8px; margin: 10px auto 0; max-width: 400px; line-height: 1.6;">
        <?php echo nl2br(htmlspecialchars($donationCalloutText)); ?>
    </p>
</div>



        <!-- Herkunft und Strom (2026-10-09) -->
        <div class="trust-badges" style="display:flex;flex-wrap:wrap;gap:10px;justify-content:center;margin:18px auto;"><span class="trust-badge" style="display:inline-flex;align-items:center;gap:8px;padding:5px 12px 5px 6px;border-radius:999px;font:600 13px/1.2 -apple-system,BlinkMacSystemFont,'Segoe UI',Helvetica,Arial,sans-serif;white-space:nowrap;background:#fff;border:1px solid #e3d6d4;color:#222;"><svg width="22" height="22" viewBox="0 0 32 32" aria-hidden="true" focusable="false"><rect width="32" height="32" rx="3" fill="#DA291C"/><path fill="#fff" d="M13 6h6v7h7v6h-7v7h-6v-7H6v-6h7z"/></svg><span class="trust-text" data-de="Aus der Schweiz" data-en="From Switzerland" data-it="Dalla Svizzera" data-fr="De Suisse" data-es="Desde Suiza" data-zh="来自瑞士">Aus der Schweiz</span></span><span class="trust-badge" style="display:inline-flex;align-items:center;gap:8px;padding:5px 12px 5px 6px;border-radius:999px;font:600 13px/1.2 -apple-system,BlinkMacSystemFont,'Segoe UI',Helvetica,Arial,sans-serif;white-space:nowrap;background:#fff;border:1px solid #e3d6d4;color:#222;"><svg width="22" height="22" viewBox="0 0 32 32" aria-hidden="true" focusable="false"><circle cx="16" cy="16" r="15" fill="#fff" stroke="#DA291C" stroke-width="2"/><circle cx="16" cy="16" r="11" fill="none" stroke="#DA291C" stroke-width="1" stroke-dasharray="2 2"/><path fill="#DA291C" d="M14.2 10h3.6v4.2H22v3.6h-4.2V22h-3.6v-4.2H10v-3.6h4.2z"/></svg>Swiss made</span><span class="trust-badge" style="display:inline-flex;align-items:center;gap:8px;padding:5px 12px 5px 6px;border-radius:999px;font:600 13px/1.2 -apple-system,BlinkMacSystemFont,'Segoe UI',Helvetica,Arial,sans-serif;white-space:nowrap;background:#E8F5E9;border:1px solid #A5D6A7;color:#1B5E20;"><svg width="22" height="22" viewBox="0 0 32 32" aria-hidden="true" focusable="false"><circle cx="16" cy="16" r="16" fill="#2E7D32"/><circle cx="22" cy="10" r="3.2" fill="#FFD54F"/><g stroke="#FFD54F" stroke-width="1.4" stroke-linecap="round"><path d="M22 4.6v1.6M22 13.8v1.6M16.6 10h1.6M25.8 10h1.6M18.2 6.2l1.1 1.1M24.7 12.7l1.1 1.1M25.8 6.2l-1.1 1.1"/></g><path fill="#fff" d="M8 24c0-7 5-12 13-12-1 8-5 13-12 13l-1-1zm1 0c3-4 6-6 9-7-3 2-6 4-8 8z"/></svg><span class="trust-text" data-de="CO₂-neutral · mit Solarstrom betrieben" data-en="CO₂-neutral · solar powered" data-it="CO₂ neutrale · alimentata a energia solare" data-fr="Neutre en CO₂ · alimentée à l&#x27;énergie solaire" data-es="Neutra en CO₂ · con energía solar" data-zh="碳中和 · 太阳能供电">CO₂-neutral · mit Solarstrom betrieben</span></span></div>
        <div class="footer-links">
            <a href="#webcams">Webcam</a>
            <a href="#guestbook">Gästebuch</a>
            <a href="#kontakt">Kontakt</a>
            <a href="#gallery">Galerie</a>
            <a href="#zeitreise">Zeitreise</a>
            <a href="#patrouille-suisse">Patrouille Suisse</a>
            <a href="#blog">Blog</a>
            <a href="#impressum">Impressum</a>
        </div>
        <div class="footer-bottom">
            <p><?php echo $siteConfig['copyright']; ?></p>
            <p style="font-size: 12px; color: #999; margin-top: 5px;">Live Webcam Schweiz | Zürichsee Blick | Patrouille Suisse Trainings</p>
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
<script src="js/video-zoom.js?v=20261007"></script>

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
    reverseEnabled: <?php echo $settingsManager->isTimelapseReverseEnabled() ? 'true' : 'false'; ?>,

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
                <button id="tl-reverse" class="tl-btn" title="Rückwärts" style="display: ${this.reverseEnabled ? 'inline-block' : 'none'};">
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
    hlsInstance: null,

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

        // Alte HLS-Instanz aufräumen
        if (this.hlsInstance) {
            this.hlsInstance.destroy();
            this.hlsInstance = null;
        }

        // HLS oder direkte MP4?
        const isHLS = videoPath.endsWith('.m3u8');

        if (isHLS && Hls.isSupported()) {
            // HLS-Streaming (adaptiv, wie YouTube)
            var hls = new Hls({
                enableWorker: true,
                lowLatencyMode: false,
                startLevel: 0,           // Start mit niedrigster Qualität (360p)
                capLevelToPlayerSize: true // Nicht höher als Player-Grösse
            });
            hls.loadSource(videoPath);
            hls.attachMedia(this.videoElement);
            hls.on(Hls.Events.MANIFEST_PARSED, () => {
                this.videoElement.play().catch(console.log);
            });
            hls.on(Hls.Events.ERROR, (event, data) => {
                if (data.fatal) {
                    console.error('HLS Fatal Error:', data.type);
                    // Fallback: versuche als direkte MP4
                    hls.destroy();
                    this.hlsInstance = null;
                    var mp4Path = videoPath.replace(/\/remote-hls\/(.+)\/master\.m3u8/, '/remote-videos/daily_video_$1.mp4');
                    this.videoElement.src = mp4Path;
                    this.videoElement.load();
                    this.videoElement.play().catch(console.log);
                }
            });
            this.hlsInstance = hls;
        } else if (isHLS && this.videoElement.canPlayType('application/vnd.apple.mpegurl')) {
            // Safari: native HLS
            this.videoElement.src = videoPath;
            this.videoElement.load();
            this.videoElement.play().catch(console.log);
        } else {
            // Direkte MP4 (lokale Videos oder Fallback)
            this.videoElement.src = videoPath;
            this.videoElement.load();
            this.videoElement.play().catch(console.log);
        }

        // Download Button (immer Original-MP4, nicht HLS)
        const downloadBtn = document.getElementById('dvp-download');
        if (allowDownload && downloadBtn) {
            downloadBtn.style.display = 'inline-block';
            if (isHLS) {
                // Download-Link auf Original-MP4 umleiten
                var origName = videoPath.match(/\/remote-hls\/(.+)\/master\.m3u8/);
                downloadBtn.href = origName ? '/remote-videos/daily_video_' + origName[1] + '.mp4' : videoPath;
                downloadBtn.download = origName ? 'daily_video_' + origName[1] + '.mp4' : '';
            } else {
                downloadBtn.href = videoPath;
                downloadBtn.download = videoPath.split('/').pop();
            }
        } else if (downloadBtn) {
            downloadBtn.style.display = 'none';
        }

        // Scroll zum Video
        document.getElementById('webcams').scrollIntoView({ behavior: 'smooth' });
    },

    backToLive: function() {
        // HLS aufräumen
        if (this.hlsInstance) {
            this.hlsInstance.destroy();
            this.hlsInstance = null;
        }
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

<!-- VIDEO SEARCH -->
<script>
const VideoSearch = {
    init: function() {
        const form = document.getElementById('video-search-form');
        if (form) {
            form.addEventListener('submit', (e) => {
                e.preventDefault();
                this.search();
            });
        }
    },

    search: function() {
        const date = document.getElementById('search-date').value;
        const time = document.getElementById('search-time').value;
        const type = document.getElementById('search-type').value;

        if (!date) {
            alert('Bitte wählen Sie ein Datum aus.');
            return;
        }

        const params = new URLSearchParams();
        params.append('date', date);
        if (time) params.append('time', time);
        params.append('type', type);

        const resultsDiv = document.getElementById('search-results');
        const contentDiv = document.getElementById('search-results-content');
        resultsDiv.style.display = 'block';
        contentDiv.innerHTML = '<div style="text-align:center;padding:20px;"><span style="font-size:2rem;">🔄</span><br>Suche läuft...</div>';

        fetch('/api/video-search.php?' + params.toString())
            .then(r => r.json())
            .then(data => {
                if (data.success) {
                    this.displayResults(data, contentDiv);
                } else {
                    contentDiv.innerHTML = '<div style="color:red;padding:20px;">Fehler bei der Suche.</div>';
                }
            })
            .catch(err => {
                console.error('Search error:', err);
                contentDiv.innerHTML = '<div style="color:red;padding:20px;">Netzwerkfehler bei der Suche.</div>';
            });
    },

    displayResults: function(data, container) {
        let html = '';

        // Statistiken
        html += '<div style="margin-bottom:15px;padding:10px;background:#f0f4ff;border-radius:8px;">';
        html += '<strong>Gefunden:</strong> ' + data.stats.total + ' Videos ';
        html += '(' + data.stats.total_daily + ' Tagesvideos, ' + data.stats.total_ai + ' AI-Ereignisse)';
        html += '</div>';

        if (data.stats.total === 0) {
            html += '<div style="text-align:center;padding:30px;color:#666;">';
            html += '<span style="font-size:3rem;">📭</span><br>';
            html += 'Keine Videos für dieses Datum/Uhrzeit gefunden.';
            html += '</div>';
        } else {
            // Tagesvideos
            if (data.daily_videos.length > 0) {
                html += '<div style="margin-bottom:20px;">';
                html += '<h5 style="margin:0 0 10px 0;">📹 Tagesvideos</h5>';
                html += '<div style="display:grid;gap:10px;">';
                data.daily_videos.forEach(video => {
                    html += this.renderVideoCard(video, 'daily');
                });
                html += '</div></div>';
            }

            // AI-Videos
            if (data.ai_videos.length > 0) {
                html += '<div>';
                html += '<h5 style="margin:0 0 10px 0;">🤖 AI-Ereignisse</h5>';
                html += '<div style="display:grid;gap:10px;">';
                data.ai_videos.forEach(video => {
                    html += this.renderVideoCard(video, 'ai');
                });
                html += '</div></div>';
            }
        }

        container.innerHTML = html;
    },

    renderVideoCard: function(video, type) {
        const categoryIcons = {
            sunny: '☀️', rainy: '🌧️', snowy: '❄️', planes: '✈️',
            birds: '🐦', sunset: '🌅', sunrise: '🌄', rainbow: '🌈'
        };
        const icon = type === 'ai' ? (categoryIcons[video.category] || '🎬') : '📹';

        return `
            <div style="background:white;border:1px solid #e0e0e0;border-radius:8px;padding:12px;display:flex;justify-content:space-between;align-items:center;flex-wrap:wrap;gap:10px;">
                <div>
                    <span style="font-size:1.2rem;">${icon}</span>
                    <strong>${video.date}</strong> um <strong>${video.time}</strong>
                    ${type === 'ai' ? '<span style="background:#e8f4ea;padding:2px 8px;border-radius:4px;margin-left:8px;font-size:0.85rem;">' + video.category + '</span>' : ''}
                    <span style="color:#888;font-size:0.85rem;margin-left:10px;">${video.size_mb} MB</span>
                </div>
                <div style="display:flex;gap:8px;">
                    <button onclick="DailyVideoPlayer.playVideo('${video.path}', true)" class="button" style="padding:6px 12px;font-size:0.85rem;">
                        ▶️ Abspielen
                    </button>
                    <a href="${video.path}" download class="button" style="padding:6px 12px;font-size:0.85rem;background:#4CAF50;text-decoration:none;">
                        ⬇️ Download
                    </a>
                    <?php if ($settingsManager->isEmailSharingEnabled()): ?>
                    <button onclick="ShareModal.open('${video.path}', 'video')" class="button" style="padding:6px 12px;font-size:0.85rem;background:#2196F3;">
                        📤 Teilen
                    </button>
                    <?php endif; ?>
                </div>
            </div>
        `;
    }
};

document.addEventListener('DOMContentLoaded', function() {
    VideoSearch.init();
});
</script>

<!-- SHARE MODAL -->
<?php if ($settingsManager->isEmailSharingEnabled()): ?>
<div id="share-modal" style="display:none;position:fixed;top:0;left:0;right:0;bottom:0;background:rgba(0,0,0,0.7);z-index:10000;align-items:center;justify-content:center;">
    <div style="background:white;border-radius:16px;padding:30px;max-width:450px;width:90%;box-shadow:0 20px 60px rgba(0,0,0,0.3);">
        <div style="display:flex;justify-content:space-between;align-items:center;margin-bottom:20px;">
            <h3 style="margin:0;color:#667eea;">📤 Per E-Mail teilen</h3>
            <button onclick="ShareModal.close()" style="background:none;border:none;font-size:1.5rem;cursor:pointer;color:#888;">×</button>
        </div>
        <form id="share-form">
            <input type="hidden" id="share-path" name="path">
            <input type="hidden" id="share-type" name="type">
            <div style="margin-bottom:15px;">
                <label style="display:block;font-size:0.85rem;color:#666;margin-bottom:5px;">Dein Name</label>
                <input type="text" id="share-sender" name="sender_name" placeholder="Dein Name" style="width:100%;padding:12px;border:1px solid #ddd;border-radius:8px;font-size:1rem;">
            </div>
            <div style="margin-bottom:15px;">
                <label style="display:block;font-size:0.85rem;color:#666;margin-bottom:5px;">E-Mail-Adresse des Empfängers *</label>
                <input type="email" id="share-email" name="email" placeholder="freund@beispiel.ch" required style="width:100%;padding:12px;border:1px solid #ddd;border-radius:8px;font-size:1rem;">
            </div>
            <div style="margin-bottom:20px;">
                <label style="display:block;font-size:0.85rem;color:#666;margin-bottom:5px;">Nachricht (optional)</label>
                <textarea id="share-message" name="message" placeholder="Schau dir das an!" rows="3" style="width:100%;padding:12px;border:1px solid #ddd;border-radius:8px;font-size:1rem;resize:vertical;"></textarea>
            </div>
            <div style="display:flex;gap:10px;">
                <button type="button" onclick="ShareModal.close()" class="button" style="flex:1;background:#e0e0e0;color:#333;">Abbrechen</button>
                <button type="submit" class="button" style="flex:1;background:linear-gradient(135deg, #667eea 0%, #764ba2 100%);">📧 Senden</button>
            </div>
        </form>
        <div id="share-result" style="margin-top:15px;display:none;"></div>
    </div>
</div>
<script>
const ShareModal = {
    open: function(path, type) {
        document.getElementById('share-path').value = path;
        document.getElementById('share-type').value = type;
        document.getElementById('share-email').value = '';
        document.getElementById('share-message').value = '';
        document.getElementById('share-result').style.display = 'none';
        document.getElementById('share-modal').style.display = 'flex';
    },

    close: function() {
        document.getElementById('share-modal').style.display = 'none';
    },

    send: function() {
        const form = document.getElementById('share-form');
        const path = document.getElementById('share-path').value;
        const type = document.getElementById('share-type').value;
        const email = document.getElementById('share-email').value;
        const message = document.getElementById('share-message').value;
        const senderName = document.getElementById('share-sender').value || 'Ein Freund';
        const resultDiv = document.getElementById('share-result');

        if (!email) {
            resultDiv.innerHTML = '<div style="color:#f44336;padding:10px;background:#ffebee;border-radius:8px;">Bitte E-Mail-Adresse eingeben.</div>';
            resultDiv.style.display = 'block';
            return;
        }

        resultDiv.innerHTML = '<div style="color:#666;padding:10px;text-align:center;">🔄 Wird gesendet...</div>';
        resultDiv.style.display = 'block';

        fetch('/api/share.php', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ path, type, email, message, sender_name: senderName })
        })
        .then(r => r.json())
        .then(data => {
            if (data.success) {
                resultDiv.innerHTML = '<div style="color:#4CAF50;padding:10px;background:#e8f5e9;border-radius:8px;">✅ E-Mail wurde gesendet!</div>';
                setTimeout(() => ShareModal.close(), 2000);
            } else {
                let msg = data.error || 'Fehler beim Senden';
                if (data.share_url) {
                    msg += '<br><br>Link zum manuellen Teilen:<br><input type="text" value="' + data.share_url + '" style="width:100%;padding:5px;margin-top:5px;" onclick="this.select()" readonly>';
                }
                resultDiv.innerHTML = '<div style="color:#f44336;padding:10px;background:#ffebee;border-radius:8px;">' + msg + '</div>';
            }
        })
        .catch(err => {
            console.error('Share error:', err);
            resultDiv.innerHTML = '<div style="color:#f44336;padding:10px;background:#ffebee;border-radius:8px;">Netzwerkfehler beim Senden.</div>';
        });
    }
};

document.getElementById('share-form')?.addEventListener('submit', function(e) {
    e.preventDefault();
    ShareModal.send();
});

// Modal schliessen bei Klick ausserhalb
document.getElementById('share-modal')?.addEventListener('click', function(e) {
    if (e.target === this) ShareModal.close();
});
</script>
<?php endif; ?>

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
        const boolValue = (value === true || value === 'true');

        switch(key) {
            case 'viewer_display.enabled':
                const viewerEl = document.getElementById('viewer-stat-container');
                if (viewerEl) {
                    viewerEl.style.display = boolValue ? 'inline-flex' : 'none';
                }
                break;
            case 'viewer_display.min_viewers':
                window.minViewersToShow = parseInt(value);
                break;

            // UI Display (Punkt 2)
            case 'ui_display.show_recommendation_banner':
                const bannerEl = document.querySelector('.banner-container');
                if (bannerEl) bannerEl.style.display = boolValue ? 'block' : 'none';
                break;
            case 'ui_display.show_qr_code':
                const qrSection = document.getElementById('qr-code');
                if (qrSection) qrSection.style.display = boolValue ? 'block' : 'none';
                break;
            case 'ui_display.show_social_media':
                const socialEls = document.querySelectorAll('.social-link, .social-media-container');
                socialEls.forEach(el => el.style.display = boolValue ? '' : 'none');
                break;
            case 'social_links.instagram':
            case 'social_links.facebook':
            case 'social_links.youtube':
            case 'social_links.tiktok':
                const socialKey = key.split('.').pop();
                document.querySelectorAll('.social-link-' + socialKey).forEach(el => el.href = value || '#');
                this.showNotification('Social-Link gespeichert', 'success');
                break;
            case 'social_links.buymeacoffee':
                document.querySelectorAll('.donation-link').forEach(el => el.href = value || '#');
                this.showNotification('Spenden-Link gespeichert', 'success');
                break;
            case 'ui_display.show_patrouille_suisse':
                const patrouilleSection = document.getElementById('patrouille-suisse');
                if (patrouilleSection) patrouilleSection.style.display = boolValue ? 'block' : 'none';
                break;

            // Zoom & Timelapse (Punkt 3)
            case 'zoom_timelapse.show_zoom_controls':
                const zoomControls = document.getElementById('zoom-controls');
                if (zoomControls) zoomControls.style.display = boolValue ? 'flex' : 'none';
                break;
            case 'zoom_timelapse.max_zoom_level':
                window.maxZoomLevel = parseFloat(value);
                this.showNotification('Max Zoom: ' + value + 'x (Reload empfohlen)', 'success');
                break;
            case 'zoom_timelapse.timelapse_reverse_enabled':
                const reverseBtn = document.getElementById('tl-reverse');
                if (reverseBtn) reverseBtn.style.display = boolValue ? 'inline-block' : 'none';
                break;

            // Content Management (Punkt 5)
            case 'content.guestbook_enabled':
                const guestbookSection = document.getElementById('guestbook');
                if (guestbookSection) guestbookSection.style.display = boolValue ? 'block' : 'none';
                break;
            case 'content.gallery_enabled':
                const gallerySection = document.getElementById('gallery');
                if (gallerySection) gallerySection.style.display = boolValue ? 'block' : 'none';
                break;
            case 'content.ai_detection_enabled':
                const aiLegend = document.querySelector('.ai-legend');
                const aiSectionsByDetection = document.querySelectorAll('.ai-events-section');
                if (aiLegend) aiLegend.style.display = boolValue ? 'block' : 'none';
                aiSectionsByDetection.forEach(section => section.style.display = boolValue ? 'block' : 'none');
                this.showNotification('KI-Erkennung ' + (boolValue ? 'aktiviert' : 'deaktiviert') + ' (Reload empfohlen)', 'success');
                break;
            case 'content.ai_events_enabled':
                const aiSections = document.querySelectorAll('.ai-events-section');
                aiSections.forEach(section => section.style.display = boolValue ? 'block' : 'none');
                this.showNotification('KI-Anzeige ' + (boolValue ? 'aktiviert' : 'deaktiviert') + ' (Reload empfohlen)', 'success');
                break;
            case 'content.max_guestbook_entries':
                this.showNotification('Max Einträge: ' + value + ' (Reload empfohlen)', 'success');
                break;
            case 'content.hero_welcome_de':
                const heroWelcome = document.getElementById('hero-welcome-text');
                if (heroWelcome) {
                    const nextValue = value && String(value).trim() !== '' ? value : '<?php echo addslashes($siteConfig['welcomeDe']); ?>';
                    heroWelcome.textContent = nextValue;
                    heroWelcome.setAttribute('data-de', nextValue);
                }
                break;
            case 'content.hero_region_text_de':
                const heroRegionText = document.getElementById('hero-region-text');
                if (heroRegionText) {
                    const fallbackRegion = 'Erleben Sie faszinierende Ausblicke der Züricher Region - in Echtzeit!';
                    const nextValue = value && String(value).trim() !== '' ? value : fallbackRegion;
                    heroRegionText.textContent = nextValue;
                    heroRegionText.setAttribute('data-de', nextValue);
                }
                break;
            case 'content.local_flag_alt_de':
                const localFlagImage = document.getElementById('local-flag-image');
                if (localFlagImage) {
                    const nextValue = value && String(value).trim() !== '' ? value : 'Ortsflagge';
                    localFlagImage.setAttribute('alt', nextValue);
                }
                break;
            case 'content.donation_callout_de':
                const donationEls = document.querySelectorAll('.donation-callout-text');
                const donationFallback = 'Gefällt dir die Livecam? Unterstütze das Projekt –\njeder Franken zählt und hilft die Betriebskosten\nzu decken. So kannst du auch weiterhin\nFotos und Videos kostenlos nutzen.';
                const donationText = value && String(value).trim() !== '' ? value : donationFallback;
                donationEls.forEach(el => {
                    el.innerHTML = donationText.replace(/\n/g, '<br>');
                });
                break;
            case 'content.twint_support_text_de':
                const twintTextEl = document.querySelector('.twint-support-text');
                if (twintTextEl) {
                    const fallbackTwint = 'Unterstütze die Aurora Livecam mit deiner Spende. Du hilfst dabei, Kamera, Server, Internetverbindung und die 100 % autarke Stromversorgung mit Solarenergie und EcoFlow-Speichern zu finanzieren. So bleibt das Projekt nachhaltig, unabhängig und für alle frei zugänglich.';
                    const nextValue = value && String(value).trim() !== '' ? value : fallbackTwint;
                    twintTextEl.textContent = nextValue;
                    twintTextEl.setAttribute('data-de', nextValue);
                }
                break;
            case 'social_links.paypal':
                document.querySelectorAll('.paypal-donation-link').forEach(el => el.href = value || '#');
                this.showNotification('PayPal-Link gespeichert', 'success');
                break;
            case 'content.paypal_enabled':
            case 'content.buymeacoffee_enabled': {
                const spendenKlasse = key === 'content.paypal_enabled' ? '.paypal-donation-link' : '.donation-link';
                const spendenEls = document.querySelectorAll(spendenKlasse);
                spendenEls.forEach(el => el.style.display = value ? '' : 'none');
                const spendenName = key === 'content.paypal_enabled' ? 'PayPal-Spendenknopf' : 'Buy-me-a-Coffee-Knopf';
                this.showNotification(spendenName + (value ? ' aktiviert' : ' deaktiviert') + (value && !spendenEls.length ? ' (Reload nötig)' : ''), 'success');
                break;
            }
            case 'content.twint_enabled':
                const twintPaymentEl = document.querySelector('.twint-payment-content');
                if (twintPaymentEl) {
                    twintPaymentEl.style.display = value ? '' : 'none';
                }
                this.showNotification(value ? 'TWINT-Spendenlink aktiviert (Reload empfohlen)' : 'TWINT-Spendenlink deaktiviert', 'success');
                break;

            // Technical (Punkt 6)
            case 'technical.viewer_update_interval':
                this.showNotification('Update-Intervall: ' + value + 's (Reload empfohlen)', 'success');
                break;
            case 'technical.session_timeout':
                this.showNotification('Session Timeout: ' + value + 's (Reload empfohlen)', 'success');
                break;

            // Theme (Punkt 7)
            case 'theme.default_theme':
                document.body.className = value;
                this.showNotification('Theme geändert: ' + value, 'success');
                break;
            case 'theme.show_theme_switcher':
                const themeSwitcher = document.querySelector('.theme-switcher');
                if (themeSwitcher) themeSwitcher.style.display = boolValue ? 'flex' : 'none';
                break;

            // SEO (Punkt 8)
            case 'seo.custom_title':
                document.title = value || document.title;
                this.showNotification('Title aktualisiert (Meta bei Reload)', 'success');
                break;
            case 'seo.meta_description':
            case 'seo.meta_keywords':
                this.showNotification('SEO Meta gespeichert (wirksam bei Reload)', 'success');
                break;

            // Weather Settings
            case 'weather.enabled':
                const weatherWidget = document.getElementById('weather-widget');
                if (weatherWidget) {
                    weatherWidget.style.display = boolValue ? 'flex' : 'none';
                }
                this.showNotification('Wetter-Widget ' + (boolValue ? 'aktiviert' : 'deaktiviert'), 'success');
                break;
           
            case 'weather.location':
            case 'weather.lat':
            case 'weather.lon':
            case 'weather.units':
                this.showNotification('Wetter-Einstellung gespeichert (Reload empfohlen)', 'success');
                break;
            case 'weather.update_interval':
                this.showNotification('Update-Intervall: ' + value + ' Minuten (Reload empfohlen)', 'success');
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

        // UI Display Settings (Punkt 2)
        document.getElementById('setting-show-banner')?.addEventListener('change', (e) => {
            this.updateSetting('ui_display.show_recommendation_banner', e.target.checked);
        });

        document.getElementById('setting-show-qr')?.addEventListener('change', (e) => {
            this.updateSetting('ui_display.show_qr_code', e.target.checked);
        });

        document.getElementById('setting-show-social')?.addEventListener('change', (e) => {
            this.updateSetting('ui_display.show_social_media', e.target.checked);
        });

        document.getElementById('setting-social-instagram')?.addEventListener('change', (e) => {
            this.updateSetting('social_links.instagram', e.target.value);
        });

        document.getElementById('setting-social-facebook')?.addEventListener('change', (e) => {
            this.updateSetting('social_links.facebook', e.target.value);
        });

        document.getElementById('setting-social-youtube')?.addEventListener('change', (e) => {
            this.updateSetting('social_links.youtube', e.target.value);
        });

        document.getElementById('setting-social-tiktok')?.addEventListener('change', (e) => {
            this.updateSetting('social_links.tiktok', e.target.value);
        });

        document.getElementById('setting-social-buymeacoffee')?.addEventListener('change', (e) => {
            this.updateSetting('social_links.buymeacoffee', e.target.value);
        });

        document.getElementById('setting-show-patrouille')?.addEventListener('change', (e) => {
            this.updateSetting('ui_display.show_patrouille_suisse', e.target.checked);
        });

        // Zoom & Timelapse Settings (Punkt 3)
        document.getElementById('setting-show-zoom')?.addEventListener('change', (e) => {
            this.updateSetting('zoom_timelapse.show_zoom_controls', e.target.checked);
        });

        document.getElementById('setting-max-zoom')?.addEventListener('change', (e) => {
            this.updateSetting('zoom_timelapse.max_zoom_level', parseFloat(e.target.value));
        });

        document.getElementById('setting-timelapse-reverse')?.addEventListener('change', (e) => {
            this.updateSetting('zoom_timelapse.timelapse_reverse_enabled', e.target.checked);
        });

        // Content Management Settings (Punkt 5)
        document.getElementById('setting-guestbook-enabled')?.addEventListener('change', (e) => {
            this.updateSetting('content.guestbook_enabled', e.target.checked);
        });

        document.getElementById('setting-gallery-enabled')?.addEventListener('change', (e) => {
            this.updateSetting('content.gallery_enabled', e.target.checked);
        });

        document.getElementById('setting-ai-detection-enabled')?.addEventListener('change', (e) => {
            this.updateSetting('content.ai_detection_enabled', e.target.checked);
        });

        document.getElementById('setting-ai-events-enabled')?.addEventListener('change', (e) => {
            this.updateSetting('content.ai_events_enabled', e.target.checked);
        });

        document.getElementById('setting-max-guestbook')?.addEventListener('change', (e) => {
            this.updateSetting('content.max_guestbook_entries', parseInt(e.target.value));
        });

        document.getElementById('setting-hero-welcome')?.addEventListener('change', (e) => {
            this.updateSetting('content.hero_welcome_de', e.target.value);
        });

        document.getElementById('setting-hero-region-text')?.addEventListener('change', (e) => {
            this.updateSetting('content.hero_region_text_de', e.target.value);
        });

        document.getElementById('setting-local-flag-alt')?.addEventListener('change', (e) => {
            this.updateSetting('content.local_flag_alt_de', e.target.value);
        });

        document.getElementById('setting-donation-callout')?.addEventListener('change', (e) => {
            this.updateSetting('content.donation_callout_de', e.target.value);
        });

        document.getElementById('setting-social-paypal')?.addEventListener('change', (e) => {
            this.updateSetting('social_links.paypal', e.target.value);
        });

        document.getElementById('setting-paypal-enabled')?.addEventListener('change', (e) => {
            this.updateSetting('content.paypal_enabled', e.target.checked);
        });

        document.getElementById('setting-buymeacoffee-enabled')?.addEventListener('change', (e) => {
            this.updateSetting('content.buymeacoffee_enabled', e.target.checked);
        });

        document.getElementById('setting-twint-enabled')?.addEventListener('change', (e) => {
            this.updateSetting('content.twint_enabled', e.target.checked);
        });

        document.getElementById('setting-twint-support-text')?.addEventListener('change', (e) => {
            this.updateSetting('content.twint_support_text_de', e.target.value);
        });

        // Technical Settings (Punkt 6)
        document.getElementById('setting-viewer-interval')?.addEventListener('change', (e) => {
            this.updateSetting('technical.viewer_update_interval', parseInt(e.target.value));
        });

        document.getElementById('setting-session-timeout')?.addEventListener('change', (e) => {
            this.updateSetting('technical.session_timeout', parseInt(e.target.value));
        });

        // Theme Settings (Punkt 7)
        document.getElementById('setting-default-theme')?.addEventListener('change', (e) => {
            this.updateSetting('theme.default_theme', e.target.value);
        });

        document.getElementById('setting-show-theme-switcher')?.addEventListener('change', (e) => {
            this.updateSetting('theme.show_theme_switcher', e.target.checked);
        });

        // SEO Settings (Punkt 8)
        document.getElementById('setting-custom-title')?.addEventListener('change', (e) => {
            this.updateSetting('seo.custom_title', e.target.value);
        });

        document.getElementById('setting-meta-description')?.addEventListener('change', (e) => {
            this.updateSetting('seo.meta_description', e.target.value);
        });

        document.getElementById('setting-meta-keywords')?.addEventListener('change', (e) => {
            this.updateSetting('seo.meta_keywords', e.target.value);
        });

        // Weather Settings
        document.getElementById('setting-weather-enabled')?.addEventListener('change', (e) => {
            this.updateSetting('weather.enabled', e.target.checked);
        });

    
        document.getElementById('setting-weather-location')?.addEventListener('change', (e) => {
            this.updateSetting('weather.location', e.target.value);
        });

        document.getElementById('setting-weather-lat')?.addEventListener('change', (e) => {
            this.updateSetting('weather.lat', e.target.value);
        });

        document.getElementById('setting-weather-lon')?.addEventListener('change', (e) => {
            this.updateSetting('weather.lon', e.target.value);
        });

        document.getElementById('setting-weather-interval')?.addEventListener('change', (e) => {
            this.updateSetting('weather.update_interval', parseInt(e.target.value));
        });

        document.getElementById('setting-weather-units')?.addEventListener('change', (e) => {
            this.updateSetting('weather.units', e.target.value);
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
    document.querySelectorAll('[data-en], [data-de], [data-it], [data-fr], [data-es], [data-zh]').forEach(elem => {
        const text = elem.getAttribute(`data-${lang}`);
        if (text) elem.textContent = text;
    });
    document.documentElement.lang = lang === 'zh' ? 'zh-CN' : lang;
    try { localStorage.setItem('auroraSprache', lang); } catch (e) {}
}
// Gewaehlte Sprache beim naechsten Besuch wiederherstellen (2026-10-07).
document.addEventListener('DOMContentLoaded', function() {
    let gemerkt = null;
    try { gemerkt = localStorage.getItem('auroraSprache'); } catch (e) {}
    const knopf = gemerkt && gemerkt !== 'de' && document.getElementById('lang-' + gemerkt);
    if (knopf) knopf.click();
});
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

<!-- WEATHER AUTO-UPDATE -->
<script>
const WeatherUpdater = {
    updateInterval: <?php echo $settingsManager->getWeatherUpdateInterval() * 60 * 1000; ?>, // Minuten -> Millisekunden

    init: function() {
        if (!document.getElementById('weather-widget')) return;

        // Update alle X Minuten
        setInterval(() => this.updateWeather(), this.updateInterval);
    },

    updateWeather: function() {
        fetch(window.location.href + '?weather_action=get')
            .then(r => r.json())
            .then(data => {
                if (data.success && data.data && !data.data.error) {
                    this.renderWeather(data.data);
                }
            })
            .catch(err => console.error('Weather update error:', err));
    },

    renderWeather: function(weather) {
        const widget = document.getElementById('weather-widget');
        if (!widget) return;

        const rainSnow = weather.rain_1h > 0 || weather.snow_1h > 0;
        const precipIcon = weather.rain_1h > 0 ? '🌧️' : '❄️';
        const precipValue = weather.rain_1h > 0 ? weather.rain_1h : weather.snow_1h;

        widget.innerHTML = `
            <div class="weather-item weather-temp">
                <span class="weather-icon">🌡️</span>
                <span class="weather-value">${weather.temp}°C</span>
                <span class="weather-label" data-de="Temperatur" data-en="Temperature" data-it="Temperatura" data-fr="Température" data-es="Temperatura" data-zh="温度">Temperatur</span>
            </div>
            <div class="weather-item weather-wind">
                <span class="weather-icon">💨</span>
                <span class="weather-value">${weather.wind_speed} km/h ${weather.wind_direction}</span>
                <span class="weather-label" data-de="Wind" data-en="Wind" data-it="Vento" data-fr="Vent" data-es="Viento" data-zh="风">Wind</span>
            </div>
            <div class="weather-item weather-pressure">
                <span class="weather-icon">🔽</span>
                <span class="weather-value">${weather.pressure} hPa</span>
                <span class="weather-label" data-de="Luftdruck" data-en="Pressure" data-it="Pressione" data-fr="Pression" data-es="Presión" data-zh="气压">Luftdruck</span>
            </div>
            <div class="weather-item weather-humidity">
                <span class="weather-icon">💧</span>
                <span class="weather-value">${weather.humidity}%</span>
                <span class="weather-label" data-de="Luftfeuchtigkeit" data-en="Humidity" data-it="Umidità" data-fr="Humidité" data-es="Humedad" data-zh="湿度">Luftfeuchtigkeit</span>
            </div>
            <div class="weather-item weather-description">
                <span class="weather-icon">${this.getWeatherEmoji(weather.icon)}</span>
                <span class="weather-value">${weather.description}</span>
                <span class="weather-label">${weather.location}</span>
            </div>
            ${rainSnow ? `
            <div class="weather-item weather-precipitation">
                <span class="weather-icon">${precipIcon}</span>
                <span class="weather-value">${precipValue} mm</span>
                <span class="weather-label" data-de="Niederschlag" data-en="Precipitation" data-it="Precipitazioni" data-fr="Précipitations" data-es="Precipitación" data-zh="降水">Niederschlag</span>
            </div>
            ` : ''}
        `;
        { const aktiv = document.querySelector('.lang-button.active'); if (aktiv && typeof setLanguage === 'function') setLanguage(aktiv.id.split('-')[1]); }

        // Fade-in Animation
        widget.style.animation = 'weatherFadeIn 0.5s ease';
    },

    getWeatherEmoji: function(iconCode) {
        const map = {
            '01d': '☀️', '01n': '🌙',
            '02d': '⛅', '02n': '☁️',
            '03d': '☁️', '03n': '☁️',
            '04d': '☁️', '04n': '☁️',
            '09d': '🌧️', '09n': '🌧️',
            '10d': '🌦️', '10n': '🌧️',
            '11d': '⛈️', '11n': '⛈️',
            '13d': '❄️', '13n': '❄️',
            '50d': '🌫️', '50n': '🌫️'
        };
        return map[iconCode] || '🌤️';
    }
};

document.addEventListener('DOMContentLoaded', function() {
    WeatherUpdater.init();
});
</script>



















<script>
function copyEmbedUrl() {
    const input = document.getElementById('embed-url');
    input.select();
    input.setSelectionRange(0, 99999);
    navigator.clipboard.writeText(input.value).then(function() {
        const feedback = document.getElementById('copy-feedback');
        feedback.style.display = 'block';
        setTimeout(() => { feedback.style.display = 'none'; }, 3000);
    });
}
</script>

 




</body>
</html>
