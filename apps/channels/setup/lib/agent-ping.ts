/**
 * Round-trip check against the CLI Unix socket.
 *
 * Shared by `setup/verify.ts` (end-of-run health check) and `setup/auto.ts`
 * (confirm the freshly-wired agent actually responds before prompting the
 * user to chat with it).
 *
 * Exit-code contract follows `scripts/chat.ts`:
 *   0  → got a reply on stdout
 *   2  → socket unreachable (service not running or wrong checkout)
 *   3  → no reply before chat.ts's own 120s hard stop
 * This wrapper also guards with its own timeout in case chat.ts hangs.
 */
import { spawn } from 'child_process';

export type PingResult = 'ok' | 'no_reply' | 'socket_error';

export function pingCliAgent(timeoutMs = 30_000): Promise<PingResult> {
  return new Promise((resolve) => {
    const child = spawn('pnpm', ['run', 'chat', 'ping'], {
      stdio: ['ignore', 'pipe', 'pipe'],
    });
    let stdout = '';
    let settled = false;
    const timer = setTimeout(() => {
      if (settled) return;
      settled = true;
      child.kill('SIGKILL');
      resolve('no_reply');
    }, timeoutMs);

    child.stdout.on('data', (chunk: Buffer) => {
      stdout += chunk.toString('utf-8');
    });
    child.on('close', (code) => {
      if (settled) return;
      settled = true;
      clearTimeout(timer);
      if (code === 2) resolve('socket_error');
      else if (code === 0 && stdout.trim().length > 0) resolve('ok');
      else resolve('no_reply');
    });
    child.on('error', () => {
      if (settled) return;
      settled = true;
      clearTimeout(timer);
      resolve('socket_error');
    });
  });
}
