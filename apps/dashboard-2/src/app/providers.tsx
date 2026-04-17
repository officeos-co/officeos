"use client"

import { ApolloProvider } from "@apollo/client"
import { apolloClient } from "@/lib/graphql/client"
import { AuthProvider } from "@/contexts/AuthContext"

/**
 * Client-side provider tree. Keep this thin — any providers that must run
 * in the browser (Apollo, theme, toast host, etc.) belong here. `layout.tsx`
 * stays a server component and just wraps children in <Providers>.
 */
export function Providers({ children }: { children: React.ReactNode }) {
  return (
    <ApolloProvider client={apolloClient}>
      <AuthProvider>{children}</AuthProvider>
    </ApolloProvider>
  )
}

export default Providers
