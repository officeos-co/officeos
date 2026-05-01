import { Router } from 'express';
import { reloadFromBackend } from '../connection-manager.js';
import { log } from '../log.js';

const router = Router();

router.post('/reload', async (_req, res) => {
  try {
    await reloadFromBackend();
    res.json({ ok: true });
  } catch (err) {
    log.error('Reload failed', { err });
    res.status(500).json({ error: 'Reload failed', detail: String(err) });
  }
});

export default router;
