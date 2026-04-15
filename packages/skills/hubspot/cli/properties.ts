import { z } from "@harro/skill-sdk";
import type { ActionDefinition } from "@harro/skill-sdk";
import { hsGet, hsPost } from "../core/client.ts";

const CrmObjectType = z.enum(["contacts", "companies", "deals", "tickets"]);

export const properties: Record<string, ActionDefinition> = {
  list_properties: {
    description: "List all properties for a CRM object type.",
    params: z.object({
      object_type: CrmObjectType.describe("Object type"),
    }),
    returns: z.array(
      z.object({
        name: z.string(),
        label: z.string(),
        type: z.string(),
        fieldType: z.string(),
        groupName: z.string(),
        description: z.string().nullable(),
      }),
    ),
    execute: async (params, ctx) => {
      const data = await hsGet(ctx, `/crm/v3/properties/${params.object_type}`);
      return (data.results ?? []).map((p: any) => ({
        name: p.name,
        label: p.label,
        type: p.type,
        fieldType: p.fieldType,
        groupName: p.groupName,
        description: p.description ?? null,
      }));
    },
  },

  create_property: {
    description: "Create a custom property for a CRM object type.",
    params: z.object({
      object_type: CrmObjectType.describe("Object type"),
      name: z.string().describe("Internal property name (no spaces, lowercase)"),
      label: z.string().describe("Display label"),
      type: z.enum(["string", "number", "date", "datetime", "enumeration"]).describe("Property data type"),
      field_type: z.enum(["text", "textarea", "number", "select", "checkbox", "date"]).describe("Field type"),
      group_name: z.string().describe("Property group name"),
      description: z.string().optional().describe("Property description"),
      options: z.array(z.any()).optional().describe("Options for enumeration type"),
    }),
    returns: z.object({
      name: z.string(),
      label: z.string(),
      type: z.string(),
      fieldType: z.string(),
      groupName: z.string(),
    }),
    execute: async (params, ctx) => {
      const body: Record<string, unknown> = {
        name: params.name,
        label: params.label,
        type: params.type,
        fieldType: params.field_type,
        groupName: params.group_name,
      };
      if (params.description) body.description = params.description;
      if (params.options) body.options = params.options;
      const p = await hsPost(ctx, `/crm/v3/properties/${params.object_type}`, body);
      return {
        name: p.name,
        label: p.label,
        type: p.type,
        fieldType: p.fieldType,
        groupName: p.groupName,
      };
    },
  },
};
