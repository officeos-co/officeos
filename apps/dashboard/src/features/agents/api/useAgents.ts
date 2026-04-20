"use client";

import { gql, useMutation, useQuery } from "@apollo/client";
import { USE_MOCKS } from "@/lib/graphql/mock-mode";
import { mockAgentsList, type AgentListRow } from "../data/agents-list-mock";
import { mockAgent, type AgentDetail } from "../data/agent-mock";

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

/* ── Hooks ───────────────────────────────────────────────── */

export function useAgents(): {
  agents: AgentListRow[];
  loading: boolean;
  error?: Error;
  refetch: () => void;
} {
  const { data, loading, error, refetch } = useQuery(AGENTS_QUERY, { skip: USE_MOCKS });
  if (USE_MOCKS) return { agents: mockAgentsList, loading: false, refetch: () => {} };
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
  agent: AgentDetail | null;
  loading: boolean;
  error?: Error;
} {
  const { data, loading, error } = useQuery(AGENT_QUERY, {
    variables: { id },
    skip: USE_MOCKS || !id,
  });
  if (USE_MOCKS) return { agent: mockAgent, loading: false };
  const a = data?.agent as
    | {
        id: string;
        name: string;
        model: string;
        status: string;
        prompt: string;
        createdAt?: string;
      }
    | null
    | undefined;
  if (!a) return { agent: null, loading, error: error ?? undefined };
  const agent: AgentDetail = {
    id: a.id,
    name: a.name,
    model: a.model,
    status: (a.status ?? "stopped").toLowerCase(),
    prompt: a.prompt ?? "",
    integrations: [],
    channels: [],
    createdAt: a.createdAt ? Date.parse(a.createdAt) : Date.now(),
  };
  return { agent, loading, error: error ?? undefined };
}

/**
 * Input shape that maps onto backend `CreateAgentInput`. The dashboard thinks
 * in terms of `systemPrompt` + `toolNames` + `channelSlugs`; we translate to
 * `prompt` / `toolNames` / `channelSlugs` at the boundary. `provider` is
 * derived from the model prefix (claude- → anthropic, gpt- → openai, etc.).
 */
export type CreateAgentHookInput = {
  name: string;
  model: string;
  systemPrompt: string;
  toolNames: string[];
  /** Per-tool allow/deny overrides. `tool` is a "skill:tool" key. */
  toolPermissions: Array<{ tool: string; mode: "ALLOW" | "DENY" }>;
  channelSlugs: string[];
};

function providerFromModel(model: string): string {
  if (model.startsWith("claude-")) return "anthropic";
  if (model.startsWith("gpt-") || model.startsWith("o")) return "openai";
  if (model.startsWith("gemini-")) return "google";
  return "anthropic";
}

export function useCreateAgent() {
  const [fn, state] = useMutation(CREATE_AGENT);
  return {
    createAgent: async (input: CreateAgentHookInput) => {
      if (USE_MOCKS)
        return { id: `agt_mock_${Date.now().toString(36)}`, name: input.name };
      const { data } = await fn({
        variables: {
          input: {
            name: input.name,
            provider: providerFromModel(input.model),
            model: input.model,
            prompt: input.systemPrompt,
            toolNames: input.toolNames,
            toolPermissions: input.toolPermissions,
            channelSlugs: input.channelSlugs,
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
                    __typename: "Agent",
                    id: result.createAgent.id,
                    name: result.createAgent.name,
                    model: input.model,
                    status: "stopped",
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
        prompt: string;
        integrations: string[];
        channels: string[];
      }>,
    ) => {
      if (USE_MOCKS) return { id, name: input.name ?? mockAgent.name };
      const { data } = await fn({
        variables: { id, input },
        optimisticResponse: {
          updateAgent: {
            __typename: "Agent",
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
      if (USE_MOCKS) return true;
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
          cache.evict({ id: cache.identify({ __typename: "Agent", id }) });
          cache.gc();
        },
      });
      return Boolean(data?.deleteAgent);
    },
    ...state,
  };
}
