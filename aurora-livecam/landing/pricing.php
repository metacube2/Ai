<?php
/**
 * Landing Page - Preise
 */

require_once dirname(__DIR__) . '/vendor/autoload.php';
require_once dirname(__DIR__) . '/SettingsManager.php';

if (file_exists(dirname(__DIR__) . '/src/bootstrap.php')) {
    require_once dirname(__DIR__) . '/src/bootstrap.php';
}

use AuroraLivecam\Billing\SubscriptionManager;

$settingsManager = new SettingsManager();

// Pläne laden
$plans = [];
try {
    $subscriptions = new SubscriptionManager();
    $plans = $subscriptions->getPlans();
} catch (\Exception $e) {
    // Fallback-Pläne
    $plans = [
        ['name' => 'Free', 'slug' => 'free', 'price_monthly' => 0, 'features' => ['max_viewers' => 10, 'weather_widget' => true]],
        ['name' => 'Basic', 'slug' => 'basic', 'price_monthly' => 19, 'features' => ['max_viewers' => 50, 'weather_widget' => true, 'timelapse' => true, 'analytics' => true]],
        ['name' => 'Professional', 'slug' => 'professional', 'price_monthly' => 49, 'features' => ['max_viewers' => 200, 'custom_domain' => true, 'weather_widget' => true, 'timelapse' => true, 'analytics' => true, 'branding' => true]],
        ['name' => 'Enterprise', 'slug' => 'enterprise', 'price_monthly' => 149, 'features' => ['max_viewers' => -1, 'custom_domain' => true, 'weather_widget' => true, 'timelapse' => true, 'analytics' => true, 'branding' => true, 'priority_support' => true]],
    ];
}

$trialDays = $settingsManager->getTrialDays();

// Feature-Labels
$featureLabels = [
    'max_viewers' => 'Gleichzeitige Zuschauer',
    'storage_gb' => 'Speicherplatz',
    'custom_domain' => 'Eigene Domain',
    'weather_widget' => 'Wetter-Widget',
    'timelapse' => 'Timelapse',
    'analytics' => 'Analytics & Statistiken',
    'branding' => 'Custom Branding',
    'priority_support' => 'Priority Support',
];
?>
<!DOCTYPE html>
<html lang="de">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Preise - Aurora Livecam</title>
    <link rel="stylesheet" href="/dashboard/assets/dashboard.css">
    <style>
        :root {
            --gradient: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
        }

        * { margin: 0; padding: 0; box-sizing: border-box; }

        body {
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
            line-height: 1.6;
            color: #1a202c;
            background: #f7fafc;
        }

        .header {
            background: white;
            border-bottom: 1px solid #e2e8f0;
            padding: 1rem 2rem;
        }

        .header-inner {
            max-width: 1200px;
            margin: 0 auto;
            display: flex;
            justify-content: space-between;
            align-items: center;
        }

        .logo {
            font-size: 1.5rem;
            font-weight: 700;
            background: var(--gradient);
            -webkit-background-clip: text;
            -webkit-text-fill-color: transparent;
            text-decoration: none;
        }

        .nav-links a {
            color: #4a5568;
            text-decoration: none;
            margin-left: 1.5rem;
        }

        .page-header {
            text-align: center;
            padding: 4rem 2rem;
            background: var(--gradient);
            color: white;
        }

        .page-header h1 {
            font-size: 2.5rem;
            margin-bottom: 1rem;
        }

        .page-header p {
            font-size: 1.1rem;
            opacity: 0.9;
        }

        .pricing-toggle {
            display: flex;
            justify-content: center;
            gap: 1rem;
            margin-top: 2rem;
            align-items: center;
        }

        .pricing-toggle span {
            font-size: 0.9rem;
        }

        .pricing-toggle .active {
            font-weight: 600;
        }

        .toggle-switch {
            width: 60px;
            height: 30px;
            background: rgba(255,255,255,0.3);
            border-radius: 15px;
            position: relative;
            cursor: pointer;
        }

        .toggle-switch::after {
            content: '';
            position: absolute;
            width: 26px;
            height: 26px;
            background: white;
            border-radius: 50%;
            top: 2px;
            left: 2px;
            transition: 0.3s;
        }

        .toggle-switch.yearly::after {
            left: 32px;
        }

        .save-badge {
            background: #48bb78;
            padding: 0.25rem 0.5rem;
            border-radius: 0.25rem;
            font-size: 0.75rem;
            font-weight: 600;
        }

        .pricing-container {
            max-width: 1200px;
            margin: -3rem auto 4rem;
            padding: 0 2rem;
        }

        .pricing-grid {
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(280px, 1fr));
            gap: 1.5rem;
        }

        .pricing-card {
            background: white;
            border-radius: 1rem;
            padding: 2rem;
            box-shadow: 0 10px 40px rgba(0,0,0,0.1);
            position: relative;
            display: flex;
            flex-direction: column;
        }

        .pricing-card.featured {
            border: 2px solid #667eea;
            transform: scale(1.05);
        }

        .pricing-card.featured::before {
            content: 'Beliebt';
            position: absolute;
            top: -12px;
            left: 50%;
            transform: translateX(-50%);
            background: var(--gradient);
            color: white;
            padding: 0.25rem 1rem;
            border-radius: 1rem;
            font-size: 0.75rem;
            font-weight: 600;
        }

        .pricing-card h3 {
            font-size: 1.25rem;
            margin-bottom: 0.5rem;
        }

        .pricing-card .price {
            font-size: 3rem;
            font-weight: 800;
            margin: 1rem 0;
        }

        .pricing-card .price span {
            font-size: 1rem;
            font-weight: 400;
            color: #718096;
        }

        .pricing-card .price-yearly {
            display: none;
        }

        .yearly-mode .price-monthly { display: none; }
        .yearly-mode .price-yearly { display: block; }

        .pricing-card ul {
            list-style: none;
            flex: 1;
            margin: 1.5rem 0;
        }

        .pricing-card li {
            padding: 0.5rem 0;
            color: #4a5568;
            display: flex;
            align-items: center;
            gap: 0.5rem;
        }

        .pricing-card li.included::before {
            content: '✓';
            color: #48bb78;
            font-weight: bold;
        }

        .pricing-card li.not-included {
            color: #a0aec0;
            text-decoration: line-through;
        }

        .pricing-card li.not-included::before {
            content: '✗';
            color: #e53e3e;
        }

        .pricing-card .btn {
            width: 100%;
            padding: 1rem;
            border: none;
            border-radius: 0.5rem;
            font-size: 1rem;
            font-weight: 600;
            cursor: pointer;
            text-decoration: none;
            text-align: center;
            transition: all 0.2s;
        }

        .pricing-card .btn-primary {
            background: var(--gradient);
            color: white;
        }

        .pricing-card .btn-primary:hover {
            transform: translateY(-2px);
            box-shadow: 0 4px 12px rgba(102, 126, 234, 0.4);
        }

        .pricing-card .btn-secondary {
            background: #e2e8f0;
            color: #4a5568;
        }

        .faq {
            max-width: 800px;
            margin: 0 auto 4rem;
            padding: 0 2rem;
        }

        .faq h2 {
            text-align: center;
            margin-bottom: 2rem;
        }

        .faq-item {
            background: white;
            border-radius: 0.5rem;
            margin-bottom: 1rem;
            overflow: hidden;
        }

        .faq-question {
            padding: 1.25rem;
            font-weight: 600;
            cursor: pointer;
            display: flex;
            justify-content: space-between;
            align-items: center;
        }

        .faq-answer {
            padding: 0 1.25rem 1.25rem;
            color: #718096;
            display: none;
        }

        .faq-item.open .faq-answer {
            display: block;
        }

        .footer {
            background: #1a202c;
            color: #a0aec0;
            padding: 2rem;
            text-align: center;
        }

        @media (max-width: 768px) {
            .pricing-card.featured {
                transform: none;
            }
            .pricing-grid {
                grid-template-columns: 1fr;
            }
        }
    </style>
</head>
<body>
    <header class="header">
        <div class="header-inner">
            <a href="/landing/" class="logo">Aurora Livecam</a>
            <nav class="nav-links">
                <a href="/landing/">Home</a>
                <a href="/dashboard/login.php">Login</a>
                <a href="/onboarding/register.php" class="btn btn-primary btn-sm">Kostenlos starten</a>
            </nav>
        </div>
    </header>

    <section class="page-header">
        <h1>Einfache, transparente Preise</h1>
        <p><?php echo $trialDays; ?> Tage kostenlos testen - jederzeit kündbar</p>

        <div class="pricing-toggle">
            <span class="monthly-label active">Monatlich</span>
            <div class="toggle-switch" id="billing-toggle"></div>
            <span class="yearly-label">Jährlich</span>
            <span class="save-badge">2 Monate gratis</span>
        </div>
    </section>

    <div class="pricing-container" id="pricing-container">
        <div class="pricing-grid">
            <?php foreach ($plans as $index => $plan): ?>
            <?php $isFeatured = $plan['slug'] === 'professional'; ?>
            <div class="pricing-card <?php echo $isFeatured ? 'featured' : ''; ?>">
                <h3><?php echo htmlspecialchars($plan['name']); ?></h3>

                <div class="price price-monthly">
                    <?php if ($plan['price_monthly'] > 0): ?>
                    CHF <?php echo number_format($plan['price_monthly'], 0); ?><span>/Monat</span>
                    <?php else: ?>
                    Kostenlos
                    <?php endif; ?>
                </div>

                <div class="price price-yearly">
                    <?php if (isset($plan['price_yearly']) && $plan['price_yearly'] > 0): ?>
                    CHF <?php echo number_format($plan['price_yearly'] / 12, 0); ?><span>/Monat</span>
                    <div style="font-size: 0.875rem; color: #718096;">
                        CHF <?php echo number_format($plan['price_yearly'], 0); ?> jährlich
                    </div>
                    <?php elseif ($plan['price_monthly'] > 0): ?>
                    CHF <?php echo number_format($plan['price_monthly'] * 10 / 12, 0); ?><span>/Monat</span>
                    <div style="font-size: 0.875rem; color: #718096;">
                        CHF <?php echo number_format($plan['price_monthly'] * 10, 0); ?> jährlich
                    </div>
                    <?php else: ?>
                    Kostenlos
                    <?php endif; ?>
                </div>

                <ul>
                    <?php
                    $features = is_array($plan['features']) ? $plan['features'] : json_decode($plan['features'], true) ?? [];
                    $allFeatures = ['max_viewers', 'weather_widget', 'timelapse', 'analytics', 'custom_domain', 'branding', 'priority_support'];

                    foreach ($allFeatures as $feature):
                        $hasFeature = !empty($features[$feature]);
                        $value = $features[$feature] ?? null;
                    ?>
                    <li class="<?php echo $hasFeature ? 'included' : 'not-included'; ?>">
                        <?php
                        if ($feature === 'max_viewers' && $value) {
                            echo $value === -1 ? 'Unbegrenzte Zuschauer' : "Bis $value Zuschauer";
                        } elseif ($feature === 'storage_gb' && $value) {
                            echo "$value GB Speicher";
                        } else {
                            echo $featureLabels[$feature] ?? ucfirst(str_replace('_', ' ', $feature));
                        }
                        ?>
                    </li>
                    <?php endforeach; ?>
                </ul>

                <a href="/onboarding/register.php?plan=<?php echo $plan['slug']; ?>"
                   class="btn <?php echo $isFeatured || $plan['price_monthly'] > 0 ? 'btn-primary' : 'btn-secondary'; ?>">
                    <?php echo $plan['price_monthly'] > 0 ? 'Jetzt starten' : 'Kostenlos starten'; ?>
                </a>
            </div>
            <?php endforeach; ?>
        </div>
    </div>

    <!-- FAQ -->
    <section class="faq">
        <h2>Häufige Fragen</h2>

        <div class="faq-item">
            <div class="faq-question">
                Kann ich jederzeit wechseln oder kündigen?
                <span>+</span>
            </div>
            <div class="faq-answer">
                Ja! Sie können Ihren Plan jederzeit upgraden oder downgraden. Bei einer Kündigung bleibt Ihr Zugang bis zum Ende der Abrechnungsperiode aktiv.
            </div>
        </div>

        <div class="faq-item">
            <div class="faq-question">
                Was passiert nach dem Trial?
                <span>+</span>
            </div>
            <div class="faq-answer">
                Nach Ablauf der <?php echo $trialDays; ?> Tage werden Sie automatisch auf den kostenlosen Plan umgestellt, sofern Sie kein Abo abschliessen. Keine Sorge, Ihre Daten bleiben erhalten.
            </div>
        </div>

        <div class="faq-item">
            <div class="faq-question">
                Welche Zahlungsmethoden werden akzeptiert?
                <span>+</span>
            </div>
            <div class="faq-answer">
                Wir akzeptieren alle gängigen Kreditkarten (Visa, Mastercard, American Express) sowie TWINT und Banküberweisung bei Jahresabos.
            </div>
        </div>

        <div class="faq-item">
            <div class="faq-question">
                Brauche ich technisches Wissen?
                <span>+</span>
            </div>
            <div class="faq-answer">
                Nein! Unser Onboarding-Wizard führt Sie Schritt für Schritt durch die Einrichtung. Sie benötigen lediglich eine Stream-URL (HLS/m3u8) von Ihrem Kamera-Anbieter.
            </div>
        </div>
    </section>

    <footer class="footer">
        © <?php echo date('Y'); ?> Aurora Livecam. Alle Rechte vorbehalten.
    </footer>

    <script>
    // Billing toggle
    const toggle = document.getElementById('billing-toggle');
    const container = document.getElementById('pricing-container');

    toggle.addEventListener('click', () => {
        toggle.classList.toggle('yearly');
        container.classList.toggle('yearly-mode');

        document.querySelector('.monthly-label').classList.toggle('active');
        document.querySelector('.yearly-label').classList.toggle('active');
    });

    // FAQ accordion
    document.querySelectorAll('.faq-question').forEach(q => {
        q.addEventListener('click', () => {
            q.parentElement.classList.toggle('open');
            q.querySelector('span').textContent = q.parentElement.classList.contains('open') ? '−' : '+';
        });
    });
    </script>
</body>
</html>
