"use client";

import { gql, useMutation, useQuery } from "@apollo/client";

/* ── Types ──────────────────────────────────────────────── */

export type CronJob = {
  id: string;
  agentId: string;
  name: string;
  expression: string;
  prompt: string;
  enabled: boolean;
  lastRunAt: string | null;
  nextRunAt: string | null;
  createdAt: string;
};

/* ── Queries / mutations ─────────────────────────────────── */

const CRON_JOBS_QUERY = gql`
  query AgentCronJobs($agentId: UUID!) {
    agentCronJobs(agentId: $agentId) {
      id
      agentId
      name
      expression
      prompt
      enabled
      lastRunAt
      nextRunAt
      createdAt
    }
  }
`;

const CREATE_CRON_JOB = gql`
  mutation CreateCronJob($input: CreateCronJobInput!) {
    createCronJob(input: $input) {
      id
      agentId
      name
      expression
      prompt
      enabled
      lastRunAt
      nextRunAt
      createdAt
    }
  }
`;

const SET_CRON_JOB_ENABLED = gql`
  mutation SetCronJobEnabled($id: UUID!, $enabled: Boolean!) {
    setCronJobEnabled(id: $id, enabled: $enabled)
  }
`;

const DELETE_CRON_JOB = gql`
  mutation DeleteCronJob($id: UUID!) {
    deleteCronJob(id: $id)
  }
`;

/* ── Hook ────────────────────────────────────────────────── */

export function useCronJobs(agentId: string) {
  const { data, loading, error, refetch } = useQuery<{ agentCronJobs: CronJob[] }>(
    CRON_JOBS_QUERY,
    { variables: { agentId } }
  );

  const [createMutation, { loading: creating }] = useMutation(CREATE_CRON_JOB);
  const [setEnabledMutation] = useMutation(SET_CRON_JOB_ENABLED);
  const [deleteMutation] = useMutation(DELETE_CRON_JOB);

  const jobs: CronJob[] = data?.agentCronJobs ?? [];

  async function createCronJob(name: string, expression: string, prompt: string) {
    const optimisticId = `cron_optimistic_${Date.now().toString(36)}`;
    await createMutation({
      variables: { input: { agentId, name, expression, prompt } },
      optimisticResponse: {
        createCronJob: {
          __typename: "AgentCronJob",
          id: optimisticId,
          agentId,
          name,
          expression,
          prompt,
          enabled: true,
          lastRunAt: null,
          nextRunAt: null,
          createdAt: new Date().toISOString(),
        },
      },
      update(cache, { data: result }) {
        if (!result?.createCronJob) return;
        const existing = cache.readQuery<{ agentCronJobs: CronJob[] }>({
          query: CRON_JOBS_QUERY,
          variables: { agentId },
        });
        cache.writeQuery({
          query: CRON_JOBS_QUERY,
          variables: { agentId },
          data: { agentCronJobs: [...(existing?.agentCronJobs ?? []), result.createCronJob] },
        });
      },
    });
  }

  async function setCronJobEnabled(id: string, enabled: boolean) {
    await setEnabledMutation({
      variables: { id, enabled },
      optimisticResponse: { setCronJobEnabled: true },
      update(cache) {
        cache.modify({
          id: cache.identify({ __typename: "AgentCronJob", id }),
          fields: { enabled: () => enabled },
        });
      },
    });
  }

  async function deleteCronJob(id: string) {
    await deleteMutation({
      variables: { id },
      optimisticResponse: { deleteCronJob: true },
      update(cache) {
        const existing = cache.readQuery<{ agentCronJobs: CronJob[] }>({
          query: CRON_JOBS_QUERY,
          variables: { agentId },
        });
        if (existing) {
          cache.writeQuery({
            query: CRON_JOBS_QUERY,
            variables: { agentId },
            data: { agentCronJobs: existing.agentCronJobs.filter((j) => j.id !== id) },
          });
        }
        cache.evict({ id: cache.identify({ __typename: "AgentCronJob", id }) });
        cache.gc();
      },
    });
  }

  return { jobs, loading, error, creating, createCronJob, setCronJobEnabled, deleteCronJob };
}
