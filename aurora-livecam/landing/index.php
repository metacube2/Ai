<?php
/**
 * Landing Page - Marketing Seite
 */

require_once dirname(__DIR__) . '/vendor/autoload.php';
require_once dirname(__DIR__) . '/SettingsManager.php';

$settingsManager = new SettingsManager();

// Prüfe ob Landing Page aktiviert
if (!$settingsManager->isLandingPageEnabled()) {
    header('Location: /');
    exit;
}

$trialDays = $settingsManager->getTrialDays();
?>
<!DOCTYPE html>
<html lang="de">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Aurora Livecam - Ihre Webcam als Service</title>
    <meta name="description" content="Erstellen Sie Ihre eigene Live-Webcam in wenigen Minuten. Wetter-Widget, Timelapse, Analytics und mehr. Jetzt kostenlos testen!">
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
        }

        /* Header */
        .header {
            position: fixed;
            top: 0;
            left: 0;
            right: 0;
            background: rgba(255,255,255,0.95);
            backdrop-filter: blur(10px);
            z-index: 100;
            border-bottom: 1px solid #e2e8f0;
        }

        .header-inner {
            max-width: 1200px;
            margin: 0 auto;
            padding: 1rem 2rem;
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

        .nav-links {
            display: flex;
            gap: 2rem;
            align-items: center;
        }

        .nav-links a {
            color: #4a5568;
            text-decoration: none;
            font-weight: 500;
            transition: color 0.2s;
        }

        .nav-links a:hover {
            color: #667eea;
        }

        /* Hero */
        .hero {
            padding: 8rem 2rem 6rem;
            background: var(--gradient);
            color: white;
            text-align: center;
        }

        .hero h1 {
            font-size: 3rem;
            font-weight: 800;
            margin-bottom: 1.5rem;
            max-width: 800px;
            margin-left: auto;
            margin-right: auto;
        }

        .hero p {
            font-size: 1.25rem;
            opacity: 0.9;
            max-width: 600px;
            margin: 0 auto 2rem;
        }

        .hero-buttons {
            display: flex;
            gap: 1rem;
            justify-content: center;
            flex-wrap: wrap;
        }

        .btn-hero {
            padding: 1rem 2rem;
            border-radius: 0.5rem;
            font-size: 1.1rem;
            font-weight: 600;
            text-decoration: none;
            transition: transform 0.2s, box-shadow 0.2s;
        }

        .btn-hero-primary {
            background: white;
            color: #667eea;
        }

        .btn-hero-secondary {
            background: rgba(255,255,255,0.2);
            color: white;
            border: 2px solid rgba(255,255,255,0.5);
        }

        .btn-hero:hover {
            transform: translateY(-2px);
            box-shadow: 0 10px 20px rgba(0,0,0,0.2);
        }

        .trial-badge {
            display: inline-block;
            background: rgba(255,255,255,0.2);
            padding: 0.5rem 1rem;
            border-radius: 2rem;
            margin-top: 2rem;
            font-size: 0.9rem;
        }

        /* Features */
        .features {
            padding: 6rem 2rem;
            background: #f7fafc;
        }

        .section-title {
            text-align: center;
            margin-bottom: 4rem;
        }

        .section-title h2 {
            font-size: 2.5rem;
            margin-bottom: 1rem;
        }

        .section-title p {
            color: #718096;
            font-size: 1.1rem;
        }

        .features-grid {
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(300px, 1fr));
            gap: 2rem;
            max-width: 1200px;
            margin: 0 auto;
        }

        .feature-card {
            background: white;
            padding: 2rem;
            border-radius: 1rem;
            box-shadow: 0 4px 6px rgba(0,0,0,0.05);
            transition: transform 0.2s, box-shadow 0.2s;
        }

        .feature-card:hover {
            transform: translateY(-5px);
            box-shadow: 0 10px 20px rgba(0,0,0,0.1);
        }

        .feature-icon {
            font-size: 3rem;
            margin-bottom: 1rem;
        }

        .feature-card h3 {
            font-size: 1.25rem;
            margin-bottom: 0.75rem;
        }

        .feature-card p {
            color: #718096;
        }

        /* How it works */
        .how-it-works {
            padding: 6rem 2rem;
            max-width: 1000px;
            margin: 0 auto;
        }

        .steps {
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
            gap: 2rem;
            margin-top: 3rem;
        }

        .step {
            text-align: center;
        }

        .step-number {
            width: 60px;
            height: 60px;
            background: var(--gradient);
            color: white;
            border-radius: 50%;
            display: flex;
            align-items: center;
            justify-content: center;
            font-size: 1.5rem;
            font-weight: 700;
            margin: 0 auto 1rem;
        }

        .step h4 {
            margin-bottom: 0.5rem;
        }

        .step p {
            color: #718096;
            font-size: 0.9rem;
        }

        /* CTA */
        .cta {
            padding: 6rem 2rem;
            background: var(--gradient);
            color: white;
            text-align: center;
        }

        .cta h2 {
            font-size: 2.5rem;
            margin-bottom: 1rem;
        }

        .cta p {
            font-size: 1.1rem;
            opacity: 0.9;
            margin-bottom: 2rem;
        }

        /* Footer */
        .footer {
            background: #1a202c;
            color: #a0aec0;
            padding: 3rem 2rem;
        }

        .footer-inner {
            max-width: 1200px;
            margin: 0 auto;
            display: flex;
            justify-content: space-between;
            flex-wrap: wrap;
            gap: 2rem;
        }

        .footer-links a {
            color: #a0aec0;
            text-decoration: none;
            margin-right: 1.5rem;
        }

        .footer-links a:hover {
            color: white;
        }

        /* Responsive */
        @media (max-width: 768px) {
            .hero h1 { font-size: 2rem; }
            .nav-links { display: none; }
            .features-grid { grid-template-columns: 1fr; }
        }
    </style>
</head>
<body>
    <!-- Header -->
    <header class="header">
        <div class="header-inner">
            <a href="/" class="logo">Aurora Livecam</a>
            <nav class="nav-links">
                <a href="#features">Features</a>
                <a href="/landing/pricing.php">Preise</a>
                <a href="/dashboard/login.php">Login</a>
                <a href="/onboarding/register.php" class="btn btn-primary btn-sm">Kostenlos starten</a>
            </nav>
        </div>
    </header>

    <!-- Hero -->
    <section class="hero">
        <h1>Ihre Webcam als Service - in 5 Minuten online</h1>
        <p>Erstellen Sie Ihre eigene Live-Webcam-Website mit Wetter-Widget, Timelapse, Analytics und mehr. Keine Programmierkenntnisse erforderlich.</p>
        <div class="hero-buttons">
            <a href="/onboarding/register.php" class="btn-hero btn-hero-primary">
                Jetzt starten
            </a>
            <a href="#features" class="btn-hero btn-hero-secondary">
                Features ansehen
            </a>
        </div>
        <div class="trial-badge">
            <?php echo $trialDays; ?> Tage kostenlos testen - Keine Kreditkarte erforderlich
        </div>
    </section>

    <!-- Features -->
    <section class="features" id="features">
        <div class="section-title">
            <h2>Alles was Sie brauchen</h2>
            <p>Professionelle Features für Ihre Live-Webcam</p>
        </div>
        <div class="features-grid">
            <div class="feature-card">
                <div class="feature-icon">📹</div>
                <h3>Live-Streaming</h3>
                <p>HLS, RTMP oder WebRTC - verbinden Sie jeden Stream in Sekunden. Automatische Qualitätsanpassung inklusive.</p>
            </div>
            <div class="feature-card">
                <div class="feature-icon">🌤️</div>
                <h3>Wetter-Widget</h3>
                <p>Zeigen Sie Temperatur, Wind, Luftdruck und mehr an. Kostenlose Open-Meteo Integration ohne API-Key.</p>
            </div>
            <div class="feature-card">
                <div class="feature-icon">⏱️</div>
                <h3>Timelapse</h3>
                <p>Automatische Zeitraffer-Erstellung. Scrubben Sie durch den ganzen Tag mit variabler Geschwindigkeit.</p>
            </div>
            <div class="feature-card">
                <div class="feature-icon">🔍</div>
                <h3>Zoom & Pan</h3>
                <p>Lassen Sie Besucher in Ihren Stream hineinzoomen. Unterstützt Touch-Gesten und Maus-Steuerung.</p>
            </div>
            <div class="feature-card">
                <div class="feature-icon">📊</div>
                <h3>Analytics</h3>
                <p>Sehen Sie wer Ihre Webcam besucht. Echtzeit-Zuschauerzähler und detaillierte Statistiken.</p>
            </div>
            <div class="feature-card">
                <div class="feature-icon">🎨</div>
                <h3>Custom Branding</h3>
                <p>Ihr Logo, Ihre Farben, Ihre Domain. Machen Sie die Webcam zu Ihrer eigenen.</p>
            </div>
        </div>
    </section>

    <!-- How it works -->
    <section class="how-it-works">
        <div class="section-title">
            <h2>So einfach geht's</h2>
            <p>In 3 Schritten zur eigenen Livecam</p>
        </div>
        <div class="steps">
            <div class="step">
                <div class="step-number">1</div>
                <h4>Registrieren</h4>
                <p>Erstellen Sie in 30 Sekunden Ihr kostenloses Konto.</p>
            </div>
            <div class="step">
                <div class="step-number">2</div>
                <h4>Stream verbinden</h4>
                <p>Fügen Sie Ihre Stream-URL ein. Wir unterstützen alle gängigen Formate.</p>
            </div>
            <div class="step">
                <div class="step-number">3</div>
                <h4>Anpassen & Teilen</h4>
                <p>Personalisieren Sie Ihre Seite und teilen Sie den Link.</p>
            </div>
        </div>
    </section>

    <!-- CTA -->
    <section class="cta">
        <h2>Bereit loszulegen?</h2>
        <p><?php echo $trialDays; ?> Tage kostenlos testen - keine Kreditkarte erforderlich</p>
        <a href="/onboarding/register.php" class="btn-hero btn-hero-primary">
            Jetzt kostenlos starten
        </a>
    </section>

    <!-- Footer -->
    <footer class="footer">
        <div class="footer-inner">
            <div>
                © <?php echo date('Y'); ?> Aurora Livecam. Alle Rechte vorbehalten.
            </div>
            <div class="footer-links">
                <a href="/terms">AGB</a>
                <a href="/privacy">Datenschutz</a>
                <a href="/imprint">Impressum</a>
                <a href="mailto:support@aurora-livecam.com">Kontakt</a>
            </div>
        </div>
    </footer>
</body>
</html>
