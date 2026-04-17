"use client"

import { gql, useMutation, useQuery } from "@apollo/client"
import { USE_MOCKS } from "@/lib/graphql/mock-mode"

export type NotificationPrefs = {
  taskCompletions: boolean
  email: boolean
  channelMessages: boolean
}

export type ProfilePayload = {
  id: string
  email: string
  name: string | null
  avatarUrl: string | null
  displayName: string | null
  timezone: string | null
  notificationPrefs: NotificationPrefs
}

const DEFAULT_PREFS: NotificationPrefs = {
  taskCompletions: false,
  email: false,
  channelMessages: false,
}

export const MOCK_PROFILE: ProfilePayload = {
  id: "usr_mock_1",
  email: "harro@officeos.co",
  name: "Harro Krog",
  avatarUrl: null,
  displayName: "Harro",
  timezone: "Europe/Amsterdam",
  notificationPrefs: DEFAULT_PREFS,
}

export const ME_QUERY = gql`
  query Me {
    me {
      id
      email
      name
      avatarUrl
      displayName
      timezone
      notificationPrefsJson
    }
  }
`

const UPDATE_PROFILE = gql`
  mutation UpdateProfile($input: UpdateProfileInput!) {
    updateProfile(input: $input) {
      id
      email
      name
      avatarUrl
      displayName
      timezone
      notificationPrefsJson
    }
  }
`

function parsePrefs(json: string | null | undefined): NotificationPrefs {
  if (!json) return DEFAULT_PREFS
  try {
    const p = JSON.parse(json) as Partial<NotificationPrefs>
    return {
      taskCompletions: Boolean(p.taskCompletions),
      email: Boolean(p.email),
      channelMessages: Boolean(p.channelMessages),
    }
  } catch {
    return DEFAULT_PREFS
  }
}

type MeRaw = {
  id: string
  email: string
  name: string | null
  avatarUrl: string | null
  displayName: string | null
  timezone: string | null
  notificationPrefsJson: string | null
}

function toProfile(raw: MeRaw): ProfilePayload {
  return {
    id: raw.id,
    email: raw.email,
    name: raw.name,
    avatarUrl: raw.avatarUrl,
    displayName: raw.displayName,
    timezone: raw.timezone,
    notificationPrefs: parsePrefs(raw.notificationPrefsJson),
  }
}

export function useProfile(): {
  profile: ProfilePayload | null
  loading: boolean
  error?: Error
} {
  const { data, loading, error } = useQuery(ME_QUERY, { skip: USE_MOCKS })
  if (USE_MOCKS) return { profile: MOCK_PROFILE, loading: false }
  const raw = data?.me as MeRaw | null | undefined
  if (!raw) return { profile: null, loading, error: error ?? undefined }
  return { profile: toProfile(raw), loading, error: error ?? undefined }
}

export function useUpdateProfile() {
  const [fn, state] = useMutation(UPDATE_PROFILE, {
    refetchQueries: [{ query: ME_QUERY }],
  })
  return {
    updateProfile: async (input: {
      name?: string | null
      displayName?: string | null
      timezone?: string | null
      notificationPrefs?: NotificationPrefs
    }): Promise<ProfilePayload> => {
      const payload = {
        name: input.name ?? null,
        displayName: input.displayName ?? null,
        timezone: input.timezone ?? null,
        notificationPrefsJson:
          input.notificationPrefs ? JSON.stringify(input.notificationPrefs) : null,
      }
      if (USE_MOCKS) {
        return {
          ...MOCK_PROFILE,
          name: payload.name ?? MOCK_PROFILE.name,
          displayName: payload.displayName ?? MOCK_PROFILE.displayName,
          timezone: payload.timezone ?? MOCK_PROFILE.timezone,
          notificationPrefs: input.notificationPrefs ?? MOCK_PROFILE.notificationPrefs,
        }
      }
      const { data } = await fn({ variables: { input: payload } })
      const raw = data?.updateProfile as MeRaw
      return toProfile(raw)
    },
    ...state,
  }
}
