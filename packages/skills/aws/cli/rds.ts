import { z } from "@harro/skill-sdk";
import type { ActionDefinition } from "@harro/skill-sdk";
import { rdsQuery, xmlVal, xmlAll, xmlBlocks, resolveRegion } from "../core/client.ts";

export const rds: Record<string, ActionDefinition> = {
  list_db_instances: {
    description: "List RDS database instances.",
    params: z.object({
      engine: z.string().optional().describe("Filter by engine: postgres, mysql, aurora, mariadb"),
      region: z.string().optional().describe("AWS region"),
    }),
    returns: z.array(z.object({
      db_instance_id: z.string().describe("DB instance identifier"),
      engine: z.string().describe("Database engine"),
      engine_version: z.string().describe("Engine version"),
      instance_class: z.string().describe("Instance class"),
      status: z.string().describe("Instance status"),
      endpoint: z.string().nullable().describe("Connection endpoint"),
      port: z.number().describe("Port"),
      storage_gb: z.number().describe("Allocated storage in GB"),
      multi_az: z.boolean().describe("Multi-AZ deployment"),
      vpc_id: z.string().nullable().describe("VPC ID"),
    })),
    execute: async (params, ctx) => {
      const region = resolveRegion(ctx, params.region);
      const p: Record<string, string> = { Action: "DescribeDBInstances" };
      if (params.engine) {
        p["Filters.member.1.Name"] = "engine";
        p["Filters.member.1.Values.member.1"] = params.engine;
      }
      const xml = await rdsQuery(ctx, region, p);
      return xmlBlocks(xml, "DBInstance").map((b) => ({
        db_instance_id: xmlVal(b, "DBInstanceIdentifier"),
        engine: xmlVal(b, "Engine"),
        engine_version: xmlVal(b, "EngineVersion"),
        instance_class: xmlVal(b, "DBInstanceClass"),
        status: xmlVal(b, "DBInstanceStatus"),
        endpoint: xmlVal(b, "Address") || null,
        port: parseInt(xmlVal(b, "Port") || "0", 10),
        storage_gb: parseInt(xmlVal(b, "AllocatedStorage") || "0", 10),
        multi_az: xmlVal(b, "MultiAZ") === "true",
        vpc_id: xmlVal(b, "VpcId") || null,
      }));
    },
  },

  describe_db_instance: {
    description: "Get detailed info about an RDS database instance.",
    params: z.object({
      db_instance_id: z.string().describe("DB instance identifier"),
      region: z.string().optional().describe("AWS region"),
    }),
    returns: z.object({
      db_instance_id: z.string().describe("DB instance identifier"),
      engine: z.string().describe("Database engine"),
      engine_version: z.string().describe("Engine version"),
      instance_class: z.string().describe("Instance class"),
      status: z.string().describe("Instance status"),
      endpoint: z.string().nullable().describe("Connection endpoint"),
      port: z.number().describe("Port"),
      storage_gb: z.number().describe("Allocated storage in GB"),
      multi_az: z.boolean().describe("Multi-AZ deployment"),
      vpc_id: z.string().nullable().describe("VPC ID"),
      security_groups: z.array(z.string()).describe("Security group IDs"),
      parameter_group: z.string().describe("Parameter group"),
      backup_retention: z.number().describe("Backup retention period in days"),
      latest_restorable_time: z.string().nullable().describe("Latest restorable time"),
      ca_certificate: z.string().nullable().describe("CA certificate identifier"),
    }),
    execute: async (params, ctx) => {
      const region = resolveRegion(ctx, params.region);
      const xml = await rdsQuery(ctx, region, { Action: "DescribeDBInstances", DBInstanceIdentifier: params.db_instance_id });
      const b = xml;
      return {
        db_instance_id: xmlVal(b, "DBInstanceIdentifier"),
        engine: xmlVal(b, "Engine"),
        engine_version: xmlVal(b, "EngineVersion"),
        instance_class: xmlVal(b, "DBInstanceClass"),
        status: xmlVal(b, "DBInstanceStatus"),
        endpoint: xmlVal(b, "Address") || null,
        port: parseInt(xmlVal(b, "Port") || "0", 10),
        storage_gb: parseInt(xmlVal(b, "AllocatedStorage") || "0", 10),
        multi_az: xmlVal(b, "MultiAZ") === "true",
        vpc_id: xmlVal(b, "VpcId") || null,
        security_groups: xmlAll(b, "VpcSecurityGroupId"),
        parameter_group: xmlVal(b, "DBParameterGroupName") || "",
        backup_retention: parseInt(xmlVal(b, "BackupRetentionPeriod") || "0", 10),
        latest_restorable_time: xmlVal(b, "LatestRestorableTime") || null,
        ca_certificate: xmlVal(b, "CACertificateIdentifier") || null,
      };
    },
  },

  create_db_snapshot: {
    description: "Create a manual snapshot of an RDS instance.",
    params: z.object({
      db_instance_id: z.string().describe("DB instance identifier"),
      snapshot_id: z.string().describe("Identifier for the snapshot"),
      tags: z.string().optional().describe("JSON object of key-value tags"),
      region: z.string().optional().describe("AWS region"),
    }),
    returns: z.object({
      snapshot_id: z.string().describe("Snapshot identifier"),
      db_instance_id: z.string().describe("DB instance identifier"),
      status: z.string().describe("Snapshot status"),
      engine: z.string().describe("Database engine"),
      allocated_storage: z.number().describe("Allocated storage in GB"),
      snapshot_create_time: z.string().nullable().describe("Snapshot creation time"),
    }),
    execute: async (params, ctx) => {
      const region = resolveRegion(ctx, params.region);
      const p: Record<string, string> = {
        Action: "CreateDBSnapshot",
        DBInstanceIdentifier: params.db_instance_id,
        DBSnapshotIdentifier: params.snapshot_id,
      };
      if (params.tags) {
        const t = JSON.parse(params.tags) as Record<string, string>;
        let idx = 1;
        for (const [k, v] of Object.entries(t)) {
          p[`Tags.member.${idx}.Key`] = k;
          p[`Tags.member.${idx}.Value`] = v;
          idx++;
        }
      }
      const xml = await rdsQuery(ctx, region, p);
      return {
        snapshot_id: xmlVal(xml, "DBSnapshotIdentifier"),
        db_instance_id: xmlVal(xml, "DBInstanceIdentifier"),
        status: xmlVal(xml, "Status"),
        engine: xmlVal(xml, "Engine"),
        allocated_storage: parseInt(xmlVal(xml, "AllocatedStorage") || "0", 10),
        snapshot_create_time: xmlVal(xml, "SnapshotCreateTime") || null,
      };
    },
  },
};
