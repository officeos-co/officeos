import { z } from "@harro/skill-sdk";
import type { ActionDefinition } from "@harro/skill-sdk";
import { doGet, doPost, doDelete, qs } from "../core/client.ts";

export const databases: Record<string, ActionDefinition> = {
  list_databases: {
    description: "List database clusters.",
    params: z.object({
      per_page: z.number().min(1).max(200).default(20).describe("Results per page (1-200)"),
    }),
    returns: z.array(z.object({
      id: z.string().describe("Database cluster ID"),
      name: z.string().describe("Cluster name"),
      engine: z.string().describe("Database engine"),
      version: z.string().describe("Engine version"),
      size: z.string().describe("Size slug"),
      region: z.string().describe("Region slug"),
      status: z.string().describe("Cluster status"),
      num_nodes: z.number().describe("Number of nodes"),
      created_at: z.string().describe("Creation timestamp"),
      connection_uri: z.string().nullable().describe("Public connection URI"),
      private_connection_uri: z.string().nullable().describe("Private connection URI"),
    })),
    execute: async (params, ctx) => {
      const data = await doGet(ctx, `/databases${qs({ per_page: params.per_page })}`);
      return (data.databases ?? []).map((d: any) => ({
        id: d.id,
        name: d.name,
        engine: d.engine,
        version: d.version,
        size: d.size,
        region: d.region,
        status: d.status,
        num_nodes: d.num_nodes,
        created_at: d.created_at,
        connection_uri: d.connection?.uri ?? null,
        private_connection_uri: d.private_connection?.uri ?? null,
      }));
    },
  },

  get_database: {
    description: "Get detailed info about a database cluster.",
    params: z.object({
      database_id: z.string().describe("Database cluster ID"),
    }),
    returns: z.object({
      id: z.string().describe("Database cluster ID"),
      name: z.string().describe("Cluster name"),
      engine: z.string().describe("Database engine"),
      version: z.string().describe("Engine version"),
      size: z.string().describe("Size slug"),
      region: z.string().describe("Region slug"),
      status: z.string().describe("Cluster status"),
      num_nodes: z.number().describe("Number of nodes"),
      created_at: z.string().describe("Creation timestamp"),
      connection: z.any().nullable().describe("Connection details"),
      private_connection: z.any().nullable().describe("Private connection details"),
      maintenance_window: z.any().nullable().describe("Maintenance window"),
      db_names: z.array(z.string()).describe("Database names"),
      users: z.array(z.object({ name: z.string(), role: z.string() })).describe("Database users"),
    }),
    execute: async (params, ctx) => {
      const data = await doGet(ctx, `/databases/${params.database_id}`);
      const d = data.database;
      return {
        id: d.id,
        name: d.name,
        engine: d.engine,
        version: d.version,
        size: d.size,
        region: d.region,
        status: d.status,
        num_nodes: d.num_nodes,
        created_at: d.created_at,
        connection: d.connection ?? null,
        private_connection: d.private_connection ?? null,
        maintenance_window: d.maintenance_window ?? null,
        db_names: (d.db_names ?? []),
        users: (d.users ?? []).map((u: any) => ({ name: u.name, role: u.role ?? "normal" })),
      };
    },
  },

  create_database: {
    description: "Create a new database cluster.",
    params: z.object({
      name: z.string().describe("Cluster name"),
      engine: z.string().describe("Engine: pg, mysql, redis, mongodb, kafka"),
      region: z.string().describe("Region slug"),
      size: z.string().describe("Size slug (e.g. db-s-1vcpu-1gb)"),
      num_nodes: z.number().min(1).max(3).default(1).describe("Number of nodes"),
      version: z.string().optional().describe("Engine version (e.g. 16 for Postgres 16)"),
      tags: z.array(z.string()).optional().describe("Tags to apply"),
    }),
    returns: z.object({
      id: z.string().describe("Database cluster ID"),
      name: z.string().describe("Cluster name"),
      engine: z.string().describe("Database engine"),
      status: z.string().describe("Cluster status"),
      connection_uri: z.string().nullable().describe("Connection URI"),
      created_at: z.string().describe("Creation timestamp"),
    }),
    execute: async (params, ctx) => {
      const body: any = {
        name: params.name,
        engine: params.engine,
        region: params.region,
        size: params.size,
        num_nodes: params.num_nodes,
      };
      if (params.version) body.version = params.version;
      if (params.tags) body.tags = params.tags;
      const data = await doPost(ctx, "/databases", body);
      const d = data.database;
      return {
        id: d.id,
        name: d.name,
        engine: d.engine,
        status: d.status,
        connection_uri: d.connection?.uri ?? null,
        created_at: d.created_at,
      };
    },
  },

  delete_database: {
    description: "Delete a database cluster permanently.",
    params: z.object({
      database_id: z.string().describe("Database cluster ID"),
    }),
    returns: z.object({
      deleted: z.boolean().describe("Whether deletion succeeded"),
    }),
    execute: async (params, ctx) => {
      await doDelete(ctx, `/databases/${params.database_id}`);
      return { deleted: true };
    },
  },

  list_connection_pools: {
    description: "List connection pools for a database cluster.",
    params: z.object({
      database_id: z.string().describe("Database cluster ID"),
    }),
    returns: z.array(z.object({
      name: z.string().describe("Pool name"),
      mode: z.string().describe("Pool mode"),
      size: z.number().describe("Pool size"),
      db: z.string().describe("Database name"),
      user: z.string().describe("Database user"),
      connection_uri: z.string().nullable().describe("Connection URI"),
    })),
    execute: async (params, ctx) => {
      const data = await doGet(ctx, `/databases/${params.database_id}/pools`);
      return (data.pools ?? []).map((p: any) => ({
        name: p.name,
        mode: p.mode,
        size: p.size,
        db: p.db,
        user: p.user,
        connection_uri: p.connection?.uri ?? null,
      }));
    },
  },

  create_connection_pool: {
    description: "Create a connection pool for a database cluster.",
    params: z.object({
      database_id: z.string().describe("Database cluster ID"),
      name: z.string().describe("Pool name"),
      mode: z.enum(["session", "transaction", "statement"]).default("transaction").describe("Pool mode"),
      size: z.number().describe("Pool size"),
      db: z.string().describe("Database name"),
      user: z.string().describe("Database user"),
    }),
    returns: z.object({
      name: z.string().describe("Pool name"),
      mode: z.string().describe("Pool mode"),
      size: z.number().describe("Pool size"),
      connection_uri: z.string().nullable().describe("Connection URI"),
    }),
    execute: async (params, ctx) => {
      const data = await doPost(ctx, `/databases/${params.database_id}/pools`, {
        name: params.name,
        mode: params.mode,
        size: params.size,
        db: params.db,
        user: params.user,
      });
      const p = data.pool;
      return {
        name: p.name,
        mode: p.mode,
        size: p.size,
        connection_uri: p.connection?.uri ?? null,
      };
    },
  },

  list_db_users: {
    description: "List users for a database cluster.",
    params: z.object({
      database_id: z.string().describe("Database cluster ID"),
    }),
    returns: z.array(z.object({
      name: z.string().describe("User name"),
      role: z.string().describe("User role"),
      password: z.string().nullable().describe("User password"),
    })),
    execute: async (params, ctx) => {
      const data = await doGet(ctx, `/databases/${params.database_id}/users`);
      return (data.users ?? []).map((u: any) => ({
        name: u.name,
        role: u.role ?? "normal",
        password: u.password ?? null,
      }));
    },
  },
};
