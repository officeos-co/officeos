import { WhatsAppAdapter } from '@chat-adapter/whatsapp';
import { createChatSdkBridge } from './chat-sdk-bridge.js';
import { registerAdapterFactory } from './adapter-factory.js';

registerAdapterFactory('whatsapp', (creds) => {
  const adapter = new WhatsAppAdapter({
    accessToken: creds.accessToken,
    phoneNumberId: creds.phoneNumberId,
    verifyToken: creds.verifyToken,
    appSecret: creds.appSecret,
    userName: 'Agent',
    logger: console as never,
  });
  return createChatSdkBridge({
    adapter,
    supportsThreads: false,
    maxTextLength: 4096,
  });
});
