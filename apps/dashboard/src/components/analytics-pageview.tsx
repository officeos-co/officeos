"use client"

import { useEffect } from "react"
import { usePathname } from "next/navigation"
import { useAnalytics } from "@/features/analytics"
import { isDevelopment } from "@/lib/env"

/**
 * Fires `$pageview` to the backend whenever the route changes. Kept as a
 * leaf client component so the surrounding layout can stay a server
 * component. Disabled in development — no PostHog in dev.
 */
export function AnalyticsPageview() {
  const pathname = usePathname()
  const { trackPageView } = useAnalytics()
  useEffect(() => {
    if (isDevelopment()) return
    trackPageView(pathname)
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [pathname])
  return null
}
