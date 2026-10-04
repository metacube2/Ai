import { notFound } from 'next/navigation';
import { Locale, locales, getDictionary } from '@/lib/i18n';
import Header from '@/components/layout/Header';
import Footer from '@/components/layout/Footer';

export async function generateStaticParams() {
  return locales.map((locale) => ({ locale }));
}

export async function generateMetadata({ params }: { params: Promise<{ locale: string }> }) {
  const { locale } = await params;
  const dictionary = getDictionary(locale as Locale);
  return {
    title: dictionary.meta.title,
    description: dictionary.meta.description,
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
