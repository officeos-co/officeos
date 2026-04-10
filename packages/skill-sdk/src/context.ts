import type { SkillContext } from "./types.js";

/**
 * Create a SkillContext for executing a skill action.
 * Called by the skill runtime — not by skill authors directly.
 */
export function createContext(opts: {
  credentials: Record<string, string>;
  fetch?: typeof globalThis.fetch;
  log?: (...args: unknown[]) => void;
}): SkillContext {
  return {
    credentials: opts.credentials,
    fetch: opts.fetch ?? globalThis.fetch,
    log: opts.log ?? console.log,
  };
}
