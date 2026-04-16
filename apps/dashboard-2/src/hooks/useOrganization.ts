"use client"

import { gql, useMutation, useQuery } from "@apollo/client"
import { USE_MOCKS } from "@/lib/graphql/mock-mode"

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

const MOCK_ORG: OrganizationPayload = {
  id: "org_mock_1",
  name: "Acme Corp",
  ownerUserId: "usr_mock_1",
  members: [
    { id: "m1", organizationId: "org_mock_1", userId: "usr_mock_1", email: "harro@officeos.co", name: "Harro Krog", role: "Owner", status: "active", joinedAgo: "6 months ago" },
    { id: "m2", organizationId: "org_mock_1", userId: null, email: "anna@officeos.co", name: "Anna Schmidt", role: "Admin", status: "active", joinedAgo: "3 months ago" },
    { id: "m3", organizationId: "org_mock_1", userId: null, email: "max@officeos.co", name: "Max Weber", role: "Member", status: "active", joinedAgo: "1 month ago" },
  ],
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
  organization: OrganizationPayload
  loading: boolean
  error?: Error
} {
  const { data, loading, error } = useQuery(ORG_QUERY, { skip: USE_MOCKS })
  if (USE_MOCKS) return { organization: MOCK_ORG, loading: false }
  const raw = data?.org as
    | { id: string; name: string; ownerUserId: string; members: MemberRaw[] }
    | null
    | undefined
  if (!raw) return { organization: MOCK_ORG, loading, error: error ?? undefined }
  const organization: OrganizationPayload = {
    id: raw.id,
    name: raw.name,
    ownerUserId: raw.ownerUserId,
    members: (raw.members ?? []).map(toMember),
  }
  return { organization, loading, error: error ?? undefined }
}

export function useInviteMember() {
  const [fn, state] = useMutation(INVITE_MEMBER, {
    refetchQueries: [{ query: ORG_QUERY }],
  })
  return {
    inviteMember: async (input: { email: string; role?: "Admin" | "Member" }): Promise<OrgMember> => {
      if (USE_MOCKS) {
        return {
          id: `m_${Date.now().toString(36)}`,
          organizationId: MOCK_ORG.id,
          userId: null,
          email: input.email,
          name: null,
          role: input.role ?? "Member",
          status: "invited",
          joinedAgo: "just now",
        }
      }
      const { data } = await fn({
        variables: { input: { email: input.email, role: input.role ?? "Member" } },
      })
      return toMember(data?.inviteMember as MemberRaw)
    },
    ...state,
  }
}

export function useRemoveMember() {
  const [fn, state] = useMutation(REMOVE_MEMBER, {
    refetchQueries: [{ query: ORG_QUERY }],
  })
  return {
    removeMember: async (memberId: string): Promise<boolean> => {
      if (USE_MOCKS) return true
      const { data } = await fn({ variables: { memberId } })
      return Boolean(data?.removeMember)
    },
    ...state,
  }
}

export function useRenameOrg() {
  const [fn, state] = useMutation(RENAME_ORG, {
    refetchQueries: [{ query: ORG_QUERY }],
  })
  return {
    renameOrg: async (name: string): Promise<{ id: string; name: string }> => {
      if (USE_MOCKS) return { id: MOCK_ORG.id, name }
      const { data } = await fn({ variables: { input: { name } } })
      return data?.renameOrg as { id: string; name: string }
    },
    ...state,
  }
}
