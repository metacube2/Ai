<?php
/**
 * StreamValidator - Validiert Stream-URLs
 */

namespace AuroraLivecam\Onboarding;

class StreamValidator
{
    private array $supportedTypes = ['hls', 'rtmp', 'webrtc', 'iframe'];
    private int $timeout = 10;

    /**
     * Validiert eine Stream-URL
     */
    public function validate(string $url): array
    {
        $result = [
            'valid' => false,
            'type' => null,
            'error' => null,
            'details' => [],
        ];

        // URL-Format prüfen
        if (!filter_var($url, FILTER_VALIDATE_URL)) {
            $result['error'] = 'Ungültiges URL-Format';
            return $result;
        }

        // Stream-Typ erkennen
        $type = $this->detectStreamType($url);
        $result['type'] = $type;
        $result['details']['detected_type'] = $type;

        // Je nach Typ validieren
        switch ($type) {
            case 'hls':
                return $this->validateHls($url, $result);
            case 'rtmp':
                return $this->validateRtmp($url, $result);
            case 'iframe':
                return $this->validateIframe($url, $result);
            default:
                // Generische HTTP-Prüfung
                return $this->validateHttp($url, $result);
        }
    }

    /**
     * Erkennt den Stream-Typ anhand der URL
     */
    public function detectStreamType(string $url): string
    {
        $url = strtolower($url);

        if (str_contains($url, '.m3u8')) {
            return 'hls';
        }

        if (str_starts_with($url, 'rtmp://') || str_starts_with($url, 'rtmps://')) {
            return 'rtmp';
        }

        if (str_contains($url, 'youtube.com') || str_contains($url, 'youtu.be') ||
            str_contains($url, 'vimeo.com') || str_contains($url, 'twitch.tv')) {
            return 'iframe';
        }

        if (str_contains($url, '.mp4') || str_contains($url, '.webm')) {
            return 'video';
        }

        return 'unknown';
    }

    /**
     * Validiert HLS-Stream
     */
    private function validateHls(string $url, array $result): array
    {
        $ch = curl_init();
        curl_setopt_array($ch, [
            CURLOPT_URL => $url,
            CURLOPT_RETURNTRANSFER => true,
            CURLOPT_TIMEOUT => $this->timeout,
            CURLOPT_FOLLOWLOCATION => true,
            CURLOPT_SSL_VERIFYPEER => false,
            CURLOPT_HTTPHEADER => [
                'User-Agent: Mozilla/5.0 (compatible; StreamValidator/1.0)'
            ],
        ]);

        $response = curl_exec($ch);
        $httpCode = curl_getinfo($ch, CURLINFO_HTTP_CODE);
        $contentType = curl_getinfo($ch, CURLINFO_CONTENT_TYPE);
        $error = curl_error($ch);
        curl_close($ch);

        $result['details']['http_code'] = $httpCode;
        $result['details']['content_type'] = $contentType;

        if ($error) {
            $result['error'] = 'Verbindungsfehler: ' . $error;
            return $result;
        }

        if ($httpCode !== 200) {
            $result['error'] = "HTTP-Fehler: $httpCode";
            return $result;
        }

        // Prüfe ob es ein gültiges M3U8 ist
        if (!str_contains($response, '#EXTM3U')) {
            $result['error'] = 'Keine gültige HLS-Playlist gefunden';
            return $result;
        }

        $result['valid'] = true;
        $result['details']['is_master'] = str_contains($response, '#EXT-X-STREAM-INF');
        $result['details']['segments'] = substr_count($response, '#EXTINF');

        return $result;
    }

    /**
     * Validiert RTMP-Stream (nur Format-Check)
     */
    private function validateRtmp(string $url, array $result): array
    {
        // RTMP kann nicht einfach per HTTP geprüft werden
        // Wir prüfen nur das Format

        $parsed = parse_url($url);

        if (!isset($parsed['host']) || empty($parsed['host'])) {
            $result['error'] = 'RTMP-URL enthält keinen gültigen Host';
            return $result;
        }

        // DNS-Check
        $ip = gethostbyname($parsed['host']);
        if ($ip === $parsed['host']) {
            $result['error'] = 'RTMP-Host nicht erreichbar (DNS-Fehler)';
            return $result;
        }

        $result['valid'] = true;
        $result['details']['host'] = $parsed['host'];
        $result['details']['note'] = 'RTMP-Streams können erst zur Laufzeit vollständig validiert werden';

        return $result;
    }

    /**
     * Validiert iFrame-Embed URL
     */
    private function validateIframe(string $url, array $result): array
    {
        // Bekannte Embed-Plattformen
        $embedPatterns = [
            'youtube' => '/(?:youtube\.com\/(?:embed|watch)|youtu\.be)/i',
            'vimeo' => '/vimeo\.com/i',
            'twitch' => '/(?:twitch\.tv|player\.twitch\.tv)/i',
            'dailymotion' => '/dailymotion\.com/i',
        ];

        $platform = 'unknown';
        foreach ($embedPatterns as $name => $pattern) {
            if (preg_match($pattern, $url)) {
                $platform = $name;
                break;
            }
        }

        $result['details']['platform'] = $platform;

        // HTTP-Check
        $ch = curl_init();
        curl_setopt_array($ch, [
            CURLOPT_URL => $url,
            CURLOPT_RETURNTRANSFER => true,
            CURLOPT_TIMEOUT => $this->timeout,
            CURLOPT_FOLLOWLOCATION => true,
            CURLOPT_NOBODY => true, // HEAD request
            CURLOPT_SSL_VERIFYPEER => false,
        ]);

        curl_exec($ch);
        $httpCode = curl_getinfo($ch, CURLINFO_HTTP_CODE);
        curl_close($ch);

        $result['details']['http_code'] = $httpCode;

        if ($httpCode >= 200 && $httpCode < 400) {
            $result['valid'] = true;
        } else {
            $result['error'] = "URL nicht erreichbar (HTTP $httpCode)";
        }

        return $result;
    }

    /**
     * Generische HTTP-Validierung
     */
    private function validateHttp(string $url, array $result): array
    {
        $ch = curl_init();
        curl_setopt_array($ch, [
            CURLOPT_URL => $url,
            CURLOPT_RETURNTRANSFER => true,
            CURLOPT_TIMEOUT => $this->timeout,
            CURLOPT_FOLLOWLOCATION => true,
            CURLOPT_NOBODY => true,
            CURLOPT_SSL_VERIFYPEER => false,
        ]);

        curl_exec($ch);
        $httpCode = curl_getinfo($ch, CURLINFO_HTTP_CODE);
        $contentType = curl_getinfo($ch, CURLINFO_CONTENT_TYPE);
        $error = curl_error($ch);
        curl_close($ch);

        $result['details']['http_code'] = $httpCode;
        $result['details']['content_type'] = $contentType;

        if ($error) {
            $result['error'] = 'Verbindungsfehler: ' . $error;
            return $result;
        }

        if ($httpCode >= 200 && $httpCode < 400) {
            $result['valid'] = true;
        } else {
            $result['error'] = "URL nicht erreichbar (HTTP $httpCode)";
        }

        return $result;
    }

    /**
     * Schnelle Erreichbarkeitsprüfung
     */
    public function isReachable(string $url): bool
    {
        $ch = curl_init();
        curl_setopt_array($ch, [
            CURLOPT_URL => $url,
            CURLOPT_RETURNTRANSFER => true,
            CURLOPT_TIMEOUT => 5,
            CURLOPT_FOLLOWLOCATION => true,
            CURLOPT_NOBODY => true,
            CURLOPT_SSL_VERIFYPEER => false,
        ]);

        curl_exec($ch);
        $httpCode = curl_getinfo($ch, CURLINFO_HTTP_CODE);
        curl_close($ch);

        return $httpCode >= 200 && $httpCode < 400;
    }
}
