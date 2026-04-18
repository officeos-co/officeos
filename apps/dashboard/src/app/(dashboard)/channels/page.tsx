"use client"

import { useState } from "react"
import { useRouter } from "next/navigation"
import { PageHeader } from "@/components/page-header"
import { ChannelOnboardingDialog } from "@/features/agents"
import { Button } from "@/components/ui/button"
import { type Channel } from "@/features/agents/data/channels"
import { useChannels, useCreateChannelConnection, useBindChannelToAgent } from "@/features/agents"
import { useAnalytics } from "@/features/analytics"
import { PlusIcon, RadioIcon } from "lucide-react"

type View = "all" | "connected" | "available"

export default function ChannelsPage() {
  const router = useRouter()
  const { channels, loading } = useChannels()
  const { createChannelConnection } = useCreateChannelConnection()
  const { bindChannelToAgent } = useBindChannelToAgent()
  const { trackChannelConnected } = useAnalytics()
  void createChannelConnection
  void bindChannelToAgent
  const [view, setView] = useState<View>("all")
  const [onboardingChannel, setOnboardingChannel] = useState<Channel | null>(null)

  const filtered = channels.filter((c) => {
    if (view === "connected" && !c.added) return false
    if (view === "available" && c.added) return false
    return true
  })

  const connectedCount = channels.filter((c) => c.added).length

  return (
    <>
      <PageHeader group="Managed Agents" page="Channels" />
      <div className="flex flex-1 flex-col gap-4 p-4 pt-0">
        <div className="flex items-center gap-2">
          <div className="flex items-center rounded-lg border border-border">
            {([["all", "All"], ["connected", `Connected (${connectedCount})`], ["available", "Available"]] as const).map(([key, label]) => (
              <button key={key} type="button" onClick={() => setView(key)}
                className={`px-3 py-1.5 text-xs font-medium transition-colors ${view === key ? "bg-primary text-primary-foreground" : "text-muted-foreground hover:text-foreground"} ${key === "all" ? "rounded-l-md" : ""} ${key === "available" ? "rounded-r-md" : ""}`}>
                {label}
              </button>
            ))}
          </div>
        </div>

        {loading ? (
          <div className="py-8 text-center text-sm text-muted-foreground">Loading channels...</div>
        ) : (
          <div className="grid grid-cols-1 gap-3 sm:grid-cols-2 lg:grid-cols-3">
            {filtered.map((channel) => (
              <button
                key={channel.slug}
                type="button"
                onClick={() => router.push(`/channels/${channel.slug}`)}
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
              </button>
            ))}
          </div>
        )}

        {!loading && filtered.length === 0 && (
          <div className="py-8 text-center text-sm text-muted-foreground">No channels found.</div>
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
