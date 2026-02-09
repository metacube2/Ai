<?php

namespace VideoConverter\Pipeline;

class Pipeline
{
    private string $id;
    private string $name;
    private array $stages = [];
    private string $status = 'idle'; // idle, running, paused, error, completed
    private ?string $inputSource = null;
    private array $metadata = [];
    private float $progress = 0;
    private ?int $pid = null;
    private string $createdAt;

    public function __construct(string $name, ?string $id = null)
    {
        $this->id = $id ?? bin2hex(random_bytes(8));
        $this->name = $name;
        $this->createdAt = date('c');
    }

    public function getId(): string { return $this->id; }
    public function getName(): string { return $this->name; }
    public function getStatus(): string { return $this->status; }
    public function getProgress(): float { return $this->progress; }
    public function getPid(): ?int { return $this->pid; }
    public function getStages(): array { return $this->stages; }
    public function getInputSource(): ?string { return $this->inputSource; }

    public function setStatus(string $status): void { $this->status = $status; }
    public function setProgress(float $progress): void { $this->progress = min(100, max(0, $progress)); }
    public function setPid(?int $pid): void { $this->pid = $pid; }
    public function setInputSource(string $source): void { $this->inputSource = $source; }

    public function addStage(PipelineStage $stage): self
    {
        $this->stages[] = $stage;
        return $this;
    }

    public function removeStage(int $index): self
    {
        if (isset($this->stages[$index])) {
            array_splice($this->stages, $index, 1);
        }
        return $this;
    }

    public function insertStage(int $index, PipelineStage $stage): self
    {
        array_splice($this->stages, $index, 0, [$stage]);
        return $this;
    }

    public function setMetadata(string $key, mixed $value): void
    {
        $this->metadata[$key] = $value;
    }

    public function getMetadata(?string $key = null): mixed
    {
        if ($key === null) return $this->metadata;
        return $this->metadata[$key] ?? null;
    }

    public function buildFFmpegCommand(string $inputPath, string $outputPath): string
    {
        $config = require __DIR__ . '/../../config/app.php';
        $cmd = $config['ffmpeg']['binary'];
        $parts = ["-y -i " . escapeshellarg($inputPath)];

        foreach ($this->stages as $stage) {
            $parts[] = $stage->toFFmpegArgs();
        }

        $parts[] = escapeshellarg($outputPath);
        return $cmd . ' ' . implode(' ', $parts);
    }

    public function buildStreamCommand(string $inputUrl, string $outputUrl): string
    {
        $config = require __DIR__ . '/../../config/app.php';
        $cmd = $config['ffmpeg']['binary'];
        $parts = ["-re -i " . escapeshellarg($inputUrl)];

        foreach ($this->stages as $stage) {
            $parts[] = $stage->toFFmpegArgs();
        }

        $parts[] = "-f flv " . escapeshellarg($outputUrl);
        return $cmd . ' ' . implode(' ', $parts);
    }

    public function toArray(): array
    {
        return [
            'id' => $this->id,
            'name' => $this->name,
            'status' => $this->status,
            'progress' => $this->progress,
            'pid' => $this->pid,
            'input_source' => $this->inputSource,
            'stages' => array_map(fn(PipelineStage $s) => $s->toArray(), $this->stages),
            'metadata' => $this->metadata,
            'created_at' => $this->createdAt,
        ];
    }

    public static function fromArray(array $data): self
    {
        $pipeline = new self($data['name'], $data['id']);
        $pipeline->status = $data['status'] ?? 'idle';
        $pipeline->progress = $data['progress'] ?? 0;
        $pipeline->pid = $data['pid'] ?? null;
        $pipeline->inputSource = $data['input_source'] ?? null;
        $pipeline->metadata = $data['metadata'] ?? [];
        $pipeline->createdAt = $data['created_at'] ?? date('c');

        foreach (($data['stages'] ?? []) as $stageData) {
            $pipeline->addStage(PipelineStage::fromArray($stageData));
        }

        return $pipeline;
    }
}
