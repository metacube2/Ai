<?php
/**
 * Database - PDO Wrapper mit Singleton Pattern
 *
 * Verwendung:
 *   $db = Database::getInstance();
 *   $users = $db->fetchAll("SELECT * FROM users WHERE tenant_id = ?", [$tenantId]);
 */

namespace AuroraLivecam\Core;

use PDO;
use PDOException;
use Exception;

class Database
{
    private static ?Database $instance = null;
    private ?PDO $pdo = null;
    private array $config;

    private function __construct()
    {
        $this->config = $this->loadConfig();
    }

    /**
     * Singleton: Gibt die einzige Instanz zurück
     */
    public static function getInstance(): Database
    {
        if (self::$instance === null) {
            self::$instance = new self();
        }
        return self::$instance;
    }

    /**
     * Lädt die Datenbank-Konfiguration
     */
    private function loadConfig(): array
    {
        // Versuche .env oder config.php zu laden
        $configFile = dirname(__DIR__, 2) . '/config.php';

        if (file_exists($configFile)) {
            $config = require $configFile;
            return $config['database'] ?? [];
        }

        // Fallback auf Umgebungsvariablen
        return [
            'host' => getenv('DB_HOST') ?: 'localhost',
            'port' => getenv('DB_PORT') ?: 3306,
            'database' => getenv('DB_DATABASE') ?: 'aurora_livecam',
            'username' => getenv('DB_USERNAME') ?: 'root',
            'password' => getenv('DB_PASSWORD') ?: '',
            'charset' => 'utf8mb4',
        ];
    }

    /**
     * Stellt die Datenbankverbindung her (Lazy Loading)
     */
    public function connect(): PDO
    {
        if ($this->pdo !== null) {
            return $this->pdo;
        }

        $dsn = sprintf(
            'mysql:host=%s;port=%d;dbname=%s;charset=%s',
            $this->config['host'],
            $this->config['port'],
            $this->config['database'],
            $this->config['charset']
        );

        try {
            $this->pdo = new PDO($dsn, $this->config['username'], $this->config['password'], [
                PDO::ATTR_ERRMODE => PDO::ERRMODE_EXCEPTION,
                PDO::ATTR_DEFAULT_FETCH_MODE => PDO::FETCH_ASSOC,
                PDO::ATTR_EMULATE_PREPARES => false,
                PDO::MYSQL_ATTR_INIT_COMMAND => "SET NAMES utf8mb4 COLLATE utf8mb4_unicode_ci"
            ]);
        } catch (PDOException $e) {
            throw new Exception('Database connection failed: ' . $e->getMessage());
        }

        return $this->pdo;
    }

    /**
     * Führt eine Query aus und gibt alle Ergebnisse zurück
     */
    public function fetchAll(string $sql, array $params = []): array
    {
        $stmt = $this->connect()->prepare($sql);
        $stmt->execute($params);
        return $stmt->fetchAll();
    }

    /**
     * Führt eine Query aus und gibt eine Zeile zurück
     */
    public function fetchOne(string $sql, array $params = []): ?array
    {
        $stmt = $this->connect()->prepare($sql);
        $stmt->execute($params);
        $result = $stmt->fetch();
        return $result ?: null;
    }

    /**
     * Führt eine Query aus und gibt einen einzelnen Wert zurück
     */
    public function fetchColumn(string $sql, array $params = [], int $column = 0): mixed
    {
        $stmt = $this->connect()->prepare($sql);
        $stmt->execute($params);
        return $stmt->fetchColumn($column);
    }

    /**
     * Führt INSERT/UPDATE/DELETE aus und gibt die Anzahl betroffener Zeilen zurück
     */
    public function execute(string $sql, array $params = []): int
    {
        $stmt = $this->connect()->prepare($sql);
        $stmt->execute($params);
        return $stmt->rowCount();
    }

    /**
     * INSERT und gibt die neue ID zurück
     */
    public function insert(string $table, array $data): int
    {
        $columns = implode(', ', array_map(fn($col) => "`$col`", array_keys($data)));
        $placeholders = implode(', ', array_fill(0, count($data), '?'));

        $sql = "INSERT INTO `$table` ($columns) VALUES ($placeholders)";
        $this->execute($sql, array_values($data));

        return (int) $this->connect()->lastInsertId();
    }

    /**
     * UPDATE mit WHERE-Bedingung
     */
    public function update(string $table, array $data, string $where, array $whereParams = []): int
    {
        $set = implode(', ', array_map(fn($col) => "`$col` = ?", array_keys($data)));
        $sql = "UPDATE `$table` SET $set WHERE $where";

        return $this->execute($sql, [...array_values($data), ...$whereParams]);
    }

    /**
     * DELETE mit WHERE-Bedingung
     */
    public function delete(string $table, string $where, array $params = []): int
    {
        return $this->execute("DELETE FROM `$table` WHERE $where", $params);
    }

    /**
     * Startet eine Transaktion
     */
    public function beginTransaction(): bool
    {
        return $this->connect()->beginTransaction();
    }

    /**
     * Bestätigt eine Transaktion
     */
    public function commit(): bool
    {
        return $this->connect()->commit();
    }

    /**
     * Macht eine Transaktion rückgängig
     */
    public function rollback(): bool
    {
        return $this->connect()->rollBack();
    }

    /**
     * Prüft ob eine Datenbankverbindung besteht
     */
    public function isConnected(): bool
    {
        return $this->pdo !== null;
    }

    /**
     * Gibt die PDO-Instanz direkt zurück (für komplexe Queries)
     */
    public function getPdo(): PDO
    {
        return $this->connect();
    }

    // Prevent cloning
    private function __clone() {}

    // Prevent unserialization
    public function __wakeup()
    {
        throw new Exception("Cannot unserialize singleton");
    }
}
