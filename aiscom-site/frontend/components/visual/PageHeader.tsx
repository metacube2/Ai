import HeroBackdrop from './HeroBackdrop';

// Einheitlicher Seitenkopf fuer die Unterseiten.
export default function PageHeader({ title, subtitle }: { title: string; subtitle?: string }) {
  return (
    <div className="relative left-1/2 w-screen -translate-x-1/2 -mt-20 mb-16 overflow-hidden pt-28 pb-16 text-center">
      <HeroBackdrop floor={false} />
      <div className="relative z-10 max-w-3xl mx-auto px-4">
        <h1 className="text-4xl md:text-6xl font-bold tracking-tight text-white mb-4">
          <span className="gradient-text">{title}</span>
        </h1>
        {subtitle && <p className="text-lg text-gray-300">{subtitle}</p>}
        <div className="mx-auto mt-8 h-px w-40 bg-gradient-to-r from-transparent via-accent to-transparent" />
      </div>
    </div>
  );
}
