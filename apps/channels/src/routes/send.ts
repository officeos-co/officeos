import { Router } from 'express';
import { getActiveConnection } from '../connection-manager.js';

const router = Router();

router.post('/api/send', async (req, res) => {
  const { connectionId, text, platformId, threadId } = req.body;

  if (!connectionId || !text || !platformId) {
    res.status(400).json({ error: 'connectionId, text, and platformId are required' });
    return;
  }

  const conn = getActiveConnection(connectionId);
  if (!conn) {
    res.status(404).json({ error: 'Connection not found or not active' });
    return;
  }

  try {
    const messageId = await conn.adapter.deliver(platformId, threadId ?? null, {
      kind: 'chat',
      content: text,
    });
    res.json({ ok: true, messageId });
  } catch (err) {
    res.status(502).json({ error: 'Delivery failed', detail: String(err) });
  }
});

export default router;
