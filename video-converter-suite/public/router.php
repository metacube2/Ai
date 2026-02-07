<?php

/**
 * Video Converter Suite - Front Controller / Router
 *
 * Usage: php -S 0.0.0.0:8080 -t public public/router.php
 */

// Serve static files directly
$uri = urldecode(parse_url($_SERVER['REQUEST_URI'], PHP_URL_PATH));
if ($uri !== '/' && file_exists(__DIR__ . $uri)) {
    return false;
}

require_once __DIR__ . '/api.php';
