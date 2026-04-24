import { GoogleChatAdapter } from '@chat-adapter/gchat';
import { createChatSdkBridge } from './chat-sdk-bridge.js';
import { registerAdapterFactory } from './adapter-factory.js';

registerAdapterFactory('google-chat', (creds) => {
  const credentials = JSON.parse(creds.serviceAccountJson);
  const adapter = new GoogleChatAdapter({ credentials });
  return createChatSdkBridge({
    adapter,
    supportsThreads: true,
  });
});
