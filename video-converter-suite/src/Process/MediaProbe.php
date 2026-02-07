<?php

namespace VideoConverter\Process;

class MediaProbe
{
    private string $ffprobe;

    public function __construct()
    {
        $config = require __DIR__ . '/../../config/app.php';
        $this->ffprobe = $config['ffmpeg']['ffprobe'];
    }

    public function analyze(string $filePath): array
    {
        $cmd = sprintf(
            '%s -v quiet -print_format json -show_format -show_streams %s',
            $this->ffprobe,
            escapeshellarg($filePath)
        );

        $output = shell_exec($cmd);
        $data = json_decode($output ?: '{}', true);

        if (!$data) {
            return ['error' => 'Could not analyze file'];
        }

        return $this->parseProbeData($data);
    }

    public function getDuration(string $filePath): float
    {
        $info = $this->analyze($filePath);
        return (float)($info['duration'] ?? 0);
    }

    public function getThumbnail(string $filePath, string $outputPath, string $time = '00:00:01'): bool
    {
        $config = require __DIR__ . '/../../config/app.php';
        $cmd = sprintf(
            '%s -y -i %s -ss %s -vframes 1 -vf scale=320:-1 %s 2>/dev/null',
            $config['ffmpeg']['binary'],
            escapeshellarg($filePath),
            escapeshellarg($time),
            escapeshellarg($outputPath)
        );
        exec($cmd, $output, $exitCode);
        return $exitCode === 0;
    }

    private function parseProbeData(array $data): array
    {
        $result = [
            'format' => $data['format']['format_long_name'] ?? 'Unknown',
            'format_name' => $data['format']['format_name'] ?? '',
            'duration' => (float)($data['format']['duration'] ?? 0),
            'size' => (int)($data['format']['size'] ?? 0),
            'bitrate' => (int)($data['format']['bit_rate'] ?? 0),
            'streams' => [],
            'video' => null,
            'audio' => null,
        ];

        foreach (($data['streams'] ?? []) as $stream) {
            $type = $stream['codec_type'] ?? '';
            $info = [
                'index' => $stream['index'],
                'type' => $type,
                'codec' => $stream['codec_name'] ?? 'unknown',
                'codec_long' => $stream['codec_long_name'] ?? '',
            ];

            if ($type === 'video') {
                $info['width'] = (int)($stream['width'] ?? 0);
                $info['height'] = (int)($stream['height'] ?? 0);
                $info['fps'] = $this->parseFps($stream['r_frame_rate'] ?? '0/1');
                $info['pix_fmt'] = $stream['pix_fmt'] ?? '';
                $info['bitrate'] = (int)($stream['bit_rate'] ?? 0);
                $info['profile'] = $stream['profile'] ?? '';
                $info['level'] = $stream['level'] ?? '';
                if (!$result['video']) $result['video'] = $info;
            } elseif ($type === 'audio') {
                $info['sample_rate'] = (int)($stream['sample_rate'] ?? 0);
                $info['channels'] = (int)($stream['channels'] ?? 0);
                $info['channel_layout'] = $stream['channel_layout'] ?? '';
                $info['bitrate'] = (int)($stream['bit_rate'] ?? 0);
                if (!$result['audio']) $result['audio'] = $info;
            }

            $result['streams'][] = $info;
        }

        return $result;
    }

    private function parseFps(string $frac): float
    {
        $parts = explode('/', $frac);
        if (count($parts) === 2 && (int)$parts[1] > 0) {
            return round((int)$parts[0] / (int)$parts[1], 2);
        }
        return (float)$frac;
    }
}
