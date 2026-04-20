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
  /** Playwright Page object — injected by runtime for browser skills. */
  page?: unknown;
}

/**
 * A single action within a skill (e.g. "search", "read_page").
 */
export interface ActionDefinition<T extends z.ZodType = z.ZodType> {
  /** Human-readable description shown in --help and LLM tool schema. */
  description: string;
  /** Zod schema for the action's parameters. */
  params: T;
  /**
   * Zod schema describing the shape of the return value.
   * Used by the runtime to generate GraphQL return types for introspection.
   * If omitted, the return type is treated as opaque JSON.
   */
  returns?: z.ZodType;
  /** Execute the action with validated params and injected context. */
  execute: (params: z.infer<T>, ctx: SkillContext) => Promise<unknown>;
}

/**
 * OAuth2 provider configuration for credentials that use browser-based login.
 * The backend handles the full OAuth2 flow — the skill just receives an access_token.
 */
export interface OAuth2CredentialConfig {
  /** OAuth2 provider identifier. */
  provider: "google" | "microsoft" | "github";
  /** OAuth2 scopes to request. */
  scopes: string[];
}

/**
 * Credential field definition — describes a single credential the skill needs.
 */
export interface CredentialFieldDefinition {
  /** Human-readable label for the dashboard form (e.g. "Personal Access Token"). */
  label: string;
  /** Input kind: "password" for secrets, "text" for short strings, "textarea" for multi-line, "oauth2" for browser login. */
  kind: "password" | "text" | "textarea" | "oauth2";
  /** Whether this field is required. Defaults to true. */
  required?: boolean;
  /** Placeholder text for the input field. */
  placeholder?: string;
  /** Help text shown below the input. */
  help?: string;
  /** OAuth2 configuration — required when kind is "oauth2". */
  oauth2?: OAuth2CredentialConfig;
}

export interface SkillAuthor {
  name: string;
  url?: string;
}

export interface SkillContributor {
  name: string;
  url?: string;
}

/**
 * A complete skill definition — the unit of packaging and deployment.
 */
export interface SkillDefinition {
  /** Unique skill identifier (lowercase, e.g. "notion", "github"). */
  name: string;
  /** Human-readable title (e.g. "Notion", "GitHub"). */
  title: string;
  /**
   * Raw inline SVG markup used as the skill's logo in the dashboard.
   * Must be a complete `<svg ...>...</svg>` string —
   * not a URL, not a file path. Typically sourced from simpleicons.org
   * with a minimal `<svg viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg"><path d="..."/></svg>` wrapper.
   */
  logo: string;
  /** Short description of the skill's purpose. */
  description: string;
  /**
   * Markdown documentation for the skill's CLI interface.
   * Injected into the agent's context so it knows how to use the skill.
   * Must include workflow guidance, examples, and limitations.
   */
  doc: string;
  /** Credential fields — keys are credential names, values describe the field and its UI. */
  credentials: Record<string, CredentialFieldDefinition>;
  /** Map of action name → action definition. */
  actions: Record<string, ActionDefinition>;
  /** Semver version string (e.g. "1.0.0"). */
  version?: string;
  /** SPDX license identifier (e.g. "MIT"). */
  license?: string;
  /** GitHub repository URL. */
  repository?: string;
  /** 1-3 categories from the fixed category list. */
  categories?: string[];
  /** Freeform search keywords, max 30. */
  keywords?: string[];
  /** Skill author. */
  author?: SkillAuthor;
  /** Contributors list. */
  contributors?: SkillContributor[];
}
