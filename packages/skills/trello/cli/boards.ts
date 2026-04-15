import { z } from "@harro/skill-sdk";
import type { ActionDefinition } from "@harro/skill-sdk";
import { trelloGet, trelloPost, trelloPut } from "../core/client.ts";

export const boards: Record<string, ActionDefinition> = {
  list_boards: {
    description: "List all boards for the authenticated user.",
    params: z.object({
      filter: z.enum(["all", "open", "closed", "starred"]).optional().describe("Board filter: all, open, closed, starred"),
    }),
    returns: z.array(z.object({
      id: z.string(),
      name: z.string(),
      desc: z.string(),
      url: z.string(),
      closed: z.boolean(),
    })),
    execute: async (params, ctx) => {
      const filter = params.filter ?? "open";
      const data = await trelloGet(ctx, `/members/me/boards`, { filter });
      return (Array.isArray(data) ? data : []).map((b: Record<string, unknown>) => ({
        id: b.id as string,
        name: b.name as string,
        desc: (b.desc as string) ?? "",
        url: b.url as string,
        closed: (b.closed as boolean) ?? false,
      }));
    },
  },

  get_board: {
    description: "Get details of a board.",
    params: z.object({
      board_id: z.string().describe("Board ID"),
    }),
    returns: z.object({
      id: z.string(),
      name: z.string(),
      desc: z.string(),
      url: z.string(),
      closed: z.boolean(),
    }),
    execute: async (params, ctx) => {
      const data = await trelloGet(ctx, `/boards/${encodeURIComponent(params.board_id)}`);
      return {
        id: data.id as string,
        name: data.name as string,
        desc: (data.desc as string) ?? "",
        url: data.url as string,
        closed: (data.closed as boolean) ?? false,
      };
    },
  },

  create_board: {
    description: "Create a new board.",
    params: z.object({
      name: z.string().describe("Board name"),
      desc: z.string().optional().describe("Board description"),
      default_lists: z.boolean().optional().describe("Create default To Do/Doing/Done lists"),
      id_organization: z.string().optional().describe("Organization ID to add board to"),
    }),
    returns: z.object({
      id: z.string(),
      name: z.string(),
      url: z.string(),
    }),
    execute: async (params, ctx) => {
      const body: Record<string, unknown> = {
        name: params.name,
        defaultLists: params.default_lists !== false,
      };
      if (params.desc) body["desc"] = params.desc;
      if (params.id_organization) body["idOrganization"] = params.id_organization;
      const data = await trelloPost(ctx, "/boards", body);
      return {
        id: data.id as string,
        name: data.name as string,
        url: data.url as string,
      };
    },
  },

  update_board: {
    description: "Update a board.",
    params: z.object({
      board_id: z.string().describe("Board ID"),
      name: z.string().optional().describe("New name"),
      desc: z.string().optional().describe("New description"),
      closed: z.boolean().optional().describe("Archive/unarchive the board"),
    }),
    returns: z.object({
      id: z.string(),
      name: z.string(),
    }),
    execute: async (params, ctx) => {
      const body: Record<string, unknown> = {};
      if (params.name) body["name"] = params.name;
      if (params.desc !== undefined) body["desc"] = params.desc;
      if (params.closed !== undefined) body["closed"] = params.closed;
      const data = await trelloPut(ctx, `/boards/${encodeURIComponent(params.board_id)}`, body);
      return { id: data.id as string, name: data.name as string };
    },
  },

  list_board_labels: {
    description: "List all labels on a board.",
    params: z.object({
      board_id: z.string().describe("Board ID"),
    }),
    returns: z.array(z.object({
      id: z.string(),
      name: z.string(),
      color: z.string().nullable(),
    })),
    execute: async (params, ctx) => {
      const data = await trelloGet(ctx, `/boards/${encodeURIComponent(params.board_id)}/labels`);
      return (Array.isArray(data) ? data : []).map((l: Record<string, unknown>) => ({
        id: l.id as string,
        name: (l.name as string) ?? "",
        color: (l.color as string) ?? null,
      }));
    },
  },

  create_label: {
    description: "Create a label on a board.",
    params: z.object({
      board_id: z.string().describe("Board ID"),
      name: z.string().describe("Label name"),
      color: z.string().describe("Color: red, orange, yellow, green, blue, purple"),
    }),
    returns: z.object({
      id: z.string(),
      name: z.string(),
      color: z.string(),
    }),
    execute: async (params, ctx) => {
      const data = await trelloPost(ctx, `/boards/${encodeURIComponent(params.board_id)}/labels`, {
        name: params.name,
        color: params.color,
      });
      return {
        id: data.id as string,
        name: data.name as string,
        color: data.color as string,
      };
    },
  },

  list_board_members: {
    description: "List members of a board.",
    params: z.object({
      board_id: z.string().describe("Board ID"),
    }),
    returns: z.array(z.object({
      id: z.string(),
      username: z.string(),
      full_name: z.string(),
    })),
    execute: async (params, ctx) => {
      const data = await trelloGet(ctx, `/boards/${encodeURIComponent(params.board_id)}/members`);
      return (Array.isArray(data) ? data : []).map((m: Record<string, unknown>) => ({
        id: m.id as string,
        username: m.username as string,
        full_name: (m.fullName as string) ?? "",
      }));
    },
  },
};
