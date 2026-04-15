import { z } from "@harro/skill-sdk";
import type { ActionDefinition } from "@harro/skill-sdk";
import { gql } from "../core/client.ts";

export const roadmap: Record<string, ActionDefinition> = {
  list_roadmaps: {
    description: "List roadmaps.",
    params: z.object({
      first: z.number().default(20).describe("Number of results to return"),
    }),
    returns: z.array(z.object({
      id: z.string().describe("Roadmap UUID"),
      name: z.string().describe("Roadmap name"),
      description: z.string().nullable().describe("Roadmap description"),
      slug: z.string().describe("Roadmap slug"),
    })),
    execute: async (params, ctx) => {
      const data = await gql(ctx, `query($first: Int) { roadmaps(first: $first) { nodes { id name description slugId } } }`, { first: params.first });
      return (data.roadmaps.nodes ?? []).map((r: any) => ({
        id: r.id,
        name: r.name,
        description: r.description ?? null,
        slug: r.slugId,
      }));
    },
  },

  get_roadmap: {
    description: "Get detailed roadmap information.",
    params: z.object({
      roadmap_id: z.string().describe("Roadmap UUID"),
    }),
    returns: z.object({
      id: z.string().describe("Roadmap UUID"),
      name: z.string().describe("Roadmap name"),
      description: z.string().nullable().describe("Roadmap description"),
      slug: z.string().describe("Roadmap slug"),
      projects: z.array(z.string()).describe("Project names in the roadmap"),
    }),
    execute: async (params, ctx) => {
      const data = await gql(ctx, `query($id: String!) { roadmap(id: $id) { id name description slugId projects { nodes { name } } } }`, { id: params.roadmap_id });
      const r = data.roadmap;
      return {
        id: r.id,
        name: r.name,
        description: r.description ?? null,
        slug: r.slugId,
        projects: (r.projects?.nodes ?? []).map((p: any) => p.name),
      };
    },
  },

  create_attachment: {
    description: "Create an attachment on an issue.",
    params: z.object({
      issue_id: z.string().describe("Issue UUID to attach to"),
      url: z.string().describe("URL of the attachment"),
      title: z.string().describe("Display title"),
    }),
    returns: z.object({
      id: z.string().describe("Attachment UUID"),
      url: z.string().describe("Attachment URL"),
      title: z.string().describe("Attachment title"),
      created_at: z.string().describe("Creation date"),
    }),
    execute: async (params, ctx) => {
      const data = await gql(ctx, `mutation($input: AttachmentCreateInput!) { attachmentCreate(input: $input) { success attachment { id url title createdAt } } }`, { input: { issueId: params.issue_id, url: params.url, title: params.title } });
      const a = data.attachmentCreate.attachment;
      return { id: a.id, url: a.url, title: a.title, created_at: a.createdAt };
    },
  },
};
