<?php
/**
 * Log API
 * Retrieves log entries
 */

require_once '../../src/ConfigManager.php';
require_once '../../src/Logger.php';

header('Content-Type: application/json');

if ($_SERVER['REQUEST_METHOD'] !== 'GET') {
    http_response_code(405);
    echo json_encode(['error' => 'Method not allowed']);
    exit;
}

$configManager = new ConfigManager();
$logger = new Logger($configManager);

// Get query parameters
$limit = isset($_GET['limit']) ? (int)$_GET['limit'] : 100;
$offset = isset($_GET['offset']) ? (int)$_GET['offset'] : 0;
$repoId = $_GET['repo_id'] ?? null;
$type = $_GET['type'] ?? null;

// Get logs based on filters
if ($repoId) {
    $logs = $logger->getByRepository($repoId, $limit);
} elseif ($type) {
    $logs = $logger->getByType($type, $limit);
} else {
    $logs = $logger->getAll($limit, $offset);
}

// Get statistics
$stats = $logger->getStats();

echo json_encode([
    'success' => true,
    'logs' => $logs,
    'stats' => $stats,
    'count' => count($logs)
]);
