<?php
/**
 * OnboardingManager - Verwaltet den Onboarding-Prozess
 */

namespace AuroraLivecam\Onboarding;

use AuroraLivecam\Core\Database;
use AuroraLivecam\Tenant\TenantManager;
use AuroraLivecam\Auth\AuthManager;

class OnboardingManager
{
    private Database $db;
    private TenantManager $tenantManager;
    private StreamValidator $streamValidator;

    public const STEP_REGISTER = 1;
    public const STEP_VERIFY_EMAIL = 2;
    public const STEP_STREAM = 3;
    public const STEP_BRANDING = 4;
    public const STEP_COMPLETE = 5;

    public function __construct(?Database $db = null)
    {
        $this->db = $db ?? Database::getInstance();
        $this->tenantManager = new TenantManager($this->db);
        $this->streamValidator = new StreamValidator();
    }

    /**
     * Startet den Onboarding-Prozess (Registrierung)
     */
    public function register(array $data): array
    {
        $errors = $this->validateRegistration($data);

        if (!empty($errors)) {
            return ['success' => false, 'errors' => $errors];
        }

        try {
            $this->db->beginTransaction();

            // Tenant erstellen
            $tenantId = $this->tenantManager->create([
                'name' => $data['company_name'] ?? $data['name'],
                'email' => $data['email'],
                'subdomain' => $this->generateSubdomain($data['company_name'] ?? $data['name']),
                'stream_url' => $data['stream_url'] ?? '',
                'stream_type' => $data['stream_type'] ?? 'hls',
            ]);

            // Admin-User für den Tenant erstellen
            $auth = new AuthManager($this->db);
            $userId = $auth->register([
                'tenant_id' => $tenantId,
                'email' => $data['email'],
                'password' => $data['password'],
                'name' => $data['name'],
                'role' => 'tenant_admin',
            ]);

            // Verification-Token generieren
            $verificationToken = $this->generateVerificationToken($userId);

            $this->db->commit();

            return [
                'success' => true,
                'tenant_id' => $tenantId,
                'user_id' => $userId,
                'verification_token' => $verificationToken,
                'next_step' => self::STEP_VERIFY_EMAIL,
            ];

        } catch (\Exception $e) {
            $this->db->rollback();
            return ['success' => false, 'errors' => ['general' => $e->getMessage()]];
        }
    }

    /**
     * Validiert Registrierungsdaten
     */
    private function validateRegistration(array $data): array
    {
        $errors = [];

        // Name
        if (empty($data['name'])) {
            $errors['name'] = 'Name ist erforderlich';
        }

        // Company/Site Name
        if (empty($data['company_name'])) {
            $errors['company_name'] = 'Firmen-/Site-Name ist erforderlich';
        }

        // Email
        if (empty($data['email'])) {
            $errors['email'] = 'E-Mail ist erforderlich';
        } elseif (!filter_var($data['email'], FILTER_VALIDATE_EMAIL)) {
            $errors['email'] = 'Ungültige E-Mail-Adresse';
        } else {
            // Prüfe ob Email bereits existiert
            $existing = $this->db->fetchOne("SELECT id FROM users WHERE email = ?", [strtolower($data['email'])]);
            if ($existing) {
                $errors['email'] = 'Diese E-Mail-Adresse ist bereits registriert';
            }
        }

        // Password
        if (empty($data['password'])) {
            $errors['password'] = 'Passwort ist erforderlich';
        } elseif (strlen($data['password']) < 8) {
            $errors['password'] = 'Passwort muss mindestens 8 Zeichen lang sein';
        }

        // Password Confirmation
        if (($data['password'] ?? '') !== ($data['password_confirm'] ?? '')) {
            $errors['password_confirm'] = 'Passwörter stimmen nicht überein';
        }

        // Stream URL (optional, aber wenn angegeben, validieren)
        if (!empty($data['stream_url'])) {
            $validation = $this->streamValidator->validate($data['stream_url']);
            if (!$validation['valid']) {
                $errors['stream_url'] = $validation['error'] ?? 'Stream-URL ungültig';
            }
        }

        // Terms
        if (empty($data['accept_terms'])) {
            $errors['accept_terms'] = 'Sie müssen die AGB akzeptieren';
        }

        return $errors;
    }

    /**
     * Generiert eine Subdomain aus dem Firmennamen
     */
    private function generateSubdomain(string $name): string
    {
        // Umlaute ersetzen
        $replacements = ['ä' => 'ae', 'ö' => 'oe', 'ü' => 'ue', 'ß' => 'ss'];
        $slug = str_replace(array_keys($replacements), array_values($replacements), strtolower($name));

        // Nur alphanumerische Zeichen und Bindestriche
        $slug = preg_replace('/[^a-z0-9]+/', '-', $slug);
        $slug = trim($slug, '-');

        // Max 30 Zeichen
        $slug = substr($slug, 0, 30);

        // Eindeutigkeit prüfen
        $baseSlug = $slug;
        $counter = 1;
        while (!$this->tenantManager->isDomainAvailable($slug . '.aurora-livecam.com')) {
            $slug = $baseSlug . '-' . $counter;
            $counter++;
        }

        return $slug;
    }

    /**
     * Generiert einen E-Mail-Verification-Token
     */
    private function generateVerificationToken(int $userId): string
    {
        $token = bin2hex(random_bytes(32));

        // Token in einer separaten Tabelle speichern (oder im User)
        // Vereinfacht: Wir nutzen remember_token temporär
        $this->db->update('users', ['remember_token' => hash('sha256', $token)], 'id = ?', [$userId]);

        return $token;
    }

    /**
     * Verifiziert E-Mail-Adresse
     */
    public function verifyEmail(string $token): array
    {
        $hashedToken = hash('sha256', $token);

        $user = $this->db->fetchOne(
            "SELECT id, tenant_id FROM users WHERE remember_token = ? AND email_verified_at IS NULL",
            [$hashedToken]
        );

        if (!$user) {
            return ['success' => false, 'error' => 'Ungültiger oder abgelaufener Token'];
        }

        $this->db->update('users', [
            'email_verified_at' => date('Y-m-d H:i:s'),
            'remember_token' => null,
        ], 'id = ?', [$user['id']]);

        // Onboarding-Status aktualisieren
        $this->updateOnboardingStep($user['tenant_id'], self::STEP_STREAM);

        return [
            'success' => true,
            'user_id' => $user['id'],
            'tenant_id' => $user['tenant_id'],
            'next_step' => self::STEP_STREAM,
        ];
    }

    /**
     * Speichert Stream-Konfiguration
     */
    public function saveStream(int $tenantId, string $url, string $type = 'hls'): array
    {
        // Validieren
        $validation = $this->streamValidator->validate($url);

        if (!$validation['valid']) {
            return ['success' => false, 'error' => $validation['error']];
        }

        // Speichern
        $existing = $this->db->fetchOne(
            "SELECT id FROM tenant_streams WHERE tenant_id = ? AND is_primary = 1",
            [$tenantId]
        );

        if ($existing) {
            $this->db->update('tenant_streams', [
                'stream_url' => $url,
                'stream_type' => $validation['type'] ?? $type,
                'last_status' => 'online',
                'last_check_at' => date('Y-m-d H:i:s'),
            ], 'id = ?', [$existing['id']]);
        } else {
            $this->db->insert('tenant_streams', [
                'tenant_id' => $tenantId,
                'stream_url' => $url,
                'stream_type' => $validation['type'] ?? $type,
                'is_primary' => 1,
                'last_status' => 'online',
                'last_check_at' => date('Y-m-d H:i:s'),
            ]);
        }

        // Onboarding-Schritt aktualisieren
        $this->updateOnboardingStep($tenantId, self::STEP_BRANDING, ['stream_verified' => 1]);

        return [
            'success' => true,
            'stream_type' => $validation['type'],
            'next_step' => self::STEP_BRANDING,
        ];
    }

    /**
     * Speichert Basis-Branding
     */
    public function saveBranding(int $tenantId, array $branding): array
    {
        $this->tenantManager->updateBranding($tenantId, $branding);

        // Onboarding-Schritt aktualisieren
        $this->updateOnboardingStep($tenantId, self::STEP_COMPLETE, ['branding_configured' => 1]);

        return [
            'success' => true,
            'next_step' => self::STEP_COMPLETE,
        ];
    }

    /**
     * Schliesst das Onboarding ab
     */
    public function complete(int $tenantId): array
    {
        $this->db->update('tenant_onboarding', [
            'current_step' => self::STEP_COMPLETE,
            'completed_at' => date('Y-m-d H:i:s'),
        ], 'tenant_id = ?', [$tenantId]);

        // Tenant aktivieren
        $this->tenantManager->activate($tenantId);

        return ['success' => true, 'completed' => true];
    }

    /**
     * Aktualisiert den Onboarding-Schritt
     */
    private function updateOnboardingStep(int $tenantId, int $step, array $extra = []): void
    {
        $data = array_merge(['current_step' => $step], $extra);
        $this->db->update('tenant_onboarding', $data, 'tenant_id = ?', [$tenantId]);
    }

    /**
     * Gibt den aktuellen Onboarding-Status zurück
     */
    public function getStatus(int $tenantId): array
    {
        $onboarding = $this->db->fetchOne(
            "SELECT * FROM tenant_onboarding WHERE tenant_id = ?",
            [$tenantId]
        );

        if (!$onboarding) {
            return [
                'current_step' => self::STEP_REGISTER,
                'completed' => false,
            ];
        }

        return [
            'current_step' => (int)$onboarding['current_step'],
            'stream_verified' => (bool)$onboarding['stream_verified'],
            'branding_configured' => (bool)$onboarding['branding_configured'],
            'payment_configured' => (bool)$onboarding['payment_configured'],
            'completed' => $onboarding['completed_at'] !== null,
            'completed_at' => $onboarding['completed_at'],
        ];
    }

    /**
     * Prüft ob E-Mail-Verification erforderlich ist
     */
    public function requiresEmailVerification(): bool
    {
        // Aus Settings laden
        $settingsFile = dirname(__DIR__, 2) . '/SettingsManager.php';
        if (file_exists($settingsFile)) {
            require_once $settingsFile;
            $settings = new \SettingsManager();
            return $settings->get('saas_features.email_verification_required') ?? true;
        }
        return true;
    }

    /**
     * Sendet Verification-E-Mail erneut
     */
    public function resendVerification(int $userId): array
    {
        $user = $this->db->fetchOne("SELECT email, email_verified_at FROM users WHERE id = ?", [$userId]);

        if (!$user) {
            return ['success' => false, 'error' => 'Benutzer nicht gefunden'];
        }

        if ($user['email_verified_at']) {
            return ['success' => false, 'error' => 'E-Mail bereits verifiziert'];
        }

        $token = $this->generateVerificationToken($userId);

        return [
            'success' => true,
            'token' => $token,
            'email' => $user['email'],
        ];
    }
}
