import type { SkillContext } from "@harro/skill-sdk";
import { createContext } from "@harro/skill-sdk";

/**
 * Create a sandboxed SkillContext for executing a skill action.
 *
 * Phase 1: Uses globalThis.fetch directly (same Node.js process).
 * Phase 2: Will use Deno isolates with restricted permissions.
 */
export function createSandboxedContext(opts: {
  credentials: Record<string, string>;
  page?: unknown;
}): SkillContext {
  return createContext({
    credentials: opts.credentials,
    fetch: globalThis.fetch,
    log: (...args: unknown[]) => {
      console.log("[skill]", ...args);
    },
    page: opts.page,
  });
}
