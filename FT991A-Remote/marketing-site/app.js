const translations = {
  de: {
    nav_preview: "Vorschau",
    nav_sales: "Vertrieb",
    nav_contact: "Kontakt",
    nav_appstore: "App Store",
    nav_demo: "Download",
    brand_subline: "FT-991A Remote für macOS",
    mainnav_operations: "Betrieb",
    mainnav_workflow: "Ablauf",
    mainnav_sales: "Vertrieb",
    mainnav_contact: "Kontakt",
    hero_kicker: "CAT-Steuerung ohne Umwege.",
    hero_intro: "Der FT-991A. Direkt am Mac.",
    hero_title: "Der FT-991A fühlt sich auf dem Mac endlich wie ein echtes Funkgerät an.",
    hero_text: "MacYaesu bringt CAT-Steuerung, QSO-Log, Speicherkanäle, Repeater-Setup und digitale Betriebsarten in eine native macOS-App. Entwickelt als belastbares Arbeitswerkzeug für Operatoren, die im Shack klare Zustände, direkte Kontrolle und saubere Abläufe erwarten.",
    hero_cta_primary: "Live-Demo ansehen",
    hero_cta_secondary: "Demo herunterladen",
    hero_point_1: "CP2105, USB-CAT und sauberer Verbindungsstatus statt bloss Port offen",
    hero_point_2: "100 Speicherkanäle mit Tone, Offset und Leistung direkt in der App",
    hero_point_3: "QSO-Log, Debug, CAT-Trace und Audio ohne Hilfstool-Kette",
    hero_meta: "Gebaut für Operatoren, die ohne Wine, VM und Bastellösungen funken wollen. Im Einsatz für SSB, DIGI, Repeater und strukturierte Shack-Abläufe.",
    stage_label: "Ansicht aus der App",
    stage_controls_label: "Ansicht",
    proof_1_title: "2 Ansichten",
    proof_1_text: "Modern UI plus Frontpanel-Charakter",
    proof_2_title: "100 Memories",
    proof_2_text: "Mit Tone-, Offset- und Leistungsdaten",
    proof_3_title: "CAT Diagnose",
    proof_3_text: "Port, RTS, Echo und Polling im Blick",
    proof_4_title: "macOS nativ",
    proof_4_text: "Keine VM, kein Wine, kein Gefrickel",
    features_kicker: "Für den laufenden Betrieb",
    features_title: "Alles, was am FT-991A im Alltag zählt, ist direkt erreichbar.",
    feature_1_title: "Direkter Gerätekontakt",
    feature_1_text: "USB-CAT mit CP210x-Port-Erkennung, Auto-Reconnect, Handshake-Logik und verwertbarem Status statt bloss “Port offen”.",
    feature_2_title: "Ein konsistenter Betriebsablauf",
    feature_2_text: "Frequenz, Mode, Log, Debug, Repeater-Setup und Speicherplätze laufen in einer Oberfläche, ohne zwischen Hilfsprogrammen zu wechseln.",
    feature_3_title: "Digitale Betriebsarten vorbereitet",
    feature_3_text: "Audio-Routing, BlackHole-Hinweise und die passenden macOS-Geräte sind für FT8, fldigi und ähnliche Betriebsarten von Anfang an eingeplant.",
    feature_4_title: "Diagnose auf Operator-Niveau",
    feature_4_text: "Support-Bundle, CAT-Trace und klare Setup-Hinweise helfen genau dort, wo FT-991A-Betrieb auf dem Mac verifiziert werden muss.",
    experience_kicker: "Arbeitsgefühl",
    experience_title: "Zwischen Frontpanel-Gefühl und moderner Mac-Oberfläche muss man sich nicht entscheiden.",
    experience_1_label: "Ruhig, aber fachlich klar",
    experience_1_title: "Interaktive Station",
    experience_1_text: "Band, Mode und Signal ändern sich live. Die Oberfläche bleibt dabei bewusst ruhig, grosszügig und hochwertig, aber sie spricht klar die Sprache von Station, CAT, Diagnose und Funkbetrieb.",
    experience_2_label: "Für ernsthaften Einsatz gebaut",
    experience_2_point_1: "Frequenzsteuerung mit Schrittweiten und Tastatur",
    experience_2_point_2: "Repeater-, Tone- und DCS-Setup mit Speicherbezug",
    experience_2_point_3: "Menüleistenbetrieb für längere Sessions",
    experience_2_point_4: "Deutsch und Englisch ohne halbfertige Oberflächen",
    workflow_kicker: "Typischer Ablauf im Shack",
    workflow_title: "Vom Anschliessen bis zum Betrieb bleibt alles in einem sauberen Ablauf.",
    workflow_1_title: "Verbinden",
    workflow_1_text: "Passenden CP2105-Port wählen, CAT verbinden und den Status sofort technisch nachvollziehen.",
    workflow_2_title: "Einstellen",
    workflow_2_text: "Band, Mode, Shift, Tone, Power und Memories in derselben Oberfläche reproduzierbar anpassen.",
    workflow_3_title: "Betreiben",
    workflow_3_text: "QSO führen, digitale Betriebsarten anbinden und bei Bedarf Debug oder Trace direkt zur Kontrolle öffnen.",
    sales_kicker: "Vertrieb",
    sales_title: "Für diese App ist Direktvertrieb realistischer als der Mac App Store.",
    sales_1_label: "Empfohlener Weg",
    sales_1_title: "Developer ID + signiertes DMG",
    sales_1_point_1: "Direkter Zugriff auf serielle Ports und USB-CAT bleibt erhalten",
    sales_1_point_2: "Keine App-Sandbox, die `/dev/cu.*` blockiert",
    sales_1_point_3: "Volle Kontrolle über Preis, Updates und Support für die Nische",
    sales_1_point_4: "Vertrieb über eigene Website, QRZ.com, eHam und Mailinglisten",
    sales_2_label: "Technisch möglich, aber heikel",
    sales_2_title: "Mac App Store",
    sales_2_point_1: "App Sandbox kollidiert mit direktem IOKit-/Serial-Port-Zugriff",
    sales_2_point_2: "Öffentliche Entitlements für serielle Ports gibt es praktisch nicht",
    sales_2_point_3: "BlackHole-/treibernahe Audio-Workflows sind für Store-Review unhandlich",
    sales_2_point_4: "Eher sinnvoll nur für eine Companion-App ohne Hardware-Steuerung",
    purchase_kicker: "Kaufen",
    purchase_title: "Einfacher Direktverkauf statt komplizierter Store-Logik.",
    purchase_text: "Die Vollversion kostet einmalig 49 CHF. Die Zahlung wird sicher bei PayPal abgeschlossen. Nach bestätigtem Zahlungseingang wird der Lizenzschlüssel an die beim Kauf angegebene E-Mail-Adresse geschickt. Regelmässige Updates für diese Version sind kostenlos enthalten.",
    purchase_meta_1: "49 CHF einmalig",
    purchase_meta_2: "PayPal",
    purchase_meta_3: "Updates inklusive",
    purchase_cta_primary: "Vollversion mit PayPal kaufen",
    purchase_cta_secondary: "Fragen zur Lizenz",
    purchase_email_label: "E-Mail für den Lizenzschlüssel",
    download_kicker: "Download",
    download_title: "Demo direkt laden und 15 Minuten im eigenen Shack testen.",
    download_text: "Die Demo läuft 15 Minuten ohne Aktivierung. Danach kann die App mit einem Lizenzschlüssel freigeschaltet werden. Der Download liegt direkt neben dieser Website als DMG.",
    download_meta_1: "macOS",
    download_meta_2: "Direktdownload",
    download_meta_3: "15-Minuten-Demo",
    download_meta_4: "Spätere Updates kostenlos",
    download_release_version_label: "Version",
    download_release_build_label: "Build",
    download_release_updated_label: "Aktualisiert",
    download_release_size_label: "Dateigrösse",
    download_cta_primary: "Demo herunterladen",
    download_cta_secondary: "Lizenzmodell ansehen",
    footer_kicker: "Nächster Schritt",
    footer_title: "Demo herunterladen, im Shack testen und bei Bedarf direkt freischalten.",
    footer_contact: "Kontakt für Lizenzfragen, Vertrieb und Support: metacube@gmail.com",
    footer_cta_1: "Demo herunterladen",
    footer_cta_2: "Lizenz anfragen",
    faq_kicker: "FAQ",
    faq_title: "Die wichtigsten Punkte vor dem Download.",
    faq_1_title: "Welche Geräte werden unterstützt?",
    faq_1_text: "MacYaesu ist aktuell auf Yaesu FT-991A ausgerichtet und legt den Fokus auf belastbare CAT-Steuerung am Mac.",
    faq_2_title: "Wie lange kann ich die Demo testen?",
    faq_2_text: "Die Demo läuft 15 Minuten ohne Aktivierung. Danach kann sie mit einem Lizenzschlüssel freigeschaltet werden.",
    faq_3_title: "Läuft die App über den Mac App Store?",
    faq_3_text: "Nein. Der empfohlene Weg ist der Direktvertrieb als signiertes DMG, damit CAT- und Audio-Funktionen nicht durch Sandbox-Regeln beschnitten werden."
  },
  en: {
    nav_preview: "Preview",
    nav_sales: "Distribution",
    nav_contact: "Contact",
    nav_appstore: "App Store",
    nav_demo: "Download",
    brand_subline: "FT-991A Remote for macOS",
    mainnav_operations: "Operation",
    mainnav_workflow: "Workflow",
    mainnav_sales: "Distribution",
    mainnav_contact: "Contact",
    hero_kicker: "CAT control without detours.",
    hero_intro: "The FT-991A. Directly on the Mac.",
    hero_title: "On the Mac, the FT-991A finally feels like a real radio again.",
    hero_text: "MacYaesu brings CAT control, QSO logging, memory channels, repeater setup and digital operating modes into a native macOS app. Built as a reliable working tool for operators who expect clear state, direct control and clean workflows in the shack.",
    hero_cta_primary: "View Live Demo",
    hero_cta_secondary: "Download Demo",
    hero_point_1: "CP2105, USB CAT and clear connection status instead of merely port open",
    hero_point_2: "100 memory channels with tone, offset and power directly in the app",
    hero_point_3: "QSO log, debug, CAT trace and audio without a chain of helper tools",
    hero_meta: "Built for operators who want to run radio without Wine, VMs or improvised workarounds. Used for SSB, digital modes, repeaters and structured shack workflows.",
    stage_label: "View from the app",
    stage_controls_label: "View",
    proof_1_title: "2 Views",
    proof_1_text: "Modern UI plus front-panel character",
    proof_2_title: "100 Memories",
    proof_2_text: "With tone, offset and power data",
    proof_3_title: "CAT Diagnostics",
    proof_3_text: "Port, RTS, echo and polling under control",
    proof_4_title: "Native macOS",
    proof_4_text: "No VM, no Wine, no workarounds",
    features_kicker: "For day-to-day operation",
    features_title: "Everything that matters on the FT-991A is directly accessible.",
    feature_1_title: "Direct hardware contact",
    feature_1_text: "USB CAT with CP210x port detection, auto reconnect, handshake logic and useful status instead of merely “port open”.",
    feature_2_title: "One consistent operating workflow",
    feature_2_text: "Frequency, mode, log, debug, repeater setup and memories live in one interface without jumping between helper tools.",
    feature_3_title: "Prepared for digital modes",
    feature_3_text: "Audio routing, BlackHole guidance and the right macOS devices are designed in from the start for FT8, fldigi and similar modes.",
    feature_4_title: "Operator-level diagnostics",
    feature_4_text: "Support bundles, CAT trace and clear setup guidance help exactly where FT-991A operation on the Mac needs to be verified.",
    experience_kicker: "Working feel",
    experience_title: "You do not have to choose between front-panel feel and a modern Mac interface.",
    experience_1_label: "Calm, but technically clear",
    experience_1_title: "Interactive station",
    experience_1_text: "Band, mode and signal update live. The interface remains intentionally calm, spacious and refined while clearly speaking the language of station control, CAT, diagnostics and radio operation.",
    experience_2_label: "Built for serious use",
    experience_2_point_1: "Frequency control with steps and keyboard input",
    experience_2_point_2: "Repeater, tone and DCS setup tied to memories",
    experience_2_point_3: "Menu bar operation for longer sessions",
    experience_2_point_4: "German and English without half-finished UI",
    workflow_kicker: "Typical shack workflow",
    workflow_title: "From connecting to operating, everything stays in one clean flow.",
    workflow_1_title: "Connect",
    workflow_1_text: "Choose the correct CP2105 port, connect CAT and verify status immediately.",
    workflow_2_title: "Configure",
    workflow_2_text: "Adjust band, mode, shift, tone, power and memories reproducibly in the same interface.",
    workflow_3_title: "Operate",
    workflow_3_text: "Run QSOs, integrate digital modes and open debug or trace directly when needed.",
    sales_kicker: "Distribution",
    sales_title: "For this app, direct distribution is more realistic than the Mac App Store.",
    sales_1_label: "Recommended path",
    sales_1_title: "Developer ID + signed DMG",
    sales_1_point_1: "Direct access to serial ports and USB CAT remains available",
    sales_1_point_2: "No app sandbox blocking `/dev/cu.*`",
    sales_1_point_3: "Full control over pricing, updates and niche support",
    sales_1_point_4: "Distribution via your own website, QRZ.com, eHam and mailing lists",
    sales_2_label: "Technically possible, but awkward",
    sales_2_title: "Mac App Store",
    sales_2_point_1: "App sandbox conflicts with direct IOKit and serial port access",
    sales_2_point_2: "Public entitlements for serial ports are effectively unavailable",
    sales_2_point_3: "BlackHole and driver-adjacent audio workflows are awkward in review",
    sales_2_point_4: "More realistic only as a companion app without hardware control",
    purchase_kicker: "Buy",
    purchase_title: "Simple direct sales instead of complicated store logic.",
    purchase_text: "The full version costs 49 CHF as a one-time purchase. Payment is completed securely on PayPal. Once the payment is confirmed, the license key is sent to the email address entered during checkout. Regular updates for this version are included at no extra cost.",
    purchase_meta_1: "49 CHF one-time",
    purchase_meta_2: "PayPal",
    purchase_meta_3: "Updates included",
    purchase_cta_primary: "Buy Full Version with PayPal",
    purchase_cta_secondary: "License Questions",
    purchase_email_label: "Email for the license key",
    download_kicker: "Download",
    download_title: "Download the demo directly and test it in your own shack for 15 minutes.",
    download_text: "The demo runs for 15 minutes without activation. After that, the app can be unlocked with a license key. The download is placed directly next to this website as a DMG.",
    download_meta_1: "macOS",
    download_meta_2: "Direct Download",
    download_meta_3: "15-Minute Demo",
    download_meta_4: "Future updates free",
    download_release_version_label: "Version",
    download_release_build_label: "Build",
    download_release_updated_label: "Updated",
    download_release_size_label: "File Size",
    download_cta_primary: "Download Demo",
    download_cta_secondary: "View Licensing",
    footer_kicker: "Next step",
    footer_title: "Download the demo, test it in the shack and unlock it directly when needed.",
    footer_contact: "Contact for licensing, sales and support: metacube@gmail.com",
    footer_cta_1: "Download Demo",
    footer_cta_2: "Request License",
    faq_kicker: "FAQ",
    faq_title: "The key points before you download.",
    faq_1_title: "Which radios are supported?",
    faq_1_text: "MacYaesu is currently focused on the Yaesu FT-991A and prioritizes reliable CAT control on the Mac.",
    faq_2_title: "How long can I test the demo?",
    faq_2_text: "The demo runs for 15 minutes without activation. After that it can be unlocked with a license key.",
    faq_3_title: "Is the app distributed through the Mac App Store?",
    faq_3_text: "No. The recommended path is direct distribution as a signed DMG so CAT and audio workflows are not constrained by sandbox rules."
  }
};

const state = {
  currentLanguage: "de"
};

const elements = {
  rigStatus: document.getElementById("rig-status"),
  modeCaption: document.getElementById("mode-caption"),
  langButtons: Array.from(document.querySelectorAll(".lang-toggle")),
  bandButtons: Array.from(document.querySelectorAll("#band-controls .chip")),
  modeButtons: Array.from(document.querySelectorAll("#mode-controls .mode-button")),
  translatable: Array.from(document.querySelectorAll("[data-i18n]")),
  images: Array.from(document.querySelectorAll("[data-alt-de][data-alt-en]")),
  configLinks: Array.from(document.querySelectorAll("[data-config-link]")),
  paypalForm: document.getElementById("paypal-buy-form"),
  paymentStatus: document.getElementById("payment-status"),
  releaseValues: Array.from(document.querySelectorAll("[data-release]"))
};

const config = window.MACYAESU_CONFIG || {
  sellerName: "MacYaesu",
  sellerCallsign: "",
  siteUrl: "https://www.aiscom.ch/macyaesu/",
  companyName: "MacYaesu",
  priceChf: 49,
  currency: "CHF",
  paypalMerchantEmail: "metacube@gmail.com",
  paypalEndpoint: "https://www.paypal.com/cgi-bin/webscr",
  productName: "MacYaesu Vollversion",
  productNumber: "MACYAESU-1.1",
  supportEmail: "metacube@gmail.com",
  licenseEmail: "metacube@gmail.com",
  pricingEmail: "metacube@gmail.com",
  downloadFile: "./MacYaesu.dmg",
  downloadFileName: "MacYaesu.dmg",
  demoMinutes: 15,
  releaseVersion: "1.1",
  releaseBuild: "2",
  releaseUpdated: "15.08.2026",
  releaseSize: "—",
  updatesIncludedTextDe: "Regelmässige Updates sind kostenlos enthalten.",
  updatesIncludedTextEn: "Regular updates are included at no extra cost."
};

function setActiveButton(buttons, activeButton) {
  buttons.forEach((button) => {
    button.classList.toggle("active", button === activeButton);
  });
}

function applyLanguage(language) {
  const activeLanguage = translations[language] ? language : "de";
  state.currentLanguage = activeLanguage;
  document.documentElement.lang = activeLanguage;

  elements.translatable.forEach((node) => {
    const key = node.dataset.i18n;
    const value = translations[activeLanguage][key];
    if (value) {
      node.textContent = value;
    }
  });

  elements.images.forEach((image) => {
    image.alt = activeLanguage === "de" ? image.dataset.altDe : image.dataset.altEn;
  });

  elements.langButtons.forEach((button) => {
    const isActive = button.dataset.lang === activeLanguage;
    button.classList.toggle("is-active", isActive);
    button.setAttribute("aria-pressed", isActive ? "true" : "false");
  });

  elements.bandButtons.forEach((button) => {
    button.textContent = activeLanguage === "de" ? button.dataset.labelDe : button.dataset.labelEn;
  });

  elements.modeButtons.forEach((button) => {
    button.textContent = activeLanguage === "de" ? button.dataset.labelDe : button.dataset.labelEn;
  });
}

function applyConfig() {
  const localizedUpdates = state.currentLanguage === "de" ? config.updatesIncludedTextDe : config.updatesIncludedTextEn;
  const purchaseText = state.currentLanguage === "de"
    ? `Die Vollversion kostet einmalig ${config.priceChf} ${config.currency}. Die Zahlung wird sicher bei PayPal abgeschlossen. Nach bestätigtem Zahlungseingang wird der Lizenzschlüssel an die beim Kauf angegebene E-Mail-Adresse geschickt. ${localizedUpdates}`
    : `The full version costs ${config.priceChf} ${config.currency} as a one-time purchase. Payment is completed securely on PayPal. Once the payment is confirmed, the license key is sent to the email address entered during checkout. ${localizedUpdates}`;
  const downloadText = state.currentLanguage === "de"
    ? `Die Demo läuft ${config.demoMinutes} Minuten ohne Aktivierung. Danach kann die App mit einem Lizenzschlüssel freigeschaltet werden. Der Download liegt direkt neben dieser Website als ${config.downloadFileName}.`
    : `The demo runs for ${config.demoMinutes} minutes without activation. After that, the app can be unlocked with a license key. The download is placed directly next to this website as ${config.downloadFileName}.`;
  const downloadTitle = state.currentLanguage === "de"
    ? `Demo direkt laden und ${config.demoMinutes} Minuten im eigenen Shack testen.`
    : `Download the demo directly and test it in your own shack for ${config.demoMinutes} minutes.`;
  const purchasePrice = state.currentLanguage === "de"
    ? `${config.priceChf} ${config.currency} einmalig`
    : `${config.priceChf} ${config.currency} one-time`;
  const updatesLabel = state.currentLanguage === "de" ? "Updates inklusive" : "Updates included";
  const freeUpdatesLabel = state.currentLanguage === "de" ? "Spätere Updates kostenlos" : "Future updates free";

  const setText = (key, value) => {
    const node = document.querySelector(`[data-i18n="${key}"]`);
    if (node) node.textContent = value;
  };

  setText("purchase_text", purchaseText);
  setText("download_text", downloadText);
  setText("download_title", downloadTitle);
  setText("purchase_meta_1", purchasePrice);
  setText("purchase_meta_3", updatesLabel);
  setText("download_meta_3", state.currentLanguage === "de" ? `${config.demoMinutes}-Minuten-Demo` : `${config.demoMinutes}-Minute Demo`);
  setText("download_meta_4", freeUpdatesLabel);
  setText("footer_contact", state.currentLanguage === "de"
    ? `Kontakt für Lizenzfragen, Vertrieb und Support: ${config.supportEmail}`
    : `Contact for licensing, sales and support: ${config.supportEmail}`);

  if (elements.paypalForm) {
    elements.paypalForm.action = config.paypalEndpoint;
    const setFormValue = (name, value) => {
      const field = elements.paypalForm.elements.namedItem(name);
      if (field) field.value = value;
    };
    setFormValue("business", config.paypalMerchantEmail);
    setFormValue("item_name", config.productName);
    setFormValue("item_number", config.productNumber);
    setFormValue("amount", Number(config.priceChf).toFixed(2));
    setFormValue("currency_code", config.currency);
    setFormValue("on0", state.currentLanguage === "de" ? "Lizenz-E-Mail" : "License email");
    setFormValue("return", `${config.siteUrl}?payment=success#kaufen`);
    setFormValue("cancel_return", `${config.siteUrl}?payment=cancelled#kaufen`);
  }

  const releaseConfig = {
    version: config.releaseVersion,
    build: config.releaseBuild,
    updated: config.releaseUpdated,
    size: config.releaseSize
  };
  elements.releaseValues.forEach((node) => {
    node.textContent = releaseConfig[node.dataset.release] || "—";
  });

  elements.configLinks.forEach((link) => {
    const kind = link.dataset.configLink;
    if (kind === "download") {
      link.href = config.downloadFile;
      link.setAttribute("download", config.downloadFileName);
    } else if (kind === "license_mail") {
      link.href = `mailto:${config.licenseEmail}?subject=${encodeURIComponent(`${config.sellerName} License`)}`;
    } else if (kind === "pricing_mail") {
      link.href = `mailto:${config.pricingEmail}?subject=${encodeURIComponent(`${config.sellerName} Pricing`)}`;
    }
  });

  showPaymentStatus();
}

function showPaymentStatus() {
  if (!elements.paymentStatus) return;

  const payment = new URLSearchParams(window.location.search).get("payment");
  elements.paymentStatus.hidden = payment !== "success" && payment !== "cancelled";
  elements.paymentStatus.classList.toggle("is-success", payment === "success");
  elements.paymentStatus.classList.toggle("is-cancelled", payment === "cancelled");

  if (payment === "success") {
    elements.paymentStatus.textContent = state.currentLanguage === "de"
      ? "Danke für deinen Kauf. Sobald PayPal den Zahlungseingang bestätigt hat, erhältst du den Lizenzschlüssel an die angegebene E-Mail-Adresse."
      : "Thank you for your purchase. Once PayPal confirms the payment, the license key will be sent to the email address you entered.";
  } else if (payment === "cancelled") {
    elements.paymentStatus.textContent = state.currentLanguage === "de"
      ? "Die Zahlung wurde abgebrochen. Es wurde keine Lizenz bestellt."
      : "The payment was cancelled. No license was ordered.";
  }
}

function updateBand(button) {
  state.currentBand = button.textContent.trim();
  elements.rigStatus.textContent = state.currentLanguage === "de" ? button.dataset.statusDe : button.dataset.statusEn;
  elements.modeCaption.textContent = state.currentLanguage === "de" ? button.dataset.captionDe : button.dataset.captionEn;
  setActiveButton(elements.bandButtons, button);
}

function updateMode(button) {
  state.currentMode = button.textContent.trim();
  elements.rigStatus.textContent = state.currentLanguage === "de" ? button.dataset.statusDe : button.dataset.statusEn;
  elements.modeCaption.textContent = state.currentLanguage === "de" ? button.dataset.captionDe : button.dataset.captionEn;
  setActiveButton(elements.modeButtons, button);
}

const initialBandButton = document.querySelector("#band-controls .chip.active");
const initialModeButton = document.querySelector("#mode-controls .mode-button.active");

elements.langButtons.forEach((button) => {
  button.addEventListener("click", () => {
    applyLanguage(button.dataset.lang);
    applyConfig();
    const activeBandButton = document.querySelector("#band-controls .chip.active") || initialBandButton;
    const activeModeButton = document.querySelector("#mode-controls .mode-button.active") || initialModeButton;
    if (activeBandButton) updateBand(activeBandButton);
    if (activeModeButton) updateMode(activeModeButton);
  });
});

elements.bandButtons.forEach((button) => {
  button.addEventListener("click", () => updateBand(button));
});

elements.modeButtons.forEach((button) => {
  button.addEventListener("click", () => updateMode(button));
});

const browserLanguage = (navigator.language || "de").toLowerCase();
applyLanguage(browserLanguage.startsWith("en") ? "en" : "de");
applyConfig();

if (initialBandButton) {
  updateBand(initialBandButton);
}

if (initialModeButton) {
  updateMode(initialModeButton);
}
