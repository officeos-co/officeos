"use client"

import { useEffect } from "react"
import { useRouter, useSearchParams } from "next/navigation"
import { useAuthContext } from "@/contexts/AuthContext"
import { LoginForm } from "@/features/manage"

export default function LoginPage() {
  const { authenticated, loading } = useAuthContext()
  const router = useRouter()
  const searchParams = useSearchParams()
  const returnTo = safeReturnTo(searchParams.get("returnTo"))

  useEffect(() => {
    if (authenticated) router.replace(returnTo)
  }, [authenticated, returnTo, router])

  if (loading || authenticated) {
    return (
      <div className="flex min-h-svh items-center justify-center">
        <div className="text-muted-foreground text-sm">Loading...</div>
      </div>
    )
  }

  return (
    <div className="grid min-h-svh lg:grid-cols-2">
      <div className="flex flex-col gap-4 p-6 md:p-10">
        <div className="flex justify-center gap-2 md:justify-start">
          <a href="https://officeos.co" className="flex items-center gap-2 font-semibold text-lg text-primary">
            OfficeOS
          </a>
        </div>
        <div className="flex flex-1 items-center justify-center">
          <div className="w-full max-w-xs">
            <LoginForm returnTo={returnTo} />
          </div>
        </div>
      </div>
      <div className="relative hidden lg:block">
        <div className="absolute inset-0 [background:radial-gradient(125%_125%_at_50%_10%,var(--background)_40%,var(--secondary)_100%)]" />
      </div>
    </div>
  )
}

function safeReturnTo(value: string | null): string {
  if (!value || !value.startsWith("/") || value.startsWith("//") || value.includes("://")) {
    return "/agents"
  }
  return value
}
