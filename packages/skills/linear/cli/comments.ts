import { z } from "@harro/skill-sdk";
import type { ActionDefinition } from "@harro/skill-sdk";
import { gql } from "../core/client.ts";

export const comments: Record<string, ActionDefinition> = {
  list_comments: {
    description: "List comments on an issue.",
    params: z.object({
      issue_id: z.string().describe("Issue UUID to list comments for"),
    }),
    returns: z.array(z.object({
      id: z.string().describe("Comment UUID"),
      body: z.string().describe("Comment body"),
      user: z.string().nullable().describe("Author name"),
      created_at: z.string().describe("Creation date"),
    })),
    execute: async (params, ctx) => {
      const data = await gql(ctx, `query($id: String!) { issue(id: $id) { comments { nodes { id body user { name } createdAt } } } }`, { id: params.issue_id });
      return (data.issue.comments.nodes ?? []).map((c: any) => ({
        id: c.id,
        body: c.body,
        user: c.user?.name ?? null,
        created_at: c.createdAt,
      }));
    },
  },

  create_comment: {
    description: "Add a comment to an issue.",
    params: z.object({
      issue_id: z.string().describe("Issue UUID to comment on"),
      body: z.string().describe("Comment body (markdown)"),
    }),
    returns: z.object({
      id: z.string().describe("Comment UUID"),
      body: z.string().describe("Comment body"),
      created_at: z.string().describe("Creation date"),
    }),
    execute: async (params, ctx) => {
      const data = await gql(ctx, `mutation($input: CommentCreateInput!) { commentCreate(input: $input) { success comment { id body createdAt } } }`, { input: { issueId: params.issue_id, body: params.body } });
      const c = data.commentCreate.comment;
      return { id: c.id, body: c.body, created_at: c.createdAt };
    },
  },

  update_comment: {
    description: "Update an existing comment.",
    params: z.object({
      comment_id: z.string().describe("Comment UUID to update"),
      body: z.string().describe("New comment body"),
    }),
    returns: z.object({
      id: z.string().describe("Comment UUID"),
      body: z.string().describe("Updated body"),
      updated_at: z.string().describe("Update timestamp"),
    }),
    execute: async (params, ctx) => {
      const data = await gql(ctx, `mutation($id: String!, $input: CommentUpdateInput!) { commentUpdate(id: $id, input: $input) { success comment { id body updatedAt } } }`, { id: params.comment_id, input: { body: params.body } });
      const c = data.commentUpdate.comment;
      return { id: c.id, body: c.body, updated_at: c.updatedAt };
    },
  },

  delete_comment: {
    description: "Delete a comment.",
    params: z.object({
      comment_id: z.string().describe("Comment UUID to delete"),
    }),
    returns: z.object({ success: z.boolean().describe("Whether the deletion succeeded") }),
    execute: async (params, ctx) => {
      const data = await gql(ctx, `mutation($id: String!) { commentDelete(id: $id) { success } }`, { id: params.comment_id });
      return { success: data.commentDelete.success };
    },
  },
};
