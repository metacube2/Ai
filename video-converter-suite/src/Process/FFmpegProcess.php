<?php

namespace VideoConverter\Process;

class FFmpegProcess
{
    private string $command;
    private ?int $pid = null;
    private string $logFile;
    private string $progressFile;
    private float $duration = 0;
    private string $status = 'pending';
    private array $outputLines = [];

    public function __construct(string $command, string $jobId)
    {
        $config = require __DIR__ . '/../../config/app.php';
        $this->command = $command;
        $logDir = $config['storage']['logs'];
        if (!is_dir($logDir)) mkdir($logDir, 0755, true);
        $this->logFile = $logDir . "/ffmpeg_{$jobId}.log";
        $this->progressFile = $logDir . "/progress_{$jobId}.txt";
    }

    public function start(): bool
    {
        $cmd = $this->command
            . " -progress " . escapeshellarg($this->progressFile)
            . " -stats_period 0.5"
            . " 2>" . escapeshellarg($this->logFile)
            . " & echo $!";

        $output = [];
        exec($cmd, $output);
        $this->pid = (int)($output[0] ?? 0);

        if ($this->pid > 0) {
            $this->status = 'running';
            return true;
        }
        $this->status = 'error';
        return false;
    }

    public function stop(): void
    {
        if ($this->pid && $this->isRunning()) {
            posix_kill($this->pid, SIGTERM);
            usleep(500000);
            if ($this->isRunning()) {
                posix_kill($this->pid, SIGKILL);
            }
        }
        $this->status = 'stopped';
    }

    public function pause(): void
    {
        if ($this->pid && $this->isRunning()) {
            posix_kill($this->pid, SIGSTOP);
            $this->status = 'paused';
        }
    }

    public function resume(): void
    {
        if ($this->pid) {
            posix_kill($this->pid, SIGCONT);
            $this->status = 'running';
        }
    }

    public function isRunning(): bool
    {
        if (!$this->pid) return false;
        return posix_kill($this->pid, 0);
    }

    public function getProgress(): array
    {
        $progress = [
            'percent' => 0,
            'frame' => 0,
            'fps' => 0,
            'speed' => '0x',
            'time' => '00:00:00.00',
            'bitrate' => '0kbits/s',
            'size' => '0kB',
        ];

        if (!file_exists($this->progressFile)) return $progress;

        $content = file_get_contents($this->progressFile);
        $lines = explode("\n", $content);

        foreach ($lines as $line) {
            $line = trim($line);
            if (str_contains($line, '=')) {
                [$key, $value] = explode('=', $line, 2);
                $key = trim($key);
                $value = trim($value);

                switch ($key) {
                    case 'frame': $progress['frame'] = (int)$value; break;
                    case 'fps': $progress['fps'] = (float)$value; break;
                    case 'speed': $progress['speed'] = $value; break;
                    case 'out_time': $progress['time'] = $value; break;
                    case 'total_size': $progress['size'] = $this->formatBytes((int)$value); break;
                    case 'bitrate': $progress['bitrate'] = $value; break;
                    case 'progress':
                        if ($value === 'end') $progress['percent'] = 100;
                        break;
                }
            }
        }

        if ($this->duration > 0 && $progress['percent'] < 100) {
            $currentTime = $this->timeToSeconds($progress['time']);
            $progress['percent'] = min(99, round(($currentTime / $this->duration) * 100, 1));
        }

        return $progress;
    }

    public function getLog(int $lines = 50): string
    {
        if (!file_exists($this->logFile)) return '';
        $all = file($this->logFile);
        return implode('', array_slice($all, -$lines));
    }

    public function setDuration(float $duration): void
    {
        $this->duration = $duration;
    }

    public function getPid(): ?int { return $this->pid; }
    public function getStatus(): string { return $this->status; }
    public function getCommand(): string { return $this->command; }

    private function timeToSeconds(string $time): float
    {
        $parts = explode(':', $time);
        if (count($parts) !== 3) return 0;
        return (int)$parts[0] * 3600 + (int)$parts[1] * 60 + (float)$parts[2];
    }

    private function formatBytes(int $bytes): string
    {
        $units = ['B', 'KB', 'MB', 'GB'];
        $i = 0;
        $size = (float)$bytes;
        while ($size >= 1024 && $i < count($units) - 1) {
            $size /= 1024;
            $i++;
        }
        return round($size, 1) . $units[$i];
    }
}
