"use client";

import { gql, useMutation, useQuery } from "@apollo/client";

export type Provider = {
  id: string;
  name: string;
  displayName: string;
  configured: boolean;
  configuredAt: string | null;
  models: string[];
};

export type ProviderEnvironment = {
  key: string;
  value: string;
};

export type ProviderSetupStatus = {
  provider: string;
  displayName: string;
  configured: boolean;
  enabled: boolean;
  authKind: string;
  configuredAt: string;
  pinnedModels: string[];
  environment: ProviderEnvironment[];
};

export type BedrockProviderSetupInput = {
  organizationId: string;
  displayName: string;
  awsRegion?: string | null;
  authKind: string;
  awsProfile?: string | null;
  awsAccessKeyId?: string | null;
  awsSecretAccessKey?: string | null;
  awsSessionToken?: string | null;
  bedrockApiKey?: string | null;
  baseUrl?: string | null;
  skipProviderAuth: boolean;
  pinnedModels: string[];
  enabled: boolean;
};

export type VertexProviderSetupInput = {
  organizationId: string;
  displayName: string;
  projectId?: string | null;
  location?: string | null;
  authKind: string;
  credentialsPath?: string | null;
  baseUrl?: string | null;
  skipProviderAuth: boolean;
  pinnedModels: string[];
  enabled: boolean;
};

export type FoundryProviderSetupInput = {
  organizationId: string;
  displayName: string;
  resource?: string | null;
  baseUrl?: string | null;
  authKind: string;
  apiKey?: string | null;
  skipProviderAuth: boolean;
  pinnedModels: string[];
  enabled: boolean;
};

export type CodexOAuthLogin = {
  loginId: string;
  authUrl: string;
  expiresAt: string;
};

export type CodexOAuthStatus = {
  loginId: string;
  completed: boolean;
  success: boolean;
  error?: string | null;
  accountEmail?: string | null;
  planType?: string | null;
};

const PROVIDERS_QUERY = gql`
  query Providers {
    providers {
      id
      name
      displayName
      configured
      configuredAt
      models
    }
  }
`;

const PROVIDER_SETUP_STATUS_QUERY = gql`
  query ProviderSetupStatus($organizationId: UUID!) {
    providerSetupStatus(organizationId: $organizationId) {
      provider
      displayName
      configured
      enabled
      authKind
      configuredAt
      pinnedModels
      environment {
        key
        value
      }
    }
  }
`;

const SAVE_BEDROCK_PROVIDER_SETUP = gql`
  mutation SaveBedrockProviderSetup($input: BedrockProviderSetupInput!) {
    saveBedrockProviderSetup(input: $input) {
      id
      provider
      displayName
      allowedModels
      enabled
      configuredAt
    }
  }
`;

const SAVE_VERTEX_PROVIDER_SETUP = gql`
  mutation SaveVertexProviderSetup($input: VertexProviderSetupInput!) {
    saveVertexProviderSetup(input: $input) {
      id
      provider
      displayName
      allowedModels
      enabled
      configuredAt
    }
  }
`;

const SAVE_FOUNDRY_PROVIDER_SETUP = gql`
  mutation SaveFoundryProviderSetup($input: FoundryProviderSetupInput!) {
    saveFoundryProviderSetup(input: $input) {
      id
      provider
      displayName
      allowedModels
      enabled
      configuredAt
    }
  }
`;

const START_CODEX_OAUTH_LOGIN = gql`
  mutation StartCodexOAuthLogin {
    startCodexOAuthLogin {
      loginId
      authUrl
      expiresAt
    }
  }
`;

const POLL_CODEX_OAUTH_LOGIN = gql`
  mutation PollCodexOAuthLogin($input: PollCodexOAuthLoginInput!) {
    pollCodexOAuthLogin(input: $input) {
      loginId
      completed
      success
      error
      accountEmail
      planType
    }
  }
`;

const DISCONNECT_CODEX_OAUTH_PROVIDER = gql`
  mutation DisconnectCodexOAuthProvider {
    disconnectCodexOAuthProvider
  }
`;

export function useProviders() {
  const { data, loading, error, refetch } = useQuery(PROVIDERS_QUERY);

  const providers: Provider[] = data?.providers ?? [];

  return { providers, loading, error, refetch };
}

export function useProviderSetupStatus(organizationId?: string | null) {
  const { data, loading, error, refetch } = useQuery(
    PROVIDER_SETUP_STATUS_QUERY,
    {
      variables: { organizationId },
      skip: !organizationId,
    },
  );

  const statuses: ProviderSetupStatus[] = data?.providerSetupStatus ?? [];

  return { statuses, loading, error, refetch };
}

export function useSaveBedrockProviderSetup() {
  const [fn, state] = useMutation(SAVE_BEDROCK_PROVIDER_SETUP);
  return {
    saveBedrockProviderSetup: async (input: BedrockProviderSetupInput) => {
      const { data } = await fn({ variables: { input } });
      return data?.saveBedrockProviderSetup;
    },
    ...state,
  };
}

export function useSaveVertexProviderSetup() {
  const [fn, state] = useMutation(SAVE_VERTEX_PROVIDER_SETUP);
  return {
    saveVertexProviderSetup: async (input: VertexProviderSetupInput) => {
      const { data } = await fn({ variables: { input } });
      return data?.saveVertexProviderSetup;
    },
    ...state,
  };
}

export function useSaveFoundryProviderSetup() {
  const [fn, state] = useMutation(SAVE_FOUNDRY_PROVIDER_SETUP);
  return {
    saveFoundryProviderSetup: async (input: FoundryProviderSetupInput) => {
      const { data } = await fn({ variables: { input } });
      return data?.saveFoundryProviderSetup;
    },
    ...state,
  };
}

export function useStartCodexOAuthLogin() {
  const [fn, state] = useMutation(START_CODEX_OAUTH_LOGIN);
  return {
    startCodexOAuthLogin: async (): Promise<CodexOAuthLogin | undefined> => {
      const { data } = await fn();
      return data?.startCodexOAuthLogin;
    },
    ...state,
  };
}

export function usePollCodexOAuthLogin() {
  const [fn, state] = useMutation(POLL_CODEX_OAUTH_LOGIN);
  return {
    pollCodexOAuthLogin: async (
      loginId: string,
    ): Promise<CodexOAuthStatus | undefined> => {
      const { data } = await fn({ variables: { input: { loginId } } });
      return data?.pollCodexOAuthLogin;
    },
    ...state,
  };
}

export function useDisconnectCodexOAuthProvider() {
  const [fn, state] = useMutation(DISCONNECT_CODEX_OAUTH_PROVIDER);
  return {
    disconnectCodexOAuthProvider: async (): Promise<boolean> => {
      const { data } = await fn();
      return Boolean(data?.disconnectCodexOAuthProvider);
    },
    ...state,
  };
}
