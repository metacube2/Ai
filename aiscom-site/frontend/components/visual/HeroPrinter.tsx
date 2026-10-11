// Stilisierter Bambu-Lab-X1C mit AMS, der Schicht fuer Schicht eine Vase druckt.
// Reines SVG + CSS-Animation: scharf in jeder Aufloesung, respektiert
// "weniger Bewegung" ueber globals.css.

const CYCLE = '14s';

export default function HeroPrinter() {
  return (
    <svg
      viewBox="0 0 420 470"
      className="w-full h-auto drop-shadow-[0_30px_60px_rgba(0,0,0,0.55)]"
      role="img"
      aria-label="3D-Drucker druckt eine Vase"
    >
      <defs>
        <linearGradient id="hp-body" x1="0" y1="0" x2="0" y2="1">
          <stop offset="0" stopColor="#22344f" />
          <stop offset="1" stopColor="#0f1d33" />
        </linearGradient>
        <linearGradient id="hp-edge" x1="0" y1="0" x2="1" y2="1">
          <stop offset="0" stopColor="#87CEEB" stopOpacity="0.7" />
          <stop offset="0.5" stopColor="#7c9cff" stopOpacity="0.15" />
          <stop offset="1" stopColor="#5eead4" stopOpacity="0.6" />
        </linearGradient>
        <linearGradient id="hp-print" x1="0" y1="0" x2="1" y2="1">
          <stop offset="0" stopColor="#a8dcf0" />
          <stop offset="0.55" stopColor="#87CEEB" />
          <stop offset="1" stopColor="#5b7cff" />
        </linearGradient>
        <pattern id="hp-layers" width="8" height="3" patternUnits="userSpaceOnUse">
          <rect width="8" height="1" fill="rgba(5,11,20,0.28)" />
        </pattern>
        <linearGradient id="hp-glass" x1="0" y1="0" x2="1" y2="1">
          <stop offset="0" stopColor="#ffffff" stopOpacity="0.10" />
          <stop offset="0.35" stopColor="#ffffff" stopOpacity="0.02" />
          <stop offset="0.36" stopColor="#ffffff" stopOpacity="0.07" />
          <stop offset="0.45" stopColor="#ffffff" stopOpacity="0.01" />
          <stop offset="1" stopColor="#ffffff" stopOpacity="0.04" />
        </linearGradient>
        <radialGradient id="hp-hot" cx="0.5" cy="0.5" r="0.5">
          <stop offset="0" stopColor="#ffd59a" />
          <stop offset="0.4" stopColor="#ff9f43" stopOpacity="0.8" />
          <stop offset="1" stopColor="#ff9f43" stopOpacity="0" />
        </radialGradient>
        <radialGradient id="hp-light" cx="0.5" cy="0" r="0.9">
          <stop offset="0" stopColor="#87CEEB" stopOpacity="0.28" />
          <stop offset="1" stopColor="#87CEEB" stopOpacity="0" />
        </radialGradient>
      </defs>

      <style>{`
        .hp-cover { transform-box: fill-box; transform-origin: top; animation: hp-cover ${CYCLE} linear infinite; }
        .hp-head { animation: hp-rise ${CYCLE} linear infinite; }
        .hp-sweep { animation: hp-sweep 1.1s ease-in-out infinite alternate; }
        .hp-progress { transform-box: fill-box; transform-origin: left; animation: hp-progress ${CYCLE} linear infinite; }
        .hp-print { animation: hp-print ${CYCLE} linear infinite; }
        .hp-spool { transform-box: fill-box; transform-origin: center; animation: spin 6s linear infinite; }
        .hp-spool-2 { animation-duration: 9s; animation-direction: reverse; }
        .hp-led { animation: twinkle 1.2s ease-in-out infinite alternate; }
        @keyframes hp-cover { 0% { transform: scaleY(1); } 72% { transform: scaleY(0); } 100% { transform: scaleY(0); } }
        @keyframes hp-rise { 0% { transform: translateY(0); } 72% { transform: translateY(-176px); } 84% { transform: translateY(-176px); } 92%, 100% { transform: translateY(0); } }
        @keyframes hp-sweep { from { transform: translateX(-46px); } to { transform: translateX(46px); } }
        @keyframes hp-progress { 0% { transform: scaleX(0); } 72%, 100% { transform: scaleX(1); } }
        @keyframes hp-print { 0%, 86% { opacity: 1; } 90%, 96% { opacity: 0; } 100% { opacity: 1; } }
      `}</style>

      {/* AMS (Materialstation) oben */}
      <g>
        <rect x="70" y="8" width="280" height="56" rx="12" fill="url(#hp-body)" stroke="url(#hp-edge)" strokeWidth="1.5" />
        {[0, 1, 2, 3].map((i) => {
          const cx = 112 + i * 65;
          const colors = ['#87CEEB', '#7c9cff', '#5eead4', '#f5f7fb'];
          return (
            <g key={i}>
              <circle cx={cx} cy="36" r="20" fill="#0a1628" stroke="rgba(135,206,235,0.25)" />
              <g className={`hp-spool ${i % 2 ? 'hp-spool-2' : ''}`}>
                <circle cx={cx} cy="36" r="15" fill="none" stroke={colors[i]} strokeWidth="6" strokeDasharray="6 3" opacity="0.9" />
                <circle cx={cx} cy="36" r="5" fill="#1a2a40" stroke={colors[i]} strokeWidth="1.5" />
              </g>
            </g>
          );
        })}
      </g>

      {/* Gehaeuse */}
      <rect x="30" y="72" width="360" height="388" rx="24" fill="url(#hp-body)" stroke="url(#hp-edge)" strokeWidth="2" />
      <rect x="52" y="92" width="316" height="292" rx="12" fill="#0b182b" />
      <rect x="52" y="92" width="316" height="292" rx="12" fill="url(#hp-light)" />

      {/* Fuehrungsstangen */}
      <line x1="70" y1="104" x2="70" y2="372" stroke="rgba(135,206,235,0.25)" strokeWidth="3" />
      <line x1="350" y1="104" x2="350" y2="372" stroke="rgba(135,206,235,0.25)" strokeWidth="3" />

      {/* Druckobjekt: Vase mit Schichtlinien, wird von unten freigelegt */}
      <g className="hp-print">
        <path
          d="M152,344 C150,304 178,286 174,252 C170,220 150,200 160,168 L260,168 C270,200 250,220 246,252 C242,286 270,304 268,344 Z"
          fill="url(#hp-print)"
        />
        <path
          d="M152,344 C150,304 178,286 174,252 C170,220 150,200 160,168 L260,168 C270,200 250,220 246,252 C242,286 270,304 268,344 Z"
          fill="url(#hp-layers)"
        />
        <path d="M188,170 C182,205 196,225 192,262 C188,295 172,318 176,342" fill="none" stroke="rgba(255,255,255,0.35)" strokeWidth="3" strokeLinecap="round" />
        <rect className="hp-cover" x="140" y="160" width="140" height="185" fill="#0b182b" />
        <rect className="hp-cover" x="140" y="160" width="140" height="185" fill="url(#hp-light)" />
      </g>

      {/* Druckbett */}
      <rect x="92" y="344" width="236" height="12" rx="3" fill="#2a3b55" stroke="rgba(135,206,235,0.4)" />
      <rect x="92" y="356" width="236" height="4" rx="2" fill="rgba(0,0,0,0.45)" />

      {/* Druckkopf: steigt mit jeder Schicht, faehrt hin und her */}
      <g className="hp-head">
        <rect x="64" y="304" width="292" height="8" rx="4" fill="#33496b" stroke="rgba(135,206,235,0.35)" />
        <g className="hp-sweep">
          <rect x="190" y="290" width="40" height="34" rx="6" fill="#1d2f4b" stroke="#87CEEB" strokeWidth="1.5" />
          <rect x="196" y="296" width="28" height="6" rx="2" fill="rgba(135,206,235,0.35)" />
          <path d="M204,324 L216,324 L212,338 L208,338 Z" fill="#c9d6e8" />
          <circle cx="210" cy="340" r="9" fill="url(#hp-hot)" />
        </g>
      </g>

      {/* Glastuer mit Spiegelung */}
      <rect x="52" y="92" width="316" height="292" rx="12" fill="url(#hp-glass)" stroke="rgba(135,206,235,0.18)" />

      {/* Bedienfeld mit Fortschritt */}
      <rect x="52" y="398" width="150" height="44" rx="8" fill="#0a1628" stroke="rgba(135,206,235,0.3)" />
      <text x="64" y="416" fill="#87CEEB" fontSize="11" fontFamily="Menlo, monospace" letterSpacing="1">X1C · PLA</text>
      <rect x="64" y="426" width="126" height="5" rx="2.5" fill="rgba(135,206,235,0.15)" />
      <rect className="hp-progress" x="64" y="426" width="126" height="5" rx="2.5" fill="#5eead4" />
      <circle className="hp-led" cx="350" cy="420" r="5" fill="#5eead4" />
      <circle cx="326" cy="420" r="5" fill="rgba(135,206,235,0.3)" />
    </svg>
  );
}
