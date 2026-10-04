import { Locale, getDictionary } from '@/lib/i18n';
import Card from '@/components/ui/Card';

export default async function PrivacyPage({ params }: { params: Promise<{ locale: string }> }) {
  const { locale } = await params;
  const dictionary = getDictionary(locale as Locale);

  return (
    <div className="py-20">
      <div className="max-w-3xl mx-auto px-4 sm:px-6 lg:px-8">
        {/* Header */}
        <div className="text-center mb-12">
          <h1 className="section-title">{dictionary.legal.privacy.title}</h1>
        </div>

        <Card>
          <div className="space-y-8">
            {/* Intro */}
            <p className="text-gray-300 text-lg">{dictionary.legal.privacy.intro}</p>

            {/* Responsible Party */}
            <section>
              <h2 className="text-xl font-semibold text-white mb-4">
                {dictionary.legal.privacy.sections.responsible.title}
              </h2>
              <p className="text-gray-400 mb-4">{dictionary.legal.privacy.sections.responsible.text}</p>
              <div className="text-gray-300 bg-primary rounded-lg p-4">
                <p>Ingo Kohler</p>
                <p>AISCOM</p>
                <p>8052 Zürich</p>
                <p>
                  <a href="mailto:ingo.kohler.zh@gmail.com" className="text-accent hover:text-accent-light transition-colors">
                    ingo.kohler.zh@gmail.com
                  </a>
                </p>
              </div>
            </section>

            {/* Data Collection */}
            <section>
              <h2 className="text-xl font-semibold text-white mb-4">
                {dictionary.legal.privacy.sections.collection.title}
              </h2>
              <p className="text-gray-400">{dictionary.legal.privacy.sections.collection.text}</p>
            </section>

            {/* Contact Form */}
            <section>
              <h2 className="text-xl font-semibold text-white mb-4">
                {dictionary.legal.privacy.sections.contact_form.title}
              </h2>
              <p className="text-gray-400">{dictionary.legal.privacy.sections.contact_form.text}</p>
            </section>

            {/* 3D Upload */}
            <section>
              <h2 className="text-xl font-semibold text-white mb-4">
                {dictionary.legal.privacy.sections.upload.title}
              </h2>
              <p className="text-gray-400">{dictionary.legal.privacy.sections.upload.text}</p>
            </section>

            {/* Your Rights */}
            <section>
              <h2 className="text-xl font-semibold text-white mb-4">
                {dictionary.legal.privacy.sections.rights.title}
              </h2>
              <p className="text-gray-400">{dictionary.legal.privacy.sections.rights.text}</p>
            </section>

            {/* Cookies */}
            <section>
              <h2 className="text-xl font-semibold text-white mb-4">Cookies</h2>
              <p className="text-gray-400">
                {locale === 'de'
                  ? 'Diese Website verwendet nur technisch notwendige Cookies (z.B. für die Spracheinstellung). Es werden keine Tracking-Cookies oder Cookies von Drittanbietern verwendet.'
                  : locale === 'it'
                  ? 'Questo sito web utilizza solo cookie tecnicamente necessari (ad esempio per l\'impostazione della lingua). Non vengono utilizzati cookie di tracciamento o cookie di terze parti.'
                  : 'This website only uses technically necessary cookies (e.g. for language settings). No tracking cookies or third-party cookies are used.'}
              </p>
            </section>

            {/* SSL */}
            <section>
              <h2 className="text-xl font-semibold text-white mb-4">
                {locale === 'de' ? 'SSL-Verschlüsselung' : locale === 'it' ? 'Crittografia SSL' : 'SSL Encryption'}
              </h2>
              <p className="text-gray-400">
                {locale === 'de'
                  ? 'Diese Seite nutzt aus Sicherheitsgründen eine SSL-Verschlüsselung. Eine verschlüsselte Verbindung erkennen Sie daran, dass die Adresszeile des Browsers von "http://" auf "https://" wechselt und an dem Schloss-Symbol in Ihrer Browserzeile.'
                  : locale === 'it'
                  ? 'Per motivi di sicurezza, questo sito utilizza la crittografia SSL. È possibile riconoscere una connessione crittografata quando la barra degli indirizzi del browser passa da "http://" a "https://" e dal simbolo del lucchetto nella barra del browser.'
                  : 'For security reasons, this site uses SSL encryption. You can recognize an encrypted connection when the browser address bar changes from "http://" to "https://" and by the lock symbol in your browser bar.'}
              </p>
            </section>

            {/* Changes */}
            <section>
              <h2 className="text-xl font-semibold text-white mb-4">
                {locale === 'de' ? 'Änderungen' : locale === 'it' ? 'Modifiche' : 'Changes'}
              </h2>
              <p className="text-gray-400">
                {locale === 'de'
                  ? 'Ich behalte mir vor, diese Datenschutzerklärung anzupassen, um sie an geänderte Rechtslagen oder bei Änderungen des Dienstes sowie der Datenverarbeitung anzupassen. Die aktuelle Version finden Sie immer auf dieser Seite.'
                  : locale === 'it'
                  ? 'Mi riservo il diritto di adattare questa informativa sulla privacy per adeguarla a situazioni legali modificate o a modifiche del servizio e dell\'elaborazione dei dati. La versione attuale è sempre disponibile su questa pagina.'
                  : 'I reserve the right to adapt this privacy policy to accommodate changes in legal requirements or changes to the service and data processing. The current version can always be found on this page.'}
              </p>
            </section>

            {/* Last Updated */}
            <div className="pt-6 border-t border-primary-light/30">
              <p className="text-gray-500 text-sm">
                {locale === 'de' ? 'Stand: ' : locale === 'it' ? 'Aggiornato: ' : 'Last updated: '}
                {new Date().toLocaleDateString(locale === 'de' ? 'de-CH' : locale === 'it' ? 'it-CH' : 'en-CH', {
                  year: 'numeric',
                  month: 'long',
                })}
              </p>
            </div>
          </div>
        </Card>
      </div>
    </div>
  );
}
