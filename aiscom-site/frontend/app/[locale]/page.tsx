import Link from 'next/link';
import { Locale, getDictionary } from '@/lib/i18n';
import Card from '@/components/ui/Card';
import HeroBackdrop from '@/components/visual/HeroBackdrop';
import HeroPrinter from '@/components/visual/HeroPrinter';
import CodeWindow from '@/components/visual/CodeWindow';
import Marquee from '@/components/visual/Marquee';
import Reveal from '@/components/visual/Reveal';
import Wave from '@/components/visual/Wave';
import { RadioVisual, CameraVisual } from '@/components/visual/ProductVisuals';
import Button from '@/components/ui/Button';

// Service icons as SVG components
const PrinterIcon = () => (
  <svg className="w-9 h-9 text-accent" fill="none" stroke="currentColor" viewBox="0 0 24 24">
    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} d="M6.72 13.829c-.24.03-.48.062-.72.096m.72-.096a42.415 42.415 0 0110.56 0m-10.56 0L6.34 18m10.94-4.171c.24.03.48.062.72.096m-.72-.096L17.66 18m0 0l.229 2.523a1.125 1.125 0 01-1.12 1.227H7.231c-.662 0-1.18-.568-1.12-1.227L6.34 18m11.318 0h1.091A2.25 2.25 0 0021 15.75V9.456c0-1.081-.768-2.015-1.837-2.175a48.055 48.055 0 00-1.913-.247M6.34 18H5.25A2.25 2.25 0 013 15.75V9.456c0-1.081.768-2.015 1.837-2.175a48.041 48.041 0 011.913-.247m10.5 0a48.536 48.536 0 00-10.5 0m10.5 0V3.375c0-.621-.504-1.125-1.125-1.125h-8.25c-.621 0-1.125.504-1.125 1.125v3.659M18 10.5h.008v.008H18V10.5zm-3 0h.008v.008H15V10.5z" />
  </svg>
);

const CodeIcon = () => (
  <svg className="w-9 h-9 text-accent" fill="none" stroke="currentColor" viewBox="0 0 24 24">
    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} d="M17.25 6.75L22.5 12l-5.25 5.25m-10.5 0L1.5 12l5.25-5.25m7.5-3l-4.5 16.5" />
  </svg>
);

const ServerIcon = () => (
  <svg className="w-9 h-9 text-accent" fill="none" stroke="currentColor" viewBox="0 0 24 24">
    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} d="M5.25 14.25h13.5m-13.5 0a3 3 0 01-3-3m3 3a3 3 0 100 6h13.5a3 3 0 100-6m-16.5-3a3 3 0 013-3h13.5a3 3 0 013 3m-19.5 0a4.5 4.5 0 01.9-2.7L5.737 5.1a3.375 3.375 0 012.7-1.35h7.126c1.062 0 2.062.5 2.7 1.35l2.587 3.45a4.5 4.5 0 01.9 2.7m0 0a3 3 0 01-3 3m0 3h.008v.008h-.008v-.008zm0-6h.008v.008h-.008v-.008zm-3 6h.008v.008h-.008v-.008zm0-6h.008v.008h-.008v-.008z" />
  </svg>
);

const GlobeIcon = () => (
  <svg className="w-9 h-9 text-accent" fill="none" stroke="currentColor" viewBox="0 0 24 24">
    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} d="M12 21a9.004 9.004 0 008.716-6.747M12 21a9.004 9.004 0 01-8.716-6.747M12 21c2.485 0 4.5-4.03 4.5-9S14.485 3 12 3m0 18c-2.485 0-4.5-4.03-4.5-9S9.515 3 12 3m0 0a8.997 8.997 0 017.843 4.582M12 3a8.997 8.997 0 00-7.843 4.582m15.686 0A11.953 11.953 0 0112 10.5c-2.998 0-5.74-1.1-7.843-2.918m15.686 0A8.959 8.959 0 0121 12c0 .778-.099 1.533-.284 2.253m0 0A17.919 17.919 0 0112 16.5c-3.162 0-6.133-.815-8.716-2.247m0 0A9.015 9.015 0 013 12c0-1.605.42-3.113 1.157-4.418" />
  </svg>
);

export default async function HomePage({ params }: { params: Promise<{ locale: string }> }) {
  const { locale } = await params;
  const dictionary = getDictionary(locale as Locale);

  const services = [
    {
      icon: <PrinterIcon />,
      title: dictionary.services['3dprint'].title,
      description: dictionary.services['3dprint'].description,
      price: dictionary.services['3dprint'].price,
    },
    {
      icon: <CodeIcon />,
      title: dictionary.services.csharp.title,
      description: dictionary.services.csharp.description,
      price: dictionary.services.csharp.price,
    },
    {
      icon: <ServerIcon />,
      title: dictionary.services.abap.title,
      description: dictionary.services.abap.description,
      price: dictionary.services.abap.price,
    },
    {
      icon: <GlobeIcon />,
      title: dictionary.services.web.title,
      description: dictionary.services.web.description,
      price: dictionary.services.web.price,
    },
  ];

  const stats = [
    { value: '20+', label: locale === 'de' ? 'Jahre Erfahrung' : locale === 'it' ? 'Anni di esperienza' : 'Years Experience' },
    { value: '4', label: locale === 'de' ? 'Fachbereiche' : locale === 'it' ? 'Specializzazioni' : 'Specialties' },
    { value: '256³', label: locale === 'de' ? 'mm Bauraum' : locale === 'it' ? 'mm volume di stampa' : 'mm build volume' },
    { value: 'ZH', label: locale === 'de' ? 'Standort Zürich' : locale === 'it' ? 'Sede Zurigo' : 'Based in Zurich' },
  ];

  const productVisuals: Record<string, JSX.Element> = {
    '/macyaesu/': <RadioVisual />,
    '/kamera-uploader/': <CameraVisual />,
  };

  return (
    <>
      {/* Hero Section */}
      <section className="relative -mt-16 min-h-screen flex items-center overflow-hidden pt-24 pb-16">
        <div className="absolute inset-0 bg-gradient-to-b from-primary-dark via-primary to-primary" />
        <HeroBackdrop />

        <div className="relative z-10 max-w-7xl mx-auto w-full px-4 sm:px-6 lg:px-8 grid grid-cols-1 lg:grid-cols-2 gap-12 items-center">
          <div className="text-center lg:text-left">
            <Reveal>
              <span className="eyebrow mb-6">
                <span className="pulse-dot h-2 w-2 rounded-full bg-[#5eead4]" />
                3D-Druck · C# · SAP ABAP · Web
              </span>
            </Reveal>
            <Reveal delay={100}>
              <h1 className="mt-6 text-4xl sm:text-5xl md:text-6xl xl:text-7xl font-bold tracking-tight text-white leading-[1.05]">
                {dictionary.hero.title}
                <span className="block gradient-text mt-2">{dictionary.hero.subtitle}</span>
              </h1>
            </Reveal>
            <Reveal delay={200}>
              <p className="mt-6 text-lg sm:text-xl text-gray-300 max-w-xl mx-auto lg:mx-0">
                {dictionary.hero.description}
              </p>
            </Reveal>
            <Reveal delay={300}>
              <div className="mt-10 flex flex-col sm:flex-row gap-4 justify-center lg:justify-start">
                <Link href={`/${locale}/contact`}>
                  <Button size="lg" className="shadow-[0_0_40px_-8px_rgba(135,206,235,0.8)]">{dictionary.hero.cta}</Button>
                </Link>
                <Link href={`/${locale}/services`}>
                  <Button variant="secondary" size="lg">{dictionary.hero.secondary}</Button>
                </Link>
              </div>
            </Reveal>
          </div>

          <div className="relative mx-auto w-full max-w-md lg:max-w-lg">
            <div className="absolute inset-8 rounded-full bg-accent/20 blur-3xl" aria-hidden="true" />
            <div className="relative float-slow">
              <HeroPrinter />
            </div>
            <div className="float-slower absolute -bottom-20 -right-4 lg:-right-20 hidden sm:block origin-bottom-right scale-[0.85]">
              <CodeWindow />
            </div>
          </div>
        </div>

        {/* Scroll indicator */}
        <div className="absolute bottom-6 left-1/2 -translate-x-1/2 z-10 hidden md:flex h-10 w-6 justify-center rounded-full border-2 border-accent/40 pt-2" aria-hidden="true">
          <span className="h-2 w-1 rounded-full bg-accent animate-bounce" />
        </div>
      </section>

      {/* Laufband */}
      <section className="bg-primary border-y border-white/5">
        <Marquee />
      </section>

      {/* Services Preview Section */}
      <section className="relative py-24 bg-primary-dark overflow-hidden">
        <div className="absolute -top-40 right-0 h-96 w-96 rounded-full bg-[#7c9cff]/10 blur-3xl" aria-hidden="true" />
        <div className="relative max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <Reveal className="text-center mb-14">
            <h2 className="section-title">{dictionary.services.title}</h2>
            <p className="section-subtitle">{dictionary.services.subtitle}</p>
          </Reveal>

          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
            {services.map((service, index) => (
              <Reveal key={index} delay={index * 120} className="h-full">
                <div className="glass glow-border group h-full rounded-2xl p-7 text-center transition-transform duration-300 hover:-translate-y-2">
                  <div className="icon-badge mb-5 transition-transform duration-500 group-hover:rotate-6 group-hover:scale-110">
                    {service.icon}
                  </div>
                  <h3 className="text-xl font-semibold text-white mb-3">{service.title}</h3>
                  <p className="text-gray-400 text-sm mb-5 line-clamp-3">{service.description}</p>
                  <p className="inline-block rounded-full bg-accent/10 px-4 py-1 text-accent font-semibold text-sm">{service.price}</p>
                </div>
              </Reveal>
            ))}
          </div>

          <Reveal className="text-center mt-12">
            <Link href={`/${locale}/services`}>
              <Button variant="secondary">{dictionary.hero.secondary}</Button>
            </Link>
          </Reveal>
        </div>
      </section>

      <Wave from="#050b14" to="#0a1628" />

      {/* Products Section */}
      <section id="produkte" className="relative py-24 bg-primary scroll-mt-16 overflow-hidden">
        <div className="absolute left-1/2 top-0 h-80 w-[40rem] -translate-x-1/2 rounded-full bg-accent/10 blur-3xl" aria-hidden="true" />
        <div className="relative max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <Reveal className="text-center mb-14">
            <h2 className="section-title">
              <span className="gradient-text">{dictionary.products.title}</span>
            </h2>
            <p className="section-subtitle">{dictionary.products.subtitle}</p>
          </Reveal>

          <div className="grid grid-cols-1 md:grid-cols-2 gap-8 max-w-5xl mx-auto">
            {dictionary.products.items.map((product, index) => (
              <Reveal key={product.href} delay={index * 150} className="h-full">
                <a href={product.href} className="glass glow-border group block h-full rounded-3xl p-6 transition-transform duration-300 hover:-translate-y-2">
                  {productVisuals[product.href]}
                  <div className="mt-6 flex items-center justify-between gap-4">
                    <h3 className="text-2xl font-semibold text-white">{product.name}</h3>
                    <span className="shrink-0 rounded-full border border-accent/30 bg-accent/10 px-3 py-1 text-xs font-semibold uppercase tracking-wide text-accent">
                      {product.badge}
                    </span>
                  </div>
                  <p className="mt-3 text-gray-400">{product.description}</p>
                  <p className="mt-5 inline-flex items-center gap-2 text-accent font-semibold">
                    {product.cta}
                    <span className="transition-transform duration-300 group-hover:translate-x-1">&rarr;</span>
                  </p>
                </a>
              </Reveal>
            ))}
          </div>
        </div>
      </section>

      <Wave from="#0a1628" to="#050b14" flip />

      {/* About Preview Section */}
      <section className="relative py-24 bg-primary-dark overflow-hidden">
        <div className="relative max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="grid grid-cols-1 lg:grid-cols-2 gap-14 items-center">
            <Reveal>
              <h2 className="section-title">{dictionary.about.title}</h2>
              <p className="gradient-text text-xl font-semibold mb-4">{dictionary.about.subtitle}</p>
              <p className="text-gray-300 mb-6 text-lg">{dictionary.about.intro}</p>
              <div className="flex flex-wrap gap-3 mb-8">
                {dictionary.about.skills.list.map((skill, index) => (
                  <span
                    key={index}
                    className="glass rounded-full px-4 py-2 text-accent text-sm font-medium"
                  >
                    {skill}
                  </span>
                ))}
              </div>
              <Link href={`/${locale}/about`}>
                <Button variant="secondary">{dictionary.hero.secondary}</Button>
              </Link>
            </Reveal>
            <div className="grid grid-cols-2 gap-5">
              {stats.map((stat, index) => (
                <Reveal key={stat.label} delay={index * 120}>
                  <div className="glass glow-border rounded-2xl p-7 text-center">
                    <div className="gradient-text text-4xl md:text-5xl font-bold mb-2">{stat.value}</div>
                    <div className="text-gray-400 text-sm">{stat.label}</div>
                  </div>
                </Reveal>
              ))}
            </div>
          </div>
        </div>
      </section>

      {/* CTA Section */}
      <section className="relative py-24 overflow-hidden">
        <div className="absolute inset-0 bg-gradient-to-r from-primary-dark via-primary to-primary-dark" />
        <HeroBackdrop floor={false} />
        <Reveal className="relative z-10 max-w-4xl mx-auto px-4 sm:px-6 lg:px-8 text-center">
          <h2 className="text-3xl md:text-5xl font-bold text-white mb-4 tracking-tight">
            {locale === 'de' ? 'Projekt starten?' : locale === 'it' ? 'Iniziare un progetto?' : 'Start a project?'}
          </h2>
          <p className="text-gray-300 mb-10 text-lg">
            {locale === 'de'
              ? 'Kontaktieren Sie mich für eine unverbindliche Beratung.'
              : locale === 'it'
              ? 'Contattatemi per una consulenza gratuita.'
              : 'Contact me for a free consultation.'}
          </p>
          <Link href={`/${locale}/contact`}>
            <Button size="lg" className="shadow-[0_0_50px_-8px_rgba(135,206,235,0.9)]">{dictionary.hero.cta}</Button>
          </Link>
        </Reveal>
      </section>
    </>
  );
}
