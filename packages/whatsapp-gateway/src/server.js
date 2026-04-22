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

// ── Startup: restore all known connections from backend ─────────────
async function restoreConnections() {
  try {
    const res = await fetch(`${BACKEND_URL}/api/internal/channel/connections`);
    if (!res.ok) {
      log.warn({ status: res.status }, "Failed to fetch active connections from backend");
      return;
    }

    const { connections } = await res.json();
    const whatsappConnections = connections.filter((c) => c.channelType === "whatsapp");

    log.info({ count: whatsappConnections.length }, "Restoring WhatsApp connections from backend");

    for (const conn of whatsappConnections) {
      try {
        await manager.connect(conn.id);
        log.info({ connectionId: conn.id }, "Connection restored");
      } catch (err) {
        log.error({ err, connectionId: conn.id }, "Failed to restore connection");
      }
    }
  } catch (err) {
    log.error({ err }, "Failed to restore connections on startup");
  }
}

// ── Start ───────────────────────────────────────────────────────────
app.listen(PORT, async () => {
  log.info({ port: PORT, backendUrl: BACKEND_URL }, "WhatsApp gateway started");

  // Wait a few seconds for the backend to be ready, then restore connections
  setTimeout(() => restoreConnections(), 5000);
});
