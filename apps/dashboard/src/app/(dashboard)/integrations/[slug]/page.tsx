"use client";

import { use, useState } from "react";
import { notFound } from "next/navigation";
import { cn } from "@/lib/utils";
import { buildOAuthUrl } from "@/lib/auth-url";
import { PageHeader } from "@/components/page-header";
import { PageContainer } from "@/components/page-container";
import { Button } from "@/components/ui/button";
import { Skeleton } from "@/components/ui/skeleton";
import {
  CredentialDialog,
  useIntegration,
  useSetSkillCredentials,
} from "@/features/agents";
import {
  AlertCircleIcon,
  CheckCircle2Icon,
  ExternalLinkIcon,
  KeyIcon,
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
          <PageHeader
            group="MCP Servers"
            page="Loading..."
            width="thin"
          />
          <PageContainer width="thin" className="flex flex-1 flex-col gap-6 pb-4">
            <div className="flex items-start gap-4">
              <Skeleton className="size-12 shrink-0 rounded-xl" />
              <div className="flex-1 space-y-2">
                <Skeleton className="h-5 w-48" />
                <Skeleton className="h-4 w-96" />
              </div>
            </div>
            <Skeleton className="h-28 w-full rounded-xl" />
            <Skeleton className="h-48 w-full rounded-xl" />
          </PageContainer>
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
    window.location.assign(buildOAuthUrl(integration.oauthProvider, returnTo));
  }

  return (
    <>
      <PageHeader
        group="MCP Servers"
        page={integration.title}
        width="thin"
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

      <PageContainer width="thin" className="flex flex-1 flex-col gap-6 pb-4">
        <div className="flex items-start gap-4">
          <div
            className="size-12 shrink-0 [&>img]:size-12 [&>img]:object-contain [&>svg]:size-12"
            dangerouslySetInnerHTML={{ __html: integration.logo }}
          />
          <div className="min-w-0 flex-1">
            <div className="flex items-center gap-2">
              <h1 className="text-lg font-semibold">{integration.title}</h1>
              {needsConnection && (
                <div
                  className={cn(
                    "inline-flex items-center gap-1 rounded-full px-2 py-0.5 text-[10px] font-semibold uppercase tracking-widest",
                    integration.configured
                      ? "bg-emerald-100 text-emerald-800"
                      : "bg-amber-100 text-amber-800",
                  )}
                >
                  {integration.configured ? (
                    <CheckCircle2Icon className="size-3" />
                  ) : (
                    <AlertCircleIcon className="size-3" />
                  )}
                  {integration.configured
                    ? "Configured"
                    : "Credentials required"}
                </div>
              )}
            </div>
            {integration.subtitle && (
              <p className="mt-0.5 text-sm text-muted-foreground">
                {integration.subtitle}
              </p>
            )}
          </div>
        </div>

        <p className="text-sm leading-6 text-foreground/70">
          {integration.description}
        </p>

        {integration.tools.length > 0 && (
          <div className="rounded-xl border border-border bg-card">
            <div className="border-b border-border px-4 py-3">
              <span className="text-sm font-medium">Tools</span>
              <span className="ml-2 text-xs text-muted-foreground">
                {integration.tools.length}
              </span>
            </div>
            {integration.tools.map((tool, idx) => (
              <div
                key={tool.name}
                className={cn(
                  "flex items-center gap-4 px-4 py-3",
                  idx < integration.tools.length - 1 &&
                    "border-b border-border",
                )}
              >
                <code className="shrink-0 rounded bg-muted px-2 py-1 font-mono text-xs">
                  {tool.name}
                </code>
                <span className="text-sm text-foreground/70">
                  {tool.description}
                </span>
              </div>
            ))}
          </div>
        )}

        <div className="grid gap-4 md:grid-cols-2">
          <div className="rounded-xl border border-border bg-card p-4">
            <h3 className="mb-3 text-sm font-medium">Details</h3>
            <dl className="space-y-2.5 text-sm">
              {integration.authorName && (
                <div className="flex justify-between gap-4">
                  <dt className="text-muted-foreground">Author</dt>
                  <dd className="text-right">
                    {integration.authorUrl ? (
                      <a
                        href={integration.authorUrl}
                        target="_blank"
                        rel="noreferrer"
                        className="hover:underline"
                      >
                        {integration.authorName}
                      </a>
                    ) : (
                      integration.authorName
                    )}
                  </dd>
                </div>
              )}
              {integration.category && (
                <div className="flex justify-between gap-4">
                  <dt className="text-muted-foreground">Category</dt>
                  <dd className="capitalize">{integration.category}</dd>
                </div>
              )}
              <div className="flex justify-between gap-4">
                <dt className="text-muted-foreground">Auth</dt>
                <dd className="capitalize">
                  {integration.oauthProvider ?? "Credentials"}
                </dd>
              </div>
              <div className="flex justify-between gap-4">
                <dt className="text-muted-foreground">Identifier</dt>
                <dd className="font-mono text-xs">{integration.name}</dd>
              </div>
              <div className="flex justify-between gap-4">
                <dt className="text-muted-foreground">Tools</dt>
                <dd>{integration.tools.length}</dd>
              </div>
              <div className="flex justify-between gap-4">
                <dt className="text-muted-foreground">Built-in</dt>
                <dd>{integration.isBuiltin ? "Yes" : "No"}</dd>
              </div>
            </dl>
          </div>

          {(integration.documentationUrl || integration.repositoryUrl) && (
            <div className="rounded-xl border border-border bg-card p-4">
              <h3 className="mb-3 text-sm font-medium">Resources</h3>
              <div className="space-y-2 text-sm">
                {integration.documentationUrl && (
                  <a
                    href={integration.documentationUrl}
                    target="_blank"
                    rel="noreferrer"
                    className="flex items-center gap-1.5 text-muted-foreground transition-colors hover:text-foreground"
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
                    className="flex items-center gap-1.5 text-muted-foreground transition-colors hover:text-foreground"
                  >
                    <ExternalLinkIcon className="size-3.5" />
                    Source code
                  </a>
                )}
              </div>
            </div>
          )}

          {hasOAuth && (
            <div className="rounded-xl border border-border bg-card p-4">
              <h3 className="mb-3 text-sm font-medium">OAuth</h3>
              <div className="space-y-2 text-sm">
                <div className="flex justify-between">
                  <span className="text-muted-foreground">Provider</span>
                  <span className="capitalize">{integration.oauthProvider}</span>
                </div>
                <div className="flex justify-between">
                  <span className="text-muted-foreground">Status</span>
                  <span>
                    {integration.oauthConfigured ? "Connected" : "Not connected"}
                  </span>
                </div>
                <Button
                  size="sm"
                  className="w-full"
                  variant="outline"
                  onClick={handleOAuthConnect}
                >
                  <KeyIcon className="size-4" />
                  {integration.oauthConfigured ? "Reconnect" : "Connect"}
                </Button>
              </div>
            </div>
          )}

          {hasCredentialFields && (
            <div className="rounded-xl border border-border bg-card p-4">
              <h3 className="mb-3 text-sm font-medium">Credentials</h3>
              <div className="space-y-2">
                {integration.credentialFields.map((field) => (
                  <div key={field.name} className="flex items-center gap-2">
                    <code className="rounded bg-muted px-1.5 py-0.5 font-mono text-[11px]">
                      {field.name}
                    </code>
                    {field.required && (
                      <span className="text-[10px] font-medium uppercase text-red-500">
                        Required
                      </span>
                    )}
                  </div>
                ))}
              </div>
            </div>
          )}
        </div>
      </PageContainer>

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
