import { defineSkill, z } from "@harro/skill-sdk";
import doc from "./SKILL.md";

type Ctx = { fetch: typeof globalThis.fetch; credentials: Record<string, string> };

async function proxyOp(ctx: Ctx, operation: string, collection?: string, params?: Record<string, unknown>) {
  const res = await ctx.fetch(`${ctx.credentials.proxy_url}/query`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({
      database: ctx.credentials.database,
      operation,
      collection,
      params: params ?? {},
    }),
  });
  if (!res.ok) {
    const body = await res.text();
    throw new Error(`MongoDB proxy ${res.status}: ${body}`);
  }
  return res.json();
}

export default defineSkill({
  name: "mongodb",
  title: "MongoDB",
  logo: "<svg viewBox=\"0 0 24 24\" xmlns=\"http://www.w3.org/2000/svg\"><path d=\"M17.193 9.555c-1.264-5.58-4.252-7.414-4.573-8.115-.28-.394-.53-.954-.735-1.44-.036.495-.055.685-.523 1.184-.723.566-4.438 3.682-4.74 10.02-.282 5.912 4.27 9.435 4.888 9.884l.07.05A73.49 73.49 0 0111.91 24h.481c.114-1.032.284-2.056.51-3.07.417-.296.604-.463.85-.693a11.342 11.342 0 003.639-8.464c.01-.814-.103-1.662-.197-2.218zm-5.336 8.195s0-8.291.275-8.29c.213 0 .49 10.695.49 10.695-.381-.045-.765-1.76-.765-2.405z\"/></svg>",
  description:
    "Full MongoDB database management: connect, manage collections, CRUD documents, run aggregation pipelines, manage indexes, and inspect databases via a backend proxy.",
  doc,

  credentials: {
    proxy_url: {
      label: "Proxy URL",
      kind: "text",
      placeholder: "https://api.officeos.co/mongodb",
      help: "Backend proxy endpoint for MongoDB operations.",
    },
    database: {
      label: "Database",
      kind: "text",
      placeholder: "myapp",
      help: "Default database name.",
    },
  },

  actions: {
    // ── Connection ────────────────────────────────────────────────────────

    connect: {
      description: "Test connection to a MongoDB database.",
      params: z.object({
        uri: z.string().default("mongodb://localhost:27017").describe("MongoDB connection URI"),
        database: z.string().describe("Database name to use"),
      }),
      returns: z.object({
        connected: z.boolean(),
        version: z.string(),
        database: z.string(),
      }),
      execute: async (params, ctx) => {
        const data = await proxyOp(ctx, "connect", undefined, {
          uri: params.uri,
          database: params.database,
        });
        return {
          connected: true,
          version: data.version ?? "unknown",
          database: params.database,
        };
      },
    },

    // ── Collections ──────────────────────────────────────────────────────

    list_collections: {
      description: "List all collections in the database.",
      params: z.object({}),
      returns: z.array(
        z.object({
          name: z.string(),
          type: z.string(),
        }),
      ),
      execute: async (_params, ctx) => {
        const data = await proxyOp(ctx, "listCollections");
        return data.collections ?? [];
      },
    },

    create_collection: {
      description: "Create a new collection.",
      params: z.object({
        name: z.string().describe("New collection name"),
      }),
      returns: z.object({
        created: z.boolean(),
        name: z.string(),
      }),
      execute: async (params, ctx) => {
        await proxyOp(ctx, "createCollection", undefined, { name: params.name });
        return { created: true, name: params.name };
      },
    },

    drop_collection: {
      description: "Drop a collection.",
      params: z.object({
        collection: z.string().describe("Collection name to drop"),
      }),
      returns: z.object({
        dropped: z.boolean(),
        collection: z.string(),
      }),
      execute: async (params, ctx) => {
        await proxyOp(ctx, "dropCollection", params.collection);
        return { dropped: true, collection: params.collection };
      },
    },

    collection_stats: {
      description: "Get statistics for a collection.",
      params: z.object({
        collection: z.string().describe("Collection to inspect"),
      }),
      returns: z.object({
        ns: z.string(),
        count: z.number(),
        size: z.number(),
        avgObjSize: z.number(),
        storageSize: z.number(),
        nindexes: z.number(),
        totalIndexSize: z.number(),
      }),
      execute: async (params, ctx) => {
        const data = await proxyOp(ctx, "collStats", params.collection);
        return {
          ns: data.ns ?? "",
          count: data.count ?? 0,
          size: data.size ?? 0,
          avgObjSize: data.avgObjSize ?? 0,
          storageSize: data.storageSize ?? 0,
          nindexes: data.nindexes ?? 0,
          totalIndexSize: data.totalIndexSize ?? 0,
        };
      },
    },

    // ── CRUD ─────────────────────────────────────────────────────────────

    find: {
      description: "Find documents in a collection.",
      params: z.object({
        collection: z.string().describe("Collection to query"),
        filter: z.string().default("{}").describe("JSON query filter"),
        projection: z.string().optional().describe("JSON projection (fields to include/exclude)"),
        sort: z.string().optional().describe("JSON sort specification (e.g. {\"name\": 1})"),
        limit: z.number().default(20).describe("Maximum documents to return"),
        skip: z.number().default(0).describe("Number of documents to skip"),
      }),
      returns: z.object({
        documents: z.array(z.record(z.unknown())),
        count: z.number(),
      }),
      execute: async (params, ctx) => {
        const data = await proxyOp(ctx, "find", params.collection, {
          filter: JSON.parse(params.filter),
          projection: params.projection ? JSON.parse(params.projection) : undefined,
          sort: params.sort ? JSON.parse(params.sort) : undefined,
          limit: params.limit,
          skip: params.skip,
        });
        return {
          documents: data.documents ?? [],
          count: data.documents?.length ?? 0,
        };
      },
    },

    find_one: {
      description: "Find a single document in a collection.",
      params: z.object({
        collection: z.string().describe("Collection to query"),
        filter: z.string().describe("JSON query filter"),
        projection: z.string().optional().describe("JSON projection (fields to include/exclude)"),
      }),
      returns: z.record(z.unknown()).nullable(),
      execute: async (params, ctx) => {
        const data = await proxyOp(ctx, "findOne", params.collection, {
          filter: JSON.parse(params.filter),
          projection: params.projection ? JSON.parse(params.projection) : undefined,
        });
        return data.document ?? null;
      },
    },

    insert_one: {
      description: "Insert a single document into a collection.",
      params: z.object({
        collection: z.string().describe("Target collection"),
        document: z.string().describe("JSON document to insert"),
      }),
      returns: z.object({
        inserted_id: z.string(),
        acknowledged: z.boolean(),
      }),
      execute: async (params, ctx) => {
        const data = await proxyOp(ctx, "insertOne", params.collection, {
          document: JSON.parse(params.document),
        });
        return {
          inserted_id: String(data.insertedId ?? ""),
          acknowledged: data.acknowledged ?? true,
        };
      },
    },

    insert_many: {
      description: "Insert multiple documents into a collection.",
      params: z.object({
        collection: z.string().describe("Target collection"),
        documents: z.string().describe("JSON array of documents to insert"),
      }),
      returns: z.object({
        inserted_ids: z.array(z.string()),
        inserted_count: z.number(),
        acknowledged: z.boolean(),
      }),
      execute: async (params, ctx) => {
        const data = await proxyOp(ctx, "insertMany", params.collection, {
          documents: JSON.parse(params.documents),
        });
        return {
          inserted_ids: (data.insertedIds ?? []).map(String),
          inserted_count: data.insertedCount ?? 0,
          acknowledged: data.acknowledged ?? true,
        };
      },
    },

    update_one: {
      description: "Update a single document in a collection.",
      params: z.object({
        collection: z.string().describe("Target collection"),
        filter: z.string().describe("JSON query filter to match one document"),
        update: z.string().describe("JSON update operators (e.g. {\"$set\": {}})"),
        upsert: z.boolean().default(false).describe("Insert if no document matches"),
      }),
      returns: z.object({
        matched_count: z.number(),
        modified_count: z.number(),
        upserted_id: z.string().nullable(),
        acknowledged: z.boolean(),
      }),
      execute: async (params, ctx) => {
        const data = await proxyOp(ctx, "updateOne", params.collection, {
          filter: JSON.parse(params.filter),
          update: JSON.parse(params.update),
          upsert: params.upsert,
        });
        return {
          matched_count: data.matchedCount ?? 0,
          modified_count: data.modifiedCount ?? 0,
          upserted_id: data.upsertedId ? String(data.upsertedId) : null,
          acknowledged: data.acknowledged ?? true,
        };
      },
    },

    update_many: {
      description: "Update multiple documents in a collection.",
      params: z.object({
        collection: z.string().describe("Target collection"),
        filter: z.string().describe("JSON query filter"),
        update: z.string().describe("JSON update operators (e.g. {\"$set\": {}})"),
        upsert: z.boolean().default(false).describe("Insert if no document matches"),
      }),
      returns: z.object({
        matched_count: z.number(),
        modified_count: z.number(),
        upserted_id: z.string().nullable(),
        acknowledged: z.boolean(),
      }),
      execute: async (params, ctx) => {
        const data = await proxyOp(ctx, "updateMany", params.collection, {
          filter: JSON.parse(params.filter),
          update: JSON.parse(params.update),
          upsert: params.upsert,
        });
        return {
          matched_count: data.matchedCount ?? 0,
          modified_count: data.modifiedCount ?? 0,
          upserted_id: data.upsertedId ? String(data.upsertedId) : null,
          acknowledged: data.acknowledged ?? true,
        };
      },
    },

    replace_one: {
      description: "Replace a single document in a collection.",
      params: z.object({
        collection: z.string().describe("Target collection"),
        filter: z.string().describe("JSON query filter to match one document"),
        replacement: z.string().describe("JSON replacement document (no update operators)"),
        upsert: z.boolean().default(false).describe("Insert if no document matches"),
      }),
      returns: z.object({
        matched_count: z.number(),
        modified_count: z.number(),
        upserted_id: z.string().nullable(),
        acknowledged: z.boolean(),
      }),
      execute: async (params, ctx) => {
        const data = await proxyOp(ctx, "replaceOne", params.collection, {
          filter: JSON.parse(params.filter),
          replacement: JSON.parse(params.replacement),
          upsert: params.upsert,
        });
        return {
          matched_count: data.matchedCount ?? 0,
          modified_count: data.modifiedCount ?? 0,
          upserted_id: data.upsertedId ? String(data.upsertedId) : null,
          acknowledged: data.acknowledged ?? true,
        };
      },
    },

    delete_one: {
      description: "Delete a single document from a collection.",
      params: z.object({
        collection: z.string().describe("Target collection"),
        filter: z.string().describe("JSON query filter to match one document"),
      }),
      returns: z.object({
        deleted_count: z.number(),
        acknowledged: z.boolean(),
      }),
      execute: async (params, ctx) => {
        const data = await proxyOp(ctx, "deleteOne", params.collection, {
          filter: JSON.parse(params.filter),
        });
        return {
          deleted_count: data.deletedCount ?? 0,
          acknowledged: data.acknowledged ?? true,
        };
      },
    },

    delete_many: {
      description: "Delete multiple documents from a collection.",
      params: z.object({
        collection: z.string().describe("Target collection"),
        filter: z.string().describe("JSON query filter"),
      }),
      returns: z.object({
        deleted_count: z.number(),
        acknowledged: z.boolean(),
      }),
      execute: async (params, ctx) => {
        const data = await proxyOp(ctx, "deleteMany", params.collection, {
          filter: JSON.parse(params.filter),
        });
        return {
          deleted_count: data.deletedCount ?? 0,
          acknowledged: data.acknowledged ?? true,
        };
      },
    },

    // ── Aggregation ──────────────────────────────────────────────────────

    aggregate: {
      description: "Run an aggregation pipeline on a collection.",
      params: z.object({
        collection: z.string().describe("Collection to aggregate"),
        pipeline: z.string().describe("JSON array of aggregation stages"),
      }),
      returns: z.object({
        documents: z.array(z.record(z.unknown())),
        count: z.number(),
      }),
      execute: async (params, ctx) => {
        const data = await proxyOp(ctx, "aggregate", params.collection, {
          pipeline: JSON.parse(params.pipeline),
        });
        return {
          documents: data.documents ?? [],
          count: data.documents?.length ?? 0,
        };
      },
    },

    // ── Indexes ──────────────────────────────────────────────────────────

    list_indexes: {
      description: "List indexes on a collection.",
      params: z.object({
        collection: z.string().describe("Collection to list indexes for"),
      }),
      returns: z.array(
        z.object({
          name: z.string(),
          key: z.record(z.number()),
          unique: z.boolean(),
          sparse: z.boolean(),
        }),
      ),
      execute: async (params, ctx) => {
        const data = await proxyOp(ctx, "listIndexes", params.collection);
        return (data.indexes ?? []).map((idx: Record<string, unknown>) => ({
          name: idx.name ?? "",
          key: idx.key ?? {},
          unique: idx.unique ?? false,
          sparse: idx.sparse ?? false,
        }));
      },
    },

    create_index: {
      description: "Create an index on a collection.",
      params: z.object({
        collection: z.string().describe("Collection to index"),
        keys: z.string().describe("JSON index key specification (e.g. {\"email\": 1})"),
        unique: z.boolean().default(false).describe("Create a unique index"),
        sparse: z.boolean().default(false).describe("Only index documents with the field"),
        name: z.string().optional().describe("Custom index name"),
        ttl: z.number().optional().describe("TTL in seconds (for expiring documents)"),
      }),
      returns: z.object({
        created: z.boolean(),
        index_name: z.string(),
      }),
      execute: async (params, ctx) => {
        const options: Record<string, unknown> = {
          keys: JSON.parse(params.keys),
          unique: params.unique,
          sparse: params.sparse,
        };
        if (params.name) options.name = params.name;
        if (params.ttl !== undefined) options.expireAfterSeconds = params.ttl;
        const data = await proxyOp(ctx, "createIndex", params.collection, options);
        return {
          created: true,
          index_name: data.indexName ?? params.name ?? "",
        };
      },
    },

    drop_index: {
      description: "Drop an index from a collection.",
      params: z.object({
        collection: z.string().describe("Collection name"),
        name: z.string().describe("Index name to drop"),
      }),
      returns: z.object({
        dropped: z.boolean(),
        index_name: z.string(),
      }),
      execute: async (params, ctx) => {
        await proxyOp(ctx, "dropIndex", params.collection, { name: params.name });
        return { dropped: true, index_name: params.name };
      },
    },

    // ── Database ─────────────────────────────────────────────────────────

    list_databases: {
      description: "List all databases on the server.",
      params: z.object({}),
      returns: z.array(
        z.object({
          name: z.string(),
          sizeOnDisk: z.number(),
          empty: z.boolean(),
        }),
      ),
      execute: async (_params, ctx) => {
        const data = await proxyOp(ctx, "listDatabases");
        return data.databases ?? [];
      },
    },

    db_stats: {
      description: "Get statistics for the current database.",
      params: z.object({}),
      returns: z.object({
        db: z.string(),
        collections: z.number(),
        views: z.number(),
        objects: z.number(),
        avgObjSize: z.number(),
        dataSize: z.number(),
        storageSize: z.number(),
        indexes: z.number(),
        indexSize: z.number(),
      }),
      execute: async (_params, ctx) => {
        const data = await proxyOp(ctx, "dbStats");
        return {
          db: data.db ?? "",
          collections: data.collections ?? 0,
          views: data.views ?? 0,
          objects: data.objects ?? 0,
          avgObjSize: data.avgObjSize ?? 0,
          dataSize: data.dataSize ?? 0,
          storageSize: data.storageSize ?? 0,
          indexes: data.indexes ?? 0,
          indexSize: data.indexSize ?? 0,
        };
      },
    },

    drop_database: {
      description: "Drop the current database.",
      params: z.object({}),
      returns: z.object({
        dropped: z.boolean(),
        database: z.string(),
      }),
      execute: async (_params, ctx) => {
        await proxyOp(ctx, "dropDatabase");
        return { dropped: true, database: ctx.credentials.database };
      },
    },

    // ── Count ────────────────────────────────────────────────────────────

    count_documents: {
      description: "Count documents matching a filter.",
      params: z.object({
        collection: z.string().describe("Collection name"),
        filter: z.string().default("{}").describe("JSON query filter"),
      }),
      returns: z.object({
        count: z.number(),
      }),
      execute: async (params, ctx) => {
        const data = await proxyOp(ctx, "countDocuments", params.collection, {
          filter: JSON.parse(params.filter),
        });
        return { count: data.count ?? 0 };
      },
    },

    estimated_count: {
      description: "Get an estimated document count for a collection.",
      params: z.object({
        collection: z.string().describe("Collection name"),
      }),
      returns: z.object({
        count: z.number(),
      }),
      execute: async (params, ctx) => {
        const data = await proxyOp(ctx, "estimatedDocumentCount", params.collection);
        return { count: data.count ?? 0 };
      },
    },

    // ── Distinct ─────────────────────────────────────────────────────────

    distinct: {
      description: "Get distinct values for a field.",
      params: z.object({
        collection: z.string().describe("Collection name"),
        field: z.string().describe("Field to get distinct values for"),
        filter: z.string().default("{}").describe("JSON query filter"),
      }),
      returns: z.object({
        values: z.array(z.unknown()),
        count: z.number(),
      }),
      execute: async (params, ctx) => {
        const data = await proxyOp(ctx, "distinct", params.collection, {
          field: params.field,
          filter: JSON.parse(params.filter),
        });
        const values = data.values ?? [];
        return { values, count: values.length };
      },
    },

    // ── Bulk ─────────────────────────────────────────────────────────────

    bulk_write: {
      description: "Execute multiple write operations in bulk.",
      params: z.object({
        collection: z.string().describe("Target collection"),
        operations: z.string().describe("JSON array of bulk write operations"),
        ordered: z.boolean().default(true).describe("Execute operations in order (stop on error)"),
      }),
      returns: z.object({
        inserted_count: z.number(),
        matched_count: z.number(),
        modified_count: z.number(),
        deleted_count: z.number(),
        upserted_count: z.number(),
        acknowledged: z.boolean(),
      }),
      execute: async (params, ctx) => {
        const data = await proxyOp(ctx, "bulkWrite", params.collection, {
          operations: JSON.parse(params.operations),
          ordered: params.ordered,
        });
        return {
          inserted_count: data.insertedCount ?? 0,
          matched_count: data.matchedCount ?? 0,
          modified_count: data.modifiedCount ?? 0,
          deleted_count: data.deletedCount ?? 0,
          upserted_count: data.upsertedCount ?? 0,
          acknowledged: data.acknowledged ?? true,
        };
      },
    },
  },
});
