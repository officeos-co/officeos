import { z } from "zod";
import type { SkillDefinition, ActionDefinition, SkillContext, CredentialFieldDefinition } from "./types.js";

export { z };
export type { SkillDefinition, ActionDefinition, SkillContext, CredentialFieldDefinition };
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
 *   title: 'Notion',
 *   emoji: '📝',
 *   description: 'Search and read Notion pages.',
 *   doc,
 *   credentials: {
 *     api_key: { label: 'API Key', kind: 'password' },
 *   },
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
