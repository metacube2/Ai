<?php
/**
 * Rollback API
 * Reverts repository to a specific commit
 */

require_once '../../src/ConfigManager.php';
require_once '../../src/Logger.php';
require_once '../../src/GitHandler.php';

header('Content-Type: application/json');

$configManager = new ConfigManager();
$logger = new Logger($configManager);
$gitHandler = new GitHandler($logger, $configManager);

$method = $_SERVER['REQUEST_METHOD'];

// GET - Get commit history
if ($method === 'GET') {
    if (empty($_GET['repo_id'])) {
        http_response_code(400);
        echo json_encode(['error' => 'Repository ID is required']);
        exit;
    }

    $repo = $configManager->getRepository($_GET['repo_id']);

    if (!$repo) {
        http_response_code(404);
        echo json_encode(['error' => 'Repository not found']);
        exit;
    }

    if (!file_exists($repo['target_path'])) {
        http_response_code(400);
        echo json_encode(['error' => 'Repository path does not exist']);
        exit;
    }

    $limit = isset($_GET['limit']) ? (int)$_GET['limit'] : 20;
    $commits = $gitHandler->getCommitHistory($repo['target_path'], $limit);

    echo json_encode([
        'success' => true,
        'commits' => $commits
    ]);
    exit;
}

// POST - Perform rollback
if ($method === 'POST') {
    $input = json_decode(file_get_contents('php://input'), true);

    if (empty($input['repo_id'])) {
        http_response_code(400);
        echo json_encode(['error' => 'Repository ID is required']);
        exit;
    }

    if (empty($input['commit_hash'])) {
        http_response_code(400);
        echo json_encode(['error' => 'Commit hash is required']);
        exit;
    }

    $repo = $configManager->getRepository($input['repo_id']);

    if (!$repo) {
        http_response_code(404);
        echo json_encode(['error' => 'Repository not found']);
        exit;
    }

    if (!file_exists($repo['target_path'])) {
        http_response_code(400);
        echo json_encode(['error' => 'Repository path does not exist']);
        exit;
    }

    // Perform revert
    $result = $gitHandler->revert(
        $repo['id'],
        $repo['target_path'],
        $input['commit_hash']
    );

    if ($result['success']) {
        echo json_encode([
            'success' => true,
            'message' => $result['message'],
            'output' => $result['output']
        ]);
    } else {
        http_response_code(400);
        echo json_encode([
            'success' => false,
            'message' => $result['message'],
            'error' => $result['error'] ?? null
        ]);
    }
    exit;
}

// Method not allowed
http_response_code(405);
echo json_encode(['error' => 'Method not allowed']);
