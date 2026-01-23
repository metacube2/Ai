<?php
/**
 * AuthManager - Sichere Authentifizierung für Dashboard
 */

namespace AuroraLivecam\Auth;

use AuroraLivecam\Core\Database;

class AuthManager
{
    private Database $db;
    private bool $dbAvailable = false;

    public function __construct(?Database $db = null)
    {
        $this->db = $db ?? Database::getInstance();
        $this->checkDbAvailability();

        if (session_status() === PHP_SESSION_NONE) {
            session_start();
        }
    }

    private function checkDbAvailability(): void
    {
        try {
            $this->db->fetchOne("SELECT 1 FROM users LIMIT 1");
            $this->dbAvailable = true;
        } catch (\Exception $e) {
            $this->dbAvailable = false;
        }
    }

    /**
     * Registriert einen neuen Benutzer
     */
    public function register(array $data): int
    {
        if (!$this->dbAvailable) {
            throw new \Exception('Database not available');
        }

        // Validierung
        if (empty($data['email']) || !filter_var($data['email'], FILTER_VALIDATE_EMAIL)) {
            throw new \Exception('Invalid email address');
        }

        if (empty($data['password']) || strlen($data['password']) < 8) {
            throw new \Exception('Password must be at least 8 characters');
        }

        // Prüfe ob Email bereits existiert
        $existing = $this->db->fetchOne("SELECT id FROM users WHERE email = ?", [$data['email']]);
        if ($existing) {
            throw new \Exception('Email already registered');
        }

        // Benutzer erstellen
        return $this->db->insert('users', [
            'tenant_id' => $data['tenant_id'] ?? null,
            'email' => strtolower($data['email']),
            'password_hash' => password_hash($data['password'], PASSWORD_ARGON2ID),
            'name' => $data['name'] ?? null,
            'role' => $data['role'] ?? 'tenant_user',
        ]);
    }

    /**
     * Login mit Email und Passwort
     */
    public function login(string $email, string $password, bool $remember = false): bool
    {
        // Legacy-Modus (hardcoded admin)
        if (!$this->dbAvailable) {
            return $this->legacyLogin($email, $password);
        }

        $user = $this->db->fetchOne(
            "SELECT u.*, t.name as tenant_name, t.slug as tenant_slug
             FROM users u
             LEFT JOIN tenants t ON u.tenant_id = t.id
             WHERE u.email = ?",
            [strtolower($email)]
        );

        if (!$user || !password_verify($password, $user['password_hash'])) {
            return false;
        }

        // Session setzen
        $this->setSession($user);

        // Last login aktualisieren
        $this->db->update('users', ['last_login_at' => date('Y-m-d H:i:s')], 'id = ?', [$user['id']]);

        // Remember-Me Cookie
        if ($remember) {
            $this->setRememberToken($user['id']);
        }

        return true;
    }

    /**
     * Legacy Login (kompatibel mit altem AdminManager)
     */
    private function legacyLogin(string $email, string $password): bool
    {
        // Alte hardcoded Credentials als Fallback
        if ($email === 'admin' && $password === 'sonne4000$$$$Q') {
            $_SESSION['admin'] = true;
            $_SESSION['user'] = [
                'id' => 0,
                'email' => 'admin',
                'name' => 'Administrator',
                'role' => 'super_admin',
                'tenant_id' => null,
            ];
            return true;
        }
        return false;
    }

    /**
     * Setzt die Session-Daten
     */
    private function setSession(array $user): void
    {
        $_SESSION['admin'] = true; // Kompatibilität mit Legacy
        $_SESSION['user'] = [
            'id' => $user['id'],
            'email' => $user['email'],
            'name' => $user['name'],
            'role' => $user['role'],
            'tenant_id' => $user['tenant_id'],
            'tenant_name' => $user['tenant_name'] ?? null,
            'tenant_slug' => $user['tenant_slug'] ?? null,
        ];
    }

    /**
     * Setzt Remember-Me Token
     */
    private function setRememberToken(int $userId): void
    {
        $token = bin2hex(random_bytes(32));
        $hashedToken = hash('sha256', $token);

        $this->db->update('users', ['remember_token' => $hashedToken], 'id = ?', [$userId]);

        setcookie('remember_token', $token, [
            'expires' => time() + (86400 * 30), // 30 Tage
            'path' => '/',
            'secure' => true,
            'httponly' => true,
            'samesite' => 'Lax'
        ]);
    }

    /**
     * Prüft Remember-Me Cookie
     */
    public function checkRememberToken(): bool
    {
        if (!isset($_COOKIE['remember_token']) || !$this->dbAvailable) {
            return false;
        }

        $hashedToken = hash('sha256', $_COOKIE['remember_token']);

        $user = $this->db->fetchOne(
            "SELECT u.*, t.name as tenant_name, t.slug as tenant_slug
             FROM users u
             LEFT JOIN tenants t ON u.tenant_id = t.id
             WHERE u.remember_token = ?",
            [$hashedToken]
        );

        if ($user) {
            $this->setSession($user);
            return true;
        }

        return false;
    }

    /**
     * Logout
     */
    public function logout(): void
    {
        // Remember-Token löschen
        if ($this->isLoggedIn() && $this->dbAvailable) {
            $userId = $_SESSION['user']['id'] ?? 0;
            if ($userId > 0) {
                $this->db->update('users', ['remember_token' => null], 'id = ?', [$userId]);
            }
        }

        // Cookie löschen
        setcookie('remember_token', '', [
            'expires' => time() - 3600,
            'path' => '/',
            'secure' => true,
            'httponly' => true,
        ]);

        // Session zerstören
        $_SESSION = [];
        if (ini_get("session.use_cookies")) {
            $params = session_get_cookie_params();
            setcookie(session_name(), '', time() - 42000,
                $params["path"], $params["domain"],
                $params["secure"], $params["httponly"]
            );
        }
        session_destroy();
    }

    /**
     * Prüft ob User eingeloggt ist
     */
    public function isLoggedIn(): bool
    {
        return isset($_SESSION['admin']) && $_SESSION['admin'] === true;
    }

    /**
     * Gibt den aktuellen User zurück
     */
    public function getUser(): ?array
    {
        return $_SESSION['user'] ?? null;
    }

    /**
     * Prüft ob User eine bestimmte Rolle hat
     */
    public function hasRole(string $role): bool
    {
        $user = $this->getUser();
        return $user && $user['role'] === $role;
    }

    /**
     * Prüft ob User Super-Admin ist
     */
    public function isSuperAdmin(): bool
    {
        return $this->hasRole('super_admin');
    }

    /**
     * Prüft ob User Tenant-Admin ist
     */
    public function isTenantAdmin(): bool
    {
        return $this->hasRole('tenant_admin') || $this->hasRole('super_admin');
    }

    /**
     * Gibt die Tenant-ID des aktuellen Users zurück
     */
    public function getTenantId(): ?int
    {
        $user = $this->getUser();
        return $user ? ($user['tenant_id'] ?? null) : null;
    }

    /**
     * Prüft ob User Zugriff auf einen bestimmten Tenant hat
     */
    public function canAccessTenant(int $tenantId): bool
    {
        if ($this->isSuperAdmin()) {
            return true;
        }

        return $this->getTenantId() === $tenantId;
    }

    /**
     * Ändert das Passwort
     */
    public function changePassword(int $userId, string $currentPassword, string $newPassword): bool
    {
        if (!$this->dbAvailable) {
            return false;
        }

        $user = $this->db->fetchOne("SELECT password_hash FROM users WHERE id = ?", [$userId]);

        if (!$user || !password_verify($currentPassword, $user['password_hash'])) {
            return false;
        }

        if (strlen($newPassword) < 8) {
            throw new \Exception('Password must be at least 8 characters');
        }

        return $this->db->update('users', [
            'password_hash' => password_hash($newPassword, PASSWORD_ARGON2ID)
        ], 'id = ?', [$userId]) > 0;
    }

    /**
     * Generiert ein Passwort-Reset-Token
     */
    public function generateResetToken(string $email): ?string
    {
        if (!$this->dbAvailable) {
            return null;
        }

        $user = $this->db->fetchOne("SELECT id FROM users WHERE email = ?", [strtolower($email)]);

        if (!$user) {
            return null; // Keine Info leaken ob Email existiert
        }

        $token = bin2hex(random_bytes(32));
        // Token würde normalerweise in separater Tabelle mit Ablaufzeit gespeichert
        // Für jetzt: vereinfachte Version

        return $token;
    }

    /**
     * Middleware: Erfordert Login
     */
    public function requireLogin(): void
    {
        if (!$this->isLoggedIn()) {
            if (!$this->checkRememberToken()) {
                header('Location: /dashboard/login.php');
                exit;
            }
        }
    }

    /**
     * Middleware: Erfordert bestimmte Rolle
     */
    public function requireRole(string $role): void
    {
        $this->requireLogin();

        if (!$this->hasRole($role) && !$this->isSuperAdmin()) {
            http_response_code(403);
            echo "Access denied";
            exit;
        }
    }
}
