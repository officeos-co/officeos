import Database from 'better-sqlite3';
import { runMigrations } from './migrations.js';

let db: Database.Database;

export function initDb(dbPath: string): Database.Database {
  db = new Database(dbPath);
  db.pragma('journal_mode = WAL');
  db.pragma('foreign_keys = ON');
  runMigrations(db);
  return db;
}

export function getDb(): Database.Database {
  return db;
}
