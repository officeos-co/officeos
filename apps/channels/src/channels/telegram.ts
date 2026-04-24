import { TelegramAdapter } from '@chat-adapter/telegram';
import { createChatSdkBridge } from './chat-sdk-bridge.js';
import { registerAdapterFactory } from './adapter-factory.js';

registerAdapterFactory('telegram', (creds) => {
  const adapter = new TelegramAdapter({
    botToken: creds.botToken,
  });
  return createChatSdkBridge({
    adapter,
    supportsThreads: false,
    maxTextLength: 4096,
  });
});
