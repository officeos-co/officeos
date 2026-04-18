"use client"

import { use } from "react"
import { notFound } from "next/navigation"
import { PageHeader } from "@/components/page-header"
import { useChannels } from "@/features/agents"
import { RadioIcon } from "lucide-react"

export default function ChannelDetailPage({
  params,
}: {
  params: Promise<{ slug: string }>
}) {
  const { slug } = use(params)
  const { channels, loading } = useChannels()
  const channel = channels.find((c) => c.slug === slug)

  if (!channel) {
    if (loading) return null
    return notFound()
  }

  return (
    <>
      <PageHeader group="Channels" page={channel.name} />
      <div className="flex flex-1 flex-col gap-4 p-4 pt-0 max-w-4xl">
        {/* Header */}
        <div className="flex items-start gap-4">
          <div className="size-12 shrink-0 rounded-xl [&>svg]:size-12" dangerouslySetInnerHTML={{ __html: channel.logo }} />
          <div className="flex-1 min-w-0">
            <div className="flex items-center gap-2 mb-1">
              <h1 className="text-lg font-semibold">{channel.name}</h1>
              <span className="rounded-full bg-blue-100 px-2 py-0.5 text-[10px] font-medium uppercase tracking-wider text-blue-700">channel</span>
            </div>
            <p className="text-sm text-muted-foreground">{channel.description}</p>
            {channel.added && (
              <div className="mt-2">
                <span className="flex items-center gap-1 text-xs text-emerald-600">
                  <RadioIcon className="size-3" /> Connected
                </span>
              </div>
            )}
          </div>
        </div>

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
    </>
  )
}
