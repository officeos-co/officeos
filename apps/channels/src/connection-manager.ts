import type { ChannelAdapter, ChannelSetup } from './channels/adapter.js';
import { createAdapter } from './channels/index.js';
import { forwardInbound } from './backend-client.js';
import { log } from './log.js';

interface ActiveConnection {
  adapter: ChannelAdapter;
  channelType: string;
}

const connections = new Map<string, ActiveConnection>();

export async function activateConnection(
  id: string,
  channelType: string,
  creds: string,
): Promise<void> {
  const parsedCreds = JSON.parse(creds) as Record<string, string>;
  const adapter = createAdapter(channelType, parsedCreds);
  if (!adapter) {
    log.warn('No adapter factory registered for channel type', { channelType });
    return;
  }

  const setup: ChannelSetup = {
    onInbound(platformId, threadId, message) {
      forwardInbound({
        connectionId: id,
        senderIdentifier: platformId,
        messageText: typeof message.content === 'string' ? message.content : JSON.stringify(message.content),
        isGroupMessage: !!threadId,
        messageId: message.id,
        channelId: threadId ?? platformId,
      });
    },
    onInboundEvent() {},
    onMetadata() {},
    onAction() {},
  };

  await adapter.setup(setup);
  connections.set(id, { adapter, channelType });
  log.info('Connection activated', { id, channelType });
}

export async function deactivateConnection(id: string): Promise<void> {
  const conn = connections.get(id);
  if (!conn) return;
  await conn.adapter.teardown();
  connections.delete(id);
  log.info('Connection deactivated', { id });
}

export function getActiveConnection(id: string): ActiveConnection | undefined {
  return connections.get(id);
}

export function activeConnectionCount(): number {
  return connections.size;
}

export async function activateAll(
  rows: Array<{ id: string; channel_type: string; creds_json: string }>,
): Promise<void> {
  for (const row of rows) {
    try {
      await activateConnection(row.id, row.channel_type, row.creds_json);
    } catch (err) {
      log.error('Failed to activate connection on startup', { id: row.id, err });
    }
  }
}
