"use client"

import { useQuery, useMutation, gql } from "@apollo/client"
import { apolloClient } from "@/lib/graphql/client"
import { USE_MOCKS } from "@/lib/graphql/mock-mode"
import { ME_QUERY, MOCK_PROFILE } from "@/features/manage"

export type AuthUser = {
  id: string
  name: string | null
  email: string
  avatarUrl: string | null
}

const LOGOUT_MUTATION = gql`
  mutation Logout {
    logout
  }
`

export function useAuth(): {
  user: AuthUser | null
  loading: boolean
  authenticated: boolean
  logout: () => Promise<void>
} {
  const { data, loading, error } = useQuery(ME_QUERY, { skip: USE_MOCKS })
  const [logoutMutation] = useMutation(LOGOUT_MUTATION)

  const logout = async () => {
    if (!USE_MOCKS) {
      try {
        await logoutMutation()
      } catch {
        // ignore — session may already be expired
      }
      await apolloClient.clearStore()
    }
    window.location.href = "/login"
  }

  if (USE_MOCKS) {
    return {
      user: {
        id: MOCK_PROFILE.id,
        name: MOCK_PROFILE.name,
        email: MOCK_PROFILE.email,
        avatarUrl: MOCK_PROFILE.avatarUrl,
      },
      loading: false,
      authenticated: true,
      logout,
    }
  }

  const raw = data?.me as AuthUser | null | undefined
  if (loading) return { user: null, loading: true, authenticated: false, logout }
  if (error || !raw) return { user: null, loading: false, authenticated: false, logout }

  return {
    user: raw,
    loading: false,
    authenticated: true,
    logout,
  }
}
