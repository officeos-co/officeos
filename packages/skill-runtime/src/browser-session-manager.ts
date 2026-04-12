import { chromium, type Browser, type BrowserContext, type Page, type Cookie } from 'playwright-core';
import { randomUUID } from 'node:crypto';

interface SessionEntry {
  context: BrowserContext;
  page: Page;
  lastAccess: Date;
}

/**
 * Manages browser sessions for browser-based skills.
 * Lazily launches a headless Chromium instance and pools BrowserContexts
 * so that multi-step browser workflows can reuse the same page/session.
 */
export class BrowserSessionManager {
  private sessions = new Map<string, SessionEntry>();
  private browser: Browser | null = null;
  private cleanupInterval: ReturnType<typeof setInterval> | null = null;
  private readonly IDLE_TIMEOUT = 5 * 60 * 1000; // 5 minutes
  private readonly MAX_SESSIONS = 20;

  /**
   * Lazily launch a headless Chromium browser if not already running.
   * Re-launches if the previous browser was disconnected.
   */
  async ensureBrowser(): Promise<Browser> {
    if (this.browser && this.browser.isConnected()) {
      return this.browser;
    }

    this.browser = await chromium.launch({
      headless: true,
      args: ['--no-sandbox', '--disable-setuid-sandbox', '--disable-dev-shm-usage'],
    });

    return this.browser;
  }

  /**
   * Get an existing session by ID, or create a new one.
   * Optionally inject cookies into a freshly created context.
   */
  async getOrCreate(
    sessionId?: string,
    cookies?: Cookie[],
  ): Promise<{ sessionId: string; page: Page }> {
    // Return existing session if it exists
    if (sessionId) {
      const existing = this.sessions.get(sessionId);
      if (existing) {
        existing.lastAccess = new Date();
        return { sessionId, page: existing.page };
      }
    }

    // Evict oldest session if at capacity
    if (this.sessions.size >= this.MAX_SESSIONS) {
      let oldestId: string | undefined;
      let oldestTime = Infinity;
      for (const [id, entry] of this.sessions) {
        const t = entry.lastAccess.getTime();
        if (t < oldestTime) {
          oldestTime = t;
          oldestId = id;
        }
      }
      if (oldestId) {
        await this.close(oldestId);
      }
    }

    const browser = await this.ensureBrowser();

    const context = await browser.newContext({
      userAgent:
        'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36',
    });

    if (cookies && cookies.length > 0) {
      await context.addCookies(cookies);
    }

    const page = await context.newPage();
    const newId = randomUUID();

    this.sessions.set(newId, {
      context,
      page,
      lastAccess: new Date(),
    });

    return { sessionId: newId, page };
  }

  /**
   * Extract all cookies from a session's browser context.
   */
  async extractCookies(sessionId: string): Promise<Cookie[]> {
    const session = this.sessions.get(sessionId);
    if (!session) return [];
    return session.context.cookies();
  }

  /**
   * Close a session — extracts cookies before tearing down.
   * Returns the cookies so the caller can persist them if needed.
   */
  async close(sessionId: string): Promise<Cookie[]> {
    const session = this.sessions.get(sessionId);
    if (!session) return [];

    const cookies = await session.context.cookies();
    await session.context.close();
    this.sessions.delete(sessionId);
    return cookies;
  }

  /**
   * Start periodic cleanup of idle sessions (every 60 s).
   */
  startCleanup(): void {
    this.cleanupInterval = setInterval(async () => {
      const now = Date.now();
      for (const [id, entry] of this.sessions) {
        if (now - entry.lastAccess.getTime() > this.IDLE_TIMEOUT) {
          console.log(`[browser] Closing idle session ${id}`);
          await this.close(id);
        }
      }
    }, 60_000);
  }

  /**
   * Shut down everything — clear timers, close all sessions, close browser.
   */
  async shutdown(): Promise<void> {
    if (this.cleanupInterval) {
      clearInterval(this.cleanupInterval);
      this.cleanupInterval = null;
    }

    for (const id of this.sessions.keys()) {
      await this.close(id);
    }

    if (this.browser) {
      await this.browser.close();
      this.browser = null;
    }
  }
}

/** Singleton instance shared across the runtime. */
export const browserSessions = new BrowserSessionManager();
