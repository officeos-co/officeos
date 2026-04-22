import makeWASocket, {
  useMultiFileAuthState,
  DisconnectReason,
  makeCacheableSignalKeyStore,
  fetchLatestBaileysVersion,
} from "@whiskeysockets/baileys";
import { Boom } from "@hapi/boom";
import * as fs from "fs/promises";
import * as path from "path";
import * as os from "os";
import { createLogger } from "./logger.js";

const log = createLogger("connection-manager");

/**
 * Manages multiple WhatsApp Web connections via Baileys.
 * Each connection has its own socket, auth state, and lifecycle.
 */
export class ConnectionManager {
  /** @type {Map<string, WhatsAppConnection>} */
  #connections = new Map();
  #backendUrl;

  constructor(backendUrl) {
    this.#backendUrl = backendUrl;
  }

  connectionCount() {
    return this.#connections.size;
  }

  /**
   * Start a new WhatsApp connection. Returns immediately with status.
   * QR code will be available via getStatus() once generated.
   */
  async connect(connectionId) {
    if (this.#connections.has(connectionId)) {
      const existing = this.#connections.get(connectionId);
      if (existing.status === "open") {
        return { status: "open", qrData: null };
      }
      // Clean up stale connection
      this.disconnect(connectionId);
    }

    const conn = new WhatsAppConnection(connectionId, this.#backendUrl);
    this.#connections.set(connectionId, conn);

    // Start connection in background — don't await
    conn.start().catch((err) => {
      log.error({ err, connectionId }, "Connection startup failed");
    });

    // Wait briefly for QR to appear
    const qr = await conn.waitForQr(10_000);
    return { status: conn.status, qrData: qr };
  }

  disconnect(connectionId) {
    const conn = this.#connections.get(connectionId);
    if (conn) {
      conn.close();
      this.#connections.delete(connectionId);
    }
  }

  getStatus(connectionId) {
    const conn = this.#connections.get(connectionId);
    if (!conn) return { status: "closed", qrData: null };
    return { status: conn.status, qrData: conn.qrData };
  }

  async sendMessage(connectionId, jid, text) {
    let conn = this.#connections.get(connectionId);

    // Auto-reconnect if connection isn't active (e.g. after pod restart)
    if (!conn || !conn.socket || conn.status !== "open") {
      log.info({ connectionId }, "Connection not active, auto-reconnecting");
      await this.connect(connectionId);
      conn = this.#connections.get(connectionId);

      // Wait up to 30s for connection to become open
      const deadline = Date.now() + 30_000;
      while (conn && conn.status !== "open" && Date.now() < deadline) {
        await new Promise((r) => setTimeout(r, 1000));
      }

      if (!conn?.socket || conn.status !== "open") {
        throw new Error(`Connection ${connectionId} failed to reconnect`);
      }
    }

    await conn.socket.sendMessage(jid, { text });
  }
}

/**
 * A single WhatsApp Web connection backed by Baileys.
 */
class WhatsAppConnection {
  #connectionId;
  #backendUrl;
  #authDir;
  #qrResolve = null;

  socket = null;
  status = "connecting";
  qrData = null;

  constructor(connectionId, backendUrl) {
    this.#connectionId = connectionId;
    this.#backendUrl = backendUrl;
    this.#authDir = path.join(os.tmpdir(), "eaos-wa", connectionId);
  }

  /**
   * Wait for a QR code to be generated, with timeout.
   */
  waitForQr(timeoutMs) {
    if (this.qrData) return Promise.resolve(this.qrData);
    if (this.status === "open") return Promise.resolve(null);

    return new Promise((resolve) => {
      this.#qrResolve = resolve;
      setTimeout(() => {
        this.#qrResolve = null;
        resolve(this.qrData);
      }, timeoutMs);
    });
  }

  async start() {
    // Restore creds from backend if available
    await this.#restoreCreds();

    const { state, saveCreds } = await useMultiFileAuthState(this.#authDir);
    const { version } = await fetchLatestBaileysVersion();

    log.info({ connectionId: this.#connectionId, version }, "Starting Baileys socket");

    const sock = makeWASocket({
      version,
      auth: {
        creds: state.creds,
        keys: makeCacheableSignalKeyStore(state.keys, createLogger("signal")),
      },
      printQRInTerminal: false,
      logger: createLogger("baileys"),
      generateHighQualityLinkPreview: false,
    });

    this.socket = sock;

    // ── Connection updates (QR, open, close) ──────────────────────
    sock.ev.on("connection.update", async (update) => {
      const { connection, lastDisconnect, qr } = update;

      if (qr) {
        this.qrData = qr;
        this.status = "qr";
        log.info({ connectionId: this.#connectionId }, "QR code generated");
        if (this.#qrResolve) {
          this.#qrResolve(qr);
          this.#qrResolve = null;
        }
      }

      if (connection === "open") {
        this.status = "open";
        this.qrData = null;
        log.info({ connectionId: this.#connectionId }, "Connected to WhatsApp");
        await this.#persistCreds();
      }

      if (connection === "close") {
        const boom = lastDisconnect?.error;
        const statusCode = boom instanceof Boom ? boom.output?.statusCode : 0;
        const loggedOut = statusCode === DisconnectReason.loggedOut;

        log.warn(
          { connectionId: this.#connectionId, statusCode, loggedOut },
          "WhatsApp connection closed"
        );

        if (loggedOut) {
          // Session expired — clean up auth
          this.status = "logged_out";
          await fs.rm(this.#authDir, { recursive: true, force: true });
        } else {
          // Reconnect
          this.status = "reconnecting";
          setTimeout(() => this.start(), 3000);
        }
      }
    });

    // ── Creds update (persist on change) ──────────────────────────
    sock.ev.on("creds.update", async () => {
      await saveCreds();
      await this.#persistCreds();
    });

    // ── Inbound messages → forward to backend ─────────────────────
    sock.ev.on("messages.upsert", async ({ messages, type }) => {
      if (type !== "notify") return;

      for (const msg of messages) {
        if (msg.key.fromMe) continue;
        if (msg.key.remoteJid === "status@broadcast") continue;

        const text =
          msg.message?.conversation ||
          msg.message?.extendedTextMessage?.text ||
          msg.message?.imageMessage?.caption ||
          msg.message?.videoMessage?.caption ||
          "";

        if (!text) continue;

        const senderJid = msg.key.remoteJid;
        const isGroup = senderJid?.endsWith("@g.us") ?? false;
        const participant = isGroup ? msg.key.participant : null;

        log.info(
          { connectionId: this.#connectionId, from: senderJid, isGroup },
          "Inbound message"
        );

        // Forward to backend
        try {
          const res = await fetch(
            `${this.#backendUrl}/api/internal/channel/inbound`,
            {
              method: "POST",
              headers: { "Content-Type": "application/json" },
              body: JSON.stringify({
                connectionId: this.#connectionId,
                senderJid,
                participant,
                text,
                messageId: msg.key.id,
                isGroup,
              }),
            }
          );

          if (!res.ok) {
            log.error({ status: res.status }, "Backend rejected inbound message");
            continue;
          }

          // Send agent responses back
          const { responses } = await res.json();
          if (responses) {
            for (const [, replyText] of Object.entries(responses)) {
              if (replyText) {
                await sock.sendMessage(senderJid, { text: replyText });
              }
            }
          }
        } catch (err) {
          log.error({ err }, "Failed to forward message to backend");
        }
      }
    });
  }

  close() {
    this.status = "closed";
    this.qrData = null;
    try {
      this.socket?.end();
    } catch { /* ignore */ }
    this.socket = null;
  }

  // ── Credential persistence via backend API ──────────────────────

  async #persistCreds() {
    try {
      const credsPath = path.join(this.#authDir, "creds.json");
      const credsJson = await fs.readFile(credsPath, "utf-8");

      await fetch(
        `${this.#backendUrl}/api/internal/wa/creds/${this.#connectionId}`,
        {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({ credsJson }),
        }
      );
      log.debug({ connectionId: this.#connectionId }, "Creds persisted to backend");
    } catch (err) {
      log.error({ err }, "Failed to persist creds");
    }
  }

  async #restoreCreds() {
    try {
      await fs.mkdir(this.#authDir, { recursive: true });

      const res = await fetch(
        `${this.#backendUrl}/api/internal/wa/creds/${this.#connectionId}`
      );
      if (!res.ok) return;

      const { credsJson } = await res.json();
      if (!credsJson) return;

      const credsPath = path.join(this.#authDir, "creds.json");
      await fs.writeFile(credsPath, credsJson, { mode: 0o600 });
      log.info({ connectionId: this.#connectionId }, "Creds restored from backend");
    } catch (err) {
      log.debug({ err }, "No saved creds to restore (first connection)");
    }
  }
}
