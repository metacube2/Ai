import Link from 'next/link';
import Image from 'next/image';
import { Locale } from '@/lib/i18n';

interface FooterProps {
  locale: Locale;
  dictionary: {
    nav: {
      home: string;
      about: string;
      services: string;
      portfolio: string;
      contact: string;
    };
    footer: {
      tagline: string;
      links: string;
      legal: string;
      imprint: string;
      privacy: string;
      copyright: string;
    };
  };
}

export default function Footer({ locale, dictionary }: FooterProps) {
  const currentYear = new Date().getFullYear();

  return (
    <footer className="bg-primary-dark border-t border-primary-light/30">
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-12">
        <div className="grid grid-cols-1 md:grid-cols-4 gap-8">
          {/* Brand */}
          <div className="md:col-span-2">
            <div className="flex items-center space-x-3 mb-4">
              <Image
                src="/logo.jpg"
                alt="AISCOM Logo"
                width={48}
                height={48}
                className="rounded-lg"
              />
              <span className="text-2xl font-bold text-white">AISCOM</span>
            </div>
            <p className="text-gray-400 mb-4">{dictionary.footer.tagline}</p>
            <a
              href="mailto:ingo.kohler.zh@gmail.com"
              className="text-accent hover:text-accent-light transition-colors"
            >
              ingo.kohler.zh@gmail.com
            </a>
          </div>

          {/* Links */}
          <div>
            <h3 className="text-white font-semibold mb-4">{dictionary.footer.links}</h3>
            <ul className="space-y-2">
              <li>
                <Link href={`/${locale}`} className="text-gray-400 hover:text-accent transition-colors">
                  {dictionary.nav.home}
                </Link>
              </li>
              <li>
                <Link href={`/${locale}/about`} className="text-gray-400 hover:text-accent transition-colors">
                  {dictionary.nav.about}
                </Link>
              </li>
              <li>
                <Link href={`/${locale}/services`} className="text-gray-400 hover:text-accent transition-colors">
                  {dictionary.nav.services}
                </Link>
              </li>
              <li>
                <Link href={`/${locale}/portfolio`} className="text-gray-400 hover:text-accent transition-colors">
                  {dictionary.nav.portfolio}
                </Link>
              </li>
              <li>
                <Link href={`/${locale}/contact`} className="text-gray-400 hover:text-accent transition-colors">
                  {dictionary.nav.contact}
                </Link>
              </li>
            </ul>
          </div>

          {/* Legal */}
          <div>
            <h3 className="text-white font-semibold mb-4">{dictionary.footer.legal}</h3>
            <ul className="space-y-2">
              <li>
                <Link href={`/${locale}/legal/imprint`} className="text-gray-400 hover:text-accent transition-colors">
                  {dictionary.footer.imprint}
                </Link>
              </li>
              <li>
                <Link href={`/${locale}/legal/privacy`} className="text-gray-400 hover:text-accent transition-colors">
                  {dictionary.footer.privacy}
                </Link>
              </li>
            </ul>
          </div>
        </div>

        {/* Copyright */}
        <div className="mt-8 pt-8 border-t border-primary-light/30 text-center text-gray-500">
          <p>{dictionary.footer.copyright.replace('{year}', currentYear.toString())}</p>
        </div>
      </div>
    </footer>
  );
}
