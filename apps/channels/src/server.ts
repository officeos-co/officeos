import express from 'express';
import { PORT, BACKEND_URL } from './config.js';
import { reloadFromBackend, activeAdapterCount, shutdownAll } from './connection-manager.js';
import { log } from './log.js';
import sendRoutes from './routes/send.js';
import reloadRoutes from './routes/reload.js';
import webhookRoutes from './routes/webhooks.js';

// Import channel adapter registrations
import './channels/index.js';

async function main() {
  const app = express();
  app.use(express.json());

  app.use(sendRoutes);
  app.use(reloadRoutes);
  app.use(webhookRoutes);

  app.get('/health', (_req, res) => {
    res.json({ status: 'ok', activeAdapters: activeAdapterCount() });
  });

  // Load active connections from backend on startup
  log.info('Loading channel configuration from backend...', { backendUrl: BACKEND_URL });
  await reloadFromBackend();

  const server = app.listen(PORT, () => {
    log.info('Channel sidecar started', { port: PORT, backendUrl: BACKEND_URL, adapters: activeAdapterCount() });
  });

  const shutdown = async () => {
    log.info('Shutting down...');
    await shutdownAll();
    server.close(() => process.exit(0));
  };
  process.on('SIGTERM', () => shutdown());
  process.on('SIGINT', () => shutdown());
}

main().catch((err) => {
  log.error('Fatal startup error', { err });
  process.exit(1);
});
