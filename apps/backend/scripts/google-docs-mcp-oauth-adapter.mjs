#!/usr/bin/env node

import { spawn } from 'node:child_process';
import { access, mkdtemp, readFile, rm, writeFile } from 'node:fs/promises';
import { tmpdir } from 'node:os';
import path from 'node:path';

const packageSpec = process.env.GOOGLE_DOCS_MCP_PACKAGE ?? 'github:NoManNayeem/google-docs-mcp-server';
const clientId = process.env.GOOGLE_CLIENT_ID;
const clientSecret = process.env.GOOGLE_CLIENT_SECRET;
const refreshToken = process.env.GOOGLE_REFRESH_TOKEN;
const tokenScope =
  process.env.GOOGLE_TOKEN_SCOPE ??
  'https://www.googleapis.com/auth/documents https://www.googleapis.com/auth/drive';

if (!clientId || !clientSecret || !refreshToken) {
  console.error('google-docs MCP adapter requires GOOGLE_CLIENT_ID, GOOGLE_CLIENT_SECRET, and GOOGLE_REFRESH_TOKEN.');
  process.exit(1);
}

const workDir = await mkdtemp(path.join(tmpdir(), 'eaos-google-docs-mcp-'));
let cleanedUp = false;

async function cleanup() {
  if (cleanedUp) return;
  cleanedUp = true;
  await rm(workDir, { recursive: true, force: true });
}

function run(command, args, options = {}) {
  return new Promise((resolve, reject) => {
    const child = spawn(command, args, {
      stdio: ['ignore', 'pipe', 'pipe'],
      ...options,
    });

    let stdout = '';
    let stderr = '';
    child.stdout?.on('data', chunk => {
      stdout += chunk;
    });
    child.stderr?.on('data', chunk => {
      stderr += chunk;
    });
    child.on('error', reject);
    child.on('close', code => {
      if (code === 0) {
        resolve({ stdout, stderr });
        return;
      }

      reject(new Error(`${command} ${args.join(' ')} exited with ${code}: ${stderr || stdout}`));
    });
  });
}

async function exists(filePath) {
  try {
    await access(filePath);
    return true;
  } catch {
    return false;
  }
}

async function patchServerStartup(packageDir) {
  const indexPath = path.join(packageDir, 'src', 'index.ts');
  if (!await exists(indexPath)) return;

  const source = await readFile(indexPath, 'utf8');
  if (!source.includes('(async () => {') || !source.includes('async function main() {')) {
    return;
  }

  const patched = source
    .replace(
      '// Initialize authentication and register tools\n(async () => {',
      '// Initialize authentication and register tools\nasync function initializeGoogleDocsTools() {',
    )
    .replace(
      '})();\n\n// Add authentication status tool',
      '}\n\n// Add authentication status tool',
    )
    .replace(
      'async function main() {\n  const transport = new StdioServerTransport();',
      'async function main() {\n  await initializeGoogleDocsTools();\n  const transport = new StdioServerTransport();',
    );

  if (patched !== source) {
    await writeFile(indexPath, patched);
  }
}

async function main() {
  const packed = await run('npm', ['pack', packageSpec, '--pack-destination', workDir]);
  const tarball = packed.stdout.trim().split(/\r?\n/).at(-1);
  if (!tarball) throw new Error(`npm pack did not return a tarball name for ${packageSpec}.`);

  await run('tar', ['-xzf', path.join(workDir, tarball), '-C', workDir]);

  const packageDir = path.join(workDir, 'package');
  await writeFile(
    path.join(packageDir, 'credentials.json'),
    JSON.stringify({
      installed: {
        client_id: clientId,
        client_secret: clientSecret,
        redirect_uris: ['http://localhost:3001/oauth2callback'],
      },
    }),
    { mode: 0o600 },
  );

  await writeFile(
    path.join(packageDir, 'token.json'),
    JSON.stringify({
      refresh_token: refreshToken,
      token_type: 'Bearer',
      scope: tokenScope,
    }),
    { mode: 0o600 },
  );

  let serverEntry = path.join(packageDir, 'build', 'index.js');
  if (!await exists(serverEntry)) {
    await patchServerStartup(packageDir);
    await run('npm', ['install', '--no-audit', '--fund=false'], { cwd: packageDir });
    await run('npm', ['run', 'build', '--silent'], { cwd: packageDir });
  }

  if (!await exists(serverEntry)) {
    serverEntry = path.join(packageDir, 'dist', 'index.js');
  }

  if (!await exists(serverEntry)) {
    throw new Error(`${packageSpec} did not provide build/index.js or dist/index.js.`);
  }

  const server = spawn(process.execPath, [serverEntry], {
    cwd: packageDir,
    env: {
      ...process.env,
      oauth_client_id: clientId,
      oauth_client_secret: clientSecret,
      refresh_token: refreshToken,
    },
    stdio: 'inherit',
  });

  const forward = signal => {
    if (!server.killed) server.kill(signal);
  };
  process.on('SIGINT', forward);
  process.on('SIGTERM', forward);

  server.on('exit', async (code, signal) => {
    await cleanup();
    if (signal) {
      process.kill(process.pid, signal);
      return;
    }
    process.exit(code ?? 0);
  });
}

main().catch(async error => {
  console.error(`google-docs MCP adapter failed: ${error instanceof Error ? error.message : String(error)}`);
  await cleanup();
  process.exit(1);
});
