import type Database from 'better-sqlite3';

interface Migration {
  version: number;
  description: string;
  up: string;
}

const migrations: Migration[] = [
  {
    version: 1,
    description: 'Initial connections and credentials tables',
    up: `
      CREATE TABLE IF NOT EXISTS connections (
        id TEXT PRIMARY KEY,
        channel_type TEXT NOT NULL,
        platform_id TEXT,
        thread_id TEXT,
        status TEXT NOT NULL DEFAULT 'pending',
        created_at TEXT NOT NULL DEFAULT (datetime('now'))
      );
      CREATE TABLE IF NOT EXISTS credentials (
        connection_id TEXT PRIMARY KEY REFERENCES connections(id) ON DELETE CASCADE,
        creds_json TEXT NOT NULL,
        updated_at TEXT NOT NULL DEFAULT (datetime('now'))
      );
    `,
  },
];

export function runMigrations(db: Database.Database): void {
  db.exec(`
    CREATE TABLE IF NOT EXISTS schema_migrations (
      version INTEGER PRIMARY KEY,
      description TEXT NOT NULL,
      applied_at TEXT NOT NULL DEFAULT (datetime('now'))
    );
  `);

  const applied = new Set(
    (db.prepare('SELECT version FROM schema_migrations').all() as { version: number }[]).map((r) => r.version),
  );

  const insert = db.prepare('INSERT INTO schema_migrations (version, description) VALUES (?, ?)');

  for (const m of migrations) {
    if (applied.has(m.version)) continue;
    db.exec(m.up);
    insert.run(m.version, m.description);
  }
}
