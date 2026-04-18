"use client"

import { use, useState } from "react"
import { notFound } from "next/navigation"
import ReactMarkdown from "react-markdown"
import remarkGfm from "remark-gfm"
import { cn } from "@/lib/utils"
import { PageHeader } from "@/components/page-header"
import { Button } from "@/components/ui/button"
import { Textarea } from "@/components/ui/textarea"
import {
  useIntegrations,
  useSkillComments,
  useLikeSkill,
  useCommentOnSkill,
  useDeleteSkillComment,
  useInstallSkill,
  useUninstallSkill,
} from "@/features/agents"
import {
  ExternalLinkIcon,
  HeartIcon,
  DownloadIcon,
  XIcon,
  SendIcon,
  Trash2Icon,
} from "lucide-react"

export default function IntegrationDetailPage({
  params,
}: {
  params: Promise<{ slug: string }>
}) {
  const { slug } = use(params)
  const { integrations, loading } = useIntegrations()
  const integration = integrations.find((i) => i.slug === slug)
  const likeSkill = useLikeSkill()
  const { commentOnSkill } = useCommentOnSkill()
  const deleteComment = useDeleteSkillComment()
  const installSkill = useInstallSkill()
  const uninstallSkill = useUninstallSkill()
  const { comments, refetch: refetchComments } = useSkillComments(integration?.id ?? "")
  const [commentBody, setCommentBody] = useState("")
  const [activeTab, setActiveTab] = useState<"overview" | "readme" | "changelog">("overview")

  if (!integration) {
    if (loading) return null
    return notFound()
  }

  async function handleLike() {
    if (!integration) return
    await likeSkill(integration.id, !integration.likedByMe)
  }

  async function handleComment() {
    if (!integration || !commentBody.trim()) return
    await commentOnSkill(integration.id, commentBody.trim())
    setCommentBody("")
    refetchComments()
  }

  async function handleDeleteComment(commentId: string) {
    await deleteComment(commentId)
    refetchComments()
  }

  return (
    <>
      <PageHeader
        group="Integrations"
        page={integration.name}
        action={
          integration.installed ? (
            <Button size="sm" variant="outline" onClick={() => uninstallSkill(integration.slug)}>
              <XIcon className="size-4" />
              Uninstall
            </Button>
          ) : (
            <Button size="sm" onClick={() => installSkill(integration.slug)}>
              <DownloadIcon className="size-4" />
              Install
            </Button>
          )
        }
      />
      <div className="flex flex-1 flex-col gap-4 p-4 pt-0">
        {/* Header */}
        <div className="flex items-start gap-4">
          <div className="size-12 shrink-0 rounded-xl [&>svg]:size-12" dangerouslySetInnerHTML={{ __html: integration.logo }} />
          <div className="flex-1 min-w-0">
            <div className="flex items-center gap-2 mb-1">
              <h1 className="text-lg font-semibold">{integration.name}</h1>
            </div>
            <p className="text-sm text-muted-foreground">{integration.description}</p>
            <div className="flex items-center gap-4 mt-2 text-xs text-muted-foreground">
              <button
                type="button"
                onClick={handleLike}
                className={`flex items-center gap-1 transition-colors hover:text-foreground ${integration.likedByMe ? "text-red-500" : ""}`}
              >
                <HeartIcon className={`size-3 ${integration.likedByMe ? "fill-current" : ""}`} />
                {integration.likes}
              </button>
              <span>{integration.tools.length} tools</span>
              <span>{integration.commentsCount} comments</span>
              <a
                href={integration.sourceCodeUrl}
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

        <div className="grid grid-cols-1 lg:grid-cols-[1fr_280px] gap-8">
          <div>
            {/* Tabs */}
            <div className="flex gap-4 border-b mb-6">
              {(["overview", "readme", "changelog"] as const).map((tab) => (
                <button
                  key={tab}
                  onClick={() => setActiveTab(tab)}
                  className={cn(
                    "pb-2 text-sm font-medium capitalize transition-colors border-b-2 -mb-px",
                    activeTab === tab
                      ? "border-primary text-foreground"
                      : "border-transparent text-muted-foreground hover:text-foreground"
                  )}
                >
                  {tab}
                </button>
              ))}
            </div>

            {/* Tab content */}
            {activeTab === "overview" && (
              <div className="space-y-4">
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

                {/* Documentation card */}
                {integration.doc && (
                  <div className="rounded-xl border border-border bg-card">
                    <div className="px-4 py-3 border-b border-border">
                      <span className="text-sm font-medium">Documentation</span>
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
                          {integration.doc}
                        </ReactMarkdown>
                      </div>
                    </div>
                  </div>
                )}

                {/* Comments section */}
                <div className="rounded-xl border border-border bg-card">
                  <div className="px-4 py-3 border-b border-border">
                    <span className="text-sm font-medium">Comments</span>
                    <span className="ml-2 text-xs text-muted-foreground">{comments.length}</span>
                  </div>
                  <div className="p-4 space-y-4">
                    <div className="flex gap-2">
                      <Textarea
                        placeholder="Write a comment..."
                        value={commentBody}
                        onChange={(e) => setCommentBody(e.target.value)}
                        className="min-h-[60px] text-sm"
                      />
                      <Button
                        size="sm"
                        onClick={handleComment}
                        disabled={!commentBody.trim()}
                        className="self-end"
                      >
                        <SendIcon className="size-4" />
                      </Button>
                    </div>

                    {comments.map((comment) => (
                      <div key={comment.id} className="flex gap-3 border-t border-border pt-3">
                        <div className="flex-1 min-w-0">
                          <div className="flex items-center gap-2 mb-1">
                            <span className="text-sm font-medium">{comment.author.name}</span>
                            <span className="text-xs text-muted-foreground">
                              {new Date(comment.createdAt).toLocaleDateString()}
                            </span>
                          </div>
                          <p className="text-sm text-muted-foreground whitespace-pre-wrap">{comment.body}</p>
                        </div>
                        <button
                          type="button"
                          onClick={() => handleDeleteComment(comment.id)}
                          className="text-muted-foreground hover:text-foreground transition-colors self-start"
                        >
                          <Trash2Icon className="size-3" />
                        </button>
                      </div>
                    ))}

                    {comments.length === 0 && (
                      <p className="text-sm text-muted-foreground text-center py-2">No comments yet.</p>
                    )}
                  </div>
                </div>
              </div>
            )}

            {activeTab === "readme" && (
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
                {integration.readme ? (
                  <ReactMarkdown remarkPlugins={[remarkGfm]}>
                    {integration.readme}
                  </ReactMarkdown>
                ) : (
                  <p className="text-sm text-muted-foreground">No README available.</p>
                )}
              </div>
            )}

            {activeTab === "changelog" && (
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
                {integration.changelog ? (
                  <ReactMarkdown remarkPlugins={[remarkGfm]}>
                    {integration.changelog}
                  </ReactMarkdown>
                ) : (
                  <p className="text-sm text-muted-foreground">No changelog available.</p>
                )}
              </div>
            )}
          </div>

          {/* Metadata sidebar */}
          <aside className="space-y-6 text-sm">
            {integration.version && (
              <div>
                <h4 className="font-medium text-muted-foreground mb-1">Version</h4>
                <p>{integration.version}</p>
              </div>
            )}
            {integration.license && (
              <div>
                <h4 className="font-medium text-muted-foreground mb-1">License</h4>
                <p>{integration.license}</p>
              </div>
            )}
            {integration.author && (
              <div>
                <h4 className="font-medium text-muted-foreground mb-1">Author</h4>
                <p>
                  {integration.author.url ? (
                    <a href={integration.author.url} target="_blank" rel="noopener noreferrer" className="text-primary hover:underline">
                      {integration.author.name}
                    </a>
                  ) : integration.author.name}
                </p>
              </div>
            )}
            {integration.repository && (
              <div>
                <h4 className="font-medium text-muted-foreground mb-1">Repository</h4>
                <a href={integration.repository} target="_blank" rel="noopener noreferrer" className="text-primary hover:underline break-all">
                  Source Code
                </a>
              </div>
            )}
            {integration.categories.length > 0 && (
              <div>
                <h4 className="font-medium text-muted-foreground mb-1">Categories</h4>
                <div className="flex flex-wrap gap-1">
                  {integration.categories.map((cat) => (
                    <span key={cat} className="inline-flex items-center rounded-md bg-muted px-2 py-0.5 text-xs">
                      {cat}
                    </span>
                  ))}
                </div>
              </div>
            )}
            {integration.contributors.length > 0 && (
              <div>
                <h4 className="font-medium text-muted-foreground mb-1">Contributors</h4>
                <div className="space-y-1">
                  {integration.contributors.map((c) => (
                    <div key={c.name}>
                      {c.url ? (
                        <a href={c.url} target="_blank" rel="noopener noreferrer" className="text-primary hover:underline text-xs">
                          {c.name}
                        </a>
                      ) : <span className="text-xs">{c.name}</span>}
                    </div>
                  ))}
                </div>
              </div>
            )}
          </aside>
        </div>
      </div>
    </>
  )
}
