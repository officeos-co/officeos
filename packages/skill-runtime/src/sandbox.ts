import type { SkillContext } from "@eaos/skill-sdk";
import { createContext } from "@eaos/skill-sdk";

/**
 * Create a sandboxed SkillContext for executing a skill action.
 *
 * Phase 1: Uses globalThis.fetch directly (same Node.js process).
 * Phase 2: Will use Deno isolates with restricted permissions.
 */
export function createSandboxedContext(opts: {
  credentials: Record<string, string>;
}): SkillContext {
  return createContext({
    credentials: opts.credentials,
    fetch: globalThis.fetch,
    log: (...args: unknown[]) => {
      console.log("[skill]", ...args);
    },
  });
}
