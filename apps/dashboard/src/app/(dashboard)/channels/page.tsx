"use client"

import { useState, useMemo } from "react"
import { useRouter } from "next/navigation"
import { PageHeader } from "@/components/page-header"
import { ChannelOnboardingDialog } from "@/features/agents"
import { Button } from "@/components/ui/button"
import { DataPagination } from "@/components/ui/data-pagination"
import { EmptyState } from "@/components/ui/empty-state"
import { type Channel } from "@/features/agents/data/channels"
import { useChannels, useCreateChannelConnection, useBindChannelToAgent } from "@/features/agents"
import { useAnalytics } from "@/features/analytics"
import { Skeleton } from "@/components/ui/skeleton"
import { PlusIcon, RadioIcon } from "lucide-react"
import { useFilterParams } from "@/hooks/useFilterParams"

const PAGE_SIZES = [25, 50, 100] as const

type View = "all" | "connected" | "available"

const FILTER_DEFAULTS = { view: "all", size: "50", page: "0" } as const

export default function ChannelsPage() {
  const router = useRouter()
  const { channels, loading } = useChannels()
  const { createChannelConnection } = useCreateChannelConnection()
  const { bindChannelToAgent } = useBindChannelToAgent()
  const { trackChannelConnected } = useAnalytics()
  void createChannelConnection
  void bindChannelToAgent
  const [onboardingChannel, setOnboardingChannel] = useState<Channel | null>(null)

  const { get, set: setParams } = useFilterParams(FILTER_DEFAULTS, "/channels")

  const view = (get("view") as View) ?? "all"
  const pageSize = Number(get("size")) || 50
  const page = Number(get("page")) || 0

  const setView = (v: View) => setParams({ view: v, page: null })
  const setPageSize = (v: number) => setParams({ size: String(v), page: null })
  const setPage = (v: number) => setParams({ page: String(v) })

  const filtered = useMemo(() => {
    return channels.filter((c) => {
      if (view === "connected" && !c.added) return false
      if (view === "available" && c.added) return false
      return true
    })
  }, [channels, view])

  const paged = filtered.slice(page * pageSize, (page + 1) * pageSize)

  const connectedCount = channels.filter((c) => c.added).length

  return (
    <>
      <PageHeader group="Managed Agents" page="Channels" />
      <div className="flex flex-1 flex-col gap-4 p-4 pt-0">
        <div className="flex items-center gap-2">
          <div className="flex items-center rounded-lg border border-border">
            {([["all", "All"], ["connected", `Connected (${connectedCount})`], ["available", "Available"]] as const).map(([key, label]) => (
              <button key={key} type="button" onClick={() => { setView(key); setPage(0) }}
                className={`px-3 py-1.5 text-xs font-medium transition-colors ${view === key ? "bg-primary text-primary-foreground" : "text-muted-foreground hover:text-foreground"} ${key === "all" ? "rounded-l-md" : ""} ${key === "available" ? "rounded-r-md" : ""}`}>
                {label}
              </button>
            ))}
          </div>
        </div>

        {loading && channels.length === 0 ? (
          <div className="grid grid-cols-1 gap-3 sm:grid-cols-2 lg:grid-cols-3">
            {Array.from({ length: 3 }).map((_, i) => (
              <div key={i} className="flex flex-col gap-3 rounded-xl border border-border p-4">
                <div className="flex items-start gap-3">
                  <Skeleton className="size-8 rounded-full shrink-0" />
                  <div className="flex-1 pt-0.5">
                    <Skeleton className="h-4 w-24" />
                  </div>
                  <Skeleton className="h-7 w-20 rounded-md" />
                </div>
                <Skeleton className="h-4 w-full" />
                <Skeleton className="h-4 w-2/3" />
              </div>
            ))}
          </div>
        ) : (
          <div className="grid grid-cols-1 gap-3 sm:grid-cols-2 lg:grid-cols-3">
            {paged.map((channel) => (
              <div
                key={channel.slug}
                role="button"
                tabIndex={0}
                onClick={() => router.push(`/channels/${channel.slug}`)}
                onKeyDown={(e) => { if (e.key === 'Enter' || e.key === ' ') { e.preventDefault(); router.push(`/channels/${channel.slug}`) } }}
                className="flex flex-col gap-3 rounded-xl border border-border p-4 text-left transition-colors hover:bg-muted/50 cursor-pointer"
              >
                <div className="flex items-start gap-3">
                  <div className="size-8 shrink-0 [&>svg]:size-8" dangerouslySetInnerHTML={{ __html: channel.logo }} />
                  <div className="min-w-0 flex-1">
                    <div className="font-medium text-sm">{channel.name}</div>
                  </div>
                  {channel.added ? (
                    <span className="flex items-center gap-1 rounded-full bg-emerald-100 px-2 py-0.5 text-[10px] font-semibold uppercase tracking-widest text-emerald-700">
                      <RadioIcon className="size-3" /> Live
                    </span>
                  ) : (
                    <Button size="sm" variant="outline" className="h-7 text-xs" onClick={(e) => { e.stopPropagation(); setOnboardingChannel(channel) }}>
                      <PlusIcon className="size-3" /> Connect
                    </Button>
                  )}
                </div>
                <p className="text-sm line-clamp-2 text-muted-foreground">{channel.description}</p>
              </div>
            ))}
          </div>
        )}

        {!loading && filtered.length === 0 && (
          <EmptyState message="No channels found." />
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

      {onboardingChannel && (
        <ChannelOnboardingDialog
          open={!!onboardingChannel}
          onOpenChange={(open) => { if (!open) setOnboardingChannel(null) }}
          channel={onboardingChannel}
          onComplete={() => {
            trackChannelConnected(onboardingChannel.slug)
            setOnboardingChannel(null)
          }}
        />
      )}
    </>
  )
}
