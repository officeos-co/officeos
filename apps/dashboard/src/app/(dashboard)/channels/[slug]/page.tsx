"use client"

import { use, useState } from "react"
import { notFound } from "next/navigation"
import { PageHeader } from "@/components/page-header"
import { ChannelOnboardingDialog } from "@/features/agents"
import { useChannels, useDeleteChannelConnection } from "@/features/agents"
import { useAnalytics } from "@/features/analytics"
import { Button } from "@/components/ui/button"
import { RadioIcon, PlusIcon, TrashIcon, MessageSquareIcon, SendIcon, BellIcon } from "lucide-react"

export default function ChannelDetailPage({
  params,
}: {
  params: Promise<{ slug: string }>
}) {
  const { slug } = use(params)
  const { channels, loading } = useChannels()
  const { deleteChannelConnection } = useDeleteChannelConnection()
  const { trackChannelConnected } = useAnalytics()
  const [onboarding, setOnboarding] = useState(false)
  const channel = channels.find((c) => c.slug === slug)

  if (!channel) {
    if (loading) return null
    return notFound()
  }

  return (
    <>
      <PageHeader group="Channels" page={channel.name} />
      <div className="flex flex-1 flex-col gap-6 p-4 pt-0 max-w-4xl mx-auto w-full">
        {/* Header */}
        <div className="flex items-start gap-4">
          <div className="size-12 shrink-0 rounded-xl [&>svg]:size-12" dangerouslySetInnerHTML={{ __html: channel.logo }} />
          <div className="flex-1 min-w-0">
            <div className="flex items-center gap-2 mb-1">
              <h1 className="text-lg font-semibold">{channel.name}</h1>
              <span className="rounded-full bg-blue-100 px-2 py-0.5 text-[10px] font-medium uppercase tracking-wider text-blue-700">channel</span>
            </div>
            <p className="text-sm text-muted-foreground">{channel.description}</p>
            <div className="mt-3">
              {channel.added ? (
                <div className="flex items-center gap-3">
                  <span className="flex items-center gap-1 rounded-full bg-emerald-100 px-2.5 py-1 text-[10px] font-semibold uppercase tracking-widest text-emerald-700">
                    <RadioIcon className="size-3" /> Connected
                  </span>
                  <Button size="sm" variant="ghost" className="h-7 text-xs text-destructive hover:text-destructive" onClick={() => deleteChannelConnection(slug)}>
                    <TrashIcon className="size-3" /> Disconnect
                  </Button>
                </div>
              ) : (
                <Button size="sm" onClick={() => setOnboarding(true)}>
                  <PlusIcon className="size-3.5" /> Connect {channel.name}
                </Button>
              )}
            </div>
          </div>
        </div>

        {/* Default permissions */}
        <div className="rounded-xl border border-border bg-card">
          <div className="px-4 py-3 border-b border-border">
            <span className="text-sm font-medium">Default Permissions</span>
          </div>
          {([
            { key: "receive", label: "Receive messages", icon: BellIcon, desc: "Agent receives incoming messages from this channel" },
            { key: "send", label: "Send messages", icon: SendIcon, desc: "Agent can send replies through this channel" },
            { key: "initiate", label: "Initiate conversations", icon: MessageSquareIcon, desc: "Agent can start new conversations proactively" },
          ] as const).map(({ key, label, icon: Icon, desc }, i) => (
            <div key={key} className={`flex items-center gap-3 px-4 py-3 ${i < 2 ? "border-b border-border" : ""}`}>
              <Icon className="size-4 text-muted-foreground shrink-0" />
              <div className="flex-1 min-w-0">
                <div className="text-sm font-medium">{label}</div>
                <div className="text-xs text-muted-foreground">{desc}</div>
              </div>
              <span className={`rounded-full px-2 py-0.5 text-[10px] font-semibold uppercase tracking-widest ${
                channel.defaultPermissions[key] === "allow" ? "bg-emerald-100 text-emerald-700" :
                channel.defaultPermissions[key] === "deny" ? "bg-red-100 text-red-700" :
                "bg-amber-100 text-amber-700"
              }`}>
                {channel.defaultPermissions[key]}
              </span>
            </div>
          ))}
        </div>

        {/* Capabilities */}
        {channel.capabilities.length > 0 && (
          <div className="rounded-xl border border-border bg-card">
            <div className="px-4 py-3 border-b border-border">
              <span className="text-sm font-medium">Capabilities</span>
            </div>
            {channel.capabilities.map((cap, i) => (
              <div key={i} className={`flex items-center gap-2 px-4 py-2.5 ${i < channel.capabilities.length - 1 ? "border-b border-border" : ""}`}>
                <RadioIcon className="size-3.5 text-muted-foreground" />
                <span className="text-sm">{cap}</span>
              </div>
            ))}
          </div>
        )}
      </div>

      {onboarding && (
        <ChannelOnboardingDialog
          open={onboarding}
          onOpenChange={(open) => { if (!open) setOnboarding(false) }}
          channel={channel}
          onComplete={() => {
            trackChannelConnected(channel.slug)
            setOnboarding(false)
          }}
        />
      )}
    </>
  )
}
