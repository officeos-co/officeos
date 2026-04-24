import { Router } from 'express';
import { createConnection, saveCreds, getConnection, deleteConnection } from '../db/connection-store.js';
import { activateConnection, deactivateConnection } from '../connection-manager.js';

const router = Router();

router.post('/api/connections/start', (req, res) => {
  const { connectionId, channelType } = req.body;
  if (!connectionId || !channelType) {
    res.status(400).json({ error: 'connectionId and channelType are required' });
    return;
  }
  createConnection(connectionId, channelType);
  res.status(201).json({ id: connectionId, channelType, status: 'pending' });
});

router.post('/api/connections/:id/creds', async (req, res) => {
  const { id } = req.params;
  const conn = getConnection(id);
  if (!conn) {
    res.status(404).json({ error: 'Connection not found' });
    return;
  }

  const credsJson = req.body.credsJson ?? JSON.stringify(req.body);
  saveCreds(id, credsJson);

  try {
    await activateConnection(id, conn.channel_type, credsJson);
    res.json({ ok: true, status: 'active' });
  } catch (err) {
    res.status(500).json({ error: 'Failed to activate', detail: String(err) });
  }
});

router.delete('/api/connections/:id', async (req, res) => {
  const { id } = req.params;
  const conn = getConnection(id);
  if (!conn) {
    res.status(404).json({ error: 'Connection not found' });
    return;
  }

  await deactivateConnection(id);
  deleteConnection(id);
  res.json({ ok: true });
});

export default router;
