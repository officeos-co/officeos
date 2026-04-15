import { z } from "@harro/skill-sdk";
import type { ActionDefinition } from "@harro/skill-sdk";
import { jiraGet, jiraPost, jiraPut, jiraDelete, jiraBase } from "../core/client.ts";

const CommentSchema = z.object({
  id: z.string(),
  author: z.string(),
  body: z.string(),
  created: z.string(),
  updated: z.string(),
});

function extractCommentText(body: unknown): string {
  if (typeof body === "string") return body;
  if (body && typeof body === "object") {
    const doc = body as Record<string, unknown>;
    const content = doc.content as Array<Record<string, unknown>> | undefined;
    if (content) {
      return content
        .flatMap((block) => (block.content as Array<Record<string, unknown>> | undefined) ?? [])
        .map((node) => (node.text as string) ?? "")
        .join(" ");
    }
  }
  return "";
}

export const comments: Record<string, ActionDefinition> = {
  list_comments: {
    description: "List comments on an issue.",
    params: z.object({
      issue_key: z.string().describe("Issue key or ID"),
      max_results: z.number().optional().describe("Max comments to return"),
    }),
    returns: z.array(CommentSchema),
    execute: async (params, ctx) => {
      const base = jiraBase(ctx);
      const qs: Record<string, string> = {};
      if (params.max_results) qs["maxResults"] = String(params.max_results);
      const data = await jiraGet(ctx, `${base}/issue/${encodeURIComponent(params.issue_key)}/comment`, qs);
      return (data.comments ?? []).map((c: Record<string, unknown>) => ({
        id: c.id as string,
        author: (c.author as Record<string, unknown>)?.displayName as string ?? "",
        body: extractCommentText(c.body),
        created: c.created as string,
        updated: c.updated as string,
      }));
    },
  },

  add_comment: {
    description: "Add a comment to an issue.",
    params: z.object({
      issue_key: z.string().describe("Issue key or ID"),
      body: z.string().describe("Comment text"),
    }),
    returns: z.object({
      id: z.string(),
      author: z.string(),
      body: z.string(),
      created: z.string(),
    }),
    execute: async (params, ctx) => {
      const base = jiraBase(ctx);
      const data = await jiraPost(ctx, `${base}/issue/${encodeURIComponent(params.issue_key)}/comment`, {
        body: {
          type: "doc",
          version: 1,
          content: [{ type: "paragraph", content: [{ type: "text", text: params.body }] }],
        },
      });
      return {
        id: data.id as string,
        author: (data.author as Record<string, unknown>)?.displayName as string ?? "",
        body: params.body,
        created: data.created as string,
      };
    },
  },

  update_comment: {
    description: "Update an existing comment.",
    params: z.object({
      issue_key: z.string().describe("Issue key or ID"),
      comment_id: z.string().describe("Comment ID"),
      body: z.string().describe("New comment text"),
    }),
    returns: z.object({
      id: z.string(),
      body: z.string(),
      updated: z.string(),
    }),
    execute: async (params, ctx) => {
      const base = jiraBase(ctx);
      const data = await jiraPut(
        ctx,
        `${base}/issue/${encodeURIComponent(params.issue_key)}/comment/${encodeURIComponent(params.comment_id)}`,
        {
          body: {
            type: "doc",
            version: 1,
            content: [{ type: "paragraph", content: [{ type: "text", text: params.body }] }],
          },
        },
      );
      return {
        id: data.id as string,
        body: params.body,
        updated: data.updated as string,
      };
    },
  },

  delete_comment: {
    description: "Delete a comment.",
    params: z.object({
      issue_key: z.string().describe("Issue key or ID"),
      comment_id: z.string().describe("Comment ID"),
    }),
    returns: z.object({ success: z.boolean() }),
    execute: async (params, ctx) => {
      const base = jiraBase(ctx);
      return jiraDelete(
        ctx,
        `${base}/issue/${encodeURIComponent(params.issue_key)}/comment/${encodeURIComponent(params.comment_id)}`,
      );
    },
  },
};
