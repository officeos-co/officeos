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

export type AtlasActivity = {
  id: string;
  connectionId: string;
  type: string;
  entity?: string | null;
  message: string;
  detailsJson: string;
  success: boolean;
  createdAt: string;
};

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
  fragment AtlasConnectionFields on AtlasConnectorConnectionRecord {
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
  query AtlasConnections {
    atlasConnections(filter: {}) {
      ...AtlasConnectionFields
    }
  }
`;

const ATLAS_CONNECTION = gql`
  ${CONNECTION_FIELDS}
  query AtlasConnection($id: UUID!) {
    atlasConnection(filter: { id: $id }) {
      ...AtlasConnectionFields
    }
  }
`;

const ATLAS_CONNECTOR_TYPES = gql`
  query AtlasConnectorTypes {
    atlasConnectorTypes {
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
  query AtlasRequestHistory($connectionId: UUID, $limit: Int!) {
    atlasRequestHistory(filter: { connectionId: $connectionId, limit: $limit }) {
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
  query AtlasActivity($connectionId: UUID, $limit: Int!) {
    atlasActivity(filter: { connectionId: $connectionId, limit: $limit }) {
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
  query AtlasIndexJobs($connectionId: UUID!, $limit: Int!) {
    atlasIndexJobs(filter: { connectionId: $connectionId, limit: $limit }) {
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
  query AtlasIndexedRecords(
    $connectionId: UUID!
    $entity: String!
    $query: String
    $cursor: String
    $limit: Int!
  ) {
    atlasIndexedRecords(
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
  query AtlasIndexedRecord($id: UUID!) {
    atlasIndexedRecord(filter: { id: $id }) {
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
  mutation CreateAtlasGitHubConnection($input: CreateAtlasGitHubConnectionInput!) {
    createAtlasGitHubConnection(input: $input) {
      ...AtlasConnectionFields
    }
  }
`;

const START_ATLAS_INDEX = gql`
  mutation StartAtlasIndex($connectionId: UUID!) {
    startAtlasIndex(connectionId: $connectionId) {
      id
      status
    }
  }
`;

export function useAtlasConnections({
  pollInterval,
}: { pollInterval?: number } = {}) {
  const { data, loading, error, refetch } = useQuery(ATLAS_CONNECTIONS, {
    fetchPolicy: "cache-and-network",
    pollInterval,
  });
  return {
    connections: (data?.atlasConnections ?? []) as AtlasConnection[],
    loading,
    error: error ?? undefined,
    refetch,
  };
}

export function useAtlasConnection(
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
    connection: (data?.atlasConnection ?? null) as AtlasConnection | null,
    loading,
    error: error ?? undefined,
    refetch,
  };
}

export function useAtlasConnectorTypes() {
  const { data, loading, error, refetch } = useQuery(ATLAS_CONNECTOR_TYPES, {
    fetchPolicy: "cache-and-network",
  });
  const connectorTypes = ((data?.atlasConnectorTypes ?? []) as AtlasConnectorType[]).map(
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
    history: (data?.atlasRequestHistory ?? []) as AtlasHistory[],
    loading,
    error: error ?? undefined,
    refetch,
  };
}

export function useAtlasActivity(
  connectionId?: string | null,
  { limit = 100, pollInterval }: { limit?: number; pollInterval?: number } = {},
) {
  const { data, loading, error, refetch } = useQuery(ATLAS_ACTIVITY, {
    variables: { connectionId: connectionId || null, limit },
    fetchPolicy: "cache-and-network",
    pollInterval,
  });
  return {
    activity: (data?.atlasActivity ?? []) as AtlasActivity[],
    loading,
    error: error ?? undefined,
    refetch,
  };
}

export function useAtlasIndexJobs(
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
    jobs: (data?.atlasIndexJobs ?? []) as AtlasIndexJob[],
    loading,
    error: error ?? undefined,
    refetch,
  };
}

export function useAtlasIndexedRecords({
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
    page: (data?.atlasIndexedRecords ?? {
      records: [],
      hasMore: false,
      cursor: null,
    }) as AtlasIndexedRecordPage,
    loading,
    error: error ?? undefined,
    refetch,
  };
}

export function useAtlasIndexedRecord(id?: string | null) {
  const { data, loading, error, refetch } = useQuery(ATLAS_INDEXED_RECORD, {
    variables: { id },
    skip: !id,
    fetchPolicy: "cache-and-network",
  });
  return {
    record: (data?.atlasIndexedRecord ?? null) as AtlasIndexedRecord | null,
    loading,
    error: error ?? undefined,
    refetch,
  };
}

export function useCreateAtlasGitHubConnection() {
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
      return result.data?.createAtlasGitHubConnection as AtlasConnection;
    },
    ...state,
  };
}

export function useStartAtlasIndex() {
  const [mutate, state] = useMutation(START_ATLAS_INDEX, {
    refetchQueries: [{ query: ATLAS_CONNECTIONS }],
  });
  return {
    startIndex: (connectionId: string) => mutate({ variables: { connectionId } }),
    ...state,
  };
}

export function parseJsonArray(value: string): string[] {
  try {
    const parsed = JSON.parse(value);
    return Array.isArray(parsed) ? parsed.map(String) : [];
  } catch {
    return [];
  }
}
