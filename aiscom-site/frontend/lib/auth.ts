import { cookies } from 'next/headers';
import bcrypt from 'bcryptjs';
import { v4 as uuidv4 } from 'uuid';
import { getDb } from './db';

const SESSION_DURATION = 24 * 60 * 60 * 1000; // 24 hours

export async function login(username: string, password: string): Promise<string | null> {
  const db = getDb();
  const user = db.prepare('SELECT * FROM admin WHERE username = ?').get(username) as {
    id: number;
    username: string;
    password_hash: string;
  } | undefined;

  if (!user) {
    return null;
  }

  const isValid = await bcrypt.compare(password, user.password_hash);
  if (!isValid) {
    return null;
  }

  // Create session
  const sessionId = uuidv4();
  const expiresAt = new Date(Date.now() + SESSION_DURATION);

  db.prepare('INSERT INTO sessions (id, user_id, expires_at) VALUES (?, ?, ?)').run(
    sessionId,
    user.id,
    expiresAt.toISOString()
  );

  return sessionId;
}

export async function logout(sessionId: string): Promise<void> {
  const db = getDb();
  db.prepare('DELETE FROM sessions WHERE id = ?').run(sessionId);
}

export async function validateSession(sessionId: string): Promise<boolean> {
  if (!sessionId) return false;

  const db = getDb();
  const session = db.prepare('SELECT * FROM sessions WHERE id = ? AND expires_at > datetime("now")').get(sessionId) as {
    id: string;
    user_id: number;
    expires_at: string;
  } | undefined;

  return !!session;
}

export async function getSession(): Promise<string | null> {
  const cookieStore = await cookies();
  return cookieStore.get('session')?.value || null;
}

export async function isAuthenticated(): Promise<boolean> {
  const sessionId = await getSession();
  if (!sessionId) return false;
  return validateSession(sessionId);
}

export async function changePassword(userId: number, newPassword: string): Promise<boolean> {
  const db = getDb();
  const passwordHash = await bcrypt.hash(newPassword, 10);
  const result = db.prepare('UPDATE admin SET password_hash = ? WHERE id = ?').run(passwordHash, userId);
  return result.changes > 0;
}

export function cleanupExpiredSessions(): void {
  const db = getDb();
  db.prepare('DELETE FROM sessions WHERE expires_at < datetime("now")').run();
}
