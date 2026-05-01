import { DiscordAdapter } from '@chat-adapter/discord';
import { createChatSdkBridge } from './chat-sdk-bridge.js';
import { registerAdapterFactory } from './adapter-factory.js';

registerAdapterFactory('discord', (creds) => {
  const adapter = new DiscordAdapter({
    botToken: creds.botToken,
    applicationId: creds.applicationId,
    publicKey: creds.publicKey,
  });
  return createChatSdkBridge({
    adapter,
    supportsThreads: true,
    maxTextLength: 2000,
  });
});
