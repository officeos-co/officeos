import { z } from "@harro/skill-sdk";
import type { ActionDefinition } from "@harro/skill-sdk";
import { nerdgraph } from "../core/client.ts";

export const deployments: Record<string, ActionDefinition> = {
  record_deployment: {
    description: "Record a deployment marker in New Relic change tracking.",
    params: z.object({
      entity_guid: z.string().describe("Entity GUID of the application being deployed"),
      version: z.string().describe("Deployment version, tag, or build number"),
      description: z.string().optional().describe("Deployment description"),
      user: z.string().optional().describe("User or system that triggered the deployment"),
      changelog: z.string().optional().describe("Changelog text"),
      commit: z.string().optional().describe("Git commit SHA"),
    }),
    returns: z.object({
      id: z.string().optional(),
      version: z.string(),
      timestamp: z.string().optional(),
      entityGuid: z.string().optional(),
    }),
    execute: async (params, ctx) => {
      const { api_key } = ctx.credentials;
      const deployment: Record<string, any> = {
        version: params.version,
        entityGuid: params.entity_guid,
      };
      if (params.description) deployment.description = params.description;
      if (params.user) deployment.user = params.user;
      if (params.changelog) deployment.changelog = params.changelog;
      if (params.commit) deployment.commit = params.commit;
      const gql = `
        mutation RecordDeployment($deployment: ChangeTrackingDeploymentInput!) {
          changeTrackingCreateDeployment(deployment: $deployment) {
            deploymentId
            entityGuid
            version
            timestamp
          }
        }
      `;
      const data = await nerdgraph(ctx.fetch, api_key, gql, { deployment });
      const result = data?.changeTrackingCreateDeployment;
      return {
        id: result?.deploymentId,
        version: result?.version,
        timestamp: result?.timestamp,
        entityGuid: result?.entityGuid,
      };
    },
  },

  list_deployments: {
    description: "List recent deployments for an entity.",
    params: z.object({
      entity_guid: z.string().describe("Entity GUID to list deployments for"),
      limit: z.number().int().max(100).default(10).describe("Max results"),
    }),
    returns: z.array(z.object({
      id: z.string().optional(),
      version: z.string(),
      description: z.string().optional(),
      user: z.string().optional(),
      timestamp: z.string().optional(),
      changelog: z.string().optional(),
    })),
    execute: async (params, ctx) => {
      const { api_key } = ctx.credentials;
      const gql = `
        query ListDeployments($guid: EntityGuid!) {
          actor {
            entity(guid: $guid) {
              ... on ApmApplicationEntity {
                deployments(limit: ${params.limit}) {
                  deploymentId
                  version
                  description
                  user
                  timestamp
                  changelog
                }
              }
            }
          }
        }
      `;
      const data = await nerdgraph(ctx.fetch, api_key, gql, { guid: params.entity_guid });
      const deployments = data?.actor?.entity?.deployments ?? [];
      return deployments.map((d: any) => ({
        id: d.deploymentId,
        version: d.version,
        description: d.description,
        user: d.user,
        timestamp: d.timestamp,
        changelog: d.changelog,
      }));
    },
  },
};
