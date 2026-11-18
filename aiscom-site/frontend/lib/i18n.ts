import de from '@/locales/de.json';
import en from '@/locales/en.json';
import it from '@/locales/it.json';

export const locales = ['de', 'en', 'it'] as const;
export type Locale = (typeof locales)[number];
export const defaultLocale: Locale = 'de';

const dictionaries = {
  de,
  en,
  it,
};

export function getDictionary(locale: Locale) {
  return dictionaries[locale] || dictionaries[defaultLocale];
}

export function getLocaleFromHeaders(acceptLanguage: string | null): Locale {
  if (!acceptLanguage) return defaultLocale;

  const languages = acceptLanguage
    .split(',')
    .map((lang) => {
      const [code, q = '1'] = lang.trim().split(';q=');
      return { code: code.split('-')[0].toLowerCase(), q: parseFloat(q) };
    })
    .sort((a, b) => b.q - a.q);

  for (const { code } of languages) {
    if (locales.includes(code as Locale)) {
      return code as Locale;
    }
  }

  return defaultLocale;
}

export function isValidLocale(locale: string): locale is Locale {
  return locales.includes(locale as Locale);
}
