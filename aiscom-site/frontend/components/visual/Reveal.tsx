'use client';

import { ReactNode, useEffect, useRef, useState } from 'react';

interface RevealProps {
  children: ReactNode;
  delay?: number;
  className?: string;
}

// Blendet Inhalte beim Hineinscrollen ein. Ohne JavaScript bleibt alles sichtbar
// (die Ausblendung greift nur unter html.js, siehe globals.css).
export default function Reveal({ children, delay = 0, className = '' }: RevealProps) {
  const ref = useRef<HTMLDivElement>(null);
  const [visible, setVisible] = useState(false);

  useEffect(() => {
    const node = ref.current;
    if (!node) return;
    const observer = new IntersectionObserver(
      ([entry]) => {
        if (entry.isIntersecting) {
          setVisible(true);
          observer.disconnect();
        }
      },
      { threshold: 0.15 }
    );
    observer.observe(node);
    return () => observer.disconnect();
  }, []);

  return (
    <div
      ref={ref}
      className={`reveal ${visible ? 'is-visible' : ''} ${className}`}
      style={{ ['--delay' as string]: `${delay}ms` }}
    >
      {children}
    </div>
  );
}
