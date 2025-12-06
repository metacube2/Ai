<?php
/**
 * GitHandler - Handles all Git operations
 */
class GitHandler {
    private $logger;
    private $configManager;

    public function __construct(Logger $logger, ConfigManager $configManager) {
        $this->logger = $logger;
        $this->configManager = $configManager;
    }

    /**
     * Execute shell command and return result
     */
    private function exec($command, $cwd = null) {
        $descriptorspec = [
            0 => ['pipe', 'r'],  // stdin
            1 => ['pipe', 'w'],  // stdout
            2 => ['pipe', 'w']   // stderr
        ];

        $process = proc_open($command, $descriptorspec, $pipes, $cwd);

        if (!is_resource($process)) {
            return [
                'success' => false,
                'output' => '',
                'error' => 'Failed to execute command',
                'exit_code' => -1
            ];
        }

        $stdout = stream_get_contents($pipes[1]);
        $stderr = stream_get_contents($pipes[2]);

        fclose($pipes[0]);
        fclose($pipes[1]);
        fclose($pipes[2]);

        $exitCode = proc_close($process);

        return [
            'success' => $exitCode === 0,
            'output' => trim($stdout),
            'error' => trim($stderr),
            'exit_code' => $exitCode
        ];
    }

    /**
     * Build Git URL with authentication token
     */
    private function buildGitUrl($repoUrl, $token) {
        // Convert https://github.com/user/repo.git to https://TOKEN@github.com/user/repo.git
        $parsed = parse_url($repoUrl);

        if (!$parsed || !isset($parsed['host'])) {
            return $repoUrl;
        }

        $scheme = $parsed['scheme'] ?? 'https';
        $host = $parsed['host'];
        $path = $parsed['path'] ?? '';

        return "$scheme://$token@$host$path";
    }

    /**
     * Clone repository
     */
    public function cloneRepository($repoId, $repoUrl, $targetPath, $branch = 'main') {
        $this->logger->info($repoId, "Starting clone of repository to $targetPath");

        // Check if target path already exists
        if (file_exists($targetPath)) {
            $this->logger->error($repoId, "Target path already exists: $targetPath");
            return [
                'success' => false,
                'message' => 'Target path already exists'
            ];
        }

        // Create parent directory if it doesn't exist
        $parentDir = dirname($targetPath);
        if (!file_exists($parentDir)) {
            mkdir($parentDir, 0755, true);
        }

        // Get GitHub token
        $token = $this->configManager->getGitHubToken();
        $gitUrl = $token ? $this->buildGitUrl($repoUrl, $token) : $repoUrl;

        // Clone repository
        $command = sprintf(
            'git clone --branch %s %s %s 2>&1',
            escapeshellarg($branch),
            escapeshellarg($gitUrl),
            escapeshellarg($targetPath)
        );

        $result = $this->exec($command);

        if ($result['success']) {
            $this->logger->success($repoId, "Repository cloned successfully", [
                'path' => $targetPath,
                'branch' => $branch
            ]);

            return [
                'success' => true,
                'message' => 'Repository cloned successfully',
                'output' => $result['output']
            ];
        } else {
            $this->logger->error($repoId, "Failed to clone repository", [
                'error' => $result['error'],
                'output' => $result['output']
            ]);

            return [
                'success' => false,
                'message' => 'Failed to clone repository',
                'error' => $result['error']
            ];
        }
    }

    /**
     * Pull latest changes
     */
    public function pull($repoId, $targetPath, $branch = 'main') {
        $this->logger->info($repoId, "Starting pull for $targetPath");

        // Check if path exists
        if (!file_exists($targetPath)) {
            $this->logger->error($repoId, "Repository path does not exist: $targetPath");
            return [
                'success' => false,
                'message' => 'Repository path does not exist'
            ];
        }

        // Check if it's a git repository
        if (!file_exists("$targetPath/.git")) {
            $this->logger->error($repoId, "Not a git repository: $targetPath");
            return [
                'success' => false,
                'message' => 'Not a git repository'
            ];
        }

        // Get current commit before pull
        $currentCommit = $this->getCurrentCommit($targetPath);

        // Pull changes
        $command = sprintf(
            'cd %s && git pull origin %s 2>&1',
            escapeshellarg($targetPath),
            escapeshellarg($branch)
        );

        $result = $this->exec($command);

        // Check for merge conflicts
        if (!$result['success'] || strpos($result['output'], 'CONFLICT') !== false) {
            $this->logger->warning($repoId, "Merge conflict detected", [
                'output' => $result['output'],
                'error' => $result['error']
            ]);

            $this->configManager->updateRepository($repoId, ['status' => 'conflict']);

            return [
                'success' => false,
                'message' => 'Merge conflict detected',
                'conflict' => true,
                'output' => $result['output']
            ];
        }

        // Get new commit after pull
        $newCommit = $this->getCurrentCommit($targetPath);

        // Count changed files
        $changedFiles = $this->getChangedFilesBetweenCommits($targetPath, $currentCommit, $newCommit);

        if ($result['success']) {
            $this->logger->success($repoId, "Pull completed successfully", [
                'files_changed' => count($changedFiles),
                'old_commit' => substr($currentCommit, 0, 7),
                'new_commit' => substr($newCommit, 0, 7)
            ]);

            $this->configManager->updateRepository($repoId, [
                'status' => 'synced',
                'last_sync' => date('Y-m-d H:i:s'),
                'last_commit' => $newCommit
            ]);

            return [
                'success' => true,
                'message' => 'Pull completed successfully',
                'files_changed' => count($changedFiles),
                'output' => $result['output']
            ];
        } else {
            $this->logger->error($repoId, "Pull failed", [
                'error' => $result['error'],
                'output' => $result['output']
            ]);

            return [
                'success' => false,
                'message' => 'Pull failed',
                'error' => $result['error']
            ];
        }
    }

    /**
     * Revert to specific commit
     */
    public function revert($repoId, $targetPath, $commitHash) {
        $this->logger->info($repoId, "Starting revert to commit $commitHash");

        if (!file_exists($targetPath)) {
            return [
                'success' => false,
                'message' => 'Repository path does not exist'
            ];
        }

        // Create revert commit
        $command = sprintf(
            'cd %s && git revert --no-edit %s 2>&1',
            escapeshellarg($targetPath),
            escapeshellarg($commitHash)
        );

        $result = $this->exec($command);

        if ($result['success']) {
            $this->logger->success($repoId, "Reverted to commit $commitHash", [
                'commit' => $commitHash
            ]);

            $newCommit = $this->getCurrentCommit($targetPath);

            $this->configManager->updateRepository($repoId, [
                'last_commit' => $newCommit,
                'last_sync' => date('Y-m-d H:i:s')
            ]);

            return [
                'success' => true,
                'message' => 'Revert completed successfully',
                'output' => $result['output']
            ];
        } else {
            $this->logger->error($repoId, "Revert failed", [
                'error' => $result['error']
            ]);

            return [
                'success' => false,
                'message' => 'Revert failed',
                'error' => $result['error']
            ];
        }
    }

    /**
     * Get current commit hash
     */
    public function getCurrentCommit($targetPath) {
        $result = $this->exec("cd " . escapeshellarg($targetPath) . " && git rev-parse HEAD 2>&1");
        return $result['success'] ? trim($result['output']) : null;
    }

    /**
     * Get commit history
     */
    public function getCommitHistory($targetPath, $limit = 20) {
        $command = sprintf(
            'cd %s && git log --pretty=format:"%%H|%%an|%%ae|%%at|%%s" -n %d 2>&1',
            escapeshellarg($targetPath),
            (int)$limit
        );

        $result = $this->exec($command);

        if (!$result['success']) {
            return [];
        }

        $commits = [];
        $lines = explode("\n", $result['output']);

        foreach ($lines as $line) {
            if (empty($line)) continue;

            $parts = explode('|', $line);
            if (count($parts) !== 5) continue;

            $commits[] = [
                'hash' => $parts[0],
                'hash_short' => substr($parts[0], 0, 7),
                'author_name' => $parts[1],
                'author_email' => $parts[2],
                'timestamp' => date('Y-m-d H:i:s', (int)$parts[3]),
                'message' => $parts[4]
            ];
        }

        return $commits;
    }

    /**
     * Get changed files between commits
     */
    private function getChangedFilesBetweenCommits($targetPath, $oldCommit, $newCommit) {
        if ($oldCommit === $newCommit) {
            return [];
        }

        $command = sprintf(
            'cd %s && git diff --name-only %s %s 2>&1',
            escapeshellarg($targetPath),
            escapeshellarg($oldCommit),
            escapeshellarg($newCommit)
        );

        $result = $this->exec($command);

        if (!$result['success']) {
            return [];
        }

        return array_filter(explode("\n", $result['output']));
    }

    /**
     * Get repository status
     */
    public function getStatus($targetPath) {
        $command = sprintf(
            'cd %s && git status --porcelain 2>&1',
            escapeshellarg($targetPath)
        );

        $result = $this->exec($command);

        return [
            'success' => $result['success'],
            'clean' => $result['success'] && empty($result['output']),
            'output' => $result['output']
        ];
    }

    /**
     * Get current branch
     */
    public function getCurrentBranch($targetPath) {
        $result = $this->exec("cd " . escapeshellarg($targetPath) . " && git branch --show-current 2>&1");
        return $result['success'] ? trim($result['output']) : null;
    }

    /**
     * Fetch available branches from remote
     */
    public function getRemoteBranches($repoUrl) {
        $token = $this->configManager->getGitHubToken();
        $gitUrl = $token ? $this->buildGitUrl($repoUrl, $token) : $repoUrl;

        $command = sprintf(
            'git ls-remote --heads %s 2>&1',
            escapeshellarg($gitUrl)
        );

        $result = $this->exec($command);

        if (!$result['success']) {
            return [];
        }

        $branches = [];
        $lines = explode("\n", $result['output']);

        foreach ($lines as $line) {
            if (preg_match('/refs\/heads\/(.+)$/', $line, $matches)) {
                $branches[] = $matches[1];
            }
        }

        return $branches;
    }
}
