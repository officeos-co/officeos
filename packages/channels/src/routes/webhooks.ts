import { Router } from 'express';

const router = Router();

router.post('/api/webhooks/:channelType', async (req, res) => {
  // Placeholder: route webhook payloads to the appropriate adapter's webhook handler.
  // Individual adapters will register their webhook handling logic here.
  const { channelType } = req.params;
  res.status(200).json({ received: true, channelType });
});

export default router;
