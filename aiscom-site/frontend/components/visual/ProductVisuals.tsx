// CSS-Mockups fuer die Produktkarten.

const S_BARS = [0.35, 0.55, 0.8, 1, 0.7, 0.9, 0.5, 0.75, 0.6, 0.4, 0.3, 0.2];

export function RadioVisual() {
  return (
    <div className="relative h-48 rounded-2xl bg-gradient-to-br from-[#1d2c45] to-[#0c1729] border border-white/5 p-5 overflow-hidden">
      <div className="absolute inset-x-0 top-0 h-px bg-gradient-to-r from-transparent via-accent/60 to-transparent" />
      <div className="flex h-full gap-5">
        <div className="flex-1 flex flex-col gap-3">
          <div className="lcd rounded-lg px-4 py-3 flex-1 flex flex-col justify-between">
            <div className="flex justify-between text-[10px] lcd-text tracking-widest">
              <span>VFO-A</span>
              <span>USB · FT8</span>
            </div>
            <div className="lcd-text text-3xl md:text-4xl font-bold tracking-wider">14.074.00</div>
            <div className="flex items-end gap-[3px] h-6">
              {S_BARS.map((h, i) => (
                <span
                  key={i}
                  className="s-bar w-2 rounded-sm"
                  style={{
                    height: `${h * 100}%`,
                    background: i > 8 ? '#ff8a8a' : '#5eead4',
                    animationDelay: `${i * 0.12}s`,
                    boxShadow: '0 0 6px rgba(94,234,212,0.6)',
                  }}
                />
              ))}
            </div>
          </div>
          <div className="flex gap-2">
            {['MODE', 'BAND', 'CAT', 'LOG'].map((label) => (
              <span key={label} className="flex-1 rounded-md bg-[#0a1628] border border-accent/20 py-1 text-center text-[10px] tracking-widest text-gray-400">
                {label}
              </span>
            ))}
          </div>
        </div>
        <div className="flex flex-col items-center justify-center gap-3">
          <div className="knob relative h-24 w-24 rounded-full">
            <span className="absolute left-1/2 top-2 h-4 w-1.5 -translate-x-1/2 rounded-full bg-accent shadow-[0_0_8px_#87CEEB]" />
          </div>
          <span className="flex items-center gap-2 text-[10px] tracking-widest text-gray-400">
            <span className="pulse-dot h-2 w-2 rounded-full bg-[#5eead4]" /> TX/RX
          </span>
        </div>
      </div>
    </div>
  );
}

export function CameraVisual() {
  return (
    <div className="relative h-48 rounded-2xl bg-gradient-to-br from-[#1d2c45] to-[#0c1729] border border-white/5 p-5 overflow-hidden">
      <div className="absolute inset-x-0 top-0 h-px bg-gradient-to-r from-transparent via-[#7c9cff]/60 to-transparent" />
      <div className="flex h-full items-center gap-6">
        <div className="relative h-32 w-32 shrink-0">
          <div className="lens absolute inset-0 rounded-full" />
          <div className="lens-sheen absolute inset-2 rounded-full mix-blend-screen" />
          <span className="absolute -right-1 -top-1 h-3 w-3 rounded-full bg-[#ff5f57] shadow-[0_0_10px_#ff5f57] pulse-dot" />
        </div>
        <div className="flex-1 space-y-3">
          {[0, 1, 2].map((i) => (
            <div key={i} className="flex items-center gap-3">
              <div className="h-9 w-12 rounded-md bg-gradient-to-br from-accent/40 to-[#7c9cff]/30 border border-white/10" />
              <div className="flex-1">
                <div className="h-1.5 rounded-full bg-white/10 overflow-hidden">
                  <div className="upload-bar h-full rounded-full bg-gradient-to-r from-accent to-[#5eead4]" style={{ animationDelay: `${i * 0.6}s` }} />
                </div>
              </div>
              <svg className="upload-arrow h-4 w-4 text-[#5eead4]" style={{ animationDelay: `${i * 0.5}s` }} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5">
                <path strokeLinecap="round" strokeLinejoin="round" d="M12 19V5m0 0l-6 6m6-6l6 6" />
              </svg>
            </div>
          ))}
        </div>
      </div>
    </div>
  );
}
