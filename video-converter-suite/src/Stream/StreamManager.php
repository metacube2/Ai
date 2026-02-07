<?php

namespace VideoConverter\Stream;

use VideoConverter\Process\FFmpegProcess;

class StreamManager
{
    private array $activeStreams = [];
    private string $stateFile;

    public function __construct()
    {
        $this->stateFile = __DIR__ . '/../../storage/temp/streams.json';
        $this->load();
    }

    private function load(): void
    {
        if (file_exists($this->stateFile)) {
            $this->activeStreams = json_decode(file_get_contents($this->stateFile), true) ?: [];
        }
    }

    private function save(): void
    {
        $dir = dirname($this->stateFile);
        if (!is_dir($dir)) mkdir($dir, 0755, true);
        file_put_contents($this->stateFile, json_encode($this->activeStreams, JSON_PRETTY_PRINT));
    }

    public function startStream(array $params): array
    {
        $id = bin2hex(random_bytes(8));
        $config = require __DIR__ . '/../../config/app.php';

        $inputUrl = $params['input_url'] ?? '';
        $outputFormat = $params['output_format'] ?? 'mp4';
        $resolution = $params['resolution'] ?? null;
        $preset = $params['preset'] ?? 'fast';

        $formatConfig = $config['formats']['video'][$outputFormat] ?? $config['formats']['video']['mp4'];
        $presetConfig = $config['presets'][$preset] ?? $config['presets']['fast'];

        $outputDir = $config['storage']['outputs'];
        $outputFile = "{$outputDir}/stream_{$id}.{$formatConfig['ext']}";

        $cmd = $config['ffmpeg']['binary'] . " -y";

        // Input
        if (str_starts_with($inputUrl, 'rtmp://') || str_starts_with($inputUrl, 'rtsp://')) {
            $cmd .= " -re";
        }
        $cmd .= " -i " . escapeshellarg($inputUrl);

        // Video codec
        $cmd .= " -c:v " . escapeshellarg($formatConfig['codec']);
        $cmd .= " -preset " . escapeshellarg($presetConfig['preset']);
        $cmd .= " -crf " . (int)$presetConfig['crf'];

        // Resolution
        if ($resolution && isset($config['resolutions'][$resolution])) {
            $res = $config['resolutions'][$resolution];
            $cmd .= " -vf scale={$res['width']}:{$res['height']}";
        }

        // Audio
        $audioCodec = $params['audio_codec'] ?? 'aac';
        $audioBitrate = $params['audio_bitrate'] ?? '128k';
        $cmd .= " -c:a " . escapeshellarg($audioCodec);
        $cmd .= " -b:a " . escapeshellarg($audioBitrate);

        $cmd .= " " . escapeshellarg($outputFile);

        $process = new FFmpegProcess($cmd, $id);

        $stream = [
            'id' => $id,
            'input_url' => $inputUrl,
            'output_file' => $outputFile,
            'output_format' => $outputFormat,
            'resolution' => $resolution,
            'preset' => $preset,
            'status' => 'starting',
            'pid' => null,
            'command' => $cmd,
            'started_at' => date('c'),
        ];

        if ($process->start()) {
            $stream['status'] = 'running';
            $stream['pid'] = $process->getPid();
        } else {
            $stream['status'] = 'error';
        }

        $this->activeStreams[$id] = $stream;
        $this->save();

        return $stream;
    }

    public function stopStream(string $id): bool
    {
        if (!isset($this->activeStreams[$id])) return false;

        $stream = $this->activeStreams[$id];
        if ($stream['pid']) {
            posix_kill($stream['pid'], SIGTERM);
            usleep(500000);
            if (posix_kill($stream['pid'], 0)) {
                posix_kill($stream['pid'], SIGKILL);
            }
        }

        $this->activeStreams[$id]['status'] = 'stopped';
        $this->activeStreams[$id]['stopped_at'] = date('c');
        $this->save();
        return true;
    }

    public function switchFormat(string $id, string $newFormat, ?string $resolution = null): array
    {
        if (!isset($this->activeStreams[$id])) {
            return ['error' => 'Stream not found'];
        }

        $oldStream = $this->activeStreams[$id];
        $this->stopStream($id);

        // Start new stream with same input but different output format
        return $this->startStream([
            'input_url' => $oldStream['input_url'],
            'output_format' => $newFormat,
            'resolution' => $resolution ?? $oldStream['resolution'],
            'preset' => $oldStream['preset'],
            'audio_codec' => 'aac',
        ]);
    }

    public function getStream(string $id): ?array
    {
        $this->refreshStatus($id);
        return $this->activeStreams[$id] ?? null;
    }

    public function getAllStreams(): array
    {
        foreach (array_keys($this->activeStreams) as $id) {
            $this->refreshStatus($id);
        }
        return array_values($this->activeStreams);
    }

    public function getActiveStreams(): array
    {
        return array_values(array_filter($this->getAllStreams(), fn($s) => $s['status'] === 'running'));
    }

    private function refreshStatus(string $id): void
    {
        if (!isset($this->activeStreams[$id])) return;
        $stream = &$this->activeStreams[$id];

        if ($stream['status'] === 'running' && $stream['pid']) {
            if (!posix_kill($stream['pid'], 0)) {
                $stream['status'] = 'completed';
                $stream['completed_at'] = date('c');
                $this->save();
            }
        }
    }

    public function deleteStream(string $id): bool
    {
        if (isset($this->activeStreams[$id])) {
            if ($this->activeStreams[$id]['status'] === 'running') {
                $this->stopStream($id);
            }
            unset($this->activeStreams[$id]);
            $this->save();
            return true;
        }
        return false;
    }

    public function getStats(): array
    {
        $all = $this->getAllStreams();
        return [
            'total' => count($all),
            'running' => count(array_filter($all, fn($s) => $s['status'] === 'running')),
            'completed' => count(array_filter($all, fn($s) => $s['status'] === 'completed')),
            'errors' => count(array_filter($all, fn($s) => $s['status'] === 'error')),
        ];
    }
}
