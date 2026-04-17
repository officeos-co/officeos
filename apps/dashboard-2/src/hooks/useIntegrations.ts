"use client"

import { gql, useMutation, useQuery } from "@apollo/client"
import { USE_MOCKS } from "@/lib/graphql/mock-mode"
import {
  integrations as mockIntegrations,
  type Integration,
} from "@/data/integrations"

/*
 * Integrations = skills in the backend. The dashboard does not have a
 * separate "skills" page; use this hook wherever the UI shows integrations.
 */

const SKILLS_QUERY = gql`
  query Skills {
    skills {
      id
      name
      title
      description
      emoji
      sourceCodeUrl
      doc
      status
      likes
      likedByMe
      commentsCount
      tools {
        name
        description
      }
    }
  }
`

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
`

const LIKE_SKILL = gql`
  mutation LikeSkill($skillId: UUID!) {
    likeSkill(skillId: $skillId) {
      id
    }
  }
`

const UNLIKE_SKILL = gql`
  mutation UnlikeSkill($skillId: UUID!) {
    unlikeSkill(skillId: $skillId) {
      id
    }
  }
`

const COMMENT_ON_SKILL = gql`
  mutation CommentOnSkill($skillId: UUID!, $body: String!) {
    commentOnSkill(skillId: $skillId, body: $body) {
      id
      body
    }
  }
`

export function useIntegrations(): {
  integrations: Integration[]
  loading: boolean
  error?: Error
} {
  const { data, loading, error } = useQuery(SKILLS_QUERY, { skip: USE_MOCKS })
  if (USE_MOCKS) return { integrations: mockIntegrations, loading: false }
  const raw: Array<{
    id: string
    name: string
    title: string | null
    description: string | null
    emoji: string | null
    doc: string | null
    status: string | null
    likes: number | null
    tools: Array<{ name: string; description: string }> | null
  }> = data?.skills ?? []
  const integrations: Integration[] = raw.map((s) => {
    const mockMatch = mockIntegrations.find((m) => m.slug === s.name)
    return {
      name: s.title ?? s.name,
      slug: s.name,
      logo: mockMatch?.logo ?? s.emoji ?? "",
      description: s.description ?? "",
      likes: s.likes ?? 0,
      updatedAgo: "",
      tools: (s.tools ?? []).map((t) => ({ name: t.name, description: t.description })),
      credentials: mockMatch?.credentials ?? [],
      added: (s.status ?? "").toLowerCase() === "active",
      skillMd: s.doc ?? "",
    }
  })
  return { integrations, loading, error: error ?? undefined }
}

export type SkillComment = {
  id: string
  content: string
  createdAt: string
  authorName: string
}

export function useSkillComments(skillId: string): {
  comments: SkillComment[]
  loading: boolean
  error?: Error
} {
  const { data, loading, error } = useQuery(SKILL_COMMENTS_QUERY, {
    variables: { skillId },
    skip: USE_MOCKS || !skillId,
  })
  if (USE_MOCKS) return { comments: [], loading: false }
  const raw: Array<{
    id: string
    body: string
    createdAt: string
    author?: { name: string | null } | null
  }> = data?.skillComments ?? []
  const comments: SkillComment[] = raw.map((c) => ({
    id: c.id,
    content: c.body,
    createdAt: c.createdAt,
    authorName: c.author?.name ?? "Unknown",
  }))
  return { comments, loading, error: error ?? undefined }
}

export function useLikeSkill() {
  const [likeFn] = useMutation(LIKE_SKILL)
  const [unlikeFn] = useMutation(UNLIKE_SKILL)
  return async (skillId: string, liked: boolean): Promise<void> => {
    if (USE_MOCKS) return
    if (liked) await likeFn({ variables: { skillId } })
    else await unlikeFn({ variables: { skillId } })
  }
}

export function useCommentOnSkill() {
  const [fn, state] = useMutation(COMMENT_ON_SKILL)
  return {
    commentOnSkill: async (skillId: string, content: string) => {
      if (USE_MOCKS) return { id: `cm_${Date.now()}`, content }
      const { data } = await fn({ variables: { skillId, body: content } })
      return data?.commentOnSkill as { id: string; body: string }
    },
    ...state,
  }
}
