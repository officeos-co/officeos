import { z } from "@harro/skill-sdk";
import type { ActionDefinition } from "@harro/skill-sdk";
import { r53Fetch, xmlVal, xmlAll, xmlBlocks } from "../core/client.ts";

export const route53: Record<string, ActionDefinition> = {
  list_hosted_zones: {
    description: "List Route53 hosted zones.",
    params: z.object({
      per_page: z.number().min(1).max(100).default(100).describe("Results per page"),
    }),
    returns: z.array(z.object({
      zone_id: z.string().describe("Hosted zone ID"),
      name: z.string().describe("Zone name"),
      record_count: z.number().describe("Record count"),
      private_zone: z.boolean().describe("Whether zone is private"),
      comment: z.string().describe("Zone comment"),
    })),
    execute: async (params, ctx) => {
      const xml = await r53Fetch(ctx, "GET", `/2013-04-01/hostedzone?maxitems=${params.per_page}`);
      return xmlBlocks(xml, "HostedZone").map((b) => ({
        zone_id: xmlVal(b, "Id").replace("/hostedzone/", ""),
        name: xmlVal(b, "Name"),
        record_count: parseInt(xmlVal(b, "ResourceRecordSetCount") || "0", 10),
        private_zone: xmlVal(b, "PrivateZone") === "true",
        comment: xmlVal(b, "Comment") || "",
      }));
    },
  },

  list_records: {
    description: "List DNS records in a Route53 hosted zone.",
    params: z.object({
      zone_id: z.string().describe("Hosted zone ID"),
      type: z.string().optional().describe("Filter by type: A, AAAA, CNAME, MX, TXT, NS, SOA, SRV"),
    }),
    returns: z.array(z.object({
      name: z.string().describe("Record name"),
      type: z.string().describe("Record type"),
      ttl: z.number().nullable().describe("TTL"),
      values: z.array(z.string()).describe("Record values"),
      alias_target: z.any().nullable().describe("Alias target"),
    })),
    execute: async (params, ctx) => {
      let path = `/2013-04-01/hostedzone/${params.zone_id}/rrset`;
      if (params.type) path += `?type=${params.type}`;
      const xml = await r53Fetch(ctx, "GET", path);
      return xmlBlocks(xml, "ResourceRecordSet").map((b) => {
        const aliasBlock = b.match(/<AliasTarget>[\s\S]*?<\/AliasTarget>/)?.[0];
        return {
          name: xmlVal(b, "Name"),
          type: xmlVal(b, "Type"),
          ttl: xmlVal(b, "TTL") ? parseInt(xmlVal(b, "TTL"), 10) : null,
          values: xmlAll(b, "Value"),
          alias_target: aliasBlock ? {
            dns_name: xmlVal(aliasBlock, "DNSName"),
            zone_id: xmlVal(aliasBlock, "HostedZoneId"),
            evaluate_health: xmlVal(aliasBlock, "EvaluateTargetHealth") === "true",
          } : null,
        };
      });
    },
  },

  create_record: {
    description: "Create a DNS record in Route53.",
    params: z.object({
      zone_id: z.string().describe("Hosted zone ID"),
      name: z.string().describe("Record name (FQDN)"),
      type: z.string().describe("Record type (A, CNAME, TXT, etc.)"),
      value: z.string().optional().describe("Record value (required unless alias)"),
      ttl: z.number().default(300).describe("TTL in seconds"),
      alias_target: z.string().optional().describe("JSON with dns_name, zone_id, evaluate_health"),
    }),
    returns: z.object({
      change_id: z.string().describe("Change ID"),
      status: z.string().describe("Change status"),
      submitted_at: z.string().describe("Submission time"),
    }),
    execute: async (params, ctx) => {
      let rrBlock: string;
      if (params.alias_target) {
        const alias = JSON.parse(params.alias_target);
        rrBlock = `<AliasTarget><HostedZoneId>${alias.zone_id}</HostedZoneId><DNSName>${alias.dns_name}</DNSName><EvaluateTargetHealth>${alias.evaluate_health ?? false}</EvaluateTargetHealth></AliasTarget>`;
      } else {
        rrBlock = `<TTL>${params.ttl}</TTL><ResourceRecords><ResourceRecord><Value>${params.value}</Value></ResourceRecord></ResourceRecords>`;
      }
      const body = `<?xml version="1.0" encoding="UTF-8"?><ChangeResourceRecordSetsRequest xmlns="https://route53.amazonaws.com/doc/2013-04-01/"><ChangeBatch><Changes><Change><Action>CREATE</Action><ResourceRecordSet><Name>${params.name}</Name><Type>${params.type}</Type>${rrBlock}</ResourceRecordSet></Change></Changes></ChangeBatch></ChangeResourceRecordSetsRequest>`;
      const xml = await r53Fetch(ctx, "POST", `/2013-04-01/hostedzone/${params.zone_id}/rrset`, body);
      return {
        change_id: xmlVal(xml, "Id").replace("/change/", ""),
        status: xmlVal(xml, "Status"),
        submitted_at: xmlVal(xml, "SubmittedAt"),
      };
    },
  },

  delete_record: {
    description: "Delete a DNS record from Route53.",
    params: z.object({
      zone_id: z.string().describe("Hosted zone ID"),
      name: z.string().describe("Record name (FQDN)"),
      type: z.string().describe("Record type"),
      value: z.string().describe("Current record value"),
      ttl: z.number().optional().describe("Current TTL"),
    }),
    returns: z.object({
      change_id: z.string().describe("Change ID"),
      status: z.string().describe("Change status"),
    }),
    execute: async (params, ctx) => {
      const ttl = params.ttl ?? 300;
      const body = `<?xml version="1.0" encoding="UTF-8"?><ChangeResourceRecordSetsRequest xmlns="https://route53.amazonaws.com/doc/2013-04-01/"><ChangeBatch><Changes><Change><Action>DELETE</Action><ResourceRecordSet><Name>${params.name}</Name><Type>${params.type}</Type><TTL>${ttl}</TTL><ResourceRecords><ResourceRecord><Value>${params.value}</Value></ResourceRecord></ResourceRecords></ResourceRecordSet></Change></Changes></ChangeBatch></ChangeResourceRecordSetsRequest>`;
      const xml = await r53Fetch(ctx, "POST", `/2013-04-01/hostedzone/${params.zone_id}/rrset`, body);
      return {
        change_id: xmlVal(xml, "Id").replace("/change/", ""),
        status: xmlVal(xml, "Status"),
      };
    },
  },
};
