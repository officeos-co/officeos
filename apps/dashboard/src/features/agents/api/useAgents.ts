"use client";

import { gql, useMutation, useQuery } from "@apollo/client";

/* ── Queries / mutations ─────────────────────────────────── */

const AGENTS_QUERY = gql`
  query Agents {
    agents {
      id
      name
      model
      status
      createdAt
    }
  }
`;

const AGENT_QUERY = gql`
  query Agent($id: UUID!) {
    agent(id: $id) {
      id
      name
      model
      status
      prompt
      createdAt
      memories {
        id
        key
        content
        updatedAt
      }
      channelBindings {
        id
        channelConnectionId
      }
      personalityFiles {
        id
        fileName
        content
      }
      activeSession {
        id
        status
        messageCount
        lastActivityAt
        createdAt
        endedAt
      }
    }
  }
`;

const CREATE_AGENT = gql`
  mutation CreateAgent($input: CreateAgentInput!) {
    createAgent(input: $input) {
      id
      name
    }
  }
`;

const UPDATE_AGENT = gql`
  mutation UpdateAgent($id: UUID!, $input: UpdateAgentInput!) {
    updateAgent(id: $id, input: $input) {
      id
      name
    }
  }
`;

const DELETE_AGENT = gql`
  mutation DeleteAgent($id: UUID!) {
    deleteAgent(id: $id)
  }
`;

const AGENT_TOOL_PERMISSIONS = gql`
  query AgentToolPermissions($agentId: UUID!) {
    agentToolPermissions(agentId: $agentId) {
      skillName
      toolName
      mode
    }
  }
`;

const SET_AGENT_TOOL_PERMISSIONS = gql`
  mutation SetAgentToolPermissions($input: SetAgentToolPermissionsInput!) {
    setAgentToolPermissions(input: $input) {
      skillName
      toolName
      mode
    }
  }
`;

const AGENT_TOOL_CATALOG = gql`
  query AgentToolCatalog($agentId: UUID) {
    agentToolCatalog(agentId: $agentId) {
      group
      runtimeName
      permissionSkill
      permissionTool
      description
      deferred
    }
  }
`;

/* ── Helpers ─────────────────────────────────────────────── */

function humanAgo(iso: string | number | null | undefined): string {
  if (!iso) return "";
  const then = typeof iso === "number" ? iso : Date.parse(iso);
  if (Number.isNaN(then)) return "";
  const diffMs = Date.now() - then;
  const m = Math.floor(diffMs / 60000);
  if (m < 1) return "just now";
  if (m < 60) return `${m} minute${m === 1 ? "" : "s"} ago`;
  const h = Math.floor(m / 60);
  if (h < 24) return `${h} hour${h === 1 ? "" : "s"} ago`;
  const d = Math.floor(h / 24);
  if (d < 14) return `${d} day${d === 1 ? "" : "s"} ago`;
  const w = Math.floor(d / 7);
  return `${w} week${w === 1 ? "" : "s"} ago`;
}

/* ── Types ──────────────────────────────────────────────── */

export type AgentListRow = {
  id: string;
  name: string;
  model: string;
  status: string;
  created: string;
  updated: string;
};

export type AgentDetail = {
  id: string;
  name: string;
  model: string;
  status: string;
  prompt: string;
  integrations: string[];
  channels: string[];
  createdAt: number;
};

export type AgentMemory = {
  id: string;
  key: string;
  content: string;
  updatedAt: string;
};

export type AgentChannelBinding = {
  id: string;
  channelConnectionId: string;
};

export type AgentPersonalityFile = {
  id: string;
  fileName: string;
  content: string;
};

export type AgentFull = AgentDetail & {
  memories: AgentMemory[];
  channelBindings: AgentChannelBinding[];
  personalityFiles: AgentPersonalityFile[];
  activeSession: {
    id: string;
    status: string;
    messageCount: number;
    lastActivityAt: string;
    createdAt: string;
    endedAt: string | null;
  } | null;
};

/* ── Hooks ───────────────────────────────────────────────── */

export function useAgents(): {
  agents: AgentListRow[];
  loading: boolean;
  error?: Error;
  refetch: () => void;
} {
  const { data, loading, error, refetch } = useQuery(AGENTS_QUERY);
  const raw: Array<{
    id: string;
    name: string;
    model: string;
    status: string;
    createdAt?: string;
  }> = data?.agents ?? [];
  const agents: AgentListRow[] = raw.map((a) => ({
    id: a.id,
    name: a.name,
    model: a.model,
    status: (a.status ?? "stopped").toLowerCase(),
    created: humanAgo(a.createdAt),
    updated: humanAgo(a.createdAt),
  }));
  return { agents, loading, error: error ?? undefined, refetch };
}

export function useAgent(id: string): {
  agent: AgentFull | null;
  loading: boolean;
  error?: Error;
} {
  const { data, loading, error } = useQuery(AGENT_QUERY, {
    variables: { id },
    skip: !id,
  });
  const a = data?.agent;
  if (!a) return { agent: null, loading, error: error ?? undefined };
  const agent: AgentFull = {
    id: a.id,
    name: a.name,
    model: a.model,
    status: (a.status ?? "stopped").toLowerCase(),
    prompt: a.prompt ?? "",
    integrations: [],
    channels: [],
    createdAt: a.createdAt ? Date.parse(a.createdAt) : 0,
    memories: a.memories ?? [],
    channelBindings: a.channelBindings ?? [],
    personalityFiles: a.personalityFiles ?? [],
    activeSession: a.activeSession ?? null,
  };
  return { agent, loading, error: error ?? undefined };
}

export type CreateAgentHookInput = {
  name: string;
  model: string;
  provider: string;
  systemPrompt: string;
  toolNames: string[];
  toolPermissions: Array<{ tool: string; mode: "ALLOW" | "DENY" }>;
  channelSlugs: string[];
  resources?: Array<{
    resourceType: string;
    resourceId: string;
    accessMode?: string | null;
    instructions?: string | null;
  }>;
  bootstrapMessage?: string;
};

export function useCreateAgent() {
  const [fn, state] = useMutation(CREATE_AGENT);
  return {
    createAgent: async (input: CreateAgentHookInput) => {
      const { data } = await fn({
        variables: {
          input: {
            name: input.name,
            provider: input.provider,
            model: input.model,
            prompt: input.systemPrompt,
            toolNames: input.toolNames,
            toolPermissions: input.toolPermissions,
            channelSlugs: input.channelSlugs,
            resources: input.resources ?? [],
            bootstrapMessage: input.bootstrapMessage ?? null,
          },
        },
        update(cache, { data: result }) {
          if (!result?.createAgent) return;
          const existing = cache.readQuery<{ agents: unknown[] }>({ query: AGENTS_QUERY });
          if (existing) {
            cache.writeQuery({
              query: AGENTS_QUERY,
              data: {
                agents: [
                  ...existing.agents,
                  {
                    __typename: "AgentRecord",
                    id: result.createAgent.id,
                    name: result.createAgent.name,
                    model: input.model,
                    status: "pending",
                    createdAt: new Date().toISOString(),
                  },
                ],
              },
            });
          }
        },
      });
      return data?.createAgent as { id: string; name: string };
    },
    ...state,
  };
}

export function useUpdateAgent() {
  const [fn, state] = useMutation(UPDATE_AGENT);
  return {
    updateAgent: async (
      id: string,
      input: Partial<{
        name: string;
        model: string;
        provider: string;
        prompt: string;
      }>,
    ) => {
      const { data } = await fn({
        variables: { id, input },
        optimisticResponse: {
          updateAgent: {
            __typename: "AgentRecord",
            id,
            name: input.name ?? "",
          },
        },
      });
      return data?.updateAgent as { id: string; name: string };
    },
    ...state,
  };
}

export function useDeleteAgent() {
  const [fn, state] = useMutation(DELETE_AGENT);
  return {
    deleteAgent: async (id: string) => {
      const { data } = await fn({
        variables: { id },
        optimisticResponse: { deleteAgent: true },
        update(cache) {
          const existing = cache.readQuery<{ agents: Array<{ id: string }> }>({ query: AGENTS_QUERY });
          if (existing) {
            cache.writeQuery({
              query: AGENTS_QUERY,
              data: { agents: existing.agents.filter((a) => a.id !== id) },
            });
          }
          cache.evict({ id: cache.identify({ __typename: "AgentRecord", id }) });
          cache.gc();
        },
      });
      return Boolean(data?.deleteAgent);
    },
    ...state,
  };
}

export type AgentToolPermission = {
  skillName: string;
  toolName: string;
  mode: "ALLOW" | "DENY";
};

export function useAgentToolPermissions(agentId: string): {
  permissions: AgentToolPermission[];
  loading: boolean;
  error?: Error;
} {
  const { data, loading, error } = useQuery(AGENT_TOOL_PERMISSIONS, {
    variables: { agentId },
    skip: !agentId,
  });
  return {
    permissions: data?.agentToolPermissions ?? [],
    loading,
    error: error ?? undefined,
  };
}

export function useSetAgentToolPermissions() {
  const [fn, state] = useMutation(SET_AGENT_TOOL_PERMISSIONS);
  return {
    setAgentToolPermissions: async (
      agentId: string,
      entries: Array<{ skill: string; tool: string; mode: "ALLOW" | "DENY" }>,
    ) => {
      const { data } = await fn({
        variables: { input: { agentId, entries } },
        refetchQueries: [{ query: AGENT_TOOL_PERMISSIONS, variables: { agentId } }],
      });
      return data?.setAgentToolPermissions as AgentToolPermission[];
    },
    ...state,
  };
}

export type AgentToolCatalogEntry = {
  group: string;
  runtimeName: string;
  permissionSkill: string;
  permissionTool: string;
  description: string;
  deferred: boolean;
};

export function useAgentToolCatalog(agentId?: string): {
  tools: AgentToolCatalogEntry[];
  loading: boolean;
  error?: Error;
} {
  const { data, loading, error } = useQuery(AGENT_TOOL_CATALOG, {
    variables: { agentId: agentId ?? null },
    fetchPolicy: "cache-and-network",
  });
  return {
    tools: data?.agentToolCatalog ?? [],
    loading,
    error: error ?? undefined,
  };
}
