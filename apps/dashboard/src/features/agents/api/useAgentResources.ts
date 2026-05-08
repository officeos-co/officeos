"use client";

import { gql, useMutation, useQuery } from "@apollo/client";

export const RESOURCE_TYPES = {
  browser: "browser",
  memoryStore: "memory_store",
} as const;

export type ResourceType = (typeof RESOURCE_TYPES)[keyof typeof RESOURCE_TYPES];

export type BrowserResource = {
  id: string;
  ownerId: string;
  displayName: string;
  currentAgentId: string | null;
  createdAt: string;
  updatedAt: string;
};

export type MemoryStoreEntry = {
  id: string;
  memoryStoreId: string;
  key: string;
  content: string;
  createdAt: string;
  updatedAt: string;
};

export type MemoryStore = {
  id: string;
  ownerId: string;
  displayName: string;
  createdAt: string;
  updatedAt: string;
  entries?: MemoryStoreEntry[] | null;
};

const BROWSER_RESOURCES = gql`
  query BrowserResources {
    browserResources {
      id
      ownerId
      displayName
      currentAgentId
      createdAt
      updatedAt
    }
  }
`;

const BROWSER_RESOURCE = gql`
  query BrowserResource($id: UUID!) {
    browserResource(id: $id) {
      id
      ownerId
      displayName
      currentAgentId
      createdAt
      updatedAt
    }
  }
`;

const MEMORY_STORES = gql`
  query MemoryStores {
    memoryStores {
      id
      ownerId
      displayName
      createdAt
      updatedAt
    }
  }
`;

const MEMORY_STORE = gql`
  query MemoryStore($id: UUID!) {
    memoryStore(id: $id) {
      id
      ownerId
      displayName
      createdAt
      updatedAt
      entries {
        id
        memoryStoreId
        key
        content
        createdAt
        updatedAt
      }
    }
  }
`;

const CREATE_BROWSER_RESOURCE = gql`
  mutation CreateBrowserResource($input: CreateBrowserResourceInput!) {
    createBrowserResource(input: $input) {
      id
      ownerId
      displayName
      currentAgentId
      createdAt
      updatedAt
    }
  }
`;

const CREATE_MEMORY_STORE = gql`
  mutation CreateMemoryStore($input: CreateMemoryStoreInput!) {
    createMemoryStore(input: $input) {
      id
      ownerId
      displayName
      createdAt
      updatedAt
    }
  }
`;

const DELETE_BROWSER_RESOURCE = gql`
  mutation DeleteBrowserResource($id: UUID!) {
    deleteBrowserResource(id: $id)
  }
`;

const DELETE_MEMORY_STORE = gql`
  mutation DeleteMemoryStore($id: UUID!) {
    deleteMemoryStore(id: $id)
  }
`;

const UPSERT_MEMORY_STORE_ENTRY = gql`
  mutation UpsertMemoryStoreEntry($input: UpsertMemoryStoreEntryInput!) {
    upsertMemoryStoreEntry(input: $input) {
      id
      memoryStoreId
      key
      content
      createdAt
      updatedAt
    }
  }
`;

const DELETE_MEMORY_STORE_ENTRY = gql`
  mutation DeleteMemoryStoreEntry($memoryStoreId: UUID!, $key: String!) {
    deleteMemoryStoreEntry(memoryStoreId: $memoryStoreId, key: $key)
  }
`;

export function useBrowserResources() {
  const { data, loading, error, refetch } = useQuery(BROWSER_RESOURCES, {
    fetchPolicy: "cache-and-network",
  });
  return {
    browserResources: (data?.browserResources ?? []) as BrowserResource[],
    loading,
    error: error ?? undefined,
    refetch,
  };
}

export function useBrowserResource(id: string) {
  const { data, loading, error, refetch } = useQuery(BROWSER_RESOURCE, {
    variables: { id },
    skip: !id,
    fetchPolicy: "cache-and-network",
  });
  return {
    browserResource: (data?.browserResource ?? null) as BrowserResource | null,
    loading,
    error: error ?? undefined,
    refetch,
  };
}

export function useMemoryStores() {
  const { data, loading, error, refetch } = useQuery(MEMORY_STORES, {
    fetchPolicy: "cache-and-network",
  });
  return {
    memoryStores: (data?.memoryStores ?? []) as MemoryStore[],
    loading,
    error: error ?? undefined,
    refetch,
  };
}

export function useMemoryStore(id: string) {
  const { data, loading, error, refetch } = useQuery(MEMORY_STORE, {
    variables: { id },
    skip: !id,
    fetchPolicy: "cache-and-network",
  });
  return {
    memoryStore: (data?.memoryStore ?? null) as MemoryStore | null,
    loading,
    error: error ?? undefined,
    refetch,
  };
}

export function useCreateBrowserResource() {
  const [mutate, state] = useMutation(CREATE_BROWSER_RESOURCE, {
    refetchQueries: ["BrowserResources"],
  });
  return {
    createBrowserResource: async (displayName: string) => {
      const { data } = await mutate({ variables: { input: { displayName } } });
      return data?.createBrowserResource as BrowserResource;
    },
    ...state,
  };
}

export function useDeleteBrowserResource() {
  const [mutate, state] = useMutation(DELETE_BROWSER_RESOURCE, {
    refetchQueries: ["BrowserResources"],
  });
  return {
    deleteBrowserResource: async (id: string) => {
      const { data } = await mutate({ variables: { id } });
      return Boolean(data?.deleteBrowserResource);
    },
    ...state,
  };
}

export function useCreateMemoryStore() {
  const [mutate, state] = useMutation(CREATE_MEMORY_STORE, {
    refetchQueries: ["MemoryStores"],
  });
  return {
    createMemoryStore: async (displayName: string) => {
      const { data } = await mutate({ variables: { input: { displayName } } });
      return data?.createMemoryStore as MemoryStore;
    },
    ...state,
  };
}

export function useDeleteMemoryStore() {
  const [mutate, state] = useMutation(DELETE_MEMORY_STORE, {
    refetchQueries: ["MemoryStores"],
  });
  return {
    deleteMemoryStore: async (id: string) => {
      const { data } = await mutate({ variables: { id } });
      return Boolean(data?.deleteMemoryStore);
    },
    ...state,
  };
}

export function useUpsertMemoryStoreEntry() {
  const [mutate, state] = useMutation(UPSERT_MEMORY_STORE_ENTRY, {
    refetchQueries: ["MemoryStore"],
  });
  return {
    upsertMemoryStoreEntry: async (
      memoryStoreId: string,
      key: string,
      content: string,
    ) => {
      const { data } = await mutate({
        variables: { input: { memoryStoreId, key, content } },
      });
      return data?.upsertMemoryStoreEntry as MemoryStoreEntry;
    },
    ...state,
  };
}

export function useDeleteMemoryStoreEntry() {
  const [mutate, state] = useMutation(DELETE_MEMORY_STORE_ENTRY, {
    refetchQueries: ["MemoryStore"],
  });
  return {
    deleteMemoryStoreEntry: async (memoryStoreId: string, key: string) => {
      const { data } = await mutate({ variables: { memoryStoreId, key } });
      return Boolean(data?.deleteMemoryStoreEntry);
    },
    ...state,
  };
}
