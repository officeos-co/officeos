"use client"

import { useQuery, useMutation, gql } from "@apollo/client"
import { apolloClient } from "@/lib/graphql/client"
import { ME_QUERY } from "@/features/manage"

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
  const { data, loading, error } = useQuery(ME_QUERY)
  const [logoutMutation] = useMutation(LOGOUT_MUTATION)

  const logout = async () => {
    try {
      await logoutMutation()
    } catch {
      // ignore — session may already be expired
    }
    await apolloClient.clearStore()
    window.location.href = "/login"
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
