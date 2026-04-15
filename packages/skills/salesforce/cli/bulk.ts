import { z } from "@harro/skill-sdk";
import type { ActionDefinition } from "@harro/skill-sdk";
import { sfFetch, sfPost, enc } from "../core/client.ts";

async function createBulkJob(ctx: { fetch: typeof globalThis.fetch; credentials: Record<string, string> }, objectType: string, operation: string) {
  return sfPost(ctx, `/jobs/ingest`, {
    object: objectType,
    operation,
    contentType: "JSON",
  });
}

async function uploadBatch(ctx: { fetch: typeof globalThis.fetch; credentials: Record<string, string> }, jobId: string, records: unknown[]) {
  return ctx.fetch(
    `${ctx.credentials.instance_url}/services/data/v59.0/jobs/ingest/${jobId}/batches`,
    {
      method: "PUT",
      headers: {
        Authorization: `Bearer ${ctx.credentials.access_token}`,
        "Content-Type": "application/json",
      },
      body: JSON.stringify(records),
    },
  );
}

async function closeJob(ctx: { fetch: typeof globalThis.fetch; credentials: Record<string, string> }, jobId: string) {
  return sfFetch(ctx, `/jobs/ingest/${jobId}`, {
    method: "PATCH",
    body: JSON.stringify({ state: "UploadComplete" }),
  });
}

const BulkJobResult = z.object({
  job_id: z.string().describe("Bulk job ID"),
  state: z.string().describe("Job state"),
  numberRecordsProcessed: z.number().describe("Records processed"),
  numberRecordsFailed: z.number().describe("Records failed"),
  results: z.array(z.record(z.unknown())).describe("Per-record results"),
});

export const bulk: Record<string, ActionDefinition> = {
  bulk_insert: {
    description: "Bulk insert records using the Bulk API 2.0.",
    params: z.object({
      object_type: z.string().describe("Salesforce object API name"),
      records: z.array(z.record(z.unknown())).describe("List of record objects to insert"),
    }),
    returns: BulkJobResult,
    execute: async (params, ctx) => {
      const job = await createBulkJob(ctx, params.object_type, "insert");
      const jobId = job.id;
      await uploadBatch(ctx, jobId, params.records);
      const closed = await closeJob(ctx, jobId);
      return {
        job_id: jobId,
        state: closed.state ?? "UploadComplete",
        numberRecordsProcessed: closed.numberRecordsProcessed ?? 0,
        numberRecordsFailed: closed.numberRecordsFailed ?? 0,
        results: [],
      };
    },
  },

  bulk_update: {
    description: "Bulk update records using the Bulk API 2.0.",
    params: z.object({
      object_type: z.string().describe("Salesforce object API name"),
      records: z.array(z.record(z.unknown())).describe("List of records with Id and fields to update"),
    }),
    returns: BulkJobResult,
    execute: async (params, ctx) => {
      const job = await createBulkJob(ctx, params.object_type, "update");
      const jobId = job.id;
      await uploadBatch(ctx, jobId, params.records);
      const closed = await closeJob(ctx, jobId);
      return {
        job_id: jobId,
        state: closed.state ?? "UploadComplete",
        numberRecordsProcessed: closed.numberRecordsProcessed ?? 0,
        numberRecordsFailed: closed.numberRecordsFailed ?? 0,
        results: [],
      };
    },
  },

  bulk_delete: {
    description: "Bulk delete records using the Bulk API 2.0.",
    params: z.object({
      object_type: z.string().describe("Salesforce object API name"),
      record_ids: z.array(z.string()).describe("List of record IDs to delete"),
    }),
    returns: BulkJobResult,
    execute: async (params, ctx) => {
      const job = await createBulkJob(ctx, params.object_type, "delete");
      const jobId = job.id;
      const records = params.record_ids.map((id) => ({ Id: id }));
      await uploadBatch(ctx, jobId, records);
      const closed = await closeJob(ctx, jobId);
      return {
        job_id: jobId,
        state: closed.state ?? "UploadComplete",
        numberRecordsProcessed: closed.numberRecordsProcessed ?? 0,
        numberRecordsFailed: closed.numberRecordsFailed ?? 0,
        results: [],
      };
    },
  },
};
