"use client"

import { gql, useMutation, useQuery } from "@apollo/client"

export type OrgMember = {
  id: string
  organizationId: string
  userId: string | null
  email: string
  name: string | null
  role: "Owner" | "Admin" | "Member"
  status: "active" | "invited"
  joinedAgo: string
}

export type OrganizationPayload = {
  id: string
  name: string
  ownerUserId: string
  members: OrgMember[]
}

const ORG_QUERY = gql`
  query Org {
    org {
      id
      name
      ownerUserId
      members {
        id
        organizationId
        userId
        email
        name
        role
        status
        createdAt
      }
    }
  }
`

const INVITE_MEMBER = gql`
  mutation InviteMember($input: InviteMemberInput!) {
    inviteMember(input: $input) {
      id
      organizationId
      userId
      email
      name
      role
      status
      createdAt
    }
  }
`

const REMOVE_MEMBER = gql`
  mutation RemoveMember($memberId: UUID!) {
    removeMember(memberId: $memberId)
  }
`

const RENAME_ORG = gql`
  mutation RenameOrg($input: RenameOrgInput!) {
    renameOrg(input: $input) {
      id
      name
      ownerUserId
    }
  }
`

function humanAgo(iso: string | null | undefined): string {
  if (!iso) return ""
  const t = Date.parse(iso)
  if (Number.isNaN(t)) return ""
  const d = Math.floor((Date.now() - t) / 86_400_000)
  if (d < 1) return "just now"
  if (d < 30) return `${d} day${d === 1 ? "" : "s"} ago`
  const months = Math.floor(d / 30)
  if (months < 12) return `${months} month${months === 1 ? "" : "s"} ago`
  const years = Math.floor(months / 12)
  return `${years} year${years === 1 ? "" : "s"} ago`
}

type MemberRaw = {
  id: string
  organizationId: string
  userId: string | null
  email: string
  name: string | null
  role: string
  status: string
  createdAt: string
}

function toMember(m: MemberRaw): OrgMember {
  return {
    id: m.id,
    organizationId: m.organizationId,
    userId: m.userId,
    email: m.email,
    name: m.name,
    role: (m.role as OrgMember["role"]) ?? "Member",
    status: (m.status as OrgMember["status"]) ?? "invited",
    joinedAgo: humanAgo(m.createdAt),
  }
}

export function useOrganization(): {
  organization: OrganizationPayload | null
  loading: boolean
  error?: Error
} {
  const { data, loading, error } = useQuery(ORG_QUERY)
  const raw = data?.org as
    | { id: string; name: string; ownerUserId: string; members: MemberRaw[] }
    | null
    | undefined
  if (!raw) return { organization: null, loading, error: error ?? undefined }
  const organization: OrganizationPayload = {
    id: raw.id,
    name: raw.name,
    ownerUserId: raw.ownerUserId,
    members: (raw.members ?? []).map(toMember),
  }
  return { organization, loading, error: error ?? undefined }
}

export function useInviteMember() {
  const [fn, state] = useMutation(INVITE_MEMBER)
  return {
    inviteMember: async (input: { email: string; role?: "Admin" | "Member" }): Promise<OrgMember> => {
      const { data } = await fn({
        variables: { input: { email: input.email, role: input.role ?? "Member" } },
        optimisticResponse: {
          inviteMember: {
            __typename: "OrgMember",
            id: `m_optimistic_${Date.now().toString(36)}`,
            organizationId: "",
            userId: null,
            email: input.email,
            name: null,
            role: input.role ?? "Member",
            status: "invited",
            createdAt: new Date().toISOString(),
          },
        },
        update(cache, { data: result }) {
          if (!result?.inviteMember) return
          const existing = cache.readQuery<{ org: { id: string; name: string; ownerUserId: string; members: MemberRaw[] } }>({ query: ORG_QUERY })
          if (existing?.org) {
            cache.writeQuery({
              query: ORG_QUERY,
              data: {
                org: {
                  ...existing.org,
                  members: [...existing.org.members, result.inviteMember],
                },
              },
            })
          }
        },
      })
      return toMember(data?.inviteMember as MemberRaw)
    },
    ...state,
  }
}

export function useRemoveMember() {
  const [fn, state] = useMutation(REMOVE_MEMBER)
  return {
    removeMember: async (memberId: string): Promise<boolean> => {
      const { data } = await fn({
        variables: { memberId },
        optimisticResponse: { removeMember: true },
        update(cache) {
          const existing = cache.readQuery<{ org: { id: string; name: string; ownerUserId: string; members: Array<{ id: string }> } }>({ query: ORG_QUERY })
          if (existing?.org) {
            cache.writeQuery({
              query: ORG_QUERY,
              data: {
                org: {
                  ...existing.org,
                  members: existing.org.members.filter((m) => m.id !== memberId),
                },
              },
            })
          }
        },
      })
      return Boolean(data?.removeMember)
    },
    ...state,
  }
}

export function useRenameOrg() {
  const [fn, state] = useMutation(RENAME_ORG)
  return {
    renameOrg: async (name: string): Promise<{ id: string; name: string }> => {
      const { data } = await fn({
        variables: { input: { name } },
        optimisticResponse: {
          renameOrg: {
            __typename: "Organization",
            id: "optimistic",
            name,
            ownerUserId: "",
          },
        },
        update(cache, { data: result }) {
          if (!result?.renameOrg) return
          const existing = cache.readQuery<{ org: { id: string; name: string; ownerUserId: string; members: unknown[] } }>({ query: ORG_QUERY })
          if (existing?.org) {
            cache.writeQuery({
              query: ORG_QUERY,
              data: { org: { ...existing.org, name } },
            })
          }
        },
      })
      return data?.renameOrg as { id: string; name: string }
    },
    ...state,
  }
}
