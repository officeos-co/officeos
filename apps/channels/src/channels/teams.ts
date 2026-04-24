import { TeamsAdapter } from '@chat-adapter/teams';
import { createChatSdkBridge } from './chat-sdk-bridge.js';
import { registerAdapterFactory } from './adapter-factory.js';

registerAdapterFactory('teams', (creds) => {
  const adapter = new TeamsAdapter({
    appId: creds.appId,
    appPassword: creds.appPassword,
  });
  return createChatSdkBridge({
    adapter,
    supportsThreads: true,
  });
});
