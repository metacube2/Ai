'use client';

import { usePathname, useRouter } from 'next/navigation';
import { Locale, locales } from '@/lib/i18n';

interface LanguageSwitchProps {
  locale: Locale;
}

const languageNames: Record<Locale, string> = {
  de: 'DE',
  en: 'EN',
  it: 'IT',
};

export default function LanguageSwitch({ locale }: LanguageSwitchProps) {
  const pathname = usePathname();
  const router = useRouter();

  const switchLocale = (newLocale: Locale) => {
    // Replace the locale in the pathname
    const segments = pathname.split('/');
    segments[1] = newLocale;
    const newPath = segments.join('/');
    router.push(newPath);
  };

  return (
    <div className="flex items-center space-x-1">
      {locales.map((loc) => (
        <button
          key={loc}
          onClick={() => switchLocale(loc)}
          className={`px-2 py-1 text-sm rounded transition-colors duration-200 ${
            locale === loc
              ? 'bg-accent text-primary font-semibold'
              : 'text-gray-400 hover:text-accent'
          }`}
        >
          {languageNames[loc]}
        </button>
      ))}
    </div>
  );
}
