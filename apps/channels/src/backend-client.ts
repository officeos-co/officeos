import { BACKEND_URL } from './config.js';
import { log } from './log.js';

export interface ActiveConnection {
  connectionId: string;
  channelType: string;
  creds: Record<string, string>;
}

export async function fetchActiveConnections(): Promise<ActiveConnection[]> {
  const url = `${BACKEND_URL}/api/channels/active`;
  try {
    const res = await fetch(url);
    if (!res.ok) {
      log.error('Failed to fetch active connections', { status: res.status });
      return [];
    }
    return await res.json() as ActiveConnection[];
  } catch (err) {
    log.error('Failed to fetch active connections', { err });
    return [];
  }
}

// Keep existing forwardInbound function but simplify the interface
interface InboundPayload {
  channelType: string;
  senderIdentifier: string;
  messageText: string;
  isGroupMessage: boolean;
  messageId: string;
  channelId: string;
}

const MAX_ATTEMPTS = 3;
const BASE_DELAY_MS = 500;

export async function forwardInbound(payload: InboundPayload): Promise<void> {
  const url = `${BACKEND_URL}/api/channels/inbound`;

  for (let attempt = 0; attempt < MAX_ATTEMPTS; attempt++) {
    try {
      const res = await fetch(url, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(payload),
      });

      if (res.ok) return;

      if (res.status >= 500 && attempt < MAX_ATTEMPTS - 1) {
        const delay = BASE_DELAY_MS * Math.pow(2, attempt);
        log.warn('Backend returned 5xx, retrying', { status: res.status, attempt, delay });
        await new Promise((r) => setTimeout(r, delay));
        continue;
      }

      log.error('Failed to forward inbound message', { status: res.status, body: await res.text() });
      return;
    } catch (err) {
      if (attempt < MAX_ATTEMPTS - 1) {
        const delay = BASE_DELAY_MS * Math.pow(2, attempt);
        log.warn('Network error forwarding inbound, retrying', { attempt, delay, err });
        await new Promise((r) => setTimeout(r, delay));
        continue;
      }
      log.error('Failed to forward inbound message after retries', { err });
    }
  }
}
