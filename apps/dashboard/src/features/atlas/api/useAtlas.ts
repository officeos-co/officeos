"use client";

import { gql, useMutation, useQuery } from "@apollo/client";
import { sanitizeSvg } from "@/lib/sanitize-svg";

export type AtlasEntityStatus = {
  entity: string;
  status: string;
  recordCount: number;
  error?: string | null;
  lastSyncedAt?: string | null;
  updatedAt: string;
};

export type AtlasConnection = {
  id: string;
  provider: string;
  workspaceName: string;
  displayName: string;
  repositoriesJson: string;
  entitiesJson: string;
  status: string;
  error?: string | null;
  createdAt: string;
  updatedAt: string;
  entityStatuses: AtlasEntityStatus[];
};

export type AtlasHistory = {
  id: string;
  connectionId: string;
  type: string;
  entity: string;
  action: string;
  paramsJson: string;
  success: boolean;
  durationMs: number;
  error?: string | null;
  createdAt: string;
};

export type IntegrationActivity = {
  id: string;
  connectionId: string;
  type: string;
  entity?: string | null;
  message: string;
  detailsJson: string;
  success: boolean;
  createdAt: string;
};

/** @deprecated Use IntegrationActivity instead */
export type AtlasActivity = IntegrationActivity;

export type AtlasIndexJob = {
  id: string;
  connectionId: string;
  status: string;
  error?: string | null;
  recordsIndexed: number;
  createdAt: string;
  startedAt?: string | null;
  completedAt?: string | null;
};

export type AtlasIndexedRecord = {
  id: string;
  connectionId: string;
  entity: string;
  externalId: string;
  title: string;
  searchText: string;
  rawJson: string;
  externalUpdatedAt?: string | null;
  createdAt: string;
  updatedAt: string;
};

export type AtlasIndexedRecordPage = {
  records: AtlasIndexedRecord[];
  hasMore: boolean;
  cursor?: string | null;
};

export type AtlasConnectorType = {
  name: string;
  provider: string;
  title: string;
  description: string;
  subtitle: string;
  authorName: string;
  authorUrl?: string | null;
  documentationUrl?: string | null;
  repositoryUrl?: string | null;
  logo: string;
  toolsJson?: string | null;
  category: string;
  oauthProvider?: string | null;
  oauthScopesJson?: string | null;
  oauthConfigured: boolean;
  isBuiltin: boolean;
  entities: string[];
};

const CONNECTION_FIELDS = gql`
  fragment IntegrationConnectionFields on IntegrationConnectionRecord {
    id
    provider
    workspaceName
    displayName
    repositoriesJson
    entitiesJson
    status
    error
    createdAt
    updatedAt
    entityStatuses {
      entity
      status
      recordCount
      error
      lastSyncedAt
      updatedAt
    }
  }
`;

const ATLAS_CONNECTIONS = gql`
  ${CONNECTION_FIELDS}
  query IntegrationConnections {
    integrationConnections(filter: {}) {
      ...IntegrationConnectionFields
    }
  }
`;

const ATLAS_CONNECTION = gql`
  ${CONNECTION_FIELDS}
  query IntegrationConnection($id: UUID!) {
    integrationConnection(filter: { id: $id }) {
      ...IntegrationConnectionFields
    }
  }
`;

const ATLAS_CONNECTOR_TYPES = gql`
  query IntegrationDefinitions {
    integrationDefinitions {
      name
      provider
      title
      description
      subtitle
      authorName
      authorUrl
      documentationUrl
      repositoryUrl
      logo
      toolsJson
      category
      oauthProvider
      oauthScopesJson
      oauthConfigured
      isBuiltin
      entities
    }
  }
`;

const ATLAS_HISTORY = gql`
  query IntegrationRequestHistory($connectionId: UUID, $limit: Int!) {
    integrationRequestHistory(filter: { connectionId: $connectionId, limit: $limit }) {
      id
      connectionId
      type
      entity
      action
      paramsJson
      success
      durationMs
      error
      createdAt
    }
  }
`;

const ATLAS_ACTIVITY = gql`
  query IntegrationActivity($connectionId: UUID, $limit: Int!) {
    integrationActivity(filter: { connectionId: $connectionId, limit: $limit }) {
      id
      connectionId
      type
      entity
      message
      detailsJson
      success
      createdAt
    }
  }
`;

const ATLAS_INDEX_JOBS = gql`
  query IntegrationIndexJobs($connectionId: UUID!, $limit: Int!) {
    integrationIndexJobs(filter: { connectionId: $connectionId, limit: $limit }) {
      id
      connectionId
      status
      error
      recordsIndexed
      createdAt
      startedAt
      completedAt
    }
  }
`;

const ATLAS_INDEXED_RECORDS = gql`
  query IntegrationIndexedRecords(
    $connectionId: UUID!
    $entity: String!
    $query: String
    $cursor: String
    $limit: Int!
  ) {
    integrationIndexedRecords(
      filter: {
        connectionId: $connectionId
        entity: $entity
        query: $query
        cursor: $cursor
        limit: $limit
      }
    ) {
      records {
        id
        connectionId
        entity
        externalId
        title
        searchText
        rawJson
        externalUpdatedAt
        createdAt
        updatedAt
      }
      hasMore
      cursor
    }
  }
`;

const ATLAS_INDEXED_RECORD = gql`
  query IntegrationIndexedRecord($id: UUID!) {
    integrationIndexedRecord(filter: { id: $id }) {
      id
      connectionId
      entity
      externalId
      title
      searchText
      rawJson
      externalUpdatedAt
      createdAt
      updatedAt
    }
  }
`;

const CREATE_GITHUB_CONNECTION = gql`
  ${CONNECTION_FIELDS}
  mutation CreateGitHubIntegrationConnection($input: CreateGitHubIntegrationConnectionInput!) {
    createGitHubIntegrationConnection(input: $input) {
      ...IntegrationConnectionFields
    }
  }
`;

const START_ATLAS_INDEX = gql`
  mutation StartIntegrationIndex($connectionId: UUID!) {
    startIntegrationIndex(connectionId: $connectionId) {
      id
      status
    }
  }
`;

export function useIntegrationConnections({
  pollInterval,
}: { pollInterval?: number } = {}) {
  const { data, loading, error, refetch } = useQuery(ATLAS_CONNECTIONS, {
    fetchPolicy: "cache-and-network",
    pollInterval,
  });
  return {
    connections: (data?.integrationConnections ?? []) as AtlasConnection[],
    loading,
    error: error ?? undefined,
    refetch,
  };
}

export function useIntegrationConnection(
  id?: string | null,
  { pollInterval }: { pollInterval?: number } = {},
) {
  const { data, loading, error, refetch } = useQuery(ATLAS_CONNECTION, {
    variables: { id },
    skip: !id,
    fetchPolicy: "cache-and-network",
    pollInterval,
  });
  return {
    connection: (data?.integrationConnection ?? null) as AtlasConnection | null,
    loading,
    error: error ?? undefined,
    refetch,
  };
}

export function useIntegrationDefinitions() {
  const { data, loading, error, refetch } = useQuery(ATLAS_CONNECTOR_TYPES, {
    fetchPolicy: "cache-and-network",
  });
  const connectorTypes = ((data?.integrationDefinitions ?? []) as AtlasConnectorType[]).map(
    (connectorType) => ({
      ...connectorType,
      logo: sanitizeSvg(connectorType.logo ?? ""),
    }),
  );
  return {
    connectorTypes,
    loading,
    error: error ?? undefined,
    refetch,
  };
}

export function useAtlasHistory(
  connectionId?: string | null,
  { limit = 100, pollInterval }: { limit?: number; pollInterval?: number } = {},
) {
  const { data, loading, error, refetch } = useQuery(ATLAS_HISTORY, {
    variables: { connectionId: connectionId || null, limit },
    fetchPolicy: "cache-and-network",
    pollInterval,
  });
  return {
    history: (data?.integrationRequestHistory ?? []) as AtlasHistory[],
    loading,
    error: error ?? undefined,
    refetch,
  };
}

export function useIntegrationActivity(
  connectionId?: string | null,
  { limit = 100, pollInterval }: { limit?: number; pollInterval?: number } = {},
) {
  const { data, loading, error, refetch } = useQuery(ATLAS_ACTIVITY, {
    variables: { connectionId: connectionId || null, limit },
    fetchPolicy: "cache-and-network",
    pollInterval,
  });
  return {
    activity: (data?.integrationActivity ?? []) as IntegrationActivity[],
    loading,
    error: error ?? undefined,
    refetch,
  };
}

export function useIntegrationIndexJobs(
  connectionId?: string | null,
  { limit = 20, pollInterval }: { limit?: number; pollInterval?: number } = {},
) {
  const { data, loading, error, refetch } = useQuery(ATLAS_INDEX_JOBS, {
    variables: { connectionId, limit },
    skip: !connectionId,
    fetchPolicy: "cache-and-network",
    pollInterval,
  });
  return {
    jobs: (data?.integrationIndexJobs ?? []) as AtlasIndexJob[],
    loading,
    error: error ?? undefined,
    refetch,
  };
}

export function useIntegrationIndexedRecords({
  connectionId,
  entity,
  query,
  cursor,
  limit = 20,
}: {
  connectionId?: string | null;
  entity: string;
  query?: string | null;
  cursor?: string | null;
  limit?: number;
}) {
  const { data, loading, error, refetch } = useQuery(ATLAS_INDEXED_RECORDS, {
    variables: {
      connectionId,
      entity,
      query: query || null,
      cursor: cursor || null,
      limit,
    },
    skip: !connectionId || !entity,
    fetchPolicy: "cache-and-network",
  });
  return {
    page: (data?.integrationIndexedRecords ?? {
      records: [],
      hasMore: false,
      cursor: null,
    }) as AtlasIndexedRecordPage,
    loading,
    error: error ?? undefined,
    refetch,
  };
}

export function useIntegrationIndexedRecord(id?: string | null) {
  const { data, loading, error, refetch } = useQuery(ATLAS_INDEXED_RECORD, {
    variables: { id },
    skip: !id,
    fetchPolicy: "cache-and-network",
  });
  return {
    record: (data?.integrationIndexedRecord ?? null) as AtlasIndexedRecord | null,
    loading,
    error: error ?? undefined,
    refetch,
  };
}

export function useCreateGitHubIntegrationConnection() {
  const [mutate, state] = useMutation(CREATE_GITHUB_CONNECTION, {
    refetchQueries: [{ query: ATLAS_CONNECTIONS }],
  });
  return {
    createConnection: async (input: {
      workspaceName: string;
      displayName: string;
      repositories: string[];
      entities: string[];
    }) => {
      const result = await mutate({ variables: { input } });
      return result.data?.createGitHubIntegrationConnection as AtlasConnection;
    },
    ...state,
  };
}

export function useStartIntegrationIndex() {
  const [mutate, state] = useMutation(START_ATLAS_INDEX, {
    refetchQueries: [{ query: ATLAS_CONNECTIONS }],
  });
  return {
    startIndex: (connectionId: string) => mutate({ variables: { connectionId } }),
    ...state,
  };
}

/** @deprecated Use useIntegrationConnections instead */
export const useAtlasConnections = useIntegrationConnections;

/** @deprecated Use useIntegrationConnection instead */
export const useAtlasConnection = useIntegrationConnection;

/** @deprecated Use useIntegrationDefinitions instead */
export const useAtlasConnectorTypes = useIntegrationDefinitions;

/** @deprecated Use useIntegrationActivity instead */
export const useAtlasActivity = useIntegrationActivity;

/** @deprecated Use useIntegrationIndexJobs instead */
export const useAtlasIndexJobs = useIntegrationIndexJobs;

/** @deprecated Use useIntegrationIndexedRecords instead */
export const useAtlasIndexedRecords = useIntegrationIndexedRecords;

/** @deprecated Use useIntegrationIndexedRecord instead */
export const useAtlasIndexedRecord = useIntegrationIndexedRecord;

/** @deprecated Use useCreateGitHubIntegrationConnection instead */
export const useCreateAtlasGitHubConnection = useCreateGitHubIntegrationConnection;

/** @deprecated Use useStartIntegrationIndex instead */
export const useStartAtlasIndex = useStartIntegrationIndex;

export function parseJsonArray(value: string): string[] {
  try {
    const parsed = JSON.parse(value);
    return Array.isArray(parsed) ? parsed.map(String) : [];
  } catch {
    return [];
  }
}
