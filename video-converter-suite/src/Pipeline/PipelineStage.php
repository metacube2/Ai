<?php

namespace VideoConverter\Pipeline;

class PipelineStage
{
    private string $id;
    private string $type; // transcode, scale, filter, audio, watermark, trim, split
    private array $params;
    private bool $enabled;
    private string $label;

    public function __construct(string $type, array $params = [], string $label = '', bool $enabled = true)
    {
        $this->id = bin2hex(random_bytes(4));
        $this->type = $type;
        $this->params = $params;
        $this->label = $label ?: ucfirst($type);
        $this->enabled = $enabled;
    }

    public function getId(): string { return $this->id; }
    public function getType(): string { return $this->type; }
    public function getParams(): array { return $this->params; }
    public function isEnabled(): bool { return $this->enabled; }
    public function getLabel(): string { return $this->label; }

    public function setEnabled(bool $enabled): void { $this->enabled = $enabled; }
    public function setParams(array $params): void { $this->params = $params; }

    public function toFFmpegArgs(): string
    {
        if (!$this->enabled) return '';

        return match ($this->type) {
            'transcode' => $this->buildTranscodeArgs(),
            'scale' => $this->buildScaleArgs(),
            'filter' => $this->buildFilterArgs(),
            'audio' => $this->buildAudioArgs(),
            'watermark' => $this->buildWatermarkArgs(),
            'trim' => $this->buildTrimArgs(),
            'bitrate' => $this->buildBitrateArgs(),
            'framerate' => $this->buildFramerateArgs(),
            'deinterlace' => '-vf yadif',
            'denoise' => '-vf hqdn3d',
            'stabilize' => '-vf deshake',
            default => '',
        };
    }

    private function buildTranscodeArgs(): string
    {
        $args = [];
        if (isset($this->params['video_codec'])) {
            $args[] = "-c:v " . escapeshellarg($this->params['video_codec']);
        }
        if (isset($this->params['audio_codec'])) {
            $args[] = "-c:a " . escapeshellarg($this->params['audio_codec']);
        }
        if (isset($this->params['preset'])) {
            $args[] = "-preset " . escapeshellarg($this->params['preset']);
        }
        if (isset($this->params['crf'])) {
            $args[] = "-crf " . (int)$this->params['crf'];
        }
        return implode(' ', $args);
    }

    private function buildScaleArgs(): string
    {
        $w = (int)($this->params['width'] ?? -1);
        $h = (int)($this->params['height'] ?? -1);
        $algo = $this->params['algorithm'] ?? 'lanczos';
        return "-vf scale={$w}:{$h}:flags={$algo}";
    }

    private function buildFilterArgs(): string
    {
        $filters = [];
        if (isset($this->params['brightness'])) {
            $filters[] = "eq=brightness=" . (float)$this->params['brightness'];
        }
        if (isset($this->params['contrast'])) {
            $filters[] = "eq=contrast=" . (float)$this->params['contrast'];
        }
        if (isset($this->params['saturation'])) {
            $filters[] = "eq=saturation=" . (float)$this->params['saturation'];
        }
        if (isset($this->params['gamma'])) {
            $filters[] = "eq=gamma=" . (float)$this->params['gamma'];
        }
        if (isset($this->params['custom'])) {
            $filters[] = $this->params['custom'];
        }
        return $filters ? '-vf ' . escapeshellarg(implode(',', $filters)) : '';
    }

    private function buildAudioArgs(): string
    {
        $args = [];
        if (isset($this->params['codec'])) {
            $args[] = "-c:a " . escapeshellarg($this->params['codec']);
        }
        if (isset($this->params['bitrate'])) {
            $args[] = "-b:a " . escapeshellarg($this->params['bitrate']);
        }
        if (isset($this->params['sample_rate'])) {
            $args[] = "-ar " . (int)$this->params['sample_rate'];
        }
        if (isset($this->params['channels'])) {
            $args[] = "-ac " . (int)$this->params['channels'];
        }
        if (isset($this->params['volume'])) {
            $args[] = "-af volume=" . (float)$this->params['volume'];
        }
        return implode(' ', $args);
    }

    private function buildWatermarkArgs(): string
    {
        $image = $this->params['image'] ?? '';
        $position = $this->params['position'] ?? 'topright';
        $overlay = match ($position) {
            'topleft' => 'overlay=10:10',
            'topright' => 'overlay=W-w-10:10',
            'bottomleft' => 'overlay=10:H-h-10',
            'bottomright' => 'overlay=W-w-10:H-h-10',
            'center' => 'overlay=(W-w)/2:(H-h)/2',
            default => 'overlay=W-w-10:10',
        };
        return "-i " . escapeshellarg($image) . " -filter_complex \"{$overlay}\"";
    }

    private function buildTrimArgs(): string
    {
        $args = [];
        if (isset($this->params['start'])) {
            $args[] = "-ss " . escapeshellarg($this->params['start']);
        }
        if (isset($this->params['duration'])) {
            $args[] = "-t " . escapeshellarg($this->params['duration']);
        }
        if (isset($this->params['end'])) {
            $args[] = "-to " . escapeshellarg($this->params['end']);
        }
        return implode(' ', $args);
    }

    private function buildBitrateArgs(): string
    {
        $args = [];
        if (isset($this->params['video'])) {
            $args[] = "-b:v " . escapeshellarg($this->params['video']);
        }
        if (isset($this->params['audio'])) {
            $args[] = "-b:a " . escapeshellarg($this->params['audio']);
        }
        if (isset($this->params['maxrate'])) {
            $args[] = "-maxrate " . escapeshellarg($this->params['maxrate']);
            $args[] = "-bufsize " . escapeshellarg($this->params['bufsize'] ?? $this->params['maxrate']);
        }
        return implode(' ', $args);
    }

    private function buildFramerateArgs(): string
    {
        $fps = (float)($this->params['fps'] ?? 30);
        return "-r {$fps}";
    }

    public function toArray(): array
    {
        return [
            'id' => $this->id,
            'type' => $this->type,
            'label' => $this->label,
            'params' => $this->params,
            'enabled' => $this->enabled,
        ];
    }

    public static function fromArray(array $data): self
    {
        $stage = new self(
            $data['type'],
            $data['params'] ?? [],
            $data['label'] ?? '',
            $data['enabled'] ?? true
        );
        if (isset($data['id'])) {
            // Use reflection to set the id for deserialization
            $ref = new \ReflectionProperty($stage, 'id');
            $ref->setValue($stage, $data['id']);
        }
        return $stage;
    }
}
