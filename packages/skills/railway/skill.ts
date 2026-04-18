import { defineSkill, z } from "@harro/skill-sdk";
import manifest from "./skill.json" with { type: "json" };
import doc from "./SKILL.md";

const RAILWAY_GQL = "https://backboard.railway.app/graphql/v2";

async function railwayQuery(
  ctx: { fetch: typeof globalThis.fetch; credentials: Record<string, string> },
  query: string,
  variables?: Record<string, unknown>,
): Promise<unknown> {
  const res = await ctx.fetch(RAILWAY_GQL, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
      Authorization: `Bearer ${ctx.credentials.api_token}`,
    },
    body: JSON.stringify({ query, variables }),
  });
  if (!res.ok) {
    const text = await res.text();
    throw new Error(`Railway API ${res.status}: ${text}`);
  }
  const json = await res.json();
  if (json.errors?.length) {
    throw new Error(`Railway GraphQL error: ${json.errors.map((e: { message: string }) => e.message).join("; ")}`);
  }
  return json.data;
}

export default defineSkill({
  ...manifest,
  doc,

  actions: {
    // ── Projects ──────────────────────────────────────────────────────

    projects_list: {
      description: "List all projects accessible to the authenticated user.",
      params: z.object({}),
      returns: z.array(
        z.object({
          id: z.string(),
          name: z.string(),
          description: z.string().nullable(),
          createdAt: z.string(),
        }),
      ),
      execute: async (_params, ctx) => {
        const data: any = await railwayQuery(ctx, `
          query { me { projects { edges { node { id name description createdAt } } } } }
        `);
        return data.me.projects.edges.map((e: any) => e.node);
      },
    },

    projects_get: {
      description: "Get a specific project by ID.",
      params: z.object({
        project_id: z.string().describe("Railway project ID"),
      }),
      returns: z.object({
        id: z.string(),
        name: z.string(),
        description: z.string().nullable(),
        createdAt: z.string(),
        environments: z.array(z.object({ id: z.string(), name: z.string() })),
      }),
      execute: async (params, ctx) => {
        const data: any = await railwayQuery(
          ctx,
          `query($id: String!) {
            project(id: $id) {
              id name description createdAt
              environments { edges { node { id name } } }
            }
          }`,
          { id: params.project_id },
        );
        const p = data.project;
        return { ...p, environments: p.environments.edges.map((e: any) => e.node) };
      },
    },

    projects_create: {
      description: "Create a new Railway project.",
      params: z.object({
        name: z.string().describe("Project name"),
        description: z.string().optional().describe("Project description"),
      }),
      returns: z.object({ id: z.string(), name: z.string() }),
      execute: async (params, ctx) => {
        const data: any = await railwayQuery(
          ctx,
          `mutation($input: ProjectCreateInput!) { projectCreate(input: $input) { id name } }`,
          { input: { name: params.name, description: params.description } },
        );
        return data.projectCreate;
      },
    },

    projects_delete: {
      description: "Delete a Railway project by ID.",
      params: z.object({
        project_id: z.string().describe("Railway project ID"),
      }),
      returns: z.object({ success: z.boolean() }),
      execute: async (params, ctx) => {
        await railwayQuery(
          ctx,
          `mutation($id: String!) { projectDelete(id: $id) }`,
          { id: params.project_id },
        );
        return { success: true };
      },
    },

    // ── Services ──────────────────────────────────────────────────────

    services_list: {
      description: "List all services in a project.",
      params: z.object({
        project_id: z.string().describe("Railway project ID"),
      }),
      returns: z.array(z.object({ id: z.string(), name: z.string(), createdAt: z.string() })),
      execute: async (params, ctx) => {
        const data: any = await railwayQuery(
          ctx,
          `query($id: String!) {
            project(id: $id) {
              services { edges { node { id name createdAt } } }
            }
          }`,
          { id: params.project_id },
        );
        return data.project.services.edges.map((e: any) => e.node);
      },
    },

    services_get: {
      description: "Get details of a specific service.",
      params: z.object({
        service_id: z.string().describe("Railway service ID"),
      }),
      returns: z.object({
        id: z.string(),
        name: z.string(),
        createdAt: z.string(),
      }),
      execute: async (params, ctx) => {
        const data: any = await railwayQuery(
          ctx,
          `query($id: String!) { service(id: $id) { id name createdAt } }`,
          { id: params.service_id },
        );
        return data.service;
      },
    },

    services_create: {
      description: "Create a new service in a project.",
      params: z.object({
        project_id: z.string().describe("Railway project ID"),
        name: z.string().describe("Service name"),
      }),
      returns: z.object({ id: z.string(), name: z.string() }),
      execute: async (params, ctx) => {
        const data: any = await railwayQuery(
          ctx,
          `mutation($input: ServiceCreateInput!) { serviceCreate(input: $input) { id name } }`,
          { input: { projectId: params.project_id, name: params.name } },
        );
        return data.serviceCreate;
      },
    },

    services_delete: {
      description: "Delete a service.",
      params: z.object({
        service_id: z.string().describe("Railway service ID"),
      }),
      returns: z.object({ success: z.boolean() }),
      execute: async (params, ctx) => {
        await railwayQuery(
          ctx,
          `mutation($id: String!) { serviceDelete(id: $id) }`,
          { id: params.service_id },
        );
        return { success: true };
      },
    },

    // ── Deployments ───────────────────────────────────────────────────

    deployments_list: {
      description: "List deployments for a service in an environment.",
      params: z.object({
        service_id: z.string().describe("Railway service ID"),
        environment_id: z.string().describe("Railway environment ID"),
        limit: z.number().int().min(1).max(100).default(20).describe("Maximum number of deployments to return"),
      }),
      returns: z.array(
        z.object({ id: z.string(), status: z.string(), createdAt: z.string(), url: z.string().nullable() }),
      ),
      execute: async (params, ctx) => {
        const data: any = await railwayQuery(
          ctx,
          `query($serviceId: String!, $environmentId: String!) {
            deployments(input: { serviceId: $serviceId, environmentId: $environmentId }) {
              edges { node { id status createdAt url } }
            }
          }`,
          { serviceId: params.service_id, environmentId: params.environment_id },
        );
        return data.deployments.edges.slice(0, params.limit).map((e: any) => e.node);
      },
    },

    deployments_get: {
      description: "Get details of a specific deployment.",
      params: z.object({
        deployment_id: z.string().describe("Railway deployment ID"),
      }),
      returns: z.object({
        id: z.string(),
        status: z.string(),
        createdAt: z.string(),
        url: z.string().nullable(),
      }),
      execute: async (params, ctx) => {
        const data: any = await railwayQuery(
          ctx,
          `query($id: String!) { deployment(id: $id) { id status createdAt url } }`,
          { id: params.deployment_id },
        );
        return data.deployment;
      },
    },

    deployments_trigger: {
      description: "Trigger a new deployment (redeploy latest) for a service.",
      params: z.object({
        service_id: z.string().describe("Railway service ID"),
        environment_id: z.string().describe("Railway environment ID"),
      }),
      returns: z.object({ id: z.string(), status: z.string() }),
      execute: async (params, ctx) => {
        const data: any = await railwayQuery(
          ctx,
          `mutation($serviceId: String!, $environmentId: String!) {
            serviceInstanceRedeploy(serviceId: $serviceId, environmentId: $environmentId) { id status }
          }`,
          { serviceId: params.service_id, environmentId: params.environment_id },
        );
        return data.serviceInstanceRedeploy;
      },
    },

    deployments_cancel: {
      description: "Cancel a running deployment.",
      params: z.object({
        deployment_id: z.string().describe("Railway deployment ID"),
      }),
      returns: z.object({ success: z.boolean() }),
      execute: async (params, ctx) => {
        await railwayQuery(
          ctx,
          `mutation($id: String!) { deploymentCancel(id: $id) }`,
          { id: params.deployment_id },
        );
        return { success: true };
      },
    },

    deployments_rollback: {
      description: "Rollback to a previous deployment.",
      params: z.object({
        deployment_id: z.string().describe("Railway deployment ID to rollback to"),
      }),
      returns: z.object({ id: z.string(), status: z.string() }),
      execute: async (params, ctx) => {
        const data: any = await railwayQuery(
          ctx,
          `mutation($id: String!) { deploymentRollback(id: $id) { id status } }`,
          { id: params.deployment_id },
        );
        return data.deploymentRollback;
      },
    },

    // ── Variables ─────────────────────────────────────────────────────

    variables_list: {
      description: "List all variables for a service in an environment.",
      params: z.object({
        project_id: z.string().describe("Railway project ID"),
        service_id: z.string().describe("Railway service ID"),
        environment_id: z.string().describe("Railway environment ID"),
      }),
      returns: z.record(z.string()),
      execute: async (params, ctx) => {
        const data: any = await railwayQuery(
          ctx,
          `query($projectId: String!, $serviceId: String!, $environmentId: String!) {
            variables(projectId: $projectId, serviceId: $serviceId, environmentId: $environmentId)
          }`,
          {
            projectId: params.project_id,
            serviceId: params.service_id,
            environmentId: params.environment_id,
          },
        );
        return data.variables;
      },
    },

    variables_upsert: {
      description: "Create or update variables for a service in an environment.",
      params: z.object({
        project_id: z.string().describe("Railway project ID"),
        service_id: z.string().describe("Railway service ID"),
        environment_id: z.string().describe("Railway environment ID"),
        variables: z.record(z.string()).describe("Key-value pairs of variables to set"),
      }),
      returns: z.object({ success: z.boolean() }),
      execute: async (params, ctx) => {
        await railwayQuery(
          ctx,
          `mutation($input: VariableCollectionUpsertInput!) { variableCollectionUpsert(input: $input) }`,
          {
            input: {
              projectId: params.project_id,
              serviceId: params.service_id,
              environmentId: params.environment_id,
              variables: params.variables,
            },
          },
        );
        return { success: true };
      },
    },

    variables_delete: {
      description: "Delete a variable from a service environment.",
      params: z.object({
        project_id: z.string().describe("Railway project ID"),
        service_id: z.string().describe("Railway service ID"),
        environment_id: z.string().describe("Railway environment ID"),
        name: z.string().describe("Variable name to delete"),
      }),
      returns: z.object({ success: z.boolean() }),
      execute: async (params, ctx) => {
        await railwayQuery(
          ctx,
          `mutation($input: VariableDeleteInput!) { variableDelete(input: $input) }`,
          {
            input: {
              projectId: params.project_id,
              serviceId: params.service_id,
              environmentId: params.environment_id,
              name: params.name,
            },
          },
        );
        return { success: true };
      },
    },

    // ── Domains ───────────────────────────────────────────────────────

    domains_list: {
      description: "List domains for a service in an environment.",
      params: z.object({
        service_id: z.string().describe("Railway service ID"),
        environment_id: z.string().describe("Railway environment ID"),
      }),
      returns: z.array(z.object({ id: z.string(), domain: z.string(), createdAt: z.string() })),
      execute: async (params, ctx) => {
        const data: any = await railwayQuery(
          ctx,
          `query($serviceId: String!, $environmentId: String!) {
            domains(serviceId: $serviceId, environmentId: $environmentId) {
              customDomains { edges { node { id domain createdAt } } }
              serviceDomains { edges { node { id domain createdAt } } }
            }
          }`,
          { serviceId: params.service_id, environmentId: params.environment_id },
        );
        return [
          ...data.domains.customDomains.edges.map((e: any) => e.node),
          ...data.domains.serviceDomains.edges.map((e: any) => e.node),
        ];
      },
    },

    domains_create: {
      description: "Add a custom domain to a service.",
      params: z.object({
        service_id: z.string().describe("Railway service ID"),
        environment_id: z.string().describe("Railway environment ID"),
        domain: z.string().describe("Custom domain to add (e.g. app.example.com)"),
      }),
      returns: z.object({ id: z.string(), domain: z.string() }),
      execute: async (params, ctx) => {
        const data: any = await railwayQuery(
          ctx,
          `mutation($input: CustomDomainCreateInput!) {
            customDomainCreate(input: $input) { id domain }
          }`,
          {
            input: {
              serviceId: params.service_id,
              environmentId: params.environment_id,
              domain: params.domain,
            },
          },
        );
        return data.customDomainCreate;
      },
    },

    domains_delete: {
      description: "Remove a custom domain from a service.",
      params: z.object({
        domain_id: z.string().describe("Railway domain ID"),
      }),
      returns: z.object({ success: z.boolean() }),
      execute: async (params, ctx) => {
        await railwayQuery(
          ctx,
          `mutation($id: String!) { customDomainDelete(id: $id) }`,
          { id: params.domain_id },
        );
        return { success: true };
      },
    },

    // ── Logs ──────────────────────────────────────────────────────────

    logs_deployment: {
      description: "Fetch runtime logs for a deployment.",
      params: z.object({
        deployment_id: z.string().describe("Railway deployment ID"),
        limit: z.number().int().min(1).max(500).default(100).describe("Maximum number of log lines"),
      }),
      returns: z.array(
        z.object({ timestamp: z.string(), message: z.string(), severity: z.string().optional() }),
      ),
      execute: async (params, ctx) => {
        const data: any = await railwayQuery(
          ctx,
          `query($deploymentId: String!, $limit: Int!) {
            deploymentLogs(deploymentId: $deploymentId, limit: $limit) {
              timestamp message severity
            }
          }`,
          { deploymentId: params.deployment_id, limit: params.limit },
        );
        return data.deploymentLogs;
      },
    },

    logs_build: {
      description: "Fetch build logs for a deployment.",
      params: z.object({
        deployment_id: z.string().describe("Railway deployment ID"),
      }),
      returns: z.array(z.object({ timestamp: z.string(), message: z.string() })),
      execute: async (params, ctx) => {
        const data: any = await railwayQuery(
          ctx,
          `query($deploymentId: String!) {
            buildLogs(deploymentId: $deploymentId) { timestamp message }
          }`,
          { deploymentId: params.deployment_id },
        );
        return data.buildLogs;
      },
    },

    // ── Environments ──────────────────────────────────────────────────

    environments_list: {
      description: "List environments in a project.",
      params: z.object({
        project_id: z.string().describe("Railway project ID"),
      }),
      returns: z.array(z.object({ id: z.string(), name: z.string(), createdAt: z.string() })),
      execute: async (params, ctx) => {
        const data: any = await railwayQuery(
          ctx,
          `query($id: String!) {
            project(id: $id) {
              environments { edges { node { id name createdAt } } }
            }
          }`,
          { id: params.project_id },
        );
        return data.project.environments.edges.map((e: any) => e.node);
      },
    },
  },
});
