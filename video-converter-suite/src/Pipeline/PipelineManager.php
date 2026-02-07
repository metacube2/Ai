<?php

namespace VideoConverter\Pipeline;

class PipelineManager
{
    private string $stateFile;
    private array $pipelines = [];

    public function __construct()
    {
        $this->stateFile = __DIR__ . '/../../storage/temp/pipelines.json';
        $this->load();
    }

    private function load(): void
    {
        if (file_exists($this->stateFile)) {
            $data = json_decode(file_get_contents($this->stateFile), true);
            foreach (($data['pipelines'] ?? []) as $pData) {
                $this->pipelines[$pData['id']] = Pipeline::fromArray($pData);
            }
        }
    }

    public function save(): void
    {
        $dir = dirname($this->stateFile);
        if (!is_dir($dir)) {
            mkdir($dir, 0755, true);
        }
        $data = ['pipelines' => []];
        foreach ($this->pipelines as $pipeline) {
            $data['pipelines'][] = $pipeline->toArray();
        }
        file_put_contents($this->stateFile, json_encode($data, JSON_PRETTY_PRINT));
    }

    public function create(string $name): Pipeline
    {
        $pipeline = new Pipeline($name);
        $this->pipelines[$pipeline->getId()] = $pipeline;
        $this->save();
        return $pipeline;
    }

    public function get(string $id): ?Pipeline
    {
        return $this->pipelines[$id] ?? null;
    }

    public function getAll(): array
    {
        return $this->pipelines;
    }

    public function delete(string $id): bool
    {
        if (isset($this->pipelines[$id])) {
            $pipeline = $this->pipelines[$id];
            if ($pipeline->getStatus() === 'running' && $pipeline->getPid()) {
                posix_kill($pipeline->getPid(), SIGTERM);
            }
            unset($this->pipelines[$id]);
            $this->save();
            return true;
        }
        return false;
    }

    public function getRunningCount(): int
    {
        $count = 0;
        foreach ($this->pipelines as $p) {
            if ($p->getStatus() === 'running') $count++;
        }
        return $count;
    }

    public function getByStatus(string $status): array
    {
        return array_filter($this->pipelines, fn(Pipeline $p) => $p->getStatus() === $status);
    }

    public function toArray(): array
    {
        return array_map(fn(Pipeline $p) => $p->toArray(), array_values($this->pipelines));
    }
}
