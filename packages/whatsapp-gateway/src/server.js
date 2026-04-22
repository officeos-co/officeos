import express from "express";
import { ConnectionManager } from "./connection-manager.js";
import { createLogger } from "./logger.js";

const log = createLogger("server");
const app = express();
app.use(express.json());

const BACKEND_URL = "http://eaos-backend-prod.default.svc.cluster.local:8000";
const PORT = 3002;

const manager = new ConnectionManager(BACKEND_URL);

// ── Health ──────────────────────────────────────────────────────────
app.get("/health", (_req, res) => {
  res.json({ ok: true, connections: manager.connectionCount() });
});

// ── Connect (start new WhatsApp session, returns QR) ────────────────
app.post("/connect", async (req, res) => {
  const { connectionId } = req.body;
  if (!connectionId) return res.status(400).json({ error: "connectionId required" });

  try {
    const result = await manager.connect(connectionId);
    res.json(result);
  } catch (err) {
    log.error({ err, connectionId }, "Failed to start connection");
    res.status(500).json({ error: err.message });
  }
});

// ── Disconnect ──────────────────────────────────────────────────────
app.post("/disconnect/:id", (req, res) => {
  manager.disconnect(req.params.id);
  res.json({ ok: true });
});

// ── Status ──────────────────────────────────────────────────────────
app.get("/status/:id", (req, res) => {
  const status = manager.getStatus(req.params.id);
  res.json(status);
});

// ── Send message ────────────────────────────────────────────────────
app.post("/send", async (req, res) => {
  const { connectionId, jid, text } = req.body;
  if (!connectionId || !jid || !text) {
    return res.status(400).json({ error: "connectionId, jid, text required" });
  }

  try {
    await manager.sendMessage(connectionId, jid, text);
    res.json({ ok: true });
  } catch (err) {
    log.error({ err, connectionId, jid }, "Failed to send message");
    res.status(500).json({ error: err.message });
  }
});

// ── Start ───────────────────────────────────────────────────────────
app.listen(PORT, () => {
  log.info({ port: PORT, backendUrl: BACKEND_URL }, "WhatsApp gateway started");
});
