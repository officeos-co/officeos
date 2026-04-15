"use client"

import { use } from "react"
import Image from "next/image"
import { notFound } from "next/navigation"
import ReactMarkdown from "react-markdown"
import remarkGfm from "remark-gfm"
import { PageHeader } from "@/components/page-header"
import { Button } from "@/components/ui/button"
import { channels, channelSourceUrl } from "@/data/channels"
import { ExternalLinkIcon, HeartIcon, RadioIcon } from "lucide-react"

export default function ChannelDetailPage({
  params,
}: {
  params: Promise<{ slug: string }>
}) {
  const { slug } = use(params)
  const channel = channels.find((c) => c.slug === slug)

  if (!channel) return notFound()

  const source = channelSourceUrl(channel.slug)

  return (
    <>
      <PageHeader group="Channels" page={channel.name} />
      <div className="flex flex-1 flex-col gap-4 p-4 pt-0 max-w-4xl">
        {/* Header */}
        <div className="flex items-start gap-4">
          <Image src={channel.logo} alt={channel.name} width={48} height={48} className="shrink-0 rounded-xl" />
          <div className="flex-1 min-w-0">
            <div className="flex items-center gap-2 mb-1">
              <h1 className="text-lg font-semibold">{channel.name}</h1>
              <span className="rounded-full bg-blue-100 px-2 py-0.5 text-[10px] font-medium uppercase tracking-wider text-blue-700">channel</span>
            </div>
            <p className="text-sm text-muted-foreground">{channel.description}</p>
            <div className="flex items-center gap-4 mt-2 text-xs text-muted-foreground">
              <span className="flex items-center gap-1"><HeartIcon className="size-3" />{channel.likes}</span>
              <span>{channel.protocol}</span>
              <a href={source} target="_blank" rel="noopener noreferrer" className="flex items-center gap-1 hover:text-foreground transition-colors">
                <ExternalLinkIcon className="size-3" /> Source
              </a>
            </div>
          </div>
        </div>

        {/* Capabilities */}
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

        {/* Docs */}
        <div className="rounded-xl border border-border bg-card">
          <div className="px-4 py-3 border-b border-border">
            <span className="text-sm font-medium">Documentation</span>
          </div>
          <div className="p-6 prose prose-sm max-w-none
            prose-headings:font-semibold prose-headings:text-foreground
            prose-h1:text-lg prose-h1:mt-0 prose-h1:mb-3
            prose-h2:text-sm prose-h2:mt-6 prose-h2:mb-3
            prose-p:text-sm prose-p:leading-relaxed prose-p:text-muted-foreground
            prose-strong:text-foreground prose-strong:font-medium
            prose-code:rounded prose-code:bg-muted prose-code:px-1.5 prose-code:py-0.5 prose-code:text-xs prose-code:font-mono prose-code:text-foreground prose-code:before:content-none prose-code:after:content-none
            prose-table:text-sm prose-table:w-full
            prose-th:text-left prose-th:font-medium prose-th:text-muted-foreground prose-th:py-2 prose-th:px-3 prose-th:border-b prose-th:border-border
            prose-td:py-2 prose-td:px-3 prose-td:border-b prose-td:border-border prose-td:text-muted-foreground
            prose-li:text-sm prose-li:text-muted-foreground
          ">
            <ReactMarkdown remarkPlugins={[remarkGfm]}>
              {channel.skillMd}
            </ReactMarkdown>
          </div>
        </div>
      </div>
    </>
  )
}
