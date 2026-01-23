<?php
/**
 * WebhookHandler - Verarbeitet Stripe Webhooks
 */

namespace AuroraLivecam\Billing;

use AuroraLivecam\Core\Database;

class WebhookHandler
{
    private Database $db;
    private StripeService $stripe;
    private SubscriptionManager $subscriptions;

    public function __construct(?Database $db = null)
    {
        $this->db = $db ?? Database::getInstance();
        $this->stripe = new StripeService($this->db);
        $this->subscriptions = new SubscriptionManager($this->db);
    }

    /**
     * Verarbeitet einen Webhook
     */
    public function handle(string $payload, string $signature): array
    {
        // Signatur verifizieren
        $event = $this->stripe->verifyWebhook($payload, $signature);

        if (!$event) {
            return ['success' => false, 'error' => 'Invalid signature'];
        }

        $type = $event['type'] ?? '';
        $data = $event['data']['object'] ?? [];

        try {
            switch ($type) {
                case 'checkout.session.completed':
                    return $this->handleCheckoutComplete($data);

                case 'customer.subscription.created':
                case 'customer.subscription.updated':
                    return $this->handleSubscriptionUpdate($data);

                case 'customer.subscription.deleted':
                    return $this->handleSubscriptionDeleted($data);

                case 'invoice.paid':
                    return $this->handleInvoicePaid($data);

                case 'invoice.payment_failed':
                    return $this->handlePaymentFailed($data);

                default:
                    return ['success' => true, 'message' => 'Event ignored: ' . $type];
            }
        } catch (\Exception $e) {
            error_log("Webhook error: " . $e->getMessage());
            return ['success' => false, 'error' => $e->getMessage()];
        }
    }

    /**
     * Checkout abgeschlossen
     */
    private function handleCheckoutComplete(array $session): array
    {
        $tenantId = $session['metadata']['tenant_id'] ?? null;
        $subscriptionId = $session['subscription'] ?? null;

        if (!$tenantId || !$subscriptionId) {
            return ['success' => false, 'error' => 'Missing tenant_id or subscription'];
        }

        // Subscription-Details von Stripe holen
        $subscription = $this->stripe->getSubscription($subscriptionId);

        if (!$subscription) {
            return ['success' => false, 'error' => 'Could not fetch subscription'];
        }

        // Plan aus Stripe Price ID ermitteln
        $priceId = $subscription['items']['data'][0]['price']['id'] ?? null;
        $plan = $this->db->fetchOne(
            "SELECT id FROM plans WHERE stripe_price_id = ?",
            [$priceId]
        );

        $planId = $plan['id'] ?? 1;

        // Subscription aktivieren
        $this->subscriptions->activate($tenantId, $subscriptionId, $planId);

        // Customer ID speichern
        $this->db->update('subscriptions', [
            'stripe_customer_id' => $session['customer'],
        ], 'tenant_id = ?', [$tenantId]);

        return ['success' => true, 'message' => 'Subscription activated'];
    }

    /**
     * Subscription erstellt/aktualisiert
     */
    private function handleSubscriptionUpdate(array $subscription): array
    {
        $customerId = $subscription['customer'] ?? null;

        if (!$customerId) {
            return ['success' => false, 'error' => 'No customer ID'];
        }

        // Tenant über Customer ID finden
        $sub = $this->db->fetchOne(
            "SELECT tenant_id FROM subscriptions WHERE stripe_customer_id = ?",
            [$customerId]
        );

        if (!$sub) {
            return ['success' => true, 'message' => 'Customer not found in DB'];
        }

        $tenantId = $sub['tenant_id'];
        $status = $this->mapStripeStatus($subscription['status']);

        $this->db->update('subscriptions', [
            'stripe_subscription_id' => $subscription['id'],
            'status' => $status,
            'current_period_start' => date('Y-m-d H:i:s', $subscription['current_period_start']),
            'current_period_end' => date('Y-m-d H:i:s', $subscription['current_period_end']),
        ], 'tenant_id = ?', [$tenantId]);

        // Tenant-Status aktualisieren
        $tenantStatus = in_array($status, ['active', 'trialing']) ? 'active' : 'suspended';
        $this->db->update('tenants', ['status' => $tenantStatus], 'id = ?', [$tenantId]);

        return ['success' => true, 'message' => 'Subscription updated'];
    }

    /**
     * Subscription gelöscht/gekündigt
     */
    private function handleSubscriptionDeleted(array $subscription): array
    {
        $customerId = $subscription['customer'] ?? null;

        if (!$customerId) {
            return ['success' => false, 'error' => 'No customer ID'];
        }

        $sub = $this->db->fetchOne(
            "SELECT tenant_id FROM subscriptions WHERE stripe_customer_id = ?",
            [$customerId]
        );

        if (!$sub) {
            return ['success' => true, 'message' => 'Customer not found'];
        }

        $this->db->update('subscriptions', [
            'status' => 'canceled',
            'canceled_at' => date('Y-m-d H:i:s'),
        ], 'tenant_id = ?', [$sub['tenant_id']]);

        // Downgrade zu Free-Plan
        $freePlan = $this->db->fetchOne("SELECT id FROM plans WHERE slug = 'free'");
        if ($freePlan) {
            $this->db->update('tenants', [
                'status' => 'active',
                'plan_id' => $freePlan['id'],
            ], 'id = ?', [$sub['tenant_id']]);
        }

        return ['success' => true, 'message' => 'Subscription canceled'];
    }

    /**
     * Rechnung bezahlt
     */
    private function handleInvoicePaid(array $invoice): array
    {
        $customerId = $invoice['customer'] ?? null;

        if (!$customerId) {
            return ['success' => false, 'error' => 'No customer ID'];
        }

        $sub = $this->db->fetchOne(
            "SELECT tenant_id FROM subscriptions WHERE stripe_customer_id = ?",
            [$customerId]
        );

        if (!$sub) {
            return ['success' => true, 'message' => 'Customer not found'];
        }

        // Rechnung speichern
        $this->subscriptions->saveInvoice($sub['tenant_id'], $invoice);

        return ['success' => true, 'message' => 'Invoice saved'];
    }

    /**
     * Zahlung fehlgeschlagen
     */
    private function handlePaymentFailed(array $invoice): array
    {
        $customerId = $invoice['customer'] ?? null;

        if (!$customerId) {
            return ['success' => false, 'error' => 'No customer ID'];
        }

        $sub = $this->db->fetchOne(
            "SELECT tenant_id FROM subscriptions WHERE stripe_customer_id = ?",
            [$customerId]
        );

        if (!$sub) {
            return ['success' => true, 'message' => 'Customer not found'];
        }

        // Status auf past_due setzen
        $this->db->update('subscriptions', ['status' => 'past_due'], 'tenant_id = ?', [$sub['tenant_id']]);

        // TODO: E-Mail an Tenant senden

        return ['success' => true, 'message' => 'Payment failure recorded'];
    }

    /**
     * Mappt Stripe-Status auf DB-Status
     */
    private function mapStripeStatus(string $stripeStatus): string
    {
        $map = [
            'active' => 'active',
            'trialing' => 'trialing',
            'past_due' => 'past_due',
            'canceled' => 'canceled',
            'unpaid' => 'unpaid',
            'incomplete' => 'incomplete',
            'incomplete_expired' => 'canceled',
        ];

        return $map[$stripeStatus] ?? 'unknown';
    }
}
