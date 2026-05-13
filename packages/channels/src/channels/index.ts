// Channel adapter registrations — each import triggers registerAdapterFactory()
import './slack.js';
import './telegram.js';

export { createAdapter } from './adapter-factory.js';
