import { NextResponse } from 'next/server';
import { logout, getSession } from '@/lib/auth';
import { cookies } from 'next/headers';

export async function POST() {
  try {
    const sessionId = await getSession();

    if (sessionId) {
      await logout(sessionId);
    }

    // Clear session cookie
    const cookieStore = await cookies();
    cookieStore.delete('session');

    return NextResponse.json({ success: true });
  } catch (error) {
    console.error('Logout error:', error);
    return NextResponse.json(
      { error: 'Logout failed' },
      { status: 500 }
    );
  }
}
