"use client"

import { useState, useMemo } from "react"
import { useRouter } from "next/navigation"
import { PageHeader } from "@/components/page-header"
import { SearchInput } from "@/components/ui/search-input"
import { DataPagination } from "@/components/ui/data-pagination"
import { EmptyState } from "@/components/ui/empty-state"
import { useIntegrations, useSetSkillCredentials, sortIntegrations, CredentialDialog, IntegrationCard } from "@/features/agents"
import { Skeleton } from "@/components/ui/skeleton"
import { cn } from "@/lib/utils"
import { useFilterParams } from "@/hooks/useFilterParams"

const PAGE_SIZES = [25, 50, 100] as const

type View = "all" | "configured"

const FILTER_DEFAULTS = { q: null, view: "all", size: "50", page: "0", category: null } as const

export default function IntegrationsPage() {
  const router = useRouter()
  const { integrations, loading } = useIntegrations()
  const setCredentials = useSetSkillCredentials()
  const [configSlug, setConfigSlug] = useState<string | null>(null)

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

  const configIntegration = configSlug ? integrations.find((i) => i.name === configSlug) : null

  const allCategories = useMemo(() => {
    const cats = new Set<string>()
    integrations.forEach((i) => { if (i.category) cats.add(i.category) })
    return Array.from(cats).sort()
  }, [integrations])

  const filtered = useMemo(() => {
    const list = integrations.filter((i) => {
      if (search && !i.title.toLowerCase().includes(search.toLowerCase())) return false
      if (view === "configured" && !i.configured) return false
      if (selectedCategory && i.category !== selectedCategory) return false
      return true
    })
    return sortIntegrations(list)
  }, [integrations, search, view, selectedCategory])

  const paged = filtered.slice(page * pageSize, (page + 1) * pageSize)

  const configuredCount = integrations.filter((i) => i.configured).length

  return (
    <>
      <PageHeader group="Managed Agents" page="MCP Servers" />
      <div className="flex flex-1 flex-col gap-4 p-4 pt-0">
        <div className="flex items-center gap-2">
          <SearchInput
            placeholder="Search MCP servers..."
            value={search}
            onChange={(v) => { setSearch(v); setPage(0) }}
          />
          <div className="flex items-center rounded-lg border border-border">
            {([["all", "All"], ["configured", `Configured (${configuredCount})`]] as const).map(([key, label]) => (
              <button key={key} type="button" onClick={() => { setView(key as View); setPage(0) }}
                className={`px-3 py-1.5 text-xs font-medium transition-colors ${view === key ? "bg-primary text-primary-foreground" : "text-muted-foreground hover:text-foreground"} ${key === "all" ? "rounded-l-md" : ""} ${key === "configured" ? "rounded-r-md" : ""}`}>
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
              </div>
            ))}
          </div>
        ) : (
          <div className="grid grid-cols-1 gap-3 sm:grid-cols-2 lg:grid-cols-3">
            {paged.map((server) => (
              <IntegrationCard
                key={server.name}
                integration={server}
                variant="marketplace"
                onConfigure={() => setConfigSlug(server.name)}
                onClick={() => router.push(`/integrations/${server.name}`)}
              />
            ))}
          </div>
        )}

        {!loading && filtered.length === 0 && (
          <EmptyState message="No MCP servers found." />
        )}

        {!loading && filtered.length > 0 && (
          <DataPagination
            page={page}
            pageSize={pageSize}
            total={filtered.length}
            pageSizes={PAGE_SIZES}
            onPageChange={setPage}
            onPageSizeChange={(s) => { setPageSize(s); setPage(0) }}
          />
        )}
      </div>

      {configIntegration && (
        <CredentialDialog
          open={!!configSlug}
          onOpenChange={(open) => { if (!open) setConfigSlug(null) }}
          name={configIntegration.title}
          slug={configIntegration.name}
          logo={configIntegration.logo}
          credentials={configIntegration.credentialFields}
          onSave={async (values) => {
            await setCredentials(configIntegration.name, values)
            setConfigSlug(null)
          }}
        />
      )}
    </>
  )
}
