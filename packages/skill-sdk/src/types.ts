import { z } from "zod";

/**
 * Runtime context injected into every skill action execution.
 * Credentials are decrypted and injected by the skill runtime — skills
 * never access the credential store directly.
 */
export interface SkillContext {
  /** Decrypted credentials (injected by runtime, keys match the skill's credential schema). */
  credentials: Record<string, string>;
  /** Sandboxed fetch — in production this is scoped to allowed origins. */
  fetch: typeof globalThis.fetch;
  /** Structured logger. */
  log: (...args: unknown[]) => void;
}

/**
 * A single action within a skill (e.g. "search", "read_page").
 */
export interface ActionDefinition<T extends z.ZodType = z.ZodType> {
  /** Human-readable description shown in --help and LLM tool schema. */
  description: string;
  /** Zod schema for the action's parameters. */
  params: T;
  /** Execute the action with validated params and injected context. */
  execute: (params: z.infer<T>, ctx: SkillContext) => Promise<unknown>;
}

/**
 * A complete skill definition — the unit of packaging and deployment.
 */
export interface SkillDefinition {
  /** Unique skill identifier (lowercase, e.g. "notion", "github"). */
  name: string;
  /** Human-readable title (e.g. "Notion", "GitHub"). */
  description: string;
  /** Credential schema — keys are credential field names, values are Zod schemas for validation. */
  credentials: Record<string, z.ZodType>;
  /** Map of action name → action definition. */
  actions: Record<string, ActionDefinition>;
}
