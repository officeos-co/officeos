"use client";

import { gql, useMutation, useQuery } from "@apollo/client";

export type RoutineTriggerKind = "api" | "github" | "schedule";

export type RoutineTrigger = {
  id: string;
  kind: string;
  name: string;
  enabled: boolean;
  configJson: string;
  lastTriggeredAt: string | null;
  nextRunAt: string | null;
  createdAt: string;
};

export type AgentRoutine = {
  id: string;
  agentId: string;
  agentName?: string;
  name: string;
  prompt: string;
  enabled: boolean;
  lastTriggeredAt: string | null;
  createdAt: string;
  triggers: RoutineTrigger[];
};

export type CreateRoutineInput = {
  agentId: string;
  name: string;
  prompt: string;
  scheduleTriggers: Array<{ name: string; expression: string }>;
  apiTriggers: Array<{ name: string }>;
  gitHubTriggers: Array<{
    name: string;
    owner: string;
    repo: string;
    events: string[];
    secret: string;
  }>;
};

export type RoutineGeneratedSecret = {
  triggerId: string;
  kind: string;
  name: string;
  secret: string;
};

export type CreateRoutineResult = {
  routine: AgentRoutine;
  generatedSecrets: RoutineGeneratedSecret[];
};

export type GitHubRoutineTriggerConfig = {
  owner?: string;
  repo?: string;
  events?: string[];
};

const ROUTINES_FOR_AGENT_QUERY = gql`
  query RoutinesForAgent($agentId: UUID!) {
    routinesForAgent(agentId: $agentId) {
      id
      agentId
      name
      prompt
      enabled
      lastTriggeredAt
      createdAt
      triggers {
        id
        kind
        name
        enabled
        configJson
        lastTriggeredAt
        nextRunAt
        createdAt
      }
    }
  }
`;

const ALL_ROUTINES_QUERY = gql`
  query AgentRoutines {
    agentRoutines {
      id
      agentId
      agentName
      name
      prompt
      enabled
      lastTriggeredAt
      createdAt
      triggers {
        id
        kind
        name
        enabled
        configJson
        lastTriggeredAt
        nextRunAt
        createdAt
      }
    }
  }
`;

const ROUTINE_QUERY = gql`
  query AgentRoutine($id: UUID!) {
    agentRoutine(id: $id) {
      id
      agentId
      agentName
      name
      prompt
      enabled
      lastTriggeredAt
      createdAt
      triggers {
        id
        kind
        name
        enabled
        configJson
        lastTriggeredAt
        nextRunAt
        createdAt
      }
    }
  }
`;

const CREATE_ROUTINE = gql`
  mutation CreateAgentRoutine($input: CreateAgentRoutineInput!) {
    createAgentRoutine(input: $input) {
      routine {
        id
        agentId
        agentName
        name
        prompt
        enabled
        lastTriggeredAt
        createdAt
        triggers {
          id
          kind
          name
          enabled
          configJson
          lastTriggeredAt
          nextRunAt
          createdAt
        }
      }
      generatedSecrets {
        triggerId
        kind
        name
        secret
      }
    }
  }
`;

const SET_ROUTINE_ENABLED = gql`
  mutation SetAgentRoutineEnabled($id: UUID!, $enabled: Boolean!) {
    setAgentRoutineEnabled(id: $id, enabled: $enabled)
  }
`;

const DELETE_ROUTINE = gql`
  mutation DeleteAgentRoutine($id: UUID!) {
    deleteAgentRoutine(id: $id)
  }
`;

type CreateRoutinePayload = {
  createAgentRoutine: CreateRoutineResult;
};

export function parseScheduleExpression(trigger: RoutineTrigger): string {
  try {
    const config = JSON.parse(trigger.configJson) as { expression?: unknown };
    return typeof config.expression === "string" ? config.expression : "";
  } catch {
    return "";
  }
}

export function parseGitHubTriggerConfig(
  trigger: RoutineTrigger,
): GitHubRoutineTriggerConfig {
  try {
    const config = JSON.parse(trigger.configJson) as GitHubRoutineTriggerConfig;
    return config && typeof config === "object" ? config : {};
  } catch {
    return {};
  }
}

export function getScheduleTriggers(routine: AgentRoutine): RoutineTrigger[] {
  return routine.triggers.filter((trigger) => trigger.kind === "schedule");
}

export function getTriggerKinds(routine: AgentRoutine): RoutineTriggerKind[] {
  const kinds = new Set<RoutineTriggerKind>();
  for (const trigger of routine.triggers) {
    if (
      trigger.kind === "api" ||
      trigger.kind === "github" ||
      trigger.kind === "schedule"
    ) {
      kinds.add(trigger.kind);
    }
  }
  return Array.from(kinds);
}

export function latestRoutineRunAt(routine: AgentRoutine): string | null {
  const triggerRuns = routine.triggers
    .map((trigger) => trigger.lastTriggeredAt)
    .filter((value): value is string => Boolean(value));
  return [routine.lastTriggeredAt, ...triggerRuns]
    .filter((value): value is string => Boolean(value))
    .sort((a, b) => Date.parse(b) - Date.parse(a))[0] ?? null;
}

export function nextRoutineRunAt(routine: AgentRoutine): string | null {
  const nextRuns = getScheduleTriggers(routine)
    .map((trigger) => trigger.nextRunAt)
    .filter((value): value is string => Boolean(value))
    .sort((a, b) => Date.parse(a) - Date.parse(b));
  return nextRuns[0] ?? null;
}

export function routineMatchesSearch(routine: AgentRoutine, query: string) {
  const normalized = query.trim().toLowerCase();
  if (!normalized) return true;
  const triggerText = routine.triggers
    .map((trigger) => {
      const github = parseGitHubTriggerConfig(trigger);
      return [
        trigger.kind,
        trigger.name,
        parseScheduleExpression(trigger),
        github.owner,
        github.repo,
        github.events?.join(" "),
      ]
        .filter(Boolean)
        .join(" ");
    })
    .join(" ");

  return [
    routine.id,
    routine.name,
    routine.agentName,
    routine.prompt,
    routine.enabled ? "enabled" : "disabled",
    triggerText,
  ]
    .filter(Boolean)
    .some((value) => value!.toLowerCase().includes(normalized));
}

export function useAgentRoutines(agentId: string) {
  const { data, loading, error, refetch } = useQuery<{
    routinesForAgent: AgentRoutine[];
  }>(ROUTINES_FOR_AGENT_QUERY, {
    variables: { agentId },
    skip: !agentId,
    fetchPolicy: "cache-and-network",
  });
  const [createMutation, { loading: creating }] = useMutation<CreateRoutinePayload>(
    CREATE_ROUTINE,
    { refetchQueries: ["RoutinesForAgent", "AgentRoutines"] },
  );
  const [setEnabledMutation] = useMutation(SET_ROUTINE_ENABLED, {
    refetchQueries: ["RoutinesForAgent", "AgentRoutines", "AgentRoutine"],
  });
  const [deleteMutation] = useMutation(DELETE_ROUTINE, {
    refetchQueries: ["RoutinesForAgent", "AgentRoutines"],
  });

  async function createRoutine(input: CreateRoutineInput) {
    const { data: result } = await createMutation({ variables: { input } });
    return result?.createAgentRoutine ?? null;
  }

  async function setRoutineEnabled(id: string, enabled: boolean) {
    await setEnabledMutation({
      variables: { id, enabled },
      optimisticResponse: { setAgentRoutineEnabled: true },
      update(cache) {
        for (const typename of ["AgentRoutineRecord", "AgentRoutinePayload"]) {
          cache.modify({
            id: cache.identify({ __typename: typename, id }),
            fields: { enabled: () => enabled },
          });
        }
      },
    });
  }

  async function deleteRoutine(id: string) {
    await deleteMutation({
      variables: { id },
      optimisticResponse: { deleteAgentRoutine: true },
      update(cache) {
        for (const typename of ["AgentRoutineRecord", "AgentRoutinePayload"]) {
          cache.evict({ id: cache.identify({ __typename: typename, id }) });
        }
        cache.gc();
      },
    });
  }

  return {
    routines: data?.routinesForAgent ?? [],
    loading,
    error: error ?? undefined,
    creating,
    createRoutine,
    setRoutineEnabled,
    deleteRoutine,
    refetch,
  };
}

export function useAllRoutines() {
  const { data, loading, error, refetch } = useQuery<{
    agentRoutines: AgentRoutine[];
  }>(ALL_ROUTINES_QUERY, { fetchPolicy: "cache-and-network" });
  const [createMutation, { loading: creating }] = useMutation<CreateRoutinePayload>(
    CREATE_ROUTINE,
    { refetchQueries: ["AgentRoutines", "RoutinesForAgent"] },
  );
  const [setEnabledMutation] = useMutation(SET_ROUTINE_ENABLED, {
    refetchQueries: ["AgentRoutines", "RoutinesForAgent", "AgentRoutine"],
  });
  const [deleteMutation] = useMutation(DELETE_ROUTINE, {
    refetchQueries: ["AgentRoutines", "RoutinesForAgent"],
  });

  async function createRoutine(input: CreateRoutineInput) {
    const { data: result } = await createMutation({ variables: { input } });
    return result?.createAgentRoutine ?? null;
  }

  async function setRoutineEnabled(id: string, enabled: boolean) {
    await setEnabledMutation({ variables: { id, enabled } });
  }

  async function deleteRoutine(id: string) {
    await deleteMutation({ variables: { id } });
  }

  return {
    routines: data?.agentRoutines ?? [],
    loading,
    error: error ?? undefined,
    creating,
    createRoutine,
    setRoutineEnabled,
    deleteRoutine,
    refetch,
  };
}

export function useRoutine(id: string) {
  const { data, loading, error, refetch } = useQuery<{
    agentRoutine: AgentRoutine | null;
  }>(ROUTINE_QUERY, {
    variables: { id },
    skip: !id,
    fetchPolicy: "cache-and-network",
  });
  const [setEnabledMutation] = useMutation(SET_ROUTINE_ENABLED, {
    refetchQueries: ["AgentRoutine", "AgentRoutines", "RoutinesForAgent"],
  });
  const [deleteMutation] = useMutation(DELETE_ROUTINE, {
    refetchQueries: ["AgentRoutines", "RoutinesForAgent"],
  });

  return {
    routine: data?.agentRoutine ?? null,
    loading,
    error: error ?? undefined,
    setRoutineEnabled: async (enabled: boolean) => {
      await setEnabledMutation({ variables: { id, enabled } });
      await refetch();
    },
    deleteRoutine: async () => {
      await deleteMutation({ variables: { id } });
    },
    refetch,
  };
}
