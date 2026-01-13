<?php
use PHPMailer\PHPMailer\PHPMailer;
use PHPMailer\PHPMailer\Exception;

require __DIR__ . '/vendor/autoload.php';
require_once 'SettingsManager.php';

// SettingsManager initialisieren
$settingsManager = new SettingsManager();

// AJAX-Handler für Settings (MUSS ganz am Anfang sein!)
if ($_SERVER['REQUEST_METHOD'] === 'POST' && isset($_POST['settings_action'])) {
    header('Content-Type: application/json');

    switch ($_POST['settings_action']) {
        case 'get':
            echo json_encode(['success' => true, 'settings' => $settingsManager->get()]);
            exit;

        case 'update':
            $key = $_POST['key'] ?? null;
            $value = $_POST['value'] ?? null;

            if ($value === 'true') $value = true;
            if ($value === 'false') $value = false;
            if (is_numeric($value)) $value = intval($value);

            if ($key && $settingsManager->set($key, $value)) {
                echo json_encode(['success' => true, 'message' => 'Gespeichert']);
            } else {
                echo json_encode(['success' => false, 'message' => 'Fehler']);
            }
            exit;
    }
}

if (isset($_GET['download_video'])) {
    $videoDir = './videos/';
    $latestVideo = null;
    $latestTime = 0;
    foreach (glob($videoDir . '*.mp4') as $video) {
        $mtime = filemtime($video);
        if ($mtime > $latestTime) { $latestTime = $mtime; $latestVideo = $video; }
    }
    if ($latestVideo) {
        header('Content-Type: application/octet-stream');
        header('Content-Disposition: attachment; filename="'.basename($latestVideo).'"');
        header('Content-Length: ' . filesize($latestVideo));
        readfile($latestVideo);
        exit;
    }
    echo "Kein Video gefunden.";
    exit;
}

$oldDomains = ['www.aurora-wetter-lifecam.ch', 'www.aurora-wetter-livecam.ch'];
$newDomain = 'www.aurora-weather-livecam.com';
if (in_array($_SERVER['HTTP_HOST'] ?? '', $oldDomains)) {
    $protocol = isset($_SERVER['HTTPS']) && $_SERVER['HTTPS'] === 'on' ? 'https' : 'http';
    header("HTTP/1.1 301 Moved Permanently");
    header("Location: " . $protocol . '://' . $newDomain . $_SERVER['REQUEST_URI']);
    exit;
}

session_start();
error_reporting(E_ALL);
ini_set('display_errors', 0);

$imageDir = "./image";
$imageFiles = glob("$imageDir/screenshot_*.jpg");
if ($imageFiles) rsort($imageFiles);
$imageFilesJson = json_encode($imageFiles ?: []);

class ViewerCounter {
    private $file = 'active_viewers.json';
    private $timeout = 30;

    public function handleHeartbeat() {
        $ip = md5($_SERVER['REMOTE_ADDR'] . ($_SERVER['HTTP_USER_AGENT'] ?? ''));
        $now = time();
        $viewers = file_exists($this->file) ? json_decode(file_get_contents($this->file), true) ?? [] : [];
        $viewers[$ip] = $now;
        $active = [];
        foreach ($viewers as $u => $t) { if ($now - $t < $this->timeout) $active[$u] = $t; }
        file_put_contents($this->file, json_encode($active));
        header('Content-Type: application/json');
        echo json_encode(['count' => count($active)]);
        exit;
    }

    public function getInitialCount() {
        if (file_exists($this->file)) {
            return max(1, count(json_decode(file_get_contents($this->file), true) ?? []));
        }
        return 1;
    }
}

$viewerCounter = new ViewerCounter();

class WebcamManager {
    private $videoSrc = 'test_video.m3u8';

    public function displayWebcam() {
        return '<video id="webcam-player" autoplay muted playsinline></video>';
    }

    public function displayStreamStats() {
        return '<div class="info-badge tech-stat" id="bitrate-display" style="display:none;">
            <i class="fas fa-tachometer-alt"></i> <span id="bitrate-value">0.00</span> MBit/s
        </div>';
    }

    public function getImageFiles() {
        $f = glob("image/screenshot_*.jpg");
        if ($f) rsort($f);
        return json_encode($f ?: []);
    }

    public function getJavaScript() {
        return "
        document.addEventListener('DOMContentLoaded', function () {
            var video = document.getElementById('webcam-player');
            var videoSrc = '{$this->videoSrc}';
            if(video && typeof Hls !== 'undefined' && Hls.isSupported()) {
                var hls = new Hls();
                hls.loadSource(videoSrc);
                hls.attachMedia(video);
                hls.on(Hls.Events.MANIFEST_PARSED, function () { video.play().catch(()=>{}); });
            } else if (video) {
                video.src = videoSrc;
                video.play().catch(()=>{});
            }
        });";
    }
}

class VisualCalendarManager {
    private $videoDir, $settingsManager;
    private $months = [1=>'Jan',2=>'Feb',3=>'Mär',4=>'Apr',5=>'Mai',6=>'Jun',7=>'Jul',8=>'Aug',9=>'Sep',10=>'Okt',11=>'Nov',12=>'Dez'];

    public function __construct($videoDir = './videos/', $sm = null) {
        $this->videoDir = $videoDir;
        $this->settingsManager = $sm;
    }

    public function hasVideosForDate($y, $m, $d) {
        return count(glob($this->videoDir . sprintf("daily_video_%04d%02d%02d_*.mp4", $y, $m, $d))) > 0;
    }

    public function getVideosForDate($y, $m, $d) {
        $vids = [];
        foreach (glob($this->videoDir . sprintf("daily_video_%04d%02d%02d_*.mp4", $y, $m, $d)) as $v) {
            $vids[] = ['path' => $v, 'name' => basename($v), 'size' => filesize($v), 'time' => date('H:i', filemtime($v))];
        }
        return $vids;
    }

    public function displayVisualCalendar() {
        $cy = isset($_GET['cal_year']) ? intval($_GET['cal_year']) : date('Y');
        $cm = isset($_GET['cal_month']) ? intval($_GET['cal_month']) : date('n');
        $sd = isset($_GET['cal_day']) ? intval($_GET['cal_day']) : null;
        $pip = $this->settingsManager ? $this->settingsManager->get('video_mode.play_in_player') : true;
        $dl = $this->settingsManager ? $this->settingsManager->get('video_mode.allow_download') : true;

        $o = '<div class="calendar-box">';
        $o .= '<div class="cal-nav"><button onclick="chgM('.$cy.','.($cm-1).')">&laquo;</button><span>'.$this->months[$cm].' '.$cy.'</span><button onclick="chgM('.$cy.','.($cm+1).')">&raquo;</button></div>';
        $o .= '<div class="cal-grid">';
        foreach(['Mo','Di','Mi','Do','Fr','Sa','So'] as $wd) $o .= '<div class="cal-hd">'.$wd.'</div>';

        $fd = mktime(0,0,0,$cm,1,$cy);
        $dim = date('t', $fd);
        $dow = date('N', $fd) - 1;
        for ($i=0; $i<$dow; $i++) $o .= '<div class="cal-day empty"></div>';

        for ($d=1; $d<=$dim; $d++) {
            $hv = $this->hasVideosForDate($cy,$cm,$d);
            $sel = $sd==$d;
            $td = ($cy==date('Y') && $cm==date('n') && $d==date('j'));
            $cls = 'cal-day' . ($hv?' has-vid':'') . ($sel?' sel':'') . ($td?' today':'');
            $o .= '<div class="'.$cls.'" onclick="selD('.$cy.','.$cm.','.$d.')"><span>'.$d.'</span>'.($hv?'<small>📹</small>':'').'</div>';
        }
        $o .= '</div>';

        if ($sd) {
            $vids = $this->getVideosForDate($cy,$cm,$sd);
            $o .= '<div class="day-vids"><h4>📅 '.sprintf('%02d.%02d.%04d',$sd,$cm,$cy).'</h4>';
            if ($vids) {
                $o .= '<ul>';
                foreach ($vids as $v) {
                    $sz = round($v['size']/1024/1024,1);
                    $tk = hash_hmac('sha256', $v['path'], session_id());
                    $o .= '<li><span>🕐 '.$v['time'].'</span><span>'.$sz.' MB</span><span class="vid-btns">';
                    if ($pip) $o .= '<a href="#" onclick="playVid(\''.htmlspecialchars($v['path']).'\');return false;" class="btn-play">▶️</a>';
                    if ($dl) $o .= '<a href="?download_specific_video='.urlencode($v['path']).'&token='.$tk.'" class="btn-dl">⬇️</a>';
                    $o .= '</span></li>';
                }
                $o .= '</ul>';
            } else {
                $o .= '<p>Keine Videos.</p>';
            }
            $o .= '</div>';
        }
        $o .= '</div>';
        return $o;
    }
}

class GuestbookManager {
    private $entries = [], $file = 'guestbook.json';
    public function __construct() { if (file_exists($this->file)) $this->entries = json_decode(file_get_contents($this->file), true) ?? []; }
    public function handleFormSubmission() {
        if (isset($_POST['guestbook'],$_POST['guest-name'],$_POST['guest-message'])) {
            $this->entries[] = ['name'=>htmlspecialchars($_POST['guest-name']),'message'=>htmlspecialchars($_POST['guest-message']),'date'=>date('Y-m-d H:i:s')];
            file_put_contents($this->file, json_encode($this->entries));
        }
    }
    public function deleteEntry($i) { if (isset($this->entries[$i])) { unset($this->entries[$i]); $this->entries = array_values($this->entries); file_put_contents($this->file, json_encode($this->entries)); return true; } return false; }
    public function displayForm() { return '<form method="post"><input type="hidden" name="guestbook" value="1"><label>Name:</label><input name="guest-name" required><label>Nachricht:</label><textarea name="guest-message" required></textarea><button type="submit">Senden</button></form>'; }
    public function displayEntries($admin=false) {
        $o = '<div class="gb-entries">';
        foreach ($this->entries as $i=>$e) {
            $o .= '<div class="gb-entry"><h4>'.$e['name'].'</h4><p>'.$e['message'].'</p><small>'.$e['date'].'</small>';
            if ($admin) $o .= '<form method="post" style="display:inline"><input type="hidden" name="action" value="delete_guestbook"><input type="hidden" name="delete_entry" value="'.$i.'"><button class="del-btn">X</button></form>';
            $o .= '</div>';
        }
        return $o.'</div>';
    }
}

class ContactManager {
    private $file = 'feedbacks.json';
    public function displayForm() { return '<form method="post" id="contact-form"><input type="hidden" name="contact" value="1"><label>Name:</label><input name="name" required><label>E-Mail:</label><input type="email" name="email" required><label>Nachricht:</label><textarea name="message" required></textarea><button type="submit">Senden</button></form><div id="contact-fb"></div>'; }
    public function handleSubmission($n,$e,$m) {
        if (!$n||!$e||!$m) return ['success'=>false,'message'=>'Alle Felder ausfüllen'];
        $fb = ['name'=>htmlspecialchars($n),'email'=>filter_var($e,FILTER_SANITIZE_EMAIL),'message'=>htmlspecialchars($m),'date'=>date('Y-m-d H:i:s'),'ip'=>$_SERVER['REMOTE_ADDR']??''];
        $all = file_exists($this->file) ? json_decode(file_get_contents($this->file),true) : [];
        $all[] = $fb;
        file_put_contents($this->file, json_encode($all, JSON_PRETTY_PRINT));
        return ['success'=>true,'message'=>'Nachricht gesendet!'];
    }
    public function deleteFeedback($i) { $all = json_decode(file_get_contents($this->file),true); if (isset($all[$i])) { unset($all[$i]); file_put_contents($this->file, json_encode(array_values($all),JSON_PRETTY_PRINT)); return true; } return false; }
}

class AdminManager {
    public function isAdmin() { return isset($_SESSION['admin']) && $_SESSION['admin'] === true; }
    public function handleLogin($u,$p) { if ($u==='admin' && $p==='sonne4000$$$$Q') { $_SESSION['admin']=true; return true; } return false; }
    public function displayLoginForm() { return '<form method="post"><input type="hidden" name="admin-login" value="1"><label>User:</label><input name="username" required><label>Pass:</label><input type="password" name="password" required><button type="submit">Login</button></form>'; }
    public function displayAdminContent() {
        global $settingsManager;
        $o = '<div class="admin-panel">';
        $o .= '<h3>⚙️ Einstellungen</h3>';
        $o .= '<div class="setting"><label>Zuschauer anzeigen</label><input type="checkbox" id="s-viewer" '.($settingsManager->get('viewer_display.enabled')?'checked':'').'></div>';
        $o .= '<div class="setting"><label>Mindestanzahl</label><input type="number" id="s-min" value="'.$settingsManager->get('viewer_display.min_viewers').'" min="1" max="100"></div>';
        $o .= '<div class="setting"><label>Im Player abspielen</label><input type="checkbox" id="s-play" '.($settingsManager->get('video_mode.play_in_player')?'checked':'').'></div>';
        $o .= '<div class="setting"><label>Download erlauben</label><input type="checkbox" id="s-dl" '.($settingsManager->get('video_mode.allow_download')?'checked':'').'></div>';
        $o .= '</div>';
        $o .= '<div class="admin-panel"><h3>📩 Nachrichten</h3>';
        $msgs = file_exists('feedbacks.json') ? json_decode(file_get_contents('feedbacks.json'),true) : [];
        foreach ($msgs as $i=>$m) {
            $o .= '<div class="msg"><strong>'.$m['name'].'</strong> ('.$m['email'].')<p>'.$m['message'].'</p><small>'.$m['date'].'</small>';
            $o .= '<form method="post" style="display:inline"><input type="hidden" name="action" value="delete_feedback"><input type="hidden" name="delete_index" value="'.$i.'"><button class="del-btn">X</button></form></div>';
        }
        if (!$msgs) $o .= '<p>Keine Nachrichten.</p>';
        $o .= '</div>';
        return $o;
    }
    public function displayGalleryImages() {
        $o = '<div class="gallery">';
        foreach (glob("uploads/*.{jpg,jpeg,png,gif}",GLOB_BRACE) as $f) $o .= '<img src="'.$f.'" onclick="openImg(this.src)">';
        return $o.'</div>';
    }
}

class VideoArchiveManager {
    private $dir;
    public function __construct($d='./videos/') { $this->dir = $d; }
    public function handleSpecificVideoDownload() {
        if (isset($_GET['download_specific_video'],$_GET['token'])) {
            $p = $_GET['download_specific_video'];
            if (!hash_equals(hash_hmac('sha256',$p,session_id()), $_GET['token'])) { echo "Invalid"; exit; }
            $rp = realpath($p);
            $rd = realpath($this->dir);
            if ($rp && strpos($rp,$rd)===0 && file_exists($rp)) {
                header('Content-Type: video/mp4');
                header('Content-Disposition: attachment; filename="'.basename($rp).'"');
                header('Content-Length: '.filesize($rp));
                readfile($rp);
                exit;
            }
            echo "Not found"; exit;
        }
    }
}

$webcamManager = new WebcamManager();
$guestbookManager = new GuestbookManager();
$contactManager = new ContactManager();
$adminManager = new AdminManager();
$videoArchiveManager = new VideoArchiveManager('./videos/');
$videoArchiveManager->handleSpecificVideoDownload();

if ($_SERVER['REQUEST_METHOD'] === 'POST') {
    if (isset($_POST['action']) && $_POST['action'] === 'viewer_heartbeat') $viewerCounter->handleHeartbeat();
    if (isset($_POST['guestbook'])) { $guestbookManager->handleFormSubmission(); header("Location: ".$_SERVER['PHP_SELF']."#guestbook"); exit; }
    if (isset($_POST['contact'])) {
        $r = $contactManager->handleSubmission($_POST['name'],$_POST['email'],$_POST['message']);
        if (isset($_SERVER['HTTP_X_REQUESTED_WITH'])) { header('Content-Type: application/json'); echo json_encode($r); exit; }
        header('Location: '.$_SERVER['PHP_SELF'].'#kontakt'); exit;
    }
    if (isset($_POST['admin-login'])) { $adminManager->handleLogin($_POST['username'],$_POST['password']); header('Location: '.$_SERVER['PHP_SELF'].'#admin'); exit; }
    if ($adminManager->isAdmin()) {
        if (isset($_POST['action']) && $_POST['action']==='delete_guestbook') { $guestbookManager->deleteEntry(intval($_POST['delete_entry'])); header("Location: ".$_SERVER['PHP_SELF']."#guestbook"); exit; }
        if (isset($_POST['action']) && $_POST['action']==='delete_feedback') { $contactManager->deleteFeedback(intval($_POST['delete_index'])); header("Location: ".$_SERVER['PHP_SELF']."#admin"); exit; }
    }
}

$vc = $viewerCounter->getInitialCount();
$sv = $settingsManager->get('viewer_display.enabled') && $vc >= $settingsManager->get('viewer_display.min_viewers');
$mv = $settingsManager->get('viewer_display.min_viewers');
?><!DOCTYPE html>
<html lang="de">
<head>
<meta charset="UTF-8">
<meta name="viewport" content="width=device-width,initial-scale=1,maximum-scale=5,user-scalable=yes">
<title>Aurora Livecam</title>
<link rel="stylesheet" href="https://cdnjs.cloudflare.com/ajax/libs/font-awesome/5.15.3/css/all.min.css">
<script src="https://cdn.jsdelivr.net/npm/hls.js@latest"></script>
<style>
*{box-sizing:border-box;margin:0;padding:0}
body{font-family:Arial,sans-serif;background:#f0f0f0;color:#333;line-height:1.6}
.container{max-width:1100px;margin:0 auto;padding:0 15px}
.section{padding:50px 0;background:#fff;margin-bottom:15px}
.section h2{text-align:center;margin-bottom:25px;font-size:28px}

header{background:#fff;padding:12px 0;position:sticky;top:0;z-index:100;box-shadow:0 2px 8px rgba(0,0,0,0.1)}
.header-inner{display:flex;justify-content:space-between;align-items:center;flex-wrap:wrap;gap:10px}
.logo img{height:45px}
nav ul{list-style:none;display:flex;flex-wrap:wrap;gap:5px}
nav a{text-decoration:none;color:#333;padding:8px 14px;border-radius:5px;font-weight:bold;transition:.3s}
nav a:hover{background:#4CAF50;color:#fff}

.hero{text-align:center;padding:40px 15px;background:linear-gradient(135deg,#667eea,#764ba2);color:#fff}
.hero h1{font-size:2em;margin-bottom:10px}

.video-box{max-width:900px;margin:0 auto 20px}
.video-wrap{position:relative;padding-bottom:56.25%;background:#000;border-radius:10px;overflow:hidden}
.video-wrap video,.video-wrap img,.video-wrap #dvp{position:absolute;top:0;left:0;width:100%;height:100%;object-fit:contain}
#tlv,#dvp{display:none;background:#000}
#dvp video{width:100%;height:100%}

.zoom-btns{position:absolute;bottom:15px;right:15px;display:flex;gap:8px;z-index:100}
.zoom-btns button{width:44px;height:44px;border:none;border-radius:50%;background:rgba(255,255,255,.95);font-size:20px;cursor:pointer;box-shadow:0 2px 8px rgba(0,0,0,.3);transition:.2s}
.zoom-btns button:hover{transform:scale(1.1);background:#fff}

.info-bar{display:flex;justify-content:center;gap:15px;margin:15px 0;flex-wrap:wrap}
.badge{background:#fff;padding:8px 18px;border-radius:25px;font-weight:bold;display:flex;align-items:center;gap:8px;box-shadow:0 2px 8px rgba(0,0,0,.1)}
.badge.live{background:#fff5f5;color:#d32f2f}
.dot{width:8px;height:8px;background:#f44;border-radius:50%;animation:pulse 2s infinite}
@keyframes pulse{0%,100%{box-shadow:0 0 0 0 rgba(244,67,54,.6)}50%{box-shadow:0 0 0 8px transparent}}

.btns{display:flex;justify-content:center;gap:10px;flex-wrap:wrap;margin:15px 0}
.btn{padding:10px 20px;background:linear-gradient(135deg,#4CAF50,#45a049);color:#fff;border:none;border-radius:6px;font-weight:bold;cursor:pointer;text-decoration:none;transition:.3s}
.btn:hover{transform:translateY(-2px);box-shadow:0 4px 12px rgba(76,175,80,.4)}
.btn.purple{background:linear-gradient(135deg,#667eea,#764ba2)}

#tl-ctrl{display:none;background:#fff;padding:12px 20px;border-radius:30px;margin:15px auto;max-width:700px;box-shadow:0 3px 10px rgba(0,0,0,.1)}
.tl-bar{display:flex;align-items:center;gap:12px;flex-wrap:wrap;justify-content:center}
.tl-btn{width:40px;height:40px;border:none;border-radius:50%;background:linear-gradient(135deg,#667eea,#764ba2);color:#fff;cursor:pointer;font-size:14px}
.tl-btn.on{background:linear-gradient(135deg,#4CAF50,#45a049)}
.tl-btn.wide{width:auto;padding:0 15px;border-radius:20px}
#tl-slider{flex:1;min-width:120px;max-width:250px}
#tl-time{font-family:monospace;background:#f5f5f5;padding:6px 12px;border-radius:15px}

#back-live{display:none}

.calendar-box{max-width:700px;margin:0 auto;background:#fff;border-radius:10px;padding:20px;box-shadow:0 3px 15px rgba(0,0,0,.1)}
.cal-nav{display:flex;justify-content:space-between;align-items:center;background:linear-gradient(135deg,#667eea,#764ba2);color:#fff;padding:12px 15px;border-radius:8px;margin-bottom:15px}
.cal-nav button{background:rgba(255,255,255,.2);border:none;color:#fff;padding:8px 15px;border-radius:5px;font-size:18px;cursor:pointer}
.cal-grid{display:grid;grid-template-columns:repeat(7,1fr);gap:5px}
.cal-hd{text-align:center;font-weight:bold;padding:8px;background:#f5f5f5;border-radius:4px;font-size:12px}
.cal-day{aspect-ratio:1;display:flex;flex-direction:column;align-items:center;justify-content:center;background:#fff;border:2px solid #e0e0e0;border-radius:8px;cursor:pointer;transition:.2s;position:relative;font-size:14px}
.cal-day:hover:not(.empty){transform:scale(1.05);border-color:#667eea}
.cal-day.empty{background:transparent;border:none;cursor:default}
.cal-day.has-vid{background:linear-gradient(135deg,#e3f2fd,#bbdefb);border-color:#2196F3}
.cal-day.sel{background:linear-gradient(135deg,#667eea,#764ba2);color:#fff;transform:scale(1.08)}
.cal-day.today{border:2px solid #4CAF50}
.cal-day small{position:absolute;bottom:2px;right:2px;font-size:10px}

.day-vids{background:#f9f9f9;border-radius:8px;padding:15px;margin-top:15px}
.day-vids h4{margin-bottom:10px;border-bottom:2px solid #667eea;padding-bottom:8px}
.day-vids ul{list-style:none}
.day-vids li{display:flex;justify-content:space-between;align-items:center;padding:10px;background:#fff;margin-bottom:8px;border-radius:6px;flex-wrap:wrap;gap:8px}
.vid-btns{display:flex;gap:8px}
.btn-play,.btn-dl{padding:6px 12px;border-radius:15px;text-decoration:none;color:#fff;font-size:13px}
.btn-play{background:linear-gradient(135deg,#667eea,#764ba2)}
.btn-dl{background:linear-gradient(135deg,#4CAF50,#45a049)}

form{display:grid;gap:12px;background:#f9f9f9;padding:20px;border-radius:8px;max-width:500px;margin:0 auto}
input,textarea{width:100%;padding:10px;border:2px solid #ddd;border-radius:6px;font-size:15px}
input:focus,textarea:focus{border-color:#667eea;outline:none}
button[type=submit]{padding:10px 20px;background:linear-gradient(135deg,#4CAF50,#45a049);color:#fff;border:none;border-radius:6px;font-weight:bold;cursor:pointer}

.gb-entries{max-width:600px;margin:20px auto 0}
.gb-entry{background:#fff;border-left:4px solid #4CAF50;padding:15px;margin-bottom:10px;border-radius:6px;box-shadow:0 2px 6px rgba(0,0,0,.08)}
.gb-entry h4{margin-bottom:5px}
.gb-entry small{color:#888}

.gallery{display:flex;gap:10px;overflow-x:auto;padding:10px 0}
.gallery img{width:200px;height:140px;object-fit:cover;border-radius:8px;cursor:pointer;flex-shrink:0}

.admin-panel{background:#fff;padding:20px;border-radius:10px;margin-bottom:20px}
.admin-panel h3{margin-bottom:15px;border-bottom:2px solid #667eea;padding-bottom:8px}
.setting{display:flex;justify-content:space-between;align-items:center;padding:10px 0;border-bottom:1px solid #eee}
.setting:last-child{border-bottom:none}
.setting input[type=checkbox]{width:20px;height:20px}
.setting input[type=number]{width:60px;padding:5px;text-align:center}
.msg{background:#f9f9f9;padding:12px;border-left:3px solid #667eea;margin-bottom:8px;border-radius:4px}
.del-btn{background:#f44;color:#fff;border:none;padding:4px 10px;border-radius:4px;cursor:pointer}

footer{background:#333;color:#fff;padding:30px 0;text-align:center}
footer a{color:#fff;margin:0 10px}

.modal{display:none;position:fixed;z-index:1000;left:0;top:0;width:100%;height:100%;background:rgba(0,0,0,.9);align-items:center;justify-content:center}
.modal img{max-width:95%;max-height:90%}
.modal .close{position:absolute;top:15px;right:25px;color:#fff;font-size:35px;cursor:pointer}

@media(max-width:600px){
.header-inner{flex-direction:column}
nav ul{justify-content:center}
.hero h1{font-size:1.5em}
.btns{flex-direction:column}
.btn{width:100%}
.tl-bar{flex-direction:column}
#tl-slider{width:100%;max-width:none}
}
</style>
</head>
<body>

<header>
<div class="container header-inner">
<div class="logo"><img src="logo.png" alt="Logo"></div>
<nav><ul>
<li><a href="#cam">Webcam</a></li>
<li><a href="#archive">Archiv</a></li>
<li><a href="#guestbook">Gästebuch</a></li>
<li><a href="#kontakt">Kontakt</a></li>
<?php if($adminManager->isAdmin()): ?><li><a href="#admin">Admin</a></li><?php endif; ?>
</ul></nav>
</div>
</header>

<section class="hero">
<h1>Aurora Wetter Livecam</h1>
<p>Faszinierende Ausblicke aus dem Zürcher Oberland</p>
</section>

<section id="cam" class="section">
<div class="container">
<div class="video-box">
<div class="video-wrap" id="vw">
<?php echo $webcamManager->displayWebcam(); ?>
<div id="tlv"><img id="tl-img"><div id="tl-overlay" style="position:absolute;top:10px;left:10px;background:rgba(0,0,0,.7);color:#fff;padding:6px 12px;border-radius:4px;font-family:monospace"></div></div>
<div id="dvp"><video id="dv" controls playsinline></video></div>
<div class="zoom-btns">
<button onclick="zoom(-1)">−</button>
<button onclick="zoom(0)">⟲</button>
<button onclick="zoom(1)">+</button>
</div>
</div>
</div>

<div id="tl-ctrl">
<div class="tl-bar">
<button class="tl-btn" id="tl-play"><i class="fas fa-play"></i></button>
<button class="tl-btn" id="tl-rev"><i class="fas fa-backward"></i></button>
<input type="range" id="tl-slider" min="0" value="0">
<span id="tl-time">--:--:--</span>
<button class="tl-btn wide" id="tl-spd">1x</button>
<button class="tl-btn wide on" id="tl-back"><i class="fas fa-video"></i> Live</button>
</div>
</div>

<button class="btn purple" id="back-live" onclick="toLive()"><i class="fas fa-video"></i> Zurück zu Live</button>

<div class="info-bar">
<?php echo $webcamManager->displayStreamStats(); ?>
<?php if($sv): ?><div class="badge live"><span class="dot"></span><strong id="vc"><?php echo $vc; ?></strong> Zuschauer</div><?php endif; ?>
</div>

<div class="btns">
<a href="?action=snapshot" class="btn">📷 Snapshot</a>
<button class="btn" id="tl-btn">🎬 Zeitraffer</button>
<a href="?download_video=1" class="btn">⬇️ Tagesvideo</a>
</div>
</div>
</section>

<section id="archive" class="section">
<div class="container">
<h2>📅 Videoarchiv</h2>
<?php $cal = new VisualCalendarManager('./videos/', $settingsManager); echo $cal->displayVisualCalendar(); ?>
</div>
</section>

<section id="guestbook" class="section">
<div class="container">
<h2>Gästebuch</h2>
<?php echo $guestbookManager->displayForm(); echo $guestbookManager->displayEntries($adminManager->isAdmin()); ?>
</div>
</section>

<section id="kontakt" class="section">
<div class="container">
<h2>Kontakt</h2>
<?php echo $contactManager->displayForm(); ?>
</div>
</section>

<section id="gallery" class="section">
<div class="container">
<h2>Galerie</h2>
<?php echo $adminManager->displayGalleryImages(); ?>
</div>
</section>

<?php if($adminManager->isAdmin()): ?>
<section id="admin" class="section">
<div class="container">
<h2>⚙️ Admin</h2>
<?php echo $adminManager->displayAdminContent(); ?>
</div>
</section>
<?php else: ?>
<section id="admin" class="section">
<div class="container">
<h2>Admin Login</h2>
<?php echo $adminManager->displayLoginForm(); ?>
</div>
</section>
<?php endif; ?>

<footer>
<a href="#cam">Webcam</a>
<a href="#archive">Archiv</a>
<a href="#kontakt">Kontakt</a>
<p style="margin-top:15px">&copy; 2024 Aurora Livecam</p>
</footer>

<div class="modal" id="modal" onclick="this.style.display='none'">
<span class="close">&times;</span>
<img id="modal-img">
</div>

<script>
<?php echo $webcamManager->getJavaScript(); ?>

let zoomLvl=1;
function zoom(d){
if(d===0) zoomLvl=1;
else zoomLvl=Math.max(1,Math.min(4,zoomLvl+d*0.5));
// Alle Video-Elemente in allen Modi
const targets=['#webcam-player','#tl-img','#dv'];
targets.forEach(sel=>{
const el=document.querySelector(sel);
if(el){
el.style.transform='scale('+zoomLvl+')';
el.style.transformOrigin='center center';
el.style.transition='transform 0.2s ease';
}
});
// Zoom-Level Anzeige
showZoomLevel();
}
function showZoomLevel(){
let ind=document.getElementById('zoom-ind');
if(!ind){
ind=document.createElement('div');
ind.id='zoom-ind';
ind.style.cssText='position:absolute;top:15px;left:15px;background:rgba(0,0,0,0.7);color:#fff;padding:8px 14px;border-radius:20px;font-weight:bold;z-index:100;transition:opacity 0.3s';
document.getElementById('vw').appendChild(ind);
}
ind.textContent='🔍 '+Math.round(zoomLvl*100)+'%';
ind.style.opacity='1';
clearTimeout(ind.hideTimer);
ind.hideTimer=setTimeout(()=>{ind.style.opacity='0';},1500);
}

const TL={
imgs:<?php echo $imageFilesJson; ?>,
idx:0,playing:false,rev:false,spd:1,spds:[1,10,100],iv:null,
init(){
document.getElementById('tl-play').onclick=()=>this.toggle();
document.getElementById('tl-rev').onclick=()=>this.toggleRev();
document.getElementById('tl-spd').onclick=()=>this.cycleSpd();
document.getElementById('tl-back').onclick=()=>toLive();
document.getElementById('tl-slider').max=this.imgs.length-1;
document.getElementById('tl-slider').oninput=e=>this.seek(+e.target.value);
},
show(){
document.getElementById('webcam-player').style.display='none';
document.getElementById('dvp').style.display='none';
document.getElementById('tlv').style.display='block';
document.getElementById('tl-ctrl').style.display='block';
document.getElementById('back-live').style.display='none';
this.idx=0;this.frame();
},
toggle(){
this.playing=!this.playing;
document.getElementById('tl-play').innerHTML=this.playing?'<i class="fas fa-pause"></i>':'<i class="fas fa-play"></i>';
if(this.playing)this.play();else this.stop();
},
toggleRev(){this.rev=!this.rev;document.getElementById('tl-rev').classList.toggle('on',this.rev);},
cycleSpd(){const i=this.spds.indexOf(this.spd);this.spd=this.spds[(i+1)%this.spds.length];document.getElementById('tl-spd').textContent=this.spd+'x';if(this.playing){this.stop();this.play();}},
play(){this.iv=setInterval(()=>this.next(),200/this.spd);},
stop(){clearInterval(this.iv);},
next(){this.idx+=this.rev?-1:1;if(this.idx<0)this.idx=this.imgs.length-1;if(this.idx>=this.imgs.length)this.idx=0;this.frame();},
seek(i){this.idx=i;this.frame();},
frame(){
const img=this.imgs[this.idx];if(!img)return;
document.getElementById('tl-img').src=img;
document.getElementById('tl-slider').value=this.idx;
const m=img.match(/(\d{4})(\d{2})(\d{2})_(\d{2})(\d{2})(\d{2})/);
if(m){const t=m[3]+'.'+m[2]+'.'+m[1]+' '+m[4]+':'+m[5]+':'+m[6];document.getElementById('tl-time').textContent=t;document.getElementById('tl-overlay').textContent=t;}
}
};

function playVid(p){
document.getElementById('webcam-player').style.display='none';
document.getElementById('tlv').style.display='none';
document.getElementById('tl-ctrl').style.display='none';
document.getElementById('dvp').style.display='block';
document.getElementById('back-live').style.display='block';
const v=document.getElementById('dv');v.src=p;v.play();
document.getElementById('cam').scrollIntoView({behavior:'smooth'});
}

function toLive(){
TL.stop();TL.playing=false;
document.getElementById('tl-play').innerHTML='<i class="fas fa-play"></i>';
document.getElementById('tlv').style.display='none';
document.getElementById('tl-ctrl').style.display='none';
document.getElementById('dvp').style.display='none';
document.getElementById('back-live').style.display='none';
document.getElementById('webcam-player').style.display='block';
document.getElementById('tl-btn').textContent='🎬 Zeitraffer';
document.getElementById('dv').pause();document.getElementById('dv').src='';
zoomLvl=1;zoom(0);
}

function chgM(y,m){if(m<1){m=12;y--;}if(m>12){m=1;y++;}location.href='?cal_year='+y+'&cal_month='+m+'#archive';}
function selD(y,m,d){location.href='?cal_year='+y+'&cal_month='+m+'&cal_day='+d+'#archive';}

function openImg(s){document.getElementById('modal-img').src=s;document.getElementById('modal').style.display='flex';}

function updV(){
fetch(location.href,{method:'POST',body:new URLSearchParams({action:'viewer_heartbeat'})})
.then(r=>r.json()).then(d=>{const e=document.getElementById('vc');if(e&&d.count)e.textContent=d.count;});
}

<?php if($adminManager->isAdmin()): ?>
function saveSetting(key, value) {
const formData = new FormData();
formData.append('settings_action', 'update');
formData.append('key', key);
formData.append('value', value);

fetch(window.location.pathname, {
method: 'POST',
body: formData
})
.then(r => r.json())
.then(data => {
const toast = document.createElement('div');
toast.innerHTML = data.success ? '✓ Gespeichert' : '✗ Fehler: ' + (data.message || '');
toast.style.cssText = 'position:fixed;top:20px;right:20px;padding:15px 25px;border-radius:8px;background:' +
(data.success ? '#4CAF50' : '#f44336') + ';color:#fff;font-weight:bold;z-index:9999;box-shadow:0 4px 12px rgba(0,0,0,0.3);';
document.body.appendChild(toast);
setTimeout(() => { toast.style.opacity = '0'; toast.style.transition = 'opacity 0.3s'; }, 1500);
setTimeout(() => toast.remove(), 2000);
})
.catch(err => {
console.error('Settings save error:', err);
alert('Fehler beim Speichern: ' + err.message);
});
}

// Settings Event-Handler nach DOM-Load binden
document.addEventListener('DOMContentLoaded', function() {
const sViewer = document.getElementById('s-viewer');
const sMin = document.getElementById('s-min');
const sPlay = document.getElementById('s-play');
const sDl = document.getElementById('s-dl');

if (sViewer) sViewer.addEventListener('change', function() {
saveSetting('viewer_display.enabled', this.checked ? 'true' : 'false');
});
if (sMin) sMin.addEventListener('change', function() {
saveSetting('viewer_display.min_viewers', this.value);
});
if (sPlay) sPlay.addEventListener('change', function() {
saveSetting('video_mode.play_in_player', this.checked ? 'true' : 'false');
});
if (sDl) sDl.addEventListener('change', function() {
saveSetting('video_mode.allow_download', this.checked ? 'true' : 'false');
});
});
<?php endif; ?>

document.addEventListener('DOMContentLoaded',()=>{
TL.init();
document.getElementById('tl-btn').onclick=()=>{
if(document.getElementById('tlv').style.display==='block'){toLive();}
else{TL.show();document.getElementById('tl-btn').textContent='↩️ Zurück zu Live';}
};
setTimeout(updV,2000);setInterval(updV,10000);
});
</script>
</body>
</html>
