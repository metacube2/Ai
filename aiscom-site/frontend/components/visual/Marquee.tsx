const ITEMS = [
  'C# / .NET',
  'SAP ABAP',
  'Next.js',
  'React',
  'TypeScript',
  'Swift / macOS',
  'Python',
  'Tailwind CSS',
  'SQLite',
  'Bambu Lab X1C',
  'PLA',
  'ABS',
];

// Endloses Laufband; die Liste steht doppelt drin, damit der Uebergang nahtlos ist.
export default function Marquee() {
  const row = [...ITEMS, ...ITEMS];
  return (
    <div className="marquee overflow-hidden py-6" aria-label={ITEMS.join(', ')}>
      <div className="marquee-track" aria-hidden="true">
        {row.map((item, index) => (
          <span
            key={index}
            className="glass rounded-full px-6 py-2.5 text-sm font-medium text-gray-200 whitespace-nowrap"
          >
            <span className="mr-2 text-accent">&#9670;</span>
            {item}
          </span>
        ))}
      </div>
    </div>
  );
}
