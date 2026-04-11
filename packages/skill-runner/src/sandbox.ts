import type { SkillContext } from "@harro/skill-sdk";
import { createContext } from "@harro/skill-sdk";

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
