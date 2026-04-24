import { mkdirSync } from 'fs';
import { join } from 'path';
import express from 'express';
import { PORT, BACKEND_URL, DATA_DIR } from './config.js';
import { initDb } from './db/connection.js';
import { listActive } from './db/connection-store.js';
import { activateAll, activeConnectionCount } from './connection-manager.js';
import { log } from './log.js';
import sendRoutes from './routes/send.js';
import connectionRoutes from './routes/connections.js';
import webhookRoutes from './routes/webhooks.js';

async function main() {
  mkdirSync(DATA_DIR, { recursive: true });
  const dbPath = join(DATA_DIR, 'channels.db');
  initDb(dbPath);
  log.info('Database initialized', { dbPath });

  const activeRows = listActive() as Array<{ id: string; channel_type: string; creds_json: string }>;
  await activateAll(activeRows);
  log.info('Connections restored', { count: activeRows.length });

  const app = express();
  app.use(express.json());

  app.use(sendRoutes);
  app.use(connectionRoutes);
  app.use(webhookRoutes);

  app.get('/health', (_req, res) => {
    res.json({ status: 'ok', activeConnections: activeConnectionCount() });
  });

  const server = app.listen(PORT, () => {
    log.info('Channel service started', { port: PORT, backendUrl: BACKEND_URL });
  });

  const shutdown = () => {
    log.info('Shutting down...');
    server.close(() => process.exit(0));
  };
  process.on('SIGTERM', shutdown);
  process.on('SIGINT', shutdown);
}

main().catch((err) => {
  log.error('Fatal startup error', { err });
  process.exit(1);
});
