<?php
/**
 * Stripe Webhook Endpoint
 *
 * URL: /api/stripe-webhook.php
 * Konfigurieren Sie diesen Endpoint in Ihrem Stripe Dashboard
 */

// Keine Session, keine Ausgabe vor JSON
error_reporting(0);
ini_set('display_errors', 0);

require_once dirname(__DIR__) . '/vendor/autoload.php';

if (file_exists(dirname(__DIR__) . '/src/bootstrap.php')) {
    require_once dirname(__DIR__) . '/src/bootstrap.php';
}

use AuroraLivecam\Billing\WebhookHandler;

// Nur POST erlaubt
if ($_SERVER['REQUEST_METHOD'] !== 'POST') {
    http_response_code(405);
    echo json_encode(['error' => 'Method not allowed']);
    exit;
}

// Payload lesen
$payload = file_get_contents('php://input');
$signature = $_SERVER['HTTP_STRIPE_SIGNATURE'] ?? '';

if (empty($payload)) {
    http_response_code(400);
    echo json_encode(['error' => 'Empty payload']);
    exit;
}

// Webhook verarbeiten
try {
    $handler = new WebhookHandler();
    $result = $handler->handle($payload, $signature);

    if ($result['success']) {
        http_response_code(200);
    } else {
        http_response_code(400);
    }

    header('Content-Type: application/json');
    echo json_encode($result);

} catch (\Exception $e) {
    error_log('Stripe Webhook Error: ' . $e->getMessage());
    http_response_code(500);
    echo json_encode(['error' => 'Internal server error']);
}
