"use client"

import { gql, useMutation, useQuery } from "@apollo/client"
import type { Integration } from "../data/integrations"
import { sanitizeSvg } from "@/lib/sanitize-svg"

const SKILLS_QUERY = gql`
  query Skills {
    skills {
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

const INSTALL_SKILL = gql`
  mutation InstallSkill($name: String!) {
    installSkill(name: $name)
  }
`

const UNINSTALL_SKILL = gql`
  mutation UninstallSkill($name: String!) {
    uninstallSkill(name: $name)
  }
`

const SET_SKILL_CREDENTIALS = gql`
  mutation SetSkillCredentials($name: String!, $credentials: [SkillCredentialEntryInput!]!) {
    setSkillCredentials(name: $name, credentials: $credentials)
  }
`

const LIKE_SKILL = gql`
  mutation LikeSkill($skillId: UUID!) {
    likeSkill(skillId: $skillId) { id likes likedByMe }
  }
`

const UNLIKE_SKILL = gql`
  mutation UnlikeSkill($skillId: UUID!) {
    unlikeSkill(skillId: $skillId) { id likes likedByMe }
  }
`

const COMMENT_ON_SKILL = gql`
  mutation CommentOnSkill($skillId: UUID!, $body: String!) {
    commentOnSkill(skillId: $skillId, body: $body) {
      id
      body
      createdAt
      author { id name avatarUrl }
    }
  }
`

const DELETE_SKILL_COMMENT = gql`
  mutation DeleteSkillComment($commentId: UUID!) {
    deleteSkillComment(commentId: $commentId)
  }
`

export function useIntegrations(): {
  integrations: Integration[]
  loading: boolean
  error?: Error
} {
  const { data, loading, error } = useQuery(SKILLS_QUERY)
  const raw: Array<{
    id: string
    name: string
    title: string | null
    description: string | null
    logo: string | null
    doc: string | null
    sourceCodeUrl: string | null
    installed: boolean
    likes: number
    likedByMe: boolean
    commentsCount: number
    version: string | null
    license: string | null
    repository: string | null
    categories: string[] | null
    keywords: string[] | null
    readme: string | null
    changelog: string | null
    author: { name: string; url?: string | null } | null
    contributors: Array<{ name: string; url?: string | null }> | null
    createdAt: string | null
    updatedAt: string | null
    tools: Array<{ name: string; description: string }> | null
  }> = data?.skills ?? []

  const integrations: Integration[] = raw.map((s) => ({
    id: s.id,
    name: s.title ?? s.name,
    slug: s.name,
    logo: sanitizeSvg(s.logo ?? ""),
    description: s.description ?? "",
    likes: s.likes,
    likedByMe: s.likedByMe,
    commentsCount: s.commentsCount,
    tools: (s.tools ?? []).map((t) => ({ name: t.name, description: t.description })),
    installed: s.installed,
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
  }))

  return { integrations, loading, error: error ?? undefined }
}

export type SkillComment = {
  id: string
  body: string
  createdAt: string
  author: { id: string; name: string | null; avatarUrl: string | null }
}

export function useSkillComments(skillId: string): {
  comments: SkillComment[]
  loading: boolean
  error?: Error
  refetch: () => void
} {
  const { data, loading, error, refetch } = useQuery(SKILL_COMMENTS_QUERY, {
    variables: { skillId },
    skip: !skillId,
  })
  const raw: Array<{
    id: string
    body: string
    createdAt: string
    author?: { id: string; name: string | null; avatarUrl: string | null } | null
  }> = data?.skillComments ?? []

  const comments: SkillComment[] = raw.map((c) => ({
    id: c.id,
    body: c.body,
    createdAt: c.createdAt,
    author: {
      id: c.author?.id ?? "",
      name: c.author?.name ?? "Unknown",
      avatarUrl: c.author?.avatarUrl ?? null,
    },
  }))

  return { comments, loading, error: error ?? undefined, refetch }
}

export function useInstallSkill() {
  const [fn] = useMutation(INSTALL_SKILL, { refetchQueries: ["Skills"] })
  return async (name: string) => {
    await fn({ variables: { name } })
  }
}

export function useUninstallSkill() {
  const [fn] = useMutation(UNINSTALL_SKILL, { refetchQueries: ["Skills"] })
  return async (name: string) => {
    await fn({ variables: { name } })
  }
}

export function useSetSkillCredentials() {
  const [fn] = useMutation(SET_SKILL_CREDENTIALS, { refetchQueries: ["Skills"] })
  return async (name: string, credentials: Record<string, string>) => {
    const entries = Object.entries(credentials).map(([key, value]) => ({ key, value }))
    await fn({ variables: { name, credentials: entries } })
  }
}

export function useLikeSkill() {
  const [likeFn] = useMutation(LIKE_SKILL)
  const [unlikeFn] = useMutation(UNLIKE_SKILL)
  return async (skillId: string, liked: boolean): Promise<void> => {
    if (liked) await likeFn({ variables: { skillId } })
    else await unlikeFn({ variables: { skillId } })
  }
}

export function useCommentOnSkill() {
  const [fn, state] = useMutation(COMMENT_ON_SKILL)
  return {
    commentOnSkill: async (skillId: string, body: string) => {
      const { data } = await fn({ variables: { skillId, body } })
      return data?.commentOnSkill as SkillComment
    },
    ...state,
  }
}

export function useDeleteSkillComment() {
  const [fn] = useMutation(DELETE_SKILL_COMMENT)
  return async (commentId: string) => {
    await fn({ variables: { commentId } })
  }
}
