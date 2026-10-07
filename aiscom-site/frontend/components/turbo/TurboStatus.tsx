'use client';

import Link from 'next/link';
import { useRouter } from 'next/navigation';
import { useEffect } from 'react';
import { Locale } from '@/lib/i18n';

interface TurboStatusProps {
  locale: Locale;
  labels: { contact: string; imprint: string; privacy: string; services: string; copyright: string };
}

export default function TurboStatus({ locale, labels }: TurboStatusProps) {
  const router = useRouter();

  // F-Tasten wie in der TP7-Statuszeile
  useEffect(() => {
    const taste = (e: KeyboardEvent) => {
      const ziel: Record<string, string> = {
        F1: `/${locale}/services`,
        F2: `/${locale}/upload`,
        F3: `/${locale}/portfolio`,
      };
      if (ziel[e.key]) {
        e.preventDefault();
        router.push(ziel[e.key]);
      }
      if (e.altKey && e.code === 'KeyX') {
        e.preventDefault();
        router.push(`/${locale}/contact`);
      }
    };
    window.addEventListener('keydown', taste);
    return () => window.removeEventListener('keydown', taste);
  }, [locale, router]);

  return (
    <footer className="tv-status">
      <Link href={`/${locale}/services`}><span className="tv-k">F1</span> {labels.services}</Link>
      <Link href={`/${locale}/upload`} className="tv-nur-breit"><span className="tv-k">F2</span> 3D-Upload</Link>
      <Link href={`/${locale}/contact`}><span className="tv-k">Alt-X</span> {labels.contact}</Link>
      <Link href={`/${locale}/legal/imprint`}>{labels.imprint}</Link>
      <Link href={`/${locale}/legal/privacy`}>{labels.privacy}</Link>
      <span className="tv-status-rechts">{labels.copyright.replace('{year}', String(new Date().getFullYear()))}</span>
    </footer>
  );
}
