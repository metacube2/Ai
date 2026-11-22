import Database from 'better-sqlite3';
import path from 'path';
import bcrypt from 'bcryptjs';

const DB_PATH = path.join(process.cwd(), '..', 'database', 'aiscom.db');

let db: Database.Database | null = null;

export function getDb(): Database.Database {
  if (!db) {
    db = new Database(DB_PATH);
    db.pragma('journal_mode = WAL');
    initializeDatabase();
  }
  return db;
}

function initializeDatabase() {
  const database = db!;

  // Admin User
  database.exec(`
    CREATE TABLE IF NOT EXISTS admin (
      id INTEGER PRIMARY KEY,
      username TEXT UNIQUE,
      password_hash TEXT,
      created_at DATETIME DEFAULT CURRENT_TIMESTAMP
    )
  `);

  // Contact Messages
  database.exec(`
    CREATE TABLE IF NOT EXISTS contact_messages (
      id INTEGER PRIMARY KEY,
      name TEXT,
      email TEXT,
      message TEXT,
      read INTEGER DEFAULT 0,
      created_at DATETIME DEFAULT CURRENT_TIMESTAMP
    )
  `);

  // 3D Uploads
  database.exec(`
    CREATE TABLE IF NOT EXISTS uploads (
      id INTEGER PRIMARY KEY,
      filename TEXT,
      original_name TEXT,
      file_size INTEGER,
      description TEXT,
      email TEXT,
      status TEXT DEFAULT 'new',
      created_at DATETIME DEFAULT CURRENT_TIMESTAMP
    )
  `);

  // Portfolio
  database.exec(`
    CREATE TABLE IF NOT EXISTS portfolio (
      id INTEGER PRIMARY KEY,
      title_de TEXT,
      title_en TEXT,
      title_it TEXT,
      description_de TEXT,
      description_en TEXT,
      description_it TEXT,
      category TEXT,
      image_path TEXT,
      active INTEGER DEFAULT 1,
      sort_order INTEGER,
      created_at DATETIME DEFAULT CURRENT_TIMESTAMP
    )
  `);

  // CMS Content
  database.exec(`
    CREATE TABLE IF NOT EXISTS cms_content (
      id INTEGER PRIMARY KEY,
      page TEXT,
      section TEXT,
      content_de TEXT,
      content_en TEXT,
      content_it TEXT,
      updated_at DATETIME DEFAULT CURRENT_TIMESTAMP,
      UNIQUE(page, section)
    )
  `);

  // Sessions for admin auth
  database.exec(`
    CREATE TABLE IF NOT EXISTS sessions (
      id TEXT PRIMARY KEY,
      user_id INTEGER,
      expires_at DATETIME,
      created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
      FOREIGN KEY (user_id) REFERENCES admin(id)
    )
  `);

  // Create default admin user if not exists
  const adminExists = database.prepare('SELECT id FROM admin WHERE username = ?').get('admin');
  // Startpasswort nur aus der Umgebung; die bestehende DB hat ihren Admin schon.
  const initialPassword = process.env.AISCOM_ADMIN_INITIAL_PASSWORD;
  if (!adminExists && initialPassword) {
    const passwordHash = bcrypt.hashSync(initialPassword, 10);
    database.prepare('INSERT INTO admin (username, password_hash) VALUES (?, ?)').run('admin', passwordHash);
  }
}

// Helper functions for database operations
export function createContactMessage(name: string, email: string, message: string) {
  const db = getDb();
  const stmt = db.prepare('INSERT INTO contact_messages (name, email, message) VALUES (?, ?, ?)');
  return stmt.run(name, email, message);
}

export function getContactMessages(unreadOnly = false) {
  const db = getDb();
  const query = unreadOnly
    ? 'SELECT * FROM contact_messages WHERE read = 0 ORDER BY created_at DESC'
    : 'SELECT * FROM contact_messages ORDER BY created_at DESC';
  return db.prepare(query).all();
}

export function markMessageAsRead(id: number) {
  const db = getDb();
  return db.prepare('UPDATE contact_messages SET read = 1 WHERE id = ?').run(id);
}

export function deleteMessage(id: number) {
  const db = getDb();
  return db.prepare('DELETE FROM contact_messages WHERE id = ?').run(id);
}

export function createUpload(filename: string, originalName: string, fileSize: number, description: string, email: string) {
  const db = getDb();
  const stmt = db.prepare('INSERT INTO uploads (filename, original_name, file_size, description, email) VALUES (?, ?, ?, ?, ?)');
  return stmt.run(filename, originalName, fileSize, description, email);
}

export function getUploads() {
  const db = getDb();
  return db.prepare('SELECT * FROM uploads ORDER BY created_at DESC').all();
}

export function updateUploadStatus(id: number, status: string) {
  const db = getDb();
  return db.prepare('UPDATE uploads SET status = ? WHERE id = ?').run(status, id);
}

export function deleteUpload(id: number) {
  const db = getDb();
  return db.prepare('DELETE FROM uploads WHERE id = ?').run(id);
}

export function getPortfolioItems(activeOnly = true) {
  const db = getDb();
  const query = activeOnly
    ? 'SELECT * FROM portfolio WHERE active = 1 ORDER BY sort_order ASC, created_at DESC'
    : 'SELECT * FROM portfolio ORDER BY sort_order ASC, created_at DESC';
  return db.prepare(query).all();
}

export function createPortfolioItem(data: {
  title_de: string;
  title_en: string;
  title_it: string;
  description_de: string;
  description_en: string;
  description_it: string;
  category: string;
  image_path: string;
  sort_order: number;
}) {
  const db = getDb();
  const stmt = db.prepare(`
    INSERT INTO portfolio (title_de, title_en, title_it, description_de, description_en, description_it, category, image_path, sort_order)
    VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?)
  `);
  return stmt.run(
    data.title_de,
    data.title_en,
    data.title_it,
    data.description_de,
    data.description_en,
    data.description_it,
    data.category,
    data.image_path,
    data.sort_order
  );
}

export function updatePortfolioItem(id: number, data: Partial<{
  title_de: string;
  title_en: string;
  title_it: string;
  description_de: string;
  description_en: string;
  description_it: string;
  category: string;
  image_path: string;
  active: number;
  sort_order: number;
}>) {
  const db = getDb();
  const fields = Object.keys(data).map(key => `${key} = ?`).join(', ');
  const values = Object.values(data);
  const stmt = db.prepare(`UPDATE portfolio SET ${fields} WHERE id = ?`);
  return stmt.run(...values, id);
}

export function deletePortfolioItem(id: number) {
  const db = getDb();
  return db.prepare('DELETE FROM portfolio WHERE id = ?').run(id);
}

export function getCmsContent(page: string, section: string) {
  const db = getDb();
  return db.prepare('SELECT * FROM cms_content WHERE page = ? AND section = ?').get(page, section);
}

export function getAllCmsContent() {
  const db = getDb();
  return db.prepare('SELECT * FROM cms_content ORDER BY page, section').all();
}

export function upsertCmsContent(page: string, section: string, content_de: string, content_en: string, content_it: string) {
  const db = getDb();
  const stmt = db.prepare(`
    INSERT INTO cms_content (page, section, content_de, content_en, content_it, updated_at)
    VALUES (?, ?, ?, ?, ?, CURRENT_TIMESTAMP)
    ON CONFLICT(page, section) DO UPDATE SET
      content_de = excluded.content_de,
      content_en = excluded.content_en,
      content_it = excluded.content_it,
      updated_at = CURRENT_TIMESTAMP
  `);
  return stmt.run(page, section, content_de, content_en, content_it);
}
