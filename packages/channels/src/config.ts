if (!process.env.PORT) throw new Error('Missing required env var: PORT');
if (!process.env.BACKEND_URL) throw new Error('Missing required env var: BACKEND_URL');

export const PORT = parseInt(process.env.PORT);
export const BACKEND_URL = process.env.BACKEND_URL;
