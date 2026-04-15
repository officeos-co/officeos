import { z } from "@harro/skill-sdk";
import type { ActionDefinition } from "@harro/skill-sdk";
import { cfFetch, cfPost, enc } from "../core/client.ts";

export const ssl: Record<string, ActionDefinition> = {
  list_certificates: {
    description: "List SSL certificates for a zone.",
    params: z.object({
      zone_id: z.string().describe("Zone ID"),
    }),
    returns: z.array(
      z.object({
        id: z.string(),
        hosts: z.array(z.string()),
        issuer: z.string(),
        status: z.string(),
        expires_on: z.string(),
      }),
    ),
    execute: async (params, ctx) => {
      const data = await cfFetch(ctx, `/zones/${enc(params.zone_id)}/ssl/certificate_packs`);
      return (Array.isArray(data) ? data : []).map((c: any) => ({
        id: c.id,
        hosts: c.hosts ?? [],
        issuer: c.certificate_authority ?? "",
        status: c.status ?? "",
        expires_on: c.certificates?.[0]?.expires_on ?? "",
      }));
    },
  },

  get_ssl_settings: {
    description: "Get SSL/TLS settings for a zone.",
    params: z.object({
      zone_id: z.string().describe("Zone ID"),
    }),
    returns: z.object({
      mode: z.string().describe("SSL mode (off, flexible, full, strict)"),
      certificate_status: z.string(),
      min_tls_version: z.string(),
      tls_1_3: z.string(),
    }),
    execute: async (params, ctx) => {
      const [ssl, minTls, tls13] = await Promise.all([
        cfFetch(ctx, `/zones/${enc(params.zone_id)}/settings/ssl`),
        cfFetch(ctx, `/zones/${enc(params.zone_id)}/settings/min_tls_version`),
        cfFetch(ctx, `/zones/${enc(params.zone_id)}/settings/tls_1_3`),
      ]);
      return {
        mode: ssl?.value ?? "",
        certificate_status: ssl?.certificate_status ?? "",
        min_tls_version: minTls?.value ?? "",
        tls_1_3: tls13?.value ?? "",
      };
    },
  },

  update_ssl_settings: {
    description: "Update SSL/TLS settings for a zone.",
    params: z.object({
      zone_id: z.string().describe("Zone ID"),
      mode: z.string().optional().describe("off, flexible, full, or strict"),
      min_tls_version: z.string().optional().describe("1.0, 1.1, 1.2, or 1.3"),
      tls_1_3: z.string().optional().describe("on, off, or zrt"),
    }),
    returns: z.object({ mode: z.string(), min_tls_version: z.string(), tls_1_3: z.string() }),
    execute: async (params, ctx) => {
      if (params.mode !== undefined) {
        await cfPost(ctx, `/zones/${enc(params.zone_id)}/settings/ssl`, { value: params.mode }, "PATCH");
      }
      if (params.min_tls_version !== undefined) {
        await cfPost(ctx, `/zones/${enc(params.zone_id)}/settings/min_tls_version`, { value: params.min_tls_version }, "PATCH");
      }
      if (params.tls_1_3 !== undefined) {
        await cfPost(ctx, `/zones/${enc(params.zone_id)}/settings/tls_1_3`, { value: params.tls_1_3 }, "PATCH");
      }
      const [ssl, minTls, tls13] = await Promise.all([
        cfFetch(ctx, `/zones/${enc(params.zone_id)}/settings/ssl`),
        cfFetch(ctx, `/zones/${enc(params.zone_id)}/settings/min_tls_version`),
        cfFetch(ctx, `/zones/${enc(params.zone_id)}/settings/tls_1_3`),
      ]);
      return { mode: ssl?.value ?? "", min_tls_version: minTls?.value ?? "", tls_1_3: tls13?.value ?? "" };
    },
  },
};
