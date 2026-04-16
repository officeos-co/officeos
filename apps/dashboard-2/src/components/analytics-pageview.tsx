"use client"

import { useEffect } from "react"
import { usePathname } from "next/navigation"
import { useAnalytics } from "@/hooks/useAnalytics"

/**
 * Fires `$pageview` to the backend whenever the route changes. Kept as a
 * leaf client component so the surrounding layout can stay a server
 * component. Replaces the legacy dashboard's posthog-js auto-pageviews —
 * there is no PostHog JS snippet in dashboard-2.
 */
export function AnalyticsPageview() {
  const pathname = usePathname()
  const { capture } = useAnalytics()
  useEffect(() => {
    capture("$pageview", { path: pathname })
    // capture is stable enough; depend only on pathname to avoid double-firing
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [pathname])
  return null
}
