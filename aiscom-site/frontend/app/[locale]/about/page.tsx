import PageHeader from '@/components/visual/PageHeader';
import { Locale, getDictionary } from '@/lib/i18n';
import Card from '@/components/ui/Card';

export default async function AboutPage({ params }: { params: Promise<{ locale: string }> }) {
  const { locale } = await params;
  const dictionary = getDictionary(locale as Locale);

  const timeline = [
    {
      year: '2003',
      title: locale === 'de' ? 'Start der Karriere' : locale === 'it' ? 'Inizio carriera' : 'Career Start',
      description: locale === 'de'
        ? 'Beginn der professionellen Softwareentwicklung'
        : locale === 'it'
        ? 'Inizio dello sviluppo software professionale'
        : 'Beginning of professional software development',
    },
    {
      year: '2010',
      title: locale === 'de' ? 'SAP Spezialisierung' : locale === 'it' ? 'Specializzazione SAP' : 'SAP Specialization',
      description: locale === 'de'
        ? 'Fokus auf SAP ABAP Entwicklung'
        : locale === 'it'
        ? 'Focus sullo sviluppo SAP ABAP'
        : 'Focus on SAP ABAP development',
    },
    {
      year: '2020',
      title: locale === 'de' ? '3D-Druck' : locale === 'it' ? 'Stampa 3D' : '3D Printing',
      description: locale === 'de'
        ? 'Erweiterung um 3D-Druck Services'
        : locale === 'it'
        ? 'Espansione nei servizi di stampa 3D'
        : 'Expansion into 3D printing services',
    },
    {
      year: locale === 'de' ? 'Heute' : locale === 'it' ? 'Oggi' : 'Today',
      title: 'AISCOM',
      description: locale === 'de'
        ? 'Gründung von AISCOM - IT & 3D-Druck aus einer Hand'
        : locale === 'it'
        ? 'Fondazione di AISCOM - IT e stampa 3D da un unico fornitore'
        : 'Founded AISCOM - IT & 3D printing from one source',
    },
  ];

  return (
    <div className="py-20">
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
        {/* Header */}
        <PageHeader title={dictionary.about.title} subtitle={dictionary.about.subtitle} />

        {/* Main Content */}
        <div className="grid grid-cols-1 lg:grid-cols-2 gap-12 mb-20">
          {/* Introduction */}
          <div>
            <Card className="h-full">
              <h2 className="text-2xl font-bold text-white mb-4">
                {locale === 'de' ? 'Über mich' : locale === 'it' ? 'Chi sono' : 'About me'}
              </h2>
              <p className="text-gray-300 mb-6 leading-relaxed">
                {dictionary.about.intro}
              </p>
              <p className="text-gray-300 leading-relaxed">
                {dictionary.about.text}
              </p>
            </Card>
          </div>

          {/* Stats & Skills */}
          <div className="space-y-6">
            <Card>
              <div className="grid grid-cols-2 gap-4">
                <div className="text-center p-4 bg-primary rounded-lg">
                  <div className="text-3xl font-bold text-accent mb-1">20+</div>
                  <div className="text-gray-400 text-sm">{dictionary.about.experience}</div>
                </div>
                <div className="text-center p-4 bg-primary rounded-lg">
                  <div className="text-3xl font-bold text-accent mb-1">Zürich</div>
                  <div className="text-gray-400 text-sm">{dictionary.about.location}</div>
                </div>
              </div>
            </Card>

            <Card>
              <h3 className="text-xl font-semibold text-white mb-4">{dictionary.about.skills.title}</h3>
              <div className="flex flex-wrap gap-3">
                {dictionary.about.skills.list.map((skill, index) => (
                  <span
                    key={index}
                    className="px-4 py-2 bg-accent/10 border border-accent/30 rounded-full text-accent text-sm font-medium"
                  >
                    {skill}
                  </span>
                ))}
              </div>
            </Card>

            <Card>
              <h3 className="text-xl font-semibold text-white mb-4">
                {locale === 'de' ? 'Equipment' : locale === 'it' ? 'Attrezzatura' : 'Equipment'}
              </h3>
              <ul className="space-y-2 text-gray-300">
                <li className="flex items-center gap-2">
                  <svg className="w-5 h-5 text-accent" fill="currentColor" viewBox="0 0 20 20">
                    <path fillRule="evenodd" d="M16.707 5.293a1 1 0 010 1.414l-8 8a1 1 0 01-1.414 0l-4-4a1 1 0 011.414-1.414L8 12.586l7.293-7.293a1 1 0 011.414 0z" clipRule="evenodd" />
                  </svg>
                  Bambu Lab X1 Carbon
                </li>
                <li className="flex items-center gap-2">
                  <svg className="w-5 h-5 text-accent" fill="currentColor" viewBox="0 0 20 20">
                    <path fillRule="evenodd" d="M16.707 5.293a1 1 0 010 1.414l-8 8a1 1 0 01-1.414 0l-4-4a1 1 0 011.414-1.414L8 12.586l7.293-7.293a1 1 0 011.414 0z" clipRule="evenodd" />
                  </svg>
                  PLA & ABS Filament
                </li>
                <li className="flex items-center gap-2">
                  <svg className="w-5 h-5 text-accent" fill="currentColor" viewBox="0 0 20 20">
                    <path fillRule="evenodd" d="M16.707 5.293a1 1 0 010 1.414l-8 8a1 1 0 01-1.414 0l-4-4a1 1 0 011.414-1.414L8 12.586l7.293-7.293a1 1 0 011.414 0z" clipRule="evenodd" />
                  </svg>
                  {locale === 'de' ? 'Moderne Entwicklungsumgebung' : locale === 'it' ? 'Ambiente di sviluppo moderno' : 'Modern Development Environment'}
                </li>
              </ul>
            </Card>
          </div>
        </div>

        {/* Timeline */}
        <div className="mb-20">
          <h2 className="text-2xl font-bold text-white text-center mb-12">
            {locale === 'de' ? 'Mein Werdegang' : locale === 'it' ? 'Il mio percorso' : 'My Journey'}
          </h2>
          <div className="relative">
            {/* Timeline line */}
            <div className="absolute left-1/2 transform -translate-x-1/2 h-full w-0.5 bg-accent/30" />

            <div className="space-y-12">
              {timeline.map((item, index) => (
                <div
                  key={index}
                  className={`flex items-center ${index % 2 === 0 ? 'flex-row' : 'flex-row-reverse'}`}
                >
                  <div className={`w-1/2 ${index % 2 === 0 ? 'pr-8 text-right' : 'pl-8 text-left'}`}>
                    <Card className="inline-block">
                      <div className="text-accent font-bold text-lg mb-1">{item.year}</div>
                      <div className="text-white font-semibold mb-2">{item.title}</div>
                      <div className="text-gray-400 text-sm">{item.description}</div>
                    </Card>
                  </div>
                  <div className="relative z-10 w-4 h-4 bg-accent rounded-full border-4 border-primary" />
                  <div className="w-1/2" />
                </div>
              ))}
            </div>
          </div>
        </div>

        {/* Contact CTA */}
        <div className="text-center">
          <Card className="inline-block max-w-xl">
            <h2 className="text-2xl font-bold text-white mb-4">
              {locale === 'de' ? 'Interesse geweckt?' : locale === 'it' ? 'Interessato?' : 'Interested?'}
            </h2>
            <p className="text-gray-300 mb-6">
              {locale === 'de'
                ? 'Ich freue mich auf Ihre Anfrage!'
                : locale === 'it'
                ? 'Non vedo l\'ora di sentirti!'
                : 'I look forward to hearing from you!'}
            </p>
            <a href="mailto:ingo.kohler.zh@gmail.com" className="text-accent text-lg font-semibold hover:text-accent-light transition-colors">
              ingo.kohler.zh@gmail.com
            </a>
          </Card>
        </div>
      </div>
    </div>
  );
}
