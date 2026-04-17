"use client"

import { useState } from "react"
import Image from "next/image"
import { useRouter } from "next/navigation"
import { PageHeader } from "@/components/page-header"
import { CredentialDialog } from "@/features/agents"
import { Input } from "@/components/ui/input"
import { Button } from "@/components/ui/button"
import { useIntegrations } from "@/features/agents"
import { useAnalytics } from "@/features/analytics"
import { SearchIcon, HeartIcon, PlusIcon, CheckIcon, KeyRoundIcon } from "lucide-react"

type View = "all" | "added" | "explore"

export default function IntegrationsPage() {
  const router = useRouter()
  const { integrations } = useIntegrations()
  const { trackSkillInstalled, trackSkillConfigured } = useAnalytics()
  const [search, setSearch] = useState("")
  const [view, setView] = useState<View>("all")
  const [addedSet, setAddedSet] = useState<Set<string>>(() => new Set(integrations.filter((i) => i.added).map((i) => i.slug)))
  const [credDialogSlug, setCredDialogSlug] = useState<string | null>(null)

  const filtered = integrations.filter((i) => {
    if (search && !i.name.toLowerCase().includes(search.toLowerCase())) return false
    if (view === "added" && !addedSet.has(i.slug)) return false
    if (view === "explore" && addedSet.has(i.slug)) return false
    return true
  })

  const addedCount = integrations.filter((i) => addedSet.has(i.slug)).length
  const credDialogIntegration = credDialogSlug ? integrations.find((i) => i.slug === credDialogSlug) : null

  function handleAdd(slug: string, e: React.MouseEvent) {
    e.stopPropagation()
    const i = integrations.find((x) => x.slug === slug)
    if (!i) return
    if (i.credentials.length > 0) {
      setCredDialogSlug(slug)
    } else {
      setAddedSet((prev) => new Set([...prev, slug]))
      trackSkillInstalled(slug)
    }
  }

  function handleCredSave(slug: string) {
    setAddedSet((prev) => new Set([...prev, slug]))
    trackSkillConfigured(slug)
    trackSkillInstalled(slug)
  }

  return (
    <>
      <PageHeader group="Managed Agents" page="Integrations" />
      <div className="flex flex-1 flex-col gap-4 p-4 pt-0">
        <div className="flex items-center gap-2">
          <div className="relative flex-1 max-w-sm">
            <SearchIcon className="absolute left-2.5 top-2.5 size-4 text-muted-foreground" />
            <Input placeholder="Search integrations..." value={search} onChange={(e) => setSearch(e.target.value)} className="pl-8" />
          </div>
          <div className="flex items-center rounded-lg border border-border">
            {([["all", "All"], ["added", `Added (${addedCount})`], ["explore", "Explore"]] as const).map(([key, label]) => (
              <button key={key} type="button" onClick={() => setView(key)}
                className={`px-3 py-1.5 text-xs font-medium transition-colors ${view === key ? "bg-primary text-primary-foreground" : "text-muted-foreground hover:text-foreground"} ${key === "all" ? "rounded-l-md" : ""} ${key === "explore" ? "rounded-r-md" : ""}`}>
                {label}
              </button>
            ))}
          </div>
        </div>

        <div className="grid grid-cols-1 gap-3 sm:grid-cols-2 lg:grid-cols-3">
          {filtered.map((integration) => {
            const isAdded = addedSet.has(integration.slug)
            return (
              <button
                key={integration.slug}
                type="button"
                onClick={() => router.push(`/integrations/${integration.slug}`)}
                className="flex flex-col gap-3 rounded-xl border border-border p-4 text-left transition-colors hover:bg-muted/50 cursor-pointer"
              >
                <div className="flex items-start gap-3">
                  <Image src={integration.logo} alt={integration.name} width={32} height={32} className="shrink-0" />
                  <div className="min-w-0 flex-1">
                    <span className="font-medium text-sm">{integration.name}</span>
                  </div>
                  {isAdded ? (
                    <span className="flex items-center gap-1 rounded-full bg-emerald-100 px-2 py-0.5 text-[10px] font-semibold uppercase tracking-widest text-emerald-700">
                      <CheckIcon className="size-3" /> Added
                    </span>
                  ) : (
                    <Button size="sm" variant="outline" className="h-7 text-xs" onClick={(e) => handleAdd(integration.slug, e)}>
                      <PlusIcon className="size-3" /> Add
                    </Button>
                  )}
                </div>
                <p className="text-sm line-clamp-2 text-muted-foreground">{integration.description}</p>
                <div className="flex items-center gap-3 text-xs text-muted-foreground">
                  <span>{integration.tools.length} tools</span>
                  <span>·</span>
                  <span className="flex items-center gap-1"><HeartIcon className="size-3" />{integration.likes}</span>
                  {isAdded && integration.credentials.length > 0 && (
                    <>
                      <span>·</span>
                      <span
                        role="button"
                        onClick={(e) => { e.stopPropagation(); setCredDialogSlug(integration.slug) }}
                        className="flex items-center gap-1 hover:text-foreground"
                      >
                        <KeyRoundIcon className="size-3" /> Credentials
                      </span>
                    </>
                  )}
                </div>
              </button>
            )
          })}
        </div>

        {filtered.length === 0 && (
          <div className="py-8 text-center text-sm text-muted-foreground">No integrations found.</div>
        )}
      </div>

      {/* Credential overlay */}
      {credDialogIntegration && (
        <CredentialDialog
          open={!!credDialogSlug}
          onOpenChange={(open) => { if (!open) setCredDialogSlug(null) }}
          name={credDialogIntegration.name}
          logo={credDialogIntegration.logo}
          credentials={credDialogIntegration.credentials}
          onSave={() => handleCredSave(credDialogIntegration.slug)}
        />
      )}
    </>
  )
}
