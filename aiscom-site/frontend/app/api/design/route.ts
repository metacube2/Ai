import { NextResponse } from 'next/server';
import { isAuthenticated } from '@/lib/auth';
import { aktuellesDesign, designSetzen, Design, DESIGNS } from '@/lib/design';

// Nur fuer den Admin. Der Apache-Proxy reicht /api/design nicht nach aussen
// weiter (Allowlist: nur /api/contact und /api/upload), Aufruf per Tunnel.
export const dynamic = 'force-dynamic';

export async function GET() {
  if (!(await isAuthenticated())) {
    return NextResponse.json({ error: 'nicht_angemeldet' }, { status: 401 });
  }
  return NextResponse.json({ design: aktuellesDesign(), designs: DESIGNS });
}

export async function POST(request: Request) {
  if (!(await isAuthenticated())) {
    return NextResponse.json({ error: 'nicht_angemeldet' }, { status: 401 });
  }
  try {
    const { design } = await request.json();
    if (!DESIGNS.includes(design)) {
      return NextResponse.json({ error: 'unbekanntes_design' }, { status: 400 });
    }
    designSetzen(design as Design);
    return NextResponse.json({ design: aktuellesDesign() });
  } catch (error) {
    console.error('Design speichern:', error);
    return NextResponse.json({ error: 'speichern_fehlgeschlagen' }, { status: 500 });
  }
}
