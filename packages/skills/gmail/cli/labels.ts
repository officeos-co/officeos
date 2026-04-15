import { z } from "@harro/skill-sdk";
import type { ActionDefinition } from "@harro/skill-sdk";
import { gFetch, gPost, gDelete, GMAIL_API } from "../core/client.ts";

export const labels: Record<string, ActionDefinition> = {
  list_labels: {
    description: "List all labels in the mailbox.",
    params: z.object({}),
    returns: z.array(z.object({
      id: z.string().describe("Label ID"),
      name: z.string().describe("Label name"),
      type: z.string().describe("Label type (system or user)"),
      message_list_visibility: z.string().optional().describe("Message list visibility"),
      label_list_visibility: z.string().optional().describe("Label list visibility"),
    })),
    execute: async (_params, ctx) => {
      const res = await gFetch(ctx, `${GMAIL_API}/labels`);
      return (res.labels ?? []).map((l: any) => ({
        id: l.id,
        name: l.name,
        type: l.type,
        message_list_visibility: l.messageListVisibility,
        label_list_visibility: l.labelListVisibility,
      }));
    },
  },

  get_label: {
    description: "Get details about a specific label.",
    params: z.object({
      label_id: z.string().describe("Label ID"),
    }),
    returns: z.object({
      id: z.string().describe("Label ID"),
      name: z.string().describe("Label name"),
      type: z.string().describe("Label type"),
      messages_total: z.number().describe("Total messages"),
      messages_unread: z.number().describe("Unread messages"),
      threads_total: z.number().describe("Total threads"),
      threads_unread: z.number().describe("Unread threads"),
    }),
    execute: async (params, ctx) => {
      const l = await gFetch(ctx, `${GMAIL_API}/labels/${params.label_id}`);
      return {
        id: l.id,
        name: l.name,
        type: l.type,
        messages_total: l.messagesTotal ?? 0,
        messages_unread: l.messagesUnread ?? 0,
        threads_total: l.threadsTotal ?? 0,
        threads_unread: l.threadsUnread ?? 0,
      };
    },
  },

  create_label: {
    description: "Create a new label.",
    params: z.object({
      name: z.string().describe("Label name"),
    }),
    returns: z.object({
      id: z.string().describe("Label ID"),
      name: z.string().describe("Label name"),
    }),
    execute: async (params, ctx) => {
      const res = await gPost(ctx, `${GMAIL_API}/labels`, { name: params.name });
      return { id: res.id, name: res.name };
    },
  },

  update_label: {
    description: "Rename a label.",
    params: z.object({
      label_id: z.string().describe("Label ID"),
      name: z.string().describe("New label name"),
    }),
    returns: z.object({
      id: z.string().describe("Label ID"),
      name: z.string().describe("Label name"),
    }),
    execute: async (params, ctx) => {
      const res = await gFetch(ctx, `${GMAIL_API}/labels/${params.label_id}`, {
        method: "PATCH",
        body: JSON.stringify({ name: params.name }),
      });
      return { id: res.id, name: res.name };
    },
  },

  delete_label: {
    description: "Delete a label.",
    params: z.object({
      label_id: z.string().describe("Label ID to delete"),
    }),
    returns: z.object({ status: z.string().describe("Confirmation status") }),
    execute: async (params, ctx) => {
      await gDelete(ctx, `${GMAIL_API}/labels/${params.label_id}`);
      return { status: "deleted" };
    },
  },

  add_label: {
    description: "Add a label to a message.",
    params: z.object({
      message_id: z.string().describe("Message ID"),
      label_id: z.string().describe("Label ID to add"),
    }),
    returns: z.object({
      id: z.string().describe("Message ID"),
      label_ids: z.array(z.string()).describe("Label IDs"),
    }),
    execute: async (params, ctx) => {
      const res = await gPost(ctx, `${GMAIL_API}/messages/${params.message_id}/modify`, { addLabelIds: [params.label_id] });
      return { id: res.id, label_ids: res.labelIds ?? [] };
    },
  },

  remove_label: {
    description: "Remove a label from a message.",
    params: z.object({
      message_id: z.string().describe("Message ID"),
      label_id: z.string().describe("Label ID to remove"),
    }),
    returns: z.object({
      id: z.string().describe("Message ID"),
      label_ids: z.array(z.string()).describe("Label IDs"),
    }),
    execute: async (params, ctx) => {
      const res = await gPost(ctx, `${GMAIL_API}/messages/${params.message_id}/modify`, { removeLabelIds: [params.label_id] });
      return { id: res.id, label_ids: res.labelIds ?? [] };
    },
  },
};
