<?php
/**
 * Logger - Manages log entries for sync operations
 */
class Logger {
    private $configManager;
    private $maxLogEntries = 1000; // Keep last 1000 entries

    public function __construct(ConfigManager $configManager) {
        $this->configManager = $configManager;
    }

    /**
     * Add log entry
     */
    public function log($repoId, $type, $message, $details = []) {
        $logs = $this->configManager->read('log.json');

        if (!isset($logs['entries'])) {
            $logs['entries'] = [];
        }

        $entry = [
            'id' => uniqid('log_', true),
            'timestamp' => date('Y-m-d H:i:s'),
            'repo_id' => $repoId,
            'type' => $type, // success, error, warning, info
            'message' => $message,
            'details' => $details
        ];

        // Add to beginning of array
        array_unshift($logs['entries'], $entry);

        // Keep only last N entries
        if (count($logs['entries']) > $this->maxLogEntries) {
            $logs['entries'] = array_slice($logs['entries'], 0, $this->maxLogEntries);
        }

        $this->configManager->write('log.json', $logs);

        // Also log to PHP error log for debugging
        error_log("[GitPusher] [$type] $repoId: $message");

        return $entry['id'];
    }

    /**
     * Log success
     */
    public function success($repoId, $message, $details = []) {
        return $this->log($repoId, 'success', $message, $details);
    }

    /**
     * Log error
     */
    public function error($repoId, $message, $details = []) {
        return $this->log($repoId, 'error', $message, $details);
    }

    /**
     * Log warning
     */
    public function warning($repoId, $message, $details = []) {
        return $this->log($repoId, 'warning', $message, $details);
    }

    /**
     * Log info
     */
    public function info($repoId, $message, $details = []) {
        return $this->log($repoId, 'info', $message, $details);
    }

    /**
     * Get all log entries
     */
    public function getAll($limit = 100, $offset = 0) {
        $logs = $this->configManager->read('log.json');
        $entries = $logs['entries'] ?? [];

        return array_slice($entries, $offset, $limit);
    }

    /**
     * Get logs for specific repository
     */
    public function getByRepository($repoId, $limit = 100) {
        $logs = $this->configManager->read('log.json');
        $entries = $logs['entries'] ?? [];

        $filtered = array_filter($entries, function($entry) use ($repoId) {
            return $entry['repo_id'] === $repoId;
        });

        return array_slice(array_values($filtered), 0, $limit);
    }

    /**
     * Get logs by type
     */
    public function getByType($type, $limit = 100) {
        $logs = $this->configManager->read('log.json');
        $entries = $logs['entries'] ?? [];

        $filtered = array_filter($entries, function($entry) use ($type) {
            return $entry['type'] === $type;
        });

        return array_slice(array_values($filtered), 0, $limit);
    }

    /**
     * Clear all logs
     */
    public function clear() {
        return $this->configManager->write('log.json', ['entries' => []]);
    }

    /**
     * Clear logs for specific repository
     */
    public function clearByRepository($repoId) {
        $logs = $this->configManager->read('log.json');
        $entries = $logs['entries'] ?? [];

        $filtered = array_filter($entries, function($entry) use ($repoId) {
            return $entry['repo_id'] !== $repoId;
        });

        $logs['entries'] = array_values($filtered);

        return $this->configManager->write('log.json', $logs);
    }

    /**
     * Get statistics
     */
    public function getStats() {
        $logs = $this->configManager->read('log.json');
        $entries = $logs['entries'] ?? [];

        $stats = [
            'total' => count($entries),
            'success' => 0,
            'error' => 0,
            'warning' => 0,
            'info' => 0,
            'last_24h' => 0
        ];

        $yesterday = strtotime('-24 hours');

        foreach ($entries as $entry) {
            $stats[$entry['type']]++;

            $timestamp = strtotime($entry['timestamp']);
            if ($timestamp >= $yesterday) {
                $stats['last_24h']++;
            }
        }

        return $stats;
    }
}
