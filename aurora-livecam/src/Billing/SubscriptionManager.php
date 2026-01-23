<?php
/**
 * SubscriptionManager - Verwaltet Subscriptions
 */

namespace AuroraLivecam\Billing;

use AuroraLivecam\Core\Database;

class SubscriptionManager
{
    private Database $db;
    private StripeService $stripe;

    public function __construct(?Database $db = null)
    {
        $this->db = $db ?? Database::getInstance();
        $this->stripe = new StripeService($this->db);
    }

    /**
     * Gibt alle Pläne zurück
     */
    public function getPlans(bool $activeOnly = true): array
    {
        $sql = "SELECT * FROM plans";
        if ($activeOnly) {
            $sql .= " WHERE is_active = 1";
        }
        $sql .= " ORDER BY sort_order ASC";

        $plans = $this->db->fetchAll($sql);

        // Features JSON decodieren
        foreach ($plans as &$plan) {
            if (isset($plan['features'])) {
                $plan['features'] = json_decode($plan['features'], true) ?? [];
            }
        }

        return $plans;
    }

    /**
     * Gibt einen Plan zurück
     */
    public function getPlan(int $planId): ?array
    {
        $plan = $this->db->fetchOne("SELECT * FROM plans WHERE id = ?", [$planId]);

        if ($plan && isset($plan['features'])) {
            $plan['features'] = json_decode($plan['features'], true) ?? [];
        }

        return $plan;
    }

    /**
     * Gibt Plan by Slug zurück
     */
    public function getPlanBySlug(string $slug): ?array
    {
        $plan = $this->db->fetchOne("SELECT * FROM plans WHERE slug = ?", [$slug]);

        if ($plan && isset($plan['features'])) {
            $plan['features'] = json_decode($plan['features'], true) ?? [];
        }

        return $plan;
    }

    /**
     * Gibt die aktuelle Subscription eines Tenants zurück
     */
    public function getSubscription(int $tenantId): ?array
    {
        $sub = $this->db->fetchOne(
            "SELECT s.*, p.name as plan_name, p.slug as plan_slug, p.features as plan_features
             FROM subscriptions s
             JOIN plans p ON s.plan_id = p.id
             WHERE s.tenant_id = ?
             ORDER BY s.created_at DESC LIMIT 1",
            [$tenantId]
        );

        if ($sub && isset($sub['plan_features'])) {
            $sub['plan_features'] = json_decode($sub['plan_features'], true) ?? [];
        }

        return $sub;
    }

    /**
     * Erstellt oder aktualisiert eine Subscription
     */
    public function createOrUpdate(int $tenantId, array $data): int
    {
        $existing = $this->db->fetchOne(
            "SELECT id FROM subscriptions WHERE tenant_id = ?",
            [$tenantId]
        );

        if ($existing) {
            $this->db->update('subscriptions', $data, 'id = ?', [$existing['id']]);
            return $existing['id'];
        }

        $data['tenant_id'] = $tenantId;
        return $this->db->insert('subscriptions', $data);
    }

    /**
     * Startet Trial für einen Tenant
     */
    public function startTrial(int $tenantId, int $planId = null, int $days = 14): void
    {
        if (!$planId) {
            $freePlan = $this->getPlanBySlug('basic');
            $planId = $freePlan['id'] ?? 1;
        }

        $this->createOrUpdate($tenantId, [
            'plan_id' => $planId,
            'status' => 'trialing',
            'current_period_start' => date('Y-m-d H:i:s'),
            'current_period_end' => date('Y-m-d H:i:s', strtotime("+$days days")),
        ]);

        // Tenant Status
        $this->db->update('tenants', [
            'status' => 'trial',
            'trial_ends_at' => date('Y-m-d H:i:s', strtotime("+$days days")),
        ], 'id = ?', [$tenantId]);
    }

    /**
     * Aktiviert Subscription nach Zahlung
     */
    public function activate(int $tenantId, string $stripeSubscriptionId, int $planId): void
    {
        $this->createOrUpdate($tenantId, [
            'plan_id' => $planId,
            'stripe_subscription_id' => $stripeSubscriptionId,
            'status' => 'active',
            'current_period_start' => date('Y-m-d H:i:s'),
        ]);

        $this->db->update('tenants', ['status' => 'active', 'plan_id' => $planId], 'id = ?', [$tenantId]);
    }

    /**
     * Kündigt Subscription
     */
    public function cancel(int $tenantId, bool $immediately = false): bool
    {
        $sub = $this->getSubscription($tenantId);

        if (!$sub) {
            return false;
        }

        // Bei Stripe kündigen
        if (!empty($sub['stripe_subscription_id'])) {
            $this->stripe->cancelSubscription($sub['stripe_subscription_id'], $immediately);
        }

        $status = $immediately ? 'canceled' : 'active'; // Bleibt aktiv bis Periodenende

        $this->db->update('subscriptions', [
            'status' => $status,
            'canceled_at' => date('Y-m-d H:i:s'),
        ], 'tenant_id = ?', [$tenantId]);

        if ($immediately) {
            $this->db->update('tenants', ['status' => 'cancelled'], 'id = ?', [$tenantId]);
        }

        return true;
    }

    /**
     * Prüft ob Tenant aktiv ist (Trial oder bezahlt)
     */
    public function isActive(int $tenantId): bool
    {
        $sub = $this->getSubscription($tenantId);

        if (!$sub) {
            return false;
        }

        if ($sub['status'] === 'active') {
            return true;
        }

        if ($sub['status'] === 'trialing') {
            $endDate = strtotime($sub['current_period_end']);
            return $endDate > time();
        }

        return false;
    }

    /**
     * Gibt verbleibende Trial-Tage zurück
     */
    public function getTrialDaysRemaining(int $tenantId): int
    {
        $tenant = $this->db->fetchOne(
            "SELECT trial_ends_at FROM tenants WHERE id = ?",
            [$tenantId]
        );

        if (!$tenant || !$tenant['trial_ends_at']) {
            return 0;
        }

        $remaining = strtotime($tenant['trial_ends_at']) - time();
        return max(0, (int)ceil($remaining / 86400));
    }

    /**
     * Prüft Feature-Zugriff
     */
    public function hasFeature(int $tenantId, string $feature): bool
    {
        $sub = $this->getSubscription($tenantId);

        if (!$sub || !isset($sub['plan_features'])) {
            return false;
        }

        return !empty($sub['plan_features'][$feature]);
    }

    /**
     * Gibt Feature-Limit zurück
     */
    public function getFeatureLimit(int $tenantId, string $feature): int
    {
        $sub = $this->getSubscription($tenantId);

        if (!$sub || !isset($sub['plan_features'][$feature])) {
            return 0;
        }

        $value = $sub['plan_features'][$feature];

        // -1 = unlimited
        if ($value === -1 || $value === true) {
            return PHP_INT_MAX;
        }

        return (int)$value;
    }

    /**
     * Speichert Rechnung
     */
    public function saveInvoice(int $tenantId, array $invoiceData): void
    {
        $this->db->insert('invoices', [
            'tenant_id' => $tenantId,
            'stripe_invoice_id' => $invoiceData['id'] ?? null,
            'amount' => ($invoiceData['amount_paid'] ?? 0) / 100,
            'currency' => strtoupper($invoiceData['currency'] ?? 'CHF'),
            'status' => $invoiceData['status'] ?? 'unknown',
            'paid_at' => isset($invoiceData['status_transitions']['paid_at'])
                ? date('Y-m-d H:i:s', $invoiceData['status_transitions']['paid_at'])
                : null,
            'invoice_pdf_url' => $invoiceData['invoice_pdf'] ?? null,
        ]);
    }

    /**
     * Gibt Rechnungen eines Tenants zurück
     */
    public function getInvoices(int $tenantId, int $limit = 10): array
    {
        return $this->db->fetchAll(
            "SELECT * FROM invoices WHERE tenant_id = ? ORDER BY created_at DESC LIMIT ?",
            [$tenantId, $limit]
        );
    }
}
