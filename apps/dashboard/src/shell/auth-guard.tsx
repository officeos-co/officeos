"use client"

import { useEffect } from "react"
import { useRouter } from "next/navigation"
import { useAuthContext } from "@/contexts/AuthContext"
import { Loader2Icon } from "lucide-react"

export function AuthGuard({ children }: { children: React.ReactNode }) {
  const { authenticated, loading } = useAuthContext()
  const router = useRouter()

  useEffect(() => {
    if (!loading && !authenticated) router.replace("/login")
  }, [loading, authenticated, router])

  if (loading) {
    return (
      <div className="flex min-h-screen items-center justify-center">
        <Loader2Icon className="size-6 animate-spin text-muted-foreground" />
      </div>
    )
  }

  if (!authenticated) return null

  return <>{children}</>
}
