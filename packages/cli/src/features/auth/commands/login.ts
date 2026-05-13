import { hostname } from "node:os";
import { createDeviceCode, getMe, pollDeviceToken } from "../api/auth-api";
import { resolveApiUrl } from "../../../lib/env";
import { writeContext } from "../../../lib/config-store";
import { openBrowser } from "../../../shell/browser";
import { print } from "../../../shell/output";

export async function loginCommand(args: string[]): Promise<void> {
  const apiUrl = resolveApiUrl(readOption(args, "--api-url"));
  const context = readOption(args, "--context") ?? "default";
  const code = await createDeviceCode(apiUrl, hostname());

  print(`Open this URL to authenticate: ${code.verificationUriComplete}`);
  await openBrowser(code.verificationUriComplete).catch(() => undefined);

  const token = await waitForToken(apiUrl, code.deviceCode, code.intervalSeconds);
  await writeContext(context, apiUrl, token);
  const me = await getMe(apiUrl, token);
  print(`Logged in as ${me.email}`);
}

async function waitForToken(apiUrl: string, deviceCode: string, intervalSeconds: number): Promise<string> {
  for (;;) {
    await sleep(intervalSeconds * 1000);
    const result = await pollDeviceToken(apiUrl, deviceCode);
    if (result.status === "authorized" && result.accessToken) return result.accessToken;
    if (result.status === "expired") throw new Error("Device login expired. Run `eaos login` again.");
    intervalSeconds = result.intervalSeconds;
  }
}

function readOption(args: string[], name: string): string | undefined {
  const index = args.indexOf(name);
  return index >= 0 ? args[index + 1] : undefined;
}

function sleep(ms: number): Promise<void> {
  return new Promise((resolve) => setTimeout(resolve, ms));
}
