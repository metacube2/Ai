// Kleines Editor-Fenster mit Zeilen, die nacheinander "getippt" werden.

type Token = [string, string];

const LINES: Token[][] = [
  [['text-gray-500', '// AISCOM · neuer Auftrag']],
  [['text-[#7c9cff]', 'var '], ['text-white', 'druck = '], ['text-[#7c9cff]', 'new '], ['text-[#5eead4]', 'Druckauftrag'], ['text-white', '('], ['text-[#ffd59a]', '"vase.stl"'], ['text-white', ')']],
  [['text-white', '    .'], ['text-[#87CEEB]', 'Material'], ['text-white', '('], ['text-[#ffd59a]', '"PLA"'], ['text-white', ')']],
  [['text-white', '    .'], ['text-[#87CEEB]', 'Schicht'], ['text-white', '('], ['text-[#f0a6ff]', '0.2'], ['text-white', ');']],
  [['text-[#7c9cff]', 'await '], ['text-white', 'x1c.'], ['text-[#87CEEB]', 'StartAsync'], ['text-white', '(druck);']],
  [['text-gray-500', '* SAP: Auftraege lesen']],
  [['text-[#7c9cff]', 'SELECT '], ['text-white', '* '], ['text-[#7c9cff]', 'FROM '], ['text-[#5eead4]', 'vbak'], ['text-white', ' INTO TABLE '], ['text-[#87CEEB]', '@DATA'], ['text-white', '(lt).']],
];

export default function CodeWindow() {
  return (
    <div className="glass rounded-2xl overflow-hidden text-left w-[min(100%,24rem)]">
      <div className="flex items-center gap-2 px-4 py-3 border-b border-white/5">
        <span className="h-3 w-3 rounded-full bg-[#ff5f57]" />
        <span className="h-3 w-3 rounded-full bg-[#febc2e]" />
        <span className="h-3 w-3 rounded-full bg-[#28c840]" />
        <span className="ml-3 text-xs text-gray-400 font-mono">Auftrag.cs</span>
      </div>
      <pre className="px-4 py-4 text-[12px] leading-6 font-mono overflow-hidden">
        {LINES.map((line, i) => (
          <div key={i} className="code-line" style={{ animationDelay: `${0.6 + i * 0.45}s` }}>
            <span className="inline-block w-5 text-gray-600 select-none">{i + 1}</span>
            {line.map(([cls, text], j) => (
              <span key={j} className={cls}>
                {text}
              </span>
            ))}
            {i === LINES.length - 1 && <span className="caret" />}
          </div>
        ))}
      </pre>
    </div>
  );
}
