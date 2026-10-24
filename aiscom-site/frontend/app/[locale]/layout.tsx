import { notFound } from 'next/navigation';
import { Locale, locales, getDictionary } from '@/lib/i18n';
import Header from '@/components/layout/Header';
import Footer from '@/components/layout/Footer';
import TrustBadges from '@/components/layout/TrustBadges';
import TurboMenu from '@/components/turbo/TurboMenu';
import TurboStatus from '@/components/turbo/TurboStatus';
import { aktuellesDesign } from '@/lib/design';

// Das Design (turbo/klassisch) wird bei jeder Anfrage aus design.json
// gelesen, damit der Schalter in /admin/design ohne Neubau wirkt.
// generateStaticParams entfernt: es renderte alle Sprachseiten beim Build
// fest vor, der Schalter haette dann erst nach einem Neubau gewirkt.
export const dynamic = 'force-dynamic';

export async function generateMetadata({ params }: { params: Promise<{ locale: string }> }) {
  const { locale } = await params;
  const dictionary = getDictionary(locale as Locale);
  return {
    title: dictionary.meta.title,
    description: dictionary.meta.description,
    // RSS-Feed der App-Versionen (statisch von Apache: /feed.xml, erzeugt mit deploy/feed/erzeugen.py)
    alternates: { types: { 'application/rss+xml': 'https://www.aiscom.ch/feed.xml' } },
  };
}

export default async function LocaleLayout({
  children,
  params,
}: {
  children: React.ReactNode;
  params: Promise<{ locale: string }>;
}) {
  const { locale } = await params;

  if (!locales.includes(locale as Locale)) {
    notFound();
  }

  const dictionary = getDictionary(locale as Locale);
  const design = aktuellesDesign();

  if (design === 'turbo') {
    return (
      <html lang={locale} data-design="turbo">
        <body data-design="turbo">
          <TurboMenu locale={locale as Locale} nav={dictionary.nav} />
          <main className="tv-main">{children}</main>
          <TrustBadges labels={dictionary.badges} className="tv-badges py-3" />
          <TurboStatus
            locale={locale as Locale}
            labels={{
              contact: dictionary.nav.contact,
              services: dictionary.nav.services,
              imprint: dictionary.footer.imprint,
              privacy: dictionary.footer.privacy,
              copyright: dictionary.footer.copyright,
            }}
          />
        </body>
      </html>
    );
  }

  return (
    <html lang={locale}>
      <body>
        <script dangerouslySetInnerHTML={{ __html: "document.documentElement.classList.add('js')" }} />
        <Header locale={locale as Locale} dictionary={dictionary} />
        <main className="min-h-screen pt-16">
          {children}
        </main>
        <Footer locale={locale as Locale} dictionary={dictionary} />
      </body>
    </html>
  );
}
