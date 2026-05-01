"use client";

import { use, useState } from "react";
import { notFound } from "next/navigation";
import { cn } from "@/lib/utils";
import { PageHeader } from "@/components/page-header";
import { Button } from "@/components/ui/button";
import { Skeleton } from "@/components/ui/skeleton";
import {
  useIntegration,
  useSetSkillCredentials,
  CredentialDialog,
} from "@/features/agents";
import {
  KeyIcon,
  CheckCircle2Icon,
  AlertCircleIcon,
  ExternalLinkIcon,
} from "lucide-react";

export default function IntegrationDetailPage({
  params,
}: {
  params: Promise<{ slug: string }>;
}) {
  const { slug } = use(params);

  const { integration, loading } = useIntegration(slug);
  const setCredentials = useSetSkillCredentials();
  const [credDialogOpen, setCredDialogOpen] = useState(false);

  if (!integration) {
    if (loading) {
      return (
        <>
          <PageHeader group="MCP Servers" page="Loading..." />
          <div className="flex flex-1 flex-col pb-4">
            <div className="flex items-start gap-4 mb-4">
              <Skeleton className="size-12 rounded-xl shrink-0" />
              <div className="flex-1 space-y-2">
                <Skeleton className="h-5 w-48" />
                <Skeleton className="h-4 w-96" />
              </div>
            </div>
            <div className="grid grid-cols-1 lg:grid-cols-[1fr_300px] gap-8 max-w-6xl">
              <div className="space-y-4">
                <Skeleton className="h-32 w-full rounded-xl" />
                <Skeleton className="h-48 w-full rounded-xl" />
              </div>
              <div className="hidden lg:block space-y-6">
                <Skeleton className="h-24 w-full rounded-xl" />
                <Skeleton className="h-32 w-full rounded-xl" />
              </div>
            </div>
          </div>
        </>
      );
    }
    return notFound();
  }

  async function handleSaveCredentials(values: Record<string, string>) {
    if (!integration) return;
    await setCredentials(integration.name, values);
  }

  const hasCredentialFields = integration.credentialFields.length > 0;
  const hasOAuth = Boolean(integration.oauthProvider);
  const needsConnection = hasCredentialFields || hasOAuth;

  function handleOAuthConnect() {
    if (!integration?.oauthProvider) return;
    const returnTo = `/integrations/${integration.name}`;
    window.location.assign(`/api/auth/${integration.oauthProvider}?returnTo=${encodeURIComponent(returnTo)}`);
  }

  return (
    <>
      <PageHeader
        group="MCP Servers"
        page={integration.title}
        action={
          hasOAuth ? (
            <Button
              size="sm"
              variant={integration.oauthConfigured ? "outline" : "default"}
              onClick={handleOAuthConnect}
            >
              <KeyIcon className="size-4" />
              {integration.oauthConfigured
                ? `Reconnect ${integration.oauthProvider}`
                : `Connect ${integration.oauthProvider}`}
            </Button>
          ) : hasCredentialFields ? (
            <Button
              size="sm"
              variant={integration.configured ? "outline" : "default"}
              onClick={() => setCredDialogOpen(true)}
            >
              <KeyIcon className="size-4" />
              {integration.configured ? "Reconfigure" : "Configure"}
            </Button>
          ) : undefined
        }
      />

      <div className="flex flex-1 flex-col pb-4">
        {/* Light header — logo, title, subtitle */}
        <div className="flex items-start gap-4 mb-6">
          <div
            className="size-12 shrink-0 rounded-xl [&>svg]:size-12"
            dangerouslySetInnerHTML={{ __html: integration.logo }}
          />
          <div className="flex-1 min-w-0">
            <div className="flex items-center gap-2">
              <h1 className="text-lg font-semibold">{integration.title}</h1>
              {needsConnection && (
                <div
                  className={cn(
                    "inline-flex items-center gap-1 rounded-full px-2 py-0.5 text-[10px] font-semibold uppercase tracking-widest",
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
            </div>
            {integration.subtitle && (
              <p className="text-sm text-muted-foreground mt-0.5">
                {integration.subtitle}
              </p>
            )}
          </div>
        </div>

        {/* Content: main + sidebar */}
        <div className="grid grid-cols-1 lg:grid-cols-[1fr_300px] gap-8 max-w-6xl">
          {/* Main content */}
          <div className="space-y-6 min-w-0">
            {/* Description */}
            <p className="text-sm text-muted-foreground leading-relaxed">
              {integration.description}
            </p>

            {/* Tools */}
            {integration.tools.length > 0 && (
              <div className="rounded-xl border border-border bg-card">
                <div className="px-4 py-3 border-b border-border">
                  <span className="text-sm font-medium">Tools</span>
                  <span className="ml-2 text-xs text-muted-foreground">
                    {integration.tools.length}
                  </span>
                </div>
                {integration.tools.map((tool, idx) => (
                  <div
                    key={tool.name}
                    className={`flex items-center gap-4 px-4 py-3${
                      idx < integration.tools.length - 1 ? " border-b border-border" : ""
                    }`}
                  >
                    <code className="rounded bg-muted px-2 py-1 font-mono text-xs shrink-0">
                      {tool.name}
                    </code>
                    <span className="text-sm text-muted-foreground">
                      {tool.description}
                    </span>
                  </div>
                ))}
              </div>
            )}
          </div>

          {/* Sidebar */}
          <aside className="hidden lg:block">
            <div className="sticky top-4 space-y-6 text-sm">
              {/* Details */}
              <div>
                <h4 className="text-xs font-semibold uppercase tracking-wider text-muted-foreground mb-3">
                  Details
                </h4>
                <dl className="space-y-2.5">
                  {integration.authorName && (
                    <div className="flex justify-between">
                      <dt className="text-muted-foreground">Author</dt>
                      <dd>
                        {integration.authorUrl ? (
                          <a href={integration.authorUrl} target="_blank" rel="noreferrer" className="hover:underline">
                            {integration.authorName}
                          </a>
                        ) : integration.authorName}
                      </dd>
                    </div>
                  )}
                  {integration.category && (
                    <div className="flex justify-between">
                      <dt className="text-muted-foreground">Category</dt>
                      <dd className="capitalize">{integration.category}</dd>
                    </div>
                  )}
                  <div className="flex justify-between">
                    <dt className="text-muted-foreground">Auth</dt>
                    <dd className="capitalize">{integration.oauthProvider ?? "Credentials"}</dd>
                  </div>
                  <div className="flex justify-between">
                    <dt className="text-muted-foreground">Identifier</dt>
                    <dd className="font-mono text-xs">{integration.name}</dd>
                  </div>
                  <div className="flex justify-between">
                    <dt className="text-muted-foreground">Tools</dt>
                    <dd>{integration.tools.length}</dd>
                  </div>
                  <div className="flex justify-between">
                    <dt className="text-muted-foreground">Built-in</dt>
                    <dd>{integration.isBuiltin ? "Yes" : "No"}</dd>
                  </div>
                </dl>
              </div>

              {/* Resources */}
              {(integration.documentationUrl || integration.repositoryUrl) && (
                <div>
                  <h4 className="text-xs font-semibold uppercase tracking-wider text-muted-foreground mb-3">
                    Resources
                  </h4>
                  <div className="space-y-2">
                    {integration.documentationUrl && (
                      <a
                        href={integration.documentationUrl}
                        target="_blank"
                        rel="noreferrer"
                        className="flex items-center gap-1.5 text-muted-foreground hover:text-foreground transition-colors"
                      >
                        <ExternalLinkIcon className="size-3.5" />
                        Documentation
                      </a>
                    )}
                    {integration.repositoryUrl && (
                      <a
                        href={integration.repositoryUrl}
                        target="_blank"
                        rel="noreferrer"
                        className="flex items-center gap-1.5 text-muted-foreground hover:text-foreground transition-colors"
                      >
                        <ExternalLinkIcon className="size-3.5" />
                        Source code
                      </a>
                    )}
                  </div>
                </div>
              )}

              {/* OAuth */}
              {hasOAuth && (
                <div>
                  <h4 className="text-xs font-semibold uppercase tracking-wider text-muted-foreground mb-3">
                    OAuth
                  </h4>
                  <div className="space-y-2">
                    <div className="flex justify-between">
                      <span className="text-muted-foreground">Provider</span>
                      <span className="capitalize">{integration.oauthProvider}</span>
                    </div>
                    <div className="flex justify-between">
                      <span className="text-muted-foreground">Status</span>
                      <span>{integration.oauthConfigured ? "Connected" : "Not connected"}</span>
                    </div>
                    <Button size="sm" className="w-full" variant="outline" onClick={handleOAuthConnect}>
                      <KeyIcon className="size-4" />
                      {integration.oauthConfigured ? "Reconnect" : "Connect"}
                    </Button>
                  </div>
                </div>
              )}

              {/* Credentials */}
              {hasCredentialFields && (
                <div>
                  <h4 className="text-xs font-semibold uppercase tracking-wider text-muted-foreground mb-3">
                    Credentials
                  </h4>
                  <div className="space-y-2">
                    {integration.credentialFields.map((field) => (
                      <div key={field.name} className="flex items-center gap-2">
                        <code className="rounded bg-muted px-1.5 py-0.5 font-mono text-[11px]">
                          {field.name}
                        </code>
                        {field.required && (
                          <span className="text-[10px] font-medium text-red-500 uppercase">Required</span>
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
          name={integration.title}
          slug={integration.name}
          logo={integration.logo}
          credentials={integration.credentialFields}
          onSave={handleSaveCredentials}
        />
      )}
    </>
  );
}
