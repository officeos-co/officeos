import { z } from "@harro/skill-sdk";
import type { ActionDefinition } from "@harro/skill-sdk";
import { hsGet, hsPost, hsDelete } from "../core/client.ts";

const CrmObjectType = z.enum(["contacts", "companies", "deals", "tickets"]);

export const associations: Record<string, ActionDefinition> = {
  create_association: {
    description: "Create an association between two CRM objects.",
    params: z.object({
      from_object_type: CrmObjectType.describe("Source object type"),
      from_id: z.string().describe("Source object ID"),
      to_object_type: CrmObjectType.describe("Target object type"),
      to_id: z.string().describe("Target object ID"),
      association_type: z.string().describe("Association type label (e.g. contact_to_company)"),
    }),
    returns: z.object({
      from_id: z.string(),
      to_id: z.string(),
      association_type: z.string(),
    }),
    execute: async (params, ctx) => {
      await hsPost(
        ctx,
        `/crm/v3/objects/${params.from_object_type}/${params.from_id}/associations/${params.to_object_type}/${params.to_id}/${params.association_type}`,
        undefined,
        "PUT",
      );
      return {
        from_id: params.from_id,
        to_id: params.to_id,
        association_type: params.association_type,
      };
    },
  },

  list_associations: {
    description: "List associations from a CRM object to a target type.",
    params: z.object({
      object_type: CrmObjectType.describe("Source object type"),
      object_id: z.string().describe("Source object ID"),
      to_object_type: CrmObjectType.describe("Target object type"),
    }),
    returns: z.array(
      z.object({
        id: z.string(),
        type: z.string().describe("Association type label"),
      }),
    ),
    execute: async (params, ctx) => {
      const data = await hsGet(
        ctx,
        `/crm/v3/objects/${params.object_type}/${params.object_id}/associations/${params.to_object_type}`,
      );
      return (data.results ?? []).map((a: any) => ({
        id: a.id,
        type: a.type,
      }));
    },
  },

  remove_association: {
    description: "Remove an association between two CRM objects.",
    params: z.object({
      from_object_type: CrmObjectType.describe("Source object type"),
      from_id: z.string().describe("Source object ID"),
      to_object_type: CrmObjectType.describe("Target object type"),
      to_id: z.string().describe("Target object ID"),
      association_type: z.string().describe("Association type label"),
    }),
    returns: z.object({ success: z.boolean() }),
    execute: async (params, ctx) => {
      await hsDelete(
        ctx,
        `/crm/v3/objects/${params.from_object_type}/${params.from_id}/associations/${params.to_object_type}/${params.to_id}/${params.association_type}`,
      );
      return { success: true };
    },
  },
};
