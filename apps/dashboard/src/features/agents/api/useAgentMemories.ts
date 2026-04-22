"use client";

import { gql, useQuery } from "@apollo/client";
import { USE_MOCKS } from "@/lib/graphql/mock-mode";
import { mockFileTree, type FileNode } from "../data/agent-mock";

const AGENT_MEMORIES_QUERY = gql`
  query AgentMemories($agentId: UUID!) {
    agentMemories(agentId: $agentId) {
      id
      key
      content
      updatedAt
    }
  }
`;

export type AgentMemory = {
  id: string;
  key: string;
  content: string;
  updatedAt: string;
};

function memoriesToFileTree(memories: AgentMemory[]): FileNode[] {
  return memories.map((m) => ({
    name: m.key,
    type: "file" as const,
    content: m.content,
  }));
}

export function useAgentMemories(agentId: string): {
  memories: AgentMemory[];
  fileTree: FileNode[];
  loading: boolean;
  error?: Error;
} {
  const { data, loading, error } = useQuery(AGENT_MEMORIES_QUERY, {
    variables: { agentId },
    skip: USE_MOCKS || !agentId,
  });

  if (USE_MOCKS) {
    return { memories: [], fileTree: mockFileTree, loading: false };
  }

  const memories: AgentMemory[] = data?.agentMemories ?? [];
  const fileTree = memoriesToFileTree(memories);

  return { memories, fileTree, loading, error: error ?? undefined };
}
