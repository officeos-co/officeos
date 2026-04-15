"use client"

import { useState } from "react"
import Image from "next/image"
import { useRouter } from "next/navigation"
import { PageHeader } from "@/components/page-header"
import { ChannelOnboardingDialog } from "@/components/channel-onboarding-dialog"
import { Button } from "@/components/ui/button"
import { channels, type Channel } from "@/data/channels"
import { PlusIcon, CheckIcon, RadioIcon } from "lucide-react"

type View = "all" | "connected" | "available"

export default function ChannelsPage() {
  const router = useRouter()
  const [view, setView] = useState<View>("all")
  const [connectedSet, setConnectedSet] = useState<Set<string>>(() => new Set(channels.filter((c) => c.added).map((c) => c.slug)))
  const [onboardingChannel, setOnboardingChannel] = useState<Channel | null>(null)

  const filtered = channels.filter((c) => {
    if (view === "connected" && !connectedSet.has(c.slug)) return false
    if (view === "available" && connectedSet.has(c.slug)) return false
    return true
  })

  const connectedCount = channels.filter((c) => connectedSet.has(c.slug)).length

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

        <div className="grid grid-cols-1 gap-3 sm:grid-cols-2 lg:grid-cols-3">
          {filtered.map((channel) => {
            const isConnected = connectedSet.has(channel.slug)
            return (
              <button
                key={channel.slug}
                type="button"
                onClick={() => router.push(`/channels/${channel.slug}`)}
                className="flex flex-col gap-3 rounded-xl border border-border p-4 text-left transition-colors hover:bg-muted/50 cursor-pointer"
              >
                <div className="flex items-start gap-3">
                  <Image src={channel.logo} alt={channel.name} width={32} height={32} className="shrink-0" />
                  <div className="min-w-0 flex-1">
                    <div className="font-medium text-sm">{channel.name}</div>
                    <div className="text-xs text-muted-foreground">{channel.protocol}</div>
                  </div>
                  {isConnected ? (
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
                <div className="flex flex-wrap gap-1.5">
                  {channel.capabilities.slice(0, 3).map((cap) => (
                    <span key={cap} className="rounded bg-muted px-1.5 py-0.5 text-[10px] text-muted-foreground">{cap}</span>
                  ))}
                </div>
              </button>
            )
          })}
        </div>

        {filtered.length === 0 && (
          <div className="py-8 text-center text-sm text-muted-foreground">No channels found.</div>
        )}
      </div>

      {/* Onboarding overlay */}
      {onboardingChannel && (
        <ChannelOnboardingDialog
          open={!!onboardingChannel}
          onOpenChange={(open) => { if (!open) setOnboardingChannel(null) }}
          channel={onboardingChannel}
          onComplete={() => {
            setConnectedSet((prev) => new Set([...prev, onboardingChannel.slug]))
            setOnboardingChannel(null)
          }}
        />
      )}
    </>
  )
}
