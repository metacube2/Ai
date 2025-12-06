<?php
/**
 * ConfigManager - Handles reading and writing JSON configuration files
 */
class ConfigManager {
    private $dataDir;

    public function __construct($dataDir = '/gitpusher/data') {
        $this->dataDir = $dataDir;
        $this->ensureDataDirExists();
    }

    /**
     * Ensure data directory exists with proper permissions
     */
    private function ensureDataDirExists() {
        if (!file_exists($this->dataDir)) {
            mkdir($this->dataDir, 0755, true);
        }
    }

    /**
     * Read JSON file
     */
    public function read($filename) {
        $filepath = $this->dataDir . '/' . $filename;

        if (!file_exists($filepath)) {
            return [];
        }

        $content = file_get_contents($filepath);
        $data = json_decode($content, true);

        if (json_last_error() !== JSON_ERROR_NONE) {
            error_log("JSON decode error in $filename: " . json_last_error_msg());
            return [];
        }

        return $data;
    }

    /**
     * Write JSON file
     */
    public function write($filename, $data) {
        $filepath = $this->dataDir . '/' . $filename;

        $json = json_encode($data, JSON_PRETTY_PRINT | JSON_UNESCAPED_SLASHES);

        if ($json === false) {
            error_log("JSON encode error: " . json_last_error_msg());
            return false;
        }

        $result = file_put_contents($filepath, $json);

        if ($result === false) {
            error_log("Failed to write file: $filepath");
            return false;
        }

        // Set appropriate permissions (readable only by owner)
        chmod($filepath, 0600);

        return true;
    }

    /**
     * Get all repositories from config
     */
    public function getRepositories() {
        $config = $this->read('config.json');
        return $config['repositories'] ?? [];
    }

    /**
     * Get repository by ID
     */
    public function getRepository($repoId) {
        $repos = $this->getRepositories();

        foreach ($repos as $repo) {
            if ($repo['id'] === $repoId) {
                return $repo;
            }
        }

        return null;
    }

    /**
     * Add new repository
     */
    public function addRepository($repoData) {
        $config = $this->read('config.json');

        if (!isset($config['repositories'])) {
            $config['repositories'] = [];
        }

        // Generate unique ID
        $repoData['id'] = uniqid('repo_', true);
        $repoData['created_at'] = date('Y-m-d H:i:s');
        $repoData['status'] = 'pending';

        $config['repositories'][] = $repoData;

        return $this->write('config.json', $config) ? $repoData['id'] : false;
    }

    /**
     * Update repository
     */
    public function updateRepository($repoId, $updates) {
        $config = $this->read('config.json');

        if (!isset($config['repositories'])) {
            return false;
        }

        foreach ($config['repositories'] as &$repo) {
            if ($repo['id'] === $repoId) {
                $repo = array_merge($repo, $updates);
                $repo['updated_at'] = date('Y-m-d H:i:s');
                return $this->write('config.json', $config);
            }
        }

        return false;
    }

    /**
     * Delete repository
     */
    public function deleteRepository($repoId) {
        $config = $this->read('config.json');

        if (!isset($config['repositories'])) {
            return false;
        }

        $config['repositories'] = array_filter($config['repositories'], function($repo) use ($repoId) {
            return $repo['id'] !== $repoId;
        });

        // Re-index array
        $config['repositories'] = array_values($config['repositories']);

        return $this->write('config.json', $config);
    }

    /**
     * Get GitHub Personal Access Token
     */
    public function getGitHubToken() {
        $secrets = $this->read('secrets.json');
        return $secrets['github_pat'] ?? null;
    }

    /**
     * Set GitHub Personal Access Token
     */
    public function setGitHubToken($token) {
        $secrets = $this->read('secrets.json');
        $secrets['github_pat'] = $token;
        return $this->write('secrets.json', $secrets);
    }

    /**
     * Get webhook secret for a repository
     */
    public function getWebhookSecret($repoId) {
        $secrets = $this->read('secrets.json');
        return $secrets['webhook_secrets'][$repoId] ?? null;
    }

    /**
     * Set webhook secret for a repository
     */
    public function setWebhookSecret($repoId, $secret) {
        $secrets = $this->read('secrets.json');

        if (!isset($secrets['webhook_secrets'])) {
            $secrets['webhook_secrets'] = [];
        }

        $secrets['webhook_secrets'][$repoId] = $secret;

        return $this->write('secrets.json', $secrets);
    }

    /**
     * Generate secure webhook secret
     */
    public function generateWebhookSecret() {
        return bin2hex(random_bytes(32));
    }
}
