import { z } from "@harro/skill-sdk";
import type { ActionDefinition } from "@harro/skill-sdk";
import { gfFetch, gfPost, gfDelete, enc } from "../core/client.ts";

export const folders: Record<string, ActionDefinition> = {
  list_folders: {
    description: "List all folders.",
    params: z.object({}),
    returns: z.array(z.object({ uid: z.string(), id: z.number(), title: z.string() })),
    execute: async (_params, ctx) => {
      const data = await gfFetch(ctx, "/folders");
      return data.map((f: any) => ({ uid: f.uid, id: f.id, title: f.title }));
    },
  },

  create_folder: {
    description: "Create a new folder.",
    params: z.object({
      title: z.string().describe("Folder title"),
      uid: z.string().optional().describe("Custom UID (auto-generated if omitted)"),
    }),
    returns: z.object({ uid: z.string(), id: z.number(), title: z.string() }),
    execute: async (params, ctx) => {
      const body: any = { title: params.title };
      if (params.uid) body.uid = params.uid;
      const data = await gfPost(ctx, "/folders", body);
      return { uid: data.uid, id: data.id, title: data.title };
    },
  },

  delete_folder: {
    description: "Delete a folder by UID.",
    params: z.object({ uid: z.string().describe("Folder UID") }),
    returns: z.object({ message: z.string() }),
    execute: async (params, ctx) => {
      const data = await gfDelete(ctx, `/folders/${enc(params.uid)}`);
      return { message: data.message ?? "Folder deleted" };
    },
  },
};
