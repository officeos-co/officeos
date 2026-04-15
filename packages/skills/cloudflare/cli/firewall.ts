import { z } from "@harro/skill-sdk";
import type { ActionDefinition } from "@harro/skill-sdk";
import { cfFetch, cfPost, cfDelete, enc } from "../core/client.ts";

export const firewall: Record<string, ActionDefinition> = {
  list_firewall_rules: {
    description: "List firewall rules for a zone.",
    params: z.object({
      zone_id: z.string().describe("Zone ID"),
    }),
    returns: z.array(
      z.object({
        id: z.string(),
        description: z.string(),
        action: z.string(),
        filter: z.any(),
        priority: z.number().optional(),
        paused: z.boolean(),
      }),
    ),
    execute: async (params, ctx) => {
      const data = await cfFetch(ctx, `/zones/${enc(params.zone_id)}/firewall/rules`);
      return (Array.isArray(data) ? data : []).map((r: any) => ({
        id: r.id,
        description: r.description ?? "",
        action: r.action,
        filter: r.filter ?? {},
        priority: r.priority,
        paused: r.paused ?? false,
      }));
    },
  },

  create_firewall_rule: {
    description: "Create a firewall rule.",
    params: z.object({
      zone_id: z.string().describe("Zone ID"),
      description: z.string().describe("Human-readable description"),
      action: z.string().describe("block, challenge, js_challenge, allow, log, or bypass"),
      filter: z.string().describe("Firewall filter expression"),
      priority: z.number().optional().describe("Rule priority (lower = higher priority)"),
      paused: z.boolean().default(false).describe("Create in paused state"),
    }),
    returns: z.object({ id: z.string(), description: z.string(), action: z.string(), filter: z.any() }),
    execute: async (params, ctx) => {
      const filterResult = await cfPost(ctx, `/zones/${enc(params.zone_id)}/filters`, [
        { expression: params.filter },
      ]);
      const filterId = (Array.isArray(filterResult) ? filterResult : [filterResult])[0]?.id;
      if (!filterId) throw new Error("Failed to create filter");
      const body: any = [{
        action: params.action,
        description: params.description,
        filter: { id: filterId },
        paused: params.paused,
      }];
      if (params.priority !== undefined) body[0].priority = params.priority;
      const result = await cfPost(ctx, `/zones/${enc(params.zone_id)}/firewall/rules`, body);
      const rule = (Array.isArray(result) ? result : [result])[0];
      return {
        id: rule?.id ?? "",
        description: rule?.description ?? params.description,
        action: rule?.action ?? params.action,
        filter: rule?.filter ?? {},
      };
    },
  },

  update_firewall_rule: {
    description: "Update a firewall rule.",
    params: z.object({
      zone_id: z.string().describe("Zone ID"),
      rule_id: z.string().describe("Firewall rule ID"),
      description: z.string().optional().describe("Updated description"),
      action: z.string().optional().describe("Updated action"),
      filter: z.string().optional().describe("Updated filter expression"),
      priority: z.number().optional().describe("Updated priority"),
      paused: z.boolean().optional().describe("Pause or unpause"),
    }),
    returns: z.object({ id: z.string(), description: z.string(), action: z.string(), filter: z.any() }),
    execute: async (params, ctx) => {
      const current = await cfFetch(ctx, `/zones/${enc(params.zone_id)}/firewall/rules/${enc(params.rule_id)}`);
      const body: any = {
        id: params.rule_id,
        action: params.action ?? current.action,
        description: params.description ?? current.description,
        filter: current.filter,
        paused: params.paused ?? current.paused,
      };
      if (params.priority !== undefined) body.priority = params.priority;
      if (params.filter !== undefined && current.filter?.id) {
        await cfPost(
          ctx,
          `/zones/${enc(params.zone_id)}/filters/${enc(current.filter.id)}`,
          { id: current.filter.id, expression: params.filter },
          "PUT",
        );
      }
      const r = await cfPost(ctx, `/zones/${enc(params.zone_id)}/firewall/rules/${enc(params.rule_id)}`, body, "PUT");
      return {
        id: r?.id ?? params.rule_id,
        description: r?.description ?? body.description,
        action: r?.action ?? body.action,
        filter: r?.filter ?? {},
      };
    },
  },

  delete_firewall_rule: {
    description: "Delete a firewall rule.",
    params: z.object({
      zone_id: z.string().describe("Zone ID"),
      rule_id: z.string().describe("Firewall rule ID"),
    }),
    returns: z.object({ id: z.string() }),
    execute: async (params, ctx) => {
      await cfDelete(ctx, `/zones/${enc(params.zone_id)}/firewall/rules/${enc(params.rule_id)}`);
      return { id: params.rule_id };
    },
  },
};
