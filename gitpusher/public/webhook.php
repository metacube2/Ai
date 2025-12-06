<?php
/**
 * GitHub Webhook Endpoint
 * Receives push events from GitHub and triggers sync
 */

require_once '../src/ConfigManager.php';
require_once '../src/Logger.php';
require_once '../src/GitHandler.php';

// Set JSON response header
header('Content-Type: application/json');

// Only allow POST requests
if ($_SERVER['REQUEST_METHOD'] !== 'POST') {
    http_response_code(405);
    echo json_encode(['error' => 'Method not allowed']);
    exit;
}

// Get payload
$payload = file_get_contents('php://input');
$data = json_decode($payload, true);

if (json_last_error() !== JSON_ERROR_NONE) {
    http_response_code(400);
    echo json_encode(['error' => 'Invalid JSON payload']);
    exit;
}

// Initialize classes
$configManager = new ConfigManager();
$logger = new Logger($configManager);
$gitHandler = new GitHandler($logger, $configManager);

// Get repository URL from payload
$repoUrl = $data['repository']['clone_url'] ?? null;

if (!$repoUrl) {
    http_response_code(400);
    echo json_encode(['error' => 'Repository URL not found in payload']);
    exit;
}

// Find matching repository in config
$repos = $configManager->getRepositories();
$matchedRepo = null;

foreach ($repos as $repo) {
    if ($repo['repo_url'] === $repoUrl) {
        $matchedRepo = $repo;
        break;
    }
}

if (!$matchedRepo) {
    http_response_code(404);
    $logger->warning('webhook', "Webhook received for unknown repository: $repoUrl");
    echo json_encode(['error' => 'Repository not configured']);
    exit;
}

// Verify webhook signature if secret is configured
$webhookSecret = $configManager->getWebhookSecret($matchedRepo['id']);

if ($webhookSecret) {
    $signature = $_SERVER['HTTP_X_HUB_SIGNATURE_256'] ?? '';

    if (empty($signature)) {
        http_response_code(401);
        $logger->error($matchedRepo['id'], "Webhook signature missing");
        echo json_encode(['error' => 'Signature required']);
        exit;
    }

    $expectedSignature = 'sha256=' . hash_hmac('sha256', $payload, $webhookSecret);

    if (!hash_equals($expectedSignature, $signature)) {
        http_response_code(401);
        $logger->error($matchedRepo['id'], "Invalid webhook signature");
        echo json_encode(['error' => 'Invalid signature']);
        exit;
    }
}

// Check if the push is for the configured branch
$ref = $data['ref'] ?? '';
$pushedBranch = str_replace('refs/heads/', '', $ref);

if ($pushedBranch !== $matchedRepo['branch']) {
    $logger->info($matchedRepo['id'], "Ignoring push to branch '$pushedBranch' (configured: '{$matchedRepo['branch']}')");
    echo json_encode([
        'success' => true,
        'message' => 'Ignored - different branch',
        'pushed_branch' => $pushedBranch,
        'configured_branch' => $matchedRepo['branch']
    ]);
    exit;
}

// Log webhook received
$commits = $data['commits'] ?? [];
$logger->info($matchedRepo['id'], "Webhook received: " . count($commits) . " commits pushed", [
    'pusher' => $data['pusher']['name'] ?? 'unknown',
    'branch' => $pushedBranch
]);

// Perform git pull
$result = $gitHandler->pull(
    $matchedRepo['id'],
    $matchedRepo['target_path'],
    $matchedRepo['branch']
);

// Return result
if ($result['success']) {
    http_response_code(200);
    echo json_encode([
        'success' => true,
        'message' => $result['message'],
        'files_changed' => $result['files_changed'] ?? 0
    ]);
} else {
    // Still return 200 to GitHub, but log the error
    http_response_code(200);
    echo json_encode([
        'success' => false,
        'message' => $result['message'],
        'conflict' => $result['conflict'] ?? false
    ]);
}
