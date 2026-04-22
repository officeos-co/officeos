"use client"

import { useState, useMemo } from "react"
import { useRouter } from "next/navigation"
import { PageHeader } from "@/components/page-header"
import { Input } from "@/components/ui/input"
import { Button } from "@/components/ui/button"
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select"
import { useIntegrations, useInstallSkill, sortIntegrations, CredentialDialog, IntegrationCard } from "@/features/agents"
import { useAnalytics } from "@/features/analytics"
import { Skeleton } from "@/components/ui/skeleton"
import { SearchIcon, ChevronLeftIcon, ChevronRightIcon } from "lucide-react"
import { cn } from "@/lib/utils"
import { useFilterParams } from "@/hooks/useFilterParams"

const PAGE_SIZES = [25, 50, 100] as const

type View = "all" | "installed" | "explore"

const FILTER_DEFAULTS = { q: null, view: "all", size: "50", page: "0", category: null } as const

export default function IntegrationsPage() {
  const router = useRouter()
  const { integrations, loading } = useIntegrations()
  const installSkill = useInstallSkill()
  const { trackSkillInstalled } = useAnalytics()
  const [configSlug, setConfigSlug] = useState<string | null>(null)
  const [installingSlug, setInstallingSlug] = useState<string | null>(null)

  const { get, set: setParams } = useFilterParams(FILTER_DEFAULTS, "/integrations")

  const search = get("q") ?? ""
  const view = (get("view") as View) ?? "all"
  const pageSize = Number(get("size")) || 50
  const page = Number(get("page")) || 0
  const selectedCategory = get("category")

  const setSearch = (v: string) => setParams({ q: v, page: null })
  const setView = (v: View) => setParams({ view: v, page: null })
  const setPageSize = (v: number) => setParams({ size: String(v), page: null })
  const setPage = (v: number) => setParams({ page: String(v) })
  const setSelectedCategory = (v: string | null) => setParams({ category: v, page: null })

  const configIntegration = configSlug ? integrations.find((i) => i.slug === configSlug) : null

  const allCategories = useMemo(() => {
    const cats = new Set<string>()
    integrations.forEach((i) => i.categories.forEach((c) => cats.add(c)))
    return Array.from(cats).sort()
  }, [integrations])

  const filtered = useMemo(() => {
    const list = integrations.filter((i) => {
      if (search && !i.name.toLowerCase().includes(search.toLowerCase())) return false
      if (view === "installed" && !i.installed) return false
      if (view === "explore" && i.installed) return false
      if (selectedCategory && !i.categories.includes(selectedCategory)) return false
      return true
    })
    return sortIntegrations(list)
  }, [integrations, search, view, selectedCategory])

  const totalPages = Math.ceil(filtered.length / pageSize)
  const paged = filtered.slice(page * pageSize, (page + 1) * pageSize)

  const installedCount = integrations.filter((i) => i.installed).length

  return (
    <>
      <PageHeader group="Managed Agents" page="Integrations" />
      <div className="flex flex-1 flex-col gap-4 p-4 pt-0">
        <div className="flex items-center gap-2">
          <div className="relative flex-1 max-w-sm">
            <SearchIcon className="absolute left-2.5 top-2.5 size-4 text-muted-foreground" />
            <Input placeholder="Search integrations..." value={search} onChange={(e) => { setSearch(e.target.value); setPage(0) }} className="pl-8" />
          </div>
          <div className="flex items-center rounded-lg border border-border">
            {([["all", "All"], ["installed", `Installed (${installedCount})`], ["explore", "Explore"]] as const).map(([key, label]) => (
              <button key={key} type="button" onClick={() => { setView(key); setPage(0) }}
                className={`px-3 py-1.5 text-xs font-medium transition-colors ${view === key ? "bg-primary text-primary-foreground" : "text-muted-foreground hover:text-foreground"} ${key === "all" ? "rounded-l-md" : ""} ${key === "explore" ? "rounded-r-md" : ""}`}>
                {label}
              </button>
            ))}
          </div>
        </div>

        {allCategories.length > 0 && (
          <div className="flex flex-wrap gap-2 mb-4">
            <button
              onClick={() => setSelectedCategory(null)}
              className={cn(
                "rounded-full px-3 py-1 text-xs font-medium transition-colors",
                !selectedCategory ? "bg-primary text-primary-foreground" : "bg-muted text-muted-foreground hover:bg-muted/80"
              )}
            >
              All
            </button>
            {allCategories.map((cat) => (
              <button
                key={cat}
                onClick={() => setSelectedCategory(cat === selectedCategory ? null : cat)}
                className={cn(
                  "rounded-full px-3 py-1 text-xs font-medium transition-colors",
                  cat === selectedCategory ? "bg-primary text-primary-foreground" : "bg-muted text-muted-foreground hover:bg-muted/80"
                )}
              >
                {cat}
              </button>
            ))}
          </div>
        )}

        {loading && integrations.length === 0 ? (
          <div className="grid grid-cols-1 gap-3 sm:grid-cols-2 lg:grid-cols-3">
            {Array.from({ length: 3 }).map((_, i) => (
              <div key={i} className="flex flex-col gap-3 rounded-xl border border-border p-4">
                <div className="flex items-start gap-3">
                  <Skeleton className="size-8 rounded-full shrink-0" />
                  <div className="flex-1 pt-0.5">
                    <Skeleton className="h-4 w-28" />
                  </div>
                  <Skeleton className="h-7 w-14 rounded-md" />
                </div>
                <Skeleton className="h-4 w-full" />
                <Skeleton className="h-4 w-3/4" />
                <div className="flex items-center gap-3">
                  <Skeleton className="h-3 w-16" />
                  <Skeleton className="h-3 w-12" />
                </div>
              </div>
            ))}
          </div>
        ) : (
          <div className="grid grid-cols-1 gap-3 sm:grid-cols-2 lg:grid-cols-3">
            {paged.map((integration) => (
              <IntegrationCard
                key={integration.slug}
                integration={integration}
                variant="marketplace"
                installing={installingSlug === integration.slug}
                onInstall={async () => {
                  setInstallingSlug(integration.slug)
                  try { await installSkill(integration.slug); trackSkillInstalled(integration.slug) }
                  finally { setInstallingSlug(null) }
                }}
                onConfigure={() => setConfigSlug(integration.slug)}
                onClick={() => router.push(`/integrations/${integration.slug}`)}
              />
            ))}
          </div>
        )}

        {!loading && filtered.length === 0 && (
          <div className="py-8 text-center text-sm text-muted-foreground">No integrations found.</div>
        )}

        {/* Pagination */}
        {!loading && filtered.length > 0 && (
          <div className="flex items-center justify-between text-sm">
            <div className="flex items-center gap-2 text-muted-foreground">
              <span>Rows per page</span>
              <Select value={String(pageSize)} onValueChange={(v) => { if (v) { setPageSize(Number(v)); setPage(0) } }}>
                <SelectTrigger className="w-[70px] h-8"><SelectValue /></SelectTrigger>
                <SelectContent>
                  {PAGE_SIZES.map((s) => <SelectItem key={s} value={String(s)}>{s}</SelectItem>)}
                </SelectContent>
              </Select>
            </div>
            <div className="flex items-center gap-2">
              <span className="text-muted-foreground text-xs">
                {filtered.length > 0 ? `${page * pageSize + 1}–${Math.min((page + 1) * pageSize, filtered.length)} of ${filtered.length}` : "0 results"}
              </span>
              <Button variant="outline" size="icon" className="h-8 w-8" disabled={page === 0} onClick={() => setPage(page - 1)}>
                <ChevronLeftIcon className="size-4" />
              </Button>
              <Button variant="outline" size="icon" className="h-8 w-8" disabled={page >= totalPages - 1} onClick={() => setPage(page + 1)}>
                <ChevronRightIcon className="size-4" />
              </Button>
            </div>
          </div>
        )}
      </div>

      {configIntegration && (
        <CredentialDialog
          open={!!configSlug}
          onOpenChange={(open) => { if (!open) setConfigSlug(null) }}
          name={configIntegration.name}
          slug={configIntegration.slug}
          logo={configIntegration.logo}
          credentials={configIntegration.credentialFields}
          onSave={() => {}}
        />
      )}
    </>
  )
}
