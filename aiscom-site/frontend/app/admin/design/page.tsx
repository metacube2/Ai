'use client';

import { useEffect, useState } from 'react';
import { useRouter } from 'next/navigation';
import Link from 'next/link';
import Card from '@/components/ui/Card';
import Button from '@/components/ui/Button';

// Versteckte Verwaltungsseite: Design der oeffentlichen Seite umschalten.
// Nicht verlinkt ausser im Admin-Dashboard; /admin ist nur per Tunnel erreichbar.
const OPTIONEN = [
  {
    wert: 'turbo',
    titel: 'Turbo Vision',
    text: 'DOS-Oberfläche wie Turbo Pascal 7: blauer Desktop, Menüleiste, Fenster mit Doppelrahmen, Statuszeile mit F-Tasten.',
  },
  {
    wert: 'klassisch',
    titel: 'Klassisch',
    text: 'Bisheriges Design mit Polarlicht, animiertem 3D-Drucker und Glaskarten.',
  },
];

export default function DesignVerwaltung() {
  const router = useRouter();
  const [design, setDesign] = useState<string | null>(null);
  const [gewaehlt, setGewaehlt] = useState<string>('turbo');
  const [meldung, setMeldung] = useState('');
  const [laeuft, setLaeuft] = useState(false);

  useEffect(() => {
    fetch('/api/design')
      .then(async (r) => {
        if (r.status === 401) {
          router.push('/admin');
          return;
        }
        const d = await r.json();
        setDesign(d.design);
        setGewaehlt(d.design);
      })
      .catch(() => setMeldung('Design konnte nicht geladen werden.'));
  }, [router]);

  const speichern = async () => {
    setLaeuft(true);
    setMeldung('');
    try {
      const r = await fetch('/api/design', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ design: gewaehlt }),
      });
      const d = await r.json();
      if (!r.ok) throw new Error(d.error || 'Fehler');
      setDesign(d.design);
      setMeldung(`Gespeichert. Die Seite zeigt ab sofort „${OPTIONEN.find((o) => o.wert === d.design)?.titel}“.`);
    } catch (e) {
      setMeldung(`Speichern fehlgeschlagen: ${(e as Error).message}`);
    } finally {
      setLaeuft(false);
    }
  };

  return (
    <div className="min-h-screen bg-primary p-6">
      <div className="max-w-2xl mx-auto">
        <div className="flex items-center justify-between mb-8">
          <h1 className="text-2xl font-bold text-white">Design der Webseite</h1>
          <Link href="/admin/dashboard" className="text-accent text-sm">← Dashboard</Link>
        </div>

        <Card>
          <p className="text-gray-300 mb-6">
            Aktiv: <span className="text-accent font-semibold">{design ? OPTIONEN.find((o) => o.wert === design)?.titel : '…'}</span>.
            Die Umstellung wirkt sofort für alle Besucher, ohne Neubau.
          </p>
          <div className="space-y-4">
            {OPTIONEN.map((o) => (
              <label
                key={o.wert}
                className={`block cursor-pointer rounded-xl border p-4 ${gewaehlt === o.wert ? 'border-accent bg-accent/10' : 'border-white/10'}`}
              >
                <input
                  type="radio"
                  name="design"
                  value={o.wert}
                  checked={gewaehlt === o.wert}
                  onChange={() => setGewaehlt(o.wert)}
                  className="mr-3"
                />
                <span className="text-white font-semibold">{o.titel}</span>
                <p className="text-gray-400 text-sm mt-1 ml-7">{o.text}</p>
              </label>
            ))}
          </div>
          <div className="mt-6 flex items-center gap-4">
            <Button onClick={speichern} isLoading={laeuft} disabled={gewaehlt === design}>
              Speichern
            </Button>
            <a href="/de" target="_blank" rel="noopener noreferrer" className="text-accent text-sm">Seite ansehen ↗</a>
          </div>
          {meldung && <p className="mt-4 text-sm text-gray-300">{meldung}</p>}
        </Card>
      </div>
    </div>
  );
}
