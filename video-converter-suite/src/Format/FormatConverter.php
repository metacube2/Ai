<?php

namespace VideoConverter\Format;

use VideoConverter\Process\FFmpegProcess;
use VideoConverter\Process\MediaProbe;
use VideoConverter\Pipeline\Pipeline;

class FormatConverter
{
    private array $jobs = [];
    private string $stateFile;
    private MediaProbe $probe;

    public function __construct()
    {
        $this->stateFile = __DIR__ . '/../../storage/temp/jobs.json';
        $this->probe = new MediaProbe();
        $this->load();
    }

    private function load(): void
    {
        if (file_exists($this->stateFile)) {
            $this->jobs = json_decode(file_get_contents($this->stateFile), true) ?: [];
        }
    }

    private function save(): void
    {
        $dir = dirname($this->stateFile);
        if (!is_dir($dir)) mkdir($dir, 0755, true);
        file_put_contents($this->stateFile, json_encode($this->jobs, JSON_PRETTY_PRINT));
    }

    public function convert(array $params): array
    {
        $config = require __DIR__ . '/../../config/app.php';

        $inputFile = $params['input_file'] ?? '';
        $outputFormat = $params['output_format'] ?? 'mp4';
        $preset = $params['preset'] ?? 'balanced';
        $resolution = $params['resolution'] ?? null;
        $customPipeline = $params['pipeline'] ?? null;

        if (!file_exists($inputFile)) {
            return ['error' => 'Input file not found'];
        }

        $id = bin2hex(random_bytes(8));
        $formatConfig = $config['formats']['video'][$outputFormat]
            ?? $config['formats']['audio'][$outputFormat]
            ?? null;

        if (!$formatConfig) {
            return ['error' => "Unknown format: {$outputFormat}"];
        }

        $inputInfo = $this->probe->analyze($inputFile);
        $baseName = pathinfo($inputFile, PATHINFO_FILENAME);
        $outputFile = $config['storage']['outputs'] . "/{$baseName}_{$id}.{$formatConfig['ext']}";

        // Build command
        if ($customPipeline instanceof Pipeline) {
            $cmd = $customPipeline->buildFFmpegCommand($inputFile, $outputFile);
        } else {
            $cmd = $this->buildCommand($inputFile, $outputFile, $outputFormat, $preset, $resolution, $params);
        }

        $process = new FFmpegProcess($cmd, $id);
        if (isset($inputInfo['duration'])) {
            $process->setDuration($inputInfo['duration']);
        }

        // Generate thumbnail
        $thumbPath = $config['storage']['thumbnails'] . "/{$id}.jpg";
        $this->probe->getThumbnail($inputFile, $thumbPath);

        $job = [
            'id' => $id,
            'input_file' => $inputFile,
            'input_info' => $inputInfo,
            'output_file' => $outputFile,
            'output_format' => $outputFormat,
            'preset' => $preset,
            'resolution' => $resolution,
            'thumbnail' => file_exists($thumbPath) ? $thumbPath : null,
            'status' => 'starting',
            'pid' => null,
            'command' => $cmd,
            'created_at' => date('c'),
        ];

        if ($process->start()) {
            $job['status'] = 'running';
            $job['pid'] = $process->getPid();
        } else {
            $job['status'] = 'error';
            $job['error'] = 'Failed to start FFmpeg process';
        }

        $this->jobs[$id] = $job;
        $this->save();

        return $job;
    }

    public function batchConvert(string $inputFile, array $formats): array
    {
        $results = [];
        foreach ($formats as $format => $settings) {
            $params = array_merge(
                ['input_file' => $inputFile, 'output_format' => $format],
                $settings
            );
            $results[$format] = $this->convert($params);
        }
        return $results;
    }

    public function getJob(string $id): ?array
    {
        $this->refreshJob($id);
        return $this->jobs[$id] ?? null;
    }

    public function getAllJobs(): array
    {
        foreach (array_keys($this->jobs) as $id) {
            $this->refreshJob($id);
        }
        return array_values($this->jobs);
    }

    public function cancelJob(string $id): bool
    {
        if (!isset($this->jobs[$id])) return false;

        $job = $this->jobs[$id];
        if ($job['pid'] && $job['status'] === 'running') {
            posix_kill($job['pid'], SIGTERM);
            $this->jobs[$id]['status'] = 'cancelled';
            $this->save();
            return true;
        }
        return false;
    }

    public function deleteJob(string $id): bool
    {
        if (isset($this->jobs[$id])) {
            $this->cancelJob($id);
            // Clean up output file
            if (isset($this->jobs[$id]['output_file']) && file_exists($this->jobs[$id]['output_file'])) {
                unlink($this->jobs[$id]['output_file']);
            }
            unset($this->jobs[$id]);
            $this->save();
            return true;
        }
        return false;
    }

    public function getProgress(string $id): array
    {
        if (!isset($this->jobs[$id])) {
            return ['error' => 'Job not found'];
        }

        $config = require __DIR__ . '/../../config/app.php';
        $progressFile = $config['storage']['logs'] . "/progress_{$id}.txt";

        $progress = ['percent' => 0, 'fps' => 0, 'speed' => '0x', 'time' => '00:00:00'];

        if (file_exists($progressFile)) {
            $content = file_get_contents($progressFile);
            foreach (explode("\n", $content) as $line) {
                if (str_contains($line, '=')) {
                    [$key, $val] = explode('=', $line, 2);
                    $key = trim($key);
                    $val = trim($val);
                    if ($key === 'out_time') $progress['time'] = $val;
                    if ($key === 'fps') $progress['fps'] = (float)$val;
                    if ($key === 'speed') $progress['speed'] = $val;
                    if ($key === 'progress' && $val === 'end') $progress['percent'] = 100;
                }
            }

            $duration = $this->jobs[$id]['input_info']['duration'] ?? 0;
            if ($duration > 0 && $progress['percent'] < 100) {
                $current = $this->timeToSeconds($progress['time']);
                $progress['percent'] = min(99, round(($current / $duration) * 100, 1));
            }
        }

        return $progress;
    }

    private function refreshJob(string $id): void
    {
        if (!isset($this->jobs[$id])) return;
        $job = &$this->jobs[$id];

        if ($job['status'] === 'running' && $job['pid']) {
            if (!posix_kill($job['pid'], 0)) {
                // Check if output file exists and has size
                if (isset($job['output_file']) && file_exists($job['output_file']) && filesize($job['output_file']) > 0) {
                    $job['status'] = 'completed';
                    $job['completed_at'] = date('c');
                    $job['output_size'] = filesize($job['output_file']);
                } else {
                    $job['status'] = 'error';
                    $job['error'] = 'Process ended without output';
                }
                $this->save();
            }
        }
    }

    private function buildCommand(string $input, string $output, string $format, string $preset, ?string $resolution, array $params): string
    {
        $config = require __DIR__ . '/../../config/app.php';
        $ffmpeg = $config['ffmpeg']['binary'];
        $formatConfig = $config['formats']['video'][$format] ?? $config['formats']['audio'][$format] ?? [];
        $presetConfig = $config['presets'][$preset] ?? $config['presets']['balanced'];
        $threads = $config['ffmpeg']['threads'];

        $cmd = "{$ffmpeg} -y -i " . escapeshellarg($input);
        $cmd .= " -threads {$threads}";

        // Check if audio-only
        $isAudio = isset($config['formats']['audio'][$format]);

        if ($isAudio) {
            $cmd .= " -vn";
            $cmd .= " -c:a " . escapeshellarg($formatConfig['codec']);
            if (isset($params['audio_bitrate'])) {
                $cmd .= " -b:a " . escapeshellarg($params['audio_bitrate']);
            }
        } else {
            $cmd .= " -c:v " . escapeshellarg($formatConfig['codec']);
            $cmd .= " -preset " . escapeshellarg($presetConfig['preset']);
            $cmd .= " -crf " . (int)$presetConfig['crf'];

            if ($resolution && isset($config['resolutions'][$resolution])) {
                $res = $config['resolutions'][$resolution];
                $cmd .= " -vf scale={$res['width']}:{$res['height']}";
            }

            $cmd .= " -c:a aac -b:a 128k";
        }

        // HLS specific
        if ($format === 'hls') {
            $cmd .= " -hls_time 4 -hls_list_size 0 -hls_segment_filename "
                . escapeshellarg(dirname($output) . "/segment_%03d.ts");
        }

        // DASH specific
        if ($format === 'dash') {
            $cmd .= " -use_timeline 1 -use_template 1 -adaptation_sets 'id=0,streams=v id=1,streams=a'";
        }

        // Extra params
        if (isset($params['video_bitrate'])) {
            $cmd .= " -b:v " . escapeshellarg($params['video_bitrate']);
        }
        if (isset($params['fps'])) {
            $cmd .= " -r " . (int)$params['fps'];
        }

        $cmd .= " " . escapeshellarg($output);
        return $cmd;
    }

    private function timeToSeconds(string $time): float
    {
        $parts = explode(':', $time);
        if (count($parts) !== 3) return 0;
        return (int)$parts[0] * 3600 + (int)$parts[1] * 60 + (float)$parts[2];
    }
}
