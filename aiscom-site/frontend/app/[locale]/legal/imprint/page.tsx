import { Locale, getDictionary } from '@/lib/i18n';
import Card from '@/components/ui/Card';

export default async function ImprintPage({ params }: { params: Promise<{ locale: string }> }) {
  const { locale } = await params;
  const dictionary = getDictionary(locale as Locale);

  return (
    <div className="py-20">
      <div className="max-w-3xl mx-auto px-4 sm:px-6 lg:px-8">
        {/* Header */}
        <div className="text-center mb-12">
          <h1 className="section-title">{dictionary.legal.imprint.title}</h1>
        </div>

        <Card>
          <div className="space-y-8">
            {/* Owner */}
            <section>
              <h2 className="text-xl font-semibold text-white mb-4">{dictionary.legal.imprint.owner}</h2>
              <p className="text-gray-300">Ingo Kohler</p>
              <p className="text-gray-300">AISCOM</p>
            </section>

            {/* Address */}
            <section>
              <h2 className="text-xl font-semibold text-white mb-4">{dictionary.legal.imprint.address}</h2>
              <p className="text-gray-300">8052 Zürich</p>
              <p className="text-gray-300">
                {locale === 'de' ? 'Schweiz' : locale === 'it' ? 'Svizzera' : 'Switzerland'}
              </p>
            </section>

            {/* Contact */}
            <section>
              <h2 className="text-xl font-semibold text-white mb-4">{dictionary.legal.imprint.contact}</h2>
              <p className="text-gray-300">
                E-Mail:{' '}
                <a href="mailto:ingo.kohler.zh@gmail.com" className="text-accent hover:text-accent-light transition-colors">
                  ingo.kohler.zh@gmail.com
                </a>
              </p>
            </section>

            {/* Legal Notice */}
            <section>
              <h2 className="text-xl font-semibold text-white mb-4">
                {locale === 'de' ? 'Rechtliche Hinweise' : locale === 'it' ? 'Note legali' : 'Legal Notice'}
              </h2>
              <div className="text-gray-400 text-sm space-y-4">
                <p>
                  {locale === 'de'
                    ? 'Keine UID-Nummer vorhanden (nebenberufliche Tätigkeit unter Kleinunternehmerregelung).'
                    : locale === 'it'
                    ? 'Nessun numero UID (attività secondaria sotto il regime delle piccole imprese).'
                    : 'No VAT number (side business under small business regulation).'}
                </p>
                <p>
                  {locale === 'de'
                    ? 'Alle Inhalte dieser Website wurden sorgfältig erstellt. Für die Richtigkeit, Vollständigkeit und Aktualität der Inhalte kann jedoch keine Gewähr übernommen werden.'
                    : locale === 'it'
                    ? 'Tutti i contenuti di questo sito web sono stati creati con cura. Tuttavia, non si può garantire l\'accuratezza, completezza e attualità dei contenuti.'
                    : 'All content on this website has been carefully created. However, no guarantee can be given for the accuracy, completeness and timeliness of the content.'}
                </p>
              </div>
            </section>

            {/* Copyright */}
            <section>
              <h2 className="text-xl font-semibold text-white mb-4">
                {locale === 'de' ? 'Urheberrecht' : locale === 'it' ? 'Copyright' : 'Copyright'}
              </h2>
              <p className="text-gray-400 text-sm">
                {locale === 'de'
                  ? 'Die durch den Seitenbetreiber erstellten Inhalte und Werke auf diesen Seiten unterliegen dem schweizerischen Urheberrecht. Die Vervielfältigung, Bearbeitung, Verbreitung und jede Art der Verwertung ausserhalb der Grenzen des Urheberrechtes bedürfen der schriftlichen Zustimmung des jeweiligen Autors bzw. Erstellers.'
                  : locale === 'it'
                  ? 'I contenuti e le opere create dall\'operatore del sito su queste pagine sono soggetti al diritto d\'autore svizzero. La riproduzione, elaborazione, distribuzione e qualsiasi tipo di utilizzo al di fuori dei limiti del diritto d\'autore richiedono il consenso scritto del rispettivo autore o creatore.'
                  : 'The content and works created by the site operator on these pages are subject to Swiss copyright law. The reproduction, editing, distribution and any kind of exploitation outside the limits of copyright require the written consent of the respective author or creator.'}
              </p>
            </section>
          </div>
        </Card>
      </div>
    </div>
  );
}
