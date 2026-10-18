/**
 * Herkunft und Strom (2026-10-09): Schweizerkreuz (Fahne, nicht Wappen -
 * das ist dem Bund vorbehalten), neutrales "Swiss made" (nicht die Marke
 * SWISS LABEL) und CO2-neutral mit Solarstrom. Dieselben drei Abzeichen
 * stehen auf der Aurora-Livecam, der App-Beta-Seite und in der Android-App.
 */
interface TrustBadgesProps {
  labels: { swiss: string; swissmade: string; solar: string };
  className?: string;
}

const pill =
  'inline-flex items-center gap-2 rounded-full py-1 pl-1.5 pr-3 text-[13px] font-semibold leading-tight whitespace-nowrap';

export default function TrustBadges({ labels, className = '' }: TrustBadgesProps) {
  return (
    <div className={`trust-badges flex flex-wrap justify-center gap-2.5 ${className}`}>
      <span className={`${pill} bg-white text-neutral-900 border border-[#e3d6d4]`}>
        <svg width="22" height="22" viewBox="0 0 32 32" aria-hidden="true" focusable="false">
          <rect width="32" height="32" rx="3" fill="#DA291C" />
          <path fill="#fff" d="M13 6h6v7h7v6h-7v7h-6v-7H6v-6h7z" />
        </svg>
        {labels.swiss}
      </span>
      <span className={`${pill} bg-white text-neutral-900 border border-[#e3d6d4]`}>
        <svg width="22" height="22" viewBox="0 0 32 32" aria-hidden="true" focusable="false">
          <circle cx="16" cy="16" r="15" fill="#fff" stroke="#DA291C" strokeWidth="2" />
          <circle cx="16" cy="16" r="11" fill="none" stroke="#DA291C" strokeWidth="1" strokeDasharray="2 2" />
          <path fill="#DA291C" d="M14.2 10h3.6v4.2H22v3.6h-4.2V22h-3.6v-4.2H10v-3.6h4.2z" />
        </svg>
        {labels.swissmade}
      </span>
      <span className={`${pill} bg-[#E8F5E9] text-[#1B5E20] border border-[#A5D6A7]`}>
        <svg width="22" height="22" viewBox="0 0 32 32" aria-hidden="true" focusable="false">
          <circle cx="16" cy="16" r="16" fill="#2E7D32" />
          <circle cx="22" cy="10" r="3.2" fill="#FFD54F" />
          <g stroke="#FFD54F" strokeWidth="1.4" strokeLinecap="round">
            <path d="M22 4.6v1.6M22 13.8v1.6M16.6 10h1.6M25.8 10h1.6M18.2 6.2l1.1 1.1M24.7 12.7l1.1 1.1M25.8 6.2l-1.1 1.1" />
          </g>
          <path fill="#fff" d="M8 24c0-7 5-12 13-12-1 8-5 13-12 13l-1-1z" />
        </svg>
        {labels.solar}
      </span>
    </div>
  );
}
