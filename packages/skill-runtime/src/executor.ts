import { z } from "zod";
import type { SkillDefinition } from "@harro/skill-sdk";
import { createSandboxedContext } from "./sandbox.js";
import { extractManifest, type SkillManifest } from "./manifest.js";

export interface ExecuteRequest {
  skill: string;
  action: string;
  params: Record<string, unknown>;
  credentials: Record<string, string>;
  sessionContext?: {
    sessionId?: string;
    cookies?: Array<{ name: string; value: string; domain: string; path: string; [key: string]: unknown }>;
  };
}

export interface ExecuteResult {
  success: boolean;
  result?: unknown;
  error?: string;
  sessionMeta?: {
    sessionId: string;
    cookies: Array<{ name: string; value: string; domain: string; path: string; [key: string]: unknown }>;
  };
}

/**
 * Manages loaded skill definitions and executes actions.
 */
export class SkillExecutor {
  private skills = new Map<string, SkillDefinition>();

  register(def: SkillDefinition): void {
    this.skills.set(def.name, def);
  }

  getManifest(name: string): SkillManifest | undefined {
    const def = this.skills.get(name);
    return def ? extractManifest(def) : undefined;
  }

  getAllManifests(): SkillManifest[] {
    return Array.from(this.skills.values()).map(extractManifest);
  }

  async execute(req: ExecuteRequest): Promise<ExecuteResult> {
    const def = this.skills.get(req.skill);
    if (!def) {
      return { success: false, error: `Unknown skill: ${req.skill}` };
    }

    const actionDef = def.actions[req.action];
    if (!actionDef) {
      return {
        success: false,
        error: `Unknown action: ${req.action} on skill ${req.skill}. Available: ${Object.keys(def.actions).join(", ")}`,
      };
    }

    // Validate params against Zod schema
    let validatedParams: unknown;
    try {
      validatedParams = actionDef.params.parse(req.params);
    } catch (err) {
      if (err instanceof z.ZodError) {
        return {
          success: false,
          error: `Parameter validation failed: ${err.errors.map((e) => `${e.path.join(".")}: ${e.message}`).join("; ")}`,
        };
      }
      return { success: false, error: `Validation error: ${String(err)}` };
    }

    // Set up browser session if this is a browser skill
    let page: unknown = undefined;
    let activeSessionId: string | undefined;

    if (req.skill === "browser") {
      const { browserSessions } = await import("./browser-session-manager.js");
      const session = await browserSessions.getOrCreate(
        req.sessionContext?.sessionId,
        req.sessionContext?.cookies as any,
      );
      page = session.page;
      activeSessionId = session.sessionId;
    }

    // Create sandboxed context with injected credentials
    const ctx = createSandboxedContext({ credentials: req.credentials, page });

    // Execute the action
    try {
      const result = await actionDef.execute(validatedParams, ctx);

      // Return session metadata for browser skills
      if (req.skill === "browser" && activeSessionId) {
        const { browserSessions } = await import("./browser-session-manager.js");
        if (req.action === "close") {
          const cookies = await browserSessions.close(activeSessionId);
          return { success: true, result, sessionMeta: { sessionId: "", cookies: cookies as any } };
        }
        const cookies = await browserSessions.extractCookies(activeSessionId);
        return { success: true, result, sessionMeta: { sessionId: activeSessionId, cookies: cookies as any } };
      }

      return { success: true, result };
    } catch (err) {
      return {
        success: false,
        error: err instanceof Error ? err.message : String(err),
      };
    }
  }
}
