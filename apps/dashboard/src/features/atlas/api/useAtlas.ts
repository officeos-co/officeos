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
    atlasConnections {
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
      isBuiltin
      entities
    }
  }
`;

const ATLAS_HISTORY = gql`
  query AtlasRequestHistory($connectionId: UUID) {
    atlasRequestHistory(connectionId: $connectionId) {
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

export function useAtlasConnections() {
  const { data, loading, error, refetch } = useQuery(ATLAS_CONNECTIONS, {
    fetchPolicy: "cache-and-network",
  });
  return {
    connections: (data?.atlasConnections ?? []) as AtlasConnection[],
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

export function useAtlasHistory(connectionId?: string | null) {
  const { data, loading, error, refetch } = useQuery(ATLAS_HISTORY, {
    variables: { connectionId: connectionId || null },
    fetchPolicy: "cache-and-network",
  });
  return {
    history: (data?.atlasRequestHistory ?? []) as AtlasHistory[],
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
