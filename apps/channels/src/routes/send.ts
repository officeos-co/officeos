import { Router } from 'express';
import { getAdapter } from '../connection-manager.js';
import { log } from '../log.js';

const router = Router();

router.post('/send', async (req, res) => {
  const { channelType, platformId, threadId, message } = req.body;

  if (!channelType || !platformId || !message) {
    res.status(400).json({ error: 'channelType, platformId, and message are required' });
    return;
  }

  const adapter = getAdapter(channelType);
  if (!adapter) {
    res.status(404).json({ error: `No active adapter for channel type: ${channelType}` });
    return;
  }

  try {
    const messageId = await adapter.deliver(platformId, threadId ?? null, {
      kind: message.kind || 'chat',
      content: message.content ?? message,
    });
    res.json({ ok: true, messageId });
  } catch (err) {
    log.error('Send failed', { channelType, platformId, err });
    res.status(502).json({ error: 'Delivery failed', detail: String(err) });
  }
});

export default router;
