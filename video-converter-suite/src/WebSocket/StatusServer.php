<?php

namespace VideoConverter\WebSocket;

use Ratchet\MessageComponentInterface;
use Ratchet\ConnectionInterface;
use VideoConverter\Format\FormatConverter;
use VideoConverter\Stream\StreamManager;
use VideoConverter\Pipeline\PipelineManager;
use VideoConverter\Queue\JobQueue;

class StatusServer implements MessageComponentInterface
{
    protected \SplObjectStorage $clients;
    private FormatConverter $converter;
    private StreamManager $streamManager;
    private PipelineManager $pipelineManager;
    private JobQueue $queue;

    public function __construct()
    {
        $this->clients = new \SplObjectStorage();
        $this->converter = new FormatConverter();
        $this->streamManager = new StreamManager();
        $this->pipelineManager = new PipelineManager();
        $this->queue = new JobQueue();
    }

    public function onOpen(ConnectionInterface $conn): void
    {
        $this->clients->attach($conn);
        $conn->send(json_encode([
            'type' => 'connected',
            'message' => 'Connected to Video Converter Suite',
            'client_id' => spl_object_id($conn),
        ]));
    }

    public function onMessage(ConnectionInterface $from, $msg): void
    {
        $data = json_decode($msg, true);
        if (!$data || !isset($data['action'])) return;

        $response = match ($data['action']) {
            'get_status' => $this->getFullStatus(),
            'get_jobs' => ['type' => 'jobs', 'data' => $this->converter->getAllJobs()],
            'get_streams' => ['type' => 'streams', 'data' => $this->streamManager->getAllStreams()],
            'get_pipelines' => ['type' => 'pipelines', 'data' => $this->pipelineManager->toArray()],
            'get_queue' => ['type' => 'queue', 'data' => $this->queue->getQueue()],
            'get_progress' => $this->getJobProgress($data['job_id'] ?? ''),
            'start_stream' => $this->handleStartStream($data),
            'stop_stream' => $this->handleStopStream($data['stream_id'] ?? ''),
            'switch_format' => $this->handleSwitchFormat($data),
            default => ['type' => 'error', 'message' => 'Unknown action'],
        };

        $from->send(json_encode($response));
    }

    public function onClose(ConnectionInterface $conn): void
    {
        $this->clients->detach($conn);
    }

    public function onError(ConnectionInterface $conn, \Exception $e): void
    {
        $conn->send(json_encode([
            'type' => 'error',
            'message' => $e->getMessage(),
        ]));
        $conn->close();
    }

    public function broadcastStatus(): void
    {
        $status = $this->getFullStatus();
        $json = json_encode($status);

        foreach ($this->clients as $client) {
            $client->send($json);
        }
    }

    private function getFullStatus(): array
    {
        // Reload state
        $this->converter = new FormatConverter();
        $this->streamManager = new StreamManager();
        $this->pipelineManager = new PipelineManager();
        $this->queue = new JobQueue();

        $jobs = $this->converter->getAllJobs();
        $runningJobs = array_filter($jobs, fn($j) => $j['status'] === 'running');

        $progressData = [];
        foreach ($runningJobs as $job) {
            $progressData[$job['id']] = $this->converter->getProgress($job['id']);
        }

        return [
            'type' => 'status',
            'timestamp' => date('c'),
            'system' => $this->getSystemStats(),
            'jobs' => $jobs,
            'progress' => $progressData,
            'streams' => $this->streamManager->getAllStreams(),
            'pipelines' => $this->pipelineManager->toArray(),
            'queue' => $this->queue->getStats(),
        ];
    }

    private function getJobProgress(string $jobId): array
    {
        $this->converter = new FormatConverter();
        return [
            'type' => 'progress',
            'job_id' => $jobId,
            'data' => $this->converter->getProgress($jobId),
        ];
    }

    private function handleStartStream(array $data): array
    {
        $this->streamManager = new StreamManager();
        $result = $this->streamManager->startStream($data);
        return ['type' => 'stream_started', 'data' => $result];
    }

    private function handleStopStream(string $streamId): array
    {
        $this->streamManager = new StreamManager();
        $success = $this->streamManager->stopStream($streamId);
        return ['type' => 'stream_stopped', 'success' => $success, 'stream_id' => $streamId];
    }

    private function handleSwitchFormat(array $data): array
    {
        $this->streamManager = new StreamManager();
        $result = $this->streamManager->switchFormat(
            $data['stream_id'] ?? '',
            $data['format'] ?? 'mp4',
            $data['resolution'] ?? null
        );
        return ['type' => 'format_switched', 'data' => $result];
    }

    private function getSystemStats(): array
    {
        $load = sys_getloadavg();
        $memInfo = $this->getMemoryInfo();

        return [
            'cpu_load' => $load[0] ?? 0,
            'memory_used' => $memInfo['used'] ?? 0,
            'memory_total' => $memInfo['total'] ?? 0,
            'memory_percent' => $memInfo['percent'] ?? 0,
            'disk_free' => disk_free_space('/'),
            'disk_total' => disk_total_space('/'),
            'uptime' => (int)(file_exists('/proc/uptime')
                ? (float)explode(' ', file_get_contents('/proc/uptime'))[0]
                : 0),
        ];
    }

    private function getMemoryInfo(): array
    {
        if (!file_exists('/proc/meminfo')) {
            return ['total' => 0, 'used' => 0, 'percent' => 0];
        }
        $content = file_get_contents('/proc/meminfo');
        preg_match('/MemTotal:\s+(\d+)/', $content, $total);
        preg_match('/MemAvailable:\s+(\d+)/', $content, $available);
        $t = (int)($total[1] ?? 0) * 1024;
        $a = (int)($available[1] ?? 0) * 1024;
        $u = $t - $a;
        return [
            'total' => $t,
            'used' => $u,
            'percent' => $t > 0 ? round(($u / $t) * 100, 1) : 0,
        ];
    }
}
