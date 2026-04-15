import { z } from "@harro/skill-sdk";
import type { ActionDefinition } from "@harro/skill-sdk";
import { cfFetch, enc } from "../core/client.ts";

export const analytics: Record<string, ActionDefinition> = {
  get_zone_analytics: {
    description: "Get zone analytics (requests, bandwidth, threats).",
    params: z.object({
      zone_id: z.string().describe("Zone ID"),
      since: z.number().default(-1440).describe("Minutes relative to now (negative) or Unix timestamp"),
      until: z.number().optional().describe("End time (same format as since)"),
      continuous: z.boolean().default(true).describe("Continuous time series"),
    }),
    returns: z.object({
      requests: z.any(),
      bandwidth: z.any(),
      threats: z.any(),
      pageviews: z.any(),
      uniques: z.any(),
      status_codes: z.any(),
    }),
    execute: async (params, ctx) => {
      const qs = new URLSearchParams({ since: String(params.since) });
      if (params.until !== undefined) qs.set("until", String(params.until));
      qs.set("continuous", String(params.continuous));
      const data = await cfFetch(ctx, `/zones/${enc(params.zone_id)}/analytics/dashboard?${qs}`);
      const totals = data?.totals ?? {};
      return {
        requests: totals.requests ?? {},
        bandwidth: totals.bandwidth ?? {},
        threats: totals.threats ?? {},
        pageviews: totals.pageviews ?? {},
        uniques: totals.uniques ?? {},
        status_codes: totals.requests?.http_status ?? {},
      };
    },
  },

  get_dns_analytics: {
    description: "Get DNS analytics for a zone.",
    params: z.object({
      zone_id: z.string().describe("Zone ID"),
      since: z.number().default(-1440).describe("Minutes relative to now (negative) or Unix timestamp"),
      until: z.number().optional().describe("End time"),
    }),
    returns: z.object({
      query_count: z.number(),
      response_codes: z.any(),
      query_types: z.any(),
      top_records: z.any(),
    }),
    execute: async (params, ctx) => {
      const qs = new URLSearchParams({ since: String(params.since) });
      if (params.until !== undefined) qs.set("until", String(params.until));
      const data = await cfFetch(ctx, `/zones/${enc(params.zone_id)}/dns_analytics/report?${qs}`);
      return {
        query_count: data?.totals?.queryCount ?? 0,
        response_codes: data?.totals?.responseCode ?? {},
        query_types: data?.totals?.queryType ?? {},
        top_records: data?.top ?? [],
      };
    },
  },
};
