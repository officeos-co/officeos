import { z } from "@harro/skill-sdk";
import type { ActionDefinition } from "@harro/skill-sdk";
import { gfFetch, qs } from "../core/client.ts";

export const users: Record<string, ActionDefinition> = {
  list_users: {
    description: "List users (Admin only).",
    params: z.object({
      per_page: z.number().min(1).max(1000).default(100).describe("Results per page"),
    }),
    returns: z.array(
      z.object({ id: z.number(), login: z.string(), name: z.string(), email: z.string(), is_admin: z.boolean() }),
    ),
    execute: async (params, ctx) => {
      const data = await gfFetch(ctx, `/users${qs({ perpage: params.per_page })}`);
      return data.map((u: any) => ({
        id: u.id, login: u.login, name: u.name ?? "", email: u.email ?? "", is_admin: u.isGrafanaAdmin ?? false,
      }));
    },
  },

  get_user: {
    description: "Get a user by ID (Admin only).",
    params: z.object({ id: z.number().describe("User ID") }),
    returns: z.object({
      id: z.number(), login: z.string(), name: z.string(), email: z.string(),
      is_admin: z.boolean(), is_disabled: z.boolean(), last_seen_at: z.string(),
    }),
    execute: async (params, ctx) => {
      const u = await gfFetch(ctx, `/users/${params.id}`);
      return {
        id: u.id, login: u.login, name: u.name ?? "", email: u.email ?? "",
        is_admin: u.isGrafanaAdmin ?? false, is_disabled: u.isDisabled ?? false, last_seen_at: u.lastSeenAt ?? "",
      };
    },
  },

  get_org: {
    description: "Get the current organization.",
    params: z.object({}),
    returns: z.object({ id: z.number(), name: z.string(), address: z.any() }),
    execute: async (_params, ctx) => {
      const data = await gfFetch(ctx, "/org");
      return { id: data.id, name: data.name, address: data.address ?? {} };
    },
  },

  list_org_users: {
    description: "List users in the current organization.",
    params: z.object({}),
    returns: z.array(z.object({ login: z.string(), role: z.string(), email: z.string() })),
    execute: async (_params, ctx) => {
      const data = await gfFetch(ctx, "/org/users");
      return data.map((u: any) => ({ login: u.login, role: u.role, email: u.email ?? "" }));
    },
  },
};
