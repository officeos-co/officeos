import { SlackAdapter } from '@chat-adapter/slack';
import { createChatSdkBridge } from './chat-sdk-bridge.js';
import { registerAdapterFactory } from './adapter-factory.js';

registerAdapterFactory('slack', (creds) => {
  const adapter = new SlackAdapter({
    botToken: creds.botToken,
    signingSecret: creds.signingSecret,
  });
  return createChatSdkBridge({
    adapter,
    supportsThreads: true,
    maxTextLength: 4000,
  });
});
