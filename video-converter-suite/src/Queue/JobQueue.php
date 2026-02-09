<?php

namespace VideoConverter\Queue;

class JobQueue
{
    private string $queueFile;
    private array $queue = [];

    public function __construct()
    {
        $this->queueFile = __DIR__ . '/../../storage/temp/queue.json';
        $this->load();
    }

    private function load(): void
    {
        if (file_exists($this->queueFile)) {
            $this->queue = json_decode(file_get_contents($this->queueFile), true) ?: [];
        }
    }

    private function save(): void
    {
        $dir = dirname($this->queueFile);
        if (!is_dir($dir)) mkdir($dir, 0755, true);
        file_put_contents($this->queueFile, json_encode($this->queue, JSON_PRETTY_PRINT));
    }

    public function enqueue(array $job): string
    {
        $id = bin2hex(random_bytes(8));
        $job['queue_id'] = $id;
        $job['queued_at'] = date('c');
        $job['queue_status'] = 'waiting';
        $job['priority'] = $job['priority'] ?? 5;

        $this->queue[] = $job;

        // Sort by priority (lower = higher priority)
        usort($this->queue, fn($a, $b) => ($a['priority'] ?? 5) <=> ($b['priority'] ?? 5));

        $this->save();
        return $id;
    }

    public function dequeue(): ?array
    {
        foreach ($this->queue as &$job) {
            if ($job['queue_status'] === 'waiting') {
                $job['queue_status'] = 'processing';
                $job['started_at'] = date('c');
                $this->save();
                return $job;
            }
        }
        return null;
    }

    public function complete(string $queueId, array $result = []): void
    {
        foreach ($this->queue as &$job) {
            if ($job['queue_id'] === $queueId) {
                $job['queue_status'] = 'completed';
                $job['completed_at'] = date('c');
                $job['result'] = $result;
                break;
            }
        }
        $this->save();
    }

    public function fail(string $queueId, string $error): void
    {
        foreach ($this->queue as &$job) {
            if ($job['queue_id'] === $queueId) {
                $job['queue_status'] = 'failed';
                $job['failed_at'] = date('c');
                $job['error'] = $error;
                break;
            }
        }
        $this->save();
    }

    public function getQueue(): array { return $this->queue; }

    public function getWaiting(): array
    {
        return array_values(array_filter($this->queue, fn($j) => $j['queue_status'] === 'waiting'));
    }

    public function getProcessing(): array
    {
        return array_values(array_filter($this->queue, fn($j) => $j['queue_status'] === 'processing'));
    }

    public function clear(string $status = 'completed'): int
    {
        $before = count($this->queue);
        $this->queue = array_values(array_filter($this->queue, fn($j) => $j['queue_status'] !== $status));
        $this->save();
        return $before - count($this->queue);
    }

    public function getStats(): array
    {
        $stats = ['waiting' => 0, 'processing' => 0, 'completed' => 0, 'failed' => 0];
        foreach ($this->queue as $job) {
            $status = $job['queue_status'] ?? 'waiting';
            $stats[$status] = ($stats[$status] ?? 0) + 1;
        }
        return $stats;
    }
}
