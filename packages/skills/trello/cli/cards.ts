import { z } from "@harro/skill-sdk";
import type { ActionDefinition } from "@harro/skill-sdk";
import { trelloGet, trelloPost, trelloPut, trelloDelete } from "../core/client.ts";

const CardSchema = z.object({
  id: z.string(),
  name: z.string(),
  desc: z.string(),
  url: z.string(),
  due: z.string().nullable(),
  closed: z.boolean(),
  id_list: z.string(),
  pos: z.number(),
});

export const cards: Record<string, ActionDefinition> = {
  list_lists: {
    description: "Get all lists on a board.",
    params: z.object({
      board_id: z.string().describe("Board ID"),
      filter: z.enum(["all", "open", "closed"]).optional().describe("Filter: all, open, closed"),
    }),
    returns: z.array(z.object({
      id: z.string(),
      name: z.string(),
      closed: z.boolean(),
      pos: z.number(),
    })),
    execute: async (params, ctx) => {
      const filter = params.filter ?? "open";
      const data = await trelloGet(ctx, `/boards/${encodeURIComponent(params.board_id)}/lists`, { filter });
      return (Array.isArray(data) ? data : []).map((l: Record<string, unknown>) => ({
        id: l.id as string,
        name: l.name as string,
        closed: (l.closed as boolean) ?? false,
        pos: (l.pos as number) ?? 0,
      }));
    },
  },

  create_list: {
    description: "Create a new list on a board.",
    params: z.object({
      board_id: z.string().describe("Board ID"),
      name: z.string().describe("List name"),
    }),
    returns: z.object({
      id: z.string(),
      name: z.string(),
      pos: z.number(),
    }),
    execute: async (params, ctx) => {
      const data = await trelloPost(ctx, "/lists", {
        name: params.name,
        idBoard: params.board_id,
      });
      return {
        id: data.id as string,
        name: data.name as string,
        pos: (data.pos as number) ?? 0,
      };
    },
  },

  update_list: {
    description: "Update a list.",
    params: z.object({
      list_id: z.string().describe("List ID"),
      name: z.string().optional().describe("New name"),
      closed: z.boolean().optional().describe("Archive/unarchive"),
    }),
    returns: z.object({
      id: z.string(),
      name: z.string(),
    }),
    execute: async (params, ctx) => {
      const body: Record<string, unknown> = {};
      if (params.name) body["name"] = params.name;
      if (params.closed !== undefined) body["closed"] = params.closed;
      const data = await trelloPut(ctx, `/lists/${encodeURIComponent(params.list_id)}`, body);
      return { id: data.id as string, name: data.name as string };
    },
  },

  list_cards: {
    description: "Get all cards on a list or board.",
    params: z.object({
      list_id: z.string().optional().describe("List ID"),
      board_id: z.string().optional().describe("Board ID (alternative to list_id)"),
      filter: z.enum(["all", "open", "closed"]).optional().describe("Filter: all, open, closed"),
    }),
    returns: z.array(CardSchema),
    execute: async (params, ctx) => {
      const filter = params.filter ?? "open";
      let path: string;
      if (params.list_id) {
        path = `/lists/${encodeURIComponent(params.list_id)}/cards`;
      } else if (params.board_id) {
        path = `/boards/${encodeURIComponent(params.board_id)}/cards`;
      } else {
        throw new Error("Either list_id or board_id is required");
      }
      const data = await trelloGet(ctx, path, { filter });
      return (Array.isArray(data) ? data : []).map((c: Record<string, unknown>) => ({
        id: c.id as string,
        name: c.name as string,
        desc: (c.desc as string) ?? "",
        url: c.url as string,
        due: (c.due as string) ?? null,
        closed: (c.closed as boolean) ?? false,
        id_list: c.idList as string,
        pos: (c.pos as number) ?? 0,
      }));
    },
  },

  get_card: {
    description: "Get details of a card.",
    params: z.object({
      card_id: z.string().describe("Card ID"),
    }),
    returns: CardSchema,
    execute: async (params, ctx) => {
      const data = await trelloGet(ctx, `/cards/${encodeURIComponent(params.card_id)}`);
      return {
        id: data.id as string,
        name: data.name as string,
        desc: (data.desc as string) ?? "",
        url: data.url as string,
        due: (data.due as string) ?? null,
        closed: (data.closed as boolean) ?? false,
        id_list: data.idList as string,
        pos: (data.pos as number) ?? 0,
      };
    },
  },

  create_card: {
    description: "Create a new card.",
    params: z.object({
      list_id: z.string().describe("List ID to add card to"),
      name: z.string().describe("Card name"),
      desc: z.string().optional().describe("Card description"),
      due: z.string().optional().describe("Due date (ISO 8601)"),
      id_members: z.array(z.string()).optional().describe("Member IDs to assign"),
      id_labels: z.array(z.string()).optional().describe("Label IDs to attach"),
      pos: z.enum(["top", "bottom"]).optional().describe("Position: top or bottom"),
    }),
    returns: z.object({
      id: z.string(),
      name: z.string(),
      url: z.string(),
      id_list: z.string(),
    }),
    execute: async (params, ctx) => {
      const body: Record<string, unknown> = {
        idList: params.list_id,
        name: params.name,
        pos: params.pos ?? "bottom",
      };
      if (params.desc) body["desc"] = params.desc;
      if (params.due) body["due"] = params.due;
      if (params.id_members) body["idMembers"] = params.id_members.join(",");
      if (params.id_labels) body["idLabels"] = params.id_labels.join(",");
      const data = await trelloPost(ctx, "/cards", body);
      return {
        id: data.id as string,
        name: data.name as string,
        url: data.url as string,
        id_list: data.idList as string,
      };
    },
  },

  update_card: {
    description: "Update a card.",
    params: z.object({
      card_id: z.string().describe("Card ID"),
      name: z.string().optional().describe("New name"),
      desc: z.string().optional().describe("New description"),
      closed: z.boolean().optional().describe("Archive/unarchive"),
      id_list: z.string().optional().describe("Move to list (list ID)"),
      due: z.string().optional().describe("New due date (ISO 8601)"),
      due_complete: z.boolean().optional().describe("Mark due date complete"),
    }),
    returns: z.object({
      id: z.string(),
      name: z.string(),
      id_list: z.string(),
    }),
    execute: async (params, ctx) => {
      const body: Record<string, unknown> = {};
      if (params.name) body["name"] = params.name;
      if (params.desc !== undefined) body["desc"] = params.desc;
      if (params.closed !== undefined) body["closed"] = params.closed;
      if (params.id_list) body["idList"] = params.id_list;
      if (params.due !== undefined) body["due"] = params.due;
      if (params.due_complete !== undefined) body["dueComplete"] = params.due_complete;
      const data = await trelloPut(ctx, `/cards/${encodeURIComponent(params.card_id)}`, body);
      return {
        id: data.id as string,
        name: data.name as string,
        id_list: data.idList as string,
      };
    },
  },

  delete_card: {
    description: "Delete a card.",
    params: z.object({
      card_id: z.string().describe("Card ID"),
    }),
    returns: z.object({ success: z.boolean() }),
    execute: async (params, ctx) => {
      return trelloDelete(ctx, `/cards/${encodeURIComponent(params.card_id)}`);
    },
  },

  move_card: {
    description: "Move a card to a different list.",
    params: z.object({
      card_id: z.string().describe("Card ID"),
      list_id: z.string().describe("Destination list ID"),
      pos: z.enum(["top", "bottom"]).optional().describe("Position: top or bottom"),
    }),
    returns: z.object({
      id: z.string(),
      name: z.string(),
      id_list: z.string(),
    }),
    execute: async (params, ctx) => {
      const data = await trelloPut(ctx, `/cards/${encodeURIComponent(params.card_id)}`, {
        idList: params.list_id,
        pos: params.pos ?? "bottom",
      });
      return {
        id: data.id as string,
        name: data.name as string,
        id_list: data.idList as string,
      };
    },
  },

  list_card_comments: {
    description: "List comments on a card.",
    params: z.object({
      card_id: z.string().describe("Card ID"),
    }),
    returns: z.array(z.object({
      id: z.string(),
      text: z.string(),
      date: z.string(),
      member_creator: z.string().nullable(),
    })),
    execute: async (params, ctx) => {
      const data = await trelloGet(ctx, `/cards/${encodeURIComponent(params.card_id)}/actions`, {
        filter: "commentCard",
      });
      return (Array.isArray(data) ? data : []).map((a: Record<string, unknown>) => ({
        id: a.id as string,
        text: ((a.data as Record<string, unknown>)?.text as string) ?? "",
        date: a.date as string,
        member_creator: (a.memberCreator as Record<string, unknown>)?.username as string ?? null,
      }));
    },
  },

  add_card_comment: {
    description: "Add a comment to a card.",
    params: z.object({
      card_id: z.string().describe("Card ID"),
      text: z.string().describe("Comment text"),
    }),
    returns: z.object({
      id: z.string(),
      text: z.string(),
      date: z.string(),
    }),
    execute: async (params, ctx) => {
      const data = await trelloPost(ctx, `/cards/${encodeURIComponent(params.card_id)}/actions/comments`, {
        text: params.text,
      });
      return {
        id: data.id as string,
        text: (data.data as Record<string, unknown>)?.text as string ?? params.text,
        date: data.date as string,
      };
    },
  },

  delete_card_comment: {
    description: "Delete a comment on a card.",
    params: z.object({
      card_id: z.string().describe("Card ID"),
      comment_id: z.string().describe("Comment ID"),
    }),
    returns: z.object({ success: z.boolean() }),
    execute: async (params, ctx) => {
      return trelloDelete(
        ctx,
        `/cards/${encodeURIComponent(params.card_id)}/actions/${encodeURIComponent(params.comment_id)}/comments`,
      );
    },
  },

  list_card_checklists: {
    description: "List checklists on a card.",
    params: z.object({
      card_id: z.string().describe("Card ID"),
    }),
    returns: z.array(z.object({
      id: z.string(),
      name: z.string(),
      check_items: z.array(z.object({
        id: z.string(),
        name: z.string(),
        state: z.string(),
      })),
    })),
    execute: async (params, ctx) => {
      const data = await trelloGet(ctx, `/cards/${encodeURIComponent(params.card_id)}/checklists`);
      return (Array.isArray(data) ? data : []).map((cl: Record<string, unknown>) => ({
        id: cl.id as string,
        name: cl.name as string,
        check_items: ((cl.checkItems as Array<Record<string, unknown>>) ?? []).map((item) => ({
          id: item.id as string,
          name: item.name as string,
          state: item.state as string,
        })),
      }));
    },
  },

  create_checklist: {
    description: "Create a checklist on a card.",
    params: z.object({
      card_id: z.string().describe("Card ID"),
      name: z.string().describe("Checklist name"),
    }),
    returns: z.object({
      id: z.string(),
      name: z.string(),
    }),
    execute: async (params, ctx) => {
      const data = await trelloPost(ctx, "/checklists", {
        idCard: params.card_id,
        name: params.name,
      });
      return { id: data.id as string, name: data.name as string };
    },
  },

  add_checklist_item: {
    description: "Add an item to a checklist.",
    params: z.object({
      checklist_id: z.string().describe("Checklist ID"),
      name: z.string().describe("Item name"),
      checked: z.boolean().optional().describe("Initial checked state"),
    }),
    returns: z.object({
      id: z.string(),
      name: z.string(),
      state: z.string(),
    }),
    execute: async (params, ctx) => {
      const body: Record<string, unknown> = { name: params.name };
      if (params.checked !== undefined) body["checked"] = params.checked;
      const data = await trelloPost(
        ctx,
        `/checklists/${encodeURIComponent(params.checklist_id)}/checkItems`,
        body,
      );
      return {
        id: data.id as string,
        name: data.name as string,
        state: (data.state as string) ?? "incomplete",
      };
    },
  },
};
