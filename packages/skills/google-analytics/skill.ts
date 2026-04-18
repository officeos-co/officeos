import { defineSkill, z } from "@harro/skill-sdk";

const DATA_API = "https://analyticsdata.googleapis.com/v1beta";
const ADMIN_API = "https://analyticsadmin.googleapis.com/v1beta";

type Ctx = { fetch: typeof globalThis.fetch; credentials: Record<string, string> };

function authHeaders(token: string) {
  return {
    Authorization: `Bearer ${token}`,
    "Content-Type": "application/json",
  };
}

async function gaFetch(ctx: Ctx, url: string, init?: RequestInit) {
  const res = await ctx.fetch(url, {
    ...init,
    headers: { ...authHeaders(ctx.credentials.access_token), ...init?.headers },
  });
  if (!res.ok) {
    const body = await res.text();
    throw new Error(`Google Analytics API ${res.status}: ${body}`);
  }
  if (res.status === 204) return {};
  return res.json();
}

async function gaPost(ctx: Ctx, url: string, body: unknown) {
  return gaFetch(ctx, url, { method: "POST", body: JSON.stringify(body) });
}

import doc from "./SKILL.md";

export default defineSkill({
  name: "google-analytics",
  title: "Google Analytics",
  logo: "<svg viewBox=\"0 0 24 24\" xmlns=\"http://www.w3.org/2000/svg\"><path d=\"M22.84 2.9982v17.9987c.0086 1.6473-1.3197 2.9897-2.967 2.9984a2.9808 2.9808 0 01-.3677-.0208c-1.528-.226-2.6477-1.5558-2.6105-3.1V3.1204c-.0369-1.5458 1.0856-2.8762 2.6157-3.1 1.6361-.1915 3.1178.9796 3.3093 2.6158.014.1201.0208.241.0202.3619zM4.1326 18.0548c-1.6417 0-2.9726 1.331-2.9726 2.9726C1.16 22.6691 2.4909 24 4.1326 24s2.9726-1.3309 2.9726-2.9726-1.331-2.9726-2.9726-2.9726zm7.8728-9.0098c-.0171 0-.0342 0-.0513.0003-1.6495.0904-2.9293 1.474-2.891 3.1256v7.9846c0 2.167.9535 3.4825 2.3505 3.763 1.6118.3266 3.1832-.7152 3.5098-2.327.04-.1974.06-.3983.0593-.5998v-8.9585c.003-1.6474-1.33-2.9852-2.9773-2.9882z\"/></svg>",
  description:
    "Query reports, explore dimensions and metrics, manage audiences, and track events and conversions via the Google Analytics 4 Data API.",
  doc,

  credentials: {
    access_token: {
      label: "OAuth2 Access Token",
      kind: "password",
      placeholder: "ya29.\u2026",
      help: "Google OAuth2 access token with Analytics scopes.",
    },
  },

  actions: {
    // ── Properties ────────────────────────────────────────────────────

    list_properties: {
      description: "List GA4 properties accessible to the authenticated user.",
      params: z.object({}),
      returns: z.array(
        z.object({
          property_id: z.string().describe("Property ID"),
          display_name: z.string().describe("Display name"),
          time_zone: z.string().describe("Reporting time zone"),
          currency_code: z.string().describe("Currency code"),
          industry_category: z.string().optional().describe("Industry category"),
          create_time: z.string().describe("Creation time"),
        }),
      ),
      execute: async (_params, ctx) => {
        const res = await gaFetch(ctx, `${ADMIN_API}/properties?filter=parent:accounts/-`);
        return (res.properties ?? []).map((p: any) => ({
          property_id: p.name ?? "",
          display_name: p.displayName ?? "",
          time_zone: p.timeZone ?? "",
          currency_code: p.currencyCode ?? "",
          industry_category: p.industryCategory,
          create_time: p.createTime ?? "",
        }));
      },
    },

    get_property: {
      description: "Get details about a specific GA4 property.",
      params: z.object({
        property_id: z.string().describe("Property ID (e.g. properties/123456789)"),
      }),
      returns: z.object({
        property_id: z.string().describe("Property ID"),
        display_name: z.string().describe("Display name"),
        time_zone: z.string().describe("Reporting time zone"),
        currency_code: z.string().describe("Currency code"),
        industry_category: z.string().optional().describe("Industry category"),
        parent: z.string().optional().describe("Parent account"),
        create_time: z.string().describe("Creation time"),
        update_time: z.string().describe("Last update time"),
      }),
      execute: async (params, ctx) => {
        const p = await gaFetch(ctx, `${ADMIN_API}/${params.property_id}`);
        return {
          property_id: p.name ?? "",
          display_name: p.displayName ?? "",
          time_zone: p.timeZone ?? "",
          currency_code: p.currencyCode ?? "",
          industry_category: p.industryCategory,
          parent: p.parent,
          create_time: p.createTime ?? "",
          update_time: p.updateTime ?? "",
        };
      },
    },

    // ── Reports ───────────────────────────────────────────────────────

    run_report: {
      description: "Run a standard analytics report with dimensions and metrics.",
      params: z.object({
        property_id: z.string().describe("GA4 property ID"),
        date_ranges: z.string().describe("JSON array of {start_date, end_date} objects"),
        dimensions: z.array(z.string()).optional().describe("Dimension names (e.g. country, pagePath)"),
        metrics: z.array(z.string()).describe("Metric names (e.g. activeUsers, sessions)"),
        dimension_filter: z.string().optional().describe("JSON filter expression for dimensions"),
        metric_filter: z.string().optional().describe("JSON filter expression for metrics"),
        order_bys: z.string().optional().describe("JSON array of order-by specifications"),
        limit: z.number().min(1).max(100000).default(10).describe("Maximum rows to return"),
      }),
      returns: z.object({
        dimension_headers: z.array(z.any()).describe("Dimension headers"),
        metric_headers: z.array(z.any()).describe("Metric headers"),
        rows: z.array(z.any()).describe("Report rows"),
        row_count: z.number().describe("Total row count"),
        metadata: z.any().optional().describe("Report metadata"),
      }),
      execute: async (params, ctx) => {
        const body: any = {
          dateRanges: JSON.parse(params.date_ranges).map((dr: any) => ({
            startDate: dr.start_date,
            endDate: dr.end_date,
          })),
          metrics: params.metrics.map((m) => ({ name: m })),
          limit: params.limit,
        };
        if (params.dimensions) body.dimensions = params.dimensions.map((d) => ({ name: d }));
        if (params.dimension_filter) body.dimensionFilter = JSON.parse(params.dimension_filter);
        if (params.metric_filter) body.metricFilter = JSON.parse(params.metric_filter);
        if (params.order_bys) body.orderBys = JSON.parse(params.order_bys);

        const res = await gaPost(ctx, `${DATA_API}/${params.property_id}:runReport`, body);
        return {
          dimension_headers: res.dimensionHeaders ?? [],
          metric_headers: res.metricHeaders ?? [],
          rows: (res.rows ?? []).map((r: any) => ({
            dimension_values: r.dimensionValues ?? [],
            metric_values: r.metricValues ?? [],
          })),
          row_count: res.rowCount ?? 0,
          metadata: res.metadata,
        };
      },
    },

    run_realtime_report: {
      description: "Run a realtime report for live traffic data.",
      params: z.object({
        property_id: z.string().describe("GA4 property ID"),
        dimensions: z.array(z.string()).optional().describe("Dimension names"),
        metrics: z.array(z.string()).describe("Metric names"),
        dimension_filter: z.string().optional().describe("JSON filter expression for dimensions"),
        metric_filter: z.string().optional().describe("JSON filter expression for metrics"),
        limit: z.number().min(1).default(10).describe("Maximum rows to return"),
      }),
      returns: z.object({
        dimension_headers: z.array(z.any()).describe("Dimension headers"),
        metric_headers: z.array(z.any()).describe("Metric headers"),
        rows: z.array(z.any()).describe("Report rows"),
        row_count: z.number().describe("Total row count"),
      }),
      execute: async (params, ctx) => {
        const body: any = {
          metrics: params.metrics.map((m) => ({ name: m })),
          limit: params.limit,
        };
        if (params.dimensions) body.dimensions = params.dimensions.map((d) => ({ name: d }));
        if (params.dimension_filter) body.dimensionFilter = JSON.parse(params.dimension_filter);
        if (params.metric_filter) body.metricFilter = JSON.parse(params.metric_filter);

        const res = await gaPost(ctx, `${DATA_API}/${params.property_id}:runRealtimeReport`, body);
        return {
          dimension_headers: res.dimensionHeaders ?? [],
          metric_headers: res.metricHeaders ?? [],
          rows: (res.rows ?? []).map((r: any) => ({
            dimension_values: r.dimensionValues ?? [],
            metric_values: r.metricValues ?? [],
          })),
          row_count: res.rowCount ?? 0,
        };
      },
    },

    run_pivot_report: {
      description: "Run a pivot report for cross-tabulated data.",
      params: z.object({
        property_id: z.string().describe("GA4 property ID"),
        date_ranges: z.string().describe("JSON array of {start_date, end_date} objects"),
        pivots: z.string().describe("JSON array of pivot definitions ({field_names, limit})"),
        metrics: z.array(z.string()).describe("Metric names"),
        dimensions: z.array(z.string()).optional().describe("Dimension names"),
      }),
      returns: z.object({
        pivot_headers: z.array(z.any()).describe("Pivot headers"),
        rows: z.array(z.any()).describe("Report rows"),
        metadata: z.any().optional().describe("Report metadata"),
      }),
      execute: async (params, ctx) => {
        const body: any = {
          dateRanges: JSON.parse(params.date_ranges).map((dr: any) => ({
            startDate: dr.start_date,
            endDate: dr.end_date,
          })),
          metrics: params.metrics.map((m) => ({ name: m })),
          pivots: JSON.parse(params.pivots).map((p: any) => ({
            fieldNames: p.field_names,
            limit: p.limit,
          })),
        };
        if (params.dimensions) body.dimensions = params.dimensions.map((d) => ({ name: d }));

        const res = await gaPost(ctx, `${DATA_API}/${params.property_id}:runPivotReport`, body);
        return {
          pivot_headers: res.pivotHeaders ?? [],
          rows: res.rows ?? [],
          metadata: res.metadata,
        };
      },
    },

    batch_run_reports: {
      description: "Run multiple reports in a single request.",
      params: z.object({
        property_id: z.string().describe("GA4 property ID"),
        requests: z.string().describe("JSON array of report request objects"),
      }),
      returns: z.array(z.object({
        dimension_headers: z.array(z.any()).describe("Dimension headers"),
        metric_headers: z.array(z.any()).describe("Metric headers"),
        rows: z.array(z.any()).describe("Report rows"),
      })).describe("List of report responses"),
      execute: async (params, ctx) => {
        const requests = JSON.parse(params.requests).map((r: any) => ({
          ...r,
          dateRanges: (r.date_ranges ?? r.dateRanges ?? []).map((dr: any) => ({
            startDate: dr.start_date ?? dr.startDate,
            endDate: dr.end_date ?? dr.endDate,
          })),
          metrics: (r.metrics ?? []).map((m: any) => (typeof m === "string" ? { name: m } : m)),
          dimensions: (r.dimensions ?? []).map((d: any) => (typeof d === "string" ? { name: d } : d)),
        }));

        const res = await gaPost(ctx, `${DATA_API}/${params.property_id}:batchRunReports`, { requests });
        return (res.reports ?? []).map((report: any) => ({
          dimension_headers: report.dimensionHeaders ?? [],
          metric_headers: report.metricHeaders ?? [],
          rows: (report.rows ?? []).map((r: any) => ({
            dimension_values: r.dimensionValues ?? [],
            metric_values: r.metricValues ?? [],
          })),
        }));
      },
    },

    // ── Dimensions / Metrics ──────────────────────────────────────────

    list_dimensions: {
      description: "List available dimensions for a GA4 property.",
      params: z.object({
        property_id: z.string().describe("GA4 property ID"),
      }),
      returns: z.array(
        z.object({
          api_name: z.string().describe("API name"),
          ui_name: z.string().describe("Display name"),
          description: z.string().describe("Description"),
          category: z.string().describe("Category"),
          deprecated: z.boolean().describe("Whether deprecated"),
        }),
      ),
      execute: async (params, ctx) => {
        const res = await gaFetch(ctx, `${DATA_API}/${params.property_id}/metadata`);
        return (res.dimensions ?? []).map((d: any) => ({
          api_name: d.apiName ?? "",
          ui_name: d.uiName ?? "",
          description: d.description ?? "",
          category: d.category ?? "",
          deprecated: d.deprecatedApiNames?.length > 0 || false,
        }));
      },
    },

    list_metrics: {
      description: "List available metrics for a GA4 property.",
      params: z.object({
        property_id: z.string().describe("GA4 property ID"),
      }),
      returns: z.array(
        z.object({
          api_name: z.string().describe("API name"),
          ui_name: z.string().describe("Display name"),
          description: z.string().describe("Description"),
          category: z.string().describe("Category"),
          type: z.string().describe("Metric type"),
          deprecated: z.boolean().describe("Whether deprecated"),
        }),
      ),
      execute: async (params, ctx) => {
        const res = await gaFetch(ctx, `${DATA_API}/${params.property_id}/metadata`);
        return (res.metrics ?? []).map((m: any) => ({
          api_name: m.apiName ?? "",
          ui_name: m.uiName ?? "",
          description: m.description ?? "",
          category: m.category ?? "",
          type: m.type ?? "",
          deprecated: m.deprecatedApiNames?.length > 0 || false,
        }));
      },
    },

    // ── Audiences ─────────────────────────────────────────────────────

    list_audiences: {
      description: "List audiences for a GA4 property.",
      params: z.object({
        property_id: z.string().describe("GA4 property ID"),
      }),
      returns: z.array(
        z.object({
          audience_id: z.string().describe("Audience ID"),
          display_name: z.string().describe("Display name"),
          description: z.string().describe("Description"),
          membership_duration_days: z.number().describe("Membership duration in days"),
          ads_personalization_enabled: z.boolean().describe("Whether ads personalization is enabled"),
        }),
      ),
      execute: async (params, ctx) => {
        const res = await gaFetch(ctx, `${ADMIN_API}/${params.property_id}/audiences`);
        return (res.audiences ?? []).map((a: any) => ({
          audience_id: a.name?.split("/").pop() ?? "",
          display_name: a.displayName ?? "",
          description: a.description ?? "",
          membership_duration_days: a.membershipDurationDays ?? 0,
          ads_personalization_enabled: a.adsPersonalizationEnabled ?? false,
        }));
      },
    },

    get_audience: {
      description: "Get details about a specific audience.",
      params: z.object({
        property_id: z.string().describe("GA4 property ID"),
        audience_id: z.string().describe("Audience ID"),
      }),
      returns: z.object({
        audience_id: z.string().describe("Audience ID"),
        display_name: z.string().describe("Display name"),
        description: z.string().describe("Description"),
        membership_duration_days: z.number().describe("Membership duration in days"),
        filter_clauses: z.array(z.any()).describe("Filter clauses"),
        event_trigger: z.any().optional().describe("Event trigger"),
      }),
      execute: async (params, ctx) => {
        const a = await gaFetch(ctx, `${ADMIN_API}/${params.property_id}/audiences/${params.audience_id}`);
        return {
          audience_id: a.name?.split("/").pop() ?? params.audience_id,
          display_name: a.displayName ?? "",
          description: a.description ?? "",
          membership_duration_days: a.membershipDurationDays ?? 0,
          filter_clauses: a.filterClauses ?? [],
          event_trigger: a.eventTrigger,
        };
      },
    },

    // ── Events ────────────────────────────────────────────────────────

    list_event_names: {
      description: "List tracked event names and their counts for a property.",
      params: z.object({
        property_id: z.string().describe("GA4 property ID"),
        date_ranges: z.string().describe("JSON array of {start_date, end_date}"),
        limit: z.number().min(1).default(25).describe("Maximum events to return"),
      }),
      returns: z.array(
        z.object({
          event_name: z.string().describe("Event name"),
          event_count: z.string().describe("Event count"),
        }),
      ),
      execute: async (params, ctx) => {
        const body = {
          dateRanges: JSON.parse(params.date_ranges).map((dr: any) => ({
            startDate: dr.start_date,
            endDate: dr.end_date,
          })),
          dimensions: [{ name: "eventName" }],
          metrics: [{ name: "eventCount" }],
          limit: params.limit,
          orderBys: [{ metric: { metricName: "eventCount" }, desc: true }],
        };
        const res = await gaPost(ctx, `${DATA_API}/${params.property_id}:runReport`, body);
        return (res.rows ?? []).map((r: any) => ({
          event_name: r.dimensionValues?.[0]?.value ?? "",
          event_count: r.metricValues?.[0]?.value ?? "0",
        }));
      },
    },

    // ── Conversions ───────────────────────────────────────────────────

    list_conversions: {
      description: "List conversion events and their counts.",
      params: z.object({
        property_id: z.string().describe("GA4 property ID"),
        date_ranges: z.string().describe("JSON array of {start_date, end_date}"),
      }),
      returns: z.array(
        z.object({
          event_name: z.string().describe("Conversion event name"),
          conversions: z.string().describe("Conversion count"),
          total_revenue: z.string().describe("Total revenue"),
        }),
      ),
      execute: async (params, ctx) => {
        const body = {
          dateRanges: JSON.parse(params.date_ranges).map((dr: any) => ({
            startDate: dr.start_date,
            endDate: dr.end_date,
          })),
          dimensions: [{ name: "eventName" }],
          metrics: [{ name: "conversions" }, { name: "totalRevenue" }],
          dimensionFilter: {
            filter: {
              fieldName: "isConversionEvent",
              stringFilter: { value: "true" },
            },
          },
        };
        const res = await gaPost(ctx, `${DATA_API}/${params.property_id}:runReport`, body);
        return (res.rows ?? []).map((r: any) => ({
          event_name: r.dimensionValues?.[0]?.value ?? "",
          conversions: r.metricValues?.[0]?.value ?? "0",
          total_revenue: r.metricValues?.[1]?.value ?? "0",
        }));
      },
    },

    get_conversion_rate: {
      description: "Get the conversion rate for a specific conversion event.",
      params: z.object({
        property_id: z.string().describe("GA4 property ID"),
        date_ranges: z.string().describe("JSON array of {start_date, end_date}"),
        event_name: z.string().describe("Conversion event name"),
      }),
      returns: z.object({
        event_name: z.string().describe("Conversion event name"),
        sessions: z.string().describe("Total sessions"),
        conversions: z.string().describe("Conversion count"),
        conversion_rate: z.string().describe("Conversion rate as percentage"),
      }),
      execute: async (params, ctx) => {
        const dateRanges = JSON.parse(params.date_ranges).map((dr: any) => ({
          startDate: dr.start_date,
          endDate: dr.end_date,
        }));

        // Get total sessions
        const sessionsRes = await gaPost(ctx, `${DATA_API}/${params.property_id}:runReport`, {
          dateRanges,
          metrics: [{ name: "sessions" }],
        });
        const sessions = parseInt(sessionsRes.rows?.[0]?.metricValues?.[0]?.value ?? "0");

        // Get conversions for the specific event
        const convRes = await gaPost(ctx, `${DATA_API}/${params.property_id}:runReport`, {
          dateRanges,
          dimensions: [{ name: "eventName" }],
          metrics: [{ name: "eventCount" }],
          dimensionFilter: {
            filter: {
              fieldName: "eventName",
              stringFilter: { value: params.event_name },
            },
          },
        });
        const conversions = parseInt(convRes.rows?.[0]?.metricValues?.[0]?.value ?? "0");
        const rate = sessions > 0 ? ((conversions / sessions) * 100).toFixed(2) : "0.00";

        return {
          event_name: params.event_name,
          sessions: String(sessions),
          conversions: String(conversions),
          conversion_rate: `${rate}%`,
        };
      },
    },
  },
});
