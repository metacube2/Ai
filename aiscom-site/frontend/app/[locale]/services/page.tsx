import PageHeader from '@/components/visual/PageHeader';
import { Locale, getDictionary } from '@/lib/i18n';
import Card from '@/components/ui/Card';
import Button from '@/components/ui/Button';
import Link from 'next/link';

export default async function ServicesPage({ params }: { params: Promise<{ locale: string }> }) {
  const { locale } = await params;
  const dictionary = getDictionary(locale as Locale);

  const services = [
    {
      id: '3dprint',
      icon: (
        <svg className="w-16 h-16 text-accent" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} d="M6.72 13.829c-.24.03-.48.062-.72.096m.72-.096a42.415 42.415 0 0110.56 0m-10.56 0L6.34 18m10.94-4.171c.24.03.48.062.72.096m-.72-.096L17.66 18m0 0l.229 2.523a1.125 1.125 0 01-1.12 1.227H7.231c-.662 0-1.18-.568-1.12-1.227L6.34 18m11.318 0h1.091A2.25 2.25 0 0021 15.75V9.456c0-1.081-.768-2.015-1.837-2.175a48.055 48.055 0 00-1.913-.247M6.34 18H5.25A2.25 2.25 0 013 15.75V9.456c0-1.081.768-2.015 1.837-2.175a48.041 48.041 0 011.913-.247m10.5 0a48.536 48.536 0 00-10.5 0m10.5 0V3.375c0-.621-.504-1.125-1.125-1.125h-8.25c-.621 0-1.125.504-1.125 1.125v3.659M18 10.5h.008v.008H18V10.5zm-3 0h.008v.008H15V10.5z" />
        </svg>
      ),
      title: dictionary.services['3dprint'].title,
      description: dictionary.services['3dprint'].description,
      price: dictionary.services['3dprint'].price,
      features: locale === 'de'
        ? ['Bambu Lab X1 Carbon', 'PLA & ABS Materialien', 'Max. 256x256x256mm', 'Schnelle Lieferung', 'Prototypen & Kleinserien']
        : locale === 'it'
        ? ['Bambu Lab X1 Carbon', 'Materiali PLA e ABS', 'Max. 256x256x256mm', 'Consegna rapida', 'Prototipi e piccole serie']
        : ['Bambu Lab X1 Carbon', 'PLA & ABS materials', 'Max. 256x256x256mm', 'Fast delivery', 'Prototypes & small batches'],
      cta: `/${locale}/upload`,
    },
    {
      id: 'csharp',
      icon: (
        <svg className="w-16 h-16 text-accent" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} d="M17.25 6.75L22.5 12l-5.25 5.25m-10.5 0L1.5 12l5.25-5.25m7.5-3l-4.5 16.5" />
        </svg>
      ),
      title: dictionary.services.csharp.title,
      description: dictionary.services.csharp.description,
      price: dictionary.services.csharp.price,
      features: locale === 'de'
        ? ['.NET Framework & .NET Core', 'Desktop-Anwendungen (WPF, WinForms)', 'REST APIs & Web Services', 'Datenbank-Integration', 'Unit Testing & CI/CD']
        : locale === 'it'
        ? ['.NET Framework e .NET Core', 'Applicazioni desktop (WPF, WinForms)', 'API REST e Web Services', 'Integrazione database', 'Unit Testing e CI/CD']
        : ['.NET Framework & .NET Core', 'Desktop applications (WPF, WinForms)', 'REST APIs & Web Services', 'Database integration', 'Unit Testing & CI/CD'],
      cta: `/${locale}/contact`,
    },
    {
      id: 'abap',
      icon: (
        <svg className="w-16 h-16 text-accent" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} d="M5.25 14.25h13.5m-13.5 0a3 3 0 01-3-3m3 3a3 3 0 100 6h13.5a3 3 0 100-6m-16.5-3a3 3 0 013-3h13.5a3 3 0 013 3m-19.5 0a4.5 4.5 0 01.9-2.7L5.737 5.1a3.375 3.375 0 012.7-1.35h7.126c1.062 0 2.062.5 2.7 1.35l2.587 3.45a4.5 4.5 0 01.9 2.7m0 0a3 3 0 01-3 3m0 3h.008v.008h-.008v-.008zm0-6h.008v.008h-.008v-.008zm-3 6h.008v.008h-.008v-.008zm0-6h.008v.008h-.008v-.008z" />
        </svg>
      ),
      title: dictionary.services.abap.title,
      description: dictionary.services.abap.description,
      price: dictionary.services.abap.price,
      features: locale === 'de'
        ? ['Reports & ALV', 'SAP Script & Smart Forms', 'RFC & IDocs', 'Erweiterungen (User Exits, BAdIs)', 'ABAP Objects']
        : locale === 'it'
        ? ['Reports e ALV', 'SAP Script e Smart Forms', 'RFC e IDocs', 'Estensioni (User Exits, BAdIs)', 'ABAP Objects']
        : ['Reports & ALV', 'SAP Script & Smart Forms', 'RFC & IDocs', 'Enhancements (User Exits, BAdIs)', 'ABAP Objects'],
      cta: `/${locale}/contact`,
    },
    {
      id: 'web',
      icon: (
        <svg className="w-16 h-16 text-accent" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} d="M12 21a9.004 9.004 0 008.716-6.747M12 21a9.004 9.004 0 01-8.716-6.747M12 21c2.485 0 4.5-4.03 4.5-9S14.485 3 12 3m0 18c-2.485 0-4.5-4.03-4.5-9S9.515 3 12 3m0 0a8.997 8.997 0 017.843 4.582M12 3a8.997 8.997 0 00-7.843 4.582m15.686 0A11.953 11.953 0 0112 10.5c-2.998 0-5.74-1.1-7.843-2.918m15.686 0A8.959 8.959 0 0121 12c0 .778-.099 1.533-.284 2.253m0 0A17.919 17.919 0 0112 16.5c-3.162 0-6.133-.815-8.716-2.247m0 0A9.015 9.015 0 013 12c0-1.605.42-3.113 1.157-4.418" />
        </svg>
      ),
      title: dictionary.services.web.title,
      description: dictionary.services.web.description,
      price: dictionary.services.web.price,
      features: locale === 'de'
        ? ['React / Next.js', 'Responsive Design', 'SEO-Optimierung', 'CMS-Integration', 'Hosting & Wartung']
        : locale === 'it'
        ? ['React / Next.js', 'Design responsive', 'Ottimizzazione SEO', 'Integrazione CMS', 'Hosting e manutenzione']
        : ['React / Next.js', 'Responsive Design', 'SEO Optimization', 'CMS Integration', 'Hosting & Maintenance'],
      cta: `/${locale}/contact`,
    },
  ];

  return (
    <div className="py-20">
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
        {/* Header */}
        <PageHeader title={dictionary.services.title} subtitle={dictionary.services.subtitle} />

        {/* Services Grid */}
        <div className="space-y-12">
          {services.map((service, index) => (
            <Card key={service.id} className={`overflow-hidden ${index % 2 === 1 ? 'lg:flex-row-reverse' : ''}`}>
              <div className="grid grid-cols-1 lg:grid-cols-3 gap-8">
                {/* Icon & Title */}
                <div className="flex flex-col items-center justify-center text-center p-6 bg-primary rounded-xl">
                  {service.icon}
                  <h2 className="text-2xl font-bold text-white mt-4">{service.title}</h2>
                  <div className="text-accent text-xl font-semibold mt-2">{service.price}</div>
                </div>

                {/* Description & Features */}
                <div className="lg:col-span-2 p-6">
                  <p className="text-gray-300 text-lg mb-6">{service.description}</p>

                  <div className="grid grid-cols-1 sm:grid-cols-2 gap-3 mb-6">
                    {service.features.map((feature, featureIndex) => (
                      <div key={featureIndex} className="flex items-center gap-2 text-gray-300">
                        <svg className="w-5 h-5 text-accent flex-shrink-0" fill="currentColor" viewBox="0 0 20 20">
                          <path fillRule="evenodd" d="M16.707 5.293a1 1 0 010 1.414l-8 8a1 1 0 01-1.414 0l-4-4a1 1 0 011.414-1.414L8 12.586l7.293-7.293a1 1 0 011.414 0z" clipRule="evenodd" />
                        </svg>
                        <span>{feature}</span>
                      </div>
                    ))}
                  </div>

                  <Link href={service.cta}>
                    <Button>
                      {service.id === '3dprint'
                        ? (locale === 'de' ? '3D-Datei hochladen' : locale === 'it' ? 'Carica file 3D' : 'Upload 3D file')
                        : dictionary.hero.cta}
                    </Button>
                  </Link>
                </div>
              </div>
            </Card>
          ))}
        </div>

        {/* Pricing Note */}
        <Card className="mt-16 text-center">
          <h3 className="text-xl font-semibold text-white mb-4">
            {locale === 'de' ? 'Preise' : locale === 'it' ? 'Prezzi' : 'Pricing'}
          </h3>
          <p className="text-gray-300 max-w-2xl mx-auto">
            {locale === 'de'
              ? 'Alle Preise verstehen sich exkl. MwSt. Für grössere Projekte oder regelmässige Zusammenarbeit biete ich gerne individuelle Konditionen an.'
              : locale === 'it'
              ? 'Tutti i prezzi sono IVA esclusa. Per progetti più grandi o collaborazioni regolari, offro volentieri condizioni individuali.'
              : 'All prices are excl. VAT. For larger projects or regular collaboration, I am happy to offer individual conditions.'}
          </p>
        </Card>
      </div>
    </div>
  );
}
