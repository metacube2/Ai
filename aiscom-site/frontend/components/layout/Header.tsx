'use client';

import Link from 'next/link';
import Image from 'next/image';
import { useState } from 'react';
import { Locale } from '@/lib/i18n';
import LanguageSwitch from './LanguageSwitch';

interface HeaderProps {
  locale: Locale;
  dictionary: {
    nav: {
      home: string;
      about: string;
      services: string;
      portfolio: string;
      products: string;
      upload: string;
      contact: string;
    };
  };
}

export default function Header({ locale, dictionary }: HeaderProps) {
  const [isMenuOpen, setIsMenuOpen] = useState(false);

  const navItems = [
    { href: `/${locale}`, label: dictionary.nav.home },
    { href: `/${locale}/about`, label: dictionary.nav.about },
    { href: `/${locale}/services`, label: dictionary.nav.services },
    { href: `/${locale}/portfolio`, label: dictionary.nav.portfolio },
    { href: `/${locale}#produkte`, label: dictionary.nav.products },
    { href: `/${locale}/upload`, label: dictionary.nav.upload },
    { href: `/${locale}/contact`, label: dictionary.nav.contact },
  ];

  return (
    <header className="fixed top-0 left-0 right-0 z-50 bg-primary/60 backdrop-blur-xl border-b border-white/5 shadow-[0_10px_30px_-20px_rgba(0,0,0,0.8)]">
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
        <div className="flex items-center justify-between h-16">
          {/* Logo */}
          <Link href={`/${locale}`} className="flex items-center space-x-3">
            <Image
              src="/logo.jpg"
              alt="AISCOM Logo"
              width={40}
              height={40}
              className="rounded-lg"
            />
            <span className="text-xl font-bold text-white">AISCOM</span>
          </Link>

          {/* Desktop Navigation */}
          <nav className="hidden md:flex items-center space-x-6">
            {navItems.map((item) => (
              <Link
                key={item.href}
                href={item.href}
                className="relative text-gray-300 hover:text-white transition-colors duration-200 after:absolute after:-bottom-1 after:left-0 after:h-0.5 after:w-full after:origin-left after:scale-x-0 after:rounded-full after:bg-gradient-to-r after:from-accent after:to-[#7c9cff] after:transition-transform after:duration-300 hover:after:scale-x-100"
              >
                {item.label}
              </Link>
            ))}
            <LanguageSwitch locale={locale} />
          </nav>

          {/* Mobile Menu Button */}
          <button
            className="md:hidden text-white p-2"
            onClick={() => setIsMenuOpen(!isMenuOpen)}
            aria-label="Toggle menu"
          >
            <svg
              className="w-6 h-6"
              fill="none"
              stroke="currentColor"
              viewBox="0 0 24 24"
            >
              {isMenuOpen ? (
                <path
                  strokeLinecap="round"
                  strokeLinejoin="round"
                  strokeWidth={2}
                  d="M6 18L18 6M6 6l12 12"
                />
              ) : (
                <path
                  strokeLinecap="round"
                  strokeLinejoin="round"
                  strokeWidth={2}
                  d="M4 6h16M4 12h16M4 18h16"
                />
              )}
            </svg>
          </button>
        </div>

        {/* Mobile Navigation */}
        {isMenuOpen && (
          <nav className="md:hidden py-4 border-t border-primary-light/30">
            <div className="flex flex-col space-y-3">
              {navItems.map((item) => (
                <Link
                  key={item.href}
                  href={item.href}
                  className="text-gray-300 hover:text-accent transition-colors duration-200 px-2 py-1"
                  onClick={() => setIsMenuOpen(false)}
                >
                  {item.label}
                </Link>
              ))}
              <div className="pt-2 border-t border-primary-light/30">
                <LanguageSwitch locale={locale} />
              </div>
            </div>
          </nav>
        )}
      </div>
    </header>
  );
}
