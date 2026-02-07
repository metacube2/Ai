<?php

/**
 * Video Converter Suite - REST API
 */

spl_autoload_register(function ($class) {
    $prefix = 'VideoConverter\\';
    $baseDir = __DIR__ . '/../src/';
    $len = strlen($prefix);
    if (strncmp($prefix, $class, $len) !== 0) return;
    $relative = substr($class, $len);
    $file = $baseDir . str_replace('\\', '/', $relative) . '.php';
    if (file_exists($file)) require $file;
});

header('Content-Type: application/json');
header('Access-Control-Allow-Origin: *');
header('Access-Control-Allow-Methods: GET, POST, PUT, DELETE, OPTIONS');
header('Access-Control-Allow-Headers: Content-Type');

if ($_SERVER['REQUEST_METHOD'] === 'OPTIONS') {
    http_response_code(204);
    exit;
}

$method = $_SERVER['REQUEST_METHOD'];
$path = parse_url($_SERVER['REQUEST_URI'], PHP_URL_PATH);
$segments = array_values(array_filter(explode('/', $path)));

// Page routes (return HTML)
if (empty($segments) || ($segments[0] ?? '') !== 'api') {
    header('Content-Type: text/html; charset=utf-8');
    $page = $segments[0] ?? 'dashboard';
    $templateFile = __DIR__ . '/../templates/' . basename($page) . '.php';
    if (file_exists($templateFile)) {
        $config = require __DIR__ . '/../config/app.php';
        require $templateFile;
    } else {
        $config = require __DIR__ . '/../config/app.php';
        require __DIR__ . '/../templates/dashboard.php';
    }
    exit;
}

// API routes
array_shift($segments); // remove 'api'
$resource = $segments[0] ?? '';
$id = $segments[1] ?? null;
$action = $segments[2] ?? null;

$input = json_decode(file_get_contents('php://input'), true) ?? [];

try {
    $response = match (true) {
        // System
        $resource === 'system' && $method === 'GET' => handleSystem(),

        // Convert
        $resource === 'convert' && $method === 'POST' => handleConvert($input),
        $resource === 'convert' && $action === 'batch' && $method === 'POST' => handleBatchConvert($input),
        $resource === 'upload' && $method === 'POST' => handleUpload(),

        // Jobs
        $resource === 'jobs' && $method === 'GET' && !$id => handleGetJobs(),
        $resource === 'jobs' && $method === 'GET' && $id && $action === 'progress' => handleJobProgress($id),
        $resource === 'jobs' && $method === 'GET' && $id => handleGetJob($id),
        $resource === 'jobs' && $method === 'DELETE' && $id => handleDeleteJob($id),
        $resource === 'jobs' && $action === 'cancel' && $method === 'POST' => handleCancelJob($id),

        // Streams
        $resource === 'streams' && $method === 'GET' && !$id => handleGetStreams(),
        $resource === 'streams' && $method === 'POST' => handleStartStream($input),
        $resource === 'streams' && $method === 'GET' && $id => handleGetStream($id),
        $resource === 'streams' && $method === 'DELETE' && $id => handleStopStream($id),
        $resource === 'streams' && $action === 'switch' && $method === 'POST' => handleSwitchFormat($id, $input),

        // Pipelines
        $resource === 'pipelines' && $method === 'GET' && !$id => handleGetPipelines(),
        $resource === 'pipelines' && $method === 'POST' => handleCreatePipeline($input),
        $resource === 'pipelines' && $method === 'GET' && $id => handleGetPipeline($id),
        $resource === 'pipelines' && $method === 'PUT' && $id => handleUpdatePipeline($id, $input),
        $resource === 'pipelines' && $method === 'DELETE' && $id => handleDeletePipeline($id),
        $resource === 'pipelines' && $action === 'run' && $method === 'POST' => handleRunPipeline($id, $input),
        $resource === 'pipelines' && $action === 'stage' && $method === 'POST' => handleAddStage($id, $input),

        // Queue
        $resource === 'queue' && $method === 'GET' => handleGetQueue(),
        $resource === 'queue' && $method === 'POST' => handleEnqueue($input),
        $resource === 'queue' && $method === 'DELETE' => handleClearQueue(),

        // Formats info
        $resource === 'formats' && $method === 'GET' => handleGetFormats(),
        $resource === 'presets' && $method === 'GET' => handleGetPresets(),
        $resource === 'resolutions' && $method === 'GET' => handleGetResolutions(),

        // Probe
        $resource === 'probe' && $method === 'POST' => handleProbe($input),

        // Downloads
        $resource === 'download' && $method === 'GET' && $id => handleDownload($id),

        default => ['error' => 'Not found', 'status' => 404],
    };

    $status = $response['status'] ?? 200;
    unset($response['status']);
    http_response_code($status);
    echo json_encode($response, JSON_PRETTY_PRINT | JSON_UNESCAPED_SLASHES);

} catch (\Throwable $e) {
    http_response_code(500);
    echo json_encode(['error' => $e->getMessage()]);
}

// ---- Handler Functions ----

function handleSystem(): array
{
    $load = sys_getloadavg();
    $config = require __DIR__ . '/../config/app.php';
    return [
        'app' => $config['app_name'],
        'version' => $config['version'],
        'cpu_load' => $load,
        'memory' => [
            'used' => memory_get_usage(true),
            'peak' => memory_get_peak_usage(true),
        ],
        'disk' => [
            'free' => disk_free_space('/'),
            'total' => disk_total_space('/'),
        ],
        'php_version' => PHP_VERSION,
        'ffmpeg_available' => file_exists($config['ffmpeg']['binary']),
    ];
}

function handleUpload(): array
{
    $config = require __DIR__ . '/../config/app.php';

    if (empty($_FILES['file'])) {
        return ['error' => 'No file uploaded', 'status' => 400];
    }

    $file = $_FILES['file'];
    if ($file['error'] !== UPLOAD_ERR_OK) {
        return ['error' => 'Upload error: ' . $file['error'], 'status' => 400];
    }

    if ($file['size'] > $config['limits']['max_upload_size']) {
        return ['error' => 'File too large', 'status' => 400];
    }

    $uploadDir = $config['storage']['uploads'];
    if (!is_dir($uploadDir)) mkdir($uploadDir, 0755, true);

    $ext = pathinfo($file['name'], PATHINFO_EXTENSION);
    $safeName = bin2hex(random_bytes(8)) . '.' . $ext;
    $destination = $uploadDir . '/' . $safeName;

    if (!move_uploaded_file($file['tmp_name'], $destination)) {
        return ['error' => 'Failed to save file', 'status' => 500];
    }

    $probe = new \VideoConverter\Process\MediaProbe();
    $info = $probe->analyze($destination);

    // Generate thumbnail
    $thumbDir = $config['storage']['thumbnails'];
    if (!is_dir($thumbDir)) mkdir($thumbDir, 0755, true);
    $thumbPath = $thumbDir . '/' . pathinfo($safeName, PATHINFO_FILENAME) . '.jpg';
    $probe->getThumbnail($destination, $thumbPath);

    return [
        'file' => $safeName,
        'path' => $destination,
        'original_name' => $file['name'],
        'size' => $file['size'],
        'info' => $info,
        'thumbnail' => file_exists($thumbPath) ? '/api/thumbnail/' . pathinfo($safeName, PATHINFO_FILENAME) : null,
    ];
}

function handleConvert(array $input): array
{
    $converter = new \VideoConverter\Format\FormatConverter();
    return $converter->convert($input);
}

function handleBatchConvert(array $input): array
{
    $converter = new \VideoConverter\Format\FormatConverter();
    return $converter->batchConvert($input['input_file'] ?? '', $input['formats'] ?? []);
}

function handleGetJobs(): array
{
    $converter = new \VideoConverter\Format\FormatConverter();
    return ['jobs' => $converter->getAllJobs()];
}

function handleGetJob(string $id): array
{
    $converter = new \VideoConverter\Format\FormatConverter();
    $job = $converter->getJob($id);
    return $job ? $job : ['error' => 'Job not found', 'status' => 404];
}

function handleJobProgress(string $id): array
{
    $converter = new \VideoConverter\Format\FormatConverter();
    return $converter->getProgress($id);
}

function handleCancelJob(string $id): array
{
    $converter = new \VideoConverter\Format\FormatConverter();
    return ['success' => $converter->cancelJob($id)];
}

function handleDeleteJob(string $id): array
{
    $converter = new \VideoConverter\Format\FormatConverter();
    return ['success' => $converter->deleteJob($id)];
}

function handleGetStreams(): array
{
    $mgr = new \VideoConverter\Stream\StreamManager();
    return ['streams' => $mgr->getAllStreams(), 'stats' => $mgr->getStats()];
}

function handleStartStream(array $input): array
{
    $mgr = new \VideoConverter\Stream\StreamManager();
    return $mgr->startStream($input);
}

function handleGetStream(string $id): array
{
    $mgr = new \VideoConverter\Stream\StreamManager();
    $stream = $mgr->getStream($id);
    return $stream ?: ['error' => 'Stream not found', 'status' => 404];
}

function handleStopStream(string $id): array
{
    $mgr = new \VideoConverter\Stream\StreamManager();
    return ['success' => $mgr->stopStream($id)];
}

function handleSwitchFormat(string $id, array $input): array
{
    $mgr = new \VideoConverter\Stream\StreamManager();
    return $mgr->switchFormat($id, $input['format'] ?? 'mp4', $input['resolution'] ?? null);
}

function handleGetPipelines(): array
{
    $mgr = new \VideoConverter\Pipeline\PipelineManager();
    return ['pipelines' => $mgr->toArray()];
}

function handleCreatePipeline(array $input): array
{
    $mgr = new \VideoConverter\Pipeline\PipelineManager();
    $pipeline = $mgr->create($input['name'] ?? 'Unnamed Pipeline');

    foreach (($input['stages'] ?? []) as $stageData) {
        $stage = new \VideoConverter\Pipeline\PipelineStage(
            $stageData['type'] ?? 'transcode',
            $stageData['params'] ?? [],
            $stageData['label'] ?? '',
            $stageData['enabled'] ?? true
        );
        $pipeline->addStage($stage);
    }

    $mgr->save();
    return $pipeline->toArray();
}

function handleGetPipeline(string $id): array
{
    $mgr = new \VideoConverter\Pipeline\PipelineManager();
    $pipeline = $mgr->get($id);
    return $pipeline ? $pipeline->toArray() : ['error' => 'Pipeline not found', 'status' => 404];
}

function handleUpdatePipeline(string $id, array $input): array
{
    $mgr = new \VideoConverter\Pipeline\PipelineManager();
    $pipeline = $mgr->get($id);
    if (!$pipeline) return ['error' => 'Pipeline not found', 'status' => 404];

    if (isset($input['stages'])) {
        // Rebuild stages
        $ref = new \ReflectionProperty($pipeline, 'stages');
        $ref->setAccessible(true);
        $ref->setValue($pipeline, []);

        foreach ($input['stages'] as $stageData) {
            $stage = \VideoConverter\Pipeline\PipelineStage::fromArray($stageData);
            $pipeline->addStage($stage);
        }
    }

    $mgr->save();
    return $pipeline->toArray();
}

function handleDeletePipeline(string $id): array
{
    $mgr = new \VideoConverter\Pipeline\PipelineManager();
    return ['success' => $mgr->delete($id)];
}

function handleRunPipeline(string $id, array $input): array
{
    $mgr = new \VideoConverter\Pipeline\PipelineManager();
    $pipeline = $mgr->get($id);
    if (!$pipeline) return ['error' => 'Pipeline not found', 'status' => 404];

    $converter = new \VideoConverter\Format\FormatConverter();
    return $converter->convert([
        'input_file' => $input['input_file'] ?? '',
        'output_format' => $input['output_format'] ?? 'mp4',
        'pipeline' => $pipeline,
    ]);
}

function handleAddStage(string $id, array $input): array
{
    $mgr = new \VideoConverter\Pipeline\PipelineManager();
    $pipeline = $mgr->get($id);
    if (!$pipeline) return ['error' => 'Pipeline not found', 'status' => 404];

    $stage = new \VideoConverter\Pipeline\PipelineStage(
        $input['type'] ?? 'transcode',
        $input['params'] ?? [],
        $input['label'] ?? '',
        $input['enabled'] ?? true
    );
    $pipeline->addStage($stage);
    $mgr->save();

    return $pipeline->toArray();
}

function handleGetQueue(): array
{
    $queue = new \VideoConverter\Queue\JobQueue();
    return ['queue' => $queue->getQueue(), 'stats' => $queue->getStats()];
}

function handleEnqueue(array $input): array
{
    $queue = new \VideoConverter\Queue\JobQueue();
    $queueId = $queue->enqueue($input);
    return ['queue_id' => $queueId, 'position' => count($queue->getWaiting())];
}

function handleClearQueue(): array
{
    $queue = new \VideoConverter\Queue\JobQueue();
    $cleared = $queue->clear();
    return ['cleared' => $cleared];
}

function handleGetFormats(): array
{
    $config = require __DIR__ . '/../config/app.php';
    return $config['formats'];
}

function handleGetPresets(): array
{
    $config = require __DIR__ . '/../config/app.php';
    return $config['presets'];
}

function handleGetResolutions(): array
{
    $config = require __DIR__ . '/../config/app.php';
    return $config['resolutions'];
}

function handleProbe(array $input): array
{
    $probe = new \VideoConverter\Process\MediaProbe();
    return $probe->analyze($input['file'] ?? '');
}

function handleDownload(string $id): array
{
    $converter = new \VideoConverter\Format\FormatConverter();
    $job = $converter->getJob($id);
    if (!$job || !isset($job['output_file']) || !file_exists($job['output_file'])) {
        return ['error' => 'File not found', 'status' => 404];
    }

    header('Content-Type: application/octet-stream');
    header('Content-Disposition: attachment; filename="' . basename($job['output_file']) . '"');
    header('Content-Length: ' . filesize($job['output_file']));
    readfile($job['output_file']);
    exit;
}
