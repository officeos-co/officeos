"use client"

import { gql, useMutation, useQuery } from "@apollo/client"
import { USE_MOCKS } from "@/lib/graphql/mock-mode"

export type Runner = {
  id: string
  name: string
  status: string
  createdAt: number
}

const mockRunners: Runner[] = []

const RUNNERS_QUERY = gql`
  query Runners {
    runners {
      id
      name
      status
      createdAt
    }
  }
`

const CREATE_RUNNER = gql`
  mutation CreateRunner($input: CreateRunnerInput!) {
    createRunner(input: $input) {
      id
      name
    }
  }
`

const DELETE_RUNNER = gql`
  mutation DeleteRunner($id: String!) {
    deleteRunner(id: $id)
  }
`

export function useRunners(): {
  runners: Runner[]
  loading: boolean
  error?: Error
} {
  const { data, loading, error } = useQuery(RUNNERS_QUERY, { skip: USE_MOCKS })
  if (USE_MOCKS) return { runners: mockRunners, loading: false }
  const runners: Runner[] = (data?.runners ?? []).map(
    (r: { id: string; name: string; status: string; createdAt: string | number }) => ({
      id: r.id,
      name: r.name,
      status: r.status,
      createdAt: typeof r.createdAt === "number" ? r.createdAt : Date.parse(r.createdAt) || Date.now(),
    }),
  )
  return { runners, loading, error: error ?? undefined }
}

export function useCreateRunner() {
  const [fn, state] = useMutation(CREATE_RUNNER)
  return {
    createRunner: async (input: { name: string }) => {
      if (USE_MOCKS) return { id: `run_mock_${Date.now().toString(36)}`, name: input.name }
      const { data } = await fn({ variables: { input } })
      return data?.createRunner as { id: string; name: string }
    },
    ...state,
  }
}

export function useDeleteRunner() {
  const [fn, state] = useMutation(DELETE_RUNNER)
  return {
    deleteRunner: async (id: string) => {
      if (USE_MOCKS) return true
      const { data } = await fn({ variables: { id } })
      return Boolean(data?.deleteRunner)
    },
    ...state,
  }
}
