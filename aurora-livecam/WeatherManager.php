<?php
/**
 * WeatherManager - Holt und cached Wetterdaten von Open-Meteo (kostenlos!)
 * Keine API Key nötig!
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

        // Prüfe Cache
        $cached = $this->getCache();
        if ($cached !== null) {
            return $cached;
        }

        // Hole frische Daten von API (Open-Meteo)
        $coords = $this->settingsManager->getWeatherCoords();

        // Open-Meteo API URL - komplett kostenlos, kein API Key!
        $url = "https://api.open-meteo.com/v1/forecast?" . http_build_query([
            'latitude' => $coords['lat'],
            'longitude' => $coords['lon'],
            'current' => 'temperature_2m,relative_humidity_2m,precipitation,weather_code,wind_speed_10m,wind_direction_10m,pressure_msl,cloud_cover',
            'timezone' => 'Europe/Zurich'
        ]);

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
        if (!$data || !isset($data['current'])) {
            return ['error' => 'Ungültige API Antwort'];
        }

        $current = $data['current'];

        // Formatiere Daten
        $weather = [
            'temp' => round($current['temperature_2m'], 1),
            'feels_like' => round($current['temperature_2m'], 1), // Open-Meteo hat keine "feels like"
            'humidity' => $current['relative_humidity_2m'],
            'pressure' => round($current['pressure_msl'], 0),
            'wind_speed' => round($current['wind_speed_10m'], 1), // Schon in km/h!
            'wind_deg' => $current['wind_direction_10m'],
            'wind_direction' => $this->getWindDirection($current['wind_direction_10m']),
            'clouds' => $current['cloud_cover'] ?? 0,
            'description' => $this->getWeatherDescription($current['weather_code']),
            'icon' => $this->getWeatherIcon($current['weather_code']),
            'rain_1h' => $current['precipitation'] ?? 0,
            'snow_1h' => 0, // Open-Meteo gibt Niederschlag gesamt
            'location' => $this->settingsManager->getWeatherLocation(),
            'timestamp' => time()
        ];

        // Cache speichern
        $this->saveCache($weather);

        return $weather;
    }

    /**
     * Wandelt WMO Weather Code in Beschreibung um
     * https://open-meteo.com/en/docs
     */
    private function getWeatherDescription($code) {
        $descriptions = [
            0 => 'Klar',
            1 => 'Überwiegend klar',
            2 => 'Teilweise bewölkt',
            3 => 'Bewölkt',
            45 => 'Neblig',
            48 => 'Nebel mit Reifablagerung',
            51 => 'Leichter Nieselregen',
            53 => 'Mäßiger Nieselregen',
            55 => 'Dichter Nieselregen',
            61 => 'Leichter Regen',
            63 => 'Mäßiger Regen',
            65 => 'Starker Regen',
            71 => 'Leichter Schneefall',
            73 => 'Mäßiger Schneefall',
            75 => 'Starker Schneefall',
            77 => 'Schneegraupeln',
            80 => 'Leichte Regenschauer',
            81 => 'Mäßige Regenschauer',
            82 => 'Starke Regenschauer',
            85 => 'Leichte Schneeschauer',
            86 => 'Starke Schneeschauer',
            95 => 'Gewitter',
            96 => 'Gewitter mit leichtem Hagel',
            99 => 'Gewitter mit starkem Hagel'
        ];

        return $descriptions[$code] ?? 'Unbekannt';
    }

    /**
     * Wandelt WMO Weather Code in Icon-Code um (OpenWeatherMap kompatibel)
     */
    private function getWeatherIcon($code) {
        if ($code == 0) return '01d'; // Klar
        if ($code >= 1 && $code <= 2) return '02d'; // Teilweise bewölkt
        if ($code == 3) return '04d'; // Bewölkt
        if ($code >= 45 && $code <= 48) return '50d'; // Nebel
        if ($code >= 51 && $code <= 55) return '09d'; // Nieselregen
        if ($code >= 61 && $code <= 65) return '10d'; // Regen
        if ($code >= 71 && $code <= 77) return '13d'; // Schnee
        if ($code >= 80 && $code <= 82) return '09d'; // Regenschauer
        if ($code >= 85 && $code <= 86) return '13d'; // Schneeschauer
        if ($code >= 95 && $code <= 99) return '11d'; // Gewitter

        return '01d'; // Default
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
