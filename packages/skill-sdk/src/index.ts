import { z } from "zod";
import type { SkillDefinition, ActionDefinition, SkillContext } from "./types.js";

export { z };
export type { SkillDefinition, ActionDefinition, SkillContext };
export { createContext } from "./context.js";

/**
 * Define a skill. This is the main entry point for skill authors.
 *
 * @example
 * ```ts
 * import { defineSkill, z } from '@harro/skill-sdk'
 *
 * export default defineSkill({
 *   name: 'notion',
 *   description: 'Search and read Notion pages.',
 *   credentials: { api_key: z.string() },
 *   actions: {
 *     search: {
 *       description: 'Search pages',
 *       params: z.object({ query: z.string() }),
 *       execute: async (params, ctx) => { ... }
 *     }
 *   }
 * })
 * ```
 */
export function defineSkill(def: SkillDefinition): SkillDefinition {
  return def;
}
