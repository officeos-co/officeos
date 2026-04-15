import { z } from "@harro/skill-sdk";
import type { ActionDefinition } from "@harro/skill-sdk";
import { sfFetch, sfPost, enc } from "../core/client.ts";

export const reports: Record<string, ActionDefinition> = {
  list_reports: {
    description: "List available reports.",
    params: z.object({
      limit: z.number().default(100).describe("Max results to return"),
    }),
    returns: z.array(
      z.object({
        Id: z.string().describe("Report ID"),
        Name: z.string().describe("Report name"),
        ReportFormat: z.string().describe("Report format"),
        FolderName: z.string().nullable().describe("Folder name"),
        LastRunDate: z.string().nullable().describe("Last run timestamp"),
      }),
    ),
    execute: async (params, ctx) => {
      const soql = `SELECT Id, Name, Format, FolderName, LastRunDate FROM Report LIMIT ${params.limit}`;
      const data = await sfFetch(ctx, `/query?q=${enc(soql)}`);
      return (data.records ?? []).map((r: any) => ({
        Id: r.Id,
        Name: r.Name,
        ReportFormat: r.Format,
        FolderName: r.FolderName ?? null,
        LastRunDate: r.LastRunDate ?? null,
      }));
    },
  },

  run_report: {
    description: "Run a report and return the results.",
    params: z.object({
      report_id: z.string().describe("Report ID"),
      filters: z.record(z.unknown()).optional().describe("Runtime filter overrides (JSON)"),
    }),
    returns: z.object({
      reportMetadata: z.record(z.unknown()).describe("Report metadata (name, columns, filters)"),
      factMap: z.record(z.unknown()).describe("Aggregated data"),
      hasDetailRows: z.boolean().describe("Whether detail rows are included"),
      reportExtendedMetadata: z.record(z.unknown()).describe("Extended metadata"),
    }),
    execute: async (params, ctx) => {
      const body = params.filters ? { reportMetadata: params.filters } : {};
      const data = await sfPost(ctx, `/analytics/reports/${enc(params.report_id)}`, body);
      return {
        reportMetadata: data.reportMetadata ?? {},
        factMap: data.factMap ?? {},
        hasDetailRows: data.hasDetailRows ?? false,
        reportExtendedMetadata: data.reportExtendedMetadata ?? {},
      };
    },
  },

  get_report_metadata: {
    description: "Get the metadata (columns, filters, groupings) for a report without running it.",
    params: z.object({
      report_id: z.string().describe("Report ID"),
    }),
    returns: z.object({
      id: z.string().describe("Report ID"),
      name: z.string().describe("Report name"),
      reportFormat: z.string().describe("Report format"),
      reportType: z.record(z.unknown()).describe("Report type info"),
      detailColumns: z.array(z.string()).describe("Detail column API names"),
      aggregates: z.array(z.record(z.unknown())).describe("Aggregate definitions"),
      groupingsDown: z.array(z.record(z.unknown())).describe("Row groupings"),
      groupingsAcross: z.array(z.record(z.unknown())).describe("Column groupings"),
      reportFilters: z.array(z.record(z.unknown())).describe("Defined filters"),
    }),
    execute: async (params, ctx) => {
      const data = await sfFetch(ctx, `/analytics/reports/${enc(params.report_id)}/describe`);
      const meta = data.reportMetadata ?? data;
      return {
        id: meta.id ?? params.report_id,
        name: meta.name ?? "",
        reportFormat: meta.reportFormat ?? "",
        reportType: meta.reportType ?? {},
        detailColumns: meta.detailColumns ?? [],
        aggregates: meta.aggregates ?? [],
        groupingsDown: meta.groupingsDown ?? [],
        groupingsAcross: meta.groupingsAcross ?? [],
        reportFilters: meta.reportFilters ?? [],
      };
    },
  },

  list_users: {
    description: "List users in the org.",
    params: z.object({
      active: z.boolean().optional().describe("Filter by active status"),
      role: z.string().optional().describe("Filter by role name"),
      limit: z.number().default(100).describe("Max records to return"),
    }),
    returns: z.array(
      z.object({
        Id: z.string().describe("User ID"),
        Name: z.string().describe("Full name"),
        Email: z.string().describe("Email"),
        Username: z.string().describe("Username"),
        IsActive: z.boolean().describe("Whether active"),
        Profile: z.string().nullable().describe("Profile name"),
        UserRole: z.string().nullable().describe("Role name"),
        LastLoginDate: z.string().nullable().describe("Last login timestamp"),
      }),
    ),
    execute: async (params, ctx) => {
      const clauses: string[] = [];
      if (params.active !== undefined) clauses.push(`IsActive = ${params.active}`);
      if (params.role) clauses.push(`UserRole.Name = '${params.role}'`);
      const where = clauses.length ? ` WHERE ${clauses.join(" AND ")}` : "";
      const soql = `SELECT Id, Name, Email, Username, IsActive, Profile.Name, UserRole.Name, LastLoginDate FROM User${where} LIMIT ${params.limit}`;
      const data = await sfFetch(ctx, `/query?q=${enc(soql)}`);
      return (data.records ?? []).map((u: any) => ({
        Id: u.Id,
        Name: u.Name,
        Email: u.Email,
        Username: u.Username,
        IsActive: u.IsActive,
        Profile: u.Profile?.Name ?? null,
        UserRole: u.UserRole?.Name ?? null,
        LastLoginDate: u.LastLoginDate ?? null,
      }));
    },
  },

  get_user: {
    description: "Get detailed information about a single user.",
    params: z.object({
      user_id: z.string().describe("User ID"),
    }),
    returns: z.object({
      Id: z.string().describe("User ID"),
      Name: z.string().describe("Full name"),
      Email: z.string().describe("Email"),
      Username: z.string().describe("Username"),
      IsActive: z.boolean().describe("Whether active"),
      Profile: z.string().nullable().describe("Profile name"),
      UserRole: z.string().nullable().describe("Role name"),
      Department: z.string().nullable().describe("Department"),
      Title: z.string().nullable().describe("Title"),
      Phone: z.string().nullable().describe("Phone"),
      LastLoginDate: z.string().nullable().describe("Last login timestamp"),
      CreatedDate: z.string().describe("Creation timestamp"),
    }),
    execute: async (params, ctx) => {
      const soql = `SELECT Id, Name, Email, Username, IsActive, Profile.Name, UserRole.Name, Department, Title, Phone, LastLoginDate, CreatedDate FROM User WHERE Id = '${params.user_id}' LIMIT 1`;
      const data = await sfFetch(ctx, `/query?q=${enc(soql)}`);
      const u = data.records?.[0];
      if (!u) throw new Error(`User ${params.user_id} not found`);
      return {
        Id: u.Id,
        Name: u.Name,
        Email: u.Email,
        Username: u.Username,
        IsActive: u.IsActive,
        Profile: u.Profile?.Name ?? null,
        UserRole: u.UserRole?.Name ?? null,
        Department: u.Department ?? null,
        Title: u.Title ?? null,
        Phone: u.Phone ?? null,
        LastLoginDate: u.LastLoginDate ?? null,
        CreatedDate: u.CreatedDate,
      };
    },
  },
};
