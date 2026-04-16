"use client"

import { use } from "react"
import Image from "next/image"
import { notFound } from "next/navigation"
import ReactMarkdown from "react-markdown"
import remarkGfm from "remark-gfm"
import { PageHeader } from "@/components/page-header"
import { Button } from "@/components/ui/button"
import { sourceUrl } from "@/data/integrations"
import { useIntegrations } from "@/hooks/useIntegrations"
import { ExternalLinkIcon, HeartIcon, DownloadIcon } from "lucide-react"

export default function IntegrationDetailPage({
  params,
}: {
  params: Promise<{ slug: string }>
}) {
  const { slug } = use(params)
  const { integrations, loading } = useIntegrations()
  const integration = integrations.find((i) => i.slug === slug)

  if (!integration) {
    if (loading) return null
    return notFound()
  }

  const source = sourceUrl(integration.slug)

  return (
    <>
      <PageHeader
        group="Integrations"
        page={integration.name}
        action={
          <Button size="sm">
            <DownloadIcon />
            Install
          </Button>
        }
      />
      <div className="flex flex-1 flex-col gap-4 p-4 pt-0">
        {/* Header */}
        <div className="flex items-start gap-4">
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
            </div>
            <p className="text-sm text-muted-foreground">{integration.description}</p>
            <div className="flex items-center gap-4 mt-2 text-xs text-muted-foreground">
              <span className="flex items-center gap-1">
                <HeartIcon className="size-3" />
                {integration.likes}
              </span>
              <span>{integration.tools.length} tools</span>
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

        {/* Tools card */}
        <div className="rounded-xl border border-border bg-card">
          <div className="px-4 py-3 border-b border-border">
            <span className="text-sm font-medium">Tools</span>
            <span className="ml-2 text-xs text-muted-foreground">{integration.tools.length}</span>
          </div>
          {integration.tools.map((tool, i) => (
            <div
              key={tool.name}
              className={`flex items-center gap-4 px-4 py-3 ${
                i < integration.tools.length - 1 ? "border-b border-border" : ""
              }`}
            >
              <code className="rounded bg-muted px-2 py-1 font-mono text-xs">{tool.name}</code>
              <span className="text-sm text-muted-foreground">{tool.description}</span>
            </div>
          ))}
        </div>

        {/* SKILL.md card */}
        <div className="rounded-xl border border-border bg-card">
          <div className="px-4 py-3 border-b border-border">
            <span className="text-sm font-medium">SKILL.md</span>
          </div>
          <div className="p-6">
            <div className="prose prose-sm max-w-none
              prose-headings:font-semibold prose-headings:text-foreground
              prose-h1:text-lg prose-h1:mt-0 prose-h1:mb-3
              prose-h2:text-sm prose-h2:mt-6 prose-h2:mb-3
              prose-h3:text-sm prose-h3:font-mono prose-h3:mt-4 prose-h3:mb-2 prose-h3:text-foreground
              prose-p:text-sm prose-p:leading-relaxed prose-p:text-muted-foreground
              prose-strong:text-foreground prose-strong:font-medium
              prose-code:rounded prose-code:bg-muted prose-code:px-1.5 prose-code:py-0.5 prose-code:text-xs prose-code:font-mono prose-code:text-foreground prose-code:before:content-none prose-code:after:content-none
              prose-pre:bg-zinc-950 prose-pre:text-zinc-300 prose-pre:rounded-lg prose-pre:text-xs prose-pre:leading-relaxed
              prose-table:text-sm prose-table:w-full
              prose-th:text-left prose-th:font-medium prose-th:text-muted-foreground prose-th:py-2 prose-th:px-3 prose-th:border-b prose-th:border-border
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
        </div>
      </div>
    </>
  )
}
