"use client";

import { gql, useMutation, useQuery } from "@apollo/client";
import type { Integration } from "../data/integrations";
import { sanitizeSvg } from "@/lib/sanitize-svg";

const SKILLS_LIST_QUERY = gql`
  query Skills {
    skills {
      id
      name
      title
      description
      logo
      sourceCodeUrl
      status
      installed
      likes
      likedByMe
      commentsCount
      version
      license
      repository
      requiresApproval
      categories
      keywords
      author {
        name
        url
      }
      contributors {
        name
        url
      }
      configured
      credentialFields {
        key
        label
        kind
        required
        placeholder
        help
        oauth2 {
          provider
          scopes
        }
      }
      tools {
        name
        description
      }
      createdAt
      updatedAt
    }
  }
`;

const SKILL_DETAIL_QUERY = gql`
  query Skill($name: String!) {
    skill(name: $name) {
      id
      name
      title
      description
      logo
      sourceCodeUrl
      doc
      status
      installed
      likes
      likedByMe
      commentsCount
      version
      license
      repository
      requiresApproval
      categories
      keywords
      readme
      changelog
      author {
        name
        url
      }
      contributors {
        name
        url
      }
      configured
      credentialFields {
        key
        label
        kind
        required
        placeholder
        help
        oauth2 {
          provider
          scopes
        }
      }
      tools {
        name
        description
      }
      createdAt
      updatedAt
    }
  }
`;

const SKILL_COMMENTS_QUERY = gql`
  query SkillComments($skillId: UUID!) {
    skillComments(skillId: $skillId) {
      id
      body
      createdAt
      author {
        id
        name
        avatarUrl
      }
    }
  }
`;

const INSTALL_SKILL = gql`
  mutation InstallSkill($name: String!) {
    installSkill(name: $name)
  }
`;

const UNINSTALL_SKILL = gql`
  mutation UninstallSkill($name: String!) {
    uninstallSkill(name: $name)
  }
`;

const SET_SKILL_CREDENTIALS = gql`
  mutation SetSkillCredentials(
    $name: String!
    $credentials: [SkillCredentialEntryInput!]!
  ) {
    setSkillCredentials(name: $name, credentials: $credentials)
  }
`;

const LIKE_SKILL = gql`
  mutation LikeSkill($skillId: UUID!) {
    likeSkill(skillId: $skillId) {
      id
      likes
      likedByMe
    }
  }
`;

const UNLIKE_SKILL = gql`
  mutation UnlikeSkill($skillId: UUID!) {
    unlikeSkill(skillId: $skillId) {
      id
      likes
      likedByMe
    }
  }
`;

const COMMENT_ON_SKILL = gql`
  mutation CommentOnSkill($skillId: UUID!, $body: String!) {
    commentOnSkill(skillId: $skillId, body: $body) {
      id
      body
      createdAt
      author {
        id
        name
        avatarUrl
      }
    }
  }
`;

const DELETE_SKILL_COMMENT = gql`
  mutation DeleteSkillComment($commentId: UUID!) {
    deleteSkillComment(commentId: $commentId)
  }
`;

type RawSkill = {
  id: string;
  name: string;
  title: string | null;
  description: string | null;
  logo: string | null;
  doc: string | null;
  sourceCodeUrl: string | null;
  installed: boolean;
  configured: boolean;
  credentialFields: Array<{
    key: string;
    label: string;
    kind: string;
    required: boolean;
    placeholder: string | null;
    help: string | null;
    oauth2: { provider: string; scopes: string[] } | null;
  }> | null;
  likes: number;
  likedByMe: boolean;
  commentsCount: number;
  version: string | null;
  license: string | null;
  repository: string | null;
  categories: string[] | null;
  keywords: string[] | null;
  readme: string | null;
  changelog: string | null;
  author: { name: string; url?: string | null } | null;
  contributors: Array<{ name: string; url?: string | null }> | null;
  createdAt: string | null;
  updatedAt: string | null;
  tools: Array<{ name: string; description: string }> | null;
};

function mapSkill(s: RawSkill): Integration {
  return {
    id: s.id,
    name: s.title ?? s.name,
    slug: s.name,
    logo: sanitizeSvg(s.logo ?? ""),
    description: s.description ?? "",
    likes: s.likes,
    likedByMe: s.likedByMe,
    commentsCount: s.commentsCount,
    tools: (s.tools ?? []).map((t) => ({
      name: t.name,
      description: t.description,
    })),
    installed: s.installed,
    configured: s.configured,
    credentialFields: (s.credentialFields ?? []).map((f) => ({
      key: f.key,
      label: f.label,
      kind: f.kind,
      required: f.required,
      placeholder: f.placeholder ?? null,
      help: f.help ?? null,
      oauth2: f.oauth2 ?? null,
    })),
    doc: s.doc ?? "",
    sourceCodeUrl: s.sourceCodeUrl ?? "",
    version: s.version ?? "1.0.0",
    license: s.license ?? null,
    repository: s.repository ?? null,
    categories: s.categories ?? [],
    keywords: s.keywords ?? [],
    readme: s.readme ?? null,
    changelog: s.changelog ?? null,
    author: s.author ?? null,
    contributors: s.contributors ?? [],
    createdAt: s.createdAt ?? null,
    updatedAt: s.updatedAt ?? null,
  };
}

export function useIntegrations(): {
  integrations: Integration[];
  loading: boolean;
  error?: Error;
} {
  const { data, loading, error } = useQuery(SKILLS_LIST_QUERY, {
    fetchPolicy: "cache-and-network",
  });
  const raw: RawSkill[] = data?.skills ?? [];
  const integrations = raw.map(mapSkill);
  return { integrations, loading, error: error ?? undefined };
}

export function useIntegration(slug: string): {
  integration: Integration | null;
  loading: boolean;
  error?: Error;
  refetch: () => void;
} {
  const { data, loading, error, refetch } = useQuery(SKILL_DETAIL_QUERY, {
    variables: { name: slug },
    skip: !slug,
  });
  const raw: RawSkill | null = data?.skill ?? null;
  const integration = raw ? mapSkill(raw) : null;
  return { integration, loading, error: error ?? undefined, refetch };
}

export type SkillComment = {
  id: string;
  body: string;
  createdAt: string;
  author: { id: string; name: string | null; avatarUrl: string | null };
};

export function useSkillComments(skillId: string): {
  comments: SkillComment[];
  loading: boolean;
  error?: Error;
  refetch: () => void;
} {
  const { data, loading, error, refetch } = useQuery(SKILL_COMMENTS_QUERY, {
    variables: { skillId },
    skip: !skillId,
  });
  const raw: Array<{
    id: string;
    body: string;
    createdAt: string;
    author?: {
      id: string;
      name: string | null;
      avatarUrl: string | null;
    } | null;
  }> = data?.skillComments ?? [];

  const comments: SkillComment[] = raw.map((c) => ({
    id: c.id,
    body: c.body,
    createdAt: c.createdAt,
    author: {
      id: c.author?.id ?? "",
      name: c.author?.name ?? "Unknown",
      avatarUrl: c.author?.avatarUrl ?? null,
    },
  }));

  return { comments, loading, error: error ?? undefined, refetch };
}

export function useInstallSkill() {
  const [fn] = useMutation(INSTALL_SKILL);
  return async (name: string) => {
    await fn({
      variables: { name },
      optimisticResponse: { installSkill: true },
      update(cache) {
        cache.modify({
          id: cache.identify({ __typename: "Skill", name }),
          fields: { installed: () => true },
        });
      },
    });
  };
}

export function useUninstallSkill() {
  const [fn] = useMutation(UNINSTALL_SKILL);
  return async (name: string) => {
    await fn({
      variables: { name },
      optimisticResponse: { uninstallSkill: true },
      update(cache) {
        cache.modify({
          id: cache.identify({ __typename: "Skill", name }),
          fields: { installed: () => false },
        });
      },
    });
  };
}

export function useSetSkillCredentials() {
  const [fn] = useMutation(SET_SKILL_CREDENTIALS);
  return async (name: string, credentials: Record<string, string>) => {
    const entries = Object.entries(credentials).map(([key, value]) => ({
      key,
      value,
    }));
    await fn({
      variables: { name, credentials: entries },
      optimisticResponse: { setSkillCredentials: true },
      update(cache) {
        cache.modify({
          id: cache.identify({ __typename: "Skill", name }),
          fields: { configured: () => true },
        });
      },
    });
  };
}

export function useLikeSkill() {
  const [likeFn] = useMutation(LIKE_SKILL);
  const [unlikeFn] = useMutation(UNLIKE_SKILL);
  return async (skillId: string, liked: boolean): Promise<void> => {
    if (liked) {
      await likeFn({
        variables: { skillId },
        optimisticResponse: {
          likeSkill: {
            __typename: "Skill",
            id: skillId,
            likes: -1, // Will be corrected by server response
            likedByMe: true,
          },
        },
        update(cache) {
          cache.modify({
            id: cache.identify({ __typename: "Skill", id: skillId }),
            fields: {
              likedByMe: () => true,
              likes: (prev: number) => prev + 1,
            },
          });
        },
      });
    } else {
      await unlikeFn({
        variables: { skillId },
        optimisticResponse: {
          unlikeSkill: {
            __typename: "Skill",
            id: skillId,
            likes: -1,
            likedByMe: false,
          },
        },
        update(cache) {
          cache.modify({
            id: cache.identify({ __typename: "Skill", id: skillId }),
            fields: {
              likedByMe: () => false,
              likes: (prev: number) => Math.max(0, prev - 1),
            },
          });
        },
      });
    }
  };
}

export function useCommentOnSkill() {
  const [fn, state] = useMutation(COMMENT_ON_SKILL);
  return {
    commentOnSkill: async (skillId: string, body: string) => {
      const optimisticId = `comment_optimistic_${Date.now().toString(36)}`;
      const { data } = await fn({
        variables: { skillId, body },
        optimisticResponse: {
          commentOnSkill: {
            __typename: "SkillComment",
            id: optimisticId,
            body,
            createdAt: new Date().toISOString(),
            author: {
              __typename: "User",
              id: "me",
              name: "You",
              avatarUrl: null,
            },
          },
        },
        update(cache, { data: result }) {
          if (!result?.commentOnSkill) return;
          const existing = cache.readQuery<{ skillComments: unknown[] }>({
            query: SKILL_COMMENTS_QUERY,
            variables: { skillId },
          });
          if (existing) {
            cache.writeQuery({
              query: SKILL_COMMENTS_QUERY,
              variables: { skillId },
              data: { skillComments: [...existing.skillComments, result.commentOnSkill] },
            });
          }
          cache.modify({
            id: cache.identify({ __typename: "Skill", id: skillId }),
            fields: { commentsCount: (prev: number) => prev + 1 },
          });
        },
      });
      return data?.commentOnSkill as SkillComment;
    },
    ...state,
  };
}

export function useDeleteSkillComment() {
  const [fn] = useMutation(DELETE_SKILL_COMMENT);
  return async (commentId: string, skillId?: string) => {
    await fn({
      variables: { commentId },
      optimisticResponse: { deleteSkillComment: true },
      update(cache) {
        cache.evict({ id: cache.identify({ __typename: "SkillComment", id: commentId }) });
        if (skillId) {
          cache.modify({
            id: cache.identify({ __typename: "Skill", id: skillId }),
            fields: { commentsCount: (prev: number) => Math.max(0, prev - 1) },
          });
        }
        cache.gc();
      },
    });
  };
}

const OAUTH_STATUS_QUERY = gql`
  query OAuthConnectionStatus($provider: String!) {
    oauthConnectionStatus(provider: $provider) {
      connected
      email
      scopes
    }
  }
`;

export function useOAuthStatus(provider: string | null) {
  const { data, loading, refetch } = useQuery(OAUTH_STATUS_QUERY, {
    variables: { provider: provider ?? "" },
    skip: !provider,
    fetchPolicy: "network-only",
  });
  const status = data?.oauthConnectionStatus as
    | { connected: boolean; email: string | null; scopes: string | null }
    | undefined;
  return { connected: status?.connected ?? false, email: status?.email ?? null, loading, refetch };
}
