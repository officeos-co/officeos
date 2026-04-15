"use client"

import { use } from "react"
import Image from "next/image"
import { notFound } from "next/navigation"
import ReactMarkdown from "react-markdown"
import remarkGfm from "remark-gfm"
import { PageHeader } from "@/components/page-header"
import { Button } from "@/components/ui/button"
import { Separator } from "@/components/ui/separator"
import { integrations, sourceUrl } from "@/data/integrations"
import { ExternalLinkIcon, HeartIcon, DownloadIcon } from "lucide-react"

export default function IntegrationDetailPage({
  params,
}: {
  params: Promise<{ slug: string }>
}) {
  const { slug } = use(params)
  const integration = integrations.find((i) => i.slug === slug)

  if (!integration) return notFound()

  const source = sourceUrl(integration.slug)

  return (
    <>
      <PageHeader
        group="Integrations"
        page={integration.name}
        action={
          <div className="flex items-center gap-2">
            <Button size="sm" variant="outline" render={
              <a href={source} target="_blank" rel="noopener noreferrer" />
            }>
              <ExternalLinkIcon />
              Source
            </Button>
            <Button size="sm">
              <DownloadIcon />
              Install
            </Button>
          </div>
        }
      />
      <div className="flex flex-1 flex-col p-4 pt-0">
        {/* Header */}
        <div className="flex items-start gap-4 mb-6">
          <Image
            src={integration.logo}
            alt={integration.name}
            width={48}
            height={48}
            className="shrink-0 rounded-xl"
          />
          <div className="flex-1 min-w-0">
            <div className="flex items-center gap-2 mb-1">
              <h1 className="text-lg font-semibold">{integration.name}</h1>
              <span className={`rounded-full px-2 py-0.5 text-[10px] font-medium uppercase tracking-wider ${
                integration.type === "channel"
                  ? "bg-blue-100 text-blue-700"
                  : "bg-zinc-100 text-zinc-600"
              }`}>
                {integration.type}
              </span>
            </div>
            <p className="text-sm text-muted-foreground">{integration.description}</p>
            <div className="flex items-center gap-4 mt-2 text-xs text-muted-foreground">
              <span className="flex items-center gap-1">
                <HeartIcon className="size-3" />
                {integration.likes}
              </span>
              <span>Updated {integration.updatedAgo}</span>
              <a
                href={source}
                target="_blank"
                rel="noopener noreferrer"
                className="flex items-center gap-1 hover:text-foreground transition-colors"
              >
                <ExternalLinkIcon className="size-3" />
                Source
              </a>
            </div>
          </div>
        </div>

        <Separator className="mb-6" />

        {/* SKILL.md */}
        <div className="prose prose-sm prose-zinc max-w-none
          prose-headings:font-semibold
          prose-h1:text-xl prose-h1:mt-0 prose-h1:mb-3
          prose-h2:text-base prose-h2:mt-8 prose-h2:mb-3
          prose-h3:text-sm prose-h3:font-mono prose-h3:mt-5 prose-h3:mb-2
          prose-p:text-sm prose-p:leading-relaxed prose-p:text-muted-foreground
          prose-strong:text-foreground
          prose-code:rounded prose-code:bg-muted prose-code:px-1.5 prose-code:py-0.5 prose-code:text-xs prose-code:font-mono prose-code:before:content-none prose-code:after:content-none
          prose-pre:bg-zinc-950 prose-pre:text-zinc-100 prose-pre:rounded-lg prose-pre:text-xs prose-pre:leading-relaxed
          prose-table:text-sm prose-table:w-full
          prose-th:text-left prose-th:font-medium prose-th:py-2 prose-th:px-3 prose-th:border-b prose-th:border-border
          prose-td:py-2 prose-td:px-3 prose-td:border-b prose-td:border-border prose-td:text-muted-foreground
          prose-li:text-sm prose-li:text-muted-foreground
          prose-ol:text-sm
          prose-a:text-foreground prose-a:underline prose-a:underline-offset-2
        ">
          <ReactMarkdown remarkPlugins={[remarkGfm]}>
            {integration.skillMd}
          </ReactMarkdown>
        </div>
      </div>
    </>
  )
}
