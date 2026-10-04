// Weicher Uebergang zwischen zwei Abschnitten.
export default function Wave({ from, to, flip = false }: { from: string; to: string; flip?: boolean }) {
  return (
    <div aria-hidden="true" style={{ background: from, lineHeight: 0 }}>
      <svg
        viewBox="0 0 1440 90"
        preserveAspectRatio="none"
        className="block w-full h-[60px] md:h-[90px]"
        style={flip ? { transform: 'scaleX(-1)' } : undefined}
      >
        <path
          d="M0,40 C240,95 480,0 720,30 C960,60 1200,10 1440,45 L1440,90 L0,90 Z"
          fill={to}
        />
        <path
          d="M0,40 C240,95 480,0 720,30 C960,60 1200,10 1440,45"
          fill="none"
          stroke="rgba(135,206,235,0.25)"
          strokeWidth="1.5"
        />
      </svg>
    </div>
  );
}
