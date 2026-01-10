# Integration Guide für Aurora Livecam Erweiterungen

## Übersicht der neuen Dateien

```
aurora-livecam/
├── SettingsManager.php      # Admin-Einstellungen Klasse
├── settings.json            # Einstellungen Datei
├── js/
│   ├── timelapse-controls.js   # Timelapse mit Slider
│   ├── video-player.js         # Tagesvideos im Player
│   └── admin-settings.js       # Admin AJAX
├── css/
│   └── player-controls.css     # Styles für Controls
└── INTEGRATION.md           # Diese Anleitung
```

## Änderungen in index.php

### 1. Am Anfang der Datei (nach den requires)

```php
<?php
// ... bestehende requires ...

// NEU: Settings Manager einbinden
require_once 'SettingsManager.php';
$settingsManager = new SettingsManager();

// AJAX-Handler für Settings (VOR session_start!)
$settingsManager->handleAjax();
```

### 2. Im HEAD-Bereich (CSS einbinden)

```html
<link rel="stylesheet" href="css/player-controls.css">
```

### 3. Vor </body> (JavaScript einbinden)

```html
<script src="js/timelapse-controls.js"></script>
<script src="js/video-player.js"></script>
<?php if ($adminManager->isAdmin()): ?>
<script src="js/admin-settings.js"></script>
<?php endif; ?>
```

### 4. Video-Container anpassen

Ersetze den bestehenden video-container:

```html
<div class="video-container">
    <?php echo $webcamManager->displayWebcam(); ?>

    <!-- Timelapse Overlay -->
    <div id="timelapse-viewer" style="display: none;">
        <img id="timelapse-image" src="" alt="Timelapse">
    </div>

    <!-- NEU: Daily Video Player (wird dynamisch befüllt) -->
</div>

<!-- NEU: Timelapse Controls (außerhalb des Containers) -->
<div id="timelapse-controls"></div>
```

### 5. Zuschauer-Anzeige konditionell machen

Ersetze die Viewer-Stat Anzeige:

```php
<?php
$viewerCount = $viewerCounter->getInitialCount();
$showViewers = $settingsManager->shouldShowViewers($viewerCount);
?>

<?php if ($showViewers): ?>
<div class="info-badge viewer-stat">
    <span class="live-dot"></span>
    <strong id="viewer-count-display"><?php echo $viewerCount; ?></strong>
    <span>Zuschauer</span>
</div>
<?php endif; ?>
```

### 6. Kalender Links anpassen

In der `VisualCalendarManager::displayVisualCalendar()` Methode:

```php
// Für Tagesvideos
$playInPlayer = $settingsManager->shouldPlayInPlayer();
$allowDownload = $settingsManager->shouldAllowDownload();

if ($playInPlayer) {
    // Im Player abspielen
    $output .= '<a href="#" onclick="DailyVideoPlayer.playVideo(\'' . $video['path'] . '\', ' . ($allowDownload ? 'true' : 'false') . '); return false;" class="play-link">';
    $output .= '▶️ Abspielen';
    $output .= '</a>';
}

if ($allowDownload) {
    // Download Link
    $output .= '<a href="?download_specific_video=..." class="download-link">⬇️ Download</a>';
}
```

### 7. Admin-Panel erweitern

Füge im Admin-Bereich hinzu:

```php
<?php if ($adminManager->isAdmin()): ?>
<section id="admin" class="section">
    <div class="container">
        <h2>Admin-Bereich</h2>

        <!-- NEU: Settings Panel -->
        <div id="admin-settings-panel">
            <h3>⚙️ Anzeige-Einstellungen</h3>

            <div class="settings-group">
                <h4>👥 Zuschauer-Anzeige</h4>

                <div class="setting-row">
                    <span class="setting-label">Zuschauer-Anzahl anzeigen</span>
                    <div class="setting-input">
                        <label class="toggle-switch">
                            <input type="checkbox" id="setting-viewer-enabled"
                                   <?php echo $settingsManager->get('viewer_display.enabled') ? 'checked' : ''; ?>>
                            <span class="toggle-slider"></span>
                        </label>
                    </div>
                </div>

                <div class="setting-row">
                    <span class="setting-label">Mindestanzahl für Anzeige</span>
                    <div class="setting-input">
                        <input type="number" id="setting-min-viewers" class="number-input"
                               min="1" max="100"
                               value="<?php echo $settingsManager->get('viewer_display.min_viewers'); ?>">
                    </div>
                </div>
            </div>

            <div class="settings-group">
                <h4>🎬 Video-Modus</h4>

                <div class="setting-row">
                    <span class="setting-label">Videos im Player abspielen</span>
                    <div class="setting-input">
                        <label class="toggle-switch">
                            <input type="checkbox" id="setting-play-in-player"
                                   <?php echo $settingsManager->get('video_mode.play_in_player') ? 'checked' : ''; ?>>
                            <span class="toggle-slider"></span>
                        </label>
                    </div>
                </div>

                <div class="setting-row">
                    <span class="setting-label">Download erlauben</span>
                    <div class="setting-input">
                        <label class="toggle-switch">
                            <input type="checkbox" id="setting-allow-download"
                                   <?php echo $settingsManager->get('video_mode.allow_download') ? 'checked' : ''; ?>>
                            <span class="toggle-slider"></span>
                        </label>
                    </div>
                </div>
            </div>
        </div>

        <!-- Bestehender Admin-Content -->
        <?php echo $adminManager->displayAdminContent(); ?>
    </div>
</section>
<?php endif; ?>
```

### 8. Timelapse Button Event anpassen

Im bestehenden JavaScript:

```javascript
timelapseButton.addEventListener('click', function(e) {
    e.preventDefault();

    if (timelapseViewer.style.display === 'none') {
        // NEU: TimelapseController verwenden
        TimelapseController.init(imageFiles);
        TimelapseController.show();
        timelapseButton.textContent = 'Zurück zur Live-Webcam';
    } else {
        TimelapseController.backToLive();
    }
});
```

### 9. Viewer Heartbeat anpassen

Im JavaScript für den Viewer-Counter:

```javascript
function updateViewerCount() {
    fetch(window.location.href, {
        method: 'POST',
        body: new URLSearchParams({action: 'viewer_heartbeat'})
    })
    .then(r => r.json())
    .then(data => {
        const display = document.getElementById('viewer-count-display');
        const container = document.querySelector('.viewer-stat');

        if (data.count && display) {
            display.textContent = data.count;

            // Mindestanzahl prüfen (aus Settings)
            const minViewers = window.minViewersToShow || 1;
            if (container) {
                container.style.display = data.count >= minViewers ? 'inline-flex' : 'none';
            }
        }
    });
}
```

## Fertig!

Nach diesen Änderungen hast du:
- ✅ Timelapse mit Slider und 1x/10x/100x Geschwindigkeit
- ✅ Rückwärts-Spulen im Timelapse
- ✅ Tagesvideos im Player abspielen statt nur Download
- ✅ "Zurück zu Live" Button
- ✅ Admin-Einstellungen für Zuschauer-Anzeige
- ✅ Mindestanzahl für Zuschauer-Anzeige
- ✅ Video-Modus wählbar (Player/Download)
- ✅ Alles ohne Seiten-Reload
