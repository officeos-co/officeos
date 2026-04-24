import type { ChannelAdapter } from './channels/adapter.js';
import { createAdapter } from './channels/index.js';
import { fetchActiveConnections } from './backend-client.js';
import { log } from './log.js';

// One adapter per channel type (simple model)
const adapters = new Map<string, ChannelAdapter>();

export function getAdapter(channelType: string): ChannelAdapter | undefined {
  return adapters.get(channelType);
}

export function activeAdapterCount(): number {
  return adapters.size;
}

export async function reloadFromBackend(): Promise<void> {
  const connections = await fetchActiveConnections();

  // Determine which channel types should be active
  const desired = new Map<string, { creds: Record<string, string> }>();
  for (const conn of connections) {
    // First connection per channel type wins (simple model)
    if (!desired.has(conn.channelType)) {
      desired.set(conn.channelType, { creds: conn.creds });
    }
  }

  // Tear down adapters no longer needed
  for (const [channelType, adapter] of adapters) {
    if (!desired.has(channelType)) {
      log.info('Deactivating adapter', { channelType });
      await adapter.teardown();
      adapters.delete(channelType);
    }
  }

  // Start new adapters
  for (const [channelType, { creds }] of desired) {
    if (adapters.has(channelType)) continue; // already running

    try {
      const adapter = createAdapter(channelType, creds);
      if (!adapter) {
        log.warn('No factory for channel type', { channelType });
        continue;
      }

      // Setup with onInbound callback that forwards to backend
      const { forwardInbound } = await import('./backend-client.js');
      await adapter.setup({
        onInbound(platformId, threadId, message) {
          forwardInbound({
            connectionId: '', // backend resolves via channelType + platformId
            senderIdentifier: platformId,
            messageText: typeof message.content === 'string'
              ? message.content
              : JSON.stringify(message.content),
            isGroupMessage: !!threadId,
            messageId: message.id,
            channelId: threadId ?? platformId,
          });
        },
        onInboundEvent() {},
        onMetadata() {},
        onAction() {},
      });

      adapters.set(channelType, adapter);
      log.info('Adapter activated', { channelType });
    } catch (err) {
      log.error('Failed to activate adapter', { channelType, err });
    }
  }

  log.info('Reload complete', { activeAdapters: adapters.size });
}

export async function shutdownAll(): Promise<void> {
  for (const [channelType, adapter] of adapters) {
    try {
      await adapter.teardown();
      log.info('Adapter shut down', { channelType });
    } catch (err) {
      log.error('Failed to shut down adapter', { channelType, err });
    }
  }
  adapters.clear();
}
