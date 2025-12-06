<?php
/**
 * Repository Management API
 * Handles CRUD operations for repositories
 */

require_once '../../src/ConfigManager.php';
require_once '../../src/Logger.php';
require_once '../../src/GitHandler.php';

header('Content-Type: application/json');

$configManager = new ConfigManager();
$logger = new Logger($configManager);
$gitHandler = new GitHandler($logger, $configManager);

$method = $_SERVER['REQUEST_METHOD'];

// GET - List all repositories or get single repository
if ($method === 'GET') {
    if (isset($_GET['id'])) {
        $repo = $configManager->getRepository($_GET['id']);

        if (!$repo) {
            http_response_code(404);
            echo json_encode(['error' => 'Repository not found']);
            exit;
        }

        // Add current git status if path exists
        if (file_exists($repo['target_path'])) {
            $status = $gitHandler->getStatus($repo['target_path']);
            $repo['git_status'] = $status;
            $repo['current_branch'] = $gitHandler->getCurrentBranch($repo['target_path']);
            $repo['current_commit'] = $gitHandler->getCurrentCommit($repo['target_path']);
        }

        echo json_encode($repo);
    } else {
        $repos = $configManager->getRepositories();

        // Add status for each repo
        foreach ($repos as &$repo) {
            if (file_exists($repo['target_path'])) {
                $repo['exists'] = true;
                $repo['current_branch'] = $gitHandler->getCurrentBranch($repo['target_path']);
            } else {
                $repo['exists'] = false;
            }
        }

        echo json_encode(['repositories' => $repos]);
    }
    exit;
}

// POST - Add new repository
if ($method === 'POST') {
    $input = json_decode(file_get_contents('php://input'), true);

    // Validate required fields
    $required = ['name', 'repo_url', 'target_path', 'branch'];
    foreach ($required as $field) {
        if (empty($input[$field])) {
            http_response_code(400);
            echo json_encode(['error' => "Field '$field' is required"]);
            exit;
        }
    }

    // Validate target path
    $targetPath = rtrim($input['target_path'], '/');

    // Check if target path already exists
    if (file_exists($targetPath)) {
        http_response_code(400);
        echo json_encode(['error' => 'Target path already exists']);
        exit;
    }

    // Validate repository URL format
    if (!filter_var($input['repo_url'], FILTER_VALIDATE_URL)) {
        http_response_code(400);
        echo json_encode(['error' => 'Invalid repository URL']);
        exit;
    }

    // Generate webhook secret
    $webhookSecret = $configManager->generateWebhookSecret();

    // Prepare repository data
    $repoData = [
        'name' => $input['name'],
        'repo_url' => $input['repo_url'],
        'target_path' => $targetPath,
        'branch' => $input['branch'],
        'auto_sync' => $input['auto_sync'] ?? true,
        'status' => 'cloning'
    ];

    // Add repository to config
    $repoId = $configManager->addRepository($repoData);

    if (!$repoId) {
        http_response_code(500);
        echo json_encode(['error' => 'Failed to add repository']);
        exit;
    }

    // Save webhook secret
    $configManager->setWebhookSecret($repoId, $webhookSecret);

    // Clone repository
    $result = $gitHandler->cloneRepository(
        $repoId,
        $input['repo_url'],
        $targetPath,
        $input['branch']
    );

    if ($result['success']) {
        $configManager->updateRepository($repoId, [
            'status' => 'synced',
            'last_sync' => date('Y-m-d H:i:s')
        ]);

        $repo = $configManager->getRepository($repoId);
        $repo['webhook_secret'] = $webhookSecret;
        $repo['webhook_url'] = (isset($_SERVER['HTTPS']) ? 'https' : 'http') .
                               '://' . $_SERVER['HTTP_HOST'] .
                               dirname(dirname($_SERVER['REQUEST_URI'])) . '/webhook.php';

        echo json_encode([
            'success' => true,
            'repository' => $repo
        ]);
    } else {
        $configManager->updateRepository($repoId, ['status' => 'error']);

        http_response_code(500);
        echo json_encode([
            'success' => false,
            'error' => $result['message'],
            'details' => $result['error'] ?? null
        ]);
    }

    exit;
}

// PUT - Update repository
if ($method === 'PUT') {
    $input = json_decode(file_get_contents('php://input'), true);

    if (empty($input['id'])) {
        http_response_code(400);
        echo json_encode(['error' => 'Repository ID is required']);
        exit;
    }

    $repo = $configManager->getRepository($input['id']);

    if (!$repo) {
        http_response_code(404);
        echo json_encode(['error' => 'Repository not found']);
        exit;
    }

    // Prepare updates (only allow certain fields to be updated)
    $allowedFields = ['name', 'branch', 'auto_sync'];
    $updates = [];

    foreach ($allowedFields as $field) {
        if (isset($input[$field])) {
            $updates[$field] = $input[$field];
        }
    }

    if (empty($updates)) {
        http_response_code(400);
        echo json_encode(['error' => 'No valid fields to update']);
        exit;
    }

    $success = $configManager->updateRepository($input['id'], $updates);

    if ($success) {
        $repo = $configManager->getRepository($input['id']);
        echo json_encode([
            'success' => true,
            'repository' => $repo
        ]);
    } else {
        http_response_code(500);
        echo json_encode(['error' => 'Failed to update repository']);
    }

    exit;
}

// DELETE - Delete repository
if ($method === 'DELETE') {
    $input = json_decode(file_get_contents('php://input'), true);

    if (empty($input['id'])) {
        http_response_code(400);
        echo json_encode(['error' => 'Repository ID is required']);
        exit;
    }

    $repo = $configManager->getRepository($input['id']);

    if (!$repo) {
        http_response_code(404);
        echo json_encode(['error' => 'Repository not found']);
        exit;
    }

    // Delete repository from config
    $success = $configManager->deleteRepository($input['id']);

    if ($success) {
        $logger->info($input['id'], "Repository removed from configuration");

        // Optionally delete files if requested
        if (!empty($input['delete_files']) && file_exists($repo['target_path'])) {
            exec('rm -rf ' . escapeshellarg($repo['target_path']));
            $logger->info($input['id'], "Repository files deleted from disk");
        }

        echo json_encode([
            'success' => true,
            'message' => 'Repository deleted'
        ]);
    } else {
        http_response_code(500);
        echo json_encode(['error' => 'Failed to delete repository']);
    }

    exit;
}

// Method not allowed
http_response_code(405);
echo json_encode(['error' => 'Method not allowed']);
