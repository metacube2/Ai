#!/usr/bin/env php
<?php

/**
 * Video Converter Suite - Queue Worker
 *
 * Processes jobs from the queue sequentially.
 * Usage: php bin/queue-worker.php
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

use VideoConverter\Queue\JobQueue;
use VideoConverter\Format\FormatConverter;

$config = require __DIR__ . '/../config/app.php';

echo "=== Video Converter Suite - Queue Worker ===\n";
echo "Max concurrent: {$config['limits']['max_concurrent_jobs']}\n\n";

$queue = new JobQueue();
$converter = new FormatConverter();
$running = 0;

while (true) {
    $queue = new JobQueue(); // Reload state
    $converter = new FormatConverter();

    $activeJobs = array_filter($converter->getAllJobs(), fn($j) => $j['status'] === 'running');
    $running = count($activeJobs);

    if ($running < $config['limits']['max_concurrent_jobs']) {
        $nextJob = $queue->dequeue();
        if ($nextJob) {
            echo "[" . date('H:i:s') . "] Processing: {$nextJob['queue_id']}\n";

            try {
                $result = $converter->convert([
                    'input_file' => $nextJob['input_file'] ?? '',
                    'output_format' => $nextJob['output_format'] ?? 'mp4',
                    'preset' => $nextJob['preset'] ?? 'balanced',
                    'resolution' => $nextJob['resolution'] ?? null,
                ]);

                if (isset($result['error'])) {
                    $queue->fail($nextJob['queue_id'], $result['error']);
                    echo "[" . date('H:i:s') . "] Failed: {$result['error']}\n";
                } else {
                    $queue->complete($nextJob['queue_id'], $result);
                    echo "[" . date('H:i:s') . "] Started job: {$result['id']}\n";
                }
            } catch (\Throwable $e) {
                $queue->fail($nextJob['queue_id'], $e->getMessage());
                echo "[" . date('H:i:s') . "] Error: {$e->getMessage()}\n";
            }
        }
    }

    sleep(2);
}
