"use client";

import { use, useState } from "react";
import { notFound, useSearchParams } from "next/navigation";
import Link from "next/link";
import { cn } from "@/lib/utils";
import { PageHeader } from "@/components/page-header";
import { Button } from "@/components/ui/button";
import {
  useIntegration,
  useSkillComments,
  useLikeSkill,
  useCommentOnSkill,
  useDeleteSkillComment,
  useInstallSkill,
  useUninstallSkill,
  useSetSkillCredentials,
  CredentialDialog,
  IntegrationDetailTab,
  IntegrationReadmeTab,
  IntegrationChangelogTab,
  IntegrationCommentsTab,
} from "@/features/agents";
import { useAnalytics } from "@/features/analytics";
import {
  ExternalLinkIcon,
  HeartIcon,
  DownloadIcon,
  XIcon,
  BookOpenIcon,
  FileTextIcon,
  HistoryIcon,
  MessageSquareIcon,
  KeyIcon,
  CheckCircle2Icon,
  AlertCircleIcon,
} from "lucide-react";

/* ── Tab config (URL-driven, like agent detail) ──────────────── */

const TABS = [
  { key: "detail", label: "Detail", icon: BookOpenIcon },
  { key: "readme", label: "README", icon: FileTextIcon },
  { key: "changelog", label: "Changelog", icon: HistoryIcon },
  { key: "comments", label: "Comments", icon: MessageSquareIcon },
] as const;
type TabKey = (typeof TABS)[number]["key"];

/* ── Page ─────────────────────────────────────────────────────── */

export default function IntegrationDetailPage({
  params,
}: {
  params: Promise<{ slug: string }>;
}) {
  const { slug } = use(params);
  const searchParams = useSearchParams();
  const tab = (searchParams.get("tab") as TabKey) ?? "detail";

  const { integration, loading } = useIntegration(slug);
  const likeSkill = useLikeSkill();
  const { commentOnSkill } = useCommentOnSkill();
  const deleteComment = useDeleteSkillComment();
  const installSkill = useInstallSkill();
  const uninstallSkill = useUninstallSkill();
  const setCredentials = useSetSkillCredentials();
  const { trackSkillConfigured } = useAnalytics();
  const { comments, refetch: refetchComments } = useSkillComments(
    integration?.id ?? "",
  );
  const [credDialogOpen, setCredDialogOpen] = useState(false);

  if (!integration) {
    if (loading) return null;
    return notFound();
  }

  async function handleLike() {
    if (!integration) return;
    await likeSkill(integration.id, !integration.likedByMe);
  }

  async function handleComment(body: string) {
    if (!integration) return;
    await commentOnSkill(integration.id, body);
    refetchComments();
  }

  async function handleDeleteComment(commentId: string) {
    await deleteComment(commentId);
    refetchComments();
  }

  async function handleSaveCredentials(values: Record<string, string>) {
    if (!integration) return;
    await setCredentials(integration.slug, values);
    trackSkillConfigured(integration.slug);
  }

  const hasCredentialFields = integration.credentialFields.length > 0;
  const issuesUrl = integration.repository
    ? `${integration.repository}/issues`
    : null;

  return (
    <>
      <PageHeader
        group="Integrations"
        page={integration.name}
        action={
          <div className="flex items-center gap-2">
            {integration.installed && hasCredentialFields && (
              <Button
                size="sm"
                variant={integration.configured ? "outline" : "default"}
                onClick={() => setCredDialogOpen(true)}
              >
                <KeyIcon className="size-4" />
                {integration.configured ? "Reconfigure" : "Configure"}
              </Button>
            )}
            {integration.installed ? (
              <Button
                size="sm"
                variant="outline"
                onClick={() => uninstallSkill(integration.slug)}
              >
                <XIcon className="size-4" />
                Uninstall
              </Button>
            ) : (
              <Button size="sm" onClick={() => installSkill(integration.slug)}>
                <DownloadIcon className="size-4" />
                Install
              </Button>
            )}
          </div>
        }
      />

      <div className="flex flex-1 flex-col p-4 pt-0">
        {/* Header */}
        <div className="flex items-start gap-4 mb-4">
          <div
            className="size-12 shrink-0 rounded-xl [&>svg]:size-12"
            dangerouslySetInnerHTML={{ __html: integration.logo }}
          />
          <div className="flex-1 min-w-0">
            <div className="flex items-center gap-2 mb-1">
              <h1 className="text-lg font-semibold">{integration.name}</h1>
              {integration.author && (
                <span className="text-xs text-muted-foreground">
                  by{" "}
                  {integration.author.url ? (
                    <a
                      href={integration.author.url}
                      target="_blank"
                      rel="noopener noreferrer"
                      className="hover:underline"
                    >
                      {integration.author.name}
                    </a>
                  ) : (
                    integration.author.name
                  )}
                </span>
              )}
            </div>
            <p className="text-sm text-muted-foreground">
              {integration.description}
            </p>
            {integration.installed && hasCredentialFields && (
              <div
                className={cn(
                  "inline-flex items-center gap-1.5 rounded-full px-2.5 py-1 text-[10px] font-semibold uppercase tracking-widest mt-2",
                  integration.configured
                    ? "bg-emerald-100 text-emerald-700"
                    : "bg-amber-100 text-amber-700",
                )}
              >
                {integration.configured ? (
                  <CheckCircle2Icon className="size-3" />
                ) : (
                  <AlertCircleIcon className="size-3" />
                )}
                {integration.configured ? "Configured" : "Credentials required"}
              </div>
            )}
            <div className="flex items-center gap-4 mt-2 text-xs text-muted-foreground">
              <button
                type="button"
                onClick={handleLike}
                className={cn(
                  "flex items-center gap-1 transition-colors hover:text-foreground",
                  integration.likedByMe && "text-red-500",
                )}
              >
                <HeartIcon
                  className={cn(
                    "size-3",
                    integration.likedByMe && "fill-current",
                  )}
                />
                {integration.likes}
              </button>
              <span>{integration.tools.length} tools</span>
              <span>{comments.length} comments</span>
            </div>
          </div>
        </div>

        {/* Tab bar */}
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
                    : "border-transparent text-muted-foreground hover:text-foreground",
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
          {/* Main content */}
          <div className="min-w-0">
            {tab === "detail" && (
              <IntegrationDetailTab
                tools={integration.tools}
                doc={integration.doc}
              />
            )}
            {tab === "readme" && (
              <IntegrationReadmeTab readme={integration.readme} />
            )}
            {tab === "changelog" && (
              <IntegrationChangelogTab changelog={integration.changelog} />
            )}
            {tab === "comments" && (
              <IntegrationCommentsTab
                comments={comments}
                onComment={handleComment}
                onDeleteComment={handleDeleteComment}
              />
            )}
          </div>

          {/* Sticky sidebar */}
          <aside className="hidden lg:block">
            <div className="sticky top-14 space-y-6 text-sm">
              {/* Resources */}
              <div>
                <h4 className="text-xs font-semibold uppercase tracking-wider text-muted-foreground mb-3">
                  Resources
                </h4>
                <div className="space-y-2">
                  {integration.repository && (
                    <a
                      href={integration.repository}
                      target="_blank"
                      rel="noopener noreferrer"
                      className="flex items-center gap-2 text-sm hover:text-foreground text-muted-foreground transition-colors"
                    >
                      <ExternalLinkIcon className="size-3.5" />
                      Repository
                    </a>
                  )}
                  {issuesUrl && (
                    <a
                      href={issuesUrl}
                      target="_blank"
                      rel="noopener noreferrer"
                      className="flex items-center gap-2 text-sm hover:text-foreground text-muted-foreground transition-colors"
                    >
                      <ExternalLinkIcon className="size-3.5" />
                      Issues
                    </a>
                  )}
                  {integration.license && (
                    <a
                      href={
                        integration.repository
                          ? `${integration.repository}/blob/main/LICENSE`
                          : "#"
                      }
                      target="_blank"
                      rel="noopener noreferrer"
                      className="flex items-center gap-2 text-sm hover:text-foreground text-muted-foreground transition-colors"
                    >
                      <ExternalLinkIcon className="size-3.5" />
                      License ({integration.license})
                    </a>
                  )}
                </div>
              </div>

              {/* Categories */}
              {integration.categories.length > 0 && (
                <div>
                  <h4 className="text-xs font-semibold uppercase tracking-wider text-muted-foreground mb-3">
                    Categories
                  </h4>
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
                <h4 className="text-xs font-semibold uppercase tracking-wider text-muted-foreground mb-3">
                  Marketplace
                </h4>
                <dl className="space-y-2.5">
                  <div className="flex justify-between">
                    <dt className="text-muted-foreground">Version</dt>
                    <dd className="font-mono text-xs">{integration.version}</dd>
                  </div>
                  {integration.createdAt && (
                    <div className="flex justify-between">
                      <dt className="text-muted-foreground">Published</dt>
                      <dd>
                        {new Date(integration.createdAt).toLocaleDateString()}
                      </dd>
                    </div>
                  )}
                  {integration.updatedAt && (
                    <div className="flex justify-between">
                      <dt className="text-muted-foreground">Last updated</dt>
                      <dd>
                        {new Date(integration.updatedAt).toLocaleDateString()}
                      </dd>
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
                  <h4 className="text-xs font-semibold uppercase tracking-wider text-muted-foreground mb-3">
                    Contributors
                  </h4>
                  <div className="space-y-2">
                    {integration.contributors.map((c) => (
                      <div key={c.name}>
                        {c.url ? (
                          <a
                            href={c.url}
                            target="_blank"
                            rel="noopener noreferrer"
                            className="text-sm hover:underline text-muted-foreground hover:text-foreground transition-colors"
                          >
                            {c.name}
                          </a>
                        ) : (
                          <span className="text-sm text-muted-foreground">
                            {c.name}
                          </span>
                        )}
                      </div>
                    ))}
                  </div>
                </div>
              )}
            </div>
          </aside>
        </div>
      </div>

      {hasCredentialFields && (
        <CredentialDialog
          open={credDialogOpen}
          onOpenChange={setCredDialogOpen}
          name={integration.name}
          logo={integration.logo}
          credentials={integration.credentialFields}
          onSave={handleSaveCredentials}
        />
      )}
    </>
  );
}
