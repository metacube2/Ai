<?php
/**
 * WeatherManager - Holt und cached Wetterdaten von OpenWeatherMap
 */
class WeatherManager {
    private $settingsManager;
    private $cacheFile = 'weather_cache.json';
    private $cacheTime = 300; // 5 Minuten in Sekunden

    public function __construct($settingsManager) {
        $this->settingsManager = $settingsManager;
    }

    /**
     * Holt aktuelle Wetterdaten (cached)
     */
    public function getCurrentWeather() {
        // Prüfe ob Weather aktiviert ist
        if (!$this->settingsManager->isWeatherEnabled()) {
            return null;
        }

        // Prüfe API Key
        $apiKey = $this->settingsManager->getWeatherApiKey();
        if (empty($apiKey)) {
            return ['error' => 'API Key fehlt'];
        }

        // Prüfe Cache
        $cached = $this->getCache();
        if ($cached !== null) {
            return $cached;
        }

        // Hole frische Daten von API
        $coords = $this->settingsManager->getWeatherCoords();
        $units = $this->settingsManager->getWeatherUnits();

        $url = "https://api.openweathermap.org/data/2.5/weather?lat={$coords['lat']}&lon={$coords['lon']}&units={$units}&appid={$apiKey}&lang=de";

        $ch = curl_init();
        curl_setopt($ch, CURLOPT_URL, $url);
        curl_setopt($ch, CURLOPT_RETURNTRANSFER, true);
        curl_setopt($ch, CURLOPT_TIMEOUT, 5);
        curl_setopt($ch, CURLOPT_SSL_VERIFYPEER, true);

        $response = curl_exec($ch);
        $httpCode = curl_getinfo($ch, CURLINFO_HTTP_CODE);
        curl_close($ch);

        if ($httpCode !== 200 || !$response) {
            return ['error' => 'API Fehler'];
        }

        $data = json_decode($response, true);
        if (!$data || !isset($data['main'])) {
            return ['error' => 'Ungültige API Antwort'];
        }

        // Formatiere Daten
        $weather = [
            'temp' => round($data['main']['temp'], 1),
            'feels_like' => round($data['main']['feels_like'], 1),
            'humidity' => $data['main']['humidity'],
            'pressure' => $data['main']['pressure'],
            'wind_speed' => round($data['wind']['speed'] * 3.6, 1), // m/s -> km/h
            'wind_deg' => $data['wind']['deg'] ?? 0,
            'wind_direction' => $this->getWindDirection($data['wind']['deg'] ?? 0),
            'clouds' => $data['clouds']['all'] ?? 0,
            'description' => ucfirst($data['weather'][0]['description'] ?? 'Unbekannt'),
            'icon' => $data['weather'][0]['icon'] ?? '01d',
            'rain_1h' => $data['rain']['1h'] ?? 0,
            'snow_1h' => $data['snow']['1h'] ?? 0,
            'location' => $data['name'] ?? $this->settingsManager->getWeatherLocation(),
            'timestamp' => time()
        ];

        // Cache speichern
        $this->saveCache($weather);

        return $weather;
    }

    /**
     * Wandelt Windrichtung (Grad) in Kompassrichtung um
     */
    private function getWindDirection($deg) {
        $directions = ['N', 'NNO', 'NO', 'ONO', 'O', 'OSO', 'SO', 'SSO', 'S', 'SSW', 'SW', 'WSW', 'W', 'WNW', 'NW', 'NNW'];
        $index = round($deg / 22.5) % 16;
        return $directions[$index];
    }

    /**
     * Holt Daten aus Cache (wenn noch gültig)
     */
    private function getCache() {
        if (!file_exists($this->cacheFile)) {
            return null;
        }

        $content = file_get_contents($this->cacheFile);
        $data = json_decode($content, true);

        if (!$data || !isset($data['timestamp'])) {
            return null;
        }

        // Update-Intervall aus Settings holen (in Minuten)
        $updateInterval = $this->settingsManager->getWeatherUpdateInterval() * 60; // Minuten -> Sekunden

        // Prüfe ob Cache noch gültig
        if (time() - $data['timestamp'] < $updateInterval) {
            return $data;
        }

        return null;
    }

    /**
     * Speichert Daten im Cache
     */
    private function saveCache($data) {
        $json = json_encode($data, JSON_PRETTY_PRINT);
        file_put_contents($this->cacheFile, $json, LOCK_EX);
    }

    /**
     * Gibt Wetter-Icon-Emoji zurück
     */
    public function getWeatherEmoji($iconCode) {
        $map = [
            '01d' => '☀️', '01n' => '🌙',
            '02d' => '⛅', '02n' => '☁️',
            '03d' => '☁️', '03n' => '☁️',
            '04d' => '☁️', '04n' => '☁️',
            '09d' => '🌧️', '09n' => '🌧️',
            '10d' => '🌦️', '10n' => '🌧️',
            '11d' => '⛈️', '11n' => '⛈️',
            '13d' => '❄️', '13n' => '❄️',
            '50d' => '🌫️', '50n' => '🌫️'
        ];
        return $map[$iconCode] ?? '🌤️';
    }

    /**
     * AJAX Handler für Wetter-Updates
     */
    public function handleAjax() {
        if ($_SERVER['REQUEST_METHOD'] !== 'GET') return;
        if (!isset($_GET['weather_action'])) return;

        header('Content-Type: application/json');

        if ($_GET['weather_action'] === 'get') {
            $weather = $this->getCurrentWeather();
            echo json_encode(['success' => true, 'data' => $weather]);
            exit;
        }
    }
}
