import { z } from "@harro/skill-sdk";
import type { ActionDefinition } from "@harro/skill-sdk";
import { sentryFetch, sentryPost, sentryDelete, enc } from "../core/client.ts";

export const issues: Record<string, ActionDefinition> = {
  list_issues: {
    description: "List issues for a project with optional search and sorting.",
    params: z.object({
      project: z.string().describe("Project slug"),
      query: z.string().default("is:unresolved").describe("Search query (Sentry search syntax)"),
      sort: z.enum(["date", "new", "priority", "freq", "users"]).default("date").describe("Sort order"),
      per_page: z.number().min(1).max(100).default(25).describe("Results per page"),
      cursor: z.string().optional().describe("Pagination cursor"),
    }),
    returns: z.array(z.object({
      id: z.string().describe("Issue ID"),
      title: z.string().describe("Issue title"),
      culprit: z.string().nullable().describe("Culprit"),
      level: z.string().describe("Error level"),
      status: z.string().describe("Issue status"),
      count: z.string().describe("Event count"),
      user_count: z.number().describe("Affected user count"),
      first_seen: z.string().describe("First seen timestamp"),
      last_seen: z.string().describe("Last seen timestamp"),
      permalink: z.string().describe("Link to issue in Sentry"),
    })),
    execute: async (params, ctx) => {
      const org = ctx.credentials.organization;
      let url = `/projects/${enc(org)}/${enc(params.project)}/issues/?query=${enc(params.query)}&sort=${params.sort}&limit=${params.per_page}`;
      if (params.cursor) url += `&cursor=${enc(params.cursor)}`;
      const data = await sentryFetch(ctx, url);
      return data.map((i: any) => ({
        id: i.id,
        title: i.title,
        culprit: i.culprit ?? null,
        level: i.level,
        status: i.status,
        count: i.count,
        user_count: i.userCount ?? 0,
        first_seen: i.firstSeen,
        last_seen: i.lastSeen,
        permalink: i.permalink,
      }));
    },
  },

  get_issue: {
    description: "Get detailed information about a specific issue.",
    params: z.object({
      issue_id: z.string().describe("Issue ID"),
    }),
    returns: z.object({
      id: z.string().describe("Issue ID"),
      title: z.string().describe("Issue title"),
      culprit: z.string().nullable().describe("Culprit"),
      level: z.string().describe("Error level"),
      status: z.string().describe("Issue status"),
      count: z.string().describe("Event count"),
      user_count: z.number().describe("Affected user count"),
      first_seen: z.string().describe("First seen timestamp"),
      last_seen: z.string().describe("Last seen timestamp"),
      metadata: z.any().describe("Issue metadata"),
      tags: z.array(z.any()).describe("Issue tags"),
      assigned_to: z.any().nullable().describe("Assigned user or team"),
      permalink: z.string().describe("Link to issue in Sentry"),
    }),
    execute: async (params, ctx) => {
      const i = await sentryFetch(ctx, `/issues/${enc(params.issue_id)}/`);
      return {
        id: i.id,
        title: i.title,
        culprit: i.culprit ?? null,
        level: i.level,
        status: i.status,
        count: i.count,
        user_count: i.userCount ?? 0,
        first_seen: i.firstSeen,
        last_seen: i.lastSeen,
        metadata: i.metadata ?? {},
        tags: i.tags ?? [],
        assigned_to: i.assignedTo ?? null,
        permalink: i.permalink,
      };
    },
  },

  resolve_issue: {
    description: "Resolve an issue.",
    params: z.object({
      issue_id: z.string().describe("Issue ID"),
      status: z.enum(["resolved", "resolvedInNextRelease"]).default("resolved").describe("Resolution status"),
    }),
    returns: z.object({
      id: z.string().describe("Issue ID"),
      status: z.string().describe("New status"),
    }),
    execute: async (params, ctx) => {
      const body: any = { status: "resolved" };
      if (params.status === "resolvedInNextRelease") {
        body.statusDetails = { inNextRelease: true };
      }
      const i = await sentryPost(ctx, `/issues/${enc(params.issue_id)}/`, body, "PUT");
      return { id: i.id, status: i.status };
    },
  },

  ignore_issue: {
    description: "Ignore an issue with optional conditions.",
    params: z.object({
      issue_id: z.string().describe("Issue ID"),
      ignore_count: z.number().optional().describe("Ignore until seen this many more times"),
      ignore_window: z.number().optional().describe("Time window in minutes for ignore_count"),
      ignore_until: z.string().optional().describe("ISO 8601 timestamp to ignore until"),
    }),
    returns: z.object({
      id: z.string().describe("Issue ID"),
      status: z.string().describe("New status"),
    }),
    execute: async (params, ctx) => {
      const statusDetails: any = {};
      if (params.ignore_count !== undefined) statusDetails.ignoreCount = params.ignore_count;
      if (params.ignore_window !== undefined) statusDetails.ignoreWindow = params.ignore_window;
      if (params.ignore_until !== undefined) statusDetails.ignoreUntil = params.ignore_until;
      const i = await sentryPost(ctx, `/issues/${enc(params.issue_id)}/`, { status: "ignored", statusDetails }, "PUT");
      return { id: i.id, status: i.status };
    },
  },

  assign_issue: {
    description: "Assign an issue to a user or team.",
    params: z.object({
      issue_id: z.string().describe("Issue ID"),
      assignee: z.string().describe("Assignee: user:<email>, team:<slug>, or empty to unassign"),
    }),
    returns: z.object({
      id: z.string().describe("Issue ID"),
      assigned_to: z.any().nullable().describe("New assignee"),
    }),
    execute: async (params, ctx) => {
      const i = await sentryPost(ctx, `/issues/${enc(params.issue_id)}/`, { assignedTo: params.assignee || "" }, "PUT");
      return { id: i.id, assigned_to: i.assignedTo ?? null };
    },
  },

  delete_issue: {
    description: "Permanently delete an issue and all its events. Cannot be undone.",
    params: z.object({
      issue_id: z.string().describe("Issue ID"),
    }),
    returns: z.object({ id: z.string().describe("Deleted issue ID") }),
    execute: async (params, ctx) => {
      await sentryDelete(ctx, `/issues/${enc(params.issue_id)}/`);
      return { id: params.issue_id };
    },
  },

  get_issue_frequency: {
    description: "Get event frequency time series for a specific issue.",
    params: z.object({
      issue_id: z.string().describe("Issue ID"),
      resolution: z.string().default("1h").describe("Bucket resolution (e.g. 1h, 1d)"),
      since: z.string().default("-24h").describe("Start time (relative or ISO 8601)"),
      until: z.string().optional().describe("End time"),
    }),
    returns: z.array(z.object({
      timestamp: z.number().describe("Unix timestamp"),
      count: z.number().describe("Event count"),
    })),
    execute: async (params, ctx) => {
      let url = `/issues/${enc(params.issue_id)}/stats/?stat=events&resolution=${enc(params.resolution)}&since=${enc(params.since)}`;
      if (params.until) url += `&until=${enc(params.until)}`;
      const data = await sentryFetch(ctx, url);
      return data.map((point: any) => ({ timestamp: point[0], count: point[1] }));
    },
  },
};
