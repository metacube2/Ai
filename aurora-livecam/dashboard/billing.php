<?php
/**
 * Dashboard - Abrechnung
 */

require_once dirname(__DIR__) . '/vendor/autoload.php';
require_once dirname(__DIR__) . '/SettingsManager.php';

if (file_exists(dirname(__DIR__) . '/src/bootstrap.php')) {
    require_once dirname(__DIR__) . '/src/bootstrap.php';
}

use AuroraLivecam\Auth\AuthManager;
use AuroraLivecam\Billing\StripeService;
use AuroraLivecam\Billing\SubscriptionManager;

$settingsManager = new SettingsManager();
$auth = new AuthManager();
$auth->requireLogin();

// Prüfe ob Billing aktiviert
if (!$settingsManager->isBillingEnabled()) {
    header('Location: /dashboard/');
    exit;
}

$user = $auth->getUser();
$tenantId = $user['tenant_id'] ?? 0;

$flashMessage = null;
$flashType = 'info';

$stripe = new StripeService();
$subscriptions = new SubscriptionManager();

// Aktuelle Subscription
$currentSub = null;
$plans = [];
$invoices = [];
$trialDays = 0;

try {
    $currentSub = $subscriptions->getSubscription($tenantId);
    $plans = $subscriptions->getPlans();
    $invoices = $subscriptions->getInvoices($tenantId, 5);
    $trialDays = $subscriptions->getTrialDaysRemaining($tenantId);
} catch (\Exception $e) {
    $flashMessage = 'Fehler beim Laden der Abrechnungsdaten';
    $flashType = 'error';
}

// Checkout starten
if ($_SERVER['REQUEST_METHOD'] === 'POST' && isset($_POST['plan_id'])) {
    $planId = (int)$_POST['plan_id'];
    $plan = $subscriptions->getPlan($planId);

    if ($plan && !empty($plan['stripe_price_id'])) {
        $baseUrl = (isset($_SERVER['HTTPS']) ? 'https' : 'http') . '://' . $_SERVER['HTTP_HOST'];
        $session = $stripe->createCheckoutSession(
            $tenantId,
            $plan['stripe_price_id'],
            $baseUrl . '/dashboard/billing.php?success=1',
            $baseUrl . '/dashboard/billing.php?canceled=1'
        );

        if ($session && isset($session['url'])) {
            header('Location: ' . $session['url']);
            exit;
        } else {
            $flashMessage = 'Fehler beim Erstellen der Checkout-Session';
            $flashType = 'error';
        }
    }
}

// Billing Portal öffnen
if (isset($_GET['portal'])) {
    $baseUrl = (isset($_SERVER['HTTPS']) ? 'https' : 'http') . '://' . $_SERVER['HTTP_HOST'];
    $session = $stripe->createPortalSession($tenantId, $baseUrl . '/dashboard/billing.php');

    if ($session && isset($session['url'])) {
        header('Location: ' . $session['url']);
        exit;
    }
}

// Success/Cancel Messages
if (isset($_GET['success'])) {
    $flashMessage = 'Zahlung erfolgreich! Ihr Abo ist jetzt aktiv.';
    $flashType = 'success';
}
if (isset($_GET['canceled'])) {
    $flashMessage = 'Checkout abgebrochen.';
    $flashType = 'warning';
}

$pageTitle = 'Abrechnung';
$currentPage = 'billing';

ob_start();
?>

<!-- Aktueller Plan -->
<div class="card">
    <div class="card-header">
        <h3 class="card-title">Aktueller Plan</h3>
        <?php if ($currentSub): ?>
        <span class="badge badge-<?php echo $currentSub['status'] === 'active' ? 'success' : ($currentSub['status'] === 'trialing' ? 'warning' : 'danger'); ?>">
            <?php echo ucfirst($currentSub['status']); ?>
        </span>
        <?php endif; ?>
    </div>
    <div class="card-body">
        <?php if ($currentSub): ?>
        <div style="display: flex; justify-content: space-between; align-items: center; flex-wrap: wrap; gap: 1rem;">
            <div>
                <h2 style="margin: 0; font-size: 1.75rem;"><?php echo htmlspecialchars($currentSub['plan_name'] ?? 'Free'); ?></h2>
                <?php if ($currentSub['status'] === 'trialing' && $trialDays > 0): ?>
                <p style="color: var(--warning); margin: 0.5rem 0 0 0;">
                    Trial endet in <?php echo $trialDays; ?> Tag<?php echo $trialDays !== 1 ? 'en' : ''; ?>
                </p>
                <?php elseif ($currentSub['current_period_end']): ?>
                <p style="color: var(--gray-500); margin: 0.5rem 0 0 0;">
                    Nächste Abrechnung: <?php echo date('d.m.Y', strtotime($currentSub['current_period_end'])); ?>
                </p>
                <?php endif; ?>
            </div>
            <?php if ($stripe->isConfigured() && !empty($currentSub['stripe_customer_id'])): ?>
            <a href="?portal=1" class="btn btn-secondary">
                Abo verwalten
            </a>
            <?php endif; ?>
        </div>

        <?php if (!empty($currentSub['plan_features'])): ?>
        <div style="margin-top: 1.5rem; padding-top: 1.5rem; border-top: 1px solid var(--gray-200);">
            <h4 style="font-size: 0.875rem; color: var(--gray-500); margin-bottom: 0.75rem;">Enthaltene Features:</h4>
            <div style="display: flex; flex-wrap: wrap; gap: 0.5rem;">
                <?php foreach ($currentSub['plan_features'] as $feature => $value): ?>
                <?php if ($value): ?>
                <span class="badge badge-info">
                    <?php
                    $labels = [
                        'max_viewers' => 'Max. Zuschauer: ' . ($value === -1 ? '∞' : $value),
                        'storage_gb' => 'Speicher: ' . $value . ' GB',
                        'custom_domain' => 'Custom Domain',
                        'weather_widget' => 'Wetter-Widget',
                        'timelapse' => 'Timelapse',
                        'analytics' => 'Analytics',
                        'branding' => 'Custom Branding',
                        'priority_support' => 'Priority Support',
                    ];
                    echo $labels[$feature] ?? ucfirst(str_replace('_', ' ', $feature));
                    ?>
                </span>
                <?php endif; ?>
                <?php endforeach; ?>
            </div>
        </div>
        <?php endif; ?>
        <?php else: ?>
        <p style="color: var(--gray-500);">Kein aktives Abo</p>
        <?php endif; ?>
    </div>
</div>

<!-- Verfügbare Pläne -->
<?php if (!empty($plans)): ?>
<div class="card">
    <div class="card-header">
        <h3 class="card-title">Verfügbare Pläne</h3>
    </div>
    <div class="card-body">
        <div style="display: grid; grid-template-columns: repeat(auto-fit, minmax(250px, 1fr)); gap: 1.5rem;">
            <?php foreach ($plans as $plan): ?>
            <?php $isCurrent = $currentSub && $currentSub['plan_id'] == $plan['id']; ?>
            <div style="border: 2px solid <?php echo $isCurrent ? 'var(--primary)' : 'var(--gray-200)'; ?>; border-radius: 0.75rem; padding: 1.5rem; <?php echo $isCurrent ? 'background: rgba(102,126,234,0.05);' : ''; ?>">
                <h4 style="margin: 0 0 0.5rem 0;"><?php echo htmlspecialchars($plan['name']); ?></h4>
                <div style="font-size: 2rem; font-weight: 700; color: var(--gray-900);">
                    <?php if ($plan['price_monthly'] > 0): ?>
                    CHF <?php echo number_format($plan['price_monthly'], 0); ?>
                    <span style="font-size: 1rem; font-weight: 400; color: var(--gray-500);">/Monat</span>
                    <?php else: ?>
                    Kostenlos
                    <?php endif; ?>
                </div>

                <?php if (!empty($plan['features'])): ?>
                <ul style="list-style: none; padding: 0; margin: 1rem 0; font-size: 0.875rem;">
                    <?php foreach ($plan['features'] as $feature => $value): ?>
                    <?php if ($value): ?>
                    <li style="padding: 0.25rem 0; color: var(--gray-600);">
                        ✓ <?php
                        $labels = [
                            'max_viewers' => 'Bis ' . ($value === -1 ? 'unbegrenzt' : $value) . ' Zuschauer',
                            'storage_gb' => $value . ' GB Speicher',
                            'custom_domain' => 'Eigene Domain',
                            'weather_widget' => 'Wetter-Widget',
                            'timelapse' => 'Timelapse',
                            'analytics' => 'Analytics',
                            'branding' => 'Custom Branding',
                            'priority_support' => 'Priority Support',
                        ];
                        echo $labels[$feature] ?? ucfirst(str_replace('_', ' ', $feature));
                        ?>
                    </li>
                    <?php endif; ?>
                    <?php endforeach; ?>
                </ul>
                <?php endif; ?>

                <?php if ($isCurrent): ?>
                <button class="btn btn-secondary" style="width: 100%;" disabled>Aktueller Plan</button>
                <?php elseif ($plan['price_monthly'] > 0 && $stripe->isConfigured()): ?>
                <form method="POST" action="">
                    <input type="hidden" name="plan_id" value="<?php echo $plan['id']; ?>">
                    <button type="submit" class="btn btn-primary" style="width: 100%;">
                        Upgrade
                    </button>
                </form>
                <?php elseif ($plan['price_monthly'] == 0): ?>
                <button class="btn btn-secondary" style="width: 100%;" disabled>Free Plan</button>
                <?php else: ?>
                <button class="btn btn-secondary" style="width: 100%;" disabled>Stripe nicht konfiguriert</button>
                <?php endif; ?>
            </div>
            <?php endforeach; ?>
        </div>
    </div>
</div>
<?php endif; ?>

<!-- Rechnungen -->
<?php if (!empty($invoices)): ?>
<div class="card">
    <div class="card-header">
        <h3 class="card-title">Rechnungen</h3>
    </div>
    <div class="card-body">
        <table class="table">
            <thead>
                <tr>
                    <th>Datum</th>
                    <th>Betrag</th>
                    <th>Status</th>
                    <th>PDF</th>
                </tr>
            </thead>
            <tbody>
                <?php foreach ($invoices as $invoice): ?>
                <tr>
                    <td><?php echo date('d.m.Y', strtotime($invoice['created_at'])); ?></td>
                    <td><?php echo $invoice['currency']; ?> <?php echo number_format($invoice['amount'], 2); ?></td>
                    <td>
                        <span class="badge badge-<?php echo $invoice['status'] === 'paid' ? 'success' : 'warning'; ?>">
                            <?php echo ucfirst($invoice['status']); ?>
                        </span>
                    </td>
                    <td>
                        <?php if ($invoice['invoice_pdf_url']): ?>
                        <a href="<?php echo htmlspecialchars($invoice['invoice_pdf_url']); ?>" target="_blank" class="btn btn-sm btn-secondary">
                            Download
                        </a>
                        <?php endif; ?>
                    </td>
                </tr>
                <?php endforeach; ?>
            </tbody>
        </table>
    </div>
</div>
<?php endif; ?>

<?php if (!$stripe->isConfigured()): ?>
<div class="alert alert-warning">
    <strong>Hinweis:</strong> Stripe ist noch nicht konfiguriert. Bitte fügen Sie Ihre Stripe API-Keys in config.php hinzu.
</div>
<?php endif; ?>

<?php
$content = ob_get_clean();
include __DIR__ . '/templates/layout.php';
