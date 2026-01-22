<?php
// Fehler anzeigen
error_reporting(E_ALL);
ini_set('display_errors', 1);

echo "Test 1: Settings Manager laden...<br>";
require_once 'SettingsManager.php';
echo "✓ SettingsManager.php geladen<br>";

echo "Test 2: Weather Manager laden...<br>";
require_once 'WeatherManager.php';
echo "✓ WeatherManager.php geladen<br>";

echo "Test 3: SettingsManager initialisieren...<br>";
$settingsManager = new SettingsManager();
echo "✓ SettingsManager initialisiert<br>";

echo "Test 4: WeatherManager initialisieren...<br>";
$weatherManager = new WeatherManager($settingsManager);
echo "✓ WeatherManager initialisiert<br>";

echo "Test 5: Wetter abrufen...<br>";
$weather = $weatherManager->getCurrentWeather();
echo "✓ Wetter abgerufen<br>";

echo "<pre>";
print_r($weather);
echo "</pre>";

echo "<br><br>✅ ALLE TESTS ERFOLGREICH!";
?>
