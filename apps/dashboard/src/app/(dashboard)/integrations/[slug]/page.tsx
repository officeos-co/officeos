"use client"

import { use, useState } from "react"
import { notFound, useSearchParams } from "next/navigation"
import Link from "next/link"
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
  BookOpenIcon,
  FileTextIcon,
  HistoryIcon,
  MessageSquareIcon,
} from "lucide-react"

/* ── Tab config (URL-driven, like agent detail) ──────────────── */

const TABS = [
  { key: "detail", label: "Detail", icon: BookOpenIcon },
  { key: "readme", label: "README", icon: FileTextIcon },
  { key: "changelog", label: "Changelog", icon: HistoryIcon },
  { key: "comments", label: "Comments", icon: MessageSquareIcon },
] as const
type TabKey = (typeof TABS)[number]["key"]

/* ── Prose classes for markdown rendering ─────────────────────── */

const proseClasses = `prose prose-sm max-w-none
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
  prose-a:text-foreground prose-a:underline prose-a:underline-offset-2`

/* ── Page ─────────────────────────────────────────────────────── */

export default function IntegrationDetailPage({
  params,
}: {
  params: Promise<{ slug: string }>
}) {
  const { slug } = use(params)
  const searchParams = useSearchParams()
  const tab = (searchParams.get("tab") as TabKey) ?? "detail"

  const { integrations, loading } = useIntegrations()
  const integration = integrations.find((i) => i.slug === slug)
  const likeSkill = useLikeSkill()
  const { commentOnSkill } = useCommentOnSkill()
  const deleteComment = useDeleteSkillComment()
  const installSkill = useInstallSkill()
  const uninstallSkill = useUninstallSkill()
  const { comments, refetch: refetchComments } = useSkillComments(integration?.id ?? "")
  const [commentBody, setCommentBody] = useState("")

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

  const issuesUrl = integration.repository ? `${integration.repository}/issues` : null

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

      <div className="flex flex-1 flex-col p-4 pt-0">
        {/* Header */}
        <div className="flex items-start gap-4 mb-4">
          <div className="size-12 shrink-0 rounded-xl [&>svg]:size-12" dangerouslySetInnerHTML={{ __html: integration.logo }} />
          <div className="flex-1 min-w-0">
            <div className="flex items-center gap-2 mb-1">
              <h1 className="text-lg font-semibold">{integration.name}</h1>
              {integration.author && (
                <span className="text-xs text-muted-foreground">
                  by{" "}
                  {integration.author.url ? (
                    <a href={integration.author.url} target="_blank" rel="noopener noreferrer" className="hover:underline">
                      {integration.author.name}
                    </a>
                  ) : integration.author.name}
                </span>
              )}
            </div>
            <p className="text-sm text-muted-foreground">{integration.description}</p>
            <div className="flex items-center gap-4 mt-2 text-xs text-muted-foreground">
              <button
                type="button"
                onClick={handleLike}
                className={cn("flex items-center gap-1 transition-colors hover:text-foreground", integration.likedByMe && "text-red-500")}
              >
                <HeartIcon className={cn("size-3", integration.likedByMe && "fill-current")} />
                {integration.likes}
              </button>
              <span>{integration.tools.length} tools</span>
              <span>{comments.length} comments</span>
            </div>
          </div>
        </div>

        {/* Tab bar — sticky */}
        <div className="sticky top-0 z-10 bg-background border-b -mx-4 px-4 mb-6">
          <div className="flex -mb-px">
            {TABS.map((t) => (
              <Link
                key={t.key}
                href={`/integrations/${slug}?tab=${t.key}`}
                className={cn(
                  "flex items-center gap-1.5 px-4 py-2.5 text-sm font-medium border-b-2 transition-colors",
                  tab === t.key
                    ? "border-foreground text-foreground"
                    : "border-transparent text-muted-foreground hover:text-foreground"
                )}
              >
                <t.icon className="size-3.5" />
                {t.label}
              </Link>
            ))}
          </div>
        </div>

        {/* Content: main + sticky sidebar */}
        <div className="grid grid-cols-1 lg:grid-cols-[1fr_300px] gap-8 max-w-6xl mx-auto w-full">
          {/* ── Main content ──────────────────────────────────── */}
          <div className="min-w-0">
            {tab === "detail" && (
              <div className="space-y-6">
                {/* Tools card */}
                <div className="rounded-xl border border-border bg-card">
                  <div className="px-4 py-3 border-b border-border">
                    <span className="text-sm font-medium">Tools</span>
                    <span className="ml-2 text-xs text-muted-foreground">{integration.tools.length}</span>
                  </div>
                  {integration.tools.map((tool, i) => (
                    <div
                      key={tool.name}
                      className={cn(
                        "flex items-center gap-4 px-4 py-3",
                        i < integration.tools.length - 1 && "border-b border-border"
                      )}
                    >
                      <code className="rounded bg-muted px-2 py-1 font-mono text-xs">{tool.name}</code>
                      <span className="text-sm text-muted-foreground">{tool.description}</span>
                    </div>
                  ))}
                </div>

                {/* Documentation */}
                {integration.doc && (
                  <div className="rounded-xl border border-border bg-card">
                    <div className="px-4 py-3 border-b border-border">
                      <span className="text-sm font-medium">Documentation</span>
                    </div>
                    <div className="p-6">
                      <div className={proseClasses}>
                        <ReactMarkdown remarkPlugins={[remarkGfm]}>
                          {integration.doc}
                        </ReactMarkdown>
                      </div>
                    </div>
                  </div>
                )}
              </div>
            )}

            {tab === "readme" && (
              <div className={proseClasses}>
                {integration.readme ? (
                  <ReactMarkdown remarkPlugins={[remarkGfm]}>{integration.readme}</ReactMarkdown>
                ) : (
                  <p className="text-sm text-muted-foreground">No README available.</p>
                )}
              </div>
            )}

            {tab === "changelog" && (
              <div className={proseClasses}>
                {integration.changelog ? (
                  <ReactMarkdown remarkPlugins={[remarkGfm]}>{integration.changelog}</ReactMarkdown>
                ) : (
                  <p className="text-sm text-muted-foreground">No changelog available.</p>
                )}
              </div>
            )}

            {tab === "comments" && (
              <div className="space-y-4">
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
                  <div key={comment.id} className="flex gap-3 rounded-xl border border-border bg-card p-4">
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
                  <p className="text-sm text-muted-foreground text-center py-8">No comments yet. Be the first to share your thoughts.</p>
                )}
              </div>
            )}
          </div>

          {/* ── Sticky sidebar ────────────────────────────────── */}
          <aside className="hidden lg:block">
            <div className="sticky top-14 space-y-6 text-sm">
              {/* Resources */}
              <div>
                <h4 className="text-xs font-semibold uppercase tracking-wider text-muted-foreground mb-3">Resources</h4>
                <div className="space-y-2">
                  {integration.repository && (
                    <a href={integration.repository} target="_blank" rel="noopener noreferrer" className="flex items-center gap-2 text-sm hover:text-foreground text-muted-foreground transition-colors">
                      <ExternalLinkIcon className="size-3.5" />
                      Repository
                    </a>
                  )}
                  {issuesUrl && (
                    <a href={issuesUrl} target="_blank" rel="noopener noreferrer" className="flex items-center gap-2 text-sm hover:text-foreground text-muted-foreground transition-colors">
                      <ExternalLinkIcon className="size-3.5" />
                      Issues
                    </a>
                  )}
                  {integration.license && (
                    <a href={integration.repository ? `${integration.repository}/blob/main/LICENSE` : "#"} target="_blank" rel="noopener noreferrer" className="flex items-center gap-2 text-sm hover:text-foreground text-muted-foreground transition-colors">
                      <ExternalLinkIcon className="size-3.5" />
                      License ({integration.license})
                    </a>
                  )}
                </div>
              </div>

              {/* Categories */}
              {integration.categories.length > 0 && (
                <div>
                  <h4 className="text-xs font-semibold uppercase tracking-wider text-muted-foreground mb-3">Categories</h4>
                  <div className="flex flex-wrap gap-1.5">
                    {integration.categories.map((cat) => (
                      <Link
                        key={cat}
                        href={`/integrations?category=${encodeURIComponent(cat)}`}
                        className="inline-flex items-center rounded-md bg-muted px-2.5 py-1 text-xs text-muted-foreground hover:text-foreground transition-colors"
                      >
                        {cat}
                      </Link>
                    ))}
                  </div>
                </div>
              )}

              {/* Marketplace details */}
              <div>
                <h4 className="text-xs font-semibold uppercase tracking-wider text-muted-foreground mb-3">Marketplace</h4>
                <dl className="space-y-2.5">
                  <div className="flex justify-between">
                    <dt className="text-muted-foreground">Version</dt>
                    <dd className="font-mono text-xs">{integration.version}</dd>
                  </div>
                  {integration.createdAt && (
                    <div className="flex justify-between">
                      <dt className="text-muted-foreground">Published</dt>
                      <dd>{new Date(integration.createdAt).toLocaleDateString()}</dd>
                    </div>
                  )}
                  {integration.updatedAt && (
                    <div className="flex justify-between">
                      <dt className="text-muted-foreground">Last updated</dt>
                      <dd>{new Date(integration.updatedAt).toLocaleDateString()}</dd>
                    </div>
                  )}
                  <div className="flex justify-between">
                    <dt className="text-muted-foreground">Identifier</dt>
                    <dd className="font-mono text-xs">{integration.slug}</dd>
                  </div>
                  <div className="flex justify-between">
                    <dt className="text-muted-foreground">Tools</dt>
                    <dd>{integration.tools.length}</dd>
                  </div>
                </dl>
              </div>

              {/* Contributors */}
              {integration.contributors.length > 0 && (
                <div>
                  <h4 className="text-xs font-semibold uppercase tracking-wider text-muted-foreground mb-3">Contributors</h4>
                  <div className="space-y-2">
                    {integration.contributors.map((c) => (
                      <div key={c.name}>
                        {c.url ? (
                          <a href={c.url} target="_blank" rel="noopener noreferrer" className="text-sm hover:underline text-muted-foreground hover:text-foreground transition-colors">
                            {c.name}
                          </a>
                        ) : <span className="text-sm text-muted-foreground">{c.name}</span>}
                      </div>
                    ))}
                  </div>
                </div>
              )}
            </div>
          </aside>
        </div>
      </div>
    </>
  )
}
