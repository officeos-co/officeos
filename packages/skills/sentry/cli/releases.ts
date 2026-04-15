import { z } from "@harro/skill-sdk";
import type { ActionDefinition } from "@harro/skill-sdk";
import { sentryFetch, sentryPost, sentryDelete, enc } from "../core/client.ts";

export const releases: Record<string, ActionDefinition> = {
  list_releases: {
    description: "List releases for the organization, optionally filtered by project.",
    params: z.object({
      project: z.string().optional().describe("Filter by project slug"),
      per_page: z.number().min(1).max(100).default(25).describe("Results per page"),
    }),
    returns: z.array(z.object({
      version: z.string().describe("Release version"),
      short_version: z.string().nullable().describe("Short version"),
      date_created: z.string().describe("Creation date"),
      date_released: z.string().nullable().describe("Release date"),
      new_groups: z.number().describe("New issue groups"),
      authors: z.array(z.any()).describe("Commit authors"),
      commit_count: z.number().describe("Number of commits"),
      deploy_count: z.number().describe("Number of deploys"),
    })),
    execute: async (params, ctx) => {
      const org = ctx.credentials.organization;
      let url = `/organizations/${enc(org)}/releases/?per_page=${params.per_page}`;
      if (params.project) url += `&project=${enc(params.project)}`;
      const data = await sentryFetch(ctx, url);
      return data.map((r: any) => ({
        version: r.version,
        short_version: r.shortVersion ?? null,
        date_created: r.dateCreated,
        date_released: r.dateReleased ?? null,
        new_groups: r.newGroups ?? 0,
        authors: r.authors ?? [],
        commit_count: r.commitCount ?? 0,
        deploy_count: r.deployCount ?? 0,
      }));
    },
  },

  create_release: {
    description: "Create a new release.",
    params: z.object({
      version: z.string().describe("Release version (e.g. v1.2.0 or SHA)"),
      projects: z.array(z.string()).describe("Project slugs to associate"),
      ref: z.string().optional().describe("Git ref (branch or SHA)"),
      url: z.string().optional().describe("URL for the release (e.g. changelog)"),
    }),
    returns: z.object({
      version: z.string().describe("Release version"),
      date_created: z.string().describe("Creation date"),
      projects: z.array(z.any()).describe("Associated projects"),
    }),
    execute: async (params, ctx) => {
      const org = ctx.credentials.organization;
      const body: any = { version: params.version, projects: params.projects };
      if (params.ref) body.ref = params.ref;
      if (params.url) body.url = params.url;
      const r = await sentryPost(ctx, `/organizations/${enc(org)}/releases/`, body);
      return { version: r.version, date_created: r.dateCreated, projects: r.projects ?? [] };
    },
  },

  finalize_release: {
    description: "Finalize a release by setting its dateReleased.",
    params: z.object({
      version: z.string().describe("Release version"),
    }),
    returns: z.object({
      version: z.string().describe("Release version"),
      date_released: z.string().describe("Release date"),
    }),
    execute: async (params, ctx) => {
      const org = ctx.credentials.organization;
      const r = await sentryPost(ctx, `/organizations/${enc(org)}/releases/${enc(params.version)}/`, { dateReleased: new Date().toISOString() }, "PUT");
      return { version: r.version, date_released: r.dateReleased };
    },
  },

  delete_release: {
    description: "Delete a release record.",
    params: z.object({
      version: z.string().describe("Release version"),
    }),
    returns: z.object({ version: z.string().describe("Deleted release version") }),
    execute: async (params, ctx) => {
      const org = ctx.credentials.organization;
      await sentryDelete(ctx, `/organizations/${enc(org)}/releases/${enc(params.version)}/`);
      return { version: params.version };
    },
  },

  set_commits: {
    description: "Associate commits with a release.",
    params: z.object({
      version: z.string().describe("Release version"),
      repository: z.string().describe("Repository name (owner/repo)"),
      commit: z.string().describe("Head commit SHA"),
      prev_commit: z.string().optional().describe("Previous release commit SHA"),
    }),
    returns: z.object({
      version: z.string().describe("Release version"),
      commit_count: z.number().describe("Number of commits associated"),
    }),
    execute: async (params, ctx) => {
      const org = ctx.credentials.organization;
      const commits: any = { repository: params.repository, commit: params.commit };
      if (params.prev_commit) commits.previousCommit = params.prev_commit;
      const r = await sentryPost(ctx, `/organizations/${enc(org)}/releases/${enc(params.version)}/commits/`, [commits]);
      return { version: params.version, commit_count: Array.isArray(r) ? r.length : 0 };
    },
  },

  list_deploys: {
    description: "List deploys for a release.",
    params: z.object({
      version: z.string().describe("Release version"),
    }),
    returns: z.array(z.object({
      id: z.string().describe("Deploy ID"),
      environment: z.string().describe("Environment name"),
      name: z.string().nullable().describe("Deploy name"),
      date_started: z.string().nullable().describe("Start timestamp"),
      date_finished: z.string().nullable().describe("Finish timestamp"),
    })),
    execute: async (params, ctx) => {
      const org = ctx.credentials.organization;
      const data = await sentryFetch(ctx, `/organizations/${enc(org)}/releases/${enc(params.version)}/deploys/`);
      return data.map((d: any) => ({
        id: d.id,
        environment: d.environment,
        name: d.name ?? null,
        date_started: d.dateStarted ?? null,
        date_finished: d.dateFinished ?? null,
      }));
    },
  },

  create_deploy: {
    description: "Create a deploy for a release.",
    params: z.object({
      version: z.string().describe("Release version"),
      environment: z.string().describe("Environment name (e.g. production, staging)"),
      name: z.string().optional().describe("Deploy name or description"),
      url: z.string().optional().describe("URL for the deploy"),
    }),
    returns: z.object({
      id: z.string().describe("Deploy ID"),
      environment: z.string().describe("Environment name"),
      date_started: z.string().nullable().describe("Start timestamp"),
      date_finished: z.string().nullable().describe("Finish timestamp"),
    }),
    execute: async (params, ctx) => {
      const org = ctx.credentials.organization;
      const body: any = { environment: params.environment };
      if (params.name) body.name = params.name;
      if (params.url) body.url = params.url;
      const d = await sentryPost(ctx, `/organizations/${enc(org)}/releases/${enc(params.version)}/deploys/`, body);
      return { id: d.id, environment: d.environment, date_started: d.dateStarted ?? null, date_finished: d.dateFinished ?? null };
    },
  },

  upload_sourcemaps: {
    description: "Upload source maps for a release (lists existing files; use sentry-cli for full upload).",
    params: z.object({
      project: z.string().describe("Project slug"),
      version: z.string().describe("Release version"),
      path: z.string().describe("Directory containing source maps"),
      url_prefix: z.string().default("~").describe("URL prefix to prepend to filenames"),
    }),
    returns: z.object({
      files_uploaded: z.number().describe("Number of files uploaded"),
      version: z.string().describe("Release version"),
    }),
    execute: async (params, ctx) => {
      const org = ctx.credentials.organization;
      const data = await sentryFetch(ctx, `/projects/${enc(org)}/${enc(params.project)}/releases/${enc(params.version)}/files/`);
      return { files_uploaded: Array.isArray(data) ? data.length : 0, version: params.version };
    },
  },
};
