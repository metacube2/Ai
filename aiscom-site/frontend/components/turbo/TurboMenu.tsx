'use client';

import Link from 'next/link';
import { usePathname, useRouter } from 'next/navigation';
import { useEffect, useState } from 'react';
import { Locale, locales } from '@/lib/i18n';

interface TurboMenuProps {
  locale: Locale;
  nav: {
    home: string;
    about: string;
    services: string;
    portfolio: string;
    products: string;
    upload: string;
    contact: string;
  };
}

// Erster Buchstabe rot wie die Hotkeys in der TP7-IDE
function Hotkey({ text }: { text: string }) {
  return (
    <>
      <span className="tv-k">{text.charAt(0)}</span>
      {text.slice(1)}
    </>
  );
}

export default function TurboMenu({ locale, nav }: TurboMenuProps) {
  const pathname = usePathname();
  const router = useRouter();
  const [uhr, setUhr] = useState('');
  const [offen, setOffen] = useState(false);

  const eintraege = [
    { href: `/${locale}`, label: nav.home },
    { href: `/${locale}/about`, label: nav.about },
    { href: `/${locale}/services`, label: nav.services },
    { href: `/${locale}/portfolio`, label: nav.portfolio },
    { href: `/${locale}#produkte`, label: nav.products },
    { href: `/${locale}/upload`, label: nav.upload },
    { href: `/${locale}/contact`, label: nav.contact },
  ];

  useEffect(() => {
    const tick = () => setUhr(new Date().toLocaleTimeString('de-CH', { hour: '2-digit', minute: '2-digit', second: '2-digit' }));
    tick();
    const t = setInterval(tick, 1000);
    return () => clearInterval(t);
  }, []);

  // Alt+Buchstabe springt wie im Original direkt zum Menuepunkt
  useEffect(() => {
    const taste = (e: KeyboardEvent) => {
      if (e.key === 'Escape') setOffen(false);
      if (e.key === 'F10') {
        e.preventDefault();
        setOffen((o) => !o);
      }
      if (!e.altKey) return;
      const ziel = eintraege.find((x) => x.label.charAt(0).toLowerCase() === e.code.replace('Key', '').toLowerCase());
      if (ziel) {
        e.preventDefault();
        router.push(ziel.href);
      }
    };
    window.addEventListener('keydown', taste);
    return () => window.removeEventListener('keydown', taste);
  });

  const sprache = (neu: Locale) => {
    const teile = pathname.split('/');
    teile[1] = neu;
    router.push(teile.join('/'));
  };

  const aktiv = (href: string) => (href === `/${locale}` ? pathname === href : pathname.startsWith(href));

  return (
    <header className="tv-menu" role="menubar">
      <Link href={`/${locale}`} className="tv-menu-item tv-system" aria-label="AISCOM">≡</Link>
      <button type="button" className="tv-menu-item tv-burger" aria-expanded={offen} onClick={() => setOffen(!offen)}>
        <span className="tv-k">M</span>enü
      </button>
      <nav className={`tv-menu-nav ${offen ? 'tv-offen' : ''}`}>
        {eintraege.map((x) => (
          <Link key={x.href} href={x.href} className={`tv-menu-item ${aktiv(x.href) ? 'tv-aktiv' : ''}`} onClick={() => setOffen(false)}>
            <Hotkey text={x.label} />
          </Link>
        ))}
      </nav>
      <span className="tv-menu-rechts">
        {locales.map((l) => (
          <button key={l} type="button" onClick={() => sprache(l)} className={`tv-menu-item tv-lang ${l === locale ? 'tv-aktiv' : ''}`}>
            {l.toUpperCase()}
          </button>
        ))}
        <span className="tv-uhr" suppressHydrationWarning>{uhr}</span>
      </span>
    </header>
  );
}
