import { z } from "@harro/skill-sdk";
import type { ActionDefinition } from "@harro/skill-sdk";
import { sfFetch, sfPost, enc } from "../core/client.ts";

export const records: Record<string, ActionDefinition> = {
  list_objects: {
    description: "List all SObject types available in the org.",
    params: z.object({}),
    returns: z.array(
      z.object({
        name: z.string().describe("API name"),
        label: z.string().describe("Display label"),
        keyPrefix: z.string().nullable().describe("ID prefix"),
        custom: z.boolean().describe("Whether this is a custom object"),
        queryable: z.boolean().describe("Whether SOQL queryable"),
        createable: z.boolean().describe("Whether records can be created"),
        updateable: z.boolean().describe("Whether records can be updated"),
        deletable: z.boolean().describe("Whether records can be deleted"),
      }),
    ),
    execute: async (_params, ctx) => {
      const data = await sfFetch(ctx, `/sobjects`);
      return (data.sobjects ?? []).map((o: any) => ({
        name: o.name,
        label: o.label,
        keyPrefix: o.keyPrefix ?? null,
        custom: o.custom,
        queryable: o.queryable,
        createable: o.createable,
        updateable: o.updateable,
        deletable: o.deletable,
      }));
    },
  },

  describe_object: {
    description: "Describe an SObject type, returning its fields, relationships, and metadata.",
    params: z.object({
      object_type: z.string().describe("API name (e.g. Contact, Account)"),
    }),
    returns: z.object({
      name: z.string().describe("API name"),
      label: z.string().describe("Display label"),
      fields: z.array(z.object({
        name: z.string().describe("Field API name"),
        label: z.string().describe("Field label"),
        type: z.string().describe("Field data type"),
        length: z.number().describe("Max length"),
        nillable: z.boolean().describe("Whether nullable"),
        createable: z.boolean().describe("Whether settable on create"),
        updateable: z.boolean().describe("Whether settable on update"),
        picklistValues: z.array(z.object({
          value: z.string(),
          label: z.string(),
          active: z.boolean(),
        })).describe("Picklist values if applicable"),
      })).describe("Object fields"),
      recordTypeInfos: z.array(z.record(z.unknown())).describe("Record type info"),
      childRelationships: z.array(z.record(z.unknown())).describe("Child relationships"),
    }),
    execute: async (params, ctx) => {
      const data = await sfFetch(ctx, `/sobjects/${enc(params.object_type)}/describe`);
      return {
        name: data.name,
        label: data.label,
        fields: (data.fields ?? []).map((f: any) => ({
          name: f.name,
          label: f.label,
          type: f.type,
          length: f.length,
          nillable: f.nillable,
          createable: f.createable,
          updateable: f.updateable,
          picklistValues: (f.picklistValues ?? []).map((p: any) => ({
            value: p.value,
            label: p.label,
            active: p.active,
          })),
        })),
        recordTypeInfos: data.recordTypeInfos ?? [],
        childRelationships: data.childRelationships ?? [],
      };
    },
  },

  get_record: {
    description: "Get a single record by ID.",
    params: z.object({
      object_type: z.string().describe("Salesforce object API name"),
      record_id: z.string().describe("Record ID (15 or 18 character)"),
      fields: z.array(z.string()).optional().describe("Fields to retrieve (default: all)"),
    }),
    returns: z.record(z.unknown()).describe("Record with requested fields"),
    execute: async (params, ctx) => {
      const fieldParam = params.fields?.length ? `?fields=${params.fields.join(",")}` : "";
      return sfFetch(ctx, `/sobjects/${enc(params.object_type)}/${enc(params.record_id)}${fieldParam}`);
    },
  },

  create_record: {
    description: "Create a record on any SObject type.",
    params: z.object({
      object_type: z.string().describe("Salesforce object API name"),
      fields: z.record(z.unknown()).describe("Field-value pairs to set"),
    }),
    returns: z.object({
      id: z.string().describe("New record ID"),
      success: z.boolean().describe("Whether creation succeeded"),
      errors: z.array(z.record(z.unknown())).describe("Error details if any"),
    }),
    execute: async (params, ctx) => {
      return sfPost(ctx, `/sobjects/${enc(params.object_type)}`, params.fields);
    },
  },

  update_record: {
    description: "Update fields on an existing record.",
    params: z.object({
      object_type: z.string().describe("Salesforce object API name"),
      record_id: z.string().describe("Record ID"),
      fields: z.record(z.unknown()).describe("Field-value pairs to update"),
    }),
    returns: z.object({
      id: z.string().describe("Record ID"),
      success: z.boolean().describe("Whether update succeeded"),
      errors: z.array(z.record(z.unknown())).describe("Error details if any"),
    }),
    execute: async (params, ctx) => {
      await sfFetch(ctx, `/sobjects/${enc(params.object_type)}/${enc(params.record_id)}`, {
        method: "PATCH",
        body: JSON.stringify(params.fields),
      });
      return { id: params.record_id, success: true, errors: [] };
    },
  },

  delete_record: {
    description: "Delete a record (moves to Recycle Bin).",
    params: z.object({
      object_type: z.string().describe("Salesforce object API name"),
      record_id: z.string().describe("Record ID"),
    }),
    returns: z.object({
      id: z.string().describe("Deleted record ID"),
      success: z.boolean().describe("Whether deletion succeeded"),
    }),
    execute: async (params, ctx) => {
      await sfFetch(ctx, `/sobjects/${enc(params.object_type)}/${enc(params.record_id)}`, {
        method: "DELETE",
      });
      return { id: params.record_id, success: true };
    },
  },

  upsert_record: {
    description: "Insert or update a record based on an external ID field.",
    params: z.object({
      object_type: z.string().describe("Salesforce object API name"),
      external_id_field: z.string().describe("External ID field name"),
      external_id_value: z.string().describe("External ID value to match"),
      fields: z.record(z.unknown()).describe("Field-value pairs to set"),
    }),
    returns: z.object({
      id: z.string().describe("Record ID"),
      success: z.boolean().describe("Whether upsert succeeded"),
      created: z.boolean().describe("True if inserted, false if updated"),
    }),
    execute: async (params, ctx) => {
      const data = await sfFetch(
        ctx,
        `/sobjects/${enc(params.object_type)}/${enc(params.external_id_field)}/${enc(params.external_id_value)}`,
        { method: "PATCH", body: JSON.stringify(params.fields) },
      );
      return { id: data.id ?? "", success: true, created: data.created ?? false };
    },
  },
};
