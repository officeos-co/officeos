import { z } from "@harro/skill-sdk";
import type { ActionDefinition } from "@harro/skill-sdk";
import { gql } from "../core/client.ts";

export const users: Record<string, ActionDefinition> = {
  list_users: {
    description: "List users in the workspace.",
    params: z.object({
      first: z.number().default(50).describe("Number of results to return"),
    }),
    returns: z.array(z.object({
      id: z.string().describe("User UUID"),
      name: z.string().describe("User name"),
      email: z.string().describe("User email"),
      display_name: z.string().describe("Display name"),
      active: z.boolean().describe("Whether the user is active"),
    })),
    execute: async (params, ctx) => {
      const data = await gql(ctx, `query($first: Int) { users(first: $first) { nodes { id name email displayName active } } }`, { first: params.first });
      return (data.users.nodes ?? []).map((u: any) => ({
        id: u.id,
        name: u.name,
        email: u.email,
        display_name: u.displayName,
        active: u.active,
      }));
    },
  },

  get_user: {
    description: "Get detailed user information.",
    params: z.object({
      user_id: z.string().describe("User UUID"),
    }),
    returns: z.object({
      id: z.string().describe("User UUID"),
      name: z.string().describe("User name"),
      email: z.string().describe("User email"),
      display_name: z.string().describe("Display name"),
      active: z.boolean().describe("Whether the user is active"),
      admin: z.boolean().describe("Whether the user is an admin"),
      created_at: z.string().describe("Creation date"),
    }),
    execute: async (params, ctx) => {
      const data = await gql(ctx, `query($id: String!) { user(id: $id) { id name email displayName active admin createdAt } }`, { id: params.user_id });
      const u = data.user;
      return { id: u.id, name: u.name, email: u.email, display_name: u.displayName, active: u.active, admin: u.admin, created_at: u.createdAt };
    },
  },

  me: {
    description: "Get the authenticated user's details.",
    params: z.object({}),
    returns: z.object({
      id: z.string().describe("User UUID"),
      name: z.string().describe("User name"),
      email: z.string().describe("User email"),
      display_name: z.string().describe("Display name"),
      active: z.boolean().describe("Whether the user is active"),
      teams: z.array(z.string()).describe("Team names"),
    }),
    execute: async (_params, ctx) => {
      const data = await gql(ctx, `query { viewer { id name email displayName active teams { nodes { name } } } }`);
      const u = data.viewer;
      return {
        id: u.id,
        name: u.name,
        email: u.email,
        display_name: u.displayName,
        active: u.active,
        teams: (u.teams?.nodes ?? []).map((t: any) => t.name),
      };
    },
  },
};
