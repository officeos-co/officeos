"use client";

import { gql, useMutation, useQuery } from "@apollo/client";
import { USE_MOCKS } from "@/lib/graphql/mock-mode";

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
    { variables: { agentId }, skip: USE_MOCKS }
  );

  const [createMutation, { loading: creating }] = useMutation(CREATE_CRON_JOB);
  const [setEnabledMutation] = useMutation(SET_CRON_JOB_ENABLED);
  const [deleteMutation] = useMutation(DELETE_CRON_JOB);

  const jobs: CronJob[] = USE_MOCKS ? [] : (data?.agentCronJobs ?? []);

  async function createCronJob(name: string, expression: string, prompt: string) {
    await createMutation({
      variables: { input: { agentId, name, expression, prompt } },
    });
    await refetch();
  }

  async function setCronJobEnabled(id: string, enabled: boolean) {
    await setEnabledMutation({ variables: { id, enabled } });
    await refetch();
  }

  async function deleteCronJob(id: string) {
    await deleteMutation({ variables: { id } });
    await refetch();
  }

  return { jobs, loading, error, creating, createCronJob, setCronJobEnabled, deleteCronJob };
}
