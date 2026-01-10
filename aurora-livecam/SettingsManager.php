<?php
/**
 * SettingsManager - Verwaltet Admin-Einstellungen
 * Speichert in settings.json, lädt ohne Reload
 */
class SettingsManager {
    private $settingsFile = 'settings.json';
    private $settings = [];

    public function __construct($file = null) {
        if ($file) $this->settingsFile = $file;
        $this->load();
    }

    private function load() {
        if (file_exists($this->settingsFile)) {
            $content = file_get_contents($this->settingsFile);
            $this->settings = json_decode($content, true) ?? $this->getDefaults();
        } else {
            $this->settings = $this->getDefaults();
            $this->save();
        }
    }

    private function getDefaults() {
        return [
            'viewer_display' => [
                'enabled' => true,
                'min_viewers' => 1
            ],
            'video_mode' => [
                'play_in_player' => true,
                'allow_download' => true
            ],
            'timelapse' => [
                'default_speed' => 1,
                'available_speeds' => [1, 10, 100]
            ],
            'last_updated' => null,
            'updated_by' => null
        ];
    }

    public function get($key = null) {
        if ($key === null) return $this->settings;
        $keys = explode('.', $key);
        $value = $this->settings;
        foreach ($keys as $k) {
            if (!isset($value[$k])) return null;
            $value = $value[$k];
        }
        return $value;
    }

    public function set($key, $value) {
        $keys = explode('.', $key);
        $ref = &$this->settings;
        foreach ($keys as $i => $k) {
            if ($i === count($keys) - 1) {
                $ref[$k] = $value;
            } else {
                if (!isset($ref[$k])) $ref[$k] = [];
                $ref = &$ref[$k];
            }
        }
        $this->settings['last_updated'] = date('Y-m-d H:i:s');
        return $this->save();
    }

    private function save() {
        return file_put_contents(
            $this->settingsFile,
            json_encode($this->settings, JSON_PRETTY_PRINT)
        ) !== false;
    }

    // Für AJAX-Anfragen
    public function handleAjax() {
        if ($_SERVER['REQUEST_METHOD'] !== 'POST') return;
        if (!isset($_POST['settings_action'])) return;

        header('Content-Type: application/json');

        switch ($_POST['settings_action']) {
            case 'get':
                echo json_encode(['success' => true, 'settings' => $this->settings]);
                exit;

            case 'update':
                $key = $_POST['key'] ?? null;
                $value = $_POST['value'] ?? null;

                // Boolean-Werte konvertieren
                if ($value === 'true') $value = true;
                if ($value === 'false') $value = false;
                if (is_numeric($value)) $value = intval($value);

                if ($key && $this->set($key, $value)) {
                    echo json_encode(['success' => true, 'message' => 'Einstellung gespeichert']);
                } else {
                    echo json_encode(['success' => false, 'message' => 'Fehler beim Speichern']);
                }
                exit;
        }
    }

    // Viewer-Anzeige prüfen
    public function shouldShowViewers($currentCount) {
        if (!$this->get('viewer_display.enabled')) return false;
        return $currentCount >= $this->get('viewer_display.min_viewers');
    }

    // Video-Modus prüfen
    public function shouldPlayInPlayer() {
        return $this->get('video_mode.play_in_player') === true;
    }

    public function shouldAllowDownload() {
        return $this->get('video_mode.allow_download') === true;
    }
}
