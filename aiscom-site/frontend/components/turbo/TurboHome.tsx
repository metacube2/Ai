import Link from 'next/link';
import { Locale, getDictionary } from '@/lib/i18n';

type Dictionary = ReturnType<typeof getDictionary>;

const LOGO = String.raw` █████╗ ██╗███████╗ ██████╗ ██████╗ ███╗   ███╗
██╔══██╗██║██╔════╝██╔════╝██╔═══██╗████╗ ████║
███████║██║███████╗██║     ██║   ██║██╔████╔██║
██╔══██║██║╚════██║██║     ██║   ██║██║╚██╔╝██║
██║  ██║██║███████║╚██████╗╚██████╔╝██║ ╚═╝ ██║
╚═╝  ╚═╝╚═╝╚══════╝ ╚═════╝ ╚═════╝ ╚═╝     ╚═╝`;

// Fenster im Turbo-Vision-Stil: Doppelrahmen, Titel in der Rahmenlinie, Schatten
export function TvFenster({
  titel,
  nummer,
  id,
  grau = false,
  className = '',
  children,
}: {
  titel: string;
  nummer?: number;
  id?: string;
  grau?: boolean;
  className?: string;
  children: React.ReactNode;
}) {
  return (
    <section id={id} className={`tv-fenster ${grau ? 'tv-dialog' : ''} ${className}`}>
      <span className="tv-schliessen" aria-hidden="true">[<b>■</b>]</span>
      <h2 className="tv-titel"><span>{titel}</span></h2>
      {nummer !== undefined && <span className="tv-nummer" aria-hidden="true">{nummer}</span>}
      <div className="tv-inhalt">{children}</div>
    </section>
  );
}

export default function TurboHome({ locale, dictionary }: { locale: Locale; dictionary: Dictionary }) {
  const t = (de: string, en: string, it: string) => (locale === 'de' ? de : locale === 'it' ? it : en);

  const dienste = [
    { key: '3dprint' as const, href: `/${locale}/upload`, kuerzel: '3D' },
    { key: 'csharp' as const, href: `/${locale}/contact`, kuerzel: 'C#' },
    { key: 'abap' as const, href: `/${locale}/contact`, kuerzel: 'SAP' },
    { key: 'web' as const, href: `/${locale}/contact`, kuerzel: 'WWW' },
  ];

  const kennzahlen = [
    { wert: '20+', text: t('Jahre Erfahrung', 'Years experience', 'Anni di esperienza') },
    { wert: '4', text: t('Fachbereiche', 'Specialties', 'Specializzazioni') },
    { wert: '256³', text: t('mm Bauraum', 'mm build volume', 'mm volume di stampa') },
    { wert: 'ZH', text: t('Standort Zürich', 'Based in Zurich', 'Sede Zurigo') },
  ];

  return (
    <div className="tv-desktop">
      {/* Startfenster */}
      <TvFenster titel="AISCOM.EXE" nummer={1} className="tv-hero">
        <pre className="tv-logo" aria-label="AISCOM">{LOGO}</pre>
        <p className="tv-zeile tv-gelb">3D-Druck · C# · SAP ABAP · Web</p>
        <h1 className="tv-h1">
          {dictionary.hero.title} <span className="tv-hell">{dictionary.hero.subtitle}</span>
        </h1>
        <p>{dictionary.hero.description}</p>
        <p className="tv-prompt" aria-hidden="true">
          C:\AISCOM&gt; <span className="tv-cursor">_</span>
        </p>
        <div className="tv-knoepfe">
          <Link href={`/${locale}/contact`} className="tv-knopf tv-standard">{dictionary.hero.cta}</Link>
          <Link href={`/${locale}/services`} className="tv-knopf">{dictionary.hero.secondary}</Link>
        </div>
      </TvFenster>

      <div className="tv-raster-2">
        {/* Dienstleistungen als Liste */}
        <TvFenster titel={`${dictionary.services.title.toUpperCase()}.TXT`} nummer={2}>
          <p className="tv-hell">{dictionary.services.subtitle}</p>
          <ul className="tv-liste">
            {dienste.map((d) => (
              <li key={d.key}>
                <Link href={d.href}>
                  <span className="tv-kuerzel">{d.kuerzel}</span>
                  <span className="tv-liste-titel">{dictionary.services[d.key].title}</span>
                  <span className="tv-preis">{dictionary.services[d.key].price}</span>
                </Link>
                <p>{dictionary.services[d.key].description}</p>
              </li>
            ))}
          </ul>
          <div className="tv-knoepfe">
            <Link href={`/${locale}/services`} className="tv-knopf">{dictionary.hero.secondary}</Link>
          </div>
        </TvFenster>

        {/* Ueber mich mit Kennzahlen */}
        <TvFenster titel={dictionary.about.title} nummer={3}>
          <p className="tv-hell">{dictionary.about.subtitle}</p>
          <p>{dictionary.about.intro}</p>
          <ul className="tv-checks">
            {dictionary.about.skills.list.map((s: string) => (
              <li key={s}>[<b>X</b>] {s}</li>
            ))}
          </ul>
          <table className="tv-tabelle">
            <tbody>
              {kennzahlen.map((k) => (
                <tr key={k.text}>
                  <td className="tv-gelb">{k.wert}</td>
                  <td>{k.text}</td>
                </tr>
              ))}
            </tbody>
          </table>
          <div className="tv-knoepfe">
            <Link href={`/${locale}/about`} className="tv-knopf">{dictionary.hero.secondary}</Link>
          </div>
        </TvFenster>
      </div>

      {/* Produkte */}
      {/* Titel unveraendert lassen: pruefen.sh und Suchmaschinen suchen genau diesen Text */}
      <TvFenster titel={dictionary.products.title} nummer={4} id="produkte">
        <p className="tv-hell">{dictionary.products.subtitle}</p>
        <div className="tv-produkte">
          {dictionary.products.items.map((p) => (
            <a
              key={p.href}
              href={p.href}
              className="tv-produkt"
              {...(p.href.startsWith('http') ? { target: '_blank', rel: 'noopener noreferrer' } : {})}
            >
              <span className="tv-produkt-kopf">
                <span className="tv-gelb">{p.name}</span>
                <span className="tv-badge">{p.badge}</span>
              </span>
              <span className="tv-produkt-text">{p.description}</span>
              <span className="tv-knopf tv-klein">{p.cta} ►</span>
            </a>
          ))}
        </div>
        {dictionary.products.items.filter((p) => p.live).map((p) => (
          <p key={p.live} className="tv-hell">
            <a href={p.live} target="_blank" rel="noopener noreferrer" className="tv-knopf tv-klein">
              ▶ {dictionary.products.live_text}
            </a>
          </p>
        ))}
        <p className="tv-hell">
          <a href="/feed.xml" type="application/rss+xml" className="tv-knopf tv-klein">
            ◉ {dictionary.products.rss_text}
          </a>
        </p>
      </TvFenster>

      {/* Zeitreise zur ersten Homepage (statisch unter /nextvision/) */}
      <TvFenster titel={dictionary.retro.title} nummer={5} id="zeitreise">
        <p>{dictionary.retro.text}</p>
        <div className="tv-knoepfe">
          <a href={dictionary.retro.href} className="tv-knopf">{dictionary.retro.cta} ►</a>
        </div>
      </TvFenster>

      {/* Abschluss als Dialog */}
      <TvFenster titel={t('Projekt starten?', 'Start a project?', 'Iniziare un progetto?')} grau className="tv-cta">
        <p>
          {t(
            'Kontaktieren Sie mich für eine unverbindliche Beratung.',
            'Contact me for a free consultation.',
            'Contattatemi per una consulenza gratuita.',
          )}
        </p>
        <div className="tv-knoepfe">
          <Link href={`/${locale}/contact`} className="tv-knopf tv-standard">   OK   </Link>
          <Link href={`/${locale}/upload`} className="tv-knopf">3D-Upload</Link>
        </div>
      </TvFenster>
    </div>
  );
}
