<?php
/**
 * StripeService - Stripe API Wrapper
 */

namespace AuroraLivecam\Billing;

use AuroraLivecam\Core\Database;

class StripeService
{
    private ?string $secretKey;
    private ?string $publicKey;
    private ?string $webhookSecret;
    private string $currency;
    private Database $db;

    public function __construct(?Database $db = null)
    {
        $this->db = $db ?? Database::getInstance();
        $this->loadConfig();
    }

    /**
     * Lädt Stripe-Konfiguration
     */
    private function loadConfig(): void
    {
        $configFile = dirname(__DIR__, 2) . '/config.php';

        if (file_exists($configFile)) {
            $config = require $configFile;
            $this->secretKey = $config['stripe']['secret_key'] ?? '';
            $this->publicKey = $config['stripe']['public_key'] ?? '';
            $this->webhookSecret = $config['stripe']['webhook_secret'] ?? '';
            $this->currency = $config['stripe']['currency'] ?? 'chf';
        } else {
            $this->secretKey = getenv('STRIPE_SECRET_KEY') ?: '';
            $this->publicKey = getenv('STRIPE_PUBLIC_KEY') ?: '';
            $this->webhookSecret = getenv('STRIPE_WEBHOOK_SECRET') ?: '';
            $this->currency = 'chf';
        }
    }

    /**
     * Prüft ob Stripe konfiguriert ist
     */
    public function isConfigured(): bool
    {
        return !empty($this->secretKey) && !empty($this->publicKey);
    }

    /**
     * Gibt den Public Key zurück
     */
    public function getPublicKey(): string
    {
        return $this->publicKey ?? '';
    }

    /**
     * Erstellt einen Stripe Customer
     */
    public function createCustomer(int $tenantId, string $email, string $name): ?string
    {
        $response = $this->request('POST', '/v1/customers', [
            'email' => $email,
            'name' => $name,
            'metadata' => [
                'tenant_id' => $tenantId,
            ],
        ]);

        if ($response && isset($response['id'])) {
            // In DB speichern
            $this->db->execute(
                "UPDATE subscriptions SET stripe_customer_id = ? WHERE tenant_id = ?",
                [$response['id'], $tenantId]
            );
            return $response['id'];
        }

        return null;
    }

    /**
     * Erstellt eine Checkout Session
     */
    public function createCheckoutSession(int $tenantId, string $priceId, string $successUrl, string $cancelUrl): ?array
    {
        // Customer ID holen oder erstellen
        $customerId = $this->getOrCreateCustomer($tenantId);

        $params = [
            'customer' => $customerId,
            'payment_method_types' => ['card'],
            'line_items' => [[
                'price' => $priceId,
                'quantity' => 1,
            ]],
            'mode' => 'subscription',
            'success_url' => $successUrl,
            'cancel_url' => $cancelUrl,
            'metadata' => [
                'tenant_id' => $tenantId,
            ],
        ];

        return $this->request('POST', '/v1/checkout/sessions', $params);
    }

    /**
     * Erstellt ein Billing Portal Session
     */
    public function createPortalSession(int $tenantId, string $returnUrl): ?array
    {
        $customerId = $this->getCustomerId($tenantId);

        if (!$customerId) {
            return null;
        }

        return $this->request('POST', '/v1/billing_portal/sessions', [
            'customer' => $customerId,
            'return_url' => $returnUrl,
        ]);
    }

    /**
     * Holt oder erstellt Customer
     */
    private function getOrCreateCustomer(int $tenantId): ?string
    {
        $customerId = $this->getCustomerId($tenantId);

        if ($customerId) {
            return $customerId;
        }

        // Tenant-Daten laden
        $tenant = $this->db->fetchOne(
            "SELECT t.*, u.email, u.name FROM tenants t
             LEFT JOIN users u ON u.tenant_id = t.id AND u.role = 'tenant_admin'
             WHERE t.id = ? LIMIT 1",
            [$tenantId]
        );

        if (!$tenant) {
            return null;
        }

        return $this->createCustomer($tenantId, $tenant['email'], $tenant['name'] ?? $tenant['name']);
    }

    /**
     * Holt Customer ID aus DB
     */
    private function getCustomerId(int $tenantId): ?string
    {
        $sub = $this->db->fetchOne(
            "SELECT stripe_customer_id FROM subscriptions WHERE tenant_id = ?",
            [$tenantId]
        );

        return $sub['stripe_customer_id'] ?? null;
    }

    /**
     * Holt Subscription von Stripe
     */
    public function getSubscription(string $subscriptionId): ?array
    {
        return $this->request('GET', '/v1/subscriptions/' . $subscriptionId);
    }

    /**
     * Kündigt Subscription
     */
    public function cancelSubscription(string $subscriptionId, bool $immediately = false): ?array
    {
        if ($immediately) {
            return $this->request('DELETE', '/v1/subscriptions/' . $subscriptionId);
        }

        return $this->request('POST', '/v1/subscriptions/' . $subscriptionId, [
            'cancel_at_period_end' => true,
        ]);
    }

    /**
     * Holt Rechnungen
     */
    public function getInvoices(string $customerId, int $limit = 10): array
    {
        $response = $this->request('GET', '/v1/invoices', [
            'customer' => $customerId,
            'limit' => $limit,
        ]);

        return $response['data'] ?? [];
    }

    /**
     * Verifiziert Webhook-Signatur
     */
    public function verifyWebhook(string $payload, string $signature): ?array
    {
        if (empty($this->webhookSecret)) {
            return json_decode($payload, true);
        }

        $elements = explode(',', $signature);
        $timestamp = null;
        $signatures = [];

        foreach ($elements as $element) {
            $parts = explode('=', $element, 2);
            if ($parts[0] === 't') {
                $timestamp = $parts[1];
            } elseif ($parts[0] === 'v1') {
                $signatures[] = $parts[1];
            }
        }

        if (!$timestamp || empty($signatures)) {
            return null;
        }

        // Toleranz: 5 Minuten
        if (abs(time() - $timestamp) > 300) {
            return null;
        }

        $signedPayload = $timestamp . '.' . $payload;
        $expectedSignature = hash_hmac('sha256', $signedPayload, $this->webhookSecret);

        foreach ($signatures as $sig) {
            if (hash_equals($expectedSignature, $sig)) {
                return json_decode($payload, true);
            }
        }

        return null;
    }

    /**
     * Stripe API Request
     */
    private function request(string $method, string $endpoint, array $data = []): ?array
    {
        if (!$this->isConfigured()) {
            return null;
        }

        $url = 'https://api.stripe.com' . $endpoint;

        $ch = curl_init();

        $headers = [
            'Authorization: Bearer ' . $this->secretKey,
            'Content-Type: application/x-www-form-urlencoded',
        ];

        curl_setopt_array($ch, [
            CURLOPT_URL => $url . ($method === 'GET' && $data ? '?' . http_build_query($data) : ''),
            CURLOPT_RETURNTRANSFER => true,
            CURLOPT_HTTPHEADER => $headers,
            CURLOPT_TIMEOUT => 30,
        ]);

        if ($method === 'POST') {
            curl_setopt($ch, CURLOPT_POST, true);
            curl_setopt($ch, CURLOPT_POSTFIELDS, http_build_query($data));
        } elseif ($method === 'DELETE') {
            curl_setopt($ch, CURLOPT_CUSTOMREQUEST, 'DELETE');
        }

        $response = curl_exec($ch);
        $httpCode = curl_getinfo($ch, CURLINFO_HTTP_CODE);
        curl_close($ch);

        if ($httpCode >= 200 && $httpCode < 300) {
            return json_decode($response, true);
        }

        // Log error
        error_log("Stripe API Error ($httpCode): $response");
        return null;
    }
}
