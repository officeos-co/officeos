import type { ChannelAdapter } from './adapter.js';

type AdapterConstructor = (creds: Record<string, string>) => ChannelAdapter;

const factories = new Map<string, AdapterConstructor>();

export function registerAdapterFactory(channelType: string, factory: AdapterConstructor): void {
  factories.set(channelType, factory);
}

export function createAdapter(channelType: string, creds: Record<string, string>): ChannelAdapter | null {
  const factory = factories.get(channelType);
  if (!factory) return null;
  return factory(creds);
}
