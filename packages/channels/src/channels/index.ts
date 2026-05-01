// Channel adapter registrations — each import triggers registerAdapterFactory()
import './slack.js';
import './discord.js';
import './telegram.js';
import './whatsapp.js';
import './teams.js';
import './google-chat.js';

export { createAdapter } from './adapter-factory.js';
