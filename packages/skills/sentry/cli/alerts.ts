import { z } from "@harro/skill-sdk";
import type { ActionDefinition } from "@harro/skill-sdk";
import { sentryFetch, sentryPost, sentryDelete, enc } from "../core/client.ts";

export const alerts: Record<string, ActionDefinition> = {
  list_alert_rules: {
    description: "List alert rules for a project.",
    params: z.object({
      project: z.string().describe("Project slug"),
    }),
    returns: z.array(z.object({
      id: z.string().describe("Alert rule ID"),
      name: z.string().describe("Alert rule name"),
      conditions: z.array(z.any()).describe("Alert conditions"),
      actions: z.array(z.any()).describe("Alert actions"),
      frequency: z.number().describe("Minutes between alerts"),
      date_created: z.string().describe("Creation date"),
    })),
    execute: async (params, ctx) => {
      const org = ctx.credentials.organization;
      const data = await sentryFetch(ctx, `/projects/${enc(org)}/${enc(params.project)}/rules/`);
      return data.map((r: any) => ({
        id: String(r.id),
        name: r.name,
        conditions: r.conditions ?? [],
        actions: r.actions ?? [],
        frequency: r.frequency ?? 30,
        date_created: r.dateCreated,
      }));
    },
  },

  create_alert_rule: {
    description: "Create an alert rule for a project.",
    params: z.object({
      project: z.string().describe("Project slug"),
      name: z.string().describe("Alert rule name"),
      conditions: z.string().describe("JSON array of condition objects"),
      actions: z.string().describe("JSON array of action objects"),
      frequency: z.number().default(30).describe("Minutes between alerts for the same issue"),
      environment: z.string().optional().describe("Filter to environment"),
    }),
    returns: z.object({
      id: z.string().describe("Alert rule ID"),
      name: z.string().describe("Alert rule name"),
      conditions: z.array(z.any()).describe("Alert conditions"),
      actions: z.array(z.any()).describe("Alert actions"),
    }),
    execute: async (params, ctx) => {
      const org = ctx.credentials.organization;
      const body: any = {
        name: params.name,
        conditions: JSON.parse(params.conditions),
        actions: JSON.parse(params.actions),
        actionMatch: "all",
        frequency: params.frequency,
      };
      if (params.environment) body.environment = params.environment;
      const r = await sentryPost(ctx, `/projects/${enc(org)}/${enc(params.project)}/rules/`, body);
      return { id: String(r.id), name: r.name, conditions: r.conditions ?? [], actions: r.actions ?? [] };
    },
  },

  delete_alert_rule: {
    description: "Delete an alert rule.",
    params: z.object({
      project: z.string().describe("Project slug"),
      rule_id: z.string().describe("Alert rule ID"),
    }),
    returns: z.object({ id: z.string().describe("Deleted alert rule ID") }),
    execute: async (params, ctx) => {
      const org = ctx.credentials.organization;
      await sentryDelete(ctx, `/projects/${enc(org)}/${enc(params.project)}/rules/${enc(params.rule_id)}/`);
      return { id: params.rule_id };
    },
  },
};
