"use client"

import { useCallback } from "react"
import { useRouter, useSearchParams } from "next/navigation"

/**
 * Persists filter/pagination state in URL search params so it survives
 * navigation (e.g. detail page → back). Params that match their default
 * value are omitted from the URL to keep it clean.
 */
export function useFilterParams<
  T extends Record<string, string | null>,
>(defaults: T, basePath: string) {
  const router = useRouter()
  const searchParams = useSearchParams()

  const get = useCallback(
    (key: keyof T & string): string | null => {
      return searchParams.get(key) ?? defaults[key]
    },
    [searchParams, defaults],
  )

  const set = useCallback(
    (updates: Partial<Record<keyof T & string, string | null>>) => {
      const params = new URLSearchParams(searchParams.toString())
      for (const [k, v] of Object.entries(updates)) {
        const def = defaults[k as keyof T & string] ?? null
        if (v === null || v === "" || v === def) {
          params.delete(k)
        } else {
          params.set(k, v!)
        }
      }
      const qs = params.toString()
      router.replace(qs ? `${basePath}?${qs}` : basePath, { scroll: false })
    },
    [searchParams, router, defaults, basePath],
  )

  return { get, set, searchParams }
}
