<?php
/**
 * Manual Sync API
 * Triggers manual sync for a repository
 */

require_once '../../src/ConfigManager.php';
require_once '../../src/Logger.php';
require_once '../../src/GitHandler.php';

header('Content-Type: application/json');

if ($_SERVER['REQUEST_METHOD'] !== 'POST') {
    http_response_code(405);
    echo json_encode(['error' => 'Method not allowed']);
    exit;
}

$input = json_decode(file_get_contents('php://input'), true);

if (empty($input['repo_id'])) {
    http_response_code(400);
    echo json_encode(['error' => 'Repository ID is required']);
    exit;
}

$configManager = new ConfigManager();
$logger = new Logger($configManager);
$gitHandler = new GitHandler($logger, $configManager);

$repo = $configManager->getRepository($input['repo_id']);

if (!$repo) {
    http_response_code(404);
    echo json_encode(['error' => 'Repository not found']);
    exit;
}

// Check if repository path exists
if (!file_exists($repo['target_path'])) {
    http_response_code(400);
    echo json_encode(['error' => 'Repository path does not exist. Please clone first.']);
    exit;
}

// Perform sync
$logger->info($repo['id'], "Manual sync triggered");

$result = $gitHandler->pull(
    $repo['id'],
    $repo['target_path'],
    $repo['branch']
);

if ($result['success']) {
    echo json_encode([
        'success' => true,
        'message' => $result['message'],
        'files_changed' => $result['files_changed'] ?? 0,
        'output' => $result['output']
    ]);
} else {
    http_response_code(400);
    echo json_encode([
        'success' => false,
        'message' => $result['message'],
        'conflict' => $result['conflict'] ?? false,
        'error' => $result['error'] ?? null,
        'output' => $result['output'] ?? null
    ]);
}
