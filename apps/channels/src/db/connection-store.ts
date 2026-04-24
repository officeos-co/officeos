import { getDb } from './connection.js';

export function createConnection(id: string, channelType: string) {
  const db = getDb();
  db.prepare('INSERT INTO connections (id, channel_type) VALUES (?, ?)').run(id, channelType);
}

export function saveCreds(connectionId: string, credsJson: string) {
  const db = getDb();
  db.prepare(`
    INSERT INTO credentials (connection_id, creds_json, updated_at)
    VALUES (?, ?, datetime('now'))
    ON CONFLICT(connection_id) DO UPDATE SET creds_json = excluded.creds_json, updated_at = excluded.updated_at
  `).run(connectionId, credsJson);
  db.prepare("UPDATE connections SET status = 'active' WHERE id = ?").run(connectionId);
}

export function getCreds(connectionId: string): string | null {
  const db = getDb();
  const row = db.prepare('SELECT creds_json FROM credentials WHERE connection_id = ?').get(connectionId) as { creds_json: string } | undefined;
  return row?.creds_json ?? null;
}

export function getConnection(connectionId: string) {
  const db = getDb();
  return db.prepare('SELECT * FROM connections WHERE id = ?').get(connectionId) as Record<string, string> | undefined;
}

export function deleteConnection(connectionId: string) {
  const db = getDb();
  db.prepare('DELETE FROM connections WHERE id = ?').run(connectionId);
}

export function listByType(channelType: string) {
  const db = getDb();
  return db.prepare('SELECT * FROM connections WHERE channel_type = ?').all(channelType) as Record<string, string>[];
}

export function listActive() {
  const db = getDb();
  return db.prepare(`
    SELECT c.*, cr.creds_json FROM connections c
    JOIN credentials cr ON cr.connection_id = c.id
    WHERE c.status = 'active'
  `).all() as (Record<string, string>)[];
}
